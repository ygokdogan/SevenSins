using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyApproachState : IState<EnemyCombatContext>
    {
        public void Enter(EnemyCombatContext ctx)
        {
            ctx.AnimationManager?.PlayAnimation("Idle");
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Sprint);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null) { ctx.RequestTransition(ctx.States.Idle); return; }
            ctx.Movement.FaceTarget(ctx.Target, dt);
            float engagementRange = ctx.AttackStyle.EngagementRange(ctx);
            if (ctx.HorizontalDistance <= engagementRange)
            {
                ctx.RequestTransition(ctx.States.Stalk);
                return;
            }
            ctx.Movement.MoveTo(ctx.Target.position, ctx.SprintSpeed, engagementRange * 0.85f);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();
    }
}
