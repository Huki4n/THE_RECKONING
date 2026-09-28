using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS
{
    public struct WaveProgress : IComponentData
    {
        public int WaveIndex;
        public float NextWaveIn;
        public float SpawnTimer;

        public float CarryBudget;

        public Random Random;
        public bool Started;
    }

    public struct PendingWaveUnit : IBufferElementData
    {
        public int UnitId;
    }
}
