using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private VisualElement _modalShade;
        private VisualElement _councilModal;
        private Label _councilTitle;
        private Label _councilBody;
        private Label _councilOptionAText;
        private Label _councilOptionBText;
        private Button _councilOptionA;
        private Button _councilOptionB;
        private VisualElement _councilTimerProgress;
        private Label _councilTimerText;

        private VisualElement _levelUpModal;
        private Label _levelUpTitle;
        private VisualElement _levelUpCards;
        private bool _levelUpCardsBuilt;

        private VisualElement _pauseModal;
        private VisualElement _settingsModal;
        private Slider _sfxSlider;
        private Slider _ambienceSlider;
        private Button _resetTutorialButton;
        private Label _resetTutorialStatus;
        private bool _pauseOpen;
        private bool _settingsOpen;
        private bool _tutorialResetArmed;
        private float _tutorialResetArmUntil;

        private VisualElement _gameOverModal;
        private Label _gameOverTitle;
        private Label _gameOverStats;
        private Label _metaSummary;
        private Label _metaRecord;
        private Label _metaEarned;
        private Label _metaSouls;
        private ScrollView _metaShopRows;
        private int _metaShopSignature = -1;

        private void BindModalActions()
        {
            _modalShade = Q<VisualElement>("modalShade");
            _councilModal = Q<VisualElement>("councilModal");
            _councilTitle = Q<Label>("councilTitle");
            _councilBody = Q<Label>("councilBody");
            _councilOptionAText = Q<Label>("councilOptionAText");
            _councilOptionBText = Q<Label>("councilOptionBText");
            _councilOptionA = Q<Button>("councilOptionA");
            _councilOptionB = Q<Button>("councilOptionB");
            _councilTimerProgress = Q<VisualElement>("councilTimerProgress");
            _councilTimerText = Q<Label>("councilTimerText");
            _councilOptionA.clicked += () => ChooseCouncil(true);
            _councilOptionB.clicked += () => ChooseCouncil(false);

            _levelUpModal = Q<VisualElement>("levelUpModal");
            _levelUpTitle = Q<Label>("levelUpTitle");
            _levelUpCards = Q<VisualElement>("levelUpCards");

            _pauseModal = Q<VisualElement>("pauseModal");
            _settingsModal = Q<VisualElement>("settingsModal");
            Q<Button>("resumeButton").clicked += ResumeFromPause;
            Q<Button>("settingsButton").clicked += OpenSettings;
            Q<Button>("mainMenuButton").clicked += SaveAndReturnToMenu;
            Q<Button>("settingsClose").clicked += CloseSettings;
            _sfxSlider = Q<Slider>("sfxSlider");
            _ambienceSlider = Q<Slider>("ambienceSlider");
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            _ambienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            _sfxSlider.RegisterValueChangedCallback(evt => SoundSettings.SfxVolume = evt.newValue);
            _ambienceSlider.RegisterValueChangedCallback(evt => SoundSettings.AmbienceVolume = evt.newValue);
            _resetTutorialButton = Q<Button>("resetTutorialButton");
            _resetTutorialStatus = Q<Label>("resetTutorialStatus");
            _resetTutorialButton.clicked += HandleTutorialReset;

            _gameOverModal = Q<VisualElement>("gameOverModal");
            _gameOverTitle = Q<Label>("gameOverTitle");
            _gameOverStats = Q<Label>("gameOverStats");
            _metaSummary = Q<Label>("metaSummary");
            _metaRecord = Q<Label>("metaRecord");
            _metaEarned = Q<Label>("metaEarned");
            _metaSouls = Q<Label>("metaSouls");
            _metaShopRows = Q<ScrollView>("metaShopRows");
            Q<Button>("restartButton").clicked += RestartRun;
        }

        private void RefreshModalPresentation(GameManager gm)
        {
            bool gameOver = _uiManager != null && _uiManager.GameOverPanel != null && _uiManager.GameOverPanel.activeSelf;
            bool levelUp = _uiManager != null && _uiManager.LevelUpPanel != null && _uiManager.LevelUpPanel.activeSelf;
            bool council = gm.ActiveCouncilEvent != null;

            SetModalActive(_gameOverModal, gameOver);
            SetModalActive(_levelUpModal, !gameOver && levelUp);
            SetModalActive(_councilModal, !gameOver && !levelUp && council);
            SetModalActive(_settingsModal, !gameOver && !levelUp && !council && _settingsOpen);
            SetModalActive(_pauseModal, !gameOver && !levelUp && !council && !_settingsOpen && _pauseOpen);
            bool any = gameOver || levelUp || council || _settingsOpen || _pauseOpen;
            _modalShade.EnableInClassList("has-modal", any);

            if (council)
                RefreshCouncil(gm);
            if (levelUp)
                RefreshLevelUp();
            else
                _levelUpCardsBuilt = false;
            if (gameOver)
                RefreshGameOver();

            if (_tutorialResetArmed && Time.unscaledTime > _tutorialResetArmUntil)
                ResetTutorialResetPresentation();
        }

        private void RefreshModalContinuous(GameManager gm)
        {
            if (_pauseLease != null)
                SimulationPauseService.EnforcePausedState();

            if (gm.ActiveCouncilEvent != null)
            {
                CouncilRuntimeTuningTelemetry telemetry = gm.GetCouncilRuntimeTuningTelemetry();
                float ratio = telemetry.TotalDecisionSeconds > 0.01f
                    ? Mathf.Clamp01(telemetry.RemainingDecisionSeconds / telemetry.TotalDecisionSeconds)
                    : 0f;
                _councilTimerProgress.style.width = Length.Percent(ratio * 100f);
                _councilTimerText.text = $"{Mathf.CeilToInt(telemetry.RemainingDecisionSeconds)}s TO DECIDE";
            }
        }

        private static void SetModalActive(VisualElement modal, bool active)
        {
            modal?.EnableInClassList("is-active", active);
        }

        private void RefreshCouncil(GameManager gm)
        {
            ComposedCouncilEvent active = gm.ActiveCouncilEvent;
            if (active == null)
                return;

            _councilTitle.text = active.Title;
            _councilBody.text = active.Body;
            CouncilOptionPresentation a = gm.GetCouncilOptionPresentation(active.OptionA);
            CouncilOptionPresentation b = gm.GetCouncilOptionPresentation(active.OptionB);
            _councilOptionAText.text = BuildCouncilOptionCopy(active.OptionA?.Label, a);
            _councilOptionBText.text = BuildCouncilOptionCopy(active.OptionB?.Label, b);
            _councilOptionA.SetEnabled(a.CanApplyExactly);
            _councilOptionB.SetEnabled(b.CanApplyExactly);
        }

        private static string BuildCouncilOptionCopy(string label, CouncilOptionPresentation presentation)
        {
            string heading = string.IsNullOrWhiteSpace(label) ? "DECIDE" : label.ToUpperInvariant();
            if (presentation.CanApplyExactly)
                return heading + "\n\n" + presentation.RichText;
            string reason = string.IsNullOrWhiteSpace(presentation.UnavailableReason)
                ? "UNAVAILABLE"
                : presentation.UnavailableReason;
            return heading + "\n\n" + presentation.RichText + "\n\n" + reason;
        }

        private void ChooseCouncil(bool optionA)
        {
            GameManager gm = GameManager.Instance;
            bool chosen = gm != null && gm.ChooseCouncilOption(optionA);
            ShowPrimaryToast(chosen ? "COUNCIL DECISION COMMITTED" : "COUNCIL OPTION UNAVAILABLE");
            if (gm != null)
                RefreshModalPresentation(gm);
        }

        private void RefreshLevelUp()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;
            _levelUpTitle.text = $"LEVEL {gm.GameState.Level + 1:N0}";
            if (_levelUpCardsBuilt)
                return;

            _levelUpCardsBuilt = true;
            _levelUpCards.Clear();
            UpgradeCard[] cards = gm.GetCurrentUpgradeCards();
            if (cards == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                UpgradeCard offer = cards[i];
                UpgradeType capturedType = offer.Type;
                Button cardButton = new Button(() => ChooseLevelUpUpgrade(capturedType));
                cardButton.AddToClassList("levelup-card");
                cardButton.SetEnabled(gm.CanApplyUpgrade(offer.Type));
                Label label = new Label($"{offer.Title.ToUpperInvariant()}\n\n{offer.Description}\n\nTIER {offer.Tier:N0}");
                label.AddToClassList("levelup-card-copy");
                cardButton.Add(label);
                _levelUpCards.Add(cardButton);
            }
        }

        private void ChooseLevelUpUpgrade(UpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.ApplyUpgrade(type))
            {
                ShowPrimaryToast("ADVANTAGE UNAVAILABLE");
                _levelUpCardsBuilt = false;
                RefreshLevelUp();
                return;
            }

            _uiManager?.HideLevelUp();
            _levelUpCardsBuilt = false;
            ShowPrimaryToast("ADVANTAGE COMMITTED");
        }

        private void RefreshGameOver()
        {
            if (_gameOverLegacy != null)
            {
                _gameOverTitle.text = _gameOverLegacy.GameOverText != null
                    ? _gameOverLegacy.GameOverText.text
                    : "RUN ENDED";
                _gameOverStats.text = _gameOverLegacy.StatsText != null
                    ? _gameOverLegacy.StatsText.text
                    : string.Empty;
            }
            if (_metaLegacy != null)
            {
                _metaSummary.text = ReadText(_metaLegacy.MetaSummaryText);
                _metaRecord.text = ReadText(_metaLegacy.MetaRecordText);
                _metaEarned.text = ReadText(_metaLegacy.MetaEarnedText);
                _metaSouls.text = ReadText(_metaLegacy.MetaSoulsText);
                RebuildMetaShopRows();
            }
        }

        private void RebuildMetaShopRows()
        {
            if (_metaLegacy?.MetaShopListRoot == null || _metaShopRows == null)
                return;
            int signature = _metaLegacy.MetaShopListRoot.childCount;
            for (int i = 0; i < _metaLegacy.MetaShopListRoot.childCount; i++)
                signature = signature * 31 + _metaLegacy.MetaShopListRoot.GetChild(i).gameObject.activeSelf.GetHashCode();
            if (signature == _metaShopSignature)
                return;

            _metaShopSignature = signature;
            _metaShopRows.Clear();
            for (int i = 0; i < _metaLegacy.MetaShopListRoot.childCount; i++)
            {
                Transform row = _metaLegacy.MetaShopListRoot.GetChild(i);
                if (!row.gameObject.activeSelf)
                    continue;
                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
                var copy = new StringBuilder();
                for (int t = 0; t < texts.Length; t++)
                {
                    string value = texts[t] != null ? texts[t].text : string.Empty;
                    if (string.IsNullOrWhiteSpace(value))
                        continue;
                    if (copy.Length > 0)
                        copy.Append("  ·  ");
                    copy.Append(value.Replace("\n", " "));
                }
                UnityEngine.UI.Button legacyButton = row.GetComponentInChildren<UnityEngine.UI.Button>(true);
                Button toolkitRow = new Button(() => legacyButton?.onClick.Invoke()) { text = copy.ToString() };
                toolkitRow.AddToClassList("meta-shop-row");
                toolkitRow.SetEnabled(legacyButton == null || legacyButton.interactable);
                _metaShopRows.Add(toolkitRow);
            }
        }

        private static string ReadText(TMP_Text text)
        {
            return text != null ? text.text : string.Empty;
        }

        private void OpenPause()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.GameState.IsGameOver)
                return;
            CloseSurface();
            _pauseOpen = true;
            _settingsOpen = false;
            _pauseLease ??= SimulationPauseService.Acquire(nameof(GameplayHUDToolkitUI));
            if (_inputMode != null && _inputMode.CurrentMode == UIInputMode.Gamepad)
                Q<Button>("resumeButton").Focus();
        }

        private void ResumeFromPause()
        {
            _pauseOpen = false;
            _settingsOpen = false;
            ReleasePause();
        }

        private void ReleasePause()
        {
            _pauseLease?.Dispose();
            _pauseLease = null;
        }

        private void OpenSettings()
        {
            _pauseOpen = true;
            _settingsOpen = true;
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            _ambienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            ResetTutorialResetPresentation();
            if (_inputMode != null && _inputMode.CurrentMode == UIInputMode.Gamepad)
                _sfxSlider.Focus();
        }

        private void CloseSettings()
        {
            _settingsOpen = false;
            _pauseOpen = true;
            ResetTutorialResetPresentation();
        }

        private bool IsPauseOpen() => _pauseOpen;
        private bool IsSettingsOpen() => _settingsOpen;

        private bool IsBlockingModalOpen()
        {
            GameManager gm = GameManager.Instance;
            bool gameOver = _uiManager != null && _uiManager.GameOverPanel != null && _uiManager.GameOverPanel.activeSelf;
            bool levelUp = _uiManager != null && _uiManager.LevelUpPanel != null && _uiManager.LevelUpPanel.activeSelf;
            return gameOver || levelUp || (gm != null && gm.ActiveCouncilEvent != null) || _pauseOpen || _settingsOpen;
        }

        private void SaveAndReturnToMenu()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.SaveRunSnapshot())
            {
                ShowPrimaryToast("SAVE FAILED  ·  RETURN CANCELLED");
                return;
            }
            ReleasePause();
            SceneManager.LoadScene(GameBootstrap.MainMenuSceneName);
        }

        private void RestartRun()
        {
            _uiManager?.OnRestart();
            _metaShopSignature = -1;
            ShowSecondaryToast("NEW RUN STARTED");
        }

        private void HandleTutorialReset()
        {
            if (!_tutorialResetArmed || Time.unscaledTime > _tutorialResetArmUntil)
            {
                _tutorialResetArmed = true;
                _tutorialResetArmUntil = Time.unscaledTime + 4f;
                _resetTutorialButton.text = "CONFIRM RESET";
                _resetTutorialStatus.text = "Press again to erase tutorial progress.";
                return;
            }

            bool reset = FirstRunOnboardingUI.ResetTutorialProgress();
            _resetTutorialButton.text = reset ? "TUTORIAL RESET" : "RESET FAILED";
            _resetTutorialStatus.text = reset
                ? "Tutorial guidance will begin on the next eligible step."
                : "Tutorial progress could not be reset.";
            _tutorialResetArmed = false;
        }

        private void ResetTutorialResetPresentation()
        {
            _tutorialResetArmed = false;
            if (_resetTutorialButton != null)
                _resetTutorialButton.text = "RESET TUTORIAL";
            if (_resetTutorialStatus != null)
                _resetTutorialStatus.text = string.Empty;
        }
    }
}
