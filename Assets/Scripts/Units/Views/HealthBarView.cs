using TMPro;using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace TheReckoning
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [Header("ECS Base")]
        [SerializeField]
        private Team targetTeam = Team.Left;

        [Header("UI")]
        [SerializeField]
        private Slider healthSlider;

        [SerializeField]
        private TMP_Text healthText;

        private Entity baseEntity = Entity.Null;
        private EntityManager entityManager;
        private bool ecsReady;

        private void OnEnable()
        {
            baseEntity = Entity.Null;
            ecsReady = false;

            TryFindBase();
        }

        private void Update()
        {
            if (!ecsReady ||
                baseEntity == Entity.Null ||
                !entityManager.Exists(baseEntity))
            {
                baseEntity = Entity.Null;
                ecsReady = false;

                TryFindBase();
            }

            if (!ecsReady)
                return;

            if (!entityManager.HasComponent<
                    TheReckoning.ECS.Health>(
                    baseEntity))
            {
                baseEntity = Entity.Null;
                ecsReady = false;
                return;
            }

            TheReckoning.ECS.Health health =
                entityManager.GetComponentData<
                    TheReckoning.ECS.Health>(
                    baseEntity);

            Refresh(
                health.Current,
                health.Max);
        }

        private void TryFindBase()
        {
            World world =
                World.DefaultGameObjectInjectionWorld;

            if (world == null ||
                !world.IsCreated)
            {
                return;
            }

            entityManager =
                world.EntityManager;

            EntityQuery query =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<
                        TheReckoning.ECS.BaseTag>(),
                    ComponentType.ReadOnly<
                        TheReckoning.ECS.TeamId>(),
                    ComponentType.ReadOnly<
                        TheReckoning.ECS.Health>());

            using NativeArray<Entity> entities =
                query.ToEntityArray(
                    Unity.Collections.Allocator.Temp);

            byte wantedTeam =
                targetTeam == Team.Left
                    ? (byte)0
                    : (byte)1;

            foreach (Entity entity in entities)
            {
                TheReckoning.ECS.TeamId team =
                    entityManager.GetComponentData<
                        TheReckoning.ECS.TeamId>(
                        entity);

                if (team.Value != wantedTeam)
                    continue;

                baseEntity = entity;
                ecsReady = true;

                TheReckoning.ECS.Health health =
                    entityManager.GetComponentData<
                        TheReckoning.ECS.Health>(
                        entity);

                Refresh(
                    health.Current,
                    health.Max);

                break;
            }

            query.Dispose();
        }

        private void Refresh(
            float current,
            float maximum)
        {
            bool initialized =
                maximum > 0f;

            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;

                healthSlider.maxValue =
                    initialized
                        ? maximum
                        : 1f;

                healthSlider.SetValueWithoutNotify(
                    initialized
                        ? current
                        : 0f);
            }

            if (healthText != null)
            {
                healthText.text =
                    initialized
                        ? $"{Mathf.CeilToInt(current)} / " +
                          $"{Mathf.CeilToInt(maximum)}"
                        : "";
            }
        }
    }
}