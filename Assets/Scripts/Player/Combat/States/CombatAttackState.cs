using System.Collections.Generic;
using Combat;
using Player.Movement;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatAttackState : PlayerCombatState
    {
        private PlayerCombat combat;
        private BoxCollider hitCollider;
        private LayerMask hittableLayers;
        private bool comboWindowOpen;
        private bool attackWindowOpen;
        private bool animationFinished;
        private Vector3 lungeDirection;
        private float currentLungeSpeed;
        private bool isStationaryLunge;
        private readonly HashSet<Collider> hitTargets = new();

        public AttackData CurrentAttack { get; private set; }
        public bool IsAttackWindowOpen => attackWindowOpen && !animationFinished;
        public uint AttackSequence { get; private set; }
        public uint AttackWindowSequence { get; private set; }

        public override void Initialize(PlayerCombat combat)
        {
            base.Initialize(combat);
            this.combat = combat;
            hitCollider = combat.HitCollider;
            hittableLayers = combat.HittableLayers;
        }

        public bool Begin(PlayerCombat combat, AttackData attack)
        {
            if (attack == null) return false;
            CurrentAttack = attack;
            if (combat.CurrentState == this) Enter(combat);
            else combat.ChangeState(this);
            return true;
        }
        
        public override void Enter(PlayerCombat combat)
        {
            AttackSequence++;
            player.Movement.Restrictions.RemoveAll(this);
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            lungeDirection = combat.transform.forward;

            hitTargets.Clear();
            
            player.Input.UsePrimaryAttackInput();
            player.Movement.Restrictions.Add(this, MovementRestriction.Sprint);
            
            combat.IsSheathed = false;
            comboWindowOpen = false;
            animationFinished = false;
            attackWindowOpen = false;

            player.AnimationManager.PlayAttackAnimation(CurrentAttack.animationName,
                CurrentAttack.crossFadeDuration);
        }

        public override void OnLungeTriggered()
        {
            if (CurrentAttack == null) return;

            bool isStandingStill = player.Input.MoveInput.sqrMagnitude < 0.01f;

            if (CurrentAttack.hasLunge)
            {
                player.Movement.Restrictions.Add(this, MovementRestriction.Move);
                player.Movement.StopPlanarMovement();
                currentLungeSpeed = CurrentAttack.forwardImpulse;
                isStationaryLunge = false;
            }
            else if (isStandingStill && CurrentAttack.forwardImpulse > 0.01f)
            {
                player.Movement.Restrictions.Add(this, MovementRestriction.Move);
                player.Movement.StopPlanarMovement();
                currentLungeSpeed = CurrentAttack.forwardImpulse;
                isStationaryLunge = true;
            }
        }

        public override void Tick(PlayerCombat combat, float deltaTime)
        {
            HandleAttackLunge(deltaTime);
            HandleAttack();
            if (combat.CurrentState != this) return;
            
            if (comboWindowOpen && player.Input.PrimaryAttackInput)
            {
                if (CurrentAttack != null && CurrentAttack.nextLightAttack != null)
                {
                    player.Input.UsePrimaryAttackInput();
                    CurrentAttack = CurrentAttack.nextLightAttack;
                    
                    Enter(combat);
                    return;
                }
            }

            if (comboWindowOpen && (player.Input.BlockInput || player.Input.IsBlockHeld))
            {
                combat.ChangeState(combat.BlockState);
                return;
            }

            if (animationFinished)
            {
                combat.ChangeState(combat.IdleState);
            }
        }

        public override void Exit(PlayerCombat combat)
        {
            player.Movement.Restrictions.RemoveAll(this);
            hitTargets.Clear();

            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            comboWindowOpen = false;
            animationFinished = false;
            attackWindowOpen = false;
            CurrentAttack = null;
        }

        public override void OnComboWindowOpened() => comboWindowOpen = true;
        public override void OnComboWindowClosed() => comboWindowOpen = false;
        public override void OnAttackWindowOpened()
        {
            attackWindowOpen = true;
            AttackWindowSequence++;
        }
        public override void OnAttackWindowClosed() => attackWindowOpen = false;
        public override void OnAnimationFinished() => animationFinished = true;

        private void HandleAttackLunge(float deltaTime)
        {
            if (isStationaryLunge)
            {
                if (player.Input.MoveInput.sqrMagnitude > 0.01f || currentLungeSpeed <= 0.01f)
                {
                    currentLungeSpeed = 0f;
                    player.Movement.Restrictions.Remove(this, MovementRestriction.Move);
                    isStationaryLunge = false;
                }
            }

            if (comboWindowOpen)
            {
                player.Movement.Restrictions.Remove(this, MovementRestriction.Move);
            }
            
            if (currentLungeSpeed > 0.01f)
            {
                Vector3 currentMoveVelocity = player.Movement.Velocity;
                Vector3 desiredLungeVelocity = lungeDirection * currentLungeSpeed;
                Vector3 extraVelocity = desiredLungeVelocity - currentMoveVelocity;

                player.Controller.Move(extraVelocity * deltaTime);

                float decay = CurrentAttack != null ? CurrentAttack.impulseDecay : 25f;
                currentLungeSpeed = Mathf.MoveTowards(currentLungeSpeed, 0f, decay * deltaTime);
            }
        }

        private void HandleAttack()
        {
            if (!attackWindowOpen) return;

            Vector3 center = hitCollider.transform.TransformPoint(hitCollider.center);
            Vector3 halfExtents = Vector3.Scale(hitCollider.size, hitCollider.transform.lossyScale) * 0.5f;
            Quaternion orientation = hitCollider.transform.rotation;
            
            Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, hittableLayers);

            var attack = CurrentAttack;
            float damage = attack != null ? attack.damage : 25f;
            float poiseDamage = attack != null ? attack.poiseDamage : 10f;
            float hitStopDuration = attack != null ? attack.hitStopDuration : 0.05f;
            float impactMultiplier = attack != null ? attack.impactMultiplier : 1f;
            float knockbackForce = attack != null ? attack.knockbackForce : 6f;

            foreach (var hit in hits)
            {
                if (hitTargets.Add(hit))
                {
                    if (hit.TryGetComponent(out IDamageable damageable))
                    {
                        Vector3 hitPoint = HitPointUtility.OnTargetSurface(hit, combat.transform.position, out Vector3 hitNormal);
                        Vector3 hitDir = hit.transform.position - combat.transform.position;
                        hitDir.y = 0f;
                        if (hitDir.sqrMagnitude > 0.001f)
                            hitDir.Normalize();
                        else
                            hitDir = combat.transform.forward;

                        HitInfo info = new HitInfo
                        {
                            damage = damage,
                            poiseDamage = poiseDamage,
                            impactMultiplier = impactMultiplier,
                            knockbackForce = knockbackForce,
                            hitPoint = hitPoint,
                            hitNormal = hitNormal,
                            hitDirection = hitDir,
                            attacker = combat.transform.gameObject
                        };

                        damageable.TakeHit(info);
                        if (damage > 0f) combat.NotifyHitConfirmed();

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

                        player.Camera?.Shake(0.22f, 0.1f);
                        combat.TriggerHitStop(hitStopDuration);
                        if (combat.CurrentState != this) return;
                    }
                }
            }
        }
    }
}
