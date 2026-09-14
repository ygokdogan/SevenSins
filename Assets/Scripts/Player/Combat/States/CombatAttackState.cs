using UnityEngine;

namespace Player.Combat.States
{
    public class CombatAttackState : IState<CombatContext>
    {
        private Vector3 lungeDirection;
        private float currentLungeSpeed;
        private CombatContext currentCtx;
        private bool isStationaryLunge;
        
        public void Enter(CombatContext ctx)
        {
            currentCtx = ctx;
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            
            ctx.Input.UsePrimaryAttackInput();
            ctx.Movement.CanSprint = false;
            
            ctx.IsSheathed = false;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = false;
            
            lungeDirection = ctx.Transform.forward;

            ctx.OnLungeTriggered += HandleLunge;
            ctx.AnimationManager.PlayAttackAnimation(ctx.CurrentAttack.animationName, ctx.CurrentAttack.crossFadeDuration);
        }

        private void HandleLunge()
        {
            if (currentCtx?.CurrentAttack == null) return;

            bool isStandingStill = currentCtx.Input.MoveInput.sqrMagnitude < 0.01f;

            if (currentCtx.CurrentAttack.hasLunge)
            {
                currentCtx.Movement.CanMove = false;
                currentCtx.Movement.Velocity = Vector3.zero;
                currentLungeSpeed = currentCtx.CurrentAttack.forwardImpulse;
                isStationaryLunge = false;
            }
            else if (isStandingStill && currentCtx.CurrentAttack.forwardImpulse > 0.01f)
            {
                currentCtx.Movement.CanMove = false;
                currentCtx.Movement.Velocity = Vector3.zero;
                currentLungeSpeed = currentCtx.CurrentAttack.forwardImpulse;
                isStationaryLunge = true;
            }
        }

        public void Tick(CombatContext ctx, float dt)
        {
            if (isStationaryLunge)
            {
                if (ctx.Input.MoveInput.sqrMagnitude > 0.01f || currentLungeSpeed <= 0.01f)
                {
                    currentLungeSpeed = 0f;
                    ctx.Movement.CanMove = true;
                    isStationaryLunge = false;
                }
            }

            if (ctx.IsComboWindowOpen)
            {
                ctx.Movement.CanMove = true;
            }
            
            if (currentLungeSpeed > 0.01f)
            {
                Vector3 currentMoveVel = ctx.Movement != null ? ctx.Movement.Velocity : Vector3.zero;
                Vector3 desiredLungeVel = lungeDirection * currentLungeSpeed;
                Vector3 extraVelocity = desiredLungeVel - currentMoveVel;

                ctx.Controller.Move(extraVelocity * dt);

                float decay = ctx.CurrentAttack != null ? ctx.CurrentAttack.impulseDecay : 25f;
                currentLungeSpeed = Mathf.MoveTowards(currentLungeSpeed, 0f, decay * dt);
            }
            
            if (ctx.IsComboWindowOpen && ctx.Input.PrimaryAttackInput)
            {
                if (ctx.CurrentAttack != null && ctx.CurrentAttack.nextLightAttack != null)
                {
                    ctx.Input.UsePrimaryAttackInput();
                    ctx.CurrentAttack = ctx.CurrentAttack.nextLightAttack;
                    
                    ctx.OnLungeTriggered -= HandleLunge;
                    Enter(ctx);
                    return;
                }
            }

            if (ctx.IsAnimationFinished)
            {
                ctx.RequestTransition(ctx.States.Idle);
            }
        }

        public void Exit(CombatContext ctx)
        {
            ctx.OnLungeTriggered -= HandleLunge;
            ctx.Movement.CanSprint = true;
            ctx.Movement.CanMove = true;
            
            currentLungeSpeed = 0f;
            isStationaryLunge = false;
            currentCtx = null;
            ctx.IsComboWindowOpen = false;
            ctx.IsAnimationFinished = true;
        }
    }
}