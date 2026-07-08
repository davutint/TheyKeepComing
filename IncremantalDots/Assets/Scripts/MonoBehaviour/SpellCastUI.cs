using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Ates Topu UI'i (M-C buyuculuk): cooldown gostergeli buton + hedefleme modu + patlama
    /// gorseli. arcane_tower tech'i alinana kadar panel gizlidir. Akis: butona bas ->
    /// hedefleme (dunya-uzayi yaricap dairesi imleci izler) -> sol tik = cast
    /// (GameManager.TryCastFireball), sag tik/ESC = iptal. Gorseller runtime uretilen radial
    /// sprite'tir (asset bagimliligi yok); kalici VFX kanali M-D isi.
    /// Controller SpellUiRoot uzerinde yasar (hep aktif); paneli kendisi ac/kapar.
    /// </summary>
    public class SpellCastUI : MonoBehaviour
    {
        [Header("Bindings (setup tool baglar)")]
        public GameObject SpellPanel;
        public Button FireballButton;
        public Image FireballCooldownFill;
        public TMP_Text FireballLabelText;

        private static readonly Color IndicatorColor = new Color(1f, 0.55f, 0.15f, 0.30f);
        private static readonly Color BlastColor = new Color(1f, 0.45f, 0.10f, 0.85f);

        private bool _targeting;
        private SpriteRenderer _targetingIndicator;
        private Sprite _circleSprite;
        private Camera _camera;

        private void Start()
        {
            if (FireballButton != null)
                FireballButton.onClick.AddListener(ToggleTargeting);
        }

        private void OnDisable()
        {
            CancelTargeting();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool visible = gm != null && gm.FireballUnlocked && !gm.GameState.IsGameOver;
            if (SpellPanel != null && SpellPanel.activeSelf != visible)
                SpellPanel.SetActive(visible);

            if (!visible)
            {
                CancelTargeting();
                return;
            }

            UpdateCooldownVisual(gm);

            if (_targeting)
                UpdateTargeting(gm);
        }

        private void UpdateCooldownVisual(GameManager gm)
        {
            float duration = Mathf.Max(0.01f, gm.FireballCooldownDuration);
            float remaining = gm.FireballCooldownRemaining;
            bool ready = gm.FireballReady;

            if (FireballCooldownFill != null)
                FireballCooldownFill.fillAmount = Mathf.Clamp01(remaining / duration);

            if (FireballLabelText != null)
                FireballLabelText.text = ready ? "FIREBALL" : $"{Mathf.CeilToInt(remaining)}s";

            if (FireballButton != null)
                FireballButton.interactable = ready;
        }

        private void ToggleTargeting()
        {
            if (_targeting)
            {
                CancelTargeting();
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || !gm.FireballReady)
                return;

            _targeting = true;
            EnsureIndicator();
            _targetingIndicator.gameObject.SetActive(true);
        }

        private void UpdateTargeting(GameManager gm)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
                return;
            }

            Vector3 world = GetMouseWorldPosition();
            float diameter = gm.FireballRadius * 2f;
            _targetingIndicator.transform.position = new Vector3(world.x, world.y, 0f);
            _targetingIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);

            if (Input.GetMouseButtonDown(0))
            {
                // UI ustune tiklama cast sayilmaz (buton/panel korunur)
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                if (gm.TryCastFireball(new Vector2(world.x, world.y)))
                {
                    SpawnBlastVisual(world, gm.FireballRadius);
                    CancelTargeting();
                }
            }
        }

        private void CancelTargeting()
        {
            _targeting = false;
            if (_targetingIndicator != null)
                _targetingIndicator.gameObject.SetActive(false);
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return Vector3.zero;

            Vector3 world = _camera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            return world;
        }

        private void EnsureIndicator()
        {
            if (_targetingIndicator != null)
                return;

            var go = new GameObject("FireballTargetingIndicator");
            _targetingIndicator = go.AddComponent<SpriteRenderer>();
            _targetingIndicator.sprite = GetCircleSprite();
            _targetingIndicator.color = IndicatorColor;
            _targetingIndicator.sortingOrder = 200;
            go.SetActive(false);
        }

        private void SpawnBlastVisual(Vector3 position, float radius)
        {
            var go = new GameObject("FireballBlastVfx");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color = BlastColor;
            sr.sortingOrder = 210;

            float diameter = radius * 2f;
            go.transform.localScale = Vector3.one * (diameter * 0.25f);
            DOTween.Sequence()
                .Append(go.transform.DOScale(diameter, 0.22f).SetEase(Ease.OutCubic))
                .Join(sr.DOFade(0f, 0.45f).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(go));
        }

        /// <summary>1 dunya-birimi capinda, kenari yumusak radial daire (runtime uretim, cache'li).</summary>
        private Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
                return _circleSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    // merkez dolu, kenara dogru yumusak dusus (0.85..1.0 bandinda erir)
                    float alpha = Mathf.Clamp01((1f - dist) / 0.15f);
                    byte a = (byte)(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circleSprite;
        }
    }
}
