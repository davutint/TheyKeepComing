using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Gunun an'larini vurgulayan kisa ekran vuruslari (Polish 2): safak sokunce ALTIN,
    /// kanli ay gecesi baslayinca KIZIL parlama. DamageFlashUI'nin renkli vurus API'sini
    /// kullanir (ayni overlay — ekstra UI objesi gerekmez). Faz kenari polling kalibi.
    /// </summary>
    public class MomentVignetteUI : MonoBehaviour
    {
        [Tooltip("Safak girisinde altin vurus siddeti (0 = kapali).")]
        public float DawnPeak = 0.20f;
        [Tooltip("Kanli ay gecesi girisinde kizil vurus siddeti (0 = kapali).")]
        public float BloodMoonPeak = 0.30f;

        private static readonly Color DawnGold = new Color(0.95f, 0.72f, 0.30f);
        private static readonly Color BloodRed = new Color(0.85f, 0.10f, 0.05f);

        private const float CheckInterval = 0.15f;
        private float _checkTimer;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer > 0f)
                return;
            _checkTimer = CheckInterval;

            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled || gm.GameState.IsGameOver)
                return;

            var cycle = gm.ContinuousSiegeCycle;
            if (cycle.Phase != _lastPhase)
            {
                if (cycle.Phase == SiegeCyclePhase.Dawn && DawnPeak > 0f)
                    DamageFlashUI.Instance?.Flash(DawnGold, DawnPeak);
                else if (cycle.Phase == SiegeCyclePhase.Night && cycle.IsBloodMoonNight && BloodMoonPeak > 0f)
                    DamageFlashUI.Instance?.Flash(BloodRed, BloodMoonPeak);
            }
            _lastPhase = cycle.Phase;
        }
    }
}
