using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Combat
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovement : MonoBehaviour
    {
        private static readonly float[] DashAngles = { 0f, -25f, 25f, -45f, 45f };
        [SerializeField, Min(0.1f)] private float knockbackDamping = 12f;

        private NavMeshAgent agent;
        private Vector3 knockbackVelocity;
        private float accelerationBeforeDash;
        private bool dashing;

        public bool IsOnNavMesh => agent != null && agent.isOnNavMesh;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
        }

        private void Update()
        {
            if (knockbackVelocity.sqrMagnitude < 0.0001f) return;
            float dt = Time.deltaTime;
            if (IsOnNavMesh) agent.Move(knockbackVelocity * dt);
            knockbackVelocity *= Mathf.Exp(-knockbackDamping * dt);
        }

        public bool MoveTo(Vector3 destination, float speed, float stoppingDistance)
        {
            if (!IsOnNavMesh) return false;
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
            return agent.SetDestination(destination);
        }

        public bool MoveToNearest(Vector3 destination, float sampleRadius,
            float speed, float stoppingDistance)
        {
            if (IsOnNavMesh && NavMesh.SamplePosition(destination, out NavMeshHit hit,
                    sampleRadius, agent.areaMask))
                return MoveTo(hit.position, speed, stoppingDistance);
            return false;
        }

        public void FaceMovementDirection(float dt)
        {
            if (!IsOnNavMesh || agent.desiredVelocity.sqrMagnitude < 0.01f) return;
            Vector3 direction = agent.desiredVelocity;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), 540f * dt);
        }

        public bool TryRetreat(Transform target, float distance, float speed,
            out Vector3 destination)
        {
            destination = transform.position;
            if (!IsOnNavMesh) return false;

            Vector3 away = target == null ? -transform.forward : transform.position - target.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();

            NavMeshPath path = new NavMeshPath();
            for (float step = distance; step >= 0.5f; step -= 0.5f)
            {
                Vector3 candidate = transform.position + away * step;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.75f, agent.areaMask))
                    continue;
                if (!agent.CalculatePath(hit.position, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                    continue;

                if (!MoveTo(hit.position, speed, 0f)) continue;
                destination = hit.position;
                return true;
            }
            return false;
        }

        public bool TryFindSafeDashDestination(Transform target, float distance,
            out Vector3 destination)
        {
            destination = transform.position;
            if (!IsOnNavMesh || target == null) return false;
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start,
                    2f, agent.areaMask)) return false;

            Vector3 away = transform.position - target.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            away.Normalize();
            float startingDistance = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(target.position.x, 0f, target.position.z));

            NavMeshPath path = new NavMeshPath();
            foreach (float angle in DashAngles)
            {
                Vector3 retreatDirection = Quaternion.Euler(0f, angle, 0f) * away;
                for (float step = distance; step >= 1.5f; step -= 0.5f)
                {
                    Vector3 candidate = start.position + retreatDirection * step;
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit sampled, 0.6f, agent.areaMask))
                        continue;
                    if (Mathf.Abs(sampled.position.y - start.position.y) > 0.75f)
                        continue;
                    Vector3 escape = sampled.position - transform.position;
                    escape.y = 0f;
                    if (escape.magnitude < 1.25f || Vector3.Dot(escape.normalized, away) < 0.6f)
                        continue;
                    Vector3 fromPlayer = sampled.position - target.position;
                    fromPlayer.y = 0f;
                    if (fromPlayer.magnitude < startingDistance + 1f)
                        continue;
                    if (!NavMesh.FindClosestEdge(sampled.position, out NavMeshHit edge, agent.areaMask) ||
                        edge.distance < agent.radius + 0.2f)
                        continue;
                    if (NavMesh.Raycast(start.position, sampled.position, out _, agent.areaMask))
                        continue;
                    if (!agent.CalculatePath(sampled.position, path) ||
                        path.status != NavMeshPathStatus.PathComplete)
                        continue;

                    destination = sampled.position;
                    return true;
                }
            }
            return false;
        }

        public bool BeginDash(Vector3 destination, float speed)
        {
            if (!IsOnNavMesh) return false;
            accelerationBeforeDash = agent.acceleration;
            agent.acceleration = Mathf.Max(agent.acceleration, speed * 10f);
            knockbackVelocity = Vector3.zero;
            dashing = true;
            if (MoveTo(destination, speed, 0.1f)) return true;
            EndDash();
            return false;
        }

        public void EndDash()
        {
            if (!dashing) return;
            if (agent != null) agent.acceleration = accelerationBeforeDash;
            dashing = false;
            Stop();
        }

        public bool HasReached(Vector3 destination, float tolerance)
        {
            if (!IsOnNavMesh || agent.pathPending) return false;
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= tolerance * tolerance;
        }

        public bool HasReachedDestination(float tolerance) =>
            IsOnNavMesh && !agent.pathPending &&
            (!agent.hasPath || agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, tolerance));

        public void Stop()
        {
            if (!IsOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        public void BeginLunge()
        {
            if (!IsOnNavMesh) return;
            Stop();
            agent.isStopped = false;
        }

        public float LungeTowards(Transform target, float maxStep, float minimumDistance)
        {
            if (!IsOnNavMesh || target == null) return 0f;
            Vector3 toward = target.position - transform.position;
            toward.y = 0f;
            float step = Mathf.Min(maxStep, Mathf.Max(0f, toward.magnitude - minimumDistance));
            if (step <= 0f || toward.sqrMagnitude < 0.001f) return 0f;
            agent.Move(toward.normalized * step);
            return step;
        }

        public void ApplyKnockback(Vector3 direction, float force, Transform source)
        {
            if (force <= 0f) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f && source != null)
            {
                direction = transform.position - source.position;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude < 0.001f) direction = -transform.forward;
            knockbackVelocity = direction.normalized * force;
            if (IsOnNavMesh) agent.isStopped = false;
        }

        public void FaceTarget(Transform target, float dt)
        {
            if (target == null) return;
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), 720f * dt);
        }
    }
}
