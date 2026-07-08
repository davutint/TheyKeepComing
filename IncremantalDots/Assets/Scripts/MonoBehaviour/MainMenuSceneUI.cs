using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Ana menu sahnesi controller'i (M-E v2 — ayri sahne, owner istegi "guzel olsun").
    /// Gorsel kimlik koddan uretilir (asset bagimliligi yok, Inspector'dan override edilebilir):
    /// gece gradyani arka plan + kanli ay (glow'lu, yavas nabiz) + rounded-rect butonlar +
    /// DOTween giris animasyonlari. CONTINUE yalniz kayit varsa gorunur ("CONTINUE — DAY X").
    /// Secim GameBootstrap.PendingAction'a yazilir; oyun sahnesindeki RunBootstrap uygular.
    /// </summary>
    public class MainMenuSceneUI : MonoBehaviour
    {
        [Header("Bindings (setup tool baglar)")]
        public Image BackgroundImage;
        public Image MoonGlowImage;
        public Image MoonImage;
        public TMP_Text TitleText;
        public TMP_Text TaglineText;
        public TMP_Text VersionText;
        public CanvasGroup ButtonsGroup;
        public Button ContinueButton;
        public TMP_Text ContinueLabelText;
        public Button NewRunButton;
        public Button SettingsButton;
        public SettingsUI Settings;
        [Tooltip("Menu arka plan ambiyansi (loop; setup atar). Volume SoundSettings.AmbienceVolume'a tabidir.")]
        public AudioSource AmbienceSource;
        [Range(0f, 1f)] public float AmbienceVolume = 0.22f;

        private static readonly Color NightTop = new Color(0.030f, 0.034f, 0.075f);
        private static readonly Color NightMid = new Color(0.045f, 0.030f, 0.055f);
        private static readonly Color HorizonRed = new Color(0.16f, 0.045f, 0.045f);
        private static readonly Color MoonColor = new Color(0.80f, 0.22f, 0.15f, 0.90f);
        private static readonly Color MoonGlow = new Color(0.75f, 0.15f, 0.10f, 0.16f);

        private void Start()
        {
            ApplyGeneratedVisuals();
            ConfigureButtons();
            PlayIntroAnimation();

            if (AmbienceSource != null && AmbienceSource.clip != null)
            {
                AmbienceSource.loop = true;
                AmbienceSource.spatialBlend = 0f;
                AmbienceSource.volume = AmbienceVolume * SoundSettings.AmbienceVolume;
                AmbienceSource.Play();
            }
        }

        private void Update()
        {
            // ayar slider'i canli etki etsin
            if (AmbienceSource != null && AmbienceSource.isPlaying)
                AmbienceSource.volume = AmbienceVolume * SoundSettings.AmbienceVolume;
        }

        // ---------------------------------------------------------------------------
        // Gorsel uretim: Inspector'da sprite atanmissa dokunulmaz (owner override yolu)
        // ---------------------------------------------------------------------------

        private void ApplyGeneratedVisuals()
        {
            if (BackgroundImage != null && BackgroundImage.sprite == null)
            {
                BackgroundImage.sprite = MenuSpriteFactory.CreateVerticalGradient(NightTop, NightMid, HorizonRed);
                BackgroundImage.color = Color.white;
            }

            Sprite moonSprite = MenuSpriteFactory.CreateSoftCircle();
            if (MoonImage != null && MoonImage.sprite == null)
            {
                MoonImage.sprite = moonSprite;
                MoonImage.color = MoonColor;
            }
            if (MoonGlowImage != null && MoonGlowImage.sprite == null)
            {
                MoonGlowImage.sprite = moonSprite;
                MoonGlowImage.color = MoonGlow;
            }

            Sprite rounded = MenuSpriteFactory.CreateRoundedRect();
            StyleButton(ContinueButton, rounded, new Color(0.62f, 0.22f, 0.12f, 1f));
            StyleButton(NewRunButton, rounded, new Color(0.13f, 0.30f, 0.18f, 1f));
            StyleButton(SettingsButton, rounded, new Color(0.13f, 0.15f, 0.21f, 1f));
        }

        private static void StyleButton(Button button, Sprite rounded, Color baseColor)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = baseColor * 1.35f;
            colors.pressedColor = baseColor * 0.75f;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        // ---------------------------------------------------------------------------

        private void ConfigureButtons()
        {
            bool hasSave = RunPersistence.HasSave;
            RunSaveState save = hasSave ? RunPersistence.TryLoad() : null;
            hasSave = save != null;

            if (ContinueButton != null)
            {
                ContinueButton.gameObject.SetActive(hasSave);
                if (hasSave && ContinueLabelText != null)
                    ContinueLabelText.text = $"CONTINUE — DAY {save.CycleIndex + 2}";
                ContinueButton.onClick.AddListener(() => StartGame(GameBootstrap.StartAction.Continue));
            }

            if (NewRunButton != null)
                NewRunButton.onClick.AddListener(() => StartGame(GameBootstrap.StartAction.NewRun));
            if (SettingsButton != null)
                SettingsButton.onClick.AddListener(() => Settings?.Open());
        }

        private void StartGame(GameBootstrap.StartAction action)
        {
            if (action == GameBootstrap.StartAction.NewRun)
                RunPersistence.Delete();

            GameBootstrap.PendingAction = action;
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameBootstrap.GameSceneName);
        }

        private void PlayIntroAnimation()
        {
            // baslik: yukaridan yumusak inis + fade
            if (TitleText != null)
            {
                var rect = TitleText.rectTransform;
                Vector2 target = rect.anchoredPosition;
                rect.anchoredPosition = target + new Vector2(0f, 26f);
                TitleText.alpha = 0f;
                TitleText.DOFade(1f, 0.7f).SetEase(Ease.OutQuad);
                rect.DOAnchorPos(target, 0.7f).SetEase(Ease.OutCubic);
            }

            if (TaglineText != null)
            {
                TaglineText.alpha = 0f;
                TaglineText.DOFade(0.75f, 0.9f).SetDelay(0.35f);
            }

            // butonlar: grup olarak fade + hafif yukselis
            if (ButtonsGroup != null)
            {
                var rect = (RectTransform)ButtonsGroup.transform;
                Vector2 target = rect.anchoredPosition;
                rect.anchoredPosition = target - new Vector2(0f, 22f);
                ButtonsGroup.alpha = 0f;
                ButtonsGroup.DOFade(1f, 0.55f).SetDelay(0.45f);
                rect.DOAnchorPos(target, 0.55f).SetDelay(0.45f).SetEase(Ease.OutCubic);
            }

            // kanli ay: cok yavas nabiz (sahne yasadikca)
            if (MoonImage != null)
                MoonImage.transform.DOScale(1.045f, 4.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            if (MoonGlowImage != null)
                MoonGlowImage.transform.DOScale(1.09f, 5.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    /// <summary>Menu gorselleri icin runtime sprite ureticileri (cache'li; asset bagimliligi yok).</summary>
    public static class MenuSpriteFactory
    {
        private static Sprite _gradient;
        private static Sprite _softCircle;
        private static Sprite _roundedRect;

        /// <summary>Ust -> orta -> alt uc renkli dikey gradyan (tam ekran arka plan).</summary>
        public static Sprite CreateVerticalGradient(Color top, Color mid, Color bottom)
        {
            if (_gradient != null)
                return _gradient;

            const int height = 256;
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1); // 0 = alt, 1 = ust
                Color c = t > 0.45f
                    ? Color.Lerp(mid, top, (t - 0.45f) / 0.55f)
                    : Color.Lerp(bottom, mid, t / 0.45f);
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            _gradient = Sprite.Create(tex, new Rect(0, 0, 4, height), new Vector2(0.5f, 0.5f), 100f);
            return _gradient;
        }

        /// <summary>Yumusak kenarli dolu daire (ay + glow; smoothstep falloff).</summary>
        public static Sprite CreateSoftCircle()
        {
            if (_softCircle != null)
                return _softCircle;

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    // ic %78 dolu, kenara dogru smoothstep erime
                    float t = Mathf.Clamp01((1f - dist) / 0.22f);
                    float alpha = t * t * (3f - 2f * t);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _softCircle;
        }

        /// <summary>Yuvarlak koseli beyaz kutu — 9-slice (Image.Type.Sliced ile her boyutta temiz).</summary>
        public static Sprite CreateRoundedRect()
        {
            if (_roundedRect != null)
                return _roundedRect;

            const int size = 64;
            const float radius = 18f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // kose merkezlerine gore signed distance (rounded rect)
                    float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                    float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f); // 1px anti-alias
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            // border = radius+2: koseler bozulmadan 9-slice gerilir
            _roundedRect = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
            return _roundedRect;
        }
    }
}
