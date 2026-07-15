using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DeadWalls
{
    public partial class GameManager : MonoBehaviour, IHeartGraveEssenceWallet
    {
        public static GameManager Instance { get; private set; }

        [Header("Mobile Test")]
        [SerializeField] private bool freeEconomyTestMode;

        [Header("Mobile Recruitment")]
        [SerializeField] private ArcherRecruitmentCatalogSO archerCatalog;

        [Header("Mobile Tech Tree")]
        [SerializeField] private TechTreeCatalogSO techTreeCatalog;

        [Header("Mobile Council Events")]
        [SerializeField] private CouncilEventCatalogSO councilCatalog;

        [Header("Meta Progression (roguelite — kosular ARASI kalici)")]
        [SerializeField] private MetaUpgradeCatalogSO metaUpgradeCatalog;

        private EntityManager _entityManager;
        private Entity _gameStateEntity;
        private Entity _waveStateEntity;
        private Entity _castleEntity;
        private Entity _archerPrefabEntity;
        private Entity _zombiePrefabEntity;
        private Entity _enemyPoolEntity;
        private Entity _arrowPrefabEntity;
        private Entity _arrowPoolEntity;
        private Entity _workerPrefabEntity;
        private Entity _mobileConfigEntity;
        private bool _initialized;
        private bool _mobileInitialPrepApplied;
        private bool _workerVisualSyncInitialized;
        private int _lastSurvivorArrivalVisualMarker;
        private readonly Dictionary<UpgradeType, int> _upgradeTiers = new Dictionary<UpgradeType, int>();
        private readonly Dictionary<ArcherType, int> _archerTypeLevels = new Dictionary<ArcherType, int>();
        private readonly HashSet<ArcherType> _unlockedArcherTypes = new HashSet<ArcherType> { ArcherType.Basic };
        private int4 _lastSyncedWorkerVisualCounts;
        private int4 _lastSyncedWorkerVisualActualCounts;
        private UpgradeCard[] _currentUpgradeCards;
        private const float TypeDamageMultiplierPerLevel = 1.12f;
        private const float TypeFireRateMultiplierPerLevel = 1.08f;
        private const float FrostSlowDurationPerLevel = 0.15f;
        private const float FrostSlowMultiplierStep = 0.02f;
        private const float FrostMinSlowMultiplier = 0.40f;
        private const float GlobalFireRateCardMultiplier = 1.15f;
        private const float GlobalDamageCardBonus = 5f;
        private static readonly ResourceCost FortifyCost = new ResourceCost(0, 50, 25, 0);
        private const int MobileInitialPopulation = 60;
        private const int MobileInitialWoodWorkers = 20;
        private const int MobileInitialStoneWorkers = 10;
        private const int MobileInitialIronWorkers = 8;
        private const int MobileInitialFoodWorkers = 15;
        private const int MobileFallbackWoodWorkerCap = 40;
        private const int MobileFallbackStoneWorkerCap = 30;
        private const int MobileFallbackIronWorkerCap = 24;
        private const int MobileFallbackFoodWorkerCap = 40;
        private const int MobileInitialBasicArchers = 4;
        private const float EconomyEventProductionMultiplier = 1.5f;
        private float _globalArrowDamageBonus;
        private float _globalFireRateMultiplier = 1f;
        private bool _missingArcherPlacementWarningLogged;
        private bool _missingWorkerPlacementWarningLogged;
        private int _archerFormationVersion = ArcherFormationUtility.CurrentVersion;
        private ArcherDefinitionSO[] _runtimeDefaultArcherDefinitions;

        // Tech tree run-scoped state (persistence yok; RestartGame sifirlar — _unlockedArcherTypes kalibi)
        private readonly Dictionary<string, int> _techNodeLevels = new Dictionary<string, int>();
        private readonly HashSet<string> _revealedTechNodes = new HashSet<string>();
        private bool _techTreeInitialized;
        private float _techDamageMultiplier = 1f;
        private float _techFireRateMultiplier = 1f;
        private float _techRepairCostMultiplier = 1f;

        // Buyuculuk (M-C): arcane_tower unlock'u + fire_power/fire_radius/fire_cooldown gelisimi.
        // Run-scoped (RestartGame sifirlar); baz degerler sabit, tech carpimsal/duz biner.
        private const float FireballBaseDamage = 60f;
        private const float FireballBaseRadius = 2.2f;
        private const float FireballBaseCooldown = 45f;
        private bool _fireballUnlocked;
        private float _spellDamageMultiplier = 1f;
        private float _spellRadiusBonus;
        private float _spellCooldownMultiplier = 1f;
        private float _fireballCooldownRemaining;
        private float _rallyCooldownRemaining;
        private float _emergencyRepairCooldownRemaining;
        // Config/defense base degerleri: tech ilk dokunmadan once yakalanir, her satin almada
        // toplam etki base'ten YENIDEN hesaplanir (compound hatasi yok), restart'ta base geri yazilir.
        private bool _techConfigBaselineCaptured;
        private int _baseWoodWorkerCap;
        private int _baseStoneWorkerCap;
        private int _baseIronWorkerCap;
        private int _baseFoodWorkerCap;
        private float _baseWoodProductionPerMin;
        private float _baseStoneProductionPerMin;
        private float _baseIronProductionPerMin;
        private float _baseFoodProductionPerMin;
        private int _basePopulationGrowthPerCycle;
        private bool _techDefenseBaselineCaptured;
        private float _baseWallMaxHp;

        // Council event run-state. V1 yalniz exact 3/6/9... regular schedule'i kullanir.
        private readonly Dictionary<string, int> _councilFlags = new Dictionary<string, int>();
        private readonly List<string> _recentCouncilTemplates = new List<string>();
        private readonly HashSet<string> _usedOneShotCouncils = new HashSet<string>();
        private ComposedCouncilEvent _activeCouncilEvent;
        private int _lastRegularCouncilDay = -1;
        // Kosu basina rastgele tuz: ayni authored catalog farkli kosularda farkli kart icerigi
        // uretebilir; kosu ICINDE determinizm korunur (scheduled gun basina tek compose).
        private uint _councilRunSalt;
        private int _councilWoodCapBonus;
        private int _councilStoneCapBonus;
        private int _councilIronCapBonus;
        private int _councilFoodCapBonus;

        // Meta-progression kosu-ici state'i: her kosu basinda kalici seviyelerden yeniden kurulur
        private bool _metaAppliedThisRun;
        private bool _metaRunCollected;
        private string _currentRunId;
        private float _metaWallHpPercent;
        private float _metaDamageMultiplier = 1f;
        private float _metaProductionPercent;

        public GameStateData GameState { get; private set; }
        public WaveStateData WaveState { get; private set; }
        public WallSegment Wall { get; private set; }
        public ResourceData Resources { get; private set; }
        public ResourceProductionRate ResourceProduction { get; private set; }
        public ResourceConsumptionRate ResourceConsumption { get; private set; }
        public EconomyFocusType EconomyFocus { get; private set; } = EconomyFocusType.Balanced;
        public PopulationState Population { get; private set; }
        public ArrowSupply ArrowSupply { get; private set; }
        public GraveEssence HeartEssence { get; private set; }
        public WaveClearRewardData WaveClearReward { get; private set; }
        public CastleYardPrepState CastleYardPrep { get; private set; }
        public ContinuousSiegeCycleData ContinuousSiegeCycle { get; private set; }
        public ContinuousSpawnBudgetData ContinuousSpawnBudget { get; private set; }
        public MobilePopulationAllocation PopulationAllocation { get; private set; }
        public MobileBedCapacityState BedCapacity { get; private set; }
        public MobileWorkerBuildingUpgradeState WorkerBuildingUpgrades { get; private set; }
        public MobilePrepPauseState PrepPause { get; private set; }
        public MobileEconomyEventState EconomyEvent { get; private set; }
        public int BasicArcherCount { get; private set; }
        public int RapidArcherCount { get; private set; }
        public int FrostArcherCount { get; private set; }
        public int KillsThisWave => math.max(0, WaveState.ZombiesSpawned - WaveState.ZombiesAlive);
        public bool IsMobileMode => _initialized && TryGetMobileConfigEntity(out _);
        public bool IsFreeEconomyTestMode => freeEconomyTestMode;
        public ArcherRecruitmentCatalogSO ArcherCatalog => archerCatalog;
        public TechTreeCatalogSO TechCatalog => techTreeCatalog;
        public CouncilEventCatalogSO CouncilCatalog => councilCatalog;
        public ComposedCouncilEvent ActiveCouncilEvent => _activeCouncilEvent;
        public MetaUpgradeCatalogSO MetaCatalog => metaUpgradeCatalog;
        public string CurrentRunId => _currentRunId;
        public int ActiveArcherFormationVersion => _archerFormationVersion;
        /// <summary>Son biten kosunun meta ozeti (olum ekrani gosterir).</summary>
        public MetaRunResult LastRunResult { get; private set; }
        /// <summary>Meta magazasi yalniz durable olum sonucu sonrasinda harcama kabul eder.</summary>
        public bool IsMetaShopPurchaseAllowed => _initialized && MetaPurchaseRules.CanPurchase(
            GameState.IsGameOver,
            _metaRunCollected,
            LastRunResult.Persisted,
            MetaProgression.CanPersist);

        public event System.Action OnGameOver;
        public event System.Action OnLevelUp;
        public event System.Action OnWaveCompleted;
        public event System.Action OnWaveChanged;
        public event System.Action OnGameStateChanged;

        public bool CanBuyMetaUpgrade(MetaUpgradeSO upgrade)
        {
            if (!IsMetaShopPurchaseAllowed
                || upgrade == null
                || metaUpgradeCatalog == null
                || string.IsNullOrWhiteSpace(upgrade.Id))
            {
                return false;
            }

            MetaUpgradeSO canonical = metaUpgradeCatalog.GetUpgrade(upgrade.Id);
            if (canonical != upgrade || !MetaUpgradePolicy.IsRunGraphIsolatedEffect(canonical.EffectType))
                return false;

            int level = MetaProgression.GetUpgradeLevel(canonical.Id);
            return level < canonical.MaxLevel && MetaProgression.State.Souls >= canonical.GetCost(level);
        }

        public bool TryBuyMetaUpgrade(MetaUpgradeSO upgrade)
        {
            if (!CanBuyMetaUpgrade(upgrade))
                return false;

            MetaUpgradeSO canonical = metaUpgradeCatalog.GetUpgrade(upgrade.Id);
            return canonical != null && MetaProgression.TryBuyUpgrade(canonical);
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            RunPersistence.RecoverPendingDeathReward();
        }

        private void Update()
        {
            if (!TryInitialize())
                return;

            EnsureHeartRuntime();
            ReadECSData();
            TickAbilityCooldowns();
        }

        private void TickAbilityCooldowns()
        {
            if (GameState.IsGameOver)
                return;

            float deltaTime = Time.deltaTime;
            if (_fireballCooldownRemaining > 0f)
                _fireballCooldownRemaining = Mathf.Max(0f, _fireballCooldownRemaining - deltaTime);
            if (_rallyCooldownRemaining > 0f)
                _rallyCooldownRemaining = Mathf.Max(0f, _rallyCooldownRemaining - deltaTime);
            if (_emergencyRepairCooldownRemaining > 0f)
                _emergencyRepairCooldownRemaining = Mathf.Max(0f, _emergencyRepairCooldownRemaining - deltaTime);
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

            var mobileConfigQuery = _entityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            _mobileConfigEntity = mobileConfigQuery.IsEmpty
                ? Entity.Null
                : mobileConfigQuery.GetSingletonEntity();

            var archerPrefabQuery = _entityManager.CreateEntityQuery(typeof(ArcherPrefabData));
            if (archerPrefabQuery.IsEmpty) return false;

            _archerPrefabEntity = _entityManager.GetComponentData<ArcherPrefabData>(
                archerPrefabQuery.GetSingletonEntity()).ArcherPrefab;

            var zombiePrefabQuery = _entityManager.CreateEntityQuery(typeof(ZombiePrefabData));
            if (zombiePrefabQuery.IsEmpty) return false;
            _zombiePrefabEntity = _entityManager.GetComponentData<ZombiePrefabData>(
                zombiePrefabQuery.GetSingletonEntity()).ZombiePrefab;

            var enemyPoolQuery = _entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData), typeof(EnemyPoolAvailable));
            _enemyPoolEntity = enemyPoolQuery.IsEmpty
                ? Entity.Null
                : enemyPoolQuery.GetSingletonEntity();
            if (_enemyPoolEntity != Entity.Null)
                EnemyPoolRuntimeUtility.EnsureInitialized(_entityManager, _enemyPoolEntity);

            var arrowPrefabQuery = _entityManager.CreateEntityQuery(typeof(ArrowPrefabData));
            if (arrowPrefabQuery.IsEmpty) return false;
            _arrowPrefabEntity = _entityManager.GetComponentData<ArrowPrefabData>(
                arrowPrefabQuery.GetSingletonEntity()).ArrowPrefab;

            var arrowPoolQuery = _entityManager.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable));
            _arrowPoolEntity = arrowPoolQuery.IsEmpty
                ? Entity.Null
                : arrowPoolQuery.GetSingletonEntity();
            if (_arrowPoolEntity != Entity.Null)
                ArrowPoolRuntimeUtility.EnsureInitialized(
                    _entityManager, _arrowPoolEntity, _arrowPrefabEntity);
            TryResolveWorkerPrefabEntity();

            var castleQuery = _entityManager.CreateEntityQuery(typeof(WallSegment));
            if (castleQuery.IsEmpty) return false;

            _castleEntity = castleQuery.GetSingletonEntity();
            _initialized = true;
            ApplyMobileInitialPrepIfNeeded();
            EnsureCurrentRunId();
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
            HeartEssence = _entityManager.GetComponentData<GraveEssence>(_gameStateEntity);
            Wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            if (BasicArcherCount + RapidArcherCount + FrostArcherCount != Population.Archers)
                ReadArcherTypeCounts();
            ReadMobileRuntimeData();
            SyncWorkerVisualsIfNeeded();
            SyncSurvivorArrivalVisualsIfNeeded();

            OnGameStateChanged?.Invoke();

            if (GameState.IsGameOver && !prevGameState.IsGameOver)
            {
                ProcessRunDeath(); // journal -> durable meta -> receipt cleanup
                OnGameOver?.Invoke();
            }

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
                    return "Repair Wall";
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
                    return "Wall full HP";
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
                {
                    if (!_entityManager.Exists(_castleEntity))
                        return false;

                    var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
                    var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
                    return !gameState.IsGameOver
                        && !SingleWallDefenseRules.IsDestroyed(wall.CurrentHP)
                        && wall.CurrentHP < wall.MaxHP - 0.001f;
                }

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
                    applied = RepairWallToFull();
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

            if (!SpendResources(GetRepairCost()))
                return false;

            bool repaired = RepairWallByMaxPercent(GetNormalRepairHealPercent());
            if (repaired)
                OnGameStateChanged?.Invoke();

            return repaired;
        }

        /// <summary>
        /// Wall hasarliysa ve kaynak yeterliyse tamir denenebilir.
        /// Sifir HP veya Game Over sonrasi tamir Wall'i diriltemez.
        /// </summary>
        public bool CanRepairDefenseFull()
        {
            if (!_initialized
                || !CanAccessEntityManager()
                || !_entityManager.Exists(_castleEntity)
                || !_entityManager.Exists(_gameStateEntity))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            return !gameState.IsGameOver
                && IsRepairPhaseAvailable()
                && !SingleWallDefenseRules.IsDestroyed(wall.CurrentHP)
                && SingleWallDefenseRules.GetHealthRatio(wall.CurrentHP, wall.MaxHP) < 0.995f
                && CanAfford(GetRepairCost());
        }

        public bool IsRepairPhaseAvailable()
        {
            if (!_initialized || !CanAccessEntityManager())
                return false;

            if (!TryGetMobileConfigEntity(out var mobileConfigEntity))
                return true;

            if (_entityManager.HasComponent<ContinuousSiegeCycleData>(mobileConfigEntity))
            {
                var cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
                if (cycle.Enabled)
                    return SingleWallDefenseRules.IsRepairPhaseAllowed(cycle.Phase);
            }

            return CanUseMobilePrepAction();
        }

        /// <summary>
        /// Normal repair gercek iyilestirilecek HP kadar fiyatlanir. Paket buyuklugu,
        /// HP basina Stone ve day-price carpani DifficultyProfile tarafindan tune edilir.
        /// Tech/Heart repair indirimi son fiyat carpanina uygulanir.
        /// </summary>
        public ResourceCost GetRepairCost()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity))
                return ResourceCost.Zero;

            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            float missingHp = Mathf.Max(0f, wall.MaxHP - wall.CurrentHP);
            if (missingHp <= 0.005f || wall.MaxHP <= 0f)
                return ResourceCost.Zero;

            float healHp = Mathf.Min(missingHp, wall.MaxHP * GetNormalRepairHealPercent());
            float stonePerHp = 0.10f;
            float dayPriceMultiplier = 1f;
            if (TryGetMobileCombatConfig(out var config))
            {
                stonePerHp = config.RepairStonePerMissingHp > 0f
                    ? config.RepairStonePerMissingHp
                    : config.RepairBaseStoneCost / Mathf.Max(1f, wall.MaxHP);
                dayPriceMultiplier = config.RepairDayPriceMultiplier > 0f
                    ? config.RepairDayPriceMultiplier
                    : 1f;
            }

            float multiplier = dayPriceMultiplier * Mathf.Max(
                0.05f,
                _techRepairCostMultiplier * GetHeartRepairCostMultiplier());
            return new ResourceCost(
                0,
                Mathf.Max(1, Mathf.CeilToInt(healHp * stonePerHp * multiplier)),
                0, 0);
        }

        public float GetNormalRepairHealPercent()
        {
            if (TryGetMobileCombatConfig(out var config) && config.NormalRepairHealPercent > 0f)
                return Mathf.Clamp01(config.NormalRepairHealPercent);

            return 0.25f;
        }

        public ResourceCost GetFortifyCost()
        {
            return FortifyCost;
        }

        public ResourceCost GetRallyCost()
        {
            return ResourceCost.Zero;
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
            return CanUseRally();
        }

        public bool BuyRally()
        {
            return TryUseRally();
        }

        public bool RallyUnlocked => true;
        public float RallyCooldownRemaining => _rallyCooldownRemaining;
        public float RallyCooldownDuration
        {
            get
            {
                return TryGetMobileCombatConfig(out var config) && config.RallyCooldown > 0f
                    ? config.RallyCooldown
                    : 60f;
            }
        }
        public float RallyActiveRemaining => CastleYardPrep.RallyTimer;
        public bool RallyActive => RallyActiveRemaining > 0f;
        public bool RallyReady => CanUseRally();

        public bool CanUseRally()
        {
            if (!_initialized
                || Time.timeScale <= 0f
                || !TryGetCastleYardPrepEntity(out var prepEntity)
                || !_entityManager.Exists(_gameStateEntity))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            return ActiveAbilityRules.CanUseRally(
                RallyUnlocked,
                _rallyCooldownRemaining,
                prep.RallyTimer,
                gameState.IsGameOver,
                gameState.IsLevelUpPending);
        }

        public bool TryUseRally()
        {
            if (!CanUseRally() || !TryGetCastleYardPrepEntity(out var prepEntity))
                return false;

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            if (prep.RallyDuration <= 0f)
                prep.RallyDuration = 10f;
            if (prep.RallyFireRateMultiplier <= 0f)
                prep.RallyFireRateMultiplier = 1.25f;

            prep.RallyTimer = prep.RallyDuration;
            _rallyCooldownRemaining = RallyCooldownDuration;
            _entityManager.SetComponentData(prepEntity, prep);
            CastleYardPrep = prep;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool EmergencyRepairUnlocked => true;
        public float EmergencyRepairCooldownRemaining => _emergencyRepairCooldownRemaining;
        public float EmergencyRepairCooldownDuration
        {
            get
            {
                return TryGetMobileCombatConfig(out var config) && config.EmergencyRepairCooldown > 0f
                    ? config.EmergencyRepairCooldown
                    : 120f;
            }
        }
        public float EmergencyRepairHealPercent
        {
            get
            {
                return TryGetMobileCombatConfig(out var config) && config.EmergencyRepairHealPercent > 0f
                    ? Mathf.Clamp01(config.EmergencyRepairHealPercent)
                    : 0.20f;
            }
        }
        public bool EmergencyRepairReady => CanUseEmergencyRepair();

        public bool CanUseEmergencyRepair()
        {
            if (!_initialized
                || Time.timeScale <= 0f
                || !CanAccessEntityManager()
                || !_entityManager.Exists(_gameStateEntity)
                || !_entityManager.Exists(_castleEntity))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            SiegeCyclePhase phase = TryGetContinuousSiegeCycle(out var cycle)
                ? cycle.Phase
                : SiegeCyclePhase.Day;
            return ActiveAbilityRules.CanUseEmergencyRepair(
                EmergencyRepairUnlocked,
                _emergencyRepairCooldownRemaining,
                phase,
                wall.CurrentHP,
                wall.MaxHP,
                gameState.IsGameOver,
                gameState.IsLevelUpPending);
        }

        public bool TryUseEmergencyRepair()
        {
            if (!CanUseEmergencyRepair())
                return false;

            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            float healedHp = SingleWallDefenseRules.HealByMaxPercent(
                wall.CurrentHP,
                wall.MaxHP,
                EmergencyRepairHealPercent);
            if (healedHp <= wall.CurrentHP + 0.001f)
                return false;

            wall.CurrentHP = healedHp;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
            _emergencyRepairCooldownRemaining = EmergencyRepairCooldownDuration;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public int GetArrowCapacity()
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            return ArrowEconomyUtility.GetCapacity(supply, GetEconomyPriceTuning());
        }

        public long GraveEssenceAmount
        {
            get
            {
                return TryGetGraveEssence(out _, out var current)
                    ? current.Current
                    : HeartEssence.Current;
            }
        }

        public bool CanSpendGraveEssenceAtHeart(long cost)
        {
            return cost > 0
                && _initialized
                && !GameState.IsGameOver
                && TryGetGraveEssence(out _, out var essence)
                && essence.Current >= cost;
        }

        /// <summary>
        /// Castle Heart node satin alimlarinin tek Grave Essence harcama kapisi.
        /// E4 purchase pipeline bu metodu kullanacak; genel resource harcama yoluna baglanmaz.
        /// </summary>
        public bool TrySpendGraveEssenceAtHeart(long cost)
        {
            if (!CanSpendGraveEssenceAtHeart(cost)
                || !TryGetGraveEssence(out Entity entity, out var essence))
            {
                return false;
            }

            essence.Current -= cost;
            _entityManager.SetComponentData(entity, essence);
            HeartEssence = essence;
            OnGameStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Gelecekteki enemy drop owner'inin kullanacagi run-ici kazanc kapisi.
        /// Bu paket herhangi bir drop kaynagi veya oran uydurmaz.
        /// </summary>
        public bool GrantGraveEssence(long amount)
        {
            if (amount <= 0
                || !_initialized
                || GameState.IsGameOver
                || !TryGetGraveEssence(out Entity entity, out var essence))
            {
                return false;
            }

            long current = essence.Current < 0 ? 0 : essence.Current;
            essence.Current = current > long.MaxValue - amount
                ? long.MaxValue
                : current + amount;
            _entityManager.SetComponentData(entity, essence);
            HeartEssence = essence;
            OnGameStateChanged?.Invoke();
            return true;
        }

        private bool TryGetGraveEssence(out Entity entity, out GraveEssence essence)
        {
            entity = _gameStateEntity;
            essence = default;
            if (!CanAccessEntityManager() || !_entityManager.Exists(entity)
                || !_entityManager.HasComponent<GraveEssence>(entity))
            {
                return false;
            }

            essence = _entityManager.GetComponentData<GraveEssence>(entity);
            return true;
        }

        public int GetArrowPackageSize()
        {
            return GetEconomyPriceTuning().ArrowRefillPackageSize;
        }

        public int GetArrowsPerWood()
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            return ArrowEconomyUtility.GetArrowsPerWood(supply, GetEconomyPriceTuning());
        }

        public ArrowRefillQuote GetArrowRefillQuote(int packageCount)
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            if (!ArrowEconomyUtility.TryGetPackageQuote(
                    supply, GetEconomyPriceTuning(), packageCount, out var quote))
            {
                return default;
            }

            return quote;
        }

        public ArrowRefillQuote GetArrowBuyMaxQuote()
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            int availableWood = freeEconomyTestMode ? int.MaxValue : math.max(0, Resources.Wood);
            return ArrowEconomyUtility.TryGetBuyMaxQuote(
                supply, GetEconomyPriceTuning(), availableWood, out var quote)
                ? quote
                : default;
        }

        public bool CanBuyArrowRefill(int packageCount)
        {
            ArrowRefillQuote quote = GetArrowRefillQuote(packageCount);
            return _initialized && !GameState.IsGameOver && !GameState.IsLevelUpPending
                && quote.IsValid
                && CanAfford(new ResourceCost(quote.WoodCost, 0, 0, 0));
        }

        public bool CanBuyMaxArrowRefill()
        {
            ArrowRefillQuote quote = GetArrowBuyMaxQuote();
            return _initialized && !GameState.IsGameOver && !GameState.IsLevelUpPending
                && quote.IsValid
                && CanAfford(new ResourceCost(quote.WoodCost, 0, 0, 0));
        }

        public bool TryBuyArrowRefill(int packageCount)
        {
            if (!CanBuyArrowRefill(packageCount)
                || !TryGetArrowSupply(out Entity entity, out var supply))
            {
                return false;
            }

            MobileEconomyPriceTuning tuning = GetEconomyPriceTuning();
            if (!ArrowEconomyUtility.TryGetPackageQuote(supply, tuning, packageCount, out var quote))
                return false;

            var next = supply;
            if (!ArrowEconomyUtility.TryApplyRefill(ref next, tuning, quote)
                || !SpendResources(new ResourceCost(quote.WoodCost, 0, 0, 0)))
            {
                return false;
            }

            _entityManager.SetComponentData(entity, next);
            ArrowSupply = next;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool TryBuyMaxArrowRefill()
        {
            if (!CanBuyMaxArrowRefill()
                || !TryGetArrowSupply(out Entity entity, out var supply))
            {
                return false;
            }

            MobileEconomyPriceTuning tuning = GetEconomyPriceTuning();
            int availableWood = freeEconomyTestMode ? int.MaxValue : math.max(0, Resources.Wood);
            if (!ArrowEconomyUtility.TryGetBuyMaxQuote(supply, tuning, availableWood, out var quote))
                return false;

            var next = supply;
            if (!ArrowEconomyUtility.TryApplyRefill(ref next, tuning, quote)
                || !SpendResources(new ResourceCost(quote.WoodCost, 0, 0, 0)))
            {
                return false;
            }

            _entityManager.SetComponentData(entity, next);
            ArrowSupply = next;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public int GetArrowUpgradeLevel(ArrowUpgradeType type)
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            return ArrowEconomyUtility.GetUpgradeLevel(supply, type);
        }

        public ResourceCost GetArrowUpgradeCost(ArrowUpgradeType type)
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out var current) ? current : ArrowSupply;
            if (!ArrowEconomyUtility.TryGetUpgradeCost(
                    supply, type, GetEconomyPriceTuning(), out var cost))
            {
                return ResourceCost.Zero;
            }

            return new ResourceCost(cost.Wood, 0, cost.Iron, 0);
        }

        public bool CanBuyArrowUpgrade(ArrowUpgradeType type)
        {
            ResourceCost cost = GetArrowUpgradeCost(type);
            return _initialized && !GameState.IsGameOver && !GameState.IsLevelUpPending
                && (cost.Wood > 0 || cost.Iron > 0)
                && CanAfford(cost);
        }

        public bool TryBuyArrowUpgrade(ArrowUpgradeType type)
        {
            if (!CanBuyArrowUpgrade(type)
                || !TryGetArrowSupply(out Entity entity, out var supply)
                || !ArrowEconomyUtility.TryGetUpgradeCost(
                    supply, type, GetEconomyPriceTuning(), out var upgradeCost))
            {
                return false;
            }

            var next = supply;
            if (!ArrowEconomyUtility.TryIncreaseUpgradeLevel(ref next, type))
                return false;

            var cost = new ResourceCost(upgradeCost.Wood, 0, upgradeCost.Iron, 0);
            if (!SpendResources(cost))
                return false;

            _entityManager.SetComponentData(entity, next);
            ArrowSupply = next;
            OnGameStateChanged?.Invoke();
            return true;
        }

        private bool TryGetArrowSupply(out Entity entity, out ArrowSupply supply)
        {
            entity = _gameStateEntity;
            supply = default;
            if (!CanAccessEntityManager() || !_entityManager.Exists(entity)
                || !_entityManager.HasComponent<ArrowSupply>(entity))
            {
                return false;
            }

            supply = _entityManager.GetComponentData<ArrowSupply>(entity);
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

        public bool TryGetContinuousSiegeCycle(out ContinuousSiegeCycleData cycle)
        {
            cycle = default;
            if (!_initialized
                || !CanAccessEntityManager()
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<ContinuousSiegeCycleData>(mobileConfigEntity))
            {
                return false;
            }

            cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
            ContinuousSiegeCycle = cycle;
            return cycle.Enabled;
        }

        public bool TryGetContinuousSpawnBudget(out ContinuousSpawnBudgetData budget)
        {
            budget = default;
            if (!_initialized
                || !CanAccessEntityManager()
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<ContinuousSpawnBudgetData>(mobileConfigEntity))
            {
                return false;
            }

            budget = _entityManager.GetComponentData<ContinuousSpawnBudgetData>(mobileConfigEntity);
            ContinuousSpawnBudget = budget;
            return true;
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

        public int GetWorkerTargetRatioBps(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Wood: return PopulationAllocation.WoodTargetRatioBps;
                case EconomyFocusType.Stone: return PopulationAllocation.StoneTargetRatioBps;
                case EconomyFocusType.Iron: return PopulationAllocation.IronTargetRatioBps;
                case EconomyFocusType.Food: return PopulationAllocation.FoodTargetRatioBps;
                default: return WorkerAllocationUtility.RatioScale;
            }
        }

        public bool SetWorkerTargetRatios(int wood, int stone, int iron, int food)
        {
            if (!IsMobilePopulationEconomyEnabled() || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
            allocation.WoodTargetRatioBps = Mathf.Max(0, wood);
            allocation.StoneTargetRatioBps = Mathf.Max(0, stone);
            allocation.IronTargetRatioBps = Mathf.Max(0, iron);
            allocation.FoodTargetRatioBps = Mathf.Max(0, food);
            WorkerAllocationUtility.NormalizeTargetRatios(ref allocation);
            _entityManager.SetComponentData(mobileConfigEntity, allocation);
            PopulationAllocation = allocation;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool SetWorkerTargetRatioBps(EconomyFocusType resource, int targetRatioBps)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced
                || !IsMobilePopulationEconomyEnabled()
                || !TryGetMobileConfigEntity(out var mobileConfigEntity))
            {
                return false;
            }

            var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
            WorkerAllocationUtility.SetTargetRatioBps(ref allocation,
                GetWorkerResourceIndex(resource), targetRatioBps);
            _entityManager.SetComponentData(mobileConfigEntity, allocation);
            PopulationAllocation = allocation;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public bool SetWorkerTargetRatioPercent(EconomyFocusType resource, float targetPercent)
        {
            int targetRatioBps = Mathf.RoundToInt(Mathf.Clamp(targetPercent, 0f, 100f) * 100f);
            return SetWorkerTargetRatioBps(resource, targetRatioBps);
        }

        public bool AdjustWorkerTargetRatioPercent(EconomyFocusType resource, int deltaPercent)
        {
            int currentRatioBps = GetWorkerTargetRatioBps(resource);
            int targetRatioBps = currentRatioBps + deltaPercent * 100;
            return SetWorkerTargetRatioBps(resource, targetRatioBps);
        }

        public int GetMaxWorkersForResource(EconomyFocusType resource)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced)
                return GetAvailablePopulation();

            int cap = 0;
            if (TryGetMobileCombatConfig(out var config))
                cap = GetWorkerCap(resource, config);

            if (cap <= 0)
                cap = GetFallbackWorkerCap(resource);

            return Mathf.Max(0, cap);
        }

        private static int GetWorkerCap(EconomyFocusType resource, MobileCastleCombatConfig config)
        {
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return config.WoodWorkerCap;
                case EconomyFocusType.Stone:
                    return config.StoneWorkerCap;
                case EconomyFocusType.Iron:
                    return config.IronWorkerCap;
                case EconomyFocusType.Food:
                    return config.FoodWorkerCap;
                default:
                    return 0;
            }
        }

        private static int GetFallbackWorkerCap(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return MobileFallbackWoodWorkerCap;
                case EconomyFocusType.Stone:
                    return MobileFallbackStoneWorkerCap;
                case EconomyFocusType.Iron:
                    return MobileFallbackIronWorkerCap;
                case EconomyFocusType.Food:
                    return MobileFallbackFoodWorkerCap;
                default:
                    return 0;
            }
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
            int populationLimit = current + GetIdlePopulation();
            int workerCap = GetMaxWorkersForResource(resource);
            int max = Mathf.Min(populationLimit, workerCap);
            int clamped = Mathf.Clamp(value, 0, max);
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
                && GetResourceWorkers(resource) < GetMaxWorkersForResource(resource)
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

        public MobileEconomyPriceTuning GetEconomyPriceTuning()
        {
            if (!TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileEconomyPriceTuning>(mobileConfigEntity))
            {
                return MobileEconomyPriceTuningUtility.Default;
            }

            var tuning = _entityManager.GetComponentData<MobileEconomyPriceTuning>(mobileConfigEntity);
            return MobileEconomyPriceTuningUtility.Sanitize(tuning);
        }

        public int GetTotalBedCapacity()
        {
            if (!TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                return 0;

            var state = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);
            return MobileBedCapacityUtility.GetTotalCapacity(state);
        }

        public int GetPurchasedBedCapacity()
        {
            if (!TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                return 0;

            return Mathf.Max(0,
                _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity).PurchasedCapacity);
        }

        public ResourceCost GetBedCapacityPurchaseCost(int requestedCapacity = 1)
        {
            if (requestedCapacity <= 0
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                return ResourceCost.Zero;

            var state = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);
            var tuning = GetEconomyPriceTuning();
            int wood = MobileBedCapacityUtility.GetPurchaseWoodCost(state, requestedCapacity, tuning);
            return new ResourceCost(wood, 0, 0, 0);
        }

        public bool CanBuyBedCapacity(int requestedCapacity = 1)
        {
            if (requestedCapacity <= 0
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                return false;

            var state = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);
            int addedCapacity = MobileBedCapacityUtility.GetPurchasableIncrement(state, requestedCapacity);
            var tuning = GetEconomyPriceTuning();
            if (addedCapacity <= 0
                || !MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                    state, addedCapacity, tuning, out int woodCost))
                return false;

            return CanAfford(new ResourceCost(woodCost, 0, 0, 0));
        }

        public bool TryBuyBedCapacity(int requestedCapacity = 1)
        {
            if (!CanBuyBedCapacity(requestedCapacity)
                || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            var state = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);
            int addedCapacity = MobileBedCapacityUtility.GetPurchasableIncrement(state, requestedCapacity);
            var tuning = GetEconomyPriceTuning();
            if (!MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                    state, addedCapacity, tuning, out int woodCost))
                return false;

            ResourceCost cost = new ResourceCost(woodCost, 0, 0, 0);
            if (addedCapacity <= 0
                || !MobileBedCapacityUtility.TryAddPurchasedCapacity(ref state, addedCapacity, out _)
                || !SpendResources(cost))
                return false;

            _entityManager.SetComponentData(mobileConfigEntity, state);
            BedCapacity = state;
            OnGameStateChanged?.Invoke();
            return true;
        }

        public int GetWorkerBuildingUpgradeLevel(EconomyFocusType resource,
            WorkerBuildingUpgradeType upgradeType)
        {
            if (!TryGetWorkerBuildingUpgradeState(out var state))
                return 0;

            return MobileWorkerBuildingUpgradeUtility.GetLevel(state, resource, upgradeType);
        }

        public ResourceCost GetWorkerBuildingUpgradeCost(EconomyFocusType resource,
            WorkerBuildingUpgradeType upgradeType)
        {
            var tuning = GetEconomyPriceTuning();
            if (!TryGetWorkerBuildingUpgradeState(out var state)
                || !MobileWorkerBuildingUpgradeUtility.TryGetNextCost(
                    state, resource, upgradeType, tuning, out var cost))
            {
                return ResourceCost.Zero;
            }

            return new ResourceCost(cost.Wood, 0, cost.Iron, 0);
        }

        public bool CanBuyWorkerBuildingUpgrade(EconomyFocusType resource,
            WorkerBuildingUpgradeType upgradeType)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            var tuning = GetEconomyPriceTuning();
            if (!_initialized || GameState.IsGameOver || resource == EconomyFocusType.Balanced
                || !TryGetWorkerBuildingUpgradeState(out var state)
                || !MobileWorkerBuildingUpgradeUtility.TryGetNextCost(
                    state, resource, upgradeType, tuning, out var cost))
            {
                return false;
            }

            return CanAfford(new ResourceCost(cost.Wood, 0, cost.Iron, 0));
        }

        public bool TryBuyWorkerBuildingUpgrade(EconomyFocusType resource,
            WorkerBuildingUpgradeType upgradeType)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (!CanBuyWorkerBuildingUpgrade(resource, upgradeType)
                || !TryGetMobileConfigEntity(out var mobileConfigEntity))
            {
                return false;
            }

            var state = _entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(mobileConfigEntity);
            var tuning = GetEconomyPriceTuning();
            if (!MobileWorkerBuildingUpgradeUtility.TryGetNextCost(
                    state, resource, upgradeType, tuning, out var upgradeCost))
            {
                return false;
            }

            var nextState = state;
            if (!MobileWorkerBuildingUpgradeUtility.TryIncreaseLevel(
                    ref nextState, resource, upgradeType))
            {
                return false;
            }

            var cost = new ResourceCost(upgradeCost.Wood, 0, upgradeCost.Iron, 0);
            if (!SpendResources(cost))
                return false;

            _entityManager.SetComponentData(mobileConfigEntity, nextState);
            WorkerBuildingUpgrades = nextState;
            ApplyTechEconomyAggregates();
            OnGameStateChanged?.Invoke();
            return true;
        }

        private bool TryGetWorkerBuildingUpgradeState(out MobileWorkerBuildingUpgradeState state)
        {
            state = default;
            if (!TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(mobileConfigEntity))
            {
                return false;
            }

            state = _entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(mobileConfigEntity);
            WorkerBuildingUpgrades = state;
            return true;
        }

        public int GetLastAcceptedPopulationArrivalCount()
        {
            if (!TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
                return 0;

            return Mathf.Max(0,
                _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity)
                    .LastArrivalAcceptedCount);
        }

        public float GetDefensePercent()
        {
            return SingleWallDefenseRules.GetHealthRatio(Wall.CurrentHP, Wall.MaxHP);
        }

        public bool IsMobileFinalWavePressure()
        {
            if (TryGetContinuousSiegeCycle(out var cycle))
                return cycle.Phase == SiegeCyclePhase.Night || cycle.HordePressure01 >= 0.75f;

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

        public int GetTotalArcherCount()
        {
            if (!CanAccessEntityManager())
            {
                long cachedTotal = (long)math.max(0, BasicArcherCount)
                    + math.max(0, RapidArcherCount)
                    + math.max(0, FrostArcherCount);
                return cachedTotal >= int.MaxValue ? int.MaxValue : (int)cachedTotal;
            }

            return GetArcherCount();
        }

        public int GetRemainingArcherCapacity()
        {
            return ArcherCapacityUtility.GetRemainingCapacity(GetTotalArcherCount());
        }

        public bool CanAddArchers(int requestedCount = 1)
        {
            return _initialized
                && ArcherCapacityUtility.CanAdd(GetTotalArcherCount(), requestedCount);
        }

        public float GetArcherTypeDps(ArcherType type)
        {
            var stats = GetScaledArcherStats(type);
            return stats.Damage * stats.FireRate * GetArcherTypeCount(type);
        }

        public ArcherDefinitionSO[] GetArcherDefinitions()
        {
            var catalogDefinitions = archerCatalog != null ? archerCatalog.GetOrderedDefinitions() : null;
            if (catalogDefinitions != null && catalogDefinitions.Length > 0)
                return catalogDefinitions;

            _runtimeDefaultArcherDefinitions ??= new[]
            {
                ArcherDefinitionSO.CreateRuntimeDefault(ArcherType.Basic),
                ArcherDefinitionSO.CreateRuntimeDefault(ArcherType.Rapid),
                ArcherDefinitionSO.CreateRuntimeDefault(ArcherType.Frost)
            };

            return _runtimeDefaultArcherDefinitions;
        }

        public ArcherDefinitionSO GetArcherDefinition(ArcherType type)
        {
            var definition = archerCatalog != null ? archerCatalog.GetDefinition(type) : null;
            if (definition != null)
                return definition;

            var definitions = GetArcherDefinitions();
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].Type == type)
                    return definitions[i];
            }

            return null;
        }

        public ResourceCost GetArcherBuyCost(ArcherType type)
        {
            var definition = GetArcherDefinition(type);
            if (definition != null)
                return GetScaledArcherCost(definition.BuyCost, definition);

            ResourceCost baseCost;
            switch (type)
            {
                case ArcherType.Rapid:
                    baseCost = new ResourceCost(55, 0, 35, 20);
                    break;
                case ArcherType.Frost:
                    baseCost = new ResourceCost(45, 55, 25, 0);
                    break;
                default:
                    baseCost = new ResourceCost(45, 0, 0, 20);
                    break;
            }

            return ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost,
                GetArcherTypeCount(type),
                ArcherRecruitmentCostUtility.DefaultGrowthInterval,
                ArcherRecruitmentCostUtility.DefaultGrowthExponent);
        }

        public ResourceCost GetArcherBuyCost(ArcherDefinitionSO definition)
        {
            return definition != null
                ? GetScaledArcherCost(definition.BuyCost, definition)
                : ResourceCost.Zero;
        }

        public ResourceCost GetArcherRetrainCost(ArcherType targetType)
        {
            if (targetType == ArcherType.Basic)
                return ResourceCost.Zero;

            var definition = GetArcherDefinition(targetType);
            if (definition != null)
                return GetScaledArcherCost(definition.RetrainCost, definition);

            ResourceCost baseCost = targetType == ArcherType.Rapid
                ? new ResourceCost(55, 0, 35, 0)
                : new ResourceCost(45, 55, 25, 0);
            return ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost,
                GetArcherTypeCount(targetType),
                ArcherRecruitmentCostUtility.DefaultGrowthInterval,
                ArcherRecruitmentCostUtility.DefaultGrowthExponent);
        }

        private ResourceCost GetScaledArcherCost(
            ResourceCost baseCost, ArcherDefinitionSO definition)
        {
            return ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost,
                GetArcherTypeCount(definition.Type),
                definition.CostGrowthInterval,
                definition.CostGrowthExponent);
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
            var definition = GetArcherDefinition(type);
            if (definition != null)
                return CanBuyArcher(definition);

            return _initialized
                && _archerPrefabEntity != Entity.Null
                && _entityManager.Exists(_archerPrefabEntity)
                && IsArcherTypeUnlocked(type)
                && CanAddArchers()
                && HasPopulationForNewArcher()
                && CanAfford(GetArcherBuyCost(type));
        }

        public bool CanBuyArcher(ArcherDefinitionSO definition)
        {
            if (definition == null)
                return false;

            return _initialized
                && _archerPrefabEntity != Entity.Null
                && _entityManager.Exists(_archerPrefabEntity)
                && IsArcherTypeUnlocked(definition.Type)
                && CanAddArchers()
                && HasPopulationForNewArcher(definition.PopulationCost)
                && CanAfford(GetArcherBuyCost(definition));
        }

        public bool BuyArcher(ArcherType type)
        {
            var definition = GetArcherDefinition(type);
            return definition != null ? BuyArcher(definition) : BuyArcherFallback(type);
        }

        public bool BuyArcher(ArcherDefinitionSO definition)
        {
            if (!CanBuyArcher(definition))
                return false;

            var cost = GetArcherBuyCost(definition);
            if (!SpendResources(cost))
                return false;

            if (!SpawnArcher(definition.Type))
            {
                if (!freeEconomyTestMode)
                    AddResources(cost);
                return false;
            }

            ConsumePopulationForNewArcher(definition.PopulationCost);
            ReadArcherTypeCounts();
            OnGameStateChanged?.Invoke();
            return true;
        }

        private bool BuyArcherFallback(ArcherType type)
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

        public bool CanRetrainBasicArcher(ArcherType targetType)
        {
            return _initialized
                && targetType != ArcherType.Basic
                && CanAccessEntityManager()
                && IsArcherTypeUnlocked(targetType)
                && GetArcherTypeCount(ArcherType.Basic) > 0
                && CanAfford(GetArcherRetrainCost(targetType));
        }

        public bool RetrainBasicArcher(ArcherType targetType)
        {
            if (!CanRetrainBasicArcher(targetType)
                || !TryFindArcherEntity(ArcherType.Basic, out Entity entity))
            {
                return false;
            }

            ResourceCost cost = GetArcherRetrainCost(targetType);
            if (!SpendResources(cost))
                return false;

            if (!_entityManager.Exists(entity))
            {
                if (!freeEconomyTestMode)
                    AddResources(cost);
                return false;
            }

            ApplyArcherTypeToEntity(entity, targetType);
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

        // ---------------------------------------------------------------------------------
        // Tech Tree (SO-driven dinamik reveal grafi — otoriter dok: TECH_TREE_SO_ARCHITECTURE.md)
        // Kurallar: node satin alinabilir <=> revealed + prerequisite'ler sahipli + !maxed + kaynak yeter.
        // Root otomatik sahipli baslar; satin alma RevealChildNodeIds'i gorunur yapar.
        // UnlockArcherType effect'i MALIYETSIZ icsel yoldan gider (UnlockArcherType() cift harcardi).
        // ---------------------------------------------------------------------------------

        public bool HasTechTreeCatalog => techTreeCatalog != null
            && techTreeCatalog.Nodes != null
            && techTreeCatalog.Nodes.Length > 0;

        private void EnsureTechTreeInitialized()
        {
            if (_techTreeInitialized || techTreeCatalog == null)
                return;

            var root = techTreeCatalog.GetRootNode();
            if (root == null)
                return;

            _techTreeInitialized = true;
            // Root sahipli baslar: oyuncu agaci ilk actiginda satin alinabilir gercek tech'leri gorur.
            _revealedTechNodes.Add(root.Id);
            _techNodeLevels[root.Id] = 1;
            RevealTechChildren(root);
        }

        private void RevealTechChildren(TechNodeDefinitionSO node)
        {
            if (node == null || node.RevealChildNodeIds == null || techTreeCatalog == null)
                return;

            foreach (var childId in node.RevealChildNodeIds)
            {
                if (!string.IsNullOrEmpty(childId) && techTreeCatalog.GetNode(childId) != null)
                    _revealedTechNodes.Add(childId);
            }
        }

        public bool IsTechNodeRevealed(string nodeId)
        {
            EnsureTechTreeInitialized();
            return !string.IsNullOrEmpty(nodeId) && _revealedTechNodes.Contains(nodeId);
        }

        public int GetTechNodeLevel(string nodeId)
        {
            EnsureTechTreeInitialized();
            return !string.IsNullOrEmpty(nodeId) && _techNodeLevels.TryGetValue(nodeId, out int level) ? level : 0;
        }

        public bool IsTechNodeMaxed(TechNodeDefinitionSO node)
        {
            return node != null && GetTechNodeLevel(node.Id) >= node.MaxLevel;
        }

        /// <summary>Su an gorunur (revealed) node tanimlarini katalog sirasiyla dondurur. UI graf kurulumu bunu kullanir.</summary>
        public List<TechNodeDefinitionSO> GetRevealedTechNodes()
        {
            EnsureTechTreeInitialized();
            var result = new List<TechNodeDefinitionSO>();
            if (techTreeCatalog == null || techTreeCatalog.Nodes == null)
                return result;

            foreach (var node in techTreeCatalog.Nodes)
            {
                if (node != null && _revealedTechNodes.Contains(node.Id))
                    result.Add(node);
            }

            return result;
        }

        /// <summary>
        /// Satin alma kural zinciri; reason UI status etiketi icin kisa koddur:
        /// WAIT / HIDDEN / MAX / LOCKED / NEED ... (bos = alinabilir).
        /// </summary>
        public bool CanBuyTechNode(TechNodeDefinitionSO node, out string reason)
        {
            reason = string.Empty;
            if (node == null || !_initialized)
            {
                reason = "WAIT";
                return false;
            }

            EnsureTechTreeInitialized();

            if (!_revealedTechNodes.Contains(node.Id))
            {
                reason = "HIDDEN";
                return false;
            }

            if (GetTechNodeLevel(node.Id) >= node.MaxLevel)
            {
                reason = "MAX";
                return false;
            }

            if (node.PrerequisiteNodeIds != null)
            {
                foreach (var prereqId in node.PrerequisiteNodeIds)
                {
                    if (!string.IsNullOrEmpty(prereqId) && GetTechNodeLevel(prereqId) <= 0)
                    {
                        reason = "LOCKED";
                        return false;
                    }
                }
            }

            var effectiveCost = GetTechNodeCost(node);
            if (!CanAfford(effectiveCost))
            {
                reason = effectiveCost.ToNeedDisplayString(Resources);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Node'un mevcut seviyeye gore efektif maliyeti: Cost * (1 + level * CostGrowthPerLevel).
        /// Tekrarlanabilir sink node'lari (mastery) her seviyede pahalanir.
        /// </summary>
        public ResourceCost GetTechNodeCost(TechNodeDefinitionSO node)
        {
            if (node == null)
                return ResourceCost.Zero;

            float growth = Mathf.Max(0f, node.CostGrowthPerLevel);
            if (growth <= 0f)
                return node.Cost;

            float scale = 1f + GetTechNodeLevel(node.Id) * growth;
            return new ResourceCost(
                Mathf.CeilToInt(node.Cost.Wood * scale),
                Mathf.CeilToInt(node.Cost.Stone * scale),
                Mathf.CeilToInt(node.Cost.Iron * scale),
                Mathf.CeilToInt(node.Cost.Food * scale));
        }

        public bool TryBuyTechNode(string nodeId)
        {
            return TryBuyTechNode(techTreeCatalog != null ? techTreeCatalog.GetNode(nodeId) : null);
        }

        public bool TryBuyTechNode(TechNodeDefinitionSO node)
        {
            if (!CanBuyTechNode(node, out _))
                return false;

            if (!SpendResources(GetTechNodeCost(node)))
                return false;

            int newLevel = GetTechNodeLevel(node.Id) + 1;
            _techNodeLevels[node.Id] = newLevel;

            if (newLevel == 1)
                RevealTechChildren(node);

            ApplyTechNodeEffects(node);
            OnGameStateChanged?.Invoke();
            return true;
        }

        private void ApplyTechNodeEffects(TechNodeDefinitionSO node)
        {
            if (node.Effects == null || node.Effects.Length == 0)
                return;

            bool statsDirty = false;
            bool economyDirty = false;
            bool defenseDirty = false;

            foreach (var effect in node.Effects)
            {
                switch (effect.Type)
                {
                    case TechNodeEffectType.UnlockArcherType:
                        UnlockArcherTypeFromTech(effect.ArcherType);
                        break;
                    case TechNodeEffectType.ModifyArcherDamagePercent:
                        _techDamageMultiplier *= 1f + effect.Value;
                        statsDirty = true;
                        break;
                    case TechNodeEffectType.ModifyArcherFireRatePercent:
                        _techFireRateMultiplier *= 1f + effect.Value;
                        statsDirty = true;
                        break;
                    case TechNodeEffectType.IncreaseWorkerCap:
                    case TechNodeEffectType.IncreaseResourceProductionPercent:
                    case TechNodeEffectType.IncreasePopulationGrowth:
                        economyDirty = true;
                        break;
                    case TechNodeEffectType.DeepenMoatSlowPercent:
                    case TechNodeEffectType.AddMoatDamagePerSecond:
                        // V1: legacy Moat effect'i stale catalog/save icinden sizsa dahi etkisiz.
                        break;
                    case TechNodeEffectType.IncreaseDefenseMaxHpPercent:
                        defenseDirty = true;
                        break;
                    case TechNodeEffectType.ReduceRepairCostPercent:
                        _techRepairCostMultiplier *= Mathf.Clamp01(1f - effect.Value);
                        break;
                    case TechNodeEffectType.UnlockSpellcasting:
                        _fireballUnlocked = true;
                        break;
                    case TechNodeEffectType.ModifySpellDamagePercent:
                        _spellDamageMultiplier *= 1f + effect.Value;
                        break;
                    case TechNodeEffectType.AddSpellRadius:
                        _spellRadiusBonus += effect.Value;
                        break;
                    case TechNodeEffectType.ReduceSpellCooldownPercent:
                        _spellCooldownMultiplier *= Mathf.Clamp01(1f - effect.Value);
                        break;
                }
            }

            if (statsDirty)
                ApplyScaledStatsToArchers(ArcherType.Basic, false);
            if (economyDirty)
                ApplyTechEconomyAggregates();
            if (defenseDirty)
                ApplyTechDefenseAggregates();
        }

        /// <summary>Tech ile acilan okcu tipi: maliyetsiz icsel unlock (node maliyeti zaten odendi).</summary>
        private void UnlockArcherTypeFromTech(ArcherType type)
        {
            if (type == ArcherType.Basic)
                return;

            _unlockedArcherTypes.Add(type);
        }

        /// <summary>
        /// Sahip olunan TUM tech node'larin ekonomi etkilerini base config degerlerinden yeniden hesaplar
        /// ve MobileCastleCombatConfig'e yazar. ECS tuketicileri (MobilePopulationEconomySystem,
        /// GetWorkerCap/GetWorkerProductionRate) config'i her frame taze okudugu icin baska sey gerekmez.
        /// </summary>
        private void ApplyTechEconomyAggregates()
        {
            if (!TryGetMobileConfigEntity(out var configEntity))
                return;

            var config = _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);

            if (!_techConfigBaselineCaptured)
            {
                _baseWoodWorkerCap = config.WoodWorkerCap;
                _baseStoneWorkerCap = config.StoneWorkerCap;
                _baseIronWorkerCap = config.IronWorkerCap;
                _baseFoodWorkerCap = config.FoodWorkerCap;
                _baseWoodProductionPerMin = config.WoodWorkerProductionPerMin;
                _baseStoneProductionPerMin = config.StoneWorkerProductionPerMin;
                _baseIronProductionPerMin = config.IronWorkerProductionPerMin;
                _baseFoodProductionPerMin = config.FoodWorkerProductionPerMin;
                _basePopulationGrowthPerCycle = config.PopulationGrowthPerDayPrep;
                _techConfigBaselineCaptured = true;
            }

            int woodCap = 0, stoneCap = 0, ironCap = 0, foodCap = 0;
            float woodProd = 0f, stoneProd = 0f, ironProd = 0f, foodProd = 0f;
            int growth = 0;

            if (techTreeCatalog != null && techTreeCatalog.Nodes != null)
            {
                foreach (var node in techTreeCatalog.Nodes)
                {
                    if (node == null || node.Effects == null)
                        continue;

                    int level = GetTechNodeLevel(node.Id);
                    if (level <= 0)
                        continue;

                    foreach (var effect in node.Effects)
                    {
                        switch (effect.Type)
                        {
                            case TechNodeEffectType.IncreaseWorkerCap:
                            {
                                int amount = Mathf.RoundToInt(effect.Value) * level;
                                bool all = effect.Resource == EconomyFocusType.Balanced;
                                if (all || effect.Resource == EconomyFocusType.Wood) woodCap += amount;
                                if (all || effect.Resource == EconomyFocusType.Stone) stoneCap += amount;
                                if (all || effect.Resource == EconomyFocusType.Iron) ironCap += amount;
                                if (all || effect.Resource == EconomyFocusType.Food) foodCap += amount;
                                break;
                            }
                            case TechNodeEffectType.IncreaseResourceProductionPercent:
                            {
                                float amount = effect.Value * level;
                                bool all = effect.Resource == EconomyFocusType.Balanced;
                                if (all || effect.Resource == EconomyFocusType.Wood) woodProd += amount;
                                if (all || effect.Resource == EconomyFocusType.Stone) stoneProd += amount;
                                if (all || effect.Resource == EconomyFocusType.Iron) ironProd += amount;
                                if (all || effect.Resource == EconomyFocusType.Food) foodProd += amount;
                                break;
                            }
                            case TechNodeEffectType.IncreasePopulationGrowth:
                                growth += Mathf.RoundToInt(effect.Value) * level;
                                break;
                        }
                    }
                }
            }

            var buildings = _entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(configEntity)
                ? _entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(configEntity)
                : default;
            int woodBuildingCap = MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(
                buildings.WoodCapacityLevel);
            int stoneBuildingCap = MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(
                buildings.StoneCapacityLevel);
            int ironBuildingCap = MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(
                buildings.IronCapacityLevel);
            int foodBuildingCap = MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(
                buildings.FoodCapacityLevel);
            float woodBuildingProd = MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(
                buildings.WoodEfficiencyLevel);
            float stoneBuildingProd = MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(
                buildings.StoneEfficiencyLevel);
            float ironBuildingProd = MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(
                buildings.IronEfficiencyLevel);
            float foodBuildingProd = MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(
                buildings.FoodEfficiencyLevel);

            // Cap toplamlari = base + tech + council + bina yatirimi. Hepsi ayni aggregate'te
            // yasadigi icin bir katmandaki degisiklik diger katmanlarin kazanimini ezmez.
            config.WoodWorkerCap = SaturatingWorkerCap(
                _baseWoodWorkerCap, woodCap, _councilWoodCapBonus, woodBuildingCap);
            config.StoneWorkerCap = SaturatingWorkerCap(
                _baseStoneWorkerCap, stoneCap, _councilStoneCapBonus, stoneBuildingCap);
            config.IronWorkerCap = SaturatingWorkerCap(
                _baseIronWorkerCap, ironCap, _councilIronCapBonus, ironBuildingCap);
            config.FoodWorkerCap = SaturatingWorkerCap(
                _baseFoodWorkerCap, foodCap, _councilFoodCapBonus, foodBuildingCap);
            config.WoodWorkerProductionPerMin = _baseWoodProductionPerMin
                * Mathf.Max(0f, 1f + woodProd + _metaProductionPercent + woodBuildingProd);
            config.StoneWorkerProductionPerMin = _baseStoneProductionPerMin
                * Mathf.Max(0f, 1f + stoneProd + _metaProductionPercent + stoneBuildingProd);
            config.IronWorkerProductionPerMin = _baseIronProductionPerMin
                * Mathf.Max(0f, 1f + ironProd + _metaProductionPercent + ironBuildingProd);
            config.FoodWorkerProductionPerMin = _baseFoodProductionPerMin
                * Mathf.Max(0f, 1f + foodProd + _metaProductionPercent + foodBuildingProd);
            config.PopulationGrowthPerDayPrep = _basePopulationGrowthPerCycle + growth;
            ApplyHeartEconomyOverrides(ref config);
            MoatDormancyRules.ApplyV1(ref config);
            _entityManager.SetComponentData(configEntity, config);
            WorkerBuildingUpgrades = buildings;
        }

        private static int SaturatingWorkerCap(int baseValue, int techBonus,
            int councilBonus, int buildingBonus)
        {
            long total = (long)baseValue + techBonus + councilBonus + buildingBonus;
            if (total <= 0L)
                return 0;
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// Sahip olunan tech'lerin toplam MaxHP yuzdesini base degerden hesaplar.
        /// Yalniz Wall MaxHP'sini yazar ve CurrentHP oranini korur.
        /// </summary>
        private void ApplyTechDefenseAggregates()
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity))
                return;

            if (!_techDefenseBaselineCaptured)
            {
                _baseWallMaxHp = _entityManager.GetComponentData<WallSegment>(_castleEntity).MaxHP;
                _techDefenseBaselineCaptured = true;
            }

            float totalPercent = 0f;
            if (techTreeCatalog != null && techTreeCatalog.Nodes != null)
            {
                foreach (var node in techTreeCatalog.Nodes)
                {
                    if (node == null || node.Effects == null)
                        continue;

                    int level = GetTechNodeLevel(node.Id);
                    if (level <= 0)
                        continue;

                    foreach (var effect in node.Effects)
                    {
                        if (effect.Type == TechNodeEffectType.IncreaseDefenseMaxHpPercent)
                            totalPercent += effect.Value * level;
                    }
                }
            }

            float multiplier = Mathf.Max(0f, 1f + totalPercent + _metaWallHpPercent);
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            float healthRatio = SingleWallDefenseRules.GetHealthRatio(wall.CurrentHP, wall.MaxHP);
            wall.MaxHP = ApplyHeartWallMultiplier(_baseWallMaxHp * multiplier);
            wall.CurrentHP = wall.MaxHP * healthRatio;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
        }

        /// <summary>Restart'ta tech state'i sifirlar ve config/defense degerlerini base'e dondurur.</summary>
        private void ResetTechTreeState()
        {
            ResetHeartRuntime();
            _techNodeLevels.Clear();
            _revealedTechNodes.Clear();
            _techTreeInitialized = false;
            _techDamageMultiplier = 1f;
            _techFireRateMultiplier = 1f;
            _techRepairCostMultiplier = 1f;
            _fireballUnlocked = false;
            _spellDamageMultiplier = 1f;
            _spellRadiusBonus = 0f;
            _spellCooldownMultiplier = 1f;
            _fireballCooldownRemaining = 0f;

            if (_techConfigBaselineCaptured && TryGetMobileConfigEntity(out var configEntity))
            {
                var config = _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
                config.WoodWorkerCap = _baseWoodWorkerCap;
                config.StoneWorkerCap = _baseStoneWorkerCap;
                config.IronWorkerCap = _baseIronWorkerCap;
                config.FoodWorkerCap = _baseFoodWorkerCap;
                config.WoodWorkerProductionPerMin = _baseWoodProductionPerMin;
                config.StoneWorkerProductionPerMin = _baseStoneProductionPerMin;
                config.IronWorkerProductionPerMin = _baseIronProductionPerMin;
                config.FoodWorkerProductionPerMin = _baseFoodProductionPerMin;
                config.PopulationGrowthPerDayPrep = _basePopulationGrowthPerCycle;
                MoatDormancyRules.ApplyV1(ref config);
                _entityManager.SetComponentData(configEntity, config);
            }

            if (_techDefenseBaselineCaptured && CanAccessEntityManager() && _entityManager.Exists(_castleEntity))
            {
                var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
                float healthRatio = SingleWallDefenseRules.GetHealthRatio(wall.CurrentHP, wall.MaxHP);
                wall.MaxHP = _baseWallMaxHp;
                wall.CurrentHP = wall.MaxHP * healthRatio;
                _entityManager.SetComponentData(_castleEntity, wall);
                Wall = wall;
            }
        }

        // ---------------------------------------------------------------------------------
        // Council Events (safak meclisi — otoriter dok: COUNCIL_EVENTS_ARCHITECTURE.md)
        // Kart DAWN'da belirir, DAY boyunca yasar, DUSK girisinde expire olur (UI surer).
        // Event'ler asset degil: CouncilComposer sablon x atom x baglam x olcekten uretir.
        // ---------------------------------------------------------------------------------

        public bool HasCouncilCatalog => councilCatalog != null
            && councilCatalog.Templates != null && councilCatalog.Templates.Length > 0
            && councilCatalog.Atoms != null && councilCatalog.Atoms.Length > 0;

        /// <summary>
        /// UI tarafindan Dawn kenarinda cagrilir. Yalniz Day 3,6,9,12... gunlerinde bir kez
        /// regular kart acar. Chance/pity/cooldown yoktur; seed run icinde deterministiktir.
        /// </summary>
        public bool TryOpenRegularCouncilEvent()
        {
            // NOT: _initialized burada KULLANILMAZ — frame-arasi okumalarda dalgalanabiliyor
            // (TryGetMobileConfigEntity'nin dispose-catch'i dusurup Update yeniden kuruyor).
            // Cycle cache'inin akiyor olmasi yeterli sinyaldir; etki uygulayicilar zaten
            // kendi CanAccessEntityManager guard'larina sahiptir.
            if (!HasCouncilCatalog || !ContinuousSiegeCycle.Enabled || _activeCouncilEvent != null)
                return false;

            int day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            if (!CouncilRegularSchedule.ShouldOpen(
                    day,
                    _lastRegularCouncilDay,
                    ContinuousSiegeCycle.Phase))
                return false;

            // Ayni scheduled gun, catalog/runtime problemi olsa bile ikinci kez reroll edilmez.
            // Bu fail-closed davranis seed veya hot-reload ile farkli kart cikmasini engeller.
            _lastRegularCouncilDay = day;
            if (!councilCatalog.TryValidateRuntimeContent(out string contentProblem))
            {
                Debug.LogError($"[GameManager] Day {day} Council content gate reddetti: "
                               + contentProblem);
                return false;
            }

            uint seed = GetCouncilSeed(day);
            var context = BuildCouncilContext(day);
            var composed = CouncilComposer.Compose(councilCatalog, seed, context);
            if (composed == null)
            {
                Debug.LogError($"[GameManager] Day {day} regular Council compose edilemedi; "
                               + "scheduled gun fail-closed kapatildi.");
                return false;
            }

            _activeCouncilEvent = composed;

            _recentCouncilTemplates.Add(composed.TemplateId);
            int memory = Mathf.Max(1, councilCatalog.RecentTemplateMemory);
            while (_recentCouncilTemplates.Count > memory)
                _recentCouncilTemplates.RemoveAt(0);

            foreach (var template in councilCatalog.Templates)
            {
                if (template != null && template.Id == composed.TemplateId && template.OneShot)
                    _usedOneShotCouncils.Add(template.Id);
            }

            OnGameStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Legacy API wrapper. Regular schedule disinda chance roll yapmaz.
        /// Yeni cagrilar TryOpenRegularCouncilEvent kullanmalidir.
        /// </summary>
        public bool TryRollCouncilEvent()
        {
            return TryOpenRegularCouncilEvent();
        }

        /// <summary>
        /// Secenegin canli state'te tam uygulanacak sayisal ozetini dondurur. UI metni ile
        /// interactable karari ayni quote'tan gelir; composer'in eski yaklaşık etiketi owner degildir.
        /// </summary>
        public CouncilOptionPresentation GetCouncilOptionPresentation(ComposedCouncilOption option)
        {
            TryBuildCouncilOptionPresentationContext(out CouncilOptionPresentationContext context);
            return CouncilOptionPresentationUtility.Build(option, context);
        }

        /// <summary>Secenegin butun etkileri yazdigi exact sayilarla uygulanabiliyor mu.</summary>
        public bool CanAffordCouncilOption(ComposedCouncilOption option)
        {
            return GetCouncilOptionPresentation(option).CanApplyExactly;
        }

        private bool TryBuildCouncilOptionPresentationContext(
            out CouncilOptionPresentationContext context)
        {
            context = new CouncilOptionPresentationContext
            {
                IgnoreResourcePayments = freeEconomyTestMode,
                FoodCostPerArrival = MobilePopulationArrivalUtility.DefaultFoodCostPerArrival,
                WallCurrentHp = Wall.CurrentHP,
                WallMaxHp = Wall.MaxHP,
            };

            if (!CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return false;

            ResourceData resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            PopulationState population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            context.RuntimeReady = true;
            context.Resources = resources;
            context.CurrentPopulation = population.Total;
            context.TotalArchers = GetArcherCount();
            context.IdlePopulation = GetIdlePopulation();

            if (_entityManager.Exists(_castleEntity)
                && _entityManager.HasComponent<WallSegment>(_castleEntity))
            {
                WallSegment wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
                context.WallCurrentHp = wall.CurrentHP;
                context.WallMaxHp = wall.MaxHP;
            }

            if (TryGetMobileConfigEntity(out Entity configEntity)
                && _entityManager.HasComponent<MobileCastleCombatConfig>(configEntity)
                && _entityManager.HasComponent<MobileBedCapacityState>(configEntity))
            {
                MobileCastleCombatConfig config =
                    _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
                MobileBedCapacityState beds =
                    _entityManager.GetComponentData<MobileBedCapacityState>(configEntity);
                context.PopulationRulesReady = true;
                context.TotalBedCapacity = MobileBedCapacityUtility.GetTotalCapacity(beds);
                context.FoodCostPerArrival = Mathf.Max(1, config.FoodCostPerArrival);
            }

            return true;
        }

        public bool ChooseCouncilOption(bool optionA)
        {
            var active = _activeCouncilEvent;
            if (active == null)
                return false;

            if (!CouncilContentPolicy.TryValidateComposedEvent(
                    councilCatalog, active, out string contentProblem))
            {
                Debug.LogError("[GameManager] Council content gate karari reddetti: "
                               + contentProblem);
                return false;
            }

            var option = optionA ? active.OptionA : active.OptionB;
            if (!CanAffordCouncilOption(option))
                return false;

            ApplyCouncilEffects(option.Effects);

            int day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            SetCouncilFlag("council_" + active.TemplateId + (optionA ? "_a" : "_b"), day);
            string extraFlag = optionA ? active.SetsFlagOnA : active.SetsFlagOnB;
            if (!string.IsNullOrEmpty(extraFlag))
            {
                if (councilCatalog != null
                    && councilCatalog.IsApprovedChainSource(active.TemplateId, optionA, extraFlag))
                {
                    SetCouncilFlag(extraFlag, day);
                }
                else
                {
                    Debug.LogError($"[GameManager] Council '{active.TemplateId}' "
                                   + $"onaysiz chain flag'i setlemeye calisti: '{extraFlag}'.");
                }
            }

            _activeCouncilEvent = null;
            OnGameStateChanged?.Invoke();
            return true;
        }

        /// <summary>Karar penceresi kapandi (DUSK) — kart secilmeden dagilir; flag yazilmaz.</summary>
        public void ExpireCouncilEvent()
        {
            if (_activeCouncilEvent == null)
                return;

            _activeCouncilEvent = null;
            OnGameStateChanged?.Invoke();
        }

        private void ApplyCouncilEffects(List<ComposedCouncilEffect> effects)
        {
            bool capsDirty = false;
            bool archerCountsDirty = false;
            foreach (var effect in effects)
            {
                if (!CouncilContentPolicy.IsCouncilOwnedEffectKind(effect.Kind))
                {
                    Debug.LogError($"[GameManager] Council-owned olmayan effect apply reddedildi: "
                                   + $"'{effect.Kind}' ({(int)effect.Kind}).");
                    continue;
                }

                switch (effect.Kind)
                {
                    case CouncilEffectKind.GainResource:
                        AddResources(BuildSingleResourceCost(effect.Resource, effect.Amount));
                        break;
                    case CouncilEffectKind.PayResource:
                        SpendResources(BuildSingleResourceCost(effect.Resource, effect.Amount));
                        break;
                    case CouncilEffectKind.GainPopulation:
                        ApplyCouncilPopulationGain(effect.Amount);
                        break;
                    case CouncilEffectKind.GainFreeArchers:
                        archerCountsDirty |= ApplyCouncilFreeArcherGain(effect.Amount) > 0;
                        break;
                    case CouncilEffectKind.HealDefensePercent:
                        // Council savunma etkisinin tek hedefi Wall'dur; Gate/Core yolu yoktur.
                        HealWallByPercent(Mathf.Abs(effect.Rate));
                        break;
                    case CouncilEffectKind.WorkerCapBonus:
                        ApplyCouncilCapBonus(effect.Resource, effect.Amount);
                        capsDirty = true;
                        break;
                    case CouncilEffectKind.TempProductionBoost:
                        ApplyCouncilProductionModifier(effect.Resource, 1f + Mathf.Abs(effect.Rate), effect.DurationDays);
                        break;
                    case CouncilEffectKind.TempProductionPenalty:
                        ApplyCouncilProductionModifier(effect.Resource, Mathf.Clamp(1f - Mathf.Abs(effect.Rate), 0.1f, 1f), effect.DurationDays);
                        break;
                    case CouncilEffectKind.NextNightSpawnDelta:
                        ApplyCouncilNightModifier(effect.Rate);
                        break;
                }
            }

            if (capsDirty)
                ApplyTechEconomyAggregates(); // cap toplamlari base+tech+council olarak yeniden yazilir
            if (archerCountsDirty)
                ReadArcherTypeCounts();
        }

        private bool TryGetCouncilPopulationGainBudget(
            int requestedPopulation,
            out MobilePopulationArrivalBudget budget)
        {
            budget = default;
            if (!CanAccessEntityManager()
                || !_entityManager.Exists(_gameStateEntity)
                || !TryGetMobileConfigEntity(out var configEntity)
                || !_entityManager.HasComponent<MobileCastleCombatConfig>(configEntity)
                || !_entityManager.HasComponent<MobileBedCapacityState>(configEntity))
            {
                return false;
            }

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            var config = _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            var beds = _entityManager.GetComponentData<MobileBedCapacityState>(configEntity);
            budget = CouncilEffectGuardUtility.CalculatePopulationGain(
                requestedPopulation,
                population.Total,
                MobileBedCapacityUtility.GetTotalCapacity(beds),
                resources.Food,
                config.FoodCostPerArrival);
            return true;
        }

        private int ApplyCouncilPopulationGain(int requestedPopulation)
        {
            if (!TryGetCouncilPopulationGainBudget(requestedPopulation, out var budget)
                || budget.AcceptedArrivals <= 0
                || !TryGetMobileConfigEntity(out var configEntity))
            {
                return 0;
            }

            var beds = _entityManager.GetComponentData<MobileBedCapacityState>(configEntity);
            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);

            resources.Food = math.max(0, resources.Food - budget.RequiredFood);
            population.Total += budget.AcceptedArrivals;
            population.BaseCapacity = math.max(0, beds.BaseCapacity);
            population.Capacity = MobileBedCapacityUtility.GetTotalCapacity(beds);
            population.Idle = math.max(0, population.Total - population.Workers - population.Archers);

            _entityManager.SetComponentData(_gameStateEntity, resources);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Resources = resources;
            Population = population;
            return budget.AcceptedArrivals;
        }

        private int ApplyCouncilFreeArcherGain(int requestedArchers)
        {
            int allowed = CouncilEffectGuardUtility.GetAllowedFreeArcherGain(
                requestedArchers,
                GetArcherCount(),
                GetIdlePopulation());
            int spawned = 0;
            for (int i = 0; i < allowed; i++)
            {
                if (!SpawnArcher(ArcherType.Basic))
                    break;

                if (!ConsumePopulationForCouncilArcher())
                {
                    Debug.LogError("[GameManager] Council archer spawn population commit olmadan tamamlandi.");
                    break;
                }

                spawned++;
            }

            return spawned;
        }

        private bool ConsumePopulationForCouncilArcher()
        {
            if (!IsMobilePopulationEconomyEnabled()
                || !CanAccessEntityManager()
                || !_entityManager.Exists(_gameStateEntity))
            {
                return false;
            }

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            int workers = PopulationAllocation.WoodWorkers
                + PopulationAllocation.StoneWorkers
                + PopulationAllocation.IronWorkers
                + PopulationAllocation.FoodWorkers;
            int idle = math.max(0, population.Total - population.Archers - workers);
            if (idle <= 0)
                return false;

            population.Workers = workers;
            population.Archers = math.min(population.Total, population.Archers + 1);
            population.Idle = math.max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
            return true;
        }

        private static ResourceCost BuildSingleResourceCost(EconomyFocusType resource, int amount)
        {
            switch (resource)
            {
                case EconomyFocusType.Stone: return new ResourceCost(0, amount, 0, 0);
                case EconomyFocusType.Iron: return new ResourceCost(0, 0, amount, 0);
                case EconomyFocusType.Food: return new ResourceCost(0, 0, 0, amount);
                default: return new ResourceCost(amount, 0, 0, 0);
            }
        }

        private void HealWallByPercent(float percent)
        {
            if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity) || percent <= 0f)
                return;

            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            float healedHp = SingleWallDefenseRules.HealByMaxPercent(wall.CurrentHP, wall.MaxHP, percent);
            if (Mathf.Approximately(healedHp, wall.CurrentHP))
                return;

            wall.CurrentHP = healedHp;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
        }

        private void ApplyCouncilCapBonus(EconomyFocusType resource, int amount)
        {
            bool all = resource == EconomyFocusType.Balanced;
            if (all || resource == EconomyFocusType.Wood) _councilWoodCapBonus += amount;
            if (all || resource == EconomyFocusType.Stone) _councilStoneCapBonus += amount;
            if (all || resource == EconomyFocusType.Iron) _councilIronCapBonus += amount;
            if (all || resource == EconomyFocusType.Food) _councilFoodCapBonus += amount;
        }

        private void ApplyCouncilProductionModifier(EconomyFocusType resource, float multiplier, int durationDays)
        {
            if (!TryGetMobileConfigEntity(out var configEntity)
                || !_entityManager.HasComponent<MobileEconomyEventState>(configEntity))
                return;

            int day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            var eventState = _entityManager.GetComponentData<MobileEconomyEventState>(configEntity);
            // Tek aktif bonus slotu: yeni gelen eskisini ezer (V1 kisiti, dokumante)
            eventState.ProductionBonusResource = resource;
            eventState.ProductionBonusMultiplier = multiplier;
            eventState.ProductionBonusExpiresAfterWave = day + Mathf.Max(1, durationDays);
            _entityManager.SetComponentData(configEntity, eventState);
            EconomyEvent = eventState;
        }

        private void ApplyCouncilNightModifier(float rateDelta)
        {
            if (!TryGetMobileConfigEntity(out var configEntity)
                || !_entityManager.HasComponent<MobileEconomyEventState>(configEntity))
                return;

            int day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            // "Sonraki gece": Dawn'da secildiyse bir sonraki cycle'in gecesi (expire +2),
            // Day/Dusk'ta secildiyse bu cycle'in gecesi (expire +1).
            bool inDawn = ContinuousSiegeCycle.Phase == SiegeCyclePhase.Dawn;
            var eventState = _entityManager.GetComponentData<MobileEconomyEventState>(configEntity);
            eventState.NextNightSpawnMultiplier =
                CouncilEffectGuardUtility.ResolveNightCountMultiplier(rateDelta);
            eventState.NightSpawnExpiresAfterWave = day + (inDawn ? 2 : 1);
            _entityManager.SetComponentData(configEntity, eventState);
            EconomyEvent = eventState;
        }

        private void SetCouncilFlag(string flag, int day)
        {
            if (string.IsNullOrEmpty(flag))
                return;

            if (!_councilFlags.ContainsKey(flag))
                _councilFlags[flag] = day;
        }

        private uint GetCouncilSeed(int day)
        {
            uint baseSeed = 91273u;
            if (TryGetMobileConfigEntity(out var configEntity)
                && _entityManager.HasComponent<MobileEconomyEventState>(configEntity))
            {
                uint stored = _entityManager.GetComponentData<MobileEconomyEventState>(configEntity).RandomSeed;
                if (stored != 0u)
                    baseSeed = stored;
            }

            if (_councilRunSalt == 0u)
                _councilRunSalt = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            return math.hash(new uint3(baseSeed, _councilRunSalt, (uint)day));
        }

        private CouncilContext BuildCouncilContext(int day)
        {
            return new CouncilContext
            {
                Day = day,
                Wood = Resources.Wood,
                Stone = Resources.Stone,
                Iron = Resources.Iron,
                Food = Resources.Food,
                WoodPerMin = GetWorkerProductionRate(EconomyFocusType.Wood),
                StonePerMin = GetWorkerProductionRate(EconomyFocusType.Stone),
                IronPerMin = GetWorkerProductionRate(EconomyFocusType.Iron),
                FoodPerMin = GetWorkerProductionRate(EconomyFocusType.Food),
                Defense01 = GetDefensePercent(),
                Flags = _councilFlags,
                RecentTemplateIds = _recentCouncilTemplates,
                UsedOneShotTemplateIds = _usedOneShotCouncils,
            };
        }

        private void ResetCouncilState()
        {
            _councilFlags.Clear();
            _recentCouncilTemplates.Clear();
            _usedOneShotCouncils.Clear();
            _activeCouncilEvent = null;
            _lastRegularCouncilDay = -1;
            _councilRunSalt = (uint)UnityEngine.Random.Range(1, int.MaxValue); // her kosuya taze zar
            _councilWoodCapBonus = 0;
            _councilStoneCapBonus = 0;
            _councilIronCapBonus = 0;
            _councilFoodCapBonus = 0;
        }

        // ---------------------------------------------------------------------------------
        // Safak-Checkpoint Save/Load (M-E — otoriter dok: RUN_SAVE_ARCHITECTURE.md)
        // Kayit: Dawn'a giris kenari. Restore: temiz RestartGame tabani + snapshot yazimi;
        // turetilebilir her sey (tech carpanlari, reveal, spell, meta) yeniden hesaplanir.
        // ---------------------------------------------------------------------------------

        /// <summary>Kosunun safak fotografini diske yazar (yalniz recompute-EDILEMEYEN durum).</summary>
        private void SaveRunCheckpointLegacy()
        {
            var save = new RunSaveState
            {
                CycleIndex = ContinuousSiegeCycle.CycleIndex,
                XP = GameState.XP,
                Level = GameState.Level,
                XPToNextLevel = GameState.XPToNextLevel,
                TotalKills = GameState.TotalKills,
                Wood = Resources.Wood,
                Stone = Resources.Stone,
                Iron = Resources.Iron,
                Food = Resources.Food,
                PopulationTotal = Population.Total,
                PopulationCapacity = Population.Capacity,
                PopulationBaseCapacity = Population.BaseCapacity,
                BedBaseCapacity = BedCapacity.BaseCapacity,
                PurchasedBedCapacity = BedCapacity.PurchasedCapacity,
                WoodBuildingCapacityLevel = WorkerBuildingUpgrades.WoodCapacityLevel,
                WoodBuildingEfficiencyLevel = WorkerBuildingUpgrades.WoodEfficiencyLevel,
                StoneBuildingCapacityLevel = WorkerBuildingUpgrades.StoneCapacityLevel,
                StoneBuildingEfficiencyLevel = WorkerBuildingUpgrades.StoneEfficiencyLevel,
                IronBuildingCapacityLevel = WorkerBuildingUpgrades.IronCapacityLevel,
                IronBuildingEfficiencyLevel = WorkerBuildingUpgrades.IronEfficiencyLevel,
                FoodBuildingCapacityLevel = WorkerBuildingUpgrades.FoodCapacityLevel,
                FoodBuildingEfficiencyLevel = WorkerBuildingUpgrades.FoodEfficiencyLevel,
                WoodWorkers = PopulationAllocation.WoodWorkers,
                StoneWorkers = PopulationAllocation.StoneWorkers,
                IronWorkers = PopulationAllocation.IronWorkers,
                FoodWorkers = PopulationAllocation.FoodWorkers,
                WallCurrentHP = Wall.CurrentHP,
                ArcherFormationVersion = _archerFormationVersion,
                BasicArchers = BasicArcherCount,
                RapidArchers = RapidArcherCount,
                FrostArchers = FrostArcherCount,
                GlobalArrowDamageBonus = _globalArrowDamageBonus,
                GlobalFireRateMultiplier = _globalFireRateMultiplier,
                CouncilRunSalt = _councilRunSalt,
                CouncilWoodCapBonus = _councilWoodCapBonus,
                CouncilStoneCapBonus = _councilStoneCapBonus,
                CouncilIronCapBonus = _councilIronCapBonus,
                CouncilFoodCapBonus = _councilFoodCapBonus,
                EconomyFocus = (int)EconomyFocus
            };

            if (CanAccessEntityManager() && _entityManager.Exists(_castleEntity)
                && _entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                save.CastleUpgradeLevel = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity).Level;
            }

            foreach (var pair in _techNodeLevels)
                save.TechNodeLevels.Add(new TechLevelEntry { Id = pair.Key, Level = pair.Value });
            foreach (var pair in _archerTypeLevels)
                save.ArcherTypeLevels.Add(new ArcherLevelEntry { Type = (int)pair.Key, Level = pair.Value });
            foreach (var pair in _upgradeTiers)
                save.UpgradeTiers.Add(new UpgradeTierEntry { Type = (int)pair.Key, Tier = pair.Value });
            foreach (var pair in _councilFlags)
                save.CouncilFlags.Add(new CouncilFlagEntry { Flag = pair.Key, Day = pair.Value });
            save.RecentCouncilTemplates.AddRange(_recentCouncilTemplates);
            save.UsedOneShotCouncils.AddRange(_usedOneShotCouncils);

            RunPersistence.Save(save);
        }

        /// <summary>
        /// Ana menu CONTINUE: kaydedilen kosuyu geri yukler. Akis: RestartGame temiz tabani
        /// (meta uygulanmis, taze okcu seed'i) -> tech'ler maliyetsiz yeniden uygulanir
        /// (carpanlar/reveal/spell/config aggregate'leri recompute) -> council hafizasi ->
        /// okcu tamamlama -> ECS snapshot yazimlari. Oyun YENI GUNUN sabahindan bas
        /// (kaydedilen gunun safak odulleri zaten verilmisti).
        /// </summary>
        private bool TryRestoreRunFromCheckpointLegacy()
        {
            var save = RunPersistence.TryLoad();
            if (save == null || !_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            RestartGame();

            RestoreWorkerBuildingUpgradeState(mobileConfigEntity, save);

            // 1) Tech: seviyeleri maliyetsiz yeniden uygula (reveal + carpan + spell + aggregate)
            foreach (var entry in save.TechNodeLevels)
                RestoreSavedTechNodeLevels(entry.Id, entry.Level);

            // 2) Council hafizasi (salt DAHIL — kosu-ici RNG determinizmi korunur)
            _councilFlags.Clear();
            foreach (var flag in save.CouncilFlags)
                _councilFlags[flag.Flag] = flag.Day;
            _recentCouncilTemplates.Clear();
            _recentCouncilTemplates.AddRange(save.RecentCouncilTemplates);
            _usedOneShotCouncils.Clear();
            foreach (var id in save.UsedOneShotCouncils)
                _usedOneShotCouncils.Add(id);
            _councilRunSalt = save.CouncilRunSalt;
            _councilWoodCapBonus = save.CouncilWoodCapBonus;
            _councilStoneCapBonus = save.CouncilStoneCapBonus;
            _councilIronCapBonus = save.CouncilIronCapBonus;
            _councilFoodCapBonus = save.CouncilFoodCapBonus;
            _lastRegularCouncilDay = -1;
            ApplyTechEconomyAggregates(); // council cap bonuslari fold'lanir

            // 3) Level-up kartlari + okcu yukseltme seviyeleri (canli okculara stats yansir)
            _upgradeTiers.Clear();
            foreach (var tier in save.UpgradeTiers)
                _upgradeTiers[(UpgradeType)tier.Type] = tier.Tier;
            _globalArrowDamageBonus = save.GlobalArrowDamageBonus;
            _globalFireRateMultiplier = save.GlobalFireRateMultiplier;
            _archerTypeLevels.Clear();
            foreach (var entry in save.ArcherTypeLevels)
                _archerTypeLevels[(ArcherType)entry.Type] = entry.Level;

            _archerFormationVersion = ArcherFormationUtility.NormalizeVersion(
                save.ArcherFormationVersion);
            RepositionExistingMobileArchersToOutside();

            // 4) Okcu sayilarini tamamla (RestartGame seed'i + meta okculari zaten sahnede;
            //    pozisyonlar tilemap slot sirasindan otomatik)
            RestoreArcherCountsWithinCapacity(
                save.BasicArchers, save.RapidArchers, save.FrostArchers);
            ApplyScaledStatsToArchers(ArcherType.Basic, false);
            ApplyScaledStatsToArchers(ArcherType.Rapid, false);
            ApplyScaledStatsToArchers(ArcherType.Frost, false);

            // 5) ECS snapshot yazimlari
            _entityManager.SetComponentData(_gameStateEntity, new ResourceData
            {
                Wood = save.Wood,
                Stone = save.Stone,
                Iron = save.Iron,
                Food = save.Food
            });

            if (_entityManager.HasComponent<EconomyFocusState>(mobileConfigEntity))
            {
                _entityManager.SetComponentData(mobileConfigEntity,
                    new EconomyFocusState { Type = (EconomyFocusType)save.EconomyFocus });
                EconomyFocus = (EconomyFocusType)save.EconomyFocus;
            }

            int restoredCycleIndex = save.CycleIndex + 1; // yeni gunun sabahi
            if (_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
            {
                var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
                ApplySavedWorkerAllocation(ref allocation, save);
                // kayit anindaki gunun safak odulu VERILMISTI — cift odul gate'i
                allocation.LastPopulationGrowthCycle = save.CycleIndex + 1;
                allocation.LastEventPrepWave = save.CycleIndex + 1;
                _entityManager.SetComponentData(mobileConfigEntity, allocation);
                PopulationAllocation = allocation;
            }

            int totalArchers = save.BasicArchers + save.RapidArchers + save.FrostArchers;
            int totalWorkers = save.WoodWorkers + save.StoneWorkers + save.IronWorkers + save.FoodWorkers;
            RestoreBedCapacityState(mobileConfigEntity, save);
            _entityManager.SetComponentData(_gameStateEntity, new PopulationState
            {
                Total = save.PopulationTotal,
                Workers = totalWorkers,
                Archers = totalArchers,
                Idle = Mathf.Max(0, save.PopulationTotal - totalWorkers - totalArchers),
                Capacity = save.PopulationCapacity,
                BaseCapacity = save.PopulationBaseCapacity,
                FoodPerAssignedPerMin = Population.FoodPerAssignedPerMin
            });

            if (_entityManager.HasComponent<ContinuousSiegeCycleData>(mobileConfigEntity))
            {
                var cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
                cycle.CycleIndex = restoredCycleIndex;
                cycle.CycleTimer = 0f;
                cycle.CycleProgress01 = 0f;
                cycle.PhaseProgress01 = 0f;
                cycle.Phase = SiegeCyclePhase.Day;
                _entityManager.SetComponentData(mobileConfigEntity, cycle);
                ContinuousSiegeCycle = cycle;
            }
            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            gameState.XP = save.XP;
            gameState.Level = save.Level;
            gameState.XPToNextLevel = save.XPToNextLevel;
            gameState.TotalKills = save.TotalKills;
            _entityManager.SetComponentData(_gameStateEntity, gameState);

            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);
                upgrade.Level = save.CastleUpgradeLevel;
                _entityManager.SetComponentData(_castleEntity, upgrade);
            }

            // 6) Wall CurrentHP EN SON (MaxHP tech/meta aggregate'lerinden kuruldu)
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            wall.CurrentHP = Mathf.Clamp(save.WallCurrentHP, 0f, wall.MaxHP);
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;

            // 7) Snapshot exact Dawn'daysa ve scheduled gun henuz handled degilse kart acilir.
            TryOpenRegularCouncilEvent();

            OnGameStateChanged?.Invoke();
            return true;
        }

        /// <summary>V1 exact run snapshot: oyuncunun ayni ana donebilmesi icin runtime state'i kaydeder.</summary>
        public bool SaveRunSnapshot()
        {
            if (!TryInitialize() || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;

            ReadECSData();
            // ReadECSData ECS truth'ini yeni okur. Lethal state bu cagri icinde tespit
            // edilmisse death transaction snapshot'tan once kazanir; canli save yazilamaz.
            if (GameState.IsGameOver || SingleWallDefenseRules.IsDestroyed(Wall.CurrentHP))
            {
                ProcessRunDeath();
                return false;
            }

            EnsureCurrentRunId();
            if (!TryCaptureHeartGraphForSave(
                    out GeneratedRunGraph heartGraphSnapshot,
                    out string heartSaveError))
            {
                Debug.LogError("[GameManager] Castle Heart exact save reddedildi: " + heartSaveError);
                return false;
            }

            var cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            var accumulator = _entityManager.GetComponentData<ResourceAccumulator>(_gameStateEntity);
            var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
            var prep = _entityManager.GetComponentData<CastleYardPrepState>(mobileConfigEntity);
            var economyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
            var bedCapacity = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);
            var workerBuildings = _entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(mobileConfigEntity)
                ? _entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(mobileConfigEntity)
                : default;
            var spawnBudget = _entityManager.HasComponent<ContinuousSpawnBudgetData>(mobileConfigEntity)
                ? _entityManager.GetComponentData<ContinuousSpawnBudgetData>(mobileConfigEntity)
                : default;

            var save = new RunSaveState
            {
                RunId = _currentRunId,
                CycleIndex = cycle.CycleIndex,
                CyclePhase = (int)cycle.Phase,
                CycleTimer = cycle.CycleTimer,
                CycleProgress01 = cycle.CycleProgress01,
                PhaseProgress01 = cycle.PhaseProgress01,
                SpawnIntensityMultiplier = cycle.SpawnIntensityMultiplier,
                HordePressure01 = cycle.HordePressure01,
                IsBloodMoonNight = false,
                XP = GameState.XP,
                Level = GameState.Level,
                XPToNextLevel = GameState.XPToNextLevel,
                TotalKills = GameState.TotalKills,
                CurrentWave = wave.CurrentWave,
                ZombiesToSpawn = wave.ZombiesToSpawn,
                ZombiesSpawned = wave.ZombiesSpawned,
                SpawnTimer = wave.SpawnTimer,
                SpawnInterval = wave.SpawnInterval,
                ZombieHP = wave.ZombieHP,
                ZombieDamage = wave.ZombieDamage,
                ZombieSpeed = wave.ZombieSpeed,
                WaveActive = wave.WaveActive,
                WavePhase = (int)wave.Phase,
                PrepTimer = wave.PrepTimer,
                PrepDuration = wave.PrepDuration,
                WaveStartDelay = wave.WaveStartDelay,
                WaveStartTimer = wave.WaveStartTimer,
                SpawnRandomState = wave.SpawnRandomState,
                SpawnBacklog = spawnBudget.PendingEnemies,
                TotalDemandedEnemies = spawnBudget.TotalDemandedEnemies,
                TotalBudgetSpawnedEnemies = spawnBudget.TotalSpawnedEnemies,
                DemandPerInterval = spawnBudget.DemandPerInterval,
                LastDemandedEnemies = spawnBudget.LastDemandedEnemies,
                LastBudgetSpawnedEnemies = spawnBudget.LastSpawnedEnemies,
                DayQuantityMultiplier = spawnBudget.DayQuantityMultiplier,
                DayBaseSpawnInterval = spawnBudget.DayBaseSpawnInterval,
                PhaseIntensityMultiplier = spawnBudget.PhaseIntensityMultiplier,
                EffectiveSpawnInterval = spawnBudget.EffectiveSpawnInterval,
                Wood = Resources.Wood,
                Stone = Resources.Stone,
                Iron = Resources.Iron,
                Food = Resources.Food,
                WoodAccumulator = accumulator.Wood,
                StoneAccumulator = accumulator.Stone,
                IronAccumulator = accumulator.Iron,
                FoodAccumulator = accumulator.Food,
                ArrowCurrent = ArrowSupply.Current,
                ArrowAccumulator = ArrowSupply.Accumulator,
                ArrowCapacityLevel = ArrowSupply.CapacityLevel,
                ArrowEfficiencyLevel = ArrowSupply.EfficiencyLevel,
                GraveEssence = GraveEssenceAmount,
                HasHeartGraph = heartGraphSnapshot != null,
                HeartGraph = heartGraphSnapshot,
                PopulationTotal = Population.Total,
                PopulationCapacity = Population.Capacity,
                PopulationBaseCapacity = Population.BaseCapacity,
                BedBaseCapacity = bedCapacity.BaseCapacity,
                PurchasedBedCapacity = bedCapacity.PurchasedCapacity,
                WoodWorkers = allocation.WoodWorkers,
                StoneWorkers = allocation.StoneWorkers,
                IronWorkers = allocation.IronWorkers,
                FoodWorkers = allocation.FoodWorkers,
                WoodWorkerTargetRatioBps = allocation.WoodTargetRatioBps,
                StoneWorkerTargetRatioBps = allocation.StoneTargetRatioBps,
                IronWorkerTargetRatioBps = allocation.IronTargetRatioBps,
                FoodWorkerTargetRatioBps = allocation.FoodTargetRatioBps,
                WoodWorkerCapacity = allocation.WoodWorkerCapacity,
                StoneWorkerCapacity = allocation.StoneWorkerCapacity,
                IronWorkerCapacity = allocation.IronWorkerCapacity,
                FoodWorkerCapacity = allocation.FoodWorkerCapacity,
                WorkerIdlePopulation = allocation.IdlePopulation,
                LastObservedPopulation = allocation.LastObservedPopulation,
                LastPopulationGrowthWave = allocation.LastPopulationGrowthWave,
                LastPopulationGrowthCycle = allocation.LastPopulationGrowthCycle,
                LastEventPrepWave = allocation.LastEventPrepWave,
                WoodBuildingCapacityLevel = workerBuildings.WoodCapacityLevel,
                WoodBuildingEfficiencyLevel = workerBuildings.WoodEfficiencyLevel,
                StoneBuildingCapacityLevel = workerBuildings.StoneCapacityLevel,
                StoneBuildingEfficiencyLevel = workerBuildings.StoneEfficiencyLevel,
                IronBuildingCapacityLevel = workerBuildings.IronCapacityLevel,
                IronBuildingEfficiencyLevel = workerBuildings.IronEfficiencyLevel,
                FoodBuildingCapacityLevel = workerBuildings.FoodCapacityLevel,
                FoodBuildingEfficiencyLevel = workerBuildings.FoodEfficiencyLevel,
                WallCurrentHP = Wall.CurrentHP,
                ArcherFormationVersion = _archerFormationVersion,
                BasicArchers = BasicArcherCount,
                RapidArchers = RapidArcherCount,
                FrostArchers = FrostArcherCount,
                GlobalArrowDamageBonus = _globalArrowDamageBonus,
                GlobalFireRateMultiplier = _globalFireRateMultiplier,
                LastRegularCouncilDay = _lastRegularCouncilDay,
                CouncilRunSalt = _councilRunSalt,
                CouncilWoodCapBonus = _councilWoodCapBonus,
                CouncilStoneCapBonus = _councilStoneCapBonus,
                CouncilIronCapBonus = _councilIronCapBonus,
                CouncilFoodCapBonus = _councilFoodCapBonus,
                HasActiveCouncilEvent = _activeCouncilEvent != null,
                ActiveCouncilEvent = _activeCouncilEvent,
                FireballCooldownRemaining = _fireballCooldownRemaining,
                FortifyActive = prep.FortifyActive,
                FortifyDamageMultiplier = prep.FortifyDamageMultiplier,
                RallyTimer = prep.RallyTimer,
                RallyDuration = prep.RallyDuration,
                RallyFireRateMultiplier = prep.RallyFireRateMultiplier,
                RallyCooldownRemaining = _rallyCooldownRemaining,
                EmergencyRepairCooldownRemaining = _emergencyRepairCooldownRemaining,
                PendingEconomyEvent = (int)economyEvent.PendingEvent,
                EconomyEventWave = economyEvent.EventWave,
                EconomyEventCooldownWaves = economyEvent.CooldownWavesRemaining,
                ProductionBonusResource = (int)economyEvent.ProductionBonusResource,
                ProductionBonusMultiplier = economyEvent.ProductionBonusMultiplier,
                ProductionBonusExpiresAfterWave = economyEvent.ProductionBonusExpiresAfterWave,
                EconomyRandomSeed = economyEvent.RandomSeed,
                NextNightSpawnMultiplier = economyEvent.NextNightSpawnMultiplier,
                NightSpawnExpiresAfterWave = economyEvent.NightSpawnExpiresAfterWave,
                EconomyFocus = (int)EconomyFocus
            };

            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
                save.CastleUpgradeLevel = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity).Level;

            foreach (var pair in _techNodeLevels)
                save.TechNodeLevels.Add(new TechLevelEntry { Id = pair.Key, Level = pair.Value });
            foreach (var pair in _archerTypeLevels)
                save.ArcherTypeLevels.Add(new ArcherLevelEntry { Type = (int)pair.Key, Level = pair.Value });
            foreach (var pair in _upgradeTiers)
                save.UpgradeTiers.Add(new UpgradeTierEntry { Type = (int)pair.Key, Tier = pair.Value });
            foreach (var pair in _councilFlags)
                save.CouncilFlags.Add(new CouncilFlagEntry { Flag = pair.Key, Day = pair.Value });
            save.RecentCouncilTemplates.AddRange(_recentCouncilTemplates);
            save.UsedOneShotCouncils.AddRange(_usedOneShotCouncils);

            CaptureCombatSnapshot(save);
            return RunPersistence.Save(save);
        }

        private void OnApplicationQuit()
        {
            if (_initialized)
                SaveRunSnapshot();
        }

        private void CaptureCombatSnapshot(RunSaveState save)
        {
            var zombieToIndex = new Dictionary<Entity, int>();
            var zombieQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZombieTag>(),
                    ComponentType.ReadOnly<ZombieStats>(),
                    ComponentType.ReadOnly<ZombieState>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            using (var zombies = zombieQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < zombies.Length; i++)
                {
                    Entity entity = zombies[i];
                    var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                    var stats = _entityManager.GetComponentData<ZombieStats>(entity);
                    var state = _entityManager.GetComponentData<ZombieState>(entity);
                    var item = new ZombieRunSaveState
                    {
                        X = transform.Position.x,
                        Y = transform.Position.y,
                        Z = transform.Position.z,
                        Scale = transform.Scale,
                        MoveSpeed = stats.MoveSpeed,
                        MaxHP = stats.MaxHP,
                        CurrentHP = stats.CurrentHP,
                        AttackDamage = stats.AttackDamage,
                        AttackCooldown = stats.AttackCooldown,
                        AttackTimer = stats.AttackTimer,
                        XPReward = stats.XPReward,
                        State = (int)state.Value
                    };

                    if (_entityManager.HasComponent<ZombieSlow>(entity))
                    {
                        var slow = _entityManager.GetComponentData<ZombieSlow>(entity);
                        item.SlowEnabled = _entityManager.IsComponentEnabled<ZombieSlow>(entity);
                        item.SlowDuration = slow.Duration;
                        item.SlowMultiplier = slow.SpeedMultiplier;
                    }

                    if (_entityManager.HasComponent<PhysicsBody>(entity))
                    {
                        var body = _entityManager.GetComponentData<PhysicsBody>(entity);
                        item.VelocityX = body.Velocity.x;
                        item.VelocityY = body.Velocity.y;
                        item.ForceX = body.Force.x;
                        item.ForceY = body.Force.y;
                    }

                    if (_entityManager.HasComponent<DeathTimer>(entity)
                        && _entityManager.IsComponentEnabled<DeathTimer>(entity))
                    {
                        item.HasDeathTimer = true;
                        item.DeathTimer = _entityManager.GetComponentData<DeathTimer>(entity).Value;
                    }

                    zombieToIndex[entity] = save.ActiveZombies.Count;
                    save.ActiveZombies.Add(item);
                }
            }

            var arrowQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ArrowTag>(),
                    ComponentType.ReadOnly<ArrowProjectile>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            using (var arrows = arrowQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < arrows.Length; i++)
                {
                    Entity entity = arrows[i];
                    var transform = _entityManager.GetComponentData<LocalTransform>(entity);
                    var projectile = _entityManager.GetComponentData<ArrowProjectile>(entity);
                    save.ActiveArrows.Add(new ArrowRunSaveState
                    {
                        X = transform.Position.x,
                        Y = transform.Position.y,
                        Z = transform.Position.z,
                        Scale = transform.Scale,
                        Speed = projectile.Speed,
                        Damage = projectile.Damage,
                        TargetZombieIndex = zombieToIndex.TryGetValue(projectile.Target, out int index) ? index : -1,
                        ArcherType = (int)projectile.ArcherType,
                        SlowDuration = projectile.SlowDuration,
                        SlowMultiplier = projectile.SlowMultiplier,
                        RemainingLifetime = projectile.RemainingLifetime
                    });
                }
            }

            if (ActiveFireballProjectile != Entity.Null
                && _entityManager.Exists(ActiveFireballProjectile)
                && _entityManager.HasComponent<FireballProjectile>(ActiveFireballProjectile)
                && _entityManager.HasComponent<LocalTransform>(ActiveFireballProjectile))
            {
                var transform = _entityManager.GetComponentData<LocalTransform>(ActiveFireballProjectile);
                var projectile = _entityManager.GetComponentData<FireballProjectile>(ActiveFireballProjectile);
                save.ActiveFireball = new FireballRunSaveState
                {
                    Active = true,
                    X = transform.Position.x,
                    Y = transform.Position.y,
                    Z = transform.Position.z,
                    Scale = transform.Scale,
                    TargetX = projectile.Target.x,
                    TargetY = projectile.Target.y,
                    Speed = projectile.Speed,
                    Radius = projectile.Radius,
                    Damage = projectile.Damage
                };
            }
        }

        public bool TryRestoreRunFromCheckpoint()
        {
            var save = RunPersistence.TryLoad();
            if (save == null || !_initialized || !TryGetMobileConfigEntity(out var mobileConfigEntity))
                return false;
            GeneratedRunGraph savedHeartGraph = save.HasHeartGraph ? save.HeartGraph : null;
            if (!TryValidateSavedHeartGraphForRestore(savedHeartGraph, out string heartPreflightError))
            {
                Debug.LogError("[GameManager] Castle Heart Continue preflight reddedildi: "
                               + heartPreflightError);
                return false;
            }
            if (save.HasActiveCouncilEvent
                && !CouncilContentPolicy.TryValidateComposedEvent(
                    councilCatalog, save.ActiveCouncilEvent, out string councilPreflightError))
            {
                Debug.LogError("[GameManager] Council Continue content preflight reddedildi: "
                               + councilPreflightError);
                return false;
            }

            RestartGame();
            _currentRunId = save.RunId;

            RestoreWorkerBuildingUpgradeState(mobileConfigEntity, save);

            foreach (var entry in save.TechNodeLevels)
                RestoreSavedTechNodeLevels(entry.Id, entry.Level);

            _councilFlags.Clear();
            foreach (var flag in save.CouncilFlags)
                _councilFlags[flag.Flag] = flag.Day;
            _recentCouncilTemplates.Clear();
            _recentCouncilTemplates.AddRange(save.RecentCouncilTemplates);
            _usedOneShotCouncils.Clear();
            foreach (var id in save.UsedOneShotCouncils)
                _usedOneShotCouncils.Add(id);
            _lastRegularCouncilDay = save.LastRegularCouncilDay;
            _councilRunSalt = save.CouncilRunSalt;
            _councilWoodCapBonus = save.CouncilWoodCapBonus;
            _councilStoneCapBonus = save.CouncilStoneCapBonus;
            _councilIronCapBonus = save.CouncilIronCapBonus;
            _councilFoodCapBonus = save.CouncilFoodCapBonus;
            _activeCouncilEvent = save.HasActiveCouncilEvent ? save.ActiveCouncilEvent : null;
            ApplyTechEconomyAggregates();

            _upgradeTiers.Clear();
            foreach (var tier in save.UpgradeTiers)
                _upgradeTiers[(UpgradeType)tier.Type] = tier.Tier;
            _globalArrowDamageBonus = save.GlobalArrowDamageBonus;
            _globalFireRateMultiplier = save.GlobalFireRateMultiplier;
            _archerTypeLevels.Clear();
            foreach (var entry in save.ArcherTypeLevels)
                _archerTypeLevels[(ArcherType)entry.Type] = entry.Level;

            _archerFormationVersion = ArcherFormationUtility.NormalizeVersion(
                save.ArcherFormationVersion);
            RepositionExistingMobileArchersToOutside();

            RestoreArcherCountsWithinCapacity(
                save.BasicArchers, save.RapidArchers, save.FrostArchers);
            ApplyScaledStatsToArchers(ArcherType.Basic, false);
            ApplyScaledStatsToArchers(ArcherType.Rapid, false);
            ApplyScaledStatsToArchers(ArcherType.Frost, false);

            _entityManager.SetComponentData(_gameStateEntity, new ResourceData
            {
                Wood = save.Wood,
                Stone = save.Stone,
                Iron = save.Iron,
                Food = save.Food
            });
            _entityManager.SetComponentData(_gameStateEntity, new ResourceAccumulator
            {
                Wood = save.WoodAccumulator,
                Stone = save.StoneAccumulator,
                Iron = save.IronAccumulator,
                Food = save.FoodAccumulator
            });
            _entityManager.SetComponentData(_gameStateEntity, new ArrowSupply
            {
                Current = math.max(0, save.ArrowCurrent),
                CapacityLevel = math.max(0, save.ArrowCapacityLevel),
                EfficiencyLevel = math.max(0, save.ArrowEfficiencyLevel),
                Accumulator = save.ArrowAccumulator
            });
            _entityManager.SetComponentData(_gameStateEntity, new GraveEssence
            {
                Current = save.GraveEssence < 0 ? 0 : save.GraveEssence
            });
            HeartEssence = _entityManager.GetComponentData<GraveEssence>(_gameStateEntity);
            if (!TryRestoreHeartRuntime(savedHeartGraph, out string heartRestoreError))
            {
                Debug.LogError("[GameManager] Castle Heart Continue replay reddedildi: "
                               + heartRestoreError);
                return false;
            }
            ClampRestoredArrowSupplyToEffectiveCapacity();

            if (_entityManager.HasComponent<EconomyFocusState>(mobileConfigEntity))
            {
                _entityManager.SetComponentData(mobileConfigEntity,
                    new EconomyFocusState { Type = (EconomyFocusType)save.EconomyFocus });
                EconomyFocus = (EconomyFocusType)save.EconomyFocus;
            }

            var allocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);
            ApplySavedWorkerAllocation(ref allocation, save);
            allocation.LastPopulationGrowthWave = save.LastPopulationGrowthWave;
            allocation.LastPopulationGrowthCycle = save.LastPopulationGrowthCycle;
            allocation.LastEventPrepWave = save.LastEventPrepWave;
            _entityManager.SetComponentData(mobileConfigEntity, allocation);
            PopulationAllocation = allocation;

            int totalArchers = save.BasicArchers + save.RapidArchers + save.FrostArchers;
            int totalWorkers = save.WoodWorkers + save.StoneWorkers + save.IronWorkers + save.FoodWorkers;
            RestoreBedCapacityState(mobileConfigEntity, save);
            _entityManager.SetComponentData(_gameStateEntity, new PopulationState
            {
                Total = save.PopulationTotal,
                Workers = totalWorkers,
                Archers = totalArchers,
                Idle = Mathf.Max(0, save.PopulationTotal - totalWorkers - totalArchers),
                Capacity = save.PopulationCapacity,
                BaseCapacity = save.PopulationBaseCapacity,
                FoodPerAssignedPerMin = Population.FoodPerAssignedPerMin
            });

            var prep = _entityManager.GetComponentData<CastleYardPrepState>(mobileConfigEntity);
            prep.FortifyActive = save.FortifyActive;
            prep.FortifyDamageMultiplier = save.FortifyDamageMultiplier;
            prep.RallyTimer = save.RallyTimer;
            prep.RallyDuration = save.RallyDuration;
            prep.RallyFireRateMultiplier = save.RallyFireRateMultiplier;
            _entityManager.SetComponentData(mobileConfigEntity, prep);

            var economyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
            economyEvent.PendingEvent = (MobileEconomyEventType)save.PendingEconomyEvent;
            economyEvent.EventWave = save.EconomyEventWave;
            economyEvent.CooldownWavesRemaining = save.EconomyEventCooldownWaves;
            economyEvent.ProductionBonusResource = (EconomyFocusType)save.ProductionBonusResource;
            economyEvent.ProductionBonusMultiplier = save.ProductionBonusMultiplier;
            economyEvent.ProductionBonusExpiresAfterWave = save.ProductionBonusExpiresAfterWave;
            economyEvent.RandomSeed = save.EconomyRandomSeed;
            economyEvent.NextNightSpawnMultiplier = save.NextNightSpawnMultiplier;
            economyEvent.NightSpawnExpiresAfterWave = save.NightSpawnExpiresAfterWave;
            _entityManager.SetComponentData(mobileConfigEntity, economyEvent);

            var cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
            cycle.CycleIndex = save.CycleIndex;
            cycle.CycleTimer = Mathf.Clamp(save.CycleTimer, 0f, Mathf.Max(0f, cycle.CycleDuration - 0.0001f));
            cycle.CycleProgress01 = Mathf.Clamp01(save.CycleProgress01);
            cycle.PhaseProgress01 = Mathf.Clamp01(save.PhaseProgress01);
            cycle.SpawnIntensityMultiplier = Mathf.Max(0.01f, save.SpawnIntensityMultiplier);
            cycle.HordePressure01 = Mathf.Clamp01(save.HordePressure01);
            cycle.Phase = (SiegeCyclePhase)save.CyclePhase;
            cycle.IsBloodMoonNight = false;
            _entityManager.SetComponentData(mobileConfigEntity, cycle);
            ContinuousSiegeCycle = cycle;
            if (_entityManager.HasComponent<ContinuousSpawnBudgetData>(mobileConfigEntity))
            {
                var spawnBudget = new ContinuousSpawnBudgetData
                {
                    PendingEnemies = save.SpawnBacklog > 0L ? save.SpawnBacklog : 0L,
                    TotalDemandedEnemies = save.TotalDemandedEnemies > 0L ? save.TotalDemandedEnemies : 0L,
                    TotalSpawnedEnemies = save.TotalBudgetSpawnedEnemies > 0L ? save.TotalBudgetSpawnedEnemies : 0L,
                    DemandPerInterval = Mathf.Max(0, save.DemandPerInterval),
                    LastDemandedEnemies = Mathf.Max(0, save.LastDemandedEnemies),
                    LastSpawnedEnemies = Mathf.Max(0, save.LastBudgetSpawnedEnemies),
                    DayQuantityMultiplier = Mathf.Max(0.01f, save.DayQuantityMultiplier),
                    DayBaseSpawnInterval = Mathf.Max(0.001f, save.DayBaseSpawnInterval),
                    PhaseIntensityMultiplier = Mathf.Max(0.01f, save.PhaseIntensityMultiplier),
                    EffectiveSpawnInterval = Mathf.Max(0.001f, save.EffectiveSpawnInterval)
                };
                _entityManager.SetComponentData(mobileConfigEntity, spawnBudget);
                ContinuousSpawnBudget = spawnBudget;
            }

            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            wave.CurrentWave = save.CurrentWave;
            wave.ZombiesToSpawn = save.ZombiesToSpawn;
            wave.ZombiesSpawned = save.ZombiesSpawned;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = save.SpawnTimer;
            wave.SpawnInterval = save.SpawnInterval;
            wave.ZombieHP = save.ZombieHP;
            wave.ZombieDamage = save.ZombieDamage;
            wave.ZombieSpeed = save.ZombieSpeed;
            wave.WaveActive = save.WaveActive;
            wave.Phase = (RunPhaseType)save.WavePhase;
            wave.PrepTimer = save.PrepTimer;
            wave.PrepDuration = save.PrepDuration;
            wave.WaveStartDelay = save.WaveStartDelay;
            wave.WaveStartTimer = save.WaveStartTimer;
            wave.SpawnRandomState = save.SpawnRandomState != 0u ? save.SpawnRandomState : 42u;
            _entityManager.SetComponentData(_gameStateEntity, wave);

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            gameState.XP = save.XP;
            gameState.Level = save.Level;
            gameState.XPToNextLevel = save.XPToNextLevel;
            gameState.TotalKills = save.TotalKills;
            gameState.IsGameOver = false;
            _entityManager.SetComponentData(_gameStateEntity, gameState);

            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);
                upgrade.Level = save.CastleUpgradeLevel;
                _entityManager.SetComponentData(_castleEntity, upgrade);
            }

            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            wall.CurrentHP = Mathf.Clamp(save.WallCurrentHP, 0f, wall.MaxHP);
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
            _fireballCooldownRemaining = Mathf.Max(0f, save.FireballCooldownRemaining);
            _rallyCooldownRemaining = Mathf.Max(0f, save.RallyCooldownRemaining);
            _emergencyRepairCooldownRemaining = Mathf.Max(
                0f,
                save.EmergencyRepairCooldownRemaining);

            RestoreCombatSnapshot(save);
            ReadECSData();
            OnGameStateChanged?.Invoke();
            return true;
        }

        private void RestoreCombatSnapshot(RunSaveState save)
        {
            var savedZombies = save.ActiveZombies ?? new List<ZombieRunSaveState>();
            var savedArrows = save.ActiveArrows ?? new List<ArrowRunSaveState>();
            var zombieEntities = new List<Entity>(savedZombies.Count);
            if (_enemyPoolEntity != Entity.Null && _entityManager.Exists(_enemyPoolEntity))
                EnemyPoolRuntimeUtility.ReturnAllActive(_entityManager, _enemyPoolEntity);
            if (_arrowPoolEntity != Entity.Null && _entityManager.Exists(_arrowPoolEntity))
                ArrowPoolRuntimeUtility.ReturnAllActive(_entityManager, _arrowPoolEntity);

            foreach (var item in savedZombies)
            {
                Entity entity;
                if (_enemyPoolEntity == Entity.Null
                    || !_entityManager.Exists(_enemyPoolEntity)
                    || !EnemyPoolRuntimeUtility.TryRent(_entityManager, _enemyPoolEntity, out entity))
                    entity = _entityManager.Instantiate(_zombiePrefabEntity);

                _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    new float3(item.X, item.Y, item.Z), quaternion.identity, Mathf.Max(0.01f, item.Scale)));
                _entityManager.SetComponentData(entity, new ZombieStats
                {
                    MoveSpeed = item.MoveSpeed,
                    MaxHP = item.MaxHP,
                    CurrentHP = item.CurrentHP,
                    AttackDamage = item.AttackDamage,
                    AttackCooldown = item.AttackCooldown,
                    AttackTimer = item.AttackTimer,
                    XPReward = item.XPReward
                });
                _entityManager.SetComponentData(entity, new ZombieState { Value = (ZombieStateType)item.State });

                if (_entityManager.HasComponent<ZombieSlow>(entity))
                {
                    _entityManager.SetComponentData(entity, new ZombieSlow
                    {
                        Duration = item.SlowDuration,
                        SpeedMultiplier = item.SlowMultiplier
                    });
                    _entityManager.SetComponentEnabled<ZombieSlow>(entity, item.SlowEnabled);
                }

                if (_entityManager.HasComponent<PhysicsBody>(entity))
                {
                    var body = _entityManager.GetComponentData<PhysicsBody>(entity);
                    body.Velocity = new float2(item.VelocityX, item.VelocityY);
                    body.Force = new float2(item.ForceX, item.ForceY);
                    _entityManager.SetComponentData(entity, body);
                }

                if (item.HasDeathTimer)
                {
                    var deathTimer = new DeathTimer { Value = item.DeathTimer };
                    if (_entityManager.HasComponent<DeathTimer>(entity))
                        _entityManager.SetComponentData(entity, deathTimer);
                    else
                        _entityManager.AddComponentData(entity, deathTimer);
                    _entityManager.SetComponentEnabled<DeathTimer>(entity, true);
                }
                else if (_entityManager.HasComponent<DeathTimer>(entity))
                {
                    _entityManager.SetComponentData(entity, new DeathTimer { Value = 0f });
                    _entityManager.SetComponentEnabled<DeathTimer>(entity, false);
                }

                zombieEntities.Add(entity);
            }

            foreach (var item in savedArrows)
            {
                if (item.TargetZombieIndex < 0 || item.TargetZombieIndex >= zombieEntities.Count)
                    continue;

                Entity entity;
                if (_arrowPoolEntity == Entity.Null
                    || !_entityManager.Exists(_arrowPoolEntity)
                    || !ArrowPoolRuntimeUtility.TryRent(
                        _entityManager, _arrowPoolEntity, _arrowPrefabEntity, out entity))
                    entity = _entityManager.Instantiate(_arrowPrefabEntity);
                _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    new float3(item.X, item.Y, item.Z), quaternion.identity, Mathf.Max(0.01f, item.Scale)));
                _entityManager.SetComponentData(entity, new ArrowProjectile
                {
                    Speed = item.Speed,
                    Damage = item.Damage,
                    Target = zombieEntities[item.TargetZombieIndex],
                    TargetPoolGeneration = EnemyPoolRuntimeUtility.GetGeneration(
                        _entityManager, zombieEntities[item.TargetZombieIndex]),
                    ArcherType = (ArcherType)item.ArcherType,
                    SlowDuration = item.SlowDuration,
                    SlowMultiplier = item.SlowMultiplier,
                    RemainingLifetime = item.RemainingLifetime > 0f
                        ? item.RemainingLifetime
                        : ArrowProjectile.DefaultLifetimeSeconds
                });
                SetSpriteTint(entity, ArcherVisualStyle.GetTint((ArcherType)item.ArcherType));
            }

            ActiveFireballProjectile = Entity.Null;
            if (save.ActiveFireball != null && save.ActiveFireball.Active)
            {
                var item = save.ActiveFireball;
                Entity entity = _entityManager.CreateEntity(typeof(FireballProjectile), typeof(LocalTransform));
                _entityManager.SetComponentData(entity, new FireballProjectile
                {
                    Target = new float2(item.TargetX, item.TargetY),
                    Speed = item.Speed,
                    Radius = item.Radius,
                    Damage = item.Damage
                });
                _entityManager.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    new float3(item.X, item.Y, item.Z), quaternion.identity, Mathf.Max(0.01f, item.Scale)));
                ActiveFireballProjectile = entity;
            }

            var wave = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            wave.ZombiesAlive = zombieEntities.Count;
            _entityManager.SetComponentData(_gameStateEntity, wave);
        }

        // ---------------------------------------------------------------------------------
        // Buyuculuk / Ates Topu (M-C): oyuncunun aktif savas gucu. Unlock + gelisim tech
        // agacindan (arcane_tower dali); cast Mono'dan ECS'e FireballStrike entity'siyle gecer,
        // hasari FireballStrikeSystem uygular. UI: SpellCastUI (polling).
        // ---------------------------------------------------------------------------------

        public bool FireballUnlocked => _fireballUnlocked;
        public float FireballDamage => GetHeartAdjustedSpellValue(
            HeartNodeEffectType.ModifySpellDamagePercent,
            FireballBaseDamage * _spellDamageMultiplier);
        public float FireballRadius => GetHeartAdjustedSpellValue(
            HeartNodeEffectType.AddSpellRadius,
            FireballBaseRadius + _spellRadiusBonus);
        public float FireballCooldownDuration => GetHeartAdjustedSpellValue(
            HeartNodeEffectType.ReduceSpellCooldownPercent,
            FireballBaseCooldown * _spellCooldownMultiplier);
        public float FireballCooldownRemaining => _fireballCooldownRemaining;
        public bool FireballReady => _fireballUnlocked && _fireballCooldownRemaining <= 0f;

        private const float FireballProjectileSpeed = 18f;
        // Meteor dususu (owner istegi): mermi hedefin USTUNDEN, hafif capraz iner
        private const float FireballDropHeight = 13f;
        private const float FireballDropSideOffset = 4f;

        /// <summary>Ucustaki mermi (SpellCastUI gorseli bunu takip eder; Null/olu = mermi yok).</summary>
        public Entity ActiveFireballProjectile { get; private set; } = Entity.Null;

        /// <summary>
        /// Ates Topu'nu dunya konumuna atar. Basarida cooldown baslar ve GOKTEN hedefe
        /// dusen bir meteor-mermi dogar (owner istegi: yukaridan dusus); hasar varista
        /// FireballStrikeSystem'de. Gorseli SpellCastUI cizer (ActiveFireballProjectile takip).
        /// </summary>
        public bool TryCastFireball(Vector2 worldPosition)
        {
            if (!_initialized
                || !FireballReady
                || Time.timeScale <= 0f
                || GameState.IsGameOver
                || GameState.IsLevelUpPending)
                return false;

            _fireballCooldownRemaining = FireballCooldownDuration;

            // Baslangic: hedefin ustunde, ekran disindan hafif capraz (meteor hissi)
            float3 start = new float3(
                worldPosition.x + FireballDropSideOffset,
                worldPosition.y + FireballDropHeight,
                MobileCastleRenderDepth.ProjectileZ);

            var projectile = _entityManager.CreateEntity(typeof(FireballProjectile), typeof(LocalTransform));
            _entityManager.SetComponentData(projectile, new FireballProjectile
            {
                Target = new float2(worldPosition.x, worldPosition.y),
                Speed = FireballProjectileSpeed,
                Radius = FireballRadius,
                Damage = FireballDamage
            });
            _entityManager.SetComponentData(projectile, LocalTransform.FromPosition(start));
            ActiveFireballProjectile = projectile;

            OnGameStateChanged?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------------------------
        // Meta Progression (roguelite, K2 karari — otoriter dok: META_PROGRESSION_ARCHITECTURE.md)
        // Kalici katman MetaProgression static sinifinda (JSON); burada kosu-basi uygulama +
        // kosu-sonu kazanim yasar. Para birimi: RUH (1 kill = 1 Ruh + yeni rekorda gun x 50).
        // ---------------------------------------------------------------------------------

        /// <summary>Kosu basinda kalici meta seviyelerini oyuna uygular (idempotent — kosu basina bir kez).</summary>
        private void ApplyMetaProgressionAtRunStart()
        {
            if (_metaAppliedThisRun || metaUpgradeCatalog == null || metaUpgradeCatalog.Upgrades == null)
                return;

            _metaAppliedThisRun = true;
            _metaWallHpPercent = 0f;
            _metaDamageMultiplier = 1f;
            _metaProductionPercent = 0f;
            bool archerCountsDirty = false;

            foreach (var upgrade in metaUpgradeCatalog.Upgrades)
            {
                if (upgrade == null)
                    continue;

                if (!MetaUpgradePolicy.IsRunGraphIsolatedEffect(upgrade.EffectType))
                {
                    Debug.LogError($"[GameManager] Meta upgrade '{upgrade.Id}' effect '{upgrade.EffectType}' "
                                   + "run graph isolation politikasina aykiri; kosuya uygulanmadi.");
                    continue;
                }

                int level = MetaProgression.GetUpgradeLevel(upgrade.Id);
                if (level <= 0)
                    continue;

                float total = upgrade.ValuePerLevel * level;
                switch (upgrade.EffectType)
                {
                    case MetaUpgradeEffectType.StartingResource:
                        AddResources(BuildMetaStartingResourceCost(upgrade.Resource, Mathf.RoundToInt(total)));
                        break;
                    case MetaUpgradeEffectType.StartingArchers:
                        for (int i = 0; i < Mathf.RoundToInt(total); i++)
                        {
                            if (!SpawnArcher(ArcherType.Basic))
                                break; // ortak 1000 cap; baslangic garnizonu population tuketmez

                            archerCountsDirty = true;
                        }
                        break;
                    case MetaUpgradeEffectType.WallHpPercent:
                        _metaWallHpPercent += total;
                        break;
                    case MetaUpgradeEffectType.ArcherDamagePercent:
                        _metaDamageMultiplier *= 1f + total;
                        break;
                    case MetaUpgradeEffectType.ProductionPercent:
                        _metaProductionPercent += total;
                        break;
                }
            }

            // Yuzdesel katkilar aggregate katmanlarindan akar (tech/council ile ayni kanal)
            if (_metaWallHpPercent > 0f)
                ApplyTechDefenseAggregates();
            if (_metaProductionPercent > 0f)
                ApplyTechEconomyAggregates();
            if (archerCountsDirty)
                ReadArcherTypeCounts();
            if (!Mathf.Approximately(_metaDamageMultiplier, 1f))
                ApplyScaledStatsToArchers(ArcherType.Basic, false);
        }

        private static ResourceCost BuildMetaStartingResourceCost(EconomyFocusType resource, int amount)
        {
            if (resource == EconomyFocusType.Balanced)
            {
                int each = Mathf.Max(1, amount / 4);
                return new ResourceCost(each, each, each, each);
            }

            return BuildSingleResourceCost(resource, amount);
        }

        /// <summary>Exact Continue icin kayitli legacy tech seviyesini maliyetsiz yeniden kurar.</summary>
        private void RestoreSavedTechNodeLevels(string nodeId, int targetLevel)
        {
            if (techTreeCatalog == null
                || string.IsNullOrEmpty(nodeId)
                || MoatDormancyRules.IsDormantTechNodeId(nodeId))
                return;

            var node = techTreeCatalog.GetNode(nodeId);
            if (node == null)
                return;

            EnsureTechTreeInitialized();
            _revealedTechNodes.Add(node.Id);

            int current = GetTechNodeLevel(node.Id);
            int goal = Mathf.Min(targetLevel, node.MaxLevel);
            for (int level = current; level < goal; level++)
            {
                _techNodeLevels[node.Id] = level + 1;
                if (level + 1 == 1)
                    RevealTechChildren(node);
                ApplyTechNodeEffects(node);
            }
        }

        private void EnsureCurrentRunId()
        {
            if (string.IsNullOrEmpty(_currentRunId))
                _currentRunId = System.Guid.NewGuid().ToString("N");
        }

        private void ProcessRunDeath()
        {
            if (_metaRunCollected)
                return;

            EnsureCurrentRunId();
            var receipt = new RunDeathReceipt
            {
                RunId = _currentRunId,
                Day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1),
                Kills = GameState.TotalKills
            };

            // Journal diske durable yazilmadan run snapshot silinmez ve meta uygulanmaz.
            if (!RunPersistence.CommitDeath(receipt))
                return;

            // Meta Save basarisizsa receipt korunur; bir sonraki acilis ayni RunId'yi
            // idempotent tamamlar. UI mevcut sonucu yine gosterebilir.
            RunPersistence.TryFinalizePendingDeathReward(out MetaRunResult result);
            LastRunResult = result;
            _metaRunCollected = true;
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
            if (!CanAccessEntityManager()
                || _archerPrefabEntity == Entity.Null
                || !_entityManager.Exists(_archerPrefabEntity))
            {
                return false;
            }

            int archerCount = GetArcherCount();
            if (!ArcherCapacityUtility.CanAdd(archerCount))
                return false;

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
            SetSpriteTint(entity, GetArcherTint(type));
            return true;
        }

        private bool TryFindArcherEntity(ArcherType type, out Entity entity)
        {
            entity = Entity.Null;
            if (!CanAccessEntityManager())
                return false;

            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            var entities = query.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (_entityManager.GetComponentData<ArcherUnit>(candidate).Type != type)
                        continue;

                    entity = candidate;
                    return true;
                }

                return false;
            }
            finally
            {
                entities.Dispose();
                query.Dispose();
            }
        }

        private void ApplyArcherTypeToEntity(Entity entity, ArcherType targetType)
        {
            ArcherUnit archer = _entityManager.GetComponentData<ArcherUnit>(entity);
            ArcherStats stats = GetScaledArcherStats(targetType);
            archer.FireRate = stats.FireRate;
            archer.ArrowDamage = stats.Damage;
            archer.Range = stats.Range;
            archer.Type = targetType;
            archer.SlowDuration = stats.SlowDuration;
            archer.SlowMultiplier = stats.SlowMultiplier;
            _entityManager.SetComponentData(entity, archer);
            SetSpriteTint(entity, GetArcherTint(targetType));
        }

        private void RestoreArcherCountsWithinCapacity(
            int basicTarget, int rapidTarget, int frostTarget)
        {
            ReadArcherTypeCounts();
            RestoreArcherTypeWithinCapacity(
                ArcherType.Basic, BasicArcherCount, basicTarget);
            RestoreArcherTypeWithinCapacity(
                ArcherType.Rapid, RapidArcherCount, rapidTarget);
            RestoreArcherTypeWithinCapacity(
                ArcherType.Frost, FrostArcherCount, frostTarget);
            ReadArcherTypeCounts();
        }

        private void RestoreArcherTypeWithinCapacity(
            ArcherType type, int currentTypeCount, int targetTypeCount)
        {
            int safeTarget = math.min(
                math.max(0, targetTypeCount), ArcherCapacityUtility.MaxTotalArchers);
            int missingCount = math.max(0, safeTarget - math.max(0, currentTypeCount));
            for (int i = 0; i < missingCount; i++)
            {
                if (!SpawnArcher(type))
                    break;
            }
        }

        private bool HasPopulationForNewArcher(int populationCost = 1)
        {
            if (freeEconomyTestMode)
                return true;

            return populationCost <= 0
                || !IsMobilePopulationEconomyEnabled()
                || GetIdlePopulation() >= populationCost;
        }

        private void ConsumePopulationForNewArcher(int populationCost = 1)
        {
            if (freeEconomyTestMode)
                return;

            if (populationCost <= 0)
                return;

            if (!IsMobilePopulationEconomyEnabled() || !CanAccessEntityManager() || !_entityManager.Exists(_gameStateEntity))
                return;

            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            population.Archers = Mathf.Min(population.Total, population.Archers + populationCost);
            population.Idle = Mathf.Max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
        }

        private ArcherStats GetScaledArcherStats(ArcherType type)
        {
            ArcherStats stats = GetHeartFreeScaledArcherStats(type);
            ApplyHeartArcherEffects(type, ref stats);
            return stats;
        }

        private ArcherStats GetHeartFreeScaledArcherStats(ArcherType type)
        {
            var stats = GetBaseArcherStats(type);
            int extraLevels = GetArcherTypeLevel(type) - 1;
            float damageScale = math.pow(TypeDamageMultiplierPerLevel, extraLevels);
            float fireRateScale = math.pow(TypeFireRateMultiplierPerLevel, extraLevels) * _globalFireRateMultiplier;

            // Tech tree carpanlari son degere uygulanir (flat bonus dahil) — bkz. TECH_TREE_SO_ARCHITECTURE.md
            // Meta-progression kalici hasar carpani ayni kanaldan biner
            stats.Damage = (stats.Damage * damageScale + _globalArrowDamageBonus) * _techDamageMultiplier * _metaDamageMultiplier;
            stats.FireRate *= fireRateScale * _techFireRateMultiplier;

            if (type == ArcherType.Frost)
            {
                stats.SlowDuration += FrostSlowDurationPerLevel * extraLevels;
                stats.SlowMultiplier = math.max(FrostMinSlowMultiplier,
                    stats.SlowMultiplier - FrostSlowMultiplierStep * extraLevels);
            }

            return stats;
        }

        private ArcherStats GetBaseArcherStats(ArcherType type)
        {
            var definition = GetArcherDefinition(type);
            if (definition != null)
                return definition.ToArcherStats();

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

        private float4 GetArcherTint(ArcherType type)
        {
            var definition = GetArcherDefinition(type);
            if (definition == null)
                return ArcherVisualStyle.GetTint(type);

            var color = definition.Tint;
            return new float4(color.r, color.g, color.b, color.a);
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
            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit) },
                None = new ComponentType[] { typeof(Prefab) }
            });
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

        private bool RepairWallToFull()
        {
            if (!CanAccessEntityManager()
                || !_entityManager.Exists(_castleEntity)
                || !_entityManager.Exists(_gameStateEntity))
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            if (gameState.IsGameOver || SingleWallDefenseRules.IsDestroyed(wall.CurrentHP))
                return false;

            float repairedHp = SingleWallDefenseRules.RepairToFull(wall.CurrentHP, wall.MaxHP);
            if (repairedHp <= wall.CurrentHP + 0.001f)
                return false;

            wall.CurrentHP = repairedHp;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
            return true;
        }

        private bool RepairWallByMaxPercent(float percent)
        {
            if (!CanAccessEntityManager()
                || !_entityManager.Exists(_castleEntity)
                || !_entityManager.Exists(_gameStateEntity)
                || percent <= 0f)
                return false;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            if (gameState.IsGameOver || SingleWallDefenseRules.IsDestroyed(wall.CurrentHP))
                return false;

            float repairedHp = SingleWallDefenseRules.HealByMaxPercent(
                wall.CurrentHP,
                wall.MaxHP,
                percent);
            if (repairedHp <= wall.CurrentHP + 0.001f)
                return false;

            wall.CurrentHP = repairedHp;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;
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
            return _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit) },
                None = new ComponentType[] { typeof(Prefab) }
            }).CalculateEntityCount();
        }

        private void ReadArcherTypeCounts()
        {
            BasicArcherCount = 0;
            RapidArcherCount = 0;
            FrostArcherCount = 0;

            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            using NativeArray<ArcherUnit> archers = query
                .ToComponentDataArray<ArcherUnit>(Allocator.Temp);
            for (int i = 0; i < archers.Length; i++)
            {
                ArcherUnit archer = archers[i];
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

            if (_mobileConfigEntity != Entity.Null
                && _entityManager.Exists(_mobileConfigEntity)
                && _entityManager.HasComponent<MobileCastleCombatConfig>(_mobileConfigEntity))
            {
                configEntity = _mobileConfigEntity;
                return true;
            }

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

            _mobileConfigEntity = query.GetSingletonEntity();
            configEntity = _mobileConfigEntity;
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
            EnsureInitialMobileArcherCount();
            ApplyMetaProgressionAtRunStart();

            var mobileConfig = _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity);
            if (mobileConfig.ContinuousSiegeEnabled)
            {
                wave.CurrentWave = math.max(1, wave.CurrentWave);
                MobileWaveUtility.ConfigureMobileWave(ref wave, mobileConfig);
                wave.WaveActive = true;
                wave.Phase = RunPhaseType.NightCombat;
                wave.PrepTimer = 0f;
                wave.PrepDuration = 0f;
                wave.WaveStartTimer = 0f;
                _entityManager.SetComponentData(_gameStateEntity, wave);
                return;
            }

            if (!wave.WaveActive && wave.Phase == RunPhaseType.DayPrep && wave.CurrentWave == 0)
                return;

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
            ContinuousSiegeCycle = default;
            ContinuousSpawnBudget = default;
            PopulationAllocation = default;
            BedCapacity = default;
            WorkerBuildingUpgrades = default;
            PrepPause = default;
            EconomyEvent = default;

            if (!TryGetMobileConfigEntity(out var mobileConfigEntity))
                return;

            if (_entityManager.HasComponent<WaveClearRewardData>(mobileConfigEntity))
                WaveClearReward = _entityManager.GetComponentData<WaveClearRewardData>(mobileConfigEntity);

            if (_entityManager.HasComponent<CastleYardPrepState>(mobileConfigEntity))
                CastleYardPrep = _entityManager.GetComponentData<CastleYardPrepState>(mobileConfigEntity);

            if (_entityManager.HasComponent<ContinuousSiegeCycleData>(mobileConfigEntity))
                ContinuousSiegeCycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);

            if (_entityManager.HasComponent<ContinuousSpawnBudgetData>(mobileConfigEntity))
                ContinuousSpawnBudget = _entityManager.GetComponentData<ContinuousSpawnBudgetData>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
                PopulationAllocation = _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                BedCapacity = _entityManager.GetComponentData<MobileBedCapacityState>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(mobileConfigEntity))
            {
                WorkerBuildingUpgrades =
                    _entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(mobileConfigEntity);
            }

            if (_entityManager.HasComponent<MobilePrepPauseState>(mobileConfigEntity))
                PrepPause = _entityManager.GetComponentData<MobilePrepPauseState>(mobileConfigEntity);

            if (_entityManager.HasComponent<MobileEconomyEventState>(mobileConfigEntity))
                EconomyEvent = _entityManager.GetComponentData<MobileEconomyEventState>(mobileConfigEntity);
        }

        private bool TryGetMobileArcherSpawnPosition(int archerCount, out float3 position)
        {
            position = default;

            if (!TryGetMobileConfigEntity(out var configEntity))
                return false;

            var config = _entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);

            MobileCastleArcherTilePlacement placement = MobileCastleArcherTilePlacement.GetOrCreateRuntime();
            if (placement != null
                && placement.TryGetSpawnPosition(
                    archerCount, _archerFormationVersion, out position))
            {
                // Tek cephe (K4): yalniz duvar hatti bolgesindeki tilemap hucreleri gecerli —
                // eski 360-duzen hucreleri (kale cevresi) elenir; owner kule tile'larini
                // duvara boyadiginda otomatik gecerli olur
                if (!config.SingleFrontEnabled || position.x <= config.FrontlineX + 1f)
                {
                    _missingArcherPlacementWarningLogged = false;
                    return true;
                }
            }

            // Tek cephe placeholder fallback: duvar hattinda dikey kolon (ortadan disa dogru)
            if (config.SingleFrontEnabled)
            {
                float spacing = 1.3f;
                int step = (archerCount + 1) / 2;
                float y = (archerCount % 2 == 0 ? 1f : -1f) * step * spacing;
                y = Mathf.Clamp(y, -config.SpawnBandYHalf, config.SpawnBandYHalf);
                position = new float3(config.FrontlineX - 0.8f, y, MobileCastleRenderDepth.UnitZ);
                _missingArcherPlacementWarningLogged = false;
                return true;
            }

            LogMissingArcherPlacementWarning();
            return false;
        }

        private void RepositionExistingMobileArchersToOutside()
        {
            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit), typeof(Unity.Transforms.LocalTransform) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                // Merkezi yerlesim yolu: tilemap hucresi + tek-cephe filtresi + kolon fallback
                // (seed okcunun eski 360-hucresine tasinmasini da ayni filtre engeller)
                if (!TryGetMobileArcherSpawnPosition(i, out float3 position))
                    return;

                var transform = _entityManager.GetComponentData<Unity.Transforms.LocalTransform>(entities[i]);
                transform.Position = position;
                _entityManager.SetComponentData(entities[i], transform);
            }
        }

        private void EnsureInitialMobileArcherCount()
        {
            int currentCount = GetArcherCount();
            for (int i = currentCount; i < MobileInitialBasicArchers; i++)
            {
                if (!SpawnArcher(ArcherType.Basic))
                    break;
            }

            int actualCount = GetArcherCount();
            if (!_entityManager.Exists(_gameStateEntity) || !_entityManager.HasComponent<PopulationState>(_gameStateEntity))
                return;

            MobilePopulationAllocation allocation = TryGetMobileConfigEntity(out var mobileConfigEntity)
                && _entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity)
                    ? _entityManager.GetComponentData<MobilePopulationAllocation>(mobileConfigEntity)
                    : PopulationAllocation;
            var population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            int workerCount = allocation.WoodWorkers
                + allocation.StoneWorkers
                + allocation.IronWorkers
                + allocation.FoodWorkers;
            population.Workers = workerCount;
            population.Archers = math.min(actualCount, math.max(0, population.Total - workerCount));
            population.Idle = math.max(0, population.Total - population.Workers - population.Archers);
            _entityManager.SetComponentData(_gameStateEntity, population);
            Population = population;
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

            int4 actualCounts = GetActualWorkerCounts(PopulationAllocation);
            int4 representativeCounts = WorkerVisualRepresentationUtility.GetRepresentativeCounts(
                PopulationAllocation);
            bool representativeCountsChanged = !_workerVisualSyncInitialized
                || !math.all(_lastSyncedWorkerVisualCounts == representativeCounts);
            bool actualCountsChanged = !_workerVisualSyncInitialized
                || !math.all(_lastSyncedWorkerVisualActualCounts == actualCounts);
            if (!representativeCountsChanged && !actualCountsChanged)
                return;

            if (representativeCountsChanged)
            {
                SyncWorkerVisualsToAllocation(representativeCounts, actualCounts);
                return;
            }

            SyncWorkerVisualRepresentationWeights(representativeCounts, actualCounts);
            _lastSyncedWorkerVisualActualCounts = actualCounts;
        }

        private void SyncSurvivorArrivalVisualsIfNeeded()
        {
            if (!TryGetMobileCombatConfig(out var config)
                || !TryGetMobileConfigEntity(out var mobileConfigEntity)
                || !_entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
            {
                return;
            }

            MobilePopulationAllocation allocation = PopulationAllocation;
            int marker = config.ContinuousSiegeEnabled
                ? allocation.LastPopulationGrowthCycle
                : allocation.LastPopulationGrowthWave;
            if (marker <= 0 || marker <= _lastSurvivorArrivalVisualMarker)
                return;

            int acceptedSurvivors = Mathf.Max(0, allocation.LastArrivalAcceptedCount);
            if (acceptedSurvivors <= 0)
            {
                _lastSurvivorArrivalVisualMarker = marker;
                return;
            }

            if (SpawnSurvivorArrivalVisuals(acceptedSurvivors, config))
                _lastSurvivorArrivalVisualMarker = marker;
        }

        private bool SpawnSurvivorArrivalVisuals(int acceptedSurvivors,
            MobileCastleCombatConfig config)
        {
            int visualCount = SurvivorArrivalVisualUtility.GetVisualCount(acceptedSurvivors);
            if (visualCount <= 0)
                return true;

            if (!TryResolveWorkerPrefabEntity())
            {
                LogMissingWorkerVisualWarning(
                    "WorkerPrefabData bulunamadi. Dawn survivor arrival visual spawn ertelendi.");
                return false;
            }

            for (int index = 0; index < visualCount; index++)
            {
                Entity entity = _entityManager.Instantiate(_workerPrefabEntity);
                if (_entityManager.HasComponent<ResourceWorkerVisual>(entity))
                    _entityManager.RemoveComponent<ResourceWorkerVisual>(entity);
                if (_entityManager.HasComponent<WorkerLogisticsRoute>(entity))
                    _entityManager.RemoveComponent<WorkerLogisticsRoute>(entity);
                if (_entityManager.HasComponent<WorkerLogisticsFeedbackState>(entity))
                    _entityManager.RemoveComponent<WorkerLogisticsFeedbackState>(entity);

                float3 spawnPosition = SurvivorArrivalVisualUtility.GetSpawnPosition(
                    config.FrontlineX, config.CastleCenter.y, index);
                float3 targetPosition = SurvivorArrivalVisualUtility.GetTargetPosition(
                    config.FrontlineX, config.CastleCenter.y, index);
                _entityManager.SetComponentData(entity,
                    LocalTransform.FromPositionRotationScale(
                        spawnPosition, quaternion.identity, 1f));
                _entityManager.AddComponentData(entity, new SurvivorArrivalVisual
                {
                    TargetPosition = targetPosition,
                    Speed = SurvivorArrivalVisualUtility.GetMoveSpeed(index),
                    StartDelay = SurvivorArrivalVisualUtility.GetStartDelay(index),
                    ArrivalDistance = SurvivorArrivalVisualUtility.DefaultArrivalDistance,
                    RepresentedSurvivorCount =
                        SurvivorArrivalVisualUtility.GetRepresentedSurvivorCount(
                            acceptedSurvivors, visualCount, index)
                });

                SetSpriteTint(entity, SurvivorArrivalVisualUtility.GetTint());
                ConfigureSurvivorArrivalSprite(entity, index);
            }

            return true;
        }

        private void ConfigureSurvivorArrivalSprite(Entity entity, int index)
        {
            if (_entityManager.HasComponent<WorkerAnimationMaterialProperty>(entity))
            {
                _entityManager.SetComponentData(entity, new WorkerAnimationMaterialProperty
                {
                    Value = (float)WorkerAnimationKind.Walk
                });
            }

            if (_entityManager.HasComponent<WorkerFeedbackMaterialProperty>(entity))
            {
                _entityManager.SetComponentData(entity, new WorkerFeedbackMaterialProperty
                {
                    Value = float4.zero
                });
            }

            if (!_entityManager.HasComponent<SpriteAnimation>(entity))
                return;

            SpriteAnimation animation = _entityManager.GetComponentData<SpriteAnimation>(entity);
            animation.DirectionRow = math.clamp(4, 0, math.max(0, animation.TotalRows - 1));
            animation.FrameCount = math.max(1, math.min(15, animation.TotalColumns));
            animation.CurrentFrame = index % animation.FrameCount;
            animation.FrameTimer = 0f;
            _entityManager.SetComponentData(entity, animation);
        }

        private void SyncWorkerVisualsToAllocation()
        {
            SyncWorkerVisualsToAllocation(
                WorkerVisualRepresentationUtility.GetRepresentativeCounts(PopulationAllocation),
                GetActualWorkerCounts(PopulationAllocation));
        }

        private void SyncWorkerVisualsToAllocation(int4 representativeCounts, int4 actualCounts)
        {
            if (!CanAccessEntityManager() || !IsMobilePopulationEconomyEnabled())
                return;

            int targetTotal = math.csum(representativeCounts);
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

            SyncResourceWorkerVisuals(EconomyFocusType.Wood, representativeCounts.x, actualCounts.x);
            SyncResourceWorkerVisuals(EconomyFocusType.Stone, representativeCounts.y, actualCounts.y);
            SyncResourceWorkerVisuals(EconomyFocusType.Iron, representativeCounts.z, actualCounts.z);
            SyncResourceWorkerVisuals(EconomyFocusType.Food, representativeCounts.w, actualCounts.w);

            _lastSyncedWorkerVisualCounts = representativeCounts;
            _lastSyncedWorkerVisualActualCounts = actualCounts;
            _workerVisualSyncInitialized = true;
        }

        private void SyncWorkerVisualRepresentationWeights(int4 representativeCounts,
            int4 actualCounts)
        {
            if (!CanAccessEntityManager())
                return;

            var query = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ResourceWorkerVisual) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                ResourceWorkerVisual visual =
                    _entityManager.GetComponentData<ResourceWorkerVisual>(entities[i]);
                int resourceIndex = GetWorkerResourceIndex(visual.Resource);
                int representedWorkerCount = WorkerVisualRepresentationUtility.GetRepresentedWorkerCount(
                    actualCounts[resourceIndex],
                    representativeCounts[resourceIndex],
                    visual.Index);
                if (visual.RepresentedWorkerCount == representedWorkerCount)
                    continue;

                visual.RepresentedWorkerCount = representedWorkerCount;
                _entityManager.SetComponentData(entities[i], visual);
            }
        }

        private static int4 GetActualWorkerCounts(MobilePopulationAllocation allocation)
        {
            return math.max(new int4(
                allocation.WoodWorkers,
                allocation.StoneWorkers,
                allocation.IronWorkers,
                allocation.FoodWorkers), new int4(0));
        }

        private void SyncResourceWorkerVisuals(EconomyFocusType resource, int targetCount,
            int actualCount)
        {
            targetCount = Mathf.Max(0, targetCount);
            actualCount = Mathf.Max(0, actualCount);
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
                visual.RepresentedWorkerCount = WorkerVisualRepresentationUtility.GetRepresentedWorkerCount(
                    actualCount, targetCount, kept);
                _entityManager.SetComponentData(entities[i], visual);
                UpdateWorkerVisualRoute(entities[i], resource, kept, false);
                ConfigureWorkerSprite(entities[i], resource, kept);
                kept++;
            }

            foreach (var entity in destroy)
                _entityManager.DestroyEntity(entity);

            for (int index = kept; index < targetCount; index++)
                SpawnWorkerVisual(resource, index, actualCount, targetCount);
        }

        private bool SpawnWorkerVisual(EconomyFocusType resource, int index, int actualCount,
            int visualCount)
        {
            if (!TryResolveWorkerPrefabEntity())
            {
                LogMissingWorkerVisualWarning("WorkerPrefabData bulunamadi. Mobile Castle Scene Setup ile VillagerWorker prefab referansini kur.");
                return false;
            }

            if (!TryGetWorkerRoutePositions(
                    resource,
                    index,
                    out float3 pickup,
                    out float3 siteApproach,
                    out float3 hubApproach,
                    out float3 delivery))
                return false;

            var entity = _entityManager.Instantiate(_workerPrefabEntity);
            _entityManager.SetComponentData(entity, new ResourceWorkerVisual
            {
                Resource = resource,
                Index = index,
                RepresentedWorkerCount = WorkerVisualRepresentationUtility.GetRepresentedWorkerCount(
                    actualCount, visualCount, index)
            });
            _entityManager.SetComponentData(entity, Unity.Transforms.LocalTransform.FromPositionRotationScale(
                pickup,
                quaternion.identity,
                1f));
            ConfigureWorkerLogisticsRoute(
                entity,
                index,
                pickup,
                siteApproach,
                hubApproach,
                delivery,
                true);
            ConfigureWorkerSprite(entity, resource, index);
            return true;
        }

        private void UpdateWorkerVisualRoute(Entity entity, EconomyFocusType resource, int index, bool resetPosition)
        {
            if (!TryGetWorkerRoutePositions(
                    resource,
                    index,
                    out float3 pickup,
                    out float3 siteApproach,
                    out float3 hubApproach,
                    out float3 delivery))
                return;

            if (resetPosition)
            {
                var transform = _entityManager.GetComponentData<Unity.Transforms.LocalTransform>(entity);
                transform.Position = pickup;
                _entityManager.SetComponentData(entity, transform);
            }

            ConfigureWorkerLogisticsRoute(
                entity,
                index,
                pickup,
                siteApproach,
                hubApproach,
                delivery,
                resetPosition);
        }

        private bool TryGetWorkerRoutePositions(
            EconomyFocusType resource,
            int index,
            out float3 pickup,
            out float3 siteApproach,
            out float3 hubApproach,
            out float3 delivery)
        {
            pickup = default;
            siteApproach = default;
            hubApproach = default;
            delivery = default;
            CastleInteriorWorkerPlacement placement = CastleInteriorWorkerPlacement.GetOrCreateRuntime();
            if (placement != null && placement.TryGetLogisticsRoutePositions(
                    resource,
                    index,
                    out pickup,
                    out siteApproach,
                    out hubApproach,
                    out delivery))
            {
                _missingWorkerPlacementWarningLogged = false;
                return true;
            }

            LogMissingWorkerVisualWarning("CastleInteriorEconomyArea worker pickup veya hub delivery point bulunamadi. Worker visual spawn atlandi.");
            return false;
        }

        private void ConfigureWorkerLogisticsRoute(
            Entity entity,
            int index,
            float3 pickup,
            float3 siteApproach,
            float3 hubApproach,
            float3 delivery,
            bool resetRoute)
        {
            float2 direction = math.normalizesafe((siteApproach - pickup).xy, new float2(1f, 0f));
            WorkerLogisticsRoute route = _entityManager.HasComponent<WorkerLogisticsRoute>(entity)
                ? _entityManager.GetComponentData<WorkerLogisticsRoute>(entity)
                : default;

            route.PickupPosition = pickup;
            route.SiteApproachPosition = siteApproach;
            route.HubApproachPosition = hubApproach;
            route.DeliveryPosition = delivery;
            route.Speed = 0.85f;
            route.WorkDuration = 0.65f;
            route.DeliveryDuration = 0.35f;
            route.LastDirection = math.lengthsq(route.LastDirection) > 0.0001f ? route.LastDirection : direction;

            if (resetRoute || !_entityManager.HasComponent<WorkerLogisticsRoute>(entity))
            {
                route.MovingToHub = 1;
                route.RouteLeg = 0;
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

            if (_entityManager.HasComponent<WorkerCargoColorMaterialProperty>(entity))
            {
                _entityManager.SetComponentData(entity, new WorkerCargoColorMaterialProperty
                {
                    Value = ResourceWorkerVisualStyle.GetCargoTint(resource)
                });
            }

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

        private static int GetWorkerResourceIndex(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Stone: return 1;
                case EconomyFocusType.Iron: return 2;
                case EconomyFocusType.Food: return 3;
                default: return 0;
            }
        }

        private static void ApplySavedWorkerAllocation(ref MobilePopulationAllocation allocation, RunSaveState save)
        {
            allocation.WoodWorkers = Mathf.Max(0, save.WoodWorkers);
            allocation.StoneWorkers = Mathf.Max(0, save.StoneWorkers);
            allocation.IronWorkers = Mathf.Max(0, save.IronWorkers);
            allocation.FoodWorkers = Mathf.Max(0, save.FoodWorkers);
            allocation.WoodTargetRatioBps = save.WoodWorkerTargetRatioBps;
            allocation.StoneTargetRatioBps = save.StoneWorkerTargetRatioBps;
            allocation.IronTargetRatioBps = save.IronWorkerTargetRatioBps;
            allocation.FoodTargetRatioBps = save.FoodWorkerTargetRatioBps;
            WorkerAllocationUtility.NormalizeTargetRatios(ref allocation);
            if (save.WoodWorkerCapacity > 0) allocation.WoodWorkerCapacity = save.WoodWorkerCapacity;
            if (save.StoneWorkerCapacity > 0) allocation.StoneWorkerCapacity = save.StoneWorkerCapacity;
            if (save.IronWorkerCapacity > 0) allocation.IronWorkerCapacity = save.IronWorkerCapacity;
            if (save.FoodWorkerCapacity > 0) allocation.FoodWorkerCapacity = save.FoodWorkerCapacity;
            allocation.IdlePopulation = Mathf.Max(0, save.WorkerIdlePopulation);
            allocation.LastObservedPopulation = Mathf.Max(0, save.LastObservedPopulation);
            allocation.AutoAllocationInitialized = 1;
        }

        private void RestoreBedCapacityState(Entity mobileConfigEntity, RunSaveState save)
        {
            if (!_entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
                return;

            int purchasedCapacity = Mathf.Max(0, save.PurchasedBedCapacity);
            int minimumPopulationSafeBase = Mathf.Max(0, save.PopulationTotal - purchasedCapacity);
            var state = new MobileBedCapacityState
            {
                BaseCapacity = Mathf.Max(Mathf.Max(0, save.BedBaseCapacity), minimumPopulationSafeBase),
                PurchasedCapacity = purchasedCapacity
            };
            _entityManager.SetComponentData(mobileConfigEntity, state);
            BedCapacity = state;
        }

        private void RestoreWorkerBuildingUpgradeState(Entity mobileConfigEntity, RunSaveState save)
        {
            if (!_entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(mobileConfigEntity))
                return;

            var state = new MobileWorkerBuildingUpgradeState
            {
                WoodCapacityLevel = Mathf.Max(0, save.WoodBuildingCapacityLevel),
                WoodEfficiencyLevel = Mathf.Max(0, save.WoodBuildingEfficiencyLevel),
                StoneCapacityLevel = Mathf.Max(0, save.StoneBuildingCapacityLevel),
                StoneEfficiencyLevel = Mathf.Max(0, save.StoneBuildingEfficiencyLevel),
                IronCapacityLevel = Mathf.Max(0, save.IronBuildingCapacityLevel),
                IronEfficiencyLevel = Mathf.Max(0, save.IronBuildingEfficiencyLevel),
                FoodCapacityLevel = Mathf.Max(0, save.FoodBuildingCapacityLevel),
                FoodEfficiencyLevel = Mathf.Max(0, save.FoodBuildingEfficiencyLevel)
            };
            _entityManager.SetComponentData(mobileConfigEntity, state);
            WorkerBuildingUpgrades = state;
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

            _currentRunId = System.Guid.NewGuid().ToString("N");
            _archerFormationVersion = ArcherFormationUtility.CurrentVersion;

            bool mobileMode = TryGetMobileConfigEntity(out var mobileConfigEntity);
            var mobileConfig = mobileMode
                ? _entityManager.GetComponentData<MobileCastleCombatConfig>(mobileConfigEntity)
                : default;
            bool continuousSiege = mobileMode && mobileConfig.ContinuousSiegeEnabled;
            int mobileInitialBedCapacity = mobileMode
                ? (mobileConfig.InitialBedCapacity > 0
                    ? mobileConfig.InitialBedCapacity
                    : MobileBedCapacityUtility.DefaultInitialCapacity)
                : 0;

            // Pool-owned zombileri rezerve geri dondur; legacy/non-pool entity kalirsa sil.
            if (_enemyPoolEntity != Entity.Null && _entityManager.Exists(_enemyPoolEntity))
                EnemyPoolRuntimeUtility.ReturnAllActive(_entityManager, _enemyPoolEntity);
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieTag));
            _entityManager.DestroyEntity(zombieQuery);

            // Pool-owned oklari rezerve dondur; yalniz legacy/non-pool oklar silinir.
            if (_arrowPoolEntity != Entity.Null && _entityManager.Exists(_arrowPoolEntity))
                ArrowPoolRuntimeUtility.ReturnAllActive(_entityManager, _arrowPoolEntity);
            var legacyArrowQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ArrowTag>() },
                None = new[]
                {
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<ArrowPoolMember>()
                }
            });
            _entityManager.DestroyEntity(legacyArrowQuery);

            var fireballQuery = _entityManager.CreateEntityQuery(typeof(FireballProjectile));
            _entityManager.DestroyEntity(fireballQuery);
            ActiveFireballProjectile = Entity.Null;

            if (mobileMode)
            {
                var archerQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(ArcherUnit) },
                    None = new ComponentType[] { typeof(Prefab) }
                });
                _entityManager.DestroyEntity(archerQuery);

                var workerQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(ResourceWorkerVisual) },
                    None = new ComponentType[] { typeof(Prefab) }
                });
                _entityManager.DestroyEntity(workerQuery);
                _workerVisualSyncInitialized = false;

                var survivorArrivalQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] { typeof(SurvivorArrivalVisual) },
                    None = new ComponentType[] { typeof(Prefab) }
                });
                _entityManager.DestroyEntity(survivorArrivalQuery);
                _lastSurvivorArrivalVisualMarker = 0;
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
            ResetTechTreeState();
            ResetCouncilState();
            if (mobileMode
                && _entityManager.HasComponent<MobileWorkerBuildingUpgradeState>(mobileConfigEntity))
            {
                _entityManager.SetComponentData(mobileConfigEntity,
                    new MobileWorkerBuildingUpgradeState());
                WorkerBuildingUpgrades = default;
            }
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
            _rallyCooldownRemaining = 0f;
            _emergencyRepairCooldownRemaining = 0f;
            if (mobileMode && _entityManager.HasComponent<MobilePopulationAllocation>(mobileConfigEntity))
            {
                var allocation = new MobilePopulationAllocation
                {
                    WoodWorkers = MobileInitialWoodWorkers,
                    StoneWorkers = MobileInitialStoneWorkers,
                    IronWorkers = MobileInitialIronWorkers,
                    FoodWorkers = MobileInitialFoodWorkers,
                    WoodWorkerCapacity = MobileFallbackWoodWorkerCap,
                    StoneWorkerCapacity = MobileFallbackStoneWorkerCap,
                    IronWorkerCapacity = MobileFallbackIronWorkerCap,
                    FoodWorkerCapacity = MobileFallbackFoodWorkerCap,
                    IdlePopulation = MobileInitialPopulation
                        - MobileInitialWoodWorkers - MobileInitialStoneWorkers
                        - MobileInitialIronWorkers - MobileInitialFoodWorkers
                        - MobileInitialBasicArchers,
                    LastObservedPopulation = MobileInitialPopulation,
                    AutoAllocationInitialized = 1,
                    LastPopulationGrowthWave = 0,
                    LastPopulationGrowthCycle = 0,
                    LastArrivalRequestedCount = 0,
                    LastArrivalAcceptedCount = 0,
                    LastArrivalFoodCost = 0,
                    LastEventPrepWave = 0
                };
                WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);
                _entityManager.SetComponentData(mobileConfigEntity, allocation);
                PopulationAllocation = allocation;
            }
            if (mobileMode && _entityManager.HasComponent<MobileBedCapacityState>(mobileConfigEntity))
            {
                var bedCapacity = MobileBedCapacityUtility.CreateInitial(
                    mobileConfig.InitialBedCapacity > 0
                        ? mobileConfig.InitialBedCapacity
                        : MobileBedCapacityUtility.DefaultInitialCapacity);
                _entityManager.SetComponentData(mobileConfigEntity, bedCapacity);
                BedCapacity = bedCapacity;
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
            if (mobileMode && _entityManager.HasComponent<ContinuousSiegeCycleData>(mobileConfigEntity))
            {
                var cycle = _entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileConfigEntity);
                cycle.Enabled = mobileConfig.ContinuousSiegeEnabled;
                cycle.CycleTimer = 0f;
                cycle.CycleDuration = Mathf.Max(1f, mobileConfig.SiegeCycleDuration);
                cycle.DayDuration = Mathf.Max(0.1f, mobileConfig.SiegeDayDuration);
                cycle.DuskDuration = Mathf.Max(0.1f, mobileConfig.SiegeDuskDuration);
                cycle.NightDuration = Mathf.Max(0.1f, mobileConfig.SiegeNightDuration);
                cycle.CycleProgress01 = 0f;
                cycle.PhaseProgress01 = 0f;
                cycle.SpawnIntensityMultiplier = Mathf.Max(0.01f, mobileConfig.SiegeDayIntensityMultiplier);
                cycle.HordePressure01 = 0f;
                cycle.CycleIndex = 0;
                cycle.Phase = SiegeCyclePhase.Day;
                _entityManager.SetComponentData(mobileConfigEntity, cycle);
                ContinuousSiegeCycle = cycle;
            }
            if (mobileMode && _entityManager.HasComponent<ContinuousSpawnBudgetData>(mobileConfigEntity))
            {
                var spawnBudget = new ContinuousSpawnBudgetData
                {
                    PendingEnemies = 0,
                    TotalDemandedEnemies = 0,
                    TotalSpawnedEnemies = 0,
                    DemandPerInterval = 0,
                    LastDemandedEnemies = 0,
                    LastSpawnedEnemies = 0,
                    DayQuantityMultiplier = 1f,
                    DayBaseSpawnInterval = Mathf.Max(mobileConfig.MinSpawnInterval, mobileConfig.BaseSpawnInterval),
                    PhaseIntensityMultiplier = Mathf.Max(0.01f, mobileConfig.SiegeDayIntensityMultiplier),
                    EffectiveSpawnInterval = Mathf.Max(mobileConfig.MinSpawnInterval,
                        mobileConfig.BaseSpawnInterval / Mathf.Max(0.01f, mobileConfig.SiegeDayIntensityMultiplier))
                };
                _entityManager.SetComponentData(mobileConfigEntity, spawnBudget);
                ContinuousSpawnBudget = spawnBudget;
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
                CurrentWave = continuousSiege ? 1 : mobileMode ? 0 : 1,
                ZombiesToSpawn = continuousSiege ? mobileConfig.BaseWaveEnemyCount : mobileMode ? 0 : 500,
                ZombiesSpawned = 0,
                ZombiesAlive = 0,
                SpawnTimer = 0f,
                SpawnInterval = mobileMode ? mobileConfig.BaseSpawnInterval : 0.05f,
                ZombieHP = 20f,
                ZombieDamage = 5f,
                ZombieSpeed = mobileMode ? mobileConfig.BaseZombieSpeed : 1.5f,
                WaveActive = continuousSiege || !mobileMode,
                Phase = mobileMode && !continuousSiege ? RunPhaseType.DayPrep : RunPhaseType.NightCombat,
                PrepTimer = mobileMode && !continuousSiege ? math.max(0f, mobileConfig.InitialDayPrepDuration) : 0f,
                PrepDuration = mobileMode && !continuousSiege ? math.max(0f, mobileConfig.InitialDayPrepDuration) : 0f,
                WaveStartDelay = mobileMode ? 0f : 3f,
                WaveStartTimer = mobileMode ? 0f : 3f,
                StressTestMode = false,
                SpawnRandomState = 42u
            });

            // Kaynak resetle (setup tool GameStateAuthoring defaultlariyla senkron: 160/80/50/120)
            _entityManager.SetComponentData(_gameStateEntity, new ResourceData
            {
                Wood = mobileMode ? 160 : 100,
                Stone = mobileMode ? 80 : 50,
                Iron = mobileMode ? 50 : 20,
                Food = mobileMode ? 120 : 100
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

            // Finite ok envanteri resetle; refill/capacity/efficiency run state'idir.
            var resetArrowSupply = new ArrowSupply
            {
                CapacityLevel = 0,
                EfficiencyLevel = 0,
                Accumulator = 0f
            };
            resetArrowSupply.Current = mobileMode
                ? ArrowEconomyUtility.GetCapacity(resetArrowSupply, GetEconomyPriceTuning())
                : 50;
            _entityManager.SetComponentData(_gameStateEntity, resetArrowSupply);

            // Grave Essence yalniz run state'idir; yeni kosu/restart her zaman sifirlar.
            HeartEssence = new GraveEssence { Current = 0 };
            _entityManager.SetComponentData(_gameStateEntity, HeartEssence);

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
                Capacity = mobileMode ? mobileInitialBedCapacity : 20,
                BaseCapacity = mobileMode ? mobileInitialBedCapacity : 20,
                FoodPerAssignedPerMin = mobileMode ? 0.25f : 2f
            });

            // Wall resetle
            var wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            wall.CurrentHP = wall.MaxHP;
            _entityManager.SetComponentData(_castleEntity, wall);
            Wall = wall;

            // Kale yukseltme resetle
            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);
                upgrade.Level = 0;
                _entityManager.SetComponentData(_castleEntity, upgrade);
            }

            if (mobileMode)
            {
                for (int i = 0; i < MobileInitialBasicArchers; i++)
                {
                    if (!SpawnArcher(ArcherType.Basic))
                        break;

                    ConsumePopulationForNewArcher();
                }
            }

            if (mobileMode)
                SyncWorkerVisualsToAllocation();

            _upgradeTiers.Clear();
            _currentUpgradeCards = null;

            // Yeni kosu: meta yeniden uygulanir (kalici seviyeler her kosuya tasinir),
            // kosu-sonu kazanim bayragi da temizlenir (siradaki olum yeniden Ruh toplar)
            _metaAppliedThisRun = false;
            _metaRunCollected = false;
            ApplyMetaProgressionAtRunStart();
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

    [System.Serializable]
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
