using System;
using TheReckoning.Balance;
using UnityEngine;

namespace TheReckoning
{
    public sealed class Economy : MonoBehaviour
    {
        [Tooltip("When assigned, starting gold and income come from the balance model.")]
        [SerializeField] private BalanceConfig balanceConfig;
        [SerializeField, Min(0)] private int startingGold = 100;
        [SerializeField, Min(0f)] private float incomePerSecond = 5f;

        private GameManager match;
        private float incomeRemainder;

        private int StartingGold =>
            balanceConfig != null ? balanceConfig.startingGold : startingGold;

        private float IncomePerSecond =>
            balanceConfig != null ? balanceConfig.incomePerSecond : incomePerSecond;

        public int Gold { get; private set; }

        public event Action<int> Changed;

        public void Initialize(GameManager game)
        {
            match = game;
            Gold = Mathf.Max(0, StartingGold);
            incomeRemainder = 0f;

            Changed?.Invoke(Gold);
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
                return;

            incomeRemainder += Mathf.Max(0f, IncomePerSecond)
                * Time.deltaTime;

            int earned = Mathf.FloorToInt(incomeRemainder);

            if (earned <= 0)
                return;

            incomeRemainder -= earned;
            Add(earned);
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && Gold >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
                return false;

            Gold -= amount;
            Changed?.Invoke(Gold);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            Changed?.Invoke(Gold);
        }
    }
}
