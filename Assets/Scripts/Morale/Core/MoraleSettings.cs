using System;
namespace TheReckoning.Morale
{
    [Serializable]
    public sealed class EffectSettings
    {
        public bool enabled = true;
        public float duration;
        public float damage;
        public float attackSpeed;
        public float moveSpeed;
        public float damageTaken;
        public float productionSpeed;
        public EffectSettings(float seconds = 5)
        {
            duration = seconds;
        }
        public float Bonus(Stat stat)
        {
            switch (stat)
            {
                case Stat.Damage:
                    return damage;
                case Stat.AttackSpeed:
                    return attackSpeed;
                case Stat.MoveSpeed:
                    return moveSpeed;
                case Stat.DamageTaken:
                    return damageTaken;
                case Stat.ProductionSpeed:
                    return productionSpeed;
                default:
                    return 0;
            }
        }
        public EffectSettings Copy() => (EffectSettings)MemberwiseClone();
    }

    [Serializable]
    public sealed class MoraleSettings
    {
        public bool affectEnemyWaveTiming = false;
        public int burstCount = 5;
        public float burstWindow = 3;
        public float rageWindow = 3;
        public float rageHealthFraction = .25f;
        public int battleCryVeterans = 3;
        public float battleCryRadius = 2.5f;
        public float fearRadius = 2.5f;
        public float fearVeteranAge = 10;
        public float baseDefenseRadius = 3;
        public float defenseQuietTime = 2;
        public int confidenceKills = 10;
        public int confidenceAllowedLosses = 1;
        public float confidenceRecruitWindow = 8;
        public int veteranBattles = 3;
        public float encounterQuietTime = 3;
        public float veteranDamageBonus = .5f;

        public EffectSettings panic = new EffectSettings(5) { productionSpeed = -.5f };
        public EffectSettings inspiration = new EffectSettings(6) { attackSpeed = .25f };
        public EffectSettings demoralization = new EffectSettings(5) { damage = -.15f };
        public EffectSettings battleCry = new EffectSettings(0) { damage = .15f };
        public EffectSettings rage = new EffectSettings(8) { damage = .30f, moveSpeed = .20f };
        public EffectSettings resilience = new EffectSettings(5) { damageTaken = -.20f, productionSpeed = .20f };
        public EffectSettings confidence = new EffectSettings(6) { damage = .10f };
        public EffectSettings fear = new EffectSettings(0) { attackSpeed = -.20f };

        public EffectSettings Get(MoraleEffect effect)
        {
            switch (effect)
            {
                case MoraleEffect.Panic:
                    return panic;
                case MoraleEffect.Inspiration:
                    return inspiration;
                case MoraleEffect.Demoralization:
                    return demoralization;
                case MoraleEffect.BattleCry:
                    return battleCry;
                case MoraleEffect.Rage:
                    return rage;
                case MoraleEffect.Resilience:
                    return resilience;
                case MoraleEffect.Confidence:
                    return confidence;
                case MoraleEffect.Fear:
                    return fear;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect));
            }
        }
        // Match owns a deep copy. Editing an asset cannot alter an active match halfway through.
        public MoraleSettings Snapshot()
        {
            Validate();
            var copy = (MoraleSettings)MemberwiseClone();
            copy.panic = panic.Copy();
            copy.inspiration = inspiration.Copy();
            copy.demoralization = demoralization.Copy();
            copy.battleCry = battleCry.Copy();
            copy.rage = rage.Copy();
            copy.resilience = resilience.Copy();
            copy.confidence = confidence.Copy();
            copy.fear = fear.Copy();
            return copy;
        }
        public void Validate()
        {
            if (burstCount < 1 || battleCryVeterans < 1 || confidenceKills < 1 ||
                confidenceAllowedLosses < 0 || veteranBattles < 1)
                throw new ArgumentException("Morale: counts must be positive; allowed losses >= 0.");
            Positive(burstWindow);
            Positive(rageWindow);
            Positive(rageHealthFraction);
            if (rageHealthFraction > 1)
                throw new ArgumentException("Rage fraction must be <= 1.");
            Positive(battleCryRadius);
            Positive(fearRadius);
            Positive(baseDefenseRadius);
            Positive(defenseQuietTime);
            Positive(confidenceRecruitWindow);
            Positive(encounterQuietTime);
            Nonnegative(fearVeteranAge);
            Nonnegative(veteranDamageBonus);
            foreach (MoraleEffect effect in Enum.GetValues(typeof(MoraleEffect)))
            {
                var s = Get(effect);
                if (s == null)
                    throw new ArgumentException("Missing effect: " + effect);
                if (effect == MoraleEffect.BattleCry || effect == MoraleEffect.Fear)
                    Nonnegative(s.duration);
                else
                    Positive(s.duration);
                foreach (Stat stat in Enum.GetValues(typeof(Stat)))
                {
                    float v = s.Bonus(stat);
                    if (float.IsNaN(v) || float.IsInfinity(v) || v < -.95f || v > 10)
                        throw new ArgumentException("Bonus must be between -0.95 and 10: " + effect);
                }
            }
        }
        private static void Positive(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                throw new ArgumentException("Morale setting must be finite and positive.");
        }
        private static void Nonnegative(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                throw new ArgumentException("Morale setting must be finite and nonnegative.");
        }
    }
}
