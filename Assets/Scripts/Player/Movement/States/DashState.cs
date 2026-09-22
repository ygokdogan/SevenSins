using UnityEngine;

namespace Player.Movement
{
    public sealed class DashState : PlayerMovementState
    {
        private float timeRemaining;
        private Vector3 direction;
        private bool startedGrounded;
        private float nextDashTime;

        public bool TryStart(PlayerMovement player, bool airborne)
        {
            if (!player.Input.DashInput) return false;
            player.Input.UseDashInput();
            if (!player.CanMove || Time.time < nextDashTime ||
                (airborne && !player.Data.AirDashAvailable)) return false;

            if (airborne) player.Data.AirDashAvailable = false;
            player.ChangeState(this);
            return true;
        }

        public override void Enter(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.DashSettings config = player.Config.Dash;
            timeRemaining = config.Duration;
            nextDashTime = Time.time + config.Cooldown;
            startedGrounded = player.Controller.isGrounded;

            Vector2 input = Vector2.ClampMagnitude(player.Input.MoveInput, 1f);
            direction = player.transform.right * input.x + player.transform.forward * input.y;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = player.transform.forward;
                direction.y = 0f;
            }

            direction.Normalize();
            data.Velocity = direction * config.Speed;
            data.VerticalVelocity = startedGrounded ? -2f : 0f;
            data.PreserveAirMomentum = false;
            player.NotifyDashStarted(direction);
        }

        public override void Tick(PlayerMovement player, float dt)
        {
            PlayerMovementRuntimeData data = player.Data;
            if (!data.CanMove)
            {
                player.ResumeLocomotion();
                return;
            }

            timeRemaining -= dt;
            data.Velocity = direction * player.Config.Dash.Speed;
            data.VerticalVelocity = startedGrounded ? -2f : 0f;
            CollisionFlags collisions = player.Controller.Move(
                new Vector3(data.Velocity.x, data.VerticalVelocity, data.Velocity.z) * dt);

            if (timeRemaining <= 0f || (collisions & CollisionFlags.Sides) != 0)
                player.ResumeLocomotion();
        }

        public override void Exit(PlayerMovement player) => player.NotifyDashEnded();

    }
}
