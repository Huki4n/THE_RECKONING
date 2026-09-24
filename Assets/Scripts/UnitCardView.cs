using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning
{
    // Keep this class name: existing UnitCardView components remain attached to their cards.
    [ExecuteAlways]
    public sealed class UnitCardView : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private UnitData unitData;
        [SerializeField] private TurretData turretData;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite turretPortrait;
        [SerializeField] private Sprite coinSprite;

        [Tooltip("Leave blank to use the name from UnitData or TurretData.")]
        [SerializeField] private string customName;

        [Header("Card Elements")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image pictureImage;
        [SerializeField] private Image coinImage;
        [SerializeField] private TMP_Text priceText;

        public UnitData Data => unitData;

        private void OnEnable() => Refresh();
        private void Update() => Refresh();

        public void SetUnit(UnitData data, Sprite newPortrait)
        {
            unitData = data;
            turretData = null;
            portrait = newPortrait;
            Refresh();
        }

        public void SetTurret(TurretData data, Sprite newPortrait = null)
        {
            turretData = data;
            unitData = null;
            turretPortrait = newPortrait;
            Refresh();
        }

        [ContextMenu("Refresh Card")]
        public void Refresh()
        {
            // Turret data wins if an existing warrior card was duplicated with both fields filled.
            string defaultTitle = turretData != null ? turretData.DisplayName :
                unitData != null ? unitData.DisplayName : "";
            string title = !string.IsNullOrWhiteSpace(customName) ? customName : defaultTitle;
            string price = turretData != null ? turretData.Cost.ToString() :
                unitData != null ? unitData.Cost.ToString() : "";
            Sprite image = turretData != null
                ? (turretPortrait != null ? turretPortrait : turretData.TurretSprite)
                : portrait;

            if (nameText != null && nameText.text != title) nameText.text = title;
            if (priceText != null && priceText.text != price) priceText.text = price;

            SetImage(pictureImage, image);
            SetImage(coinImage, coinSprite);
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            if (image.sprite != sprite) image.sprite = sprite;

            bool shouldShow = sprite != null;
            if (image.enabled != shouldShow) image.enabled = shouldShow;
        }
    }
}
