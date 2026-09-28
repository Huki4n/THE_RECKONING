using TheReckoning.ECS.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WaveSpawnSystem : ISystem
    {
        private const byte CultTeamId = 1;
        private const int MaxUnitsPerWave = 200;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<MatchConfig>();
            state.RequireForUpdate<WaveScenario>();
            state.RequireForUpdate<WaveProgress>();
            state.RequireForUpdate<TeamProduction>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            WaveScenario scenario =
                SystemAPI.GetSingleton<WaveScenario>();

            if (!scenario.Value.IsCreated)
                return;

            ref WaveScenarioBlob blob =
                ref scenario.Value.Value;

            if (blob.Units.Length == 0)
                return;

            Entity scenarioEntity =
                SystemAPI.GetSingletonEntity<WaveProgress>();

            WaveProgress progress =
                SystemAPI.GetComponent<WaveProgress>(scenarioEntity);

            DynamicBuffer<PendingWaveUnit> pending =
                SystemAPI.GetBuffer<PendingWaveUnit>(scenarioEntity);

            if (!progress.Started)
            {
                progress.Started = true;
                progress.NextWaveIn = blob.FirstWaveDelay;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            progress.NextWaveIn -= deltaTime;

            if (progress.NextWaveIn <= 0f)
            {
                ComposeWave(ref progress, ref blob, pending);
                progress.WaveIndex++;
                progress.NextWaveIn += math.max(1f, blob.WaveInterval);
            }

            int unitId = -1;

            if (pending.Length > 0)
            {
                float multiplier =
                    SystemAPI.GetSingleton<TeamProduction>().Right;

                progress.SpawnTimer = math.max(
                    0f,
                    progress.SpawnTimer - deltaTime * multiplier);

                if (progress.SpawnTimer <= 0f &&
                    CultUnitCount(ref state) <
                    SystemAPI.GetSingleton<MatchConfig>().MaxUnitsPerTeam)
                {
                    unitId = pending[0].UnitId;
                    pending.RemoveAt(0);
                    progress.SpawnTimer = blob.SpawnSpacing;
                }
            }

            SystemAPI.SetComponent(scenarioEntity, progress);

            if (unitId >= 0)
                RequestUnit(ref state, unitId);
        }

        private int CultUnitCount(ref SystemState state)
        {
            int count = 0;

            foreach (RefRO<TeamId> team in
                SystemAPI.Query<RefRO<TeamId>>()
                    .WithAll<UnitTag>()
                    .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value == CultTeamId)
                    count++;
            }

            foreach (RefRO<UnitSpawnRequest> request in
                SystemAPI.Query<RefRO<UnitSpawnRequest>>())
            {
                if (request.ValueRO.TeamId == CultTeamId)
                    count++;
            }

            return count;
        }

        private static void ComposeWave(
            ref WaveProgress progress,
            ref WaveScenarioBlob blob,
            DynamicBuffer<PendingWaveUnit> pending)
        {
            float budget =
                blob.FirstBudget *
                math.pow(blob.BudgetGrowth, progress.WaveIndex) +
                progress.CarryBudget;

            int added = 0;

            while (added < MaxUnitsPerWave)
            {
                float totalWeight = 0f;

                for (int i = 0; i < blob.Units.Length; i++)
                {
                    if (Fits(ref blob.Units[i], progress.WaveIndex, budget))
                        totalWeight += blob.Units[i].Weight;
                }

                if (totalWeight <= 0f)
                    break;

                float roll = progress.Random.NextFloat(totalWeight);
                int picked = -1;

                for (int i = 0; i < blob.Units.Length; i++)
                {
                    if (!Fits(ref blob.Units[i], progress.WaveIndex, budget))
                        continue;

                    picked = i;
                    roll -= blob.Units[i].Weight;

                    if (roll < 0f)
                        break;
                }

                budget -= blob.Units[picked].Power;
                pending.Add(new PendingWaveUnit { UnitId = blob.Units[picked].UnitId });
                added++;
            }

            if (added == 0)
            {
                int cheapest = Cheapest(ref blob, progress.WaveIndex);

                if (cheapest >= 0)
                {
                    budget -= blob.Units[cheapest].Power;
                    pending.Add(new PendingWaveUnit { UnitId = blob.Units[cheapest].UnitId });
                }
            }

            progress.CarryBudget = budget;
        }

        private static bool Fits(ref WaveUnitBlob unit, int wave, float budget) =>
            unit.UnlockWave <= wave &&
            unit.Weight > 0f &&
            unit.Power <= budget;

        private static int Cheapest(ref WaveScenarioBlob blob, int wave)
        {
            int cheapest = -1;

            for (int i = 0; i < blob.Units.Length; i++)
            {
                if (blob.Units[i].UnlockWave > wave || blob.Units[i].Weight <= 0f)
                    continue;

                if (cheapest < 0 || blob.Units[i].Power < blob.Units[cheapest].Power)
                    cheapest = i;
            }

            return cheapest;
        }

        private static void RequestUnit(
            ref SystemState state,
            int unitId)
        {
            Entity request =
                state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(
                request,
                new UnitSpawnRequest
                {
                    UnitId = unitId,
                    TeamId = CultTeamId
                });
        }
    }
}
