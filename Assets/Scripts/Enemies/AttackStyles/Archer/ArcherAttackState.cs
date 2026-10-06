using Core;

namespace Enemies.Combat.States
{
    public class ArcherAttackState : IState<Core.EnemyBase>
    {
        private readonly ArcherRangedAttackStyle style;
        private float remaining;
        private bool fired;
        private RangedAttackData attack;

        public ArcherAttackState(ArcherRangedAttackStyle style) => this.style = style;

        public void Enter(Core.EnemyBase enemy)
        {
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            attack = style.SelectAttack();
            remaining = attack != null ? attack.telegraphDuration : 0f;
            fired = false;
            enemy.AttackIndicator.Show(attack != null ? attack.defense : default, remaining);
            if (attack != null && !string.IsNullOrEmpty(attack.telegraphAnimation))
                enemy.AnimationManager?.PlayAnimation(attack.telegraphAnimation);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            if (style.TryStartEscape(enemy)) return;
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt);
            style.MaintainPreferredRange(enemy);
            remaining -= dt;
            if (!fired) enemy.AttackIndicator.UpdateRemaining(remaining);
            if (remaining > 0f) return;

            if (!fired)
            {
                fired = true;
                enemy.AttackIndicator.Strike();
                style.Fire(enemy, attack);
                if (attack != null && !string.IsNullOrEmpty(attack.animationName))
                    enemy.AnimationManager?.PlayAnimation(attack.animationName,
                        attack.crossFadeDuration);
                remaining = attack != null ? attack.recoveryDuration : 0f;
                return;
            }

            style.FinishAttack(enemy);
        }

        public void Exit(Core.EnemyBase enemy)
        {
            enemy.AttackIndicator.Hide();
            enemy.Movement.Stop();
        }
    }
}
