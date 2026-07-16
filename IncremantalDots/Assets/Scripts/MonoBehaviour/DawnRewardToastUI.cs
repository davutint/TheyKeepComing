using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// DAWN odul ve kapı sunumunun tek sahibidir. Faz Dawn'a gectiginde gercek kabul edilen
    /// population miktarini gosterir; survivor'lar yaklasirken ana portcullis tile'ini gecici olarak
    /// acar. Gameplay population/Food truth'u MobilePopulationEconomySystem ve GameManager'dadir.
    /// </summary>
    public class DawnRewardToastUI : MonoBehaviour
    {
        public static readonly Color DefaultGateGlowColor = new Color(1f, 0.68f, 0.24f, 1f);
        public const float DefaultGateOpenDelay = 2.05f;
        public const float DefaultGateOpenDuration = 2.55f;
        public const float DefaultGateGlowIntensity = 0.76f;
        public const float GateGlowFadeDuration = 0.25f;

        public TMP_Text ToastText;

        [Header("Dawn Gate Arrival (setup atar)")]
        public Tilemap GateTilemap;
        public Vector3Int GateCell;
        public TileBase ClosedGateTile;
        public TileBase OpenGateTile;
        public Light2D GateGlow;
        [Min(0f)] public float GateOpenDelay = DefaultGateOpenDelay;
        [Min(0.1f)] public float GateOpenDuration = DefaultGateOpenDuration;
        public Color GateGlowColor = DefaultGateGlowColor;
        [Min(0f)] public float GateGlowIntensity = DefaultGateGlowIntensity;

        public bool IsGateOpen { get; private set; }
        public int GateOpenCount { get; private set; }
        public int DawnPresentationPlayCount { get; private set; }
        public int LastDisplayedGrowth { get; private set; }

        private const float CheckInterval = 0.2f;
        private float _nextCheckTime;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;
        private Sequence _toastSequence;
        private bool _hasObservedPhase;
        private bool _gatePresentationActive;
        private float _gatePresentationTime;

        private void OnEnable()
        {
            if (ToastText != null)
                ToastText.alpha = 0f;
            _hasObservedPhase = false;
            _gatePresentationActive = false;
            _gatePresentationTime = 0f;
            SetGateOpen(false);
        }

        private void OnDisable()
        {
            _toastSequence?.Kill();
            _toastSequence = null;
            _gatePresentationActive = false;
            SetGateOpen(false);
        }

        private void Update()
        {
            UpdateGatePresentation();

            if (Time.unscaledTime < _nextCheckTime)
                return;

            _nextCheckTime = Time.unscaledTime + CheckInterval;

            var gm = GameManager.Instance;
            if (gm == null)
                return;

            // IsMobileMode kullanilmaz (frame-arasi dalgalanma); cycle cache'i yeterli sinyal.
            var cycle = gm.ContinuousSiegeCycle;
            if (!cycle.Enabled)
            {
                _hasObservedPhase = false;
                _gatePresentationActive = false;
                SetGateOpen(false);
                return;
            }

            if (!_hasObservedPhase)
            {
                _lastPhase = cycle.Phase;
                _hasObservedPhase = true;
                return;
            }

            if (cycle.Phase == SiegeCyclePhase.Dawn && _lastPhase != SiegeCyclePhase.Dawn)
                ShowDawnToast(gm, cycle);
            else if (cycle.Phase != SiegeCyclePhase.Dawn && _lastPhase == SiegeCyclePhase.Dawn)
            {
                _gatePresentationActive = false;
                SetGateOpen(false);
            }

            _lastPhase = cycle.Phase;
        }

        private void ShowDawnToast(GameManager gm, ContinuousSiegeCycleData cycle)
        {
            int growth = gm.GetLastAcceptedPopulationArrivalCount();
            LastDisplayedGrowth = growth;
            DawnPresentationPlayCount++;
            BeginGateArrival(growth);

            if (ToastText == null)
                return;

            int dayNumber = Mathf.Max(1, cycle.CycleIndex + 1);
            ToastText.text = $"DAWN — DAY {dayNumber} SURVIVED  ·  +{growth} POP";

            _toastSequence?.Kill();
            ToastText.alpha = 0f;
            ToastText.gameObject.SetActive(true);
            _toastSequence = DOTween.Sequence()
                .Append(ToastText.DOFade(1f, 0.2f))
                .AppendInterval(2.4f)
                .Append(ToastText.DOFade(0f, 0.5f))
                .SetUpdate(true);
        }

        private void BeginGateArrival(int acceptedSurvivors)
        {
            _gatePresentationActive = acceptedSurvivors > 0;
            _gatePresentationTime = 0f;
            SetGateOpen(false);
        }

        private void UpdateGatePresentation()
        {
            if (!_gatePresentationActive || SimulationPauseService.IsPaused)
                return;

            _gatePresentationTime += Time.deltaTime;
            float openDelay = Mathf.Max(0f, GateOpenDelay);
            float openDuration = Mathf.Max(0.1f, GateOpenDuration);
            float openElapsed = _gatePresentationTime - openDelay;
            bool shouldOpen = openElapsed >= 0f && openElapsed < openDuration;
            SetGateOpen(shouldOpen);

            if (GateGlow != null && shouldOpen)
            {
                float fade = Mathf.Min(GateGlowFadeDuration, openDuration * 0.5f);
                float fadeIn = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(openElapsed / Mathf.Max(0.01f, fade)));
                float fadeOut = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01((openDuration - openElapsed) / Mathf.Max(0.01f, fade)));
                GateGlow.color = GateGlowColor;
                GateGlow.intensity = Mathf.Max(0f, GateGlowIntensity) * fadeIn * fadeOut;
            }

            if (_gatePresentationTime < openDelay + openDuration)
                return;

            _gatePresentationActive = false;
            SetGateOpen(false);
        }

        private void SetGateOpen(bool open)
        {
            if (GateTilemap != null && ClosedGateTile != null && OpenGateTile != null)
            {
                TileBase targetTile = open ? OpenGateTile : ClosedGateTile;
                if (GateTilemap.GetTile(GateCell) != targetTile)
                    GateTilemap.SetTile(GateCell, targetTile);
            }

            if (open && !IsGateOpen)
                GateOpenCount++;
            IsGateOpen = open;

            if (!open && GateGlow != null)
            {
                GateGlow.color = GateGlowColor;
                GateGlow.intensity = 0f;
            }
        }
    }
}
