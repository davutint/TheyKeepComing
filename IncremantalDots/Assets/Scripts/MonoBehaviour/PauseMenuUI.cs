using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Pause menusu (M-E): HUD'daki pause butonu paneli acar ve merkezi lease ile hem
    /// timeScale'i hem DOTS SimulationSystemGroup'u durdurur. Heart gibi baska modal owner'lar
    /// varsa RESUME yalniz kendi lease'ini birakir. SETTINGS ses ayarlarini acar; aktif kosuda
    /// gonullu RESTART yoktur. GameOver acikken pause acilmaz.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Bindings (setup tool baglar)")]
        public Button PauseButton;
        public GameObject PausePanel;
        public Button ResumeButton;
        public Button SettingsButton;
        public Button RestartButton;
        public Button MainMenuButton;
        public SettingsUI Settings;

        private System.IDisposable _pauseLease;

        private void Start()
        {
            if (PauseButton != null)
                PauseButton.onClick.AddListener(OpenPause);
            if (ResumeButton != null)
                ResumeButton.onClick.AddListener(Resume);
            if (SettingsButton != null)
                SettingsButton.onClick.AddListener(() => Settings?.Open());
            if (RestartButton != null)
                RestartButton.gameObject.SetActive(false); // V1: aktif kosuda gonullu reset yok
            if (MainMenuButton != null)
                MainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void Update()
        {
            if (_pauseLease != null)
                SimulationPauseService.EnforcePausedState();
        }

        private void OnDisable()
        {
            ReleasePause();
        }

        private void GoToMainMenu()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.SaveRunSnapshot())
            {
                Debug.LogError("[PauseMenuUI] Exact run snapshot yazilamadi; ilerleme kaybini onlemek icin ana menuye donus iptal edildi.");
                return;
            }

            ReleasePause();
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameBootstrap.MainMenuSceneName);
        }

        private void OpenPause()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.GameState.IsGameOver)
                return; // olum ekrani kendi pause'unu yonetir

            if (PausePanel != null)
                PausePanel.SetActive(true);
            if (_pauseLease == null)
                _pauseLease = SimulationPauseService.Acquire(nameof(PauseMenuUI));
        }

        private void Resume()
        {
            if (PausePanel != null)
                PausePanel.SetActive(false);
            ReleasePause();
        }

        private void ReleasePause()
        {
            _pauseLease?.Dispose();
            _pauseLease = null;
        }

    }
}
