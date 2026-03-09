using UnityEngine;

namespace DeadWalls
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject HUDPanel;
        public GameObject LevelUpPanel;
        public GameObject GameOverPanel;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ShowHUD();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += ShowGameOver;
                GameManager.Instance.OnLevelUp += ShowLevelUp;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= ShowGameOver;
                GameManager.Instance.OnLevelUp -= ShowLevelUp;
            }
        }

        public void ShowHUD()
        {
            HUDPanel.SetActive(true);
            LevelUpPanel.SetActive(false);
            GameOverPanel.SetActive(false);
        }

        public void ShowLevelUp()
        {
            LevelUpPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void ShowGameOver()
        {
            GameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void HideLevelUp()
        {
            LevelUpPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OnRestart()
        {
            GameOverPanel.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance.RestartGame();
        }
    }
}
