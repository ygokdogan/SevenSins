#if UNITY_EDITOR
using UnityEngine;

namespace Enemies.Core
{
    public abstract partial class EnemyBase
    {
        #region Gizmos
        protected virtual void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Vector3 origin = transform.position + Vector3.up * 1f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, config.sightRange);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, -config.fieldOfView * 0.5f, 0f) *
                transform.forward * config.sightRange);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, config.fieldOfView * 0.5f, 0f) *
                transform.forward * config.sightRange);
        }
        protected virtual void OnDrawGizmos()
        {
            if (!showPatrolGizmos || config == null) return;
            int count = Application.isPlaying && PatrolPoints != null
                ? PatrolPoints.Length : config.patrolOffsets?.Length ?? 0;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 point = PatrolGizmoPoint(i);
                Gizmos.color = i == 0 ? Color.green : Color.cyan;
                Gizmos.DrawSphere(point, 0.18f);
                Gizmos.DrawWireSphere(point, 0.35f);
#if UNITY_EDITOR
                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.Label(point + Vector3.up * 0.45f, $"Patrol {i + 1}");
#endif
                if (count < 2) continue;
                Vector3 next = PatrolGizmoPoint((i + 1) % count);
                Vector3 travel = next - point;
                if (travel.sqrMagnitude < 0.01f) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(point, next);
                Vector3 direction = travel.normalized;
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                Vector3 arrow = Vector3.Lerp(point, next, 0.7f);
                Gizmos.DrawLine(arrow, arrow - direction * 0.3f + side * 0.18f);
                Gizmos.DrawLine(arrow, arrow - direction * 0.3f - side * 0.18f);
            }
        }
        private Vector3 PatrolGizmoPoint(int index) =>
            Application.isPlaying && PatrolPoints != null
                ? PatrolPoints[index] : transform.TransformPoint(config.patrolOffsets[index]);
        #endregion
    }
}
#endif
