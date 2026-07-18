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

            var gm = GameManager.Instance;
            var gs = gm.GameState;
            var ws = gm.WaveState;
            var cycle = gm.ContinuousSiegeCycle;
            MetaPresentationSettings presentation = gm.MetaCatalog != null
                ? gm.MetaCatalog.Presentation
                : null;

            if (GameOverText != null)
                GameOverText.text = presentation != null
                    ? presentation.DeathTitle
                    : "THE WALL HAS FALLEN";

            if (StatsText != null)
            {
                StatsText.text = cycle.Enabled
                    ? presentation != null
                        ? presentation.DeathSubtitle
                        : "THE RUN ENDS HERE. WHAT REMAINS WILL STRENGTHEN THE NEXT STAND."
                    : $"Wave: {ws.CurrentWave}  •  Level: {gs.Level}";
            }

            if (RestartButton != null)
            {
                RestartButton.onClick.RemoveAllListeners();
                RestartButton.onClick.AddListener(() => UIManager.Instance.OnRestart());
                TMP_Text label = RestartButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = presentation != null
                        ? presentation.RestartLabel
                        : "BEGIN NEXT RUN";
            }
        }
    }
}
