using UnityEngine;

namespace TheReckoning
{
    public static class CampaignProgress
    {
        private const string UnlockedEraKey = "TheReckoning.UnlockedEra";

        public static int UnlockedEra =>
            Mathf.Max(0, PlayerPrefs.GetInt(UnlockedEraKey, 0));

        public static int SelectedEra { get; private set; }

        public static bool IsUnlocked(int era) =>
            era >= 0 && era <= UnlockedEra;

        public static void Select(int era) =>
            SelectedEra = Mathf.Clamp(era, 0, UnlockedEra);

        public static bool UnlockNext(int completedEra, int eraCount)
        {
            int next = completedEra + 1;

            if (next >= eraCount || next <= UnlockedEra)
                return false;

            PlayerPrefs.SetInt(UnlockedEraKey, next);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(UnlockedEraKey);
            PlayerPrefs.Save();
            SelectedEra = 0;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("The Reckoning/Reset Campaign Progress")]
        private static void ResetFromMenu() => ResetProgress();
#endif
    }
}
