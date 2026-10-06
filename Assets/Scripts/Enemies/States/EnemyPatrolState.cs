using Core;
using UnityEngine;

namespace Enemies.States
{
    public class EnemyPatrolState : IState<Core.EnemyBase>
    {
        private int pointIndex;
        private float waitRemaining;
        private bool moving;
        private float travelRemaining;

        public void Enter(Core.EnemyBase enemy)
        {
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            pointIndex = NearestPoint(enemy);
            MoveToPoint(enemy);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target != null)
            {
                enemy.TransitionToCombat();
                return;
            }

            if (moving)
            {
                travelRemaining -= dt;
                enemy.Movement.FaceMovementDirection(dt);
                if (!enemy.Movement.HasReachedDestination(0.35f) && travelRemaining > 0f) return;
                enemy.Movement.Stop();
                enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                moving = false;
                waitRemaining = Random.Range(enemy.Config.patrolWaitDuration, enemy.Config.patrolWaitDuration + 2f);
                return;
            }

            waitRemaining -= dt;
            if (waitRemaining > 0f) return;
            pointIndex = (pointIndex + 1) % enemy.PatrolPoints.Length;
            MoveToPoint(enemy);
        }

        public void Exit(Core.EnemyBase enemy) => enemy.Movement.Stop();

        private void MoveToPoint(Core.EnemyBase enemy)
        {
            moving = enemy.Movement.MoveToNearest(enemy.PatrolPoints[pointIndex],
                1.5f, enemy.Config.walkSpeed, 0.2f);
            travelRemaining = Vector3.Distance(enemy.transform.position, enemy.PatrolPoints[pointIndex]) /
                Mathf.Max(enemy.Config.walkSpeed, 0.1f) + 3f;
            enemy.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Walk : EnemyLocomotionAnimation.Mode.None);
            if (!moving) waitRemaining = enemy.Config.patrolWaitDuration;
        }

        private static int NearestPoint(Core.EnemyBase enemy)
        {
            int nearest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < enemy.PatrolPoints.Length; i++)
            {
                float distance = (enemy.PatrolPoints[i] - enemy.transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }
            return nearest;
        }
    }
}
