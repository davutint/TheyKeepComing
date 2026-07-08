using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Kale hasar flash'i (M-D his katmani): tam-ekran kirmizi vuru — alpha aninda tepe
    /// degere zipla, sonra sonumlen. Canvas'ta FlashOverlay Image'inde yasar (setup kurar;
    /// raycastTarget kapali, gun/gece overlay'inin USTUNDE). Tetikleyen: CombatFeedbackBridge.
    /// </summary>
    public class DamageFlashUI : MonoBehaviour
    {
        public static DamageFlashUI Instance { get; private set; }

        public Image FlashImage;
        [Tooltip("Vurus tepe alpha'si (ust uste vuruslarda kirpilir).")]
        public float PeakAlpha = 0.18f;
        [Tooltip("Alpha'nin saniyede sonumlenme hizi.")]
        public float FadePerSecond = 0.9f;

        private static readonly Color DamageColor = new Color(0.75f, 0.08f, 0.05f);
        private Color _currentColor = new Color(0.75f, 0.08f, 0.05f);
        private float _alpha;

        private void Awake()
        {
            Instance = this;
            if (FlashImage != null)
            {
                FlashImage.raycastTarget = false;
                FlashImage.color = new Color(DamageColor.r, DamageColor.g, DamageColor.b, 0f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Hasar vurusu (kirmizi, varsayilan siddet).</summary>
        public void Flash()
        {
            Flash(DamageColor, PeakAlpha);
        }

        /// <summary>Renkli an vurusu (Polish 2): safak altini, kanli ay kizili vb.</summary>
        public void Flash(Color color, float peak)
        {
            _currentColor = color;
            _alpha = Mathf.Clamp01(Mathf.Max(_alpha, peak));
        }

        private void Update()
        {
            if (FlashImage == null || _alpha <= 0f)
                return;

            _alpha = Mathf.Max(0f, _alpha - FadePerSecond * Time.unscaledDeltaTime);
            FlashImage.color = new Color(_currentColor.r, _currentColor.g, _currentColor.b, _alpha);
        }
    }
}
