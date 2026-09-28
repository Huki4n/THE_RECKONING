using System;
using TheReckoning.Balance;
using TheReckoning.ECS;
using TheReckoning.ECS.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class WaveScenarioAuthoring : MonoBehaviour
    {
        [Serializable]
        private sealed class PoolEntry
        {
            public UnitData unit;

            [Tooltip("First wave (1-based) that may contain this unit.")]
            [Min(1)] public int unlockWave = 1;

            [Tooltip("Relative chance to be picked among affordable units.")]
            [Min(0f)] public float weight = 1f;
        }

        [SerializeField] private UnitCatalogAuthoring unitCatalog;

        [Tooltip("Wave schedule and budget: firstWaveDelay, waveInterval, firstWaveBudget, waveGrowth.")]
        [SerializeField] private BalanceConfig balanceConfig;

        [SerializeField] private PoolEntry[] pool = new PoolEntry[0];

        [Tooltip("Seconds between units of a wave, scaled by Cult production.")]
        [SerializeField, Min(0.05f)] private float spawnSpacing = .6f;

        [Tooltip("Same seed gives the same wave compositions every match.")]
        [SerializeField, Min(1)] private int seed = 1;

        private sealed class Baker : Baker<WaveScenarioAuthoring>
        {
            public override void Bake(WaveScenarioAuthoring authoring)
            {
                UnitCatalogAuthoring catalog = authoring.unitCatalog != null
                    ? GetComponent<UnitCatalogAuthoring>(authoring.unitCatalog)
                    : null;
                if (catalog == null)
                {
                    Debug.LogWarning($"{authoring.name}: assign Unit Catalog.", authoring);
                    return;
                }

                BalanceConfig config = authoring.balanceConfig;
                DependsOn(config);
                if (config == null)
                {
                    Debug.LogWarning($"{authoring.name}: assign Balance Config.", authoring);
                    return;
                }

                foreach (UnitData unit in catalog.Units)
                    DependsOn(unit);

                BlobAssetReference<WaveScenarioBlob> scenario = BuildScenario(authoring, catalog, config);
                AddBlobAsset(ref scenario, out _);

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new WaveScenario { Value = scenario });
                AddComponent(entity, new WaveProgress
                {
                    Random = Random.CreateFromIndex((uint)Mathf.Max(1, authoring.seed))
                });
                AddBuffer<PendingWaveUnit>(entity);
            }

            private static BlobAssetReference<WaveScenarioBlob> BuildScenario(
                WaveScenarioAuthoring authoring,
                UnitCatalogAuthoring catalog,
                BalanceConfig config)
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref WaveScenarioBlob root = ref builder.ConstructRoot<WaveScenarioBlob>();
                root.FirstWaveDelay = config.firstWaveDelay;
                root.WaveInterval = config.waveInterval;
                root.FirstBudget = config.firstWaveBudget;
                root.BudgetGrowth = config.waveGrowth;
                root.SpawnSpacing = Mathf.Max(0.05f, authoring.spawnSpacing);

                PoolEntry[] pool = authoring.pool ?? new PoolEntry[0];
                BlobBuilderArray<WaveUnitBlob> units = builder.Allocate(ref root.Units, pool.Length);
                for (int i = 0; i < pool.Length; i++)
                {
                    PoolEntry entry = pool[i];
                    int unitId = entry != null && entry.unit != null ? catalog.IndexOf(entry.unit) : -1;
                    if (unitId < 0)
                        Debug.LogWarning($"{authoring.name}: pool entry {i + 1} unit is not in the unit catalog.", authoring);

                    units[i] = new WaveUnitBlob
                    {
                        UnitId = unitId,
                        Power = unitId >= 0 ? Mathf.Max(1f, BalanceMath.UnitPower(config, entry.unit)) : 1f,
                        UnlockWave = entry != null ? Mathf.Max(0, entry.unlockWave - 1) : 0,
                        Weight = unitId >= 0 ? Mathf.Max(0f, entry.weight) : 0f
                    };
                }

                BlobAssetReference<WaveScenarioBlob> blob = builder.CreateBlobAssetReference<WaveScenarioBlob>(Allocator.Persistent);
                builder.Dispose();
                return blob;
            }
        }
    }
}
