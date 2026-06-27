using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Mobile Test")]
        [SerializeField] private bool freeEconomyTestMode;

        private EntityManager _entityManager;
        private Entity _gameStateEntity;
        private Entity _waveStateEntity;
        private Entity _castleEntity;
        private Entity _archerPrefabEntity;
        private Entity _workerPrefabEntity;
        private bool _initialized;
        private bool _mobileInitialPrepApplied;
        private bool _workerVisualSyncInitialized;
        private readonly Dictionary<UpgradeType, int> _upgradeTiers = new Dictionary<UpgradeType, int>();
        private readonly Dictionary<ArcherType, int> _archerTypeLevels = new Dictionary<ArcherType, int>();
        private readonly HashSet<ArcherType> _unlockedArcherTypes = new HashSet<ArcherType> { ArcherType.Basic };
        private MobilePopulationAllocation _lastSyncedWorkerVisualAllocation;
        private UpgradeCard[] _currentUpgradeCards;
        private const int MobileArrowRefillTarget = 200;
        private const int LegacyArrowRefillTarget = 50;
        private const float TypeDamageMultiplierPerLevel = 1.12f;
        private const float TypeFireRateMultiplierPerLevel = 1.08f;
        private const float FrostSlowDurationPerLevel = 0.15f;
        private const float FrostSlowMultiplierStep = 0.02f;
        private const float FrostMinSlowMultiplier = 0.40f;
        private const float GlobalFireRateCardMultiplier = 1.15f;
        private const float GlobalDamageCardBonus = 5f;
        private static readonly ResourceCost FortifyCost = new ResourceCost(0, 50, 25, 0);
        private static readonly ResourceCost RallyCost = new ResourceCost(35, 0, 0, 45);
        private const int MobileInitialPopulation = 60;
        private const int MobileInternalPopulationCapacity = 999999;
        private const int MobileInitialWoodWorkers = 20;
        private const int MobileInitialStoneWorkers = 10;
        private const int MobileInitialIronWorkers = 8;
        private const int MobileInitialFoodWorkers = 15;
        private const float EconomyEventProductionMultiplier = 1.5f;
        private float _globalArrowDamageBonus;
        private float _globalFireRateMultiplier = 1f;
        private bool _missingArcherPlacementWarningLogged;
        private bool _missingWorkerPlacementWarningLogged;

        public GameStateData GameState { get; private set; }
        public WaveStateData WaveState { get; private set; }
        public WallSegment Wall { get; private set; }
        public GateComponent Gate { get; private set; }
        public CastleHP Castle { get; private set; }
        public ResourceData Resources { get; private set; }
        public ResourceProductionRate ResourceProduction { get; private set; }
        public ResourceConsumptionRate ResourceConsumption { get; private set; }
        public EconomyFocusType EconomyFocus { get; private set; } = EconomyFocusType.Balanced;
        public PopulationState Population { get; private set; }
        public ArrowSupply ArrowSupply { get; private set; }
        public WaveClearRewardData WaveClearReward { get; private set; }
        public CastleYardPrepState CastleYardPrep { get; private set; }
        public MobilePopulationAllocation PopulationAllocation { get; private set; }
        public MobilePrepPauseState PrepPause { get; private set; }
        public MobileEconomyEventState EconomyEvent { get; private set; }
        public int BasicArcherCount { get; private set; }
        public int RapidArcherCount { get; private set; }
        public int FrostArcherCount { get; private set; }
        public int KillsThisWave => math.max(0, WaveState.ZombiesSpawned - WaveState.ZombiesAlive);
        public bool IsMobileMode => _initialized && TryGetMobileConfigEntity(out _);
        public bool IsFreeEconomyTestMode => freeEconomyTestMode;

        public event System.Action OnGameOver;
        public event System.Action OnLevelUp;
        public event System.Action OnWaveCompleted;
        public event System.Action OnWaveChanged;
        public event System.Action OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!TryInitialize())
                return;

            ReadECSData();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            _entityManager = world.EntityManager;

            var query = _entityManager.CreateEntityQuery(typeof(GameStateData));
            if (query.IsEmpty) return false;

            _gameStateEntity = query.GetSingletonEntity();
            _waveStateEntity = _gameStateEntity; // ayni entity uzerinde

            var archerPrefabQuery = _entityManager.CreateEntityQuery(typeof(ArcherPrefabData));
            if (archerPrefabQuery.IsEmpty) return false;

            _archerPrefabEntity = _entityManager.GetComponentData<ArcherPrefabData>(
                archerPrefabQuery.GetSingletonEntity()).ArcherPrefab;
            TryResolveWorkerPrefabEntity();

            var castleQuery = _entityManager.CreateEntityQuery(typeof(CastleHP));
            if (castleQuery.IsEmpty) return false;

            _castleEntity = castleQuery.GetSingletonEntity();
            _initialized = true;
            ApplyMobileInitialPrepIfNeeded();
            return true;
        }

        private void ReadECSData()
        {
            if (!CanAccessEntityManager()
                || !_entityManager.Exists(_gameStateEntity)
                || !_entityManager.Exists(_castleEntity))
            {
                _initialized = false;
                return;
            }

            ApplyMobileInitialPrepIfNeeded();

            var prevGameState = GameState;
            var prevWaveState = WaveState;

            GameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            WaveState = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            Resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            ResourceProduction = _entityManager.GetComponentData<ResourceProductionRate>(_gameStateEntity);
            ResourceConsumption = _entityManager.GetComponentData<ResourceConsumptionRate>(_gameStateEntity);
            EconomyFocus = ReadEconomyFocusState();
            Population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            ArrowSupply = _entityManager.GetComponentData<ArrowSupply>(_gameStateEntity);
            Wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            Gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            Castle = _entityManager.GetComponentData<CastleHP>(_castleEntity);
            ReadArcherTypeCounts();
            ReadMobileRuntimeData();
            SyncWorkerVisualsIfNeeded();

            OnGameStateChanged?.Invoke();

            if (GameState.IsGameOver && !prevGameState.IsGameOver)
                OnGameOver?.Invoke();

            if (GameState.IsLevelUpPending && !prevGameState.IsLevelUpPending)
            {
                _currentUpgradeCards = GenerateUpgradeCards();
                OnLevelUp?.Invoke();
            }

            if (prevWaveState.WaveActive && !WaveState.WaveActive && !WaveState.StressTestMode
                && !GameState.IsLevelUpPending)
            {
                OnWaveCompleted?.Invoke();
            }

            if (WaveState.CurrentWave != prevWaveState.CurrentWave)
                OnWaveChanged?.Invoke();
        }

        public UpgradeCard[] GetCurrentUpgradeCards()
        {
            if (_currentUpgradeCards == null || _currentUpgradeCards.Length == 0)
                _currentUpgradeCards = GenerateUpgradeCards();

            return _currentUpgradeCards;
        }

        public int GetUpgradeTier(UpgradeType type)
        {
            return _upgradeTiers.TryGetValue(type, out int tier) ? tier : 0;
        }

        private UpgradeCard[] GenerateUpgradeCards()
        {
            var candidates = new List<UpgradeType>
            {
                UpgradeType.ArrowDamageUp,
                UpgradeType.FireRateUp,
                UpgradeType.RepairGate
            };

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (!CanApplyUpgrade(candidates[i]))
                    candidates.RemoveAt(i);
            }

            if (candidates.Count == 0)
                candidates.Add(UpgradeType.RepairGate);

            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                UpgradeType temp = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = temp;
            }

            int cardCount = candidates.Count < 3 ? candidates.Count : 3;
            var cards = new UpgradeCard[cardCount];
            for (int i = 0; i < cardCount; i++)
            {
                var type = candidates[i];
                cards[i] = new UpgradeCard(
                    type,
                    GetUpgradeTitle(type),
                    GetUpgradeDescription(type),
                    GetUpgradeTier(type) + 1);
            }

            return cards;
        }

        private static string GetUpgradeTitle(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.AddBasicArcher:
                    return "Basic Archer";
                case UpgradeType.AddRapidArcher:
                    return "Rapid Archer";
                case UpgradeType.AddFrostArcher:
                    return "Frost Archer";
                case UpgradeType.ArrowDamageUp:
                    return "Arrow Damage";
                case UpgradeType.FireRateUp:
                    return "Fire Rate";
                case UpgradeType.RepairGate:
                    return "Repair Defense";
                default:
                    return "Upgrade";
            }
        }

        private static string GetUpgradeDescription(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.AddBasicArcher:
                    return "Balanced single-target DPS";
                case UpgradeType.AddRapidArcher:
                    return "Fast shots, lower damage";
                case UpgradeType.AddFrostArcher:
                    return "Slows one target";
                case UpgradeType.ArrowDamageUp:
                    return "All archers +5 damage";
                case UpgradeType.FireRateUp:
                    return "All archers +15% speed";
                case UpgradeType.RepairGate:
                    return "Wall/Gate/Castle full HP";
                default:
                    return string.Empty;
            }
        }

        public bool CanApplyUpgrade(UpgradeType type)
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return false;

            switch (type)
            {
                case UpgradeType.AddBasicArcher:
                case UpgradeType.AddRapidArcher:
                case UpgradeType.AddFrostArcher:
                    return false;

                case UpgradeType.ArrowDamageUp:
                case UpgradeType.FireRateUp:
                    return GetArcherCount() > 0;

                case UpgradeType.RepairGate:
                    return _entityManager.Exists(_castleEntity);

                default:
                    return false;
            }
        }

        public bool ApplyUpgrade(UpgradeType type)
        {
            if (!CanApplyUpgrade(type))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            bool applied = false;

            switch (type)
            {
                case UpgradeType.AddBasicArcher:
                    applied = SpawnArcher(ArcherType.Basic);
                    break;

                case UpgradeType.AddRapidArcher:
                    applied = SpawnArcher(ArcherType.Rapid);
                    break;

                case UpgradeType.AddFrostArcher:
                    applied = SpawnArcher(ArcherType.Frost);
                    break;

                case UpgradeType.ArrowDamageUp:
                    applied = UpgradeGlobalArcherDamage(GlobalDamageCardBonus);
                    break;

                case UpgradeType.FireRateUp:
                    applied = UpgradeGlobalArcherFireRate(GlobalFireRateCardMultiplier);
                    break;

                case UpgradeType.RepairGate:
                    applied = RepairGate();
                    break;
            }

            if (!applied)
                return false;

            _upgradeTiers[type] = GetUpgradeTier(type) + 1;
            _currentUpgradeCards = null;

            gameState.IsLevelUpPending = false;
            gameState.Level++;
            gameState.XP = math.max(0, gameState.XP - gameState.XPToNextLevel);
            gameState.XPToNextLevel = (int)(gameState.XPToNextLevel * 1.5f);
            _entityManager.SetComponentData(_gameStateEntity, gameState);
            return true;
        }

        public bool StartNextWave()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);

            if (gameState.IsGameOver || gameState.IsLevelUpPending || wave.StressTestMode || wave.WaveActive)
                return false;

            if (TryGetMobileConfigEntity(out var mobileConfigEntity))
            {
                var mobileConfig = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
                MobileWaveUtility.StartNightWave(ref wave, mobileConfig);
            }
            else
            {
                wave.CurrentWave++;
                ConfigureWaveForCurrentNumber(ref wave);
                wave.ZombiesSpawned = 0;
                wave.ZombiesAlive = 0;
                wave.SpawnTimer = 0f;
                wave.WaveStartTimer = wave.WaveStartDelay;
                wave.WaveActive = true;
                wave.Phase = RunPhaseType.NightCombat;
                wave.PrepTimer = 0f;
                wave.PrepDuration = 0f;
            }

            _entityManager.SetComponentData(_gameStateEntity, wave);
            WaveState = wave;
            OnWaveChanged?.Invoke();
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool RepairDefenseFull()
        {
            if (!CanRepairDefenseFull())
                return false;

            bool repaired = RepairGate();
            if (repaired)
                OnGameStateChanged?.Invoke();

            return repaired;
        }

        public bool CanRepairDefenseFull()
        {
            return CanAccessEntityManager()
                && _entityManager.Exists(_castleEntity)
                && GetDefensePercent() < 0.995f
                && (!TryGetMobileConfigEntity(out _) || CanUseMobilePrepAction());
        }

        public ResourceCost GetRepairCost()
        {
            return ResourceCost.Zero;
        }

        public ResourceCost GetFortifyCost()
        {
            return FortifyCost;
        }

        public ResourceCost GetRallyCost()
        {
            return RallyCost;
        }

        public bool CanBuyFortify()
        {
            if (!CanUseMobilePrepAction() || !TryGetCastleYardPrepEntity(out var prepEntity))
                return false;

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            return !prep.FortifyActive && CanAfford(FortifyCost);
        }

        public bool BuyFortify()
        {
            if (!CanBuyFortify())
                return false;

            if (!TryGetCastleYardPrepEntity(out var prepEntity) || !SpendResources(FortifyCost))
                return false;

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            prep.FortifyActive = true;
            if (prep.FortifyDamageMultiplier <= 0f)
                prep.FortifyDamageMultiplier = 0.70f;
            _entityManager.SetComponentData(prepEntity, prep);
            CastleYardPrep = prep;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool CanBuyRally()
        {
            if (!CanUseMobilePrepAction() || !TryGetCastleYardPrepEntity(out var prepEntity))
                return false;

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            return prep.RallyTimer <= 0f && CanAfford(RallyCost);
        }

        public bool BuyRally()
        {
            if (!CanBuyRally())
                return false;

            if (!TryGetCastleYardPrepEntity(out var prepEntity) || !SpendResources(RallyCost))
                return false;

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            if (prep.RallyDuration <= 0f)
                prep.RallyDuration = 10f;
            if (prep.RallyFireRateMultiplier <= 0f)
                prep.RallyFireRateMultiplier = 1.25f;
            prep.RallyTimer = prep.RallyDuration;
            _entityManager.SetComponentData(prepEntity, prep);
            CastleYardPrep = prep;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool RefillArrows()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity)
                || !_entityManager.HasComponent<ArrowSupply>(_gameStateEntity))
            {
                return false;
            }

            var arrowSupply = _entityManager.GetComponentData<ArrowSupply>(_gameStateEntity);
            int target = TryGetMobileConfigEntity(out _) ? MobileArrowRefillTarget : LegacyArrowRefillTarget;
            arrowSupply.Current = math.max(arrowSupply.Current, target);
            arrowSupply.Accumulator = 0f;
            _entityManager.SetComponentData(_gameStateEntity, arrowSupply);
            ArrowSupply = arrowSupply;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool CanUseEconomyFocus()
        {
            return _initialized
                && TryGetMobileConfigEntity(out var mobileConfigEntity)
                && _entityManager.HasComponent<EconomyFocusState>(mobileConfigEntity)
                && !_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity);
        }

        public EconomyFocusType GetEconomyFocus()
        {
            return EconomyFocus;
        }

        public bool SetEconomyFocus(EconomyFocusType focus)
        {
            if (!CanUseEconomyFocus())
                return false;

            focus = EconomyFocusUtility.Normalize(focus);
            TryGetMobileConfigEntity(out var mobileConfigEntity);
            _entityManager.SetComponentData(mobileConfigEntity, new EconomyFocusState
            {
                Type = focus
            });

            EconomyFocus = focus;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public ResourceProductionRate GetEffectiveResourceProduction()
        {
            var production = ResourceProduction;
            if (!_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return production;

            var config = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
            if (_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
                return production;

            EconomyFocusType focus = _entityManager.HasComponent<EconomyFocusState>(mobileConfigEntity)
                ? _entityManager.GetComponentData<EconomyFocusState>(mobileConfigEntity).Type
                : EconomyFocusType.Balanced;
            return EconomyFocusUtility.ApplyPassiveFocus(production, config, focus);
        }

        public bool TryGetMobileCombatConfig(out MobileCastleCombatConfig config)
        {
            config = default;
            if (!_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            config = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
            return true;
        }

        public bool IsUnlimitedArrowsEnabled()
        {
            return TryGetMobileCombatConfig(out var config) && config.UnlimitedArrows;
        }

        public bool IsMobilePopulationEconomyEnabled()
        {
            return _initialized
                && TryGetMobileConfigEntity(out var mobileConfigEntity)
                && _entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity);
        }

        public bool CanOpenCastleEconomy()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity)
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
            {
                return false;
            }

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            return !gameState.IsGameOver
                && !gameState.IsLevelUpPending
                && !wave.StressTestMode
                && !wave.WaveActive
                && wave.Phase == RunPhaseType.DayPrep;
        }

        public bool OpenCastleEconomy()
        {
            if (!CanOpenCastleEconomy() || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            var pause = _entityManager.GetComponentData<MobilePrepPauseState>(mobileConfigEntity);
            pause.IsPaused = true;
            _entityManager.SetComponentData(mobileConfigEntity, pause);
            PrepPause = pause;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public void CloseCastleEconomy()
        {
            if (!_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
            {
                return;
            }

            var pause = _entityManager.GetComponentData<MobilePrepPauseState>(mobileConfigEntity);
            pause.IsPaused = false;
            _entityManager.SetComponentData(mobileConfigEntity, pause);
            PrepPause = pause;
            OnGameStateChanged?.Invoke();
        }

        public bool IsCastleEconomyOpen()
        {
            if (!_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
            {
                return false;
            }

            return _entityManager.GetComponentData<MobilePrepPauseState>(mobileConfigEntity).IsPaused;
        }

        public int GetAvailablePopulation()
        {
            return Mathf.Max(0, Population.Total - Population.Archers);
        }

        public int GetIdlePopulation()
        {
            if (!IsMobilePopulationEconomyEnabled())
                return Mathf.Max(0, Population.Idle);

            int allocated = PopulationAllocation.WoodWorkers
                + PopulationAllocation.StoneWorkers
                + PopulationAllocation.IronWorkers
                + PopulationAllocation.FoodWorkers;
            return Mathf.Max(0, Population.Total - Population.Archers - allocated);
        }

        public int GetResourceWorkers(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return PopulationAllocation.WoodWorkers;
                case EconomyFocusType.Stone:
                    return PopulationAllocation.StoneWorkers;
                case EconomyFocusType.Iron:
                    return PopulationAllocation.IronWorkers;
                case EconomyFocusType.Food:
                    return PopulationAllocation.FoodWorkers;
                default:
                    return PopulationAllocation.WoodWorkers
                        + PopulationAllocation.StoneWorkers
                        + PopulationAllocation.IronWorkers
                        + PopulationAllocation.FoodWorkers;
            }
        }

        public int GetMaxWorkersForResource(EconomyFocusType resource)
        {
            return GetResourceWorkers(resource) + GetIdlePopulation();
        }

        public float GetWorkerProductionRate(EconomyFocusType resource)
        {
            if (IsMobilePopulationEconomyEnabled() && TryGetMobileCombatConfig(out var config))
            {
                float rate;
                switch (resource)
                {
                    case EconomyFocusType.Wood:
                        rate = PopulationAllocation.WoodWorkers * config.WoodWorkerProductionPerMin;
                        break;
                    case EconomyFocusType.Stone:
                        rate = PopulationAllocation.StoneWorkers * config.StoneWorkerProductionPerMin;
                        break;
                    case EconomyFocusType.Iron:
                        rate = PopulationAllocation.IronWorkers * config.IronWorkerProductionPerMin;
                        break;
                    case EconomyFocusType.Food:
                        rate = PopulationAllocation.FoodWorkers * config.FoodWorkerProductionPerMin;
                        break;
                    default:
                        rate = 0f;
                        break;
                }

                if (EconomyEvent.ProductionBonusResource == resource && EconomyEvent.ProductionBonusMultiplier > 0f)
                    rate *= EconomyEvent.ProductionBonusMultiplier;

                return rate;
            }

            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return ResourceProduction.WoodPerMin;
                case EconomyFocusType.Stone:
                    return ResourceProduction.StonePerMin;
                case EconomyFocusType.Iron:
                    return ResourceProduction.IronPerMin;
                case EconomyFocusType.Food:
                    return ResourceProduction.FoodPerMin;
                default:
                    var production = GetEffectiveResourceProduction();
                    return production.WoodPerMin + production.StonePerMin + production.IronPerMin + production.FoodPerMin;
            }
        }

        public bool SetResourceWorkers(EconomyFocusType resource, int value)
        {
            if (!IsMobilePopulationEconomyEnabled() || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced)
                return false;

            var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
            int current = GetResourceWorkers(resource);
            int clamped = Mathf.Clamp(value, 0, current + GetIdlePopulation());
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    allocation.WoodWorkers = clamped;
                    break;
                case EconomyFocusType.Stone:
                    allocation.StoneWorkers = clamped;
                    break;
                case EconomyFocusType.Iron:
                    allocation.IronWorkers = clamped;
                    break;
                case EconomyFocusType.Food:
                    allocation.FoodWorkers = clamped;
                    break;
            }

            _entityManager.SetComponentData(mobileConfigEntity, allocation);
            PopulationAllocation = allocation;
            SyncWorkerVisualsToAllocation();
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool CanAssignResourceWorker(EconomyFocusType resource)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            return _initialized
                && resource != EconomyFocusType.Balanced
                && IsMobilePopulationEconomyEnabled()
                && (freeEconomyTestMode || GetIdlePopulation() > 0);
        }

        public bool AssignResourceWorker(EconomyFocusType resource)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (!CanAssignResourceWorker(resource))
                return false;

            if (freeEconomyTestMode)
                EnsurePopulationForDebugWorkerAssignment();

            int current = GetResourceWorkers(resource);
            return SetResourceWorkers(resource, current + 1);
        }

        public float GetDefensePercent()
        {
            float max = Wall.MaxHP + Gate.MaxHP + Castle.MaxHP;
            if (max <= 0.01f)
                return 1f;

            float current = Wall.CurrentHP + Gate.CurrentHP + Castle.CurrentHP;
            return Mathf.Clamp01(current / max);
        }

        public bool IsMobileFinalWavePressure()
        {
            if (!TryGetMobileCombatConfig(out var config))
                return false;

            var wave = WaveState;
            if (wave.StressTestMode || !wave.WaveActive || wave.Phase != RunPhaseType.NightCombat)
                return false;

            int total = math.max(1, wave.ZombiesToSpawn);
            float progress = math.saturate((float)wave.ZombiesSpawned / total);
            return progress >= 1f - math.clamp(config.FinalEnemyRatio, 0f, 0.95f);
        }

        public bool HasPendingEconomyEvent()
        {
            return EconomyEvent.PendingEvent != MobileEconomyEventType.None;
        }

        public string GetEconomyEventTitle()
        {
            switch (EconomyEvent.PendingEvent)
            {
                case MobileEconomyEventType.ForestCache:
                    return "Forest Cache";
                case MobileEconomyEventType.QuarryCrew:
                    return "Quarry Crew";
                case MobileEconomyEventType.RefugeeCart:
                    return "Refugee Cart";
                default:
                    return "No Event";
            }
        }

        public string GetEconomyEventDescription()
        {
            switch (EconomyEvent.PendingEvent)
            {
                case MobileEconomyEventType.ForestCache:
                    return "Scouts found supplies near the old woods.";
                case MobileEconomyEventType.QuarryCrew:
                    return "A crew offers to help reinforce the quarry line.";
                case MobileEconomyEventType.RefugeeCart:
                    return "Families arrive with food and ask for shelter.";
                default:
                    return string.Empty;
            }
        }

        public string GetEconomyEventChoiceText(int choiceIndex)
        {
            bool instant = choiceIndex == 0;
            switch (EconomyEvent.PendingEvent)
            {
                case MobileEconomyEventType.ForestCache:
                    return instant ? "Take +120W +60F" : "Wood crews +50%";
                case MobileEconomyEventType.QuarryCrew:
                    return instant ? "Take +80S +45I" : "Stone crews +50%";
                case MobileEconomyEventType.RefugeeCart:
                    return instant ? "Shelter +8 POP +80F" : "Food crews +50%";
                default:
                    return string.Empty;
            }
        }

        public bool ChooseEconomyEvent(int choiceIndex)
        {
            if (!IsMobilePopulationEconomyEnabled() || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileEconomyEventState>(mobileConfigEntity))
            {
                return false;
            }

            var economyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
            if (economyEvent.PendingEvent == MobileEconomyEventType.None)
                return false;

            if (choiceIndex <= 0)
                ApplyInstantEconomyEventReward(economyEvent.PendingEvent);
            else
                ApplyEconomyEventProductionBonus(ref economyEvent);

            economyEvent.PendingEvent = MobileEconomyEventType.None;
            economyEvent.EventWave = 0;
            _entityManager.SetComponentData(mobileConfigEntity, economyEvent);
            EconomyEvent = economyEvent;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool IsArcherTypeUnlocked(ArcherType type)
        {
            return type == ArcherType.Basic || _unlockedArcherTypes.Contains(type);
        }

        public int GetArcherTypeLevel(ArcherType type)
        {
            return _archerTypeLevels.TryGetValue(type, out int level) ? math.max(1, level) : 1;
        }

        public int GetArcherTypeCount(ArcherType type)
        {
            switch (type)
            {
                case ArcherType.Rapid:
                    return RapidArcherCount;
                case ArcherType.Frost:
                    return FrostArcherCount;
                default:
                    return BasicArcherCount;
            }
        }

        public float GetArcherTypeDps(ArcherType type)
        {
            var stats = GetScaledArcherStats(type);
            return stats.Damage * stats.FireRate * GetArcherTypeCount(type);
        }

        public ResourceCost GetArcherBuyCost(ArcherType type)
        {
            switch (type)
            {
                case ArcherType.Rapid:
                    return new ResourceCost(55, 0, 35, 20);
                case ArcherType.Frost:
                    return new ResourceCost(45, 55, 25, 0);
                default:
                    return new ResourceCost(45, 0, 0, 20);
            }
        }

        public ResourceCost GetArcherUpgradeCost(ArcherType type)
        {
            int completedUpgrades = GetArcherTypeLevel(type) - 1;
            switch (type)
            {
                case ArcherType.Rapid:
                    return ScaleCost(new ResourceCost(85, 0, 55, 0), 1.40f, completedUpgrades);
                case ArcherType.Frost:
                    return ScaleCost(new ResourceCost(0, 70, 45, 0), 1.40f, completedUpgrades);
                default:
                    return ScaleCost(new ResourceCost(70, 0, 0, 30), 1.35f, completedUpgrades);
            }
        }

        public ResourceCost GetArcherUnlockCost(ArcherType type)
        {
            switch (type)
            {
                case ArcherType.Rapid:
                    return new ResourceCost(90, 0, 50, 0);
                case ArcherType.Frost:
                    return new ResourceCost(0, 80, 45, 30);
                default:
                    return ResourceCost.Zero;
            }
        }

        public bool CanBuyArcher(ArcherType type)
        {
            return _initialized
                && _archerPrefabEntity != Entity.Null
                && _entityManager.Exists(_archerPrefabEntity)
                && IsArcherTypeUnlocked(type)
                && HasPopulationForNewArcher()
                && CanAfford(GetArcherBuyCost(type));
        }

        public bool BuyArcher(ArcherType type)
        {
            if (!CanBuyArcher(type))
                return false;

            var cost = GetArcherBuyCost(type);
            if (!SpendResources(cost))
                return false;

            if (!SpawnArcher(type))
            {
                if (!freeEconomyTestMode)
                    AddResources(cost);
                return false;
            }

            ConsumePopulationForNewArcher();
            ReadArcherTypeCounts();
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool CanUpgradeArcherType(ArcherType type)
        {
            return _initialized && IsArcherTypeUnlocked(type) && CanAfford(GetArcherUpgradeCost(type));
        }

        public bool UpgradeArcherType(ArcherType type)
        {
            if (!CanUpgradeArcherType(type))
                return false;

            if (!SpendResources(GetArcherUpgradeCost(type)))
                return false;

            _archerTypeLevels[type] = GetArcherTypeLevel(type) + 1;
            ApplyScaledStatsToArchers(type, true);
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool CanUnlockArcherType(ArcherType type)
        {
            return type != ArcherType.Basic
                && !IsArcherTypeUnlocked(type)
                && CanAfford(GetArcherUnlockCost(type));
        }

        public bool UnlockArcherType(ArcherType type)
        {
            if (!CanUnlockArcherType(type))
                return false;

            if (!SpendResources(GetArcherUnlockCost(type)))
                return false;

            _unlockedArcherTypes.Add(type);
            OnGameStateChanged?.Invoke();
            return true;
        }

        private void ConfigureWaveForCurrentNumber(ref WaveStateData wave)
        {
            int w = wave.CurrentWave;
            if (TryGetMobileConfigEntity(out var mobileConfigEntity))
            {
                var mobileConfig = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
                MobileWaveUtility.ConfigureMobileWave(ref wave, mobileConfig);
            }
            else
            {
                wave.ZombiesToSpawn = 500 * w;
                wave.ZombieHP = 20f * math.pow(w, 1.4f);
                wave.ZombieDamage = 5f + (w - 1) * 0.5f;
                wave.ZombieSpeed = 1.5f + (w - 1) * 0.1f;
            }
        }

        private bool SpawnArcher(ArcherType type)
        {
            int archerCount = GetArcherCount();
            float3 spawnPosition;

            bool mobileMode = TryGetMobileConfigEntity(out _);
            if (mobileMode)
            {
                if (!TryGetMobileArcherSpawnPosition(archerCount, out spawnPosition))
                    return false;
            }
            else
            {
                spawnPosition = new float3(3.76f, -5f + archerCount * 2f, MobileCastleRenderDepth.UnitZ);
            }

            ArcherStats stats = GetScaledArcherStats(type);
            var entity = _entityManager.Instantiate(_archerPrefabEntity);
            _entityManager.SetComponentData(entity, new ArcherUnit
            {
                FireRate = stats.FireRate,
                FireTimer = 0f,
                ArrowDamage = stats.Damage,
                Range = stats.Range,
                Type = type,
                SlowDuration = stats.SlowDuration,
                SlowMultiplier = stats.SlowMultiplier,
                FacingDirection = new float2(1f, 0f),
                AttackAnimTimer = 0f
            });
            _entityManager.SetComponentData(entity, Unity.Transforms.LocalTransform.FromPositionRotationScale(
                spawnPosition,
                quaternion.identity,
                1f));
            SetSpriteTint(entity, ArcherVisualStyle.GetTint(type));
            return true;
        }

        private bool HasPopulationForNewArcher()
        {
            if (freeEconomyTestMode)
                return true;

            return !IsMobilePopulationEconomyEnabled() || GetIdlePopulation() > 0;
        }

        private void ConsumePopulationForNewArcher()
        {
            if (freeEconomyTestMode)
                return;

            if (!IsMobilePopulationEconomyEnabled() || !CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return;

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            population.Archers = Mathf.Min(population.Total, population.Archers + 1);
            population.Idle = Mathf.Max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
        }

        private ArcherStats GetScaledArcherStats(ArcherType type)
        {
            var stats = GetBaseArcherStats(type);
            int extraLevels = GetArcherTypeLevel(type) - 1;
            float damageScale = math.pow(TypeDamageMultiplierPerLevel, extraLevels);
            float fireRateScale = math.pow(TypeFireRateMultiplierPerLevel, extraLevels) * _globalFireRateMultiplier;

            stats.Damage = stats.Damage * damageScale + _globalArrowDamageBonus;
            stats.FireRate *= fireRateScale;

            if (type == ArcherType.Frost)
            {
                stats.SlowDuration += FrostSlowDurationPerLevel * extraLevels;
                stats.SlowMultiplier = math.max(FrostMinSlowMultiplier,
                    stats.SlowMultiplier - FrostSlowMultiplierStep * extraLevels);
            }

            return stats;
        }

        private static ArcherStats GetBaseArcherStats(ArcherType type)
        {
            switch (type)
            {
                case ArcherType.Rapid:
                    return new ArcherStats
                    {
                        FireRate = 3f,
                        Damage = 6f,
                        Range = 14f,
                        SlowDuration = 0f,
                        SlowMultiplier = 1f
                    };

                case ArcherType.Frost:
                    return new ArcherStats
                    {
                        FireRate = 1.2f,
                        Damage = 5f,
                        Range = 14f,
                        SlowDuration = 2f,
                        SlowMultiplier = 0.55f
                    };

                default:
                    return new ArcherStats
                    {
                        FireRate = 1.5f,
                        Damage = 10f,
                        Range = 15f,
                        SlowDuration = 0f,
                        SlowMultiplier = 1f
                    };
            }
        }

        private bool UpgradeGlobalArcherFireRate(float multiplier)
        {
            if (GetArcherCount() == 0)
                return false;

            _globalFireRateMultiplier *= multiplier;
            ApplyScaledStatsToArchers(ArcherType.Basic, false);
            return true;
        }

        private bool UpgradeGlobalArcherDamage(float amount)
        {
            if (GetArcherCount() == 0)
                return false;

            _globalArrowDamageBonus += amount;
            ApplyScaledStatsToArchers(ArcherType.Basic, false);
            return true;
        }

        private void ApplyScaledStatsToArchers(ArcherType typeFilter, bool useFilter)
        {
            var query = _entityManager.CreateEntityQuery(typeof(ArcherUnit));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var e in entities)
            {
                var archer = _entityManager.GetComponentData<ArcherUnit>(e);
                if (useFilter && archer.Type != typeFilter)
                    continue;

                var stats = GetScaledArcherStats(archer.Type);
                archer.FireRate = stats.FireRate;
                archer.ArrowDamage = stats.Damage;
                archer.Range = stats.Range;
                archer.SlowDuration = stats.SlowDuration;
                archer.SlowMultiplier = stats.SlowMultiplier;
                _entityManager.SetComponentData(e, archer);
            }

            entities.Dispose();
        }

        private bool RepairGate()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity)) return false;

            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            wall.CurrentHP = wall.MaxHP;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;

            var gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            gate.CurrentHP = gate.MaxHP;
            _entityManager.SetComponentData(_castleEntity, gate);
            Gate = gate;

            var castle = _entityManager.GetComponentData<CastleHP>(_castleEntity);
            castle.CurrentHP = castle.MaxHP;
            _entityManager.SetComponentData(_castleEntity, castle);
            Castle = castle;
            return true;
        }

        private bool CanAfford(ResourceCost cost)
        {
            if (freeEconomyTestMode)
                return true;

            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return false;

            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            return cost.CanAfford(resources);
        }

        private bool SpendResources(ResourceCost cost)
        {
            if (freeEconomyTestMode)
                return true;

            if (!CanAfford(cost))
                return false;

            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            resources.Wood -= cost.Wood;
            resources.Stone -= cost.Stone;
            resources.Iron -= cost.Iron;
            resources.Food -= cost.Food;
            _entityManager.SetComponentData(_gameStateEntity, resources);
            Resources = resources;
            return true;
        }

        private void AddResources(ResourceCost cost)
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return;

            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            resources.Wood += cost.Wood;
            resources.Stone += cost.Stone;
            resources.Iron += cost.Iron;
            resources.Food += cost.Food;
            _entityManager.SetComponentData(_gameStateEntity, resources);
            Resources = resources;
        }

        private void AddPopulation(int amount)
        {
            if (!CanAccessEntityManager() || amount <= 0 || !_entityManager.Exists(_gameStateEntity))
                return;

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            population.Total += amount;
            population.Capacity = Mathf.Max(population.Capacity, population.Total);
            population.BaseCapacity = Mathf.Max(population.BaseCapacity, population.Capacity);
            population.Idle = Mathf.Max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
        }

        private void ApplyInstantEconomyEventReward(MobileEconomyEventType eventType)
        {
            switch (eventType)
            {
                case MobileEconomyEventType.ForestCache:
                    AddResources(new ResourceCost(120, 0, 0, 60));
                    break;
                case MobileEconomyEventType.QuarryCrew:
                    AddResources(new ResourceCost(0, 80, 45, 0));
                    break;
                case MobileEconomyEventType.RefugeeCart:
                    AddPopulation(8);
                    AddResources(new ResourceCost(0, 0, 0, 80));
                    break;
            }
        }

        private void ApplyEconomyEventProductionBonus(ref MobileEconomyEventState economyEvent)
        {
            switch (economyEvent.PendingEvent)
            {
                case MobileEconomyEventType.ForestCache:
                    economyEvent.ProductionBonusResource = EconomyFocusType.Wood;
                    break;
                case MobileEconomyEventType.QuarryCrew:
                    economyEvent.ProductionBonusResource = EconomyFocusType.Stone;
                    break;
                case MobileEconomyEventType.RefugeeCart:
                    economyEvent.ProductionBonusResource = EconomyFocusType.Food;
                    break;
                default:
                    economyEvent.ProductionBonusResource = EconomyFocusType.Balanced;
                    break;
            }

            economyEvent.ProductionBonusMultiplier = EconomyEventProductionMultiplier;
            economyEvent.ProductionBonusExpiresAfterWave = Mathf.Max(1, WaveState.CurrentWave + 1);
        }

        private static ResourceCost ScaleCost(ResourceCost baseCost, float multiplier, int exponent)
        {
            float scale = math.pow(multiplier, math.max(0, exponent));
            return new ResourceCost(
                Mathf.CeilToInt(baseCost.Wood * scale),
                Mathf.CeilToInt(baseCost.Stone * scale),
                Mathf.CeilToInt(baseCost.Iron * scale),
                Mathf.CeilToInt(baseCost.Food * scale));
        }

        private int GetArcherCount()
        {
            return _entityManager.CreateEntityQuery(typeof(ArcherUnit)).CalculateEntityCount();
        }

        private void ReadArcherTypeCounts()
        {
            BasicArcherCount = 0;
            RapidArcherCount = 0;
            FrostArcherCount = 0;

            var query = _entityManager.CreateEntityQuery(typeof(ArcherUnit));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var archer = _entityManager.GetComponentData<ArcherUnit>(entity);
                switch (archer.Type)
                {
                    case ArcherType.Rapid:
                        RapidArcherCount++;
                        break;
                    case ArcherType.Frost:
                        FrostArcherCount++;
                        break;
                    default:
                        BasicArcherCount++;
                        break;
                }
            }
            entities.Dispose();
        }

        private void SetSpriteTint(Entity entity, float4 tint)
        {
            if (!_entityManager.HasComponent<SpriteTint>(entity))
                return;

            _entityManager.SetComponentData(entity, new SpriteTint { Value = tint });
        }

        private bool TryGetMobileConfigEntity(out Entity configEntity)
        {
            configEntity = Entity.Null;
            if (!CanAccessEntityManager())
                return false;

            EntityQuery query;
            try
            {
                query = _entityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            }
            catch (System.ObjectDisposedException)
            {
                _initialized = false;
                return false;
            }

            if (query.IsEmpty)
            {
                return false;
            }

            configEntity = query.GetSingletonEntity();
            return true;
        }

        private bool CanAccessEntityManager()
        {
            if (!_initialized)
                return false;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _initialized = false;
                return false;
            }

            try
            {
                return _entityManager.World == world;
            }
            catch (System.ObjectDisposedException)
            {
                _initialized = false;
                return false;
            }
        }

        private bool TryGetCastleYardPrepEntity(out Entity prepEntity)
        {
            prepEntity = Entity.Null;
            if (!_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            if (!_entityManager.HasComponent<CastleYardPrepState>(mobileConfigEntity))
                return false;

            prepEntity = mobileConfigEntity;
            return true;
        }

        private bool CanUseMobilePrepAction()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity) || !TryGetMobileConfigEntity(out _))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            return !gameState.IsGameOver
                && !gameState.IsLevelUpPending
                && !wave.StressTestMode
                && !wave.WaveActive
                && wave.Phase == RunPhaseType.DayPrep;
        }

        private void ApplyMobileInitialPrepIfNeeded()
        {
            if (_mobileInitialPrepApplied || !_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return;

            _mobileInitialPrepApplied = true;
            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            if (wave.StressTestMode)
                return;

            RepositionExistingMobileArchersToOutside();

            if (!wave.WaveActive && wave.Phase == RunPhaseType.DayPrep && wave.CurrentWave == 0)
                return;

            var mobileConfig = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
            if (wave.CurrentWave > 1 || wave.ZombiesSpawned > 0 || wave.ZombiesAlive > 0)
                return;

            float prepDuration = math.max(0f, mobileConfig.InitialDayPrepDuration);
            wave.CurrentWave = 0;
            wave.ZombiesToSpawn = 0;
            wave.ZombiesSpawned = 0;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 0f;
            wave.WaveStartTimer = 0f;
            wave.WaveActive = false;
            wave.Phase = RunPhaseType.DayPrep;
            wave.PrepDuration = prepDuration;
            wave.PrepTimer = prepDuration;
            _entityManager.SetComponentData(_gameStateEntity, wave);
        }

        private EconomyFocusType ReadEconomyFocusState()
        {
            if (!CanUseEconomyFocus())
                return EconomyFocusType.Balanced;

            TryGetMobileConfigEntity(out var mobileConfigEntity);
            return EconomyFocusUtility.Normalize(
                _entityManager.GetComponentData<EconomyFocusState>(mobileConfigEntity).Type);
        }

        private void ReadMobileRuntimeData()
        {
            WaveClearReward = default;
            CastleYardPrep = default;
            PopulationAllocation = default;
            PrepPause = default;
            EconomyEvent = default;

            if (!TryGetMobileConfigEntity(out var mobileConfigEntity))
                return;

            if (_entityManager.HasComponent<WaveClearRewardData>(mobileConfigEntity))
                WaveClearReward = _entityManager.GetComponentData<WaveClearRewardData>(mobileConfigEntity);

            if (_entityManager.HasComponent<CastleYardPrepState>(mobileConfigEntity))
                CastleYardPrep = _entityManager.GetComponentData<CastleYardPrepState>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
                PopulationAllocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
                PrepPause = _entityManager.GetComponentData<MobilePrepPauseState>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobileEconomyEventState>(mobileConfigEntity))
                EconomyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
        }

        private bool TryGetMobileArcherSpawnPosition(int archerCount, out float3 position)
        {
            position = default;

            if (!TryGetMobileConfigEntity(out _))
                return false;

            MobileCastleArcherTilePlacement placement = MobileCastleArcherTilePlacement.GetOrCreateRuntime();
            if (placement != null && placement.TryGetSpawnPosition(archerCount, out position))
            {
                _missingArcherPlacementWarningLogged = false;
                return true;
            }

            LogMissingArcherPlacementWarning();
            return false;
        }

        private void RepositionExistingMobileArchersToOutside()
        {
            MobileCastleArcherTilePlacement placement = MobileCastleArcherTilePlacement.GetOrCreateRuntime();
            if (placement == null)
            {
                LogMissingArcherPlacementWarning();
                return;
            }

            var query = _entityManager.CreateEntityQuery(typeof(ArcherUnit), typeof(Unity.Transforms.LocalTransform));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!placement.TryGetSpawnPosition(i, out float3 position))
                    return;

                var transform = _entityManager.GetComponentData<Unity.Transforms.LocalTransform>(entities[i]);
                transform.Position = position;
                _entityManager.SetComponentData(entities[i], transform);
            }
        }

        private bool TryResolveWorkerPrefabEntity()
        {
            if (!CanAccessEntityManager())
                return false;

            if (_workerPrefabEntity != Entity.Null && _entityManager.Exists(_workerPrefabEntity))
                return true;

            var workerPrefabQuery = _entityManager.CreateEntityQuery(typeof(WorkerPrefabData));
            if (workerPrefabQuery.IsEmpty)
                return false;

            _workerPrefabEntity = _entityManager.GetComponentData<WorkerPrefabData>(
                workerPrefabQuery.GetSingletonEntity()).WorkerPrefab;
            return _workerPrefabEntity != Entity.Null && _entityManager.Exists(_workerPrefabEntity);
        }

        private void SyncWorkerVisualsIfNeeded()
        {
            if (!IsMobilePopulationEconomyEnabled())
                return;

            if (_workerVisualSyncInitialized && SameWorkerAllocation(_lastSyncedWorkerVisualAllocation, PopulationAllocation))
                return;

            SyncWorkerVisualsToAllocation();
        }

        private void SyncWorkerVisualsToAllocation()
        {
            if (!CanAccessEntityManager() || !IsMobilePopulationEconomyEnabled())
                return;

            int targetTotal = PopulationAllocation.WoodWorkers
                + PopulationAllocation.StoneWorkers
                + PopulationAllocation.IronWorkers
                + PopulationAllocation.FoodWorkers;
            if (targetTotal > 0 && !TryResolveWorkerPrefabEntity())
            {
                LogMissingWorkerVisualWarning("WorkerPrefabData bulunamadi. Mobile Castle Scene Setup ile VillagerWorker prefab referansini kur.");
                return;
            }

            if (targetTotal > 0 && CastleInteriorWorkerPlacement.GetOrCreateRuntime() == null)
            {
                LogMissingWorkerVisualWarning("CastleInteriorEconomyArea worker spawn point bulunamadi. Worker visual spawn atlandi.");
                return;
            }

            SyncResourceWorkerVisuals(EconomyFocusType.Wood, PopulationAllocation.WoodWorkers);
            SyncResourceWorkerVisuals(EconomyFocusType.Stone, PopulationAllocation.StoneWorkers);
            SyncResourceWorkerVisuals(EconomyFocusType.Iron, PopulationAllocation.IronWorkers);
            SyncResourceWorkerVisuals(EconomyFocusType.Food, PopulationAllocation.FoodWorkers);

            _lastSyncedWorkerVisualAllocation = PopulationAllocation;
            _workerVisualSyncInitialized = true;
        }

        private void SyncResourceWorkerVisuals(EconomyFocusType resource, int targetCount)
        {
            targetCount = Mathf.Max(0, targetCount);
            int kept = 0;
            var destroy = new List<Entity>();
            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(ResourceWorkerVisual),
                    typeof(Unity.Transforms.LocalTransform)
                },
                None = new ComponentType[]
                {
                    typeof(Prefab)
                }
            });
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var visual = _entityManager.GetComponentData<ResourceWorkerVisual>(entities[i]);
                if (EconomyFocusUtility.Normalize(visual.Resource) != resource)
                    continue;

                if (kept >= targetCount)
                {
                    destroy.Add(entities[i]);
                    continue;
                }

                visual.Resource = resource;
                visual.Index = kept;
                _entityManager.SetComponentData(entities[i], visual);
                UpdateWorkerVisualRoute(entities[i], resource, kept, false);
                ConfigureWorkerSprite(entities[i], resource, kept);
                kept++;
            }

            foreach (var entity in destroy)
                _entityManager.DestroyEntity(entity);

            for (int index = kept; index < targetCount; index++)
                SpawnWorkerVisual(resource, index);
        }

        private bool SpawnWorkerVisual(EconomyFocusType resource, int index)
        {
            if (!TryResolveWorkerPrefabEntity())
            {
                LogMissingWorkerVisualWarning("WorkerPrefabData bulunamadi. Mobile Castle Scene Setup ile VillagerWorker prefab referansini kur.");
                return false;
            }

            if (!TryGetWorkerRoutePositions(resource, index, out float3 pickup, out float3 delivery))
                return false;

            var entity = _entityManager.Instantiate(_workerPrefabEntity);
            _entityManager.SetComponentData(entity, new ResourceWorkerVisual
            {
                Resource = resource,
                Index = index
            });
            _entityManager.SetComponentData(entity, Unity.Transforms.LocalTransform.FromPositionRotationScale(
                pickup,
                quaternion.identity,
                1f));
            ConfigureWorkerLogisticsRoute(entity, index, pickup, delivery, true);
            ConfigureWorkerSprite(entity, resource, index);
            return true;
        }

        private void UpdateWorkerVisualRoute(Entity entity, EconomyFocusType resource, int index, bool resetPosition)
        {
            if (!TryGetWorkerRoutePositions(resource, index, out float3 pickup, out float3 delivery))
                return;

            if (resetPosition)
            {
                var transform = _entityManager.GetComponentData<Unity.Transforms.LocalTransform>(entity);
                transform.Position = pickup;
                _entityManager.SetComponentData(entity, transform);
            }

            ConfigureWorkerLogisticsRoute(entity, index, pickup, delivery, resetPosition);
        }

        private bool TryGetWorkerRoutePositions(EconomyFocusType resource, int index, out float3 pickup, out float3 delivery)
        {
            pickup = default;
            delivery = default;
            CastleInteriorWorkerPlacement placement = CastleInteriorWorkerPlacement.GetOrCreateRuntime();
            if (placement != null && placement.TryGetLogisticsPositions(resource, index, out pickup, out delivery))
            {
                _missingWorkerPlacementWarningLogged = false;
                return true;
            }

            LogMissingWorkerVisualWarning("CastleInteriorEconomyArea worker pickup veya hub delivery point bulunamadi. Worker visual spawn atlandi.");
            return false;
        }

        private void ConfigureWorkerLogisticsRoute(Entity entity, int index, float3 pickup, float3 delivery, bool resetRoute)
        {
            float2 direction = math.normalizesafe((delivery - pickup).xy, new float2(1f, 0f));
            WorkerLogisticsRoute route = _entityManager.HasComponent<WorkerLogisticsRoute>(entity)
                ? _entityManager.GetComponentData<WorkerLogisticsRoute>(entity)
                : default;

            route.PickupPosition = pickup;
            route.DeliveryPosition = delivery;
            route.Speed = 0.85f;
            route.WorkDuration = 0.65f;
            route.DeliveryDuration = 0.35f;
            route.LastDirection = math.lengthsq(route.LastDirection) > 0.0001f ? route.LastDirection : direction;

            if (resetRoute || !_entityManager.HasComponent<WorkerLogisticsRoute>(entity))
            {
                route.MovingToHub = 1;
                route.WaitTimer = 0.12f + (index % 5) * 0.08f;
                route.LastDirection = direction;
            }

            if (_entityManager.HasComponent<WorkerLogisticsRoute>(entity))
                _entityManager.SetComponentData(entity, route);
            else
                _entityManager.AddComponentData(entity, route);
        }

        private void ConfigureWorkerSprite(Entity entity, EconomyFocusType resource, int index)
        {
            SetSpriteTint(entity, ResourceWorkerVisualStyle.GetTint(resource));

            if (!_entityManager.HasComponent<SpriteAnimation>(entity))
                return;

            var anim = _entityManager.GetComponentData<SpriteAnimation>(entity);
            int direction = index % math.max(1, anim.TotalRows);
            anim.DirectionRow = math.clamp(direction, 0, math.max(0, anim.TotalRows - 1));
            anim.FrameCount = math.max(1, anim.FrameCount);
            anim.CurrentFrame = index % anim.FrameCount;
            anim.FrameTimer = 0f;
            _entityManager.SetComponentData(entity, anim);
        }

        private void EnsurePopulationForDebugWorkerAssignment()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return;

            int assigned = PopulationAllocation.WoodWorkers
                + PopulationAllocation.StoneWorkers
                + PopulationAllocation.IronWorkers
                + PopulationAllocation.FoodWorkers
                + Population.Archers;
            if (Population.Total > assigned)
                return;

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            population.Total = assigned + 1;
            population.Capacity = Mathf.Max(population.Capacity, population.Total);
            population.BaseCapacity = Mathf.Max(population.BaseCapacity, population.Capacity);
            population.Idle = Mathf.Max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
        }

        private static bool SameWorkerAllocation(MobilePopulationAllocation a, MobilePopulationAllocation b)
        {
            return a.WoodWorkers == b.WoodWorkers
                && a.StoneWorkers == b.StoneWorkers
                && a.IronWorkers == b.IronWorkers
                && a.FoodWorkers == b.FoodWorkers;
        }

        private void LogMissingArcherPlacementWarning()
        {
            if (_missingArcherPlacementWarningLogged)
                return;

            _missingArcherPlacementWarningLogged = true;
            Debug.LogWarning("[GameManager] Mobile okcu spawn icin Grid/outside tilemap bulunamadi veya bos. Okcu spawn iptal edildi.");
        }

        private void LogMissingWorkerVisualWarning(string message)
        {
            if (_missingWorkerPlacementWarningLogged)
                return;

            _missingWorkerPlacementWarningLogged = true;
            Debug.LogWarning("[GameManager] " + message);
        }

        public void RestartGame()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity) || !_entityManager.Exists(_castleEntity))
            {
                _initialized = false;
                return;
            }

            bool mobileMode = TryGetMobileConfigEntity(out var mobileConfigEntity);
            var mobileConfig = mobileMode
                ? _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity)
                : default;

            // Tum zombileri sil
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieTag));
            _entityManager.DestroyEntity(zombieQuery);

            // Tum oklari sil
            var arrowQuery = _entityManager.CreateEntityQuery(typeof(ArrowTag));
            _entityManager.DestroyEntity(arrowQuery);

            if (mobileMode)
            {
                var archerQuery = _entityManager.CreateEntityQuery(typeof(ArcherUnit));
                _entityManager.DestroyEntity(archerQuery);

                var workerQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(ResourceWorkerVisual) },
                    None = new ComponentType[] { typeof(Prefab) }
                });
                _entityManager.DestroyEntity(workerQuery);
                _workerVisualSyncInitialized = false;
            }

            // Tum bina entity'lerini sil
            var buildingQuery = _entityManager.CreateEntityQuery(typeof(BuildingData));
            _entityManager.DestroyEntity(buildingQuery);

            // Grid'i sifirla
            if (BuildingGridManager.Instance != null)
                BuildingGridManager.Instance.ResetGrid();

            // Detay paneli kapat
            if (BuildingDetailUI.Instance != null)
                BuildingDetailUI.Instance.CloseDetail();

            ResetArcherEconomyState();
            if (mobileMode && _entityManager.HasComponent<EconomyFocusState>(mobileConfigEntity))
            {
                _entityManager.SetComponentData(mobileConfigEntity, new EconomyFocusState
                {
                    Type = EconomyFocusType.Balanced
                });
                EconomyFocus = EconomyFocusType.Balanced;
            }
            if (mobileMode && _entityManager.HasComponent<WaveClearRewardData>(mobileConfigEntity))
            {
                _entityManager.SetComponentData(mobileConfigEntity, new WaveClearRewardData
                {
                    Sequence = 0,
                    Wave = 0,
                    Wood = 0,
                    Stone = 0,
                    Iron = 0,
                    Food = 0
                });
                WaveClearReward = default;
            }
            if (mobileMode && _entityManager.HasComponent<CastleYardPrepState>(mobileConfigEntity))
            {
                var prep = _entityManager.GetComponentData<CastleYardPrepState>(mobileConfigEntity);
                prep.FortifyActive = false;
                prep.RallyTimer = 0f;
                _entityManager.SetComponentData(mobileConfigEntity, prep);
                CastleYardPrep = prep;
            }
            if (mobileMode && _entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
            {
                var allocation = new MobilePopulationAllocation
                {
                    WoodWorkers = MobileInitialWoodWorkers,
                    StoneWorkers = MobileInitialStoneWorkers,
                    IronWorkers = MobileInitialIronWorkers,
                    FoodWorkers = MobileInitialFoodWorkers,
                    LastPopulationGrowthWave = 0,
                    LastEventPrepWave = 0
                };
                _entityManager.SetComponentData(mobileConfigEntity, allocation);
                PopulationAllocation = allocation;
            }
            if (mobileMode && _entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
            {
                var pause = new MobilePrepPauseState { IsPaused = false };
                _entityManager.SetComponentData(mobileConfigEntity, pause);
                PrepPause = pause;
            }
            if (mobileMode && _entityManager.HasComponent<MobileEconomyEventState>(mobileConfigEntity))
            {
                var economyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
                economyEvent.PendingEvent = MobileEconomyEventType.None;
                economyEvent.EventWave = 0;
                economyEvent.CooldownWavesRemaining = 0;
                economyEvent.ProductionBonusResource = EconomyFocusType.Balanced;
                economyEvent.ProductionBonusMultiplier = 1f;
                economyEvent.ProductionBonusExpiresAfterWave = 0;
                _entityManager.SetComponentData(mobileConfigEntity, economyEvent);
                EconomyEvent = economyEvent;
            }

            // Game state resetle
            _entityManager.SetComponentData(_gameStateEntity, new GameStateData
            {
                XP = 0,
                Level = 1,
                XPToNextLevel = 100,
                IsGameOver = false,
                IsLevelUpPending = false
            });

            _entityManager.SetComponentData(_gameStateEntity, new WaveStateData
            {
                CurrentWave = mobileMode ? 0 : 1,
                ZombiesToSpawn = mobileMode ? 0 : 500,
                ZombiesSpawned = 0,
                ZombiesAlive = 0,
                SpawnTimer = 0f,
                SpawnInterval = mobileMode ? 0.8f : 0.05f,
                ZombieHP = 20f,
                ZombieDamage = 5f,
                ZombieSpeed = mobileMode ? mobileConfig.BaseZombieSpeed : 1.5f,
                WaveActive = !mobileMode,
                Phase = mobileMode ? RunPhaseType.DayPrep : RunPhaseType.NightCombat,
                PrepTimer = mobileMode ? math.max(0f, mobileConfig.InitialDayPrepDuration) : 0f,
                PrepDuration = mobileMode ? math.max(0f, mobileConfig.InitialDayPrepDuration) : 0f,
                WaveStartDelay = mobileMode ? 0f : 3f,
                WaveStartTimer = mobileMode ? 0f : 3f,
                StressTestMode = false
            });

            // Kaynak resetle
            _entityManager.SetComponentData(_gameStateEntity, new ResourceData
            {
                Wood = mobileMode ? 150 : 100,
                Stone = mobileMode ? 80 : 50,
                Iron = mobileMode ? 45 : 20,
                Food = mobileMode ? 150 : 100
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceProductionRate
            {
                WoodPerMin = mobileMode ? MobileInitialWoodWorkers * mobileConfig.WoodWorkerProductionPerMin : 0f,
                StonePerMin = mobileMode ? MobileInitialStoneWorkers * mobileConfig.StoneWorkerProductionPerMin : 0f,
                IronPerMin = mobileMode ? MobileInitialIronWorkers * mobileConfig.IronWorkerProductionPerMin : 0f,
                FoodPerMin = mobileMode ? MobileInitialFoodWorkers * mobileConfig.FoodWorkerProductionPerMin : 0f
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceConsumptionRate
            {
                WoodPerMin = 0f,
                StonePerMin = 0f,
                IronPerMin = 0f,
                FoodPerMin = 0f
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceAccumulator
            {
                Wood = 0f,
                Stone = 0f,
                Iron = 0f,
                Food = 0f
            });

            // Ok envanter resetle
            _entityManager.SetComponentData(_gameStateEntity, new ArrowSupply
            {
                Current = mobileMode ? 200 : 50,
                Accumulator = 0f
            });

            // Nufus resetle
            _entityManager.SetComponentData(_gameStateEntity, new PopulationState
            {
                Total = mobileMode ? MobileInitialPopulation : 10,
                Workers = mobileMode
                    ? MobileInitialWoodWorkers + MobileInitialStoneWorkers + MobileInitialIronWorkers + MobileInitialFoodWorkers
                    : 0,
                Archers = 0,
                Idle = mobileMode
                    ? MobileInitialPopulation - MobileInitialWoodWorkers - MobileInitialStoneWorkers - MobileInitialIronWorkers - MobileInitialFoodWorkers
                    : 10,
                Capacity = mobileMode ? MobileInternalPopulationCapacity : 20,
                BaseCapacity = mobileMode ? MobileInternalPopulationCapacity : 20,
                FoodPerAssignedPerMin = mobileMode ? 0.25f : 2f
            });

            // Kale resetle
            var castle = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            castle.CurrentHP = castle.MaxHP;
            _entityManager.SetComponentData(_castleEntity, castle);

            var gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            gate.CurrentHP = gate.MaxHP;
            _entityManager.SetComponentData(_castleEntity, gate);

            var castleHP = _entityManager.GetComponentData<CastleHP>(_castleEntity);
            castleHP.CurrentHP = castleHP.MaxHP;
            _entityManager.SetComponentData(_castleEntity, castleHP);

            // Kale yukseltme resetle
            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);
                upgrade.Level = 0;
                _entityManager.SetComponentData(_castleEntity, upgrade);
            }

            if (mobileMode && SpawnArcher(ArcherType.Basic))
                ConsumePopulationForNewArcher();

            if (mobileMode)
                SyncWorkerVisualsToAllocation();

            _upgradeTiers.Clear();
            _currentUpgradeCards = null;
        }

        private void ResetArcherEconomyState()
        {
            _archerTypeLevels.Clear();
            _unlockedArcherTypes.Clear();
            _unlockedArcherTypes.Add(ArcherType.Basic);
            _globalArrowDamageBonus = 0f;
            _globalFireRateMultiplier = 1f;
        }

        /// <summary>
        /// Kaleyi bir seviye yukseltir. Basarili ise true doner.
        /// </summary>
        public bool UpgradeCastle()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity) || !_entityManager.Exists(_gameStateEntity))
                return false;

            // CastleUpgradeData oku
            if (!_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
                return false;

            var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);

            // Maks seviye kontrolu
            if (upgrade.Level >= upgrade.MaxLevel)
                return false;

            // Kaynak yeterliligi kontrolu
            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            if (resources.Wood < upgrade.WoodCostPerLevel || resources.Stone < upgrade.StoneCostPerLevel)
                return false;

            // Kaynaklari dus
            resources.Wood -= upgrade.WoodCostPerLevel;
            resources.Stone -= upgrade.StoneCostPerLevel;
            _entityManager.SetComponentData(_gameStateEntity, resources);

            // Seviye artir
            upgrade.Level++;
            _entityManager.SetComponentData(_castleEntity, upgrade);

            return true;
        }
    }

    public struct UpgradeCard
    {
        public UpgradeType Type;
        public string Title;
        public string Description;
        public int Tier;

        public UpgradeCard(UpgradeType type, string title, string description, int tier)
        {
            Type = type;
            Title = title;
            Description = description;
            Tier = tier;
        }
    }

    public enum UpgradeType
    {
        AddBasicArcher,
        AddRapidArcher,
        AddFrostArcher,
        ArrowDamageUp,
        FireRateUp,
        RepairGate
    }

    internal struct ArcherStats
    {
        public float FireRate;
        public float Damage;
        public float Range;
        public float SlowDuration;
        public float SlowMultiplier;
    }

    public struct ResourceCost
    {
        public static ResourceCost Zero => new ResourceCost(0, 0, 0, 0);

        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;

        public ResourceCost(int wood, int stone, int iron, int food)
        {
            Wood = wood;
            Stone = stone;
            Iron = iron;
            Food = food;
        }

        public bool CanAfford(ResourceData resources)
        {
            return resources.Wood >= Wood
                && resources.Stone >= Stone
                && resources.Iron >= Iron
                && resources.Food >= Food;
        }

        public ResourceCost GetMissing(ResourceData resources)
        {
            return new ResourceCost(
                Wood > resources.Wood ? Wood - resources.Wood : 0,
                Stone > resources.Stone ? Stone - resources.Stone : 0,
                Iron > resources.Iron ? Iron - resources.Iron : 0,
                Food > resources.Food ? Food - resources.Food : 0);
        }

        public string ToDisplayString()
        {
            var parts = new List<string>(4);
            if (Wood > 0) parts.Add($"{Wood}W");
            if (Stone > 0) parts.Add($"{Stone}S");
            if (Iron > 0) parts.Add($"{Iron}I");
            if (Food > 0) parts.Add($"{Food}F");
            return parts.Count == 0 ? "Free" : string.Join(" ", parts);
        }

        public string ToNeedDisplayString(ResourceData resources)
        {
            var missing = GetMissing(resources);
            if (missing.Wood <= 0 && missing.Stone <= 0 && missing.Iron <= 0 && missing.Food <= 0)
                return string.Empty;

            return "NEED " + missing.ToDisplayString();
        }
    }
}
