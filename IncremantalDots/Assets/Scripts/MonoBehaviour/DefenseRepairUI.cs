using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// CastleDefensePanel'deki player-facing REPAIR butonunun controller'i.
    /// Normal tamir yalniz Day/Dusk sirasinda denenebilir; gercek iyilesecek HP kadar
    /// Stone maliyeti vardir. Night basladiginda buton gizlenir ve maliyet harcanamaz.
    /// HUDController read-only kaldigi icin buton ayri controller'dadir.
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
            bool phaseAvailable = gm.IsRepairPhaseAvailable();
            var cost = gm.GetRepairCost();

            if (RepairButton != null)
            {
                if (RepairButton.gameObject.activeSelf != phaseAvailable)
                    RepairButton.gameObject.SetActive(phaseAvailable);
                RepairButton.interactable = gm.CanRepairDefenseFull();
            }

            if (RepairCostText != null)
            {
                string label;
                if (!damaged)
                    label = "FULL";
                else if (!phaseAvailable)
                    label = string.Empty;
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
                UiSoundFeedback.Instance?.PlaySuccess();
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
