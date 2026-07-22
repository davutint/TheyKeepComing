using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private static readonly string[] ToastToneClasses =
        {
            "toast-tone--primary",
            "toast-tone--secondary",
            "toast-tone--warning",
            "toast-tone--critical"
        };

        private static readonly string[] ActiveSpeedLabels =
        {
            "1X ACTIVE",
            "2X ACTIVE",
            "3X ACTIVE"
        };

        private static readonly string[] PausedSpeedLabels =
        {
            "PAUSED - 1X",
            "PAUSED - 2X",
            "PAUSED - 3X"
        };

        private sealed class ActiveToastPresentation
        {
            public VisualElement Element;
            public float ExpiresAt;
            public float RemoveAt;
            public bool IsExiting;
        }

        private VisualElement _timeSpeedControls;
        private Button _timeSpeedOne;
        private Button _timeSpeedTwo;
        private Button _timeSpeedThree;
        private Label _timeSpeedState;
        private VisualElement _toastStack;
        private readonly List<ActiveToastPresentation> _activeToasts =
            new List<ActiveToastPresentation>(GameplayToastService.MaximumVisibleMessages);

        private void BindGameFlowControls()
        {
            _timeSpeedControls = Q<VisualElement>("timeSpeedControls");
            _timeSpeedOne = Q<Button>("timeSpeedOne");
            _timeSpeedTwo = Q<Button>("timeSpeedTwo");
            _timeSpeedThree = Q<Button>("timeSpeedThree");
            _timeSpeedState = Q<Label>("timeSpeedState");
            _toastStack = Q<VisualElement>("toastStack");

            _timeSpeedOne.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.Normal);
            _timeSpeedTwo.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.Fast);
            _timeSpeedThree.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.VeryFast);
            _root.RegisterCallback<ClickEvent>(HandleToolkitButtonClick);
            RefreshGameFlowControls();
        }

        private static void HandleToolkitButtonClick(ClickEvent evt)
        {
            VisualElement target = evt.target as VisualElement;
            Button button = target as Button ?? target?.GetFirstAncestorOfType<Button>();
            if (button == null || !button.enabledInHierarchy)
                return;

            UiSoundFeedback.Instance?.PlayClick();
        }

        private void SetRunningTimeScale(float timeScale)
        {
            if (IsBlockingModalOpen())
                return;

            bool changed = SimulationPauseService.TrySetRunningTimeScale(timeScale);
            if (changed && Mathf.Approximately(timeScale, SimulationSpeedUtility.Fast))
            {
                MarkGuidedOnboardingStepFromSuccessfulAction(GuidedOnboardingStep.SpeedTwo);
            }
            RefreshGameFlowControls();
        }

        private void RefreshGameFlowControls()
        {
            if (_timeSpeedControls == null)
                return;

            float runningTimeScale = SimulationPauseService.RunningTimeScale;
            bool paused = SimulationPauseService.IsPaused;
            bool blocked = paused || IsBlockingModalOpen();
            SetSpeedButtonState(_timeSpeedOne, runningTimeScale, SimulationSpeedUtility.Normal, blocked);
            SetSpeedButtonState(_timeSpeedTwo, runningTimeScale, SimulationSpeedUtility.Fast, blocked);
            SetSpeedButtonState(_timeSpeedThree, runningTimeScale, SimulationSpeedUtility.VeryFast, blocked);
            _timeSpeedControls.EnableInClassList("is-paused", paused);
            int speedIndex = ResolveSpeedIndex(runningTimeScale);
            string stateText = paused
                ? PausedSpeedLabels[speedIndex]
                : ActiveSpeedLabels[speedIndex];
            if (_timeSpeedState.text != stateText)
                _timeSpeedState.text = stateText;
        }

        private static int ResolveSpeedIndex(float runningTimeScale)
        {
            if (Mathf.Approximately(runningTimeScale, SimulationSpeedUtility.VeryFast))
                return 2;
            return Mathf.Approximately(runningTimeScale, SimulationSpeedUtility.Fast) ? 1 : 0;
        }

        private static void SetSpeedButtonState(
            Button button,
            float runningTimeScale,
            float buttonTimeScale,
            bool blocked)
        {
            if (button == null)
                return;

            button.SetEnabled(!blocked);
            button.EnableInClassList(
                "is-selected",
                Mathf.Approximately(runningTimeScale, buttonTimeScale));
        }

        private void ShowPrimaryToast(string text)
        {
            GameplayToastService.TryEnqueue(text, GameplayToastTone.Primary);
        }

        private void ShowSecondaryToast(string text)
        {
            GameplayToastService.TryEnqueue(text, GameplayToastTone.Secondary);
        }

        private void ShowWarningToast(string text)
        {
            GameplayToastService.TryEnqueue(text, GameplayToastTone.Warning, 3.2f);
        }

        private void UpdateToastPresentation(float now)
        {
            if (_toastStack == null)
                return;

            UpdateActiveToastLifetimes(now);
            while (GameplayToastService.TryDequeue(out GameplayToastMessage message))
                PresentToast(message, now);
        }

        private void UpdateActiveToastLifetimes(float now)
        {
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                ActiveToastPresentation active = _activeToasts[i];
                if (!active.IsExiting)
                {
                    if (now < active.ExpiresAt)
                        continue;

                    active.IsExiting = true;
                    active.RemoveAt = now + GameplayToastService.ExitAnimationSeconds;
                    active.Element.RemoveFromClassList("is-visible");
                    active.Element.AddToClassList("is-exiting");
                    continue;
                }

                if (now >= active.RemoveAt)
                    RemoveToastAt(i);
            }
        }

        private void PresentToast(GameplayToastMessage message, float now)
        {
            while (_activeToasts.Count >= GameplayToastService.MaximumVisibleMessages)
                RemoveToastAt(0);

            var card = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            card.AddToClassList("toast");
            ApplyToastTone(card, message.Tone);

            var marker = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            marker.AddToClassList("toast__marker");

            var label = new Label(message.Text)
            {
                pickingMode = PickingMode.Ignore
            };
            label.AddToClassList("toast__message");
            card.Add(marker);
            card.Add(label);
            _toastStack.Add(card);

            var active = new ActiveToastPresentation
            {
                Element = card,
                ExpiresAt = now + message.DurationSeconds
            };
            _activeToasts.Add(active);

            card.schedule.Execute(() =>
            {
                if (card.panel != null && !active.IsExiting)
                    card.AddToClassList("is-visible");
            }).StartingIn(16);
        }

        private void RemoveToastAt(int index)
        {
            if (index < 0 || index >= _activeToasts.Count)
                return;

            ActiveToastPresentation active = _activeToasts[index];
            active.Element.RemoveFromHierarchy();
            _activeToasts.RemoveAt(index);
        }

        private void ResetToastPresentation(bool clearQueue)
        {
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
                RemoveToastAt(i);
            _toastStack?.Clear();
            if (clearQueue)
                GameplayToastService.Clear();
        }

        private static void ApplyToastTone(VisualElement target, GameplayToastTone tone)
        {
            for (int i = 0; i < ToastToneClasses.Length; i++)
                target.EnableInClassList(ToastToneClasses[i], i == (int)tone);
        }
    }
}
