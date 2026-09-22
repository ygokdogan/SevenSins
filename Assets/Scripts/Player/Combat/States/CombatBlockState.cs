using Core;
using UnityEngine;

namespace Player.Combat.States
{
    public class CombatBlockState : IState<CombatContext>
    {
        private const float ParryWindowDuration = 0.2f;
        private const float BlockMoveSpeedMultiplier = 0.7f;
        private float blockTimer;

        public void Enter(CombatContext ctx)
        {
            bool canParry = ctx.Input.BlockInput && Time.time >= ctx.NextParryTime;
            ctx.Input.UseBlockInput();
            
            ctx.CurrentAttack = null;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = false;
            
            ctx.IsBlocking = true;
            ctx.IsParrying = canParry;
            if (canParry) ctx.NextParryTime = Time.time + ctx.ParryCooldown;
            blockTimer = 0f;

            if (ctx.IsSheathed)
            {
                ctx.IsSheathed = false;
            }

            ctx.Player.CanSprint = false;
            ctx.Player.SpeedMultiplier = BlockMoveSpeedMultiplier;
            ctx.AnimationManager.PlayBlockAnimation();
        }

        public void Tick(CombatContext ctx, float dt)
        {
            blockTimer += dt;
            if (blockTimer >= ParryWindowDuration)
            {
                ctx.IsParrying = false;
            }
            
            if (!ctx.Input.IsBlockHeld)
            {
                ctx.RequestTransition(ctx.States.Idle);
                return;
            }
            
            if (ctx.Input.PrimaryAttackInput)
            {
                ctx.CurrentAttack = ctx.DefaultLightAttack;
                if (ctx.CurrentAttack != null)
                {
                    ctx.RequestTransition(ctx.States.Attack);
                    return;
                }
            }
        }

        public void Exit(CombatContext ctx)
        {
            ctx.IsBlocking = false;
            ctx.IsParrying = false;
            ctx.Player.CanSprint = true;
            ctx.Player.SpeedMultiplier = 1f;
            ctx.AnimationManager.StopBlockAnimation();
        }
    }
}
