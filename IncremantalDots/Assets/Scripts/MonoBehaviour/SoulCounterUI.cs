using TMPro;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Kosu ici projected death reward sayaci: oyuncu su anda olurse receipt'e yazilacak exact
    /// MetaRewardQuote toplam Soul miktarini gorur. Ayri formul tutmaz; GameManager aggregate
    /// telemetry'sini 0.25 saniyede bir okur.
    /// </summary>
    public class SoulCounterUI : MonoBehaviour
    {
        public GameObject CounterPanel;
        public TMP_Text CounterText;

        private const float CheckInterval = 0.25f;
        private float _checkTimer;
        private int _lastShown = -1;

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer > 0f)
                return;
            _checkTimer = CheckInterval;

            var gm = GameManager.Instance;
            MetaRuntimeTelemetry telemetry = gm != null ? gm.GetMetaRuntimeTelemetry() : default;
            bool visible = gm != null
                           && gm.ContinuousSiegeCycle.Enabled
                           && !gm.GameState.IsGameOver
                           && telemetry.HasCurrentRewardQuote;
            if (CounterPanel != null && CounterPanel.activeSelf != visible)
                CounterPanel.SetActive(visible);
            if (!visible)
                return;

            int projectedSouls = telemetry.CurrentRewardQuote.TotalSouls;
            if (projectedSouls != _lastShown && CounterText != null)
            {
                _lastShown = projectedSouls;
                MetaPresentationSettings presentation = gm.MetaCatalog != null
                    ? gm.MetaCatalog.Presentation
                    : null;
                string currency = presentation != null
                    ? presentation.ShortName
                    : MetaProgression.CurrencyName;
                CounterText.text =
                    $"<color=#FFB33F>ON DEATH</color>  +{projectedSouls:N0} {currency}";
            }
        }
    }
}
