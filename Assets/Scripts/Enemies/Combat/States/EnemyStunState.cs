using Core;

namespace Enemies.Combat.States
{
    public class EnemyStunState : IState<EnemyCombatContext>
    {
        private float remaining;

        public void Enter(EnemyCombatContext ctx)
        {
            ctx.Movement.Stop();
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            remaining = ctx.StunDuration;
            ctx.IsAttackWindowOpen = false;
            ctx.IsAnimationFinished = false;
            ctx.AnimationManager?.PlayAnimation("Idle", 0.08f);
            ctx.StunVisual?.Show();
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            remaining -= dt;
            if (remaining <= 0f)
                ctx.RequestTransition(ctx.States.Idle);
        }

        public void Exit(EnemyCombatContext ctx)
        {
            ctx.StunVisual?.Hide();
        }
    }
}
