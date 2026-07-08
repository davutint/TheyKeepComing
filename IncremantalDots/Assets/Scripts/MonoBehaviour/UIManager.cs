using System.Collections;
using UnityEngine;

namespace DeadWalls
{
    public class UIManager : MonoBehaviour
    {
        private Coroutine _gameOverRoutine;

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
            // Olum ani agir cekimi (Polish 2): son an kisa sure izlenir, sonra ekran acilir
            if (_gameOverRoutine != null)
                StopCoroutine(_gameOverRoutine);
            _gameOverRoutine = StartCoroutine(GameOverSequence());
        }

        private IEnumerator GameOverSequence()
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(0.9f);
            GameOverPanel?.SetActive(true);
            Time.timeScale = 0f;
            _gameOverRoutine = null;
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
            RunPersistence.Delete(); // yeni kosu = eski checkpoint gecersiz
            GameManager.Instance?.RestartGame();
        }
    }
}
