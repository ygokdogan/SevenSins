using UnityEngine;

namespace Player.Movement
{
    public static class MovementUtility
    {
        public static void UpdatePlanarVelocity(PlayerMovement player, float dt, float controlMultiplier)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.GroundSettings config = player.Config.Ground;
            if (!data.CanMove) return;

            Vector2 input = Vector2.ClampMagnitude(player.Input.MoveInput, 1f);
            float targetSpeed = (player.IsSprinting ? config.SprintSpeed : config.WalkSpeed) * data.SpeedMultiplier;
            Vector3 targetVelocity = (player.transform.right * input.x + player.transform.forward * input.y) * targetSpeed;
            targetVelocity.y = 0f;

            Vector3 currentVelocity = Vector3.ProjectOnPlane(data.Velocity, Vector3.up);
            bool changingDirection = input.sqrMagnitude > 0.001f &&
                                     currentVelocity.sqrMagnitude > 0.001f &&
                                     Vector3.Dot(currentVelocity.normalized, targetVelocity.normalized) < 0f;
            float rate = changingDirection
                ? config.TurnAcceleration
                : input.sqrMagnitude > 0.001f ? config.Acceleration : config.Deceleration;

            data.Velocity = Vector3.MoveTowards(currentVelocity, targetVelocity, rate * controlMultiplier * dt);
        }

        public static void StartJump(PlayerMovement player)
        {
            PlayerMovementRuntimeData data = player.Data;
            PlayerMovementConfig.JumpSettings config = player.Config.Jump;
            data.VerticalVelocity = Mathf.Sqrt(config.Height * 2f * config.Gravity);
            data.CoyoteTimer = 0f;
        }
    }
}
