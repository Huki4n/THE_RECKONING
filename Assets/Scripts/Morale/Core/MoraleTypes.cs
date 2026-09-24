using System;
namespace TheReckoning.Morale
{
    public enum MoraleEffect
    {
        Panic, Inspiration, Demoralization, BattleCry, Rage, Resilience, Confidence, Fear
    }
    public enum Stat
    {
        Damage, AttackSpeed, MoveSpeed, DamageTaken, ProductionSpeed
    }
    public enum BattleEventKind
    {
        UnitKilled, BaseDamaged
    }

    public readonly struct BattleEvent
    {
        public readonly BattleEventKind Kind;
        public readonly Team Side;
        public readonly bool WasVeteran;
        public readonly float Amount;
        public readonly float Maximum;
        public BattleEvent(BattleEventKind kind, Team side, bool veteran = false, float amount = 0, float maximum = 1)
        {
            Kind = kind;
            Side = side;
            WasVeteran = veteran;
            Amount = amount;
            Maximum = maximum;
        }
    }

    public readonly struct MoraleStatus
    {
        public readonly MoraleEffect Effect;
        public readonly double Remaining;
        public readonly bool IsAura;
        public MoraleStatus(MoraleEffect effect, double remaining, bool aura)
        {
            Effect = effect;
            Remaining = remaining;
            IsAura = aura;
        }
    }

    public static class TeamUtility
    {
        public static Team Opponent(Team side) => side == Team.Left ? Team.Right : Team.Left;
        public static int Index(Team side) => side == Team.Left ? 0 : 1;
    }
}
