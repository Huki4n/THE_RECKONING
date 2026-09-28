using Unity.Entities;

namespace TheReckoning.ECS
{
    public struct UnitTag : IComponentData
    {
    }

    public struct Target : IComponentData
    {
        public Entity Value;
    }

    public struct AttackCooldown : IComponentData
    {
        public float Remaining;
    }

    public struct StatMultipliers : IComponentData
    {
        public float Damage;
        public float AttackSpeed;
        public float MoveSpeed;
        public float DamageTaken;
    }

    public struct AbilityModifiers : IComponentData
    {
        public float Damage;
        public float AttackSpeed;
        public float MoveSpeed;
        public float DamageTaken;
    }

    public struct Veteran : IComponentData
    {
        public int Battles;
        public byte IsVeteran;
    }
}