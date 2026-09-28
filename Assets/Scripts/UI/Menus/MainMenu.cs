using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TheReckoning
{
    public sealed class MainMenu : MonoBehaviour
    {
        [Serializable]
        private sealed class EraButton
        {
            public Button button;
            [Tooltip("Optional; shows the era name or Locked.")]
            public TMP_Text label;
            [NonSerialized] public UnityAction callback;
        }

        [SerializeField] private string battleSceneName;
        [SerializeField] private EraCatalog eraCatalog;

        [Tooltip("Element index is the era index.")]
        [SerializeField] private EraButton[] eraButtons = new EraButton[0];

        [Tooltip("Starts the latest unlocked era.")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            Time.timeScale = 1f;
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);

            SetupEraButtons();
        }

        private void SetupEraButtons()
        {
            if (eraButtons == null)
                return;

            for (int i = 0; i < eraButtons.Length; i++)
            {
                EraButton entry = eraButtons[i];

                if (entry == null || entry.button == null)
                    continue;

                int era = i;
                EraData data = eraCatalog != null ? eraCatalog.Get(era) : null;
                bool available = data != null && CampaignProgress.IsUnlocked(era);

                entry.button.interactable = available;

                if (entry.label != null)
                    entry.label.text = available ? data.DisplayName : "Locked";

                entry.callback = () => PlayEra(era);
                entry.button.onClick.AddListener(entry.callback);
            }
        }

        public void PlayEra(int era)
        {
            if (!CampaignProgress.IsUnlocked(era))
                return;

            CampaignProgress.Select(era);
            LoadBattle();
        }

        private void Update()
        {
            if (settingsPanel != null && settingsPanel.activeSelf && EscapePressed())
                CloseSettings();
        }

        public void Play()
        {
            int latest = CampaignProgress.UnlockedEra;

            if (eraCatalog != null && eraCatalog.Count > 0)
                latest = Mathf.Min(latest, eraCatalog.Count - 1);

            CampaignProgress.Select(latest);
            LoadBattle();
        }

        private void LoadBattle()
        {
            if (string.IsNullOrWhiteSpace(battleSceneName) ||
                !Application.CanStreamedLevelBeLoaded(battleSceneName))
            {
                Debug.LogError("MainMenu: assign Battle Scene Name and add the battle scene to the build scene list.", this);
                return;
            }

            SceneManager.LoadScene(battleSceneName);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Play);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveListener(CloseSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);

            if (eraButtons != null)
                foreach (EraButton entry in eraButtons)
                    if (entry?.button != null && entry.callback != null)
                        entry.button.onClick.RemoveListener(entry.callback);
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
