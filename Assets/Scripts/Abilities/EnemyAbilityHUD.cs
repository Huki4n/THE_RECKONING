using UnityEngine;

namespace TheReckoning
{
    // Keep this component and its container active. Only the spawned indicators disappear.
    public sealed class EnemyAbilityHUD : MonoBehaviour
    {
        [SerializeField] private AbilitySystem source;
        [SerializeField] private Transform container;
        [SerializeField] private EnemyAbilityIndicator indicatorPrefab;

        [SerializeField] private Sprite fallenCurseIcon;
        [SerializeField] private Sprite darkBlessingIcon;
        [SerializeField] private Sprite bloodlustIcon;
        [SerializeField] private Sprite willOfTheFallenIcon;

        private readonly EnemyAbilityIndicator[] indicators = new EnemyAbilityIndicator[4];

        private void Awake()
        {
            if (source != null && container != null && indicatorPrefab != null &&
                container != transform && container.IsChildOf(transform)) return;

            Debug.LogError("EnemyAbilityHUD: assign Source, a separate child Container, and an Indicator Prefab.", this);
            enabled = false;
        }

        private void Update()
        {
            Refresh(0, CultAbilityEffect.FallenCurse, "Curse of the Fallen", fallenCurseIcon, true);
            Refresh(1, CultAbilityEffect.DarkBlessing, "Dark Blessing", darkBlessingIcon, false);
            Refresh(2, CultAbilityEffect.Bloodlust, "Bloodlust", bloodlustIcon, false);
            Refresh(3, CultAbilityEffect.WillOfTheFallen, "Will of the Fallen", willOfTheFallenIcon, false);
        }

        private void Refresh(int index, CultAbilityEffect effect, string title, Sprite sprite, bool instant)
        {
            float remaining = source.CultEffectRemaining(effect);
            EnemyAbilityIndicator indicator = indicators[index];

            if (remaining <= 0f)
            {
                if (indicator != null) Destroy(indicator.gameObject);
                indicators[index] = null;
                return;
            }

            if (indicator == null)
            {
                indicator = Instantiate(indicatorPrefab, container, false);
                indicator.gameObject.SetActive(true); // Works with an inactive prefab, too.
                indicator.Configure(title, sprite, instant, source.CultEffectDescription(effect));
                indicators[index] = indicator;
            }

            indicator.SetRemaining(remaining);
        }

        private void OnDisable()
        {
            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] != null) Destroy(indicators[i].gameObject);
                indicators[i] = null;
            }
        }
    }
}
