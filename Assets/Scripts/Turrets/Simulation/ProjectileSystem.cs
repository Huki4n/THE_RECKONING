using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(DamageSystem))]
    public partial struct ProjectileSystem : ISystem
    {
        private struct UnitSnapshot
        {
            public byte Team;
            public float3 Position;
            public float Radius;
        }

        private struct Hit
        {
            public Entity Target;
            public byte AttackerTeam;
            public float X;
            public float Damage;
            public float SplashRadius;
        }

        private EntityQuery unitQuery;
        private EntityQuery projectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            unitQuery = SystemAPI.QueryBuilder()
                .WithAll<UnitTag, TeamId, LocalTransform, BodyRadius, Health>()
                .WithDisabled<Dead>()
                .Build();

            projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile>()
                .Build();

            state.RequireForUpdate<Projectile>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<MatchRunning>())
            {
                state.EntityManager.DestroyEntity(projectileQuery);
                return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            using NativeHashMap<Entity, UnitSnapshot> units =
                SnapshotUnits();

            var hits = new NativeList<Hit>(Allocator.Temp);
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (projectileRef, transformRef, entity) in SystemAPI
                         .Query<RefRW<Projectile>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                ref Projectile projectile = ref projectileRef.ValueRW;
                byte enemyTeam = projectile.AttackerTeam == 0 ? (byte)1 : (byte)0;

                bool direct = projectile.ArcHeight <= 0f;

                if (direct &&
                    units.TryGetValue(projectile.Target, out UnitSnapshot target) &&
                    target.Team == enemyTeam)
                {
                    float3 destination = target.Position;
                    destination.y += projectile.TargetHeight;
                    destination.z = projectile.Start.z;
                    projectile.Destination = destination;
                }

                projectile.Elapsed += deltaTime;
                float t = math.saturate(projectile.Elapsed / projectile.Duration);

                float3 position = math.lerp(projectile.Start, projectile.Destination, t);
                position.y += 4f * projectile.ArcHeight * t * (1f - t);

                ref LocalTransform transform = ref transformRef.ValueRW;
                transform.Position = position;

                if (direct)
                    transform.Rotation = FacingRotation(projectile.Destination - projectile.Start);

                if (t < 1f)
                    continue;

                hits.Add(new Hit
                {
                    Target = projectile.Target,
                    AttackerTeam = projectile.AttackerTeam,
                    X = projectile.Destination.x,
                    Damage = projectile.Damage,
                    SplashRadius = projectile.SplashRadius
                });

                commands.DestroyEntity(entity);
            }

            BufferLookup<DamageEvent> damageLookup =
                SystemAPI.GetBufferLookup<DamageEvent>();

            for (int i = 0; i < hits.Length; i++)
                ApplyHit(hits[i], units, ref damageLookup);

            commands.Playback(state.EntityManager);
            commands.Dispose();
            hits.Dispose();
        }

        public static quaternion FacingRotation(float3 direction)
        {
            if (math.lengthsq(direction.xy) <= 0.0001f)
                return quaternion.identity;

            return quaternion.RotateZ(math.atan2(direction.y, direction.x));
        }

        private NativeHashMap<Entity, UnitSnapshot> SnapshotUnits()
        {
            using NativeArray<Entity> entities =
                unitQuery.ToEntityArray(Allocator.Temp);

            using NativeArray<TeamId> teams =
                unitQuery.ToComponentDataArray<TeamId>(Allocator.Temp);

            using NativeArray<LocalTransform> transforms =
                unitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            using NativeArray<BodyRadius> radii =
                unitQuery.ToComponentDataArray<BodyRadius>(Allocator.Temp);

            using NativeArray<Health> health =
                unitQuery.ToComponentDataArray<Health>(Allocator.Temp);

            var units = new NativeHashMap<Entity, UnitSnapshot>(
                entities.Length, Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (health[i].Current <= 0f)
                    continue;

                units.Add(entities[i], new UnitSnapshot
                {
                    Team = teams[i].Value,
                    Position = transforms[i].Position,
                    Radius = radii[i].Value
                });
            }

            return units;
        }

        private static void ApplyHit(
            in Hit hit,
            NativeHashMap<Entity, UnitSnapshot> units,
            ref BufferLookup<DamageEvent> damageLookup)
        {
            byte enemyTeam = hit.AttackerTeam == 0 ? (byte)1 : (byte)0;

            if (hit.SplashRadius > 0f)
            {
                foreach (KVPair<Entity, UnitSnapshot> pair in units)
                {
                    UnitSnapshot unit = pair.Value;

                    if (unit.Team == enemyTeam &&
                        math.abs(unit.Position.x - hit.X) <= hit.SplashRadius + unit.Radius)
                    {
                        AddDamage(pair.Key, hit, ref damageLookup);
                    }
                }

                return;
            }

            if (units.TryGetValue(hit.Target, out UnitSnapshot target) &&
                target.Team == enemyTeam &&
                math.abs(target.Position.x - hit.X) <= target.Radius + 0.1f)
            {
                AddDamage(hit.Target, hit, ref damageLookup);
            }
        }

        private static void AddDamage(
            Entity victim,
            in Hit hit,
            ref BufferLookup<DamageEvent> damageLookup)
        {
            if (!damageLookup.HasBuffer(victim))
                return;

            damageLookup[victim].Add(new DamageEvent
            {
                Source = Entity.Null,
                Amount = hit.Damage,
                AttackerTeam = hit.AttackerTeam,
                NonLethal = false
            });
        }
    }
}
