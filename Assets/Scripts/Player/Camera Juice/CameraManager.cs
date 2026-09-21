using System;
using Input;
using Player.Movement;
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
        [SerializeField, Range(0f, 180f)] private float wallRunYawLimit = 55f;
        [SerializeField, Range(0f, 45f)] private float wallRunTowardWallYawLimit = 10f;
        
        [Header("FOV")]
        [SerializeField] private float defaultFov = 60f;
        [SerializeField] private float sprintFov = 80f;
        [SerializeField] private float wallRunFov = 74f;
        [SerializeField] private float fovInterpSpeed = 8f;
        
        [Header("Bob")]
        [SerializeField] private Transform cameraBobPivot;
        [SerializeField] private float defaultBobFrequency = 12f;
        [SerializeField] private float defaultBobAmplitude = 0.08f;
        [SerializeField] private float sprintBobFrequency = 16f;
        [SerializeField] private float sprintBobAmplitude = 0.14f;
        [SerializeField] private float wallRunBobFrequency = 17f;
        [SerializeField] private float wallRunBobAmplitude = 0.065f;
        [SerializeField] private float wallRunLateralAmplitude = 0.045f;
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
        [SerializeField] private float wallRunTilt = 11f;
        [SerializeField] private float tiltSpeed = 12f;
        [SerializeField] private float wallRunTiltSpeed = 12f;

        private Camera cam;
        private InputHandler input;
        private PlayerMovement playerMovement;
        
        private float currentPitch;
        private float currentTilt;
        private float wallRunStartYaw;
        private float wallRunWallSide;
        private bool wasWallRunning;
        private float bobPhase;
        private float bobWeight;
        
        private float landingOffset;
        private float landingVelocity;
        
        private Vector3 currentBobOffset;
        private float shakePitch;
        private float shakeRoll;
        private float shakeVelocity;
        private float shakeEndTime;

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

            if (playerMovement.IsWallRunning)
            {
                if (!wasWallRunning)
                {
                    wallRunStartYaw = transform.eulerAngles.y;
                    wallRunWallSide = Mathf.Sign(Vector3.Dot(-playerMovement.WallNormal, transform.right));
                    wasWallRunning = true;
                }

                float currentYawOffset = Mathf.DeltaAngle(wallRunStartYaw, transform.eulerAngles.y);
                float minYaw = wallRunWallSide < 0f ? -wallRunTowardWallYawLimit : -wallRunYawLimit;
                float maxYaw = wallRunWallSide > 0f ? wallRunTowardWallYawLimit : wallRunYawLimit;
                float clampedYawOffset = Mathf.Clamp(
                    currentYawOffset + look.x,
                    minYaw,
                    maxYaw);
                transform.rotation = Quaternion.Euler(0f, wallRunStartYaw + clampedYawOffset, 0f);
                return;
            }

            wasWallRunning = false;
            
            transform.Rotate(Vector3.up * look.x);
            
            currentPitch = Mathf.Clamp(currentPitch - look.y, minPitch, maxPitch);
            cameraPitchPivot.localRotation = Quaternion.Euler(currentPitch, 0.0f, 0.0f);
        }

        private void CameraBob()
        {
            float dt = Time.deltaTime;
            float unscaledDt = Time.unscaledDeltaTime;
            UpdateHitShake(unscaledDt);

            if (dt > 0f)
            {
                Vector3 planarVelocity = playerMovement.Velocity;
                planarVelocity.y = 0f;
                float currentSpeed = planarVelocity.magnitude;
                float maxSpeed = Mathf.Max(0.11f, playerMovement.CurrentMaxSpeed);
                bool isWallRunning = playerMovement.IsWallRunning;
                float targetWeight = playerMovement.IsGrounded
                    ? Mathf.InverseLerp(0.1f, maxSpeed, currentSpeed)
                    : isWallRunning ? Mathf.InverseLerp(0.1f, maxSpeed, currentSpeed) : 0f;

                float targetFrequency = isWallRunning
                    ? wallRunBobFrequency
                    : playerMovement.IsSprinting ? sprintBobFrequency : defaultBobFrequency;
                float targetAmplitude = isWallRunning
                    ? wallRunBobAmplitude
                    : playerMovement.IsSprinting ? sprintBobAmplitude : defaultBobAmplitude;
                float targetLateralAmplitude = isWallRunning ? wallRunLateralAmplitude : bobLateralAmplitude;
                targetFrequency *= playerMovement.SpeedMultiplier;
                targetAmplitude *= playerMovement.SpeedMultiplier;
                
                bobWeight = Mathf.MoveTowards(bobWeight, targetWeight, bobSpeed * dt);
                bobPhase += targetFrequency * bobWeight * dt;

                Vector3 targetOffset = new Vector3(
                    Mathf.Cos(bobPhase * 0.5f) * targetLateralAmplitude * bobWeight,
                    Mathf.Sin(bobPhase) * targetAmplitude * bobWeight,
                    0f);
                float smoothFactor = 1f - Mathf.Exp(-bobSpeed * dt);
                currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, smoothFactor);

                UpdateLandingSpring(dt);

                cameraBobPivot.localPosition = cameraBobDefaultPos + currentBobOffset + Vector3.up * landingOffset;
            }

            float pitchTilt = -landingOffset * landingPitchMultiplier;
            cameraBobPivot.localRotation = Quaternion.Euler(pitchTilt + shakePitch, 0f, shakeRoll);
        }

        public void Shake(float strength, float duration)
        {
            if (strength <= 0f) return;

            shakeVelocity += strength * 130f;
            shakeEndTime = Mathf.Max(shakeEndTime, Time.unscaledTime + duration);
        }

        private void UpdateHitShake(float unscaledDt)
        {
            if (Time.unscaledTime < shakeEndTime)
            {
                shakePitch = Mathf.Sin(Time.unscaledTime * 95f) * shakeVelocity * 0.018f;
                shakeRoll = Mathf.Cos(Time.unscaledTime * 83f) * shakeVelocity * 0.012f;
            }
            else
            {
                shakePitch = Mathf.MoveTowards(shakePitch, 0f, 100f * unscaledDt);
                shakeRoll = Mathf.MoveTowards(shakeRoll, 0f, 100f * unscaledDt);
            }

            shakeVelocity = Mathf.MoveTowards(shakeVelocity, 0f, 550f * unscaledDt);
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
            float targetTilt = playerMovement.IsWallRunning
                ? Vector3.Dot(playerMovement.WallNormal, transform.right) * -wallRunTilt
                : -input.MoveInput.x * maxTilt;
            float speed = playerMovement.IsWallRunning ? wallRunTiltSpeed : tiltSpeed;
            currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, speed * Time.deltaTime);
            cameraTiltPivot.localRotation = Quaternion.Euler(0.0f, 0.0f, currentTilt);
        }

        private void UpdateFov()
        {
            float targetFov = playerMovement.IsWallRunning
                ? wallRunFov
                : playerMovement.IsSprinting ? sprintFov : defaultFov;
            cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, targetFov, fovInterpSpeed * Time.deltaTime);
        }
    }
}
