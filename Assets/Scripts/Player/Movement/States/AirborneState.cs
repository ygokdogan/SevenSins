using UnityEngine;

namespace Player.Movement
{
    public sealed class AirborneState : PlayerMovementState
    {
        public override void Enter(PlayerMovement player) { }

        public override void Tick(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig config = player.Config;

            if (player.DashState.TryStart(player, airborne: true)) return;

            data.WallRunCooldownTimer = Mathf.Max(0f, data.WallRunCooldownTimer - dt);
            if (data.PreserveAirMomentum)
                UpdateMomentumAirControl(player, dt);
            else
                MovementUtility.UpdatePlanarVelocity(player, dt, config.Ground.AirControl);

            data.CoyoteTimer -= dt;
            if (data.CanMove && player.Input.JumpInput && data.CoyoteTimer > 0f)
            {
                MovementUtility.StartJump(player);
                player.Input.UseJumpInput();
            }

            if (!data.PreserveAirMomentum && player.Input.JumpInputReleased && data.VerticalVelocity > 0f)
            {
                data.VerticalVelocity *= config.Jump.CutMultiplier;
                player.Input.ConsumeJumpRelease();
            }

            if (player.WallRunState.CanStart(player))
            {
                player.ChangeState(player.WallRunState);
                return;
            }

            float gravityMultiplier = data.VerticalVelocity > 0f
                ? (player.Input.IsJumpHeld ? 1f : config.Jump.LowJumpGravityMultiplier)
                : config.Jump.FallGravityMultiplier;
            if (data.PreserveAirMomentum) gravityMultiplier = 1f;
            float fallLimit = data.PreserveAirMomentum
                ? Mathf.Min(data.VerticalVelocity, -config.Jump.MaxFallSpeed)
                : -config.Jump.MaxFallSpeed;
            data.VerticalVelocity = Mathf.Max(
                data.VerticalVelocity - config.Jump.Gravity * gravityMultiplier * dt, fallLimit);

            player.Controller.Move(new Vector3(data.Velocity.x, data.VerticalVelocity, data.Velocity.z) * dt);
            if (player.Controller.isGrounded && data.VerticalVelocity <= 0f)
                player.ChangeState(player.GroundedState);
        }

        public override void Exit(PlayerMovement player) { }

        private static void UpdateMomentumAirControl(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.GroundSettings config = player.Config.Ground;
            if (!data.CanMove) return;

            Vector2 input = Vector2.ClampMagnitude(player.Input.MoveInput, 1f);
            Vector3 wish = player.transform.right * input.x + player.transform.forward * input.y;
            wish.y = 0f;
            float amount = wish.magnitude;
            if (amount < 0.001f) return;

            wish /= amount;
            float targetSpeed = config.SprintSpeed * data.SpeedMultiplier;
            float room = Mathf.Max(0f, targetSpeed - Vector3.Dot(data.Velocity, wish));
            data.Velocity += wish * Mathf.Min(room, config.Acceleration * config.AirControl * amount * dt);
        }
    }
}
