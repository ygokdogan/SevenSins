namespace Player.Combat.States
{
    public class CombatIdleState : PlayerCombatState
    {
        public override void Enter(PlayerCombat combat) { }

        public override void Tick(PlayerCombat combat, float deltaTime)
        {
            if (player.Input.SheatheInput)
            {
                player.Input.UseSheatheInput();
                combat.ChangeState(combat.SheatheState);
                return;
            }

            if (!player.Movement.IsGrappling && (player.Input.BlockInput || player.Input.IsBlockHeld))
            {
                combat.ChangeState(combat.BlockState);
                return;
            }

            if (player.Input.PrimaryAttackInput)
            {
                var attack = combat.IsSheathed && config.SheathedAttack != null
                    ? config.SheathedAttack : config.DefaultLightAttack;
                combat.AttackState.Begin(combat, attack);
            }
        }

        public override void Exit(PlayerCombat combat) { }
    }
}
