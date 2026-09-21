using System;
using Core;
using Input;
using UnityEngine;

namespace Player.Movement
{
    public class MovementContext
    {
        public CharacterController Controller;
        public InputHandler Input;
        public Transform Transform;
        
        public Action<IState<MovementContext>> RequestTransition;
        public Action<float> NotifyLanded;
        
        public Vector3 Velocity;
        public float VerticalVelocity;
        public float CoyoteTimer;
        public float WallRunCooldownTimer;
        public Vector3 WallNormal;
        public bool HasConsumedWallRunSurface;
        public Vector3 LastWallRunNormal;
        public Vector3 LastWallRunPoint;
        
        public bool CanMove = false;
        public bool CanSprint = true;
        public bool IsSprinting => CanSprint && Input.SprintInput && Velocity.sqrMagnitude > 0.01f;
        public float SpeedMultiplier = 1f;

        public MovementStates States;

        public float WalkSpeed = 6f;
        public float SprintSpeed = 10f;
        public float Acceleration = 12f;
        public float Deceleration = 16f;
        public float TurnAcceleration = 100f;
        public float AirControl = 0.4f;
        public float Gravity = -22f;
        public float FallGravityMultiplier = 1.6f;
        public float LowJumpGravityMultiplier = 2.2f;
        public float MaxFallSpeed = 30f;
        public float JumpHeight = 1.3f;
        public float JumpCutMultiplier = 0.5f;
        public float CoyoteTime = 0.15f;

        public LayerMask WallLayers = ~((1 << 2) | (1 << 6) | (1 << 7) | (1 << 8));
        public float WallCheckDistance = 0.7f;
        public float WallRunSpeed = 9f;
        public float WallRunAcceleration = 35f;
        public float WallRunGravityRampDuration = 0.6f;
        public float WallStickForce = 3f;
        public float WallRunDuration = 1.15f;
        public float WallRunCooldown = 0.2f;
        public float WallRunMinInput = 0.2f;
        public float WallRunMinSpeed = 2.5f;
        public float WallJumpHeight = 1.5f;
        public float WallJumpHorizontalSpeed = 7.5f;
        public float WallJumpForwardCarry = 2.5f;
    }

    public class MovementStates
    {
        public GroundedState Grounded;
        public AirborneState Airborne;
        public WallRunState WallRun;
    }
}
