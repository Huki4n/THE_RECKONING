using System;
namespace TheReckoning.Morale
{
    // A retreat out of the zone counts as repelling an attack too.
    public sealed class BaseDefenseRule : IMoraleRule
    {
        private readonly bool[] threatened = new bool[2];
        private readonly double[] lastThreat = new double[2];
        public void Handle(BattleEvent message, MoraleContext c)
        {
            if (message.Kind != BattleEventKind.BaseDamaged)
                return;
            int i = TeamUtility.Index(message.Side);
            threatened[i] = true;
            lastThreat[i] = c.Now;
        }
        public void Tick(MoraleContext c)
        {
            for (int i = 0; i < 2; i++)
            {
                Team side = i == 0 ? Team.Left : Team.Right;
                bool enemyNear = false;
                foreach (var actor in c.World.Actors)
                    if (actor.IsAlive && actor.Side != side && Math.Abs(actor.X - c.World.BaseX(side)) <= c.Settings.baseDefenseRadius)
                    {
                        enemyNear = true;
                        break;
                    }
                if (enemyNear)
                {
                    threatened[i] = true;
                    lastThreat[i] = c.Now;
                }
                else if (threatened[i] && c.Now - lastThreat[i] >= c.Settings.defenseQuietTime)
                {
                    threatened[i] = false;
                    c.ApplyTeam(side, MoraleEffect.Resilience);
                }
            }
        }
    }
}
