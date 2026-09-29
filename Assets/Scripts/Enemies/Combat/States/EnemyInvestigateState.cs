using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyInvestigateState : IState<EnemyBase>
    {
        private bool moving;
        private float travelRemaining;
        private float searchRemaining;
        private float lookTimer;
        private int lookDirection;
        private Quaternion arrivalRotation;

        public void Enter(EnemyBase enemy)
        {
            if (!enemy.HasLastSeenPosition)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }

            enemy.AnimationManager?.PlayAnimation("Idle");
            moving = enemy.Movement.MoveToNearest(enemy.LastSeenPosition, 1.5f,
                enemy.SprintSpeed, 0.2f);
            enemy.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Sprint : EnemyLocomotionAnimation.Mode.None);
            travelRemaining = Vector3.Distance(enemy.Transform.position, enemy.LastSeenPosition) /
                Mathf.Max(enemy.SprintSpeed, 0.1f) + 3f;
            searchRemaining = enemy.Config.searchDuration;
            lookTimer = 0f;
            lookDirection = 1;
            arrivalRotation = enemy.Transform.rotation;
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
                if (!enemy.Movement.HasReachedDestination(0.45f) && travelRemaining > 0f) return;
                enemy.Movement.Stop();
                enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                moving = false;
                arrivalRotation = enemy.Transform.rotation;
            }

            searchRemaining -= dt;
            lookTimer -= dt;
            if (lookTimer <= 0f)
            {
                lookDirection = -lookDirection;
                lookTimer = 0.65f;
            }
            Quaternion look = arrivalRotation * Quaternion.Euler(0f, lookDirection * 70f, 0f);
            enemy.Transform.rotation = Quaternion.RotateTowards(enemy.Transform.rotation, look, 180f * dt);

            if (searchRemaining > 0f) return;
            enemy.HasLastSeenPosition = false;
            enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();
    }
}
