using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyDashRetreatState : IState<EnemyCombatContext>
    {
        private readonly ArcherRangedAttackStyle style;
        private Vector3 destination;
        private float remaining;

        public EnemyDashRetreatState(ArcherRangedAttackStyle style) => this.style = style;

        public void SetDestination(Vector3 safeDestination) => destination = safeDestination;

        public void Enter(EnemyCombatContext ctx)
        {
            ctx.AnimationManager?.PlayAnimation("Idle");
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Sprint);
            remaining = Vector3.Distance(ctx.Transform.position, destination) /
                Mathf.Max(style.EscapeSpeed, 0.1f) + 0.65f;
            if (!ctx.Movement.BeginDash(destination, style.EscapeSpeed))
                remaining = 0f;
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }

            remaining -= dt;
            ctx.Movement.FaceTarget(ctx.Target, dt);
            if (remaining > 0f && !ctx.Movement.HasReached(destination, 0.25f)) return;
            ctx.RequestTransition(ctx.States.Stalk);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.EndDash();
    }
}
