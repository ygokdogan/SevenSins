using Core;
using UnityEngine;

namespace Player.Movement
{
    public sealed class WallRunState : IState<MovementContext>
    {
        private float timeRemaining;
        private float gravityRampTimer;
        private Vector3 runDirection;

        public static bool CanStart(MovementContext ctx)
        {
            if (!ctx.CanMove || ctx.Controller.isGrounded || ctx.WallRunCooldownTimer > 0f)
                return false;
            if (ctx.Input.MoveInput.y < ctx.WallRunMinInput)
                return false;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(ctx.Velocity, Vector3.up);
            Vector3 forward = ctx.Transform.forward;
            forward.y = 0f;
            
            if (planarVelocity.magnitude < ctx.WallRunMinSpeed)
                return false;
            
            var dotProduct = Vector3.Dot(forward, planarVelocity);
            if(dotProduct <= 0f)
                return false;

            return TryFindWall(ctx, out Vector3 wallNormal, out RaycastHit hit) &&
                   !IsConsumedSurface(ctx, wallNormal, hit.point);
        }

        public void Enter(MovementContext ctx)
        {
            timeRemaining = ctx.WallRunDuration;
            gravityRampTimer = 0f;
            if (!TryFindWall(ctx, out ctx.WallNormal, out RaycastHit hit))
            {
                ctx.RequestTransition(ctx.States.Airborne);
                return;
            }

            ctx.HasConsumedWallRunSurface = true;
            ctx.LastWallRunNormal = ctx.WallNormal;
            ctx.LastWallRunPoint = hit.point;
            runDirection = GetRunDirection(ctx, ctx.WallNormal);
            ctx.VerticalVelocity = 0f;
        }

        public void Tick(MovementContext ctx, float dt)
        {
            timeRemaining -= dt;
            if (!ctx.CanMove || ctx.Input.MoveInput.y < ctx.WallRunMinInput ||
                !TryFindWall(ctx, out Vector3 wallNormal, out _))
            {
                LeaveWall(ctx);
                return;
            }

            ctx.WallNormal = wallNormal;
            Vector3 desiredDirection = GetRunDirection(ctx, wallNormal);
            if (Vector3.Dot(desiredDirection, runDirection) < 0f)
                desiredDirection = runDirection;
            runDirection = Vector3.Slerp(runDirection, desiredDirection, 12f * dt).normalized;

            if (ctx.Input.JumpInput)
            {
                ctx.Input.UseJumpInput();
                ctx.Input.ConsumeJumpRelease();
                ctx.Velocity = wallNormal * ctx.WallJumpHorizontalSpeed + runDirection * ctx.WallJumpForwardCarry;
                ctx.VerticalVelocity = Mathf.Sqrt(ctx.WallJumpHeight * -2f * ctx.Gravity);
                ctx.WallRunCooldownTimer = ctx.WallRunCooldown;
                ctx.RequestTransition(ctx.States.Airborne);
                return;
            }

            Vector3 planarVelocity = Vector3.MoveTowards(
                Vector3.ProjectOnPlane(ctx.Velocity, Vector3.up),
                runDirection * ctx.WallRunSpeed,
                ctx.WallRunAcceleration * dt);
            ctx.Velocity = planarVelocity;

            if (timeRemaining > 0f)
            {
                ctx.VerticalVelocity = 0f;
            }
            else
            {
                gravityRampTimer += dt;
                float gravityBlend = Mathf.Clamp01(gravityRampTimer / ctx.WallRunGravityRampDuration);
                float currentGravity = Mathf.Lerp(0f, ctx.Gravity, gravityBlend);
                ctx.VerticalVelocity = Mathf.Max(
                    ctx.VerticalVelocity + currentGravity * dt,
                    -ctx.MaxFallSpeed);

                if (gravityBlend >= 1f)
                {
                    ctx.WallRunCooldownTimer = ctx.WallRunCooldown;
                    ctx.RequestTransition(ctx.States.Airborne);
                    return;
                }
            }

            Vector3 motion = planarVelocity + Vector3.up * ctx.VerticalVelocity - wallNormal * ctx.WallStickForce;
            ctx.Controller.Move(motion * dt);

            if (ctx.Controller.isGrounded)
                ctx.RequestTransition(ctx.States.Grounded);
        }

        public void Exit(MovementContext ctx) { }

        private static void LeaveWall(MovementContext ctx)
        {
            ctx.WallRunCooldownTimer = ctx.WallRunCooldown;
            ctx.RequestTransition(ctx.States.Airborne);
        }

        private static bool IsConsumedSurface(MovementContext ctx, Vector3 normal, Vector3 point)
        {
            if (!ctx.HasConsumedWallRunSurface) return false;
            bool sameFacing = Vector3.Dot(normal, ctx.LastWallRunNormal) > 0.97f;
            float planeDistance = Mathf.Abs(Vector3.Dot(point - ctx.LastWallRunPoint, ctx.LastWallRunNormal));
            return sameFacing && planeDistance < 0.4f;
        }

        private static Vector3 GetRunDirection(MovementContext ctx, Vector3 wallNormal)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            Vector3 look = ctx.Transform.forward;
            look.y = 0f;
            
            float facing = Vector3.Dot(look, tangent);
            if (Mathf.Abs(facing) < 0.1f)
                facing = Vector3.Dot(tangent, ctx.Velocity);

            return facing >= 0f ? tangent : -tangent;
        }

        private static bool TryFindWall(MovementContext ctx, out Vector3 wallNormal, out RaycastHit wallHit)
        {
            Vector3 center = ctx.Controller.bounds.center;
            float radius = Mathf.Max(0.05f, ctx.Controller.radius * 0.65f);
            float distance = ctx.WallCheckDistance;

            bool leftHit = Physics.SphereCast(center, radius, -ctx.Transform.right, out RaycastHit left,
                distance, ctx.WallLayers, QueryTriggerInteraction.Ignore) && IsRunnable(left.normal);
            bool rightHit = Physics.SphereCast(center, radius, ctx.Transform.right, out RaycastHit right,
                distance, ctx.WallLayers, QueryTriggerInteraction.Ignore) && IsRunnable(right.normal);

            if (!leftHit && !rightHit)
            {
                wallNormal = Vector3.zero;
                wallHit = default;
                return false;
            }

            if (leftHit && rightHit)
                wallHit = left.distance <= right.distance ? left : right;
            else
                wallHit = leftHit ? left : right;

            wallNormal = wallHit.normal;
            wallNormal.y = 0f;
            wallNormal.Normalize();
            return true;
        }

        private static bool IsRunnable(Vector3 normal) => Mathf.Abs(normal.y) < 0.2f;
    }
}
