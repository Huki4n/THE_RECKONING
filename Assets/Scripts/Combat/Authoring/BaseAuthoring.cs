using TheReckoning.Balance;
using TheReckoning.ECS;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class BaseAuthoring : MonoBehaviour
    {
        [Header("Team")]
        [SerializeField]
        private Team team = Team.Left;

        [Header("Combat")]
        [Tooltip("When assigned, Maximum Health comes from the balance model.")]
        [SerializeField]
        private BalanceConfig balanceConfig;

        [SerializeField, Min(1f)]
        private float maximumHealth = 500f;

        [SerializeField, Min(0.05f)]
        private float bodyRadius = 0.3f;

        private sealed class Baker :
            Baker<BaseAuthoring>
        {
            public override void Bake(
                BaseAuthoring authoring)
            {
                Entity entity =
                    GetEntity(
                        TransformUsageFlags.Dynamic);

                AddComponent<BaseTag>(entity);

                AddComponent(
                    entity,
                    new TeamId
                    {
                        Value =
                            TeamToId(authoring.team)
                    });

                DependsOn(authoring.balanceConfig);

                float maxHealth =
                    Mathf.Max(
                        1f,
                        authoring.balanceConfig != null
                            ? BalanceMath.BaseHealth(authoring.balanceConfig)
                            : authoring.maximumHealth);

                AddComponent(
                    entity,
                    new TheReckoning.ECS.Health
                    {
                        Current = maxHealth,
                        Max = maxHealth
                    });

                AddComponent(
                    entity,
                    new BodyRadius
                    {
                        Value =
                            Mathf.Max(
                                0.05f,
                                authoring.bodyRadius)
                    });

                AddComponent(
                    entity,
                    new StatMultipliers
                    {
                        Damage = 1f,
                        AttackSpeed = 1f,
                        MoveSpeed = 1f,
                        DamageTaken = 1f
                    });

                AddBuffer<DamageEvent>(entity);

                AddComponent<Dead>(entity);

                SetComponentEnabled<Dead>(
                    entity,
                    false);
            }

            private static byte TeamToId(
                Team team)
            {
                return team == Team.Left
                    ? (byte)0
                    : (byte)1;
            }
        }
    }
}