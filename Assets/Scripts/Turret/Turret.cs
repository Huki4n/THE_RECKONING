using System.Collections.Generic;
using UnityEngine;

namespace TheReckoning
{
    // One prefab works for both teams and every era; TurretData provides its appearance and stats.
    public sealed class Turret : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Transform muzzle;
        [SerializeField] private TurretProjectile projectilePrefab;

        private readonly List<Unit> targets = new List<Unit>();
        private GameManager match;
        private TurretData data;
        private Team side;
        private float cooldown;

        public bool CanUse(TurretData candidate) => enabled && body != null && muzzle != null &&
            projectilePrefab != null && projectilePrefab.enabled && candidate != null &&
            candidate.TurretSprite != null && candidate.ProjectileSprite != null;

        public void Initialize(GameManager game, Team team, TurretData turretData)
        {
            match = game;
            side = team;
            data = turretData;
            cooldown = 0f;

            if (match == null || !CanUse(data))
            {
                Debug.LogError("Turret: assign GameManager, TurretData, Body, Muzzle, Projectile Prefab, and both sprites.", this);
                enabled = false;
                return;
            }

            body.sprite = data.TurretSprite;
            body.flipX = side == Team.Right; // Art is assumed to face right.
            Vector3 muzzleOffset = muzzle.localPosition;
            muzzleOffset.x = Mathf.Abs(muzzleOffset.x) * (side == Team.Left ? 1f : -1f);
            muzzle.localPosition = muzzleOffset;
        }

        private void Update()
        {
            if (match == null || data == null || !match.IsRunning) return;

            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
            if (cooldown > 0f) return;

            Unit target = FindTarget();
            if (target == null) return;

            TurretProjectile shot = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            shot.gameObject.SetActive(true);
            shot.Initialize(match, side, target, data, body.sortingLayerID, body.sortingOrder + 1);
            cooldown = data.AttackInterval;
        }

        private Unit FindTarget()
        {
            match.CopyUnits(side == Team.Left ? Team.Right : Team.Left, targets);

            Unit nearest = null;
            float nearestDistance = float.PositiveInfinity;
            float direction = side == Team.Left ? 1f : -1f;

            foreach (Unit unit in targets)
            {
                if (unit == null || !unit.IsAlive) continue;

                float distance = (unit.transform.position.x - muzzle.position.x) * direction;
                if (distance < 0f || distance > data.Range || distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearest = unit;
            }

            return nearest;
        }
    }
}
