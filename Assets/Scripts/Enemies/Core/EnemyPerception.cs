using Enemies.Combat;
using UnityEngine;

namespace Enemies.Core
{
    public class EnemyPerception
    {
        private readonly EnemyVisionSensor vision = new EnemyVisionSensor();
        private readonly Transform observer;
        private readonly Transform player;
        private readonly Collider playerCollider;
        private readonly EnemyConfig config;

        public Transform Target { get; private set; }
        public Collider TargetCollider { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }
        public bool HasLastSeenPosition { get; private set; }

        public EnemyPerception(Transform observer, Transform player, EnemyConfig config)
        {
            this.observer = observer;
            this.config = config;
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            this.player = player;
            playerCollider = player != null
                ? player.GetComponent<Collider>() ?? player.GetComponentInChildren<Collider>() : null;
        }

        public void Refresh()
        {
            if (vision.CanSee(observer, player, playerCollider, config))
                Remember(player, playerCollider);
            else
            {
                Target = null;
                TargetCollider = null;
            }
        }

        public void AlertFrom(GameObject attacker)
        {
            if (player != null && attacker != null && attacker.transform.IsChildOf(player))
                Remember(player, playerCollider);
        }

        public void Remember(Transform target, Collider collider)
        {
            Target = target;
            TargetCollider = collider;
            if (target == null) return;
            LastSeenPosition = target.position;
            HasLastSeenPosition = true;
        }

        public void ClearLastSeenPosition() => HasLastSeenPosition = false;
    }
}
