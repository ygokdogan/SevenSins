using Core;

namespace Enemies.Combat.States
{
    public class EnemyIdleState : IState<EnemyCombatContext>
    {
        public void Enter(EnemyCombatContext ctx)
        {
            ctx.Movement.Stop();
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            ctx.IsAnimationFinished = false;
            ctx.AnimationManager?.PlayAnimation("Idle");
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (!ctx.Movement.IsOnNavMesh) return;
            if (ctx.Target == null)
            {
                if (ctx.HasLastSeenPosition)
                {
                    ctx.RequestTransition(ctx.States.Investigate);
                    return;
                }
                if (ctx.PatrolPoints != null && ctx.PatrolPoints.Length > 0)
                    ctx.RequestTransition(ctx.States.Patrol);
                return;
            }
            ctx.RequestTransition(ctx.HorizontalDistance > ctx.AttackStyle.EngagementRange(ctx)
                ? ctx.States.Approach : ctx.States.Stalk);
        }

        public void Exit(EnemyCombatContext ctx)
        {
        }
    }
}
