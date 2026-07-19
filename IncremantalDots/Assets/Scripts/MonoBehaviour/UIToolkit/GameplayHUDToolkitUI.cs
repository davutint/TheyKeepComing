using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DeadWalls
{
    /// <summary>
    /// Dead Walls'in production runtime UI owner'i. Eski uGUI hiyerarsisini render referansi
    /// olarak kullanmaz; GameManager ve mevcut controller'lari yalniz davranis/data kontrati
    /// olarak tuketir. Tum player-facing screen-space sunum UI Toolkit'tir.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    [RequireComponent(typeof(UIDocument), typeof(UIInputModeService))]
    public sealed partial class GameplayHUDToolkitUI : MonoBehaviour
    {
        private const float TextRefreshInterval = 0.10f;
        private const float GraphRefreshInterval = 0.25f;
        private const float ToastDuration = 2.4f;

        private static readonly EconomyFocusType[] EconomyResources =
        {
            EconomyFocusType.Wood,
            EconomyFocusType.Stone,
            EconomyFocusType.Iron,
            EconomyFocusType.Food
        };

        private static readonly string[] PhaseClasses =
        {
            "phase--day", "phase--dusk", "phase--night", "phase--dawn"
        };

        private static readonly string[] CycleIconClasses =
        {
            "dw-icon--day", "dw-icon--dusk", "dw-icon--night", "dw-icon--dawn"
        };

        private enum SurfaceKind
        {
            None,
            Economy,
            Barracks,
            Arrows,
            Heart
        }

        private UIDocument _document;
        private UIInputModeService _inputMode;
        private VisualElement _root;
        private VisualElement _hudLayer;
        private VisualElement _worldTint;
        private VisualElement _damageFlash;

        private HUDController _legacyHud;
        private SpellCastUI _spellCast;
        private WorkerEconomyDrawerUI _workerLegacy;
        private MarketUI _marketLegacy;
        private ArrowSupplyUI _arrowLegacy;
        private HeartScreenUI _heartLegacy;
        private CouncilEventUI _councilLegacy;
        private FirstRunOnboardingUI _onboardingLegacy;
        private DawnRewardToastUI _dawnToastLegacy;
        private DamageFlashUI _damageFlashLegacy;
        private DayNightOverlayController _dayNightLegacy;
        private SoulCounterUI _soulLegacy;
        private PauseMenuUI _pauseLegacy;
        private SettingsUI _settingsLegacy;
        private LevelUpUI _levelUpLegacy;
        private UIManager _uiManager;

        private CanvasGroup _legacyCanvasGroup;
        private float _legacyCanvasAlpha;
        private bool _legacyCanvasInteractable;
        private bool _legacyCanvasBlocksRaycasts;
        private bool _legacySuppressed;

        private Label _woodValue;
        private Label _woodRate;
        private Label _stoneValue;
        private Label _stoneRate;
        private Label _ironValue;
        private Label _ironRate;
        private Label _foodValue;
        private Label _foodRate;
        private Label _economyBalance;
        private Label _populationValue;
        private Label _populationDetail;
        private Label _dayValue;
        private Label _phaseValue;
        private Label _phaseCountdown;
        private Label _cycleMessage;
        private VisualElement _cycleArc;
        private VisualElement _cycleCelestial;
        private VisualElement _cycleCelestialMarker;
        private readonly VisualElement[] _phaseSegments = new VisualElement[4];
        private readonly VisualElement[] _phaseProgressFills = new VisualElement[4];
        private Label _wallValue;
        private VisualElement _wallTrack;
        private VisualElement _wallProgress;
        private Label _hostilesValue;
        private Label _arrowValue;
        private Label _arrowDetail;
        private Label _soulValue;
        private VisualElement _soulAnchor;

        private Button _fireballButton;
        private Button _rallyButton;
        private Button _repairButton;
        private Label _fireballState;
        private Label _rallyState;
        private Label _repairState;
        private VisualElement _fireballCooldown;
        private VisualElement _rallyCooldown;
        private VisualElement _repairCooldown;

        private Button _economyButton;
        private Button _barracksButton;
        private Button _arrowsButton;
        private Button _heartButton;
        private Button _pauseButton;

        private VisualElement _criticalBanner;
        private Label _criticalBannerTitle;
        private Label _criticalBannerBody;
        private VisualElement _onboardingHint;
        private Label _onboardingHintText;
        private Label _primaryToast;
        private Label _secondaryToast;
        private VisualElement _soulFlightLayer;

        private SurfaceKind _openSurface;
        private float _nextTextRefresh;
        private float _nextGraphRefresh;
        private float _primaryToastUntil;
        private float _secondaryToastUntil;
        private IDisposable _pauseLease;
        private bool _initialized;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _inputMode = GetComponent<UIInputModeService>();
        }

        private void OnEnable()
        {
            if (_root == null || _hudLayer == null)
                _initialized = false;

            if (_inputMode != null)
                _inputMode.ModeChanged += HandleInputModeChanged;

            InitializeVisualTree();
            SoulCounterUI.ToolkitSoulPickupRequested += HandleSoulPickupRequested;
        }

        private void Start()
        {
            ResolveRuntimeOwners();
            SuppressLegacyCanvas();
            BuildEconomyRows();
            BuildArcherRows();
            RebuildHeartGraph(true);
            RefreshAll(true);
        }

        private void OnDisable()
        {
            SoulCounterUI.ToolkitSoulPickupRequested -= HandleSoulPickupRequested;
            if (_inputMode != null)
                _inputMode.ModeChanged -= HandleInputModeChanged;

            ReleasePause();
            RestoreLegacyCanvas();
        }

        private void Update()
        {
            if (_initialized && (_root == null || _hudLayer == null))
                _initialized = false;

            if (!_initialized)
            {
                InitializeVisualTree();
                if (!_initialized)
                    return;
            }

            ResolveRuntimeOwners();
            SuppressLegacyCanvas();
            HandleGlobalInput();

            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;

            float now = Time.unscaledTime;
            if (now >= _nextTextRefresh)
            {
                _nextTextRefresh = now + TextRefreshInterval;
                RefreshHudText(gm);
                RefreshOpenSurface(gm);
                RefreshModalPresentation(gm);
                RefreshFeedbackPresentation();
            }

            RefreshHudContinuous(gm);
            RefreshModalContinuous(gm);
            UpdateSoulFlights(Time.unscaledDeltaTime);
            UpdateToastVisibility(now);

            if (now >= _nextGraphRefresh)
            {
                _nextGraphRefresh = now + GraphRefreshInterval;
                if (_openSurface == SurfaceKind.Heart)
                    RebuildHeartGraph(false);
            }
        }

        private void InitializeVisualTree()
        {
            if (_initialized)
                return;

            _root = _document != null
                ? _document.rootVisualElement?.Q<VisualElement>("screen")
                : null;
            if (_root == null)
                return;

            _hudLayer = Q<VisualElement>("hudLayer");
            if (_hudLayer == null)
                return;

            _worldTint = Q<VisualElement>("worldTint");
            _damageFlash = Q<VisualElement>("damageFlash");

            _woodValue = Q<Label>("woodValue");
            _woodRate = Q<Label>("woodRate");
            _stoneValue = Q<Label>("stoneValue");
            _stoneRate = Q<Label>("stoneRate");
            _ironValue = Q<Label>("ironValue");
            _ironRate = Q<Label>("ironRate");
            _foodValue = Q<Label>("foodValue");
            _foodRate = Q<Label>("foodRate");
            _economyBalance = Q<Label>("economyBalance");
            _populationValue = Q<Label>("populationValue");
            _populationDetail = Q<Label>("populationDetail");
            _dayValue = Q<Label>("dayValue");
            _phaseValue = Q<Label>("phaseValue");
            _phaseCountdown = Q<Label>("phaseCountdown");
            _cycleMessage = Q<Label>("cycleMessage");
            _cycleArc = Q<VisualElement>("cycleArc");
            _cycleCelestial = Q<VisualElement>("cycleCelestial");
            _cycleCelestialMarker = Q<VisualElement>("cycleCelestialMarker");
            _phaseSegments[0] = Q<VisualElement>("phaseDay");
            _phaseSegments[1] = Q<VisualElement>("phaseDusk");
            _phaseSegments[2] = Q<VisualElement>("phaseNight");
            _phaseSegments[3] = Q<VisualElement>("phaseDawn");
            _phaseProgressFills[0] = Q<VisualElement>("phaseDayFill");
            _phaseProgressFills[1] = Q<VisualElement>("phaseDuskFill");
            _phaseProgressFills[2] = Q<VisualElement>("phaseNightFill");
            _phaseProgressFills[3] = Q<VisualElement>("phaseDawnFill");
            _wallValue = Q<Label>("wallValue");
            _wallTrack = Q<VisualElement>("defensePanel");
            _wallProgress = Q<VisualElement>("wallProgress");
            _hostilesValue = Q<Label>("hostilesValue");
            _arrowValue = Q<Label>("arrowValue");
            _arrowDetail = Q<Label>("arrowDetail");
            _soulValue = Q<Label>("soulValue");
            _soulAnchor = Q<VisualElement>("soulAnchor");

            _fireballButton = Q<Button>("fireballButton");
            _rallyButton = Q<Button>("rallyButton");
            _repairButton = Q<Button>("repairButton");
            _fireballState = Q<Label>("fireballState");
            _rallyState = Q<Label>("rallyState");
            _repairState = Q<Label>("repairState");
            _fireballCooldown = Q<VisualElement>("fireballCooldown");
            _rallyCooldown = Q<VisualElement>("rallyCooldown");
            _repairCooldown = Q<VisualElement>("repairCooldown");

            _economyButton = Q<Button>("economyButton");
            _barracksButton = Q<Button>("barracksButton");
            _arrowsButton = Q<Button>("arrowsButton");
            _heartButton = Q<Button>("heartButton");
            _pauseButton = Q<Button>("pauseButton");

            _criticalBanner = Q<VisualElement>("criticalBanner");
            _criticalBannerTitle = Q<Label>("criticalBannerTitle");
            _criticalBannerBody = Q<Label>("criticalBannerBody");
            _onboardingHint = Q<VisualElement>("onboardingHint");
            _onboardingHintText = Q<Label>("onboardingHintText");
            _primaryToast = Q<Label>("primaryToast");
            _secondaryToast = Q<Label>("secondaryToast");
            _soulFlightLayer = Q<VisualElement>("soulFlightLayer");

            BindCoreActions();
            BindManagementActions();
            BindGraphActions();
            BindModalActions();
            BindGraphManipulation();
            BuildEconomyRows();
            BuildArcherRows();
            _root.RegisterCallback<GeometryChangedEvent>(HandleGeometryChanged);
            ApplyInputMode(_inputMode != null ? _inputMode.CurrentMode : UIInputMode.Pointer);
            _initialized = true;
        }

        private void BindCoreActions()
        {
            _fireballButton.clicked += () => ActivateAbility(AbilityHotkeySlot.Fireball);
            _rallyButton.clicked += () => ActivateAbility(AbilityHotkeySlot.Rally);
            _repairButton.clicked += () => ActivateAbility(AbilityHotkeySlot.EmergencyRepair);
            _economyButton.clicked += () => ToggleSurface(SurfaceKind.Economy);
            _barracksButton.clicked += () => ToggleSurface(SurfaceKind.Barracks);
            _arrowsButton.clicked += () => ToggleSurface(SurfaceKind.Arrows);
            _heartButton.clicked += () => ToggleSurface(SurfaceKind.Heart);
            _pauseButton.clicked += OpenPause;
        }

        private void ResolveRuntimeOwners()
        {
            _legacyHud ??= FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            _spellCast ??= FindFirstObjectByType<SpellCastUI>(FindObjectsInactive.Include);
            _workerLegacy ??= FindFirstObjectByType<WorkerEconomyDrawerUI>(FindObjectsInactive.Include);
            _marketLegacy ??= FindFirstObjectByType<MarketUI>(FindObjectsInactive.Include);
            _arrowLegacy ??= FindFirstObjectByType<ArrowSupplyUI>(FindObjectsInactive.Include);
            _heartLegacy ??= FindFirstObjectByType<HeartScreenUI>(FindObjectsInactive.Include);
            _councilLegacy ??= FindFirstObjectByType<CouncilEventUI>(FindObjectsInactive.Include);
            _onboardingLegacy ??= FindFirstObjectByType<FirstRunOnboardingUI>(FindObjectsInactive.Include);
            _dawnToastLegacy ??= FindFirstObjectByType<DawnRewardToastUI>(FindObjectsInactive.Include);
            _damageFlashLegacy ??= FindFirstObjectByType<DamageFlashUI>(FindObjectsInactive.Include);
            _dayNightLegacy ??= FindFirstObjectByType<DayNightOverlayController>(FindObjectsInactive.Include);
            _soulLegacy ??= FindFirstObjectByType<SoulCounterUI>(FindObjectsInactive.Include);
            _pauseLegacy ??= FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            _settingsLegacy ??= FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
            _levelUpLegacy ??= FindFirstObjectByType<LevelUpUI>(FindObjectsInactive.Include);
            _uiManager ??= FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        }

        private void SuppressLegacyCanvas()
        {
            if (_legacySuppressed || _legacyHud == null)
                return;

            Canvas canvas = _legacyHud.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return;

            _legacyCanvasGroup = canvas.GetComponent<CanvasGroup>();
            if (_legacyCanvasGroup == null)
                _legacyCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();

            _legacyCanvasAlpha = _legacyCanvasGroup.alpha;
            _legacyCanvasInteractable = _legacyCanvasGroup.interactable;
            _legacyCanvasBlocksRaycasts = _legacyCanvasGroup.blocksRaycasts;
            _legacyCanvasGroup.alpha = 0f;
            _legacyCanvasGroup.interactable = false;
            _legacyCanvasGroup.blocksRaycasts = false;

            _legacyHud.SetLegacyCorePresentationVisible(false);
            _spellCast?.SetLegacyAbilityPanelVisible(false);
            _legacySuppressed = true;
        }

        private void RestoreLegacyCanvas()
        {
            if (!_legacySuppressed)
                return;

            if (_legacyCanvasGroup != null)
            {
                _legacyCanvasGroup.alpha = _legacyCanvasAlpha;
                _legacyCanvasGroup.interactable = _legacyCanvasInteractable;
                _legacyCanvasGroup.blocksRaycasts = _legacyCanvasBlocksRaycasts;
            }

            _legacyHud?.SetLegacyCorePresentationVisible(true);
            _spellCast?.SetLegacyAbilityPanelVisible(true);
            _legacySuppressed = false;
        }

        private void RefreshAll(bool rebuildDynamic)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
                return;

            RefreshHudText(gm);
            RefreshHudContinuous(gm);
            RefreshManagement(gm, rebuildDynamic);
            RefreshModalPresentation(gm);
            RefreshFeedbackPresentation();
        }

        private void RefreshHudText(GameManager gm)
        {
            ResourceData resources = gm.Resources;
            ResourceProductionRate production = gm.GetEffectiveResourceProduction();
            ResourceConsumptionRate consumption = gm.ResourceConsumption;
            float woodRate = production.WoodPerMin - consumption.WoodPerMin;
            float stoneRate = production.StonePerMin - consumption.StonePerMin;
            float ironRate = production.IronPerMin - consumption.IronPerMin;
            float foodRate = production.FoodPerMin - consumption.FoodPerMin;

            SetResource(_woodValue, _woodRate, resources.Wood, woodRate);
            SetResource(_stoneValue, _stoneRate, resources.Stone, stoneRate);
            SetResource(_ironValue, _ironRate, resources.Iron, ironRate);
            SetResource(_foodValue, _foodRate, resources.Food, foodRate);
            bool deficit = woodRate < -0.01f || stoneRate < -0.01f || ironRate < -0.01f || foodRate < -0.01f;
            _economyBalance.text = deficit ? "DEFICIT" : "STABLE";
            _economyBalance.EnableInClassList("is-negative", deficit);

            PopulationState population = gm.Population;
            _populationValue.text = $"{population.Total:N0} / {population.Capacity:N0} PEOPLE";
            _populationDetail.text = $"{gm.GetIdlePopulation():N0} IDLE  ·  {population.Archers:N0} ARCHERS";

            int capacity = gm.GetArrowCapacity();
            int arrows = gm.ArrowSupply.Current;
            float arrowRatio = capacity > 0 ? arrows / (float)capacity : 0f;
            _arrowValue.text = $"{arrows:N0} / {capacity:N0} ARROWS";
            _arrowDetail.text = arrowRatio <= 0.25f ? "LOW SUPPLY" : "SUPPLY READY";
            _arrowDetail.EnableInClassList("is-negative", arrowRatio <= 0.25f);
            _hostilesValue.text = gm.WaveState.ZombiesAlive.ToString("N0", CultureInfo.InvariantCulture);

            MetaRuntimeTelemetry meta = gm.GetMetaRuntimeTelemetry();
            _soulValue.text = meta.HasCurrentRewardQuote
                ? meta.CurrentRewardQuote.TotalSouls.ToString("N0", CultureInfo.InvariantCulture)
                : "0";

            RefreshAbilityText(gm);
        }

        private void RefreshHudContinuous(GameManager gm)
        {
            ContinuousSiegeCycleData cycle = gm.ContinuousSiegeCycle;
            int phaseIndex = Mathf.Clamp((int)cycle.Phase, 0, 3);
            _dayValue.text = $"DAY {Mathf.Max(1, cycle.CycleIndex + 1)}";
            _phaseValue.text = GetPhaseDisplayName(cycle.Phase, cycle.IsBloodMoonNight);
            float duration = GetPhaseDuration(cycle);
            float remaining = Mathf.Max(0f, duration * (1f - Mathf.Clamp01(cycle.PhaseProgress01)));
            _phaseCountdown.text = FormatClock(remaining);
            _cycleMessage.text = GetPhaseMessage(cycle);
            ApplyPhaseClass(cycle.Phase);
            UpdateCycleDial(cycle.CycleProgress01, phaseIndex);

            for (int i = 0; i < _phaseSegments.Length; i++)
            {
                _phaseSegments[i].EnableInClassList("is-active", i == phaseIndex);
                _phaseSegments[i].EnableInClassList("is-passed", i < phaseIndex);
                float fill01 = i < phaseIndex
                    ? 1f
                    : i == phaseIndex ? Mathf.Clamp01(cycle.PhaseProgress01) : 0f;
                _phaseProgressFills[i].style.width = Length.Percent(fill01 * 100f);
            }

            float wallRatio = Mathf.Clamp01(gm.GetDefensePercent());
            _wallValue.text = $"{Mathf.RoundToInt(wallRatio * 100f)}%";
            _wallProgress.style.width = Length.Percent(wallRatio * 100f);
            _wallTrack.EnableInClassList("is-warning", wallRatio <= 0.50f && wallRatio > 0.25f);
            _wallTrack.EnableInClassList("is-critical", wallRatio <= 0.25f);

            SetCooldown(_fireballCooldown, gm.FireballCooldownRemaining, gm.FireballCooldownDuration);
            SetCooldown(_rallyCooldown, gm.RallyCooldownRemaining, gm.RallyCooldownDuration);
            SetCooldown(_repairCooldown, gm.EmergencyRepairCooldownRemaining, gm.EmergencyRepairCooldownDuration);
            RefreshCriticalBanner(gm, wallRatio);
            MirrorLegacyOverlays();
        }

        private void UpdateCycleDial(float cycleProgress01, int phaseIndex)
        {
            if (_cycleArc == null || _cycleCelestial == null || _cycleCelestialMarker == null)
                return;

            float arcWidth = _cycleArc.resolvedStyle.width;
            if (float.IsNaN(arcWidth) || arcWidth < 64f)
                arcWidth = 210f;

            const float markerSize = 28f;
            float ratio = Mathf.Clamp01(cycleProgress01);
            float travel = Mathf.Max(1f, arcWidth - markerSize);
            float arcLift = Mathf.Sin(ratio * Mathf.PI) * 27f;
            _cycleCelestial.style.left = ratio * travel;
            _cycleCelestial.style.top = 31f - arcLift;

            for (int i = 0; i < CycleIconClasses.Length; i++)
                _cycleCelestialMarker.EnableInClassList(CycleIconClasses[i], i == phaseIndex);
        }

        private static VisualElement CreateRoleIcon(string role, string sizeClass)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("dw-icon");
            icon.AddToClassList(sizeClass);
            icon.AddToClassList("dw-icon--" + role);
            return icon;
        }

        private static string ResourceIconRole(EconomyFocusType resource)
        {
            return resource switch
            {
                EconomyFocusType.Wood => "wood",
                EconomyFocusType.Stone => "stone",
                EconomyFocusType.Iron => "iron",
                EconomyFocusType.Food => "food",
                _ => "workers"
            };
        }

        private static string ArcherIconRole(ArcherType type)
        {
            return type switch
            {
                ArcherType.Rapid => "archer-rapid",
                ArcherType.Frost => "archer-frost",
                _ => "archer-basic"
            };
        }

        private static string LevelUpIconRole(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.AddBasicArcher => "archer-basic",
                UpgradeType.AddRapidArcher => "archer-rapid",
                UpgradeType.AddFrostArcher => "archer-frost",
                UpgradeType.ArrowDamageUp => "arrow-damage",
                UpgradeType.FireRateUp => "fire-rate",
                UpgradeType.RepairGate => "repair",
                _ => "trophy"
            };
        }

        private static string MetaUpgradeIconRole(MetaUpgradeSO upgrade)
        {
            if (upgrade == null)
                return "trophy";

            return upgrade.Id switch
            {
                "start_wood" => "wood",
                "start_stone" => "stone",
                "start_iron" => "iron",
                "start_food" => "food",
                "start_beds" => "housing",
                "start_archers" => "archer-basic",
                "production" => "production",
                "arrow_efficiency" => "efficiency",
                "wall_hp" => "wall",
                "essence_gain" => "souls",
                "node_pool_unlock" => "heart",
                _ => "trophy"
            };
        }

        private void RefreshAbilityText(GameManager gm)
        {
            bool targeting = _spellCast != null && _spellCast.IsTargeting;
            _fireballState.text = !gm.FireballUnlocked
                ? "LOCKED"
                : targeting ? "SELECT AREA" : FormatCooldownState(gm.FireballCooldownRemaining);
            _fireballButton.SetEnabled(gm.FireballReady || targeting);
            _fireballButton.EnableInClassList("is-targeting", targeting);

            _rallyState.text = !gm.RallyUnlocked
                ? "LOCKED"
                : gm.RallyActive ? $"ACTIVE {Mathf.CeilToInt(gm.RallyActiveRemaining)}s" : FormatCooldownState(gm.RallyCooldownRemaining);
            _rallyButton.SetEnabled(gm.RallyReady);

            if (!gm.EmergencyRepairUnlocked)
                _repairState.text = "LOCKED";
            else if (gm.EmergencyRepairCooldownRemaining > 0f)
                _repairState.text = FormatCooldownState(gm.EmergencyRepairCooldownRemaining);
            else if (gm.ContinuousSiegeCycle.Phase != SiegeCyclePhase.Night)
                _repairState.text = "NIGHT ONLY";
            else if (gm.GetDefensePercent() >= 0.995f)
                _repairState.text = "WALL FULL";
            else
                _repairState.text = "READY";
            _repairButton.SetEnabled(gm.EmergencyRepairReady);
        }

        private void RefreshCriticalBanner(GameManager gm, float wallRatio)
        {
            int arrowCapacity = gm.GetArrowCapacity();
            float arrowRatio = arrowCapacity > 0 ? gm.ArrowSupply.Current / (float)arrowCapacity : 0f;
            bool visible = false;
            if (wallRatio <= 0.25f)
            {
                _criticalBannerTitle.text = "THE WALL IS FAILING";
                _criticalBannerBody.text = "Commit repairs before the next breach.";
                visible = true;
            }
            else if (gm.ContinuousSiegeCycle.IsBloodMoonNight && gm.ContinuousSiegeCycle.Phase != SiegeCyclePhase.Night)
            {
                _criticalBannerTitle.text = "BLOOD MOON TONIGHT";
                _criticalBannerBody.text = "Rebalance labour and secure the arrow reserve.";
                visible = true;
            }
            else if (arrowRatio <= 0.15f)
            {
                _criticalBannerTitle.text = "ARROW RESERVE CRITICAL";
                _criticalBannerBody.text = "The wall garrison will soon stop firing.";
                visible = true;
            }

            _criticalBanner.EnableInClassList("is-visible", visible);
        }

        private void MirrorLegacyOverlays()
        {
            if (_dayNightLegacy != null && _dayNightLegacy.OverlayImage != null)
                _worldTint.style.backgroundColor = _dayNightLegacy.OverlayImage.color;
            if (_damageFlashLegacy != null && _damageFlashLegacy.FlashImage != null)
                _damageFlash.style.backgroundColor = _damageFlashLegacy.FlashImage.color;
        }

        private void ActivateAbility(AbilityHotkeySlot slot)
        {
            ResolveRuntimeOwners();
            _spellCast?.TryActivateAbilityFromPlayer(slot);
        }

        private void ToggleSurface(SurfaceKind surface)
        {
            if (_openSurface == surface)
            {
                CloseSurface();
                return;
            }

            CloseSurface();
            _openSurface = surface;
            string surfaceClass = GetSurfaceRootClass(surface);
            if (!string.IsNullOrEmpty(surfaceClass))
                _root?.AddToClassList(surfaceClass);
            VisualElement element = GetSurfaceElement(surface);
            element?.AddToClassList("is-open");
            GetSurfaceButton(surface)?.AddToClassList("is-selected");
            bool fullscreen = surface == SurfaceKind.Heart;
            _hudLayer.style.display = fullscreen ? DisplayStyle.None : DisplayStyle.Flex;

            if (surface == SurfaceKind.Economy)
                BuildEconomyRows();
            else if (surface == SurfaceKind.Barracks)
                BuildArcherRows();
            else if (surface == SurfaceKind.Heart)
            {
                RebuildHeartGraph(true);
                ResolveRuntimeOwners();
                _onboardingLegacy?.NotifyHeartSurfaceOpenedByPlayer();
            }

            if (_inputMode != null && _inputMode.CurrentMode == UIInputMode.Gamepad)
                FocusFirstAction(element);
        }

        private void CloseSurface()
        {
            if (_openSurface == SurfaceKind.None)
                return;

            SurfaceKind closingSurface = _openSurface;
            GetSurfaceElement(_openSurface)?.RemoveFromClassList("is-open");
            GetSurfaceButton(_openSurface)?.RemoveFromClassList("is-selected");
            string surfaceClass = GetSurfaceRootClass(_openSurface);
            if (!string.IsNullOrEmpty(surfaceClass))
                _root?.RemoveFromClassList(surfaceClass);
            _openSurface = SurfaceKind.None;
            _hudLayer.style.display = DisplayStyle.Flex;
            if (closingSurface == SurfaceKind.Heart)
            {
                ResolveRuntimeOwners();
                _onboardingLegacy?.NotifyHeartSurfaceClosedByPlayer();
            }
        }

        private static string GetSurfaceRootClass(SurfaceKind surface)
        {
            return surface switch
            {
                SurfaceKind.Economy => "surface--economy",
                SurfaceKind.Barracks => "surface--barracks",
                SurfaceKind.Arrows => "surface--arrows",
                SurfaceKind.Heart => "surface--heart",
                _ => string.Empty
            };
        }

        private VisualElement GetSurfaceElement(SurfaceKind surface)
        {
            return surface switch
            {
                SurfaceKind.Economy => Q<VisualElement>("economyDrawer"),
                SurfaceKind.Barracks => Q<VisualElement>("barracksDrawer"),
                SurfaceKind.Arrows => Q<VisualElement>("arrowsDrawer"),
                SurfaceKind.Heart => Q<VisualElement>("heartScreen"),
                _ => null
            };
        }

        private Button GetSurfaceButton(SurfaceKind surface)
        {
            return surface switch
            {
                SurfaceKind.Economy => _economyButton,
                SurfaceKind.Barracks => _barracksButton,
                SurfaceKind.Arrows => _arrowsButton,
                SurfaceKind.Heart => _heartButton,
                _ => null
            };
        }

        private void RefreshOpenSurface(GameManager gm)
        {
            RefreshManagement(gm, false);
            if (_openSurface == SurfaceKind.Heart)
                RefreshHeartInspector(gm);
        }

        private void HandleGlobalInput()
        {
            bool cancel = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool menu = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            if (!cancel && !menu)
                return;

            if (IsSettingsOpen())
                CloseSettings();
            else if (_openSurface != SurfaceKind.None)
                CloseSurface();
            else if (IsPauseOpen())
                ResumeFromPause();
            else if (!IsBlockingModalOpen())
                OpenPause();
        }

        private void HandleInputModeChanged(UIInputMode mode)
        {
            ApplyInputMode(mode);
            if (mode == UIInputMode.Gamepad && !IsBlockingModalOpen())
                _economyButton?.Focus();
        }

        private void ApplyInputMode(UIInputMode mode)
        {
            if (_root == null)
                return;
            _root.EnableInClassList("input--pointer", mode == UIInputMode.Pointer);
            _root.EnableInClassList("input--touch", mode == UIInputMode.Touch);
            _root.EnableInClassList("input--gamepad", mode == UIInputMode.Gamepad);
        }

        private void HandleGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            float height = evt.newRect.height;
            _root.EnableInClassList("is-compact", width < 1500f || width / Mathf.Max(1f, height) < 1.55f);
            _root.EnableInClassList("is-short", height < 720f);
        }

        private static void FocusFirstAction(VisualElement root)
        {
            root?.Query<Button>().First()?.Focus();
        }

        private T Q<T>(string name) where T : VisualElement
        {
            return _root?.Q<T>(name);
        }

        private static void SetResource(Label valueLabel, Label rateLabel, int value, float rate)
        {
            string formattedValue = value.ToString("N0", CultureInfo.InvariantCulture);
            valueLabel.text = formattedValue;
            valueLabel.EnableInClassList("is-compact-number", formattedValue.Length >= 6);
            rateLabel.text = FormatRate(rate);
            rateLabel.EnableInClassList("is-positive", rate > 0.01f);
            rateLabel.EnableInClassList("is-negative", rate < -0.01f);
        }

        private static string FormatRate(float value)
        {
            if (Mathf.Abs(value) < 0.01f)
                return "0/m";
            return (value > 0f ? "+" : string.Empty) + value.ToString("0.#", CultureInfo.InvariantCulture) + "/m";
        }

        private static string FormatCooldownState(float remaining)
        {
            return remaining > 0f ? $"{Mathf.CeilToInt(remaining)}s" : "READY";
        }

        private static void SetCooldown(VisualElement fill, float remaining, float duration)
        {
            float ratio = Mathf.Clamp01(remaining / Mathf.Max(0.01f, duration));
            fill.style.width = Length.Percent(ratio * 100f);
        }

        private void ApplyPhaseClass(SiegeCyclePhase phase)
        {
            for (int i = 0; i < PhaseClasses.Length; i++)
                _root.RemoveFromClassList(PhaseClasses[i]);
            _root.AddToClassList(PhaseClasses[Mathf.Clamp((int)phase, 0, PhaseClasses.Length - 1)]);
        }

        private static float GetPhaseDuration(ContinuousSiegeCycleData cycle)
        {
            return cycle.Phase switch
            {
                SiegeCyclePhase.Day => cycle.DayDuration,
                SiegeCyclePhase.Dusk => cycle.DuskDuration,
                SiegeCyclePhase.Night => cycle.NightDuration,
                SiegeCyclePhase.Dawn => cycle.DawnDuration,
                _ => 0f
            };
        }

        private static string GetPhaseDisplayName(SiegeCyclePhase phase, bool bloodMoon)
        {
            if (phase == SiegeCyclePhase.Night && bloodMoon)
                return "BLOOD MOON";
            return phase switch
            {
                SiegeCyclePhase.Day => "DAYLIGHT",
                SiegeCyclePhase.Dusk => "LAST LIGHT",
                SiegeCyclePhase.Night => "NIGHT SIEGE",
                SiegeCyclePhase.Dawn => "DAWN",
                _ => phase.ToString().ToUpperInvariant()
            };
        }

        private static string GetPhaseMessage(ContinuousSiegeCycleData cycle)
        {
            if (cycle.IsBloodMoonNight && cycle.Phase != SiegeCyclePhase.Night)
                return "BLOOD MOON APPROACHING";
            return cycle.Phase switch
            {
                SiegeCyclePhase.Day => "BUILD WHILE THE LIGHT HOLDS",
                SiegeCyclePhase.Dusk => "FINAL PREPARATIONS",
                SiegeCyclePhase.Night => "THE WALL IS UNDER PRESSURE",
                SiegeCyclePhase.Dawn => "COUNT THE LIVING",
                _ => string.Empty
            };
        }

        private static string FormatClock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static string ResourceName(EconomyFocusType resource)
        {
            return resource.ToString().ToUpperInvariant();
        }

        private static string FormatCost(ResourceCost cost)
        {
            string value = cost.ToDisplayString();
            return string.IsNullOrWhiteSpace(value) ? "FREE" : value.ToUpperInvariant();
        }

        private void ShowPrimaryToast(string text)
        {
            _primaryToast.text = text;
            _primaryToastUntil = Time.unscaledTime + ToastDuration;
            _primaryToast.AddToClassList("is-visible");
        }

        private void ShowSecondaryToast(string text)
        {
            _secondaryToast.text = text;
            _secondaryToastUntil = Time.unscaledTime + ToastDuration;
            _secondaryToast.AddToClassList("is-visible");
        }

        private void UpdateToastVisibility(float now)
        {
            if (now >= _primaryToastUntil)
                _primaryToast.RemoveFromClassList("is-visible");
            if (now >= _secondaryToastUntil)
                _secondaryToast.RemoveFromClassList("is-visible");
        }
    }
}
