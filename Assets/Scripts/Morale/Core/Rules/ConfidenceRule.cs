namespace TheReckoning.Morale
{
    public sealed class ConfidenceRule : IMoraleRule
    {
        private readonly int[] kills = new int[2];
        private readonly int[] losses = new int[2];
        public void Handle(BattleEvent message, MoraleContext c)
        {
            if (message.Kind != BattleEventKind.UnitKilled)
                return;
            int loser = TeamUtility.Index(message.Side), winner = 1 - loser;
            losses[loser]++;
            if (losses[loser] > c.Settings.confidenceAllowedLosses)
            {
                kills[loser] = 0;
                losses[loser] = 0;
            }
            if (++kills[winner] < c.Settings.confidenceKills)
                return;
            kills[winner] = 0;
            losses[winner] = 0;
            c.OpenRecruitWindow(TeamUtility.Opponent(message.Side));
        }
        public void Tick(MoraleContext context)
        {
        }
    }
}
