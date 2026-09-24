using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(
        fileName = "UnitData",
        menuName = "The Reckoning/Unit Data")]
    public sealed class UnitData : ScriptableObject
    {
        [SerializeField] private string displayName = "Warrior";
        [SerializeField] private Unit prefab;

        [SerializeField, Min(1)] private int cost = 25;
        [SerializeField, Min(0)] private int killReward = 10;

        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0.01f)] private float damage = 15f;
        [SerializeField, Min(0.05f)] private float attackInterval = 1f;
        [SerializeField, Min(0f)] private float attackRange = 0.15f;
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.2f;
        [SerializeField, Min(0.05f)] private float spawnInterval = 1f;

        public string DisplayName => displayName;
        public Unit Prefab => prefab;

        public int Cost => Mathf.Max(1, cost);
        public int KillReward => Mathf.Max(0, killReward);

        public float MaximumHealth => Mathf.Max(1f, maximumHealth);
        public float Damage => Mathf.Max(0.01f, damage);
        public float AttackInterval => Mathf.Max(0.05f, attackInterval);
        public float AttackRange => Mathf.Max(0f, attackRange);
        public float MoveSpeed => Mathf.Max(0.01f, moveSpeed);
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
    }
}