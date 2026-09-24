using System;
using System.Collections.Generic;
using UnityEngine;
namespace TheReckoning.Morale
{
    [DisallowMultipleComponent]
    public sealed class MoraleSystem : MonoBehaviour, IMoraleWorld
    {
        [SerializeField] private MoraleConfig config;
        private readonly List<IMoraleActor> actors = new List<IMoraleActor>();
        private GameManager match;
        private MoraleEngine engine;
        private IReadOnlyList<IMoraleActor> readOnlyActors;
        public IReadOnlyList<IMoraleActor> Actors => readOnlyActors ?? (readOnlyActors = actors.AsReadOnly());
        public double Now => engine != null ? engine.Now : 0;
        public float EnemyProductionMultiplier => engine != null && engine.AffectEnemyWaveTiming
            ? engine.ProductionMultiplier(Team.Right) : 1f;
        public event Action<Team> StatusChanged;
        public void Initialize(GameManager game)
        {
            Shutdown();
            match = game;
            var settings = config != null ? config.CreateSettings() : new MoraleSettings();
            if (config == null)
                Debug.LogWarning("MoraleSystem: using default settings; assign MoraleConfig to customize.", this);
            engine = new MoraleEngine(settings, this);
            engine.StatusChanged += ForwardStatus;
            foreach (var actor in actors)
                engine.Register(actor);
            ForwardStatus(Team.Left);
            ForwardStatus(Team.Right);
        }
        public float BaseX(Team side)
        {
            Base target = side == Team.Left ? match.LeftBase : match.RightBase;
            return target.transform.position.x;
        }
        public void Register(Unit unit)
        {
            if (actors.Contains(unit))
                return;
            actors.Add(unit);
            engine?.Register(unit);
        }
        public void Unregister(Unit unit)
        {
            if (!actors.Remove(unit))
                return;
            engine?.Unregister(unit);
        }
        public void Contact(Unit unit)
        {
            engine?.Contact(unit);
        }
        public bool Cleanse(Team side, float immunitySeconds) =>
            engine != null && engine.Cleanse(side, immunitySeconds);
        public void UnitKilled(Unit unit)
        {
            match?.Abilities?.OnUnitKilled(unit);
            engine?.Publish(new BattleEvent(BattleEventKind.UnitKilled, unit.Side, unit.Veteran.IsVeteran));
        }
        public void BaseDamaged(Base target, float actualDamage)
        {
            engine?.Publish(new BattleEvent(BattleEventKind.BaseDamaged, target.Side, false, actualDamage, target.Health.Maximum));
        }
        public float Multiplier(Unit unit, Stat stat) => engine != null ? engine.Multiplier(unit, stat) : 1;
        public bool HasLocalEffect(Unit unit, MoraleEffect effect) =>
            engine != null && engine.HasLocalEffect(unit, effect);
        public float ProductionMultiplier(Team side) => engine != null ? engine.ProductionMultiplier(side) : 1;
        public void CopyStatuses(Team side, List<MoraleStatus> destination)
        {
            if (engine != null)
                engine.CopyStatuses(side, destination);
            else
                destination.Clear();
        }
        // A single match-owned clock. No component execution order dependence for timers.
        public void Tick(float dt)
        {
            engine?.Tick(dt);
        }
        public void Stop()
        {
            Shutdown();
        }
        public void Shutdown()
        {
            if (engine == null)
                return;
            engine.Stop();
            engine.StatusChanged -= ForwardStatus;
            engine = null;
        }
        private void ForwardStatus(Team side)
        {
            StatusChanged?.Invoke(side);
        }
        private void OnDestroy()
        {
            Shutdown();
            actors.Clear();
        }
    }
}
