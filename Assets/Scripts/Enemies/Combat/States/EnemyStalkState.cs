using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyStalkState : IState<EnemyCombatContext>
    {
        private float remaining;
        private float directionTimer;
        private float orbitAngle;
        private float angularSpeed;
        private int strafeDirection;
        private float orbitRadius;
        private Vector3 orbitDirection;

        public void Enter(EnemyCombatContext ctx)
        {
            remaining = Random.Range(0.35f, 0.75f);
            orbitAngle = 0f;
            strafeDirection = Random.value < 0.5f ? -1 : 1;
            ChooseNextStep();
            orbitRadius = ctx.HorizontalDistance;
            orbitDirection = ctx.Transform.position - ctx.Target.position;
            orbitDirection.y = 0f;
            orbitDirection = orbitDirection.sqrMagnitude > 0.001f
                ? orbitDirection.normalized : -ctx.Transform.forward;
            ctx.AnimationManager?.PlayAnimation("Idle");
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.Walk);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null) { ctx.RequestTransition(ctx.States.Idle); return; }
            if (ctx.HorizontalDistance > ctx.AttackStyle.EngagementRange(ctx) * 1.2f)
            {
                ctx.RequestTransition(ctx.States.Approach);
                return;
            }
            if (ctx.AttackStyle.TryStartEscape(ctx)) return;

            remaining -= dt;
            directionTimer -= dt;
            if (directionTimer <= 0f) ChooseNextStep();
            ctx.Movement.FaceTarget(ctx.Target, dt);
            if (!ctx.AttackStyle.MoveWhileStalking(ctx, dt))
            {
                orbitRadius = Mathf.MoveTowards(orbitRadius, ctx.AttackStyle.DesiredDistance(ctx),
                    ctx.WalkSpeed * dt);
                orbitAngle += strafeDirection * angularSpeed * dt;
                float leadAngle = strafeDirection * 20f;
                Vector3 direction = Quaternion.Euler(0f, orbitAngle + leadAngle, 0f) * orbitDirection;
                Vector3 destination = ctx.Target.position + direction * orbitRadius;
                ctx.Movement.MoveToNearest(destination, 1f, ctx.WalkSpeed, 0.05f);
            }
            if ((remaining <= 0f || ctx.AttackStyle.ShouldAttackImmediately(ctx)) &&
                Time.time >= ctx.NextAttackTime && ctx.AttackStyle.CanStartAttack(ctx))
            {
                ctx.RequestTransition(ctx.States.Attack);
            }
        }

        public void Exit(EnemyCombatContext ctx) => ctx.Movement.Stop();

        private void ChooseNextStep()
        {
            directionTimer = Random.Range(0.7f, 1.4f);
            angularSpeed = Random.Range(22f, 38f);
            
            float choice = Random.value;
            if (choice < 0.15f) strafeDirection = 0;
            else if (choice < 0.35f) strafeDirection = strafeDirection == 0
                ? (Random.value < 0.5f ? -1 : 1) : -strafeDirection;
            else if (strafeDirection == 0) strafeDirection = Random.value < 0.5f ? -1 : 1;
        }
    }
}
