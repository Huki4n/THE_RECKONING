using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct UnitSpawnRequest : IComponentData
    {
        public int UnitId;
        public byte TeamId;
    }
}