using System;
using System.Collections.Generic;
namespace TheReckoning.Morale
{
    public sealed class MoraleEngine
    {
        private readonly MoraleContext context;
        private readonly IMoraleRule[] rules;
        public bool Running { get; private set; } = true;
        public double Now => context.Now;
        public bool AffectEnemyWaveTiming => context.Settings.affectEnemyWaveTiming;
        public event Action<Team> StatusChanged
        {
            add
            {
                context.StatusChanged += value;
            }
            remove
            {
                context.StatusChanged -= value;
            }
        }
        public MoraleEngine(MoraleSettings settings, IMoraleWorld world)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            context = new MoraleContext(settings.Snapshot(), world);
            rules = new IMoraleRule[] { new BurstRule(), new VeteranDeathRule(), new RageRule(),
                new ConfidenceRule(), new BaseDefenseRule(), new AuraRule() };
        }
        public void Register(IMoraleActor actor)
        {
            if (Running)
                context.Register(actor);
        }
        public void Unregister(IMoraleActor actor)
        {
            context.Unregister(actor.Id);
            actor.Veteran.CancelEncounter();
        }
        public void Contact(IMoraleActor actor)
        {
            if (Running && actor.IsAlive)
                actor.Veteran.Touch(Now);
        }
        public void Publish(BattleEvent message)
        {
            if (Running)
                foreach (var rule in rules)
                    rule.Handle(message, context);
        }
        public void Tick(float deltaTime)
        {
            if (!Running)
                return;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            context.Now += deltaTime;
            context.TickEffects();
            foreach (var actor in context.World.Actors)
                if (actor.IsAlive)
                    actor.Veteran.Tick(Now, context.Settings);
            foreach (var rule in rules)
                rule.Tick(context);
        }
        public bool Cleanse(Team side, float immunitySeconds) =>
            Running && context.Cleanse(side, immunitySeconds);
        public float ProductionMultiplier(Team side) => Running ? context.TeamModifiers(side).Multiplier(Stat.ProductionSpeed, Now) : 1;
        public float Multiplier(IMoraleActor actor, Stat stat)
        {
            if (!Running)
                return 1;
            // Percentage bonuses are additive across morale sources and veterancy.
            float total = context.TeamModifiers(actor.Side).Bonus(stat, Now);
            var local = context.UnitModifiers(actor.Id);
            if (local != null)
                total += local.Bonus(stat, Now);
            if (stat == Stat.Damage && actor.Veteran.IsVeteran)
                total += context.Settings.veteranDamageBonus;
            return Math.Max(.05f, total + 1);
        }
        public bool HasLocalEffect(IMoraleActor actor, MoraleEffect effect) =>
            Running && actor != null &&
            context.UnitModifiers(actor.Id)?.Has(effect, Now) == true;
        public void CopyStatuses(Team side, List<MoraleStatus> destination)
        {
            if (Running)
                context.CopyStatuses(side, destination);
            else
                destination.Clear();
        }
        public void Stop()
        {
            if (!Running)
                return;
            Running = false;
            context.Clear();
        }
    }
}
    