using Combat;
using Enemies.AttackStyles.Shield;
using Enemies.Core;
using Enemies.States;
using Player.Combat;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Enemies.Types.Shield
{
    [RequireComponent(typeof(ShieldAttackStyle))]
    public sealed class EnemyShield : Core.EnemyBase
    {
        [SerializeField] private Transform shield;
        [SerializeField, Min(0f)] private float grappleOpeningDuration = 2f;
        private float exposedUntil;
        public bool ShieldBroken { get; private set; }
        public bool IsBashing { get; set; }
        public bool GuardOpen => ShieldBroken || IsGrappleLocked || Time.time < exposedUntil;
        protected override void ConfigureStates(EnemyCombatStates states)
        {
            states.Approach = new EnemyApproachState(attackDirectly: true,
                locomotion: EnemyLocomotionAnimation.Mode.Walk, turnSpeed: 110f);
            states.Engage = states.Approach;
        }

        public bool BlocksDirection(Vector3 directionToAttacker)
        {
            directionToAttacker.y = 0f;
            return !GuardOpen && directionToAttacker.sqrMagnitude > 0.001f &&
                Vector3.Dot(transform.forward, directionToAttacker.normalized) > 0.5f;
        }

        protected override bool TryBlockHit(HitInfo hit)
        {
            Vector3 direction = hit.attacker != null
                ? hit.attacker.transform.position - transform.position : -hit.hitDirection;
            if (BlocksDirection(direction))
            {
                // A guarded strike never enters HitState or interrupts the bash.
                hit.damage = 0f;
                hit.impactMultiplier = 0.15f;
                // TODO(Animation): Play the shield block reaction.
                hit.attacker?.GetComponentInParent<PlayerCombat>()?.OnStrikeBlocked(transform.position);
                Feedback.PlayBlock(hit.impactMultiplier);

                return true;
            }
            return false;
        }

        protected override void OnParried()
        {
            if (IsBashing && !ShieldBroken)
            {
                ShieldBroken = true;
                if (shield != null)
                {
                    // Assign only the shield mesh/pivot, not the manually authored hand rig.
                    // TODO(Animation): Play shield break / drop animation and handle its visual cleanup.
                    shield.gameObject.SetActive(false);
                    shield = null;
                }
            }
        }

        public override void OnGrapplePulled()
        {
            exposedUntil = Time.time + grappleOpeningDuration;
            // TODO(Animation): Play grapple pull / guard opening; restore guard pose when GuardOpen becomes false.
        }

        protected override void OnGrappleReleased()
        {
            exposedUntil = Time.time + grappleOpeningDuration;
        }
    }
}
