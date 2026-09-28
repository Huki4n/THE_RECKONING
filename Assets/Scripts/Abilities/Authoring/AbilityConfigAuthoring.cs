using TheReckoning.ECS;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class AbilityConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private AbilityConfig config;

        private sealed class Baker : Baker<AbilityConfigAuthoring>
        {
            public override void Bake(AbilityConfigAuthoring authoring)
            {
                AbilityConfig config = authoring.config;
                DependsOn(config);

                if (config == null)
                {
                    Debug.LogError($"{authoring.name}: assign AbilityConfig.", authoring);
                    return;
                }

                CultTriggerSettings triggers = config.Triggers;
                CultEffectSettings effects = config.cultEffects;

                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new AbilityEffectSettings
                {
                    InspirationDamageBonus = config.inspirationDamageBonus,
                    InspirationAttackSpeedBonus = config.inspirationAttackSpeedBonus,
                    ShieldDamageReduction = config.holyShieldDamageReduction,
                    BloodlustDamageBonus = effects.bloodlustDamageBonus,
                    BloodlustMoveSpeedBonus = effects.bloodlustMoveSpeedBonus,
                    BlessingDamageReduction = effects.blessingDamageReduction,
                    LastStandDamageBonus = effects.lastStandDamageBonus,
                    LastStandAttackSpeedBonus = effects.lastStandAttackSpeedBonus
                });

                AddComponent(entity, new CultAbilitySettings
                {
                    Ability = config.cultAbility == CultAbility.FallenCurse
                        ? CultAbilityKind.FallenCurse
                        : CultAbilityKind.DarkBlessing,

                    CurseMinimumOrderUnits = triggers.curseMinimumOrderUnits,
                    CurseCooldown = triggers.curseCooldown,
                    CurseMaxHealthDamage = effects.curseMaxHealthDamage,
                    CurseIndicatorSeconds = effects.curseIndicatorSeconds,

                    BlessingBaseHealthFraction = triggers.blessingBaseHealthFraction,
                    BlessingMinimumNearbyOrderUnits = triggers.blessingMinimumNearbyOrderUnits,
                    BlessingCooldown = triggers.blessingCooldown,
                    BlessingSeconds = effects.blessingSeconds,
                    BaseDefenseRadius = config.baseDefenseRadius,

                    BloodlustOrderDeaths = triggers.bloodlustOrderDeaths,
                    BloodlustDeathWindow = triggers.bloodlustDeathWindow,
                    BloodlustSeconds = effects.bloodlustSeconds,

                    LastStandBaseHealthFraction = triggers.lastStandBaseHealthFraction,
                    LastStandSeconds = effects.lastStandSeconds
                });
            }
        }
    }
}
