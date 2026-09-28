using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS.Data
{
    public struct UnitPrefabElement : IBufferElementData
    {
        public Entity Prefab;
        public BlobAssetReference<UnitStatsBlob> Stats;

        public int LeftViewId;
        public int RightViewId;
    }

    public struct TurretCatalogElement : IBufferElementData
    {
        public BlobAssetReference<TurretStatsBlob> Stats;
        public int ProjectileViewId;
    }

    public struct WaveScenario : IComponentData
    {
        public BlobAssetReference<WaveScenarioBlob> Value;
    }

    public struct MatchConfig : IComponentData
    {
        public float3 LeftSpawn;
        public float3 RightSpawn;
        public int MaxUnitsPerTeam;
        public float SiegeRadius;
        public float SiegeSpawnDelay;
    }
}