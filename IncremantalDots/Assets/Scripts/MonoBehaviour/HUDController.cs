using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class HUDController : MonoBehaviour
    {
        [Header("HP Bars")]
        public Slider WallHPBar;
        public Slider GateHPBar;
        public Slider CastleHPBar;

        [Header("Text")]
        public TMP_Text GoldText;
        public TMP_Text XPText;
        public TMP_Text WaveText;
        public TMP_Text LevelText;
        public TMP_Text ZombiesAliveText;

        // Onceki degerler — sadece degisince string alloc yap
        private int _lastGold = -1, _lastXP = -1, _lastXPToNext = -1;
        private int _lastWave = -1, _lastLevel = -1, _lastAlive = -1;

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // HP Bars (float, her zaman guncelle — ucuz)
            if (WallHPBar != null)
            {
                WallHPBar.maxValue = gm.Wall.MaxHP;
                WallHPBar.value = gm.Wall.CurrentHP;
            }

            if (GateHPBar != null)
            {
                GateHPBar.maxValue = gm.Gate.MaxHP;
                GateHPBar.value = gm.Gate.CurrentHP;
            }

            if (CastleHPBar != null)
            {
                CastleHPBar.maxValue = gm.Castle.MaxHP;
                CastleHPBar.value = gm.Castle.CurrentHP;
            }

            // Text (string alloc — sadece degisince)
            if (GoldText != null && _lastGold != gm.GameState.Gold)
            {
                _lastGold = gm.GameState.Gold;
                GoldText.text = $"Gold: {_lastGold}";
            }

            if (XPText != null && (_lastXP != gm.GameState.XP || _lastXPToNext != gm.GameState.XPToNextLevel))
            {
                _lastXP = gm.GameState.XP;
                _lastXPToNext = gm.GameState.XPToNextLevel;
                XPText.text = $"XP: {_lastXP}/{_lastXPToNext}";
            }

            if (WaveText != null && _lastWave != gm.WaveState.CurrentWave)
            {
                _lastWave = gm.WaveState.CurrentWave;
                WaveText.text = $"Wave: {_lastWave}";
            }

            if (LevelText != null && _lastLevel != gm.GameState.Level)
            {
                _lastLevel = gm.GameState.Level;
                LevelText.text = $"Level: {_lastLevel}";
            }

            if (ZombiesAliveText != null && _lastAlive != gm.WaveState.ZombiesAlive)
            {
                _lastAlive = gm.WaveState.ZombiesAlive;
                ZombiesAliveText.text = $"Zombies: {_lastAlive}";
            }
        }
    }
}
