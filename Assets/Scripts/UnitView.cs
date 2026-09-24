using UnityEngine;

namespace TheReckoning
{
    [RequireComponent(typeof(Unit))]
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite attackSprite;

        [SerializeField, Min(0.01f)]
        private float attackDuration = 0.2f;

        private Unit unit;
        private float attackTimer;
        private bool showingAttack;

        private void Awake()
        {
            unit = GetComponent<Unit>();

            // Если обычный спрайт не назначен,
            // запоминаем текущую картинку.
            if (body != null && idleSprite == null)
                idleSprite = body.sprite;
        }

        private void OnEnable()
        {
            unit.Attacked += ShowAttack;
            RestoreIdle();
        }

        private void OnDisable()
        {
            unit.Attacked -= ShowAttack;
            RestoreIdle();
        }

        private void Update()
        {
            if (!showingAttack)
                return;

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
                RestoreIdle();
        }

        private void ShowAttack(Combatant target)
        {
            if (body == null || attackSprite == null)
                return;

            body.sprite = attackSprite;
            attackTimer = Mathf.Max(0.01f, attackDuration);
            showingAttack = true;
        }

        private void RestoreIdle()
        {
            attackTimer = 0f;
            showingAttack = false;

            if (body != null && idleSprite != null)
                body.sprite = idleSprite;
        }
    }
}