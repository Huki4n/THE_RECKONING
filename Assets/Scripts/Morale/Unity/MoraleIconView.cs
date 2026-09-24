using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning.Morale
{
    public sealed class MoraleIconView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text fallbackLabel;

        private int lastSeconds = int.MinValue;

        public void Configure(Sprite sprite, string title, MoraleEffect effect)
        {
            TooltipTarget tooltip = GetComponent<TooltipTarget>();
            if (tooltip == null) tooltip = gameObject.AddComponent<TooltipTarget>();
            tooltip.SetContent(title, TooltipContent.Morale(effect));

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.raycastTarget = true;
            }
            if (fallbackLabel != null)
            {
                fallbackLabel.text = title;
                fallbackLabel.gameObject.SetActive(sprite == null);
                fallbackLabel.raycastTarget = true;
            }
            if (timer != null) timer.raycastTarget = false;
            lastSeconds = int.MinValue;
        }

        public void Show(MoraleStatus status)
        {
            int seconds = status.IsAura ? -1 : Mathf.CeilToInt((float)status.Remaining);
            if (timer != null && seconds != lastSeconds)
            {
                timer.text = seconds < 0 ? "" : seconds.ToString();
                lastSeconds = seconds;
            }
        }
    }
}
