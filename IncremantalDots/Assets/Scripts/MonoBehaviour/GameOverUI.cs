using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class GameOverUI : MonoBehaviour
    {
        public TMP_Text GameOverText;
        public TMP_Text StatsText;
        public Button RestartButton;

        private void OnEnable()
        {
            if (GameManager.Instance == null) return;

            var gs = GameManager.Instance.GameState;
            var ws = GameManager.Instance.WaveState;

            if (GameOverText != null)
                GameOverText.text = "GAME OVER";

            if (StatsText != null)
                StatsText.text = $"Wave: {ws.CurrentWave}\nLevel: {gs.Level}\nGold: {gs.Gold}";

            if (RestartButton != null)
            {
                RestartButton.onClick.RemoveAllListeners();
                RestartButton.onClick.AddListener(() => UIManager.Instance.OnRestart());
            }
        }
    }
}
