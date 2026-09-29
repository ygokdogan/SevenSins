using Core;

namespace Enemies.Combat.States
{
    public class EnemyRangedAttackState : IState<EnemyBase>
    {
        private readonly ArcherRangedAttackStyle style;
        private float remaining;
        private bool fired;
        private RangedAttackData attack;

        public EnemyRangedAttackState(ArcherRangedAttackStyle style) => this.style = style;

        public void Enter(EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            attack = style.SelectAttack();
            remaining = attack != null ? attack.telegraphDuration : 0f;
            fired = false;
            if (attack != null && !string.IsNullOrEmpty(attack.telegraphAnimation))
                enemy.AnimationManager?.PlayAnimation(attack.telegraphAnimation);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            if (style.TryStartEscape(enemy)) return;
            enemy.Movement.FaceTarget(enemy.Target, dt);
            style.MaintainPreferredRange(enemy);
            remaining -= dt;
            if (remaining > 0f) return;

            if (!fired)
            {
                fired = true;
                style.Fire(enemy, attack);
                if (attack != null && !string.IsNullOrEmpty(attack.animationName))
                    enemy.AnimationManager?.PlayAnimation(attack.animationName,
                        attack.crossFadeDuration);
                remaining = attack != null ? attack.recoveryDuration : 0f;
                return;
            }

            style.FinishAttack(enemy);
        }

        public void Exit(EnemyBase enemy) => enemy.Movement.Stop();
    }
}
