using Enemies.Core;
using UnityEngine;

namespace Enemies.Combat
{
    public class EnemyVisionSensor
    {
        private readonly RaycastHit[] sightHits = new RaycastHit[16];

        public bool CanSee(Transform observer, Transform player,
            Collider playerCollider, EnemyConfig config)
        {
            if (player == null || playerCollider == null || config == null) return false;
            Vector3 origin = observer.position + Vector3.up * 1f;
            Vector3 toPlayer = playerCollider.bounds.center - origin;
            float distance = toPlayer.magnitude;
            if (distance > config.sightRange || distance < 0.001f) return false;

            Vector3 flatDirection = toPlayer;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > config.closeAwarenessRange * config.closeAwarenessRange &&
                Vector3.Angle(observer.forward, flatDirection) > config.fieldOfView * 0.5f)
                return false;

            int count = Physics.RaycastNonAlloc(origin, toPlayer / distance, sightHits,
                distance, config.sightLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            Collider nearest = null;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = sightHits[i].collider;
                if (candidate.transform.IsChildOf(observer) ||
                    sightHits[i].distance >= nearestDistance) continue;
                nearestDistance = sightHits[i].distance;
                nearest = candidate;
            }
            return nearest == null || nearest.transform.IsChildOf(player);
        }
    }
}
