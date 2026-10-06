using Combat;
using Core;
using UnityEngine;

namespace Enemies.Combat.States
{
    public sealed class ShieldAttackState : IState<Core.EnemyBase>
    {
        private readonly ShieldAttackStyle style;
        private EnemyShield bearer;
        private float elapsed;
        private enum Phase { BashTelegraph, Bash, FirstSword, SecondSword }
        private Phase phase;
        private bool hit;
        private bool active;
        private bool cueStruck;
        private Vector3 dashDestination;
        public ShieldAttackState(ShieldAttackStyle style) => this.style = style;
        public void Enter(Core.EnemyBase enemy)
        {
            bearer = (EnemyShield)enemy;
            enemy.Movement.Stop();
            enemy.Locomotion.PlayLocomotionAnimation(EnemyLocomotionAnimation.Mode.None);
            active = true; elapsed = 0f; hit = false; cueStruck = false;
            phase = bearer.ShieldBroken ? Phase.FirstSword : Phase.BashTelegraph;
            bearer.IsBashing = !bearer.ShieldBroken;
            // TODO(Animation): Play bash windup, or first sword swing if the shield is broken.
            enemy.AttackIndicator.Show(phase == Phase.BashTelegraph ? style.BashDefense : style.SwordDefense, phase == Phase.BashTelegraph ? style.TelegraphDuration : 0.12f);
            dashDestination = enemy.transform.position + enemy.transform.forward * 2.1f;
        }
        public void Tick(Core.EnemyBase enemy, float dt)
        {
            elapsed += dt;
            if (enemy.Perception.Target == null) { style.FinishAttack(enemy); return; }
            // Facing is committed at telegraph start: dash and wall-run can flank it.
            if (phase == Phase.BashTelegraph)
            {
                enemy.AttackIndicator.UpdateRemaining(style.TelegraphDuration - elapsed);

                if (elapsed < style.TelegraphDuration) return;
                phase = Phase.Bash; elapsed = 0f;
                enemy.AttackIndicator.Strike();
                // TODO(Animation): Play shield bash. Dash movement remains gameplay-driven.
                enemy.Movement.BeginDash(dashDestination, style.BashSpeed);
            }
            else if (phase == Phase.Bash)
            {
                Strike(enemy, 1.35f, 1.3f, style.BashDamage, style.BashPush);
                if (!active) return; // TakeHit can synchronously parry and change state.
                if (elapsed < 0.28f) return;
                enemy.Movement.EndDash(); bearer.IsBashing = false;
                // TODO(Animation): Play first sword swing.
                phase = Phase.FirstSword; elapsed = 0f; hit = false; cueStruck = false;
                enemy.AttackIndicator.Show(style.SwordDefense, 0.12f);
            }
            else
            {
                enemy.AttackIndicator.UpdateRemaining(0.12f - elapsed);
                if (elapsed >= 0.12f && !cueStruck) { enemy.AttackIndicator.Strike(); cueStruck = true; }

                if (elapsed >= 0.12f && elapsed <= 0.26f)
                    Strike(enemy, 1.15f, 1.05f, style.SwordDamage, 3f);
                if (!active) return;
                if (elapsed < 0.38f) return;
                if (phase == Phase.FirstSword)
                {
                    // TODO(Animation): Play second sword swing.
                    phase = Phase.SecondSword;
                    elapsed = 0f; hit = false; cueStruck = false;
                    enemy.AttackIndicator.Show(style.SwordDefense, 0.12f);
                }
                else style.FinishAttack(enemy);
            }
        }
        private void Strike(Core.EnemyBase enemy, float reach, float width, float damage, float push)
        {
            if (hit || enemy.Perception.TargetCollider == null) return;
            Vector3 center = enemy.transform.position + Vector3.up + enemy.transform.forward * reach * 0.6f;
            foreach (Collider target in Physics.OverlapBox(center, new Vector3(width * 0.5f, 0.85f, reach * 0.5f), enemy.transform.rotation))
            {
                if (target != enemy.Perception.TargetCollider && !target.transform.IsChildOf(enemy.Perception.Target)) continue;
                var damageable = target.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;
                hit = true;
                Vector3 point = HitPointUtility.OnTargetSurface(target, enemy.transform.position, out Vector3 normal);
                damageable.TakeHit(new HitInfo { defense = phase == Phase.Bash ? style.BashDefense : style.SwordDefense, damage = damage, poiseDamage = phase == Phase.Bash ? 8f : 3f,
                    impactMultiplier = phase == Phase.Bash ? 1.2f : 0.5f, knockbackForce = push,
                    hitPoint = point, hitNormal = normal, hitDirection = enemy.transform.forward, attacker = enemy.gameObject });
                return;
            }
        }
        public void Exit(Core.EnemyBase enemy)
        {
            active = false; enemy.AttackIndicator.Hide(); enemy.Movement.EndDash(); enemy.Movement.Stop();
            bearer.IsBashing = false;
            // TODO(Animation): Cancel attack animation on interruption / completion.
            enemy.IsAttackWindowOpen = false;
        }
    }
}
