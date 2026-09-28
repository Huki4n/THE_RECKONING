using System.Collections.Generic;
using TheReckoning.Morale;
using Unity.Entities;

namespace TheReckoning.ECS
{
    public sealed class EcsMoraleWorld : IMoraleWorld
    {
        private readonly EntityManager entityManager;

        private readonly List<IMoraleActor> actors =
            new List<IMoraleActor>();

        private readonly Dictionary<Entity, EcsMoraleActor>
            actorsByEntity =
                new Dictionary<Entity, EcsMoraleActor>();

        private readonly IReadOnlyList<IMoraleActor>
            readOnlyActors;

        private Entity leftBase = Entity.Null;
        private Entity rightBase = Entity.Null;

        private int nextActorId = 1;

        public IReadOnlyList<IMoraleActor> Actors =>
            readOnlyActors;

        public EcsMoraleWorld(
            EntityManager entityManager)
        {
            this.entityManager =
                entityManager;

            readOnlyActors =
                actors.AsReadOnly();
        }

        public EcsMoraleActor Register(
            Entity entity)
        {
            if (!entityManager.Exists(entity))
                return null;

            if (actorsByEntity.TryGetValue(
                    entity,
                    out EcsMoraleActor existing))
            {
                return existing;
            }

            EcsMoraleActor actor =
                new EcsMoraleActor(
                    entityManager,
                    entity,
                    nextActorId++);

            actorsByEntity.Add(
                entity,
                actor);

            actors.Add(actor);

            return actor;
        }

        public bool TryGetActor(
            Entity entity,
            out EcsMoraleActor actor)
        {
            return actorsByEntity.TryGetValue(
                entity,
                out actor);
        }

        public void Unregister(
            Entity entity)
        {
            if (!actorsByEntity.TryGetValue(
                    entity,
                    out EcsMoraleActor actor))
            {
                return;
            }

            actorsByEntity.Remove(entity);
            actors.Remove(actor);
        }

        public void SetBase(
            Team side,
            Entity entity)
        {
            if (side == Team.Left)
            {
                leftBase = entity;
            }
            else
            {
                rightBase = entity;
            }
        }

        public float BaseX(
            Team side)
        {
            Entity entity =
                side == Team.Left
                    ? leftBase
                    : rightBase;

            if (entity == Entity.Null)
                return 0f;

            if (!entityManager.Exists(entity))
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

        public void Clear()
        {
            actorsByEntity.Clear();
            actors.Clear();

            leftBase = Entity.Null;
            rightBase = Entity.Null;

            nextActorId = 1;
        }
    }
}