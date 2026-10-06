using Core;
using UnityEngine;

namespace Enemies.States
{
    public class EnemyHitState : IState<Core.EnemyBase>
    {
        private float hitTimer;
        
        public void Enter(Core.EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            enemy.IsAnimationFinished = false;
            enemy.IsAttackWindowOpen = false;
            
            hitTimer = 0.2f * Mathf.Max(0f, enemy.CurrentHit.impactMultiplier);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            hitTimer -= dt;
            
            if (hitTimer <= 0f) enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(Core.EnemyBase enemy)
        {
            enemy.IsAnimationFinished = false;
        }
    }
}
