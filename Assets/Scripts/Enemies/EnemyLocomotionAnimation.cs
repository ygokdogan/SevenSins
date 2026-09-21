using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    // Moves only the capsule visual. Navigation and the hit box stay on the root.
    public class EnemyLocomotionAnimation : MonoBehaviour
    {
        public enum Mode { None, Walk, Sprint }

        [SerializeField] private Transform capsuleVisual;

        private NavMeshAgent agent;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private Mode mode;
        private float phase;
        private float blend;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (capsuleVisual == null) capsuleVisual = transform.Find("Visual/Body Parts");
            if (capsuleVisual == null) return;
            basePosition = capsuleVisual.localPosition;
            baseRotation = capsuleVisual.localRotation;
        }

        public void PlayLocomotionAnimation(Mode nextMode) => mode = nextMode;

        private void LateUpdate()
        {
            if (capsuleVisual == null || agent == null) return;

            float speed = agent.velocity.magnitude;
            float targetBlend = mode == Mode.None ? 0f : Mathf.Clamp01(speed / agent.speed);
            blend = Mathf.MoveTowards(blend, targetBlend, Time.deltaTime * 7f);
            phase += speed * Time.deltaTime * (mode == Mode.Sprint ? 2.8f : 2.2f);

            float bob = Mathf.Sin(phase * 2f) * (mode == Mode.Sprint ? 0.055f : 0.03f);
            float lean = mode == Mode.Sprint ? 7f : 3f;
            float side = Vector3.Dot(agent.velocity, transform.right) /
                         Mathf.Max(agent.speed, 0.01f);
            capsuleVisual.localPosition = basePosition + Vector3.up * bob * blend;
            capsuleVisual.localRotation = baseRotation * Quaternion.Euler(
                lean * blend, 0f, Mathf.Clamp(side, -1f, 1f) * -5f * blend);
        }

        private void OnDisable()
        {
            if (capsuleVisual == null) return;
            capsuleVisual.localPosition = basePosition;
            capsuleVisual.localRotation = baseRotation;
        }   
    }
}
