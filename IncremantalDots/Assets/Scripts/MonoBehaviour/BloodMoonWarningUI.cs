using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Kanli ay gunduz uyarisi (M-C): gun DAY fazina girdiginde o gunun gecesi kanli ay ise
    /// ("ContinuousSiegeCycleData.IsBloodMoonNight") toast gosterir — oyuncu hazirlik kararini
    /// gunduzden verebilsin. DawnRewardToastUI kalibi: polling + faz-kenari tespiti + DOTween fade.
    /// </summary>
    public class BloodMoonWarningUI : MonoBehaviour
    {
        public TMP_Text WarningText;

        private const float CheckInterval = 0.2f;
        private float _checkTimer;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Dawn;
        private Sequence _toastSequence;

        private void OnEnable()
        {
            if (WarningText != null)
            {
                WarningText.alpha = 0f;
                WarningText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            _toastSequence?.Kill();
            _toastSequence = null;
        }

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer > 0f)
                return;
            _checkTimer = CheckInterval;

            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
                return;

            var cycle = gm.ContinuousSiegeCycle;
            if (cycle.Phase == SiegeCyclePhase.Day && _lastPhase != SiegeCyclePhase.Day && cycle.IsBloodMoonNight)
                ShowWarning(cycle.CycleIndex + 1);
            _lastPhase = cycle.Phase;
        }

        private void ShowWarning(int dayNumber)
        {
            if (WarningText == null)
                return;

            WarningText.text = $"<color=#E05545>BLOOD MOON RISES TONIGHT</color>\nDAY {dayNumber} — the horde will be relentless";

            _toastSequence?.Kill();
            WarningText.alpha = 0f;
            WarningText.gameObject.SetActive(true);
            _toastSequence = DOTween.Sequence()
                .Append(WarningText.DOFade(1f, 0.25f))
                .AppendInterval(3.2f)
                .Append(WarningText.DOFade(0f, 0.6f))
                .SetUpdate(true);
        }
    }
}
