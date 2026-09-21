using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyPatrolState : IState<EnemyCombatContext>
    {
        private int pointIndex;
        private float waitRemaining;
        private bool moving;
        private float travelRemaining;

        public void Enter(EnemyCombatContext ctx)
        {
            ctx.AnimationManager?.PlayAnimation("Idle");
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
            pointIndex = NearestPoint(ctx);
            MoveToPoint(ctx);
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
                if (!ctx.Movement.HasReachedDestination(0.35f) && travelRemaining > 0f) return;
                ctx.Movement.Stop();
                ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
                moving = false;
                waitRemaining = Random.Range(ctx.PatrolWaitDuration, ctx.PatrolWaitDuration + 2f);
                return;
            }

            waitRemaining -= dt;
            if (waitRemaining > 0f) return;
            pointIndex = (pointIndex + 1) % ctx.PatrolPoints.Length;
            MoveToPoint(ctx);
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();

        private void MoveToPoint(EnemyCombatContext ctx)
        {
            moving = ctx.Movement.MoveToNearest(ctx.PatrolPoints[pointIndex],
                1.5f, ctx.WalkSpeed, 0.2f);
            travelRemaining = Vector3.Distance(ctx.Transform.position, ctx.PatrolPoints[pointIndex]) /
                Mathf.Max(ctx.WalkSpeed, 0.1f) + 3f;
            ctx.Locomotion.PlayLocomotionAnimation(moving
                ? EnemyLocomotionAnimation.Mode.Walk : EnemyLocomotionAnimation.Mode.None);
            if (!moving) waitRemaining = ctx.PatrolWaitDuration;
        }

        private static int NearestPoint(EnemyCombatContext ctx)
        {
            int nearest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < ctx.PatrolPoints.Length; i++)
            {
                float distance = (ctx.PatrolPoints[i] - ctx.Transform.position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }
            return nearest;
        }
    }
}
