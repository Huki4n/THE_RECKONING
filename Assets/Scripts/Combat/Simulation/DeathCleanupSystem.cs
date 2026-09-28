using Unity.Collections;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DeathInitializationSystem))]
    public partial struct DeathCleanupSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime =
                SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb =
                new EntityCommandBuffer(Allocator.Temp);

            foreach ((
                RefRW<DeathTimer> timer,
                Entity entity)
                in SystemAPI
                    .Query<RefRW<DeathTimer>>()
                    .WithAll<Dead>()
                    .WithEntityAccess())
            {
                timer.ValueRW.Remaining -= deltaTime;

                if (timer.ValueRO.Remaining > 0f)
                    continue;

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}