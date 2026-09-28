using System;
using UnityEngine;

namespace TheReckoning
{
    [Serializable]
    public sealed class CultTriggerSettings
    {
        [Min(1)] public int curseMinimumOrderUnits = 6;
        [Min(0.1f)] public float curseCooldown = 40f;

        [Range(0f, 1f)]
        public float blessingBaseHealthFraction = .5f;

        [Min(1)]
        public int blessingMinimumNearbyOrderUnits = 3;

        [Min(0.1f)]
        public float blessingCooldown = 35f;

        [Min(1)]
        public int bloodlustOrderDeaths = 5;

        [Min(0.1f)]
        public float bloodlustDeathWindow = 3f;

        [Range(0f, 1f)]
        public float lastStandBaseHealthFraction = .25f;

        public static CultTriggerSettings Frequent() =>
            new CultTriggerSettings
            {
                curseMinimumOrderUnits = 4,
                curseCooldown = 22f,
                blessingBaseHealthFraction = .75f,
                blessingMinimumNearbyOrderUnits = 2,
                blessingCooldown = 20f,
                bloodlustOrderDeaths = 3,
                bloodlustDeathWindow = 5f,
                lastStandBaseHealthFraction = .5f
            };
    }

    [Serializable]
    public sealed class CultEffectSettings
    {
        [Range(0f, 1f)]
        public float curseMaxHealthDamage = .15f;

        [Min(0.1f)]
        public float curseIndicatorSeconds = 2f;

        [Min(0.1f)]
        public float blessingSeconds = 5f;

        [Range(0f, .95f)]
        public float blessingDamageReduction = .30f;

        [Min(0.1f)]
        public float bloodlustSeconds = 5f;

        [Min(0f)]
        public float bloodlustDamageBonus = .25f;

        [Min(0f)]
        public float bloodlustMoveSpeedBonus = .15f;

        [Min(0.1f)]
        public float lastStandSeconds = 8f;

        [Min(0f)]
        public float lastStandDamageBonus = .30f;

        [Min(0f)]
        public float lastStandAttackSpeedBonus = .20f;
    }

    [CreateAssetMenu(fileName = "AbilityConfig", menuName = "The Reckoning/Ability Config")]
    public sealed class AbilityConfig : ScriptableObject
    {
        [Header("Heavenly Wrath")]
        [Min(0f)] public float heavenlyWrathDamage = 150f;
        [Min(0.1f)] public float heavenlyWrathRadius = 2.1f;
        [Min(0f)] public float heavenlyWrathDelay = .5f;
        [Min(0.1f)] public float heavenlyWrathCooldown = 30f;

        [Header("Holy Shield")]
        [Min(0.1f)] public float holyShieldSeconds = 5f;
        [Range(0f, .95f)] public float holyShieldDamageReduction = .40f;
        [Min(0.1f)] public float holyShieldCooldown = 35f;

        [Header("Inspiration")]
        [Min(0.1f)] public float inspirationSeconds = 6f;
        [Min(0f)] public float inspirationDamageBonus = .25f;
        [Min(0f)] public float inspirationAttackSpeedBonus = .20f;
        [Min(0.1f)] public float inspirationCooldown = 35f;

        [Header("Cleansing")]
        [Tooltip("Morale suppression time; also the production bonus time when nothing was removed.")]
        [Min(0.1f)] public float cleansingSeconds = 3f;
        [Min(0f)] public float cleansingProductionBonus = .20f;
        [Min(0.1f)] public float cleansingCooldown = 30f;

        [Header("Cult")]
        public CultAbility cultAbility = CultAbility.FallenCurse;
        [Min(0.1f)] public float baseDefenseRadius = 3f;
        public bool useFrequentTriggers = true;
        public CultTriggerSettings originalTriggers = new CultTriggerSettings();
        public CultTriggerSettings frequentTriggers = CultTriggerSettings.Frequent();
        public CultEffectSettings cultEffects = new CultEffectSettings();

        public CultTriggerSettings Triggers =>
            useFrequentTriggers
                ? frequentTriggers
                : originalTriggers;
    }
}
