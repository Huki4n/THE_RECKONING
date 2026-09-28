using TheReckoning.ECS.Data;
using TheReckoning.ECS.Presentation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class TurretCatalogAuthoring : MonoBehaviour
    {
        [SerializeField] private ViewCatalog viewCatalog;
        [SerializeField] private TurretData[] turrets = new TurretData[0];

        private sealed class Baker : Baker<TurretCatalogAuthoring>
        {
            public override void Bake(TurretCatalogAuthoring authoring)
            {
                DependsOn(authoring.viewCatalog);

                Entity catalog = GetEntity(TransformUsageFlags.None);
                DynamicBuffer<TurretCatalogElement> elements = AddBuffer<TurretCatalogElement>(catalog);
                if (authoring.turrets == null)
                    return;

                for (int i = 0; i < authoring.turrets.Length; i++)
                {
                    TurretData data = authoring.turrets[i];
                    DependsOn(data);
                    if (data == null)
                    {
                        Debug.LogWarning($"{authoring.name}: turret entry is empty.", authoring);
                        elements.Add(new TurretCatalogElement { ProjectileViewId = -1 });
                        continue;
                    }

                    DependsOn(data.ProjectileViewPrefab);

                    if (data.EcsTurretId != i)
                        Debug.LogWarning($"{authoring.name}: {data.name} has ECS Turret Id {data.EcsTurretId}, expected {i}.", authoring);

                    int projectileViewId = authoring.viewCatalog != null
                        ? authoring.viewCatalog.IndexOf(data.ProjectileViewPrefab)
                        : -1;

                    if (projectileViewId < 0)
                        Debug.LogWarning($"{authoring.name}: {data.name} has no Projectile View Prefab listed in ViewCatalog.", authoring);

                    BlobAssetReference<TurretStatsBlob> stats = BuildStats(data);
                    AddBlobAsset(ref stats, out _);
                    elements.Add(new TurretCatalogElement
                    {
                        Stats = stats,
                        ProjectileViewId = projectileViewId
                    });
                }
            }

            private static BlobAssetReference<TurretStatsBlob> BuildStats(TurretData data)
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref TurretStatsBlob root = ref builder.ConstructRoot<TurretStatsBlob>();
                root.Cost = data.Cost;
                root.Damage = data.Damage;
                root.AttackInterval = data.AttackInterval;
                root.Range = data.Range;
                root.ProjectileSpeed = data.ProjectileSpeed;
                root.ArcHeight = data.ArcHeight;
                root.SplashRadius = data.SplashRadius;
                root.TargetHeight = data.TargetHeight;

                BlobAssetReference<TurretStatsBlob> blob = builder.CreateBlobAssetReference<TurretStatsBlob>(Allocator.Persistent);
                builder.Dispose();
                return blob;
            }
        }
    }
}
