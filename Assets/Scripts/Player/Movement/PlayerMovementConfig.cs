using System;
using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Western Samurai/Player/Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        public GroundSettings Ground = new();
        public JumpSettings Jump = new();
        public DashSettings Dash = new();
        public WallRunSettings WallRun = new();
        public GrappleSettings Grapple = new();
        [Space]
        [Min(0.1f)] public float KnockbackDamping = 12f;

        [Serializable]
        public sealed class GroundSettings
        {
            [Min(0f)] public float WalkSpeed = 6f;
            [Min(0f)] public float SprintSpeed = 10f;
            [Min(0f)] public float Acceleration = 45f;
            [Min(0f)] public float Deceleration = 60f;
            [Min(0f)] public float TurnAcceleration = 110f;
            [Range(0f, 1f)] public float AirControl = 0.45f;
        }

        [Serializable]
        public sealed class JumpSettings
        {
            [Min(0.01f)] public float Height = 1.3f;
            [Min(0.01f)] public float Gravity = 22f;
            [Min(1f)] public float FallGravityMultiplier = 1.6f;
            [Min(1f)] public float LowJumpGravityMultiplier = 2.2f;
            [Min(0f)] public float MaxFallSpeed = 30f;
            [Range(0.05f, 1f)] public float CutMultiplier = 0.5f;
            [Min(0f)] public float CoyoteTime = 0.15f;
        }

        [Serializable]
        public sealed class DashSettings
        {
            [Min(0f)] public float Speed = 18f;
            [Min(0.01f)] public float Duration = 0.18f;
            [Tooltip("Minimum time between any two dashes.")]
            [Min(0f)] public float Cooldown = 0.55f;
        }

        [Serializable]
        public sealed class WallRunSettings
        {
            public LayerMask Layers = ~((1 << 2) | (1 << 6) | (1 << 7) | (1 << 8));
            [Min(0.05f)] public float CheckDistance = 0.7f;
            [Min(0f)] public float Speed = 9f;
            [Min(0f)] public float Acceleration = 35f;
            [Min(0.05f)] public float GravityRampDuration = 0.6f;
            [Min(0f)] public float StickForce = 3f;
            [Min(0.05f)] public float Duration = 3f;
            [Min(0f)] public float Cooldown = 0.2f;
            [Range(0f, 1f)] public float MinInput = 0.2f;
            [Min(0f)] public float MinSpeed = 2.5f;
            [Min(0.01f)] public float JumpHeight = 1.5f;
            [Min(0f)] public float JumpHorizontalSpeed = 7.5f;
            [Min(0f)] public float JumpForwardCarry = 2.5f;
        }

        [Serializable]
        public sealed class GrappleSettings
        {
            [Min(1f)] public float PullSpeed = 26f;
            [Min(0.05f)] public float RampTime = 0.35f;
            [Min(1f)] public float Steering = 12f;
            [Min(1f)] public float FlySpeed = 30f;
            [Min(0f)] public float FlyLift = 5f;
            [Min(0.1f)] public float ArrivalRadius = 1.4f;
            [Min(0.5f)] public float Timeout = 4f;
        }
    }
}
