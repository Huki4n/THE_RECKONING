using Unity.Burst;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EcsBattleEventBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQuery query =
                state.GetEntityQuery(
                    ComponentType.ReadOnly<EcsBattleEventStream>());

            if (!query.IsEmptyIgnoreFilter)
                return;

            Entity entity =
                state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(
                entity,
                new EcsBattleEventStream());

            state.EntityManager.AddBuffer<EcsBattleEvent>(
                entity);

#if UNITY_EDITOR
            state.EntityManager.SetName(
                entity,
                "ECS Battle Event Stream");
#endif
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }
    }
}