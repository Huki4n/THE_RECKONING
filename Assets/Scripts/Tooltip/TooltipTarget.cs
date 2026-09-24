using UnityEngine;
using UnityEngine.EventSystems;

namespace TheReckoning
{
    // Attach to a Button or the root of an icon prefab with a raycastable Image.
    public sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string title;
        [TextArea(2, 5)][SerializeField] private string description;

        public void SetContent(string newTitle, string newDescription)
        {
            title = newTitle;
            description = newDescription;
            if (BattleTooltip.Active != null)
                BattleTooltip.Active.UpdateContent(this, title, description);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (BattleTooltip.Active != null)
                BattleTooltip.Active.Show(this, title, description, eventData);
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();
        private void OnDisable() => Hide();

        private void Hide()
        {
            if (BattleTooltip.Active != null) BattleTooltip.Active.Hide(this);
        }
    }
}
