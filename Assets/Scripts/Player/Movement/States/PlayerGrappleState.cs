using UnityEngine;

namespace Player.Movement.States
{
    public sealed class PlayerGrappleState : PlayerMovementState
    {
        private PlayerMovementConfig.GrappleSettings config;
        private PlayerMovementConfig.WallRunSettings wallRunConfig;

        public override void Initialize(PlayerMovement movement)
        {
            config = movement.config.Grapple;
            wallRunConfig = movement.config.WallRun;
        }

        private float elapsed;
        private float blockedTime;
        private Vector3 velocity;
        private Vector3 approachDirection;
        private Vector3 anchor;
        private Vector3 surfaceNormal;
        private bool flyThrough;

        public Vector3 Anchor => anchor;

        public void Begin(PlayerMovement movement, Vector3 anchorPoint, Vector3 normal, bool passThrough)
        {
            anchor = anchorPoint;
            surfaceNormal = normal.normalized;
            flyThrough = passThrough;
            movement.ChangeState(this);
        }

        public override void Enter(PlayerMovement movement)
        {
            elapsed = blockedTime = 0f;
            velocity = movement.Velocity + Vector3.up * movement.VerticalVelocity;
            approachDirection = (anchor - movement.Player.Controller.bounds.center).normalized;
            movement.AirborneState.SetMomentumPreservation(true);
            movement.AirborneState.ConsumeCoyoteTime();
            movement.Player.Input.UseJumpInput();
            movement.Player.Input.ConsumeJumpRelease();
        }

        public override void Tick(PlayerMovement movement, float dt)
        {
            if (!movement.CanMove || movement.Player.GrappleController == null ||
                !movement.Player.GrappleController.TryGetAnchor(out anchor) || elapsed >= config.Timeout)
            {
                Leave(movement);
                return;
            }
            if (dt <= 0f) return;

            int steps = Mathf.Clamp(Mathf.CeilToInt(dt * 120f), 1, 32);
            float step = Mathf.Min(dt, 0.25f) / steps;
            for (int i = 0; i < steps; i++)
            {
                elapsed += step;
                Vector3 center = movement.Player.Controller.bounds.center;
                Vector3 target = anchor;
                if (!flyThrough)
                {
                    Vector3 extent = movement.Player.Controller.bounds.extents;
                    float capsuleRadius = Mathf.Max(extent.x, extent.z);
                    float support = capsuleRadius +
                        Mathf.Max(0f, extent.y - capsuleRadius) * Mathf.Abs(surfaceNormal.y);
                    target += surfaceNormal * (support + 0.15f);
                }

                Vector3 delta = target - center;
                float distance = delta.magnitude;
                float radius = flyThrough ? config.ArrivalRadius : 0.1f;
                if (distance <= radius + 0.01f)
                {
                    Launch(movement);
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

                movement.Velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
                movement.VerticalVelocity = velocity.y;
                Vector3 before = movement.Player.Controller.transform.position;
                movement.Player.Controller.Move(motion);
                float moved = Vector3.Distance(before, movement.Player.Controller.transform.position);
                blockedTime = motion.magnitude > 0.002f && moved < motion.magnitude * 0.15f
                    ? blockedTime + step : 0f;
                if (blockedTime > 0.18f)
                {
                    Leave(movement);
                    return;
                }
            }
        }

        private void Launch(PlayerMovement movement)
        {
            if (flyThrough)
            {
                Vector3 launch = approachDirection * config.FlySpeed + Vector3.up * config.FlyLift;
                movement.Velocity = Vector3.ProjectOnPlane(launch, Vector3.up);
                movement.VerticalVelocity = launch.y;
                movement.NotifyGrappleLaunched();
                movement.Player.Camera?.Shake(0.22f, 0.09f);
                movement.ChangeState(movement.AirborneState);
                return;
            }

            if (Mathf.Abs(surfaceNormal.y) < 0.2f)
            {
                movement.WallRunState.Begin(movement, surfaceNormal, movement.Player.GrappleController.AnchorLayerMask);
            }
            else
            {
                movement.Velocity = Vector3.ProjectOnPlane(movement.Velocity, surfaceNormal);
                movement.VerticalVelocity = 0f;
                Leave(movement);
            }
        }

        private static void Leave(PlayerMovement player)
        {
            player.ResumeLocomotion();
        }

        public override void Exit(PlayerMovement movement)
        {
            movement.AirborneState.ConsumeCoyoteTime();
            movement.WallRunState.StartCooldown(Mathf.Max(0.4f, wallRunConfig.Cooldown));
            movement.Player.Input.UseJumpInput();
            movement.Player.Input.ConsumeJumpRelease();
        }
    }
}
