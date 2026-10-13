using System;
using UnityEngine;

namespace Player.CameraFeel
{
    public class CameraManager : MonoBehaviour
    {
        [Header("Camera Settings")]
        [Header("Mouse Look")]
        [SerializeField] private Transform cameraPitchPivot;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Gamepad Look")]
        [SerializeField] private float stickSensitivity = 180f;
        [SerializeField] private float deadzone = 0.15f;
        [SerializeField] private float curvePower = 2f;

        [Header("Impact Pivot")]
        [SerializeField] private Transform cameraImpactPivot;
        private Vector3 cameraImpactDefaultPos;

        [Header("Landing Impact Shock")]
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 4f;
        [SerializeField, Min(0.01f)] private float maximumImpactSpeed = 9f;
        [SerializeField, Min(0f)] private float landingImpactStrength = 1.5f;
        [SerializeField, Min(1f)] private float landingStiffness = 160f;
        [SerializeField, Min(1f)] private float landingDamping = 18f;
        [SerializeField] private float landingPitchMultiplier = 15f;

        [Header("Tilt")]
        [SerializeField] private Transform cameraTiltPivot;

        [Header("Movement Bob")]
        [SerializeField] private Transform cameraBobPivot;
        [SerializeField] private Vector2 cameraBobAmplitude = new(0.003f, 0.006f);
        [SerializeField, Min(0f)] private float cameraBobFrequency = 12f;
        [SerializeField, Min(0f)] private float cameraBobSmoothing = 12f;


        private PlayerManager player;
        private Vector3 cameraBobRestPosition;
        private Vector3 previousPosition;
        private Vector3 cameraBobOffset;
        private float cameraBobPhase;

        private Camera cam;
        private LockOn lockOn;
        private float currentPitch;
        private float currentTilt;
        private float targetFov;
        private float fovSpeed;
        private float targetTilt;
        private float tiltSpeed;

        public float Sensitivity => sensitivity;

        private float landingOffset;
        private float landingVelocity;

        private float shakePitch;
        private float shakeRoll;
        private float shakeVelocity;
        private float shakeEndTime;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            cam = Camera.main;
            targetFov = cam.fieldOfView;

            if (cameraImpactPivot != null) cameraImpactDefaultPos = cameraImpactPivot.localPosition;
            player = GetComponent<PlayerManager>();

            if (cameraBobPivot != null) cameraBobRestPosition = cameraBobPivot.localPosition;
            previousPosition = transform.position;

            lockOn = GetComponent<LockOn>();
        }

        private void OnEnable() => previousPosition = transform.position;

        private void OnDisable()
        {
            ReleaseLockOn();
            if (cameraBobPivot != null) cameraBobPivot.localPosition = cameraBobRestPosition;
            cameraBobOffset = Vector3.zero;
            cameraBobPhase = 0f;
        }

        private void Update()
        {
            if (player.Input.LockOnInput)
            {
                if (lockOn != null && !player.Movement.IsWallRunning) lockOn.Toggle();
                player.Input.UseLockOnInput();
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, tiltSpeed * dt);
            cameraTiltPivot.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
            cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, targetFov, fovSpeed * dt);
            UpdateCameraImpact();
            UpdateMovementBob(dt);
            if (lockOn != null && lockOn.isActiveAndEnabled)
            {
                if (player.Movement.IsWallRunning) lockOn.Release();
                else lockOn.Tick(dt, minPitch, maxPitch);
            }
        }

        private void UpdateMovementBob(float dt)
        {
            Vector3 displacement = transform.position - previousPosition;
            previousPosition = transform.position;
            if (dt <= 0f || cameraBobPivot == null || player == null || player.Movement == null) return;

            float speed = Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude / dt;
            float weight = (player.Movement.IsGrounded || player.Movement.IsWallRunning)
                ? Mathf.Clamp01(speed / Mathf.Max(0.1f, player.Movement.CurrentMaxSpeed)) : 0f;
            cameraBobPhase = (cameraBobPhase + cameraBobFrequency * weight * dt) % (Mathf.PI * 4f);
            Vector3 target = new Vector3(
                Mathf.Sin(cameraBobPhase * 0.5f) * cameraBobAmplitude.x,
                Mathf.Sin(cameraBobPhase) * cameraBobAmplitude.y, 0f) * weight;
            cameraBobOffset = Vector3.Lerp(cameraBobOffset, target, 1f - Mathf.Exp(-cameraBobSmoothing * dt));
            cameraBobPivot.localPosition = cameraBobRestPosition + cameraBobOffset;
        }

        public void Look(Vector2 input)
        {
            if (lockOn != null && lockOn.IsLocked)
            {
                lockOn.HandleSwitchInput(input.x, player.Input.IsGamepad);
                return;
            }

            Vector2 look;
            if (player.Input.IsGamepad)
            {
                float mag = input.magnitude;
                if (mag < deadzone) return;

                float scaled = Mathf.InverseLerp(deadzone, 1f, mag);
                float curved = Mathf.Pow(scaled, curvePower);
                Vector2 dir = input / mag;

                look = dir * curved * stickSensitivity * Time.deltaTime;
            }
            else
            {
                look = input * sensitivity;
            }

            LookPitch(look.y);
            transform.Rotate(Vector3.up * look.x);
        }

        public void LookPitch(float degrees)
        {
            currentPitch = Mathf.DeltaAngle(0f, cameraPitchPivot.localEulerAngles.x);
            currentPitch = Mathf.Clamp(currentPitch - degrees, minPitch, maxPitch);
            cameraPitchPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        public void ReleaseLockOn()
        {
            if (lockOn != null) lockOn.Release();
        }

        public void SetYaw(float degrees) => transform.rotation = Quaternion.Euler(0f, degrees, 0f);

        public void SetFov(float target, float speed)
        {
            targetFov = Mathf.Clamp(target, 1f, 179f);
            fovSpeed = Mathf.Max(0f, speed);
        }

        public void TiltCamera(float target, float speed)
        {
            targetTilt = target;
            tiltSpeed = Mathf.Max(0f, speed);
        }

        private void UpdateCameraImpact()
        {
            if (cameraImpactPivot == null) return;
            float dt = Time.deltaTime;
            float unscaledDt = Time.unscaledDeltaTime;
            UpdateHitShake(unscaledDt);

            if (dt > 0f)
            {
                UpdateLandingSpring(dt);

                cameraImpactPivot.localPosition = cameraImpactDefaultPos + Vector3.up * landingOffset;
            }

            float pitchTilt = -landingOffset * landingPitchMultiplier;
            cameraImpactPivot.localRotation = Quaternion.Euler(pitchTilt + shakePitch, 0f, shakeRoll);
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

        public void LandingImpact(float landingSpeed)
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

    }
}
