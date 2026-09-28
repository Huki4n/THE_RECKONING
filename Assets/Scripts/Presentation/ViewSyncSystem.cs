using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace TheReckoning.ECS.Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ViewSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            ViewRegistry registry = ViewRegistry.Active;
            var commands = new EntityCommandBuffer(Allocator.Temp);

            ReleaseDestroyed(registry, commands);
            if (registry != null)
                AcquireNew(registry, commands);

            commands.Playback(EntityManager);
            commands.Dispose();

            if (registry == null)
                return;

            SyncPoses(registry);
            ForwardAttacks(registry);
        }

        private void ReleaseDestroyed(ViewRegistry registry, EntityCommandBuffer commands)
        {
            foreach (var (handle, entity) in SystemAPI.Query<RefRO<ViewHandle>>()
                .WithNone<ViewPrefab>()
                .WithEntityAccess())
            {
                if (registry != null)
                    registry.Release(handle.ValueRO);
                commands.RemoveComponent<ViewHandle>(entity);
            }
        }

        private void AcquireNew(ViewRegistry registry, EntityCommandBuffer commands)
        {
            foreach (var (prefab, entity) in SystemAPI.Query<RefRO<ViewPrefab>>()
                .WithNone<ViewHandle>()
                .WithEntityAccess())
            {
                if (registry.TryAcquire(prefab.ValueRO.Id, entity, EntityManager, out ViewHandle handle))
                    commands.AddComponent(entity, handle);
            }
        }

        private void SyncPoses(ViewRegistry registry)
        {
            foreach (var (handle, prefab, localToWorld, entity) in SystemAPI
                .Query<RefRW<ViewHandle>, RefRO<ViewPrefab>, RefRO<LocalToWorld>>()
                .WithEntityAccess())
            {
                if (handle.ValueRO.Owner != registry.Owner &&
                    registry.TryAcquire(prefab.ValueRO.Id, entity, EntityManager, out ViewHandle rebound))
                    handle.ValueRW = rebound;

                registry.SetPose(handle.ValueRO, localToWorld.ValueRO.Position, localToWorld.ValueRO.Rotation);
            }
        }

        private void ForwardAttacks(ViewRegistry registry)
        {
            foreach (var (handle, attack) in SystemAPI
                .Query<RefRO<ViewHandle>, EnabledRefRW<ViewAttackEvent>>())
            {
                if (!attack.ValueRO)
                    continue;

                registry.PlayAttack(handle.ValueRO);
                attack.ValueRW = false;
            }
        }
    }
}
