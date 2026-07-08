using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Oyuncu ses ayarlari (M-E): SFX ve ambiyans icin global carpanlar. PlayerPrefs'te
    /// kalici (cihaz-bazli tercih; run-save'e girmez). Tuketiciler:
    /// - CombatFeedbackBridge.PlaySfx -> SfxVolume (tum combat SFX tek noktadan gecer)
    /// - AmbientAudioController -> AmbienceVolume (loop hedef sesi + sting)
    /// UI: SettingsPanel slider'lari (SettingsUI) yazar.
    /// </summary>
    public static class SoundSettings
    {
        private const string SfxKey = "dw_sfx_volume";
        private const string AmbienceKey = "dw_ambience_volume";

        private static float _sfx = -1f;
        private static float _ambience = -1f;

        public static float SfxVolume
        {
            get
            {
                if (_sfx < 0f)
                    _sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
                return _sfx;
            }
            set
            {
                _sfx = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxKey, _sfx);
            }
        }

        public static float AmbienceVolume
        {
            get
            {
                if (_ambience < 0f)
                    _ambience = PlayerPrefs.GetFloat(AmbienceKey, 1f);
                return _ambience;
            }
            set
            {
                _ambience = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(AmbienceKey, _ambience);
            }
        }
    }
}
