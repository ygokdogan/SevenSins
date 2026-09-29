using UnityEngine;

namespace Player.Movement
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(150)]
    public sealed class DashVisualEffects : MonoBehaviour
    {
        private PlayerMovement movement;
        [SerializeField] private ParticleSystem speedLines;
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(1)] private float emissionRate = 150;
        [SerializeField, Min(.01f)] private float lifetime = .22f;
        [SerializeField] private Color color = new Color(.72f, .94f, 1, .85f);
        [SerializeField] private Vector2 width = new Vector2(.012f, .028f);
        [SerializeField] private Vector2 length = new Vector2(.352f, .88f);
        [SerializeField, Min(.1f)] private float flowSpeed = 4;
        private readonly ParticleSystem.Particle[] particles = new ParticleSystem.Particle[128];
        private Vector3 direction;
        private bool dashing;
        private float emissionRemainder;

        private void OnEnable()
        {
            movement = GetComponent<PlayerMovement>();
            if (movement == null) return;
            movement.DashStarted += BeginDash;
            movement.DashEnded += EndDash;
        }

        private void BeginDash(Vector3 worldDirection)
        {
            if (speedLines == null || viewCamera == null) return;
            direction = worldDirection.normalized;
            dashing = true;
            emissionRemainder = 10;
            speedLines.Play(false);
        }

        private void EndDash()
        {
            dashing = false;
            emissionRemainder = 0;
            if (speedLines != null) speedLines.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void LateUpdate()
        {
            if (speedLines == null || viewCamera == null) return;
            if (!dashing && speedLines.particleCount == 0) return;
            float depth = Mathf.Max(.65f, viewCamera.nearClipPlane + .3f);
            speedLines.transform.localPosition = Vector3.forward * depth;
            float halfHeight = viewCamera.orthographic ? viewCamera.orthographicSize :
                Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad) * depth;
            float halfWidth = halfHeight * viewCamera.aspect;
            Vector3 localDirection = viewCamera.transform.InverseTransformDirection(direction);
            if (dashing)
            {
                emissionRemainder += emissionRate * Time.deltaTime;
                int count = Mathf.Min(32, Mathf.FloorToInt(emissionRemainder));
                emissionRemainder -= count;
                for (int i = 0; i < count; i++)
                {
                    float angle = Random.value * Mathf.PI * 2;
                    Vector2 point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    point /= Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y));
                    point *= Random.Range(.72f, 1.13f);
                    var emit = new ParticleSystem.EmitParams
                    {
                        position = new Vector3(point.x * halfWidth, point.y * halfHeight, 0),
                        startLifetime = lifetime * Random.Range(.75f, 1.15f),
                        startColor = color,
                        startSize3D = new Vector3(Random.Range(width.x, width.y) * halfHeight,
                            Random.Range(length.x, length.y) * halfHeight, 1)
                    };
                    speedLines.Emit(emit, 1);
                }
            }
            int alive = speedLines.GetParticles(particles);
            for (int i = 0; i < alive; i++)
            {
                Vector3 p = particles[i].position;
                Vector2 radial = new Vector2(p.x / halfWidth, p.y / halfHeight);
                // Reproject world dash direction as the camera turns: forward expands,
                // backward contracts, strafing flows opposite to player movement.
                Vector2 flow = radial * localDirection.z - new Vector2(localDirection.x, localDirection.y);
                Vector3 velocity = new Vector3(flow.x * halfWidth, flow.y * halfHeight, 0) * flowSpeed;
                particles[i].velocity = velocity;
                particles[i].rotation = -Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + 90;
            }
            speedLines.SetParticles(particles, alive);
        }

        private void OnDisable()
        {
            if (movement != null)
            {
                movement.DashStarted -= BeginDash;
                movement.DashEnded -= EndDash;
            }
            EndDash();
            if (speedLines != null) speedLines.Clear(false);
        }
    }
}
