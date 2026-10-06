using UnityEngine;

namespace Enemies.Combat.Visual_Feedback
{
    public class EnemyStunVisual : MonoBehaviour
    {
        [SerializeField] private float orbitSpeed = 220f;
        [SerializeField] private Color iconColor = new Color(1f, 0.82f, 0.15f);

        private Transform orbit;
        private Material iconMaterial;

        private void Awake()
        {
            orbit = transform.GetChild(0);
            orbit.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (orbit != null && orbit.gameObject.activeSelf)
                orbit.Rotate(Vector3.up, orbitSpeed * Time.deltaTime, Space.Self);
        }

        public void Show() => orbit.gameObject.SetActive(true);
        public void Hide() => orbit.gameObject.SetActive(false);

        private void OnDestroy()
        {
            if (iconMaterial != null) Destroy(iconMaterial);
        }
    }
}
