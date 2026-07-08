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
        public Button CloseButton;

        private void Start()
        {
            if (SfxSlider != null)
                SfxSlider.onValueChanged.AddListener(v => SoundSettings.SfxVolume = v);
            if (AmbienceSlider != null)
                AmbienceSlider.onValueChanged.AddListener(v => SoundSettings.AmbienceVolume = v);
            if (CloseButton != null)
                CloseButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (SfxSlider != null)
                SfxSlider.SetValueWithoutNotify(SoundSettings.SfxVolume);
            if (AmbienceSlider != null)
                AmbienceSlider.SetValueWithoutNotify(SoundSettings.AmbienceVolume);
            if (SettingsPanel != null)
            {
                SettingsPanel.SetActive(true);
                SettingsPanel.transform.SetAsLastSibling(); // acan panelin ustunde
            }
        }

        public void Close()
        {
            if (SettingsPanel != null)
                SettingsPanel.SetActive(false);
        }
    }
}
