using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EcsMoraleRuntimeSystem))]
    public partial struct AbilityModifierSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity(typeof(AbilityTimers));

#if UNITY_EDITOR
            state.EntityManager.SetName(entity, "Ability Timers");
#endif

            state.RequireForUpdate<MatchRunning>();
            state.RequireForUpdate<AbilityTimers>();
            state.RequireForUpdate<AbilityEffectSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            RefRW<AbilityTimers> timersRef = SystemAPI.GetSingletonRW<AbilityTimers>();
            ref AbilityTimers timers = ref timersRef.ValueRW;
            timers.Curse = math.max(0f, timers.Curse - deltaTime);
            timers.Inspiration = math.max(0f, timers.Inspiration - deltaTime);
            timers.Bloodlust = math.max(0f, timers.Bloodlust - deltaTime);
            timers.Blessing = math.max(0f, timers.Blessing - deltaTime);
            timers.LastStand = math.max(0f, timers.LastStand - deltaTime);
            timers.Production = math.max(0f, timers.Production - deltaTime);

            AbilityEffectSettings settings = SystemAPI.GetSingleton<AbilityEffectSettings>();
            bool inspiration = timers.Inspiration > 0f;
            bool bloodlust = timers.Bloodlust > 0f;

            foreach (var (buffsRef, modifiersRef, team) in SystemAPI
                         .Query<RefRW<AbilityBuffs>, RefRW<AbilityModifiers>, RefRO<TeamId>>()
                         .WithAll<UnitTag>()
                         .WithDisabled<Dead>())
            {
                ref AbilityBuffs buffs = ref buffsRef.ValueRW;
                buffs.Shield = math.max(0f, buffs.Shield - deltaTime);
                buffs.Blessing = math.max(0f, buffs.Blessing - deltaTime);
                buffs.LastStand = math.max(0f, buffs.LastStand - deltaTime);

                var modifiers = new AbilityModifiers
                {
                    Damage = 1f,
                    AttackSpeed = 1f,
                    MoveSpeed = 1f,
                    DamageTaken = 1f
                };

                if (team.ValueRO.Value == 0)
                {
                    if (inspiration)
                    {
                        modifiers.Damage *= 1f + settings.InspirationDamageBonus;
                        modifiers.AttackSpeed *= 1f + settings.InspirationAttackSpeedBonus;
                    }
                }
                else if (bloodlust)
                {
                    modifiers.Damage *= 1f + settings.BloodlustDamageBonus;
                    modifiers.MoveSpeed *= 1f + settings.BloodlustMoveSpeedBonus;
                }

                if (buffs.Shield > 0f)
                    modifiers.DamageTaken *= 1f - settings.ShieldDamageReduction;

                if (buffs.LastStand > 0f)
                {
                    modifiers.Damage *= 1f + settings.LastStandDamageBonus;
                    modifiers.AttackSpeed *= 1f + settings.LastStandAttackSpeedBonus;
                }

                if (buffs.Blessing > 0f)
                    modifiers.DamageTaken *= 1f - settings.BlessingDamageReduction;

                modifiersRef.ValueRW = modifiers;
            }
        }
    }
}
