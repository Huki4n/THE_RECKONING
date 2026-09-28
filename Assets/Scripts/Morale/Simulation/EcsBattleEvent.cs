using Unity.Entities;

namespace TheReckoning.ECS
{
    public enum EcsBattleEventKind : byte
    {
        UnitKilled,
        BaseDamaged
    }

    public struct EcsBattleEvent : IBufferElementData
    {
        public EcsBattleEventKind Kind;

        public byte Side;

        public byte WasVeteran;

        public float Amount;

        public float Maximum;

        public int KillReward;
    }

    public struct EcsBattleEventStream : IComponentData
    {
    }
}