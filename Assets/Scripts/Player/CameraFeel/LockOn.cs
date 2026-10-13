using System.Collections.Generic;
using Enemies.Core;
using UnityEngine;

namespace Player.CameraFeel
{
    public class LockOn : MonoBehaviour
    {
        [SerializeField] private Transform camPivot;
        [SerializeField, Min(0f)] private float range = 20f;
        [SerializeField, Range(0f, 89f)] private float maxAngle = 45f;
        [SerializeField, Min(0f)] private float turnSpeed = 12f;
        [SerializeField, Min(0f)] private float breakDistance = 25f;
        [SerializeField] private LayerMask enemyMask, obstacleMask;
        [SerializeField, Min(0f)] private float occlusionGraceTime = 0.35f;
        [SerializeField, Min(0f)] private float targetSwitchDelay = 0.3f;
        [SerializeField, Range(1f, 89f)] private float switchMaxAngle = 75f;

        public EnemyBase Target { get; private set; }
        public bool IsLocked => Target != null;

        private float switchCooldown;
        private float occludedTime;
        private bool switchArmed;
        private readonly HashSet<EnemyBase> candidates = new();
        
        public void Tick(float dt, float minPitch, float maxPitch)
        {
            if (!IsLocked || camPivot == null || dt <= 0f) return;
            if (!IsAvailable(Target))
            {
                SetTarget(FindBest(0));
                if (!IsLocked) return;
            }

            Vector3 dir = Target.lockPoint.position - camPivot.position;
            if (dir.magnitude > Mathf.Max(range, breakDistance)) { Release(); return; }
            occludedTime = HasLineOfSight(Target) ? 0f : occludedTime + dt;
            if (occludedTime > occlusionGraceTime) { Release(); return; }

            float blend = 1f - Mathf.Exp(-turnSpeed * dt);
            Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up);
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), blend);
            
            Vector3 localDir = camPivot.parent != null ? camPivot.parent.InverseTransformDirection(dir) : dir;
            float pitch = -Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;
            camPivot.localRotation = Quaternion.Slerp(camPivot.localRotation,
                Quaternion.Euler(Mathf.Clamp(pitch, minPitch, maxPitch), 0f, 0f), blend);
        }

        public void Toggle()
        {
            if (IsLocked) Release();
            else SetTarget(FindBest(0));
        }

        public void Release() => SetTarget(null);
        private void OnDisable() => Release();

        private void SetTarget(EnemyBase target)
        {
            Target = target;
            occludedTime = 0f;
            switchCooldown = Time.time + targetSwitchDelay;
            switchArmed = false;
        }

        public void HandleSwitchInput(float horizontal, bool gamepad)
        {
            float threshold = gamepad ? 0.8f : 15f;
            float releaseThreshold = gamepad ? 0.3f : 3f;
            if (Mathf.Abs(horizontal) <= releaseThreshold) switchArmed = true;
            if (!IsLocked || !switchArmed || Time.time < switchCooldown || Mathf.Abs(horizontal) < threshold) return;
            switchArmed = false;
            SwitchTarget(horizontal > 0f ? 1 : -1);
        }

        public void SwitchTarget(int dir)
        {
            if (!IsLocked || camPivot == null || dir == 0 || Time.time < switchCooldown) return;
            switchCooldown = Time.time + targetSwitchDelay;
            var next = FindBest(dir);
            if (next != null) SetTarget(next);
        }

        private EnemyBase FindBest(int side)
        {
            if (camPivot == null) return null;
            EnemyBase best = null;
            float bestScore = float.MaxValue;
            candidates.Clear();
            
            var hits = Physics.OverlapSphere(camPivot.position, range, enemyMask, QueryTriggerInteraction.Ignore);
            Vector3 reference = IsLocked ? Target.lockPoint.position - camPivot.position : camPivot.forward;
            float referenceYaw = Mathf.Atan2(Vector3.Dot(reference, camPivot.right), Vector3.Dot(reference, camPivot.forward)) * Mathf.Rad2Deg;
            
            foreach (var hit in hits)
            {
                var e = hit.GetComponentInParent<EnemyBase>();
                if (!IsAvailable(e) || e == Target || !candidates.Add(e)) continue;
                Vector3 to = e.lockPoint.position - camPivot.position;
                float distance = to.magnitude;
                float angle = Vector3.Angle(camPivot.forward, to);
                
                if (distance > range || angle > (side == 0 ? maxAngle : switchMaxAngle) || !HasLineOfSight(e)) continue;
                float score = angle + distance * 0.5f;
                if (side != 0)
                {
                    float yaw = Mathf.Atan2(Vector3.Dot(to, camPivot.right), Vector3.Dot(to, camPivot.forward)) * Mathf.Rad2Deg;
                    float delta = Mathf.DeltaAngle(referenceYaw, yaw);
                    if (delta * side <= 2f) continue;
                    score = Mathf.Abs(delta) + distance * 0.1f;
                }
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        private static bool IsAvailable(EnemyBase e) => e != null && e.isActiveAndEnabled && e.IsAlive;

        private bool HasLineOfSight(EnemyBase e)
        {
            Vector3 to = e.lockPoint.position - camPivot.position;
            foreach (var hit in Physics.RaycastAll(camPivot.position, to.normalized, to.magnitude,
                         obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(e.transform)) continue;
                return false;
            }
            return true;
        }
    }
}
