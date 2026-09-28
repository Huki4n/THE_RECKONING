using Unity.Entities;

namespace TheReckoning.ECS.Presentation
{
    public struct ViewPrefab : IComponentData
    {
        public int Id;
    }

    public struct ViewHandle : ICleanupComponentData
    {
        public int Index;
        public int Owner;
    }

    public struct ViewAttackEvent : IComponentData, IEnableableComponent
    {
    }
}
