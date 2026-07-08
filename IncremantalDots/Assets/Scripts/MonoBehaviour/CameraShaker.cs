using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Trauma-tabanli kamera sarsintisi (M-D his katmani). Ana kamerada yasar (setup kurar).
    /// AddTrauma(t) biriktirir; her frame trauma^2 siddetinde Perlin offset uygulanir ve
    /// trauma dogal sonumlenir. Kamera pozisyonu SABIT tasarim oldugundan base pozisyon
    /// Start'ta cache'lenir — sarsinti hep base cevresinde salinir (drift yok).
    /// Tetikleyen: CombatFeedbackBridge (CastleHit SFX'i rate-limit'ten gectiginde).
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        [Tooltip("Maksimum pozisyon sapmasi (dunya birimi, trauma=1'de).")]
        public float MaxOffset = 0.22f;
        [Tooltip("Trauma'nin saniyede sonumlenme hizi.")]
        public float TraumaDecayPerSecond = 1.6f;
        [Tooltip("Perlin ornekleme hizi (titresim frekansi).")]
        public float Frequency = 18f;

        private Vector3 _basePosition;
        private bool _baseCached;
        private float _trauma;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            _basePosition = transform.position;
            _baseCached = true;
        }

        /// <summary>Sarsinti ekle (0..1; birikir, 1'de kirpilir).</summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        private void LateUpdate()
        {
            if (!_baseCached)
                return;

            if (_trauma <= 0f)
            {
                if (transform.position != _basePosition)
                    transform.position = _basePosition;
                return;
            }

            _trauma = Mathf.Max(0f, _trauma - TraumaDecayPerSecond * Time.unscaledDeltaTime);
            float shake = _trauma * _trauma; // kare: kucuk vuruslar yumusak, buyukler sert
            float t = Time.unscaledTime * Frequency;
            float offsetX = (Mathf.PerlinNoise(t, 17.31f) * 2f - 1f) * MaxOffset * shake;
            float offsetY = (Mathf.PerlinNoise(41.77f, t) * 2f - 1f) * MaxOffset * shake;
            transform.position = _basePosition + new Vector3(offsetX, offsetY, 0f);
        }
    }
}
