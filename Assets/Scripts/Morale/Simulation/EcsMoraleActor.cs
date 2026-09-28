using TheReckoning.Morale;
using Unity.Entities;

namespace TheReckoning.ECS
{
    public sealed class EcsMoraleActor : IMoraleActor
    {
        private readonly EntityManager entityManager;
        private readonly Entity entity;

        private readonly int id;
        private readonly VeteranProgress veteran;

        public Entity Entity => entity;

        public int Id => id;

        public VeteranProgress Veteran => veteran;

        public EcsMoraleActor(
            EntityManager entityManager,
            Entity entity,
            int id)
        {
            this.entityManager = entityManager;
            this.entity = entity;
            this.id = id;

            veteran = new VeteranProgress();
        }

        public Team Side
        {
            get
            {
                if (!Exists())
                    return Team.Left;

                if (!entityManager.HasComponent<TeamId>(entity))
                    return Team.Left;

                TeamId team =
                    entityManager.GetComponentData<TeamId>(
                        entity);

                return team.Value == 0
                    ? Team.Left
                    : Team.Right;
            }
        }

        public bool IsAlive
        {
            get
            {
                if (!Exists())
                    return false;

                if (!entityManager.HasComponent<Health>(entity))
                    return false;

                Health health =
                    entityManager.GetComponentData<Health>(
                        entity);

                if (health.Current <= 0f)
                    return false;

                if (entityManager.HasComponent<Dead>(entity))
                {
                    bool deadEnabled =
                        entityManager.IsComponentEnabled<Dead>(
                            entity);

                    if (deadEnabled)
                        return false;
                }

                return true;
            }
        }

        public float X
        {
            get
            {
                if (!Exists())
                    return 0f;

                if (!entityManager.HasComponent<
                        Unity.Transforms.LocalTransform>(
                        entity))
                {
                    return 0f;
                }

                Unity.Transforms.LocalTransform transform =
                    entityManager.GetComponentData<
                        Unity.Transforms.LocalTransform>(
                        entity);

                return transform.Position.x;
            }
        }

        public bool Exists()
        {
            return entityManager.Exists(entity);
        }
    }
}