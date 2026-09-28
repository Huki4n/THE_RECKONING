using Unity.Entities;

namespace TheReckoning.ECS.Data
{
    public struct WaveScenarioBlob
    {
        public float FirstWaveDelay;
        public float WaveInterval;
        public float FirstBudget;
        public float BudgetGrowth;

        public float SpawnSpacing;

        public BlobArray<WaveUnitBlob> Units;
    }

    public struct WaveUnitBlob
    {
        public int UnitId;
        public float Power;

        public int UnlockWave;
        public float Weight;
    }
}
