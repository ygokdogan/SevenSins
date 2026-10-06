using UnityEngine;

namespace Player.Movement.States
{
    public sealed class DashState : PlayerMovementState
    {
        private PlayerMovementConfig.DashSettings config;

        public override void Initialize(PlayerMovement movement)
        {
            config = movement.config.Dash;
        }

        private float timeRemaining;
        private Vector3 direction;
        private bool startedGrounded;
        private float nextDashTime;
        private bool airDashAvailable = true;

        public void RestoreAirDash() => airDashAvailable = true;

        public bool TryStart(PlayerMovement movement, bool airborne)
        {
            if (!movement.Player.Input.DashInput) return false;
            movement.Player.Input.UseDashInput();
            if (movement.IsGrappling || !movement.CanMove || Time.time < nextDashTime ||
                (airborne && !airDashAvailable)) return false;

            movement.Player.GrappleController?.TryPerformAction();
            if (airborne) airDashAvailable = false;
            movement.ChangeState(this);
            return true;
        }

        public override void Enter(PlayerMovement movement)
        {
            timeRemaining = config.Duration;
            nextDashTime = Time.time + config.Cooldown;
            startedGrounded = movement.Player.Controller.isGrounded;

            Vector2 input = Vector2.ClampMagnitude(movement.Player.Input.MoveInput, 1f);
            direction = movement.transform.right * input.x + movement.transform.forward * input.y;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = movement.transform.forward;
                direction.y = 0f;
            }

            direction.Normalize();
            movement.Velocity = direction * config.Speed;
            movement.VerticalVelocity = startedGrounded ? -2f : 0f;
            movement.AirborneState.SetMomentumPreservation(false);
            movement.NotifyDashStarted(direction);
            movement.Player.Camera?.Shake(0.16f, 0.07f);
        }

        public override void Tick(PlayerMovement movement, float dt)
        {
            if (!movement.CanMove)
            {
                movement.ResumeLocomotion();
                return;
            }

            timeRemaining -= dt;
            movement.Velocity = direction * config.Speed;
            movement.VerticalVelocity = startedGrounded ? -2f : 0f;
            CollisionFlags collisions = movement.Player.Controller.Move(
                new Vector3(movement.Velocity.x, movement.VerticalVelocity, movement.Velocity.z) * dt);

            if (timeRemaining <= 0f || (collisions & CollisionFlags.Sides) != 0)
                movement.ResumeLocomotion();
        }

        public override void Exit(PlayerMovement movement) => movement.NotifyDashEnded();


    }
}
