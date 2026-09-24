using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning
{
    // Put this component on the root of the single UI prefab used by EnemyAbilityHUD.
    public sealed class EnemyAbilityIndicator : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text timer;

        private bool instant;

        // Support HUD prefabs/scripts that still use the three-argument call.
        public void Configure(string abilityTitle, Sprite sprite, bool isInstant)
        {
            string description;
            switch (abilityTitle)
            {
                case "Curse of the Fallen": description = TooltipContent.Cult(CultAbilityEffect.FallenCurse); break;
                case "Dark Blessing": description = TooltipContent.Cult(CultAbilityEffect.DarkBlessing); break;
                case "Bloodlust": description = TooltipContent.Cult(CultAbilityEffect.Bloodlust); break;
                case "Will of the Fallen": description = TooltipContent.Cult(CultAbilityEffect.WillOfTheFallen); break;
                default: description = ""; break;
            }
            Configure(abilityTitle, sprite, isInstant, description);
        }

        public void Configure(string abilityTitle, Sprite sprite, bool isInstant, string description)
        {
            instant = isInstant;
            // The root owns the complete hover area. Child graphics only draw its contents.
            Graphic hitArea = GetComponent<Graphic>();
            if (hitArea == null)
            {
                Image transparentHitArea = gameObject.AddComponent<Image>();
                transparentHitArea.color = Color.clear;
                hitArea = transparentHitArea;
            }
            hitArea.raycastTarget = true;

            TooltipTarget tooltip = GetComponent<TooltipTarget>();
            if (tooltip == null) tooltip = gameObject.AddComponent<TooltipTarget>();
            tooltip.SetContent(abilityTitle, description);
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                if (icon != hitArea) icon.raycastTarget = false;
            }
            if (title != null)
            {
                title.text = abilityTitle;
                if (title != hitArea) title.raycastTarget = false;
            }
            if (timer != null)
            {
                timer.text = "";
                if (timer != hitArea) timer.raycastTarget = false;
            }
        }

        public void SetRemaining(float seconds)
        {
            if (timer != null)
                timer.text = instant ? "" : Mathf.CeilToInt(seconds).ToString();
        }
    }
}
