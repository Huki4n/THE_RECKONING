using TheReckoning.ECS.Data;
using TheReckoning.ECS.Presentation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace TheReckoning.ECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UnitSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitSpawnRequest>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<SpawnGate>();
            state.RequireForUpdate<UnitPrefabElement>();
        }

        public void OnUpdate(ref SystemState state)
        {
            MatchConfig matchConfig =
                SystemAPI.GetSingleton<MatchConfig>();

            SpawnGate gate =
                SystemAPI.GetSingleton<SpawnGate>();

            double now =
                SystemAPI.Time.ElapsedTime;

            bool leftSieged = false;
            bool rightSieged = false;

            int leftUnitCount = 0;
            int rightUnitCount = 0;

            NativeList<float2> leftBodies =
                new NativeList<float2>(Allocator.Temp);

            NativeList<float2> rightBodies =
                new NativeList<float2>(Allocator.Temp);

            foreach ((
                RefRO<TeamId> team,
                RefRO<LocalTransform> transform,
                RefRO<BodyRadius> radius,
                EnabledRefRO<Dead> dead)
                in SystemAPI
                    .Query<
                        RefRO<TeamId>,
                        RefRO<LocalTransform>,
                        RefRO<BodyRadius>,
                        EnabledRefRO<Dead>>()
                    .WithAll<UnitTag>()
                    .WithOptions(
                        EntityQueryOptions
                            .IgnoreComponentEnabledState))
            {
                if (dead.ValueRO)
                    continue;

                float2 body =
                    new float2(
                        transform.ValueRO.Position.x,
                        radius.ValueRO.Value);

                if (team.ValueRO.Value == 0)
                {
                    leftUnitCount++;
                    leftBodies.Add(body);

                    if (math.abs(body.x - matchConfig.RightSpawn.x) <=
                        matchConfig.SiegeRadius)
                    {
                        rightSieged = true;
                    }
                }
                else if (team.ValueRO.Value == 1)
                {
                    rightUnitCount++;
                    rightBodies.Add(body);

                    if (math.abs(body.x - matchConfig.LeftSpawn.x) <=
                        matchConfig.SiegeRadius)
                    {
                        leftSieged = true;
                    }
                }
            }

            bool leftSpawned = false;
            bool rightSpawned = false;

            EntityQuery requestQuery =
                SystemAPI.QueryBuilder()
                    .WithAll<UnitSpawnRequest>()
                    .Build();

            NativeArray<Entity> requestEntities =
                requestQuery.ToEntityArray(
                    Allocator.Temp);

            NativeArray<UnitSpawnRequest> requestData =
                requestQuery
                    .ToComponentDataArray<UnitSpawnRequest>(
                        Allocator.Temp);

            for (int i = 0; i < requestEntities.Length; i++)
            {
                DynamicBuffer<UnitPrefabElement> catalog =
                    SystemAPI.GetSingletonBuffer
                        <UnitPrefabElement>(true);

                Entity requestEntity =
                    requestEntities[i];

                UnitSpawnRequest request =
                    requestData[i];

                if (request.TeamId > 1)
                {
                    Debug.LogError(
                        $"UnitSpawnSystem: invalid TeamId " +
                        $"{request.TeamId}. Expected 0 or 1.");

                    state.EntityManager.DestroyEntity(
                        requestEntity);

                    continue;
                }

                if (request.UnitId < 0 ||
                    request.UnitId >= catalog.Length)
                {
                    Debug.LogError(
                        $"UnitSpawnSystem: UnitId " +
                        $"{request.UnitId} is outside " +
                        $"UnitCatalog range " +
                        $"0..{catalog.Length - 1}.");

                    state.EntityManager.DestroyEntity(
                        requestEntity);

                    continue;
                }

                UnitPrefabElement entry =
                    catalog[request.UnitId];

                if (entry.Prefab == Entity.Null)
                {
                    Debug.LogError(
                        $"UnitSpawnSystem: UnitCatalog" +
                        $"[{request.UnitId}] contains " +
                        $"Entity.Null prefab.");

                    state.EntityManager.DestroyEntity(
                        requestEntity);

                    continue;
                }

                Entity prefab =
                    entry.Prefab;

                int viewId =
                    request.TeamId == 0
                        ? entry.LeftViewId
                        : entry.RightViewId;

                int currentUnitCount =
                    request.TeamId == 0
                        ? leftUnitCount
                        : rightUnitCount;

                if (currentUnitCount >=
                    matchConfig.MaxUnitsPerTeam)
                {
                    Debug.LogWarning(
                        $"UnitSpawnSystem: Team " +
                        $"{request.TeamId} reached unit cap " +
                        $"{matchConfig.MaxUnitsPerTeam}. " +
                        $"Spawn request for UnitId " +
                        $"{request.UnitId} was rejected.");

                    state.EntityManager.DestroyEntity(
                        requestEntity);

                    continue;
                }

                float3 spawnPosition =
                    request.TeamId == 0
                        ? matchConfig.LeftSpawn
                        : matchConfig.RightSpawn;

                bool teamSpawned =
                    request.TeamId == 0
                        ? leftSpawned
                        : rightSpawned;

                if (teamSpawned)
                    continue;

                float newRadius =
                    state.EntityManager.HasComponent<BodyRadius>(prefab)
                        ? state.EntityManager
                            .GetComponentData<BodyRadius>(prefab)
                            .Value
                        : 0f;

                bool sieged =
                    request.TeamId == 0
                        ? leftSieged
                        : rightSieged;

                if (IsSpawnOccupied(
                        request.TeamId == 0
                            ? leftBodies
                            : rightBodies,
                        spawnPosition.x,
                        newRadius))
                {
                    if (sieged)
                        DelayGate(ref gate, request.TeamId, now + matchConfig.SiegeSpawnDelay);

                    continue;
                }

                double readyAt =
                    request.TeamId == 0
                        ? gate.LeftReadyAt
                        : gate.RightReadyAt;

                if (sieged && now < readyAt)
                    continue;

                state.EntityManager.DestroyEntity(
                    requestEntity);

                SpawnUnit(
                    ref state,
                    prefab,
                    request.UnitId,
                    request.TeamId,
                    viewId,
                    spawnPosition);

                if (request.TeamId == 0)
                {
                    leftUnitCount++;
                    leftSpawned = true;
                }
                else
                {
                    rightUnitCount++;
                    rightSpawned = true;
                }
            }

            SystemAPI.SetSingleton(gate);

            requestEntities.Dispose();
            requestData.Dispose();
            leftBodies.Dispose();
            rightBodies.Dispose();
        }

        private static void DelayGate(ref SpawnGate gate, byte teamId, double readyAt)
        {
            if (teamId == 0)
                gate.LeftReadyAt = readyAt;
            else
                gate.RightReadyAt = readyAt;
        }

        private static bool IsSpawnOccupied(
            NativeList<float2> allies,
            float spawnX,
            float radius)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                float2 ally = allies[i];

                if (math.abs(ally.x - spawnX) < radius + ally.y)
                    return true;
            }

            return false;
        }

        private static void SpawnUnit(
            ref SystemState state,
            Entity prefab,
            int unitId,
            byte teamId,
            int viewId,
            float3 position)
        {
            Entity unit =
                state.EntityManager.Instantiate(
                    prefab);

#if UNITY_EDITOR
            state.EntityManager.SetName(
                unit,
                teamId == 0
                    ? $"Order_Unit_{unitId}"
                    : $"Cult_Unit_{unitId}");
#endif

            state.EntityManager.AddComponentData(
                unit,
                new TeamId
                {
                    Value = teamId
                });

            if (viewId >= 0)
            {
                state.EntityManager.AddComponentData(
                    unit,
                    new ViewPrefab
                    {
                        Id = viewId
                    });
            }
            else
            {
                Debug.LogWarning(
                    $"UnitSpawnSystem: UnitId {unitId}, " +
                    $"Team {teamId} has no valid ViewId.");
            }

            LocalTransform transform =
                state.EntityManager
                    .GetComponentData<LocalTransform>(
                        unit);

            transform.Position = position;
            transform.Rotation = quaternion.identity;
            transform.Scale = 1f;

            state.EntityManager.SetComponentData(
                unit,
                transform);

            UnitStatsRef statsRef =
                state.EntityManager
                    .GetComponentData<UnitStatsRef>(
                        unit);

            float maximumHealth =
                statsRef.Value.Value.MaximumHealth;

            state.EntityManager.SetComponentData(
                unit,
                new Health
                {
                    Current = maximumHealth,
                    Max = maximumHealth
                });

            state.EntityManager.SetComponentData(
                unit,
                new Target
                {
                    Value = Entity.Null
                });

            state.EntityManager.SetComponentData(
                unit,
                new AttackCooldown
                {
                    Remaining = 0f
                });

            state.EntityManager.SetComponentData(
                unit,
                new StatMultipliers
                {
                    Damage = 1f,
                    AttackSpeed = 1f,
                    MoveSpeed = 1f,
                    DamageTaken = 1f
                });

            state.EntityManager.SetComponentData(
                unit,
                new AbilityModifiers
                {
                    Damage = 1f,
                    AttackSpeed = 1f,
                    MoveSpeed = 1f,
                    DamageTaken = 1f
                });

            state.EntityManager.SetComponentData(
                unit,
                new AbilityBuffs());

            state.EntityManager.SetComponentData(
                unit,
                new Veteran
                {
                    Battles = 0,
                    IsVeteran = 0
                });

            state.EntityManager.SetComponentEnabled<Dead>(
                unit,
                false);

            if (state.EntityManager
                .HasComponent<ViewAttackEvent>(unit))
            {
                state.EntityManager
                    .SetComponentEnabled<ViewAttackEvent>(
                        unit,
                        false);
            }

            if (!state.EntityManager
                    .HasComponent<MoraleContact>(unit))
            {
                state.EntityManager
                    .AddComponent<MoraleContact>(unit);
            }

            state.EntityManager
                .SetComponentEnabled<MoraleContact>(
                    unit,
                    false);

            if (state.EntityManager
                .HasBuffer<DamageEvent>(unit))
            {
                DynamicBuffer<DamageEvent> damageBuffer =
                    state.EntityManager
                        .GetBuffer<DamageEvent>(unit);

                damageBuffer.Clear();
            }
        }
    }
}