using System.Collections.Generic;
using TheReckoning.ECS;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning
{
    public sealed class UnitSpawner : MonoBehaviour
    {
        private const byte OrderTeamId = 0;

        [Tooltip("Units available for player purchases when GameManager has no era.")]
        [SerializeField]
        private UnitData[] availableUnits;

        private GameManager match;
        private Economy economy;
        private float cooldown;

        private World productionWorld;
        private EntityQuery productionQuery;

        [Header("Runtime Debug")]
        [SerializeField]
        private float debugProductionMultiplier = 1f;

        public IReadOnlyList<UnitData> AvailableUnits =>
            availableUnits;

        public float Cooldown => cooldown;

        public float ProductionMultiplier
        {
            get
            {
                float moraleMultiplier =
                    GetMoraleProductionMultiplier();

                float abilityBonus =
                    match != null &&
                    match.Abilities != null
                        ? match.Abilities.ProductionBonus(Team.Left)
                        : 0f;

                return Mathf.Max(
                    .05f,
                    moraleMultiplier + abilityBonus);
            }
        }

        public float EstimatedCooldownSeconds =>
            cooldown / ProductionMultiplier;

        public void Initialize(
            GameManager game,
            Economy wallet)
        {
            match = game;
            economy = wallet;
            cooldown = 0f;
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
            {
                debugProductionMultiplier = 1f;
                return;
            }

            float productionMultiplier =
                ProductionMultiplier;

            debugProductionMultiplier =
                productionMultiplier;

            cooldown = Mathf.Max(
                0f,
                cooldown -
                Time.deltaTime * productionMultiplier);
        }

        public bool CanSpawn(UnitData data)
        {
            if (economy == null)
                return false;

            if (!CanRequestUnit(data))
                return false;

            if (!IsAvailable(data))
                return false;

            if (cooldown > 0f)
                return false;

            return economy.CanAfford(data.Cost);
        }

        public bool TrySpawn(UnitData data)
        {
            if (!CanSpawn(data))
                return false;

            if (!economy.TrySpend(data.Cost))
                return false;

            if (!CreateEcsSpawnRequest(data))
            {
                economy.Add(data.Cost);
                return false;
            }

            cooldown = data.SpawnInterval;

            return true;
        }

        private bool CanRequestUnit(UnitData data)
        {
            if (match == null || !match.IsRunning)
                return false;

            if (data == null)
                return false;

            if (data.EcsUnitId < 0)
                return false;

            if (!TryGetEntityManager(out _))
                return false;

            return true;
        }

        private bool CreateEcsSpawnRequest(UnitData data)
        {
            if (data == null)
                return false;

            if (!TryGetEntityManager(
                    out EntityManager entityManager))
            {
                Debug.LogError(
                    "UnitSpawner: Default ECS World is not " +
                    "available.",
                    this);

                return false;
            }

            Entity request =
                entityManager.CreateEntity();

            entityManager.AddComponentData(
                request,
                new UnitSpawnRequest
                {
                    UnitId = data.EcsUnitId,
                    TeamId = OrderTeamId
                });

            #if UNITY_EDITOR
            entityManager.SetName(
                request,
                $"SpawnRequest_Order_{data.EcsUnitId}");
            #endif

            return true;
        }

        private static bool TryGetEntityManager(
            out EntityManager entityManager)
        {
            World world =
                World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                entityManager = default;
                return false;
            }

            entityManager = world.EntityManager;

            return true;
        }

        private float GetMoraleProductionMultiplier()
        {
            World world =
                World.DefaultGameObjectInjectionWorld;

            if (world == null ||
                !world.IsCreated)
            {
                return 1f;
            }

            if (productionWorld != world)
            {
                productionQuery =
                    world.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<TeamProduction>());

                productionWorld = world;
            }

            return productionQuery.TryGetSingleton(
                out TeamProduction production)
                ? production.Left
                : 1f;
        }

        private bool IsAvailable(UnitData data)
        {
            if (match != null && match.Era != null)
                return match.Era.HasUnit(data);

            if (availableUnits == null)
                return false;

            foreach (UnitData available in availableUnits)
            {
                if (available == data)
                    return true;
            }

            return false;
        }

        [ContextMenu("Diagnose Recruitment")]
        private void DiagnoseRecruitment()
        {
            if (match == null)
            {
                Debug.LogWarning(
                    "UnitSpawner is not initialized.",
                    this);

                return;
            }

            if (!match.IsRunning)
            {
                Debug.LogWarning(
                    "Match is not running.",
                    this);

                return;
            }

            if (economy == null)
            {
                Debug.LogWarning(
                    "Economy is missing.",
                    this);

                return;
            }

            if (availableUnits == null ||
                availableUnits.Length == 0)
            {
                Debug.LogWarning(
                    "Available Units is empty.",
                    this);

                return;
            }

            bool ecsAvailable =
                TryGetEntityManager(out _);

            foreach (UnitData data in availableUnits)
            {
                if (data == null)
                {
                    Debug.LogWarning(
                        "Available Units contains a null entry.",
                        this);

                    continue;
                }

                Debug.Log(
                    $"{data.name}: " +
                    $"CanSpawn={CanSpawn(data)}, " +
                    $"Gold={economy.Gold}, " +
                    $"Cost={data.Cost}, " +
                    $"Cooldown={cooldown:F2}, " +
                    $"EcsUnitId={data.EcsUnitId}, " +
                    $"ECSWorld={ecsAvailable}",
                    this);
            }
        }
    }
}