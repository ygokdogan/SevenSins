using System;
using Core;
using Input;
using Player.Movement.States;
using UnityEngine;

namespace Player.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerManager))]
    public class PlayerMovement : MonoBehaviour
    {
        public PlayerManager Player { get; private set; }
        public PlayerMovementConfig config;
        public MovementRestrictions Restrictions { get; } = new();

        private StateMachine<PlayerMovement> stateMachine;
        public GroundedState GroundedState { get; private set; }
        public AirborneState AirborneState { get; private set; }
        public WallRunState WallRunState { get; private set; }
        public PlayerGrappleState GrappleState { get; private set; }
        public DashState DashState { get; private set; }

        public bool CanMove => Restrictions.CanMove;
        public bool CanSprint => Restrictions.CanSprint;
        public float SpeedMultiplier => Restrictions.SpeedMultiplier;

        public bool IsSprinting => CanSprint && Player.Input.SprintInput && Velocity.sqrMagnitude > 0.01f;
        public bool IsGrounded => ReferenceEquals(stateMachine.CurrentState, GroundedState);
        public bool IsWallRunning => ReferenceEquals(stateMachine.CurrentState, WallRunState);
        public bool IsGrappling => ReferenceEquals(stateMachine.CurrentState, GrappleState);
        public bool IsDashing => ReferenceEquals(stateMachine.CurrentState, DashState);
        public Vector3 WallNormal => WallRunState.WallNormal;
        public Vector3 WallRunDirection => WallRunState.RunDirection;
        public Vector3 GrappleAnchor => GrappleState.Anchor;
        public Vector3 Velocity { get; set; }
        public float VerticalVelocity { get; set; }
        public float CurrentMaxSpeed =>
            (IsSprinting ? config.Ground.SprintSpeed : config.Ground.WalkSpeed) * SpeedMultiplier;

        public event Action<float> Landed;
        public event Action GrappleLaunched;
        public event Action<Vector3> DashStarted;
        public event Action DashEnded;
        
        private Vector3 knockbackVelocity;

        private void Awake()
        {
            Player = GetComponent<PlayerManager>();

            GroundedState = Player.GetMovementState<GroundedState>();
            AirborneState = Player.GetMovementState<AirborneState>();
            WallRunState = Player.GetMovementState<WallRunState>();
            GrappleState = Player.GetMovementState<PlayerGrappleState>();
            DashState = Player.GetMovementState<DashState>();

            GroundedState.Initialize(this);
            AirborneState.Initialize(this);
            WallRunState.Initialize(this);
            GrappleState.Initialize(this);
            DashState.Initialize(this);

            stateMachine = new StateMachine<PlayerMovement>(this);
            ChangeState(GroundedState);

        }

        public void ChangeState(PlayerMovementState nextState)
        {
            if (ReferenceEquals(stateMachine.CurrentState, nextState)) return;

            stateMachine.ChangeState(nextState);
            if (nextState != null && ReferenceEquals(stateMachine.CurrentState, nextState))
                nextState.CameraProfile.Enter(this);
        }
        public void ResumeLocomotion() => ChangeState(Player.Controller.isGrounded && VerticalVelocity <= 0f
            ? GroundedState : AirborneState);
        public void NotifyLanded(float speed) => Landed?.Invoke(speed);
        public void NotifyGrappleLaunched() => GrappleLaunched?.Invoke();
        public void NotifyDashStarted(Vector3 direction) => DashStarted?.Invoke(direction);
        public void NotifyDashEnded() => DashEnded?.Invoke();

        public bool TryStartGrapple(Vector3 anchorPoint, Vector3 surfaceNormal, bool flyThrough)
        {
            if (!CanMove || IsGrappling) return false;
            GrappleState.Begin(this, anchorPoint, surfaceNormal, flyThrough);
            return IsGrappling;
        }

        public void StopGrapple()
        {
            if (IsGrappling)
                ResumeLocomotion();
        }
        
        public void StopPlanarMovement() => Velocity = Vector3.zero;
        
        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f || force <= 0f) return;
            knockbackVelocity = direction.normalized * force;
        }
        
        private void TickKnockback(float dt)
        {
            if (knockbackVelocity.sqrMagnitude < 0.0001f) return;
            Player.Controller.Move(knockbackVelocity * dt);
            knockbackVelocity *= Mathf.Exp(-config.KnockbackDamping * dt);
        }
        

        private void Update()
        {
            float dt = Time.deltaTime;
            
            if (Player.Camera != null && Player.Camera.isActiveAndEnabled && stateMachine.CurrentState is PlayerMovementState state)
                state.CameraProfile.Handle(this, dt);

            if (Player.GrappleController != null && Player.GrappleController.isActiveAndEnabled)
                Player.GrappleController.ProcessInput();
            
            stateMachine.Tick(dt);

            TickKnockback(dt);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsGrappling && !AirborneState.PreserveMomentum) return;
            Vector3 velocity = Velocity + Vector3.up * VerticalVelocity;
            
            float intoSurface = Vector3.Dot(velocity, hit.normal);
            if (intoSurface >= 0f) return;
            
            velocity -= hit.normal * intoSurface;
            Velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            VerticalVelocity = velocity.y;
        }
    }
}
