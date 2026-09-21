using UnityEngine;

namespace Player.Combat.VIsual_Feedback
{
    public class GuardImpactVisual : MonoBehaviour
    {
        [SerializeField] private ParticleSystem parryParticles;
        [SerializeField] private ParticleSystem blockParticles;

        private Vector3 defaultParryScale;
        private Vector3 defaultBlockScale;
        private void Awake()
        {
            parryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            blockParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            defaultParryScale = parryParticles.transform.localScale;
            defaultBlockScale = blockParticles.transform.localScale;
        }

        public void Play(Vector3 point, Vector3 normal, bool parry, float sizeMultiplier)
        {
            var currSparks = parry ? parryParticles : blockParticles;
            var defaultScale = parry ? parryParticles.transform.localScale : defaultParryScale;
            
            currSparks.transform.localScale = defaultScale * sizeMultiplier;
            currSparks.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            currSparks.Play();
        }
    }
}
