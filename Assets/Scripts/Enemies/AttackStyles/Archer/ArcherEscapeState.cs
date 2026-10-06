using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class ArcherEscapeState : IState<Core.EnemyBase>
    {
        private readonly ArcherRangedAttackStyle style;
        private Vector3 destination;
        private float remaining;

        public ArcherEscapeState(ArcherRangedAttackStyle style) => this.style = style;

        public void SetDestination(Vector3 safeDestination) => destination = safeDestination;

        public void Enter(Core.EnemyBase enemy)
        {
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Sprint);
            remaining = Vector3.Distance(enemy.transform.position, destination) /
                Mathf.Max(style.EscapeSpeed, 0.1f) + 0.65f;
            if (!enemy.Movement.BeginDash(destination, style.EscapeSpeed))
                remaining = 0f;
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }

            remaining -= dt;
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt);
            if (remaining > 0f && !enemy.Movement.HasReached(destination, 0.25f)) return;
            enemy.RequestTransition(enemy.States.Engage);
        }

        public void Exit(Core.EnemyBase enemy) => enemy.Movement.EndDash();
    }
}
