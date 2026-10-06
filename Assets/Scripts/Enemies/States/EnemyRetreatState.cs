using Core;
using UnityEngine;

namespace Enemies.States
{
    public class EnemyRetreatState : IState<Core.EnemyBase>
    {
        private float minimumTimeRemaining;
        private float timeoutRemaining;
        private bool hasDestination;
        private Vector3 retreatDestination;

        public void Enter(Core.EnemyBase enemy)
        {
            minimumTimeRemaining = enemy.Config.retreatDuration;
            timeoutRemaining = enemy.Config.retreatDuration +
                enemy.Config.retreatDistance / Mathf.Max(enemy.Config.walkSpeed, 0.1f) + 0.5f;
            hasDestination = false;
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            hasDestination = enemy.Movement.TryRetreat(enemy.Perception.Target, enemy.Config.retreatDistance,
                enemy.Config.walkSpeed, out retreatDestination);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            minimumTimeRemaining -= dt;
            timeoutRemaining -= dt;
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt);
            bool arrived = hasDestination && enemy.Movement.HasReached(retreatDestination, 0.15f);
            if (timeoutRemaining <= 0f ||
                (minimumTimeRemaining <= 0f && (arrived || !hasDestination)))
                enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(Core.EnemyBase enemy) => enemy.Movement.Stop();
    }
}
