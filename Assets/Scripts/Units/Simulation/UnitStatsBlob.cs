using Unity.Entities;

namespace TheReckoning.ECS.Data
{
    public struct UnitStatsBlob
    {
        public int Cost;
        public int KillReward;
        public float MaximumHealth;
        public float Damage;
        public float AttackInterval;
        public float AttackRange;
        public float MoveSpeed;
        public float SpawnInterval;
        public float BodyRadius;
    }

    public struct UnitStatsRef : IComponentData
    {
        public BlobAssetReference<UnitStatsBlob> Value;
    }
}
