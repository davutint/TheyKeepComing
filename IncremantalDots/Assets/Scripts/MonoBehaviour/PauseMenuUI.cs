using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Pause menusu (M-E): HUD'daki pause butonu paneli acar + timeScale=0 (LevelUp/GameOver
    /// kalibi). RESUME devam ettirir; SETTINGS ses ayarlarini acar; RESTART UIManager.OnRestart
    /// yolunu kullanir (checkpoint silinir, temiz kosu). GameOver acikken pause acilmaz.
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

        private void GoToMainMenu()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.SaveRunSnapshot())
            {
                Debug.LogError("[PauseMenuUI] Exact run snapshot yazilamadi; ilerleme kaybini onlemek icin ana menuye donus iptal edildi.");
                return;
            }

            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameBootstrap.MainMenuSceneName);
        }

        private void OpenPause()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.GameState.IsGameOver)
                return; // olum ekrani kendi pause'unu yonetir

            if (PausePanel != null)
                PausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Resume()
        {
            if (PausePanel != null)
                PausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

    }
}
