using TheReckoning.ECS.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamageEvent>();
            state.RequireForUpdate<EcsBattleEventStream>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity eventStreamEntity =
                SystemAPI
                    .GetSingletonEntity<EcsBattleEventStream>();

            DynamicBuffer<EcsBattleEvent> battleEvents =
                SystemAPI.GetBuffer<EcsBattleEvent>(
                    eventStreamEntity);

            var job = new DamageJob
            {
                BattleEvents =
                    battleEvents,

                BaseLookup =
                    SystemAPI
                        .GetComponentLookup<BaseTag>(
                            true),

                UnitLookup =
                    SystemAPI
                        .GetComponentLookup<UnitTag>(
                            true),

                VeteranLookup =
                    SystemAPI
                        .GetComponentLookup<Veteran>(
                            true),

                UnitStatsLookup =
                    SystemAPI
                        .GetComponentLookup<UnitStatsRef>(
                            true)
            };

            state.Dependency =
                job.Schedule(
                    state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(Dead))]
        private partial struct DamageJob :
            IJobEntity
        {
            public DynamicBuffer<EcsBattleEvent>
                BattleEvents;

            [Unity.Collections.ReadOnly]
            public ComponentLookup<BaseTag>
                BaseLookup;

            [Unity.Collections.ReadOnly]
            public ComponentLookup<UnitTag>
                UnitLookup;

            [Unity.Collections.ReadOnly]
            public ComponentLookup<Veteran>
                VeteranLookup;

            [Unity.Collections.ReadOnly]
            public ComponentLookup<UnitStatsRef>
                UnitStatsLookup;

            private void Execute(
                Entity entity,
                ref Health health,
                ref DynamicBuffer<DamageEvent>
                    damageEvents,
                in TeamId team,
                in StatMultipliers multipliers,
                EnabledRefRW<Dead> dead)
            {
                if (damageEvents.Length == 0)
                    return;

                float maximumHealth =
                    health.Max;

                bool isBase =
                    BaseLookup.HasComponent(
                        entity);

                bool isUnit =
                    UnitLookup.HasComponent(
                        entity);

                for (int i = 0;
                     i < damageEvents.Length;
                     i++)
                {
                    DamageEvent damageEvent =
                        damageEvents[i];

                    if (damageEvent.AttackerTeam ==
                        team.Value)
                    {
                        continue;
                    }

                    float damage =
                        math.max(
                            0f,
                            damageEvent.Amount);

                    if (damage <= 0f)
                        continue;

                    damage *=
                        math.max(
                            0f,
                            multipliers.DamageTaken);

                    if (damage <= 0f)
                        continue;

                    float healthBefore =
                        health.Current;

                    if (damageEvent.NonLethal)
                    {
                        health.Current =
                            math.max(
                                1f,
                                health.Current - damage);
                    }
                    else
                    {
                        health.Current =
                            math.max(
                                0f,
                                health.Current - damage);
                    }

                    float actualDamage =
                        math.max(
                            0f,
                            healthBefore -
                            health.Current);

                    if (isBase &&
                        actualDamage > 0f)
                    {
                        BattleEvents.Add(
                            new EcsBattleEvent
                            {
                                Kind =
                                    EcsBattleEventKind
                                        .BaseDamaged,

                                Side =
                                    team.Value,

                                WasVeteran = 0,

                                Amount =
                                    actualDamage,

                                Maximum =
                                    maximumHealth
                            });
                    }

                    if (health.Current <= 0f)
                    {
                        health.Current = 0f;

                        if (isUnit)
                        {
                            byte wasVeteran = 0;

                            if (VeteranLookup
                                .TryGetComponent(
                                    entity,
                                    out Veteran veteran))
                            {
                                wasVeteran =
                                    veteran.IsVeteran;
                            }

                            int killReward = 0;

                            if (team.Value == 1 &&
                                UnitStatsLookup.TryGetComponent(
                                    entity,
                                    out UnitStatsRef statsRef) &&
                                statsRef.Value.IsCreated)
                            {
                                killReward = math.max(
                                    0,
                                    statsRef.Value.Value.KillReward);
                            }

                            BattleEvents.Add(
                                new EcsBattleEvent
                                {
                                    Kind =
                                        EcsBattleEventKind
                                            .UnitKilled,

                                    Side =
                                        team.Value,

                                    WasVeteran =
                                        wasVeteran,

                                    Amount = 0f,
                                    Maximum = 0f,
                                    KillReward = killReward
                                });
                        }

                        dead.ValueRW = true;

                        break;
                    }
                }

                damageEvents.Clear();
            }
        }
    }
}