using UnityEngine;

namespace TheReckoning.Balance.EditorTools
{
    internal static class AbilityBalance
    {
        public static float PowerMultiplier(
            BalanceConfig config,
            float damage,
            float attackSpeed,
            float damageTaken,
            float moveSpeed = 0f)
        {
            float dps = Mathf.Max(0f, 1f + damage) * Mathf.Max(0f, 1f + attackSpeed);
            float health = 1f / Mathf.Max(0.05f, 1f + damageTaken);
            float speed = Mathf.Pow(Mathf.Max(0.01f, 1f + moveSpeed), config.moveSpeedWeight);
            return Mathf.Sqrt(dps * health) * speed;
        }

        public static float AverageBonus(float multiplier, float duration, float cooldown) =>
            cooldown > 0f
                ? (multiplier - 1f) * Mathf.Clamp01(duration / cooldown)
                : 0f;

        public static float CooldownForBonus(float multiplier, float duration, float targetBonus) =>
            targetBonus > 0f
                ? Mathf.Max(duration, duration * (multiplier - 1f) / targetBonus)
                : duration;

        public static float ShieldMultiplier(BalanceConfig config, AbilityConfig abilities) =>
            PowerMultiplier(config, 0f, 0f, -abilities.holyShieldDamageReduction);

        public static float InspirationMultiplier(BalanceConfig config, AbilityConfig abilities) =>
            PowerMultiplier(
                config,
                abilities.inspirationDamageBonus,
                abilities.inspirationAttackSpeedBonus,
                0f);

        public static float BlessingMultiplier(BalanceConfig config, AbilityConfig abilities) =>
            PowerMultiplier(config, 0f, 0f, -abilities.cultEffects.blessingDamageReduction);

        public static float BloodlustMultiplier(BalanceConfig config, AbilityConfig abilities) =>
            PowerMultiplier(
                config,
                abilities.cultEffects.bloodlustDamageBonus,
                0f,
                0f,
                abilities.cultEffects.bloodlustMoveSpeedBonus);

        public static float LastStandMultiplier(BalanceConfig config, AbilityConfig abilities) =>
            PowerMultiplier(
                config,
                abilities.cultEffects.lastStandDamageBonus,
                abilities.cultEffects.lastStandAttackSpeedBonus,
                0f);

        public static float PlayerMultiplier(BalanceConfig config, AbilityConfig abilities)
        {
            if (abilities == null)
                return 1f;

            float shield = AverageBonus(
                ShieldMultiplier(config, abilities),
                abilities.holyShieldSeconds,
                abilities.holyShieldCooldown);

            float inspiration = AverageBonus(
                InspirationMultiplier(config, abilities),
                abilities.inspirationSeconds,
                abilities.inspirationCooldown);

            return 1f + shield + inspiration;
        }

        public static float WaveMultiplier(BalanceConfig config, AbilityConfig abilities)
        {
            if (abilities == null || abilities.cultAbility != CultAbility.DarkBlessing)
                return 1f;

            return 1f + AverageBonus(
                BlessingMultiplier(config, abilities),
                abilities.cultEffects.blessingSeconds,
                abilities.Triggers.blessingCooldown);
        }

        public static float WrathDamage(BalanceConfig config)
        {
            float power = BalanceMath.TierPower(config, config.wrathKillsTier);
            BalanceMath.RoleStats(config, power, UnitRole.Fighter, out float health, out _);
            return health;
        }
    }
}
