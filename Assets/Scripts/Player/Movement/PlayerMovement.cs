using System;
using Core;
using Input;
using UnityEngine;

namespace Player.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        private StateMachine<MovementContext> stateMachine;
        private MovementContext ctx;
        private Vector3 knockbackVelocity;

        [Header("Hit Knockback")]
        [SerializeField, Min(0.1f)] private float knockbackDamping = 12f;

        public bool CanMove
        {
            get => ctx.CanMove;
            set => ctx.CanMove = value;
        }
        public bool CanSprint
        {
            get => ctx.CanSprint;
            set => ctx.CanSprint = value;
        }
        public float SpeedMultiplier
        {
            get => ctx.SpeedMultiplier;
            set => ctx.SpeedMultiplier = value;
        }
        public bool IsSprinting => ctx.IsSprinting;
        public bool IsGrounded => stateMachine.CurrentState == ctx.States.Grounded;
        public bool IsWallRunning => stateMachine.CurrentState == ctx.States.WallRun;
        public Vector3 WallNormal => ctx.WallNormal;

        public Vector3 Velocity
        {
            get => ctx.Velocity;
            set => ctx.Velocity = value;
        }
        public float VerticalVelocity => ctx.VerticalVelocity;
        public float CurrentMaxSpeed => (IsSprinting ? sprintSpeed : walkSpeed) * ctx.SpeedMultiplier;
        
        public event Action<float> Landed;
        
        [Header("Ground Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 6f;
        [SerializeField, Min(0f)] private float sprintSpeed = 10f;
        [SerializeField, Min(0f)] private float acceleration = 45f;
        [SerializeField, Min(0f)] private float deceleration = 60f;
        [SerializeField, Min(0f)] private float turnAcceleration = 110f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.45f;

        [Header("Jump")]
        [SerializeField, Min(0.01f)] private float jumpHeight = 1.3f;
        [SerializeField] private float gravity = -22f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.6f;
        [SerializeField, Min(1f)] private float lowJumpGravityMultiplier = 2.2f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 30f;
        [SerializeField, Range(0.05f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.15f;

        [Header("Wall Run")]
        [SerializeField] private LayerMask wallLayers;
        [SerializeField, Min(0.05f)] private float wallCheckDistance = 0.7f;
        [SerializeField, Min(0f)] private float wallRunSpeed = 9f;
        [SerializeField, Min(0f)] private float wallRunAcceleration = 35f;
        [SerializeField, Min(0.05f)] private float wallRunGravityRampDuration = 0.6f;
        [SerializeField, Min(0f)] private float wallStickForce = 3f;
        [SerializeField, Min(0.05f)] private float wallRunDuration = 3f;
        [SerializeField, Min(0f)] private float wallRunCooldown = 0.2f;
        [SerializeField, Range(0f, 1f)] private float wallRunMinInput = 0.2f;
        [SerializeField, Min(0f)] private float wallRunMinSpeed = 2.5f;
        [SerializeField, Min(0.01f)] private float wallJumpHeight = 1.5f;
        [SerializeField, Min(0f)] private float wallJumpHorizontalSpeed = 7.5f;
        [SerializeField, Min(0f)] private float wallJumpForwardCarry = 2.5f;

        private void Awake()
        {
            var states = new MovementStates
            {
                Grounded = new GroundedState(),
                Airborne = new AirborneState(),
                WallRun = new WallRunState(),
            };

            ctx = new MovementContext
            {
                Controller = GetComponentInParent<CharacterController>(),
                Input = GetComponent<InputHandler>(),
                Transform = transform,
                States = states,
                NotifyLanded = landingSpeed => Landed?.Invoke(landingSpeed),
                WalkSpeed = walkSpeed,
                SprintSpeed = sprintSpeed,
                Acceleration = acceleration,
                Deceleration = deceleration,
                TurnAcceleration = turnAcceleration,
                AirControl = airControl,
                JumpHeight = jumpHeight,
                Gravity = -Mathf.Abs(gravity),
                FallGravityMultiplier = fallGravityMultiplier,
                LowJumpGravityMultiplier = lowJumpGravityMultiplier,
                MaxFallSpeed = maxFallSpeed,
                JumpCutMultiplier = jumpCutMultiplier,
                CoyoteTime = coyoteTime,
                WallLayers = wallLayers,
                WallCheckDistance = wallCheckDistance,
                WallRunSpeed = wallRunSpeed,
                WallRunAcceleration = wallRunAcceleration,
                WallRunGravityRampDuration = wallRunGravityRampDuration,
                WallStickForce = wallStickForce,
                WallRunDuration = wallRunDuration,
                WallRunCooldown = wallRunCooldown,
                WallRunMinInput = wallRunMinInput,
                WallRunMinSpeed = wallRunMinSpeed,
                WallJumpHeight = wallJumpHeight,
                WallJumpHorizontalSpeed = wallJumpHorizontalSpeed,
                WallJumpForwardCarry = wallJumpForwardCarry,
                CanMove = true,
                CanSprint = true,
            };

            stateMachine = new StateMachine<MovementContext>(ctx);
            ctx.RequestTransition = stateMachine.ChangeState;
            stateMachine.ChangeState(ctx.States.Grounded);
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f || force <= 0f) return;
            knockbackVelocity = direction.normalized * force;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            stateMachine.Tick(dt);
            if (knockbackVelocity.sqrMagnitude < 0.0001f) return;

            ctx.Controller.Move(knockbackVelocity * dt);
            knockbackVelocity *= Mathf.Exp(-knockbackDamping * dt);
        }
    }
}
