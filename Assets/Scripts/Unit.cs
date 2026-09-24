using System;
using TheReckoning.Morale;
using UnityEngine;


namespace TheReckoning
{
    public sealed class Unit : Combatant, IMoraleActor
    {
        [SerializeField] private SpriteRenderer body;

        private UnitData data;
        private float attackTimer;

        public UnitData Data => data;
        private static int nextRuntimeId;

        [System.NonSerialized]
        private readonly int runtimeId =
            System.Threading.Interlocked.Increment(ref nextRuntimeId);

        public int Id => runtimeId;
        public float X => transform.position.x;
        public VeteranProgress Veteran { get; private set; } = new VeteranProgress();
        public float StatMultiplier(Stat stat) => Match != null && Match.Morale != null
            ? Mathf.Max(.05f, Match.Morale.Multiplier(this, stat) +
                (Match.Abilities != null ? Match.Abilities.Bonus(this, stat) : 0f)) : 1f;

        public event System.Action<Combatant> Attacked;

        protected override int KillReward =>
            data != null ? data.KillReward : 0;

        public void Initialize(
            GameManager match,
            Team side,
            UnitData unitData)
        {
            if (unitData == null)
                throw new ArgumentNullException(nameof(unitData));
            data = unitData;
            Veteran = new VeteranProgress();
            attackTimer = 0f;

            InitializeCombatant(match, side, data.MaximumHealth);

            if (body != null)
            {
                // Исходный спрайт должен смотреть вправо.
                body.flipX = side == Team.Right;
            }
        }

        private void Update()
        {
            if (data == null || !IsAlive || Match == null || !Match.IsRunning)
                return;

            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime * StatMultiplier(Stat.AttackSpeed));

            Combatant target = Match.FindEnemyAhead(this);

            float allowedStep = data.MoveSpeed * StatMultiplier(Stat.MoveSpeed) * Time.deltaTime;

            if (target != null)
            {
                float gap = Mathf.Abs(
                    target.transform.position.x - transform.position.x)
                    - BodyRadius
                    - target.BodyRadius;

                if (gap <= data.AttackRange + 0.001f)
                {
                    Match.Morale?.Contact(this);
                    if (attackTimer <= 0f)
                    {
                        attackTimer = data.AttackInterval;

                        target.TakeDamage(data.Damage * StatMultiplier(Stat.Damage), Side);
                        Attacked?.Invoke(target);
                    }

                    return;
                }

                // Не перескакиваем дистанцию атаки за один кадр.
                allowedStep = Mathf.Min(
                    allowedStep,
                    gap - data.AttackRange);
            }

            // Не проходим сквозь союзников или противников.
            allowedStep = Mathf.Min(
                allowedStep,
                Match.GetFreeDistanceAhead(this));

            float direction = Side == Team.Left ? 1f : -1f;

            transform.position += Vector3.right
                * direction
                * Mathf.Max(0f, allowedStep);
        }

        protected override void OnKilled()
        {
            base.OnKilled();
            Destroy(gameObject);
        }
    }
}
