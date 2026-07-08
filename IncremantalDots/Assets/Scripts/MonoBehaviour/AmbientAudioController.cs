using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Gece/kanli ay ambiyans katmani (M-D). Faz polling ile (DawnRewardToast kalibi):
    /// - DUSK + NIGHT: gece drone loop'u (normal gece = NightLoop, kanli ay = BloodMoonLoop)
    /// - DAY + DAWN: sessizlik (fade-out)
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

        [Header("Mix")]
        [Range(0f, 1f)] public float NightVolume = 0.30f;
        [Range(0f, 1f)] public float BloodMoonVolume = 0.40f;
        [Range(0f, 1f)] public float StingVolume = 0.65f;
        public float FadeSpeed = 0.5f;

        private const float CheckInterval = 0.2f;
        private float _checkTimer;
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _stingSource;
        private AudioSource _activeSource; // hedef klibi calan kaynak
        private float _targetVolume;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;

        private void Awake()
        {
            _sourceA = CreateSource("AmbientLoopA", true);
            _sourceB = CreateSource("AmbientLoopB", true);
            _stingSource = CreateSource("AmbientSting", false);
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
        }

        private void EvaluatePhase()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
                return;

            var cycle = gm.ContinuousSiegeCycle;
            bool nightSide = cycle.Phase == SiegeCyclePhase.Dusk || cycle.Phase == SiegeCyclePhase.Night;
            bool bloodMoon = cycle.IsBloodMoonNight;

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
    }
}
