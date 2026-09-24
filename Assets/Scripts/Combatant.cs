using UnityEngine;
using TheReckoning.Morale;

namespace TheReckoning
{
    public enum Team
    {
        Left,
        Right
    }

    [RequireComponent(typeof(Health))]
    public abstract class Combatant : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float bodyRadius = 0.3f;

        public Team Side
        {
            get; private set;
        }
        public Health Health
        {
            get; private set;
        }
        public GameManager Match
        {
            get; private set;
        }

        public float BodyRadius => Mathf.Max(0.05f, bodyRadius);
        public bool IsAlive => Health != null && Health.IsAlive;

        protected virtual int KillReward => 0;

        protected void InitializeCombatant(
            GameManager match,
            Team side,
            float maximumHealth)
        {
            Match = match;
            Side = side;

            Health = GetComponent<Health>();
            Health.Initialize(maximumHealth);

            Match.Register(this);
        }

        public void TakeDamage(float amount, Team attacker)
        {
            if (Match == null || !Match.IsRunning)
                return;

            if (!IsAlive || attacker == Side)
                return;

            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
                return;

            Unit unit = this as Unit;
            if (unit != null)
            {
                Match.Morale?.Contact(unit);
                amount *= unit.StatMultiplier(Stat.DamageTaken);
            }

            float before = Health.Current;
            bool killed = Health.ApplyDamage(amount);
            float actualDamage = before - Health.Current;
            if (this is Base targetBase && actualDamage > 0f)
                Match.Morale?.BaseDamaged(targetBase, actualDamage);
            if (!killed)
                return;
            if (unit != null)
                Match.Morale?.UnitKilled(unit);

            Match.GrantKillReward(attacker, KillReward);
            OnKilled();
        }

        // Curse damages current HP without killing; it is deliberately independent of armor.
        public void TakeNonLethalDamage(float amount, Team attacker)
        {
            if (Match == null || !Match.IsRunning || !IsAlive || Side == attacker ||
                float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f) return;
            float applied = Mathf.Min(amount, Mathf.Max(0f, Health.Current - 1f));
            if (applied > 0f)
            {
                Health.ApplyDamage(applied);
                if (this is Unit unit) Match.Morale?.Contact(unit);
            }
        }

        protected virtual void OnKilled()
        {
            Match.Unregister(this);
        }

        protected virtual void OnEnable()
        {
            if (Match != null && IsAlive)
                Match.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (Match != null)
                Match.Unregister(this);
        }
    }
}
