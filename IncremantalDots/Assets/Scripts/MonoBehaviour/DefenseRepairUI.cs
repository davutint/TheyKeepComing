using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// CastleDefensePanel'deki player-facing REPAIR butonunun controller'i.
    /// Tamir artik continuous siege sirasinda HER ZAMAN denenebilir ve kayip oraniyla
    /// olceklenen bir kaynak maliyeti vardir (GameManager.GetRepairCost) — ana ekonomi
    /// sink'lerinden biri. HUDController read-only kaldigi icin buton ayri controller'dadir
    /// (WorkerDrawerToggleButton/TechTreeOpenButton emsali).
    /// </summary>
    public class DefenseRepairUI : MonoBehaviour
    {
        public Button RepairButton;
        public TMP_Text RepairCostText;

        private const float RefreshInterval = 0.25f;
        private float _nextRefreshTime;
        private bool _bound;

        private void OnEnable()
        {
            if (!_bound && RepairButton != null)
            {
                _bound = true;
                RepairButton.onClick.RemoveListener(HandleRepairClicked);
                RepairButton.onClick.AddListener(HandleRepairClicked);
            }
        }

        private void OnDisable()
        {
            _bound = false;
            if (RepairButton != null)
                RepairButton.onClick.RemoveListener(HandleRepairClicked);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                if (RepairButton != null)
                    RepairButton.interactable = false;
                return;
            }

            bool damaged = gm.GetDefensePercent() < 0.995f;
            var cost = gm.GetRepairCost();

            if (RepairButton != null)
                RepairButton.interactable = gm.CanRepairDefenseFull();

            if (RepairCostText != null)
            {
                string label;
                if (!damaged)
                    label = "FULL";
                else
                {
                    label = cost.ToDisplayString();
                    if (string.IsNullOrEmpty(label))
                        label = "FREE";
                }

                if (RepairCostText.text != label)
                    RepairCostText.text = label;
            }
        }

        private void HandleRepairClicked()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            if (gm.RepairDefenseFull())
            {
                // Basarili tamir: butonda kucuk punch (juice tutarliligi)
                var rect = RepairButton != null ? (RectTransform)RepairButton.transform : null;
                if (rect != null)
                {
                    rect.DOKill(true);
                    rect.DOPunchScale(Vector3.one * 0.08f, 0.18f, 7, 0.7f).SetUpdate(true);
                }
            }
            else if (RepairButton != null)
            {
                // Reddetme: shake
                var rect = (RectTransform)RepairButton.transform;
                rect.DOKill(true);
                rect.DOShakeAnchorPos(0.2f, new Vector2(5f, 0f), 16, 90f, false, true).SetUpdate(true);
            }

            Refresh();
        }
    }
}
