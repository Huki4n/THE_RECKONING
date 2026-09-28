using System.Collections;
using TheReckoning.ECS;
using EcsHealth = TheReckoning.ECS.Health;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TheReckoning
{
    public enum OrderAbility
    {
        None,
        HeavenlyWrath,
        HolyShield,
        Inspiration,
        Cleansing
    }

    public enum CultAbility
    {
        FallenCurse,
        DarkBlessing
    }

    public enum CultAbilityEffect
    {
        FallenCurse,
        DarkBlessing,
        Bloodlust,
        WillOfTheFallen
    }

    [DisallowMultipleComponent]
    public sealed class AbilitySystem : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Must match AbilityConfigAuthoring in the battle SubScene.")]
        private AbilityConfig config;

        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private GameObject selectionPanel;

        [SerializeField]
        private GameObject targetingIndicator;

        [SerializeField]
        private GameObject impactEffect;

        [SerializeField]
        private float battlefieldY;

        private GameManager match;

        private World ecsWorld;
        private EntityQuery timersQuery;
        private EntityQuery baseQuery;

        private float clock;
        private float playerReadyAt;

        private bool targeting;

        public OrderAbility Selected { get; private set; }

        public bool IsTargeting => targeting;

        public float CooldownRemaining =>
            Mathf.Max(0f, playerReadyAt - clock);

        public bool CanActivate =>
            match != null &&
            match.IsRunning &&
            config != null &&
            Selected != OrderAbility.None &&
            !targeting &&
            CooldownRemaining <= 0f;

        public float CultEffectRemaining(
            CultAbilityEffect effect)
        {
            if (match == null || !match.IsRunning)
                return 0f;

            if (!TryGetTimers(out AbilityTimers timers))
                return 0f;

            switch (effect)
            {
                case CultAbilityEffect.FallenCurse:
                    return timers.Curse;

                case CultAbilityEffect.DarkBlessing:
                    return timers.Blessing;

                case CultAbilityEffect.Bloodlust:
                    return timers.Bloodlust;

                case CultAbilityEffect.WillOfTheFallen:
                    return timers.LastStand;

                default:
                    return 0f;
            }
        }

        public string CultEffectDescription(
            CultAbilityEffect effect)
        {
            if (config == null)
                return "";

            CultTriggerSettings triggers = config.Triggers;
            CultEffectSettings cultEffects = config.cultEffects;

            switch (effect)
            {
                case CultAbilityEffect.FallenCurse:
                    return
                        $"When at least " +
                        $"{triggers.curseMinimumOrderUnits} Order " +
                        $"units are alive, each loses " +
                        $"{cultEffects.curseMaxHealthDamage * 100f:0.#}% " +
                        $"of maximum health, but stays above 0 HP. " +
                        $"Cooldown: {triggers.curseCooldown:0.#} s.";

                case CultAbilityEffect.DarkBlessing:
                    return
                        $"When the Cult base is below " +
                        $"{triggers.blessingBaseHealthFraction * 100f:0.#}% " +
                        $"health and " +
                        $"{triggers.blessingMinimumNearbyOrderUnits} " +
                        $"Order units approach it, current Cult units " +
                        $"take " +
                        $"{cultEffects.blessingDamageReduction * 100f:0.#}% " +
                        $"less damage for " +
                        $"{cultEffects.blessingSeconds:0.#} s. " +
                        $"Cooldown: {triggers.blessingCooldown:0.#} s.";

                case CultAbilityEffect.Bloodlust:
                    return
                        $"After {triggers.bloodlustOrderDeaths} Order " +
                        $"units die within " +
                        $"{triggers.bloodlustDeathWindow:0.#} s, Cult " +
                        $"units gain " +
                        $"{cultEffects.bloodlustDamageBonus * 100f:0.#}% " +
                        $"damage and " +
                        $"{cultEffects.bloodlustMoveSpeedBonus * 100f:0.#}% " +
                        $"movement speed for " +
                        $"{cultEffects.bloodlustSeconds:0.#} s.";

                case CultAbilityEffect.WillOfTheFallen:
                    return
                        $"When the Cult base first falls to " +
                        $"{triggers.lastStandBaseHealthFraction * 100f:0.#}% " +
                        $"health, current Cult units gain " +
                        $"{cultEffects.lastStandDamageBonus * 100f:0.#}% " +
                        $"damage and " +
                        $"{cultEffects.lastStandAttackSpeedBonus * 100f:0.#}% " +
                        $"attack speed for " +
                        $"{cultEffects.lastStandSeconds:0.#} s.";

                default:
                    return "";
            }
        }

        public void Initialize(GameManager game)
        {
            match = game;

            Selected = OrderAbility.None;

            clock = 0f;
            playerReadyAt = 0f;
            targeting = false;

            if (config == null)
                Debug.LogError("AbilitySystem: assign AbilityConfig.", this);

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (selectionPanel != null)
                selectionPanel.SetActive(true);

            if (targetingIndicator != null)
                targetingIndicator.SetActive(false);
        }

        public void ChooseHeavenlyWrath() =>
            Choose(OrderAbility.HeavenlyWrath);

        public void ChooseHolyShield() =>
            Choose(OrderAbility.HolyShield);

        public void ChooseInspiration() =>
            Choose(OrderAbility.Inspiration);

        public void ChooseCleansing() =>
            Choose(OrderAbility.Cleansing);

        private void Choose(OrderAbility choice)
        {
            if (match == null ||
                match.State != MatchState.Preparing ||
                choice == OrderAbility.None)
            {
                return;
            }

            Selected = choice;

            if (selectionPanel != null)
                selectionPanel.SetActive(false);

            match.BeginMatch();
        }

        public void ActivateSelected()
        {
            if (!CanActivate)
                return;

            if (Selected == OrderAbility.HeavenlyWrath)
            {
                if (worldCamera == null)
                {
                    Debug.LogError(
                        "AbilitySystem: assign World Camera.",
                        this);

                    return;
                }

                targeting = true;

                if (targetingIndicator != null)
                    targetingIndicator.SetActive(true);

                return;
            }

            switch (Selected)
            {
                case OrderAbility.HolyShield:
                    RequestAbility(new AbilityRequest
                    {
                        Kind = AbilityRequestKind.HolyShield,
                        Duration = config.holyShieldSeconds
                    });

                    playerReadyAt = clock + config.holyShieldCooldown;
                    break;

                case OrderAbility.Inspiration:
                    RequestAbility(new AbilityRequest
                    {
                        Kind = AbilityRequestKind.Inspiration,
                        Duration = config.inspirationSeconds
                    });

                    playerReadyAt = clock + config.inspirationCooldown;
                    break;

                case OrderAbility.Cleansing:
                    RequestAbility(new AbilityRequest
                    {
                        Kind = AbilityRequestKind.Cleansing,
                        Duration = config.cleansingSeconds
                    });

                    playerReadyAt = clock + config.cleansingCooldown;
                    break;
            }
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
            {
                if (targeting)
                    CancelTargeting();

                return;
            }

            clock += Time.deltaTime;

            if (targeting)
                UpdateTargeting();
        }

        private void UpdateTargeting()
        {
            if (RightClick() || EscapePressed())
            {
                CancelTargeting();
                return;
            }

            Vector3 point =
                worldCamera.ScreenToWorldPoint(
                    PointerPosition());

            point.y = battlefieldY;
            point.z = 0f;

            if (targetingIndicator != null)
            {
                targetingIndicator.transform.position =
                    point;
            }

            if (!LeftClick() ||
                (EventSystem.current != null &&
                 EventSystem.current.IsPointerOverGameObject()))
            {
                return;
            }

            if (!TryGetBaseX(0, out float leftBaseX) ||
                !TryGetBaseX(1, out float rightBaseX))
            {
                return;
            }

            if (point.x < Mathf.Min(
                    leftBaseX,
                    rightBaseX) ||
                point.x > Mathf.Max(
                    leftBaseX,
                    rightBaseX))
            {
                return;
            }

            targeting = false;

            if (targetingIndicator != null)
                targetingIndicator.SetActive(false);

            playerReadyAt = clock + config.heavenlyWrathCooldown;

            StartCoroutine(
                HeavenlyWrathImpact(point));
        }

        private IEnumerator HeavenlyWrathImpact(
            Vector3 point)
        {
            yield return new WaitForSeconds(config.heavenlyWrathDelay);

            if (match == null || !match.IsRunning)
                yield break;

            if (impactEffect != null)
            {
                GameObject fx =
                    Instantiate(
                        impactEffect,
                        point,
                        Quaternion.identity);

                Destroy(fx, 2f);
            }

            RequestAbility(new AbilityRequest
            {
                Kind = AbilityRequestKind.HeavenlyWrath,
                X = point.x,
                Radius = config.heavenlyWrathRadius,
                Amount = config.heavenlyWrathDamage
            });
        }

        private static void RequestAbility(
            AbilityRequest request)
        {
            if (!TryGetEntityManager(
                    out EntityManager entityManager))
            {
                return;
            }

            request.Team = 0;

            Entity entity =
                entityManager.CreateEntity(
                    typeof(AbilityRequest));

            entityManager.SetComponentData(
                entity,
                request);
        }

        private bool TryGetBaseX(
            byte teamId,
            out float x)
        {
            x = 0f;

            if (!TryPrepareEcsQueries())
                return false;

            using NativeArray<TeamId> teams =
                baseQuery.ToComponentDataArray<TeamId>(
                    Allocator.Temp);

            using NativeArray<EcsHealth> health =
                baseQuery.ToComponentDataArray<EcsHealth>(
                    Allocator.Temp);

            using NativeArray<LocalTransform> transforms =
                baseQuery.ToComponentDataArray<LocalTransform>(
                    Allocator.Temp);

            for (int i = 0;
                 i < teams.Length;
                 i++)
            {
                if (teams[i].Value != teamId ||
                    health[i].Current <= 0f)
                {
                    continue;
                }

                x = transforms[i].Position.x;
                return true;
            }

            return false;
        }

        private bool TryGetTimers(
            out AbilityTimers timers)
        {
            timers = default;

            if (!TryPrepareEcsQueries() ||
                timersQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            timers = timersQuery.GetSingleton<AbilityTimers>();
            return true;
        }

        private bool TryPrepareEcsQueries()
        {
            if (!TryGetEntityManager(
                    out EntityManager entityManager))
            {
                return false;
            }

            World world =
                World.DefaultGameObjectInjectionWorld;

            if (ecsWorld == world)
                return true;

            ecsWorld = world;

            timersQuery =
                entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<AbilityTimers>());

            EntityQueryBuilder builder =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<BaseTag, TeamId, EcsHealth, LocalTransform>()
                    .WithDisabled<Dead>();

            baseQuery = builder.Build(entityManager);
            builder.Dispose();

            return true;
        }

        private static bool TryGetEntityManager(
            out EntityManager entityManager)
        {
            World world =
                World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                entityManager = default;
                return false;
            }

            entityManager = world.EntityManager;
            return true;
        }

        private void CancelTargeting()
        {
            targeting = false;

            if (targetingIndicator != null)
                targetingIndicator.SetActive(false);
        }

        private static Vector3 PointerPosition()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null
                ? (Vector3)Mouse.current.position.ReadValue()
                : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static bool LeftClick()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null &&
                   Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool RightClick()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null &&
                   Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null &&
                   Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public float ProductionBonus(Team side) =>
            side == Team.Left &&
            config != null &&
            match != null &&
            match.IsRunning &&
            TryGetTimers(out AbilityTimers timers) &&
            timers.Production > 0f
                ? config.cleansingProductionBonus
                : 0f;
    }
}
