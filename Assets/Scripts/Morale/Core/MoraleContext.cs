using System;
using System.Collections.Generic;
namespace TheReckoning.Morale
{
    public sealed class MoraleContext
    {
        public MoraleSettings Settings
        {
            get;
        }
        public IMoraleWorld World
        {
            get;
        }
        public double Now
        {
            get; internal set;
        }
        private readonly ModifierCollection[] teams = { new ModifierCollection(), new ModifierCollection() };
        private readonly Dictionary<int, ModifierCollection> units = new Dictionary<int, ModifierCollection>();
        private readonly double[] recruitUntil = new double[2];
        private readonly double[] fearBlockedUntil = new double[2];
        private readonly bool[,] auraVisible = new bool[2, 8];
        public event Action<Team> StatusChanged;
        public MoraleContext(MoraleSettings settings, IMoraleWorld world)
        {
            Settings = settings;
            World = world;
            teams[0].Changed += () => StatusChanged?.Invoke(Team.Left);
            teams[1].Changed += () => StatusChanged?.Invoke(Team.Right);
        }
        public ModifierCollection TeamModifiers(Team side) => teams[TeamUtility.Index(side)];
        public ModifierCollection UnitModifiers(int id) => units.TryGetValue(id, out var result) ? result : null;
        public void Register(IMoraleActor actor)
        {
            if (units.ContainsKey(actor.Id))
                return;
            var modifiers = new ModifierCollection();
            units.Add(actor.Id, modifiers);
            if (recruitUntil[TeamUtility.Index(actor.Side)] > Now)
                modifiers.Set(MoraleEffect.Confidence, Settings.confidence, Now + Settings.confidence.duration);
        }
        public void Unregister(int id)
        {
            units.Remove(id);
        }
        public void ApplyTeam(Team side, MoraleEffect id)
        {
            var effect = Settings.Get(id);
            TeamModifiers(side).Set(id, effect, Now + effect.duration);
        }
        public bool FearBlocked(Team side) => fearBlockedUntil[TeamUtility.Index(side)] > Now;
        public bool Cleanse(Team side, float immunitySeconds)
        {
            bool hadPanic = TeamModifiers(side).Has(MoraleEffect.Panic, Now);
            bool hadFear = auraVisible[TeamUtility.Index(side), (int)MoraleEffect.Fear];
            TeamModifiers(side).Remove(MoraleEffect.Panic);
            fearBlockedUntil[TeamUtility.Index(side)] = Now + immunitySeconds;
            // Remove any existing local Fear immediately; the aura rule will keep it off.
            foreach (var actor in World.Actors)
                if (actor.IsAlive && actor.Side == side)
                    UnitModifiers(actor.Id)?.Remove(MoraleEffect.Fear);
            ShowAura(side, MoraleEffect.Fear, false);
            return hadPanic || hadFear;
        }
        public void OpenRecruitWindow(Team side)
        {
            if (!Settings.confidence.enabled)
                return;
            recruitUntil[TeamUtility.Index(side)] = Now + Settings.confidenceRecruitWindow;
            StatusChanged?.Invoke(side);
        }
        public void Aura(IMoraleActor actor, MoraleEffect effect, bool active)
        {
            var modifiers = UnitModifiers(actor.Id);
            if (modifiers == null)
                return;
            if (active)
                modifiers.Set(effect, Settings.Get(effect), double.PositiveInfinity);
            else
                modifiers.Remove(effect);
        }
        public void ShowAura(Team side, MoraleEffect effect, bool visible)
        {
            int i = TeamUtility.Index(side), e = (int)effect;
            if (auraVisible[i, e] == visible)
                return;
            auraVisible[i, e] = visible;
            StatusChanged?.Invoke(side);
        }
        public void TickEffects()
        {
            teams[0].Tick(Now);
            teams[1].Tick(Now);
            foreach (var modifiers in units.Values)
                modifiers.Tick(Now);
            for (int i = 0; i < 2; i++)
                if (recruitUntil[i] > 0 && recruitUntil[i] <= Now)
                {
                    recruitUntil[i] = 0;
                    StatusChanged?.Invoke(i == 0 ? Team.Left : Team.Right);
                }
        }
        public void CopyStatuses(Team side, List<MoraleStatus> destination)
        {
            destination.Clear();
            TeamModifiers(side).CopyStatuses(Now, destination);
            int i = TeamUtility.Index(side);
            if (recruitUntil[i] > Now)
                destination.Add(new MoraleStatus(MoraleEffect.Confidence, recruitUntil[i] - Now, false));
            for (int e = 0; e < 8; e++)
                if (auraVisible[i, e])
                    destination.Add(new MoraleStatus((MoraleEffect)e, 0, true));
            destination.Sort((a, b) => a.Effect.CompareTo(b.Effect));
        }
        public void Clear()
        {
            // Stop first; callbacks observe an entirely empty state.
            units.Clear();
            Array.Clear(recruitUntil, 0, 2);
            Array.Clear(fearBlockedUntil, 0, 2);
            Array.Clear(auraVisible, 0, auraVisible.Length);
            teams[0].Clear();
            teams[1].Clear();
            StatusChanged?.Invoke(Team.Left);
            StatusChanged?.Invoke(Team.Right);
        }
    }
}
