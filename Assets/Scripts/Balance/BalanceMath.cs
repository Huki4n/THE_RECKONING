using UnityEngine;

namespace TheReckoning.Balance
{
    public static class BalanceMath
    {
        public static float Dps(float damage, float attackInterval) =>
            attackInterval > 0f ? damage / attackInterval : 0f;

        public static float TierPower(BalanceConfig config, int tier) =>
            config.tier1Power * Mathf.Pow(config.tierPowerMultiplier, Mathf.Max(0, tier - 1));

        public static float UnitPower(
            BalanceConfig config,
            float health,
            float dps,
            float moveSpeed,
            float attackRange)
        {
            float power = Mathf.Sqrt(Mathf.Max(0f, health) * Mathf.Max(0f, dps));

            float speedRatio = moveSpeed / config.referenceMoveSpeed;
            power *= Mathf.Pow(Mathf.Max(0.01f, speedRatio), config.moveSpeedWeight);

            float extraRange = Mathf.Max(0f, attackRange - config.referenceAttackRange);
            power *= 1f + extraRange * config.attackRangeValue;

            return power;
        }

        public static float UnitPower(BalanceConfig config, UnitData unit) =>
            UnitPower(
                config,
                unit.MaximumHealth,
                Dps(unit.Damage, unit.AttackInterval),
                unit.MoveSpeed,
                unit.AttackRange);

        public static float UnitCost(BalanceConfig config, float power) =>
            power * config.goldPerPower;

        public static float SpawnInterval(BalanceConfig config, float cost) =>
            cost * config.spawnSecondsPerGold;

        public static float KillReward(BalanceConfig config, float cost) =>
            cost * config.killRewardFraction;

        public static void RoleStats(
            BalanceConfig config,
            float power,
            UnitRole role,
            out float health,
            out float dps)
        {
            float ratio = Mathf.Sqrt(config.HpPerDps(role));
            health = power * ratio;
            dps = power / ratio;
        }

        public static void UnitTargetStats(
            BalanceConfig config,
            int tier,
            UnitRole role,
            float moveSpeed,
            float attackRange,
            out float health,
            out float dps)
        {
            RoleStats(config, TierPower(config, tier), role, out health, out dps);

            float modifier = UnitPower(config, 1f, 1f, moveSpeed, attackRange);
            health /= modifier;
            dps /= modifier;
        }

        public static float TurretTierDps(BalanceConfig config, int tier) =>
            config.turretTier1Dps * Mathf.Pow(config.turretTierMultiplier, Mathf.Max(0, tier - 1));

        public static float TurretSplash(BalanceConfig config, bool arcing) =>
            arcing ? config.arcSplashRadius : 0f;

        public static float TurretTargetDps(
            BalanceConfig config,
            int tier,
            float range,
            float splashRadius) =>
            TurretTierDps(config, tier) / TurretEffectiveDps(config, 1f, range, splashRadius);

        public static float TurretEffectiveDps(
            BalanceConfig config,
            float dps,
            float range,
            float splashRadius)
        {
            float rangeRatio = range / config.referenceTurretRange;
            float effective = dps * Mathf.Pow(Mathf.Max(0.01f, rangeRatio), config.turretRangeWeight);
            return effective * (1f + splashRadius * config.splashValuePerUnit);
        }

        public static float TurretEffectiveDps(BalanceConfig config, TurretData turret) =>
            TurretEffectiveDps(
                config,
                Dps(turret.Damage, turret.AttackInterval),
                turret.Range,
                turret.SplashRadius);

        public static float TurretCost(BalanceConfig config, float effectiveDps) =>
            effectiveDps * config.turretGoldPerDps;

        public static float BaseHealth(BalanceConfig config)
        {
            RoleStats(config, TierPower(config, 1), UnitRole.Fighter, out _, out float dps);
            return config.baseHoldSeconds * config.baseReferenceAttackers * dps;
        }

        public static float WaveStartTime(BalanceConfig config, int wave) =>
            config.firstWaveDelay + wave * config.waveInterval;

        public static float WaveBudget(BalanceConfig config, int wave) =>
            config.firstWaveBudget * Mathf.Pow(config.waveGrowth, wave);

        public static float CumulativeWavePower(BalanceConfig config, float time)
        {
            float total = 0f;

            for (int wave = 0; WaveStartTime(config, wave) <= time; wave++)
                total += WaveBudget(config, wave);

            return total;
        }

        public static float CumulativePlayerPower(
            BalanceConfig config,
            float time,
            float playerMultiplier = 1f,
            float waveMultiplier = 1f)
        {
            float gold = config.startingGold + config.incomePerSecond * time;
            float rewards = CumulativeWavePower(config, time) * config.killRewardFraction;
            return (gold / config.goldPerPower + rewards) * playerMultiplier / waveMultiplier;
        }

        public static float EstimateCrossover(
            BalanceConfig config,
            float maxTime,
            float playerMultiplier = 1f,
            float waveMultiplier = 1f,
            float step = 1f)
        {
            for (float time = 0f; time <= maxTime; time += step)
            {
                float player = CumulativePlayerPower(config, time, playerMultiplier, waveMultiplier);

                if (CumulativeWavePower(config, time) > player)
                    return time;
            }

            return -1f;
        }
    }
}
