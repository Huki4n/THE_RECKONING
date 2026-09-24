using System;
using UnityEngine;

namespace TheReckoning
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        public float Current { get; private set; }
        public float Maximum { get; private set; }
        public bool IsAlive => Current > 0f;

        public event Action<float, float> Changed;
        public event Action Died;

        public void Initialize(float maximum)
        {
            Maximum = Mathf.Max(1f, maximum);
            Current = Maximum;
            Changed?.Invoke(Current, Maximum);
        }

        // Урон проходит через Combatant, чтобы учитывать сторону и награду.
        internal bool ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return false;

            Current = Mathf.Max(0f, Current - amount);
            bool killed = Current <= 0f;

            Changed?.Invoke(Current, Maximum);

            if (killed)
                Died?.Invoke();

            return killed;
        }
    }
}