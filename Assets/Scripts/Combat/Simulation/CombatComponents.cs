using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct TeamId : IComponentData
    {
        public byte Value;
    }

    public struct Health : IComponentData
    {
        public float Current;
        public float Max;
    }

    public struct BodyRadius : IComponentData
    {
        public float Value;
    }

    public struct BaseTag : IComponentData
    {
    }

    public struct Dead : IComponentData, IEnableableComponent
    {
    }

    public struct DeathTimer : IComponentData
    {
        public float Remaining;
    }
}