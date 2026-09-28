using System.Collections.Generic;
using TheReckoning.ECS.Data;
using TheReckoning.Morale;
using Unity.Collections;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial class EcsMoraleRuntimeSystem : SystemBase
    {
        private EcsMoraleWorld moraleWorld;
        private MoraleEngine engine;

        private EntityQuery unitQuery;
        private EntityQuery baseQuery;
        private EntityQuery abilityRequestQuery;
        private Entity productionEntity;

        private readonly HashSet<Entity> seenUnits =
            new HashSet<Entity>();

        private readonly List<Entity> removalBuffer =
            new List<Entity>();

        public MoraleEngine Engine => engine;

        public EcsMoraleWorld MoraleWorld => moraleWorld;

        protected override void OnCreate()
        {
            RequireForUpdate<EcsBattleEventStream>();

            RequireForUpdate<MatchConfig>();

            unitQuery = GetEntityQuery(
                ComponentType.ReadOnly<UnitTag>(),
                ComponentType.ReadOnly<TeamId>(),
                ComponentType.ReadOnly<Health>());

            baseQuery = GetEntityQuery(
                ComponentType.ReadOnly<BaseTag>(),
                ComponentType.ReadOnly<TeamId>());

            abilityRequestQuery = GetEntityQuery(
                ComponentType.ReadOnly<AbilityRequest>());

            productionEntity =
                EntityManager.CreateEntity(typeof(TeamProduction));

#if UNITY_EDITOR
            EntityManager.SetName(
                productionEntity,
                "Team Production");
#endif

            WriteProduction(1f, 1f);
        }

        protected override void OnStartRunning()
        {
            CreateRuntime();
        }

        protected override void OnStopRunning()
        {
            DestroyRuntime();
            WriteProduction(1f, 1f);
        }

        protected override void OnDestroy()
        {
            DestroyRuntime();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<MatchRunning>())
            {
                SystemAPI.GetSingletonBuffer<EcsBattleEvent>().Clear();
                WriteProduction(1f, 1f);
                return;
            }

            if (engine == null ||
                moraleWorld == null)
            {
                CreateRuntime();

                if (engine == null)
                    return;
            }

            RegisterBases();
            SynchronizeUnits();

            ConsumeCombatContacts();
            ConsumeBattleEvents();
            ConsumeCleanseRequests();

            engine.Tick(SystemAPI.Time.DeltaTime);

            WriteProduction(
                engine.ProductionMultiplier(Team.Left),
                engine.ProductionMultiplier(Team.Right));

            SynchronizeStatMultipliers();
            SynchronizeVeterans();
        }

        private void CreateRuntime()
        {
            if (engine != null)
                return;

            moraleWorld =
                new EcsMoraleWorld(EntityManager);

            MoraleSettings settings;

            if (SystemAPI.ManagedAPI.TryGetSingleton(
                    out MoraleSettingsData data) &&
                data.Value != null)
            {
                settings = data.Value.Snapshot();
            }
            else
            {
                UnityEngine.Debug.LogWarning(
                    "EcsMoraleRuntimeSystem: MoraleSettingsData not found in the battle SubScene, using defaults.");

                settings = new MoraleSettings();
            }

            engine =
                new MoraleEngine(
                    settings,
                    moraleWorld);

            seenUnits.Clear();
            removalBuffer.Clear();
        }

        private void DestroyRuntime()
        {
            if (engine != null)
            {
                engine.Stop();
                engine = null;
            }

            if (moraleWorld != null)
            {
                moraleWorld.Clear();
                moraleWorld = null;
            }

            seenUnits.Clear();
            removalBuffer.Clear();
        }

        private void RegisterBases()
        {
            using NativeArray<Entity> entities =
                baseQuery.ToEntityArray(
                    Unity.Collections.Allocator.Temp);

            for (int i = 0;
                 i < entities.Length;
                 i++)
            {
                Entity entity =
                    entities[i];

                if (!EntityManager.Exists(entity))
                    continue;

                TeamId team =
                    EntityManager.GetComponentData<TeamId>(
                        entity);

                moraleWorld.SetBase(
                    ToTeam(team.Value),
                    entity);
            }
        }

        private void SynchronizeUnits()
        {
            using NativeArray<Entity> entities =
                unitQuery.ToEntityArray(
                    Unity.Collections.Allocator.Temp);

            for (int i = 0;
                 i < entities.Length;
                 i++)
            {
                Entity entity =
                    entities[i];

                if (seenUnits.Contains(entity))
                    continue;

                EcsMoraleActor actor =
                    moraleWorld.Register(entity);

                if (actor == null)
                    continue;

                seenUnits.Add(entity);
                engine.Register(actor);
            }

            removalBuffer.Clear();

            foreach (Entity entity in seenUnits)
            {
                if (!EntityManager.Exists(entity))
                {
                    removalBuffer.Add(entity);
                }
            }

            for (int i = 0;
                 i < removalBuffer.Count;
                 i++)
            {
                Entity entity =
                    removalBuffer[i];

                if (moraleWorld.TryGetActor(
                        entity,
                        out EcsMoraleActor actor))
                {
                    engine.Unregister(actor);
                }

                moraleWorld.Unregister(entity);
                seenUnits.Remove(entity);
            }
        }

        private void SynchronizeStatMultipliers()
        {
            foreach (Entity entity in seenUnits)
            {
                if (!EntityManager.Exists(entity))
                    continue;

                if (!EntityManager.HasComponent<StatMultipliers>(
                        entity))
                {
                    continue;
                }

                if (!moraleWorld.TryGetActor(
                        entity,
                        out EcsMoraleActor actor))
                {
                    continue;
                }

                if (!actor.IsAlive)
                    continue;

                StatMultipliers multipliers =
                    EntityManager.GetComponentData<
                        StatMultipliers>(
                        entity);

                AbilityModifiers ability =
                    EntityManager.HasComponent<
                        AbilityModifiers>(entity)
                        ? EntityManager.GetComponentData<
                            AbilityModifiers>(entity)
                        : new AbilityModifiers
                        {
                            Damage = 1f,
                            AttackSpeed = 1f,
                            MoveSpeed = 1f,
                            DamageTaken = 1f
                        };

                multipliers.Damage =
                    engine.Multiplier(
                        actor,
                        Stat.Damage) *
                    ability.Damage;

                multipliers.AttackSpeed =
                    engine.Multiplier(
                        actor,
                        Stat.AttackSpeed) *
                    ability.AttackSpeed;

                multipliers.MoveSpeed =
                    engine.Multiplier(
                        actor,
                        Stat.MoveSpeed) *
                    ability.MoveSpeed;

                multipliers.DamageTaken =
                    engine.Multiplier(
                        actor,
                        Stat.DamageTaken) *
                    ability.DamageTaken;

                EntityManager.SetComponentData(
                    entity,
                    multipliers);
            }
        }

        private void SynchronizeVeterans()
        {
            foreach (Entity entity in seenUnits)
            {
                if (!EntityManager.Exists(entity))
                    continue;

                if (!EntityManager.HasComponent<Veteran>(
                        entity))
                {
                    continue;
                }

                if (!moraleWorld.TryGetActor(
                        entity,
                        out EcsMoraleActor actor))
                {
                    continue;
                }

                Veteran veteran =
                    EntityManager.GetComponentData<Veteran>(
                        entity);

                veteran.Battles =
                    actor.Veteran.BattlesSurvived;

                veteran.IsVeteran =
                    actor.Veteran.IsVeteran
                        ? (byte)1
                        : (byte)0;

                EntityManager.SetComponentData(
                    entity,
                    veteran);
            }
        }

        private void ConsumeBattleEvents()
        {
            Entity streamEntity =
                SystemAPI.GetSingletonEntity<
                    EcsBattleEventStream>();

            DynamicBuffer<EcsBattleEvent> events =
                EntityManager.GetBuffer<EcsBattleEvent>(
                    streamEntity);

            if (events.Length == 0)
                return;

            for (int i = 0;
                 i < events.Length;
                 i++)
            {
                EcsBattleEvent ecsEvent =
                    events[i];

                switch (ecsEvent.Kind)
                {
                    case EcsBattleEventKind.UnitKilled:
                        {
                            engine.Publish(
                                new BattleEvent(
                                    BattleEventKind.UnitKilled,
                                    ToTeam(ecsEvent.Side),
                                    ecsEvent.WasVeteran != 0));

                            break;
                        }

                    case EcsBattleEventKind.BaseDamaged:
                        {
                            engine.Publish(
                                new BattleEvent(
                                    BattleEventKind.BaseDamaged,
                                    ToTeam(ecsEvent.Side),
                                    false,
                                    ecsEvent.Amount,
                                    ecsEvent.Maximum));

                            break;
                        }
                }
            }

            events.Clear();
        }

        private void ConsumeCombatContacts()
        {
            foreach (Entity entity in seenUnits)
            {
                if (!EntityManager.Exists(entity))
                    continue;

                if (!EntityManager.HasComponent<MoraleContact>(
                        entity))
                {
                    continue;
                }

                if (!EntityManager.IsComponentEnabled<MoraleContact>(
                        entity))
                {
                    continue;
                }

                if (moraleWorld.TryGetActor(
                        entity,
                        out EcsMoraleActor actor))
                {
                    if (actor.IsAlive)
                        engine.Contact(actor);
                }

                EntityManager.SetComponentEnabled<MoraleContact>(
                    entity,
                    false);
            }
        }

        private void WriteProduction(float left, float right)
        {
            if (!EntityManager.Exists(productionEntity))
                return;

            EntityManager.SetComponentData(
                productionEntity,
                new TeamProduction
                {
                    Left = left,
                    Right = right
                });
        }

        public float ProductionMultiplier(byte side)
        {
            if (engine == null || !engine.Running)
                return 1f;

            return engine.ProductionMultiplier(ToTeam(side));
        }

        private void ConsumeCleanseRequests()
        {
            if (abilityRequestQuery.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> entities =
                abilityRequestQuery.ToEntityArray(Allocator.Temp);

            using NativeArray<AbilityRequest> requests =
                abilityRequestQuery.ToComponentDataArray<AbilityRequest>(Allocator.Temp);

            for (int i = 0; i < requests.Length; i++)
            {
                AbilityRequest request = requests[i];

                if (request.Kind != AbilityRequestKind.Cleansing)
                    continue;

                bool removed = engine.Cleanse(
                    ToTeam(request.Team),
                    request.Duration);

                if (!removed &&
                    SystemAPI.TryGetSingleton(out AbilityTimers timers))
                {
                    timers.Production = request.Duration;
                    SystemAPI.SetSingleton(timers);
                }

                EntityManager.DestroyEntity(entities[i]);
            }
        }

        public void CopyStatuses(
            byte side,
            List<MoraleStatus> destination)
        {
            if (destination == null)
                return;

            if (engine == null || !engine.Running)
            {
                destination.Clear();
                return;
            }

            engine.CopyStatuses(
                ToTeam(side),
                destination);
        }

        private static Team ToTeam(byte value)
        {
            return value == 0
                ? Team.Left
                : Team.Right;
        }
    }
}