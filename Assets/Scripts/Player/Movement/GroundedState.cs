using Core;
using UnityEngine;

namespace Player.Movement
{
    public class GroundedState : IState<MovementContext>
    {
        public void Enter(MovementContext ctx)
        {
            float landingSpeed = Mathf.Max(0f, -ctx.VerticalVelocity);
            if (landingSpeed > 0f)
                ctx.NotifyLanded?.Invoke(landingSpeed);

            ctx.CoyoteTimer = ctx.CoyoteTime;
            ctx.WallRunCooldownTimer = 0f;
            ctx.HasConsumedWallRunSurface = false;
            ctx.LastWallRunNormal = Vector3.zero;
            ctx.WallNormal = Vector3.zero;
            if (ctx.VerticalVelocity < 0f) ctx.VerticalVelocity = -2f;
        }

        public void Tick(MovementContext ctx, float dt)
        {
            MovementUtility.UpdatePlanarVelocity(ctx, dt, 1f);

            if (ctx.CanMove && ctx.Input.JumpInput)
            {
                MovementUtility.StartJump(ctx);
                ctx.Input.UseJumpInput();
                ctx.RequestTransition(ctx.States.Airborne);
                return;
            }

            ctx.Controller.Move(new Vector3(ctx.Velocity.x, -2f, ctx.Velocity.z) * dt);

            if (!ctx.Controller.isGrounded)
                ctx.RequestTransition(ctx.States.Airborne);
        }

        public void Exit(MovementContext ctx) { }
    }
}
