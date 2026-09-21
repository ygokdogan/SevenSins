using Core;

namespace Enemies.Combat.States
{
    public class EnemyRangedAttackState : IState<EnemyCombatContext>
    {
        private readonly ArcherRangedAttackStyle style;
        private float remaining;
        private bool fired;
        private RangedAttackData attack;

        public EnemyRangedAttackState(ArcherRangedAttackStyle style) => this.style = style;

        public void Enter(EnemyCombatContext ctx)
        {
            ctx.Movement.Stop();
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            attack = style.SelectAttack();
            remaining = attack != null ? attack.telegraphDuration : 0f;
            fired = false;
            if (attack != null && !string.IsNullOrEmpty(attack.telegraphAnimation))
                ctx.AnimationManager?.PlayAnimation(attack.telegraphAnimation);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }
            if (style.TryStartEscape(ctx)) return;
            ctx.Movement.FaceTarget(ctx.Target, dt);
            style.MaintainPreferredRange(ctx);
            remaining -= dt;
            if (remaining > 0f) return;

            if (!fired)
            {
                fired = true;
                style.Fire(ctx, attack);
                if (attack != null && !string.IsNullOrEmpty(attack.animationName))
                    ctx.AnimationManager?.PlayAnimation(attack.animationName,
                        attack.crossFadeDuration);
                remaining = attack != null ? attack.recoveryDuration : 0f;
                return;
            }

            style.FinishAttack(ctx);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();
    }
}
