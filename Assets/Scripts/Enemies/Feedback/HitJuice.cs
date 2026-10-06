using System;
using Combat;
using UnityEngine;

namespace Enemies.Combat.Utilities
{
    public class HitJuice : MonoBehaviour
    {
        [Header("Spring Wobble Settings")]
        [SerializeField] private float wobbleImpulse = 45f;
        [SerializeField] private float wobbleStiffness = 160f;
        [SerializeField] private float wobbleDamping = 12f;

        [Header("Positional Recoil Settings")]
        [SerializeField] private float recoilImpulse = 1.8f;
        [SerializeField] private float recoilStiffness = 160f;
        [SerializeField] private float recoilDamping = 14f;

        [Header("Squash & Stretch Settings")]
        [SerializeField] private float squashAmount = 0.18f;
        [SerializeField] private float scaleStiffness = 220f;
        [SerializeField] private float scaleDamping = 16f;
        
        private Vector3 angularDisplacement;
        private Vector3 angularVelocity;

        private Vector3 posDisplacement;
        private Vector3 posVelocity;

        private Vector3 scaleDisplacement;
        private Vector3 scaleVelocity;
        
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Vector3 baseScale;
        
        private void Start()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            baseScale = transform.localScale;
        }
        
        private void Update()
        {
            UpdateSprings(Time.deltaTime);
        }

        public void CalculateHitReaction(HitInfo hitInfo)
        {
            float damageScale = Mathf.Clamp(hitInfo.damage / 15f, 0.5f, 2.5f);
            float impactMultiplier = Mathf.Max(0f, hitInfo.impactMultiplier);
            float impactScale = damageScale * impactMultiplier;
            
            Vector3 hitDir = hitInfo.hitDirection;
            hitDir.y = 0f;
            if (hitDir.sqrMagnitude < 0.001f)
                hitDir = -transform.forward;
            else
                hitDir.Normalize();
            
            // The hit direction is world-space, while these springs animate
            // localPosition and localRotation beneath the enemy root.
            Vector3 localHitDir = transform.parent != null
                ? transform.parent.InverseTransformDirection(hitDir)
                : hitDir;
            localHitDir.y = 0f;
            localHitDir.Normalize();

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, localHitDir).normalized;
            angularVelocity += tiltAxis * (wobbleImpulse * impactScale);
            
            posVelocity += localHitDir * (recoilImpulse * impactScale);
            
            scaleDisplacement = new Vector3(squashAmount * 0.5f, -squashAmount, squashAmount * 0.5f) * (1 + impactScale * 0.1f);
            scaleVelocity = Vector3.zero;
        }

        private void UpdateSprings(float dt)
        {
            Vector3 springTorque = -wobbleStiffness * angularDisplacement - wobbleDamping * angularVelocity;
            angularVelocity += springTorque * dt;
            angularDisplacement += angularVelocity * dt;
            
            Vector3 springForce = -recoilStiffness * posDisplacement - recoilDamping * posVelocity;
            posVelocity += springForce * dt;
            posDisplacement += posVelocity * dt;

            Vector3 scaleForce = -scaleStiffness * scaleDisplacement - scaleDamping * scaleVelocity;
            scaleVelocity += scaleForce * dt;
            scaleDisplacement += scaleVelocity * dt;
            
            transform.localPosition = basePosition + posDisplacement;
            transform.localRotation = baseRotation * Quaternion.Euler(angularDisplacement);
            transform.localScale = baseScale + scaleDisplacement;
        }
    }
}
