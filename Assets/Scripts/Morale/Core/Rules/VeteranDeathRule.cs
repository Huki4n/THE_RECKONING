namespace TheReckoning.Morale
{
    public sealed class VeteranDeathRule : IMoraleRule
    {
        public void Handle(BattleEvent message, MoraleContext c)
        {
            if (message.Kind == BattleEventKind.UnitKilled && message.WasVeteran)
                c.ApplyTeam(message.Side, MoraleEffect.Demoralization);
        }
        public void Tick(MoraleContext context)
        {
        }
    }
}
