using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    internal enum FirstRunOnboardingStep
    {
        None = 0,
        WorkerRatio = 1,
        BasicArcher = 2,
        LowAmmo = 3,
        HeartEntry = 4,
        HeartPause = 5
    }

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

        public static bool ShouldShowBasicArcherStep(
            bool completed,
            bool mobileWorkerEconomyEnabled,
            bool isGameOver,
            bool canBuyBasicArcher)
        {
            return !completed
                && mobileWorkerEconomyEnabled
                && !isGameOver
                && canBuyBasicArcher;
        }

        public static bool ShouldShowLowAmmoStep(
            bool completed,
            bool mobileWorkerEconomyEnabled,
            bool isGameOver,
            int current,
            int capacity,
            int thresholdPercent)
        {
            if (completed
                || !mobileWorkerEconomyEnabled
                || isGameOver
                || capacity <= 0
                || thresholdPercent <= 0)
            {
                return false;
            }

            int clampedThreshold = Mathf.Clamp(thresholdPercent, 1, 100);
            int clampedCurrent = Mathf.Clamp(current, 0, capacity);
            return (long)clampedCurrent * 100L <= (long)capacity * clampedThreshold;
        }

        public static bool ShouldShowHeartEntryStep(
            bool completed,
            bool mobileWorkerEconomyEnabled,
            bool isGameOver,
            long graveEssence)
        {
            return !completed
                && mobileWorkerEconomyEnabled
                && !isGameOver
                && graveEssence > 0L;
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
        public const string BasicArcherFlagId = "tutorial.v1.basic_archer";
        public const string BasicArcherHint = "RECRUIT A BASIC ARCHER.";
        public const string LowAmmoFlagId = "tutorial.v1.low_ammo";
        public const string LowAmmoHint = "RESTOCK YOUR ARROWS.";
        public const int LowAmmoThresholdPercent = 25;
        public const string HeartEntryFlagId = "tutorial.v1.heart";
        public const string HeartEntryHint = "OPEN THE CASTLE HEART.";
        public const string HeartPauseHint = "THE CASTLE HEART FULLY PAUSES THE BATTLE.";
        public const int HeartPauseHintSortingOrder = 260;

        [Header("Onboarding Owners")]
        public WorkerEconomyDrawerUI WorkerDrawer;
        public MarketUI ArcherMarket;
        public ArrowSupplyUI AmmoSupply;
        public HeartScreenUI CastleHeart;

        [Header("Shared Presentation")]
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
        private MarketUI _subscribedMarket;
        private ArrowSupplyUI _subscribedAmmoSupply;
        private HeartScreenUI _subscribedCastleHeart;
        private RectTransform _activePulseTarget;
        private FirstRunOnboardingStep _activeStep;
        private bool _heartPauseTeachingActive;
        private bool _persistenceWarningLogged;

        public bool IsWorkerRatioStepVisible => _activeStep == FirstRunOnboardingStep.WorkerRatio;
        public bool IsBasicArcherStepVisible => _activeStep == FirstRunOnboardingStep.BasicArcher;
        public bool IsLowAmmoStepVisible => _activeStep == FirstRunOnboardingStep.LowAmmo;
        public bool IsHeartEntryStepVisible => _activeStep == FirstRunOnboardingStep.HeartEntry;
        public bool IsHeartPauseStepVisible => _activeStep == FirstRunOnboardingStep.HeartPause;
        public RectTransform ActivePulseTarget => _activePulseTarget;

        private void OnEnable()
        {
            ResolveMissingReferences();
            BindWorkerDrawer();
            BindArcherMarket();
            BindAmmoSupply();
            BindCastleHeart();
            SetPresentation(FirstRunOnboardingStep.None, null);
        }

        private void OnDisable()
        {
            UnbindWorkerDrawer();
            UnbindArcherMarket();
            UnbindAmmoSupply();
            UnbindCastleHeart();
            _heartPauseTeachingActive = false;
            SetPresentation(FirstRunOnboardingStep.None, null);
        }

        private void Update()
        {
            if (WorkerDrawer == null || ArcherMarket == null || AmmoSupply == null || CastleHeart == null)
            {
                ResolveMissingReferences();
                BindWorkerDrawer();
                BindArcherMarket();
                BindAmmoSupply();
                BindCastleHeart();
            }

            bool workerRatioCompleted = MetaProgression.HasTutorialFlag(WorkerRatioFlagId);
            GameManager gm = GameManager.Instance;
            ContinuousSiegeCycleData cycle = default;
            bool hasCycle = gm != null && gm.TryGetContinuousSiegeCycle(out cycle);
            bool shouldShowWorkerRatio = FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                workerRatioCompleted,
                gm != null && gm.IsMobilePopulationEconomyEnabled(),
                gm != null && gm.GameState.IsGameOver,
                hasCycle,
                hasCycle ? cycle.CycleIndex : -1,
                hasCycle ? cycle.Phase : SiegeCyclePhase.Day);

            bool basicArcherCompleted = MetaProgression.HasTutorialFlag(BasicArcherFlagId);
            bool canBuyBasicArcher = !basicArcherCompleted
                && gm != null
                && gm.CanBuyArcher(ArcherType.Basic);
            bool shouldShowBasicArcher = !shouldShowWorkerRatio
                && FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                    basicArcherCompleted,
                    gm != null && gm.IsMobilePopulationEconomyEnabled(),
                    gm != null && gm.GameState.IsGameOver,
                    canBuyBasicArcher);

            bool lowAmmoCompleted = MetaProgression.HasTutorialFlag(LowAmmoFlagId);
            bool shouldEvaluateLowAmmo = !lowAmmoCompleted && gm != null;
            int arrowCapacity = shouldEvaluateLowAmmo ? gm.GetArrowCapacity() : 0;
            int arrowCurrent = shouldEvaluateLowAmmo ? gm.ArrowSupply.Current : 0;
            bool shouldShowLowAmmo = !shouldShowWorkerRatio
                && !shouldShowBasicArcher
                && FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                    lowAmmoCompleted,
                    gm != null && gm.IsMobilePopulationEconomyEnabled(),
                    gm != null && gm.GameState.IsGameOver,
                    arrowCurrent,
                    arrowCapacity,
                    LowAmmoThresholdPercent);

            bool heartEntryCompleted = MetaProgression.HasTutorialFlag(HeartEntryFlagId);
            bool shouldEvaluateHeartEntry = !heartEntryCompleted
                && !shouldShowWorkerRatio
                && !shouldShowBasicArcher
                && !shouldShowLowAmmo
                && gm != null;
            long graveEssence = shouldEvaluateHeartEntry ? gm.GraveEssenceAmount : 0L;
            bool shouldShowHeartEntry = shouldEvaluateHeartEntry
                && FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                    heartEntryCompleted,
                    gm.IsMobilePopulationEconomyEnabled(),
                    gm.GameState.IsGameOver,
                    graveEssence);

            FirstRunOnboardingStep step = FirstRunOnboardingStep.None;
            RectTransform target = null;
            bool showPulse = true;
            if (_heartPauseTeachingActive && CastleHeart != null && CastleHeart.IsOpen)
            {
                step = FirstRunOnboardingStep.HeartPause;
                showPulse = false;
            }
            else if (shouldShowWorkerRatio && WorkerDrawer != null)
            {
                step = FirstRunOnboardingStep.WorkerRatio;
                target = WorkerDrawer.IsOpen && WorkerDrawer.WoodWorkerTargetPlus10Button != null
                    ? WorkerDrawer.WoodWorkerTargetPlus10Button.GetComponent<RectTransform>()
                    : WorkerDrawer.WorkerDrawerToggleButton != null
                        ? WorkerDrawer.WorkerDrawerToggleButton.GetComponent<RectTransform>()
                        : null;
            }
            else if (shouldShowBasicArcher && ArcherMarket != null)
            {
                step = FirstRunOnboardingStep.BasicArcher;
                Button buyButton = ArcherMarket.GetArcherBuyButton(ArcherType.Basic);
                target = ArcherMarket.IsDrawerOpen && buyButton != null
                    ? buyButton.GetComponent<RectTransform>()
                    : ArcherMarket.DrawerToggleButton != null
                        ? ArcherMarket.DrawerToggleButton.GetComponent<RectTransform>()
                        : null;
            }
            else if (shouldShowLowAmmo && AmmoSupply != null)
            {
                step = FirstRunOnboardingStep.LowAmmo;
                target = AmmoSupply.ToggleButton != null
                    ? AmmoSupply.ToggleButton.GetComponent<RectTransform>()
                    : null;
            }
            else if (shouldShowHeartEntry && CastleHeart != null)
            {
                step = FirstRunOnboardingStep.HeartEntry;
                target = CastleHeart.HeartOpenButton != null
                    ? CastleHeart.HeartOpenButton.GetComponent<RectTransform>()
                    : null;
            }

            bool canPresent = step == FirstRunOnboardingStep.HeartPause || target != null;
            SetPresentation(canPresent ? step : FirstRunOnboardingStep.None, target, showPulse);
            if (_activePulseTarget != null)
                UpdatePulsePresentation();
        }

        private void ResolveMissingReferences()
        {
            WorkerDrawer ??= GetComponent<WorkerEconomyDrawerUI>();
            ArcherMarket ??= GetComponent<MarketUI>();
            AmmoSupply ??= GetComponent<ArrowSupplyUI>();
            CastleHeart ??= GetComponent<HeartScreenUI>();
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

        private void BindArcherMarket()
        {
            if (_subscribedMarket == ArcherMarket)
                return;

            UnbindArcherMarket();
            _subscribedMarket = ArcherMarket;
            if (_subscribedMarket != null)
                _subscribedMarket.ArcherPurchasedByPlayer += HandleArcherPurchased;
        }

        private void UnbindArcherMarket()
        {
            if (_subscribedMarket != null)
                _subscribedMarket.ArcherPurchasedByPlayer -= HandleArcherPurchased;
            _subscribedMarket = null;
        }

        private void BindAmmoSupply()
        {
            if (_subscribedAmmoSupply == AmmoSupply)
                return;

            UnbindAmmoSupply();
            _subscribedAmmoSupply = AmmoSupply;
            if (_subscribedAmmoSupply != null)
                _subscribedAmmoSupply.ArrowRefillPurchasedByPlayer += HandleArrowRefillPurchased;
        }

        private void UnbindAmmoSupply()
        {
            if (_subscribedAmmoSupply != null)
                _subscribedAmmoSupply.ArrowRefillPurchasedByPlayer -= HandleArrowRefillPurchased;
            _subscribedAmmoSupply = null;
        }

        private void BindCastleHeart()
        {
            if (_subscribedCastleHeart == CastleHeart)
                return;

            UnbindCastleHeart();
            _subscribedCastleHeart = CastleHeart;
            if (_subscribedCastleHeart == null)
                return;

            _subscribedCastleHeart.HeartOpenedByPlayer += HandleHeartOpenedByPlayer;
            _subscribedCastleHeart.HeartClosedByPlayer += HandleHeartClosedByPlayer;
        }

        private void UnbindCastleHeart()
        {
            if (_subscribedCastleHeart != null)
            {
                _subscribedCastleHeart.HeartOpenedByPlayer -= HandleHeartOpenedByPlayer;
                _subscribedCastleHeart.HeartClosedByPlayer -= HandleHeartClosedByPlayer;
            }

            _subscribedCastleHeart = null;
        }

        private void HandleWorkerTargetRatioChanged(EconomyFocusType resource)
        {
            if (MetaProgression.HasTutorialFlag(WorkerRatioFlagId)
                || MetaProgression.SetTutorialFlag(WorkerRatioFlagId, true))
            {
                _persistenceWarningLogged = false;
                SetPresentation(FirstRunOnboardingStep.None, null);
                return;
            }

            if (_persistenceWarningLogged)
                return;

            _persistenceWarningLogged = true;
            Debug.LogWarning("[FirstRunOnboardingUI] Worker ratio tutorial flag durable yazilamadi.");
        }

        private void HandleArcherPurchased(ArcherType type)
        {
            if (type != ArcherType.Basic)
                return;

            if (MetaProgression.HasTutorialFlag(BasicArcherFlagId)
                || MetaProgression.SetTutorialFlag(BasicArcherFlagId, true))
            {
                _persistenceWarningLogged = false;
                SetPresentation(FirstRunOnboardingStep.None, null);
                return;
            }

            if (_persistenceWarningLogged)
                return;

            _persistenceWarningLogged = true;
            Debug.LogWarning("[FirstRunOnboardingUI] Basic Archer tutorial flag durable yazilamadi.");
        }

        private void HandleArrowRefillPurchased()
        {
            if (MetaProgression.HasTutorialFlag(LowAmmoFlagId)
                || MetaProgression.SetTutorialFlag(LowAmmoFlagId, true))
            {
                _persistenceWarningLogged = false;
                SetPresentation(FirstRunOnboardingStep.None, null);
                return;
            }

            if (_persistenceWarningLogged)
                return;

            _persistenceWarningLogged = true;
            Debug.LogWarning("[FirstRunOnboardingUI] Low ammo tutorial flag durable yazilamadi.");
        }

        private void HandleHeartOpenedByPlayer()
        {
            GameManager gm = GameManager.Instance;
            if (MetaProgression.HasTutorialFlag(HeartEntryFlagId)
                || gm == null
                || gm.GameState.IsGameOver
                || gm.GraveEssenceAmount <= 0L)
            {
                return;
            }

            _heartPauseTeachingActive = true;
        }

        private void HandleHeartClosedByPlayer()
        {
            if (!_heartPauseTeachingActive)
                return;

            _heartPauseTeachingActive = false;
            if (MetaProgression.HasTutorialFlag(HeartEntryFlagId)
                || MetaProgression.SetTutorialFlag(HeartEntryFlagId, true))
            {
                _persistenceWarningLogged = false;
                SetPresentation(FirstRunOnboardingStep.None, null);
                return;
            }

            if (_persistenceWarningLogged)
                return;

            _persistenceWarningLogged = true;
            Debug.LogWarning("[FirstRunOnboardingUI] Castle Heart tutorial flag durable yazilamadi.");
        }

        private void SetPresentation(
            FirstRunOnboardingStep step,
            RectTransform target,
            bool showPulse = true)
        {
            bool visible = step != FirstRunOnboardingStep.None;
            bool heartPauseStep = step == FirstRunOnboardingStep.HeartPause;
            _activeStep = step;
            _activePulseTarget = visible && showPulse ? target : null;

            if (HintPanel != null && HintPanel.activeSelf != visible)
                HintPanel.SetActive(visible);
            bool pulseVisible = visible && showPulse && target != null;
            if (PulseFrame != null && PulseFrame.gameObject.activeSelf != pulseVisible)
                PulseFrame.gameObject.SetActive(pulseVisible);

            Canvas hintCanvas = HintPanel != null ? HintPanel.GetComponent<Canvas>() : null;
            if (hintCanvas != null)
            {
                int sortingOrder = heartPauseStep ? HeartPauseHintSortingOrder : 0;
                if (hintCanvas.overrideSorting != heartPauseStep)
                    hintCanvas.overrideSorting = heartPauseStep;
                if (hintCanvas.sortingOrder != sortingOrder)
                    hintCanvas.sortingOrder = sortingOrder;
            }

            if (!visible)
                return;

            string hint = step switch
            {
                FirstRunOnboardingStep.BasicArcher => BasicArcherHint,
                FirstRunOnboardingStep.LowAmmo => LowAmmoHint,
                FirstRunOnboardingStep.HeartEntry => HeartEntryHint,
                FirstRunOnboardingStep.HeartPause => HeartPauseHint,
                _ => WorkerRatioHint
            };
            if (HintText != null && HintText.text != hint)
                HintText.text = hint;

            RectTransform hintRect = HintPanel != null
                ? HintPanel.GetComponent<RectTransform>()
                : null;
            if (hintRect != null)
            {
                bool archerStep = step == FirstRunOnboardingStep.BasicArcher;
                bool ammoStep = step == FirstRunOnboardingStep.LowAmmo;
                bool heartEntryStep = step == FirstRunOnboardingStep.HeartEntry;
                hintRect.anchorMin = heartPauseStep
                    ? new Vector2(0.5f, 1f)
                    : archerStep || heartEntryStep
                    ? new Vector2(1f, 0f)
                    : ammoStep ? new Vector2(0f, 1f) : Vector2.zero;
                hintRect.anchorMax = hintRect.anchorMin;
                hintRect.pivot = heartPauseStep
                    ? new Vector2(0.5f, 1f)
                    : archerStep || heartEntryStep
                    ? new Vector2(1f, 0f)
                    : ammoStep ? new Vector2(0f, 1f) : Vector2.zero;
                hintRect.anchoredPosition = heartPauseStep
                    ? new Vector2(0f, -88f)
                    : archerStep
                        ? new Vector2(-24f, ArcherMarket != null && ArcherMarket.IsDrawerOpen ? 522f : 96f)
                    : heartEntryStep
                        ? new Vector2(-24f, 96f)
                    : ammoStep
                        ? new Vector2(24f, -96f)
                    : new Vector2(24f, WorkerDrawer != null && WorkerDrawer.IsOpen ? 554f : 96f);

                if (heartPauseStep)
                    hintRect.SetAsLastSibling();
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
