using Enemies.Core;
using UnityEngine;
using Utilities;

namespace Enemies.Movement
{
    public class MeleeLunge
    {
        private bool stopped = true;
        private float remaining = 0f;
        private float timeRemaining;
        
        public bool IsActive => !stopped;

        public void Reset()
        {
            stopped = true;
            remaining = 0f;
            timeRemaining = 0f;
        }

        public void Begin(EnemyBase enemy, BoxCollider box, float maxDistance, float maxTime, bool startedOutsideBox)
        {
            var target = enemy.Perception.TargetCollider;
            if (box == null || target == null || maxDistance <= 0f || maxTime <= 0f || !enemy.Movement.IsOnNavMesh ||
                (!startedOutsideBox && MeleeHitUtility.IsOverlapping(box, target)))
            {
                Reset();
                return;
            }
            
            remaining = Mathf.Min(maxDistance,  Mathf.Max(MeleeHitUtility.BoxGap(box, target) + 0.2f, startedOutsideBox ? 0.35f : 0f));
            timeRemaining = maxTime;
            stopped = remaining <= 0f;
            
            if (!stopped) enemy.Movement.BeginLunge();
        }

        public void Tick(EnemyBase enemy, float dt, float speed, float stopDistance)
        {
            if (stopped) return;
            if (timeRemaining <= 0f || enemy.Perception.Target == null || !enemy.Movement.IsOnNavMesh)
            {
                Stop(enemy);
                return;
            }
            
            float step = enemy.Movement.LungeTowards(enemy.Perception.Target,
                Mathf.Min(remaining, speed * Mathf.Min(dt, timeRemaining)), stopDistance);
            remaining -= step;
            timeRemaining -= dt;
            if (step <= 0f || remaining <= 0f || timeRemaining <= 0f) Stop(enemy);
        }
        
        private void Stop(EnemyBase enemy)
        {
            stopped = true;
            enemy.Movement.Stop();
        }
    }
}
