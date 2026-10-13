using System;
using DG.Tweening;
using Enemies.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Enemies.HUD
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider chunkSlider;
        [SerializeField] private Image flash;

        private EnemyBase enemy;

        private Vector3 startingScale;

        private void Awake()
        {
            enemy = GetComponentInParent<EnemyBase>();
            
            flash.gameObject.SetActive(false);
        }

        public void SetValue(float value)
        {
            float v = value / enemy.Config.maxHealth;
            
            healthSlider.DOValue(v, .2f).SetEase(Ease.OutCubic).OnComplete(() => healthSlider.value = v);
            chunkSlider.DOValue(v, .4f).SetDelay(.8f).SetEase(Ease.OutCubic).OnComplete(() => chunkSlider.value = v);
            
            flash.gameObject.SetActive(true);
            flash.DOFade(0.85f, 0.04f).SetLoops(2, LoopType.Yoyo).OnComplete(() => flash.gameObject.SetActive(false));
        }
    }
}
