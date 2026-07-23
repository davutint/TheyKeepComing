using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private ScrollView _economyRows;
        private Label _economyIdle;
        private Label _economyWorkers;
        private Label _economyHousing;
        private VisualElement _housingCard;
        private Label _housingStatus;
        private Label _housingCost;
        private Button _housingOne;
        private Button _housingTen;
        private Button _housingHundred;

        private ScrollView _archerRows;
        private Label _archerCapacity;
        private Label _archerSummary;

        private Label _supplyHeroValue;
        private Label _supplyHeroState;
        private VisualElement _supplyHeroProgress;
        private Label _arrowPackageDetail;
        private Label _arrowPackageCost;
        private Label _arrowLargeDetail;
        private Label _arrowLargeCost;
        private Label _arrowMaxDetail;
        private Label _arrowMaxCost;
        private Label _arrowCapacityDetail;
        private Label _arrowCapacityCost;
        private Label _arrowEfficiencyDetail;
        private Label _arrowEfficiencyCost;
        private Button _arrowPackageButton;
        private Button _arrowLargeButton;
        private Button _arrowMaxButton;
        private Button _arrowCapacityButton;
        private Button _arrowEfficiencyButton;

        private int _builtArcherDefinitionCount = -1;

        private void BindManagementActions()
        {
            Q<Button>("economyClose").clicked += CloseEconomySurfaceFromPlayer;
            Q<Button>("barracksClose").clicked += CloseSurface;
            Q<Button>("arrowsClose").clicked += CloseSurface;

            _economyRows = Q<ScrollView>("economyRows");
            _economyIdle = Q<Label>("economyIdle");
            _economyWorkers = Q<Label>("economyWorkers");
            _economyHousing = Q<Label>("economyHousing");
            _housingCard = Q<VisualElement>("housingCard");
            _housingStatus = Q<Label>("housingStatus");
            _housingCost = Q<Label>("housingCost");
            _housingOne = Q<Button>("housingOne");
            _housingTen = Q<Button>("housingTen");
            _housingHundred = Q<Button>("housingHundred");
            _housingOne.clicked += () => BuyHousing(1);
            _housingTen.clicked += () => BuyHousing(10);
            _housingHundred.clicked += () => BuyHousing(100);

            _archerRows = Q<ScrollView>("archerRows");
            _archerCapacity = Q<Label>("archerCapacity");
            _archerSummary = Q<Label>("archerSummary");

            _supplyHeroValue = Q<Label>("supplyHeroValue");
            _supplyHeroState = Q<Label>("supplyHeroState");
            _supplyHeroProgress = Q<VisualElement>("supplyHeroProgress");
            _arrowPackageDetail = Q<Label>("arrowPackageDetail");
            _arrowPackageCost = Q<Label>("arrowPackageCost");
            _arrowLargeDetail = Q<Label>("arrowLargeDetail");
            _arrowLargeCost = Q<Label>("arrowLargeCost");
            _arrowMaxDetail = Q<Label>("arrowMaxDetail");
            _arrowMaxCost = Q<Label>("arrowMaxCost");
            _arrowCapacityDetail = Q<Label>("arrowCapacityDetail");
            _arrowCapacityCost = Q<Label>("arrowCapacityCost");
            _arrowEfficiencyDetail = Q<Label>("arrowEfficiencyDetail");
            _arrowEfficiencyCost = Q<Label>("arrowEfficiencyCost");
            _arrowPackageButton = Q<Button>("arrowPackage");
            _arrowLargeButton = Q<Button>("arrowLargePackage");
            _arrowMaxButton = Q<Button>("arrowBuyMax");
            _arrowCapacityButton = Q<Button>("arrowCapacityUpgrade");
            _arrowEfficiencyButton = Q<Button>("arrowEfficiencyUpgrade");
            _arrowPackageButton.clicked += () => BuyArrows(1);
            _arrowLargeButton.clicked += () => BuyArrows(5);
            _arrowMaxButton.clicked += BuyMaxArrows;
            _arrowCapacityButton.clicked += () => BuyArrowUpgrade(ArrowUpgradeType.Capacity);
            _arrowEfficiencyButton.clicked += () => BuyArrowUpgrade(ArrowUpgradeType.Efficiency);
        }

        private void BuildEconomyRows()
        {
            if (_economyRows == null)
                return;

            _economyRows.Clear();
            for (int i = 0; i < EconomyResources.Length; i++)
            {
                EconomyFocusType resource = EconomyResources[i];
                VisualElement row = new VisualElement { name = "economyRow" + resource };
                row.AddToClassList("economy-row");

                VisualElement heading = new VisualElement();
                heading.AddToClassList("row-heading");
                VisualElement headingCopy = new VisualElement();
                headingCopy.AddToClassList("row-heading-copy");
                headingCopy.Add(CreateRoleIcon(ResourceIconRole(resource), "dw-icon--row"));
                Label title = new Label(ResourceName(resource)) { name = "economyTitle" + resource };
                title.AddToClassList("row-title");
                headingCopy.Add(title);
                heading.Add(headingCopy);
                Label stat = new Label("0 / 0") { name = "economyStat" + resource };
                stat.AddToClassList("row-stat");
                heading.Add(stat);
                row.Add(heading);

                Label detail = new Label("0/MIN") { name = "economyDetail" + resource };
                detail.AddToClassList("row-detail");
                row.Add(detail);

                VisualElement allocationControl = new VisualElement();
                allocationControl.AddToClassList("worker-allocation-control");
                VisualElement allocationHeader = new VisualElement();
                allocationHeader.AddToClassList("worker-allocation-header");
                Label assignmentLabel = new Label("WORKER SHARE");
                assignmentLabel.AddToClassList("assignment-label");
                Label allocationValue = new Label("25% SHARE")
                {
                    name = "economyAllocationValue" + resource
                };
                allocationValue.AddToClassList("worker-allocation-value");
                allocationHeader.Add(assignmentLabel);
                allocationHeader.Add(allocationValue);
                allocationControl.Add(allocationHeader);

                SliderInt allocationSlider = new SliderInt
                {
                    name = "economyAllocationSlider" + resource,
                    lowValue = 0,
                    highValue = 100,
                    pageSize = 5,
                    showInputField = false
                };
                allocationSlider.AddToClassList("worker-allocation-slider");
                allocationSlider.RegisterValueChangedCallback(evt =>
                    SetWorkerAllocationShare(resource, evt.newValue));
                allocationControl.Add(allocationSlider);
                row.Add(allocationControl);

                VisualElement actions = new VisualElement();
                actions.AddToClassList("row-actions");
                actions.AddToClassList("upgrade-actions");
                Button capacity = CreateCompactButton("CAPACITY", () => BuyWorkerUpgrade(resource, WorkerBuildingUpgradeType.Capacity));
                capacity.name = "economyCapacity" + resource;
                Button efficiency = CreateCompactButton("EFFICIENCY", () => BuyWorkerUpgrade(resource, WorkerBuildingUpgradeType.Efficiency));
                efficiency.name = "economyEfficiency" + resource;
                actions.Add(capacity);
                actions.Add(efficiency);
                row.Add(actions);
                _economyRows.Add(row);
            }
        }

        private void BuildArcherRows()
        {
            GameManager gm = GameManager.Instance;
            if (_archerRows == null || gm == null)
                return;

            ArcherDefinitionSO[] source = gm.GetArcherDefinitions();
            var definitions = new List<ArcherDefinitionSO>();
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                    if (source[i] != null)
                        definitions.Add(source[i]);
            }
            definitions.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            if (_builtArcherDefinitionCount == definitions.Count && _archerRows.childCount == definitions.Count)
                return;

            _builtArcherDefinitionCount = definitions.Count;
            _archerRows.Clear();
            for (int i = 0; i < definitions.Count; i++)
            {
                ArcherDefinitionSO definition = definitions[i];
                ArcherType type = definition.Type;
                VisualElement row = new VisualElement { name = "archerRow" + type };
                row.AddToClassList("archer-row");

                VisualElement heading = new VisualElement();
                heading.AddToClassList("row-heading");
                VisualElement headingCopy = new VisualElement();
                headingCopy.AddToClassList("row-heading-copy");
                headingCopy.Add(CreateRoleIcon(ArcherIconRole(type), "dw-icon--row"));
                Label title = new Label(definition.DisplayName.ToUpperInvariant());
                title.AddToClassList("row-title");
                headingCopy.Add(title);
                heading.Add(headingCopy);
                Label count = new Label("0 DEPLOYED") { name = "archerCount" + type };
                count.AddToClassList("row-stat");
                heading.Add(count);
                row.Add(heading);

                Label role = new Label(definition.Description) { name = "archerRole" + type };
                role.AddToClassList("archer-role");
                row.Add(role);
                Label detail = new Label("0 DAMAGE / SEC") { name = "archerDetail" + type };
                detail.AddToClassList("row-detail");
                row.Add(detail);

                VisualElement actions = new VisualElement();
                actions.AddToClassList("row-actions");
                Button buy = CreateCompactButton("RECRUIT", () => BuyArcher(type));
                buy.name = "archerBuy" + type;
                buy.AddToClassList("compact-action--primary");
                actions.Add(buy);
                if (type != ArcherType.Basic)
                {
                    Button retrain = CreateCompactButton("RETRAIN BASIC", () => RetrainArcher(type));
                    retrain.name = "archerRetrain" + type;
                    actions.Add(retrain);
                }
                row.Add(actions);
                _archerRows.Add(row);
            }
        }

        private static Button CreateCompactButton(string text, Action clicked)
        {
            Button button = new Button(clicked) { text = text };
            button.AddToClassList("compact-action");
            return button;
        }

        private void RefreshManagement(GameManager gm, bool rebuildDynamic)
        {
            if (rebuildDynamic)
            {
                BuildEconomyRows();
                BuildArcherRows();
            }

            RefreshEconomy(gm);
            RefreshBarracks(gm);
            RefreshArrowSupply(gm);
        }

        private void RefreshEconomy(GameManager gm)
        {
            if (_economyIdle == null)
                return;

            _economyIdle.text = gm.GetIdlePopulation().ToString("N0", CultureInfo.InvariantCulture);
            _economyWorkers.text = gm.GetResourceWorkers(EconomyFocusType.Balanced)
                .ToString("N0", CultureInfo.InvariantCulture);
            int bedCapacity = gm.GetTotalBedCapacity();
            int availableBeds = Mathf.Max(0, bedCapacity - gm.Population.Total);
            _economyHousing.text = $"{gm.Population.Total:N0} / {bedCapacity:N0}";
            _housingCard.EnableInClassList("is-full", availableBeds <= 0);
            _housingStatus.text = availableBeds <= 0
                ? "HOUSING FULL — ADD BEDS BEFORE MORE PEOPLE ARRIVE"
                : $"{availableBeds:N0} BEDS AVAILABLE";
            _housingCost.text = "Choose a package. Beds raise your population capacity.";
            _housingOne.text = $"ADD 1 BED\nCOST: {FormatCost(gm.GetBedCapacityPurchaseCost(1))}";
            _housingTen.text = $"ADD 10 BEDS\nCOST: {FormatCost(gm.GetBedCapacityPurchaseCost(10))}";
            _housingHundred.text = $"ADD 100 BEDS\nCOST: {FormatCost(gm.GetBedCapacityPurchaseCost(100))}";
            SetExplainedActionState(_housingOne, gm.CanBuyBedCapacity(1));
            SetExplainedActionState(_housingTen, gm.CanBuyBedCapacity(10));
            SetExplainedActionState(_housingHundred, gm.CanBuyBedCapacity(100));

            for (int i = 0; i < EconomyResources.Length; i++)
            {
                EconomyFocusType resource = EconomyResources[i];
                int workers = gm.GetResourceWorkers(resource);
                int target = Mathf.RoundToInt(gm.GetWorkerTargetRatioBps(resource) / 100f);
                int capacityValue = gm.GetMaxWorkersForResource(resource);
                float rate = gm.GetWorkerProductionRate(resource);
                Q<Label>("economyStat" + resource).text = $"{workers:N0} WORKERS  ·  {capacityValue:N0} CAPACITY";
                Q<Label>("economyDetail" + resource).text = $"+{rate:0.#}/MIN";
                Q<Label>("economyAllocationValue" + resource).text = target == 0 && workers > 0
                    ? $"0% TARGET  ·  {workers:N0} WORKERS  ·  CAPACITY OVERFLOW"
                    : $"{target}% TARGET  ·  {workers:N0} WORKERS";
                Q<SliderInt>("economyAllocationSlider" + resource).SetValueWithoutNotify(target);

                int capacityLevel = gm.GetWorkerBuildingUpgradeLevel(resource, WorkerBuildingUpgradeType.Capacity);
                int efficiencyLevel = gm.GetWorkerBuildingUpgradeLevel(resource, WorkerBuildingUpgradeType.Efficiency);
                ResourceCost capacityCost = gm.GetWorkerBuildingUpgradeCost(resource, WorkerBuildingUpgradeType.Capacity);
                ResourceCost efficiencyCost = gm.GetWorkerBuildingUpgradeCost(resource, WorkerBuildingUpgradeType.Efficiency);
                Button capacity = Q<Button>("economyCapacity" + resource);
                Button efficiency = Q<Button>("economyEfficiency" + resource);
                capacity.text = $"CAPACITY  ·  LEVEL {capacityLevel}\nCOST: {FormatCost(capacityCost)}";
                efficiency.text = $"EFFICIENCY  ·  LEVEL {efficiencyLevel}\nCOST: {FormatCost(efficiencyCost)}";
                SetExplainedActionState(
                    capacity,
                    gm.CanBuyWorkerBuildingUpgrade(resource, WorkerBuildingUpgradeType.Capacity),
                    HasResourceCost(capacityCost));
                SetExplainedActionState(
                    efficiency,
                    gm.CanBuyWorkerBuildingUpgrade(resource, WorkerBuildingUpgradeType.Efficiency),
                    HasResourceCost(efficiencyCost));
            }
        }

        private void RefreshBarracks(GameManager gm)
        {
            if (_archerCapacity == null)
                return;

            BuildArcherRows();
            int total = gm.GetTotalArcherCount();
            _archerCapacity.text = gm.GetRemainingArcherCapacity() <= 0
                ? "GARRISON FULL"
                : $"{total:N0} ARCHERS DEPLOYED";
            _archerSummary.text = $"{gm.BasicArcherCount:N0} BASIC  ·  {gm.RapidArcherCount:N0} RAPID  ·  {gm.FrostArcherCount:N0} FROST";

            ArcherDefinitionSO[] definitions = gm.GetArcherDefinitions();
            if (definitions == null)
                return;
            for (int i = 0; i < definitions.Length; i++)
            {
                ArcherDefinitionSO definition = definitions[i];
                if (definition == null)
                    continue;
                ArcherType type = definition.Type;
                int count = gm.GetArcherTypeCount(type);
                bool unlocked = gm.IsArcherTypeUnlocked(type);
                Q<Label>("archerCount" + type).text = unlocked ? $"{count:N0} DEPLOYED" : "LOCKED";
                Q<Label>("archerDetail" + type).text = unlocked
                    ? $"{gm.GetArcherTypeDps(type):0.#} DAMAGE / SEC  ·  {FormatCost(gm.GetArcherBuyCost(definition))}"
                    : "RESEARCH IN CASTLE HEART";
                Button buy = Q<Button>("archerBuy" + type);
                buy.text = unlocked ? $"RECRUIT  ·  {FormatCost(gm.GetArcherBuyCost(definition))}" : "LOCKED";
                SetExplainedActionState(
                    buy,
                    gm.CanBuyArcher(definition),
                    GameplayActionFeedbackUtility.CanExplainArcherRecruitmentFailure(
                        unlocked,
                        gm.GetRemainingArcherCapacity()));
                Button retrain = Q<Button>("archerRetrain" + type);
                if (retrain != null)
                {
                    retrain.text = $"RETRAIN  ·  {FormatCost(gm.GetArcherRetrainCost(type))}";
                    SetExplainedActionState(
                        retrain,
                        gm.CanRetrainBasicArcher(type),
                        GameplayActionFeedbackUtility.CanExplainArcherRetrainingFailure(
                            unlocked,
                            gm.BasicArcherCount));
                }
            }
        }

        private void RefreshArrowSupply(GameManager gm)
        {
            if (_supplyHeroValue == null)
                return;

            int capacity = gm.GetArrowCapacity();
            int current = gm.ArrowSupply.Current;
            float actualRatio = capacity > 0 ? current / (float)capacity : 0f;
            bool deliveryActive = gm.IsArrowRefillDeliveryActive;
            int projectedCurrent = Mathf.Min(
                capacity,
                current + gm.PendingArrowRefillDeliveryAmount);
            float projectedRatio = capacity > 0
                ? projectedCurrent / (float)capacity
                : 0f;
            float displayRatio = deliveryActive
                ? Mathf.Lerp(
                    actualRatio,
                    projectedRatio,
                    gm.ArrowRefillDeliveryProgress01)
                : actualRatio;
            _supplyHeroValue.text = $"{current:N0} / {capacity:N0}";
            _supplyHeroState.text = deliveryActive
                ? $"DELIVERING  ·  {gm.ArrowRefillDeliveryRemainingSeconds:0.0}S"
                : actualRatio <= 0.25f
                    ? "LOW SUPPLY"
                    : actualRatio >= 0.995f
                        ? "RESERVE FULL"
                        : "SUPPLY READY";
            _supplyHeroState.EnableInClassList(
                "is-negative",
                !deliveryActive && actualRatio <= 0.25f);
            _supplyHeroState.EnableInClassList("is-delivering", deliveryActive);
            _supplyHeroProgress.style.width = Length.Percent(displayRatio * 100f);
            _supplyHeroProgress.style.backgroundColor = deliveryActive
                ? new Color(0.84f, 0.60f, 0.25f, 1f)
                : actualRatio <= 0.25f
                    ? new Color(0.84f, 0.36f, 0.25f, 1f)
                    : new Color(0.55f, 0.70f, 0.48f, 1f);

            ArrowRefillQuote package = gm.GetArrowRefillQuote(1);
            ArrowRefillQuote large = gm.GetArrowRefillQuote(5);
            ArrowRefillQuote max = gm.GetArrowBuyMaxQuote();
            SetArrowQuote(_arrowPackageDetail, _arrowPackageCost, package);
            SetArrowQuote(_arrowLargeDetail, _arrowLargeCost, large);
            SetArrowQuote(_arrowMaxDetail, _arrowMaxCost, max);
            SetExplainedActionState(_arrowPackageButton, gm.CanBuyArrowRefill(1));
            SetExplainedActionState(_arrowLargeButton, gm.CanBuyArrowRefill(5));
            SetExplainedActionState(_arrowMaxButton, gm.CanBuyMaxArrowRefill());

            RefreshArrowUpgrade(gm, ArrowUpgradeType.Capacity, _arrowCapacityButton, _arrowCapacityDetail, _arrowCapacityCost);
            RefreshArrowUpgrade(gm, ArrowUpgradeType.Efficiency, _arrowEfficiencyButton, _arrowEfficiencyDetail, _arrowEfficiencyCost);
        }

        private static void SetArrowQuote(Label detail, Label cost, ArrowRefillQuote quote)
        {
            detail.text = quote.IsValid ? $"+{quote.ArrowAmount:N0} ARROWS" : "NO CAPACITY AVAILABLE";
            cost.text = quote.IsValid ? $"{quote.WoodCost:N0} WOOD" : "UNAVAILABLE";
        }

        private static void RefreshArrowUpgrade(GameManager gm, ArrowUpgradeType type, Button button, Label detail, Label cost)
        {
            int level = gm.GetArrowUpgradeLevel(type);
            ResourceCost next = gm.GetArrowUpgradeCost(type);
            detail.text = $"LEVEL {level:N0}  ·  LASTS THIS RUN";
            cost.text = FormatCost(next);
            SetExplainedActionState(button, gm.CanBuyArrowUpgrade(type), HasResourceCost(next));
        }

        private void SetWorkerAllocationShare(EconomyFocusType resource, int targetPercent)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;

            bool changed = gm.SetWorkerAllocationSharePercent(resource, targetPercent);
            if (changed)
            {
                _onboardingLegacy?.NotifyWorkerTargetRatioChangedByPlayer(resource);
                MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep.WorkerShare);
            }
            ShowSecondaryToast(changed
                ? $"{ResourceName(resource)} SHARE  {gm.GetWorkerTargetRatioBps(resource) / 100f:0}%"
                : "WORKER REDISTRIBUTION BLOCKED");
            RefreshEconomy(gm);
        }

        private void BuyWorkerUpgrade(EconomyFocusType resource, WorkerBuildingUpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyWorkerBuildingUpgrade(resource, type);
            if (purchased)
            {
                ShowPrimaryToast($"{ResourceName(resource)} {type.ToString().ToUpperInvariant()} IMPROVED");
            }
            else
            {
                ResourceCost cost = gm != null
                    ? gm.GetWorkerBuildingUpgradeCost(resource, type)
                    : ResourceCost.Zero;
                ShowWarningToast(gm == null
                    ? "UPGRADE UNAVAILABLE  ·  GAME STATE NOT READY"
                    : GameplayActionFeedbackUtility.BuildResourcePurchaseFailure(
                        cost,
                        gm.Resources,
                        HasResourceCost(cost)
                            ? "UPGRADE FAILED  ·  TRY AGAIN"
                            : "UPGRADE COMPLETE  ·  MAXIMUM LEVEL REACHED"));
            }
            if (gm != null)
                RefreshEconomy(gm);
        }

        private void BuyHousing(int amount)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyBedCapacity(amount);
            if (purchased)
            {
                ShowPrimaryToast($"HOUSING EXPANDED  ·  +{amount:N0} BEDS");
                MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep.Housing);
            }
            else
                ShowWarningToast(gm == null
                    ? "HOUSING UNAVAILABLE  ·  GAME STATE NOT READY"
                    : GameplayActionFeedbackUtility.BuildResourcePurchaseFailure(
                        gm.GetBedCapacityPurchaseCost(amount),
                        gm.Resources,
                        "HOUSING PURCHASE FAILED  ·  TRY AGAIN"));
            if (gm != null)
                RefreshEconomy(gm);
        }

        private void BuyArcher(ArcherType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.BuyArcher(type);
            if (purchased)
            {
                ShowPrimaryToast($"{type.ToString().ToUpperInvariant()} ARCHER RECRUITED");
                if (type == ArcherType.Basic)
                {
                    MarkGuidedOnboardingStepFromSuccessfulAction(
                        GuidedOnboardingStep.BasicArcher);
                }
            }
            else if (gm == null)
            {
                ShowWarningToast("RECRUITMENT UNAVAILABLE  ·  GAME STATE NOT READY");
            }
            else
            {
                ArcherDefinitionSO definition = gm.GetArcherDefinition(type);
                int populationCost = definition != null ? definition.PopulationCost : 1;
                ResourceCost cost = definition != null
                    ? gm.GetArcherBuyCost(definition)
                    : gm.GetArcherBuyCost(type);
                ShowWarningToast(GameplayActionFeedbackUtility.BuildArcherRecruitmentFailure(
                    gm.IsArcherTypeUnlocked(type),
                    gm.GetRemainingArcherCapacity(),
                    gm.GetAvailablePopulation(),
                    populationCost,
                    cost,
                    gm.Resources));
            }
            if (gm != null)
                RefreshBarracks(gm);
        }

        private void RetrainArcher(ArcherType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.RetrainBasicArcher(type);
            if (purchased)
                ShowPrimaryToast($"ARCHER RETRAINED  ·  {type.ToString().ToUpperInvariant()}");
            else
                ShowWarningToast(gm == null
                    ? "RETRAINING UNAVAILABLE  ·  GAME STATE NOT READY"
                    : GameplayActionFeedbackUtility.BuildArcherRetrainingFailure(
                        gm.IsArcherTypeUnlocked(type),
                        gm.BasicArcherCount,
                        gm.GetArcherRetrainCost(type),
                        gm.Resources));
            if (gm != null)
                RefreshBarracks(gm);
        }

        private void BuyArrows(int packages)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyArrowRefill(packages);
            if (purchased)
            {
                ShowPrimaryToast("SUPPLY DELIVERY STARTED  ·  3S");
                MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep.ArrowRefill);
            }
            else
                ShowWarningToast(gm == null
                    ? "ARROW RESTOCK UNAVAILABLE  ·  GAME STATE NOT READY"
                    : GameplayActionFeedbackUtility.BuildArrowRefillFailure(
                        gm.IsArrowRefillDeliveryActive,
                        gm.ArrowRefillDeliveryRemainingSeconds,
                        gm.ArrowSupply.Current >= gm.GetArrowCapacity(),
                        gm.GetArrowRefillQuote(packages),
                        gm.Resources));
            if (gm != null)
                RefreshArrowSupply(gm);
        }

        private void BuyMaxArrows()
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyMaxArrowRefill();
            if (purchased)
            {
                ShowPrimaryToast("SUPPLY DELIVERY STARTED  ·  3S");
                MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep.ArrowRefill);
            }
            else if (gm == null)
            {
                ShowWarningToast("ARROW RESTOCK UNAVAILABLE  ·  GAME STATE NOT READY");
            }
            else
            {
                ArrowRefillQuote quote = gm.GetArrowBuyMaxQuote();
                if (!quote.IsValid)
                    quote = gm.GetArrowRefillQuote(1);
                ShowWarningToast(GameplayActionFeedbackUtility.BuildArrowRefillFailure(
                    gm.IsArrowRefillDeliveryActive,
                    gm.ArrowRefillDeliveryRemainingSeconds,
                    gm.ArrowSupply.Current >= gm.GetArrowCapacity(),
                    quote,
                    gm.Resources));
            }
            if (gm != null)
                RefreshArrowSupply(gm);
        }

        private void BuyArrowUpgrade(ArrowUpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyArrowUpgrade(type);
            if (purchased)
            {
                ShowPrimaryToast($"ARROW {type.ToString().ToUpperInvariant()} IMPROVED");
            }
            else
            {
                ResourceCost cost = gm != null ? gm.GetArrowUpgradeCost(type) : ResourceCost.Zero;
                ShowWarningToast(gm == null
                    ? "SUPPLY UPGRADE UNAVAILABLE  ·  GAME STATE NOT READY"
                    : GameplayActionFeedbackUtility.BuildResourcePurchaseFailure(
                        cost,
                        gm.Resources,
                        HasResourceCost(cost)
                            ? "SUPPLY UPGRADE FAILED  ·  TRY AGAIN"
                            : "SUPPLY UPGRADE COMPLETE  ·  MAXIMUM LEVEL REACHED"));
            }
            if (gm != null)
                RefreshArrowSupply(gm);
        }

        private static bool HasResourceCost(ResourceCost cost)
        {
            return cost.Wood > 0 || cost.Stone > 0 || cost.Iron > 0 || cost.Food > 0;
        }

        private static void SetExplainedActionState(
            Button button,
            bool canPerform,
            bool canExplainFailure = true)
        {
            if (button == null)
                return;

            button.SetEnabled(canPerform || canExplainFailure);
            button.EnableInClassList("is-action-unavailable", !canPerform && canExplainFailure);
        }
    }
}
