using System.Collections.Generic;
using Combat;
using Core;
using Enemies.Combat;
using Enemies.Core;
using Enemies.Movement;
using Enemies.Types.Shield;
using UnityEngine;
using Utilities;

namespace Enemies.AttackStyles.Shield
{
    public sealed class ShieldAttackState : IState<EnemyBase>
    {
        private enum Phase { BashTelegraph, Bash, SwordTelegraph, Sword }

        private readonly ShieldAttackStyle style;
        private readonly HashSet<IDamageable> hitSet = new();
        private readonly MeleeLunge lunge = new();
        
        private EnemyShield bearer;
        private Phase phase;
        private Vector3 dashDestination;
        
        private bool active;
        
        private float elapsed;
        private float telegraphDuration;

        public ShieldAttackState(ShieldAttackStyle style) => this.style = style;

        public void Enter(EnemyBase enemy)
        {
            bearer = (EnemyShield)enemy;
            active = true;
            lunge.Reset();
            hitSet.Clear();
            
            if (enemy.AnimationManager != null)
            {
                enemy.AnimationManager.OnAttackWindowOpened += OnAttackWindowOpened;
            }
            
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;

            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            
            
            if (bearer.ShieldBroken)
            {
                BeginSwordTelegraph(enemy, style.swordAttack.telegraphDuration,
                    style.swordAttack.telegraphAnimation);
                return;
            }

            phase = Phase.BashTelegraph;
            elapsed = 0f;
            telegraphDuration = style.bashAttack.telegraphDuration;
            bearer.IsBashing = true;
            
            enemy.AnimationManager.PlayAnimation("BashTelegraph");
            enemy.AttackIndicator.Show(style.bashAttack.defense, telegraphDuration);
        }

        public void Tick(EnemyBase enemy, float dt)
        {
            if (enemy.Perception.Target == null) { style.FinishAttack(enemy); return; }
            elapsed += dt;
            if (phase != Phase.Bash) enemy.Movement.FaceTarget(enemy.Perception.Target, dt);

            switch (phase)
            {
                case Phase.BashTelegraph:
                    enemy.AttackIndicator.UpdateRemaining(telegraphDuration - elapsed);
                    if (elapsed < telegraphDuration) return;
                    phase = Phase.Bash;
                    elapsed = 0f;
                    enemy.AttackIndicator.Strike();
                    enemy.AnimationManager.PlayAnimation("Bash");
                    float bashReach = MeleeHitUtility.ForwardReach(style.bashHitCollider, enemy.transform);
                    var targetCollider = enemy.Perception.TargetCollider;
                    if (targetCollider != null)
                        bashReach += Mathf.Max(targetCollider.bounds.extents.x, targetCollider.bounds.extents.z);
                    float dashDistance = Mathf.Min(style.bashDashDistance,
                        Mathf.Max(0f, enemy.HorizontalDistance - bashReach * style.bashStandRatio));
                    dashDestination = enemy.transform.position + enemy.transform.forward * dashDistance;
                    if (dashDistance > 0f)
                        enemy.Movement.BeginDash(dashDestination, style.bashSpeed);
                    break;

                case Phase.Bash:
                    BashHit(enemy);
                    if (!active || elapsed < style.bashDuration) return;
                    
                    enemy.Movement.EndDash();
                    bearer.IsBashing = false;
                    hitSet.Clear();
                    BeginSwordTelegraph(enemy, style.swordAttack.telegraphDuration, style.swordAttack.telegraphAnimation);
                    break;

                case Phase.SwordTelegraph:
                    enemy.AttackIndicator.UpdateRemaining(telegraphDuration - elapsed);
                    if (elapsed < telegraphDuration) return;
                    
                    StartSword(enemy);
                    break;

                case Phase.Sword:
                    lunge.Tick(enemy, dt, style.lungeSpeed, style.AttackRange(enemy) * style.standRatio);
                    if (enemy.IsAttackWindowOpen)
                        SwordHit(enemy);

                    if (!active) return;
                    
                    if (enemy.IsAnimationFinished || elapsed >= style.animationTimeout)
                        style.FinishAttack(enemy);
                    break;
            }
        }

        public void Exit(EnemyBase enemy)
        {
            active = false;
            lunge.Reset();
            if (enemy.AnimationManager != null)
            {
                enemy.AnimationManager.OnAttackWindowOpened -= OnAttackWindowOpened;
            }
            
            enemy.AttackIndicator.Hide();
            enemy.Movement.EndDash();
            enemy.Movement.Stop();
            
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            
            bearer.IsBashing = false;
            hitSet.Clear();
        }

        private void OnAttackWindowOpened()
        {
            if (!active || phase != Phase.Sword || !bearer.IsAttackWindowOpen) return;
            hitSet.Clear();
            lunge.Reset();
            bearer.Movement.Stop();

            bool outside = !MeleeHitUtility.IsOverlapping(style.hitCollider, bearer.Perception.TargetCollider);
            lunge.Begin(bearer, style.hitCollider, style.swordLungeDistance, style.lungeMaxTime, outside);
        }

        private void BeginSwordTelegraph(EnemyBase enemy, float duration, string telegraphAnim)
        {
            phase = Phase.SwordTelegraph;
            elapsed = 0f;
            telegraphDuration = duration;
            enemy.AttackIndicator.Show(style.swordAttack.defense, duration);
            if (!string.IsNullOrEmpty(telegraphAnim))
                enemy.AnimationManager.PlayAnimation(telegraphAnim);
        }

        private void StartSword(EnemyBase enemy)
        {
            phase = Phase.Sword;
            elapsed = 0f;
            enemy.IsAttackWindowOpen = false;
            enemy.IsAnimationFinished = false;
            enemy.AttackIndicator.Strike();
            enemy.AnimationManager.PlayAnimation(style.swordAttack.animationName, style.swordAttack.crossFadeDuration);
        }

        private void SwordHit(EnemyBase enemy)
        {
            var attack = style.swordAttack;
            MeleeHitUtility.Hit(style.hitCollider, style.hittableLayers, attack.ToHitInfo(),
                enemy.gameObject, hitSet, info =>
                {
                    MeleeHitUtility.SpawnVfx(attack.hitVfxPrefab, info);
                    return active;
                });
        }

        private void BashHit(EnemyBase enemy)
        {
            var attack = style.bashAttack;
            MeleeHitUtility.Hit(style.bashHitCollider, style.hittableLayers, attack.ToHitInfo(),
                enemy.gameObject, hitSet, info =>
                {
                    MeleeHitUtility.SpawnVfx(attack.hitVfxPrefab, info);
                    return active;
                });
        }
    }
}
