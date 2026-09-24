namespace TheReckoning.Morale
{
    public sealed class RageRule : IMoraleRule
    {
        private readonly RollingWindow[] damage = { new RollingWindow(), new RollingWindow() };
        public void Handle(BattleEvent message, MoraleContext c)
        {
            if (message.Kind != BattleEventKind.BaseDamaged || message.Amount <= 0 || message.Maximum <= 0)
                return;
            var window = damage[TeamUtility.Index(message.Side)];
            window.Add(c.Now, message.Amount / message.Maximum, c.Settings.rageWindow);
            if (window.Total + 0.000001f < c.Settings.rageHealthFraction)
                return;
            window.Clear();
            c.ApplyTeam(message.Side, MoraleEffect.Rage);
        }
        public void Tick(MoraleContext c)
        {
            foreach (var window in damage)
                window.Prune(c.Now, c.Settings.rageWindow);
        }
    }
}
