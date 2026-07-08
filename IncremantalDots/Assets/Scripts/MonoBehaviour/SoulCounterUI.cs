using TMPro;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Kosu ici SOUL sayaci (Polish 2, bilgi cilasi): oyuncu oldurdukce biriken kill'i
    /// (= olumde kazanacagi Soul) HUD'da gorur — roguelite dongusunun motoru kosu SIRASINDA
    /// hissedilir. Kaynak: GameStateData.TotalKills (1 kill = 1 Soul). Polling 0.25s.
    /// </summary>
    public class SoulCounterUI : MonoBehaviour
    {
        public GameObject CounterPanel;
        public TMP_Text CounterText;

        private const float CheckInterval = 0.25f;
        private float _checkTimer;
        private int _lastShown = -1;

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer > 0f)
                return;
            _checkTimer = CheckInterval;

            var gm = GameManager.Instance;
            bool visible = gm != null && gm.ContinuousSiegeCycle.Enabled && !gm.GameState.IsGameOver;
            if (CounterPanel != null && CounterPanel.activeSelf != visible)
                CounterPanel.SetActive(visible);
            if (!visible)
                return;

            int kills = gm.GameState.TotalKills;
            if (kills != _lastShown && CounterText != null)
            {
                _lastShown = kills;
                CounterText.text = $"<color=#B085F5>SOULS</color>  {kills}";
            }
        }
    }
}
