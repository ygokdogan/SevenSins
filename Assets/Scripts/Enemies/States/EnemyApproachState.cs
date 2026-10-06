using Core;

namespace Enemies.States
{
    public class EnemyApproachState : IState<Core.EnemyBase>
    {
        private readonly bool attackDirectly;
        private readonly float turnSpeed;

        private readonly EnemyLocomotionAnimation.Mode locomotion;

        public EnemyApproachState(bool attackDirectly = false,
            EnemyLocomotionAnimation.Mode locomotion = EnemyLocomotionAnimation.Mode.Sprint, float turnSpeed = 720f)
        {
            this.attackDirectly = attackDirectly;
            this.locomotion = locomotion;
            this.turnSpeed = turnSpeed;
        }

        private float Speed(Core.EnemyBase enemy) => locomotion == EnemyLocomotionAnimation.Mode.Walk
            ? enemy.Config.walkSpeed : enemy.Config.sprintSpeed;

        public void Enter(Core.EnemyBase enemy)
        {
            enemy.AnimationManager?.PlayAnimation("Idle");
            enemy.Locomotion.PlayLocomotionAnimation(locomotion);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null) { enemy.RequestTransition(enemy.States.Idle); return; }
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt, turnSpeed);
            if (attackDirectly)
            {
                if (enemy.AttackStyle.CanStartAttack(enemy))
                {
                    enemy.RequestTransition(enemy.States.Attack);
                    return;
                }
                enemy.Movement.MoveTo(enemy.Perception.Target.position, Speed(enemy),
                    enemy.AttackStyle.DesiredDistance(enemy) * 0.7f);
                return;
            }
            float engagementRange = enemy.EngagementRange;
            if (enemy.HorizontalDistance <= engagementRange)
            {
                enemy.RequestTransition(enemy.States.Engage);
                return;
            }
            enemy.Movement.MoveTo(enemy.Perception.Target.position, Speed(enemy), engagementRange * 0.85f);
        }

        public void Exit(Core.EnemyBase enemy) => enemy.Movement.Stop();
    }
}
