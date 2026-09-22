using UnityEngine;

namespace Player.Movement
{
    public sealed class GroundedState : PlayerMovementState
    {
        public override void Enter(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            data.PreserveAirMomentum = false;
            data.AirDashAvailable = true;

            float landingSpeed = Mathf.Max(0f, -data.VerticalVelocity);
            if (landingSpeed > 0f) player.NotifyLanded(landingSpeed);

            data.CoyoteTimer = player.Config.Jump.CoyoteTime;
            data.WallRunCooldownTimer = 0f;
            data.HasConsumedWallRunSurface = false;
            data.LastWallRunNormal = Vector3.zero;
            data.WallNormal = Vector3.zero;
            if (data.VerticalVelocity < 0f) data.VerticalVelocity = -2f;
        }

        public override void Tick(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            if (player.DashState.TryStart(player, airborne: false)) return;

            MovementUtility.UpdatePlanarVelocity(player, dt, 1f);
            if (data.CanMove && player.Input.JumpInput)
            {
                MovementUtility.StartJump(player);
                player.Input.UseJumpInput();
                player.ChangeState(player.AirborneState);
                return;
            }

            player.Controller.Move(new Vector3(data.Velocity.x, -2f, data.Velocity.z) * dt);
            if (!player.Controller.isGrounded)
                player.ChangeState(player.AirborneState);
        }

        public override void Exit(PlayerMovement player) { }
    }
}
