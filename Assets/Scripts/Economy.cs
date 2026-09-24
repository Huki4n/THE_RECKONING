using System;
using UnityEngine;

namespace TheReckoning
{
    public sealed class Economy : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingGold = 100;
        [SerializeField, Min(0f)] private float incomePerSecond = 5f;

        private GameManager match;
        private float incomeRemainder;

        public int Gold { get; private set; }

        public event Action<int> Changed;

        public void Initialize(GameManager game)
        {
            match = game;
            Gold = Mathf.Max(0, startingGold);
            incomeRemainder = 0f;

            Changed?.Invoke(Gold);
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
                return;

            incomeRemainder += Mathf.Max(0f, incomePerSecond)
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