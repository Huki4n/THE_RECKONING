using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct DamageEvent : IBufferElementData
    {
        public Entity Source;
        public float Amount;
        public byte AttackerTeam;
        public bool NonLethal;
    }
}