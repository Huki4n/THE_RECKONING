using System.Collections.Generic;
using TheReckoning.ECS;
using TheReckoning.ECS.Data;
using TheReckoning.ECS.Presentation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class UnitCatalogAuthoring : MonoBehaviour
    {
        [SerializeField]
        private ViewCatalog viewCatalog;

        [SerializeField]
        private UnitData[] units = new UnitData[0];

        public IReadOnlyList<UnitData> Units => units;

        public int IndexOf(UnitData data)
        {
            if (data == null || units == null)
                return -1;

            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == data)
                    return i;
            }

            return -1;
        }

        private sealed class Baker :
            Baker<UnitCatalogAuthoring>
        {
            public override void Bake(
                UnitCatalogAuthoring authoring)
            {
                DependsOn(authoring.viewCatalog);

                Entity catalog =
                    GetEntity(TransformUsageFlags.None);

                DynamicBuffer<UnitPrefabElement> elements =
                    AddBuffer<UnitPrefabElement>(catalog);

                if (authoring.units == null)
                    return;

                foreach (UnitData data in authoring.units)
                {
                    DependsOn(data);

                    elements.Add(
                        BakeUnit(authoring, data));
                }
            }

            private UnitPrefabElement BakeUnit(
                UnitCatalogAuthoring authoring,
                UnitData data)
            {
                if (data == null)
                {
                    Debug.LogWarning(
                        $"{authoring.name}: unit entry is empty.",
                        authoring);

                    return new UnitPrefabElement
                    {
                        Prefab = Entity.Null,
                        LeftViewId = -1,
                        RightViewId = -1
                    };
                }

                DependsOn(data.LeftViewPrefab);
                DependsOn(data.RightViewPrefab);

                BlobAssetReference<UnitStatsBlob> stats =
                    BuildStats(data);

                AddBlobAsset(ref stats, out _);

                int leftViewId =
                    authoring.viewCatalog != null
                        ? authoring.viewCatalog.IndexOf(
                            data.LeftViewPrefab)
                        : -1;

                int rightViewId =
                    authoring.viewCatalog != null
                        ? authoring.viewCatalog.IndexOf(
                            data.RightViewPrefab)
                        : -1;

                if (leftViewId < 0)
                {
                    Debug.LogWarning(
                        $"{authoring.name}: {data.name} has no " +
                        "Left View Prefab listed in ViewCatalog.",
                        authoring);
                }

                if (rightViewId < 0)
                {
                    Debug.LogWarning(
                        $"{authoring.name}: {data.name} has no " +
                        "Right View Prefab listed in ViewCatalog.",
                        authoring);
                }

                Entity prefab =
                    CreateAdditionalEntity(
                        TransformUsageFlags.Dynamic,
                        entityName:
                            $"UnitPrefab_{data.name}");

                AddComponent<Prefab>(prefab);

                AddComponent(
                    prefab,
                    new UnitStatsRef
                    {
                        Value = stats
                    });

                AddComponent<UnitTag>(prefab);

                AddComponent(
                    prefab,
                    new Target
                    {
                        Value = Entity.Null
                    });

                AddComponent(
                    prefab,
                    new TheReckoning.ECS.Health
                    {
                        Current =
                            stats.Value.MaximumHealth,

                        Max =
                            stats.Value.MaximumHealth
                    });

                AddComponent(
                    prefab,
                    new BodyRadius
                    {
                        Value =
                            stats.Value.BodyRadius
                    });

                AddComponent(
                    prefab,
                    new AttackCooldown
                    {
                        Remaining = 0f
                    });

                AddComponent(
                    prefab,
                    new StatMultipliers
                    {
                        Damage = 1f,
                        AttackSpeed = 1f,
                        MoveSpeed = 1f,
                        DamageTaken = 1f
                    });

                AddComponent(
                    prefab,
                    new AbilityModifiers
                    {
                        Damage = 1f,
                        AttackSpeed = 1f,
                        MoveSpeed = 1f,
                        DamageTaken = 1f
                    });

                AddComponent(
                    prefab,
                    new AbilityBuffs());

                AddComponent(
                    prefab,
                    new Veteran
                    {
                        Battles = 0,
                        IsVeteran = 0
                    });

                AddComponent<Dead>(prefab);

                SetComponentEnabled<Dead>(
                    prefab,
                    false);

                AddBuffer<DamageEvent>(prefab);

                AddComponent<ViewAttackEvent>(prefab);

                SetComponentEnabled<ViewAttackEvent>(
                    prefab,
                    false);

                return new UnitPrefabElement
                {
                    Prefab = prefab,
                    Stats = stats,
                    LeftViewId = leftViewId,
                    RightViewId = rightViewId
                };
            }

            private static BlobAssetReference<UnitStatsBlob>
                BuildStats(UnitData data)
            {
                var builder =
                    new BlobBuilder(Allocator.Temp);

                ref UnitStatsBlob root =
                    ref builder.ConstructRoot<UnitStatsBlob>();

                root.Cost =
                    data.Cost;

                root.KillReward =
                    data.KillReward;

                root.MaximumHealth =
                    data.MaximumHealth;

                root.Damage =
                    data.Damage;

                root.AttackInterval =
                    data.AttackInterval;

                root.AttackRange =
                    data.AttackRange;

                root.MoveSpeed =
                    data.MoveSpeed;

                root.SpawnInterval =
                    data.SpawnInterval;

                root.BodyRadius =
                    data.BodyRadius;

                BlobAssetReference<UnitStatsBlob> blob =
                    builder.CreateBlobAssetReference
                        <UnitStatsBlob>(
                            Allocator.Persistent);

                builder.Dispose();

                return blob;
            }
        }
    }
}