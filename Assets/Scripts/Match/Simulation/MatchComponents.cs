using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct MatchRunning : IComponentData
    {
    }

    public struct TeamProduction : IComponentData
    {
        public float Left;
        public float Right;
    }

    public struct PendingKillReward : IComponentData
    {
        public int Gold;
    }

    public struct SpawnGate : IComponentData
    {
        public double LeftReadyAt;
        public double RightReadyAt;
    }
}
