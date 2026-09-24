using System;
using System.Collections;
using System.Collections.Generic;
using TheReckoning.Morale;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TheReckoning
{
    public enum OrderAbility { None, HeavenlyStrike, HolyShield, Encouragement, Purification }
    public enum CultAbility { FallenCurse, DarkBlessing }
    public enum CultAbilityEffect { FallenCurse, DarkBlessing, Bloodlust, WillOfTheFallen }

    [Serializable]
    public sealed class CultTriggerSettings
    {
        [Min(1)] public int curseMinimumOrderUnits = 6;
        [Min(0.1f)] public float curseCooldown = 40f;
        [Range(0f, 1f)] public float blessingBaseHealthFraction = .5f;
        [Min(1)] public int blessingMinimumNearbyOrderUnits = 3;
        [Min(0.1f)] public float blessingCooldown = 35f;
        [Min(1)] public int bloodlustOrderDeaths = 5;
        [Min(0.1f)] public float bloodlustDeathWindow = 3f;
        [Range(0f, 1f)] public float lastStandBaseHealthFraction = .25f;

        public static CultTriggerSettings Frequent() => new CultTriggerSettings
        {
            curseMinimumOrderUnits = 4,
            curseCooldown = 22f,
            blessingBaseHealthFraction = .75f,
            blessingMinimumNearbyOrderUnits = 2,
            blessingCooldown = 20f,
            bloodlustOrderDeaths = 3,
            bloodlustDeathWindow = 5f,
            lastStandBaseHealthFraction = .5f
        };
    }

    [Serializable]
    public sealed class CultEffectSettings
    {
        [Range(0f, 1f)] public float curseMaxHealthDamage = .15f;
        [Min(0.1f)] public float curseIndicatorSeconds = 2f;
        [Min(0.1f)] public float blessingSeconds = 5f;
        [Range(0f, .95f)] public float blessingDamageReduction = .30f;
        [Min(0.1f)] public float bloodlustSeconds = 5f;
        [Min(0f)] public float bloodlustDamageBonus = .25f;
        [Min(0f)] public float bloodlustMoveSpeedBonus = .15f;
        [Min(0.1f)] public float lastStandSeconds = 8f;
        [Min(0f)] public float lastStandDamageBonus = .30f;
        [Min(0f)] public float lastStandAttackSpeedBonus = .20f;
    }

    [DisallowMultipleComponent]
    public sealed class AbilitySystem : MonoBehaviour
    {
        [SerializeField] private CultAbility cultAbility = CultAbility.FallenCurse;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private GameObject targetingIndicator;
        [SerializeField] private GameObject impactEffect;
        [SerializeField, Min(0.1f)] private float strikeRadius = 2.1f;
        [SerializeField, Min(0.1f)] private float baseDefenseRadius = 3f;
        [SerializeField] private float battlefieldY;

        [Header("Cult trigger settings")]
        [SerializeField] private bool useFrequentTriggers = true;
        [SerializeField] private CultTriggerSettings originalTriggers = new CultTriggerSettings();
        [SerializeField] private CultTriggerSettings frequentTriggers = CultTriggerSettings.Frequent();
        [SerializeField] private CultEffectSettings cultEffects = new CultEffectSettings();

        private CultTriggerSettings Triggers => useFrequentTriggers ? frequentTriggers : originalTriggers;

        private GameManager match;
        private readonly List<Unit> units = new List<Unit>();
        private readonly Dictionary<Unit, float> shields = new Dictionary<Unit, float>();
        private readonly Dictionary<Unit, float> blessings = new Dictionary<Unit, float>();
        private readonly Dictionary<Unit, float> lastStandBuffs = new Dictionary<Unit, float>();
        private readonly Queue<float> playerDeaths = new Queue<float>();
        private float clock, playerReadyAt, cultReadyAt, encouragementUntil;
        private float bloodlustUntil, productionUntil, aiCheckAt;
        private float curseVisibleUntil, blessingUntil, lastStandUntil;
        private bool lastStandUsed, targeting;

        public OrderAbility Selected { get; private set; }
        public bool IsTargeting => targeting;
        public float CooldownRemaining => Mathf.Max(0f, playerReadyAt - clock);
        public bool CanActivate => match != null && match.IsRunning &&
            Selected != OrderAbility.None && !targeting && CooldownRemaining <= 0f;

        // An instant curse stays visible briefly so the player can see it happened.
        public float CultEffectRemaining(CultAbilityEffect effect)
        {
            if (match == null || !match.IsRunning) return 0f;
            float until;
            switch (effect)
            {
                case CultAbilityEffect.FallenCurse: until = curseVisibleUntil; break;
                case CultAbilityEffect.DarkBlessing: until = blessingUntil; break;
                case CultAbilityEffect.Bloodlust: until = bloodlustUntil; break;
                case CultAbilityEffect.WillOfTheFallen: until = lastStandUntil; break;
                default: return 0f;
            }
            return Mathf.Max(0f, until - clock);
        }

        public string CultEffectDescription(CultAbilityEffect effect)
        {
            CultTriggerSettings triggers = Triggers;
            switch (effect)
            {
                case CultAbilityEffect.FallenCurse:
                    return $"When at least {triggers.curseMinimumOrderUnits} Order units are alive, each loses {cultEffects.curseMaxHealthDamage * 100f:0.#}% of maximum health, but stays above 0 HP. Cooldown: {triggers.curseCooldown:0.#} s.";
                case CultAbilityEffect.DarkBlessing:
                    return $"When the Cult base is below {triggers.blessingBaseHealthFraction * 100f:0.#}% health and {triggers.blessingMinimumNearbyOrderUnits} Order units approach it, current Cult units take {cultEffects.blessingDamageReduction * 100f:0.#}% less damage for {cultEffects.blessingSeconds:0.#} s. Cooldown: {triggers.blessingCooldown:0.#} s.";
                case CultAbilityEffect.Bloodlust:
                    return $"After {triggers.bloodlustOrderDeaths} Order units die within {triggers.bloodlustDeathWindow:0.#} s, Cult units gain {cultEffects.bloodlustDamageBonus * 100f:0.#}% damage and {cultEffects.bloodlustMoveSpeedBonus * 100f:0.#}% movement speed for {cultEffects.bloodlustSeconds:0.#} s.";
                case CultAbilityEffect.WillOfTheFallen:
                    return $"When the Cult base first falls to {triggers.lastStandBaseHealthFraction * 100f:0.#}% health, current Cult units gain {cultEffects.lastStandDamageBonus * 100f:0.#}% damage and {cultEffects.lastStandAttackSpeedBonus * 100f:0.#}% attack speed for {cultEffects.lastStandSeconds:0.#} s.";
                default: return "";
            }
        }

        public void Initialize(GameManager game)
        {
            match = game;
            Selected = OrderAbility.None;
            clock = playerReadyAt = cultReadyAt = encouragementUntil = 0f;
            bloodlustUntil = productionUntil = aiCheckAt = 0f;
            curseVisibleUntil = blessingUntil = lastStandUntil = 0f;
            lastStandUsed = targeting = false;
            shields.Clear(); blessings.Clear(); lastStandBuffs.Clear(); playerDeaths.Clear();
            if (worldCamera == null) worldCamera = Camera.main;
            if (selectionPanel != null) selectionPanel.SetActive(true);
            if (targetingIndicator != null) targetingIndicator.SetActive(false);
        }

        // Wire these four public methods to the pre-match selection buttons.
        public void ChooseHeavenlyStrike() => Choose(OrderAbility.HeavenlyStrike);
        public void ChooseHolyShield() => Choose(OrderAbility.HolyShield);
        public void ChooseEncouragement() => Choose(OrderAbility.Encouragement);
        public void ChoosePurification() => Choose(OrderAbility.Purification);

        private void Choose(OrderAbility choice)
        {
            if (match == null || match.State != MatchState.Preparing || choice == OrderAbility.None)
                return;
            Selected = choice;
            if (selectionPanel != null) selectionPanel.SetActive(false);
            match.BeginMatch();
        }

        // Wire this method to the single in-battle ability button.
        public void ActivateSelected()
        {
            if (!CanActivate) return;
            if (Selected == OrderAbility.HeavenlyStrike)
            {
                if (worldCamera == null) { Debug.LogError("AbilitySystem: assign World Camera.", this); return; }
                targeting = true;
                if (targetingIndicator != null) targetingIndicator.SetActive(true);
                return;
            }

            switch (Selected)
            {
                case OrderAbility.HolyShield:
                    MarkUnits(Team.Left, shields, 5f);
                    playerReadyAt = clock + 35f;
                    break;
                case OrderAbility.Encouragement:
                    encouragementUntil = clock + 6f; // Includes recruits spawned during the buff.
                    playerReadyAt = clock + 35f;
                    break;
                case OrderAbility.Purification:
                    // Cleansing blocks the continuously recalculated Fear aura for 3 s.
                    bool removed = match.Morale.Cleanse(Team.Left, 3f);
                    if (!removed) productionUntil = clock + 3f;
                    playerReadyAt = clock + 30f;
                    break;
            }
        }

        private void Update()
        {
            if (match == null || !match.IsRunning)
            {
                if (targeting) CancelTargeting();
                return;
            }

            clock += Time.deltaTime;
            if (targeting) UpdateTargeting();

            if (!lastStandUsed && match.RightBase.IsAlive &&
                match.RightBase.Health.Current <= match.RightBase.Health.Maximum * Triggers.lastStandBaseHealthFraction)
            {
                lastStandUsed = true;
                MarkUnits(Team.Right, lastStandBuffs, cultEffects.lastStandSeconds);
                lastStandUntil = clock + cultEffects.lastStandSeconds;
            }

            if (clock < aiCheckAt) return;
            aiCheckAt = clock + .2f;
            if (clock < cultReadyAt) return;

            if (cultAbility == CultAbility.FallenCurse && match.CountUnits(Team.Left) >= Triggers.curseMinimumOrderUnits)
            {
                match.CopyUnits(Team.Left, units);
                foreach (Unit unit in units)
                    if (unit != null && unit.IsAlive)
                        unit.TakeNonLethalDamage(unit.Health.Maximum * cultEffects.curseMaxHealthDamage, Team.Right);
                curseVisibleUntil = clock + cultEffects.curseIndicatorSeconds;
                cultReadyAt = clock + Triggers.curseCooldown;
            }
            else if (cultAbility == CultAbility.DarkBlessing &&
                match.RightBase.IsAlive && match.RightBase.Health.Current < match.RightBase.Health.Maximum * Triggers.blessingBaseHealthFraction &&
                EnemiesNearCultBase() >= Triggers.blessingMinimumNearbyOrderUnits)
            {
                MarkUnits(Team.Right, blessings, cultEffects.blessingSeconds);
                blessingUntil = clock + cultEffects.blessingSeconds;
                cultReadyAt = clock + Triggers.blessingCooldown;
            }
        }

        private void UpdateTargeting()
        {
            if (RightClick() || EscapePressed())
            { CancelTargeting(); return; }
            Vector3 point = worldCamera.ScreenToWorldPoint(PointerPosition());
            point.y = battlefieldY;
            point.z = 0f;
            if (targetingIndicator != null) targetingIndicator.transform.position = point;
            if (!LeftClick() ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

            float a = match.LeftBase.transform.position.x, b = match.RightBase.transform.position.x;
            if (point.x < Mathf.Min(a, b) || point.x > Mathf.Max(a, b)) return;
            targeting = false;
            if (targetingIndicator != null) targetingIndicator.SetActive(false);
            playerReadyAt = clock + 30f; // Start only when a valid field point is chosen.
            StartCoroutine(Strike(point));
        }

        private IEnumerator Strike(Vector3 point)
        {
            yield return new WaitForSeconds(.5f);
            if (match == null || !match.IsRunning) yield break;
            if (impactEffect != null)
            {
                GameObject fx = Instantiate(impactEffect, point, Quaternion.identity);
                Destroy(fx, 2f);
            }
            match.CopyUnits(Team.Right, units);
            foreach (Unit unit in units)
                if (unit != null && unit.IsAlive &&
                    Mathf.Abs(unit.transform.position.x - point.x) <= strikeRadius + unit.BodyRadius)
                    unit.TakeDamage(150f, Team.Left);
        }

        private void CancelTargeting()
        {
            targeting = false;
            if (targetingIndicator != null) targetingIndicator.SetActive(false);
        }

        private static Vector3 PointerPosition()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        private static bool LeftClick()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private static bool RightClick()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private int EnemiesNearCultBase()
        {
            int count = 0;
            float x = match.RightBase.transform.position.x;
            match.CopyUnits(Team.Left, units);
            foreach (Unit unit in units)
                if (unit != null && unit.IsAlive &&
                    Mathf.Abs(unit.transform.position.x - x) <= baseDefenseRadius) count++;
            return count;
        }

        private void MarkUnits(Team side, Dictionary<Unit, float> recipients, float duration)
        {
            match.CopyUnits(side, units);
            foreach (Unit unit in units)
                if (unit != null && unit.IsAlive) recipients[unit] = clock + duration;
        }

        private bool Active(Dictionary<Unit, float> recipients, Unit unit) =>
            recipients.TryGetValue(unit, out float until) && clock < until;

        // Called at the confirmed death, once per player casualty.
        public void OnUnitKilled(Unit victim)
        {
            if (victim == null || victim.Side != Team.Left || match == null || !match.IsRunning) return;
            playerDeaths.Enqueue(clock);
            while (playerDeaths.Count > 0 && clock - playerDeaths.Peek() > Triggers.bloodlustDeathWindow)
                playerDeaths.Dequeue();
            if (playerDeaths.Count < Triggers.bloodlustOrderDeaths) return;
            playerDeaths.Clear();
            bloodlustUntil = clock + cultEffects.bloodlustSeconds;
        }

        // Add these percentages to morale bonuses before the final 0.05 clamp.
        public float Bonus(Unit unit, Stat stat)
        {
            if (unit == null || match == null || !match.IsRunning) return 0f;
            float value = 0f;
            if (unit.Side == Team.Left)
            {
                if (clock < encouragementUntil)
                {
                    if (stat == Stat.Damage) value += .25f;
                    if (stat == Stat.AttackSpeed) value += .20f;
                }
                if (stat == Stat.DamageTaken && Active(shields, unit)) value -= .40f;
            }
            else
            {
                if (clock < bloodlustUntil)
                {
                    if (stat == Stat.Damage) value += cultEffects.bloodlustDamageBonus;
                    if (stat == Stat.MoveSpeed) value += cultEffects.bloodlustMoveSpeedBonus;
                }
                if (Active(lastStandBuffs, unit))
                {
                    if (stat == Stat.Damage) value += cultEffects.lastStandDamageBonus;
                    if (stat == Stat.AttackSpeed) value += cultEffects.lastStandAttackSpeedBonus;
                }
                if (stat == Stat.DamageTaken && Active(blessings, unit)) value -= cultEffects.blessingDamageReduction;
            }
            return value;
        }

        public float ProductionBonus(Team side) =>
            side == Team.Left && clock < productionUntil && match != null && match.IsRunning ? .20f : 0f;
    }
}
