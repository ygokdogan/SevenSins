using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyRetreatState : IState<EnemyCombatContext>
    {
        private float minimumTimeRemaining;
        private float timeoutRemaining;
        private bool hasDestination;
        private Vector3 retreatDestination;

        public void Enter(EnemyCombatContext ctx)
        {
            minimumTimeRemaining = ctx.RetreatDuration;
            timeoutRemaining = ctx.RetreatDuration +
                ctx.RetreatDistance / Mathf.Max(ctx.WalkSpeed, 0.1f) + 0.5f;
            hasDestination = false;
            ctx.AnimationManager?.PlayAnimation("Idle");
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            hasDestination = ctx.Movement.TryRetreat(ctx.Target, ctx.RetreatDistance,
                ctx.WalkSpeed, out retreatDestination);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }
            minimumTimeRemaining -= dt;
            timeoutRemaining -= dt;
            ctx.Movement.FaceTarget(ctx.Target, dt);
            bool arrived = hasDestination && ctx.Movement.HasReached(retreatDestination, 0.15f);
            if (timeoutRemaining <= 0f ||
                (minimumTimeRemaining <= 0f && (arrived || !hasDestination)))
                ctx.RequestTransition(ctx.States.Idle);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();
    }
}
