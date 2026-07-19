using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// UI ses geribildirimi (Polish 3): oyun artik dokunuldugunu hissettirir.
    /// - TIK: her pointer-down'da raycast ile interactable Button aranir, bulunursa tik
    ///   calar — runtime klonlanan butonlar (magaza satirlari) dahil, listener eklemek
    ///   gerekmez; iki sahnede de calisir (Canvas'ta yasar, setup kurar).
    /// - OLAY SESLERI: PlaySuccess/PlayFail/PlayDeathSting — satin alma noktalarindan
    ///   cagirilir (static Instance koprusu). Volume SoundSettings.SfxVolume'a tabidir.
    /// </summary>
    public class UiSoundFeedback : MonoBehaviour
    {
        public static UiSoundFeedback Instance { get; private set; }

        [Header("Clips (setup tool yalniz-bossa atar)")]
        public AudioClip ClickClip;
        public AudioClip SuccessClip;
        public AudioClip FailClip;
        public AudioClip DeathStingClip;

        [Header("Mix")]
        [Range(0f, 1f)] public float ClickVolume = 0.35f;
        [Range(0f, 1f)] public float SuccessVolume = 0.5f;
        [Range(0f, 1f)] public float FailVolume = 0.45f;
        [Range(0f, 1f)] public float StingVolume = 0.7f;

        private AudioSource _source;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        private void Awake()
        {
            Instance = this;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // UI sesi 2D
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || ClickClip == null || EventSystem.current == null)
                return;

            var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(pointer, _raycastResults);
            foreach (var result in _raycastResults)
            {
                var button = result.gameObject.GetComponentInParent<Button>();
                if (button != null && button.interactable)
                {
                    Play(ClickClip, ClickVolume);
                    return;
                }
            }
        }

        public void PlaySuccess() => Play(SuccessClip, SuccessVolume);
        public void PlayFail() => Play(FailClip, FailVolume);
        public void PlayDeathSting() => Play(DeathStingClip, StingVolume);
        public void PlayClick() => Play(ClickClip, ClickVolume);

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _source == null)
                return;

            _source.PlayOneShot(clip, volume * SoundSettings.SfxVolume);
        }
    }
}
