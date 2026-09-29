using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyHitState : IState<EnemyBase>
    {
        private float hitTimer;
        
        public void Enter(EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            enemy.IsAnimationFinished = false;
            enemy.IsAttackWindowOpen = false;
            
            enemy.CurrentHealth -= enemy.CurrentHit.damage;
            
            enemy.HitFlash?.TriggerFlash();
            enemy.HitJuice?.CalculateHitReaction(enemy.CurrentHit);
            enemy.HitFx?.PlayHitSound(enemy.CurrentHit.impactMultiplier);
            enemy.HitFx?.SpawnHitVfx(enemy.CurrentHit);

            hitTimer = 0.2f * Mathf.Max(0f, enemy.CurrentHit.impactMultiplier);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            hitTimer -= dt;
            
            if (hitTimer <= 0f) enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(EnemyBase enemy)
        {
            enemy.IsAnimationFinished = false;
        }
    }
}
