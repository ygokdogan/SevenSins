using Core;
using Input;
using Player.Combat;
using Player.Movement;
using Player.Movement.States;
using UnityEngine;

namespace Player
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(InputHandler), typeof(CharacterController))]
    public sealed class PlayerManager : MonoBehaviour
    {
        public PlayerMovement Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public InputHandler Input { get; private set; }
        public CharacterController Controller { get; private set; }
        public PlayerAnimationManager AnimationManager { get; private set; }
        public CameraManager Camera { get; private set; }
        public GrappleController GrappleController { get; private set; }

        public bool CanMove => Movement.CanMove;
        public bool CanSprint => Movement.CanSprint;
        public float SpeedMultiplier => Movement.SpeedMultiplier;
        
        [SerializeField] private Transform movementStatesRoot;
        [SerializeField] private Transform combatStatesRoot;
        
        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            Movement = GetComponent<PlayerMovement>();
            Combat = GetComponent<PlayerCombat>();
            Input = GetComponent<InputHandler>();
            Controller = GetComponent<CharacterController>();
            AnimationManager = GetComponentInChildren<PlayerAnimationManager>();
            Camera = GetComponent<CameraManager>();
            GrappleController = GetComponent<GrappleController>();
        }

        public T GetMovementState<T>() where T : PlayerMovementState =>
            movementStatesRoot != null ? movementStatesRoot.GetComponent<T>() : null;

        public T GetCombatState<T>() where T : PlayerCombatState =>
            combatStatesRoot != null ? combatStatesRoot.GetComponent<T>() : null;
    }
}
