using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private const float GuidedFocusPadding = 8f;
        private const float GuidedCardWidth = 410f;
        private const float GuidedCardFallbackHeight = 154f;
        private const float GuidedCardGap = 16f;
        private const float GuidedFocusPulseExpansion = 5f;
        private const float GuidedFocusPulseCyclesPerSecond = 1.15f;

        private VisualElement _guidedTutorialLayer;
        private VisualElement _guidedDimTop;
        private VisualElement _guidedDimBottom;
        private VisualElement _guidedDimLeft;
        private VisualElement _guidedDimRight;
        private VisualElement _guidedFocus;
        private VisualElement _guidedCard;
        private Label _guidedStepLabel;
        private Label _guidedTitle;
        private Label _guidedBody;
        private Label _guidedAction;
        private GuidedOnboardingStep _activeGuidedStep;
        private VisualElement _activeGuidedTarget;
        private IDisposable _guidedPauseLease;
        private bool _guidedInputGateRegistered;
        private bool _guidedCoreInputLocked;
        private bool _guidedPersistenceWarningLogged;

        public GuidedOnboardingStep ActiveGuidedOnboardingStep => _activeGuidedStep;
        public VisualElement ActiveGuidedOnboardingTarget => _activeGuidedTarget;
        public bool IsGuidedOnboardingInputLocked => _guidedCoreInputLocked;
        internal bool HasGuidedOnboardingPresentationOwner => _guidedTutorialLayer != null;

        private void BindGuidedOnboardingElements()
        {
            _guidedTutorialLayer = Q<VisualElement>("guidedTutorialLayer");
            _guidedDimTop = Q<VisualElement>("guidedDimTop");
            _guidedDimBottom = Q<VisualElement>("guidedDimBottom");
            _guidedDimLeft = Q<VisualElement>("guidedDimLeft");
            _guidedDimRight = Q<VisualElement>("guidedDimRight");
            _guidedFocus = Q<VisualElement>("guidedFocus");
            _guidedCard = Q<VisualElement>("guidedCard");
            _guidedStepLabel = Q<Label>("guidedStepLabel");
            _guidedTitle = Q<Label>("guidedTitle");
            _guidedBody = Q<Label>("guidedBody");
            _guidedAction = Q<Label>("guidedAction");
        }

        private void RegisterGuidedOnboardingInputGate()
        {
            if (_guidedInputGateRegistered || _root == null)
                return;

            _root.RegisterCallback<PointerDownEvent>(
                HandleGuidedPointerDown,
                TrickleDown.TrickleDown);
            _root.RegisterCallback<ClickEvent>(
                HandleGuidedClick,
                TrickleDown.TrickleDown);
            _root.RegisterCallback<NavigationSubmitEvent>(
                HandleGuidedNavigationSubmit,
                TrickleDown.TrickleDown);
            _guidedInputGateRegistered = true;
        }

        private void UnregisterGuidedOnboardingInputGate()
        {
            if (!_guidedInputGateRegistered || _root == null)
                return;

            _root.UnregisterCallback<PointerDownEvent>(
                HandleGuidedPointerDown,
                TrickleDown.TrickleDown);
            _root.UnregisterCallback<ClickEvent>(
                HandleGuidedClick,
                TrickleDown.TrickleDown);
            _root.UnregisterCallback<NavigationSubmitEvent>(
                HandleGuidedNavigationSubmit,
                TrickleDown.TrickleDown);
            _guidedInputGateRegistered = false;
        }

        private void HandleGuidedPointerDown(PointerDownEvent evt)
        {
            StopGuidedEventOutsideTarget(evt);
        }

        private void HandleGuidedClick(ClickEvent evt)
        {
            StopGuidedEventOutsideTarget(evt);
        }

        private void HandleGuidedNavigationSubmit(NavigationSubmitEvent evt)
        {
            StopGuidedEventOutsideTarget(evt);
        }

        private void StopGuidedEventOutsideTarget(EventBase evt)
        {
            if (!_guidedCoreInputLocked)
                return;

            VisualElement eventTarget = evt.target as VisualElement;
            if (IsWithinGuidedTarget(eventTarget, _activeGuidedTarget))
                return;

            evt.StopImmediatePropagation();
        }

        internal static bool IsWithinGuidedTarget(
            VisualElement eventTarget,
            VisualElement allowedTarget)
        {
            return eventTarget != null
                && allowedTarget != null
                && (ReferenceEquals(eventTarget, allowedTarget)
                    || allowedTarget.Contains(eventTarget));
        }

        private void UpdateGuidedOnboarding(GameManager gm)
        {
            if (_guidedTutorialLayer == null || gm == null)
            {
                ReleaseGuidedOnboardingPause();
                return;
            }

            GuidedOnboardingStep step = ResolveGuidedOnboardingStep(gm);
            SyncGuidedOnboardingPause(step != GuidedOnboardingStep.None);
            VisualElement target = ResolveGuidedOnboardingTarget(step);
            bool targetReady = IsGuidedTargetReady(target);
            if (step == GuidedOnboardingStep.None || !targetReady)
            {
                HideGuidedOnboardingPresentation();
                return;
            }

            bool coreStep = GuidedOnboardingProgress.IsCoreStep(step);
            if (_activeGuidedStep != step || !ReferenceEquals(_activeGuidedTarget, target))
            {
                _activeGuidedStep = step;
                _activeGuidedTarget = target;
                ApplyGuidedOnboardingCopy(step);
                if (_inputMode != null && _inputMode.CurrentMode == UIInputMode.Gamepad)
                    target.Focus();
            }

            _guidedCoreInputLocked = coreStep;
            _guidedTutorialLayer.EnableInClassList("is-visible", true);
            _guidedTutorialLayer.EnableInClassList("is-core", coreStep);
            _guidedTutorialLayer.EnableInClassList("is-contextual", !coreStep);
            UpdateGuidedOnboardingGeometry(target, coreStep);
        }

        private void SyncGuidedOnboardingPause(bool shouldPause)
        {
            if (shouldPause)
            {
                _guidedPauseLease ??= SimulationPauseService.Acquire("GuidedOnboarding");
                SimulationPauseService.EnforcePausedState();
                return;
            }

            ReleaseGuidedOnboardingPause();
        }

        private void ReleaseGuidedOnboardingPause()
        {
            _guidedPauseLease?.Dispose();
            _guidedPauseLease = null;
        }

        private GuidedOnboardingStep ResolveGuidedOnboardingStep(GameManager gm)
        {
            if (gm.GameState.IsGameOver)
                return GuidedOnboardingStep.None;

            bool suppressTutorial = MetaProgression.HasTutorialFlag(
                    FirstRunOnboardingUI.TutorialCompleteFlagId)
                || MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.CompleteFlagId);
            GuidedOnboardingStep core = GuidedOnboardingProgress.ResolveCoreStep(
                suppressTutorial,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.EconomyOpenFlagId),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.WorkerShareFlagId),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.EconomyCloseFlagId),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.BarracksOpenFlagId),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.BasicArcherFlagId),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.SpeedTwoFlagId));
            if (core != GuidedOnboardingStep.None)
            {
                return IsBlockingModalOpen()
                    ? GuidedOnboardingStep.None
                    : core;
            }

            bool councilEligible = gm.ActiveCouncilEvent != null;
            if (IsBlockingModalOpen() && !councilEligible)
                return GuidedOnboardingStep.None;

            ContinuousSiegeCycleData cycle = gm.ContinuousSiegeCycle;
            int arrowCapacity = gm.GetArrowCapacity();
            int arrowCurrent = Mathf.Clamp(gm.ArrowSupply.Current, 0, arrowCapacity);
            bool arrowRefillEligible = arrowCapacity > 0
                && (long)arrowCurrent * 100L
                    <= (long)arrowCapacity * FirstRunOnboardingUI.LowAmmoThresholdPercent
                && gm.CanBuyArrowRefill(1);
            bool rallyEligible = cycle.CycleIndex == 0
                && cycle.Phase == SiegeCyclePhase.Night
                && gm.RallyReady;
            bool repairEligible = cycle.Phase == SiegeCyclePhase.Night
                && gm.EmergencyRepairReady
                && gm.GetDefensePercent() < 0.995f;
            bool postCombatPhase = cycle.Phase == SiegeCyclePhase.Day
                || cycle.Phase == SiegeCyclePhase.Dawn;
            bool castleHeartEligible = postCombatPhase
                && gm.GraveEssenceAmount > 0L
                && _openSurface != SurfaceKind.Heart;
            bool housingEligible = gm.Population.Total >= gm.GetTotalBedCapacity()
                && gm.CanBuyBedCapacity(1);

            return GuidedOnboardingProgress.ResolveContextualStep(
                suppressTutorial,
                GuidedOnboardingProgress.IsCoreComplete(),
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.CouncilChoiceFlagId),
                councilEligible,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.RallyFlagId),
                rallyEligible,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.WallRepairFlagId),
                repairEligible,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.ArrowRefillFlagId),
                arrowRefillEligible,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.CastleHeartFlagId),
                castleHeartEligible,
                MetaProgression.HasTutorialFlag(GuidedOnboardingProgress.HousingFlagId),
                housingEligible);
        }

        private VisualElement ResolveGuidedOnboardingTarget(GuidedOnboardingStep step)
        {
            switch (step)
            {
                case GuidedOnboardingStep.EconomyOpen:
                    return _economyButton;
                case GuidedOnboardingStep.WorkerShare:
                    if (_openSurface != SurfaceKind.Economy)
                        return _economyButton;
                    return Q<SliderInt>("economyAllocationSlider" + EconomyFocusType.Wood);
                case GuidedOnboardingStep.EconomyClose:
                    return Q<Button>("economyClose");
                case GuidedOnboardingStep.BarracksOpen:
                    return _barracksButton;
                case GuidedOnboardingStep.BasicArcher:
                    if (_openSurface != SurfaceKind.Barracks)
                        return _barracksButton;
                    return Q<Button>("archerBuy" + ArcherType.Basic);
                case GuidedOnboardingStep.SpeedTwo:
                    return _timeSpeedTwo;
                case GuidedOnboardingStep.Rally:
                    return _rallyButton;
                case GuidedOnboardingStep.CouncilChoice:
                    return _councilOptionA?.parent ?? _councilModal;
                case GuidedOnboardingStep.ArrowRefill:
                    return _openSurface == SurfaceKind.Arrows
                        ? _arrowPackageButton
                        : _arrowsButton;
                case GuidedOnboardingStep.CastleHeart:
                    return _heartButton;
                case GuidedOnboardingStep.Housing:
                    return _openSurface == SurfaceKind.Economy
                        ? _housingOne
                        : _economyButton;
                case GuidedOnboardingStep.WallRepair:
                    return _repairButton;
                default:
                    return null;
            }
        }

        private static bool IsGuidedTargetReady(VisualElement target)
        {
            if (target == null || target.panel == null
                || target.resolvedStyle.display == DisplayStyle.None
                || target.resolvedStyle.visibility != Visibility.Visible)
            {
                return false;
            }

            Rect bounds = target.worldBound;
            return bounds.width > 1f && bounds.height > 1f;
        }

        private void ApplyGuidedOnboardingCopy(GuidedOnboardingStep step)
        {
            GuidedOnboardingCopy copy = GuidedOnboardingProgress.GetCopy(step);
            bool coreStep = GuidedOnboardingProgress.IsCoreStep(step);
            _guidedStepLabel.text = coreStep
                ? $"TUTORIAL PAUSED  ·  STEP {GuidedOnboardingProgress.GetCoreStepNumber(step)} OF 6"
                : "FIELD TIP  ·  GAME PAUSED";
            _guidedTitle.text = copy.Title;
            _guidedBody.text = copy.Body;
            _guidedAction.text = coreStep
                ? "COMPLETE THE HIGHLIGHTED ACTION TO CONTINUE"
                : "COMPLETE THE HIGHLIGHTED ACTION TO RESUME";
        }

        private void UpdateGuidedOnboardingGeometry(VisualElement target, bool coreStep)
        {
            float pulse = EvaluateGuidedFocusPulse(Time.unscaledTime);
            float focusPadding = GuidedFocusPadding
                + GuidedFocusPulseExpansion * pulse;
            Rect targetLocalRect = new Rect(Vector2.zero, target.localBound.size);
            Rect focusRect = target.ChangeCoordinatesTo(_root, targetLocalRect);
            float rootWidth = Mathf.Max(1f, _root.contentRect.width);
            float rootHeight = Mathf.Max(1f, _root.contentRect.height);
            focusRect.xMin = Mathf.Clamp(focusRect.xMin - focusPadding, 0f, rootWidth);
            focusRect.yMin = Mathf.Clamp(focusRect.yMin - focusPadding, 0f, rootHeight);
            focusRect.xMax = Mathf.Clamp(focusRect.xMax + focusPadding, 0f, rootWidth);
            focusRect.yMax = Mathf.Clamp(focusRect.yMax + focusPadding, 0f, rootHeight);

            SetGuidedRect(_guidedFocus, focusRect.x, focusRect.y, focusRect.width, focusRect.height);
            _guidedFocus.style.opacity = Mathf.Lerp(0.58f, 1f, pulse);
            float borderWidth = Mathf.Lerp(2f, 3.5f, pulse);
            _guidedFocus.style.borderTopWidth = borderWidth;
            _guidedFocus.style.borderRightWidth = borderWidth;
            _guidedFocus.style.borderBottomWidth = borderWidth;
            _guidedFocus.style.borderLeftWidth = borderWidth;
            if (coreStep)
            {
                SetGuidedRect(_guidedDimTop, 0f, 0f, rootWidth, focusRect.yMin);
                SetGuidedRect(
                    _guidedDimBottom,
                    0f,
                    focusRect.yMax,
                    rootWidth,
                    Mathf.Max(0f, rootHeight - focusRect.yMax));
                SetGuidedRect(
                    _guidedDimLeft,
                    0f,
                    focusRect.yMin,
                    focusRect.xMin,
                    focusRect.height);
                SetGuidedRect(
                    _guidedDimRight,
                    focusRect.xMax,
                    focusRect.yMin,
                    Mathf.Max(0f, rootWidth - focusRect.xMax),
                    focusRect.height);
            }

            float cardHeight = _guidedCard.resolvedStyle.height;
            if (float.IsNaN(cardHeight) || cardHeight < 60f)
                cardHeight = GuidedCardFallbackHeight;
            float cardWidth = Mathf.Min(GuidedCardWidth, Mathf.Max(220f, rootWidth - 32f));
            float cardLeft = Mathf.Clamp(
                focusRect.center.x - cardWidth * 0.5f,
                16f,
                Mathf.Max(16f, rootWidth - cardWidth - 16f));
            bool placeBelow = focusRect.yMax + GuidedCardGap + cardHeight <= rootHeight - 16f;
            float cardTop = placeBelow
                ? focusRect.yMax + GuidedCardGap
                : Mathf.Max(16f, focusRect.yMin - GuidedCardGap - cardHeight);
            SetGuidedRect(_guidedCard, cardLeft, cardTop, cardWidth, cardHeight);
        }

        internal static float EvaluateGuidedFocusPulse(float unscaledTime)
        {
            float radians = Mathf.Max(0f, unscaledTime)
                * GuidedFocusPulseCyclesPerSecond
                * Mathf.PI
                * 2f;
            return 0.5f + 0.5f * Mathf.Sin(radians - Mathf.PI * 0.5f);
        }

        private static void SetGuidedRect(
            VisualElement element,
            float left,
            float top,
            float width,
            float height)
        {
            if (element == null)
                return;

            element.style.left = left;
            element.style.top = top;
            element.style.width = Mathf.Max(0f, width);
            element.style.height = Mathf.Max(0f, height);
        }

        private void HideGuidedOnboardingPresentation()
        {
            _activeGuidedStep = GuidedOnboardingStep.None;
            _activeGuidedTarget = null;
            _guidedCoreInputLocked = false;
            _guidedTutorialLayer?.RemoveFromClassList("is-visible");
            _guidedTutorialLayer?.RemoveFromClassList("is-core");
            _guidedTutorialLayer?.RemoveFromClassList("is-contextual");
        }

        private void ResetGuidedOnboardingPresentation()
        {
            ReleaseGuidedOnboardingPause();
            HideGuidedOnboardingPresentation();
            _guidedPersistenceWarningLogged = false;
        }

        private bool MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep step)
        {
            if (_activeGuidedStep != step)
                return false;

            if (GuidedOnboardingProgress.TryComplete(step))
            {
                _guidedPersistenceWarningLogged = false;
                if (!GuidedOnboardingProgress.IsCoreStep(step)
                    || GuidedOnboardingProgress.IsCoreComplete())
                {
                    ReleaseGuidedOnboardingPause();
                }
                HideGuidedOnboardingPresentation();
                return true;
            }

            if (!_guidedPersistenceWarningLogged)
            {
                _guidedPersistenceWarningLogged = true;
                Debug.LogWarning(
                    $"[GameplayHUDToolkitUI] Guided onboarding flag durable yazilamadi: {step}.");
            }
            return false;
        }
    }
}
