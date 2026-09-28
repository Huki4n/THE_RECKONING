using TheReckoning.Balance;
using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(
        fileName = "UnitData",
        menuName = "The Reckoning/Unit Data")]
    public sealed class UnitData : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Warrior";

        [Header("ECS Presentation")]
        [Tooltip(
            "ECS view for Team.Left / Order of Light. " +
            "Must be listed in ViewCatalog.")]
        [SerializeField]

        private GameObject leftViewPrefab;

        [Tooltip(
            "ECS view for Team.Right / Cult of Flesh. " +
            "Must be listed in ViewCatalog.")]
        [SerializeField]
        private GameObject rightViewPrefab;

        [Header("Economy")]
        [SerializeField, Min(1)]
        private int cost = 25;

        [Header("ECS")]
        [SerializeField, Min(0)]
        private int ecsUnitId;
        public int EcsUnitId => ecsUnitId;

        [SerializeField, Min(0)]
        private int killReward = 10;

        [Header("Combat")]
        [SerializeField, Min(1f)]
        private float maximumHealth = 100f;

        [SerializeField, Min(0.01f)]
        private float damage = 15f;

        [SerializeField, Min(0.05f)]
        private float attackInterval = 1f;

        [SerializeField, Min(0f)]
        private float attackRange = 0.15f;

        [SerializeField, Min(0.01f)]
        private float moveSpeed = 1.2f;

        [SerializeField, Min(0.05f)]
        private float bodyRadius = 0.3f;

        [SerializeField, Min(0.05f)]
        private float spawnInterval = 1f;

        [Header("Balance")]
        [Tooltip("Model tier used by the Balance window.")]
        [SerializeField, Min(1)]
        private int tier = 1;

        [SerializeField]
        private UnitRole role = UnitRole.Fighter;

        public int Tier => Mathf.Max(1, tier);

        public UnitRole Role => role;

        public string DisplayName => displayName;

        public GameObject LeftViewPrefab =>
            leftViewPrefab;

        public GameObject RightViewPrefab =>
            rightViewPrefab;

        public int Cost =>
            Mathf.Max(1, cost);

        public int KillReward =>
            Mathf.Max(0, killReward);

        public float MaximumHealth =>
            Mathf.Max(1f, maximumHealth);

        public float Damage =>
            Mathf.Max(0.01f, damage);

        public float AttackInterval =>
            Mathf.Max(0.05f, attackInterval);

        public float AttackRange =>
            Mathf.Max(0f, attackRange);

        public float MoveSpeed =>
            Mathf.Max(0.01f, moveSpeed);

        public float BodyRadius =>
            Mathf.Max(0.05f, bodyRadius);

        public float SpawnInterval =>
            Mathf.Max(0.05f, spawnInterval);
    }
}