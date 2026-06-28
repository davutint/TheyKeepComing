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

        private void Update()
        {
            if (OverlayImage == null)
                return;

            float targetAlpha = ResolveTargetAlpha();
            Color color = OverlayImage.color;
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
