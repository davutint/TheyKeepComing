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

        private VisualElement _timeSpeedControls;
        private Button _timeSpeedOne;
        private Button _timeSpeedTwo;
        private Button _timeSpeedThree;
        private Label _timeSpeedState;
        private Label _activeToastLabel;
        private float _activeToastUntil;

        private void BindGameFlowControls()
        {
            _timeSpeedControls = Q<VisualElement>("timeSpeedControls");
            _timeSpeedOne = Q<Button>("timeSpeedOne");
            _timeSpeedTwo = Q<Button>("timeSpeedTwo");
            _timeSpeedThree = Q<Button>("timeSpeedThree");
            _timeSpeedState = Q<Label>("timeSpeedState");

            _timeSpeedOne.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.Normal);
            _timeSpeedTwo.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.Fast);
            _timeSpeedThree.clicked += () => SetRunningTimeScale(SimulationSpeedUtility.VeryFast);
            RefreshGameFlowControls();
        }

        private void SetRunningTimeScale(float timeScale)
        {
            if (IsBlockingModalOpen())
                return;

            SimulationPauseService.TrySetRunningTimeScale(timeScale);
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

        private void UpdateToastVisibility(float now)
        {
            if (_activeToastLabel != null && now < _activeToastUntil)
                return;

            HideActiveToast();
            if (!GameplayToastService.TryDequeue(out GameplayToastMessage message))
                return;

            Label target = message.Tone == GameplayToastTone.Secondary
                ? _secondaryToast
                : _primaryToast;
            if (target == null)
                return;

            target.text = message.Text;
            ApplyToastTone(target, message.Tone);
            target.AddToClassList("is-visible");
            _activeToastLabel = target;
            _activeToastUntil = now + message.DurationSeconds;
        }

        private void ResetToastPresentation(bool clearQueue)
        {
            HideActiveToast();
            _primaryToast?.RemoveFromClassList("is-visible");
            _secondaryToast?.RemoveFromClassList("is-visible");
            if (clearQueue)
                GameplayToastService.Clear();
        }

        private void HideActiveToast()
        {
            if (_activeToastLabel != null)
                _activeToastLabel.RemoveFromClassList("is-visible");
            _activeToastLabel = null;
            _activeToastUntil = 0f;
        }

        private static void ApplyToastTone(Label target, GameplayToastTone tone)
        {
            for (int i = 0; i < ToastToneClasses.Length; i++)
                target.EnableInClassList(ToastToneClasses[i], i == (int)tone);
        }
    }
}
