using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyApproachState : IState<EnemyBase>
    {
        private readonly bool attackDirectly;

        public EnemyApproachState(bool attackDirectly = false) =>
            this.attackDirectly = attackDirectly;

        public void Enter(EnemyBase enemy)
        {
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Sprint);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target == null) { enemy.RequestTransition(enemy.States.Idle); return; }
            enemy.Movement.FaceTarget(enemy.Target, dt);
            if (attackDirectly)
            {
                if (Time.time >= enemy.NextAttackTime &&
                    enemy.AttackStyle.CanStartAttack(enemy))
                {
                    enemy.RequestTransition(enemy.States.Attack);
                    return;
                }
                enemy.Movement.MoveTo(enemy.Target.position, enemy.SprintSpeed,
                    enemy.AttackStyle.DesiredDistance(enemy) * 0.7f);
                return;
            }
            float engagementRange = enemy.EngagementRange;
            if (enemy.HorizontalDistance <= engagementRange)
            {
                enemy.RequestTransition(enemy.States.Engage);
                return;
            }
            enemy.Movement.MoveTo(enemy.Target.position, enemy.SprintSpeed, engagementRange * 0.85f);
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();
    }
}
