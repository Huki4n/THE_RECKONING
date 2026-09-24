using UnityEngine;

namespace TheReckoning
{
    public sealed class Base : Combatant
    {
        [SerializeField, Min(1f)]
        private float maximumHealth = 500f;

        public void Initialize(GameManager match, Team side)
        {
            InitializeCombatant(match, side, maximumHealth);
        }
    }
}   