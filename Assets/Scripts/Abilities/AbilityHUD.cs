using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning
{
    // Optional helper: wire references once; no Inspector OnClick entries needed.
    public sealed class AbilityHUD : MonoBehaviour
    {
        [SerializeField] private AbilitySystem abilities;
        [SerializeField] private Button strikeChoice;
        [SerializeField] private Button shieldChoice;
        [SerializeField] private Button encouragementChoice;
        [SerializeField] private Button purificationChoice;
        [SerializeField] private Button activeButton;
        [SerializeField] private TMP_Text activeLabel;
        [SerializeField] private TMP_Text cooldownLabel;
        [SerializeField] private Image activeIcon;
        [SerializeField] private Sprite strikeIcon;
        [SerializeField] private Sprite shieldIcon;
        [SerializeField] private Sprite encouragementIcon;
        [SerializeField] private Sprite purificationIcon;

        private OrderAbility displayedAbility = OrderAbility.None;
        private Color iconColor;
        private Color labelColor;
        private Color cooldownColor;
        private bool? contentIsInteractable;
        private TooltipTarget activeTooltip;

        private void Awake()
        {
            if (abilities == null) return;
            if (strikeChoice != null) strikeChoice.onClick.AddListener(abilities.ChooseHeavenlyStrike);
            if (shieldChoice != null) shieldChoice.onClick.AddListener(abilities.ChooseHolyShield);
            if (encouragementChoice != null) encouragementChoice.onClick.AddListener(abilities.ChooseEncouragement);
            if (purificationChoice != null) purificationChoice.onClick.AddListener(abilities.ChoosePurification);
            if (activeButton != null) activeButton.onClick.AddListener(abilities.ActivateSelected);
            ConfigureTooltip(strikeChoice, OrderAbility.HeavenlyStrike);
            ConfigureTooltip(shieldChoice, OrderAbility.HolyShield);
            ConfigureTooltip(encouragementChoice, OrderAbility.Encouragement);
            ConfigureTooltip(purificationChoice, OrderAbility.Purification);
            if (activeButton != null)
            {
                activeTooltip = activeButton.GetComponent<TooltipTarget>();
                if (activeTooltip == null) activeTooltip = activeButton.gameObject.AddComponent<TooltipTarget>();
                if (activeButton.targetGraphic != null) activeButton.targetGraphic.raycastTarget = true;
            }
            if (activeIcon != null)
            {
                activeIcon.raycastTarget = false;
                iconColor = activeIcon.color;
            }
            if (activeLabel != null) labelColor = activeLabel.color;
            if (cooldownLabel != null) cooldownColor = cooldownLabel.color;
            RefreshIcon();
        }

        private void Update()
        {
            if (abilities == null) return;
            if (activeButton != null) activeButton.interactable = abilities.CanActivate;
            RefreshContentColor();
            if (displayedAbility != abilities.Selected)
            {
                displayedAbility = abilities.Selected;
                RefreshIcon();
            }
            if (activeLabel != null)
                activeLabel.text = abilities.Selected switch
                {
                    OrderAbility.HeavenlyStrike => "Heavenly Wrath",
                    OrderAbility.HolyShield => "Holy Shield",
                    OrderAbility.Encouragement => "Inspiration",
                    OrderAbility.Purification => "Cleansing",
                    _ => "Ability"
                };
            if (cooldownLabel != null)
            {
                if (abilities.IsTargeting)
                    cooldownLabel.text = "SELECT TARGET";
                else if (abilities.CooldownRemaining > 0f)
                    cooldownLabel.text = Mathf.CeilToInt(abilities.CooldownRemaining).ToString();
                else
                    cooldownLabel.text = abilities.CanActivate ? "READY" : "";
            }
        }

        private void RefreshContentColor()
        {
            if (activeButton == null) return;

            bool interactable = activeButton.IsInteractable();
            if (contentIsInteractable == interactable) return;
            contentIsInteractable = interactable;

            Color tint = interactable ? Color.white : activeButton.colors.disabledColor;
            if (activeIcon != null) activeIcon.color = Multiply(iconColor, tint);
            if (activeLabel != null) activeLabel.color = Multiply(labelColor, tint);
            if (cooldownLabel != null) cooldownLabel.color = Multiply(cooldownColor, tint);
        }

        private static Color Multiply(Color color, Color tint) =>
            new Color(color.r * tint.r, color.g * tint.g,
                color.b * tint.b, color.a * tint.a);

        private void RefreshIcon()
        {
            if (activeTooltip != null)
            {
                TooltipContent.Order(displayedAbility, out string title, out string description);
                activeTooltip.SetContent(title, description);
            }
            if (activeIcon == null) return;
            Sprite icon = displayedAbility switch
            {
                OrderAbility.HeavenlyStrike => strikeIcon,
                OrderAbility.HolyShield => shieldIcon,
                OrderAbility.Encouragement => encouragementIcon,
                OrderAbility.Purification => purificationIcon,
                _ => null
            };
            activeIcon.sprite = icon;
            activeIcon.enabled = icon != null;
        }

        private static void ConfigureTooltip(Button button, OrderAbility ability)
        {
            if (button == null) return;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            TooltipTarget tooltip = button.GetComponent<TooltipTarget>();
            if (tooltip == null) tooltip = button.gameObject.AddComponent<TooltipTarget>();
            TooltipContent.Order(ability, out string title, out string description);
            tooltip.SetContent(title, description);
        }

        private void OnDestroy()
        {
            if (abilities == null) return;
            if (strikeChoice != null) strikeChoice.onClick.RemoveListener(abilities.ChooseHeavenlyStrike);
            if (shieldChoice != null) shieldChoice.onClick.RemoveListener(abilities.ChooseHolyShield);
            if (encouragementChoice != null) encouragementChoice.onClick.RemoveListener(abilities.ChooseEncouragement);
            if (purificationChoice != null) purificationChoice.onClick.RemoveListener(abilities.ChoosePurification);
            if (activeButton != null) activeButton.onClick.RemoveListener(abilities.ActivateSelected);
        }
    }
}
