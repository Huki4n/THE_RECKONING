using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheReckoning
{
    // Put on an active Canvas; the panel is a separate child of that Canvas.
    public sealed class BattleTooltip : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Vector2 cursorOffset = new Vector2(18f, -18f);

        public static BattleTooltip Active { get; private set; }
        private TooltipTarget owner;
        private RectTransform canvasRect;
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null || panel == null || titleText == null || descriptionText == null ||
                panel.parent != canvas.transform)
            {
                Debug.LogError("BattleTooltip: place this component on the Canvas and assign its direct child Panel, Title and Description.", this);
                enabled = false;
                return;
            }

            canvasRect = canvas.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.SetAsLastSibling();
            // The ability selector may be on another Canvas drawn above the battle HUD.
            Canvas popupCanvas = panel.GetComponent<Canvas>();
            if (popupCanvas == null) popupCanvas = panel.gameObject.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = short.MaxValue;
            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            if (panel.TryGetComponent(out CanvasGroup group)) group.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
            Active = this;
        }

        public void Show(TooltipTarget target, string title, string description, PointerEventData eventData)
        {
            if (!enabled || target == null || string.IsNullOrEmpty(title)) return;
            owner = target;
            titleText.text = title;
            descriptionText.text = description ?? "";
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            Move(eventData);
        }

        public void UpdateContent(TooltipTarget target, string title, string description)
        {
            if (owner != target || !panel.gameObject.activeSelf) return;
            titleText.text = title;
            descriptionText.text = description ?? "";
        }

        public void Hide(TooltipTarget target)
        {
            if (owner != target) return;
            owner = null;
            panel.gameObject.SetActive(false);
        }

        private void Move(PointerEventData eventData)
        {
            if (eventData == null) return;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, uiCamera, out Vector2 localPoint)) return;

            Canvas.ForceUpdateCanvases();
            Vector2 desired = localPoint + cursorOffset;
            Rect bounds = canvasRect.rect;
            Rect box = panel.rect;
            float minX = bounds.xMin + box.width * panel.pivot.x;
            float maxX = bounds.xMax - box.width * (1f - panel.pivot.x);
            float minY = bounds.yMin + box.height * panel.pivot.y;
            float maxY = bounds.yMax - box.height * (1f - panel.pivot.y);
            panel.anchoredPosition = new Vector2(
                Mathf.Clamp(desired.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX)),
                Mathf.Clamp(desired.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY)));
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            owner = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }
    }
}
