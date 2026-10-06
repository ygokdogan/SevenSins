using UnityEngine;

namespace Combat.AttackIndicators
{
    public sealed class AttackIndicatorView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image countdown;
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private Color accent = Color.yellow;
        public void SetProgress(float progress, bool striking)
        {
            if (countdown != null) countdown.fillAmount = Mathf.Clamp01(progress);
            if (icon != null) icon.color = striking ? Color.white : Color.Lerp(accent, Color.white, progress * 0.15f);
        }
    }
}
