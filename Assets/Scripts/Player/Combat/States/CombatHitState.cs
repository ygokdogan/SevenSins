using Combat;
using Player.Movement;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatHitState : PlayerCombatState
    {
        private float remaining;
        private HitInfo currentHit;

        public void Begin(PlayerCombat combat, HitInfo hit)
        {
            currentHit = hit;
            combat.ForceChangeState(this);
        }

        public override void Enter(PlayerCombat combat)
        {
            remaining = config.HitRecoveryDuration * Mathf.Max(0f, currentHit.impactMultiplier);
            player.Input.UsePrimaryAttackInput();
            player.Input.UseSheatheInput();
            player.Input.UseBlockInput();
            player.Input.UseJumpInput();
            player.Input.ConsumeJumpRelease();

            player.Movement.Restrictions.Add(this, MovementRestriction.Move | MovementRestriction.Sprint);
            player.Movement.StopPlanarMovement();
            player.AnimationManager.PlayHitReaction(combat.IsSheathed);
        }

        public override void Tick(PlayerCombat combat, float deltaTime)
        {
            remaining -= deltaTime;
            if (remaining <= 0f) combat.ChangeState(combat.IdleState);
        }

        public override void Exit(PlayerCombat combat)
        {
            player.Movement.Restrictions.RemoveAll(this);
        }
    }
}
