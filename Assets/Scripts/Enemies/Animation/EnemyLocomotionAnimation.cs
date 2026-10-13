using Combat.AttackIndicators;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    // Moves only the capsule visual. Navigation and the hit box stay on the root.
    public class EnemyLocomotionAnimation : MonoBehaviour
    {
        public enum Mode { None, Walk, Sprint }

        [SerializeField] private Transform capsuleVisual;
        [SerializeField, Range(0f, 20f)] private float telegraphLeanAngle = 8f;


        private NavMeshAgent agent;
        private EnemyAttackIndicator attackIndicator;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private bool initialized;
        private Mode mode;
        private float phase;
        private float blend;
        private float telegraphBlend;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            attackIndicator = GetComponent<EnemyAttackIndicator>();
            if (capsuleVisual == null) capsuleVisual = transform.Find("Visual/Body Parts");
            if (capsuleVisual == null) return;
            basePosition = capsuleVisual.localPosition;
            baseRotation = capsuleVisual.localRotation;
            initialized = true;
        }

        public void PlayLocomotionAnimation(Mode nextMode)
        {
            mode = nextMode;
        }

        private void LateUpdate()
        {
            if (capsuleVisual == null || agent == null) return;

            float speed = agent.velocity.magnitude;
            float targetBlend = mode == Mode.None ? 0f : Mathf.Clamp01(speed / Mathf.Max(agent.speed, 0.01f));
            blend = Mathf.MoveTowards(blend, targetBlend, Time.deltaTime * 7f);
            phase += speed * Time.deltaTime * (mode == Mode.Sprint ? 2.8f : 2.2f);

            float bob = Mathf.Sin(phase * 2f) * (mode == Mode.Sprint ? 0.055f : 0.03f);
            float lean = mode == Mode.Sprint ? 7f : 3f;
            float side = Vector3.Dot(agent.velocity, transform.right) /
                         Mathf.Max(agent.speed, 0.01f);
            float targetTelegraphBlend = attackIndicator != null && attackIndicator.IsTelegraphing ? 1f : 0f;
            telegraphBlend = Mathf.MoveTowards(telegraphBlend, targetTelegraphBlend, Time.deltaTime * 10f);
            float pitch = Mathf.Lerp(lean * blend, -telegraphLeanAngle, telegraphBlend);
            capsuleVisual.localPosition = basePosition + Vector3.up * bob * blend;
            capsuleVisual.localRotation = baseRotation * Quaternion.Euler(
                pitch, 0f, Mathf.Clamp(side, -1f, 1f) * -5f * blend);
        }

        private void OnDisable()
        {
            telegraphBlend = 0f;
            if (!initialized || capsuleVisual == null) return;
            capsuleVisual.localPosition = basePosition;
            capsuleVisual.localRotation = baseRotation;
        }   
    }
}
