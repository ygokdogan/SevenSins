using System;
using Input;
using UnityEngine;

namespace Player
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Header("Look")]
        [SerializeField] private Transform cameraPitchPivot;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        
        [Header("FOV")]
        [SerializeField] private float defaultFov = 60f;
        [SerializeField] private float sprintFov = 80f;
        [SerializeField] private float fovInterpSpeed = 8f;
        
        [Header("Bob")]
        [SerializeField] private Transform cameraBobPivot;
        [SerializeField] private float defaultBobFrequency = 12f;
        [SerializeField] private float defaultBobAmplitude = 0.08f;
        [SerializeField] private float sprintBobFrequency = 16f;
        [SerializeField] private float sprintBobAmplitude = 0.14f;
        [SerializeField, Min(0f)] private float bobLateralAmplitude = 0.025f;
        [SerializeField, Min(0f)] private float bobSpeed = 14f;
        private Vector3 cameraBobDefaultPos;

        [Header("Landing Impact Shock")]
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 4f;
        [SerializeField, Min(0.01f)] private float maximumImpactSpeed = 9f;
        [SerializeField, Min(0f)] private float landingImpactStrength = 1.5f;
        [SerializeField, Min(1f)] private float landingStiffness = 160f;
        [SerializeField, Min(1f)] private float landingDamping = 18f;
        [SerializeField] private float landingPitchMultiplier = 15f;
        
        [Header("Tilt")]
        [SerializeField] private Transform cameraTiltPivot;
        [SerializeField] private float maxTilt = 2f;
        [SerializeField] private float tiltSpeed = 12f;

        private Camera cam;
        private InputHandler input;
        private PlayerMovement playerMovement;
        
        private float currentPitch;
        private float currentTilt;
        private float bobPhase;
        private float bobWeight;
        
        private float landingOffset;
        private float landingVelocity;
        
        private Vector3 currentBobOffset;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            cam = Camera.main;
            input = GetComponent<InputHandler>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        private void Start()
        {
            cameraBobDefaultPos = cameraBobPivot.localPosition;
            playerMovement.Landed += OnLanded;
        }

        private void OnDestroy()
        {
            if (playerMovement != null)
                playerMovement.Landed -= OnLanded;
        }

        private void Update()
        {
            CameraLook();
            CameraTilt();
            CameraBob();
            UpdateFov();
        }

        private void CameraLook()
        {
            Vector2 look = input.LookInput * sensitivity;
            
            transform.Rotate(Vector3.up * look.x);
            
            currentPitch = Mathf.Clamp(currentPitch - look.y, minPitch, maxPitch);
            cameraPitchPivot.localRotation = Quaternion.Euler(currentPitch, 0.0f, 0.0f);
        }

        private void CameraBob()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 planarVelocity = playerMovement.Velocity;
            planarVelocity.y = 0f;
            float currentSpeed = planarVelocity.magnitude;
            float maxSpeed = Mathf.Max(0.11f, playerMovement.CurrentMaxSpeed);
            float targetWeight = playerMovement.IsGrounded
                ? Mathf.InverseLerp(0.1f, maxSpeed, currentSpeed)
                : 0f;

            float targetFrequency = playerMovement.IsSprinting ? sprintBobFrequency : defaultBobFrequency;
            float targetAmplitude = playerMovement.IsSprinting ? sprintBobAmplitude : defaultBobAmplitude;
            
            bobWeight = Mathf.MoveTowards(bobWeight, targetWeight, bobSpeed * dt);
            bobPhase += targetFrequency * bobWeight * dt;

            Vector3 targetOffset = new Vector3(
                Mathf.Cos(bobPhase * 0.5f) * bobLateralAmplitude * bobWeight,
                Mathf.Sin(bobPhase) * targetAmplitude * bobWeight,
                0f);
            float smoothFactor = 1f - Mathf.Exp(-bobSpeed * dt);
            currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, smoothFactor);

            UpdateLandingSpring(dt);

            cameraBobPivot.localPosition = cameraBobDefaultPos + currentBobOffset + Vector3.up * landingOffset;

            float pitchTilt = -landingOffset * landingPitchMultiplier;
            cameraBobPivot.localRotation = Quaternion.Euler(pitchTilt, 0f, 0f);
        }

        private void OnLanded(float landingSpeed)
        {
            float impactStrength = Mathf.InverseLerp(
                minimumImpactSpeed,
                maximumImpactSpeed,
                landingSpeed);

            if (impactStrength > 0f)
            {
                landingVelocity -= impactStrength * landingImpactStrength;
            }
        }

        private void UpdateLandingSpring(float dt)
        {
            float springForce = -landingStiffness * landingOffset - landingDamping * landingVelocity;
            landingVelocity += springForce * dt;
            landingOffset += landingVelocity * dt;

            landingOffset = Mathf.Clamp(landingOffset, -0.1f, 0.03f);
        }

        private void CameraTilt()
        {
            float targetTilt = -input.MoveInput.x * maxTilt;
            currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);
            cameraTiltPivot.localRotation = Quaternion.Euler(0.0f, 0.0f, currentTilt);
        }

        private void UpdateFov()
        {
            float targetFov = playerMovement.IsSprinting ? sprintFov : defaultFov;
            cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, targetFov, fovInterpSpeed * Time.deltaTime);
        }
    }
}
