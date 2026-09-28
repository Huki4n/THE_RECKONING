using TheReckoning.ECS;
using TheReckoning.ECS.Presentation;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning
{
    public sealed class UnitView : MonoBehaviour, IEntityView
    {
        [Header("Sprites")]
        [SerializeField]
        private SpriteRenderer body;

        [SerializeField]
        private Sprite idleSprite;

        [SerializeField]
        private Sprite attackSprite;

        [Header("Orientation")]
        [Tooltip(
            "Root containing Visual, AuraVisual and other graphics. " +
            "Only this object is flipped for ECS presentation.")]
        [SerializeField]
        private Transform visualRoot;

        [Tooltip(
            "Enable if the source artwork faces right by default.")]
        [SerializeField]
        private bool artworkFacesRight = true;

        [Header("Attack")]
        [SerializeField, Min(0.01f)]
        private float attackDuration = 0.2f;

        private float attackTimer;
        private bool showingAttack;

        private Vector3 initialVisualScale;

        private void Awake()
        {
            if (body != null && idleSprite == null)
                idleSprite = body.sprite;

            if (visualRoot != null)
                initialVisualScale = visualRoot.localScale;
        }

        private void OnEnable()
        {
            RestoreIdle();
        }

        private void OnDisable()
        {
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

        public void Bind(
            Entity entity,
            EntityManager entityManager)
        {
            RestoreIdle();

            if (visualRoot == null)
            {
                Debug.LogWarning(
                    $"{name}: UnitView has no VisualRoot assigned.",
                    this);

                return;
            }

            if (!entityManager.HasComponent<TeamId>(entity))
            {
                Debug.LogWarning(
                    $"{name}: bound Entity has no TeamId.",
                    this);

                RestoreOrientation();
                return;
            }

            TeamId team =
                entityManager.GetComponentData<TeamId>(entity);

            ApplyTeamOrientation(team.Value);
        }

        public void Unbind()
        {
            RestoreIdle();
            RestoreOrientation();
        }

        public void PlayAttack()
        {
            if (body == null || attackSprite == null)
                return;

            body.sprite = attackSprite;

            attackTimer =
                Mathf.Max(0.01f, attackDuration);

            showingAttack = true;
        }

        private void RestoreIdle()
        {
            attackTimer = 0f;
            showingAttack = false;

            if (body != null && idleSprite != null)
                body.sprite = idleSprite;
        }

        private void ApplyTeamOrientation(byte teamId)
        {
            if (visualRoot == null)
                return;

            bool shouldFaceRight =
                teamId == 0;

            bool flip =
                shouldFaceRight != artworkFacesRight;

            Vector3 scale =
                initialVisualScale;

            scale.x =
                Mathf.Abs(initialVisualScale.x) *
                (flip ? -1f : 1f);

            visualRoot.localScale = scale;
        }

        private void RestoreOrientation()
        {
            if (visualRoot == null)
                return;

            visualRoot.localScale =
                initialVisualScale;
        }
    }
}