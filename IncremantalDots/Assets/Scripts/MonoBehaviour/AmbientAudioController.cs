using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Faz ve worker ambiyansinin tek runtime sahibidir. Faz polling ile (DawnRewardToast kalibi):
    /// - DAY: gercek aktif worker sayisina gore seyrek, dusuk sesli uretim foley ritmi
    /// - DUSK + NIGHT: gece drone loop'u (normal gece = NightLoop, kanli ay = BloodMoonLoop)
    /// - DAY + DAWN: gece drone'u sessiz (Day'de yalniz worker foley)
    /// - Kanli ay gecesine giris aninda tek seferlik sting (canavar kukremesi).
    /// Iki AudioSource arasinda crossfade; kaynaklar 2D (spatialBlend 0) ve loop'ludur.
    /// Setup tool kurar ve clip'leri yalniz-bossa atar.
    /// </summary>
    public class AmbientAudioController : MonoBehaviour
    {
        [Header("Clips (setup atar)")]
        public AudioClip NightLoop;
        public AudioClip BloodMoonLoop;
        public AudioClip BloodMoonSting;

        [Header("Day Worker Foley (setup atar)")]
        public AudioClip[] WorkerFoleyClips;

        [Header("Mix")]
        [Range(0f, 1f)] public float NightVolume = 0.30f;
        [Range(0f, 1f)] public float BloodMoonVolume = 0.40f;
        [Range(0f, 1f)] public float StingVolume = 0.65f;
        public float FadeSpeed = 0.5f;

        [Header("Day Worker Mix")]
        [Range(0f, 1f)] public float WorkerFoleyVolume = 0.11f;
        [Min(0.1f)] public float WorkerFoleyMinInterval = 1.6f;
        [Min(0.1f)] public float WorkerFoleyMaxInterval = 5.2f;
        [Range(0f, 0.2f)] public float WorkerPitchVariation = 0.06f;

        public float WorkerActivity01 { get; private set; }
        public int WorkerFoleyPlayCount { get; private set; }
        public AudioSource WorkerFoleySource => _workerFoleySource;

        private const float CheckInterval = 0.2f;
        private float _checkTimer;
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _stingSource;
        private AudioSource _workerFoleySource;
        private AudioSource _activeSource; // hedef klibi calan kaynak
        private float _targetVolume;
        private float _workerFoleyTimer;
        private bool _workerFoleyEligible;
        private int _workerClipCursor;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;

        private void Awake()
        {
            _sourceA = CreateSource("AmbientLoopA", true);
            _sourceB = CreateSource("AmbientLoopB", true);
            _stingSource = CreateSource("AmbientSting", false);
            _workerFoleySource = CreateSource("WorkerAmbience", false);
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
            source.volume = 0f;
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

            UpdateWorkerFoley();
        }

        private void EvaluatePhase()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
            {
                _targetVolume = 0f;
                WorkerActivity01 = 0f;
                _workerFoleyEligible = false;
                _workerFoleyTimer = 0f;
                if (_workerFoleySource != null && _workerFoleySource.isPlaying)
                    _workerFoleySource.Stop();
                return;
            }

            var cycle = gm.ContinuousSiegeCycle;
            bool nightSide = cycle.Phase == SiegeCyclePhase.Dusk || cycle.Phase == SiegeCyclePhase.Night;
            bool bloodMoon = cycle.IsBloodMoonNight;

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

            // kanli ay gecesine giris sting'i (Night kenari)
            if (cycle.Phase == SiegeCyclePhase.Night && _lastPhase != SiegeCyclePhase.Night
                && bloodMoon && BloodMoonSting != null && _stingSource != null)
            {
                _stingSource.PlayOneShot(BloodMoonSting, StingVolume * SoundSettings.AmbienceVolume);
            }
            _lastPhase = cycle.Phase;

            AudioClip targetClip = null;
            float targetVolume = 0f;
            if (nightSide && !gm.GameState.IsGameOver)
            {
                targetClip = bloodMoon && BloodMoonLoop != null ? BloodMoonLoop : NightLoop;
                targetVolume = bloodMoon && BloodMoonLoop != null ? BloodMoonVolume : NightVolume;
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
            if (!_workerFoleyEligible || !HasWorkerFoleyClip())
                return;

            _workerFoleyTimer -= Time.unscaledDeltaTime;
            if (_workerFoleyTimer > 0f)
                return;

            PlayNextWorkerFoley();
            float interval = ResolveWorkerFoleyInterval(
                GameManager.Instance != null
                    ? GameManager.Instance.GetResourceWorkers(EconomyFocusType.Balanced)
                    : 0,
                WorkerFoleyMinInterval,
                WorkerFoleyMaxInterval);
            float cadenceVariation = 0.90f + (WorkerFoleyPlayCount % 3) * 0.10f;
            _workerFoleyTimer = interval * cadenceVariation;
        }

        private bool HasWorkerFoleyClip()
        {
            if (WorkerFoleyClips == null)
                return false;

            for (int i = 0; i < WorkerFoleyClips.Length; i++)
            {
                if (WorkerFoleyClips[i] != null)
                    return true;
            }
            return false;
        }

        private void PlayNextWorkerFoley()
        {
            if (_workerFoleySource == null || WorkerFoleyClips == null || WorkerFoleyClips.Length == 0)
                return;

            AudioClip clip = null;
            for (int attempt = 0; attempt < WorkerFoleyClips.Length; attempt++)
            {
                int index = (_workerClipCursor + attempt) % WorkerFoleyClips.Length;
                if (WorkerFoleyClips[index] == null)
                    continue;

                clip = WorkerFoleyClips[index];
                _workerClipCursor = (index + 1) % WorkerFoleyClips.Length;
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
    }
}
