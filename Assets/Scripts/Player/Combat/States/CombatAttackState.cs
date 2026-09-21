using System.Collections.Generic;
using Combat;
using Core;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatAttackState : IState<CombatContext>
    {
        private CombatContext currentCtx;
        
        private Vector3 lungeDirection;
        private float currentLungeSpeed;
        private bool isStationaryLunge;
        
        private HashSet<Collider> hitThisAttack = new HashSet<Collider>();
        
        public void Enter(CombatContext ctx)
        {
            currentCtx = ctx;
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            lungeDirection = ctx.Transform.forward;

            hitThisAttack.Clear();
            
            ctx.Input.UsePrimaryAttackInput();
            ctx.Movement.CanSprint = false;
            
            ctx.IsSheathed = false;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = false;

            ctx.OnLungeTriggered += HandleLunge;
            
            ctx.AnimationManager.PlayAttackAnimation(ctx.CurrentAttack.animationName,
                ctx.CurrentAttack.crossFadeDuration);
        }

        private void HandleLunge()
        {
            if (currentCtx?.CurrentAttack == null) return;

            bool isStandingStill = currentCtx.Input.MoveInput.sqrMagnitude < 0.01f;

            if (currentCtx.CurrentAttack.hasLunge)
            {
                currentCtx.Movement.CanMove = false;
                currentCtx.Movement.Velocity = Vector3.zero;
                currentLungeSpeed = currentCtx.CurrentAttack.forwardImpulse;
                isStationaryLunge = false;
            }
            else if (isStandingStill && currentCtx.CurrentAttack.forwardImpulse > 0.01f)
            {
                currentCtx.Movement.CanMove = false;
                currentCtx.Movement.Velocity = Vector3.zero;
                currentLungeSpeed = currentCtx.CurrentAttack.forwardImpulse;
                isStationaryLunge = true;
            }
        }

        public void Tick(CombatContext ctx, float dt)
        {
            HandleAttackLunge(ctx, dt);
            HandleAttack();
            
            if (ctx.IsComboWindowOpen && ctx.Input.PrimaryAttackInput)
            {
                if (ctx.CurrentAttack != null && ctx.CurrentAttack.nextLightAttack != null)
                {
                    ctx.Input.UsePrimaryAttackInput();
                    ctx.CurrentAttack = ctx.CurrentAttack.nextLightAttack;
                    
                    ctx.OnLungeTriggered -= HandleLunge;
                    Enter(ctx);
                    return;
                }
            }

            if (ctx.IsComboWindowOpen && (ctx.Input.BlockInput || ctx.Input.IsBlockHeld))
            {
                ctx.RequestTransition(ctx.States.Block);
                return;
            }

            if (ctx.IsAnimationFinished)
            {
                ctx.RequestTransition(ctx.States.Idle);
            }
        }

        public void Exit(CombatContext ctx)
        {
            ctx.OnLungeTriggered -= HandleLunge;
            hitThisAttack.Clear();
            
            ctx.Movement.CanSprint = true;
            ctx.Movement.CanMove = true;
            
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            currentCtx = null;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = true;
        }

        private void HandleAttackLunge(CombatContext ctx, float dt)
        {
            if (isStationaryLunge)
            {
                if (ctx.Input.MoveInput.sqrMagnitude > 0.01f || currentLungeSpeed <= 0.01f)
                {
                    currentLungeSpeed = 0f;
                    ctx.Movement.CanMove = true;
                    isStationaryLunge = false;
                }
            }

            if (ctx.IsComboWindowOpen)
            {
                ctx.Movement.CanMove = true;
            }
            
            if (currentLungeSpeed > 0.01f)
            {
                Vector3 currentMoveVel = ctx.Movement != null ? ctx.Movement.Velocity : Vector3.zero;
                Vector3 desiredLungeVel = lungeDirection * currentLungeSpeed;
                Vector3 extraVelocity = desiredLungeVel - currentMoveVel;

                ctx.Controller.Move(extraVelocity * dt);

                float decay = ctx.CurrentAttack != null ? ctx.CurrentAttack.impulseDecay : 25f;
                currentLungeSpeed = Mathf.MoveTowards(currentLungeSpeed, 0f, decay * dt);
            }
        }

        private void HandleAttack()
        {
            if (!currentCtx.IsAttackWindowOpen) return;

            var col = currentCtx.HitCollider;
            Vector3 center = col.transform.TransformPoint(col.center);
            Vector3 halfExtents = Vector3.Scale(col.size, col.transform.lossyScale) * 0.5f;
            Quaternion orientation = col.transform.rotation;
            
            Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, currentCtx.HittableLayers);

            var attack = currentCtx.CurrentAttack;
            float damage = attack != null ? attack.damage : 25f;
            float poiseDamage = attack != null ? attack.poiseDamage : 10f;
            float hitStopDuration = attack != null ? attack.hitStopDuration : 0.05f;
            float impactMultiplier = attack != null ? attack.impactMultiplier : 1f;
            float knockbackForce = attack != null ? attack.knockbackForce : 6f;

            foreach (var hit in hits)
            {
                if (!hitThisAttack.Contains(hit))
                {
                    hitThisAttack.Add(hit);
                    if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        Vector3 hitPoint = HitPointUtility.OnTargetSurface(hit, currentCtx.Transform.position, out Vector3 hitNormal);
                        Vector3 hitDir = hit.transform.position - currentCtx.Transform.position;
                        hitDir.y = 0f;
                        if (hitDir.sqrMagnitude > 0.001f)
                            hitDir.Normalize();
                        else
                            hitDir = currentCtx.Transform.forward;

                        HitInfo info = new HitInfo
                        {
                            damage = damage,
                            poiseDamage = poiseDamage,
                            impactMultiplier = impactMultiplier,
                            knockbackForce = knockbackForce,
                            hitPoint = hitPoint,
                            hitNormal = hitNormal,
                            hitDirection = hitDir,
                            attacker = currentCtx.Transform.gameObject
                        };

                        damageable.TakeHit(info);

                        if (attack != null && attack.hitVfxPrefab != null)
                        {
                            Vector3 vfxPosition = hitPoint + hitNormal * 0.08f;
                            GameObject vfxObject = Object.Instantiate(attack.hitVfxPrefab, vfxPosition,
                                Quaternion.LookRotation(hitNormal));
                            foreach (ParticleSystem particles in vfxObject.GetComponentsInChildren<ParticleSystem>())
                            {
                                var main = particles.main;
                                main.useUnscaledTime = true;
                            }
                        }

                        currentCtx.CameraManager?.Shake(0.22f, 0.1f);
                        currentCtx.TriggerHitStop?.Invoke(hitStopDuration);
                    }
                }
            }
        }
    }
}
