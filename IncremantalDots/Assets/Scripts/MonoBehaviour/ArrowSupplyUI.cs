using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Alt-sag ARROW SUPPLY management dock girisinden acilan finite ammo paneli.
    /// Refill satin alimi Wood'u aninda harcar; oklar uc saniye sonunda stoga birlikte gelir.
    /// </summary>
    public sealed class ArrowSupplyUI : MonoBehaviour
    {
        public event System.Action ArrowRefillPurchasedByPlayer;

        public GameObject AmmoPanel;
        public Button ToggleButton;
        public TMP_Text StockText;
        public TMP_Text EfficiencyText;
        public Button PackageButton;
        public Button LargePackageButton;
        public Button BuyMaxButton;
        public Button CapacityUpgradeButton;
        public Button EfficiencyUpgradeButton;
        public bool StartOpen;

        private float _nextRefreshTime;
        private ManagementDrawerCoordinatorUI _drawerCoordinator;

        public bool IsOpen => AmmoPanel != null && AmmoPanel.activeSelf;

        private void OnEnable()
        {
            BindButtons();
            SetOpen(StartOpen);
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + 0.15f;
            Refresh();
        }

        public void Toggle()
        {
            SetOpen(AmmoPanel == null || !AmmoPanel.activeSelf);
        }

        public void SetOpen(bool open)
        {
            ManagementDrawerCoordinatorUI coordinator = ResolveDrawerCoordinator();
            if (open)
                coordinator?.Claim(ManagementDrawerId.ArrowSupply);
            else
                coordinator?.Release(ManagementDrawerId.ArrowSupply);

            if (AmmoPanel != null)
                AmmoPanel.SetActive(open);
        }

        private ManagementDrawerCoordinatorUI ResolveDrawerCoordinator()
        {
            _drawerCoordinator ??= GetComponent<ManagementDrawerCoordinatorUI>();
            return _drawerCoordinator;
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                SetInteractable(false);
                return;
            }

            int capacity = gm.GetArrowCapacity();
            int current = Mathf.Clamp(gm.ArrowSupply.Current, 0, capacity);
            int rate = gm.GetArrowsPerWood();
            SetText(
                StockText,
                gm.IsArrowRefillDeliveryActive
                    ? $"ARROWS  {current:N0} / {capacity:N0}  ·  DELIVERING {gm.ArrowRefillDeliveryRemainingSeconds:0.0}S"
                    : $"ARROWS  {current:N0} / {capacity:N0}");
            SetText(EfficiencyText, $"{rate:N0} / WOOD");

            ArrowRefillQuote package = gm.GetArrowRefillQuote(1);
            ArrowRefillQuote largePackage = gm.GetArrowRefillQuote(5);
            ArrowRefillQuote buyMax = gm.GetArrowBuyMaxQuote();
            string unavailable = gm.IsArrowRefillDeliveryActive
                ? "DELIVERING"
                : current >= capacity
                    ? "FULL"
                    : "WAIT";
            RefreshRefillButton(PackageButton, package, "BUY", gm.CanBuyArrowRefill(1),
                unavailable, gm);
            RefreshRefillButton(LargePackageButton, largePackage, "BUY x5",
                gm.CanBuyArrowRefill(5), unavailable, gm);
            RefreshRefillButton(BuyMaxButton, buyMax, "BUY MAX",
                gm.CanBuyMaxArrowRefill(), current >= capacity ? "FULL" : "NEED WOOD", gm);

            RefreshUpgradeButton(gm, CapacityUpgradeButton, ArrowUpgradeType.Capacity);
            RefreshUpgradeButton(gm, EfficiencyUpgradeButton, ArrowUpgradeType.Efficiency);
        }

        private static void RefreshRefillButton(Button button, ArrowRefillQuote quote,
            string prefix, bool interactable, string unavailableLabel, GameManager gm)
        {
            if (button == null)
                return;

            button.interactable = interactable;
            if (!quote.IsValid)
            {
                SetButtonLabel(button, unavailableLabel);
                return;
            }

            string price = gm.IsFreeEconomyTestMode ? "FREE" : $"{quote.WoodCost:N0}W";
            SetButtonLabel(button, $"{prefix} +{quote.ArrowAmount:N0}\n{price}");
        }

        private static void RefreshUpgradeButton(GameManager gm, Button button, ArrowUpgradeType type)
        {
            if (button == null)
                return;

            ResourceCost cost = gm.GetArrowUpgradeCost(type);
            int level = gm.GetArrowUpgradeLevel(type);
            button.interactable = gm.CanBuyArrowUpgrade(type);
            if (cost.Wood <= 0 && cost.Iron <= 0)
            {
                SetButtonLabel(button, "LIMIT");
                return;
            }

            string title;
            if (type == ArrowUpgradeType.Capacity)
            {
                int gain = gm.GetEconomyPriceTuning().ArrowCapacityPerLevel;
                title = $"CAP +{gain:N0}  L{level}";
            }
            else
            {
                int rate = gm.GetArrowsPerWood();
                int gain = gm.GetEconomyPriceTuning().ArrowArrowsPerWoodPerEfficiencyLevel;
                title = $"EFF {rate:N0}>{rate + gain:N0}/W  L{level}";
            }

            string price = gm.IsFreeEconomyTestMode
                ? "FREE"
                : $"{cost.Wood:N0}W + {cost.Iron:N0}I";
            SetButtonLabel(button, $"{title}\n{price}");
        }

        private void BindButtons()
        {
            UnbindButtons();
            ToggleButton?.onClick.AddListener(Toggle);
            PackageButton?.onClick.AddListener(BuyPackage);
            LargePackageButton?.onClick.AddListener(BuyLargePackage);
            BuyMaxButton?.onClick.AddListener(BuyMax);
            CapacityUpgradeButton?.onClick.AddListener(BuyCapacity);
            EfficiencyUpgradeButton?.onClick.AddListener(BuyEfficiency);
        }

        private void UnbindButtons()
        {
            ToggleButton?.onClick.RemoveListener(Toggle);
            PackageButton?.onClick.RemoveListener(BuyPackage);
            LargePackageButton?.onClick.RemoveListener(BuyLargePackage);
            BuyMaxButton?.onClick.RemoveListener(BuyMax);
            CapacityUpgradeButton?.onClick.RemoveListener(BuyCapacity);
            EfficiencyUpgradeButton?.onClick.RemoveListener(BuyEfficiency);
        }

        private void BuyPackage()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.TryBuyArrowRefill(1))
                ArrowRefillPurchasedByPlayer?.Invoke();
            Refresh();
        }

        private void BuyLargePackage()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.TryBuyArrowRefill(5))
                ArrowRefillPurchasedByPlayer?.Invoke();
            Refresh();
        }

        private void BuyMax()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.TryBuyMaxArrowRefill())
                ArrowRefillPurchasedByPlayer?.Invoke();
            Refresh();
        }

        private void BuyCapacity()
        {
            GameManager.Instance?.TryBuyArrowUpgrade(ArrowUpgradeType.Capacity);
            Refresh();
        }

        private void BuyEfficiency()
        {
            GameManager.Instance?.TryBuyArrowUpgrade(ArrowUpgradeType.Efficiency);
            Refresh();
        }

        private void SetInteractable(bool interactable)
        {
            if (PackageButton != null) PackageButton.interactable = interactable;
            if (LargePackageButton != null) LargePackageButton.interactable = interactable;
            if (BuyMaxButton != null) BuyMaxButton.interactable = interactable;
            if (CapacityUpgradeButton != null) CapacityUpgradeButton.interactable = interactable;
            if (EfficiencyUpgradeButton != null) EfficiencyUpgradeButton.interactable = interactable;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
        }
    }
}
