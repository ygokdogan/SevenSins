using System.Collections.Generic;
using Combat;
using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public class SamuraiAttackState : IState<Core.EnemyBase>
    {
        private readonly SamuraiMeleeAttackStyle style;
        private readonly HashSet<Collider> hitThisAttack = new HashSet<Collider>();
        private bool active;
        private bool usesAnimation;
        private float strikeElapsed;
        private float telegraphRemaining;
        private bool strikeStarted;
        private bool lungeStopped;
        private bool startedOutsideAttackBox;
        private float lungeRemaining;
        private float lungeTimeRemaining;
        private float telegraphStopDistance;

        public SamuraiAttackState(SamuraiMeleeAttackStyle style) => this.style = style;

        public void Enter(Core.EnemyBase enemy)
        {
            active = true;
            usesAnimation = false;
            strikeElapsed = 0f;
            hitThisAttack.Clear();

            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            telegraphRemaining = style.AttackData != null ? style.AttackData.telegraphDuration : 0f;
            enemy.AttackIndicator.Show(style.AttackData != null ? style.AttackData.defense : default, telegraphRemaining);
            strikeStarted = false;
            lungeStopped = true;
            startedOutsideAttackBox = !style.IsTargetInAttackBox(enemy);
            lungeRemaining = 0f;
            telegraphStopDistance = Mathf.Max(style.AttackRange * 0.85f,
                enemy.HorizontalDistance - style.TelegraphAdvance);
            if (enemy.Perception.Target != null)
                enemy.Movement.MoveTo(enemy.Perception.Target.position, enemy.Config.walkSpeed, telegraphStopDistance);
            if (style.AttackData != null && !string.IsNullOrEmpty(style.AttackData.telegraphAnimation))
                enemy.AnimationManager?.PlayAnimation(style.AttackData.telegraphAnimation);
        }

        public void Tick(Core.EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null)
            {
                enemy.RequestTransition(enemy.States.Idle);
                return;
            }
            enemy.Movement.FaceTarget(enemy.Perception.Target, dt);
            if (!strikeStarted)
            {
                if (enemy.Perception.Target != null)
                    enemy.Movement.MoveTo(enemy.Perception.Target.position, enemy.Config.walkSpeed, telegraphStopDistance);
                telegraphRemaining -= dt;
                enemy.AttackIndicator.UpdateRemaining(telegraphRemaining);
                if (telegraphRemaining <= 0f)
                {
                    strikeStarted = true;
                    enemy.AttackIndicator.Strike();
                    enemy.Movement.Stop();
                    StartLunge(enemy);
                    usesAnimation = enemy.AnimationManager != null &&
                        enemy.AnimationManager.PlayAnimation(style.AttackData.animationName,
                            style.AttackData.crossFadeDuration);
                }
                return;
            }

            strikeElapsed += dt;
            bool lunging = !lungeStopped;
            AdvanceLunge(enemy, dt);
            bool timedWindow = !usesAnimation && strikeElapsed >= style.FallbackHitStart &&
                strikeElapsed <= style.FallbackHitEnd;
            if (enemy.IsAttackWindowOpen || timedWindow || lunging)
                PerformHitDetection(enemy);

            if (!active) return; // A hit can synchronously parry and exit this state.
            if (enemy.IsAnimationFinished || strikeElapsed >=
                (usesAnimation ? style.AnimationTimeout : style.FallbackDuration))
            {
                style.FinishAttack(enemy);
            }
        }

        public void Exit(Core.EnemyBase enemy)
        {
            active = false;
            enemy.AttackIndicator.Hide();
            enemy.Movement.Stop();
            hitThisAttack.Clear();
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
        }

        private void StartLunge(Core.EnemyBase enemy)
        {
            if (enemy.Perception.Target == null || !enemy.Movement.IsOnNavMesh ||
                (!startedOutsideAttackBox && style.IsTargetInAttackBox(enemy))) return;

            lungeRemaining = Mathf.Min(style.LungeDistance,
                Mathf.Max(style.AttackBoxGap(enemy) + 0.2f, startedOutsideAttackBox ? 0.35f : 0f));
            lungeTimeRemaining = 0.3f;
            lungeStopped = lungeRemaining <= 0f;
            if (!lungeStopped) enemy.Movement.BeginLunge();
        }

        private void AdvanceLunge(Core.EnemyBase enemy, float dt)
        {
            if (lungeStopped) return;
            lungeTimeRemaining -= dt;
            if (lungeTimeRemaining <= 0f || enemy.Perception.Target == null || !enemy.Movement.IsOnNavMesh)
            {
                StopLunge(enemy);
                return;
            }

            float step = enemy.Movement.LungeTowards(enemy.Perception.Target,
                Mathf.Min(lungeRemaining, style.LungeSpeed * dt), style.AttackRange * 0.7f);
            if (step <= 0f)
            {
                StopLunge(enemy);
                return;
            }

            lungeRemaining -= step;
            if (lungeRemaining <= 0f) StopLunge(enemy);
        }

        private void StopLunge(Core.EnemyBase enemy)
        {
            lungeStopped = true;
            enemy.Movement.Stop();
        }

        private void PerformHitDetection(Core.EnemyBase enemy)
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
                if (!active) return;
                if (hitThisAttack.Contains(hit)) continue;
                hitThisAttack.Add(hit);

                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    Vector3 hitPoint = HitPointUtility.OnTargetSurface(hit, enemy.transform.position, out Vector3 hitNormal);
                    Vector3 hitDir = hit.transform.position - enemy.transform.position;
                    hitDir.y = 0f;

                    if (hitDir.sqrMagnitude > 0.001f)
                        hitDir.Normalize();
                    else
                        hitDir = enemy.transform.forward;

                    HitInfo info = new HitInfo
                    {
                        defense = attack != null ? attack.defense : default,
                        damage = damage,
                        poiseDamage = poiseDamage,
                        impactMultiplier = impactMultiplier,
                        knockbackForce = knockbackForce,
                        hitPoint = hitPoint,
                        hitNormal = hitNormal,
                        hitDirection = hitDir,
                        attacker = enemy.transform.gameObject
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
