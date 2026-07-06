using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class MobileCastleCombatAuthoring : MonoBehaviour
    {
        public Vector2 CastleCenter = Vector2.zero;
        public float SpawnRadius = 11f;
        public float AttackRadius = 1.35f;
        public int BaseWaveEnemyCount = 30;
        public int ExtraEnemiesPerWave = 10;
        public int SpawnBatchSize = 2;

        [Header("Single Front (K4 pivotu — dusmanlar yalniz sagdan)")]
        [Tooltip("true = tek cephe: sag-serit spawn + sola akis + duvara vurma. false = eski 360-ring (geri alma anahtari).")]
        public bool SingleFrontEnabled = true;
        [Tooltip("Savunma hattinin (duvarin) x konumu; zombiler bu hatta yaslanip vurur.")]
        public float FrontlineX = -6f;
        [Tooltip("Spawn seridinin x tabani (uzerine 0-2 birim ileri jitter eklenir).")]
        public float SpawnLineX = 13f;
        [Tooltip("Spawn seridinin dikey yarim-genisligi (y = -bu..+bu).")]
        public float SpawnBandYHalf = 6.5f;

        [Header("Moat (hendek — upgrade ile evrilir)")]
        [Tooltip("Hendek bandinin x araligi (duvarin onunde).")]
        public float MoatXMin = -4f;
        public float MoatXMax = -1.5f;
        [Tooltip("Hendek icindeyken hiz carpani (1 = etkisiz, 0.55 = %45 yavaslatma). Tech dusurur.")]
        public float MoatSlowMultiplier = 0.55f;
        [Tooltip("Hendek gecis hasari/sn. 0 = kapali; moat_flame tech'i acar.")]
        public float MoatDamagePerSecond = 0f;

        [Header("Mass Escalation")]
        public float ZombieBaseHP = 20f;
        [Tooltip("Cycle basina lineer HP buyumesi (0.30 = her cycle +%30 taban HP). Eski ustel w^1.2 egrisinin yerini alir.")]
        public float ZombieHpGrowthPerCycle = 0.30f;
        public float ZombieBaseDamage = 5f;
        public float ZombieDamagePerCycle = 0.5f;
        [Tooltip("Batch cycle ile buyur: batch = SpawnBatchSize * intensity * (1 + (cycle-1)*bu). Kalabalik eskalasyonu.")]
        public float SpawnBatchGrowthPerCycle = 0.10f;
        public int MaxSpawnBatch = 12;
        [Tooltip("Continuous modda canli zombi guvenlik tavani (performans).")]
        public int MaxAliveZombies = 900;

        public float ZombieScale = 1.4f;
        public float BaseZombieSpeed = 0.85f;
        public float ZombieSpeedPerWave = 0.04f;
        public int StressSpawnBatchSize = 25;
        public float StressSpawnInterval = 0.1f;
        public int StressMaxAliveZombies = 1500;

        [Header("Rewards")]
        public float KillRewardWood = 1f;
        public float KillRewardFood = 0.6f;
        public float KillRewardStone = 0.25f;
        public float KillRewardIron = 0.15f;
        [Tooltip("0 = kill odulu cycle ile BUYUMEZ (gelir/zorluk ayrismasi). Ana gelir worker ekonomisidir.")]
        public float KillRewardWaveScale = 0f;
        public int WaveClearWoodBase = 20;
        public int WaveClearFoodBase = 15;
        public int WaveClearStoneBase = 10;
        public int WaveClearIronBase = 6;
        public int WaveClearWoodPerWave = 6;
        public int WaveClearFoodPerWave = 5;
        public int WaveClearStonePerWave = 4;
        public int WaveClearIronPerWave = 3;

        [Header("Economy Focus")]
        public float BalancedPassiveMultiplier = 1.20f;
        public float BalancedRewardMultiplier = 1.10f;
        public float FocusedPassiveMultiplier = 1.60f;
        public float FocusedPassiveFlatBonusPerMin = 60f;
        public float FocusedKillRewardMultiplier = 2.00f;
        public float FocusedWaveClearMultiplier = 1.75f;

        [Header("Day Night Prep")]
        public float InitialDayPrepDuration = 12f;
        public float DayPrepDuration = 15f;
        [Range(0f, 1f)] public float DayOverlayAlpha = 0f;
        [Range(0f, 1f)] public float NightOverlayAlpha = 0.50f;
        public bool UnlimitedArrows = true;

        [Header("Continuous Siege Cycle")]
        public bool ContinuousSiegeEnabled = true;
        public float SiegeCycleDuration = 60f;
        public float SiegeDayDuration = 22f;
        public float SiegeDuskDuration = 8f;
        public float SiegeNightDuration = 22f;
        [Tooltip("Gece sonrasi odul/nefes fazi (GDD 4-faz dongusu). 0 = dawn'siz legacy 3-faz.")]
        public float SiegeDawnDuration = 8f;
        public float SiegeDayIntensityMultiplier = 0.55f;
        public float SiegeDuskStartIntensityMultiplier = 1.00f;
        public float SiegeDuskEndIntensityMultiplier = 1.35f;
        public float SiegeNightIntensityMultiplier = 1.65f;
        public float SiegeDawnIntensityMultiplier = 0.15f;

        [Header("Defense Repair")]
        [Tooltip("Tam kayipta odenen tamir maliyeti; gercek maliyet kayip oraniyla olceklenir.")]
        public int RepairBaseWoodCost = 120;
        public int RepairBaseStoneCost = 80;

        [Header("Wave Director")]
        public float BaseSpawnInterval = 0.95f;
        public float SpawnIntervalWaveMultiplier = 0.96f;
        public float MinSpawnInterval = 0.35f;
        [Range(0f, 0.45f)] public float OpeningEnemyRatio = 0.20f;
        [Range(0f, 0.45f)] public float FinalEnemyRatio = 0.20f;
        public float OpeningIntervalMultiplier = 1.35f;
        public float FinalIntervalMultiplier = 0.65f;
        public int OpeningBatchDelta = -1;
        public int FinalBatchDelta = 1;

        [Header("Population Economy")]
        public int PopulationGrowthPerDayPrep = 15;
        public int InitialWoodWorkers = 20;
        public int InitialStoneWorkers = 10;
        public int InitialIronWorkers = 8;
        public int InitialFoodWorkers = 15;
        public int WoodWorkerCap = 40;
        public int StoneWorkerCap = 30;
        public int IronWorkerCap = 24;
        public int FoodWorkerCap = 40;
        public float WoodWorkerProductionPerMin = 8.0f;
        public float StoneWorkerProductionPerMin = 5.5f;
        public float IronWorkerProductionPerMin = 3.8f;
        public float FoodWorkerProductionPerMin = 7.0f;
        [Range(0f, 1f)] public float WorkerEconomyRewardMultiplier = 0.25f;

        [Header("Economy Events")]
        [Range(0f, 1f)] public float EconomyEventChance = 0.15f;
        public int EconomyEventCooldownWaves = 2;
        public uint EconomyEventSeed = 91273u;

        [Header("Castle Yard Prep")]
        [Range(0.05f, 1f)] public float FortifyDamageMultiplier = 0.70f;
        public float RallyDuration = 10f;
        public float RallyFireRateMultiplier = 1.25f;

        public Transform[] ArcherSlots;

        public class Baker : Baker<MobileCastleCombatAuthoring>
        {
            public override void Bake(MobileCastleCombatAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new MobileCastleCombatConfig
                {
                    CastleCenter = authoring.CastleCenter,
                    SpawnRadius = math.max(1f, authoring.SpawnRadius),
                    AttackRadius = math.max(0.1f, authoring.AttackRadius),
                    BaseWaveEnemyCount = math.max(1, authoring.BaseWaveEnemyCount),
                    ExtraEnemiesPerWave = math.max(0, authoring.ExtraEnemiesPerWave),
                    SpawnBatchSize = math.max(1, authoring.SpawnBatchSize),
                    ZombieBaseHP = math.max(1f, authoring.ZombieBaseHP),
                    ZombieHpGrowthPerCycle = math.max(0f, authoring.ZombieHpGrowthPerCycle),
                    ZombieBaseDamage = math.max(0.1f, authoring.ZombieBaseDamage),
                    ZombieDamagePerCycle = math.max(0f, authoring.ZombieDamagePerCycle),
                    SpawnBatchGrowthPerCycle = math.max(0f, authoring.SpawnBatchGrowthPerCycle),
                    // 0 = sinirsiz sentineli KORUNUR (runtime "cap yok" olarak yorumlar)
                    MaxSpawnBatch = math.max(0, authoring.MaxSpawnBatch),
                    MaxAliveZombies = math.max(0, authoring.MaxAliveZombies),
                    ZombieScale = math.max(0.1f, authoring.ZombieScale),
                    BaseZombieSpeed = math.max(0.05f, authoring.BaseZombieSpeed),
                    ZombieSpeedPerWave = math.max(0f, authoring.ZombieSpeedPerWave),
                    StressSpawnBatchSize = math.max(1, authoring.StressSpawnBatchSize),
                    StressSpawnInterval = math.max(0.01f, authoring.StressSpawnInterval),
                    StressMaxAliveZombies = math.max(1, authoring.StressMaxAliveZombies),
                    KillRewardWood = math.max(0f, authoring.KillRewardWood),
                    KillRewardStone = math.max(0f, authoring.KillRewardStone),
                    KillRewardIron = math.max(0f, authoring.KillRewardIron),
                    KillRewardFood = math.max(0f, authoring.KillRewardFood),
                    KillRewardWaveScale = math.max(0f, authoring.KillRewardWaveScale),
                    WaveClearWoodBase = math.max(0, authoring.WaveClearWoodBase),
                    WaveClearStoneBase = math.max(0, authoring.WaveClearStoneBase),
                    WaveClearIronBase = math.max(0, authoring.WaveClearIronBase),
                    WaveClearFoodBase = math.max(0, authoring.WaveClearFoodBase),
                    WaveClearWoodPerWave = math.max(0, authoring.WaveClearWoodPerWave),
                    WaveClearStonePerWave = math.max(0, authoring.WaveClearStonePerWave),
                    WaveClearIronPerWave = math.max(0, authoring.WaveClearIronPerWave),
                    WaveClearFoodPerWave = math.max(0, authoring.WaveClearFoodPerWave),
                    BalancedPassiveMultiplier = math.max(0f, authoring.BalancedPassiveMultiplier),
                    BalancedRewardMultiplier = math.max(0f, authoring.BalancedRewardMultiplier),
                    FocusedPassiveMultiplier = math.max(0f, authoring.FocusedPassiveMultiplier),
                    FocusedPassiveFlatBonusPerMin = math.max(0f, authoring.FocusedPassiveFlatBonusPerMin),
                    FocusedKillRewardMultiplier = math.max(0f, authoring.FocusedKillRewardMultiplier),
                    FocusedWaveClearMultiplier = math.max(0f, authoring.FocusedWaveClearMultiplier),
                    InitialDayPrepDuration = math.max(0f, authoring.InitialDayPrepDuration),
                    DayPrepDuration = math.max(1f, authoring.DayPrepDuration),
                    DayOverlayAlpha = math.clamp(authoring.DayOverlayAlpha, 0f, 1f),
                    NightOverlayAlpha = math.clamp(authoring.NightOverlayAlpha, 0f, 1f),
                    UnlimitedArrows = authoring.UnlimitedArrows,
                    ContinuousSiegeEnabled = authoring.ContinuousSiegeEnabled,
                    SiegeCycleDuration = math.max(1f, authoring.SiegeCycleDuration),
                    SiegeDayDuration = math.max(0.1f, authoring.SiegeDayDuration),
                    SiegeDuskDuration = math.max(0.1f, authoring.SiegeDuskDuration),
                    SiegeNightDuration = math.max(0.1f, authoring.SiegeNightDuration),
                    SiegeDawnDuration = math.max(0f, authoring.SiegeDawnDuration),
                    SiegeDayIntensityMultiplier = math.max(0.01f, authoring.SiegeDayIntensityMultiplier),
                    SiegeDuskStartIntensityMultiplier = math.max(0.01f, authoring.SiegeDuskStartIntensityMultiplier),
                    SiegeDuskEndIntensityMultiplier = math.max(0.01f, authoring.SiegeDuskEndIntensityMultiplier),
                    SiegeNightIntensityMultiplier = math.max(0.01f, authoring.SiegeNightIntensityMultiplier),
                    SiegeDawnIntensityMultiplier = math.max(0.01f, authoring.SiegeDawnIntensityMultiplier),
                    RepairBaseWoodCost = math.max(0, authoring.RepairBaseWoodCost),
                    RepairBaseStoneCost = math.max(0, authoring.RepairBaseStoneCost),
                    SingleFrontEnabled = authoring.SingleFrontEnabled,
                    FrontlineX = authoring.FrontlineX,
                    SpawnLineX = authoring.SpawnLineX,
                    SpawnBandYHalf = math.max(1f, authoring.SpawnBandYHalf),
                    MoatXMin = math.min(authoring.MoatXMin, authoring.MoatXMax),
                    MoatXMax = math.max(authoring.MoatXMin, authoring.MoatXMax),
                    MoatSlowMultiplier = math.clamp(authoring.MoatSlowMultiplier, 0.05f, 1f),
                    MoatDamagePerSecond = math.max(0f, authoring.MoatDamagePerSecond),
                    BaseSpawnInterval = math.max(0.01f, authoring.BaseSpawnInterval),
                    SpawnIntervalWaveMultiplier = math.clamp(authoring.SpawnIntervalWaveMultiplier, 0.01f, 1f),
                    MinSpawnInterval = math.max(0.01f, authoring.MinSpawnInterval),
                    OpeningEnemyRatio = math.clamp(authoring.OpeningEnemyRatio, 0f, 0.45f),
                    FinalEnemyRatio = math.clamp(authoring.FinalEnemyRatio, 0f, 0.45f),
                    OpeningIntervalMultiplier = math.max(0.01f, authoring.OpeningIntervalMultiplier),
                    FinalIntervalMultiplier = math.max(0.01f, authoring.FinalIntervalMultiplier),
                    OpeningBatchDelta = authoring.OpeningBatchDelta,
                    FinalBatchDelta = authoring.FinalBatchDelta,
                    PopulationGrowthPerDayPrep = math.max(0, authoring.PopulationGrowthPerDayPrep),
                    WoodWorkerCap = math.max(0, authoring.WoodWorkerCap),
                    StoneWorkerCap = math.max(0, authoring.StoneWorkerCap),
                    IronWorkerCap = math.max(0, authoring.IronWorkerCap),
                    FoodWorkerCap = math.max(0, authoring.FoodWorkerCap),
                    WoodWorkerProductionPerMin = math.max(0f, authoring.WoodWorkerProductionPerMin),
                    StoneWorkerProductionPerMin = math.max(0f, authoring.StoneWorkerProductionPerMin),
                    IronWorkerProductionPerMin = math.max(0f, authoring.IronWorkerProductionPerMin),
                    FoodWorkerProductionPerMin = math.max(0f, authoring.FoodWorkerProductionPerMin),
                    WorkerEconomyRewardMultiplier = math.clamp(authoring.WorkerEconomyRewardMultiplier, 0f, 1f),
                    EconomyEventChance = math.clamp(authoring.EconomyEventChance, 0f, 1f),
                    EconomyEventCooldownWaves = math.max(0, authoring.EconomyEventCooldownWaves)
                });

                AddComponent(entity, new EconomyFocusState
                {
                    Type = EconomyFocusType.Balanced
                });

                AddComponent(entity, new WaveClearRewardData
                {
                    Sequence = 0,
                    Wave = 0,
                    Wood = 0,
                    Stone = 0,
                    Iron = 0,
                    Food = 0
                });

                AddComponent(entity, new CastleYardPrepState
                {
                    FortifyActive = false,
                    FortifyDamageMultiplier = math.clamp(authoring.FortifyDamageMultiplier, 0.05f, 1f),
                    RallyTimer = 0f,
                    RallyDuration = math.max(0.1f, authoring.RallyDuration),
                    RallyFireRateMultiplier = math.max(1f, authoring.RallyFireRateMultiplier)
                });

                AddComponent(entity, new MobilePopulationAllocation
                {
                    WoodWorkers = math.max(0, authoring.InitialWoodWorkers),
                    StoneWorkers = math.max(0, authoring.InitialStoneWorkers),
                    IronWorkers = math.max(0, authoring.InitialIronWorkers),
                    FoodWorkers = math.max(0, authoring.InitialFoodWorkers),
                    LastPopulationGrowthWave = 0,
                    LastPopulationGrowthCycle = 0,
                    LastEventPrepWave = 0
                });

                AddComponent(entity, new MobilePrepPauseState
                {
                    IsPaused = false
                });

                AddComponent(entity, new MobileEconomyEventState
                {
                    PendingEvent = MobileEconomyEventType.None,
                    EventWave = 0,
                    CooldownWavesRemaining = 0,
                    ProductionBonusResource = EconomyFocusType.Balanced,
                    ProductionBonusMultiplier = 1f,
                    ProductionBonusExpiresAfterWave = 0,
                    RandomSeed = authoring.EconomyEventSeed == 0u ? 91273u : authoring.EconomyEventSeed,
                    NextNightSpawnMultiplier = 1f,
                    NightSpawnExpiresAfterWave = 0
                });

                AddComponent(entity, new ContinuousSiegeCycleData
                {
                    Enabled = authoring.ContinuousSiegeEnabled,
                    CycleTimer = 0f,
                    CycleDuration = math.max(1f, authoring.SiegeCycleDuration),
                    DayDuration = math.max(0.1f, authoring.SiegeDayDuration),
                    DuskDuration = math.max(0.1f, authoring.SiegeDuskDuration),
                    NightDuration = math.max(0.1f, authoring.SiegeNightDuration),
                    DawnDuration = math.max(0f, authoring.SiegeDawnDuration),
                    CycleProgress01 = 0f,
                    PhaseProgress01 = 0f,
                    SpawnIntensityMultiplier = math.max(0.01f, authoring.SiegeDayIntensityMultiplier),
                    HordePressure01 = 0f,
                    CycleIndex = 0,
                    Phase = SiegeCyclePhase.Day
                });

                var slots = AddBuffer<ArcherSlotPosition>(entity);
                if (authoring.ArcherSlots != null)
                {
                    for (int i = 0; i < authoring.ArcherSlots.Length; i++)
                    {
                        var slot = authoring.ArcherSlots[i];
                        if (slot == null)
                            continue;

                        Vector3 position = slot.position;
                        slots.Add(new ArcherSlotPosition
                        {
                            Value = new float3(position.x, position.y, position.z)
                        });
                    }
                }
            }
        }
    }
}
