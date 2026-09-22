using UnityEngine;

namespace Player.Movement
{
    public sealed class PlayerGrappleState : PlayerMovementState
    {
        private float elapsed;
        private float blockedTime;
        private Vector3 velocity;
        private Vector3 approachDirection;

        public override void Enter(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            elapsed = blockedTime = 0f;
            velocity = data.Velocity + Vector3.up * data.VerticalVelocity;
            approachDirection = (data.GrappleAnchor - player.Controller.bounds.center).normalized;
            data.PreserveAirMomentum = true;
            data.CoyoteTimer = 0f;
            player.Input.UseJumpInput();
            player.Input.ConsumeJumpRelease();
        }

        public override void Tick(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.GrappleSettings config = player.Config.Grapple;
            if (!data.GrappleHeld || !data.CanMove || player.Grapple == null ||
                !player.Grapple.TryGetAnchor(out data.GrappleAnchor) || elapsed >= config.Timeout)
            {
                Leave(player);
                return;
            }
            if (dt <= 0f) return;

            int steps = Mathf.Clamp(Mathf.CeilToInt(dt * 120f), 1, 32);
            float step = Mathf.Min(dt, 0.25f) / steps;
            for (int i = 0; i < steps; i++)
            {
                elapsed += step;
                Vector3 center = player.Controller.bounds.center;
                Vector3 target = data.GrappleAnchor;
                if (!data.GrappleFlyThrough)
                {
                    Vector3 extent = player.Controller.bounds.extents;
                    float capsuleRadius = Mathf.Max(extent.x, extent.z);
                    float support = capsuleRadius +
                        Mathf.Max(0f, extent.y - capsuleRadius) * Mathf.Abs(data.GrappleNormal.y);
                    target += data.GrappleNormal * (support + 0.15f);
                }

                Vector3 delta = target - center;
                float distance = delta.magnitude;
                float radius = data.GrappleFlyThrough ? config.ArrivalRadius : 0.1f;
                if (distance <= radius + 0.01f)
                {
                    Launch(player);
                    return;
                }

                Vector3 direction = delta / distance;
                approachDirection = direction;
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / config.RampTime);
                float speed = Mathf.Lerp(4f, config.PullSpeed, blend);
                velocity = Vector3.Lerp(velocity, direction * speed,
                    1f - Mathf.Exp(-config.Steering * step));
                Vector3 motion = velocity * step;
                float forwardStep = Vector3.Dot(motion, direction);
                if (forwardStep > distance - radius)
                    motion *= (distance - radius) / forwardStep;

                data.Velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
                data.VerticalVelocity = velocity.y;
                Vector3 before = player.Controller.transform.position;
                player.Controller.Move(motion);
                float moved = Vector3.Distance(before, player.Controller.transform.position);
                blockedTime = motion.magnitude > 0.002f && moved < motion.magnitude * 0.15f
                    ? blockedTime + step : 0f;
                if (blockedTime > 0.18f)
                {
                    Leave(player);
                    return;
                }
            }
        }

        private void Launch(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.GrappleSettings config = player.Config.Grapple;
            if (data.GrappleFlyThrough)
            {
                Vector3 launch = approachDirection * config.FlySpeed + Vector3.up * config.FlyLift;
                data.Velocity = Vector3.ProjectOnPlane(launch, Vector3.up);
                data.VerticalVelocity = launch.y;
                player.NotifyGrappleLaunched();
                player.ChangeState(player.AirborneState);
                return;
            }

            if (Mathf.Abs(data.GrappleNormal.y) < 0.2f)
            {
                data.GrappleWallRunEntry = true;
                player.ChangeState(player.WallRunState);
            }
            else
            {
                data.Velocity = Vector3.ProjectOnPlane(data.Velocity, data.GrappleNormal);
                data.VerticalVelocity = 0f;
                Leave(player);
            }
        }

        private static void Leave(PlayerMovement player)
        {
            player.ResumeLocomotion();
        }

        public override void Exit(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            data.GrappleHeld = false;
            data.CoyoteTimer = 0f;
            data.WallRunCooldownTimer = Mathf.Max(0.4f, player.Config.WallRun.Cooldown);
            player.Input.UseJumpInput();
            player.Input.ConsumeJumpRelease();
        }
    }
}
