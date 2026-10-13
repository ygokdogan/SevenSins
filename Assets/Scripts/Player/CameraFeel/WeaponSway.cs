using Input;
using Player.Movement;
using UnityEngine;

namespace Player.CameraFeel
{
    [DisallowMultipleComponent]
    public class WeaponSway : MonoBehaviour
    {
        [Header("Position Sway (Look)")]
        public float swayAmount = 0.02f;
        public float maxSwayAmount = 0.06f;
        public float swaySmoothness = 10f;

        [Header("Rotation Sway (Look)")]
        public float rotationSwayAmount = 2.5f;
        public float maxRotationSwayAmount = 8f;
        public float rotationSmoothness = 12f;

        [Header("Movement / Strafe Sway (WASD)")]
        public float moveSwayAmount = 0.025f;
        public float moveTiltAmount = 3.5f;

        [Header("Jump & Airborne Sway")]
        public float jumpPosSwayAmount = 0.05f;
        public float jumpRotSwayAmount = 5.5f;

        private InputHandler input;
        private PlayerMovement playerMovement;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        
        private Vector2 smoothedLook;

        private Vector3 currentPosOffset;
        private Vector3 currentRotOffset;

        private void Start()
        {
            input = GetComponentInParent<InputHandler>();
            playerMovement = GetComponentInParent<PlayerMovement>();

            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
        }

        private void LateUpdate()
        {
            CalculateAndApplySway();
        }

        private void CalculateAndApplySway()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 rawLook = input.LookInput;
            smoothedLook = Vector2.Lerp(smoothedLook, rawLook, 1f - Mathf.Exp(-25f * dt));

            float lookPosX = Mathf.Clamp(-smoothedLook.x * swayAmount, -maxSwayAmount, maxSwayAmount);
            float lookPosY = Mathf.Clamp(-smoothedLook.y * swayAmount, -maxSwayAmount, maxSwayAmount);
            Vector3 lookPosSway = new Vector3(lookPosX, lookPosY, 0f);

            float lookRotX = Mathf.Clamp(smoothedLook.y * rotationSwayAmount, -maxRotationSwayAmount, maxRotationSwayAmount);
            float lookRotY = Mathf.Clamp(-smoothedLook.x * rotationSwayAmount, -maxRotationSwayAmount, maxRotationSwayAmount);
            float lookRotZ = Mathf.Clamp(-smoothedLook.x * (rotationSwayAmount * 0.6f), -maxRotationSwayAmount, maxRotationSwayAmount);
            Vector3 lookRotSway = new Vector3(lookRotX, lookRotY, lookRotZ);

            Vector2 move = input.MoveInput;
            Vector3 movePosSway = new Vector3(-move.x * moveSwayAmount, 0f, -move.y * moveSwayAmount);
            Vector3 moveRotSway = new Vector3(
                move.y * (moveTiltAmount * 0.5f),
                -move.x * (moveTiltAmount * 0.4f),
                -move.x * moveTiltAmount
            );

            Vector3 airPosSway = Vector3.zero;
            Vector3 airRotSway = Vector3.zero;
            if (playerMovement != null && !playerMovement.IsGrounded)
            {
                float verticalVel = Mathf.Clamp(playerMovement.VerticalVelocity / 8f, -1.2f, 1.2f);
                airPosSway.y = -verticalVel * jumpPosSwayAmount;
                airRotSway.x = -verticalVel * jumpRotSwayAmount;
            }

            Vector3 targetPosOffset = lookPosSway + movePosSway + airPosSway;
            Vector3 targetRotOffset = lookRotSway + moveRotSway + airRotSway;

            float posFactor = 1f - Mathf.Exp(-swaySmoothness * dt);
            float rotFactor = 1f - Mathf.Exp(-rotationSmoothness * dt);

            currentPosOffset = Vector3.Lerp(currentPosOffset, targetPosOffset, posFactor);
            currentRotOffset = Vector3.Lerp(currentRotOffset, targetRotOffset, rotFactor);
            
            transform.localPosition = initialLocalPos + currentPosOffset;
            transform.localRotation = initialLocalRot * Quaternion.Euler(currentRotOffset);
        }

        [ContextMenu("Reset Initial Transform")]
        public void ResetInitialTransform()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
        }
    }
}
