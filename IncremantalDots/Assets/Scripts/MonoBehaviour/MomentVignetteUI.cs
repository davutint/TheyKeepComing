using UnityEngine;

namespace DeadWalls
{
    public readonly struct PhaseAtmosphereProfile
    {
        public readonly Color SkyColor;
        public readonly Color ParticleColor;
        public readonly float EmissionRate;

        public PhaseAtmosphereProfile(
            Color skyColor,
            Color particleColor,
            float emissionRate)
        {
            SkyColor = skyColor;
            ParticleColor = particleColor;
            EmissionRate = Mathf.Max(0f, emissionRate);
        }

        public static PhaseAtmosphereProfile Lerp(
            PhaseAtmosphereProfile from,
            PhaseAtmosphereProfile to,
            float progress01)
        {
            float progress = Mathf.Clamp01(progress01);
            return new PhaseAtmosphereProfile(
                Color.Lerp(from.SkyColor, to.SkyColor, progress),
                Color.Lerp(from.ParticleColor, to.ParticleColor, progress),
                Mathf.Lerp(from.EmissionRate, to.EmissionRate, progress));
        }
    }

    /// <summary>
    /// Faz gecislerinin sky ve bounded atmosfer particle katmaninin tek sahibidir. Mevcut
    /// color-grading ve audio owner'larini kopyalamaz; ayni authoritative cycle verisini kullanir.
    /// Legacy moment flash alani yalniz opsiyonel vurgu icin korunur. Ilk scene/Continue gozlemi
    /// transition sayilmaz ve particle burst veya Dawn flash tekrarina yol acmaz.
    /// </summary>
    public class MomentVignetteUI : MonoBehaviour
    {
        public static readonly Color DefaultDaySkyColor = new Color(0.18f, 0.24f, 0.20f, 1f);
        public static readonly Color DefaultDuskSkyColor = new Color(0.34f, 0.16f, 0.08f, 1f);
        public static readonly Color DefaultNightSkyColor = new Color(0.035f, 0.055f, 0.13f, 1f);
        public static readonly Color DefaultDawnCyanSkyColor = new Color(0.08f, 0.24f, 0.30f, 1f);
        public static readonly Color DefaultDawnGoldSkyColor = new Color(0.35f, 0.23f, 0.12f, 1f);

        public static readonly Color DefaultDayParticleColor = new Color(1f, 0.82f, 0.48f, 0.22f);
        public static readonly Color DefaultDuskParticleColor = new Color(1f, 0.46f, 0.16f, 0.34f);
        public static readonly Color DefaultNightParticleColor = new Color(0.48f, 0.68f, 1f, 0.26f);
        public static readonly Color DefaultDawnCyanParticleColor = new Color(0.38f, 0.88f, 1f, 0.38f);
        public static readonly Color DefaultDawnGoldParticleColor = new Color(1f, 0.74f, 0.28f, 0.42f);

        public const int DefaultMaxParticles = 72;
        public const float DefaultDayEmissionRate = 1.8f;
        public const float DefaultDuskEmissionRate = 8f;
        public const float DefaultNightEmissionRate = 3f;
        public const float DefaultDawnEmissionRate = 10f;

        [Header("Phase Sky")]
        public Camera SkyCamera;
        [Min(0f)] public float SkyColorMoveSpeed = 2.2f;
        public Color DaySkyColor = DefaultDaySkyColor;
        public Color DuskSkyColor = DefaultDuskSkyColor;
        public Color NightSkyColor = DefaultNightSkyColor;
        public Color DawnCyanSkyColor = DefaultDawnCyanSkyColor;
        public Color DawnGoldSkyColor = DefaultDawnGoldSkyColor;

        [Header("Bounded Atmosphere Particles")]
        public ParticleSystem AtmosphereParticles;
        public Color DayParticleColor = DefaultDayParticleColor;
        public Color DuskParticleColor = DefaultDuskParticleColor;
        public Color NightParticleColor = DefaultNightParticleColor;
        public Color DawnCyanParticleColor = DefaultDawnCyanParticleColor;
        public Color DawnGoldParticleColor = DefaultDawnGoldParticleColor;
        [Min(0f)] public float DayEmissionRate = DefaultDayEmissionRate;
        [Min(0f)] public float DuskEmissionRate = DefaultDuskEmissionRate;
        [Min(0f)] public float NightEmissionRate = DefaultNightEmissionRate;
        [Min(0f)] public float DawnEmissionRate = DefaultDawnEmissionRate;

        [Header("Legacy Edge Vignette")]
        [Tooltip("Dawn artik grading/sky/particle/audio ile okunur; sifir canonical degerdir.")]
        [Range(0f, 1f)] public float DawnPeak;
        [Tooltip("Kanli ay Night kenarindaki kizil vurus; Blood Moon temizleme paketine kadar korunur.")]
        [Range(0f, 1f)] public float BloodMoonPeak = 0.30f;

        public Color CurrentSkyTarget { get; private set; }
        public Color CurrentParticleColor { get; private set; }
        public float CurrentEmissionRate { get; private set; }
        public int TransitionBurstPlayCount { get; private set; }
        public int LastTransitionBurstCount { get; private set; }

        private static readonly Color DawnGoldFlash = new Color(0.95f, 0.72f, 0.30f);
        private static readonly Color BloodRedFlash = new Color(0.85f, 0.10f, 0.05f);

        private bool _hasObservedPhase;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;

        private void Reset()
        {
            SkyCamera = Camera.main;
            AtmosphereParticles = GetComponentInChildren<ParticleSystem>(true);
        }

        private void Awake()
        {
            if (SkyCamera == null)
                SkyCamera = Camera.main;
            if (AtmosphereParticles == null)
                AtmosphereParticles = GetComponentInChildren<ParticleSystem>(true);

            if (AtmosphereParticles != null && !AtmosphereParticles.isPlaying)
                AtmosphereParticles.Play();
        }

        private void OnDisable()
        {
            SetParticleEmission(0f);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.TryGetContinuousSiegeCycle(out var cycle))
            {
                _hasObservedPhase = false;
                CurrentEmissionRate = 0f;
                SetParticleEmission(0f);
                return;
            }

            PhaseAtmosphereProfile profile = ResolvePhaseProfile(
                cycle.Phase,
                cycle.PhaseProgress01);
            CurrentSkyTarget = profile.SkyColor;
            CurrentParticleColor = profile.ParticleColor;

            bool presentationSuppressed = gm.GameState.IsGameOver || gm.WaveState.StressTestMode;
            CurrentEmissionRate = presentationSuppressed ? 0f : profile.EmissionRate;
            ApplySky(profile.SkyColor);
            ApplyParticles(profile.ParticleColor, CurrentEmissionRate);

            bool phaseChanged = _hasObservedPhase && cycle.Phase != _lastPhase;
            if (phaseChanged && !presentationSuppressed)
                PlayPhaseEdge(cycle.Phase, cycle.IsBloodMoonNight);

            _lastPhase = cycle.Phase;
            _hasObservedPhase = true;
        }

        public PhaseAtmosphereProfile ResolvePhaseProfile(
            SiegeCyclePhase phase,
            float phaseProgress01)
        {
            float progress = Mathf.Clamp01(phaseProgress01);
            PhaseAtmosphereProfile day = new PhaseAtmosphereProfile(
                DaySkyColor,
                DayParticleColor,
                DayEmissionRate);
            PhaseAtmosphereProfile dusk = new PhaseAtmosphereProfile(
                DuskSkyColor,
                DuskParticleColor,
                DuskEmissionRate);
            PhaseAtmosphereProfile night = new PhaseAtmosphereProfile(
                NightSkyColor,
                NightParticleColor,
                NightEmissionRate);
            PhaseAtmosphereProfile dawnCyan = new PhaseAtmosphereProfile(
                DawnCyanSkyColor,
                DawnCyanParticleColor,
                DawnEmissionRate);
            PhaseAtmosphereProfile dawnGold = new PhaseAtmosphereProfile(
                DawnGoldSkyColor,
                DawnGoldParticleColor,
                DawnEmissionRate * 0.82f);

            switch (phase)
            {
                case SiegeCyclePhase.Dusk:
                    if (progress < DayNightOverlayController.DuskAmberPeakProgress)
                    {
                        return PhaseAtmosphereProfile.Lerp(
                            day,
                            dusk,
                            progress / DayNightOverlayController.DuskAmberPeakProgress);
                    }

                    return PhaseAtmosphereProfile.Lerp(
                        dusk,
                        night,
                        (progress - DayNightOverlayController.DuskAmberPeakProgress)
                        / (1f - DayNightOverlayController.DuskAmberPeakProgress));
                case SiegeCyclePhase.Night:
                    return night;
                case SiegeCyclePhase.Dawn:
                    if (progress < DayNightOverlayController.DawnCyanPeakProgress)
                    {
                        return PhaseAtmosphereProfile.Lerp(
                            night,
                            dawnCyan,
                            progress / DayNightOverlayController.DawnCyanPeakProgress);
                    }
                    if (progress < DayNightOverlayController.DawnGoldPeakProgress)
                    {
                        return PhaseAtmosphereProfile.Lerp(
                            dawnCyan,
                            dawnGold,
                            (progress - DayNightOverlayController.DawnCyanPeakProgress)
                            / (DayNightOverlayController.DawnGoldPeakProgress
                                - DayNightOverlayController.DawnCyanPeakProgress));
                    }

                    return PhaseAtmosphereProfile.Lerp(
                        dawnGold,
                        day,
                        (progress - DayNightOverlayController.DawnGoldPeakProgress)
                        / (1f - DayNightOverlayController.DawnGoldPeakProgress));
                default:
                    return day;
            }
        }

        public static int ResolveTransitionBurstCount(SiegeCyclePhase enteredPhase)
        {
            switch (enteredPhase)
            {
                case SiegeCyclePhase.Dusk:
                    return 10;
                case SiegeCyclePhase.Night:
                    return 6;
                case SiegeCyclePhase.Dawn:
                    return 14;
                default:
                    return 4;
            }
        }

        private void ApplySky(Color targetColor)
        {
            if (SkyCamera == null)
                return;

            float blend = 1f - Mathf.Exp(
                -Mathf.Max(0f, SkyColorMoveSpeed) * Time.unscaledDeltaTime);
            Color color = Color.Lerp(SkyCamera.backgroundColor, targetColor, blend);
            color.a = 1f;
            SkyCamera.backgroundColor = color;
        }

        private void ApplyParticles(Color particleColor, float emissionRate)
        {
            if (AtmosphereParticles == null)
                return;

            if (SkyCamera != null)
            {
                Vector3 position = SkyCamera.transform.position;
                position.z = -1f;
                AtmosphereParticles.transform.position = position;
            }

            ParticleSystem.MainModule main = AtmosphereParticles.main;
            main.startColor = particleColor;
            SetParticleEmission(emissionRate);
            if (emissionRate > 0f && !AtmosphereParticles.isPlaying)
                AtmosphereParticles.Play();
        }

        private void SetParticleEmission(float emissionRate)
        {
            if (AtmosphereParticles == null)
                return;

            ParticleSystem.EmissionModule emission = AtmosphereParticles.emission;
            emission.rateOverTime = Mathf.Max(0f, emissionRate);
        }

        private void PlayPhaseEdge(SiegeCyclePhase phase, bool isBloodMoonNight)
        {
            LastTransitionBurstCount = ResolveTransitionBurstCount(phase);
            if (AtmosphereParticles != null && LastTransitionBurstCount > 0)
                AtmosphereParticles.Emit(LastTransitionBurstCount);
            TransitionBurstPlayCount++;

            if (phase == SiegeCyclePhase.Dawn && DawnPeak > 0f)
                DamageFlashUI.Instance?.Flash(DawnGoldFlash, DawnPeak);
            else if (phase == SiegeCyclePhase.Night && isBloodMoonNight && BloodMoonPeak > 0f)
                DamageFlashUI.Instance?.Flash(BloodRedFlash, BloodMoonPeak);
        }
    }
}
