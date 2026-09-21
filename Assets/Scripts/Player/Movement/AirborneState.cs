using Core;
using UnityEngine;

namespace Player.Movement
{
    public class AirborneState : 
        IState<MovementContext>
    {
        public void Enter(MovementContext ctx) { }

        public void Tick(MovementContext ctx, float dt)
        {
            ctx.WallRunCooldownTimer = Mathf.Max(0f, ctx.WallRunCooldownTimer - dt);
            MovementUtility.UpdatePlanarVelocity(ctx, dt, ctx.AirControl);
            
            ctx.CoyoteTimer -= dt;
            if (ctx.CanMove && ctx.Input.JumpInput && ctx.CoyoteTimer > 0f)
            {
                MovementUtility.StartJump(ctx);
                ctx.Input.UseJumpInput();
            }
            
            if (ctx.Input.JumpInputReleased && ctx.VerticalVelocity > 0f)
            {
                ctx.VerticalVelocity *= ctx.JumpCutMultiplier;
                ctx.Input.ConsumeJumpRelease();
            }

            if (WallRunState.CanStart(ctx))
            {
                ctx.RequestTransition(ctx.States.WallRun);
                return;
            }

            float gravityMultiplier = ctx.VerticalVelocity > 0f
                ? (ctx.Input.IsJumpHeld ? 1f : ctx.LowJumpGravityMultiplier)
                : ctx.FallGravityMultiplier;
            ctx.VerticalVelocity = Mathf.Max(
                ctx.VerticalVelocity + ctx.Gravity * gravityMultiplier * dt,
                -ctx.MaxFallSpeed);

            ctx.Controller.Move(new Vector3(ctx.Velocity.x, ctx.VerticalVelocity, ctx.Velocity.z) * dt);

            if (ctx.Controller.isGrounded && ctx.VerticalVelocity <= 0f)
                ctx.RequestTransition(ctx.States.Grounded);
        }

        public void Exit(MovementContext ctx) { }
    }
}
