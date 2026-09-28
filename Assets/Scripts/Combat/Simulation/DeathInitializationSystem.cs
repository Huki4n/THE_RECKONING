using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial struct DeathInitializationSystem : ISystem
    {
        private const float CleanupDelay = 0.15f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb =
                new EntityCommandBuffer(Allocator.TempJob);

            foreach ((
                RefRO<Health> health,
                Entity entity)
                in SystemAPI
                    .Query<RefRO<Health>>()
                    .WithAll<Dead>()
                    .WithNone<DeathTimer, BaseTag>()
                    .WithEntityAccess())
            {
                ecb.AddComponent(
                    entity,
                    new DeathTimer
                    {
                        Remaining = CleanupDelay
                    });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}