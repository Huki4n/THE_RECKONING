using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TheReckoning
{
    // Read Escape before AbilitySystem handles it, so targeting can cancel first.
    [DefaultExecutionOrder(-100)]
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameManager match;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName;

        private bool isPaused;

        private void Start()
        {
            if (match == null || pausePanel == null)
            {
                Debug.LogError("PauseMenu: assign GameManager and Pause Panel.", this);
                enabled = false;
                return;
            }

            pausePanel.SetActive(false);

            if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void Update()
        {
            if (match == null) return;

            if (isPaused && match.State != MatchState.Running)
            {
                Resume();
                return;
            }

            if (pauseButton != null)
                pauseButton.interactable = !isPaused && match.State == MatchState.Running;

            bool escapePressed = EscapePressed();
            bool pPressed = TogglePressed();

            if (isPaused)
            {
                if (escapePressed || pPressed) Resume();
                return;
            }

            // While aiming Heavenly Wrath, Escape cancels the aim instead of opening pause.
            if (escapePressed && match.Abilities != null && match.Abilities.IsTargeting)
                return;

            if (escapePressed || pPressed) Pause();
        }

        public void Pause()
        {
            if (isPaused || match == null || match.State != MatchState.Running) return;

            isPaused = true;
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (!isPaused) return;

            Time.timeScale = 1f;
            isPaused = false;
            pausePanel.SetActive(false);
        }

        public void Restart()
        {
            Resume();
            if (match != null) match.Restart();
        }

        public void GoToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName) ||
                !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("PauseMenu: assign Main Menu Scene Name and add that scene to the build list.", this);
                return;
            }

            Resume();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnDisable()
        {
            if (isPaused) Resume();
        }

        private void OnDestroy()
        {
            if (isPaused) Time.timeScale = 1f;

            if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }

        private static bool TogglePressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.P);
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
    }
}
