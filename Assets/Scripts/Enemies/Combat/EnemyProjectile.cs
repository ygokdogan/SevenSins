using Combat;
using Player.Combat;
using UnityEngine;

namespace Enemies.Combat
{
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float radius = 0.08f;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;
        [SerializeField] private LayerMask collisionLayers = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Transform owner;
        private RangedAttackData attack;
        private Vector3 direction;
        private Vector3 origin;
        private Vector3 targetPoint;
        private float elapsed;
        private float flightDuration;
        private float remainingLife;
        private bool reflected;
        private int activeCollisionLayers;

        public void Initialize(Transform source, RangedAttackData attackData, Vector3 destination)
        {
            owner = source;
            attack = attackData;
            origin = transform.position;
            targetPoint = destination;
            direction = (targetPoint - origin).normalized;
            flightDuration = Mathf.Max(0.01f, Vector3.Distance(origin, targetPoint) /
                Mathf.Max(0.1f, attackData.projectileSpeed));
            elapsed = 0f;
            remainingLife = lifetime;
            reflected = false;
            activeCollisionLayers = collisionLayers;
        }

        private void Update()
        {
            if (owner == null || attack == null) { Destroy(gameObject); return; }
            remainingLife -= Time.deltaTime;
            if (remainingLife <= 0f) { Destroy(gameObject); return; }

            elapsed += Time.deltaTime;
            Vector3 next = origin + direction * attack.projectileSpeed * elapsed;
            if (attack.trajectory == ProjectileTrajectory.Arc)
            {
                float t = elapsed / flightDuration;
                if (t <= 1f)
                    next = Vector3.Lerp(origin, targetPoint, t) +
                        Vector3.up * (4f * attack.arcHeight * t * (1f - t));
                else
                {
                    Vector3 exitVelocity = (targetPoint - origin - Vector3.up * 4f * attack.arcHeight) /
                        flightDuration;
                    next = targetPoint + exitVelocity * (elapsed - flightDuration);
                }
            }
            Vector3 step = next - transform.position;
            float distance = step.magnitude;
            if (distance < 0.0001f) return;
            direction = step / distance;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, direction,
                hits, distance, activeCollisionLayers, QueryTriggerInteraction.Ignore);
            int closest = -1;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(owner)) continue;
                if (hits[i].distance >= closestDistance) continue;
                closest = i;
                closestDistance = hits[i].distance;
            }

            if (closest >= 0)
            {
                if (!Impact(hits[closest])) Destroy(gameObject);
                return;
            }
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private bool Impact(RaycastHit hit)
        {
            foreach (MonoBehaviour behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (!(behaviour is IDamageable damageable)) continue;
                HitInfo info = new HitInfo
                {
                    damage = attack.damage,
                    poiseDamage = attack.poiseDamage,
                    impactMultiplier = attack.impactMultiplier,
                    knockbackForce = attack.knockbackForce,
                    hitPoint = hit.point,
                    hitNormal = hit.normal,
                    hitDirection = direction,
                    attacker = owner.gameObject
                };

                if (!reflected && behaviour is PlayerCombat player &&
                    player.TryReflectProjectile(info))
                {
                    Reflect(player.transform, hit.point);
                    return true;
                }

                damageable.TakeHit(info);
                if (reflected && attack.damage > 0f)
                    owner.GetComponentInParent<PlayerCombat>()?.NotifyHitConfirmed();
                if (reflected && behaviour is EnemyBase enemy)
                    enemy.Parried();
                break;
            }

            if (attack.hitVfxPrefab != null)
                Instantiate(attack.hitVfxPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            return false;
        }

        private void Reflect(Transform player, Vector3 impactPoint)
        {
            Transform shooter = owner;
            Collider shooterCollider = shooter.GetComponent<Collider>() ??
                shooter.GetComponentInChildren<Collider>();
            targetPoint = shooterCollider != null ? shooterCollider.bounds.center : shooter.position + Vector3.up;
            direction = (targetPoint - impactPoint).normalized;
            if (direction.sqrMagnitude < 0.001f) direction = -transform.forward;
            origin = impactPoint + direction * (radius + 0.05f);
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);
            owner = player;
            reflected = true;
            activeCollisionLayers = collisionLayers | (1 << shooter.gameObject.layer);
            flightDuration = Mathf.Max(0.01f, Vector3.Distance(origin, targetPoint) /
                Mathf.Max(0.1f, attack.projectileSpeed));
            elapsed = 0f;
            remainingLife = lifetime;
        }
    }
}
