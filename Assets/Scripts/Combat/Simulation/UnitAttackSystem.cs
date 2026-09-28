using TheReckoning.ECS.Data;
using TheReckoning.ECS.Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TheReckoning.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    public partial struct UnitAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTag>();
            state.RequireForUpdate<MatchRunning>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime =
                SystemAPI.Time.DeltaTime;

            ComponentLookup<LocalTransform> transformLookup =
                SystemAPI.GetComponentLookup<LocalTransform>(true);

            ComponentLookup<BodyRadius> radiusLookup =
                SystemAPI.GetComponentLookup<BodyRadius>(true);

            ComponentLookup<Health> healthLookup =
                SystemAPI.GetComponentLookup<Health>(true);

            ComponentLookup<Dead> deadLookup =
                SystemAPI.GetComponentLookup<Dead>(true);

            BufferLookup<DamageEvent> damageLookup =
                SystemAPI.GetBufferLookup<DamageEvent>();

            ComponentLookup<ViewAttackEvent> viewAttackLookup =
                SystemAPI.GetComponentLookup<ViewAttackEvent>();

            ComponentLookup<MoraleContact> moraleContactLookup =
                SystemAPI.GetComponentLookup<MoraleContact>();

            var job = new AttackJob
            {
                DeltaTime = deltaTime,

                TransformLookup = transformLookup,
                RadiusLookup = radiusLookup,
                HealthLookup = healthLookup,
                DeadLookup = deadLookup,

                DamageLookup = damageLookup,
                ViewAttackLookup = viewAttackLookup,
                MoraleContactLookup = moraleContactLookup
            };

            state.Dependency =
                job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(UnitTag))]
        [WithDisabled(typeof(Dead))]
        private partial struct AttackJob : IJobEntity
        {
            public float DeltaTime;

            [ReadOnly]
            public ComponentLookup<LocalTransform>
                TransformLookup;

            [ReadOnly]
            public ComponentLookup<BodyRadius>
                RadiusLookup;

            [ReadOnly]
            public ComponentLookup<Health>
                HealthLookup;

            [ReadOnly]
            public ComponentLookup<Dead>
                DeadLookup;

            public BufferLookup<DamageEvent>
                DamageLookup;

            public ComponentLookup<ViewAttackEvent>
                ViewAttackLookup;

            public ComponentLookup<MoraleContact>
                MoraleContactLookup;

            private void Execute(
                Entity entity,
                ref AttackCooldown cooldown,
                in LocalTransform transform,
                in BodyRadius bodyRadius,
                in Target target,
                in TeamId team,
                in UnitStatsRef statsRef,
                in StatMultipliers multipliers)
            {
                cooldown.Remaining =
                    math.max(
                        0f,
                        cooldown.Remaining - DeltaTime);

                Entity targetEntity =
                    target.Value;

                if (targetEntity == Entity.Null)
                    return;

                if (!TransformLookup.HasComponent(targetEntity))
                    return;

                if (!RadiusLookup.HasComponent(targetEntity))
                    return;

                if (!HealthLookup.HasComponent(targetEntity))
                    return;

                if (!DamageLookup.HasBuffer(targetEntity))
                    return;

                if (DeadLookup.HasComponent(targetEntity) &&
                    DeadLookup.IsComponentEnabled(targetEntity))
                {
                    return;
                }

                Health targetHealth =
                    HealthLookup[targetEntity];

                if (targetHealth.Current <= 0f)
                    return;

                float targetX =
                    TransformLookup[targetEntity]
                        .Position.x;

                float centerDistance =
                    math.abs(
                        targetX -
                        transform.Position.x);

                float targetRadius =
                    RadiusLookup[targetEntity].Value;

                float gap =
                    centerDistance
                    - bodyRadius.Value
                    - targetRadius;

                gap = math.max(0f, gap);

                float attackRange =
                    statsRef.Value.Value.AttackRange;

                if (gap > attackRange)
                    return;

                if (MoraleContactLookup.HasComponent(entity))
                {
                    MoraleContactLookup.SetComponentEnabled(
                        entity,
                        true);
                }

                if (MoraleContactLookup.HasComponent(targetEntity))
                {
                    MoraleContactLookup.SetComponentEnabled(
                        targetEntity,
                        true);
                }

                if (cooldown.Remaining > 0f)
                    return;

                float damage =
                    statsRef.Value.Value.Damage *
                    math.max(
                        0f,
                        multipliers.Damage);

                if (damage <= 0f)
                    return;

                DynamicBuffer<DamageEvent> damageBuffer =
                    DamageLookup[targetEntity];

                damageBuffer.Add(
                    new DamageEvent
                    {
                        Source = entity,
                        Amount = damage,
                        AttackerTeam = team.Value,
                        NonLethal = false
                    });

                float attackSpeed =
                    math.max(
                        0.01f,
                        multipliers.AttackSpeed);

                float attackInterval =
                    statsRef.Value.Value.AttackInterval;

                cooldown.Remaining =
                    attackInterval / attackSpeed;

                if (ViewAttackLookup.HasComponent(entity))
                {
                    ViewAttackLookup.SetComponentEnabled(
                        entity,
                        true);
                }
            }
        }
    }
}