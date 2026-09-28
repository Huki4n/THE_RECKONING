using Unity.Entities;
using UnityEngine;

namespace TheReckoning.ECS.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EntityViewAuthoring : MonoBehaviour
    {
        [SerializeField] private ViewCatalog catalog;
        [SerializeField] private GameObject viewPrefab;

        private sealed class Baker : Baker<EntityViewAuthoring>
        {
            public override void Bake(EntityViewAuthoring authoring)
            {
                DependsOn(authoring.catalog);
                if (authoring.catalog == null || authoring.viewPrefab == null)
                    return;

                int id = authoring.catalog.IndexOf(authoring.viewPrefab);
                if (id < 0)
                {
                    Debug.LogWarning(
                        $"{authoring.name}: view prefab '{authoring.viewPrefab.name}' is not in {authoring.catalog.name}.",
                        authoring);
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ViewPrefab { Id = id });
                AddComponent<ViewAttackEvent>(entity);
                SetComponentEnabled<ViewAttackEvent>(entity, false);
            }
        }
    }
}
