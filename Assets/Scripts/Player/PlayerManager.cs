using Input;
using Player.Combat;
using Player.Movement;
using UnityEngine;

namespace Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputHandler), typeof(CharacterController))]
    public sealed class PlayerManager : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig movementConfig;
        private bool initialized;
        private bool ownsConfig;
        private Vector3 knockbackVelocity;

        public PlayerMovement Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public InputHandler Input { get; private set; }
        public CharacterController Controller { get; private set; }
        public PlayerAnimationManager Animation { get; private set; }
        public CameraManager Camera { get; private set; }
        public GrappleController Grapple { get; private set; }
        public Transform StatesRoot { get; private set; }
        public PlayerMovementConfig MovementConfig => movementConfig;
        public PlayerMovementRuntimeData MovementData { get; } = new();

        public bool CanMove { get => MovementData.CanMove; set => MovementData.CanMove = value; }
        public bool CanSprint { get => MovementData.CanSprint; set => MovementData.CanSprint = value; }
        public float SpeedMultiplier { get => MovementData.SpeedMultiplier; set => MovementData.SpeedMultiplier = value; }

        private void Awake() => Initialize();

        internal void Initialize(PlayerMovementConfig legacyConfig = null)
        {
            // Also safe when called by a component added at runtime.
            if (legacyConfig != null && movementConfig != legacyConfig)
            {
                if (ownsConfig) Destroy(movementConfig);
                movementConfig = legacyConfig;
                ownsConfig = false;
            }
            if (initialized) return;
            initialized = true;
            
            Movement = GetComponent<PlayerMovement>();
            Combat = GetComponent<PlayerCombat>();
            Input = GetComponent<InputHandler>();
            Controller = GetComponent<CharacterController>();
            Animation = GetComponentInChildren<PlayerAnimationManager>();
            Camera = GetComponent<CameraManager>();
            Grapple = GetComponent<GrappleController>();
            if (movementConfig == null)
                movementConfig = Resources.Load<PlayerMovementConfig>("PlayerMovementConfig");
            if (movementConfig == null)
            {
                movementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
                ownsConfig = true;
            }
            StatesRoot = transform.Find("States");
            if (StatesRoot == null)
            {
                StatesRoot = new GameObject("States").transform;
                StatesRoot.SetParent(transform, false);
            }
        }

        internal T GetOrAddState<T>() where T : PlayerMovementState
        {
            T state = StatesRoot.GetComponent<T>();
            return state != null ? state : StatesRoot.gameObject.AddComponent<T>();
        }

        public void StopPlanarMovement() => MovementData.Velocity = Vector3.zero;

        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f || force <= 0f) return;
            knockbackVelocity = direction.normalized * force;
        }

        internal void TickKnockback(float dt)
        {
            if (knockbackVelocity.sqrMagnitude < 0.0001f) return;
            Controller.Move(knockbackVelocity * dt);
            knockbackVelocity *= Mathf.Exp(-movementConfig.KnockbackDamping * dt);
        }

        private void OnDestroy()
        {
            if (ownsConfig) Destroy(movementConfig);
        }
    }
}
