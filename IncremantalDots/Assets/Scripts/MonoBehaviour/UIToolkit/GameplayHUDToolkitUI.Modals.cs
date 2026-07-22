using System;
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
        private IDisposable _councilPauseLease;

        private VisualElement _levelUpModal;
        private Label _levelUpTitle;
        private VisualElement _levelUpCards;
        private bool _levelUpCardsBuilt;

        private VisualElement _pauseModal;
        private VisualElement _settingsModal;
        private Slider _sfxSlider;
        private Slider _ambienceSlider;
        private Button _zombieLimitPrevious;
        private Button _zombieLimitNext;
        private Label _zombieLimitValue;
        private Label _zombieLimitHint;
        private Button _resetTutorialButton;
        private Label _resetTutorialButtonLabel;
        private Label _resetTutorialStatus;
        private bool _pauseOpen;
        private bool _settingsOpen;
        private bool _tutorialResetArmed;
        private float _tutorialResetArmUntil;

        private VisualElement _gameOverModal;
        private Label _gameOverTitle;
        private Label _gameOverSubtitle;
        private Label _gameOverRecordBadge;
        private Label _gameOverDay;
        private Label _gameOverKills;
        private Label _gameOverBest;
        private Label _gameOverEarned;
        private Label _gameOverBalance;
        private Label _metaShopTitle;
        private Label _metaShopHint;
        private Label _metaWallet;
        private ScrollView _metaShopRows;
        private Button _restartButton;
        private int _metaShopSignature = -1;
        private bool _gameOverWasActive;

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
            _zombieLimitPrevious = Q<Button>("zombieLimitPrevious");
            _zombieLimitNext = Q<Button>("zombieLimitNext");
            _zombieLimitValue = Q<Label>("zombieLimitValue");
            _zombieLimitHint = Q<Label>("zombieLimitHint");
            _zombieLimitPrevious.clicked += () => StepZombieLimit(-1);
            _zombieLimitNext.clicked += () => StepZombieLimit(1);
            RefreshZombieLimitSetting();
            _resetTutorialButton = Q<Button>("resetTutorialButton");
            _resetTutorialButtonLabel = Q<Label>("resetTutorialButtonLabel");
            _resetTutorialStatus = Q<Label>("resetTutorialStatus");
            _resetTutorialButton.clicked += HandleTutorialReset;

            _gameOverModal = Q<VisualElement>("gameOverModal");
            _gameOverTitle = Q<Label>("gameOverTitle");
            _gameOverSubtitle = Q<Label>("gameOverSubtitle");
            _gameOverRecordBadge = Q<Label>("gameOverRecordBadge");
            _gameOverDay = Q<Label>("gameOverDay");
            _gameOverKills = Q<Label>("gameOverKills");
            _gameOverBest = Q<Label>("gameOverBest");
            _gameOverEarned = Q<Label>("gameOverEarned");
            _gameOverBalance = Q<Label>("gameOverBalance");
            _metaShopTitle = Q<Label>("metaShopTitle");
            _metaShopHint = Q<Label>("metaShopHint");
            _metaWallet = Q<Label>("metaWallet");
            _metaShopRows = Q<ScrollView>("metaShopRows");
            _restartButton = Q<Button>("restartButton");
            _restartButton.clicked += RestartRun;
            _metaShopSignature = -1;
            _gameOverWasActive = false;
        }

        private void RefreshModalPresentation(GameManager gm)
        {
            bool gameOver = _uiManager != null && _uiManager.GameOverPanel != null && _uiManager.GameOverPanel.activeSelf;
            bool levelUp = _uiManager != null && _uiManager.LevelUpPanel != null && _uiManager.LevelUpPanel.activeSelf;
            bool council = gm.ActiveCouncilEvent != null;
            SyncCouncilPause(council && !gameOver);

            if (gameOver && !_gameOverWasActive)
            {
                CloseSurface();
                _pauseOpen = false;
                _settingsOpen = false;
                ReleasePause();
                _metaShopSignature = -1;
            }
            _gameOverWasActive = gameOver;

            SetModalActive(_gameOverModal, gameOver);
            SetModalActive(_levelUpModal, !gameOver && levelUp);
            SetModalActive(_councilModal, !gameOver && !levelUp && council);
            SetModalActive(_settingsModal, !gameOver && !levelUp && !council && _settingsOpen);
            SetModalActive(_pauseModal, !gameOver && !levelUp && !council && !_settingsOpen && _pauseOpen);
            bool any = gameOver || levelUp || council || _settingsOpen || _pauseOpen;
            _modalShade.EnableInClassList("has-modal", any);
            _modalShade.EnableInClassList("has-gameover", gameOver);

            if (council)
                RefreshCouncil(gm);
            if (levelUp)
                RefreshLevelUp();
            else
                _levelUpCardsBuilt = false;
            if (gameOver)
                RefreshGameOver(gm);

            if (_tutorialResetArmed && Time.unscaledTime > _tutorialResetArmUntil)
                ResetTutorialResetPresentation();
        }

        private void RefreshModalContinuous(GameManager gm)
        {
            SyncCouncilPause(gm.ActiveCouncilEvent != null && !gm.GameState.IsGameOver);
            if (_pauseLease != null || _councilPauseLease != null)
                SimulationPauseService.EnforcePausedState();

            if (gm.ActiveCouncilEvent != null)
            {
                _councilTimerProgress.style.width = Length.Percent(100f);
                _councilTimerText.text = "GAME PAUSED · CHOOSE TO CONTINUE";
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
            if (chosen)
            {
                SyncCouncilPause(false);
                MarkGuidedOnboardingStepFromSuccessfulAction(
                    GuidedOnboardingStep.CouncilChoice);
            }
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
                cardButton.Add(CreateRoleIcon(LevelUpIconRole(offer.Type), "dw-icon--levelup"));
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

        private void RefreshGameOver(GameManager gm)
        {
            MetaUpgradeCatalogSO catalog = gm.MetaCatalog;
            MetaPresentationSettings presentation = catalog != null ? catalog.Presentation : null;
            MetaProgressState state = MetaProgression.State;
            MetaRunResult result = gm.LastRunResult;
            string currency = presentation != null ? presentation.ShortName : MetaProgression.CurrencyName;

            _gameOverTitle.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.DeathTitle)
                ? presentation.DeathTitle.ToUpperInvariant()
                : "THE WALL HAS FALLEN";
            _gameOverSubtitle.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.DeathSubtitle)
                ? presentation.DeathSubtitle
                : "The run ends here. What remains will strengthen the next stand.";

            int day = Mathf.Max(1, result.Day);
            int kills = Mathf.Max(0, result.Kills);
            int bestDay = state != null ? Mathf.Max(day, state.BestDay) : day;
            int earned = Mathf.Max(0, result.SoulsEarned);
            int balance = state != null ? Mathf.Max(0, state.Souls) : 0;
            _gameOverDay.text = day.ToString("N0");
            _gameOverKills.text = kills.ToString("N0");
            _gameOverBest.text = bestDay.ToString("N0");
            _gameOverEarned.text = "+" + earned.ToString("N0");
            _gameOverBalance.text = balance.ToString("N0");

            _gameOverRecordBadge.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.NewRecordLabel)
                ? presentation.NewRecordLabel.ToUpperInvariant()
                : "NEW LONGEST STAND";
            _gameOverRecordBadge.EnableInClassList("is-visible", result.NewRecord);
            _metaShopTitle.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.ShopTitle)
                ? presentation.ShopTitle.ToUpperInvariant()
                : "FORTIFY THE NEXT STAND";
            _metaShopHint.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.ShopHint)
                ? presentation.ShopHint
                : "Permanent upgrades apply to your next run.";
            _metaWallet.text = $"{balance:N0} {currency.ToUpperInvariant()}";
            _restartButton.text = presentation != null && !string.IsNullOrWhiteSpace(presentation.RestartLabel)
                ? presentation.RestartLabel.ToUpperInvariant()
                : "BEGIN NEXT RUN";

            RebuildMetaShopRows(gm, currency);
        }

        private void RebuildMetaShopRows(GameManager gm, string currency)
        {
            MetaUpgradeCatalogSO catalog = gm.MetaCatalog;
            if (_metaShopRows == null || catalog == null || catalog.Upgrades == null)
                return;

            MetaProgressState state = MetaProgression.State;
            int signature = catalog.GetInstanceID();
            unchecked
            {
                signature = signature * 31 + (state != null ? state.Souls : 0);
                signature = signature * 31 + gm.IsMetaShopPurchaseAllowed.GetHashCode();
                for (int i = 0; i < catalog.Upgrades.Length; i++)
                {
                    MetaUpgradeSO upgrade = catalog.Upgrades[i];
                    if (upgrade == null)
                        continue;
                    signature = signature * 31 + upgrade.GetInstanceID();
                    signature = signature * 31 + MetaProgression.GetUpgradeLevel(upgrade.Id);
                }
            }
            if (signature == _metaShopSignature)
                return;

            _metaShopSignature = signature;
            _metaShopRows.Clear();
            for (int i = 0; i < catalog.Upgrades.Length; i++)
            {
                MetaUpgradeSO upgrade = catalog.Upgrades[i];
                if (upgrade == null)
                    continue;

                MetaUpgradeSO capturedUpgrade = upgrade;
                int level = MetaProgression.GetUpgradeLevel(upgrade.Id);
                bool maxed = upgrade.IsMaxLevel(level)
                    || (MetaUpgradePolicy.IsContentUnlockEffect(upgrade.EffectType)
                        && MetaProgression.HasPoolUnlock(upgrade.PoolContentId));
                bool canBuy = !maxed && gm.CanBuyMetaUpgrade(upgrade);
                int cost = maxed ? 0 : upgrade.GetCost(level);

                Button toolkitRow = new Button(() => BuyMetaUpgrade(capturedUpgrade));
                toolkitRow.AddToClassList("meta-shop-row");
                toolkitRow.EnableInClassList("is-affordable", canBuy);
                toolkitRow.EnableInClassList("is-maxed", maxed);
                toolkitRow.SetEnabled(!maxed);
                toolkitRow.EnableInClassList("is-action-unavailable", !canBuy && !maxed);

                VisualElement copy = new VisualElement();
                copy.AddToClassList("meta-upgrade-copy");
                Label title = new Label(upgrade.Title.ToUpperInvariant());
                title.AddToClassList("meta-upgrade-title");
                Label description = new Label(upgrade.Description);
                description.AddToClassList("meta-upgrade-description");
                Label effect = new Label(MetaUpgradePresentationUtility.BuildEffectProgression(upgrade, level));
                effect.AddToClassList("meta-upgrade-effect");
                copy.Add(title);
                copy.Add(description);
                copy.Add(effect);

                VisualElement main = new VisualElement();
                main.AddToClassList("meta-upgrade-main");
                main.Add(CreateRoleIcon(MetaUpgradeIconRole(upgrade), "dw-icon--meta"));
                main.Add(copy);

                VisualElement transaction = new VisualElement();
                transaction.AddToClassList("meta-upgrade-transaction");
                string levelText = maxed
                    ? $"LEVEL {level:N0} / {upgrade.MaxLevel:N0}"
                    : $"LEVEL {level:N0}  →  {level + 1:N0}";
                Label levelLabel = new Label(levelText);
                levelLabel.AddToClassList("meta-upgrade-level");
                Label price = new Label(maxed ? "COMPLETE" : $"BUY  ·  {cost:N0} {currency.ToUpperInvariant()}");
                price.AddToClassList("meta-upgrade-price");
                transaction.Add(levelLabel);
                transaction.Add(price);

                toolkitRow.Add(main);
                toolkitRow.Add(transaction);
                _metaShopRows.Add(toolkitRow);
            }
        }

        private void BuyMetaUpgrade(MetaUpgradeSO upgrade)
        {
            GameManager gm = GameManager.Instance;
            bool purchased = gm != null && gm.TryBuyMetaUpgrade(upgrade);
            if (purchased)
            {
                ShowPrimaryToast($"PERMANENT UPGRADE PURCHASED  ·  {upgrade.Title.ToUpperInvariant()}");
            }
            else if (gm == null || upgrade == null)
            {
                ShowWarningToast("META UPGRADE UNAVAILABLE  ·  GAME STATE NOT READY");
            }
            else
            {
                int level = MetaProgression.GetUpgradeLevel(upgrade.Id);
                bool maxed = upgrade.IsMaxLevel(level)
                    || (MetaUpgradePolicy.IsContentUnlockEffect(upgrade.EffectType)
                        && MetaProgression.HasPoolUnlock(upgrade.PoolContentId));
                MetaPresentationSettings presentation = gm.MetaCatalog != null
                    ? gm.MetaCatalog.Presentation
                    : null;
                string currency = presentation != null && !string.IsNullOrWhiteSpace(presentation.ShortName)
                    ? presentation.ShortName
                    : MetaProgression.CurrencyName;
                ShowWarningToast(GameplayActionFeedbackUtility.BuildMetaUpgradeFailure(
                    gm.IsMetaShopPurchaseAllowed,
                    maxed,
                    maxed ? 0 : upgrade.GetCost(level),
                    MetaProgression.State != null ? MetaProgression.State.Souls : 0,
                    currency));
            }
            _metaShopSignature = -1;
            if (gm != null)
                RefreshGameOver(gm);
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

        private void SyncCouncilPause(bool shouldPause)
        {
            if (shouldPause)
            {
                _councilPauseLease ??= SimulationPauseService.Acquire("CouncilDecision");
                return;
            }

            ReleaseCouncilPause();
        }

        private void ReleaseCouncilPause()
        {
            _councilPauseLease?.Dispose();
            _councilPauseLease = null;
        }

        private void OpenSettings()
        {
            _pauseOpen = true;
            _settingsOpen = true;
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            _ambienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            RefreshZombieLimitSetting();
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

        private void StepZombieLimit(int direction)
        {
            ZombieLimitPreset current = GameplayPerformanceSettings.CurrentZombieLimitPreset;
            ZombieLimitPreset next = GameplayPerformanceSettings.Step(current, direction);
            if (next == current)
                return;

            GameplayPerformanceSettings.CurrentZombieLimitPreset = next;
            GameManager.Instance?.ApplyZombieLimitSetting();
            RefreshZombieLimitSetting();
        }

        private void RefreshZombieLimitSetting()
        {
            if (_zombieLimitValue == null || _zombieLimitHint == null)
                return;

            ZombieLimitPreset preset = GameplayPerformanceSettings.CurrentZombieLimitPreset;
            _zombieLimitValue.text = GameplayPerformanceSettings.GetDisplayName(preset);
            _zombieLimitHint.text = GameplayPerformanceSettings.GetPerformanceHint(preset);
            _zombieLimitPrevious?.SetEnabled(
                GameplayPerformanceSettings.CanStep(preset, -1));
            _zombieLimitNext?.SetEnabled(
                GameplayPerformanceSettings.CanStep(preset, 1));
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
            _gameOverWasActive = false;
            ShowSecondaryToast("NEW RUN STARTED");
        }

        private void HandleTutorialReset()
        {
            if (!_tutorialResetArmed || Time.unscaledTime > _tutorialResetArmUntil)
            {
                _tutorialResetArmed = true;
                _tutorialResetArmUntil = Time.unscaledTime + 4f;
                _resetTutorialButtonLabel.text = "CONFIRM RESET";
                _resetTutorialStatus.text = "Press again to erase tutorial progress.";
                return;
            }

            bool reset = FirstRunOnboardingUI.ResetTutorialProgress();
            _resetTutorialButtonLabel.text = reset ? "TUTORIAL RESET" : "RESET FAILED";
            _resetTutorialStatus.text = reset
                ? "Tutorial guidance will begin on the next eligible step."
                : "Tutorial progress could not be reset.";
            _tutorialResetArmed = false;
        }

        private void ResetTutorialResetPresentation()
        {
            _tutorialResetArmed = false;
            if (_resetTutorialButtonLabel != null)
                _resetTutorialButtonLabel.text = "RESET TUTORIAL";
            if (_resetTutorialStatus != null)
                _resetTutorialStatus.text = string.Empty;
        }
    }
}
