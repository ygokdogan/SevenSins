using System.Collections.Generic;
using Combat;
using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class EnemyAttackState : IState<EnemyBase>
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

        public void Enter(EnemyBase enemy)
        {
            hitThisAttack.Clear();

            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            telegraphRemaining = style.AttackData != null ? style.AttackData.telegraphDuration : 0f;
            strikeStarted = false;
            lungeStopped = true;
            startedOutsideAttackBox = !style.IsTargetInAttackBox(enemy);
            lungeRemaining = 0f;
            telegraphStopDistance = Mathf.Max(style.AttackRange * 0.85f,
                enemy.HorizontalDistance - style.TelegraphAdvance);
            if (enemy.Target != null)
                enemy.Movement.MoveTo(enemy.Target.position, enemy.WalkSpeed, telegraphStopDistance);
            if (style.AttackData != null && !string.IsNullOrEmpty(style.AttackData.telegraphAnimation))
                enemy.AnimationManager?.PlayAnimation(style.AttackData.telegraphAnimation);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            enemy.Movement.FaceTarget(enemy.Target, dt);
            if (!strikeStarted)
            {
                if (enemy.Target != null)
                    enemy.Movement.MoveTo(enemy.Target.position, enemy.WalkSpeed, telegraphStopDistance);
                telegraphRemaining -= dt;
                if (telegraphRemaining <= 0f)
                {
                    strikeStarted = true;
                    enemy.Movement.Stop();
                    StartLunge(enemy);
                    enemy.AnimationManager.PlayAnimation(style.AttackData.animationName,
                        style.AttackData.crossFadeDuration);
                }
                return;
            }

            bool lunging = !lungeStopped;
            AdvanceLunge(enemy, dt);
            if (enemy.IsAttackWindowOpen || lunging)
                PerformHitDetection(enemy);

            if (enemy.IsAnimationFinished)
            {
                style.FinishAttack(enemy);
            }
        }

        public void Exit(EnemyBase enemy)
        {
            enemy.Movement.Stop();
            hitThisAttack.Clear();
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
        }

        private void StartLunge(EnemyBase enemy)
        {
            if (enemy.Target == null || !enemy.Movement.IsOnNavMesh ||
                (!startedOutsideAttackBox && style.IsTargetInAttackBox(enemy))) return;

            lungeRemaining = Mathf.Min(style.LungeDistance,
                Mathf.Max(style.AttackBoxGap(enemy) + 0.2f, startedOutsideAttackBox ? 0.35f : 0f));
            lungeTimeRemaining = 0.3f;
            lungeStopped = lungeRemaining <= 0f;
            if (!lungeStopped) enemy.Movement.BeginLunge();
        }

        private void AdvanceLunge(EnemyBase enemy, float dt)
        {
            if (lungeStopped) return;
            lungeTimeRemaining -= dt;
            if (lungeTimeRemaining <= 0f || enemy.Target == null || !enemy.Movement.IsOnNavMesh)
            {
                StopLunge(enemy);
                return;
            }

            float step = enemy.Movement.LungeTowards(enemy.Target,
                Mathf.Min(lungeRemaining, style.LungeSpeed * dt), style.AttackRange * 0.7f);
            if (step <= 0f)
            {
                StopLunge(enemy);
                return;
            }

            lungeRemaining -= step;
            if (lungeRemaining <= 0f) StopLunge(enemy);
        }

        private void StopLunge(EnemyBase enemy)
        {
            lungeStopped = true;
            enemy.Movement.Stop();
        }

        private void PerformHitDetection(EnemyBase enemy)
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
                    Vector3 hitPoint = HitPointUtility.OnTargetSurface(hit, enemy.Transform.position, out Vector3 hitNormal);
                    Vector3 hitDir = hit.transform.position - enemy.Transform.position;
                    hitDir.y = 0f;

                    if (hitDir.sqrMagnitude > 0.001f)
                        hitDir.Normalize();
                    else
                        hitDir = enemy.Transform.forward;

                    HitInfo info = new HitInfo
                    {
                        damage = damage,
                        poiseDamage = poiseDamage,
                        impactMultiplier = impactMultiplier,
                        knockbackForce = knockbackForce,
                        hitPoint = hitPoint,
                        hitNormal = hitNormal,
                        hitDirection = hitDir,
                        attacker = enemy.Transform.gameObject
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
