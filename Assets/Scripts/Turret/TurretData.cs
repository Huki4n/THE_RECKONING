using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(fileName = "TurretData", menuName = "The Reckoning/Turret Data")]
    public sealed class TurretData : ScriptableObject
    {
        [SerializeField] private string displayName = "Turret";
        [SerializeField, Min(1)] private int cost = 50;
        [SerializeField] private Sprite turretSprite;
        [SerializeField] private Sprite projectileSprite;

        [SerializeField, Min(0.01f)] private float damage = 8f;
        [SerializeField, Min(0.05f)] private float attackInterval = 0.4f;
        [SerializeField, Min(0.1f)] private float range = 5f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
        [SerializeField, Min(0f)] private float arcHeight;
        [SerializeField, Min(0f)] private float splashRadius;
        [SerializeField, Min(0f)] private float targetHeight = 0.5f;

        public string DisplayName => displayName;
        public int Cost => Mathf.Max(1, cost);
        public Sprite TurretSprite => turretSprite;
        public Sprite ProjectileSprite => projectileSprite;
        public float Damage => Mathf.Max(0.01f, damage);
        public float AttackInterval => Mathf.Max(0.05f, attackInterval);
        public float Range => Mathf.Max(0.1f, range);
        public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
        public float ArcHeight => Mathf.Max(0f, arcHeight);
        public float SplashRadius => Mathf.Max(0f, splashRadius);
        public float TargetHeight => Mathf.Max(0f, targetHeight);
    }
}
