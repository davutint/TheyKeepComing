using System;
using System.Collections.Generic;
using Unity.Collections;
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

        [Header("Spell Feedback Hierarchy")]
        public string SpellSortingLayer = SpellFeedbackHierarchy.SortingLayer;
        public int FireballProjectileSortingOrder =
            SpellFeedbackHierarchy.FireballProjectileSortingOrder;
        public int FireballProjectileAuraSortingOrder =
            SpellFeedbackHierarchy.FireballProjectileAuraSortingOrder;
        public int FireballBlastSortingOrder =
            SpellFeedbackHierarchy.FireballBlastSortingOrder;
        public int FireballBlastCoreSortingOrder =
            SpellFeedbackHierarchy.FireballBlastCoreSortingOrder;
        public int FireballBlastRingSortingOrder =
            SpellFeedbackHierarchy.FireballBlastRingSortingOrder;
        public float FireballProjectileAuraDiameter =
            SpellFeedbackHierarchy.FireballProjectileAuraDiameter;
        public float FireballProjectileAuraPulse =
            SpellFeedbackHierarchy.FireballProjectileAuraPulse;
        public float FireballBlastDiameterMultiplier =
            SpellFeedbackHierarchy.FireballBlastDiameterMultiplier;
        public float FireballBlastCoreDiameterMultiplier =
            SpellFeedbackHierarchy.FireballBlastCoreDiameterMultiplier;
        public float FireballBlastRingDiameterMultiplier =
            SpellFeedbackHierarchy.FireballBlastRingDiameterMultiplier;
        public float FireballBlastRingStartScale =
            SpellFeedbackHierarchy.FireballBlastRingStartScale;
        public float FireballBlastRingEndScale =
            SpellFeedbackHierarchy.FireballBlastRingEndScale;
        public Color FireballProjectileAuraColor =
            SpellFeedbackHierarchy.FireballProjectileAuraColor;
        public Color FireballBlastCoreColor =
            SpellFeedbackHierarchy.FireballBlastCoreColor;
        public Color FireballBlastRingColor =
            SpellFeedbackHierarchy.FireballBlastRingColor;

        private static readonly Color IndicatorColor = new Color(1f, 0.55f, 0.15f, 0.30f);

        private bool _targeting;
        private SpriteRenderer _targetingIndicator;
        private Sprite _circleSprite;
        private Camera _camera;
        private bool _buttonsBound;
        private bool _legacyAbilityPanelVisible = true;

        public bool IsTargeting => _targeting;

        // mermi takibi
        private Entity _trackedProjectile = Entity.Null;
        private Vector3 _lastProjectilePosition;
        private Vector2 _pendingBlastPosition;
        private float _pendingBlastRadius;
        private SpriteRenderer _projectileVisual;
        private SpriteRenderer _projectileAuraVisual;
        private float _projectileFrameTimer;
        private int _projectileFrame;

        // patlama
        private SpriteRenderer _blastVisual;
        private SpriteRenderer _blastCoreVisual;
        private SpriteRenderer _blastRingVisual;
        private Sprite _hierarchyRingSprite;
        private float _blastRingBaseDiameter;
        private float _blastFrameTimer;
        private int _blastFrame = -1; // -1 = oynamiyor
        private Color _activeBlastCoreColor;
        private Color _activeBlastRingColor;

        private sealed class BurningGroundVisual
        {
            public GameObject Root;
            public SpriteRenderer Fill;
            public SpriteRenderer Ring;
        }

        private readonly Dictionary<Entity, BurningGroundVisual> _burningGroundVisuals =
            new Dictionary<Entity, BurningGroundVisual>();
        private readonly HashSet<Entity> _liveBurningGrounds = new HashSet<Entity>();
        private readonly HashSet<Entity> _presentedEvolutionStrikes = new HashSet<Entity>();
        private readonly List<Entity> _staleEvolutionEntities = new List<Entity>();
        private World _evolutionQueryWorld;
        private EntityQuery _burningGroundQuery;
        private EntityQuery _fireballStrikeQuery;
        private bool _evolutionQueriesCreated;

        public int ActiveBurningGroundVisualCount => _burningGroundVisuals.Count;

        private void OnEnable()
        {
            BindButtons();
        }

        private void OnDisable()
        {
            UnbindButtons();
            CancelTargeting();
            HideRuntimeSpellVisuals();
        }

        private void OnDestroy()
        {
            DestroyGeneratedSprite(ref _circleSprite);
            DestroyGeneratedSprite(ref _hierarchyRingSprite);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool visible = gm != null && !gm.GameState.IsGameOver;
            bool panelVisible = visible && _legacyAbilityPanelVisible;
            if (SpellPanel != null && SpellPanel.activeSelf != panelVisible)
                SpellPanel.SetActive(panelVisible);

            UpdateProjectileVisual(gm);
            UpdateEvolutionVisuals();
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
            FireballButton?.onClick.AddListener(HandleFireballClicked);
            RallyButton?.onClick.AddListener(HandleRallyClicked);
            EmergencyRepairButton?.onClick.AddListener(HandleEmergencyRepairClicked);
        }

        private void UnbindButtons()
        {
            if (!_buttonsBound)
                return;

            _buttonsBound = false;
            FireballButton?.onClick.RemoveListener(HandleFireballClicked);
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
            // UI Toolkit owns the visible ability dock, but the legacy button remains the
            // stable onboarding/action contract. Its GameObject can therefore be hidden.
            return ready && target != null;
        }

        private bool TryActivateHotkey(AbilityHotkeySlot slot, GameManager gm)
        {
            return TryActivateAbility(slot, gm, true);
        }

        private bool TryActivateAbility(
            AbilityHotkeySlot slot,
            GameManager gm,
            bool reportAcceptedTutorialInput)
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

            if (reportAcceptedTutorialInput)
                AbilityHotkeyAcceptedByPlayer?.Invoke(slot);
            if (slot != AbilityHotkeySlot.Fireball)
                UiSoundFeedback.Instance?.PlaySuccess();
            return true;
        }

        public bool TryActivateAbilityFromPlayer(AbilityHotkeySlot slot)
        {
            return TryActivateAbility(slot, GameManager.Instance, true);
        }

        public void SetLegacyAbilityPanelVisible(bool visible)
        {
            _legacyAbilityPanelVisible = visible;
            if (!visible && SpellPanel != null)
                SpellPanel.SetActive(false);
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
                _lastProjectilePosition = new Vector3(
                    transform.Position.x,
                    transform.Position.y,
                    MobileCastleRenderDepth.ProjectileZ);
                _projectileVisual.transform.SetPositionAndRotation(_lastProjectilePosition, transform.Rotation);
                UpdateProjectileAura();

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
                StartBlast(
                    _pendingBlastPosition,
                    _pendingBlastRadius,
                    FireballStrikeKind.Primary);
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
            _projectileVisual.sortingLayerName = SpellSortingLayer;
            _projectileVisual.sortingOrder = FireballProjectileSortingOrder;

            var auraObject = new GameObject("FireballProjectileHierarchyAura");
            auraObject.transform.SetParent(go.transform, false);
            _projectileAuraVisual = auraObject.AddComponent<SpriteRenderer>();
            _projectileAuraVisual.sprite = GetCircleSprite();
            _projectileAuraVisual.color = FireballProjectileAuraColor;
            _projectileAuraVisual.sortingLayerName = SpellSortingLayer;
            _projectileAuraVisual.sortingOrder = FireballProjectileAuraSortingOrder;
            UpdateProjectileAura();
            go.SetActive(false);
        }

        private void UpdateProjectileAura()
        {
            if (_projectileAuraVisual == null)
                return;

            float diameter = SpellFeedbackHierarchy.ResolveProjectileAuraDiameter(
                FireballProjectileAuraDiameter,
                FireballProjectileAuraPulse,
                Time.time);
            _projectileAuraVisual.transform.localScale = Vector3.one * diameter;
            _projectileAuraVisual.color = FireballProjectileAuraColor;
        }

        private void StartBlast(
            Vector2 position,
            float radius,
            FireballStrikeKind kind)
        {
            if (BlastFrames == null || BlastFrames.Length == 0)
                return;

            if (_blastVisual == null)
            {
                var go = new GameObject("FireballBlastVisual");
                _blastVisual = go.AddComponent<SpriteRenderer>();
                _blastVisual.sortingLayerName = SpellSortingLayer;
                _blastVisual.sortingOrder = FireballBlastSortingOrder;
            }

            if (_blastRingVisual == null)
            {
                var ringObject = new GameObject("FireballBlastHierarchyRing");
                _blastRingVisual = ringObject.AddComponent<SpriteRenderer>();
                _blastRingVisual.sprite = GetHierarchyRingSprite();
                _blastRingVisual.sortingLayerName = SpellSortingLayer;
                _blastRingVisual.sortingOrder = FireballBlastRingSortingOrder;
            }

            if (_blastCoreVisual == null)
            {
                var coreObject = new GameObject("FireballBlastHierarchyCore");
                _blastCoreVisual = coreObject.AddComponent<SpriteRenderer>();
                _blastCoreVisual.sprite = GetCircleSprite();
                _blastCoreVisual.sortingLayerName = SpellSortingLayer;
                _blastCoreVisual.sortingOrder = FireballBlastCoreSortingOrder;
            }

            float spriteWorldSize = BlastFrames[0].bounds.size.x;
            float scale = SpellFeedbackHierarchy.ResolveFireballBlastScale(
                radius,
                spriteWorldSize,
                FireballBlastDiameterMultiplier);
            Vector3 presentationPosition = new Vector3(
                position.x,
                position.y,
                MobileCastleRenderDepth.ProjectileZ);
            _blastVisual.transform.position = presentationPosition;
            _blastVisual.transform.localScale = Vector3.one * scale;
            _blastVisual.transform.rotation = Quaternion.identity;
            _blastVisual.sprite = BlastFrames[0];
            _blastVisual.color = kind == FireballStrikeKind.SecondBlast
                ? new Color(1f, 0.72f, 0.28f, 0.96f)
                : Color.white;
            _blastVisual.gameObject.SetActive(true);

            _activeBlastCoreColor = kind == FireballStrikeKind.SecondBlast
                ? SpellFeedbackHierarchy.SecondBlastCoreColor
                : FireballBlastCoreColor;
            _activeBlastRingColor = kind == FireballStrikeKind.SecondBlast
                ? SpellFeedbackHierarchy.SecondBlastRingColor
                : FireballBlastRingColor;

            _blastCoreVisual.transform.position = presentationPosition;
            _blastCoreVisual.transform.rotation = Quaternion.identity;
            _blastCoreVisual.transform.localScale = Vector3.one
                * Mathf.Max(0.1f, radius)
                * Mathf.Max(1f, FireballBlastCoreDiameterMultiplier);
            _blastCoreVisual.color = _activeBlastCoreColor;
            _blastCoreVisual.gameObject.SetActive(true);

            _blastRingBaseDiameter = SpellFeedbackHierarchy.ResolveFireballBlastRingDiameter(
                radius,
                FireballBlastRingDiameterMultiplier);
            _blastRingVisual.transform.position = presentationPosition;
            _blastRingVisual.transform.rotation = Quaternion.identity;
            _blastRingVisual.transform.localScale = Vector3.one
                * SpellFeedbackHierarchy.ResolveFireballBlastRingScale(
                    _blastRingBaseDiameter,
                    0f,
                    FireballBlastRingStartScale,
                    FireballBlastRingEndScale);
            _blastRingVisual.color = _activeBlastRingColor;
            _blastRingVisual.gameObject.SetActive(true);
            _blastFrame = 0;
            _blastFrameTimer = 0f;
        }

        private void UpdateBlastVisual()
        {
            if (_blastFrame < 0 || _blastVisual == null)
                return;

            _blastFrameTimer += Time.deltaTime;
            float frameRate = Mathf.Max(1f, BlastFps);
            float visualFrame = _blastFrame + _blastFrameTimer * frameRate;
            float progress01 = BlastFrames != null && BlastFrames.Length > 1
                ? Mathf.Clamp01(visualFrame / (BlastFrames.Length - 1f))
                : 1f;
            UpdateBlastHierarchyRing(progress01);
            if (_blastFrameTimer < 1f / frameRate)
                return;

            _blastFrameTimer = 0f;
            _blastFrame++;
            if (BlastFrames == null || _blastFrame >= BlastFrames.Length)
            {
                _blastFrame = -1;
                _blastVisual.gameObject.SetActive(false);
                if (_blastCoreVisual != null)
                    _blastCoreVisual.gameObject.SetActive(false);
                if (_blastRingVisual != null)
                    _blastRingVisual.gameObject.SetActive(false);
                return;
            }
            _blastVisual.sprite = BlastFrames[_blastFrame];
        }

        private void UpdateBlastHierarchyRing(float progress01)
        {
            if (_blastRingVisual == null)
                return;

            float scale = SpellFeedbackHierarchy.ResolveFireballBlastRingScale(
                _blastRingBaseDiameter,
                progress01,
                FireballBlastRingStartScale,
                FireballBlastRingEndScale);
            _blastRingVisual.transform.localScale = Vector3.one * scale;
            _blastRingVisual.color = SpellFeedbackHierarchy.ResolveFadingColor(
                _activeBlastRingColor,
                progress01);
            if (_blastCoreVisual != null)
            {
                float coreProgress = Mathf.Clamp01(progress01 * 0.72f);
                _blastCoreVisual.color = SpellFeedbackHierarchy.ResolveFadingColor(
                    _activeBlastCoreColor,
                    coreProgress);
            }
        }

        private void UpdateEvolutionVisuals()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                ClearBurningGroundVisuals();
                DisposeEvolutionQueries();
                return;
            }

            EnsureEvolutionQueries(world);
            EntityManager entityManager = world.EntityManager;
            SyncBurningGroundVisuals(entityManager);
            PresentSecondBlasts(entityManager);
        }

        private void EnsureEvolutionQueries(World world)
        {
            if (_evolutionQueriesCreated && ReferenceEquals(_evolutionQueryWorld, world))
                return;

            DisposeEvolutionQueries();
            _evolutionQueryWorld = world;
            EntityManager entityManager = world.EntityManager;
            _burningGroundQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FireballBurningGround>());
            _fireballStrikeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<FireballStrike>());
            _evolutionQueriesCreated = true;
        }

        private void SyncBurningGroundVisuals(EntityManager entityManager)
        {
            _liveBurningGrounds.Clear();
            using (NativeArray<Entity> entities = _burningGroundQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!entityManager.Exists(entity)
                        || !entityManager.HasComponent<FireballBurningGround>(entity))
                    {
                        continue;
                    }

                    _liveBurningGrounds.Add(entity);
                    FireballBurningGround ground =
                        entityManager.GetComponentData<FireballBurningGround>(entity);
                    if (!_burningGroundVisuals.TryGetValue(entity, out BurningGroundVisual visual))
                    {
                        visual = CreateBurningGroundVisual();
                        _burningGroundVisuals.Add(entity, visual);
                    }
                    UpdateBurningGroundVisual(visual, ground);
                }
            }

            _staleEvolutionEntities.Clear();
            foreach (KeyValuePair<Entity, BurningGroundVisual> pair in _burningGroundVisuals)
            {
                if (!_liveBurningGrounds.Contains(pair.Key))
                    _staleEvolutionEntities.Add(pair.Key);
            }
            for (int i = 0; i < _staleEvolutionEntities.Count; i++)
            {
                Entity stale = _staleEvolutionEntities[i];
                if (_burningGroundVisuals.TryGetValue(stale, out BurningGroundVisual visual)
                    && visual?.Root != null)
                {
                    Destroy(visual.Root);
                }
                _burningGroundVisuals.Remove(stale);
            }
        }

        private void PresentSecondBlasts(EntityManager entityManager)
        {
            using (NativeArray<Entity> entities = _fireballStrikeQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!entityManager.Exists(entity)
                        || !entityManager.HasComponent<FireballStrike>(entity))
                    {
                        continue;
                    }

                    FireballStrike strike = entityManager.GetComponentData<FireballStrike>(entity);
                    if (strike.Kind != FireballStrikeKind.SecondBlast
                        || !_presentedEvolutionStrikes.Add(entity))
                    {
                        continue;
                    }

                    StartBlast(
                        new Vector2(strike.Position.x, strike.Position.y),
                        strike.Radius,
                        FireballStrikeKind.SecondBlast);
                }
            }

            _staleEvolutionEntities.Clear();
            foreach (Entity entity in _presentedEvolutionStrikes)
            {
                if (!entityManager.Exists(entity)
                    || !entityManager.HasComponent<FireballStrike>(entity))
                {
                    _staleEvolutionEntities.Add(entity);
                }
            }
            for (int i = 0; i < _staleEvolutionEntities.Count; i++)
                _presentedEvolutionStrikes.Remove(_staleEvolutionEntities[i]);
        }

        private BurningGroundVisual CreateBurningGroundVisual()
        {
            var root = new GameObject("FireballScorchedEarthVisual");
            var fillObject = new GameObject("ScorchedEarthFill");
            fillObject.transform.SetParent(root.transform, false);
            SpriteRenderer fill = fillObject.AddComponent<SpriteRenderer>();
            fill.sprite = GetCircleSprite();
            fill.sortingLayerName = SpellSortingLayer;
            fill.sortingOrder = SpellFeedbackHierarchy.BurningGroundFillSortingOrder;

            var ringObject = new GameObject("ScorchedEarthRing");
            ringObject.transform.SetParent(root.transform, false);
            SpriteRenderer ring = ringObject.AddComponent<SpriteRenderer>();
            ring.sprite = GetHierarchyRingSprite();
            ring.sortingLayerName = SpellSortingLayer;
            ring.sortingOrder = SpellFeedbackHierarchy.BurningGroundRingSortingOrder;

            return new BurningGroundVisual
            {
                Root = root,
                Fill = fill,
                Ring = ring
            };
        }

        private static void UpdateBurningGroundVisual(
            BurningGroundVisual visual,
            FireballBurningGround ground)
        {
            if (visual?.Root == null)
                return;

            visual.Root.transform.position = new Vector3(
                ground.Position.x,
                ground.Position.y,
                MobileCastleRenderDepth.ProjectileZ);
            float diameter = SpellFeedbackHierarchy.ResolveBurningGroundDiameter(
                ground.Radius,
                Time.time);
            visual.Fill.transform.localScale = Vector3.one * diameter;
            visual.Ring.transform.localScale = Vector3.one * diameter * 1.08f;
            visual.Ring.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 10f);
            visual.Fill.color = SpellFeedbackHierarchy.ResolveBurningGroundColor(
                SpellFeedbackHierarchy.BurningGroundFillColor,
                ground.RemainingDuration,
                FireballEvolutionRules.BurningGroundDurationSeconds);
            visual.Ring.color = SpellFeedbackHierarchy.ResolveBurningGroundColor(
                SpellFeedbackHierarchy.BurningGroundRingColor,
                ground.RemainingDuration,
                FireballEvolutionRules.BurningGroundDurationSeconds);
        }

        private void ClearBurningGroundVisuals()
        {
            foreach (BurningGroundVisual visual in _burningGroundVisuals.Values)
            {
                if (visual?.Root != null)
                    Destroy(visual.Root);
            }
            _burningGroundVisuals.Clear();
            _liveBurningGrounds.Clear();
        }

        private void DisposeEvolutionQueries()
        {
            if (_evolutionQueriesCreated
                && _evolutionQueryWorld != null
                && _evolutionQueryWorld.IsCreated)
            {
                _burningGroundQuery.Dispose();
                _fireballStrikeQuery.Dispose();
            }

            _evolutionQueriesCreated = false;
            _evolutionQueryWorld = null;
            _presentedEvolutionStrikes.Clear();
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

        private void HandleFireballClicked()
        {
            TryActivateAbility(AbilityHotkeySlot.Fireball, GameManager.Instance, false);
        }

        private void HandleRallyClicked()
        {
            TryActivateAbility(AbilityHotkeySlot.Rally, GameManager.Instance, false);
        }

        private void HandleEmergencyRepairClicked()
        {
            TryActivateAbility(AbilityHotkeySlot.EmergencyRepair, GameManager.Instance, false);
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
            _targetingIndicator.transform.position = new Vector3(
                world.x,
                world.y,
                MobileCastleRenderDepth.ProjectileZ);
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

        private Sprite GetHierarchyRingSprite()
        {
            if (_hierarchyRingSprite != null)
                return _hierarchyRingSprite;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "FireballHierarchyRingTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(half, half)) / half;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.82f) / 0.12f);
                    float edge = Mathf.Clamp01((1f - distance) / 0.03f);
                    float alpha = ring * edge;
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)(Mathf.Clamp01(alpha) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            _hierarchyRingSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _hierarchyRingSprite.name = "FireballHierarchyRingSprite";
            return _hierarchyRingSprite;
        }

        private void HideRuntimeSpellVisuals()
        {
            _trackedProjectile = Entity.Null;
            _blastFrame = -1;
            ClearBurningGroundVisuals();
            DisposeEvolutionQueries();
            if (_projectileVisual != null)
                _projectileVisual.gameObject.SetActive(false);
            if (_blastVisual != null)
                _blastVisual.gameObject.SetActive(false);
            if (_blastCoreVisual != null)
                _blastCoreVisual.gameObject.SetActive(false);
            if (_blastRingVisual != null)
                _blastRingVisual.gameObject.SetActive(false);
        }

        private static void DestroyGeneratedSprite(ref Sprite sprite)
        {
            if (sprite == null)
                return;

            Texture texture = sprite.texture;
            Destroy(sprite);
            if (texture != null)
                Destroy(texture);
            sprite = null;
        }
    }
}
