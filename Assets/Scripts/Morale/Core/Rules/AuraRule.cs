using System;
namespace TheReckoning.Morale
{
    public sealed class AuraRule : IMoraleRule
    {
        private readonly int[] veterans = new int[2];
        private readonly bool[] fearVisible = new bool[2];
        private readonly bool[] cryVisible = new bool[2];
        public void Handle(BattleEvent message, MoraleContext context)
        {
        }
        public void Tick(MoraleContext c)
        {
            Array.Clear(veterans, 0, 2);
            Array.Clear(fearVisible, 0, 2);
            Array.Clear(cryVisible, 0, 2);
            foreach (var actor in c.World.Actors)
                if (actor.IsAlive && actor.Veteran.IsVeteran)
                    veterans[TeamUtility.Index(actor.Side)]++;
            foreach (var target in c.World.Actors)
            {
                if (!target.IsAlive)
                    continue;
                bool cry = false, fear = false;
                int side = TeamUtility.Index(target.Side);
                foreach (var source in c.World.Actors)
                {
                    if (!source.IsAlive || !source.Veteran.IsVeteran)
                        continue;
                    float distance = Math.Abs(source.X - target.X);
                    if (source.Side == target.Side && veterans[side] >= c.Settings.battleCryVeterans && distance <= c.Settings.battleCryRadius)
                        cry = true;
                    if (source.Side != target.Side && c.Now - source.Veteran.BecameVeteranAt >= c.Settings.fearVeteranAge && distance <= c.Settings.fearRadius)
                        fear = true;
                }
                cry &= c.Settings.battleCry.enabled;
                fear &= c.Settings.fear.enabled && !c.FearBlocked(target.Side);
                c.Aura(target, MoraleEffect.BattleCry, cry);
                c.Aura(target, MoraleEffect.Fear, fear);
                cryVisible[side] |= cry;
                fearVisible[side] |= fear;
            }
            for (int i = 0; i < 2; i++)
            {
                Team side = i == 0 ? Team.Left : Team.Right;
                c.ShowAura(side, MoraleEffect.BattleCry, cryVisible[i]);
                c.ShowAura(side, MoraleEffect.Fear, fearVisible[i]);
            }
        }
    }
}
