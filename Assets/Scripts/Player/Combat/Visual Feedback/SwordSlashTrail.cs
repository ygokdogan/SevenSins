using System.Collections.Generic;
using UnityEngine;

namespace Player.Combat
{
    /// <summary>A blade-aligned ribbon sampled after animation. All scene objects are authored in Edit Mode.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class SwordSlashTrail : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Transform bladeBase;
        [SerializeField] private Transform bladeTip;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [Header("Appearance")]
        [SerializeField, Min(.01f)] private float lifetime = .13f;
        [SerializeField, Range(4, 128)] private int maxSamples = 48;
        [SerializeField, Min(.0001f)] private float minimumDistance = .008f;
        [SerializeField, Min(.1f)] private float teleportDistance = 2f;
        [SerializeField, ColorUsage(true, true)] private Color color = new Color(1.8f, 2.2f, 2.6f, .7f);

        private struct Sample
        {
            public Vector3 bottom, top;
            public float time;
        }

        private readonly List<Sample> samples = new List<Sample>(128);
        private readonly List<Vector3> vertices = new List<Vector3>(256);
        private readonly List<Color> colors = new List<Color>(256);
        private readonly List<Vector2> uvs = new List<Vector2>(256);
        private readonly List<int> indices = new List<int>(762);
        private Mesh mesh;
        private Mesh savedMesh;
        private uint sequence;
        private uint windowSequence;
        private bool wasEmitting;
        private Sample previousPose;
        private Vector3 previousPlayerPosition;
        private bool hasPreviousPose;

        private void Awake()
        {
            if (!combat || !bladeBase || !bladeTip || !meshFilter || !meshRenderer || !meshFilter.sharedMesh)
            {
                Debug.LogError("SwordSlashTrail needs its baked scene references and mesh.", this);
                enabled = false;
                return;
            }
            // Per-instance mutable mesh data; never modify the saved asset or create scene objects.
            savedMesh = meshFilter.sharedMesh;
            mesh = new Mesh { name = "Sword Slash (Dynamic)" };
            meshFilter.sharedMesh = mesh;
            mesh.MarkDynamic();
            Clear();
        }

        private void LateUpdate()
        {
            if (!mesh) return;
            var attack = combat.AttackState;
            // Remember open events even if a very short hit window also closes in the same frame.
            bool openedThisFrame = attack != null && windowSequence != attack.AttackWindowSequence;
            bool emitting = combat.isActiveAndEnabled && attack != null &&
                combat.CurrentState == attack && (attack.IsAttackWindowOpen || openedThisFrame);
            bool newAttack = attack != null && sequence != attack.AttackSequence;
            bool teleported = hasPreviousPose && Vector3.Distance(previousPlayerPosition, combat.transform.position) > teleportDistance;
            if (newAttack || teleported || (emitting && !wasEmitting)) Clear();
            if (attack != null)
            {
                sequence = attack.AttackSequence;
                windowSequence = attack.AttackWindowSequence;
            }

            float now = Time.time;
            while (samples.Count > 0 && now - samples[0].time >= lifetime) samples.RemoveAt(0);
            if (emitting && Time.deltaTime > 0f)
            {
                var sample = new Sample { bottom = bladeBase.position, top = bladeTip.position, time = now };
                if (samples.Count == 0 && hasPreviousPose && !teleported)
                {
                    Sample seed = previousPose;
                    seed.time = Mathf.Max(seed.time, now - lifetime * .75f);
                    samples.Add(seed);
                }
                float distance = samples.Count == 0 ? float.PositiveInfinity : Mathf.Max(
                    Vector3.Distance(samples[samples.Count - 1].bottom, sample.bottom),
                    Vector3.Distance(samples[samples.Count - 1].top, sample.top));
                if (samples.Count == 0 || distance >= minimumDistance)
                {
                    if (samples.Count >= maxSamples) samples.RemoveAt(0);
                    samples.Add(sample);
                }
            }
            wasEmitting = emitting;
            Rebuild(now);
            previousPose = new Sample { bottom = bladeBase.position, top = bladeTip.position, time = now };
            hasPreviousPose = true;
            previousPlayerPosition = combat.transform.position;
        }

        private void Rebuild(float now)
        {
            mesh.Clear();
            meshRenderer.enabled = samples.Count >= 2;
            if (samples.Count < 2) return;
            vertices.Clear();
            colors.Clear();
            uvs.Clear();
            indices.Clear();
            Matrix4x4 local = meshFilter.transform.worldToLocalMatrix;
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];
                float age = Mathf.Clamp01((now - sample.time) / Mathf.Max(.01f, lifetime));
                Color tint = color;
                tint.a *= (1f - age) * (1f - age);
                if (i == 0) tint.a = 0f;
                vertices.Add(local.MultiplyPoint3x4(sample.bottom));
                vertices.Add(local.MultiplyPoint3x4(sample.top));
                colors.Add(tint);
                colors.Add(tint);
                uvs.Add(new Vector2(age, 0f));
                uvs.Add(new Vector2(age, 1f));
                if (i == 0) continue;
                int v = i * 2;
                indices.Add(v - 2); indices.Add(v - 1); indices.Add(v);
                indices.Add(v); indices.Add(v - 1); indices.Add(v + 1);
            }
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
        }

        private void Clear()
        {
            samples.Clear();
            if (mesh) mesh.Clear();
            if (meshRenderer) meshRenderer.enabled = false;
        }

        private void OnDisable()
        {
            Clear();
            wasEmitting = false;
            hasPreviousPose = false;
        }

        private void OnDestroy()
        {
            if (meshFilter && meshFilter.sharedMesh == mesh) meshFilter.sharedMesh = savedMesh;
            if (mesh) Destroy(mesh);
        }

        private void OnDrawGizmosSelected()
        {
            if (!bladeBase || !bladeTip) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bladeBase.position, bladeTip.position);
            Gizmos.DrawWireSphere(bladeBase.position, .015f);
            Gizmos.DrawWireSphere(bladeTip.position, .015f);
        }
    }
}
