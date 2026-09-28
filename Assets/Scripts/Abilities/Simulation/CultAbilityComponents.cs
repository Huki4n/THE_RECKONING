using Unity.Entities;

namespace TheReckoning.ECS
{
    public enum CultAbilityKind : byte
    {
        FallenCurse,
        DarkBlessing
    }

    public struct CultAbilitySettings : IComponentData
    {
        public CultAbilityKind Ability;

        public int CurseMinimumOrderUnits;
        public float CurseCooldown;
        public float CurseMaxHealthDamage;
        public float CurseIndicatorSeconds;

        public float BlessingBaseHealthFraction;
        public int BlessingMinimumNearbyOrderUnits;
        public float BlessingCooldown;
        public float BlessingSeconds;
        public float BaseDefenseRadius;

        public int BloodlustOrderDeaths;
        public float BloodlustDeathWindow;
        public float BloodlustSeconds;

        public float LastStandBaseHealthFraction;
        public float LastStandSeconds;
    }

    public struct CultAbilityState : IComponentData
    {
        public float Clock;
        public float ReadyIn;
        public float CheckIn;
        public bool LastStandUsed;
    }

    public struct OrderDeathTime : IBufferElementData
    {
        public float Time;
    }
}
