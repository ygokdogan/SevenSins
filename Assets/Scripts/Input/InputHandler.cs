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
        }

        private void Update()
        {
            JumpBuffer();
        }

        private void JumpBuffer()
        {
            if (JumpInput && Time.time >= jumpInputStartTime + inputHoldTime)
            {
                JumpInput = false;
            }
        }

        public void UseJumpInput() => JumpInput = false;
        public void ConsumeJumpRelease() => JumpInputReleased = false;
        
        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions.Disable();
    }
}
