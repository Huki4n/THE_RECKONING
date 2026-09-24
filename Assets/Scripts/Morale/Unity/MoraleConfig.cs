using System;
using UnityEngine;
namespace TheReckoning.Morale
{
    // Only trigger conditions live here; effect strengths and durations remain in Settings.
    [Serializable]
    public sealed class MoraleTriggerSettings
    {
        public bool affectEnemyWaveTiming = false;
        [Min(1)] public int burstCount = 5;
        [Min(0.01f)] public float burstWindow = 3f;
        [Min(0.01f)] public float rageWindow = 3f;
        [Range(0.01f, 1f)] public float rageHealthFraction = .25f;
        [Min(1)] public int battleCryVeterans = 3;
        [Min(0.01f)] public float battleCryRadius = 2.5f;
        [Min(0.01f)] public float fearRadius = 2.5f;
        [Min(0f)] public float fearVeteranAge = 10f;
        [Min(0.01f)] public float defenseQuietTime = 2f;
        [Min(1)] public int confidenceKills = 10;
        [Min(0)] public int confidenceAllowedLosses = 1;
        [Min(1)] public int veteranBattles = 3;
        [Min(0.01f)] public float encounterQuietTime = 3f;

        public static MoraleTriggerSettings Frequent() => new MoraleTriggerSettings
        {
            affectEnemyWaveTiming = true,
            burstCount = 3,
            burstWindow = 5f,
            rageWindow = 5f,
            rageHealthFraction = .15f,
            battleCryVeterans = 2,
            battleCryRadius = 3.5f,
            fearRadius = 3.5f,
            fearVeteranAge = 5f,
            defenseQuietTime = 1f,
            confidenceKills = 5,
            confidenceAllowedLosses = 2,
            veteranBattles = 2,
            encounterQuietTime = 1.5f
        };

        public void ApplyTo(MoraleSettings target)
        {
            target.affectEnemyWaveTiming = affectEnemyWaveTiming;
            target.burstCount = burstCount;
            target.burstWindow = burstWindow;
            target.rageWindow = rageWindow;
            target.rageHealthFraction = rageHealthFraction;
            target.battleCryVeterans = battleCryVeterans;
            target.battleCryRadius = battleCryRadius;
            target.fearRadius = fearRadius;
            target.fearVeteranAge = fearVeteranAge;
            target.defenseQuietTime = defenseQuietTime;
            target.confidenceKills = confidenceKills;
            target.confidenceAllowedLosses = confidenceAllowedLosses;
            target.veteranBattles = veteranBattles;
            target.encounterQuietTime = encounterQuietTime;
        }
    }

    [CreateAssetMenu(fileName = "MoraleConfig", menuName = "The Reckoning/Morale Config")]
    public sealed class MoraleConfig : ScriptableObject
    {
        [SerializeField] private MoraleSettings settings = new MoraleSettings();
        [SerializeField] private bool useFrequentTriggers = true;
        [SerializeField] private MoraleTriggerSettings originalTriggers = new MoraleTriggerSettings();
        [SerializeField] private MoraleTriggerSettings frequentTriggers = MoraleTriggerSettings.Frequent();

        public MoraleSettings CreateSettings()
        {
            if (settings == null)
                throw new ArgumentException("MoraleConfig: settings are missing.");
            MoraleSettings result = settings.Snapshot();
            MoraleTriggerSettings triggers = useFrequentTriggers ? frequentTriggers : originalTriggers;
            if (triggers == null)
                throw new ArgumentException("MoraleConfig: trigger settings are missing.");
            triggers.ApplyTo(result);
            result.Validate();
            return result;
        }
        private void OnValidate()
        {
            try
            {
                CreateSettings();
            }
            catch (ArgumentException e) { Debug.LogError(e.Message, this); }
        }
    }
}
    