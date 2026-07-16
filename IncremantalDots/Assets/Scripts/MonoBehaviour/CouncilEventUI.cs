using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Safak meclisi kartinin controller'i. Faz DAWN'a gecince
    /// GameManager.TryOpenRegularCouncilEvent cagrilir; scheduled regular event varsa kompakt
    /// kart sol bolgede belirir (dawn odul toast'undan hafif
    /// gecikmeli), DAY boyunca acik kalir, DUSK girisinde secilmemisse expire olur.
    /// Secimden sonra kart kapanmaz: 3 saniyeligine SONUC metnine donusur (promise -> choice
    /// -> consequence dongusunun son halkasi). Sureli etkiler HUD rozetinde gorunur; riskli
    /// gece secimi, gece baslarken toast'la hatirlatilir. Oyun HICBIR zaman durmaz.
    /// </summary>
    public class CouncilEventUI : MonoBehaviour
    {
        public event Action CouncilChoiceCommittedByPlayer;

        [Header("Card")]
        public GameObject CouncilPanel;
        public TMP_Text CouncilTitleText;
        public TMP_Text CouncilBodyText;
        public Image CouncilTimerFill;
        public TMP_Text CouncilTimerText;
        public Button CouncilOptionAButton;
        public TMP_Text CouncilOptionAText;
        public Button CouncilOptionBButton;
        public TMP_Text CouncilOptionBText;

        [Header("Consequence Feedback")]
        [Tooltip("Aktif sureli etkiler (uretim pakti / gece kehaneti) icin HUD rozeti.")]
        public TMP_Text CouncilEffectBadgeText;
        [Tooltip("Gece baslarken riskli/sakin gece hatirlatmasi (SiegeToastText paylasilir).")]
        public TMP_Text NightToastText;

        [Header("Juice")]
        public AudioClip AppearClip;
        public AudioClip ChooseClip;
        [Tooltip("Dawn odul toast'u ile carpismasin diye kartin belirme gecikmesi (sn).")]
        public float AppearDelay = 1.2f;

        private const float PollInterval = 0.2f;
        private const float OutcomeHoldSeconds = 3.4f;
        private const float ExpireHoldSeconds = 2.4f;
        private const string ExpireBody = "The moment passes. The council scatters to the walls, the matter unsettled.";
        private static readonly Color BadgeBoostColor = new Color(0.56f, 0.85f, 0.54f, 1f);
        private static readonly Color BadgeRiskColor = new Color(0.9f, 0.73f, 0.39f, 1f);

        private float _nextPollTime;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;
        private ComposedCouncilEvent _shownEvent;
        private float _windowDuration = 1f;
        private bool _outcomePlaying;
        private bool _buttonsBound;
        private AudioSource _audio;
        private CanvasGroup _panelGroup;
        private Tween _appearTween;
        private Tween _outcomeTween;

        public bool IsAwaitingPlayerChoice => _shownEvent != null
            && !_outcomePlaying
            && CouncilPanel != null
            && CouncilPanel.activeInHierarchy
            && _panelGroup != null
            && _panelGroup.interactable;

        public RectTransform ChoiceCardRect => CouncilPanel != null
            ? CouncilPanel.transform as RectTransform
            : null;

        private void OnEnable()
        {
            BindButtons();
            if (CouncilPanel != null && CouncilPanel.activeSelf)
                CouncilPanel.SetActive(false);
        }

        private void OnDisable()
        {
            UnbindButtons();
            _appearTween?.Kill();
            _appearTween = null;
            _outcomeTween?.Kill();
            _outcomeTween = null;
            _outcomePlaying = false;
            _shownEvent = null;
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextPollTime)
                return;

            _nextPollTime = Time.unscaledTime + PollInterval;

            var gm = GameManager.Instance;
            if (gm == null)
                return;

            // IsMobileMode KULLANILMAZ: _initialized frame-arasi dalgalanabiliyor (bkz.
            // GameManager.TryOpenRegularCouncilEvent notu). Cycle cache'i guvenilir sinyaldir.
            var cycle = gm.ContinuousSiegeCycle;
            if (!cycle.Enabled)
                return;

            // Faz gecisleri: Dawn'da scheduled open, Dusk'ta expire, Night'ta kehanet hatirlatmasi
            if (cycle.Phase == SiegeCyclePhase.Dawn && _lastPhase != SiegeCyclePhase.Dawn)
            {
                gm.TryOpenRegularCouncilEvent();
            }
            else if (cycle.Phase == SiegeCyclePhase.Dusk && _lastPhase != SiegeCyclePhase.Dusk
                && gm.ActiveCouncilEvent != null)
            {
                gm.ExpireCouncilEvent();
                if (_shownEvent != null && !_outcomePlaying)
                    ShowOutcome(ExpireBody, ExpireHoldSeconds);
            }
            else if (cycle.Phase == SiegeCyclePhase.Night && _lastPhase != SiegeCyclePhase.Night)
            {
                TryShowNightToast(gm);
            }

            _lastPhase = cycle.Phase;

            RefreshEffectBadge(gm, cycle);

            // Sonuc metni gosterilirken kart yasam dongusu dondurulur (kapanisi tween yapar)
            if (_outcomePlaying)
                return;

            var active = gm.ActiveCouncilEvent;
            if (active != null && _shownEvent != active)
                ShowCard(gm, active, cycle);
            else if (active == null && _shownEvent != null)
                HideCard();

            if (_shownEvent != null)
                RefreshCard(gm);
        }

        // ---------------------------------------------------------------
        // Kart yasam dongusu
        // ---------------------------------------------------------------
        private void ShowCard(GameManager gm, ComposedCouncilEvent composed, ContinuousSiegeCycleData cycle)
        {
            _shownEvent = composed;
            if (CouncilPanel == null)
                return;

            SetText(CouncilTitleText, composed.Title);
            SetText(CouncilBodyText, composed.Body);
            SetCardInteractiveElements(true);

            // Karar penceresi: kalan Dawn + tum Day (Dusk girisinde kapanir)
            float remaining = CouncilDecisionWindowUtility.GetRemainingSeconds(cycle);
            _windowDuration = Mathf.Max(1f, remaining);
            RefreshCard(gm);

            _appearTween?.Kill();
            CouncilPanel.SetActive(true);
            var group = GetPanelGroup();
            var rect = (RectTransform)CouncilPanel.transform;
            group.alpha = 0f;
            group.interactable = false;
            var basePos = rect.anchoredPosition;
            rect.anchoredPosition = basePos + new Vector2(0f, -24f);
            _appearTween = DOTween.Sequence()
                .AppendInterval(AppearDelay)
                .AppendCallback(() => PlaySfx(AppearClip, 0.7f))
                .Append(group.DOFade(1f, 0.25f))
                .Join(rect.DOAnchorPos(basePos, 0.3f).SetEase(Ease.OutQuad))
                .AppendCallback(() => group.interactable = true)
                .SetUpdate(true)
                .OnKill(() => rect.anchoredPosition = basePos);
        }

        private void HideCard()
        {
            _shownEvent = null;
            _appearTween?.Kill();
            if (CouncilPanel == null || !CouncilPanel.activeSelf)
                return;

            var group = GetPanelGroup();
            group.interactable = false;
            group.DOKill();
            group.DOFade(0f, 0.25f)
                .SetUpdate(true)
                .OnComplete(() => CouncilPanel.SetActive(false));
        }

        private void RefreshCard(GameManager gm)
        {
            float remaining = CouncilDecisionWindowUtility.GetRemainingSeconds(gm.ContinuousSiegeCycle);
            if (CouncilTimerFill != null)
                CouncilTimerFill.fillAmount = Mathf.Clamp01(remaining / _windowDuration);
            SetText(CouncilTimerText, CouncilDecisionWindowUtility.FormatCountdown(remaining));

            CouncilOptionPresentation optionA =
                gm.GetCouncilOptionPresentation(_shownEvent.OptionA);
            CouncilOptionPresentation optionB =
                gm.GetCouncilOptionPresentation(_shownEvent.OptionB);
            SetText(CouncilOptionAText, optionA.RichText);
            SetText(CouncilOptionBText, optionB.RichText);
            if (CouncilOptionAButton != null)
                CouncilOptionAButton.interactable = optionA.CanApplyExactly;
            if (CouncilOptionBButton != null)
                CouncilOptionBButton.interactable = optionB.CanApplyExactly;
        }

        // ---------------------------------------------------------------
        // Secim + sonuc ani
        // ---------------------------------------------------------------
        private void HandleOptionA() => Choose(true, CouncilOptionAButton);
        private void HandleOptionB() => Choose(false, CouncilOptionBButton);

        private void Choose(bool optionA, Button source)
        {
            var gm = GameManager.Instance;
            if (gm == null || _shownEvent == null || _outcomePlaying)
                return;

            if (gm.ChooseCouncilOption(optionA))
            {
                CouncilChoiceCommittedByPlayer?.Invoke();
                PlaySfx(ChooseClip, 0.85f);
                if (source != null)
                {
                    var rect = (RectTransform)source.transform;
                    rect.DOKill(true);
                    rect.DOPunchScale(Vector3.one * 0.06f, 0.16f, 7, 0.7f).SetUpdate(true);
                }

                string outcome = optionA ? _shownEvent.OutcomeA : _shownEvent.OutcomeB;
                ShowOutcome(string.IsNullOrEmpty(outcome) ? "It is done." : outcome, OutcomeHoldSeconds);
            }
            else if (source != null)
            {
                var rect = (RectTransform)source.transform;
                rect.DOKill(true);
                rect.DOShakeAnchorPos(0.2f, new Vector2(5f, 0f), 16, 90f, false, true).SetUpdate(true);
            }
        }

        /// <summary>Kart sonuc metnine donusur, bir sure gosterir, sonra kapanir.</summary>
        private void ShowOutcome(string text, float holdSeconds)
        {
            if (CouncilPanel == null || !CouncilPanel.activeSelf)
            {
                _shownEvent = null;
                return;
            }

            _outcomePlaying = true;
            _appearTween?.Kill();
            _outcomeTween?.Kill();

            SetText(CouncilBodyText, text);
            SetCardInteractiveElements(false);
            var group = GetPanelGroup();
            group.interactable = false;
            group.alpha = 1f;

            _outcomeTween = DOTween.Sequence()
                .AppendInterval(holdSeconds)
                .Append(group.DOFade(0f, 0.35f))
                .AppendCallback(CloseAfterOutcome)
                .SetUpdate(true);
        }

        private void CloseAfterOutcome()
        {
            if (CouncilPanel != null)
                CouncilPanel.SetActive(false);
            SetCardInteractiveElements(true); // sonraki kart icin butonlari geri getir
            if (_panelGroup != null)
                _panelGroup.alpha = 1f;
            _outcomePlaying = false;
            _shownEvent = null;
        }

        private void SetCardInteractiveElements(bool visible)
        {
            if (CouncilOptionAButton != null)
                CouncilOptionAButton.gameObject.SetActive(visible);
            if (CouncilOptionBButton != null)
                CouncilOptionBButton.gameObject.SetActive(visible);
            if (CouncilTimerFill != null)
                CouncilTimerFill.gameObject.SetActive(visible);
            if (CouncilTimerText != null)
                CouncilTimerText.gameObject.SetActive(visible);
        }

        // ---------------------------------------------------------------
        // Sonuc gorunurlugu: aktif etki rozeti + gece hatirlatmasi
        // ---------------------------------------------------------------
        private void RefreshEffectBadge(GameManager gm, ContinuousSiegeCycleData cycle)
        {
            if (CouncilEffectBadgeText == null)
                return;

            var evt = gm.EconomyEvent;
            int day = Mathf.Max(1, cycle.CycleIndex + 1);
            string text = string.Empty;

            if (evt.ProductionBonusExpiresAfterWave > 0 && evt.ProductionBonusMultiplier > 0f
                && !Mathf.Approximately(evt.ProductionBonusMultiplier, 1f))
            {
                int pct = Mathf.RoundToInt((evt.ProductionBonusMultiplier - 1f) * 100f);
                int daysLeft = Mathf.Max(1, evt.ProductionBonusExpiresAfterWave - day);
                string res = ResourceName(evt.ProductionBonusResource);
                text = pct > 0
                    ? $"<color=#8FD98A>PACT — {res} +{pct}%  ·  {daysLeft}d left</color>"
                    : $"<color=#E08A7A>HARDSHIP — {res} {pct}%  ·  {daysLeft}d left</color>";
            }

            if (evt.NightSpawnExpiresAfterWave > 0 && evt.NextNightSpawnMultiplier > 0f
                && !Mathf.Approximately(evt.NextNightSpawnMultiplier, 1f))
            {
                int pct = Mathf.RoundToInt((evt.NextNightSpawnMultiplier - 1f) * 100f);
                string line = pct > 0
                    ? $"<color=#E5B963>OMEN — the horde comes harder tonight (+{pct}%)</color>"
                    : $"<color=#8FD98A>QUIET — a calmer night ahead ({pct}%)</color>";
                text = string.IsNullOrEmpty(text) ? line : text + "\n" + line;
            }

            SetText(CouncilEffectBadgeText, text);
        }

        private void TryShowNightToast(GameManager gm)
        {
            if (NightToastText == null)
                return;

            var evt = gm.EconomyEvent;
            if (evt.NightSpawnExpiresAfterWave <= 0 || evt.NextNightSpawnMultiplier <= 0f
                || Mathf.Approximately(evt.NextNightSpawnMultiplier, 1f))
                return;

            int pct = Mathf.RoundToInt((evt.NextNightSpawnMultiplier - 1f) * 100f);
            if (pct > 0)
            {
                NightToastText.text = $"The noise carried. They come harder tonight (+{pct}%).";
                NightToastText.color = BadgeRiskColor;
            }
            else
            {
                NightToastText.text = $"The fires did their work. A quieter night ({pct}%).";
                NightToastText.color = BadgeBoostColor;
            }

            NightToastText.DOKill();
            var faded = NightToastText.color;
            faded.a = 0f;
            NightToastText.color = faded;
            NightToastText.gameObject.SetActive(true);
            DOTween.Sequence()
                .Append(NightToastText.DOFade(1f, 0.3f))
                .AppendInterval(3.2f)
                .Append(NightToastText.DOFade(0f, 0.5f))
                .SetUpdate(true)
                .SetTarget(NightToastText);
        }

        private static string ResourceName(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return "STONE";
                case EconomyFocusType.Iron: return "IRON";
                case EconomyFocusType.Food: return "FOOD";
                case EconomyFocusType.Wood: return "WOOD";
                default: return "SUPPLIES";
            }
        }

        // ---------------------------------------------------------------
        // Yardimcilar
        // ---------------------------------------------------------------
        private void BindButtons()
        {
            if (_buttonsBound)
                return;

            _buttonsBound = true;
            if (CouncilOptionAButton != null)
            {
                CouncilOptionAButton.onClick.RemoveListener(HandleOptionA);
                CouncilOptionAButton.onClick.AddListener(HandleOptionA);
            }
            if (CouncilOptionBButton != null)
            {
                CouncilOptionBButton.onClick.RemoveListener(HandleOptionB);
                CouncilOptionBButton.onClick.AddListener(HandleOptionB);
            }
        }

        private void UnbindButtons()
        {
            _buttonsBound = false;
            if (CouncilOptionAButton != null)
                CouncilOptionAButton.onClick.RemoveListener(HandleOptionA);
            if (CouncilOptionBButton != null)
                CouncilOptionBButton.onClick.RemoveListener(HandleOptionB);
        }

        private CanvasGroup GetPanelGroup()
        {
            if (_panelGroup == null && CouncilPanel != null)
            {
                _panelGroup = CouncilPanel.GetComponent<CanvasGroup>();
                if (_panelGroup == null)
                    _panelGroup = CouncilPanel.AddComponent<CanvasGroup>();
            }
            return _panelGroup;
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            if (_audio == null)
            {
                _audio = GetComponent<AudioSource>();
                if (_audio == null)
                    _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
            }

            _audio.PlayOneShot(clip, volume);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value)
                text.text = value;
        }
    }
}
