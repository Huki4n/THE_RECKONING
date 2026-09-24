using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TheReckoning
{
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private string battleSceneName;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            // A scene opened from the pause menu must run at normal speed.
            Time.timeScale = 1f;
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
        }

        private void Update()
        {
            if (settingsPanel != null && settingsPanel.activeSelf && EscapePressed())
                CloseSettings();
        }

        public void Play()
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
