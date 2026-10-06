using UnityEngine;

namespace Player.Camera_Juice
{
    [DisallowMultipleComponent]
    public sealed class WeaponBob : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField, Min(0f)] private float walkFrequency = 12f;
        [SerializeField, Min(0f)] private float sprintFrequency = 16f;
        [SerializeField] private Vector3 walkAmplitude = new(0.015f, 0.02f, 0.008f);
        [SerializeField] private Vector3 sprintAmplitude = new(0.025f, 0.035f, 0.015f);
        [SerializeField, Min(0f)] private float rollAmount = 1.2f;
        [SerializeField, Min(0.1f)] private float smoothing = 12f;
        [SerializeField, Range(0f, 1f)] private float combatWeight = 0.35f;

        [Header("Idle Breathing")]
        [SerializeField, Min(0f)] private float idleFrequency = 1.6f;
        [SerializeField] private Vector3 idleAmplitude = new(0.0015f, 0.0025f, 0.001f);
        [SerializeField, Min(0f)] private float idleRollAmount = 0.15f;

        [Header("Landing")]
        [SerializeField, Min(0f)] private float landingDepth = 0.035f;
        [SerializeField, Min(0.01f)] private float landingDuration = 0.22f;

        [Header("Blocked Strike")]
        [SerializeField, Min(0.01f)] private float blockedRecoilDuration = 0.2f;
        [SerializeField, Min(0f)] private float blockedRecoilDistance = 0.075f;
        [SerializeField] private float blockedRecoilPitch = -9f;
        private float blockedRecoilTime;
        public void PlayBlockedRecoil() => blockedRecoilTime = blockedRecoilDuration;

        private PlayerManager player;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 offset;
        private float roll;
        private float phase;
        private float idlePhase;
        private float landingTime;
        private float landingStrength;
        private Vector3 previousPlayerPosition;

        private void Awake()
        {
            player = GetComponentInParent<PlayerManager>();
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            if (player != null) previousPlayerPosition = player.transform.position;
            if (player != null && player.Movement != null)
                player.Movement.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (player != null && player.Movement != null)
                player.Movement.Landed -= OnLanded;
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
            offset = Vector3.zero;
            blockedRecoilTime = 0f;
            phase = idlePhase = roll = landingTime = 0f;
        }

        private void OnLanded(float speed)
        {
            landingStrength = Mathf.InverseLerp(3f, 12f, speed);
            landingTime = landingStrength > 0f ? landingDuration : 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (player == null || player.Movement == null) return;
            Vector3 displacement = player.transform.position - previousPlayerPosition;
            previousPlayerPosition = player.transform.position;
            if (dt <= 0f) return;
            // Includes locomotion, attack lunges and knockback across all Move calls this frame.
            float speed = Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude / dt;
            UpdateBob(dt, speed);
        }

        private void UpdateBob(float dt, float speed)
        {
            var movement = player.Movement;
            float movementWeight = movement.IsGrounded
                ? Mathf.Clamp01(speed / Mathf.Max(0.1f, movement.CurrentMaxSpeed)) : 0f;
            float weight = movementWeight;
            float idleWeight = movement.IsGrounded
                ? 1f - Mathf.Clamp01(speed / 0.5f) : 0f;
            if (player.AnimationManager != null && player.AnimationManager.IsPlayingAnimation)
            {
                weight *= combatWeight;
                idleWeight *= combatWeight;
            }

            idlePhase = (idlePhase + idleFrequency * dt) % (Mathf.PI * 4f);

            phase = (phase + (movement.IsSprinting ? sprintFrequency : walkFrequency) * movementWeight * dt)
                    % (Mathf.PI * 4f);
            Vector3 amplitude = movement.IsSprinting ? sprintAmplitude : walkAmplitude;
            Vector3 target = Vector3.Scale(amplitude, new Vector3(
                Mathf.Sin(phase * 0.5f), Mathf.Sin(phase), Mathf.Cos(phase))) * weight;
            target += Vector3.Scale(idleAmplitude, new Vector3(
                Mathf.Sin(idlePhase * 0.5f), Mathf.Sin(idlePhase), Mathf.Sin(idlePhase))) * idleWeight;

            if (landingTime > 0f)
            {
                landingTime = Mathf.Max(0f, landingTime - dt);
                float progress = 1f - landingTime / Mathf.Max(0.01f, landingDuration);
                target.y -= Mathf.Sin(progress * Mathf.PI) * landingDepth * landingStrength;
            }

            float blend = 1f - Mathf.Exp(-smoothing * dt);
            offset = Vector3.Lerp(offset, target, blend);
            float targetRoll = Mathf.Sin(phase * 0.5f) * rollAmount * weight
                + Mathf.Sin(idlePhase * 0.5f) * idleRollAmount * idleWeight;
            roll = Mathf.Lerp(roll, targetRoll, blend);
            float recoil = 0f;
            if (blockedRecoilTime > 0f)
            {
                blockedRecoilTime = Mathf.Max(0f, blockedRecoilTime - Time.unscaledDeltaTime);
                float t = 1f - blockedRecoilTime / blockedRecoilDuration;
                recoil = t < 0.2f ? t / 0.2f : Mathf.Pow((1f - t) / 0.8f, 2f);
            }
            transform.localPosition = restPosition + offset + Vector3.back * (recoil * blockedRecoilDistance);
            transform.localRotation = restRotation * Quaternion.Euler(recoil * blockedRecoilPitch, 0f, roll);
        }
    }
}
