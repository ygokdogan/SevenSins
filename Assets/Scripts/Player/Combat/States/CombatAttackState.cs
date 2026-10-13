using System.Collections.Generic;
using Combat;
using Player.Movement;
using UnityEngine;
using Utilities;

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
        public bool WasBlocked { get; private set; }
        private readonly HashSet<IDamageable> hitTargets = new();

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
            if (attack == null || (player.GrappleController != null &&
                !player.GrappleController.TryPerformAction())) return false;
            CurrentAttack = attack;
            if (combat.CurrentState == this) Enter(combat);
            else combat.ChangeState(this);
            return true;
        }
        
        public override void Enter(PlayerCombat combat)
        {
            AttackSequence++;
            WasBlocked = false;
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
            if (WasBlocked || player.Movement.IsGrappling) return;
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

            if (!player.Movement.IsGrappling && comboWindowOpen && (player.Input.BlockInput || player.Input.IsBlockHeld))
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

        public bool DeflectOnBlock()
        {
            if (WasBlocked || !IsAttackWindowOpen) return false;
            WasBlocked = true;
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            attackWindowOpen = false;
            player.Movement.Restrictions.Remove(this, MovementRestriction.Move);
            player.Movement.StopPlanarMovement();
            return true;
        }

        public override void OnComboWindowOpened() => comboWindowOpen = true;
        public override void OnComboWindowClosed() => comboWindowOpen = false;
        public override void OnAttackWindowOpened()
        {
            if (WasBlocked) return;
            attackWindowOpen = true;
            AttackWindowSequence++;
        }
        public override void OnAttackWindowClosed() => attackWindowOpen = false;
        public override void OnAnimationFinished() => animationFinished = true;

        private void HandleAttackLunge(float deltaTime)
        {
            if (player.Movement.IsGrappling)
            {
                currentLungeSpeed = 0f;
                isStationaryLunge = false;
                player.Movement.Restrictions.Remove(this, MovementRestriction.Move);
                return;
            }
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

            var attack = CurrentAttack;
            MeleeHitUtility.Hit(hitCollider, hittableLayers, attack.ToHitInfo(), combat.gameObject, hitTargets, info =>
            {
                if (WasBlocked) return false;
                
                MeleeHitUtility.SpawnVfx(attack?.hitVfxPrefab, info, true);
                player.Camera?.Shake(0.22f, 0.1f);
                combat.TriggerHitStop(attack != null ? attack.hitStopDuration : 0.05f);
                return combat.CurrentState == this;
            });
        }
    }
}
