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

        private void Update()
        {
            if (GameManager.Instance == null) return;

            var gm = GameManager.Instance;

            // HP Bars
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

            // Text
            if (GoldText != null)
                GoldText.text = $"Gold: {gm.GameState.Gold}";

            if (XPText != null)
                XPText.text = $"XP: {gm.GameState.XP}/{gm.GameState.XPToNextLevel}";

            if (WaveText != null)
                WaveText.text = $"Wave: {gm.WaveState.CurrentWave}";

            if (LevelText != null)
                LevelText.text = $"Level: {gm.GameState.Level}";

            if (ZombiesAliveText != null)
                ZombiesAliveText.text = $"Zombies: {gm.WaveState.ZombiesAlive}";
        }
    }
}
