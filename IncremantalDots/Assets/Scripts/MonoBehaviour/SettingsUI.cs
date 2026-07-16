using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Ses ayarlari paneli (M-E): SFX + ambiyans slider'lari SoundSettings'e (PlayerPrefs)
    /// yazar — degisiklik ANINDA etkir (bridge/ambient her calmada carpani okur).
    /// Ana menu ve pause menusu ayni paneli paylasir (Open/Close).
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Bindings (setup tool baglar)")]
        public GameObject SettingsPanel;
        public Slider SfxSlider;
        public Slider AmbienceSlider;
        public Button TutorialResetButton;
        public TMP_Text TutorialResetLabel;
        public TMP_Text TutorialResetStatusText;
        public Button CloseButton;

        private const string ResetLabel = "RESET TUTORIAL";
        private const string ResetStatus = "RESETS ONBOARDING ONLY. RUN AND UPGRADES STAY.";
        private bool _tutorialResetArmed;

        private void Start()
        {
            if (SfxSlider != null)
                SfxSlider.onValueChanged.AddListener(v => SoundSettings.SfxVolume = v);
            if (AmbienceSlider != null)
                AmbienceSlider.onValueChanged.AddListener(v => SoundSettings.AmbienceVolume = v);
            if (TutorialResetButton != null)
                TutorialResetButton.onClick.AddListener(HandleTutorialResetClicked);
            if (CloseButton != null)
                CloseButton.onClick.AddListener(Close);

            ResetTutorialConfirmation();
        }

        public void Open()
        {
            if (SfxSlider != null)
                SfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            if (AmbienceSlider != null)
                AmbienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            ResetTutorialConfirmation();
            if (SettingsPanel != null)
            {
                SettingsPanel.SetActive(true);
                SettingsPanel.transform.SetAsLastSibling(); // acan panelin ustunde
            }
        }

        public void Close()
        {
            ResetTutorialConfirmation();
            if (SettingsPanel != null)
                SettingsPanel.SetActive(false);
        }

        private void HandleTutorialResetClicked()
        {
            if (!_tutorialResetArmed)
            {
                _tutorialResetArmed = true;
                SetTutorialResetCopy(
                    "CONFIRM RESET",
                    "CLICK AGAIN TO RESET ALL TUTORIAL STEPS.");
                return;
            }

            _tutorialResetArmed = false;
            if (FirstRunOnboardingUI.ResetTutorialProgress())
            {
                SetTutorialResetCopy(
                    ResetLabel,
                    "TUTORIAL RESET. IT WILL START AGAIN IN GAME.");
                UiSoundFeedback.Instance?.PlaySuccess();
                return;
            }

            SetTutorialResetCopy(
                ResetLabel,
                "RESET FAILED. META SAVE WAS NOT CHANGED.");
            UiSoundFeedback.Instance?.PlayFail();
        }

        private void ResetTutorialConfirmation()
        {
            _tutorialResetArmed = false;
            SetTutorialResetCopy(ResetLabel, ResetStatus);
        }

        private void SetTutorialResetCopy(string label, string status)
        {
            if (TutorialResetLabel != null)
                TutorialResetLabel.text = label;
            if (TutorialResetStatusText != null)
                TutorialResetStatusText.text = status;
        }
    }
}
