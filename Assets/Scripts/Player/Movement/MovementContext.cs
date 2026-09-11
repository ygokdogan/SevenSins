using System;
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

        public bool IsSprinting => Input.SprintInput && Velocity.sqrMagnitude > 0.01f;

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
    }

    public class MovementStates
    {
        public GroundedState Grounded;
        public AirborneState Airborne;
    }
}
