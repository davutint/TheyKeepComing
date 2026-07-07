using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Olum ekrani meta katmani: kosu ozeti ("DAY X — N kill, +N RUH, YENI REKOR!") +
    /// kalici yukseltme magazasi. GameOver panelinde yasar; satirlar MetaUpgradeCatalogSO'dan
    /// runtime klonlanir (TechTree/Market kalibi). Satin alinan yukseltmeler SONRAKI kosudan
    /// itibaren gecerlidir (restart'ta GameManager.ApplyMetaProgressionAtRunStart uygular).
    /// </summary>
    public class MetaProgressionUI : MonoBehaviour
    {
        [Header("Summary")]
        public TMP_Text MetaSummaryText;
        public TMP_Text MetaSoulsText;

        [Header("Shop")]
        public RectTransform MetaShopListRoot;
        public GameObject MetaShopRowTemplate;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
            if (GameManager.Instance != null && GameManager.Instance.GameState.IsGameOver)
                Refresh();
        }

        private void Update()
        {
            // GameManager sahnede bizden sonra dogabilir — abonelik lazy kurulur
            if (!_subscribed)
                TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= Refresh;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed || GameManager.Instance == null)
                return;

            GameManager.Instance.OnGameOver += Refresh;
            _subscribed = true;
        }

        /// <summary>Olum aninda cagrilir: ozet + magaza yeniden kurulur.</summary>
        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            var result = gm.LastRunResult;
            var state = MetaProgression.State;

            if (MetaSummaryText != null)
            {
                string record = result.NewRecord
                    ? $"  <color=#F2C94C>YENI REKOR!</color>"
                    : $"  (rekor: DAY {state.BestDay})";
                MetaSummaryText.text =
                    $"DAY {result.Day} — {result.Kills} kill{record}\n" +
                    $"<color=#B085F5>+{result.SoulsEarned} {MetaProgression.CurrencyName}</color>";
            }

            RebuildShop(gm);
        }

        private void RebuildShop(GameManager gm)
        {
            foreach (var row in _rows)
                Destroy(row);
            _rows.Clear();

            if (MetaSoulsText != null)
                MetaSoulsText.text = $"{MetaProgression.State.Souls} {MetaProgression.CurrencyName}";

            var catalog = gm.MetaCatalog;
            if (catalog == null || catalog.Upgrades == null || MetaShopListRoot == null || MetaShopRowTemplate == null)
                return;

            foreach (var upgrade in catalog.Upgrades)
            {
                if (upgrade == null)
                    continue;

                var row = Instantiate(MetaShopRowTemplate, MetaShopListRoot);
                row.name = "MetaRow_" + upgrade.Id;
                row.SetActive(true);
                _rows.Add(row);
                BindRow(row, upgrade);
            }
        }

        private void BindRow(GameObject row, MetaUpgradeSO upgrade)
        {
            int level = MetaProgression.GetUpgradeLevel(upgrade.Id);
            bool maxed = level >= upgrade.MaxLevel;
            int cost = maxed ? 0 : upgrade.GetCost(level);

            var title = FindText(row, "RowTitleText");
            if (title != null)
                title.text = upgrade.Title;

            var levelText = FindText(row, "RowLevelText");
            if (levelText != null)
                levelText.text = $"LV {level}/{upgrade.MaxLevel}";

            var costText = FindText(row, "RowCostText");
            if (costText != null)
                costText.text = maxed ? "MAX" : $"{cost} {MetaProgression.CurrencyName}";

            var buyButton = row.transform.Find("RowBuyButton")?.GetComponent<Button>();
            if (buyButton == null)
                return;

            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = !maxed && MetaProgression.State.Souls >= cost;
            buyButton.onClick.AddListener(() =>
            {
                if (MetaProgression.TryBuyUpgrade(upgrade))
                {
                    var rect = (RectTransform)buyButton.transform;
                    rect.DOKill(true);
                    rect.DOPunchScale(Vector3.one * 0.08f, 0.18f, 8, 0.7f).SetUpdate(true);
                    RebuildShop(GameManager.Instance);
                }
            });
        }

        private static TMP_Text FindText(GameObject row, string childName)
        {
            var child = row.transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
    }
}
