using TheReckoning.ECS.Data;
using TheReckoning.ECS.Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ProjectileSystem))]
    public partial struct TurretFireSystem : ISystem
    {
        private EntityQuery unitQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            unitQuery = SystemAPI.QueryBuilder()
                .WithAll<UnitTag, TeamId, LocalTransform, Health>()
                .WithDisabled<Dead>()
                .Build();

            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<TurretState>();
            state.RequireForUpdate<TurretCatalogElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            DynamicBuffer<TurretCatalogElement> catalog =
                SystemAPI.GetSingletonBuffer<TurretCatalogElement>(true);

            using NativeArray<Entity> units =
                unitQuery.ToEntityArray(Allocator.Temp);

            using NativeArray<TeamId> teams =
                unitQuery.ToComponentDataArray<TeamId>(Allocator.Temp);

            using NativeArray<LocalTransform> transforms =
                unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            using NativeArray<Health> health =
                unitQuery.ToComponentDataArray<Health>(Allocator.Temp);

            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (RefRW<TurretState> turret in
                     SystemAPI.Query<RefRW<TurretState>>())
            {
                ref TurretState data = ref turret.ValueRW;

                data.Cooldown = math.max(0f, data.Cooldown - deltaTime);
                if (data.Cooldown > 0f)
                    continue;

                if (data.TurretId < 0 || data.TurretId >= catalog.Length)
                    continue;

                TurretCatalogElement entry = catalog[data.TurretId];
                if (!entry.Stats.IsCreated)
                    continue;

                ref TurretStatsBlob stats = ref entry.Stats.Value;

                int targetIndex = FindTarget(
                    data, stats.Range, teams, transforms, health);

                if (targetIndex < 0)
                    continue;

                LaunchProjectile(
                    ref commands,
                    data,
                    ref stats,
                    entry.ProjectileViewId,
                    units[targetIndex],
                    transforms[targetIndex].Position);

                data.Cooldown = stats.AttackInterval;
            }

            commands.Playback(state.EntityManager);
            commands.Dispose();
        }

        private static int FindTarget(
            in TurretState turret,
            float range,
            NativeArray<TeamId> teams,
            NativeArray<LocalTransform> transforms,
            NativeArray<Health> health)
        {
            float direction = turret.TeamId == 0 ? 1f : -1f;
            byte enemyTeam = turret.TeamId == 0 ? (byte)1 : (byte)0;

            int nearest = -1;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i].Value != enemyTeam || health[i].Current <= 0f)
                    continue;

                float distance =
                    (transforms[i].Position.x - turret.Muzzle.x) * direction;

                if (distance < 0f || distance > range || distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearest = i;
            }

            return nearest;
        }

        private static void LaunchProjectile(
            ref EntityCommandBuffer commands,
            in TurretState turret,
            ref TurretStatsBlob stats,
            int viewId,
            Entity target,
            float3 targetPosition)
        {
            float3 start = turret.Muzzle;
            float3 destination = targetPosition;
            destination.y += stats.TargetHeight;
            destination.z = start.z;

            float duration = math.max(
                0.02f,
                math.distance(start.xy, destination.xy) / stats.ProjectileSpeed);

            quaternion rotation = stats.ArcHeight > 0f
                ? quaternion.identity
                : ProjectileSystem.FacingRotation(destination - start);

            Entity projectile = commands.CreateEntity();

            commands.AddComponent(projectile, new Projectile
            {
                Target = target,
                AttackerTeam = turret.TeamId,
                Start = start,
                Destination = destination,
                Duration = duration,
                Elapsed = 0f,
                Damage = stats.Damage,
                ArcHeight = stats.ArcHeight,
                SplashRadius = stats.SplashRadius,
                TargetHeight = stats.TargetHeight
            });

            commands.AddComponent(
                projectile,
                LocalTransform.FromPositionRotation(start, rotation));

            commands.AddComponent(
                projectile,
                new LocalToWorld { Value = float4x4.TRS(start, rotation, 1f) });

            if (viewId >= 0)
                commands.AddComponent(projectile, new ViewPrefab { Id = viewId });
        }
    }
}
