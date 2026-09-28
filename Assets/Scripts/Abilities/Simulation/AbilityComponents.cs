using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct AbilityBuffs : IComponentData
    {
        public float Shield;
        public float Blessing;
        public float LastStand;
    }

    public struct AbilityTimers : IComponentData
    {
        public float Curse;
        public float Inspiration;
        public float Bloodlust;
        public float Blessing;
        public float LastStand;
        public float Production;
    }

    public enum AbilityRequestKind : byte
    {
        HeavenlyWrath,
        HolyShield,
        Inspiration,
        Cleansing
    }

    public struct AbilityRequest : IComponentData
    {
        public AbilityRequestKind Kind;
        public byte Team;
        public float X;
        public float Radius;
        public float Amount;
        public float Duration;
    }

    public struct AbilityEffectSettings : IComponentData
    {
        public float InspirationDamageBonus;
        public float InspirationAttackSpeedBonus;
        public float ShieldDamageReduction;
        public float BloodlustDamageBonus;
        public float BloodlustMoveSpeedBonus;
        public float BlessingDamageReduction;
        public float LastStandDamageBonus;
        public float LastStandAttackSpeedBonus;
    }
}
