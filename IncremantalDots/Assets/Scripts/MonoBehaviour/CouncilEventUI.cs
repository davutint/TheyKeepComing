using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Safak meclisi kartinin controller'i. Faz DAWN'a gecince GameManager.TryRollCouncilEvent
    /// cagrilir; event ciktiysa kompakt kart sol bolgede belirir (dawn odul toast'undan hafif
    /// gecikmeli), DAY boyunca acik kalir, DUSK girisinde secilmemisse expire olur.
    /// Oyun HICBIR zaman durmaz; kart savasi kapatmaz.
    /// </summary>
    public class CouncilEventUI : MonoBehaviour
    {
        [Header("Card")]
        public GameObject CouncilPanel;
        public TMP_Text CouncilTitleText;
        public TMP_Text CouncilBodyText;
        public Image CouncilTimerFill;
        public Button CouncilOptionAButton;
        public TMP_Text CouncilOptionAText;
        public Button CouncilOptionBButton;
        public TMP_Text CouncilOptionBText;

        [Header("Juice")]
        public AudioClip AppearClip;
        public AudioClip ChooseClip;
        [Tooltip("Dawn odul toast'u ile carpismasin diye kartin belirme gecikmesi (sn).")]
        public float AppearDelay = 1.2f;

        private const float PollInterval = 0.2f;
        private float _nextPollTime;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;
        private ComposedCouncilEvent _shownEvent;
        private float _windowEndsAt;
        private float _windowDuration = 1f;
        private bool _buttonsBound;
        private AudioSource _audio;
        private CanvasGroup _panelGroup;
        private Tween _appearTween;

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
            // GameManager.TryRollCouncilEvent notu). Cycle cache'i guvenilir sinyaldir.
            var cycle = gm.ContinuousSiegeCycle;
            if (!cycle.Enabled)
                return;

            // Faz gecisleri: Dawn'da roll, Dusk'ta expire
            if (cycle.Phase == SiegeCyclePhase.Dawn && _lastPhase != SiegeCyclePhase.Dawn)
                gm.TryRollCouncilEvent();
            else if (cycle.Phase == SiegeCyclePhase.Dusk && _lastPhase != SiegeCyclePhase.Dusk
                && gm.ActiveCouncilEvent != null)
                gm.ExpireCouncilEvent();
            _lastPhase = cycle.Phase;

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
            SetText(CouncilOptionAText, composed.OptionA.Label);
            SetText(CouncilOptionBText, composed.OptionB.Label);

            // Karar penceresi: kalan Dawn + tum Day (Dusk girisinde kapanir)
            float remaining;
            if (cycle.Phase == SiegeCyclePhase.Dawn)
                remaining = cycle.DawnDuration * (1f - cycle.PhaseProgress01) + cycle.DayDuration;
            else if (cycle.Phase == SiegeCyclePhase.Day)
                remaining = cycle.DayDuration * (1f - cycle.PhaseProgress01);
            else
                remaining = cycle.DayDuration;
            _windowDuration = Mathf.Max(1f, remaining);
            _windowEndsAt = Time.unscaledTime + _windowDuration;

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
            if (CouncilTimerFill != null)
                CouncilTimerFill.fillAmount = Mathf.Clamp01((_windowEndsAt - Time.unscaledTime) / _windowDuration);

            if (CouncilOptionAButton != null)
                CouncilOptionAButton.interactable = gm.CanAffordCouncilOption(_shownEvent.OptionA);
            if (CouncilOptionBButton != null)
                CouncilOptionBButton.interactable = gm.CanAffordCouncilOption(_shownEvent.OptionB);
        }

        // ---------------------------------------------------------------
        // Secim
        // ---------------------------------------------------------------
        private void HandleOptionA() => Choose(true, CouncilOptionAButton);
        private void HandleOptionB() => Choose(false, CouncilOptionBButton);

        private void Choose(bool optionA, Button source)
        {
            var gm = GameManager.Instance;
            if (gm == null || _shownEvent == null)
                return;

            if (gm.ChooseCouncilOption(optionA))
            {
                PlaySfx(ChooseClip, 0.85f);
                if (source != null)
                {
                    var rect = (RectTransform)source.transform;
                    rect.DOKill(true);
                    rect.DOPunchScale(Vector3.one * 0.06f, 0.16f, 7, 0.7f).SetUpdate(true);
                }
                // ActiveCouncilEvent null'a dustu; bir sonraki poll HideCard cagirir —
                // punch'in gorunmesi icin aninda kapatmiyoruz.
            }
            else if (source != null)
            {
                var rect = (RectTransform)source.transform;
                rect.DOKill(true);
                rect.DOShakeAnchorPos(0.2f, new Vector2(5f, 0f), 16, 90f, false, true).SetUpdate(true);
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
