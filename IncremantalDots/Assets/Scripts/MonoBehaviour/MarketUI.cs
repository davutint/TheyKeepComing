using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class MarketUI : MonoBehaviour
    {
        [Header("Drawer")]
        public RectTransform ArcherDrawerPanel;
        public Button DrawerToggleButton;
        public TMP_Text DrawerTitleText;
        public bool OpenOnHudShown = true;
        public bool OpenOnWaveCompleted = true;
        public float SlideDuration = 0.18f;

        [Header("Prep")]
        public Button RepairButton;
        public Button RefillArrowsButton;
        public Button StartNextWaveButton;
        public Button FortifyButton;
        public Button RallyButton;
        public TMP_Text RepairCostText;
        public TMP_Text FortifyCostText;
        public TMP_Text RallyCostText;
        public TMP_Text RepairStatusText;
        public TMP_Text FortifyStatusText;
        public TMP_Text RallyStatusText;

        [Header("Basic Archer Row")]
        public TMP_Text BasicCountText;
        public TMP_Text BasicDpsText;
        public TMP_Text BasicLevelText;
        public TMP_Text BasicCostText;
        public Button BasicBuyButton;
        public Button BasicUpgradeButton;

        [Header("Rapid Archer Row")]
        public TMP_Text RapidCountText;
        public TMP_Text RapidDpsText;
        public TMP_Text RapidLevelText;
        public TMP_Text RapidCostText;
        public Button RapidBuyButton;
        public Button RapidUpgradeButton;

        [Header("Frost Archer Row")]
        public TMP_Text FrostCountText;
        public TMP_Text FrostDpsText;
        public TMP_Text FrostLevelText;
        public TMP_Text FrostCostText;
        public Button FrostBuyButton;
        public Button FrostUpgradeButton;

        [Header("Arrow Tech")]
        public GameObject ArrowTechPanel;
        public Button RapidTechUnlockButton;
        public Button FrostTechUnlockButton;

        private bool _drawerOpen;
        private bool _drawerPositionsReady;
        private Vector2 _drawerOpenPosition;
        private Vector2 _drawerClosedPosition;
        private float _nextRefreshTime;

        private void OnEnable()
        {
            BindButtons();
            EnsureDrawerPositions();
            SetDrawerOpen(OpenOnHudShown, true);
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void Update()
        {
            AnimateDrawer();

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 0.20f;
                Refresh();
            }
        }

        public void SetDrawerOpen(bool open, bool instant = false)
        {
            _drawerOpen = open;
            EnsureDrawerPositions();

            if (instant && ArcherDrawerPanel != null)
                ArcherDrawerPanel.anchoredPosition = _drawerOpen ? _drawerOpenPosition : _drawerClosedPosition;

            Refresh();
        }

        public void ToggleDrawer()
        {
            SetDrawerOpen(!_drawerOpen);
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                SetAllButtonsInteractable(false);
                return;
            }

            SetText(DrawerTitleText, "ARCHER RECRUITMENT");
            HideTechControls();

            RefreshArcherRow(
                ArcherType.Basic,
                BasicCountText,
                BasicDpsText,
                BasicLevelText,
                BasicCostText,
                BasicBuyButton,
                BasicUpgradeButton);

            RefreshArcherRow(
                ArcherType.Rapid,
                RapidCountText,
                RapidDpsText,
                RapidLevelText,
                RapidCostText,
                RapidBuyButton,
                RapidUpgradeButton);

            RefreshArcherRow(
                ArcherType.Frost,
                FrostCountText,
                FrostDpsText,
                FrostLevelText,
                FrostCostText,
                FrostBuyButton,
                FrostUpgradeButton);

            bool mobileMode = gm.IsMobileMode;
            bool prepMovedToCastleInterior = gm.IsMobilePopulationEconomyEnabled();
            SetPrepActionVisible(RepairButton, RepairCostText, RepairStatusText, !prepMovedToCastleInterior);
            SetPrepActionVisible(FortifyButton, FortifyCostText, FortifyStatusText, !prepMovedToCastleInterior);
            SetPrepActionVisible(RallyButton, RallyCostText, RallyStatusText, !prepMovedToCastleInterior);
            if (!prepMovedToCastleInterior)
            {
                if (RepairButton != null) RepairButton.interactable = gm.CanRepairDefenseFull();
                RefreshPrepAction(RepairButton, RepairCostText, RepairStatusText, "Repair",
                    gm.GetRepairCost(), gm.CanRepairDefenseFull(), GetRepairStatus(gm));
                RefreshPrepAction(FortifyButton, FortifyCostText, FortifyStatusText, "Fortify",
                    gm.GetFortifyCost(), gm.CanBuyFortify(), GetFortifyStatus(gm));
                RefreshPrepAction(RallyButton, RallyCostText, RallyStatusText, "Rally",
                    gm.GetRallyCost(), gm.CanBuyRally(), GetRallyStatus(gm));
            }
            if (RefillArrowsButton != null)
            {
                bool showRefill = !mobileMode || !gm.IsUnlimitedArrowsEnabled();
                RefillArrowsButton.gameObject.SetActive(showRefill);
                RefillArrowsButton.interactable = showRefill && !gm.GameState.IsGameOver && !gm.GameState.IsLevelUpPending;
            }

            if (StartNextWaveButton != null)
            {
                bool showStartNextWave = !mobileMode;
                StartNextWaveButton.gameObject.SetActive(showStartNextWave);
                StartNextWaveButton.interactable = showStartNextWave
                    && !gm.GameState.IsGameOver
                    && !gm.GameState.IsLevelUpPending
                    && !gm.WaveState.WaveActive
                    && !gm.WaveState.StressTestMode;
            }
        }

        private void RefreshArcherRow(
            ArcherType type,
            TMP_Text countText,
            TMP_Text dpsText,
            TMP_Text levelText,
            TMP_Text costText,
            Button buyButton,
            Button upgradeButton)
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            bool unlocked = gm.IsArcherTypeUnlocked(type);
            if (countText != null)
                countText.text = $"x{gm.GetArcherTypeCount(type)}";
            if (dpsText != null)
                dpsText.text = unlocked ? $"DPS {gm.GetArcherTypeDps(type):0.#}" : "LOCKED";
            if (levelText != null)
                levelText.text = unlocked ? $"LV {gm.GetArcherTypeLevel(type)}" : "TECH";

            if (costText != null)
            {
                var resources = gm.Resources;
                var buyCost = gm.GetArcherBuyCost(type);
                bool freeMode = gm.IsFreeEconomyTestMode;
                string buyCostLabel = FormatCostWithNeed(buyCost, resources, freeMode);
                if (!freeMode
                    && unlocked
                    && gm.IsMobilePopulationEconomyEnabled()
                    && buyCost.CanAfford(resources)
                    && gm.GetIdlePopulation() <= 0)
                {
                    buyCostLabel = $"{buyCostLabel} NEED POP";
                }

                costText.text = unlocked
                    ? $"BUY {buyCostLabel}"
                    : "LOCKED BY TECH";
            }

            if (buyButton != null)
            {
                buyButton.interactable = gm.CanBuyArcher(type);
                SetButtonText(buyButton, unlocked ? "Buy" : "Locked");
            }

            HideButton(upgradeButton);
        }

        private void HideTechControls()
        {
            if (ArrowTechPanel != null)
                ArrowTechPanel.SetActive(false);

            HideButton(RapidTechUnlockButton);
            HideButton(FrostTechUnlockButton);
        }

        private void RefreshPrepAction(Button button, TMP_Text costText, TMP_Text statusText, string label,
            ResourceCost cost, bool canUse, string status)
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            string costLabel = FormatCostWithNeed(cost, gm.Resources, gm.IsFreeEconomyTestMode);
            if (button != null)
            {
                button.interactable = canUse;
                if (costText == null && statusText == null)
                    SetButtonText(button, $"{label}\n{costLabel}");
                else
                    SetButtonText(button, label);
            }

            if (costText != null)
                costText.text = costLabel;
            if (statusText != null)
                statusText.text = status;
        }

        private static void SetPrepActionVisible(Button button, TMP_Text costText, TMP_Text statusText, bool visible)
        {
            if (button != null)
            {
                if (!visible)
                    button.interactable = false;
                button.gameObject.SetActive(visible);
            }

            if (costText != null)
                costText.gameObject.SetActive(visible);
            if (statusText != null)
                statusText.gameObject.SetActive(visible);
        }

        private void BindButtons()
        {
            UnbindButtons();
            DrawerToggleButton?.onClick.AddListener(ToggleDrawer);
            RepairButton?.onClick.AddListener(HandleRepairClicked);
            RefillArrowsButton?.onClick.AddListener(HandleRefillArrowsClicked);
            StartNextWaveButton?.onClick.AddListener(HandleStartNextWaveClicked);
            FortifyButton?.onClick.AddListener(HandleFortifyClicked);
            RallyButton?.onClick.AddListener(HandleRallyClicked);
            BasicBuyButton?.onClick.AddListener(HandleBasicBuyClicked);
            RapidBuyButton?.onClick.AddListener(HandleRapidBuyClicked);
            FrostBuyButton?.onClick.AddListener(HandleFrostBuyClicked);
        }

        private void UnbindButtons()
        {
            DrawerToggleButton?.onClick.RemoveListener(ToggleDrawer);
            RepairButton?.onClick.RemoveListener(HandleRepairClicked);
            RefillArrowsButton?.onClick.RemoveListener(HandleRefillArrowsClicked);
            StartNextWaveButton?.onClick.RemoveListener(HandleStartNextWaveClicked);
            FortifyButton?.onClick.RemoveListener(HandleFortifyClicked);
            RallyButton?.onClick.RemoveListener(HandleRallyClicked);
            BasicBuyButton?.onClick.RemoveListener(HandleBasicBuyClicked);
            RapidBuyButton?.onClick.RemoveListener(HandleRapidBuyClicked);
            FrostBuyButton?.onClick.RemoveListener(HandleFrostBuyClicked);
        }

        private void HandleRepairClicked()
        {
            GameManager.Instance?.RepairDefenseFull();
            Refresh();
        }

        private void HandleRefillArrowsClicked()
        {
            GameManager.Instance?.RefillArrows();
            Refresh();
        }

        private void HandleStartNextWaveClicked()
        {
            GameManager.Instance?.StartNextWave();
            Refresh();
        }

        private void HandleFortifyClicked()
        {
            GameManager.Instance?.BuyFortify();
            Refresh();
        }

        private void HandleRallyClicked()
        {
            GameManager.Instance?.BuyRally();
            Refresh();
        }

        private void HandleBasicBuyClicked() => BuyArcher(ArcherType.Basic);
        private void HandleRapidBuyClicked() => BuyArcher(ArcherType.Rapid);
        private void HandleFrostBuyClicked() => BuyArcher(ArcherType.Frost);

        private void BuyArcher(ArcherType type)
        {
            GameManager.Instance?.BuyArcher(type);
            Refresh();
        }

        private void EnsureDrawerPositions()
        {
            if (_drawerPositionsReady)
                return;

            if (ArcherDrawerPanel == null)
                ArcherDrawerPanel = GetComponent<RectTransform>();

            if (ArcherDrawerPanel == null)
                return;

            _drawerOpenPosition = ArcherDrawerPanel.anchoredPosition;
            float width = ArcherDrawerPanel.rect.width > 1f ? ArcherDrawerPanel.rect.width : 460f;
            _drawerClosedPosition = _drawerOpenPosition + new Vector2(width + 40f, 0f);
            _drawerPositionsReady = true;
        }

        private void AnimateDrawer()
        {
            if (ArcherDrawerPanel == null)
                return;

            EnsureDrawerPositions();
            Vector2 target = _drawerOpen ? _drawerOpenPosition : _drawerClosedPosition;
            float t = SlideDuration <= 0f ? 1f : Time.unscaledDeltaTime / SlideDuration;
            ArcherDrawerPanel.anchoredPosition = Vector2.Lerp(ArcherDrawerPanel.anchoredPosition, target, Mathf.Clamp01(t));
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            if (RepairButton != null) RepairButton.interactable = interactable;
            if (RefillArrowsButton != null) RefillArrowsButton.interactable = interactable;
            if (StartNextWaveButton != null) StartNextWaveButton.interactable = interactable;
            if (FortifyButton != null) FortifyButton.interactable = interactable;
            if (RallyButton != null) RallyButton.interactable = interactable;
            if (BasicBuyButton != null) BasicBuyButton.interactable = interactable;
            if (RapidBuyButton != null) RapidBuyButton.interactable = interactable;
            if (FrostBuyButton != null) FrostBuyButton.interactable = interactable;
            HideButton(BasicUpgradeButton);
            HideButton(RapidUpgradeButton);
            HideButton(FrostUpgradeButton);
            HideTechControls();
        }

        private static void SetButtonText(Button button, string value)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = value;
        }

        private static void HideButton(Button button)
        {
            if (button == null)
                return;

            button.interactable = false;
            button.gameObject.SetActive(false);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static string FormatCostWithNeed(ResourceCost cost, ResourceData resources, bool freeMode = false)
        {
            if (freeMode)
                return "FREE";

            string need = cost.ToNeedDisplayString(resources);
            return string.IsNullOrEmpty(need)
                ? cost.ToDisplayString()
                : $"{cost.ToDisplayString()} {need}";
        }

        private static string GetRepairStatus(GameManager gm)
        {
            if (!CanUsePrepAction(gm))
                return "Day prep only";

            return gm.GetDefensePercent() >= 0.995f ? "Defense full" : "Restore defense";
        }

        private static string GetFortifyStatus(GameManager gm)
        {
            if (gm.CastleYardPrep.FortifyActive)
                return "Ready for next night";

            if (!CanUsePrepAction(gm))
                return "Day prep only";

            if (gm.IsFreeEconomyTestMode)
                return "Reduce next night damage";

            string need = gm.GetFortifyCost().ToNeedDisplayString(gm.Resources);
            return string.IsNullOrEmpty(need) ? "Reduce next night damage" : need;
        }

        private static string GetRallyStatus(GameManager gm)
        {
            if (gm.CastleYardPrep.RallyTimer > 0f)
                return $"Opening volley {Mathf.CeilToInt(gm.CastleYardPrep.RallyTimer)}s";

            if (!CanUsePrepAction(gm))
                return "Day prep only";

            if (gm.IsFreeEconomyTestMode)
                return "Faster opening volley";

            string need = gm.GetRallyCost().ToNeedDisplayString(gm.Resources);
            return string.IsNullOrEmpty(need) ? "Faster opening volley" : need;
        }

        private static bool CanUsePrepAction(GameManager gm)
        {
            var wave = gm.WaveState;
            return !gm.GameState.IsGameOver
                && !gm.GameState.IsLevelUpPending
                && !wave.StressTestMode
                && !wave.WaveActive
                && (!gm.IsMobileMode || wave.Phase == RunPhaseType.DayPrep);
        }
    }
}
