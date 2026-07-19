using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DeadWalls
{
    /// <summary>
    /// MainMenuScene'in production UI Toolkit owner'i. Legacy uGUI menu bu sahnede
    /// gecici rollback kaynagi olarak durur fakat render ve davranis olarak kapatilir.
    /// </summary>
    [DefaultExecutionOrder(-9990)]
    [RequireComponent(typeof(UIDocument), typeof(UIInputModeService))]
    public sealed class MainMenuToolkitUI : MonoBehaviour
    {
        private const float MenuAmbienceVolume = 0.22f;
        private static readonly string[] BackgroundPhaseClasses =
        {
            "cycle--day",
            "cycle--dusk",
            "cycle--night",
            "cycle--dawn"
        };
        private static readonly float[] BackgroundPhaseDurations = { 22f, 8f, 22f, 8f };

        private UIDocument _document;
        private UIInputModeService _inputMode;
        private VisualElement _root;
        private VisualElement _settingsOverlay;
        private Button _continueButton;
        private Button _newRunButton;
        private Button _settingsButton;
        private Button _tutorialResetButton;
        private Label _tutorialResetButtonLabel;
        private Button _settingsCloseButton;
        private Slider _sfxSlider;
        private Slider _ambienceSlider;
        private Label _sfxValueLabel;
        private Label _ambienceValueLabel;
        private Label _tutorialStatusLabel;
        private AudioSource _ambienceSource;
        private bool _tutorialResetArmed;
        [System.NonSerialized] private bool _initialized;
        private bool _startingGame;
        private int _backgroundPhaseIndex;
        private float _backgroundPhaseStartedAt;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _developmentOverlaySuppressed;
#endif

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _inputMode = GetComponent<UIInputModeService>();
            DisableLegacyMenu();
            DisableDevelopmentOverlay();
        }

        private void OnEnable()
        {
            if (_inputMode != null)
                _inputMode.ModeChanged += HandleInputModeChanged;

            InitializeVisualTree();
        }

        private void Start()
        {
            InitializeVisualTree();
            DisableDevelopmentOverlay();
            RunPersistence.RecoverPendingDeathReward();
            ConfigureAmbience();
        }

        private void Update()
        {
            if (!_initialized || _root == null)
            {
                _initialized = false;
                InitializeVisualTree();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_developmentOverlaySuppressed)
                _developmentOverlaySuppressed = TryDisableDevelopmentOverlay();
#endif

            if (_ambienceSource != null && _ambienceSource.isPlaying)
                _ambienceSource.volume = MenuAmbienceVolume * SoundSettings.AmbienceVolume;

            UpdateBackgroundCycle();
        }

        private void OnDisable()
        {
            if (_inputMode != null)
                _inputMode.ModeChanged -= HandleInputModeChanged;
        }

        private void InitializeVisualTree()
        {
            if (_initialized || _document == null)
                return;

            _root = _document.rootVisualElement?.Q<VisualElement>("screen");
            if (_root == null)
                return;

            _continueButton = _root.Q<Button>("continueButton");
            _newRunButton = _root.Q<Button>("newRunButton");
            _settingsButton = _root.Q<Button>("settingsButton");
            _settingsOverlay = _root.Q<VisualElement>("settingsOverlay");
            _tutorialResetButton = _root.Q<Button>("tutorialResetButton");
            _tutorialResetButtonLabel = _root.Q<Label>("tutorialResetButtonLabel");
            _settingsCloseButton = _root.Q<Button>("settingsCloseButton");
            _sfxSlider = _root.Q<Slider>("sfxSlider");
            _ambienceSlider = _root.Q<Slider>("ambienceSlider");
            _sfxValueLabel = _root.Q<Label>("sfxValueLabel");
            _ambienceValueLabel = _root.Q<Label>("ambienceValueLabel");
            _tutorialStatusLabel = _root.Q<Label>("tutorialStatusLabel");

            _continueButton.clicked += HandleContinueClicked;
            _newRunButton.clicked += HandleNewRunClicked;
            _settingsButton.clicked += HandleSettingsClicked;
            _tutorialResetButton.clicked += HandleTutorialResetClicked;
            _settingsCloseButton.clicked += HandleSettingsCloseClicked;
            _sfxSlider.RegisterValueChangedCallback(evt =>
            {
                SoundSettings.SfxVolume = evt.newValue;
                UpdateVolumeLabels();
            });
            _ambienceSlider.RegisterValueChangedCallback(evt =>
            {
                SoundSettings.AmbienceVolume = evt.newValue;
                UpdateVolumeLabels();
            });

            _root.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            _root.RegisterCallback<NavigationCancelEvent>(_ =>
            {
                if (_settingsOverlay.ClassListContains("is-open"))
                    CloseSettings();
            });

            ConfigureSaveState();
            ApplyInputMode(_inputMode.CurrentMode);
            ApplyBackgroundPhase(0);
            _initialized = true;
        }

        private void ConfigureSaveState()
        {
            RunSaveState save = RunPersistence.TryLoad();
            bool hasSave = save != null;

            _continueButton.style.display = hasSave ? DisplayStyle.Flex : DisplayStyle.None;
            _newRunButton.style.display = hasSave ? DisplayStyle.None : DisplayStyle.Flex;

            if (hasSave)
                _continueButton.text = "CONTINUE";
        }

        private void UpdateBackgroundCycle()
        {
            if (!_initialized || _root == null)
                return;

            float phaseDuration = BackgroundPhaseDurations[_backgroundPhaseIndex];
            if (Time.unscaledTime - _backgroundPhaseStartedAt < phaseDuration)
                return;

            ApplyBackgroundPhase((_backgroundPhaseIndex + 1) % BackgroundPhaseClasses.Length);
        }

        private void ApplyBackgroundPhase(int phaseIndex)
        {
            for (int i = 0; i < BackgroundPhaseClasses.Length; i++)
                _root.RemoveFromClassList(BackgroundPhaseClasses[i]);

            _backgroundPhaseIndex = phaseIndex;
            _backgroundPhaseStartedAt = Time.unscaledTime;
            _root.AddToClassList(BackgroundPhaseClasses[_backgroundPhaseIndex]);
        }

        private void HandleContinueClicked()
        {
            UiSoundFeedback.Instance?.PlayClick();
            StartGame(GameBootstrap.StartAction.Continue);
        }

        private void HandleNewRunClicked()
        {
            UiSoundFeedback.Instance?.PlayClick();
            StartGame(GameBootstrap.StartAction.NewRun);
        }

        private void HandleSettingsClicked()
        {
            UiSoundFeedback.Instance?.PlayClick();
            OpenSettings();
        }

        private void HandleSettingsCloseClicked()
        {
            UiSoundFeedback.Instance?.PlayClick();
            CloseSettings();
        }

        private void ConfigureAmbience()
        {
            GameObject ambienceObject = GameObject.Find("MenuAmbience");
            _ambienceSource = ambienceObject != null ? ambienceObject.GetComponent<AudioSource>() : null;
            if (_ambienceSource == null || _ambienceSource.clip == null)
                return;

            _ambienceSource.loop = true;
            _ambienceSource.spatialBlend = 0f;
            _ambienceSource.volume = MenuAmbienceVolume * SoundSettings.AmbienceVolume;
            if (!_ambienceSource.isPlaying)
                _ambienceSource.Play();
        }

        private void OpenSettings()
        {
            _sfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            _ambienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            UpdateVolumeLabels();
            ResetTutorialConfirmation();
            _settingsOverlay.AddToClassList("is-open");
            _settingsCloseButton.schedule.Execute(() => _settingsCloseButton.Focus()).StartingIn(40);
        }

        private void CloseSettings()
        {
            ResetTutorialConfirmation();
            _settingsOverlay.RemoveFromClassList("is-open");
            if (_inputMode.CurrentMode == UIInputMode.Gamepad)
                _settingsButton.Focus();
        }

        private void HandleTutorialResetClicked()
        {
            UiSoundFeedback.Instance?.PlayClick();
            if (!_tutorialResetArmed)
            {
                _tutorialResetArmed = true;
                _tutorialResetButtonLabel.text = SettingsUI.TutorialResetConfirmLabel;
                _tutorialStatusLabel.text = SettingsUI.TutorialResetConfirmStatus;
                return;
            }

            _tutorialResetArmed = false;
            if (FirstRunOnboardingUI.ResetTutorialProgress())
            {
                _tutorialResetButtonLabel.text = SettingsUI.TutorialResetDefaultLabel;
                _tutorialStatusLabel.text = SettingsUI.TutorialResetSuccessStatus;
                UiSoundFeedback.Instance?.PlaySuccess();
                return;
            }

            _tutorialResetButtonLabel.text = SettingsUI.TutorialResetDefaultLabel;
            _tutorialStatusLabel.text = SettingsUI.TutorialResetFailureStatus;
            UiSoundFeedback.Instance?.PlayFail();
        }

        private void ResetTutorialConfirmation()
        {
            _tutorialResetArmed = false;
            if (_tutorialResetButtonLabel != null)
                _tutorialResetButtonLabel.text = SettingsUI.TutorialResetDefaultLabel;
            if (_tutorialStatusLabel != null)
                _tutorialStatusLabel.text = SettingsUI.TutorialResetDefaultStatus;
        }

        private void UpdateVolumeLabels()
        {
            _sfxValueLabel.text = $"{Mathf.RoundToInt(SoundSettings.SfxVolume * 100f)}%";
            _ambienceValueLabel.text = $"{Mathf.RoundToInt(SoundSettings.AmbienceVolume * 100f)}%";
        }

        private void HandleInputModeChanged(UIInputMode mode)
        {
            ApplyInputMode(mode);
            if (mode != UIInputMode.Gamepad || _settingsOverlay.ClassListContains("is-open"))
                return;

            FocusPrimaryAction();
        }

        private void ApplyInputMode(UIInputMode mode)
        {
            _root.EnableInClassList("input--pointer", mode == UIInputMode.Pointer);
            _root.EnableInClassList("input--touch", mode == UIInputMode.Touch);
            _root.EnableInClassList("input--gamepad", mode == UIInputMode.Gamepad);
        }

        private void FocusPrimaryAction()
        {
            if (_continueButton.resolvedStyle.display != DisplayStyle.None)
                _continueButton.Focus();
            else
                _newRunButton.Focus();
        }

        private void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            float height = evt.newRect.height;
            _root.EnableInClassList("is-compact", width < 1280f || width / Mathf.Max(1f, height) < 1.58f);
            _root.EnableInClassList("is-short", height < 720f);
        }

        private void StartGame(GameBootstrap.StartAction action)
        {
            if (_startingGame || (action == GameBootstrap.StartAction.NewRun && RunPersistence.HasSave))
                return;

            _startingGame = true;
            if (action == GameBootstrap.StartAction.NewRun)
                RunPersistence.Delete();

            GameBootstrap.PendingAction = action;
            SceneManager.LoadScene(GameBootstrap.GameSceneName);
        }

        private void DisableLegacyMenu()
        {
            MainMenuSceneUI legacyMenu = FindFirstObjectByType<MainMenuSceneUI>(FindObjectsInactive.Include);
            if (legacyMenu != null)
                legacyMenu.enabled = false;

            SettingsUI legacySettings = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            if (legacySettings != null)
                legacySettings.enabled = false;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.gameObject.scene == gameObject.scene)
                    canvas.enabled = false;
            }
        }

        private void DisableDevelopmentOverlay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _developmentOverlaySuppressed = TryDisableDevelopmentOverlay();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool TryDisableDevelopmentOverlay()
        {
            DevelopmentTestPanel[] panels = Resources.FindObjectsOfTypeAll<DevelopmentTestPanel>();
            if (panels.Length == 0)
                return false;

            foreach (DevelopmentTestPanel panel in panels)
            {
                if (panel == null)
                    continue;

                panel.enabled = false;
                panel.gameObject.SetActive(false);
            }

            return true;
        }
#endif
    }
}
