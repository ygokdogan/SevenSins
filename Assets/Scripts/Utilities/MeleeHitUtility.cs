using System;
using System.Collections.Generic;
using Combat;
using Player.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utilities
{
    public static class MeleeHitUtility
    {
        static readonly Collider[] buffer = new Collider[32];
        
        public static HitInfo ToHitInfo(this AttackData a) => a == null
            ? new HitInfo { damage = 25f, poiseDamage = 10f, impactMultiplier = 1f, knockbackForce = 6f }
            : new HitInfo
            {
                defense = a.defense,
                damage = a.damage,
                poiseDamage = a.poiseDamage,
                impactMultiplier = a.impactMultiplier,
                knockbackForce = a.knockbackForce
            };

        public static int Hit(BoxCollider box, LayerMask mask, HitInfo template, GameObject attacker,
            HashSet<IDamageable> alreadyHit, Func<HitInfo, bool> onHit = null)
        {
            if (box == null) return 0;
            Transform t = box.transform;
            return CheckHits(t.TransformPoint(box.center),
                Vector3.Scale(box.size, t.lossyScale) * 0.5f,
                t.rotation, mask, template, attacker, alreadyHit, onHit);
        }
        
        private static int CheckHits(Vector3 center, Vector3 halfExtents, Quaternion rotation, LayerMask mask,
            HitInfo template, GameObject attacker, HashSet<IDamageable> alreadyHit,
            Func<HitInfo, bool> onHit = null)
        {
            Collider[] cols = Physics.OverlapBox(center, halfExtents, rotation, mask);
            Vector3 origin = attacker.transform.position;
            int count = 0;

            foreach (var col in cols)
            {
                if (col.transform.IsChildOf(attacker.transform)) continue;
                
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null) continue;
                if (alreadyHit != null && !alreadyHit.Add(target)) continue;

                Vector3 point = HitPointUtility.OnTargetSurface(col, origin, out Vector3 normal);
                Vector3 dir = col.transform.position - origin;
                dir.y = 0f;
                dir = dir.sqrMagnitude > 0.001f ? dir.normalized : attacker.transform.forward;

                HitInfo info = template;
                info.hitPoint = point;
                info.hitNormal = normal;
                info.hitDirection = dir;
                info.attacker = attacker;

                target.TakeHit(info);
                count++;

                if (onHit != null && !onHit(info)) break;
            }
            return count;
        }
        
        public static void SpawnVfx(GameObject prefab, HitInfo info, bool unscaledTime = false)
        {
            if (prefab == null) return;
            
            var go = Object.Instantiate(prefab, info.hitPoint + info.hitNormal * 0.08f,
                Quaternion.LookRotation(info.hitNormal));
            if (!unscaledTime) return;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }

        public static float ForwardReach(BoxCollider box, Transform origin)
        {
            if (box == null) return 0;
            Transform t = box.transform;
            Vector3 c = t.TransformPoint(box.center);
            Vector3 he = Vector3.Scale(box.size, t.lossyScale) * 0.5f;
            
            Vector3 f = origin.forward;
            float extent =
                Mathf.Abs(Vector3.Dot(t.right, f)) * he.x +
                Mathf.Abs(Vector3.Dot(t.up, f)) * he.y +
                Mathf.Abs(Vector3.Dot(t.forward, f)) * he.z;
            
            return Vector3.Dot(c - origin.position, f) + extent;
        }

        public static bool IsOverlapping(BoxCollider box, Collider target)
        {
            if (box == null || target == null) return false;
            
            return Physics.ComputePenetration(box, box.transform.position, box.transform.rotation,
                target, target.transform.position, target.transform.rotation, out _, out _);
        }
        
        public static float BoxGap(BoxCollider box, Collider target)
        {
            if (box == null || target == null) return float.PositiveInfinity;
            
            Vector3 targetPoint = target.ClosestPoint(box.bounds.center);
            Vector3 boxPoint = box.ClosestPoint(targetPoint);
            return Vector3.Distance(boxPoint, target.ClosestPoint(boxPoint));
        }
    }
}