using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyInvestigateState : IState<EnemyCombatContext>
    {
        private bool moving;
        private float travelRemaining;
        private float searchRemaining;
        private float lookTimer;
        private int lookDirection;
        private Quaternion arrivalRotation;

        public void Enter(EnemyCombatContext ctx)
        {
            if (!ctx.HasLastSeenPosition)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }

            ctx.AnimationManager?.PlayAnimation("Idle");
            moving = ctx.Movement.MoveToNearest(ctx.LastSeenPosition, 1.5f,
                ctx.SprintSpeed, 0.2f);
            ctx.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Sprint : EnemyLocomotionAnimation.Mode.None);
            travelRemaining = Vector3.Distance(ctx.Transform.position, ctx.LastSeenPosition) /
                Mathf.Max(ctx.SprintSpeed, 0.1f) + 3f;
            searchRemaining = ctx.Config.searchDuration;
            lookTimer = 0f;
            lookDirection = 1;
            arrivalRotation = ctx.Transform.rotation;
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target != null)
            {
                ctx.RequestTransition(ctx.HorizontalDistance > ctx.AttackStyle.EngagementRange(ctx)
                    ? ctx.States.Approach : ctx.States.Stalk);
                return;
            }

            if (moving)
            {
                travelRemaining -= dt;
                ctx.Movement.FaceMovementDirection(dt);
                if (!ctx.Movement.HasReachedDestination(0.45f) && travelRemaining > 0f) return;
                ctx.Movement.Stop();
                ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                moving = false;
                arrivalRotation = ctx.Transform.rotation;
            }

            searchRemaining -= dt;
            lookTimer -= dt;
            if (lookTimer <= 0f)
            {
                lookDirection = -lookDirection;
                lookTimer = 0.65f;
            }
            Quaternion look = arrivalRotation * Quaternion.Euler(0f, lookDirection * 70f, 0f);
            ctx.Transform.rotation = Quaternion.RotateTowards(ctx.Transform.rotation, look, 180f * dt);

            if (searchRemaining > 0f) return;
            ctx.HasLastSeenPosition = false;
            ctx.RequestTransition(ctx.States.Idle);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();
    }
}
