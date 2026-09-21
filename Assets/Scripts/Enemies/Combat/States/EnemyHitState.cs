using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyHitState : IState<EnemyCombatContext>
    {
        private float hitTimer;
        
        public void Enter(EnemyCombatContext ctx)
        {
            ctx.Movement.Stop();
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            ctx.IsAnimationFinished = false;
            ctx.IsAttackWindowOpen = false;
            
            ctx.CurrentHealth -= ctx.CurrentHit.damage;
            
            ctx.HitFlash?.TriggerFlash();
            ctx.HitJuice?.CalculateHitReaction(ctx.CurrentHit);
            ctx.HitFx?.PlayHitSound(ctx.CurrentHit.impactMultiplier);
            ctx.HitFx?.SpawnHitVfx(ctx.CurrentHit);

            hitTimer = 0.2f * Mathf.Max(0f, ctx.CurrentHit.impactMultiplier);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            hitTimer -= dt;
            
            if (hitTimer <= 0f) ctx.RequestTransition(ctx.States.Idle);
        }

        public void Exit(EnemyCombatContext ctx)
        {
            ctx.IsAnimationFinished = false;
        }
    }
}
