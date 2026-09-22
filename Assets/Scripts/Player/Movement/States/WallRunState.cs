using UnityEngine;

namespace Player.Movement
{
    public sealed class WallRunState : PlayerMovementState
    {
        private float timeRemaining;
        private float gravityRampTimer;
        private Vector3 runDirection;
        private bool enteredFromGrapple;
        private int grappleWallLayers;

        public bool CanStart(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.WallRunSettings config = player.Config.WallRun;
            if (!data.CanMove || player.Controller.isGrounded || data.WallRunCooldownTimer > 0f)
                return false;
            if (player.Input.MoveInput.y < config.MinInput)
                return false;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(data.Velocity, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            if (planarVelocity.magnitude < config.MinSpeed || Vector3.Dot(forward, planarVelocity) <= 0f)
                return false;

            return TryFindWall(player, out Vector3 normal, out RaycastHit hit) &&
                   !IsConsumedSurface(data, normal, hit.point);
        }

        public override void Enter(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.WallRunSettings config = player.Config.WallRun;
            enteredFromGrapple = data.GrappleWallRunEntry;
            data.IsGrappleWallRunning = enteredFromGrapple;
            data.GrappleWallRunEntry = false;
            grappleWallLayers = enteredFromGrapple && player.Grapple != null
                ? player.Grapple.AnchorLayerMask : 0;

            if (enteredFromGrapple)
            {
                data.WallNormal = data.GrappleNormal;
                data.WallRunCooldownTimer = 0f;
            }

            data.PreserveAirMomentum = false;
            timeRemaining = config.Duration;
            gravityRampTimer = 0f;
            if (!FindCurrentWall(player, out Vector3 entryNormal, out RaycastHit hit))
            {
                data.PreserveAirMomentum = enteredFromGrapple;
                data.WallRunCooldownTimer = config.Cooldown;
                player.ChangeState(player.AirborneState);
                return;
            }

            data.WallNormal = entryNormal;
            data.HasConsumedWallRunSurface = true;
            data.LastWallRunNormal = data.WallNormal;
            data.LastWallRunPoint = hit.point;
            runDirection = GetRunDirection(player, data.WallNormal);

            if (enteredFromGrapple)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, data.WallNormal).normalized;
                float sideInput = Vector3.Dot(player.transform.right * player.Input.MoveInput.x, tangent);
                if (Mathf.Abs(sideInput) > 0.1f)
                    runDirection = tangent * Mathf.Sign(sideInput);
                else if (Mathf.Abs(Vector3.Dot(player.transform.forward, tangent)) < 0.1f &&
                         Mathf.Abs(Vector3.Dot(data.Velocity, tangent)) < 0.1f)
                    runDirection = tangent * (Vector3.Dot(player.transform.right, tangent) >= 0f ? 1f : -1f);

                float carry = Mathf.Max(config.MinSpeed, Vector3.Dot(data.Velocity, runDirection));
                data.Velocity = runDirection * Mathf.Min(carry, config.Speed);
            }

            data.WallRunDirection = runDirection;
            data.VerticalVelocity = 0f;
        }

        public override void Tick(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.WallRunSettings config = player.Config.WallRun;
            timeRemaining -= dt;
            bool stopInput = enteredFromGrapple
                ? player.Input.MoveInput.y < -config.MinInput
                : player.Input.MoveInput.y < config.MinInput;
            if (!data.CanMove || stopInput || !FindCurrentWall(player, out Vector3 wallNormal, out _))
            {
                LeaveWall(player);
                return;
            }

            data.WallNormal = wallNormal;
            Vector3 desiredDirection = GetRunDirection(player, wallNormal);
            if (Vector3.Dot(desiredDirection, runDirection) < 0f) desiredDirection = runDirection;
            runDirection = Vector3.Slerp(runDirection, desiredDirection, 12f * dt).normalized;
            data.WallRunDirection = runDirection;

            if (player.Input.JumpInput)
            {
                player.Input.UseJumpInput();
                player.Input.ConsumeJumpRelease();
                data.Velocity = wallNormal * config.JumpHorizontalSpeed + runDirection * config.JumpForwardCarry;
                data.VerticalVelocity = Mathf.Sqrt(config.JumpHeight * 2f * player.Config.Jump.Gravity);
                data.WallRunCooldownTimer = config.Cooldown;
                player.ChangeState(player.AirborneState);
                return;
            }

            Vector3 planarVelocity = Vector3.MoveTowards(
                Vector3.ProjectOnPlane(data.Velocity, Vector3.up),
                runDirection * config.Speed,
                config.Acceleration * dt);
            data.Velocity = planarVelocity;

            if (timeRemaining > 0f)
                data.VerticalVelocity = 0f;
            else
            {
                gravityRampTimer += dt;
                float gravityBlend = Mathf.Clamp01(gravityRampTimer / config.GravityRampDuration);
                data.VerticalVelocity = Mathf.Max(
                    data.VerticalVelocity - player.Config.Jump.Gravity * gravityBlend * dt,
                    -player.Config.Jump.MaxFallSpeed);
                if (gravityBlend >= 1f)
                {
                    LeaveWall(player);
                    return;
                }
            }

            Vector3 motion = planarVelocity + Vector3.up * data.VerticalVelocity - wallNormal * config.StickForce;
            player.Controller.Move(motion * dt);
            if (player.Controller.isGrounded)
                player.ChangeState(player.GroundedState);
        }

        public override void Exit(PlayerMovement player)
        {
            player.Data.IsGrappleWallRunning = false;
            enteredFromGrapple = false;
        }

        private bool FindCurrentWall(PlayerMovement player, out Vector3 normal, out RaycastHit hit)
        {
            PlayerMovementRuntimeData data = player.Data;
            if (!enteredFromGrapple) return TryFindWall(player, out normal, out hit);

            float radius = Mathf.Max(0.05f, player.Controller.radius * 0.65f);
            if (Physics.SphereCast(player.Controller.bounds.center, radius, -data.WallNormal,
                out hit, player.Config.WallRun.CheckDistance, grappleWallLayers, QueryTriggerInteraction.Ignore)
                && IsRunnable(hit.normal) && Vector3.Dot(hit.normal, data.WallNormal) > 0.5f)
            {
                normal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                return true;
            }

            normal = Vector3.zero;
            hit = default;
            return false;
        }

        private static void LeaveWall(PlayerMovement player)
        {
            player.Data.WallRunCooldownTimer = player.Config.WallRun.Cooldown;
            player.ChangeState(player.AirborneState);
        }

        private static bool IsConsumedSurface(PlayerMovementRuntimeData data, Vector3 normal, Vector3 point)
        {
            if (!data.HasConsumedWallRunSurface) return false;
            bool sameFacing = Vector3.Dot(normal, data.LastWallRunNormal) > 0.97f;
            float planeDistance = Mathf.Abs(Vector3.Dot(point - data.LastWallRunPoint, data.LastWallRunNormal));
            return sameFacing && planeDistance < 0.4f;
        }

        private static Vector3 GetRunDirection(PlayerMovement player, Vector3 wallNormal)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            Vector3 look = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up);
            float facing = Vector3.Dot(look, tangent);
            if (Mathf.Abs(facing) < 0.1f) facing = Vector3.Dot(tangent, player.Data.Velocity);
            return facing >= 0f ? tangent : -tangent;
        }

        private static bool TryFindWall(PlayerMovement player, out Vector3 wallNormal, out RaycastHit wallHit)
        {
            PlayerMovementConfig.WallRunSettings config = player.Config.WallRun;
            Vector3 center = player.Controller.bounds.center;
            float radius = Mathf.Max(0.05f, player.Controller.radius * 0.65f);
            bool leftHit = Physics.SphereCast(center, radius, -player.transform.right, out RaycastHit left,
                config.CheckDistance, config.Layers, QueryTriggerInteraction.Ignore) && IsRunnable(left.normal);
            bool rightHit = Physics.SphereCast(center, radius, player.transform.right, out RaycastHit right,
                config.CheckDistance, config.Layers, QueryTriggerInteraction.Ignore) && IsRunnable(right.normal);

            if (!leftHit && !rightHit)
            {
                wallNormal = Vector3.zero;
                wallHit = default;
                return false;
            }

            wallHit = leftHit && rightHit ? (left.distance <= right.distance ? left : right) : (leftHit ? left : right);
            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            return true;
        }

        private static bool IsRunnable(Vector3 normal) => Mathf.Abs(normal.y) < 0.2f;
    }
}
