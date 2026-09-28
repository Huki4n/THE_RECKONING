using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    [UpdateBefore(typeof(AbilityModifierSystem))]
    [UpdateBefore(typeof(EcsMoraleRuntimeSystem))]
    public partial struct CultAbilitySystem : ISystem
    {
        private const float CheckInterval = .2f;
        private const byte OrderTeam = 0;
        private const byte CultTeam = 1;

        public void OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity(
                typeof(CultAbilityState),
                typeof(OrderDeathTime));

#if UNITY_EDITOR
            state.EntityManager.SetName(entity, "Cult Ability");
#endif

            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<CultAbilitySettings>();
            state.RequireForUpdate<AbilityTimers>();
            state.RequireForUpdate<EcsBattleEventStream>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            CultAbilitySettings settings = SystemAPI.GetSingleton<CultAbilitySettings>();
            CultAbilityState cult = SystemAPI.GetSingleton<CultAbilityState>();
            AbilityTimers timers = SystemAPI.GetSingleton<AbilityTimers>();

            cult.Clock += deltaTime;
            cult.ReadyIn = math.max(0f, cult.ReadyIn - deltaTime);
            cult.CheckIn -= deltaTime;

            UpdateBloodlust(ref state, settings, cult.Clock, ref timers);
            UpdateLastStand(ref state, settings, ref cult, ref timers);

            if (cult.CheckIn <= 0f)
            {
                cult.CheckIn = CheckInterval;

                if (cult.ReadyIn <= 0f)
                    TryActivate(ref state, settings, ref cult, ref timers);
            }

            SystemAPI.SetSingleton(cult);
            SystemAPI.SetSingleton(timers);
        }

        private void UpdateBloodlust(
            ref SystemState state,
            in CultAbilitySettings settings,
            float clock,
            ref AbilityTimers timers)
        {
            DynamicBuffer<OrderDeathTime> deaths =
                SystemAPI.GetSingletonBuffer<OrderDeathTime>();

            DynamicBuffer<EcsBattleEvent> events =
                SystemAPI.GetSingletonBuffer<EcsBattleEvent>(true);

            for (int i = 0; i < events.Length; i++)
            {
                if (events[i].Kind == EcsBattleEventKind.UnitKilled &&
                    events[i].Side == OrderTeam)
                {
                    deaths.Add(new OrderDeathTime { Time = clock });
                }
            }

            int expired = 0;
            while (expired < deaths.Length &&
                   clock - deaths[expired].Time > settings.BloodlustDeathWindow)
            {
                expired++;
            }

            if (expired > 0)
                deaths.RemoveRange(0, expired);

            if (deaths.Length < settings.BloodlustOrderDeaths)
                return;

            deaths.Clear();
            timers.Bloodlust = settings.BloodlustSeconds;
        }

        private void UpdateLastStand(
            ref SystemState state,
            in CultAbilitySettings settings,
            ref CultAbilityState cult,
            ref AbilityTimers timers)
        {
            if (cult.LastStandUsed ||
                !TryGetBase(ref state, CultTeam, out Health health, out _))
            {
                return;
            }

            if (health.Current <= 0f ||
                health.Current > health.Max * settings.LastStandBaseHealthFraction)
            {
                return;
            }

            cult.LastStandUsed = true;

            foreach (var (team, unitHealth, buffs) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRW<AbilityBuffs>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value == CultTeam && unitHealth.ValueRO.Current > 0f)
                    buffs.ValueRW.LastStand = settings.LastStandSeconds;
            }

            timers.LastStand = settings.LastStandSeconds;
        }

        private void TryActivate(
            ref SystemState state,
            in CultAbilitySettings settings,
            ref CultAbilityState cult,
            ref AbilityTimers timers)
        {
            if (settings.Ability == CultAbilityKind.FallenCurse)
            {
                if (CountUnits(ref state, OrderTeam, 0f, float.PositiveInfinity) <
                    settings.CurseMinimumOrderUnits)
                {
                    return;
                }

                ApplyFallenCurse(ref state, settings.CurseMaxHealthDamage);
                timers.Curse = settings.CurseIndicatorSeconds;
                cult.ReadyIn = settings.CurseCooldown;
                return;
            }

            if (!TryGetBase(ref state, CultTeam, out Health baseHealth, out float baseX) ||
                baseHealth.Current >= baseHealth.Max * settings.BlessingBaseHealthFraction ||
                CountUnits(ref state, OrderTeam, baseX, settings.BaseDefenseRadius) <
                settings.BlessingMinimumNearbyOrderUnits)
            {
                return;
            }

            foreach (var (team, health, buffs) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRW<AbilityBuffs>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value == CultTeam && health.ValueRO.Current > 0f)
                    buffs.ValueRW.Blessing = settings.BlessingSeconds;
            }

            timers.Blessing = settings.BlessingSeconds;
            cult.ReadyIn = settings.BlessingCooldown;
        }

        private void ApplyFallenCurse(ref SystemState state, float maxHealthFraction)
        {
            foreach (var (team, health, damage) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, DynamicBuffer<DamageEvent>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value != OrderTeam || health.ValueRO.Current <= 0f)
                    continue;

                damage.Add(new DamageEvent
                {
                    Source = Entity.Null,
                    Amount = health.ValueRO.Max * maxHealthFraction,
                    AttackerTeam = CultTeam,
                    NonLethal = true
                });
            }
        }

        private int CountUnits(ref SystemState state, byte teamId, float x, float radius)
        {
            int count = 0;

            foreach (var (team, health, transform) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRO<LocalTransform>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value == teamId &&
                    health.ValueRO.Current > 0f &&
                    math.abs(transform.ValueRO.Position.x - x) <= radius)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetBase(ref SystemState state, byte teamId, out Health health, out float x)
        {
            foreach (var (team, baseHealth, transform) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRO<LocalTransform>>()
                         .WithAll<BaseTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value != teamId || baseHealth.ValueRO.Current <= 0f)
                    continue;

                health = baseHealth.ValueRO;
                x = transform.ValueRO.Position.x;
                return true;
            }

            health = default;
            x = 0f;
            return false;
        }
    }
}
