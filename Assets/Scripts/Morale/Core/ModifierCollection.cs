using System;
using System.Collections.Generic;
namespace TheReckoning.Morale
{
    public sealed class ModifierCollection : IModifierSource
    {
        private struct Entry
        {
            public EffectSettings Settings; public double Until;
        }
        private readonly Dictionary<MoraleEffect, Entry> entries = new Dictionary<MoraleEffect, Entry>();
        private readonly List<MoraleEffect> expired = new List<MoraleEffect>();
        public event Action Changed;
        public void Set(MoraleEffect id, EffectSettings settings, double until)
        {
            if (!settings.enabled)
                return;
            if (entries.TryGetValue(id, out var old) && old.Until == until)
                return;
            entries[id] = new Entry { Settings = settings, Until = until };
            Changed?.Invoke();
        }
        public void Remove(MoraleEffect id)
        {
            if (entries.Remove(id))
                Changed?.Invoke();
        }
        public bool Has(MoraleEffect id, double now) => entries.TryGetValue(id, out var e) && e.Until > now;
        public float Multiplier(Stat stat, double now) => Math.Max(.05f, 1 + Bonus(stat, now));
        public float Bonus(Stat stat, double now)
        {
            float sum = 0;
            foreach (var e in entries.Values)
                if (e.Until > now)
                    sum += e.Settings.Bonus(stat);
            return sum;
        }
        public void Tick(double now)
        {
            expired.Clear();
            foreach (var pair in entries)
                if (pair.Value.Until <= now)
                    expired.Add(pair.Key);
            if (expired.Count == 0)
                return;
            foreach (var id in expired)
                entries.Remove(id);
            Changed?.Invoke();
        }
        public void CopyStatuses(double now, List<MoraleStatus> destination)
        {
            foreach (var pair in entries)
                if (pair.Value.Until > now)
                    destination.Add(new MoraleStatus(pair.Key, pair.Value.Until - now, double.IsPositiveInfinity(pair.Value.Until)));
        }
        public void Clear()
        {
            if (entries.Count == 0)
                return;
            entries.Clear();
            Changed?.Invoke();
        }
    }
}
