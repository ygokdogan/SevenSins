using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyRetreatState : IState<EnemyBase>
    {
        private float minimumTimeRemaining;
        private float timeoutRemaining;
        private bool hasDestination;
        private Vector3 retreatDestination;

        public void Enter(EnemyBase enemy)
        {
            minimumTimeRemaining = enemy.RetreatDuration;
            timeoutRemaining = enemy.RetreatDuration +
                enemy.RetreatDistance / Mathf.Max(enemy.WalkSpeed, 0.1f) + 0.5f;
            hasDestination = false;
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            hasDestination = enemy.Movement.TryRetreat(enemy.Target, enemy.RetreatDistance,
                enemy.WalkSpeed, out retreatDestination);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            minimumTimeRemaining -= dt;
            timeoutRemaining -= dt;
            enemy.Movement.FaceTarget(enemy.Target, dt);
            bool arrived = hasDestination && enemy.Movement.HasReached(retreatDestination, 0.15f);
            if (timeoutRemaining <= 0f ||
                (minimumTimeRemaining <= 0f && (arrived || !hasDestination)))
                enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();
    }
}
