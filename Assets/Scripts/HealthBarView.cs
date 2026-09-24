using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [Header("Здоровье базы")]
        [SerializeField] private Health targetHealth;

        [Header("Элементы интерфейса")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;

        private void OnEnable()
        {
            if (targetHealth == null)
                return;

            targetHealth.Changed += Refresh;
            Refresh(targetHealth.Current, targetHealth.Maximum);
        }

        private void OnDisable()
        {
            if (targetHealth != null)
                targetHealth.Changed -= Refresh;
        }

        private void Refresh(float current, float maximum)
        {
            // До Initialize() здоровье ещё не настроено.
            bool initialized = maximum > 0f;

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = initialized ? maximum : 1f;
                healthSlider.SetValueWithoutNotify(
                    initialized ? current : 0f);
            }

            if (healthText != null)
            {
                healthText.text = initialized
                    ? $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}"
                    : "";
            }
        }
    }
}