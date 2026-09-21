using Core;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatHitState : IState<CombatContext>
    {
        private float remaining;

        public void Enter(CombatContext ctx)
        {
            remaining = ctx.HitRecoveryDuration * Mathf.Max(0f, ctx.CurrentHit.impactMultiplier);
            ctx.CurrentAttack = null;
            ctx.IsAttackWindowOpen = false;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = false;
            ctx.Input.UsePrimaryAttackInput();
            ctx.Input.UseSheatheInput();
            ctx.Input.UseBlockInput();
            ctx.Input.UseJumpInput();
            ctx.Input.ConsumeJumpRelease();

            ctx.Movement.CanMove = false;
            ctx.Movement.CanSprint = false;
            ctx.Movement.Velocity = UnityEngine.Vector3.zero;
            ctx.AnimationManager.PlayHitReaction(ctx.IsSheathed);
        }

        public void Tick(CombatContext ctx, float dt)
        {
            remaining -= dt;
            if (remaining <= 0f) ctx.RequestTransition(ctx.States.Idle);
        }

        public void Exit(CombatContext ctx)
        {
            ctx.Movement.CanMove = true;
            ctx.Movement.CanSprint = true;
        }
    }
}
