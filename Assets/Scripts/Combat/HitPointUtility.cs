using UnityEngine;

namespace Combat
{
    public static class HitPointUtility
    {
        public static Vector3 OnTargetSurface(Collider target, Vector3 attackerPosition, out Vector3 normal)
        {
            Bounds bounds = target.bounds;
            Vector3 direction = bounds.center - attackerPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                direction = target.transform.forward;
            direction.Normalize();
            
            float distance = bounds.extents.magnitude + 1f;
            Camera camera = Camera.main;
            float eyeHeight = camera != null ? camera.transform.position.y : attackerPosition.y;
            float margin = Mathf.Min(0.05f, bounds.extents.y * 0.5f);
            float contactHeight = Mathf.Clamp(eyeHeight, bounds.min.y + margin, bounds.max.y - margin);
            Vector3 origin = new Vector3(bounds.center.x, contactHeight, bounds.center.z) - direction * distance;
            if (target.Raycast(new Ray(origin, direction), out RaycastHit contact, distance * 2f))
            {
                normal = contact.normal;
                return contact.point;
            }

            normal = -direction;
            return target.ClosestPoint(origin);
        }
    }
}
