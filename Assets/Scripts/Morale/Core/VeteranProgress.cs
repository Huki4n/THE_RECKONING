using System;
namespace TheReckoning.Morale
{
    public sealed class VeteranProgress
    {
        public int BattlesSurvived
        {
            get; private set;
        }
        public bool IsVeteran
        {
            get; private set;
        }
        public double BecameVeteranAt
        {
            get; private set;
        }
        private bool engaged;
        private double lastContact;
        public event Action Promoted;
        public void Touch(double now)
        {
            engaged = true;
            lastContact = now;
        }
        public void CancelEncounter()
        {
            engaged = false;
        }
        public void Tick(double now, MoraleSettings settings)
        {
            if (!engaged || now - lastContact < settings.encounterQuietTime)
                return;
            engaged = false;
            BattlesSurvived++;
            if (IsVeteran || BattlesSurvived < settings.veteranBattles)
                return;
            IsVeteran = true;
            BecameVeteranAt = now;
            Promoted?.Invoke();
        }
    }
}
