using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// DAWN odul toast'u: faz Dawn'a gectiginde bir kez "DAWN - DAY n SURVIVED +15 POP"
    /// gosterir. Nufus odulu MobilePopulationEconomySystem tarafindan Dawn'da verilir;
    /// bu controller yalnizca o ani GORUNUR kilar (GDD 4-faz odul vurusu).
    /// </summary>
    public class DawnRewardToastUI : MonoBehaviour
    {
        public TMP_Text ToastText;

        private const float CheckInterval = 0.2f;
        private float _nextCheckTime;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;
        private Sequence _toastSequence;

        private void OnEnable()
        {
            if (ToastText != null)
                ToastText.alpha = 0f;
        }

        private void OnDisable()
        {
            _toastSequence?.Kill();
            _toastSequence = null;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCheckTime)
                return;

            _nextCheckTime = Time.unscaledTime + CheckInterval;

            var gm = GameManager.Instance;
            if (gm == null || !gm.IsMobileMode)
                return;

            var cycle = gm.ContinuousSiegeCycle;
            if (!cycle.Enabled)
                return;

            if (cycle.Phase == SiegeCyclePhase.Dawn && _lastPhase != SiegeCyclePhase.Dawn)
                ShowDawnToast(gm, cycle);

            _lastPhase = cycle.Phase;
        }

        private void ShowDawnToast(GameManager gm, ContinuousSiegeCycleData cycle)
        {
            if (ToastText == null)
                return;

            int growth = 15;
            if (gm.TryGetMobileCombatConfig(out var config) && config.PopulationGrowthPerDayPrep > 0)
                growth = config.PopulationGrowthPerDayPrep;

            int dayNumber = Mathf.Max(1, cycle.CycleIndex + 1);
            ToastText.text = $"DAWN — DAY {dayNumber} SURVIVED  ·  +{growth} POP";

            _toastSequence?.Kill();
            ToastText.alpha = 0f;
            ToastText.gameObject.SetActive(true);
            _toastSequence = DOTween.Sequence()
                .Append(ToastText.DOFade(1f, 0.2f))
                .AppendInterval(2.4f)
                .Append(ToastText.DOFade(0f, 0.5f))
                .SetUpdate(true);
        }
    }
}
