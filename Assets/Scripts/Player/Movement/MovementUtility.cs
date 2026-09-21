using UnityEngine;

namespace Player.Movement
{
    public static class MovementUtility
    {
        public static void UpdatePlanarVelocity(MovementContext ctx, float dt, float controlMultiplier)
        {
            if (!ctx.CanMove) return;
            
            Vector2 input = Vector2.ClampMagnitude(ctx.Input.MoveInput, 1f);
            float targetSpeed = (ctx.IsSprinting ? ctx.SprintSpeed : ctx.WalkSpeed) * ctx.SpeedMultiplier;
            Vector3 targetVelocity = (ctx.Transform.right * input.x + ctx.Transform.forward * input.y) * targetSpeed;
            targetVelocity.y = 0f;

            Vector3 currentVelocity = new Vector3(ctx.Velocity.x, 0f, ctx.Velocity.z);
            bool isChangingDirection = input.sqrMagnitude > 0.001f &&
                                     currentVelocity.sqrMagnitude > 0.001f &&
                                     Vector3.Dot(currentVelocity.normalized, targetVelocity.normalized) < 0f;

            float rate = isChangingDirection
                ? ctx.TurnAcceleration
                : input.sqrMagnitude > 0.001f ? ctx.Acceleration : ctx.Deceleration;
            rate *= controlMultiplier;
            
            Vector3 newVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, rate * dt);
            ctx.Velocity = new Vector3(newVelocity.x, 0f, newVelocity.z);
        }

        public static void StartJump(MovementContext ctx)
        {
            ctx.VerticalVelocity = Mathf.Sqrt(ctx.JumpHeight * -2f * ctx.Gravity);
            ctx.CoyoteTimer = 0f;
        }
    }
}
