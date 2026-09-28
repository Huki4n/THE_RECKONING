using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(DamageSystem))]
    [UpdateBefore(typeof(AbilityModifierSystem))]
    public partial struct AbilityRequestSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<AbilityRequest>();
            state.RequireForUpdate<AbilityTimers>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (requestRef, entity) in SystemAPI
                         .Query<RefRO<AbilityRequest>>()
                         .WithEntityAccess())
            {
                AbilityRequest request = requestRef.ValueRO;

                switch (request.Kind)
                {
                    case AbilityRequestKind.HeavenlyWrath:
                        ApplyAreaDamage(ref state, request);
                        break;

                    case AbilityRequestKind.HolyShield:
                        ApplyShield(ref state, request);
                        break;

                    case AbilityRequestKind.Inspiration:
                        SystemAPI.GetSingletonRW<AbilityTimers>().ValueRW.Inspiration =
                            request.Duration;
                        break;

                    default:
                        continue;
                }

                commands.DestroyEntity(entity);
            }

            commands.Playback(state.EntityManager);
            commands.Dispose();
        }

        private void ApplyAreaDamage(ref SystemState state, in AbilityRequest request)
        {
            byte enemyTeam = request.Team == 0 ? (byte)1 : (byte)0;

            foreach (var (team, health, radius, transform, damage) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRO<BodyRadius>,
                             RefRO<LocalTransform>, DynamicBuffer<DamageEvent>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value != enemyTeam || health.ValueRO.Current <= 0f)
                    continue;

                if (math.abs(transform.ValueRO.Position.x - request.X) >
                    request.Radius + radius.ValueRO.Value)
                {
                    continue;
                }

                damage.Add(new DamageEvent
                {
                    Source = Entity.Null,
                    Amount = request.Amount,
                    AttackerTeam = request.Team,
                    NonLethal = false
                });
            }
        }

        private void ApplyShield(ref SystemState state, in AbilityRequest request)
        {
            foreach (var (team, health, buffs) in SystemAPI
                         .Query<RefRO<TeamId>, RefRO<Health>, RefRW<AbilityBuffs>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                if (team.ValueRO.Value != request.Team || health.ValueRO.Current <= 0f)
                    continue;

                buffs.ValueRW.Shield = request.Duration;
            }
        }
    }
}
