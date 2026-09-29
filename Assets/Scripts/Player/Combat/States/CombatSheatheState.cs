namespace Player.Combat.States
{
    public class CombatSheatheState : PlayerCombatState
    {
        private bool animationFinished;

        public override void Enter(PlayerCombat combat)
        {
            animationFinished = false;
            if (combat.IsSheathed) player.AnimationManager.PlayUnsheatheAnimation();
            else player.AnimationManager.PlaySheatheAnimation();
        }

        public override void Tick(PlayerCombat combat, float deltaTime)
        {
            if (player.Input.PrimaryAttackInput && config.DefaultLightAttack != null)
            {
                combat.AttackState.Begin(combat, config.DefaultLightAttack);
                return;
            }

            if (animationFinished) combat.ChangeState(combat.IdleState);
        }

        public override void Exit(PlayerCombat combat)
        {
            combat.IsSheathed = !combat.IsSheathed;
            animationFinished = false;
        }

        public override void OnAnimationFinished() => animationFinished = true;
    }
}
