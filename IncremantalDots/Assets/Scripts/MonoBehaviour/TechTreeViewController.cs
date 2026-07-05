using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>Tech tree gezinme giris modu. Auto platforma gore secer (editor/PC = Desktop, telefon = Mobile).</summary>
    public enum TechTreeInputMode
    {
        Auto = 0,
        Desktop = 1,
        Mobile = 2,
    }

    /// <summary>
    /// Tech tree viewport'unun pan/zoom controller'i. ScrollRect'in USTUNE eklenir
    /// (sol surukleme pan'i ScrollRect'te kalir), su etkilesimleri ekler:
    /// Desktop: mouse tekerlegi = IMLEC MERKEZLI zoom, orta tus surukleme = pan.
    /// Mobile:  iki parmak pinch = parmak-orta-noktasi merkezli zoom (tek parmak pan ScrollRect'te).
    /// Zoom, content.localScale uzerinden yapilir; layout pozisyonlarina dokunulmaz.
    /// ScrollRect'in kendi tekerlek davranisi kapali tutulmalidir (scrollSensitivity = 0).
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class TechTreeViewController : MonoBehaviour, IScrollHandler, IDragHandler, IBeginDragHandler
    {
        [Header("Input")]
        public TechTreeInputMode InputMode = TechTreeInputMode.Auto;

        [Header("Zoom")]
        [Range(0.3f, 1f)] public float ZoomMin = 0.55f;
        [Range(1f, 3f)] public float ZoomMax = 1.6f;
        [Tooltip("Tekerlek tik basina zoom orani.")]
        public float WheelZoomStep = 0.12f;
        [Tooltip("Pinch hassasiyeti (1 = birebir mesafe orani).")]
        public float PinchSensitivity = 1f;

        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private Canvas _rootCanvas;
        private bool _pinchActive;
        private float _pinchStartDistance;
        private float _pinchStartScale;

        public TechTreeInputMode ResolvedMode
        {
            get
            {
                if (InputMode != TechTreeInputMode.Auto)
                    return InputMode;
                return Application.isMobilePlatform ? TechTreeInputMode.Mobile : TechTreeInputMode.Desktop;
            }
        }

        public float CurrentZoom
        {
            get
            {
                var content = Content;
                return content != null ? content.localScale.x : 1f;
            }
        }

        private RectTransform Content => _scrollRect != null ? _scrollRect.content : null;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            _rootCanvas = canvas != null ? canvas.rootCanvas : null;
        }

        private void Update()
        {
            if (ResolvedMode == TechTreeInputMode.Mobile)
                HandlePinch();
        }

        // ---------------------------------------------------------------
        // Desktop: tekerlek = imlec merkezli zoom
        // ---------------------------------------------------------------
        public void OnScroll(PointerEventData eventData)
        {
            if (ResolvedMode != TechTreeInputMode.Desktop)
                return;

            float delta = eventData.scrollDelta.y;
            if (Mathf.Approximately(delta, 0f))
                return;

            float factor = 1f + Mathf.Sign(delta) * WheelZoomStep;
            ApplyZoom(CurrentZoom * factor, eventData.position, eventData.pressEventCamera);
        }

        // ---------------------------------------------------------------
        // Desktop: orta tus surukleme = pan (ScrollRect yalniz sol tusu isler)
        // ---------------------------------------------------------------
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (ResolvedMode == TechTreeInputMode.Desktop && eventData.button == PointerEventData.InputButton.Middle)
                _scrollRect.StopMovement();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ResolvedMode != TechTreeInputMode.Desktop || eventData.button != PointerEventData.InputButton.Middle)
                return;

            var content = Content;
            if (content == null)
                return;

            float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            content.anchoredPosition += eventData.delta / Mathf.Max(0.0001f, scaleFactor);
            _scrollRect.velocity = Vector2.zero;
        }

        // ---------------------------------------------------------------
        // Mobile: iki parmak pinch = orta-nokta merkezli zoom
        // ---------------------------------------------------------------
        private void HandlePinch()
        {
            if (Input.touchCount < 2)
            {
                if (_pinchActive)
                {
                    _pinchActive = false;
                    _scrollRect.enabled = true; // pinch bitti, tek parmak pan geri
                }
                return;
            }

            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float distance = Vector2.Distance(t0.position, t1.position);
            Vector2 midPoint = (t0.position + t1.position) * 0.5f;

            if (!_pinchActive)
            {
                _pinchActive = true;
                _pinchStartDistance = Mathf.Max(1f, distance);
                _pinchStartScale = CurrentZoom;
                _scrollRect.enabled = false; // pinch sirasinda ScrollRect drag'i devre disi
                _scrollRect.velocity = Vector2.zero;
                return;
            }

            float ratio = distance / _pinchStartDistance;
            float target = _pinchStartScale * Mathf.LerpUnclamped(1f, ratio, PinchSensitivity);
            ApplyZoom(target, midPoint, null);
        }

        // ---------------------------------------------------------------
        // Ortak zoom cekirdegi: verilen EKRAN noktasinin altindaki icerik
        // noktasi sabit kalacak sekilde content scale + pozisyon guncellenir.
        // ---------------------------------------------------------------
        public void ApplyZoom(float targetScale, Vector2 screenFocus, Camera eventCamera)
        {
            var content = Content;
            if (content == null)
                return;

            targetScale = Mathf.Clamp(targetScale, GetDynamicZoomMin(), ZoomMax);
            if (Mathf.Approximately(targetScale, CurrentZoom))
                return;

            Vector2 localFocus;
            bool hasFocus = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content, screenFocus, eventCamera, out localFocus);

            Vector3 worldBefore = hasFocus ? content.TransformPoint(localFocus) : Vector3.zero;
            content.localScale = new Vector3(targetScale, targetScale, 1f);

            if (hasFocus)
            {
                Vector3 worldAfter = content.TransformPoint(localFocus);
                content.position += worldBefore - worldAfter;
            }

            _scrollRect.velocity = Vector2.zero;
        }

        /// <summary>
        /// Icerik viewport'tan kucukse zoom-out'un daha da kuculterek bosluga dusmesini engeller:
        /// alt sinir, icerigin viewport'a sigacagi olcege (fit) clamp'lenir; fit 1'den buyukse 1.
        /// </summary>
        private float GetDynamicZoomMin()
        {
            var content = Content;
            if (content == null || _viewport == null)
                return ZoomMin;

            Vector2 contentSize = content.sizeDelta;
            Vector2 viewportSize = _viewport.rect.size;
            if (contentSize.x <= 0f || contentSize.y <= 0f)
                return ZoomMin;

            float fit = Mathf.Min(viewportSize.x / contentSize.x, viewportSize.y / contentSize.y);
            return Mathf.Clamp(fit, ZoomMin, 1f);
        }

        /// <summary>Zoom'u sifirlayip icerigi verilen content-local noktaya odaklar (panel acilisinda kullanilir).</summary>
        public void ResetZoom()
        {
            var content = Content;
            if (content == null)
                return;

            content.localScale = Vector3.one;
            _scrollRect.velocity = Vector2.zero;
        }
    }
}
