using Combat;
using Core;
using Enemies.Combat.Utilities;
using Enemies.Combat.Visual_Feedback;
using UnityEngine;

namespace Enemies.Feedback
{
    public class EnemyFeedback
    {
        private readonly HitFlash flash;
        private readonly HitJuice reaction;
        private readonly HitFx fx;
        private readonly EnemyStunVisual stun;

        public EnemyFeedback(GameObject enemy)
        {
            flash = enemy.GetComponentInChildren<HitFlash>(true);
            reaction = enemy.GetComponentInChildren<HitJuice>(true);
            fx = enemy.GetComponentInChildren<HitFx>(true);
            stun = enemy.GetComponentInChildren<EnemyStunVisual>(true);
        }

        public void PlayHit(HitInfo hit)
        {
            flash?.TriggerFlash();
            PlayReaction(hit);
            fx?.PlayHitSound(hit.impactMultiplier);
            fx?.SpawnHitVfx(hit);
        }

        public void PlayReaction(HitInfo hit)
        {
            if (reaction != null && reaction.isActiveAndEnabled)
                reaction.CalculateHitReaction(hit);
        }

        public void PlayBlock(float impact) => fx?.PlayHitSound(impact);
        public void PlayDeath(HitInfo hit) => fx?.SpawnDeathVfx(hit);
        public void ShowStun() => stun?.Show();
        public void HideStun() => stun?.Hide();
    }
}
