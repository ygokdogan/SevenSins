using UnityEngine;
using Utilities;

namespace Player.Movement.States
{
    public sealed class AirborneState : PlayerMovementState
    {
        private PlayerMovementConfig.GroundSettings groundConfig;
        private PlayerMovementConfig.JumpSettings jumpConfig;

        private float targetSpeed;

        public override void Initialize(PlayerMovement movement)
        {
            groundConfig = movement.config.Ground;
            jumpConfig = movement.config.Jump;
        }

        private float coyoteTimer;
        public bool PreserveMomentum { get; private set; }

        public void ResetOnLanding(float coyoteTime)
        {
            coyoteTimer = coyoteTime;
            PreserveMomentum = false;
        }

        public void ConsumeCoyoteTime() => coyoteTimer = 0f;
        public void SetMomentumPreservation(bool preserve) => PreserveMomentum = preserve;

        public override void Enter(PlayerMovement movement)
        {
            targetSpeed = movement.CurrentMaxSpeed;
        }

        public override void Tick(PlayerMovement movement, float dt)
        {

            if (movement.DashState.TryStart(movement, airborne: true)) return;

            movement.WallRunState.TickCooldown(dt);
            if (PreserveMomentum)
                UpdateMomentumAirControl(movement, dt);
            else
                MovementUtility.UpdatePlanarVelocity(movement, targetSpeed, dt, groundConfig.AirControl);

            coyoteTimer -= dt;
            if (movement.CanMove && movement.Player.Input.JumpInput && coyoteTimer > 0f)
            {
                MovementUtility.StartJump(movement);
                movement.Player.Input.UseJumpInput();
            }

            if (!PreserveMomentum && movement.Player.Input.JumpInputReleased && movement.VerticalVelocity > 0f)
            {
                movement.VerticalVelocity *= jumpConfig.CutMultiplier;
                movement.Player.Input.ConsumeJumpRelease();
            }

            if (movement.WallRunState.TryStart(movement)) return;

            float gravityMultiplier = movement.VerticalVelocity > 0f
                ? (movement.Player.Input.IsJumpHeld ? 1f : jumpConfig.LowJumpGravityMultiplier)
                : jumpConfig.FallGravityMultiplier;
            if (PreserveMomentum) gravityMultiplier = 1f;
            float fallLimit = PreserveMomentum
                ? Mathf.Min(movement.VerticalVelocity, -jumpConfig.MaxFallSpeed)
                : -jumpConfig.MaxFallSpeed;
            movement.VerticalVelocity = Mathf.Max(
                movement.VerticalVelocity - jumpConfig.Gravity * gravityMultiplier * dt, fallLimit);

            movement.Player.Controller.Move(new Vector3(movement.Velocity.x, movement.VerticalVelocity, movement.Velocity.z) * dt);
            if (movement.Player.Controller.isGrounded && movement.VerticalVelocity <= 0f)
                movement.ChangeState(movement.GroundedState);
        }

        public override void Exit(PlayerMovement movement) { }

        private void UpdateMomentumAirControl(PlayerMovement player, float dt)
        {
            if (!player.CanMove) return;

            Vector2 input = Vector2.ClampMagnitude(player.Player.Input.MoveInput, 1f);
            Vector3 wish = player.transform.right * input.x + player.transform.forward * input.y;
            wish.y = 0f;
            float amount = wish.magnitude;
            if (amount < 0.001f) return;

            wish /= amount;
            float speed = groundConfig.SprintSpeed * player.SpeedMultiplier;
            float room = Mathf.Max(0f, speed - Vector3.Dot(player.Velocity, wish));
            player.Velocity += wish * Mathf.Min(room, groundConfig.Acceleration * groundConfig.AirControl * amount * dt);
        }
    }
}
