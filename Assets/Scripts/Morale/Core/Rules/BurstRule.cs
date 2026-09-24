namespace TheReckoning.Morale
{
    // Separate windows for casualties and kills; consumed events cannot retrigger a burst.
    public sealed class BurstRule : IMoraleRule
    {
        private readonly RollingWindow[] losses = { new RollingWindow(), new RollingWindow() };
        private readonly RollingWindow[] kills = { new RollingWindow(), new RollingWindow() };
        public void Handle(BattleEvent message, MoraleContext c)
        {
            if (message.Kind != BattleEventKind.UnitKilled)
                return;
            if (message.Side == Team.Left || c.Settings.affectEnemyWaveTiming)
                Register(losses[TeamUtility.Index(message.Side)], message.Side, MoraleEffect.Panic, c);
            var winner = TeamUtility.Opponent(message.Side);
            Register(kills[TeamUtility.Index(winner)], winner, MoraleEffect.Inspiration, c);
        }
        private static void Register(RollingWindow window, Team side, MoraleEffect effect, MoraleContext c)
        {
            window.Add(c.Now, 1, c.Settings.burstWindow);
            if (window.Total < c.Settings.burstCount)
                return;
            window.Clear();
            c.ApplyTeam(side, effect);
        }
        public void Tick(MoraleContext c)
        {
            for (int i = 0; i < 2; i++)
            {
                losses[i].Prune(c.Now, c.Settings.burstWindow);
                kills[i].Prune(c.Now, c.Settings.burstWindow);
            }
        }
    }
}
