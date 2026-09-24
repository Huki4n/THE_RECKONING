using System.Collections.Generic;
using UnityEngine;

namespace TheReckoning
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TurretProjectile : MonoBehaviour
    {
        private readonly List<Unit> targets = new List<Unit>();
        private GameManager match;
        private TurretData data;
        private Unit target;
        private Team side;
        private Vector3 start;
        private Vector3 destination;
        private float duration;
        private float elapsed;
        private bool initialized;

        public void Initialize(GameManager game, Team team, Unit victim, TurretData turretData,
            int sortingLayer, int sortingOrder)
        {
            match = game;
            side = team;
            target = victim;
            data = turretData;
            start = transform.position;
            destination = TargetPoint(victim);
            duration = Mathf.Max(0.02f, Vector2.Distance(start, destination) / data.ProjectileSpeed);
            elapsed = 0f;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = data.ProjectileSprite;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            initialized = true;
        }

        private Vector3 TargetPoint(Unit unit)
        {
            Vector3 point = unit.transform.position;
            point.y += data.TargetHeight;
            point.z = start.z;
            return point;
        }

        private void Update()
        {
            if (!initialized) return;
            if (match == null || !match.IsRunning) { Destroy(gameObject); return; }

            // Arcing stones aim at the position observed when fired. Direct shots follow living targets.
            if (data.ArcHeight <= 0f && target != null && target.IsAlive)
                destination = TargetPoint(target);

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, destination, t);
            position.y += 4f * data.ArcHeight * t * (1f - t);
            transform.position = position;

            if (data.ArcHeight <= 0f)
            {
                Vector3 direction = destination - start;
                if (direction.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }

            if (t < 1f) return;

            Hit();
            Destroy(gameObject);
        }

        private void Hit()
        {
            if (data.SplashRadius > 0f)
            {
                match.CopyUnits(side == Team.Left ? Team.Right : Team.Left, targets);
                foreach (Unit unit in targets)
                    if (unit != null && unit.IsAlive &&
                        Mathf.Abs(unit.transform.position.x - destination.x) <= data.SplashRadius + unit.BodyRadius)
                        unit.TakeDamage(data.Damage, side);
            }
            else if (target != null && target.IsAlive &&
                Mathf.Abs(target.transform.position.x - destination.x) <= target.BodyRadius + 0.1f)
            {
                target.TakeDamage(data.Damage, side);
            }
        }
    }
}
