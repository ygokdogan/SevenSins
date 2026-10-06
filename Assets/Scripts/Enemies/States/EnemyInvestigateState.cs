using Core;
using UnityEngine;

namespace Enemies.States
{
    public class EnemyInvestigateState : IState<Core.EnemyBase>
    {
        private bool moving;
        private float travelRemaining;
        private float searchRemaining;
        private float lookTimer;
        private int lookDirection;
        private Quaternion arrivalRotation;

        public void Enter(Core.EnemyBase enemy)
        {
            if (!enemy.Perception.HasLastSeenPosition)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }

            enemy.AnimationManager?.PlayAnimation("Idle");
            moving = enemy.Movement.MoveToNearest(enemy.Perception.LastSeenPosition, 1.5f,
                enemy.Config.sprintSpeed, 0.2f);
            enemy.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Sprint : EnemyLocomotionAnimation.Mode.None);
            travelRemaining = Vector3.Distance(enemy.transform.position, enemy.Perception.LastSeenPosition) /
                Mathf.Max(enemy.Config.sprintSpeed, 0.1f) + 3f;
            searchRemaining = enemy.Config.searchDuration;
            lookTimer = 0f;
            lookDirection = 1;
            arrivalRotation = enemy.transform.rotation;
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
                if (!enemy.Movement.HasReachedDestination(0.45f) && travelRemaining > 0f) return;
                enemy.Movement.Stop();
                enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                moving = false;
                arrivalRotation = enemy.transform.rotation;
            }

            searchRemaining -= dt;
            lookTimer -= dt;
            if (lookTimer <= 0f)
            {
                lookDirection = -lookDirection;
                lookTimer = 0.65f;
            }
            Quaternion look = arrivalRotation * Quaternion.Euler(0f, lookDirection * 70f, 0f);
            enemy.transform.rotation = Quaternion.RotateTowards(enemy.transform.rotation, look, 180f * dt);

            if (searchRemaining > 0f) return;
            enemy.Perception.ClearLastSeenPosition();
            enemy.RequestTransition(enemy.States.Idle);
        }

        public void Exit(Core.EnemyBase enemy) => enemy.Movement.Stop();
    }
}
