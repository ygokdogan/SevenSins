using Player.Combat;
using UnityEngine;

namespace Enemies.Combat
{
    public enum ProjectileTrajectory { Straight, Arc }

    [CreateAssetMenu(menuName = "Combat/Ranged Attack Data", fileName = "new Ranged Attack")]
    public class RangedAttackData : AttackData
    {
        [Header("Projectile")]
        public EnemyProjectile projectilePrefab;
        [Min(0.1f)] public float projectileSpeed = 18f;
        public ProjectileTrajectory trajectory = ProjectileTrajectory.Straight;
        [Min(0f)] public float arcHeight = 0.7f;
        [Min(0f)] public float recoveryDuration = 0.5f;
    }
}
