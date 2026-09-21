using System;
using UnityEngine;

namespace Input
{
    public class InputHandler : MonoBehaviour
    {
        private PlayerInputActions actions;

        [Tooltip("How long a jump press remains valid while waiting to land.")]
        [Min(0f)] public float inputHoldTime = 0.12f;
        
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintInput { get; private set; }
        
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


        private void Awake()
        {
            actions = new PlayerInputActions();
            
            actions.Player.Movement.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            actions.Player.Movement.canceled += ctx => MoveInput = Vector2.zero;

            actions.Player.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
            actions.Player.Look.canceled += ctx => LookInput = Vector2.zero;
            
            actions.Player.Sprint.performed += ctx => SprintInput = true;
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
        }

        private void Update()
        {
            JumpBuffer();
            SheatheBuffer();
            PrimaryAttackBuffer();
            BlockBuffer();
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

        public void UseSprintInput() => SprintInput = false;
        public void UseJumpInput() => JumpInput = false;
        public void UseSheatheInput() => SheatheInput = false;
        public void UsePrimaryAttackInput() => PrimaryAttackInput = false;
        public void UseBlockInput() => BlockInput = false;
        
        public void ConsumeJumpRelease() => JumpInputReleased = false;
        
        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions.Disable();
    }
}
