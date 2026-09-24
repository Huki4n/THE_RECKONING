using UnityEngine;

namespace TheReckoning
{
    public sealed class EnemyWaveController : MonoBehaviour
    {
        [SerializeField] private GameManager match;

        [SerializeField] private WaveData[] waves;

        [SerializeField] private bool repeatLastWave = true;

        private int waveIndex;
        private int groupIndex;
        private int remainingInGroup;

        private float timer;
        private bool productionTimer;
        private bool started;
        private bool completed;

        // 0 означает, что сценарий ещё не начался.
        public int CurrentWaveNumber => started ? waveIndex + 1 : 0;
        public bool IsCompleted => completed;

        private void Start()
        {
            if (!ValidateConfiguration())
                enabled = false;
        }

        private void Update()
        {
            if (match == null || !match.IsRunning || completed)
                return;

            if (!started)
            {
                started = true;
                BeginWave(0);
            }

            timer = Mathf.Max(0f, timer - Time.deltaTime * (productionTimer && match.Morale != null ? match.Morale.EnemyProductionMultiplier : 1f));

            if (timer > 0f)
                return;

            WaveData.SpawnGroup group =
                waves[waveIndex].Groups[groupIndex];

            // При блокировке пробуем снова в следующем кадре.
            // Прогресс сценария при этом не изменяется.
            if (!match.RightSpawner.TrySpawnScripted(group.Unit))
                return;

            remainingInGroup--;

            if (remainingInGroup > 0)
            {
                timer = group.Interval;
                productionTimer = true;
                return;
            }

            groupIndex++;

            if (groupIndex < waves[waveIndex].Groups.Count)
            {
                remainingInGroup =
                    waves[waveIndex].Groups[groupIndex].Count;

                timer = group.Interval;
                productionTimer = true;
                return;
            }

            AdvanceWave();
        }

        private void BeginWave(int index)
        {
            waveIndex = index;
            groupIndex = 0;

            WaveData wave = waves[waveIndex];

            remainingInGroup = wave.Groups[0].Count;
            timer = wave.DelayBeforeWave;
            productionTimer = false;
        }

        private void AdvanceWave()
        {
            int nextIndex = waveIndex + 1;

            if (nextIndex < waves.Length)
            {
                BeginWave(nextIndex);
                return;
            }

            if (repeatLastWave)
            {
                BeginWave(waves.Length - 1);
                return;
            }

            completed = true;
        }

        private bool ValidateConfiguration()
        {
            if (match == null)
                return Fail("Assign GameManager.");

            if (match.RightSpawner == null)
                return Fail("Assign Right Spawner in GameManager.");

            if (waves == null || waves.Length == 0)
                return Fail("Add at least one wave.");

            for (int w = 0; w < waves.Length; w++)
            {
                WaveData wave = waves[w];

                if (wave == null)
                    return Fail($"Wave {w + 1} is missing.");

                if (wave.Groups == null || wave.Groups.Count == 0)
                    return Fail($"Wave {w + 1} has no groups.");

                for (int g = 0; g < wave.Groups.Count; g++)
                {
                    WaveData.SpawnGroup group = wave.Groups[g];

                    if (group == null ||
                        group.Unit == null ||
                        group.Unit.Prefab == null)
                    {
                        return Fail(
                            $"Wave {w + 1}, group {g + 1}: " +
                            "assign Unit Data and its prefab.");
                    }

                    Unit prefab = group.Unit.Prefab;

                    if (!prefab.gameObject.activeSelf || !prefab.enabled)
                    {
                        return Fail(
                            $"Wave {w + 1}, group {g + 1}: " +
                            "the unit prefab and Unit component must be active.");
                    }
                }
            }

            return true;
        }

        private bool Fail(string message)
        {
            Debug.LogError(
                $"EnemyWaveController: {message}",
                this);

            return false;
        }
    }
}
