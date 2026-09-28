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
    [UpdateAfter(typeof(UnitTargetingSystem))]
    public partial struct UnitMovementSystem : ISystem
    {
        private EntityQuery combatantQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            combatantQuery = SystemAPI.QueryBuilder()
                .WithAll<
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
            float deltaTime =
                SystemAPI.Time.DeltaTime;

            NativeArray<Entity> combatantEntities =
                combatantQuery.ToEntityArray(
                    Allocator.TempJob);

            NativeArray<TeamId> combatantTeams =
                combatantQuery.ToComponentDataArray<TeamId>(
                    Allocator.TempJob);

            NativeArray<LocalTransform> combatantTransforms =
                combatantQuery
                    .ToComponentDataArray<LocalTransform>(
                        Allocator.TempJob);

            NativeArray<BodyRadius> combatantRadii =
                combatantQuery
                    .ToComponentDataArray<BodyRadius>(
                        Allocator.TempJob);

            NativeArray<Health> combatantHealth =
                combatantQuery
                    .ToComponentDataArray<Health>(
                        Allocator.TempJob);

            ComponentLookup<Dead> deadLookup =
                SystemAPI.GetComponentLookup<Dead>(true);

            ComponentLookup<BaseTag> baseLookup =
                SystemAPI.GetComponentLookup<BaseTag>(true);

            var job = new MovementJob
            {
                DeltaTime = deltaTime,

                CombatantEntities = combatantEntities,
                CombatantTeams = combatantTeams,
                CombatantTransforms = combatantTransforms,
                CombatantRadii = combatantRadii,
                CombatantHealth = combatantHealth,

                DeadLookup = deadLookup,
                BaseLookup = baseLookup
            };

            state.Dependency =
                job.ScheduleParallel(state.Dependency);

            state.Dependency =
                combatantEntities.Dispose(
                    state.Dependency);

            state.Dependency =
                combatantTeams.Dispose(
                    state.Dependency);

            state.Dependency =
                combatantTransforms.Dispose(
                    state.Dependency);

            state.Dependency =
                combatantRadii.Dispose(
                    state.Dependency);

            state.Dependency =
                combatantHealth.Dispose(
                    state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(UnitTag))]
        [WithDisabled(typeof(Dead))]
        private partial struct MovementJob : IJobEntity
        {
            public float DeltaTime;

            [ReadOnly]
            public NativeArray<Entity> CombatantEntities;

            [ReadOnly]
            public NativeArray<TeamId> CombatantTeams;

            [ReadOnly]
            public ComponentLookup<Dead> DeadLookup;

            [ReadOnly]
            public ComponentLookup<BaseTag> BaseLookup;

            [ReadOnly]
            public NativeArray<LocalTransform>
                CombatantTransforms;

            [ReadOnly]
            public NativeArray<BodyRadius>
                CombatantRadii;

            [ReadOnly]
            public NativeArray<Health>
                CombatantHealth;

            private void Execute(
                Entity entity,
                ref LocalTransform transform,
                in TeamId team,
                in BodyRadius bodyRadius,
                in Target target,
                in UnitStatsRef statsRef,
                in StatMultipliers multipliers)
            {
                float direction =
                    team.Value == 0
                        ? 1f
                        : -1f;

                float selfX =
                    transform.Position.x;

                float moveSpeed =
                    statsRef.Value.Value.MoveSpeed
                    * math.max(
                        0f,
                        multipliers.MoveSpeed);

                if (moveSpeed <= 0f)
                    return;

                float desiredMove =
                    moveSpeed * DeltaTime;

                if (desiredMove <= 0f)
                    return;

                Entity targetEntity =
                    target.Value;

                if (targetEntity != Entity.Null)
                {
                    int targetIndex =
                        FindCombatantIndex(
                            targetEntity);

                    if (targetIndex >= 0 &&
                         CombatantHealth[targetIndex].Current > 0f &&
                         (!DeadLookup.HasComponent(targetEntity) ||
                          !DeadLookup.IsComponentEnabled(targetEntity)))
                    {
                        float targetX =
                            CombatantTransforms[targetIndex]
                                .Position.x;

                        float forwardDistance =
                            (targetX - selfX)
                            * direction;

                        if (forwardDistance > 0f)
                        {
                            float targetRadius =
                                CombatantRadii[targetIndex]
                                    .Value;

                            float targetGap =
                                forwardDistance
                                - bodyRadius.Value
                                - targetRadius;

                            float attackRange =
                                statsRef.Value.Value
                                    .AttackRange;

                            if (targetGap <= attackRange)
                                return;

                            float distanceUntilAttackRange =
                                targetGap
                                - attackRange;

                            desiredMove =
                                math.min(
                                    desiredMove,
                                    distanceUntilAttackRange);
                        }
                    }
                }

                float freeDistance =
                    CalculateFreeDistanceAhead(
                        entity,
                        team.Value,
                        selfX,
                        bodyRadius.Value);

                desiredMove =
                    math.min(
                        desiredMove,
                        freeDistance);

                if (desiredMove <= 0f)
                    return;

                transform.Position.x +=
                    direction * desiredMove;
            }

            private int FindCombatantIndex(
                Entity targetEntity)
            {
                for (int i = 0;
                     i < CombatantEntities.Length;
                     i++)
                {
                    if (CombatantEntities[i] ==
                        targetEntity)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private float CalculateFreeDistanceAhead(
                Entity self,
                byte team,
                float selfX,
                float selfRadius)
            {
                float direction =
                    team == 0
                        ? 1f
                        : -1f;

                float freeDistance =
                    float.MaxValue;

                for (int i = 0;
                     i < CombatantEntities.Length;
                     i++)
                {
                    Entity other =
                        CombatantEntities[i];

                    if (other == self)
                        continue;

                    if (DeadLookup.HasComponent(other) && DeadLookup.IsComponentEnabled(other))
                    {
                        continue;
                    }

                    if (CombatantHealth[i].Current <= 0f)
                        continue;

                    if (CombatantTeams[i].Value != team)
                        continue;

                    if (BaseLookup.HasComponent(other))
                        continue;

                    float otherX =
                        CombatantTransforms[i]
                            .Position.x;

                    float forwardDistance =
                        (otherX - selfX)
                        * direction;

                    if (forwardDistance < 0f)
                        continue;

                    if (forwardDistance == 0f &&
                        other.Index > self.Index)
                    {
                        continue;
                    }

                    float gap =
                        forwardDistance
                        - selfRadius
                        - CombatantRadii[i].Value;

                    freeDistance =
                        math.min(
                            freeDistance,
                            math.max(0f, gap));
                }

                return freeDistance;
            }
        }
    }
}