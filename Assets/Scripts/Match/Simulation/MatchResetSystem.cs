using TheReckoning.ECS.Data;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(UnitSpawnSystem))]
    public partial struct MatchResetSystem : ISystem
    {
        private EntityQuery spawnRequestQuery;
        private EntityQuery turretQuery;
        private EntityQuery projectileQuery;
        private EntityQuery abilityRequestQuery;
        private bool matchLoaded;

        public void OnCreate(ref SystemState state)
        {
            spawnRequestQuery = SystemAPI.QueryBuilder()
                .WithAll<UnitSpawnRequest>()
                .Build();

            turretQuery = SystemAPI.QueryBuilder()
                .WithAll<TurretState>()
                .Build();

            projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile>()
                .Build();

            abilityRequestQuery = SystemAPI.QueryBuilder()
                .WithAll<AbilityRequest>()
                .Build();

            state.RequireForUpdate<EcsBattleEventStream>();
        }

        public void OnUpdate(ref SystemState state)
        {
            bool loaded = SystemAPI.HasSingleton<MatchConfig>();

            if (loaded == matchLoaded)
                return;

            matchLoaded = loaded;

            SystemAPI.GetSingletonBuffer<EcsBattleEvent>().Clear();
            state.EntityManager.DestroyEntity(spawnRequestQuery);
            state.EntityManager.DestroyEntity(abilityRequestQuery);

            if (SystemAPI.HasSingleton<PendingKillReward>())
                SystemAPI.SetSingleton(new PendingKillReward());

            if (SystemAPI.HasSingleton<AbilityTimers>())
                SystemAPI.SetSingleton(new AbilityTimers());

            if (SystemAPI.HasSingleton<CultAbilityState>())
            {
                SystemAPI.SetSingleton(new CultAbilityState());
                SystemAPI.GetSingletonBuffer<OrderDeathTime>().Clear();
            }

            if (!loaded)
            {
                state.EntityManager.DestroyEntity(turretQuery);
                state.EntityManager.DestroyEntity(projectileQuery);
            }
        }
    }
}
