using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    internal static class FirstRunOnboardingRules
    {
        public static bool ShouldShowWorkerRatioStep(
            bool completed,
            bool mobileWorkerEconomyEnabled,
            bool isGameOver,
            bool hasContinuousCycle,
            int cycleIndex,
            SiegeCyclePhase phase)
        {
            return !completed
                && mobileWorkerEconomyEnabled
                && !isGameOver
                && hasContinuousCycle
                && cycleIndex == 0
                && phase == SiegeCyclePhase.Day;
        }
    }

    /// <summary>
    /// Ilk kosu onboarding sunum sahibidir. Gameplay transaction'i yapmaz; yalniz gercek
    /// player action event'lerini dinler, non-modal hint/pulse gosterir ve tamamlanan adimi
    /// canonical MetaProgression tutorial flag API'sine yazar.
    /// </summary>
    public sealed class FirstRunOnboardingUI : MonoBehaviour
    {
        public const string WorkerRatioFlagId = "tutorial.v1.worker_ratio";
        public const string WorkerRatioHint = "ADJUST A WORKER TARGET RATIO.";

        [Header("Worker Ratio Step")]
        public WorkerEconomyDrawerUI WorkerDrawer;
        public GameObject HintPanel;
        public TMP_Text HintText;
        public RectTransform PulseFrame;
        public Image PulseImage;
        public Outline PulseOutline;

        [Header("Presentation")]
        [Min(0.1f)] public float PulseSpeed = 1.6f;
        [Min(0f)] public float PulsePaddingMin = 8f;
        [Min(0f)] public float PulsePaddingMax = 16f;

        private WorkerEconomyDrawerUI _subscribedDrawer;
        private RectTransform _activePulseTarget;
        private bool _workerRatioStepVisible;
        private bool _persistenceWarningLogged;

        public bool IsWorkerRatioStepVisible => _workerRatioStepVisible;
        public RectTransform ActivePulseTarget => _activePulseTarget;

        private void OnEnable()
        {
            ResolveMissingReferences();
            BindWorkerDrawer();
            SetWorkerRatioPresentation(false, null);
        }

        private void OnDisable()
        {
            UnbindWorkerDrawer();
            SetWorkerRatioPresentation(false, null);
        }

        private void Update()
        {
            if (WorkerDrawer == null)
            {
                ResolveMissingReferences();
                BindWorkerDrawer();
            }

            bool completed = MetaProgression.HasTutorialFlag(WorkerRatioFlagId);
            GameManager gm = GameManager.Instance;
            ContinuousSiegeCycleData cycle = default;
            bool hasCycle = gm != null && gm.TryGetContinuousSiegeCycle(out cycle);
            bool shouldShow = FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                completed,
                gm != null && gm.IsMobilePopulationEconomyEnabled(),
                gm != null && gm.GameState.IsGameOver,
                hasCycle,
                hasCycle ? cycle.CycleIndex : -1,
                hasCycle ? cycle.Phase : SiegeCyclePhase.Day);

            RectTransform target = null;
            if (shouldShow && WorkerDrawer != null)
            {
                target = WorkerDrawer.IsOpen && WorkerDrawer.WoodWorkerTargetPlus10Button != null
                    ? WorkerDrawer.WoodWorkerTargetPlus10Button.GetComponent<RectTransform>()
                    : WorkerDrawer.WorkerDrawerToggleButton != null
                        ? WorkerDrawer.WorkerDrawerToggleButton.GetComponent<RectTransform>()
                        : null;
            }

            SetWorkerRatioPresentation(shouldShow && target != null, target);
            if (_workerRatioStepVisible)
                UpdatePulsePresentation();
        }

        private void ResolveMissingReferences()
        {
            WorkerDrawer ??= GetComponent<WorkerEconomyDrawerUI>();
            HintPanel ??= FindGameObject("OnboardingHintPanel");
            HintText ??= FindComponent<TMP_Text>("OnboardingHintText");
            PulseFrame ??= FindComponent<RectTransform>("OnboardingPulseFrame");
            PulseImage ??= PulseFrame != null ? PulseFrame.GetComponent<Image>() : null;
            PulseOutline ??= PulseFrame != null ? PulseFrame.GetComponent<Outline>() : null;
        }

        private void BindWorkerDrawer()
        {
            if (_subscribedDrawer == WorkerDrawer)
                return;

            UnbindWorkerDrawer();
            _subscribedDrawer = WorkerDrawer;
            if (_subscribedDrawer != null)
                _subscribedDrawer.WorkerTargetRatioChangedByPlayer += HandleWorkerTargetRatioChanged;
        }

        private void UnbindWorkerDrawer()
        {
            if (_subscribedDrawer != null)
                _subscribedDrawer.WorkerTargetRatioChangedByPlayer -= HandleWorkerTargetRatioChanged;
            _subscribedDrawer = null;
        }

        private void HandleWorkerTargetRatioChanged(EconomyFocusType resource)
        {
            if (MetaProgression.HasTutorialFlag(WorkerRatioFlagId)
                || MetaProgression.SetTutorialFlag(WorkerRatioFlagId, true))
            {
                _persistenceWarningLogged = false;
                SetWorkerRatioPresentation(false, null);
                return;
            }

            if (_persistenceWarningLogged)
                return;

            _persistenceWarningLogged = true;
            Debug.LogWarning("[FirstRunOnboardingUI] Worker ratio tutorial flag durable yazilamadi.");
        }

        private void SetWorkerRatioPresentation(bool visible, RectTransform target)
        {
            _workerRatioStepVisible = visible;
            _activePulseTarget = visible ? target : null;

            if (HintPanel != null && HintPanel.activeSelf != visible)
                HintPanel.SetActive(visible);
            if (PulseFrame != null && PulseFrame.gameObject.activeSelf != visible)
                PulseFrame.gameObject.SetActive(visible);

            if (!visible)
                return;

            if (HintText != null && HintText.text != WorkerRatioHint)
                HintText.text = WorkerRatioHint;

            RectTransform hintRect = HintPanel != null
                ? HintPanel.GetComponent<RectTransform>()
                : null;
            if (hintRect != null)
            {
                hintRect.anchoredPosition = WorkerDrawer != null && WorkerDrawer.IsOpen
                    ? new Vector2(24f, 554f)
                    : new Vector2(24f, 96f);
            }
        }

        private void UpdatePulsePresentation()
        {
            if (PulseFrame == null || _activePulseTarget == null)
                return;

            RectTransform pulseParent = PulseFrame.parent as RectTransform;
            if (pulseParent == null)
                return;

            var worldCorners = new Vector3[4];
            _activePulseTarget.GetWorldCorners(worldCorners);
            Vector3 bottomLeft = pulseParent.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = pulseParent.InverseTransformPoint(worldCorners[2]);

            float pulse01 = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed * Mathf.PI * 2f);
            float padding = Mathf.Lerp(PulsePaddingMin, PulsePaddingMax, pulse01);
            Vector2 size = new Vector2(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y)
                + Vector2.one * padding;
            Vector2 center = (bottomLeft + topRight) * 0.5f;

            PulseFrame.anchorMin = new Vector2(0.5f, 0.5f);
            PulseFrame.anchorMax = new Vector2(0.5f, 0.5f);
            PulseFrame.pivot = new Vector2(0.5f, 0.5f);
            PulseFrame.anchoredPosition = center;
            PulseFrame.sizeDelta = size;
            PulseFrame.localScale = Vector3.one;

            if (PulseImage != null)
                PulseImage.color = new Color(1f, 0.64f, 0.16f, Mathf.Lerp(0.06f, 0.18f, pulse01));
            if (PulseOutline != null)
                PulseOutline.effectColor = new Color(1f, 0.68f, 0.20f, Mathf.Lerp(0.45f, 0.95f, pulse01));
        }

        private GameObject FindGameObject(string objectName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.gameObject.name == objectName)
                    return candidate.gameObject;
            }

            return null;
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            T[] components = GetComponentsInChildren<T>(true);
            foreach (T candidate in components)
            {
                if (candidate.gameObject.name == objectName)
                    return candidate;
            }

            return null;
        }
    }
}
