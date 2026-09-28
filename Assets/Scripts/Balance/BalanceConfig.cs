using UnityEngine;

namespace TheReckoning.Balance
{
    public enum UnitRole
    {
        Tank,
        Fighter,
        Striker
    }

    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "The Reckoning/Balance Config")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Unit power")]
        [Tooltip("Power of a tier 1 unit. Power = sqrt(HP * DPS) with speed and range modifiers.")]
        [Min(1f)] public float tier1Power = 40f;

        [Tooltip("Each tier is this many times stronger than the previous one.")]
        [Min(1f)] public float tierPowerMultiplier = 2.2f;

        [Tooltip("Gold per point of power.")]
        [Min(0.01f)] public float goldPerPower = .8f;

        [Header("Unit roles (HP per 1 DPS)")]
        [Min(0.1f)] public float tankHpPerDps = 12f;
        [Min(0.1f)] public float fighterHpPerDps = 6.67f;
        [Min(0.1f)] public float strikerHpPerDps = 2.5f;

        [Header("Unit modifiers")]
        [Min(0.01f)] public float referenceMoveSpeed = 1.2f;

        [Tooltip("Power scales as (speed / reference) ^ weight.")]
        [Min(0f)] public float moveSpeedWeight = .2f;

        [Min(0f)] public float referenceAttackRange = .15f;

        [Tooltip("Extra power per world unit of attack range above the reference.")]
        [Min(0f)] public float attackRangeValue = .5f;

        [Header("Recruitment")]
        [Tooltip("Production time per gold of unit cost.")]
        [Min(0f)] public float spawnSecondsPerGold = 1f / 30f;

        [Tooltip("Kill reward as a fraction of unit cost.")]
        [Range(0f, 1f)] public float killRewardFraction = .33f;

        [Header("Abilities")]
        [Tooltip("Average army power bonus one cooldown ability should give (duration / cooldown uptime).")]
        [Range(0f, .5f)] public float abilityTargetBonus = .04f;

        [Tooltip("Heavenly Wrath kills a fighter of this tier in one hit.")]
        [Min(1)] public int wrathKillsTier = 2;

        [Header("Turrets")]
        [Tooltip("Effective DPS of a tier 1 turret.")]
        [Min(0.1f)] public float turretTier1Dps = 20f;

        [Tooltip("Each turret tier has this many times more effective DPS.")]
        [Min(1f)] public float turretTierMultiplier = 1.6f;

        [Tooltip("Gold per point of effective DPS. Turrets have no HP, so this is higher than for units.")]
        [Min(0.01f)] public float turretGoldPerDps = 3f;

        [Tooltip("Splash radius given to arcing turrets.")]
        [Min(0f)] public float arcSplashRadius = .8f;

        [Min(0.1f)] public float referenceTurretRange = 6f;

        [Tooltip("Effective DPS scales as (range / reference) ^ weight.")]
        [Min(0f)] public float turretRangeWeight = .5f;

        [Tooltip("Effective DPS multiplier per world unit of splash radius.")]
        [Min(0f)] public float splashValuePerUnit = 1.5f;

        [Header("Economy")]
        [Min(0)] public int startingGold = 100;
        [Min(0f)] public float incomePerSecond = 5f;

        [Header("Base")]
        [Tooltip("How long a base holds against the reference attackers.")]
        [Min(1f)] public float baseHoldSeconds = 30f;

        [Tooltip("Number of tier 1 fighters attacking the base at once.")]
        [Min(1)] public int baseReferenceAttackers = 5;

        [Header("Waves")]
        [Min(0f)] public float firstWaveDelay = 10f;
        [Min(1f)] public float waveInterval = 20f;

        [Tooltip("Power budget of the first wave.")]
        [Min(1f)] public float firstWaveBudget = 80f;

        [Tooltip("Budget multiplier per wave.")]
        [Min(1f)] public float waveGrowth = 1.18f;

        [Header("Match length target (seconds)")]
        [Min(1f)] public float targetMatchMin = 180f;
        [Min(1f)] public float targetMatchMax = 300f;

        public float HpPerDps(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Tank:
                    return tankHpPerDps;
                case UnitRole.Striker:
                    return strikerHpPerDps;
                default:
                    return fighterHpPerDps;
            }
        }
    }
}
