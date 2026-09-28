using System;
using System.Collections.Generic;
using TheReckoning.Morale;
using UnityEditor;
using UnityEngine;

namespace TheReckoning.Balance.EditorTools
{
    public sealed class BalanceWindow : EditorWindow
    {
        private const float CrossoverSearchSeconds = 900f;
        private const float DeviationWarning = .15f;
        private const float NameWidth = 170f;
        private const float CellWidth = 58f;
        private const int PreviewWaves = 15;

        private BalanceConfig config;
        private AbilityConfig abilities;
        private MoraleConfig morale;
        private Vector2 scroll;

        private readonly List<UnitData> units = new List<UnitData>();
        private readonly List<TurretData> turrets = new List<TurretData>();

        [MenuItem("The Reckoning/Balance")]
        private static void Open() =>
            GetWindow<BalanceWindow>("Balance");

        private void OnEnable()
        {
            if (config == null)
                config = FindFirst<BalanceConfig>();

            if (abilities == null)
                abilities = FindFirst<AbilityConfig>();

            if (morale == null)
                morale = FindFirst<MoraleConfig>();

            Refresh();
        }

        private void OnFocus() => Refresh();

        private void Refresh()
        {
            units.Clear();
            turrets.Clear();
            units.AddRange(FindAll<UnitData>());
            turrets.AddRange(FindAll<TurretData>());
            units.Sort((a, b) => a.EcsUnitId.CompareTo(b.EcsUnitId));
            turrets.Sort((a, b) => a.EcsTurretId.CompareTo(b.EcsTurretId));
        }

        private void OnGUI()
        {
            config = (BalanceConfig)EditorGUILayout.ObjectField(
                "Balance Config", config, typeof(BalanceConfig), false);
            abilities = (AbilityConfig)EditorGUILayout.ObjectField(
                "Ability Config", abilities, typeof(AbilityConfig), false);
            morale = (MoraleConfig)EditorGUILayout.ObjectField(
                "Morale Config", morale, typeof(MoraleConfig), false);

            if (config == null)
            {
                EditorGUILayout.HelpBox(
                    "Create one: Assets > Create > The Reckoning > Balance Config.",
                    MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawSummary();
            EditorGUILayout.Space();
            DrawUnits();
            EditorGUILayout.Space();
            DrawTurrets();
            EditorGUILayout.Space();
            DrawWaves();
            EditorGUILayout.Space();
            DrawAbilities();
            EditorGUILayout.Space();
            DrawMorale();
            EditorGUILayout.Space();
            DrawPacing();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);

            for (int tier = 1; tier <= 3; tier++)
            {
                float power = BalanceMath.TierPower(config, tier);
                float cost = BalanceMath.UnitCost(config, power);
                BalanceMath.RoleStats(config, power, UnitRole.Fighter, out float hp, out float dps);
                float turretDps = BalanceMath.TurretTierDps(config, tier);

                EditorGUILayout.LabelField(
                    $"Tier {tier}",
                    $"unit power {power:0.#}, cost {cost:0}, fighter {hp:0} HP / {dps:0.#} DPS, " +
                    $"spawn {BalanceMath.SpawnInterval(config, cost):0.##} s, " +
                    $"reward {BalanceMath.KillReward(config, cost):0} | " +
                    $"turret {turretDps:0.#} eff. DPS, cost {BalanceMath.TurretCost(config, turretDps):0}");
            }

            EditorGUILayout.LabelField(
                "Base health",
                $"{BalanceMath.BaseHealth(config):0} (assign Balance Config on BaseAuthoring)");

            EditorGUILayout.LabelField(
                "Economy",
                $"start {config.startingGold}, income {config.incomePerSecond:0.#}/s " +
                $"(= {config.incomePerSecond / config.goldPerPower:0.#} power/s, " +
                $"assign Balance Config on Economy)");
        }

        private void DrawUnits()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Units", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply all units", GUILayout.Width(120f)))
            {
                foreach (UnitData unit in units)
                    ApplyUnit(unit);

                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();

            BeginRow(true, "Unit");
            Cells(true, "Tier", "Role", "HP", "DPS", "Power", "Target", "Dev",
                "Cost", "Model", "Dev", "Spawn", "Reward");
            EndRow();

            foreach (UnitData unit in units)
            {
                float dps = BalanceMath.Dps(unit.Damage, unit.AttackInterval);
                float power = BalanceMath.UnitPower(config, unit);
                float target = BalanceMath.TierPower(config, unit.Tier);
                float modelCost = BalanceMath.UnitCost(config, target);

                BeginRow(false, $"{unit.EcsUnitId}: {unit.DisplayName}");
                Cells(false,
                    $"{unit.Tier}",
                    $"{unit.Role}",
                    $"{unit.MaximumHealth:0}",
                    $"{dps:0.#}",
                    $"{power:0.#}",
                    $"{target:0.#}",
                    DeviationLabel(Deviation(power, target)),
                    $"{unit.Cost}",
                    $"{modelCost:0}",
                    DeviationLabel(Deviation(unit.Cost, modelCost)),
                    $"{unit.SpawnInterval:0.##}",
                    $"{unit.KillReward}");

                if (GUILayout.Button("Apply", GUILayout.Width(52f)))
                {
                    ApplyUnit(unit);
                    AssetDatabase.SaveAssets();
                }

                EndRow();
            }
        }

        private void ApplyUnit(UnitData unit)
        {
            BalanceMath.UnitTargetStats(
                config,
                unit.Tier,
                unit.Role,
                unit.MoveSpeed,
                unit.AttackRange,
                out float health,
                out float dps);

            int cost = Mathf.Max(
                1,
                Mathf.RoundToInt(BalanceMath.UnitCost(config, BalanceMath.TierPower(config, unit.Tier))));

            var data = new SerializedObject(unit);
            data.FindProperty("maximumHealth").floatValue = Mathf.Max(1f, Mathf.Round(health));
            data.FindProperty("damage").floatValue = Round(dps * unit.AttackInterval, 1);
            data.FindProperty("cost").intValue = cost;
            data.FindProperty("spawnInterval").floatValue =
                Mathf.Max(0.05f, Round(BalanceMath.SpawnInterval(config, cost), 2));
            data.FindProperty("killReward").intValue =
                Mathf.RoundToInt(BalanceMath.KillReward(config, cost));
            data.ApplyModifiedProperties();
        }

        private void DrawTurrets()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Turrets", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply all turrets", GUILayout.Width(120f)))
            {
                foreach (TurretData turret in turrets)
                    ApplyTurret(turret);

                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();

            BeginRow(true, "Turret");
            Cells(true, "Tier", "DPS", "Range", "Splash", "Eff. DPS", "Target", "Dev",
                "Cost", "Model", "Dev", "Gold/DPS", "");
            EndRow();

            foreach (TurretData turret in turrets)
            {
                float dps = BalanceMath.Dps(turret.Damage, turret.AttackInterval);
                float effective = BalanceMath.TurretEffectiveDps(config, turret);
                float target = BalanceMath.TurretTierDps(config, turret.Tier);
                float modelCost = BalanceMath.TurretCost(config, target);

                BeginRow(false, $"{turret.EcsTurretId}: {turret.DisplayName}");
                Cells(false,
                    $"{turret.Tier}",
                    $"{dps:0.#}",
                    $"{turret.Range:0.#}",
                    $"{turret.SplashRadius:0.#}",
                    $"{effective:0.#}",
                    $"{target:0.#}",
                    DeviationLabel(Deviation(effective, target)),
                    $"{turret.Cost}",
                    $"{modelCost:0}",
                    DeviationLabel(Deviation(turret.Cost, modelCost)),
                    $"{turret.Cost / Mathf.Max(0.01f, effective):0.##}",
                    "");

                if (GUILayout.Button("Apply", GUILayout.Width(52f)))
                {
                    ApplyTurret(turret);
                    AssetDatabase.SaveAssets();
                }

                EndRow();
            }
        }

        private void ApplyTurret(TurretData turret)
        {
            float splash = BalanceMath.TurretSplash(config, turret.ArcHeight > 0f);
            float dps = BalanceMath.TurretTargetDps(config, turret.Tier, turret.Range, splash);
            float cost = BalanceMath.TurretCost(config, BalanceMath.TurretTierDps(config, turret.Tier));

            var data = new SerializedObject(turret);
            data.FindProperty("splashRadius").floatValue = splash;
            data.FindProperty("damage").floatValue = Mathf.Max(0.01f, Round(dps * turret.AttackInterval, 1));
            data.FindProperty("cost").intValue = Mathf.Max(1, Mathf.RoundToInt(cost));
            data.ApplyModifiedProperties();
        }

        private void DrawWaves()
        {
            EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);

            BeginRow(true, "Wave");
            Cells(true, "Start", "Budget", "Total", "≈ T1", "≈ T2", "≈ T3");
            EndRow();

            float total = 0f;

            for (int wave = 0; wave < PreviewWaves; wave++)
            {
                float budget = BalanceMath.WaveBudget(config, wave);
                total += budget;

                BeginRow(false, $"{wave + 1}");
                Cells(false,
                    $"{BalanceMath.WaveStartTime(config, wave):0} s",
                    $"{budget:0}",
                    $"{total:0}",
                    $"{budget / BalanceMath.TierPower(config, 1):0.#}",
                    $"{budget / BalanceMath.TierPower(config, 2):0.#}",
                    $"{budget / BalanceMath.TierPower(config, 3):0.#}");
                EndRow();
            }
        }

        private void DrawAbilities()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);

            if (abilities != null &&
                GUILayout.Button("Apply abilities", GUILayout.Width(120f)))
            {
                ApplyAbilities();
            }

            EditorGUILayout.EndHorizontal();

            if (abilities == null)
            {
                EditorGUILayout.HelpBox("Assign Ability Config.", MessageType.Info);
                return;
            }

            BeginRow(true, "Ability");
            Cells(true, "Seconds", "Cooldown", "Power ×", "Average", "Model cd", "Dev");
            EndRow();

            float wrathModel = AbilityBalance.WrathDamage(config);
            BeginRow(false, "Heavenly Wrath (Order)");
            Cells(false,
                "hit",
                $"{abilities.heavenlyWrathCooldown:0.#}",
                $"{abilities.heavenlyWrathDamage:0} dmg",
                "",
                $"{wrathModel:0} dmg",
                DeviationLabel(Deviation(abilities.heavenlyWrathDamage, wrathModel)));
            EndRow();

            CooldownRow(
                "Holy Shield (Order)",
                AbilityBalance.ShieldMultiplier(config, abilities),
                abilities.holyShieldSeconds,
                abilities.holyShieldCooldown);

            CooldownRow(
                "Inspiration (Order)",
                AbilityBalance.InspirationMultiplier(config, abilities),
                abilities.inspirationSeconds,
                abilities.inspirationCooldown);

            BeginRow(false, "Cleansing (Order)");
            Cells(false,
                $"{abilities.cleansingSeconds:0.#}",
                $"{abilities.cleansingCooldown:0.#}",
                $"+{abilities.cleansingProductionBonus * 100f:0}% prod",
                "", "", "");
            EndRow();

            CultAbilityEffectsRows();

            EditorGUILayout.LabelField(
                "Average = power bonus × uptime (seconds / cooldown). " +
                $"Model cd gives {config.abilityTargetBonus * 100f:0.#}% average; " +
                $"Wrath kills a tier {config.wrathKillsTier} fighter.",
                EditorStyles.miniLabel);
        }

        private void CultAbilityEffectsRows()
        {
            CultEffectSettings effects = abilities.cultEffects;
            CultTriggerSettings triggers = abilities.Triggers;
            string active = abilities.cultAbility == CultAbility.FallenCurse ? "active" : "off";

            BeginRow(false, $"Fallen Curse (Cult, {active})");
            Cells(false,
                "hit",
                $"{triggers.curseCooldown:0.#}",
                $"-{effects.curseMaxHealthDamage * 100f:0}% HP",
                $"{effects.curseMaxHealthDamage * 60f / triggers.curseCooldown * 100f:0.#}%/min",
                "", "");
            EndRow();

            if (abilities.cultAbility == CultAbility.DarkBlessing)
            {
                CooldownRow(
                    "Dark Blessing (Cult, active)",
                    AbilityBalance.BlessingMultiplier(config, abilities),
                    effects.blessingSeconds,
                    triggers.blessingCooldown);
            }
            else
            {
                TriggeredRow(
                    "Dark Blessing (Cult, off)",
                    AbilityBalance.BlessingMultiplier(config, abilities),
                    effects.blessingSeconds);
            }

            TriggeredRow(
                "Bloodlust (Cult)",
                AbilityBalance.BloodlustMultiplier(config, abilities),
                effects.bloodlustSeconds);

            TriggeredRow(
                "Will of the Fallen (Cult, once)",
                AbilityBalance.LastStandMultiplier(config, abilities),
                effects.lastStandSeconds);
        }

        private void CooldownRow(string name, float multiplier, float seconds, float cooldown)
        {
            float model = AbilityBalance.CooldownForBonus(multiplier, seconds, config.abilityTargetBonus);

            BeginRow(false, name);
            Cells(false,
                $"{seconds:0.#}",
                $"{cooldown:0.#}",
                $"×{multiplier:0.##}",
                $"+{AbilityBalance.AverageBonus(multiplier, seconds, cooldown) * 100f:0.#}%",
                $"{model:0}",
                DeviationLabel(Deviation(cooldown, model)));
            EndRow();
        }

        private static void TriggeredRow(string name, float multiplier, float seconds)
        {
            BeginRow(false, name);
            Cells(false, $"{seconds:0.#}", "trigger", $"×{multiplier:0.##}", "", "", "");
            EndRow();
        }

        private void ApplyAbilities()
        {
            Undo.RecordObject(abilities, "Apply ability balance");

            abilities.heavenlyWrathDamage = Mathf.Round(AbilityBalance.WrathDamage(config));

            abilities.holyShieldCooldown = Mathf.Round(AbilityBalance.CooldownForBonus(
                AbilityBalance.ShieldMultiplier(config, abilities),
                abilities.holyShieldSeconds,
                config.abilityTargetBonus));

            abilities.inspirationCooldown = Mathf.Round(AbilityBalance.CooldownForBonus(
                AbilityBalance.InspirationMultiplier(config, abilities),
                abilities.inspirationSeconds,
                config.abilityTargetBonus));

            EditorUtility.SetDirty(abilities);
            AssetDatabase.SaveAssets();
        }

        private void DrawMorale()
        {
            EditorGUILayout.LabelField("Morale", EditorStyles.boldLabel);

            if (morale == null)
            {
                EditorGUILayout.HelpBox("Assign Morale Config.", MessageType.Info);
                return;
            }

            MoraleSettings settings;

            try
            {
                settings = morale.CreateSettings();
            }
            catch (ArgumentException exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }

            BeginRow(true, "Effect");
            Cells(true, "Seconds", "Power ×", "Prod");
            EndRow();

            foreach (MoraleEffect effect in Enum.GetValues(typeof(MoraleEffect)))
            {
                EffectSettings values = settings.Get(effect);
                float multiplier = AbilityBalance.PowerMultiplier(
                    config,
                    values.damage,
                    values.attackSpeed,
                    values.damageTaken,
                    values.moveSpeed);

                BeginRow(false, values.enabled ? $"{effect}" : $"{effect} (off)");
                Cells(false,
                    values.duration > 0f ? $"{values.duration:0.#}" : "aura",
                    $"×{multiplier:0.##}",
                    values.productionSpeed != 0f ? $"{values.productionSpeed * 100f:+0;-0}%" : "");
                EndRow();
            }

            BeginRow(false, "Veteran");
            Cells(false,
                "",
                $"×{AbilityBalance.PowerMultiplier(config, settings.veteranDamageBonus, 0f, 0f):0.##}",
                "");
            EndRow();

            EditorGUILayout.LabelField(
                "Morale effects fire on events, so they are shown per activation and not included in pacing.",
                EditorStyles.miniLabel);
        }

        private float ChartSeconds => config.targetMatchMax * 1.5f;

        private void DrawPacing()
        {
            EditorGUILayout.LabelField("Match pacing", EditorStyles.boldLabel);

            float playerMultiplier = AbilityBalance.PlayerMultiplier(config, abilities);
            float waveMultiplier = AbilityBalance.WaveMultiplier(config, abilities);
            float crossover = BalanceMath.EstimateCrossover(
                config,
                CrossoverSearchSeconds,
                playerMultiplier,
                waveMultiplier);
            string verdict;
            MessageType type;

            if (crossover < 0f)
            {
                verdict = $"Waves never overtake the player within {CrossoverSearchSeconds:0} s.";
                type = MessageType.Warning;
            }
            else if (crossover < config.targetMatchMin || crossover > config.targetMatchMax)
            {
                verdict = $"Waves overtake the player at {crossover:0} s, " +
                          $"target is {config.targetMatchMin:0}-{config.targetMatchMax:0} s.";
                type = MessageType.Warning;
            }
            else
            {
                verdict = $"Waves overtake the player at {crossover:0} s, within target.";
                type = MessageType.Info;
            }

            EditorGUILayout.HelpBox(
                verdict +
                "\nGreen: power the player can buy. Red: power sent by waves." +
                $"\nAbility bonus: Order ×{playerMultiplier:0.###}, Cult ×{waveMultiplier:0.###} " +
                "(green line is scaled by their ratio).",
                type);

            Rect rect = GUILayoutUtility.GetRect(10f, 220f, GUILayout.ExpandWidth(true));
            DrawChart(rect, crossover);
        }

        private void DrawChart(Rect rect, float crossover)
        {
            EditorGUI.DrawRect(rect, new Color(.15f, .15f, .15f));

            float seconds = ChartSeconds;
            float playerMultiplier = AbilityBalance.PlayerMultiplier(config, abilities);
            float waveMultiplier = AbilityBalance.WaveMultiplier(config, abilities);
            float maxPower = Mathf.Max(
                1f,
                BalanceMath.CumulativePlayerPower(config, seconds, playerMultiplier, waveMultiplier) * 1.5f);

            Vector2 Point(float time, float power) =>
                new Vector2(
                    rect.x + rect.width * time / seconds,
                    rect.yMax - rect.height * Mathf.Clamp01(power / maxPower));

            Handles.BeginGUI();

            Handles.color = new Color(.3f, .5f, .3f, .35f);
            float minX = Point(config.targetMatchMin, 0f).x;
            float maxX = Point(config.targetMatchMax, 0f).x;
            Handles.DrawAAConvexPolygon(
                new Vector3(minX, rect.y), new Vector3(maxX, rect.y),
                new Vector3(maxX, rect.yMax), new Vector3(minX, rect.yMax));

            const int samples = 200;
            var player = new Vector3[samples + 1];
            var waves = new Vector3[samples + 1];

            for (int i = 0; i <= samples; i++)
            {
                float time = seconds * i / samples;
                player[i] = Point(
                    time,
                    BalanceMath.CumulativePlayerPower(config, time, playerMultiplier, waveMultiplier));
                waves[i] = Point(time, BalanceMath.CumulativeWavePower(config, time));
            }

            Handles.color = Color.green;
            Handles.DrawAAPolyLine(2f, player);
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(2f, waves);

            if (crossover >= 0f && crossover <= seconds)
            {
                Handles.color = Color.yellow;
                float x = Point(crossover, 0f).x;
                Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
            }

            Handles.EndGUI();

            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 200f, 18f), $"{maxPower:0} power");
            GUI.Label(new Rect(rect.xMax - 60f, rect.yMax - 18f, 60f, 18f), $"{seconds:0} s");
        }

        private static float Round(float value, int digits)
        {
            float scale = Mathf.Pow(10f, digits);
            return Mathf.Round(value * scale) / scale;
        }

        private static float Deviation(float actual, float model) =>
            model > 0f ? (actual - model) / model : 0f;

        private static string DeviationLabel(float deviation)
        {
            string label = $"{deviation * 100f:+0;-0;0}%";
            return Mathf.Abs(deviation) > DeviationWarning ? label + " !" : label;
        }

        private static void BeginRow(bool header, string name)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                name,
                header ? EditorStyles.boldLabel : EditorStyles.label,
                GUILayout.Width(NameWidth));
        }

        private static void Cells(bool header, params string[] cells)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;

            foreach (string cell in cells)
                EditorGUILayout.LabelField(cell, style, GUILayout.Width(CellWidth));
        }

        private static void EndRow() =>
            EditorGUILayout.EndHorizontal();

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static IEnumerable<T> FindAll<T>() where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                    yield return asset;
            }
        }
    }
}
