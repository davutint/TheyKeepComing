using System;
using TMPro;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadWalls
{
    public enum AbilityHotkeySlot
    {
        Fireball = 1,
        Rally = 2,
        EmergencyRepair = 3
    }

    /// <summary>
    /// Ates Topu UI'i (M-C buyuculuk + polish): cooldown gostergeli buton + hedefleme modu +
    /// UCAN MERMI gorseli + varista patlama flipbook'u. Sim otoritesi ECS'tedir:
    /// GameManager.TryCastFireball mermi entity'sini yaratir, bu sinif yalniz
    /// ActiveFireballProjectile pozisyon/rotasyonunu kopyalayip sprite cizer; entity yok
    /// olunca (varis) patlama flipbook'u hedef noktada bir kez oynar.
    /// Sprite kareleri setup tool tarafindan atanir (Super Pixel Projectiles/FX Pack 2).
    /// </summary>
    public class SpellCastUI : MonoBehaviour
    {
        public event Action<AbilityHotkeySlot> AbilityHotkeyAcceptedByPlayer;

        [Header("Bindings (setup tool baglar)")]
        public GameObject SpellPanel;
        public Button FireballButton;
        public Image FireballCooldownFill;
        public TMP_Text FireballLabelText;
        public Button RallyButton;
        public Image RallyCooldownFill;
        public TMP_Text RallyLabelText;
        public Button EmergencyRepairButton;
        public Image EmergencyRepairCooldownFill;
        public TMP_Text EmergencyRepairLabelText;

        [Header("Flipbook kareleri (setup tool sheet'lerden atar)")]
        public Sprite[] ProjectileFrames;
        public Sprite[] BlastFrames;
        public float ProjectileFps = 20f;
        public float BlastFps = 30f;

        private static readonly Color IndicatorColor = new Color(1f, 0.55f, 0.15f, 0.30f);

        private bool _targeting;
        private SpriteRenderer _targetingIndicator;
        private Sprite _circleSprite;
        private Camera _camera;
        private bool _buttonsBound;

        public bool IsTargeting => _targeting;

        // mermi takibi
        private Entity _trackedProjectile = Entity.Null;
        private Vector3 _lastProjectilePosition;
        private Vector2 _pendingBlastPosition;
        private float _pendingBlastRadius;
        private SpriteRenderer _projectileVisual;
        private float _projectileFrameTimer;
        private int _projectileFrame;

        // patlama
        private SpriteRenderer _blastVisual;
        private float _blastFrameTimer;
        private int _blastFrame = -1; // -1 = oynamiyor

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
            CancelTargeting();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool visible = gm != null && !gm.GameState.IsGameOver;
            if (SpellPanel != null && SpellPanel.activeSelf != visible)
                SpellPanel.SetActive(visible);

            UpdateProjectileVisual(gm);
            UpdateBlastVisual();

            if (!visible)
            {
                CancelTargeting();
                return;
            }

            HandleHotkeys(gm);
            UpdateAbilityVisuals(gm);

            if (_targeting && !gm.FireballReady)
                CancelTargeting();
            else if (_targeting)
                UpdateTargeting(gm);
        }

        private void BindButtons()
        {
            if (_buttonsBound)
                return;

            _buttonsBound = true;
            FireballButton?.onClick.AddListener(ToggleTargeting);
            RallyButton?.onClick.AddListener(HandleRallyClicked);
            EmergencyRepairButton?.onClick.AddListener(HandleEmergencyRepairClicked);
        }

        private void UnbindButtons()
        {
            if (!_buttonsBound)
                return;

            _buttonsBound = false;
            FireballButton?.onClick.RemoveListener(ToggleTargeting);
            RallyButton?.onClick.RemoveListener(HandleRallyClicked);
            EmergencyRepairButton?.onClick.RemoveListener(HandleEmergencyRepairClicked);
        }

        private void HandleHotkeys(GameManager gm)
        {
            if (IsTypingInInputField())
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                TryActivateHotkey(AbilityHotkeySlot.Fireball, gm);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                TryActivateHotkey(AbilityHotkeySlot.Rally, gm);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                TryActivateHotkey(AbilityHotkeySlot.EmergencyRepair, gm);
            }
        }

        public bool TryGetFirstReadyAbility(
            out AbilityHotkeySlot slot,
            out RectTransform target)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && TryGetReadyTarget(gm.FireballReady, FireballButton, out target))
            {
                slot = AbilityHotkeySlot.Fireball;
                return true;
            }

            if (gm != null && TryGetReadyTarget(gm.RallyReady, RallyButton, out target))
            {
                slot = AbilityHotkeySlot.Rally;
                return true;
            }

            if (gm != null
                && TryGetReadyTarget(gm.EmergencyRepairReady, EmergencyRepairButton, out target))
            {
                slot = AbilityHotkeySlot.EmergencyRepair;
                return true;
            }

            slot = default;
            target = null;
            return false;
        }

        private static bool TryGetReadyTarget(
            bool ready,
            Button button,
            out RectTransform target)
        {
            target = button != null ? button.transform as RectTransform : null;
            return ready && target != null && target.gameObject.activeInHierarchy;
        }

        private bool TryActivateHotkey(AbilityHotkeySlot slot, GameManager gm)
        {
            if (gm == null)
                return false;

            bool accepted = false;
            switch (slot)
            {
                case AbilityHotkeySlot.Fireball:
                {
                    bool wasTargeting = _targeting;
                    ToggleTargeting();
                    accepted = !wasTargeting && _targeting;
                    break;
                }

                case AbilityHotkeySlot.Rally:
                    CancelTargeting();
                    accepted = gm.TryUseRally();
                    break;

                case AbilityHotkeySlot.EmergencyRepair:
                    CancelTargeting();
                    accepted = gm.TryUseEmergencyRepair();
                    break;
            }

            if (!accepted)
                return false;

            AbilityHotkeyAcceptedByPlayer?.Invoke(slot);
            if (slot != AbilityHotkeySlot.Fireball)
                UiSoundFeedback.Instance?.PlaySuccess();
            return true;
        }

        private static bool IsTypingInInputField()
        {
            GameObject selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            return selected != null
                && (selected.GetComponent<TMP_InputField>() != null
                    || selected.GetComponent<InputField>() != null);
        }

        // ---------------------------------------------------------------------------
        // Mermi gorseli: ECS entity pozisyonunu kopyalar; entity olunce patlama tetiklenir
        // ---------------------------------------------------------------------------

        private void UpdateProjectileVisual(GameManager gm)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;
            var em = world.EntityManager;

            // yeni mermi firlatildi mi
            if (gm != null && gm.ActiveFireballProjectile != Entity.Null
                && gm.ActiveFireballProjectile != _trackedProjectile
                && em.Exists(gm.ActiveFireballProjectile))
            {
                _trackedProjectile = gm.ActiveFireballProjectile;
                var proj = em.GetComponentData<FireballProjectile>(_trackedProjectile);
                _pendingBlastPosition = new Vector2(proj.Target.x, proj.Target.y);
                _pendingBlastRadius = proj.Radius;
                EnsureProjectileVisual();
                _projectileVisual.gameObject.SetActive(true);
                _projectileFrame = 0;
                _projectileFrameTimer = 0f;
            }

            if (_trackedProjectile == Entity.Null)
                return;

            if (em.Exists(_trackedProjectile))
            {
                var transform = em.GetComponentData<LocalTransform>(_trackedProjectile);
                _lastProjectilePosition = new Vector3(transform.Position.x, transform.Position.y, 0f);
                _projectileVisual.transform.SetPositionAndRotation(_lastProjectilePosition, transform.Rotation);

                if (ProjectileFrames != null && ProjectileFrames.Length > 0)
                {
                    _projectileFrameTimer += Time.deltaTime;
                    if (_projectileFrameTimer >= 1f / Mathf.Max(1f, ProjectileFps))
                    {
                        _projectileFrameTimer = 0f;
                        _projectileFrame = (_projectileFrame + 1) % ProjectileFrames.Length;
                        _projectileVisual.sprite = ProjectileFrames[_projectileFrame];
                    }
                }
            }
            else
            {
                // varis: mermi silindi — patlamayi hedefte oynat
                _trackedProjectile = Entity.Null;
                if (_projectileVisual != null)
                    _projectileVisual.gameObject.SetActive(false);
                StartBlast(_pendingBlastPosition, _pendingBlastRadius);
            }
        }

        private void EnsureProjectileVisual()
        {
            if (_projectileVisual != null)
                return;

            var go = new GameObject("FireballProjectileVisual");
            _projectileVisual = go.AddComponent<SpriteRenderer>();
            if (ProjectileFrames != null && ProjectileFrames.Length > 0)
                _projectileVisual.sprite = ProjectileFrames[0];
            _projectileVisual.sortingLayerName = "Wall"; // savas alani gorselleriyle ayni katman — kesin ustte
            _projectileVisual.sortingOrder = 210;
            go.SetActive(false);
        }

        private void StartBlast(Vector2 position, float radius)
        {
            if (BlastFrames == null || BlastFrames.Length == 0)
                return;

            if (_blastVisual == null)
            {
                var go = new GameObject("FireballBlastVisual");
                _blastVisual = go.AddComponent<SpriteRenderer>();
                _blastVisual.sortingLayerName = "Wall";
                _blastVisual.sortingOrder = 211;
            }

            // patlama gorseli etki alanini kaplasin: sprite dunya boyutuna gore olcekle
            float spriteWorldSize = BlastFrames[0].bounds.size.x;
            float scale = spriteWorldSize > 0.01f ? radius * 2.4f / spriteWorldSize : 1f;
            _blastVisual.transform.position = new Vector3(position.x, position.y, 0f);
            _blastVisual.transform.localScale = Vector3.one * scale;
            _blastVisual.transform.rotation = Quaternion.identity;
            _blastVisual.sprite = BlastFrames[0];
            _blastVisual.gameObject.SetActive(true);
            _blastFrame = 0;
            _blastFrameTimer = 0f;
        }

        private void UpdateBlastVisual()
        {
            if (_blastFrame < 0 || _blastVisual == null)
                return;

            _blastFrameTimer += Time.deltaTime;
            if (_blastFrameTimer < 1f / Mathf.Max(1f, BlastFps))
                return;

            _blastFrameTimer = 0f;
            _blastFrame++;
            if (BlastFrames == null || _blastFrame >= BlastFrames.Length)
            {
                _blastFrame = -1;
                _blastVisual.gameObject.SetActive(false);
                return;
            }
            _blastVisual.sprite = BlastFrames[_blastFrame];
        }

        // ---------------------------------------------------------------------------

        private void UpdateAbilityVisuals(GameManager gm)
        {
            SetCooldownFill(
                FireballCooldownFill,
                gm.FireballCooldownRemaining,
                gm.FireballCooldownDuration);
            if (FireballLabelText != null)
            {
                string state = !gm.FireballUnlocked
                    ? "LOCKED"
                    : _targeting
                        ? "SELECT AREA"
                        : FormatCooldownState(gm.FireballCooldownRemaining);
                FireballLabelText.text = $"[1] FIREBALL\n{state}";
            }
            if (FireballButton != null)
                FireballButton.interactable = gm.FireballReady;

            SetCooldownFill(
                RallyCooldownFill,
                gm.RallyCooldownRemaining,
                gm.RallyCooldownDuration);
            if (RallyLabelText != null)
            {
                string state = !gm.RallyUnlocked
                    ? "LOCKED"
                    : gm.RallyActive
                        ? $"ACTIVE {Mathf.CeilToInt(gm.RallyActiveRemaining)}s"
                        : FormatCooldownState(gm.RallyCooldownRemaining);
                RallyLabelText.text = $"[2] RALLY\n{state}";
            }
            if (RallyButton != null)
                RallyButton.interactable = gm.RallyReady;

            SetCooldownFill(
                EmergencyRepairCooldownFill,
                gm.EmergencyRepairCooldownRemaining,
                gm.EmergencyRepairCooldownDuration);
            if (EmergencyRepairLabelText != null)
            {
                string state;
                if (!gm.EmergencyRepairUnlocked)
                    state = "LOCKED";
                else if (gm.EmergencyRepairCooldownRemaining > 0f)
                    state = FormatCooldownState(gm.EmergencyRepairCooldownRemaining);
                else if (gm.ContinuousSiegeCycle.Phase != SiegeCyclePhase.Night)
                    state = "NIGHT ONLY";
                else if (gm.GetDefensePercent() >= 0.995f)
                    state = "WALL FULL";
                else
                    state = "READY";
                EmergencyRepairLabelText.text = $"[3] REPAIR\n{state}";
            }
            if (EmergencyRepairButton != null)
                EmergencyRepairButton.interactable = gm.EmergencyRepairReady;
        }

        private static void SetCooldownFill(Image fill, float remaining, float duration)
        {
            if (fill != null)
                fill.fillAmount = Mathf.Clamp01(remaining / Mathf.Max(0.01f, duration));
        }

        private static string FormatCooldownState(float remaining)
        {
            return remaining > 0f ? $"{Mathf.CeilToInt(remaining)}s" : "READY";
        }

        private void HandleRallyClicked()
        {
            CancelTargeting();
            if (GameManager.Instance != null && GameManager.Instance.TryUseRally())
                UiSoundFeedback.Instance?.PlaySuccess();
        }

        private void HandleEmergencyRepairClicked()
        {
            CancelTargeting();
            if (GameManager.Instance != null && GameManager.Instance.TryUseEmergencyRepair())
                UiSoundFeedback.Instance?.PlaySuccess();
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
                    CancelTargeting();
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
            _targetingIndicator.sortingLayerName = "Wall";
            _targetingIndicator.sortingOrder = 200;
            go.SetActive(false);
        }

        /// <summary>1 dunya-birimi capinda, kenari yumusak radial daire (hedefleme halkasi).</summary>
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
                    float alpha = Mathf.Clamp01((1f - dist) / 0.15f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circleSprite;
        }
    }
}
