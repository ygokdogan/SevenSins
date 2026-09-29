using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyStalkState : IState<EnemyBase>
    {
        private float remaining;
        private float directionTimer;
        private float orbitAngle;
        private float angularSpeed;
        private int strafeDirection;
        private float orbitRadius;
        private Vector3 orbitDirection;

        public void Enter(EnemyBase enemy)
        {
            remaining = Random.Range(0.35f, 0.75f);
            orbitAngle = 0f;
            strafeDirection = Random.value < 0.5f ? -1 : 1;
            ChooseNextStep();
            orbitRadius = enemy.HorizontalDistance;
            orbitDirection = enemy.Transform.position - enemy.Target.position;
            orbitDirection.y = 0f;
            orbitDirection = orbitDirection.sqrMagnitude > 0.001f
                ? orbitDirection.normalized : -enemy.Transform.forward;
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target == null) { enemy.RequestTransition(enemy.States.Idle); return; }
            if (enemy.HorizontalDistance > enemy.EngagementRange * 1.2f)
            {
                enemy.RequestTransition(enemy.States.Approach);
                return;
            }
            if (enemy.AttackStyle.TryStartEscape(enemy)) return;

            remaining -= dt;
            directionTimer -= dt;
            if (directionTimer <= 0f) ChooseNextStep();
            enemy.Movement.FaceTarget(enemy.Target, dt);
            if (!enemy.AttackStyle.MoveWhileStalking(enemy, dt))
            {
                orbitRadius = Mathf.MoveTowards(orbitRadius, enemy.AttackStyle.DesiredDistance(enemy),
                    enemy.WalkSpeed * dt);
                orbitAngle += strafeDirection * angularSpeed * dt;
                float leadAngle = strafeDirection * 20f;
                Vector3 direction = Quaternion.Euler(0f, orbitAngle + leadAngle, 0f) * orbitDirection;
                Vector3 destination = enemy.Target.position + direction * orbitRadius;
                enemy.Movement.MoveToNearest(destination, 1f, enemy.WalkSpeed, 0.05f);
            }
            if ((remaining <= 0f || enemy.AttackStyle.ShouldAttackImmediately(enemy)) &&
                Time.time >= enemy.NextAttackTime && enemy.AttackStyle.CanStartAttack(enemy))
            {
                enemy.RequestTransition(enemy.States.Attack);
            }
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();

        private void ChooseNextStep()
        {
            directionTimer = Random.Range(0.7f, 1.4f);
            angularSpeed = Random.Range(22f, 38f);
            
            float choice = Random.value;
            if (choice < 0.15f) strafeDirection = 0;
            else if (choice < 0.35f) strafeDirection = strafeDirection == 0
                ? (Random.value < 0.5f ? -1 : 1) : -strafeDirection;
            else if (strafeDirection == 0) strafeDirection = Random.value < 0.5f ? -1 : 1;
        }
    }
}
