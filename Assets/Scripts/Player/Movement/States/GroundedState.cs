using UnityEngine;
using Utilities;

namespace Player.Movement.States
{
    public sealed class GroundedState : PlayerMovementState
    {
        private PlayerMovementConfig.GroundSettings groundConfig;
        private PlayerMovementConfig.JumpSettings jumpConfig;

        public override void Initialize(PlayerMovement movement)
        {
            groundConfig = movement.config.Ground;
            jumpConfig = movement.config.Jump;
        }

        public override void Enter(PlayerMovement movement)
        {
            movement.AirborneState.ResetOnLanding(jumpConfig.CoyoteTime);
            movement.DashState.RestoreAirDash();
            movement.WallRunState.ResetOnLanding();

            float landingSpeed = Mathf.Max(0f, -movement.VerticalVelocity);
            if (landingSpeed > 0f)
            {
                movement.NotifyLanded(landingSpeed);
                movement.Player.Camera?.LandingImpact(landingSpeed);
            }

            if (movement.VerticalVelocity < 0f) movement.VerticalVelocity = -2f;
        }

        public override void Tick(PlayerMovement movement, float dt)
        {
            if (movement.DashState.TryStart(movement, airborne: false)) return;
            
            MovementUtility.UpdatePlanarVelocity(movement, movement.CurrentMaxSpeed, dt, 1f);
            if (movement.CanMove && movement.Player.Input.JumpInput)
            {
                MovementUtility.StartJump(movement);
                movement.Player.Input.UseJumpInput();
                movement.ChangeState(movement.AirborneState);
                return;
            }

            movement.Player.Controller.Move(new Vector3(movement.Velocity.x, -2f, movement.Velocity.z) * dt);
            if (!movement.Player.Controller.isGrounded)
                movement.ChangeState(movement.AirborneState);
        }

        public override void Exit(PlayerMovement movement) { }
    }
}
