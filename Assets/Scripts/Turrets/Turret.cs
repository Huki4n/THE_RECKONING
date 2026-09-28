using TheReckoning.ECS;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning
{
    public sealed class Turret : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Transform muzzle;

        private World ecsWorld;
        private Entity ecsEntity = Entity.Null;

        public bool CanUse(TurretData candidate) =>
            enabled && body != null && muzzle != null &&
            candidate != null && candidate.TurretSprite != null &&
            candidate.ProjectileViewPrefab != null;

        public void Initialize(GameManager game, Team team, TurretData turretData)
        {
            if (game == null || !CanUse(turretData))
            {
                Debug.LogError("Turret: assign GameManager, TurretData, Body, Muzzle, Turret Sprite, and Projectile View Prefab.", this);
                enabled = false;
                return;
            }

            body.sprite = turretData.TurretSprite;
            body.flipX = team == Team.Right;
            Vector3 muzzleOffset = muzzle.localPosition;
            muzzleOffset.x = Mathf.Abs(muzzleOffset.x) * (team == Team.Left ? 1f : -1f);
            muzzle.localPosition = muzzleOffset;

            CreateEcsTurret(team, turretData);
        }

        private void OnDestroy()
        {
            DestroyEcsTurret();
        }

        private void CreateEcsTurret(Team team, TurretData turretData)
        {
            DestroyEcsTurret();

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            EntityManager manager = world.EntityManager;
            ecsWorld = world;
            ecsEntity = manager.CreateEntity(typeof(TurretState));
            manager.SetComponentData(ecsEntity, new TurretState
            {
                TurretId = turretData.EcsTurretId,
                TeamId = team == Team.Left ? (byte)0 : (byte)1,
                Muzzle = muzzle.position,
                Cooldown = 0f
            });

#if UNITY_EDITOR
            manager.SetName(ecsEntity, $"Turret {turretData.name} ({team})");
#endif
        }

        private void DestroyEcsTurret()
        {
            if (ecsWorld != null && ecsWorld.IsCreated &&
                ecsWorld.EntityManager.Exists(ecsEntity))
            {
                ecsWorld.EntityManager.DestroyEntity(ecsEntity);
            }

            ecsWorld = null;
            ecsEntity = Entity.Null;
        }
    }
}
