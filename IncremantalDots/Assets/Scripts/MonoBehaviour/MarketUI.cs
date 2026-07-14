using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Dynamic Archer Recruitment")]
        public ArcherRecruitmentCatalogSO ArcherCatalog;
        public RectTransform ArcherRecruitmentListRoot;
        public RectTransform ArcherRecruitmentRowTemplate;
        public float DynamicRowSpacing = 92f;

        private bool _drawerOpen;
        private bool _drawerPositionsReady;
        private Vector2 _drawerOpenPosition;
        private Vector2 _drawerClosedPosition;
        private float _nextRefreshTime;
        private ArcherRecruitmentCatalogSO _builtCatalog;
        private readonly List<DynamicArcherRow> _dynamicRows = new List<DynamicArcherRow>();
        private bool _hasRefreshFingerprint;
        private int _lastRefreshFingerprint;
        private bool _legacyRowsCached;
        private GameObject _basicLegacyRow;
        private GameObject _rapidLegacyRow;
        private GameObject _frostLegacyRow;

        private void OnEnable()
        {
            EnsureDynamicRows(GameManager.Instance);
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
                RefreshIfChanged();
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
                _hasRefreshFingerprint = false;
                SetAllButtonsInteractable(false);
                return;
            }

            SetText(DrawerTitleText, "ARCHER RECRUITMENT");
            HideTechControls();

            if (!RefreshDynamicArcherRows(gm))
            {
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
            }

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

            _lastRefreshFingerprint = ComputeRefreshFingerprint(gm);
            _hasRefreshFingerprint = true;
        }

        private void RefreshIfChanged()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                if (_hasRefreshFingerprint)
                    Refresh();
                return;
            }

            int fingerprint = ComputeRefreshFingerprint(gm);
            if (_hasRefreshFingerprint && fingerprint == _lastRefreshFingerprint)
                return;

            Refresh();
        }

        private int ComputeRefreshFingerprint(GameManager gm)
        {
            unchecked
            {
                int hash = 17;
                var resources = gm.Resources;
                AddFingerprintValue(ref hash, resources.Wood);
                AddFingerprintValue(ref hash, resources.Stone);
                AddFingerprintValue(ref hash, resources.Iron);
                AddFingerprintValue(ref hash, resources.Food);

                var population = gm.Population;
                AddFingerprintValue(ref hash, population.Total);
                AddFingerprintValue(ref hash, population.Archers);
                AddFingerprintValue(ref hash, gm.GetIdlePopulation());

                AddArcherFingerprint(ref hash, gm, ArcherType.Basic);
                AddArcherFingerprint(ref hash, gm, ArcherType.Rapid);
                AddArcherFingerprint(ref hash, gm, ArcherType.Frost);

                AddFingerprintValue(ref hash, gm.IsMobileMode);
                AddFingerprintValue(ref hash, gm.IsMobilePopulationEconomyEnabled());
                AddFingerprintValue(ref hash, gm.IsUnlimitedArrowsEnabled());
                AddFingerprintValue(ref hash, gm.IsFreeEconomyTestMode);
                AddFingerprintValue(ref hash, gm.GameState.IsGameOver);
                AddFingerprintValue(ref hash, gm.GameState.IsLevelUpPending);

                var wave = gm.WaveState;
                AddFingerprintValue(ref hash, wave.WaveActive);
                AddFingerprintValue(ref hash, wave.StressTestMode);
                AddFingerprintValue(ref hash, (int)wave.Phase);

                var prep = gm.CastleYardPrep;
                AddFingerprintValue(ref hash, prep.FortifyActive);
                AddFingerprintValue(ref hash, Mathf.CeilToInt(prep.RallyTimer));
                AddFingerprintValue(ref hash, gm.CanRepairDefenseFull());
                AddFingerprintValue(ref hash, gm.CanBuyFortify());
                AddFingerprintValue(ref hash, gm.CanBuyRally());

                var repairCost = gm.GetRepairCost();
                AddFingerprintValue(ref hash, repairCost.Wood);
                AddFingerprintValue(ref hash, repairCost.Stone);
                AddFingerprintValue(ref hash, repairCost.Iron);
                AddFingerprintValue(ref hash, repairCost.Food);
                return hash;
            }
        }

        private static void AddArcherFingerprint(ref int hash, GameManager gm, ArcherType type)
        {
            AddFingerprintValue(ref hash, gm.GetArcherTypeCount(type));
            AddFingerprintValue(ref hash, gm.GetArcherTypeLevel(type));
            AddFingerprintValue(ref hash, gm.IsArcherTypeUnlocked(type));
            AddFingerprintValue(ref hash, gm.CanBuyArcher(type));
        }

        private static void AddFingerprintValue(ref int hash, int value)
        {
            unchecked
            {
                hash = hash * 31 + value;
            }
        }

        private static void AddFingerprintValue(ref int hash, bool value)
        {
            AddFingerprintValue(ref hash, value ? 1 : 0);
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
            bool capReached = gm.GetRemainingArcherCapacity() <= 0;
            if (countText != null)
                countText.text = $"x{gm.GetArcherTypeCount(type)}";
            if (dpsText != null)
                dpsText.text = unlocked ? $"DPS {gm.GetArcherTypeDps(type):0.#}" : "LOCKED";
            if (levelText != null)
                levelText.text = unlocked ? $"LV {gm.GetArcherTypeLevel(type)}" : "TECH";

            if (costText != null)
            {
                if (unlocked && capReached)
                {
                    costText.text = $"ARMY CAP {gm.GetTotalArcherCount()}/{ArcherCapacityUtility.MaxTotalArchers}";
                }
                else
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
            }

            if (buyButton != null)
            {
                buyButton.interactable = gm.CanBuyArcher(type);
                SetButtonText(buyButton, !unlocked ? "Locked" : capReached ? "Max" : "Buy");
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

            if (_dynamicRows.Count > 0)
            {
                BindDynamicRowButtons();
            }
            else
            {
                BasicBuyButton?.onClick.AddListener(HandleBasicBuyClicked);
                RapidBuyButton?.onClick.AddListener(HandleRapidBuyClicked);
                FrostBuyButton?.onClick.AddListener(HandleFrostBuyClicked);
            }
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

            for (int i = 0; i < _dynamicRows.Count; i++)
            {
                var row = _dynamicRows[i];
                if (row.BuyButton != null && row.BuyAction != null)
                    row.BuyButton.onClick.RemoveListener(row.BuyAction);
                row.BuyAction = null;
            }
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

        private void BuyArcher(ArcherDefinitionSO definition)
        {
            GameManager.Instance?.BuyArcher(definition);
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
            for (int i = 0; i < _dynamicRows.Count; i++)
            {
                if (_dynamicRows[i].BuyButton != null)
                    _dynamicRows[i].BuyButton.interactable = interactable;
            }
            HideButton(BasicUpgradeButton);
            HideButton(RapidUpgradeButton);
            HideButton(FrostUpgradeButton);
            HideTechControls();
        }

        private bool RefreshDynamicArcherRows(GameManager gm)
        {
            if (!EnsureDynamicRows(gm))
                return false;

            SetLegacyRowsActive(false);
            for (int i = 0; i < _dynamicRows.Count; i++)
                RefreshDynamicArcherRow(_dynamicRows[i], gm);

            return true;
        }

        private bool EnsureDynamicRows(GameManager gm)
        {
            ArcherRecruitmentListRoot ??= FindChildComponent<RectTransform>(gameObject, "ArcherRecruitmentListRoot");
            ArcherRecruitmentRowTemplate ??= FindChildComponent<RectTransform>(gameObject, "ArcherRecruitmentRowTemplate");

            if (ArcherRecruitmentListRoot == null || ArcherRecruitmentRowTemplate == null)
                return false;

            if (_dynamicRows.Count > 0 && _builtCatalog == ArcherCatalog)
                return true;

            var definitions = GetDefinitions(gm);
            if (definitions == null || definitions.Length == 0)
                return false;

            ClearDynamicRows();
            ArcherRecruitmentRowTemplate.gameObject.SetActive(false);

            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                    continue;

                var rowObject = Instantiate(ArcherRecruitmentRowTemplate.gameObject, ArcherRecruitmentListRoot);
                rowObject.name = $"ArcherRecruitmentRow_{definition.Id}";
                rowObject.SetActive(true);

                var rect = rowObject.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchoredPosition = new Vector2(0f, -_dynamicRows.Count * DynamicRowSpacing);

                _dynamicRows.Add(new DynamicArcherRow
                {
                    Definition = definition,
                    Root = rowObject,
                    NameText = FindChildComponent<TMP_Text>(rowObject, "ArcherNameText"),
                    CountText = FindChildComponent<TMP_Text>(rowObject, "ArcherCountText"),
                    DpsText = FindChildComponent<TMP_Text>(rowObject, "ArcherDpsText"),
                    LevelText = FindChildComponent<TMP_Text>(rowObject, "ArcherLevelText"),
                    CostText = FindChildComponent<TMP_Text>(rowObject, "ArcherCostText"),
                    StatusText = FindChildComponent<TMP_Text>(rowObject, "ArcherStatusText"),
                    BuyButton = FindChildComponent<Button>(rowObject, "ArcherBuyButton"),
                    BuyButtonText = FindChildComponent<TMP_Text>(rowObject, "ArcherBuyButtonText")
                });
            }

            _builtCatalog = ArcherCatalog;
            SetLegacyRowsActive(false);
            BindDynamicRowButtons();
            return _dynamicRows.Count > 0;
        }

        private ArcherDefinitionSO[] GetDefinitions(GameManager gm)
        {
            var catalogDefinitions = ArcherCatalog != null ? ArcherCatalog.GetOrderedDefinitions() : null;
            if (catalogDefinitions != null && catalogDefinitions.Length > 0)
                return catalogDefinitions;

            return gm != null ? gm.GetArcherDefinitions() : null;
        }

        private void ClearDynamicRows()
        {
            for (int i = 0; i < _dynamicRows.Count; i++)
            {
                var row = _dynamicRows[i];
                if (row.BuyButton != null && row.BuyAction != null)
                    row.BuyButton.onClick.RemoveListener(row.BuyAction);

                if (row.Root == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(row.Root);
                else
                    DestroyImmediate(row.Root);
            }

            _dynamicRows.Clear();
        }

        private void RefreshDynamicArcherRow(DynamicArcherRow row, GameManager gm)
        {
            if (row == null || row.Definition == null || gm == null)
                return;

            ArcherType type = row.Definition.Type;
            bool unlocked = gm.IsArcherTypeUnlocked(type);
            bool canBuy = gm.CanBuyArcher(row.Definition);
            bool capReached = gm.GetRemainingArcherCapacity() <= 0;

            SetText(row.NameText, row.Definition.DisplayName);
            SetText(row.CountText, $"x{gm.GetArcherTypeCount(type)}");
            SetText(row.DpsText, unlocked ? $"DPS {gm.GetArcherTypeDps(type):0.#}" : "LOCKED");
            SetText(row.LevelText, unlocked ? $"LV {gm.GetArcherTypeLevel(type)}" : "TECH");
            SetText(row.CostText, !unlocked
                ? "LOCKED BY TECH"
                : capReached
                    ? $"ARMY CAP {gm.GetTotalArcherCount()}/{ArcherCapacityUtility.MaxTotalArchers}"
                    : BuildBuyCostLabel(gm, row.Definition));
            SetText(row.StatusText, BuildStatusLabel(gm, row.Definition, unlocked, canBuy));

            if (row.BuyButton != null)
            {
                row.BuyButton.interactable = canBuy;
                SetText(row.BuyButtonText, !unlocked ? "LOCKED" : capReached ? "MAX" : "BUY");
            }
        }

        private static string BuildBuyCostLabel(GameManager gm, ArcherDefinitionSO definition)
        {
            string label = FormatCostWithNeed(definition.BuyCost, gm.Resources, gm.IsFreeEconomyTestMode);
            if (!gm.IsFreeEconomyTestMode
                && gm.IsMobilePopulationEconomyEnabled()
                && definition.BuyCost.CanAfford(gm.Resources)
                && gm.GetIdlePopulation() < definition.PopulationCost)
            {
                label = $"{label} NEED POP";
            }

            return label;
        }

        private static string BuildStatusLabel(GameManager gm, ArcherDefinitionSO definition, bool unlocked, bool canBuy)
        {
            if (!unlocked)
                return string.IsNullOrEmpty(definition.RequiredTechId) ? "LOCKED" : "TECH LOCKED";

            if (gm.GetRemainingArcherCapacity() <= 0)
                return $"ARMY CAP {gm.GetTotalArcherCount()}/{ArcherCapacityUtility.MaxTotalArchers}";

            if (canBuy)
                return "READY";

            if (gm.IsFreeEconomyTestMode)
                return "READY";

            string need = definition.BuyCost.ToNeedDisplayString(gm.Resources);
            if (!string.IsNullOrEmpty(need))
                return need;

            if (gm.IsMobilePopulationEconomyEnabled() && gm.GetIdlePopulation() < definition.PopulationCost)
                return "NEED POP";

            return "WAIT";
        }

        private void BindDynamicRowButtons()
        {
            for (int i = 0; i < _dynamicRows.Count; i++)
            {
                var row = _dynamicRows[i];
                if (row.BuyButton == null || row.BuyAction != null)
                    continue;

                row.BuyAction = () => BuyArcher(row.Definition);
                row.BuyButton.onClick.AddListener(row.BuyAction);
            }
        }

        private void SetLegacyRowsActive(bool active)
        {
            CacheLegacyRows();
            SetOptionalChildActive(_basicLegacyRow, active);
            SetOptionalChildActive(_rapidLegacyRow, active);
            SetOptionalChildActive(_frostLegacyRow, active);
        }

        private void CacheLegacyRows()
        {
            if (_legacyRowsCached)
                return;

            _basicLegacyRow = FindChildByName(gameObject, "BasicArcherRow");
            _rapidLegacyRow = FindChildByName(gameObject, "RapidArcherRow");
            _frostLegacyRow = FindChildByName(gameObject, "FrostArcherRow");
            _legacyRowsCached = true;
        }

        private static void SetOptionalChildActive(GameObject child, bool active)
        {
            if (child != null && child.activeSelf != active)
                child.SetActive(active);
        }

        private static T FindChildComponent<T>(GameObject root, string name) where T : Component
        {
            var child = FindChildByName(root, name);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static GameObject FindChildByName(GameObject root, string name)
        {
            if (root == null)
                return null;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i].gameObject;
            }

            return null;
        }

        private sealed class DynamicArcherRow
        {
            public ArcherDefinitionSO Definition;
            public GameObject Root;
            public TMP_Text NameText;
            public TMP_Text CountText;
            public TMP_Text DpsText;
            public TMP_Text LevelText;
            public TMP_Text CostText;
            public TMP_Text StatusText;
            public Button BuyButton;
            public TMP_Text BuyButtonText;
            public UnityAction BuyAction;
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
            if (!gm.IsRepairPhaseAvailable())
                return "Day / Dusk only";

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
