using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS
{
    public struct TurretState : IComponentData
    {
        public int TurretId;
        public byte TeamId;
        public float3 Muzzle;
        public float Cooldown;
    }

    public struct Projectile : IComponentData
    {
        public Entity Target;
        public byte AttackerTeam;

        public float3 Start;
        public float3 Destination;
        public float Duration;
        public float Elapsed;

        public float Damage;
        public float ArcHeight;
        public float SplashRadius;
        public float TargetHeight;
    }
}
