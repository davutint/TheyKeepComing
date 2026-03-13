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
        public TMP_Text XPText;
        public TMP_Text WaveText;
        public TMP_Text LevelText;
        public TMP_Text ZombiesAliveText;

        [Header("Resources")]
        public TMP_Text WoodText;
        public TMP_Text StoneText;
        public TMP_Text IronText;
        public TMP_Text FoodText;

        [Header("Population")]
        public TMP_Text PopulationText;

        [Header("Arrow Supply")]
        public TMP_Text ArrowText;

        // Onceki degerler — sadece degisince string alloc yap
        private int _lastXP = -1, _lastXPToNext = -1;
        private int _lastWave = -1, _lastLevel = -1, _lastAlive = -1;
        private int _lastWood = -1, _lastStone = -1, _lastIron = -1, _lastFood = -1;
        private float _lastWoodNet, _lastStoneNet, _lastIronNet, _lastFoodNet;
        private int _lastPopTotal = -1, _lastPopCapacity = -1, _lastWorkers = -1, _lastArchers = -1;
        private int _lastArrowCurrent = -1;

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

            // Kaynaklar
            var res = gm.Resources;
            var prod = gm.ResourceProduction;
            var cons = gm.ResourceConsumption;

            float woodNet = prod.WoodPerMin - cons.WoodPerMin;
            float stoneNet = prod.StonePerMin - cons.StonePerMin;
            float ironNet = prod.IronPerMin - cons.IronPerMin;
            float foodNet = prod.FoodPerMin - cons.FoodPerMin;

            if (WoodText != null && (_lastWood != res.Wood || _lastWoodNet != woodNet))
            {
                _lastWood = res.Wood;
                _lastWoodNet = woodNet;
                WoodText.text = FormatResource("Ahsap", res.Wood, woodNet);
            }

            if (StoneText != null && (_lastStone != res.Stone || _lastStoneNet != stoneNet))
            {
                _lastStone = res.Stone;
                _lastStoneNet = stoneNet;
                StoneText.text = FormatResource("Tas", res.Stone, stoneNet);
            }

            if (IronText != null && (_lastIron != res.Iron || _lastIronNet != ironNet))
            {
                _lastIron = res.Iron;
                _lastIronNet = ironNet;
                IronText.text = FormatResource("Demir", res.Iron, ironNet);
            }

            if (FoodText != null && (_lastFood != res.Food || _lastFoodNet != foodNet))
            {
                _lastFood = res.Food;
                _lastFoodNet = foodNet;
                FoodText.text = FormatResource("Yemek", res.Food, foodNet);
            }

            // Ok envanter
            var arrowSupply = gm.ArrowSupply;
            if (ArrowText != null && _lastArrowCurrent != arrowSupply.Current)
            {
                _lastArrowCurrent = arrowSupply.Current;
                ArrowText.text = $"Ok: {arrowSupply.Current}";
            }

            // Nufus
            var pop = gm.Population;
            if (PopulationText != null && (_lastPopTotal != pop.Total || _lastPopCapacity != pop.Capacity
                || _lastWorkers != pop.Workers || _lastArchers != pop.Archers))
            {
                _lastPopTotal = pop.Total;
                _lastPopCapacity = pop.Capacity;
                _lastWorkers = pop.Workers;
                _lastArchers = pop.Archers;
                PopulationText.text = FormatPopulation(pop);
            }
        }

        private static string FormatPopulation(PopulationState pop)
        {
            int idle = pop.Total - pop.Workers - pop.Archers;
            if (idle < 0) idle = 0;
            return $"Nufus: {pop.Total}/{pop.Capacity} ({pop.Workers} isci, {pop.Archers} okcu, {idle} bos)";
        }

        private static string FormatResource(string name, int amount, float netRate)
        {
            // Sifir hiz → parantez yok
            if (netRate > 0.01f)
                return $"{name}: {amount} (+{netRate:F1}/dk)";
            if (netRate < -0.01f)
                return $"{name}: {amount} ({netRate:F1}/dk)";
            return $"{name}: {amount}";
        }
    }
}
