namespace Player.Combat.States
{
    public class CombatSheatheState : IState<CombatContext>
    {
        public void Enter(CombatContext ctx)
        {
            ctx.IsAnimationFinished = false;
            
            if (ctx.IsSheathed) ctx.AnimationManager.PlayUnsheatheAnimation();
            else ctx.AnimationManager.PlaySheatheAnimation();
        }

        public void Tick(CombatContext ctx, float dt)
        {
            if (ctx.Input.PrimaryAttackInput)
            {
                ctx.CurrentAttack = ctx.DefaultLightAttack;

                if (ctx.CurrentAttack != null)
                {
                    ctx.RequestTransition(ctx.States.Attack);
                    return;
                }
            }

            if (ctx.IsAnimationFinished)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }
        }

        public void Exit(CombatContext ctx)
        { 
            ctx.IsSheathed = !ctx.IsSheathed;
            ctx.IsAnimationFinished = true;
        }
    }
}