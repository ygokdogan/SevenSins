using System;
using Input;
using Player.Movement;
using UnityEngine;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private StateMachine<MovementContext> stateMachine;
        private MovementContext ctx;
        
        public bool IsSprinting => ctx.IsSprinting;
        public bool IsGrounded => stateMachine.CurrentState == ctx.States.Grounded;
        public Vector3 Velocity => ctx.Velocity;
        public float VerticalVelocity => ctx != null ? ctx.VerticalVelocity : 0f;
        public float CurrentMaxSpeed => IsSprinting ? sprintSpeed : walkSpeed;
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

        private void Awake()
        {
            var states = new MovementStates
            {
                Grounded = new GroundedState(),
                Airborne = new AirborneState(),
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
            };

            stateMachine = new StateMachine<MovementContext>(ctx);
            ctx.RequestTransition = stateMachine.ChangeState;
            stateMachine.ChangeState(ctx.States.Grounded);
        }

        void Update() => stateMachine.Tick(Time.deltaTime);
    }
}
