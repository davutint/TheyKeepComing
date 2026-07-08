using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Gun/gece atmosfer overlay'i (polish v2 — owner: "duz siyah karartmayi sevmedim").
    /// Karartma artik faz-RENKLI: Dusk amber gun batimi -> gece koyu mavi-mor -> Dawn
    /// altin-pembe acilis; kanli ay gecesi kizil. Renk ve alpha faz ilerlemesiyle yumusak
    /// gecis yapar (MoveTowards). Alpha tavani config'ten (NightOverlayAlpha) gelmeye devam eder.
    /// </summary>
    public class DayNightOverlayController : MonoBehaviour
    {
        public Image OverlayImage;
        public float AlphaMoveSpeed = 8f;

        // Faz atmosfer renkleri (owner Inspector'dan degistirebilsin diye public)
        public Color DuskColor = new Color(0.55f, 0.24f, 0.06f);   // amber gun batimi
        public Color NightColor = new Color(0.04f, 0.06f, 0.17f);  // koyu mavi-mor gece
        public Color DawnColor = new Color(0.55f, 0.30f, 0.14f);   // altin-pembe safak
        public Color BloodMoonColor = new Color(0.32f, 0.02f, 0.02f); // kanli ay kizili

        private void Reset()
        {
            OverlayImage = GetComponent<Image>();
        }

        private void Awake()
        {
            if (OverlayImage == null)
                OverlayImage = GetComponent<Image>();

            if (OverlayImage != null)
            {
                OverlayImage.raycastTarget = false;
                OverlayImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        private void Update()
        {
            if (OverlayImage == null)
                return;

            ResolveTarget(out Color targetColor, out float targetAlpha);

            Color color = OverlayImage.color;
            float dt = Time.unscaledDeltaTime;
            float tintSpeed = AlphaMoveSpeed * 0.3f * dt;
            color.r = Mathf.MoveTowards(color.r, targetColor.r, tintSpeed);
            color.g = Mathf.MoveTowards(color.g, targetColor.g, tintSpeed);
            color.b = Mathf.MoveTowards(color.b, targetColor.b, tintSpeed);
            color.a = Mathf.MoveTowards(color.a, targetAlpha, AlphaMoveSpeed * dt);
            OverlayImage.color = color;
        }

        private void ResolveTarget(out Color color, out float alpha)
        {
            color = NightColor;
            alpha = 0f;

            var gm = GameManager.Instance;
            if (gm == null || !gm.TryGetMobileCombatConfig(out var config) || gm.WaveState.StressTestMode)
                return;

            if (gm.TryGetContinuousSiegeCycle(out var cycle))
            {
                Color nightColor = cycle.IsBloodMoonNight ? BloodMoonColor : NightColor;
                float dayAlpha = config.DayOverlayAlpha;
                float nightAlpha = config.NightOverlayAlpha;

                switch (cycle.Phase)
                {
                    case SiegeCyclePhase.Day:
                        color = DuskColor; // gun icinde gorunmez (alpha ~0); Dusk'a temiz baslangic
                        alpha = dayAlpha;
                        break;
                    case SiegeCyclePhase.Dusk:
                        // gun batimi: amber'den gece rengine, alpha yukselirken
                        color = Color.Lerp(DuskColor, nightColor, cycle.PhaseProgress01);
                        alpha = Mathf.Lerp(dayAlpha, nightAlpha, cycle.PhaseProgress01);
                        break;
                    case SiegeCyclePhase.Night:
                        color = nightColor;
                        alpha = nightAlpha;
                        break;
                    default: // Dawn: geceden altin-pembe safaga, alpha dusurken
                        color = Color.Lerp(nightColor, DawnColor, cycle.PhaseProgress01);
                        alpha = Mathf.Lerp(nightAlpha, dayAlpha, cycle.PhaseProgress01);
                        break;
                }
                return;
            }

            // Legacy WallX modu (continuous kapali): eski davranis — duz gece karartmasi
            var wave = gm.WaveState;
            if (wave.Phase == RunPhaseType.DayPrep && !wave.WaveActive)
            {
                float duration = Mathf.Max(0.01f, wave.PrepDuration);
                float progress = 1f - Mathf.Clamp01(wave.PrepTimer / duration);
                alpha = Mathf.Lerp(config.DayOverlayAlpha, config.NightOverlayAlpha, progress);
                return;
            }

            alpha = wave.Phase == RunPhaseType.NightCombat ? config.NightOverlayAlpha : config.DayOverlayAlpha;
        }
    }
}
