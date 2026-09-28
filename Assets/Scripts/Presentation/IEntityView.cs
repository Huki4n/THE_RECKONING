using Unity.Entities;

namespace TheReckoning.ECS.Presentation
{
    public interface IEntityView
    {
        void Bind(Entity entity, EntityManager entityManager);
        void PlayAttack();
        void Unbind();
    }
}
