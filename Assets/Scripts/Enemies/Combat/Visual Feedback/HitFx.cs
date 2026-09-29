using Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies.Combat.Utilities
{
    public class HitFx : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip[] hitSounds;

        [Header("Preplaced Particle Prefabs")]
        [SerializeField] private ParticleSystem hitParticles;
        [SerializeField] private ParticleSystem deathParticles;

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip defaultHitClip;

        private void Awake()
        {
            Prepare(hitParticles);
            Prepare(deathParticles);


        }

        public void SpawnHitVfx(HitInfo hitInfo)
        {
            PlayParticle(hitParticles, ResolveHitPosition(hitInfo), ResolveImpactDirection(hitInfo));
        }

        public void SpawnDeathVfx(HitInfo hitInfo)
        {
            if (deathParticles == null) return;

            deathParticles.transform.SetParent(null, true);
            var main = deathParticles.main;
            main.stopAction = ParticleSystemStopAction.Destroy;
            PlayParticle(deathParticles, ResolveEnemyCenter(), ResolveImpactDirection(hitInfo));
        }

        private Vector3 ResolvePosition(HitInfo hitInfo)
        {
            return hitInfo.hitPoint != Vector3.zero ? hitInfo.hitPoint : transform.position + Vector3.up;
        }

        private Vector3 ResolveHitPosition(HitInfo hitInfo)
        {
            Vector3 surfaceNormal = hitInfo.hitNormal.sqrMagnitude > 0.001f
                ? hitInfo.hitNormal.normalized
                : -ResolveImpactDirection(hitInfo);
            return ResolvePosition(hitInfo) + surfaceNormal * 0.08f;
        }

        private Vector3 ResolveImpactDirection(HitInfo hitInfo)
        {
            if (hitInfo.hitDirection.sqrMagnitude > 0.001f) return hitInfo.hitDirection.normalized;
            if (hitInfo.hitNormal.sqrMagnitude > 0.001f) return -hitInfo.hitNormal.normalized;
            return transform.forward;
        }

        private Vector3 ResolveEnemyCenter()
        {
            Collider bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null) return bodyCollider.bounds.center;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return transform.position + Vector3.up;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        private static void Prepare(ParticleSystem particles)
        {
            if (particles == null) return;

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Disable;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false);
        }

        private static void PlayParticle(ParticleSystem particles, Vector3 position, Vector3 direction)
        {
            Quaternion rotation = direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : Quaternion.identity;
            PlayParticle(particles, position, rotation);
        }

        private static void PlayParticle(ParticleSystem particles, Vector3 position, Quaternion rotation)
        {
            if (particles == null) return;

            particles.transform.SetPositionAndRotation(position, rotation);
            particles.gameObject.SetActive(true);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }

        public void PlayHitSound(float impactMultiplier)
        {
            float volumeScale = Mathf.Max(0f, impactMultiplier);
            if (hitSounds != null && hitSounds.Length > 0)
            {
                AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (clip != null && audioSource != null)
                {
                    audioSource.pitch = Random.Range(0.92f, 1.08f);
                    audioSource.PlayOneShot(clip, volumeScale);
                    return;
                }
            }

            if (defaultHitClip != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.15f);
                audioSource.PlayOneShot(defaultHitClip, volumeScale);
            }
        }

#if UNITY_EDITOR
        public void BakePersistentObjects()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
            if (defaultHitClip != null) return;
            string path = "Assets/Generated/RuntimeSetup/DefaultHit.wav";
            defaultHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (defaultHitClip != null) return;
            var randomState = Random.state;
            Random.InitState(147);
            GenerateDefaultHitClip(path);
            Random.state = randomState;
            UnityEditor.AssetDatabase.ImportAsset(path);
            defaultHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private void GenerateDefaultHitClip(string path)
        {
            const int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * 0.12f);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Pow(1f - t, 3.5f);
                float frequency = Mathf.Lerp(180f, 35f, t);
                float sine = Mathf.Sin(2f * Mathf.PI * frequency * (i / (float)sampleRate));
                samples[i] = (sine + (Random.value * 2f - 1f) * 0.35f) * envelope * 0.75f;
            }

            using (var writer = new System.IO.BinaryWriter(System.IO.File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + sampleCount * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(sampleRate); writer.Write(sampleRate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(sampleCount * 2);
                foreach (float sample in samples)
                    writer.Write((short)(Mathf.Clamp(sample, -1, 1) * short.MaxValue));
            }
        }
#endif
    }
}
