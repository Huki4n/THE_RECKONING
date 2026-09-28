using Unity.Burst;
using Unity.Entities;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    [UpdateBefore(typeof(EcsMoraleRuntimeSystem))]
    public partial struct KillRewardSystem : ISystem
    {
        private const byte CultTeamId = 1;

        public void OnCreate(ref SystemState state)
        {
            Entity entity =
                state.EntityManager.CreateEntity(typeof(PendingKillReward));

#if UNITY_EDITOR
            state.EntityManager.SetName(entity, "Pending Kill Reward");
#endif

            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<EcsBattleEventStream>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            DynamicBuffer<EcsBattleEvent> events =
                SystemAPI.GetSingletonBuffer<EcsBattleEvent>(true);

            int gold = 0;

            for (int i = 0; i < events.Length; i++)
            {
                EcsBattleEvent battleEvent = events[i];

                if (battleEvent.Kind == EcsBattleEventKind.UnitKilled &&
                    battleEvent.Side == CultTeamId &&
                    battleEvent.KillReward > 0)
                {
                    gold += battleEvent.KillReward;
                }
            }

            if (gold == 0)
                return;

            SystemAPI.GetSingletonRW<PendingKillReward>().ValueRW.Gold += gold;
        }
    }
}
