using UnityEngine;

namespace Player.Movement
{
    public static class MovementUtility
    {
        public static void UpdatePlanarVelocity(PlayerMovement movement, float dt, float controlMultiplier)
        {
            PlayerMovementConfig.GroundSettings config = movement.config.Ground;
            if (!movement.CanMove) return;

            Vector2 input = Vector2.ClampMagnitude(movement.Player.Input.MoveInput, 1f);
            float targetSpeed = (movement.IsSprinting ? config.SprintSpeed : config.WalkSpeed) * movement.SpeedMultiplier;
            Vector3 targetVelocity = (movement.transform.right * input.x + movement.transform.forward * input.y) * targetSpeed;
            targetVelocity.y = 0f;

            Vector3 currentVelocity = Vector3.ProjectOnPlane(movement.Velocity, Vector3.up);
            bool changingDirection = input.sqrMagnitude > 0.001f &&
                                     currentVelocity.sqrMagnitude > 0.001f &&
                                     Vector3.Dot(currentVelocity.normalized, targetVelocity.normalized) < 0f;
            float rate = changingDirection
                ? config.TurnAcceleration
                : input.sqrMagnitude > 0.001f ? config.Acceleration : config.Deceleration;

            movement.Velocity = Vector3.MoveTowards(currentVelocity, targetVelocity, rate * controlMultiplier * dt);
        }

        public static void StartJump(PlayerMovement movement)
        {
            PlayerMovementConfig.JumpSettings config = movement.config.Jump;
            movement.VerticalVelocity = Mathf.Sqrt(config.Height * 2f * config.Gravity);
            movement.AirborneState.ConsumeCoyoteTime();
        }
    }
}
