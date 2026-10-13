using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public class InputHandler : MonoBehaviour
    {
        private PlayerInputActions actions;

        public bool IsGamepad { get; private set; }

        [Tooltip("How long a jump press remains valid while waiting to land.")]
        [Min(0f)] public float inputHoldTime = 0.12f;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }

        public bool LockOnInput { get; private set; }
        private float lockOnInputStartTime = -2f;
        public bool SprintInput { get; private set; }
        public bool DashInput { get; private set; }
        private float dashInputStartTime = -2f;

        public bool JumpInput { get; private set; }
        public bool JumpInputReleased { get; private set; }
        public bool IsJumpHeld { get; private set; }
        private float jumpInputStartTime = -2f;

        public bool SheatheInput { get; private set; }
        private float sheatheInputStartTime = -2f;

        public bool PrimaryAttackInput { get; private set; }
        private float primaryAttackInputStartTime = -2f;

        public bool BlockInput { get; private set; }
        public bool IsBlockHeld { get; private set; }
        private float blockInputStartTime = -2f;

        public bool GrappleInput { get; private set; }
        public bool IsGrappleHeld { get; private set; }
        private float grappleInputStartTime = -2f;


        private void Awake()
        {
            actions = new PlayerInputActions();

            actions.Player.Movement.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            actions.Player.Movement.canceled += ctx => MoveInput = Vector2.zero;

            actions.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
            actions.Player.Look.canceled += ctx => LookInput = Vector2.zero;

            actions.Player.LockOn.performed += ctx =>
            {
                LockOnInput = true;
                lockOnInputStartTime = Time.time;
            };

            actions.Player.Sprint.performed += ctx =>
            {
                SprintInput = true;
                DashInput = true;
                dashInputStartTime = Time.time;
            };
            actions.Player.Sprint.canceled += ctx => SprintInput = false;

            actions.Player.Jump.performed += ctx =>
            {
                JumpInput = true;
                IsJumpHeld = true;
                jumpInputStartTime = Time.time;
                JumpInputReleased = false;
            };
            actions.Player.Jump.canceled += ctx =>
            {
                IsJumpHeld = false;
                JumpInputReleased = true;
            };

            actions.Player.Sheathe.performed += ctx =>
            {
                SheatheInput = true;
                sheatheInputStartTime = Time.time;
            };

            actions.Player.PrimaryAttack.performed += ctx =>
            {
                PrimaryAttackInput = true;
                primaryAttackInputStartTime = Time.time;
            };

            actions.Player.Block.performed += ctx =>
            {
                BlockInput = true;
                IsBlockHeld = true;
                blockInputStartTime = Time.time;
            };
            actions.Player.Block.canceled += ctx =>
            {
                IsBlockHeld = false;
            };

            actions.Player.Grapple.performed += ctx =>
            {
                GrappleInput = true;
                IsGrappleHeld = true;
                grappleInputStartTime = Time.time;
            };
            actions.Player.Grapple.canceled += ctx => IsGrappleHeld = false;
        }

        private void Update()
        {
            if (LockOnInput && Time.time >= lockOnInputStartTime + inputHoldTime)
                LockOnInput = false;
            JumpBuffer();
            SheatheBuffer();
            PrimaryAttackBuffer();
            BlockBuffer();
            GrappleBuffer();
            DashBuffer();
        }

        private void JumpBuffer()
        {
            if (JumpInput && Time.time >= jumpInputStartTime + inputHoldTime)
            {
                JumpInput = false;
            }
        }

        private void SheatheBuffer()
        {
            if (SheatheInput && Time.time >= sheatheInputStartTime + inputHoldTime)
            {
                SheatheInput = false;
            }
        }

        private void PrimaryAttackBuffer()
        {
            if (PrimaryAttackInput && Time.time >= primaryAttackInputStartTime + inputHoldTime)
            {
                PrimaryAttackInput = false;
            }
        }

        private void BlockBuffer()
        {
            if (BlockInput && Time.time >= blockInputStartTime + inputHoldTime)
            {
                BlockInput = false;
            }
        }

        private void GrappleBuffer()
        {
            if (GrappleInput && Time.time >= grappleInputStartTime + inputHoldTime)
            {
                GrappleInput = false;
            }
        }

        private void DashBuffer()
        {
            if (DashInput && Time.time >= dashInputStartTime + inputHoldTime)
                DashInput = false;
        }

        public void UseLockOnInput() => LockOnInput = false;
        public void UseSprintInput() => SprintInput = false;
        public void UseDashInput() => DashInput = false;
        public void UseJumpInput() => JumpInput = false;
        public void UseSheatheInput() => SheatheInput = false;
        public void UsePrimaryAttackInput() => PrimaryAttackInput = false;
        public void UseBlockInput() => BlockInput = false;
        public void UseGrappleInput() => GrappleInput = false;

        public void ConsumeJumpRelease() => JumpInputReleased = false;

        private void OnEnable()
        {
            actions.Enable();
            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDisable()
        {
            actions.Disable();
            LockOnInput = false;
            InputSystem.onActionChange -= OnActionChange;
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;

            var action = (InputAction)obj;
            if (action.actionMap != actions.Player.Get()) return;

            IsGamepad = action.activeControl.device is Gamepad;
        }
    }
}
