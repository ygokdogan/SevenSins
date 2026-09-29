using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyPatrolState : IState<EnemyBase>
    {
        private int pointIndex;
        private float waitRemaining;
        private bool moving;
        private float travelRemaining;

        public void Enter(EnemyBase enemy)
        {
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            pointIndex = NearestPoint(enemy);
            MoveToPoint(enemy);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target != null)
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
                waitRemaining = Random.Range(enemy.PatrolWaitDuration, enemy.PatrolWaitDuration + 2f);
                return;
            }

            waitRemaining -= dt;
            if (waitRemaining > 0f) return;
            pointIndex = (pointIndex + 1) % enemy.PatrolPoints.Length;
            MoveToPoint(enemy);
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();

        private void MoveToPoint(EnemyBase enemy)
        {
            moving = enemy.Movement.MoveToNearest(enemy.PatrolPoints[pointIndex],
                1.5f, enemy.WalkSpeed, 0.2f);
            travelRemaining = Vector3.Distance(enemy.Transform.position, enemy.PatrolPoints[pointIndex]) /
                Mathf.Max(enemy.WalkSpeed, 0.1f) + 3f;
            enemy.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Walk : EnemyLocomotionAnimation.Mode.None);
            if (!moving) waitRemaining = enemy.PatrolWaitDuration;
        }

        private static int NearestPoint(EnemyBase enemy)
        {
            int nearest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < enemy.PatrolPoints.Length; i++)
            {
                float distance = (enemy.PatrolPoints[i] - enemy.Transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }
            return nearest;
        }
    }
}
