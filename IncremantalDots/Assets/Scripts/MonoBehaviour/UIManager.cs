using UnityEngine;

namespace DeadWalls
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject HUDPanel;
        public GameObject LevelUpPanel;
        public GameObject MarketPanel;
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
                GameManager.Instance.OnWaveCompleted += ShowMarket;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= ShowGameOver;
                GameManager.Instance.OnLevelUp -= ShowLevelUp;
                GameManager.Instance.OnWaveCompleted -= ShowMarket;
            }
        }

        public void ShowHUD()
        {
            HUDPanel?.SetActive(true);
            LevelUpPanel?.SetActive(false);
            MarketPanel?.SetActive(true);
            GameOverPanel?.SetActive(false);
            MarketPanel?.GetComponent<MarketUI>()?.SetDrawerOpen(true, true);
        }

        public void ShowLevelUp()
        {
            LevelUpPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        public void ShowMarket()
        {
            if (MarketPanel == null)
                return;

            MarketPanel.SetActive(true);
            var market = MarketPanel.GetComponent<MarketUI>();
            if (market != null && market.OpenOnWaveCompleted)
                market.SetDrawerOpen(true);
            market?.Refresh();
        }

        public void HideMarket()
        {
            MarketPanel?.GetComponent<MarketUI>()?.SetDrawerOpen(false);
        }

        public void ShowGameOver()
        {
            GameOverPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        public void HideLevelUp()
        {
            LevelUpPanel?.SetActive(false);
            Time.timeScale = 1f;

            var gm = GameManager.Instance;
            if (gm != null && !gm.WaveState.WaveActive && !gm.WaveState.StressTestMode)
                ShowMarket();
        }

        public void OnRestart()
        {
            GameOverPanel?.SetActive(false);
            MarketPanel?.SetActive(true);
            MarketPanel?.GetComponent<MarketUI>()?.SetDrawerOpen(true, true);
            LevelUpPanel?.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.RestartGame();
        }
    }
}
