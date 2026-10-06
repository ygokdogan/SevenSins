using Enemies.Combat;
using UnityEngine;
using EnemyBase = Enemies.Core.EnemyBase;

namespace Combat.AttackIndicators
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackIndicator : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float heightAboveHead = 0.5f;
        [SerializeField, Min(0.1f)] private float worldSize = 0.72f;
        private readonly AttackIndicatorView[] views = new AttackIndicatorView[3];
        private AttackIndicatorView current;
        private EnemyBase enemy;
        private Collider body;
        private Camera aimCamera;
        private float duration;
        private float remaining;
        private float strikeFlash;
        public AttackDefense CurrentType { get; private set; }
        public bool IsVisible => current != null && current.gameObject.activeSelf;

        private void Awake() { enemy = GetComponent<EnemyBase>(); body = GetComponent<Collider>(); }
        public void Show(AttackDefense type, float windup)
        {
            Hide();
            CurrentType = type;
            int index = (int)type;
            if (index < 0 || index >= views.Length) return;
            if (views[index] == null)
            {
                var prefab = Resources.Load<AttackIndicatorView>("AttackIndicators/" + type);
                if (prefab == null) { Debug.LogWarning("Missing attack indicator prefab: " + type, this); return; }
                views[index] = Instantiate(prefab, transform);
            }
            current = views[index]; duration = Mathf.Max(0.01f, windup); remaining = duration;
            strikeFlash = 0f; current.gameObject.SetActive(true);
            current.SetProgress(0f, false);
        }
        public void UpdateRemaining(float time) => remaining = Mathf.Max(0f, time);
        public void Strike()
        {
            if (current == null) return;
            remaining = 0f; strikeFlash = 0.12f; current.SetProgress(1f, true);
        }
        public void Hide()
        {
            if (current != null) current.gameObject.SetActive(false);
            current = null; strikeFlash = 0f;
        }
        private void LateUpdate()
        {
            if (!IsVisible) return;
            if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsGrappleLocked) { Hide(); return; }
            if (aimCamera == null && enemy.Perception.Target != null)
            {
                var player = enemy.Perception.Target.GetComponentInParent<Player.PlayerManager>();
                if (player != null && player.GrappleController != null) aimCamera = player.GrappleController.AimCamera;
                if (aimCamera == null) aimCamera = enemy.Perception.Target.GetComponentInChildren<Camera>();
            }
            Camera camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera == null) return;
            Vector3 head = body != null
                ? new Vector3(body.bounds.center.x, body.bounds.max.y, body.bounds.center.z)
                : transform.position + Vector3.up * 2f;
            current.transform.position = head + Vector3.up * heightAboveHead;
            current.transform.rotation = camera.transform.rotation;
            float progress = 1f - Mathf.Clamp01(remaining / duration);
            float pop = Mathf.Lerp(1.22f, 1f, Mathf.Clamp01(progress * 5f));
            float pulse = progress > 0.7f ? 1f + Mathf.Sin(progress * 35f) * 0.07f : 1f;
            current.transform.localScale = Vector3.one * (worldSize / 80f * pop * pulse);
            current.SetProgress(progress, strikeFlash > 0f);
            if (strikeFlash <= 0f) return;
            strikeFlash -= Time.deltaTime;
            if (strikeFlash <= 0f) Hide();
        }
        private void OnDisable() => Hide();
    }
}
