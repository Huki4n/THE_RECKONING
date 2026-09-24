using System.Collections.Generic;
using UnityEngine;

namespace TheReckoning
{
    public sealed class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Units available for player purchases.")]
        [SerializeField] private UnitData[] availableUnits;

        [SerializeField, Min(1)] private int maximumUnits = 30;

        private GameManager match;
        private Economy economy;
        private Team side;
        private float cooldown;

        public IReadOnlyList<UnitData> AvailableUnits => availableUnits;
        // Remaining base work, not wall-clock seconds while morale changes speed.
        public float Cooldown => cooldown;
        public float ProductionMultiplier => match != null && match.Morale != null
            ? Mathf.Max(.05f, match.Morale.ProductionMultiplier(side) +
                (match.Abilities != null ? match.Abilities.ProductionBonus(side) : 0f)) : 1f;
        public float EstimatedCooldownSeconds => cooldown / ProductionMultiplier;

        public void Initialize(
            GameManager game,
            Team team,
            Economy wallet = null)
        {
            match = game;
            side = team;
            economy = wallet;
            cooldown = 0f;
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
                return;

            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime * ProductionMultiplier);
        }

        // Проверка покупки игроком. Используется также интерфейсом.
        public bool CanSpawn(UnitData data)
        {
            if (side != Team.Left || economy == null)
                return false;

            if (!CanCreateUnit(data))
                return false;

            if (!IsAvailable(data) || cooldown > 0f)
                return false;

            return economy.CanAfford(data.Cost);
        }

        // Покупка игроком.
        public bool TrySpawn(UnitData data)
        {
            if (!CanSpawn(data))
                return false;

            if (!economy.TrySpend(data.Cost))
                return false;

            cooldown = data.SpawnInterval;
            CreateUnit(data);

            return true;
        }

        // Создание противника по сценарию.
        public bool TrySpawnScripted(UnitData data)
        {
            if (side != Team.Right || !CanCreateUnit(data))
                return false;

            CreateUnit(data);
            return true;
        }

        private bool CanCreateUnit(UnitData data)
        {
            if (match == null || !match.IsRunning)
                return false;

            if (data == null || data.Prefab == null || spawnPoint == null)
                return false;

            if (!data.Prefab.gameObject.activeSelf || !data.Prefab.enabled)
                return false;

            if (match.CountUnits(side) >= Mathf.Max(1, maximumUnits))
                return false;

            return match.IsSpawnSpaceFree(
                spawnPoint.position.x,
                data.Prefab.BodyRadius,
                side);
        }

        private void CreateUnit(UnitData data)
        {
            Unit unit = Instantiate(
                data.Prefab,
                spawnPoint.position,
                Quaternion.identity);

            unit.Initialize(match, side, data);
        }

        private bool IsAvailable(UnitData data)
        {
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
                Debug.LogWarning("Спавнер не инициализирован. Проверь ссылки GameManager.", this);
                return;
            }

            if (!match.IsRunning)
            {
                Debug.LogWarning("Матч не запущен или уже завершён.", this);
                return;
            }

            if (side != Team.Left || economy == null)
            {
                Debug.LogWarning("Это не спавнер игрока или не назначена экономика.", this);
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogWarning("Не назначен Spawn Point.", this);
                return;
            }

            if (availableUnits == null || availableUnits.Length == 0)
            {
                Debug.LogWarning("Список Available Units пуст.", this);
                return;
            }

            foreach (UnitData data in availableUnits)
            {
                if (data == null)
                {
                    Debug.LogWarning("В Available Units есть пустой элемент.", this);
                    continue;
                }

                if (data.Prefab == null)
                {
                    Debug.LogWarning($"{data.name}: не назначен Prefab.", this);
                    continue;
                }

                Debug.Log(
                    $"{data.name}: " +
                    $"CanSpawn={CanSpawn(data)}, " +
                    $"Gold={economy.Gold}, Cost={data.Cost}, " +
                    $"Cooldown={cooldown:F2}, " +
                    $"Units={match.CountUnits(side)}/{maximumUnits}, " +
                    $"PrefabActive={data.Prefab.gameObject.activeSelf}, " +
                    $"UnitEnabled={data.Prefab.enabled}, " +
                    $"SpawnX={spawnPoint.position.x:F2}, " +
                    $"SpaceFree={match.IsSpawnSpaceFree(spawnPoint.position.x, data.Prefab.BodyRadius, side)}",
                    this);
            }
        }
    }
}
