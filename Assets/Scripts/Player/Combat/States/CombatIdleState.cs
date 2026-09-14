namespace Player.Combat.States
{
    public class CombatIdleState : IState<CombatContext>
    {
        public void Enter(CombatContext ctx)
        {
            ctx.CurrentAttack = null;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = false;
        }

        public void Tick(CombatContext ctx, float dt)
        {
            if (ctx.Input.SheatheInput)
            {
                ctx.Input.UseSheatheInput();
                ctx.RequestTransition(ctx.States.Sheathe);
                return;
            }

            if (ctx.Input.PrimaryAttackInput)
            {
                if (ctx.IsSheathed)
                {
                    ctx.CurrentAttack = ctx.SheathedAttack != null ? ctx.SheathedAttack : ctx.DefaultLightAttack;
                    ctx.IsSheathed = false;
                }
                else
                {
                    ctx.CurrentAttack = ctx.DefaultLightAttack;
                }

                if (ctx.CurrentAttack != null)
                {
                    ctx.RequestTransition(ctx.States.Attack);
                    return;
                }
            }
        }

        public void Exit(CombatContext ctx)
        {
        }
    }
}