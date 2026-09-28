using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace TheReckoning
{
    public sealed class AbilityHUD : MonoBehaviour
    {
        [SerializeField] private AbilitySystem abilities;

        [FormerlySerializedAs("strikeChoice")]
        [SerializeField] private Button heavenlyWrathChoice;

        [FormerlySerializedAs("shieldChoice")]
        [SerializeField] private Button holyShieldChoice;

        [FormerlySerializedAs("encouragementChoice")]
        [SerializeField] private Button inspirationChoice;

        [FormerlySerializedAs("purificationChoice")]
        [SerializeField] private Button cleansingChoice;

        [SerializeField] private Button activeButton;
        [SerializeField] private TMP_Text activeLabel;
        [SerializeField] private TMP_Text cooldownLabel;
        [SerializeField] private Image activeIcon;

        [FormerlySerializedAs("strikeIcon")]
        [SerializeField] private Sprite heavenlyWrathIcon;

        [FormerlySerializedAs("shieldIcon")]
        [SerializeField] private Sprite holyShieldIcon;

        [FormerlySerializedAs("encouragementIcon")]
        [SerializeField] private Sprite inspirationIcon;

        [FormerlySerializedAs("purificationIcon")]
        [SerializeField] private Sprite cleansingIcon;

        private OrderAbility displayedAbility = OrderAbility.None;
        private Color iconColor;
        private Color labelColor;
        private Color cooldownColor;
        private bool? contentIsInteractable;
        private TooltipTarget activeTooltip;

        private void Awake()
        {
            if (abilities == null) return;

            if (heavenlyWrathChoice != null)
                heavenlyWrathChoice.onClick.AddListener(
                    abilities.ChooseHeavenlyWrath);

            if (holyShieldChoice != null)
                holyShieldChoice.onClick.AddListener(
                    abilities.ChooseHolyShield);

            if (inspirationChoice != null)
                inspirationChoice.onClick.AddListener(
                    abilities.ChooseInspiration);

            if (cleansingChoice != null)
                cleansingChoice.onClick.AddListener(
                    abilities.ChooseCleansing);

            if (activeButton != null)
                activeButton.onClick.AddListener(
                    abilities.ActivateSelected);

            ConfigureTooltip(
                heavenlyWrathChoice,
                OrderAbility.HeavenlyWrath);

            ConfigureTooltip(
                holyShieldChoice,
                OrderAbility.HolyShield);

            ConfigureTooltip(
                inspirationChoice,
                OrderAbility.Inspiration);

            ConfigureTooltip(
                cleansingChoice,
                OrderAbility.Cleansing);

            if (activeButton != null)
            {
                activeTooltip =
                    activeButton.GetComponent<TooltipTarget>();

                if (activeTooltip == null)
                    activeTooltip =
                        activeButton.gameObject
                            .AddComponent<TooltipTarget>();

                if (activeButton.targetGraphic != null)
                    activeButton.targetGraphic.raycastTarget = true;
            }

            if (activeIcon != null)
            {
                activeIcon.raycastTarget = false;
                iconColor = activeIcon.color;
            }

            if (activeLabel != null)
                labelColor = activeLabel.color;

            if (cooldownLabel != null)
                cooldownColor = cooldownLabel.color;

            RefreshIcon();
        }

        private void Update()
        {
            if (abilities == null)
                return;

            if (activeButton != null)
                activeButton.interactable = abilities.CanActivate;

            RefreshContentColor();

            if (displayedAbility != abilities.Selected)
            {
                displayedAbility = abilities.Selected;
                RefreshIcon();
            }

            if (activeLabel != null)
            {
                activeLabel.text = abilities.Selected switch
                {
                    OrderAbility.HeavenlyWrath =>
                        "Heavenly Wrath",

                    OrderAbility.HolyShield =>
                        "Holy Shield",

                    OrderAbility.Inspiration =>
                        "Inspiration",

                    OrderAbility.Cleansing =>
                        "Cleansing",

                    _ => "Ability"
                };
            }

            if (cooldownLabel != null)
            {
                if (abilities.IsTargeting)
                {
                    cooldownLabel.text = "SELECT TARGET";
                }
                else if (abilities.CooldownRemaining > 0f)
                {
                    cooldownLabel.text =
                        Mathf.CeilToInt(
                            abilities.CooldownRemaining).ToString();
                }
                else
                {
                    cooldownLabel.text =
                        abilities.CanActivate ? "READY" : "";
                }
            }
        }

        private void RefreshContentColor()
        {
            if (activeButton == null)
                return;

            bool interactable =
                activeButton.IsInteractable();

            if (contentIsInteractable == interactable)
                return;

            contentIsInteractable = interactable;

            Color tint = interactable
                ? Color.white
                : activeButton.colors.disabledColor;

            if (activeIcon != null)
                activeIcon.color = Multiply(iconColor, tint);

            if (activeLabel != null)
                activeLabel.color = Multiply(labelColor, tint);

            if (cooldownLabel != null)
                cooldownLabel.color = Multiply(cooldownColor, tint);
        }

        private static Color Multiply(
            Color color,
            Color tint) =>
            new Color(
                color.r * tint.r,
                color.g * tint.g,
                color.b * tint.b,
                color.a * tint.a);

        private void RefreshIcon()
        {
            if (activeTooltip != null)
            {
                TooltipContent.Order(
                    displayedAbility,
                    out string title,
                    out string description);

                activeTooltip.SetContent(
                    title,
                    description);
            }

            if (activeIcon == null)
                return;

            Sprite icon = displayedAbility switch
            {
                OrderAbility.HeavenlyWrath =>
                    heavenlyWrathIcon,

                OrderAbility.HolyShield =>
                    holyShieldIcon,

                OrderAbility.Inspiration =>
                    inspirationIcon,

                OrderAbility.Cleansing =>
                    cleansingIcon,

                _ => null
            };

            activeIcon.sprite = icon;
            activeIcon.enabled = icon != null;
        }

        private static void ConfigureTooltip(
            Button button,
            OrderAbility ability)
        {
            if (button == null)
                return;

            if (button.targetGraphic != null)
                button.targetGraphic.raycastTarget = true;

            TooltipTarget tooltip =
                button.GetComponent<TooltipTarget>();

            if (tooltip == null)
            {
                tooltip =
                    button.gameObject
                        .AddComponent<TooltipTarget>();
            }

            TooltipContent.Order(
                ability,
                out string title,
                out string description);

            tooltip.SetContent(title, description);
        }

        private void OnDestroy()
        {
            if (abilities == null)
                return;

            if (heavenlyWrathChoice != null)
            {
                heavenlyWrathChoice.onClick.RemoveListener(
                    abilities.ChooseHeavenlyWrath);
            }

            if (holyShieldChoice != null)
            {
                holyShieldChoice.onClick.RemoveListener(
                    abilities.ChooseHolyShield);
            }

            if (inspirationChoice != null)
            {
                inspirationChoice.onClick.RemoveListener(
                    abilities.ChooseInspiration);
            }

            if (cleansingChoice != null)
            {
                cleansingChoice.onClick.RemoveListener(
                    abilities.ChooseCleansing);
            }

            if (activeButton != null)
            {
                activeButton.onClick.RemoveListener(
                    abilities.ActivateSelected);
            }
        }
    }
}