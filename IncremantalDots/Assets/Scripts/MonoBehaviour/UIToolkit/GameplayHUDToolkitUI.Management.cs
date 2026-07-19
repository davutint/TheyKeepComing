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
            Q<Button>("economyClose").clicked += CloseSurface;
            Q<Button>("barracksClose").clicked += CloseSurface;
            Q<Button>("arrowsClose").clicked += CloseSurface;

            _economyRows = Q<ScrollView>("economyRows");
            _economyIdle = Q<Label>("economyIdle");
            _economyWorkers = Q<Label>("economyWorkers");
            _economyHousing = Q<Label>("economyHousing");
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

                Label detail = new Label("0/m  ·  0 AVAILABLE") { name = "economyDetail" + resource };
                detail.AddToClassList("row-detail");
                row.Add(detail);

                Label assignmentLabel = new Label("ASSIGN WORKERS");
                assignmentLabel.AddToClassList("assignment-label");
                row.Add(assignmentLabel);

                VisualElement workerActions = new VisualElement();
                workerActions.AddToClassList("row-actions");
                workerActions.AddToClassList("worker-actions");
                Button minusTen = CreateCompactButton("-10", () => ChangeResourceWorkers(resource, -10));
                minusTen.name = "economyMinusTen" + resource;
                Button minusOne = CreateCompactButton("-1", () => ChangeResourceWorkers(resource, -1));
                minusOne.name = "economyMinusOne" + resource;
                Button plusOne = CreateCompactButton("+1", () => ChangeResourceWorkers(resource, 1));
                plusOne.name = "economyPlusOne" + resource;
                plusOne.AddToClassList("compact-action--primary");
                Button plusTen = CreateCompactButton("+10", () => ChangeResourceWorkers(resource, 10));
                plusTen.name = "economyPlusTen" + resource;
                plusTen.AddToClassList("compact-action--primary");
                Button fill = CreateCompactButton("FILL", () => FillResourceWorkers(resource));
                fill.name = "economyFill" + resource;
                fill.AddToClassList("compact-action--primary");
                workerActions.Add(minusTen);
                workerActions.Add(minusOne);
                workerActions.Add(plusOne);
                workerActions.Add(plusTen);
                workerActions.Add(fill);
                row.Add(workerActions);

                VisualElement targetActions = new VisualElement();
                targetActions.AddToClassList("allocation-target-row");
                Label targetLabel = new Label("NEW ARRIVALS 25%") { name = "economyTarget" + resource };
                targetLabel.AddToClassList("allocation-target-label");
                Button targetMinus = CreateCompactButton("AUTO -10%", () => AdjustWorkerTarget(resource, -10));
                targetMinus.name = "economyTargetMinus" + resource;
                Button targetPlus = CreateCompactButton("AUTO +10%", () => AdjustWorkerTarget(resource, 10));
                targetPlus.name = "economyTargetPlus" + resource;
                targetActions.Add(targetLabel);
                targetActions.Add(targetMinus);
                targetActions.Add(targetPlus);
                row.Add(targetActions);

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
                Label detail = new Label("0 DPS") { name = "archerDetail" + type };
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
            _economyWorkers.text = gm.Population.Workers.ToString("N0", CultureInfo.InvariantCulture);
            _economyHousing.text = $"{gm.Population.Total:N0} / {gm.GetTotalBedCapacity():N0}";
            ResourceCost costOne = gm.GetBedCapacityPurchaseCost(1);
            _housingCost.text = $"CURRENT CAPACITY {gm.GetTotalBedCapacity():N0}  ·  NEXT {FormatCost(costOne)}";
            _housingOne.SetEnabled(gm.CanBuyBedCapacity(1));
            _housingTen.SetEnabled(gm.CanBuyBedCapacity(10));
            _housingHundred.SetEnabled(gm.CanBuyBedCapacity(100));

            for (int i = 0; i < EconomyResources.Length; i++)
            {
                EconomyFocusType resource = EconomyResources[i];
                int workers = gm.GetResourceWorkers(resource);
                int target = Mathf.RoundToInt(gm.GetWorkerTargetRatioBps(resource) / 100f);
                int capacityValue = gm.GetMaxWorkersForResource(resource);
                float rate = gm.GetWorkerProductionRate(resource);
                Q<Label>("economyStat" + resource).text = $"{workers:N0} / {capacityValue:N0}";
                Q<Label>("economyDetail" + resource).text = $"+{rate:0.#}/m  ·  {gm.GetIdlePopulation():N0} AVAILABLE";
                Q<Label>("economyTarget" + resource).text = $"NEW ARRIVALS  {target}%";

                bool canAssign = gm.CanAssignResourceWorker(resource);
                Q<Button>("economyMinusTen" + resource).SetEnabled(workers > 0);
                Q<Button>("economyMinusOne" + resource).SetEnabled(workers > 0);
                Q<Button>("economyPlusOne" + resource).SetEnabled(canAssign);
                Q<Button>("economyPlusTen" + resource).SetEnabled(canAssign);
                Q<Button>("economyFill" + resource).SetEnabled(canAssign);
                Q<Button>("economyTargetMinus" + resource).SetEnabled(target > 0);
                Q<Button>("economyTargetPlus" + resource).SetEnabled(target < 100);

                int capacityLevel = gm.GetWorkerBuildingUpgradeLevel(resource, WorkerBuildingUpgradeType.Capacity);
                int efficiencyLevel = gm.GetWorkerBuildingUpgradeLevel(resource, WorkerBuildingUpgradeType.Efficiency);
                ResourceCost capacityCost = gm.GetWorkerBuildingUpgradeCost(resource, WorkerBuildingUpgradeType.Capacity);
                ResourceCost efficiencyCost = gm.GetWorkerBuildingUpgradeCost(resource, WorkerBuildingUpgradeType.Efficiency);
                Button capacity = Q<Button>("economyCapacity" + resource);
                Button efficiency = Q<Button>("economyEfficiency" + resource);
                capacity.text = $"CAPACITY L{capacityLevel}  ·  {FormatCost(capacityCost)}";
                efficiency.text = $"EFFICIENCY L{efficiencyLevel}  ·  {FormatCost(efficiencyCost)}";
                capacity.SetEnabled(gm.CanBuyWorkerBuildingUpgrade(resource, WorkerBuildingUpgradeType.Capacity));
                efficiency.SetEnabled(gm.CanBuyWorkerBuildingUpgrade(resource, WorkerBuildingUpgradeType.Efficiency));
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
                    ? $"{gm.GetArcherTypeDps(type):0.#} DPS  ·  {FormatCost(gm.GetArcherBuyCost(definition))}"
                    : "REQUIRES DOCTRINE RESEARCH";
                Button buy = Q<Button>("archerBuy" + type);
                buy.text = unlocked ? $"RECRUIT  ·  {FormatCost(gm.GetArcherBuyCost(definition))}" : "LOCKED";
                buy.SetEnabled(gm.CanBuyArcher(definition));
                Button retrain = Q<Button>("archerRetrain" + type);
                if (retrain != null)
                {
                    retrain.text = $"RETRAIN  ·  {FormatCost(gm.GetArcherRetrainCost(type))}";
                    retrain.SetEnabled(gm.CanRetrainBasicArcher(type));
                }
            }
        }

        private void RefreshArrowSupply(GameManager gm)
        {
            if (_supplyHeroValue == null)
                return;

            int capacity = gm.GetArrowCapacity();
            int current = gm.ArrowSupply.Current;
            float ratio = capacity > 0 ? current / (float)capacity : 0f;
            _supplyHeroValue.text = $"{current:N0} / {capacity:N0}";
            _supplyHeroState.text = ratio <= 0.25f ? "LOW SUPPLY" : ratio >= 0.995f ? "RESERVE FULL" : "SUPPLY READY";
            _supplyHeroState.EnableInClassList("is-negative", ratio <= 0.25f);
            _supplyHeroProgress.style.width = Length.Percent(ratio * 100f);
            _supplyHeroProgress.style.backgroundColor = ratio <= 0.25f
                ? new Color(0.84f, 0.36f, 0.25f, 1f)
                : new Color(0.55f, 0.70f, 0.48f, 1f);

            ArrowRefillQuote package = gm.GetArrowRefillQuote(1);
            ArrowRefillQuote large = gm.GetArrowRefillQuote(5);
            ArrowRefillQuote max = gm.GetArrowBuyMaxQuote();
            SetArrowQuote(_arrowPackageDetail, _arrowPackageCost, package);
            SetArrowQuote(_arrowLargeDetail, _arrowLargeCost, large);
            SetArrowQuote(_arrowMaxDetail, _arrowMaxCost, max);
            _arrowPackageButton.SetEnabled(gm.CanBuyArrowRefill(1));
            _arrowLargeButton.SetEnabled(gm.CanBuyArrowRefill(5));
            _arrowMaxButton.SetEnabled(gm.CanBuyMaxArrowRefill());

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
            detail.text = $"LEVEL {level:N0}  ·  PERMANENT RUN UPGRADE";
            cost.text = FormatCost(next);
            button.SetEnabled(gm.CanBuyArrowUpgrade(type));
        }

        private void AdjustWorkerTarget(EconomyFocusType resource, int delta)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;
            bool changed = gm.AdjustWorkerTargetRatioPercent(resource, delta);
            ShowSecondaryToast(changed
                ? $"{ResourceName(resource)} TARGET {gm.GetWorkerTargetRatioBps(resource) / 100f:0}%"
                : "TARGET CHANGE BLOCKED");
            RefreshEconomy(gm);
        }

        private void ChangeResourceWorkers(EconomyFocusType resource, int delta)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;

            int previous = gm.GetResourceWorkers(resource);
            bool changed = gm.SetResourceWorkers(resource, previous + delta);
            int current = gm.GetResourceWorkers(resource);
            ShowSecondaryToast(changed && current != previous
                ? $"{ResourceName(resource)} WORKERS  {current:N0}"
                : "WORKER ASSIGNMENT BLOCKED");
            RefreshEconomy(gm);
        }

        private void FillResourceWorkers(EconomyFocusType resource)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;

            int previous = gm.GetResourceWorkers(resource);
            int requested = previous + gm.GetIdlePopulation();
            bool changed = gm.SetResourceWorkers(resource, requested);
            int current = gm.GetResourceWorkers(resource);
            ShowSecondaryToast(changed && current != previous
                ? $"{ResourceName(resource)} FILLED  ·  {current:N0} WORKERS"
                : "NO AVAILABLE WORKER CAPACITY");
            RefreshEconomy(gm);
        }

        private void BuyWorkerUpgrade(EconomyFocusType resource, WorkerBuildingUpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyWorkerBuildingUpgrade(resource, type);
            ShowPrimaryToast(purchased
                ? $"{ResourceName(resource)} {type.ToString().ToUpperInvariant()} IMPROVED"
                : "UPGRADE BLOCKED  ·  CHECK COST");
            if (gm != null)
                RefreshEconomy(gm);
        }

        private void BuyHousing(int amount)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyBedCapacity(amount);
            ShowPrimaryToast(purchased ? $"HOUSING EXPANDED  ·  +{amount:N0}" : "HOUSING PURCHASE BLOCKED");
            if (gm != null)
                RefreshEconomy(gm);
        }

        private void BuyArcher(ArcherType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.BuyArcher(type);
            ShowPrimaryToast(purchased ? $"{type.ToString().ToUpperInvariant()} ARCHER RECRUITED" : "RECRUITMENT BLOCKED");
            if (gm != null)
                RefreshBarracks(gm);
        }

        private void RetrainArcher(ArcherType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.RetrainBasicArcher(type);
            ShowPrimaryToast(purchased ? $"ARCHER RETRAINED  ·  {type.ToString().ToUpperInvariant()}" : "RETRAINING BLOCKED");
            if (gm != null)
                RefreshBarracks(gm);
        }

        private void BuyArrows(int packages)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyArrowRefill(packages);
            ShowPrimaryToast(purchased ? "ARROW RESERVE RESTOCKED" : "RESTOCK BLOCKED  ·  CHECK WOOD AND CAPACITY");
            if (gm != null)
                RefreshArrowSupply(gm);
        }

        private void BuyMaxArrows()
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyMaxArrowRefill();
            ShowPrimaryToast(purchased ? "ARROW RESERVE FILLED" : "FILL RESERVES BLOCKED");
            if (gm != null)
                RefreshArrowSupply(gm);
        }

        private void BuyArrowUpgrade(ArrowUpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyArrowUpgrade(type);
            ShowPrimaryToast(purchased ? $"ARROW {type.ToString().ToUpperInvariant()} IMPROVED" : "SUPPLY UPGRADE BLOCKED");
            if (gm != null)
                RefreshArrowSupply(gm);
        }
    }
}
