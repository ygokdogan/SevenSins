using System.Collections.Generic;
using Combat;
using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyAttackState : IState<EnemyCombatContext>
    {
        private readonly SamuraiMeleeAttackStyle style;
        private readonly HashSet<Collider> hitThisAttack = new HashSet<Collider>();
        private float telegraphRemaining;
        private bool strikeStarted;
        private bool lungeStopped;
        private bool startedOutsideAttackBox;
        private float lungeRemaining;
        private float lungeTimeRemaining;
        private float telegraphStopDistance;

        public EnemyAttackState(SamuraiMeleeAttackStyle style) => this.style = style;

        public void Enter(EnemyCombatContext ctx)
        {
            hitThisAttack.Clear();

            ctx.IsAttackWindowOpen = false;
            ctx.IsAnimationFinished = false;
            ctx.Movement.Stop();
            ctx.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            telegraphRemaining = style.AttackData != null ? style.AttackData.telegraphDuration : 0f;
            strikeStarted = false;
            lungeStopped = true;
            startedOutsideAttackBox = !style.IsTargetInAttackBox(ctx);
            lungeRemaining = 0f;
            telegraphStopDistance = Mathf.Max(style.AttackRange * 0.85f,
                ctx.HorizontalDistance - style.TelegraphAdvance);
            if (ctx.Target != null)
                ctx.Movement.MoveTo(ctx.Target.position, ctx.WalkSpeed, telegraphStopDistance);
            if (style.AttackData != null && !string.IsNullOrEmpty(style.AttackData.telegraphAnimation))
                ctx.AnimationManager?.PlayAnimation(style.AttackData.telegraphAnimation);
        }

        public void Tick(EnemyCombatContext ctx, float dt)
        {
            if (ctx.Target == null)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }
            ctx.Movement.FaceTarget(ctx.Target, dt);
            if (!strikeStarted)
            {
                if (ctx.Target != null)
                    ctx.Movement.MoveTo(ctx.Target.position, ctx.WalkSpeed, telegraphStopDistance);
                telegraphRemaining -= dt;
                if (telegraphRemaining <= 0f)
                {
                    strikeStarted = true;
                    ctx.Movement.Stop();
                    StartLunge(ctx);
                    ctx.AnimationManager.PlayAnimation(style.AttackData.animationName,
                        style.AttackData.crossFadeDuration);
                }
                return;
            }

            bool lunging = !lungeStopped;
            AdvanceLunge(ctx, dt);
            if (ctx.IsAttackWindowOpen || lunging)
                PerformHitDetection(ctx);

            if (ctx.IsAnimationFinished)
            {
                style.FinishAttack(ctx);
            }
        }

        public void Exit(EnemyCombatContext ctx)
        {
            ctx.Movement.Stop();
            hitThisAttack.Clear();
            ctx.IsAttackWindowOpen = false;
            ctx.IsAnimationFinished = false;
        }

        private void StartLunge(EnemyCombatContext ctx)
        {
            if (ctx.Target == null || !ctx.Movement.IsOnNavMesh ||
                (!startedOutsideAttackBox && style.IsTargetInAttackBox(ctx))) return;

            lungeRemaining = Mathf.Min(style.LungeDistance,
                Mathf.Max(style.AttackBoxGap(ctx) + 0.2f, startedOutsideAttackBox ? 0.35f : 0f));
            lungeTimeRemaining = 0.3f;
            lungeStopped = lungeRemaining <= 0f;
            if (!lungeStopped) ctx.Movement.BeginLunge();
        }

        private void AdvanceLunge(EnemyCombatContext ctx, float dt)
        {
            if (lungeStopped) return;
            lungeTimeRemaining -= dt;
            if (lungeTimeRemaining <= 0f || ctx.Target == null || !ctx.Movement.IsOnNavMesh)
            {
                StopLunge(ctx);
                return;
            }

            float step = ctx.Movement.LungeTowards(ctx.Target,
                Mathf.Min(lungeRemaining, style.LungeSpeed * dt), style.AttackRange * 0.7f);
            if (step <= 0f)
            {
                StopLunge(ctx);
                return;
            }

            lungeRemaining -= step;
            if (lungeRemaining <= 0f) StopLunge(ctx);
        }

        private void StopLunge(EnemyCombatContext ctx)
        {
            lungeStopped = true;
            ctx.Movement.Stop();
        }

        private void PerformHitDetection(EnemyCombatContext ctx)
        {
            var col = style.HitCollider;
            if (col == null) return;

            Vector3 center = col.transform.TransformPoint(col.center);
            Vector3 halfExtents = Vector3.Scale(col.size, col.transform.lossyScale) * 0.5f;
            Quaternion orientation = col.transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, style.HittableLayers);

            var attack = style.AttackData;
            float damage = attack != null ? attack.damage : 25f;
            float poiseDamage = attack != null ? attack.poiseDamage : 10f;
            float impactMultiplier = attack != null ? attack.impactMultiplier : 1f;
            float knockbackForce = attack != null ? attack.knockbackForce : 6f;

            foreach (var hit in hits)
            {
                if (hitThisAttack.Contains(hit)) continue;
                hitThisAttack.Add(hit);

                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    Vector3 hitPoint = HitPointUtility.OnTargetSurface(hit, ctx.Transform.position, out Vector3 hitNormal);
                    Vector3 hitDir = hit.transform.position - ctx.Transform.position;
                    hitDir.y = 0f;

                    if (hitDir.sqrMagnitude > 0.001f)
                        hitDir.Normalize();
                    else
                        hitDir = ctx.Transform.forward;

                    HitInfo info = new HitInfo
                    {
                        damage = damage,
                        poiseDamage = poiseDamage,
                        impactMultiplier = impactMultiplier,
                        knockbackForce = knockbackForce,
                        hitPoint = hitPoint,
                        hitNormal = hitNormal,
                        hitDirection = hitDir,
                        attacker = ctx.Transform.gameObject
                    };

                    damageable.TakeHit(info);

                    if (attack != null && attack.hitVfxPrefab != null)
                    {
                        Object.Instantiate(attack.hitVfxPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                    }
                }
            }
        }
    }
}
