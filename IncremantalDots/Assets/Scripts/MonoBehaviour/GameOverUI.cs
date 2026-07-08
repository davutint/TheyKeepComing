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
            var cycle = GameManager.Instance.ContinuousSiegeCycle;

            if (GameOverText != null)
                GameOverText.text = "GAME OVER";

            if (StatsText != null)
            {
                // Continuous siege dili: DAY sayaci (legacy Wave/Level yalniz eski modda)
                StatsText.text = cycle.Enabled
                    ? $"You survived {Mathf.Max(1, cycle.CycleIndex + 1)} days"
                    : $"Wave: {ws.CurrentWave}\nLevel: {gs.Level}";
            }

            if (RestartButton != null)
            {
                RestartButton.onClick.RemoveAllListeners();
                RestartButton.onClick.AddListener(() => UIManager.Instance.OnRestart());
            }
        }
    }
}
