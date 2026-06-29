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
        public TMP_Text WoodWorkerStatusText;

        [Header("Stone")]
        public TMP_Text StoneWorkerCountText;
        public TMP_Text StoneWorkerRateText;
        public Button StoneWorkerAddButton;
        public TMP_Text StoneWorkerStatusText;

        [Header("Iron")]
        public TMP_Text IronWorkerCountText;
        public TMP_Text IronWorkerRateText;
        public Button IronWorkerAddButton;
        public TMP_Text IronWorkerStatusText;

        [Header("Food")]
        public TMP_Text FoodWorkerCountText;
        public TMP_Text FoodWorkerRateText;
        public Button FoodWorkerAddButton;
        public TMP_Text FoodWorkerStatusText;

        private bool _isOpen;
        private float _nextRefreshTime;

        private void OnEnable()
        {
            BindControls();
            SetOpen(false);
            Refresh();
        }

        private void OnDisable()
        {
            UnbindControls();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.15f;
            Refresh();
        }

        private void BindControls()
        {
            UnbindControls();
            WorkerDrawerToggleButton?.onClick.AddListener(Toggle);
            WoodWorkerAddButton?.onClick.AddListener(HandleWoodClicked);
            StoneWorkerAddButton?.onClick.AddListener(HandleStoneClicked);
            IronWorkerAddButton?.onClick.AddListener(HandleIronClicked);
            FoodWorkerAddButton?.onClick.AddListener(HandleFoodClicked);
        }

        private void UnbindControls()
        {
            WorkerDrawerToggleButton?.onClick.RemoveListener(Toggle);
            WoodWorkerAddButton?.onClick.RemoveListener(HandleWoodClicked);
            StoneWorkerAddButton?.onClick.RemoveListener(HandleStoneClicked);
            IronWorkerAddButton?.onClick.RemoveListener(HandleIronClicked);
            FoodWorkerAddButton?.onClick.RemoveListener(HandleFoodClicked);
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
                return;

            var population = gm.Population;
            int totalWorkers = gm.GetResourceWorkers(EconomyFocusType.Balanced);
            int idle = gm.GetIdlePopulation();

            SetText(WorkerDrawerTitleText, "WORKERS");
            SetText(WorkerIdlePopulationText, $"IDLE {idle}");
            SetText(WorkerTotalText, $"WORKERS {totalWorkers}/{gm.GetAvailablePopulation()}");
            SetText(WorkerArcherPopulationText, $"ARCHERS {population.Archers}");

            RefreshRow(gm, EconomyFocusType.Wood, "WOOD", WoodWorkerCountText, WoodWorkerRateText,
                WoodWorkerAddButton, WoodWorkerStatusText);
            RefreshRow(gm, EconomyFocusType.Stone, "STONE", StoneWorkerCountText, StoneWorkerRateText,
                StoneWorkerAddButton, StoneWorkerStatusText);
            RefreshRow(gm, EconomyFocusType.Iron, "IRON", IronWorkerCountText, IronWorkerRateText,
                IronWorkerAddButton, IronWorkerStatusText);
            RefreshRow(gm, EconomyFocusType.Food, "FOOD", FoodWorkerCountText, FoodWorkerRateText,
                FoodWorkerAddButton, FoodWorkerStatusText);
        }

        private static void RefreshRow(GameManager gm, EconomyFocusType resource, string label,
            TMP_Text countText, TMP_Text rateText, Button addButton, TMP_Text statusText)
        {
            int count = gm.GetResourceWorkers(resource);
            int cap = gm.GetMaxWorkersForResource(resource);
            float rate = gm.GetWorkerProductionRate(resource);
            bool canAssign = gm.CanAssignResourceWorker(resource);
            bool atCap = cap > 0 && count >= cap;
            bool hasIdle = gm.GetIdlePopulation() > 0 || gm.IsFreeEconomyTestMode;
            string status = atCap ? "CAP FULL" : hasIdle ? "READY" : "NEED POP";

            SetText(countText, cap > 0 ? $"{label} {count}/{cap}" : $"{label} x{count}");
            SetText(rateText, $"+{rate:0}/min");
            SetText(statusText, status);

            if (addButton != null)
            {
                addButton.interactable = canAssign;
                SetButtonText(addButton, atCap ? "CAP FULL" : hasIdle ? "+ WORKER" : "NEED POP");
            }
        }

        private void HandleWoodClicked() => Assign(EconomyFocusType.Wood);
        private void HandleStoneClicked() => Assign(EconomyFocusType.Stone);
        private void HandleIronClicked() => Assign(EconomyFocusType.Iron);
        private void HandleFoodClicked() => Assign(EconomyFocusType.Food);

        private void Assign(EconomyFocusType resource)
        {
            GameManager.Instance?.AssignResourceWorker(resource);
            Refresh();
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
