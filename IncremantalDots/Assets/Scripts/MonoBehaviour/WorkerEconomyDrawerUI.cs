using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class WorkerEconomyDrawerUI : MonoBehaviour
    {
        [Header("Drawer")]
        public Button WorkerDrawerToggleButton;
        public GameObject WorkerEconomyDrawerPanel;
        public TMP_Text WorkerDrawerTitleText;
        public TMP_Text WorkerIdlePopulationText;
        public TMP_Text WorkerTotalText;
        public TMP_Text WorkerArcherPopulationText;

        [Header("Wood")]
        public TMP_Text WoodWorkerCountText;
        public TMP_Text WoodWorkerRateText;
        public Button WoodWorkerAddButton;
        public Button WoodWorkerTargetPlus10Button;
        public Button WoodWorkerTargetPlus100Button;
        public TMP_InputField WoodWorkerTargetInput;
        public TMP_Text WoodWorkerStatusText;
        public Button WoodCapacityUpgradeButton;
        public Button WoodEfficiencyUpgradeButton;

        [Header("Stone")]
        public TMP_Text StoneWorkerCountText;
        public TMP_Text StoneWorkerRateText;
        public Button StoneWorkerAddButton;
        public Button StoneWorkerTargetPlus10Button;
        public Button StoneWorkerTargetPlus100Button;
        public TMP_InputField StoneWorkerTargetInput;
        public TMP_Text StoneWorkerStatusText;
        public Button StoneCapacityUpgradeButton;
        public Button StoneEfficiencyUpgradeButton;

        [Header("Iron")]
        public TMP_Text IronWorkerCountText;
        public TMP_Text IronWorkerRateText;
        public Button IronWorkerAddButton;
        public Button IronWorkerTargetPlus10Button;
        public Button IronWorkerTargetPlus100Button;
        public TMP_InputField IronWorkerTargetInput;
        public TMP_Text IronWorkerStatusText;
        public Button IronCapacityUpgradeButton;
        public Button IronEfficiencyUpgradeButton;

        [Header("Food")]
        public TMP_Text FoodWorkerCountText;
        public TMP_Text FoodWorkerRateText;
        public Button FoodWorkerAddButton;
        public Button FoodWorkerTargetPlus10Button;
        public Button FoodWorkerTargetPlus100Button;
        public TMP_InputField FoodWorkerTargetInput;
        public TMP_Text FoodWorkerStatusText;
        public Button FoodCapacityUpgradeButton;
        public Button FoodEfficiencyUpgradeButton;

        private bool _isOpen;
        private float _nextRefreshTime;
        private bool _hasRefreshFingerprint;
        private int _lastRefreshFingerprint;

        private void OnEnable()
        {
            ResolveMissingUpgradeButtons();
            BindControls();
            SetOpen(false);
            Refresh();
        }

        private void OnDisable()
        {
            UnbindControls();
        }

        private void ResolveMissingUpgradeButtons()
        {
            WoodCapacityUpgradeButton ??= FindButton("WoodCapacityUpgradeButton");
            WoodEfficiencyUpgradeButton ??= FindButton("WoodEfficiencyUpgradeButton");
            StoneCapacityUpgradeButton ??= FindButton("StoneCapacityUpgradeButton");
            StoneEfficiencyUpgradeButton ??= FindButton("StoneEfficiencyUpgradeButton");
            IronCapacityUpgradeButton ??= FindButton("IronCapacityUpgradeButton");
            IronEfficiencyUpgradeButton ??= FindButton("IronEfficiencyUpgradeButton");
            FoodCapacityUpgradeButton ??= FindButton("FoodCapacityUpgradeButton");
            FoodEfficiencyUpgradeButton ??= FindButton("FoodEfficiencyUpgradeButton");
        }

        private Button FindButton(string objectName)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name == objectName)
                    return button;
            }

            return null;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.15f;
            RefreshIfChanged();
        }

        private void BindControls()
        {
            UnbindControls();
            WorkerDrawerToggleButton?.onClick.AddListener(Toggle);
            WoodWorkerAddButton?.onClick.AddListener(HandleWoodPlus1Clicked);
            WoodWorkerTargetPlus10Button?.onClick.AddListener(HandleWoodPlus10Clicked);
            WoodWorkerTargetPlus100Button?.onClick.AddListener(HandleWoodPlus100Clicked);
            WoodWorkerTargetInput?.onEndEdit.AddListener(HandleWoodTargetInput);
            WoodCapacityUpgradeButton?.onClick.AddListener(HandleWoodCapacityClicked);
            WoodEfficiencyUpgradeButton?.onClick.AddListener(HandleWoodEfficiencyClicked);
            StoneWorkerAddButton?.onClick.AddListener(HandleStonePlus1Clicked);
            StoneWorkerTargetPlus10Button?.onClick.AddListener(HandleStonePlus10Clicked);
            StoneWorkerTargetPlus100Button?.onClick.AddListener(HandleStonePlus100Clicked);
            StoneWorkerTargetInput?.onEndEdit.AddListener(HandleStoneTargetInput);
            StoneCapacityUpgradeButton?.onClick.AddListener(HandleStoneCapacityClicked);
            StoneEfficiencyUpgradeButton?.onClick.AddListener(HandleStoneEfficiencyClicked);
            IronWorkerAddButton?.onClick.AddListener(HandleIronPlus1Clicked);
            IronWorkerTargetPlus10Button?.onClick.AddListener(HandleIronPlus10Clicked);
            IronWorkerTargetPlus100Button?.onClick.AddListener(HandleIronPlus100Clicked);
            IronWorkerTargetInput?.onEndEdit.AddListener(HandleIronTargetInput);
            IronCapacityUpgradeButton?.onClick.AddListener(HandleIronCapacityClicked);
            IronEfficiencyUpgradeButton?.onClick.AddListener(HandleIronEfficiencyClicked);
            FoodWorkerAddButton?.onClick.AddListener(HandleFoodPlus1Clicked);
            FoodWorkerTargetPlus10Button?.onClick.AddListener(HandleFoodPlus10Clicked);
            FoodWorkerTargetPlus100Button?.onClick.AddListener(HandleFoodPlus100Clicked);
            FoodWorkerTargetInput?.onEndEdit.AddListener(HandleFoodTargetInput);
            FoodCapacityUpgradeButton?.onClick.AddListener(HandleFoodCapacityClicked);
            FoodEfficiencyUpgradeButton?.onClick.AddListener(HandleFoodEfficiencyClicked);
        }

        private void UnbindControls()
        {
            WorkerDrawerToggleButton?.onClick.RemoveListener(Toggle);
            WoodWorkerAddButton?.onClick.RemoveListener(HandleWoodPlus1Clicked);
            WoodWorkerTargetPlus10Button?.onClick.RemoveListener(HandleWoodPlus10Clicked);
            WoodWorkerTargetPlus100Button?.onClick.RemoveListener(HandleWoodPlus100Clicked);
            WoodWorkerTargetInput?.onEndEdit.RemoveListener(HandleWoodTargetInput);
            WoodCapacityUpgradeButton?.onClick.RemoveListener(HandleWoodCapacityClicked);
            WoodEfficiencyUpgradeButton?.onClick.RemoveListener(HandleWoodEfficiencyClicked);
            StoneWorkerAddButton?.onClick.RemoveListener(HandleStonePlus1Clicked);
            StoneWorkerTargetPlus10Button?.onClick.RemoveListener(HandleStonePlus10Clicked);
            StoneWorkerTargetPlus100Button?.onClick.RemoveListener(HandleStonePlus100Clicked);
            StoneWorkerTargetInput?.onEndEdit.RemoveListener(HandleStoneTargetInput);
            StoneCapacityUpgradeButton?.onClick.RemoveListener(HandleStoneCapacityClicked);
            StoneEfficiencyUpgradeButton?.onClick.RemoveListener(HandleStoneEfficiencyClicked);
            IronWorkerAddButton?.onClick.RemoveListener(HandleIronPlus1Clicked);
            IronWorkerTargetPlus10Button?.onClick.RemoveListener(HandleIronPlus10Clicked);
            IronWorkerTargetPlus100Button?.onClick.RemoveListener(HandleIronPlus100Clicked);
            IronWorkerTargetInput?.onEndEdit.RemoveListener(HandleIronTargetInput);
            IronCapacityUpgradeButton?.onClick.RemoveListener(HandleIronCapacityClicked);
            IronEfficiencyUpgradeButton?.onClick.RemoveListener(HandleIronEfficiencyClicked);
            FoodWorkerAddButton?.onClick.RemoveListener(HandleFoodPlus1Clicked);
            FoodWorkerTargetPlus10Button?.onClick.RemoveListener(HandleFoodPlus10Clicked);
            FoodWorkerTargetPlus100Button?.onClick.RemoveListener(HandleFoodPlus100Clicked);
            FoodWorkerTargetInput?.onEndEdit.RemoveListener(HandleFoodTargetInput);
            FoodCapacityUpgradeButton?.onClick.RemoveListener(HandleFoodCapacityClicked);
            FoodEfficiencyUpgradeButton?.onClick.RemoveListener(HandleFoodEfficiencyClicked);
        }

        private void Toggle()
        {
            SetOpen(!_isOpen);
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            if (WorkerEconomyDrawerPanel != null)
                WorkerEconomyDrawerPanel.SetActive(open);
            SetButtonText(WorkerDrawerToggleButton, open ? "Workers" : "Workers");
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            bool enabledForMode = gm != null && gm.IsMobilePopulationEconomyEnabled();

            if (WorkerDrawerToggleButton != null)
                WorkerDrawerToggleButton.gameObject.SetActive(enabledForMode);
            if (WorkerEconomyDrawerPanel != null && !enabledForMode)
                WorkerEconomyDrawerPanel.SetActive(false);

            if (!enabledForMode)
            {
                _lastRefreshFingerprint = ComputeRefreshFingerprint(gm);
                _hasRefreshFingerprint = true;
                return;
            }

            var population = gm.Population;
            int totalWorkers = gm.GetResourceWorkers(EconomyFocusType.Balanced);
            int idle = gm.GetIdlePopulation();

            SetText(WorkerDrawerTitleText, "WORKERS");
            SetText(WorkerIdlePopulationText, $"IDLE {idle}");
            SetText(WorkerTotalText, $"WORKERS {totalWorkers}/{gm.GetAvailablePopulation()}");
            SetText(WorkerArcherPopulationText, $"ARCHERS {population.Archers}");

            RefreshRow(gm, EconomyFocusType.Wood, "WOOD", WoodWorkerCountText, WoodWorkerRateText,
                WoodWorkerAddButton, WoodWorkerTargetPlus10Button, WoodWorkerTargetPlus100Button,
                WoodWorkerTargetInput, WoodWorkerStatusText,
                WoodCapacityUpgradeButton, WoodEfficiencyUpgradeButton);
            RefreshRow(gm, EconomyFocusType.Stone, "STONE", StoneWorkerCountText, StoneWorkerRateText,
                StoneWorkerAddButton, StoneWorkerTargetPlus10Button, StoneWorkerTargetPlus100Button,
                StoneWorkerTargetInput, StoneWorkerStatusText,
                StoneCapacityUpgradeButton, StoneEfficiencyUpgradeButton);
            RefreshRow(gm, EconomyFocusType.Iron, "IRON", IronWorkerCountText, IronWorkerRateText,
                IronWorkerAddButton, IronWorkerTargetPlus10Button, IronWorkerTargetPlus100Button,
                IronWorkerTargetInput, IronWorkerStatusText,
                IronCapacityUpgradeButton, IronEfficiencyUpgradeButton);
            RefreshRow(gm, EconomyFocusType.Food, "FOOD", FoodWorkerCountText, FoodWorkerRateText,
                FoodWorkerAddButton, FoodWorkerTargetPlus10Button, FoodWorkerTargetPlus100Button,
                FoodWorkerTargetInput, FoodWorkerStatusText,
                FoodCapacityUpgradeButton, FoodEfficiencyUpgradeButton);

            _lastRefreshFingerprint = ComputeRefreshFingerprint(gm);
            _hasRefreshFingerprint = true;
        }

        private void RefreshIfChanged()
        {
            var gm = GameManager.Instance;
            int fingerprint = ComputeRefreshFingerprint(gm);
            if (_hasRefreshFingerprint && fingerprint == _lastRefreshFingerprint)
                return;

            Refresh();
        }

        private static int ComputeRefreshFingerprint(GameManager gm)
        {
            unchecked
            {
                int hash = 17;
                bool enabledForMode = gm != null && gm.IsMobilePopulationEconomyEnabled();
                AddFingerprintValue(ref hash, enabledForMode);
                if (!enabledForMode)
                    return hash;

                AddFingerprintValue(ref hash, gm.IsFreeEconomyTestMode);
                AddFingerprintValue(ref hash, gm.Population.Total);
                AddFingerprintValue(ref hash, gm.Population.Archers);
                AddFingerprintValue(ref hash, gm.GetIdlePopulation());
                AddFingerprintValue(ref hash, gm.GetAvailablePopulation());
                AddFingerprintValue(ref hash, gm.Resources.Wood);
                AddFingerprintValue(ref hash, gm.Resources.Iron);
                AddFingerprintValue(ref hash, gm.GameState.IsGameOver);
                AddResourceFingerprint(ref hash, gm, EconomyFocusType.Wood);
                AddResourceFingerprint(ref hash, gm, EconomyFocusType.Stone);
                AddResourceFingerprint(ref hash, gm, EconomyFocusType.Iron);
                AddResourceFingerprint(ref hash, gm, EconomyFocusType.Food);
                return hash;
            }
        }

        private static void AddResourceFingerprint(ref int hash, GameManager gm, EconomyFocusType resource)
        {
            AddFingerprintValue(ref hash, gm.GetResourceWorkers(resource));
            AddFingerprintValue(ref hash, gm.GetWorkerTargetRatioBps(resource));
            AddFingerprintValue(ref hash, gm.GetMaxWorkersForResource(resource));
            AddFingerprintValue(ref hash, gm.GetWorkerProductionRate(resource).GetHashCode());
            AddFingerprintValue(ref hash, gm.CanAssignResourceWorker(resource));
            AddUpgradeFingerprint(ref hash, gm, resource, WorkerBuildingUpgradeType.Capacity);
            AddUpgradeFingerprint(ref hash, gm, resource, WorkerBuildingUpgradeType.Efficiency);
        }

        private static void AddUpgradeFingerprint(ref int hash, GameManager gm,
            EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType)
        {
            ResourceCost cost = gm.GetWorkerBuildingUpgradeCost(resource, upgradeType);
            AddFingerprintValue(ref hash, gm.GetWorkerBuildingUpgradeLevel(resource, upgradeType));
            AddFingerprintValue(ref hash, cost.Wood);
            AddFingerprintValue(ref hash, cost.Iron);
            AddFingerprintValue(ref hash, gm.CanBuyWorkerBuildingUpgrade(resource, upgradeType));
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

        private static void RefreshRow(GameManager gm, EconomyFocusType resource, string label,
            TMP_Text countText, TMP_Text rateText, Button plus1Button, Button plus10Button,
            Button plus100Button, TMP_InputField targetInput, TMP_Text statusText,
            Button capacityButton, Button efficiencyButton)
        {
            int count = gm.GetResourceWorkers(resource);
            int cap = gm.GetMaxWorkersForResource(resource);
            float rate = gm.GetWorkerProductionRate(resource);
            bool atCap = cap > 0 && count >= cap;
            float targetPercent = gm.GetWorkerTargetRatioBps(resource) / 100f;

            SetText(countText, cap > 0 ? $"{label} {count}/{cap}" : $"{label} x{count}");
            SetText(rateText, $"+{rate:0}/min");
            SetText(statusText, atCap ? $"TGT {targetPercent:0.##}% CAP" : $"TGT {targetPercent:0.##}%");

            ConfigureTargetButton(plus1Button, "+1%");
            ConfigureTargetButton(plus10Button, "+10%");
            ConfigureTargetButton(plus100Button, "+100%");
            ConfigureUpgradeButton(gm, resource, WorkerBuildingUpgradeType.Capacity,
                capacityButton, "CAP");
            ConfigureUpgradeButton(gm, resource, WorkerBuildingUpgradeType.Efficiency,
                efficiencyButton, "EFF");
            if (targetInput != null && !targetInput.isFocused)
                targetInput.SetTextWithoutNotify(targetPercent.ToString("0.##", CultureInfo.InvariantCulture));
        }

        private void HandleWoodPlus1Clicked() => AdjustTarget(EconomyFocusType.Wood, 1);
        private void HandleWoodPlus10Clicked() => AdjustTarget(EconomyFocusType.Wood, 10);
        private void HandleWoodPlus100Clicked() => AdjustTarget(EconomyFocusType.Wood, 100);
        private void HandleWoodTargetInput(string value) => SetTarget(EconomyFocusType.Wood, value);
        private void HandleStonePlus1Clicked() => AdjustTarget(EconomyFocusType.Stone, 1);
        private void HandleStonePlus10Clicked() => AdjustTarget(EconomyFocusType.Stone, 10);
        private void HandleStonePlus100Clicked() => AdjustTarget(EconomyFocusType.Stone, 100);
        private void HandleStoneTargetInput(string value) => SetTarget(EconomyFocusType.Stone, value);
        private void HandleIronPlus1Clicked() => AdjustTarget(EconomyFocusType.Iron, 1);
        private void HandleIronPlus10Clicked() => AdjustTarget(EconomyFocusType.Iron, 10);
        private void HandleIronPlus100Clicked() => AdjustTarget(EconomyFocusType.Iron, 100);
        private void HandleIronTargetInput(string value) => SetTarget(EconomyFocusType.Iron, value);
        private void HandleFoodPlus1Clicked() => AdjustTarget(EconomyFocusType.Food, 1);
        private void HandleFoodPlus10Clicked() => AdjustTarget(EconomyFocusType.Food, 10);
        private void HandleFoodPlus100Clicked() => AdjustTarget(EconomyFocusType.Food, 100);
        private void HandleFoodTargetInput(string value) => SetTarget(EconomyFocusType.Food, value);
        private void HandleWoodCapacityClicked() => BuyUpgrade(
            EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity);
        private void HandleWoodEfficiencyClicked() => BuyUpgrade(
            EconomyFocusType.Wood, WorkerBuildingUpgradeType.Efficiency);
        private void HandleStoneCapacityClicked() => BuyUpgrade(
            EconomyFocusType.Stone, WorkerBuildingUpgradeType.Capacity);
        private void HandleStoneEfficiencyClicked() => BuyUpgrade(
            EconomyFocusType.Stone, WorkerBuildingUpgradeType.Efficiency);
        private void HandleIronCapacityClicked() => BuyUpgrade(
            EconomyFocusType.Iron, WorkerBuildingUpgradeType.Capacity);
        private void HandleIronEfficiencyClicked() => BuyUpgrade(
            EconomyFocusType.Iron, WorkerBuildingUpgradeType.Efficiency);
        private void HandleFoodCapacityClicked() => BuyUpgrade(
            EconomyFocusType.Food, WorkerBuildingUpgradeType.Capacity);
        private void HandleFoodEfficiencyClicked() => BuyUpgrade(
            EconomyFocusType.Food, WorkerBuildingUpgradeType.Efficiency);

        private void BuyUpgrade(EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType)
        {
            GameManager.Instance?.TryBuyWorkerBuildingUpgrade(resource, upgradeType);
            Refresh();
        }

        private void AdjustTarget(EconomyFocusType resource, int deltaPercent)
        {
            GameManager.Instance?.AdjustWorkerTargetRatioPercent(resource, deltaPercent);
            Refresh();
        }

        private void SetTarget(EconomyFocusType resource, string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimEnd('%').Replace(',', '.');
            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out float targetPercent))
            {
                GameManager.Instance?.SetWorkerTargetRatioPercent(resource, targetPercent);
            }
            Refresh();
        }

        private static void ConfigureTargetButton(Button button, string label)
        {
            if (button == null)
                return;

            button.interactable = true;
            SetButtonText(button, label);
        }

        private static void ConfigureUpgradeButton(GameManager gm, EconomyFocusType resource,
            WorkerBuildingUpgradeType upgradeType, Button button, string label)
        {
            if (button == null)
                return;

            int level = gm.GetWorkerBuildingUpgradeLevel(resource, upgradeType);
            ResourceCost cost = gm.GetWorkerBuildingUpgradeCost(resource, upgradeType);
            bool validCost = cost.Wood > 0 || cost.Iron > 0;
            button.interactable = validCost
                && gm.CanBuyWorkerBuildingUpgrade(resource, upgradeType);
            SetButtonText(button, validCost
                ? $"{label} L{level}\n{cost.ToDisplayString()}"
                : $"{label} L{level}\nCOST LIMIT");
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null)
                text.text = value;
        }
    }
}
