using System;
using Core;
using Input;
using UnityEngine;

namespace Player.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerManager))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, HideInInspector] private PlayerMovementConfig config;
        private PlayerManager player;

        private StateMachine<PlayerMovement> stateMachine;
        private PlayerMovementRuntimeData data => player.MovementData;

        private PlayerMovementConfig Config => player.MovementConfig;
        public PlayerMovementRuntimeData Data => data;
        private InputHandler Input => player.Input;
        private CharacterController Controller => player.Controller;
        private GrappleController Grapple => player.Grapple;

        internal GroundedState GroundedState { get; private set; }
        internal AirborneState AirborneState { get; private set; }
        internal WallRunState WallRunState { get; private set; }
        internal PlayerGrappleState GrappleState { get; private set; }
        internal DashState DashState { get; private set; }

        public bool CanMove { get => data.CanMove; set => data.CanMove = value; }
        public bool CanSprint { get => data.CanSprint; set => data.CanSprint = value; }
        public float SpeedMultiplier { get => data.SpeedMultiplier; set => data.SpeedMultiplier = value; }

        public bool IsSprinting => data.CanSprint && Input.SprintInput && data.Velocity.sqrMagnitude > 0.01f;
        public bool IsGrounded => ReferenceEquals(stateMachine.CurrentState, GroundedState);
        public bool IsWallRunning => ReferenceEquals(stateMachine.CurrentState, WallRunState);
        public bool IsGrappling => ReferenceEquals(stateMachine.CurrentState, GrappleState);
        public bool IsDashing => ReferenceEquals(stateMachine.CurrentState, DashState);
        public Vector3 WallNormal => data.WallNormal;
        public bool IsGrappleWallRunning => IsWallRunning && data.IsGrappleWallRunning;
        public Vector3 WallRunDirection => data.WallRunDirection;
        public Vector3 GrappleAnchor => data.GrappleAnchor;
        public Vector3 Velocity { get => data.Velocity; set => data.Velocity = value; }
        public float VerticalVelocity => data.VerticalVelocity;
        public float CurrentMaxSpeed =>
            (IsSprinting ? Config.Ground.SprintSpeed : Config.Ground.WalkSpeed) * data.SpeedMultiplier;

        public event Action<float> Landed;
        public event Action GrappleLaunched;
        public event Action<Vector3> DashStarted;
        public event Action DashEnded;

        private void Awake()
        {
            player = GetComponent<PlayerManager>();
            player.Initialize(config);

            GroundedState = player.GetOrAddState<GroundedState>();
            AirborneState = player.GetOrAddState<AirborneState>();
            WallRunState = player.GetOrAddState<WallRunState>();
            GrappleState = player.GetOrAddState<PlayerGrappleState>();
            DashState = player.GetOrAddState<DashState>();

            stateMachine = new StateMachine<PlayerMovement>(this);
            stateMachine.ChangeState(GroundedState);

            if (GetComponent<DashVisualEffects>() == null)
                gameObject.AddComponent<DashVisualEffects>();
        }

        internal void ChangeState(PlayerMovementState nextState) => stateMachine.ChangeState(nextState);
        internal void ResumeLocomotion() => ChangeState(Controller.isGrounded && data.VerticalVelocity <= 0f
            ? GroundedState : AirborneState);
        internal void NotifyLanded(float speed) => Landed?.Invoke(speed);
        internal void NotifyGrappleLaunched() => GrappleLaunched?.Invoke();
        internal void NotifyDashStarted(Vector3 direction) => DashStarted?.Invoke(direction);
        internal void NotifyDashEnded() => DashEnded?.Invoke();

        public bool TryStartGrapple(Vector3 anchorPoint, Vector3 surfaceNormal, bool flyThrough)
        {
            if (!data.CanMove || IsGrappling) return false;
            data.GrappleAnchor = anchorPoint;
            data.GrappleNormal = surfaceNormal.normalized;
            data.GrappleFlyThrough = flyThrough;
            data.GrappleHeld = true;
            ChangeState(GrappleState);
            return IsGrappling;
        }

        public void StopGrapple()
        {
            data.GrappleHeld = false;
            if (IsGrappling)
                ResumeLocomotion();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (Grapple != null && Grapple.isActiveAndEnabled)
                Grapple.ProcessInput();
            stateMachine.Tick(dt);

            player.TickKnockback(dt);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!IsGrappling && !data.PreserveAirMomentum) return;
            Vector3 velocity = data.Velocity + Vector3.up * data.VerticalVelocity;
            float intoSurface = Vector3.Dot(velocity, hit.normal);
            if (intoSurface >= 0f) return;
            velocity -= hit.normal * intoSurface;
            data.Velocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            data.VerticalVelocity = velocity.y;
        }
    }
}
