using Player.Movement;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatBlockState : PlayerCombatState
    {
        private float blockTimer;
        private float nextParryTime;

        public bool IsParrying { get; private set; }

        public override void Enter(PlayerCombat combat)
        {
            IsParrying = player.Input.BlockInput && Time.time >= nextParryTime;
            player.Input.UseBlockInput();
            if (IsParrying) nextParryTime = Time.time + config.ParryCooldown;
            blockTimer = 0f;
            combat.IsSheathed = false;

            player.Movement.Restrictions.Add(this, MovementRestriction.Sprint | MovementRestriction.Dash);
            player.Movement.Restrictions.SetSpeedMultiplier(this, config.BlockMoveSpeedMultiplier);
            player.AnimationManager.PlayBlockAnimation();
        }

        public override void Tick(PlayerCombat combat, float deltaTime)
        {
            blockTimer += deltaTime;
            if (blockTimer >= config.ParryWindowDuration) IsParrying = false;

            if (!player.Input.IsBlockHeld)
            {
                combat.ChangeState(combat.IdleState);
                return;
            }

            if (player.Input.PrimaryAttackInput)
                combat.AttackState.Begin(combat, config.DefaultLightAttack);
        }

        public override void Exit(PlayerCombat combat)
        {
            player.Movement.Restrictions.RemoveAll(this);
            IsParrying = false;
            player.AnimationManager.StopBlockAnimation();
        }
    }
}
