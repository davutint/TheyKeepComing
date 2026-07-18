using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Olum ekrani meta katmani: run kapanis ozeti, Last Embers reward/bakiye sunumu ve
    /// kalici yukseltme magazasi. GameOver panelinde yasar; satirlar
    /// MetaUpgradeCatalogSO'dan runtime klonlanir. Satin alinan yukseltmeler SONRAKI
    /// kosudan itibaren gecerlidir.
    /// </summary>
    public class MetaProgressionUI : MonoBehaviour
    {
        [Header("Summary")]
        public TMP_Text MetaSummaryText;
        public TMP_Text MetaRecordText;
        public TMP_Text MetaEarnedText;
        public TMP_Text MetaSoulsText;
        public Image MetaRewardIcon;
        public Image MetaCurrencyIcon;

        [Header("Shop")]
        public TMP_Text MetaShopTitleText;
        public TMP_Text MetaShopHintText;
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
            MetaPresentationSettings presentation = ResolvePresentation(gm);

            if (MetaSummaryText != null)
            {
                MetaSummaryText.text =
                    $"DAY {Mathf.Max(1, result.Day)} HELD" +
                    $"  <color=#8C94A3>•</color>  {Mathf.Max(0, result.Kills):N0} ENEMIES SLAIN";
            }

            if (MetaRecordText != null)
                MetaRecordText.text = result.NewRecord
                    ? presentation.NewRecordLabel
                    : $"BEST: DAY {Mathf.Max(0, state.BestDay)}";

            if (MetaEarnedText != null)
                MetaEarnedText.text =
                    $"+{Mathf.Max(0, result.SoulsEarned):N0} {presentation.DisplayName}";

            ApplyCurrencyIcon(MetaRewardIcon, presentation);
            ApplyCurrencyIcon(MetaCurrencyIcon, presentation);
            if (MetaShopTitleText != null)
                MetaShopTitleText.text = presentation.ShopTitle;
            if (MetaShopHintText != null)
                MetaShopHintText.text = presentation.ShopHint;

            RebuildShop(gm, presentation);
        }

        private void RebuildShop(GameManager gm)
        {
            RebuildShop(gm, ResolvePresentation(gm));
        }

        private void RebuildShop(GameManager gm, MetaPresentationSettings presentation)
        {
            foreach (var row in _rows)
                Destroy(row);
            _rows.Clear();

            if (MetaSoulsText != null)
                MetaSoulsText.text =
                    $"{Mathf.Max(0, MetaProgression.State.Souls):N0} {presentation.ShortName}";

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
                BindRow(row, upgrade, gm, presentation);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(MetaShopListRoot);
        }

        private void BindRow(
            GameObject row,
            MetaUpgradeSO upgrade,
            GameManager gm,
            MetaPresentationSettings presentation)
        {
            int level = MetaProgression.GetUpgradeLevel(upgrade.Id);
            bool maxed = upgrade.IsMaxLevel(level);
            int cost = maxed ? 0 : upgrade.GetCost(level);

            var title = FindText(row, "RowTitleText");
            if (title != null)
                title.text = upgrade.Title;

            var description = FindText(row, "RowDescriptionText");
            if (description != null)
                description.text = upgrade.Description;

            var levelText = FindText(row, "RowLevelText");
            if (levelText != null)
                levelText.text = upgrade.IsRepeatable
                    ? $"LEVEL {level}"
                    : $"LEVEL {level}/{upgrade.MaxLevel}";

            var costText = FindText(row, "RowCostText");
            if (costText != null)
                costText.text = maxed ? "MAXED" : $"{cost:N0} {presentation.ShortName}";

            var buyButton = row.transform.Find("RowBuyButton")?.GetComponent<Button>();
            if (buyButton == null)
                return;

            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = !maxed && gm.CanBuyMetaUpgrade(upgrade);
            buyButton.onClick.AddListener(() =>
            {
                if (gm.TryBuyMetaUpgrade(upgrade))
                {
                    UiSoundFeedback.Instance?.PlaySuccess();
                    var rect = (RectTransform)buyButton.transform;
                    rect.DOKill(true);
                    rect.DOPunchScale(Vector3.one * 0.08f, 0.18f, 8, 0.7f).SetUpdate(true);
                    RebuildShop(gm, presentation);
                }
                else
                {
                    UiSoundFeedback.Instance?.PlayFail();
                }
            });
        }

        private static TMP_Text FindText(GameObject row, string childName)
        {
            var child = row.transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static MetaPresentationSettings ResolvePresentation(GameManager gm)
        {
            return gm != null && gm.MetaCatalog != null && gm.MetaCatalog.Presentation != null
                ? gm.MetaCatalog.Presentation
                : MetaPresentationSettings.CreateLastEmbers();
        }

        private static void ApplyCurrencyIcon(Image image, MetaPresentationSettings presentation)
        {
            if (image == null || presentation == null)
                return;

            image.sprite = presentation.CurrencyIcon;
            image.color = Color.white;
            image.preserveAspect = true;
            image.enabled = presentation.CurrencyIcon != null;
        }
    }
}
