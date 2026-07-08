using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    public class DayNightOverlayController : MonoBehaviour
    {
        public Image OverlayImage;
        public float AlphaMoveSpeed = 8f;

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

        // Kanli ay gecesi karartma tonu: koyu kirmizi (normal gece = siyah)
        private static readonly Color BloodMoonTint = new Color(0.30f, 0.02f, 0.02f);

        private void Update()
        {
            if (OverlayImage == null)
                return;

            float targetAlpha = ResolveTargetAlpha();

            // Kanli ay: gece boyunca overlay kirmiziya kayar; diger fazlarda siyaha doner
            Color targetTint = Color.black;
            var gmTint = GameManager.Instance;
            if (gmTint != null && gmTint.TryGetContinuousSiegeCycle(out var tintCycle)
                && tintCycle.IsBloodMoonNight
                && (tintCycle.Phase == SiegeCyclePhase.Night || tintCycle.Phase == SiegeCyclePhase.Dusk))
            {
                targetTint = BloodMoonTint;
            }

            Color color = OverlayImage.color;
            float tintSpeed = AlphaMoveSpeed * 0.25f * Time.unscaledDeltaTime;
            color.r = Mathf.MoveTowards(color.r, targetTint.r, tintSpeed);
            color.g = Mathf.MoveTowards(color.g, targetTint.g, tintSpeed);
            color.b = Mathf.MoveTowards(color.b, targetTint.b, tintSpeed);
            color.a = Mathf.MoveTowards(color.a, targetAlpha, AlphaMoveSpeed * Time.unscaledDeltaTime);
            OverlayImage.color = color;
        }

        private static float ResolveTargetAlpha()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.TryGetMobileCombatConfig(out var config) || gm.WaveState.StressTestMode)
                return 0f;

            if (gm.TryGetContinuousSiegeCycle(out var cycle))
            {
                if (cycle.Phase == SiegeCyclePhase.Day)
                    return config.DayOverlayAlpha;

                if (cycle.Phase == SiegeCyclePhase.Dusk)
                    return Mathf.Lerp(config.DayOverlayAlpha, config.NightOverlayAlpha, cycle.PhaseProgress01);

                // Dawn: gece karartmasi safakla hizla acilir (odul/nefes fazi)
                if (cycle.Phase == SiegeCyclePhase.Dawn)
                    return Mathf.Lerp(config.NightOverlayAlpha, config.DayOverlayAlpha, cycle.PhaseProgress01);

                return config.NightOverlayAlpha;
            }

            var wave = gm.WaveState;
            if (wave.Phase == RunPhaseType.DayPrep && !wave.WaveActive)
            {
                float duration = Mathf.Max(0.01f, wave.PrepDuration);
                float progress = 1f - Mathf.Clamp01(wave.PrepTimer / duration);
                return Mathf.Lerp(config.DayOverlayAlpha, config.NightOverlayAlpha, progress);
            }

            if (wave.Phase == RunPhaseType.NightCombat)
                return config.NightOverlayAlpha;

            return config.DayOverlayAlpha;
        }
    }
}
