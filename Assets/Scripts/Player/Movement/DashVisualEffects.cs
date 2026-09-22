using UnityEngine;

namespace Player.Movement
{
    [DisallowMultipleComponent]
    public sealed class DashVisualEffects : MonoBehaviour
    {
        [Header("Wind")]
        [SerializeField] private Color windColor = new(0.72f, 0.9f, 1f, 0.8f);
        [SerializeField, Min(0f)] private float streaksPerSecond = 90f;
        [SerializeField, Min(0f)] private float streakBackSpeed = 11f;
        [SerializeField, Min(0f)] private float streakSpread = 0.65f;

        [Header("Trails")]
        [SerializeField, Min(0.01f)] private float trailLifetime = 0.22f;
        [SerializeField, Min(0.001f)] private float trailWidth = 0.085f;

        private PlayerMovement movement;
        private ParticleSystem windParticles;
        private TrailRenderer leftTrail;
        private TrailRenderer rightTrail;
        private Material windMaterial;
        private Material trailMaterial;
        private Vector3 dashDirection;
        private float emissionAccumulator;
        private bool active;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            BuildWindParticles();
            leftTrail = BuildTrail("Dash Trail Left", -0.28f);
            rightTrail = BuildTrail("Dash Trail Right", 0.28f);
        }

        private void OnEnable()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (movement == null) return;
            movement.DashStarted += BeginDash;
            movement.DashEnded += EndDash;
        }

        private void OnDisable()
        {
            if (movement != null)
            {
                movement.DashStarted -= BeginDash;
                movement.DashEnded -= EndDash;
            }
            SetActive(false);
        }

        private void Update()
        {
            if (!active || windParticles == null) return;

            emissionAccumulator += streaksPerSecond * Time.deltaTime;
            int count = Mathf.FloorToInt(emissionAccumulator);
            emissionAccumulator -= count;
            for (int i = 0; i < count; i++)
                EmitStreak(false);
        }

        private void BeginDash(Vector3 direction)
        {
            dashDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (dashDirection.sqrMagnitude < 0.01f) dashDirection = transform.forward;
            emissionAccumulator = 0f;
            leftTrail.Clear();
            rightTrail.Clear();
            SetActive(true);

            windParticles.Play(true);
            for (int i = 0; i < 18; i++)
                EmitStreak(true);
        }

        private void EndDash()
        {
            SetActive(false);
            if (windParticles != null)
                windParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void SetActive(bool value)
        {
            active = value;
            if (leftTrail != null) leftTrail.emitting = value;
            if (rightTrail != null) rightTrail.emitting = value;
        }

        private void EmitStreak(bool burst)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dashDirection).normalized;
            float sideOffset = Random.Range(-streakSpread, streakSpread);
            float height = Random.Range(0.15f, 1.65f);
            Vector3 position = transform.position + side * sideOffset + Vector3.up * height;
            Vector3 scatter = side * Random.Range(-1.8f, 1.8f) + Vector3.up * Random.Range(-0.7f, 0.7f);

            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = -dashDirection * Random.Range(streakBackSpeed * 0.75f, streakBackSpeed * 1.25f) + scatter,
                startLifetime = burst ? Random.Range(0.16f, 0.28f) : Random.Range(0.2f, 0.34f),
                startSize = burst ? Random.Range(0.045f, 0.1f) : Random.Range(0.025f, 0.065f),
                startColor = new Color(windColor.r, windColor.g, windColor.b, Random.Range(0.35f, windColor.a))
            };
            windParticles.Emit(emit, 1);
        }

        private void BuildWindParticles()
        {
            var go = new GameObject("Dash Wind Streaks");
            go.transform.SetParent(transform, false);
            windParticles = go.AddComponent<ParticleSystem>();

            var main = windParticles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            main.startSpeed = 0f;
            main.startLifetime = 0.28f;
            main.startSize = 0.05f;
            main.startColor = windColor;
            main.gravityModifier = 0f;

            var emission = windParticles.emission;
            emission.enabled = false;
            var shape = windParticles.shape;
            shape.enabled = false;

            var renderer = windParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 3.2f;
            renderer.velocityScale = 0.18f;
            renderer.sortingFudge = 2f;
            windMaterial = CreateAdditiveMaterial("Dash Wind Material", windColor);
            renderer.sharedMaterial = windMaterial;
        }

        private TrailRenderer BuildTrail(string objectName, float sideOffset)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(sideOffset, 0.85f, -0.2f);

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = trailLifetime;
            trail.minVertexDistance = 0.025f;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, trailWidth, 1f, 0f);
            trail.startColor = windColor;
            trail.endColor = new Color(windColor.r, windColor.g, windColor.b, 0f);
            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.emitting = false;

            if (trailMaterial == null)
                trailMaterial = CreateAdditiveMaterial("Dash Trail Material", windColor);
            trail.sharedMaterial = trailMaterial;
            return trail;
        }

        private static Material CreateAdditiveMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        private void OnDestroy()
        {
            if (windMaterial != null) Destroy(windMaterial);
            if (trailMaterial != null) Destroy(trailMaterial);
        }
    }
}
