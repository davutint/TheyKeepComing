using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class CastleEconomyUI : MonoBehaviour
    {
        public static CastleEconomyUI Instance { get; private set; }

        [Header("Panel")]
        public bool PlayerFacingPanelEnabled;
        public GameObject CastleEconomyPanel;
        public Button CloseCastleEconomyButton;
        public Button ConfirmCastleEconomyButton;
        public GameObject CastleTapHint;
        public TMP_Text CastleTapHintText;
        public GameObject CastleTapHintPulse;

        [Header("Population")]
        public TMP_Text PopulationTotalText;
        public TMP_Text PopulationIdleText;
        public TMP_Text PopulationArchersText;
        public TMP_Text PopulationGrowthText;
        public TMP_Text WorkerBudgetText;

        [Header("Workers")]
        public Slider WoodWorkerSlider;
        public Slider StoneWorkerSlider;
        public Slider IronWorkerSlider;
        public Slider FoodWorkerSlider;
        public Button WoodAssignButton;
        public Button StoneAssignButton;
        public Button IronAssignButton;
        public Button FoodAssignButton;
        public TMP_Text WoodWorkerText;
        public TMP_Text StoneWorkerText;
        public TMP_Text IronWorkerText;
        public TMP_Text FoodWorkerText;
        public TMP_Text WoodRateText;
        public TMP_Text StoneRateText;
        public TMP_Text IronRateText;
        public TMP_Text FoodRateText;

        [Header("Projected Gain")]
        public TMP_Text ProjectedIncomeText;
        public TMP_Text ProjectedWoodText;
        public TMP_Text ProjectedStoneText;
        public TMP_Text ProjectedIronText;
        public TMP_Text ProjectedFoodText;

        [Header("Repair")]
        public Button CastleRepairButton;
        public TMP_Text CastleRepairStatusText;
        public TMP_Text CastleRepairCostText;

        [Header("Event")]
        public GameObject EconomyEventPanel;
        public TMP_Text EconomyEventTitleText;
        public TMP_Text EconomyEventDescriptionText;
        public Button EconomyEventChoiceAButton;
        public Button EconomyEventChoiceBButton;
        public TMP_Text EconomyEventChoiceAText;
        public TMP_Text EconomyEventChoiceBText;
        public GameObject EconomyEventBadge;
        public TMP_Text EconomyEventBadgeText;
        public GameObject EconomyEventGlow;

        private bool _suppressSliderCallbacks;
        private float _nextRefreshTime;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            Instance = this;
            BindControls();
            if (CastleEconomyPanel != null)
                CastleEconomyPanel.SetActive(false);
            HideEntryFeedback();
        }

        private void OnDisable()
        {
            UnbindControls();
            GameManager.Instance?.CloseCastleEconomy();
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            RefreshEntryFeedback();

            if (!PlayerFacingPanelEnabled)
            {
                if (CastleEconomyPanel != null && CastleEconomyPanel.activeSelf)
                    ClosePanel();
                return;
            }

            if (CastleEconomyPanel == null || !CastleEconomyPanel.activeSelf)
                return;

            var gm = GameManager.Instance;
            if (gm == null || !gm.CanOpenCastleEconomy())
            {
                ClosePanel();
                return;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 0.15f;
                Refresh();
            }
        }

        public bool OpenFromCastle()
        {
            var gm = GameManager.Instance;
            if (!PlayerFacingPanelEnabled || gm == null || CastleEconomyPanel == null || !gm.OpenCastleEconomy())
                return false;

            CastleEconomyPanel.SetActive(true);
            Refresh();
            return true;
        }

        public void ClosePanel()
        {
            GameManager.Instance?.CloseCastleEconomy();
            if (CastleEconomyPanel != null)
                CastleEconomyPanel.SetActive(false);
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            var population = gm.Population;
            int idle = gm.GetIdlePopulation();
            int totalWorkers = gm.GetResourceWorkers(EconomyFocusType.Balanced);
            int workerBudget = Mathf.Max(0, population.Total - population.Archers);

            SetText(PopulationTotalText, $"Total {population.Total}");
            SetText(PopulationIdleText, $"Unassigned {idle}");
            SetText(PopulationArchersText, $"Archers {population.Archers}");
            SetText(PopulationGrowthText, $"+{GetPopulationGrowth(gm)} / wave");
            SetText(WorkerBudgetText, $"Workers {totalWorkers} / {workerBudget}");

            RefreshResourceRow(EconomyFocusType.Wood, WoodWorkerSlider, WoodWorkerText, WoodRateText, "Wood", gm);
            RefreshResourceRow(EconomyFocusType.Stone, StoneWorkerSlider, StoneWorkerText, StoneRateText, "Stone", gm);
            RefreshResourceRow(EconomyFocusType.Iron, IronWorkerSlider, IronWorkerText, IronRateText, "Iron", gm);
            RefreshResourceRow(EconomyFocusType.Food, FoodWorkerSlider, FoodWorkerText, FoodRateText, "Food", gm);
            RefreshAssignButton(EconomyFocusType.Wood, WoodAssignButton, "Wood", gm);
            RefreshAssignButton(EconomyFocusType.Stone, StoneAssignButton, "Stone", gm);
            RefreshAssignButton(EconomyFocusType.Iron, IronAssignButton, "Iron", gm);
            RefreshAssignButton(EconomyFocusType.Food, FoodAssignButton, "Food", gm);

            if (totalWorkers + idle + population.Archers > population.Total && PopulationIdleText != null)
                PopulationIdleText.text = "Unassigned 0";

            RefreshProjectedGain(gm);
            RefreshRepair(gm);
            RefreshEvent(gm);
        }

        private static int GetPopulationGrowth(GameManager gm)
        {
            return gm.TryGetMobileCombatConfig(out var config)
                ? Mathf.Max(0, config.PopulationGrowthPerDayPrep)
                : 0;
        }

        private void RefreshResourceRow(EconomyFocusType resource, Slider slider, TMP_Text workerText,
            TMP_Text rateText, string label, GameManager gm)
        {
            int value = gm.GetResourceWorkers(resource);
            int max = Mathf.Max(value, gm.GetMaxWorkersForResource(resource));

            _suppressSliderCallbacks = true;
            if (slider != null)
            {
                slider.wholeNumbers = true;
                slider.minValue = 0;
                slider.maxValue = Mathf.Max(1, max);
                slider.SetValueWithoutNotify(value);
            }
            _suppressSliderCallbacks = false;

            SetText(workerText, $"{label} {value}");
            SetText(rateText, $"+{gm.GetWorkerProductionRate(resource):0}/min");
        }

        private static void RefreshAssignButton(EconomyFocusType resource, Button button, string label, GameManager gm)
        {
            if (button == null)
                return;

            button.interactable = gm.CanAssignResourceWorker(resource);
            SetButtonText(button, gm.GetIdlePopulation() > 0 || gm.IsFreeEconomyTestMode
                ? $"{label} +Worker"
                : "AUTO ASSIGNED");
        }

        private void RefreshEvent(GameManager gm)
        {
            bool hasEvent = gm.HasPendingEconomyEvent();
            if (EconomyEventPanel != null)
                EconomyEventPanel.SetActive(hasEvent);
            RefreshEventBadge(gm, hasEvent);

            if (!hasEvent)
                return;

            SetText(EconomyEventTitleText, gm.GetEconomyEventTitle());
            SetText(EconomyEventDescriptionText, gm.GetEconomyEventDescription());
            SetText(EconomyEventChoiceAText, gm.GetEconomyEventChoiceText(0));
            SetText(EconomyEventChoiceBText, gm.GetEconomyEventChoiceText(1));

            if (EconomyEventChoiceAButton != null)
                EconomyEventChoiceAButton.interactable = true;
            if (EconomyEventChoiceBButton != null)
                EconomyEventChoiceBButton.interactable = true;
        }

        private void RefreshEntryFeedback()
        {
            if (!PlayerFacingPanelEnabled)
            {
                HideEntryFeedback();
                return;
            }

            var gm = GameManager.Instance;
            bool panelOpen = CastleEconomyPanel != null && CastleEconomyPanel.activeSelf;
            bool canOpen = gm != null
                && CastleEconomyPanel != null
                && gm.IsMobilePopulationEconomyEnabled()
                && gm.CanOpenCastleEconomy()
                && !panelOpen;
            bool hasEvent = gm != null && gm.HasPendingEconomyEvent();

            if (CastleTapHint != null)
                CastleTapHint.SetActive(canOpen);
            if (CastleTapHintPulse != null)
                CastleTapHintPulse.SetActive(canOpen);
            if (CastleTapHintText != null)
                CastleTapHintText.text = hasEvent ? "COUNCIL EVENT" : "TAP CASTLE";

            RefreshEventBadge(gm, hasEvent && gm != null && gm.IsMobilePopulationEconomyEnabled() && gm.CanOpenCastleEconomy());
        }

        private void HideEntryFeedback()
        {
            if (CastleTapHint != null)
                CastleTapHint.SetActive(false);
            if (CastleTapHintPulse != null)
                CastleTapHintPulse.SetActive(false);
            if (EconomyEventBadge != null)
                EconomyEventBadge.SetActive(false);
            if (EconomyEventGlow != null)
                EconomyEventGlow.SetActive(false);
        }

        private void RefreshEventBadge(GameManager gm, bool show)
        {
            if (EconomyEventBadge != null)
                EconomyEventBadge.SetActive(show);
            if (EconomyEventGlow != null)
                EconomyEventGlow.SetActive(show);
            if (EconomyEventBadgeText != null)
                EconomyEventBadgeText.text = show && gm != null ? "Council Event" : string.Empty;
        }

        private void RefreshProjectedGain(GameManager gm)
        {
            float seconds = Mathf.Max(0f, gm.WaveState.PrepTimer);
            SetText(ProjectedIncomeText, $"Projected Gain ({Mathf.CeilToInt(seconds)}s)");
            SetText(ProjectedWoodText, FormatProjectedGain(GetProjectedGain(gm.GetWorkerProductionRate(EconomyFocusType.Wood), gm.ResourceConsumption.WoodPerMin, seconds), "W"));
            SetText(ProjectedStoneText, FormatProjectedGain(GetProjectedGain(gm.GetWorkerProductionRate(EconomyFocusType.Stone), gm.ResourceConsumption.StonePerMin, seconds), "S"));
            SetText(ProjectedIronText, FormatProjectedGain(GetProjectedGain(gm.GetWorkerProductionRate(EconomyFocusType.Iron), gm.ResourceConsumption.IronPerMin, seconds), "I"));
            SetText(ProjectedFoodText, FormatProjectedGain(GetProjectedGain(gm.GetWorkerProductionRate(EconomyFocusType.Food), gm.ResourceConsumption.FoodPerMin, seconds), "F"));
        }

        private void RefreshRepair(GameManager gm)
        {
            if (CastleRepairButton != null)
            {
                CastleRepairButton.interactable = gm.CanRepairDefenseFull();
                SetButtonText(CastleRepairButton, "Repair Defense");
            }

            if (CastleRepairCostText != null)
                CastleRepairCostText.text = gm.GetRepairCost().ToDisplayString();
            if (CastleRepairStatusText != null)
                CastleRepairStatusText.text = GetRepairStatus(gm);
        }

        private static float GetProjectedGain(float productionPerMin, float consumptionPerMin, float seconds)
        {
            return (productionPerMin - consumptionPerMin) * seconds / 60f;
        }

        private static string FormatProjectedGain(float value, string suffix)
        {
            string sign = value >= 0f ? "+" : "-";
            int rounded = Mathf.FloorToInt(Mathf.Abs(value));
            return $"{sign}{rounded}{suffix}";
        }

        private static string GetRepairStatus(GameManager gm)
        {
            if (!gm.IsRepairPhaseAvailable())
                return "Day / Dusk only";

            return gm.GetDefensePercent() >= 0.995f ? "Defense full" : "Restore defense";
        }

        private void BindControls()
        {
            UnbindControls();
            CloseCastleEconomyButton?.onClick.AddListener(ClosePanel);
            ConfirmCastleEconomyButton?.onClick.AddListener(ClosePanel);
            CastleRepairButton?.onClick.AddListener(HandleRepairClicked);
            WoodAssignButton?.onClick.AddListener(HandleWoodAssignClicked);
            StoneAssignButton?.onClick.AddListener(HandleStoneAssignClicked);
            IronAssignButton?.onClick.AddListener(HandleIronAssignClicked);
            FoodAssignButton?.onClick.AddListener(HandleFoodAssignClicked);
            WoodWorkerSlider?.onValueChanged.AddListener(HandleWoodChanged);
            StoneWorkerSlider?.onValueChanged.AddListener(HandleStoneChanged);
            IronWorkerSlider?.onValueChanged.AddListener(HandleIronChanged);
            FoodWorkerSlider?.onValueChanged.AddListener(HandleFoodChanged);
            EconomyEventChoiceAButton?.onClick.AddListener(HandleEventChoiceA);
            EconomyEventChoiceBButton?.onClick.AddListener(HandleEventChoiceB);
        }

        private void UnbindControls()
        {
            CloseCastleEconomyButton?.onClick.RemoveListener(ClosePanel);
            ConfirmCastleEconomyButton?.onClick.RemoveListener(ClosePanel);
            CastleRepairButton?.onClick.RemoveListener(HandleRepairClicked);
            WoodAssignButton?.onClick.RemoveListener(HandleWoodAssignClicked);
            StoneAssignButton?.onClick.RemoveListener(HandleStoneAssignClicked);
            IronAssignButton?.onClick.RemoveListener(HandleIronAssignClicked);
            FoodAssignButton?.onClick.RemoveListener(HandleFoodAssignClicked);
            WoodWorkerSlider?.onValueChanged.RemoveListener(HandleWoodChanged);
            StoneWorkerSlider?.onValueChanged.RemoveListener(HandleStoneChanged);
            IronWorkerSlider?.onValueChanged.RemoveListener(HandleIronChanged);
            FoodWorkerSlider?.onValueChanged.RemoveListener(HandleFoodChanged);
            EconomyEventChoiceAButton?.onClick.RemoveListener(HandleEventChoiceA);
            EconomyEventChoiceBButton?.onClick.RemoveListener(HandleEventChoiceB);
        }

        private void HandleWoodChanged(float value) => SetWorkers(EconomyFocusType.Wood, value);
        private void HandleStoneChanged(float value) => SetWorkers(EconomyFocusType.Stone, value);
        private void HandleIronChanged(float value) => SetWorkers(EconomyFocusType.Iron, value);
        private void HandleFoodChanged(float value) => SetWorkers(EconomyFocusType.Food, value);
        private void HandleWoodAssignClicked() => AssignWorker(EconomyFocusType.Wood);
        private void HandleStoneAssignClicked() => AssignWorker(EconomyFocusType.Stone);
        private void HandleIronAssignClicked() => AssignWorker(EconomyFocusType.Iron);
        private void HandleFoodAssignClicked() => AssignWorker(EconomyFocusType.Food);

        private void SetWorkers(EconomyFocusType resource, float value)
        {
            if (_suppressSliderCallbacks)
                return;

            GameManager.Instance?.SetResourceWorkers(resource, Mathf.RoundToInt(value));
            Refresh();
        }

        private void AssignWorker(EconomyFocusType resource)
        {
            GameManager.Instance?.AssignResourceWorker(resource);
            Refresh();
        }

        private void HandleEventChoiceA()
        {
            GameManager.Instance?.ChooseEconomyEvent(0);
            Refresh();
        }

        private void HandleEventChoiceB()
        {
            GameManager.Instance?.ChooseEconomyEvent(1);
            Refresh();
        }

        private void HandleRepairClicked()
        {
            GameManager.Instance?.RepairDefenseFull();
            Refresh();
        }

        private static void SetButtonText(Button button, string value)
        {
            var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (text != null)
                text.text = value;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
