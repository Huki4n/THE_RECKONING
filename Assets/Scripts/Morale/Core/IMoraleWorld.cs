using System.Collections.Generic;
namespace TheReckoning.Morale
{
    // Narrow port: rules do not depend on Transform, GameObject or Unity time.
    public interface IMoraleActor
    {
        int Id
        {
            get;
        }
        Team Side
        {
            get;
        }
        bool IsAlive
        {
            get;
        }
        float X
        {
            get;
        }
        VeteranProgress Veteran
        {
            get;
        }
    }
    public interface IMoraleWorld
    {
        IReadOnlyList<IMoraleActor> Actors
        {
            get;
        }
        float BaseX(Team side);
    }
    public interface IMoraleRule
    {
        void Handle(BattleEvent message, MoraleContext context);
        void Tick(MoraleContext context);
    }
    public interface IModifierSource
    {
        float Multiplier(Stat stat, double now);
    }
}
