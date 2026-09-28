using TheReckoning.ECS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitTargetingSystem : ISystem
    {
        private EntityQuery unitQuery;
        private EntityQuery baseQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            unitQuery =
                SystemAPI.QueryBuilder()
                    .WithAll<
                        UnitTag,
                        TeamId,
                        LocalTransform,
                        Health>()
                    .WithDisabled<Dead>()
                    .Build();

            baseQuery =
                SystemAPI.QueryBuilder()
                    .WithAll<
                        BaseTag,
                        TeamId,
                        LocalTransform,
                        BodyRadius,
                        Health>()
                    .WithDisabled<Dead>()
                    .Build();

            state.RequireForUpdate<UnitTag>();
            state.RequireForUpdate<MatchRunning>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeArray<Entity> unitEntities =
                unitQuery.ToEntityArray(
                    Allocator.TempJob);

            NativeArray<TeamId> unitTeams =
                unitQuery.ToComponentDataArray<TeamId>(
                    Allocator.TempJob);

            NativeArray<LocalTransform> unitTransforms =
                unitQuery
                    .ToComponentDataArray<LocalTransform>(
                        Allocator.TempJob);

            NativeArray<Health> unitHealth =
                unitQuery
                    .ToComponentDataArray<Health>(
                        Allocator.TempJob);

            NativeArray<Entity> baseEntities =
                baseQuery.ToEntityArray(
                    Allocator.TempJob);

            NativeArray<TeamId> baseTeams =
                baseQuery.ToComponentDataArray<TeamId>(
                    Allocator.TempJob);

            NativeArray<LocalTransform> baseTransforms =
                baseQuery
                    .ToComponentDataArray<LocalTransform>(
                        Allocator.TempJob);

            NativeArray<BodyRadius> baseRadii =
                baseQuery
                    .ToComponentDataArray<BodyRadius>(
                        Allocator.TempJob);

            NativeArray<Health> baseHealth =
                baseQuery
                    .ToComponentDataArray<Health>(
                        Allocator.TempJob);

            var job =
                new TargetingJob
                {
                    UnitEntities =
                        unitEntities,

                    UnitTeams =
                        unitTeams,

                    UnitTransforms =
                        unitTransforms,

                    UnitHealth =
                        unitHealth,

                    BaseEntities =
                        baseEntities,

                    BaseTeams =
                        baseTeams,

                    BaseTransforms =
                        baseTransforms,

                    BaseRadii =
                        baseRadii,

                    BaseHealth =
                        baseHealth
                };

            state.Dependency =
                job.ScheduleParallel(
                    state.Dependency);

            state.Dependency =
                unitEntities.Dispose(
                    state.Dependency);

            state.Dependency =
                unitTeams.Dispose(
                    state.Dependency);

            state.Dependency =
                unitTransforms.Dispose(
                    state.Dependency);

            state.Dependency =
                unitHealth.Dispose(
                    state.Dependency);

            state.Dependency =
                baseEntities.Dispose(
                    state.Dependency);

            state.Dependency =
                baseTeams.Dispose(
                    state.Dependency);

            state.Dependency =
                baseTransforms.Dispose(
                    state.Dependency);

            state.Dependency =
                baseRadii.Dispose(
                    state.Dependency);

            state.Dependency =
                baseHealth.Dispose(
                    state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(UnitTag))]
        [WithDisabled(typeof(Dead))]
        private partial struct TargetingJob :
            IJobEntity
        {
            [ReadOnly]
            public NativeArray<Entity>
                UnitEntities;

            [ReadOnly]
            public NativeArray<TeamId>
                UnitTeams;

            [ReadOnly]
            public NativeArray<LocalTransform>
                UnitTransforms;

            [ReadOnly]
            public NativeArray<Health>
                UnitHealth;

            [ReadOnly]
            public NativeArray<Entity>
                BaseEntities;

            [ReadOnly]
            public NativeArray<TeamId>
                BaseTeams;

            [ReadOnly]
            public NativeArray<LocalTransform>
                BaseTransforms;

            [ReadOnly]
            public NativeArray<BodyRadius>
                BaseRadii;

            [ReadOnly]
            public NativeArray<Health>
                BaseHealth;

            private void Execute(
                Entity entity,
                ref Target target,
                in TeamId team,
                in LocalTransform transform,
                in BodyRadius bodyRadius,
                in UnitStatsRef statsRef)
            {
                float selfX =
                    transform.Position.x;

                float direction =
                    team.Value == 0
                        ? 1f
                        : -1f;

                int baseIndex =
                    FindNearestBase(
                        team.Value,
                        selfX,
                        direction);

                if (baseIndex >= 0)
                {
                    float baseGap =
                        (BaseTransforms[baseIndex].Position.x - selfX)
                        * direction
                        - bodyRadius.Value
                        - BaseRadii[baseIndex].Value;

                    if (baseGap <=
                        statsRef.Value.Value.AttackRange)
                    {
                        target.Value =
                            BaseEntities[baseIndex];

                        return;
                    }
                }

                Entity bestUnit =
                    Entity.Null;

                float bestUnitDistance =
                    float.MaxValue;

                for (int i = 0;
                     i < UnitEntities.Length;
                     i++)
                {
                    Entity candidate =
                        UnitEntities[i];

                    if (candidate == entity)
                        continue;

                    if (UnitTeams[i].Value ==
                        team.Value)
                    {
                        continue;
                    }

                    if (UnitHealth[i].Current <= 0f)
                        continue;

                    float candidateX =
                        UnitTransforms[i]
                            .Position.x;

                    float forwardDistance =
                        (candidateX - selfX)
                        * direction;

                    if (forwardDistance <= 0f)
                        continue;

                    if (forwardDistance <
                        bestUnitDistance)
                    {
                        bestUnitDistance =
                            forwardDistance;

                        bestUnit =
                            candidate;
                    }
                }

                if (bestUnit != Entity.Null)
                {
                    target.Value =
                        bestUnit;

                    return;
                }

                target.Value =
                    baseIndex >= 0
                        ? BaseEntities[baseIndex]
                        : Entity.Null;
            }

            private int FindNearestBase(
                byte team,
                float selfX,
                float direction)
            {
                int bestBase = -1;

                float bestBaseDistance =
                    float.MaxValue;

                for (int i = 0;
                     i < BaseEntities.Length;
                     i++)
                {
                    if (BaseTeams[i].Value ==
                        team)
                    {
                        continue;
                    }

                    if (BaseHealth[i].Current <= 0f)
                        continue;

                    float candidateX =
                        BaseTransforms[i]
                            .Position.x;

                    float forwardDistance =
                        (candidateX - selfX)
                        * direction;

                    if (forwardDistance <= 0f)
                        continue;

                    if (forwardDistance <
                        bestBaseDistance)
                    {
                        bestBaseDistance =
                            forwardDistance;

                        bestBase = i;
                    }
                }

                return bestBase;
            }
        }
    }
}