using TheReckoning.ECS;
using TheReckoning.ECS.Data;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class MatchConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private Transform leftSpawn;
        [SerializeField] private Transform rightSpawn;
        [SerializeField, Min(1)] private int maxUnitsPerTeam = 30;

        [Header("Siege")]
        [Tooltip("A team is under siege while a living enemy unit is this close to its spawn point.")]
        [SerializeField, Min(0.1f)] private float siegeRadius = 2f;

        [Tooltip("Minimum seconds between spawns of a team under siege.")]
        [SerializeField, Min(0f)] private float siegeSpawnDelay = 2f;

        private sealed class Baker : Baker<MatchConfigAuthoring>
        {
            public override void Bake(MatchConfigAuthoring authoring)
            {
                if (authoring.leftSpawn == null || authoring.rightSpawn == null)
                {
                    Debug.LogWarning($"{authoring.name}: assign both spawn markers.", authoring);
                    return;
                }

                DependsOn(authoring.leftSpawn);
                DependsOn(authoring.rightSpawn);

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MatchConfig
                {
                    LeftSpawn = authoring.leftSpawn.position,
                    RightSpawn = authoring.rightSpawn.position,
                    MaxUnitsPerTeam = Mathf.Max(1, authoring.maxUnitsPerTeam),
                    SiegeRadius = Mathf.Max(0.1f, authoring.siegeRadius),
                    SiegeSpawnDelay = Mathf.Max(0f, authoring.siegeSpawnDelay)
                });
                AddComponent(entity, new SpawnGate());
            }
        }
    }
}
