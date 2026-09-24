using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(
        fileName = "WaveData",
        menuName = "The Reckoning/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        [Serializable]
        public sealed class SpawnGroup
        {
            [SerializeField] private UnitData unit;
            [SerializeField, Min(1)] private int count = 1;
            [SerializeField, Min(0.05f)] private float interval = 2f;

            public UnitData Unit => unit;
            public int Count => Mathf.Max(1, count);
            public float Interval => Mathf.Max(0.05f, interval);
        }

        [SerializeField, Min(0f)]
        private float delayBeforeWave = 5f;

        [SerializeField]
        private SpawnGroup[] groups = Array.Empty<SpawnGroup>();

        public float DelayBeforeWave => Mathf.Max(0f, delayBeforeWave);
        public IReadOnlyList<SpawnGroup> Groups => groups;
    }
}