using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Faz, worker ve bounded horde ambiyansinin tek runtime sahibidir. Faz polling ile (DawnRewardToast kalibi):
    /// - DAY: gercek aktif worker sayisina gore seyrek, dusuk sesli uretim foley ritmi
    /// - DUSK girisi: scene-load'da tekrar etmeyen, tek seferlik dusuk tension riser
    /// - DUSK + NIGHT: tek canonical NightLoop gece drone'u
    /// - NIGHT: zombie sayisi/baskisiyla logaritmik buyuyen tek 2D horde-bed loop'u
    /// - DAWN girisi: scene-load/Continue'da tekrar etmeyen, tek nefes/yeni-gun cue'su
    /// - DAY + DAWN: gece drone'u sessiz (Day'de yalniz worker foley)
    /// Faz loop'lari crossfade olur; horde katmani ayri fakat tek, 2D ve bounded bir kaynaktir.
    /// Setup tool kurar ve clip'leri yalniz-bossa atar.
    /// </summary>
    public class AmbientAudioController : MonoBehaviour
    {
        [Header("Central Audio Profile")]
        public DeadWallsAudioProfileSO AudioProfile;

        [Header("Clips (setup atar)")]
        public AudioClip NightLoop;
        public AudioClip DuskRiser;
        public AudioClip DawnCue;
        public AudioClip NightHordeLoop;

        [Header("Day Worker Foley (setup atar)")]
        public AudioClip[] WorkerFoleyClips;

        [Header("Mix")]
        [Range(0f, 1f)] public float NightVolume = 0.30f;
        [Range(0f, 1f)] public float DuskRiserVolume = 0.23f;
        [Range(0.5f, 1.5f)] public float DuskRiserPitch = 0.90f;
        [Range(0f, 1f)] public float DawnCueVolume = 0.28f;
        [Range(0.5f, 1.5f)] public float DawnCuePitch = 1f;
        [Range(0f, 1f)] public float NightHordeVolume = 0.18f;
        public float FadeSpeed = 0.5f;
        public float NightHordeFadeSpeed = 0.4f;

        [Header("Day Worker Mix")]
        [Range(0f, 1f)] public float WorkerFoleyVolume = 0.11f;
        [Min(0.1f)] public float WorkerFoleyMinInterval = 1.6f;
        [Min(0.1f)] public float WorkerFoleyMaxInterval = 5.2f;
        [Range(0f, 0.2f)] public float WorkerPitchVariation = 0.06f;

        public float WorkerActivity01 { get; private set; }
        public float NightHordeActivity01 { get; private set; }
        public int WorkerFoleyPlayCount { get; private set; }
        public int DuskRiserPlayCount { get; private set; }
        public int DawnCuePlayCount { get; private set; }
        public AudioSource WorkerFoleySource => _workerFoleySource;
        public AudioSource DuskRiserSource => _phaseTransitionSource;
        public AudioSource DawnCueSource => _phaseTransitionSource;
        public AudioSource NightHordeSource => _nightHordeSource;

        private const float CheckInterval = 0.2f;
        private float _checkTimer;
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _phaseTransitionSource;
        private AudioSource _workerFoleySource;
        private AudioSource _nightHordeSource;
        private AudioSource _activeSource; // hedef klibi calan kaynak
        private float _targetVolume;
        private float _targetNightHordeVolume;
        private float _workerFoleyTimer;
        private bool _workerFoleyEligible;
        private bool _hasObservedPhase;
        private int _workerClipCursor;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;

        private void Awake()
        {
            _sourceA = CreateSource("AmbientLoopA", true);
            _sourceB = CreateSource("AmbientLoopB", true);
            _phaseTransitionSource = CreateSource("PhaseTransition", false);
            _workerFoleySource = CreateSource("WorkerAmbience", false);
            _nightHordeSource = CreateSource("NightHordeBed", true);
            _activeSource = _sourceA;
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // ambiyans 2D
            source.volume = loop ? 0f : 1f;
            return source;
        }

        private void Update()
        {
            _checkTimer -= Time.unscaledDeltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = CheckInterval;
                EvaluatePhase();
            }

            // crossfade: aktif kaynak hedefe, digeri sifira yumusar
            float dt = FadeSpeed * Time.unscaledDeltaTime;
            var other = _activeSource == _sourceA ? _sourceB : _sourceA;
            if (_activeSource != null)
                _activeSource.volume = Mathf.MoveTowards(_activeSource.volume, _targetVolume, dt);
            if (other != null)
            {
                other.volume = Mathf.MoveTowards(other.volume, 0f, dt);
                if (other.volume <= 0f && other.isPlaying)
                    other.Stop();
            }

            if (_nightHordeSource != null)
            {
                float hordeStep = Mathf.Max(0f, NightHordeFadeSpeed) * Time.unscaledDeltaTime;
                _nightHordeSource.volume = Mathf.MoveTowards(
                    _nightHordeSource.volume,
                    _targetNightHordeVolume,
                    hordeStep);
                if (_nightHordeSource.volume <= 0f && _nightHordeSource.isPlaying)
                    _nightHordeSource.Stop();
            }

            UpdateWorkerFoley();
        }

        private void EvaluatePhase()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
            {
                _hasObservedPhase = false;
                _targetVolume = 0f;
                _targetNightHordeVolume = 0f;
                WorkerActivity01 = 0f;
                NightHordeActivity01 = 0f;
                _workerFoleyEligible = false;
                _workerFoleyTimer = 0f;
                if (_workerFoleySource != null && _workerFoleySource.isPlaying)
                    _workerFoleySource.Stop();
                return;
            }

            var cycle = gm.ContinuousSiegeCycle;
            AudioClip resolvedNightLoop = ResolveProfileClip(p => p.NightLoop, NightLoop);
            AudioClip resolvedDuskRiser = ResolveProfileClip(p => p.DuskRiser, DuskRiser);
            AudioClip resolvedDawnCue = ResolveProfileClip(p => p.DawnCue, DawnCue);
            AudioClip resolvedNightHordeLoop = ResolveProfileClip(p => p.NightHordeLoop, NightHordeLoop);
            bool nightSide = cycle.Phase == SiegeCyclePhase.Dusk || cycle.Phase == SiegeCyclePhase.Night;
            NightHordeActivity01 = ResolveNightHordeActivity01(
                cycle.Phase,
                cycle.HordePressure01,
                gm.WaveState.ZombiesAlive);
            bool nightHordeEligible = cycle.Phase == SiegeCyclePhase.Night
                && NightHordeActivity01 > 0f
                && resolvedNightHordeLoop != null
                && !gm.GameState.IsGameOver;
            _targetNightHordeVolume = nightHordeEligible
                ? NightHordeVolume * NightHordeActivity01 * SoundSettings.AmbienceVolume
                : 0f;
            if (nightHordeEligible && _nightHordeSource != null)
            {
                if (_nightHordeSource.clip != resolvedNightHordeLoop)
                {
                    _nightHordeSource.Stop();
                    _nightHordeSource.clip = resolvedNightHordeLoop;
                }
                if (!_nightHordeSource.isPlaying)
                    _nightHordeSource.Play();
            }

            int activeWorkers = gm.GetResourceWorkers(EconomyFocusType.Balanced);
            WorkerActivity01 = ResolveWorkerActivity01(activeWorkers);
            bool workerEligible = cycle.Phase == SiegeCyclePhase.Day
                && activeWorkers > 0
                && !gm.GameState.IsGameOver
                && !SimulationPauseService.IsPaused;
            if (workerEligible && !_workerFoleyEligible)
            {
                _workerFoleyTimer = Mathf.Min(
                    0.75f,
                    ResolveWorkerFoleyInterval(
                        activeWorkers,
                        WorkerFoleyMinInterval,
                        WorkerFoleyMaxInterval) * 0.35f);
            }
            else if (!workerEligible)
            {
                _workerFoleyTimer = 0f;
                if (_workerFoleySource != null && _workerFoleySource.isPlaying)
                    _workerFoleySource.Stop();
            }
            _workerFoleyEligible = workerEligible;

            bool phaseChanged = _hasObservedPhase && cycle.Phase != _lastPhase;
            if (phaseChanged && cycle.Phase == SiegeCyclePhase.Dusk
                && resolvedDuskRiser != null && _phaseTransitionSource != null
                && !gm.GameState.IsGameOver)
            {
                _phaseTransitionSource.pitch = DuskRiserPitch;
                _phaseTransitionSource.PlayOneShot(
                    resolvedDuskRiser,
                    DuskRiserVolume * SoundSettings.AmbienceVolume);
                DuskRiserPlayCount++;
            }

            if (phaseChanged && cycle.Phase == SiegeCyclePhase.Dawn
                && resolvedDawnCue != null && _phaseTransitionSource != null
                && !gm.GameState.IsGameOver)
            {
                _phaseTransitionSource.pitch = DawnCuePitch;
                _phaseTransitionSource.PlayOneShot(
                    resolvedDawnCue,
                    DawnCueVolume * SoundSettings.AmbienceVolume);
                DawnCuePlayCount++;
            }

            _lastPhase = cycle.Phase;
            _hasObservedPhase = true;

            AudioClip targetClip = null;
            float targetVolume = 0f;
            if (nightSide && !gm.GameState.IsGameOver)
            {
                targetClip = resolvedNightLoop;
                targetVolume = NightVolume;
            }

            if (targetClip == null)
            {
                _targetVolume = 0f;
                return;
            }

            // hedef klip degistiyse oteki kaynaga gec (crossfade)
            if (_activeSource.clip != targetClip)
            {
                _activeSource = _activeSource == _sourceA ? _sourceB : _sourceA;
                _activeSource.clip = targetClip;
            }

            if (!_activeSource.isPlaying)
                _activeSource.Play();
            _targetVolume = targetVolume * SoundSettings.AmbienceVolume;
        }

        public static float ResolveWorkerActivity01(int activeWorkers)
        {
            if (activeWorkers <= 0)
                return 0f;

            return Mathf.Clamp01(Mathf.Log(activeWorkers + 1f, 2f) / 7f);
        }

        public static float ResolveNightHordeActivity01(
            SiegeCyclePhase phase,
            float hordePressure01,
            int zombiesAlive)
        {
            if (phase != SiegeCyclePhase.Night || zombiesAlive <= 0)
                return 0f;

            float countActivity = Mathf.Clamp01(
                Mathf.Log(zombiesAlive + 1f, 2f) / Mathf.Log(10001f, 2f));
            float pressureScale = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(hordePressure01));
            return Mathf.Clamp01(countActivity * pressureScale);
        }

        public static float ResolveWorkerFoleyInterval(
            int activeWorkers,
            float minInterval,
            float maxInterval)
        {
            float min = Mathf.Max(0.1f, Mathf.Min(minInterval, maxInterval));
            float max = Mathf.Max(min, Mathf.Max(minInterval, maxInterval));
            return Mathf.Lerp(max, min, ResolveWorkerActivity01(activeWorkers));
        }

        private void UpdateWorkerFoley()
        {
            AudioClip[] clips = ResolveWorkerFoleyClips();
            if (!_workerFoleyEligible || !HasWorkerFoleyClip(clips))
                return;

            _workerFoleyTimer -= Time.unscaledDeltaTime;
            if (_workerFoleyTimer > 0f)
                return;

            PlayNextWorkerFoley(clips);
            float interval = ResolveWorkerFoleyInterval(
                GameManager.Instance != null
                    ? GameManager.Instance.GetResourceWorkers(EconomyFocusType.Balanced)
                    : 0,
                WorkerFoleyMinInterval,
                WorkerFoleyMaxInterval);
            float cadenceVariation = 0.90f + (WorkerFoleyPlayCount % 3) * 0.10f;
            _workerFoleyTimer = interval * cadenceVariation;
        }

        private static bool HasWorkerFoleyClip(AudioClip[] clips)
        {
            if (clips == null)
                return false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    return true;
            }
            return false;
        }

        private void PlayNextWorkerFoley(AudioClip[] clips)
        {
            if (_workerFoleySource == null || clips == null || clips.Length == 0)
                return;

            AudioClip clip = null;
            for (int attempt = 0; attempt < clips.Length; attempt++)
            {
                int index = (_workerClipCursor + attempt) % clips.Length;
                if (clips[index] == null)
                    continue;

                clip = clips[index];
                _workerClipCursor = (index + 1) % clips.Length;
                break;
            }

            if (clip == null)
                return;

            int pitchStep = (WorkerFoleyPlayCount * 5) % 7 - 3;
            _workerFoleySource.pitch = 1f + pitchStep * (WorkerPitchVariation / 3f);
            float volume = WorkerFoleyVolume
                * Mathf.Lerp(0.62f, 1f, WorkerActivity01)
                * SoundSettings.AmbienceVolume;
            _workerFoleySource.PlayOneShot(clip, volume);
            WorkerFoleyPlayCount++;
        }

        private AudioClip ResolveProfileClip(
            System.Func<DeadWallsAudioProfileSO, AudioClip> selector,
            AudioClip fallback)
        {
            DeadWallsAudioProfileSO profile = AudioProfile != null
                ? AudioProfile
                : DeadWallsAudioProfileSO.LoadDefault();
            if (profile == null || !profile.OverrideAmbience)
                return fallback;

            AudioClip selected = selector(profile);
            return selected != null ? selected : fallback;
        }

        private AudioClip[] ResolveWorkerFoleyClips()
        {
            DeadWallsAudioProfileSO profile = AudioProfile != null
                ? AudioProfile
                : DeadWallsAudioProfileSO.LoadDefault();
            return profile != null
                   && profile.OverrideAmbience
                   && profile.WorkerFoleyClips != null
                   && profile.WorkerFoleyClips.Length > 0
                ? profile.WorkerFoleyClips
                : WorkerFoleyClips;
        }
    }
}
