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
        public TMP_Text KillsText;
        public TMP_Text LevelText;
        public TMP_Text ZombiesAliveText;
        public TMP_Text ArcherTypeText;
        public TMP_Text DefenseText;
        public TMP_Text DefensePercentText;
        public TMP_Text DefenseWallText;
        public TMP_Text DefenseGateText;
        public TMP_Text DefenseCoreText;
        public TMP_Text WaveRewardText;
        public Image DefenseWallFill;
        public Image DefenseGateFill;
        public Image DefenseCoreFill;
        public Image DefenseDamageGlow;
        public Image DamageFlashImage;

        [Header("Resources")]
        public TMP_Text WoodText;
        public TMP_Text StoneText;
        public TMP_Text IronText;
        public TMP_Text FoodText;

        [Header("Population")]
        public TMP_Text PopulationText;

        [Header("Arrow Supply")]
        public TMP_Text ArrowText;

        [Header("Continuous Siege")]
        public GameObject CyclePanel;
        public TMP_Text CyclePhaseText;
        public TMP_Text CycleDayCounterText;
        public TMP_Text CycleDayLabelText;
        public TMP_Text CycleDuskLabelText;
        public TMP_Text CycleNightLabelText;
        public Image CycleProgressFill;
        public RectTransform CycleProgressMarker;
        public GameObject HordePressurePanel;
        public TMP_Text HordePressureTitleText;
        public Image HordePressureFill;
        public TMP_Text HordePressureText;
        public TMP_Text HordePressureLevelText;

        // Onceki degerler — sadece degisince string alloc yap
        private int _lastXP = -1, _lastXPToNext = -1;
        private int _lastWave = -1, _lastLevel = -1, _lastAlive = -1;
        private int _lastKills = -1, _lastWaveTotal = -1;
        private string _lastWaveLabel;
        private string _lastKillsLabel;
        private int _maxAliveSeen = 0;
        private int _lastWood = -1, _lastStone = -1, _lastIron = -1, _lastFood = -1;
        private float _lastWoodNet, _lastStoneNet, _lastIronNet, _lastFoodNet;
        private int _lastPopTotal = -1, _lastPopCapacity = -1, _lastWorkers = -1, _lastArchers = -1;
        private int _lastArrowCurrent = -1;
        private string _lastArrowLabel;
        private int _lastBasicArchers = -1, _lastRapidArchers = -1, _lastFrostArchers = -1;
        private int _lastDefensePercent = -1;
        private int _lastWallPercent = -1, _lastGatePercent = -1, _lastCorePercent = -1;
        private float _lastDefenseRatio = -1f;
        private int _lastRewardSequence = -1;
        private float _rewardDisplayTimer;
        private float _damageFlashTimer;
        private Color _waveDefaultColor;
        private Color _killsDefaultColor;
        private Color _defenseDefaultColor;
        private bool _colorsCached;
        private const float RewardDisplayDuration = 3f;
        private const float DamageFlashDuration = 0.28f;

        private void OnEnable()
        {
            HideLegacyArcherTypeText();
            CacheDefaultColors();
        }

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

            string waveLabel = FormatWaveState(gm);
            if (WaveText != null && (_lastWave != gm.WaveState.CurrentWave || _lastWaveLabel != waveLabel))
            {
                _lastWave = gm.WaveState.CurrentWave;
                _lastWaveLabel = waveLabel;
                WaveText.text = waveLabel;
            }

            string killsLabel = FormatKillsState(gm);
            if (KillsText != null && (_lastKills != gm.KillsThisWave
                || _lastWaveTotal != gm.WaveState.ZombiesToSpawn
                || _lastKillsLabel != killsLabel))
            {
                _lastKills = gm.KillsThisWave;
                _lastWaveTotal = gm.WaveState.ZombiesToSpawn;
                _lastKillsLabel = killsLabel;
                KillsText.text = killsLabel;
            }

            UpdateContinuousSiegeHud(gm);

            if (LevelText != null && _lastLevel != gm.GameState.Level)
            {
                _lastLevel = gm.GameState.Level;
                LevelText.text = $"Level: {_lastLevel}";
            }

            if (ZombiesAliveText != null && _lastAlive != gm.WaveState.ZombiesAlive)
            {
                _lastAlive = gm.WaveState.ZombiesAlive;
                if (_lastAlive > _maxAliveSeen)
                    _maxAliveSeen = _lastAlive;

                ZombiesAliveText.text = $"Zombies: {_lastAlive} (max {_maxAliveSeen})";
            }

            if (ArcherTypeText != null &&
                (_lastBasicArchers != gm.BasicArcherCount ||
                 _lastRapidArchers != gm.RapidArcherCount ||
                 _lastFrostArchers != gm.FrostArcherCount))
            {
                _lastBasicArchers = gm.BasicArcherCount;
                _lastRapidArchers = gm.RapidArcherCount;
                _lastFrostArchers = gm.FrostArcherCount;
                ArcherTypeText.text = $"Archers: B {_lastBasicArchers} / R {_lastRapidArchers} / F {_lastFrostArchers}";
            }

            UpdateDefenseState(gm);
            UpdateWaveReward(gm);
            UpdateThreatColors(gm);
            UpdateDamageFlash();

            // Kaynaklar
            var res = gm.Resources;
            var prod = gm.GetEffectiveResourceProduction();
            var cons = gm.ResourceConsumption;

            float woodNet = prod.WoodPerMin - cons.WoodPerMin;
            float stoneNet = prod.StonePerMin - cons.StonePerMin;
            float ironNet = prod.IronPerMin - cons.IronPerMin;
            float foodNet = prod.FoodPerMin - cons.FoodPerMin;

            if (WoodText != null && (_lastWood != res.Wood || _lastWoodNet != woodNet))
            {
                _lastWood = res.Wood;
                _lastWoodNet = woodNet;
                WoodText.text = FormatResource("Wood", res.Wood, woodNet);
            }

            if (StoneText != null && (_lastStone != res.Stone || _lastStoneNet != stoneNet))
            {
                _lastStone = res.Stone;
                _lastStoneNet = stoneNet;
                StoneText.text = FormatResource("Stone", res.Stone, stoneNet);
            }

            if (IronText != null && (_lastIron != res.Iron || _lastIronNet != ironNet))
            {
                _lastIron = res.Iron;
                _lastIronNet = ironNet;
                IronText.text = FormatResource("Iron", res.Iron, ironNet);
            }

            if (FoodText != null && (_lastFood != res.Food || _lastFoodNet != foodNet))
            {
                _lastFood = res.Food;
                _lastFoodNet = foodNet;
                FoodText.text = FormatResource("Food", res.Food, foodNet);
            }

            // Ok envanter
            var arrowSupply = gm.ArrowSupply;
            string arrowLabel = FormatArrowSupply(gm);
            if (ArrowText != null && (_lastArrowCurrent != arrowSupply.Current || _lastArrowLabel != arrowLabel))
            {
                _lastArrowCurrent = arrowSupply.Current;
                _lastArrowLabel = arrowLabel;
                ArrowText.text = arrowLabel;
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
                PopulationText.text = FormatPopulation(gm, pop);
            }
        }

        private void HideLegacyArcherTypeText()
        {
            if (ArcherTypeText == null)
                return;

            ArcherTypeText.gameObject.SetActive(false);
            ArcherTypeText = null;
        }

        private void CacheDefaultColors()
        {
            if (_colorsCached)
                return;

            _waveDefaultColor = WaveText != null ? WaveText.color : Color.white;
            _killsDefaultColor = KillsText != null ? KillsText.color : Color.white;
            _defenseDefaultColor = DefensePercentText != null
                ? DefensePercentText.color
                : DefenseText != null ? DefenseText.color : Color.white;
            _colorsCached = true;
        }

        private void UpdateDefenseState(GameManager gm)
        {
            if (DefenseText == null && DefensePercentText == null
                && DefenseWallFill == null && DefenseGateFill == null && DefenseCoreFill == null)
                return;

            float ratio = gm.GetDefensePercent();
            float wallRatio = GetRatio(gm.Wall.CurrentHP, gm.Wall.MaxHP);
            float gateRatio = GetRatio(gm.Gate.CurrentHP, gm.Gate.MaxHP);
            float coreRatio = GetRatio(gm.Castle.CurrentHP, gm.Castle.MaxHP);
            int percent = Mathf.RoundToInt(ratio * 100f);
            int wallPercent = Mathf.RoundToInt(wallRatio * 100f);
            int gatePercent = Mathf.RoundToInt(gateRatio * 100f);
            int corePercent = Mathf.RoundToInt(coreRatio * 100f);
            if (_lastDefenseRatio >= 0f && ratio < _lastDefenseRatio - 0.002f)
                _damageFlashTimer = DamageFlashDuration;

            _lastDefenseRatio = ratio;
            if (_lastDefensePercent != percent)
            {
                _lastDefensePercent = percent;
                if (DefenseText != null)
                    DefenseText.text = $"DEF {percent}%";
                if (DefensePercentText != null)
                    DefensePercentText.text = $"{percent}%";
            }

            if (_lastWallPercent != wallPercent)
            {
                _lastWallPercent = wallPercent;
                if (DefenseWallText != null)
                    DefenseWallText.text = $"WALL {wallPercent}%";
            }

            if (_lastGatePercent != gatePercent)
            {
                _lastGatePercent = gatePercent;
                if (DefenseGateText != null)
                    DefenseGateText.text = $"GATE {gatePercent}%";
            }

            if (_lastCorePercent != corePercent)
            {
                _lastCorePercent = corePercent;
                if (DefenseCoreText != null)
                    DefenseCoreText.text = $"CORE {corePercent}%";
            }

            UpdateDefenseFill(DefenseWallFill, wallRatio);
            UpdateDefenseFill(DefenseGateFill, gateRatio);
            UpdateDefenseFill(DefenseCoreFill, coreRatio);
        }

        private void UpdateContinuousSiegeHud(GameManager gm)
        {
            ContinuousSiegeCycleData cycle = default;
            bool active = CyclePanel != null
                && !gm.WaveState.StressTestMode
                && gm.TryGetContinuousSiegeCycle(out cycle);

            if (CyclePanel != null)
                CyclePanel.SetActive(active);
            if (HordePressurePanel != null)
                HordePressurePanel.SetActive(false);

            if (!active)
            {
                if (CyclePanel == null && WaveText != null)
                    WaveText.gameObject.SetActive(true);
                if (CyclePanel == null && KillsText != null)
                    KillsText.gameObject.SetActive(true);
                return;
            }

            if (WaveText != null)
                WaveText.gameObject.SetActive(false);
            if (KillsText != null)
                KillsText.gameObject.SetActive(false);

            string phase = FormatSiegePhase(cycle.Phase);
            if (CyclePhaseText != null)
                CyclePhaseText.text = phase;
            if (CycleDayCounterText != null)
            {
                // Gun sayaci: sonsuz kusatmada "ne kadar dayandim" hedef hissi (skor)
                string dayLabel = "DAY " + Mathf.Max(1, cycle.CycleIndex + 1);
                if (CycleDayCounterText.text != dayLabel)
                    CycleDayCounterText.text = dayLabel;
            }
            if (CycleDayLabelText != null)
                CycleDayLabelText.text = "DAY";
            if (CycleDuskLabelText != null)
                CycleDuskLabelText.text = "DUSK";
            if (CycleNightLabelText != null)
                CycleNightLabelText.text = "NIGHT";

            UpdateProgressFill(CycleProgressFill, cycle.CycleProgress01);
            UpdateProgressMarker(CycleProgressMarker, cycle.CycleProgress01);
        }

        private void UpdateWaveReward(GameManager gm)
        {
            if (WaveRewardText == null)
                return;

            var reward = gm.WaveClearReward;
            if (reward.Sequence > 0 && reward.Sequence != _lastRewardSequence)
            {
                _lastRewardSequence = reward.Sequence;
                _rewardDisplayTimer = RewardDisplayDuration;
                WaveRewardText.text = $"Wave Cleared +{reward.Wood}W +{reward.Food}F +{reward.Stone}S +{reward.Iron}I";
                WaveRewardText.gameObject.SetActive(true);
            }

            if (_rewardDisplayTimer <= 0f)
            {
                WaveRewardText.gameObject.SetActive(false);
                return;
            }

            _rewardDisplayTimer = Mathf.Max(0f, _rewardDisplayTimer - Time.unscaledDeltaTime);
            WaveRewardText.gameObject.SetActive(_rewardDisplayTimer > 0f);
        }

        private void UpdateThreatColors(GameManager gm)
        {
            CacheDefaultColors();
            bool finalPressure = gm.IsMobileFinalWavePressure();
            Color threatColor = new Color(1f, 0.48f, 0.32f, 1f);

            if (WaveText != null)
                WaveText.color = finalPressure ? threatColor : _waveDefaultColor;
            if (KillsText != null)
                KillsText.color = finalPressure ? threatColor : _killsDefaultColor;
        }

        private void UpdateDamageFlash()
        {
            if (_damageFlashTimer > 0f)
                _damageFlashTimer = Mathf.Max(0f, _damageFlashTimer - Time.unscaledDeltaTime);

            float progress = DamageFlashDuration <= 0f ? 0f : _damageFlashTimer / DamageFlashDuration;
            Color damageColor = new Color(1f, 0.18f, 0.12f, 1f);
            if (DefenseText != null)
                DefenseText.color = progress > 0f ? damageColor : _defenseDefaultColor;
            if (DefensePercentText != null)
                DefensePercentText.color = progress > 0f ? damageColor : _defenseDefaultColor;
            if (DefenseDamageGlow != null)
            {
                Color glow = damageColor;
                glow.a = 0.55f * progress;
                DefenseDamageGlow.raycastTarget = false;
                DefenseDamageGlow.color = glow;
                DefenseDamageGlow.gameObject.SetActive(progress > 0f);
            }

            if (DamageFlashImage == null)
                return;

            Color overlay = damageColor;
            overlay.a = 0.22f * progress;
            DamageFlashImage.raycastTarget = false;
            DamageFlashImage.color = overlay;
        }

        private static float GetRatio(float current, float max)
        {
            if (max <= 0.01f)
                return 1f;

            return Mathf.Clamp01(current / max);
        }

        private static void UpdateDefenseFill(Image fill, float ratio)
        {
            if (fill == null)
                return;

            ratio = Mathf.Clamp01(ratio);
            if (fill.type == Image.Type.Filled)
                fill.fillAmount = ratio;
            else
                fill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);

            fill.color = GetDefenseFillColor(ratio);
        }

        private static void UpdateProgressFill(Image fill, float ratio)
        {
            if (fill == null)
                return;

            ratio = Mathf.Clamp01(ratio);
            fill.type = Image.Type.Simple;

            RectTransform rect = fill.rectTransform;
            RectTransform parent = rect.parent as RectTransform;
            float fullWidth = parent != null && parent.rect.width > 0.1f
                ? parent.rect.width
                : Mathf.Max(1f, rect.sizeDelta.x);

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(fullWidth * ratio, rect.sizeDelta.y);
        }

        private static void UpdateProgressMarker(RectTransform marker, float ratio)
        {
            if (marker == null)
                return;

            ratio = Mathf.Clamp01(ratio);
            RectTransform parent = marker.parent as RectTransform;
            float fullWidth = parent != null && parent.rect.width > 0.1f
                ? parent.rect.width
                : 300f;

            marker.anchorMin = new Vector2(0f, 0.5f);
            marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(fullWidth * ratio, marker.anchoredPosition.y);
        }

        private static Color GetDefenseFillColor(float ratio)
        {
            if (ratio > 0.55f)
                return new Color(0.56f, 0.76f, 0.38f, 1f);
            if (ratio > 0.25f)
                return new Color(0.95f, 0.62f, 0.18f, 1f);

            return new Color(0.88f, 0.20f, 0.14f, 1f);
        }

        private static string FormatPopulation(GameManager gm, PopulationState pop)
        {
            if (gm != null && gm.IsMobilePopulationEconomyEnabled())
                return pop.Total.ToString();

            return $"{pop.Total}/{pop.Capacity}";
        }

        private static string FormatWaveState(GameManager gm)
        {
            WaveStateData wave = gm.WaveState;
            if (wave.StressTestMode)
                return $"WAVE {wave.CurrentWave:00} STRESS";

            if (gm.TryGetContinuousSiegeCycle(out var cycle))
                return FormatSiegePhase(cycle.Phase);

            if (gm.IsMobileMode && wave.Phase == RunPhaseType.DayPrep && !wave.WaveActive)
                return $"DAY {wave.CurrentWave + 1:00} - {Mathf.CeilToInt(wave.PrepTimer)}s";

            if (gm.IsMobileMode && wave.Phase == RunPhaseType.NightCombat)
                return $"NIGHT {wave.CurrentWave:00}";

            if (!wave.WaveActive)
                return $"WAVE {wave.CurrentWave:00} CLEARED";

            if (wave.WaveStartTimer > 0f)
                return $"WAVE {wave.CurrentWave:00} STARTING";

            return $"WAVE {wave.CurrentWave:00}";
        }

        private static string FormatKillsState(GameManager gm)
        {
            WaveStateData wave = gm.WaveState;
            if (!wave.StressTestMode && gm.TryGetContinuousSiegeCycle(out _))
                return $"KILLS {gm.KillsThisWave}";

            if (gm.IsMobileMode && wave.Phase == RunPhaseType.DayPrep && !wave.WaveActive && !wave.StressTestMode)
                return "PREPARE DEFENSE";

            return $"KILLS {gm.KillsThisWave} / {wave.ZombiesToSpawn}";
        }

        private static string FormatSiegePhase(SiegeCyclePhase phase)
        {
            switch (phase)
            {
                case SiegeCyclePhase.Dusk:
                    return "DUSK";
                case SiegeCyclePhase.Night:
                    return "NIGHT";
                case SiegeCyclePhase.Dawn:
                    return "DAWN";
                default:
                    return "DAY";
            }
        }

        private static string FormatArrowSupply(GameManager gm)
        {
            return gm.IsUnlimitedArrowsEnabled() ? "INF" : gm.ArrowSupply.Current.ToString();
        }

        private static string FormatResource(string name, int amount, float netRate)
        {
            // Sifir hiz → parantez yok
            if (netRate > 0.01f)
                return $"{amount}\n+{netRate:0}/min";
            if (netRate < -0.01f)
                return $"{amount}\n{netRate:0}/min";
            return amount.ToString();
        }

    }
}
