using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Faz atmosferinin tek presentation sahibidir. UI overlay rengini ve sahnedeki tek global
    /// 2D light'i ayni authoritative cycle verisinden yumusak gecisle surer. Day paleti sicak fakat
    /// yuksek okunurlukta; Dusk/Night/Dawn hedefleri sonraki faz polish'lerine temiz taban verir.
    /// </summary>
    public class DayNightOverlayController : MonoBehaviour
    {
        public static readonly Color DefaultDayLightColor = new Color(1f, 0.93f, 0.82f, 1f);
        public static readonly Color DefaultDuskLightColor = new Color(1f, 0.72f, 0.47f, 1f);
        public static readonly Color DefaultNightLightColor = new Color(0.56f, 0.64f, 0.88f, 1f);
        public static readonly Color DefaultDawnLightColor = new Color(1f, 0.80f, 0.60f, 1f);
        public const float DefaultDayLightIntensity = 1.08f;
        public const float DefaultDuskLightIntensity = 0.90f;
        public const float DefaultNightLightIntensity = 0.72f;
        public const float DefaultDawnLightIntensity = 0.96f;

        public Image OverlayImage;
        public float AlphaMoveSpeed = 8f;

        [Header("Global 2D Light")]
        public Light2D GlobalLight;
        public float LightMoveSpeed = 2.5f;
        public Color DayLightColor = DefaultDayLightColor;
        public Color DuskLightColor = DefaultDuskLightColor;
        public Color NightLightColor = DefaultNightLightColor;
        public Color DawnLightColor = DefaultDawnLightColor;
        [Min(0f)] public float DayLightIntensity = DefaultDayLightIntensity;
        [Min(0f)] public float DuskLightIntensity = DefaultDuskLightIntensity;
        [Min(0f)] public float NightLightIntensity = DefaultNightLightIntensity;
        [Min(0f)] public float DawnLightIntensity = DefaultDawnLightIntensity;

        // Faz atmosfer renkleri (owner Inspector'dan degistirebilsin diye public)
        public Color DuskColor = new Color(0.55f, 0.24f, 0.06f);   // amber gun batimi
        public Color NightColor = new Color(0.04f, 0.06f, 0.17f);  // koyu mavi-mor gece
        public Color DawnColor = new Color(0.55f, 0.30f, 0.14f);   // altin-pembe safak
        public Color BloodMoonColor = new Color(0.32f, 0.02f, 0.02f); // kanli ay kizili

        private void Reset()
        {
            OverlayImage = GetComponent<Image>();
            GlobalLight = FindFirstObjectByType<Light2D>();
        }

        private void Awake()
        {
            if (OverlayImage == null)
                OverlayImage = GetComponent<Image>();
            if (GlobalLight == null)
                GlobalLight = FindFirstObjectByType<Light2D>();

            if (OverlayImage != null)
            {
                OverlayImage.raycastTarget = false;
                OverlayImage.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (OverlayImage != null)
            {
                ResolveTarget(out Color targetColor, out float targetAlpha);
                Color color = OverlayImage.color;
                float tintSpeed = AlphaMoveSpeed * 0.3f * dt;
                color.r = Mathf.MoveTowards(color.r, targetColor.r, tintSpeed);
                color.g = Mathf.MoveTowards(color.g, targetColor.g, tintSpeed);
                color.b = Mathf.MoveTowards(color.b, targetColor.b, tintSpeed);
                color.a = Mathf.MoveTowards(color.a, targetAlpha, AlphaMoveSpeed * dt);
                OverlayImage.color = color;
            }

            ResolveLightTarget(out Color lightColor, out float lightIntensity);
            ApplyGlobalLight(lightColor, lightIntensity, dt);
        }

        public void ResolveLightTarget(out Color color, out float intensity)
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.TryGetContinuousSiegeCycle(out var cycle))
            {
                ResolvePhaseLightTarget(cycle.Phase, cycle.PhaseProgress01, out color, out intensity);
                return;
            }

            bool night = gm != null && gm.WaveState.Phase == RunPhaseType.NightCombat;
            color = night ? NightLightColor : DayLightColor;
            intensity = night ? NightLightIntensity : DayLightIntensity;
        }

        public void ResolvePhaseLightTarget(
            SiegeCyclePhase phase,
            float phaseProgress01,
            out Color color,
            out float intensity)
        {
            float progress = Mathf.Clamp01(phaseProgress01);
            switch (phase)
            {
                case SiegeCyclePhase.Dusk:
                    color = Color.Lerp(DayLightColor, DuskLightColor, progress);
                    intensity = Mathf.Lerp(DayLightIntensity, DuskLightIntensity, progress);
                    return;
                case SiegeCyclePhase.Night:
                    color = NightLightColor;
                    intensity = NightLightIntensity;
                    return;
                case SiegeCyclePhase.Dawn:
                    if (progress < 0.5f)
                    {
                        float dawnIn = progress * 2f;
                        color = Color.Lerp(NightLightColor, DawnLightColor, dawnIn);
                        intensity = Mathf.Lerp(NightLightIntensity, DawnLightIntensity, dawnIn);
                    }
                    else
                    {
                        float dayIn = (progress - 0.5f) * 2f;
                        color = Color.Lerp(DawnLightColor, DayLightColor, dayIn);
                        intensity = Mathf.Lerp(DawnLightIntensity, DayLightIntensity, dayIn);
                    }
                    return;
                default:
                    color = DayLightColor;
                    intensity = DayLightIntensity;
                    return;
            }
        }

        private void ApplyGlobalLight(Color targetColor, float targetIntensity, float deltaTime)
        {
            if (GlobalLight == null)
                return;

            float colorStep = Mathf.Max(0f, LightMoveSpeed) * deltaTime;
            Color current = GlobalLight.color;
            current.r = Mathf.MoveTowards(current.r, targetColor.r, colorStep);
            current.g = Mathf.MoveTowards(current.g, targetColor.g, colorStep);
            current.b = Mathf.MoveTowards(current.b, targetColor.b, colorStep);
            current.a = 1f;
            GlobalLight.color = current;
            GlobalLight.intensity = Mathf.MoveTowards(
                GlobalLight.intensity,
                Mathf.Max(0f, targetIntensity),
                colorStep);
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
