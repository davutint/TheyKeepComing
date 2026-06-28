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
        public int SpawnBatchSize = 3;
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
        public float KillRewardWaveScale = 0.05f;
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
        public float SiegeDayDuration = 25f;
        public float SiegeDuskDuration = 10f;
        public float SiegeNightDuration = 25f;
        public float SiegeDayIntensityMultiplier = 0.55f;
        public float SiegeDuskStartIntensityMultiplier = 1.00f;
        public float SiegeDuskEndIntensityMultiplier = 1.35f;
        public float SiegeNightIntensityMultiplier = 1.65f;

        [Header("Wave Director")]
        public float BaseSpawnInterval = 0.8f;
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
        public int InitialWoodWorkers = 6;
        public int InitialStoneWorkers = 3;
        public int InitialIronWorkers = 2;
        public int InitialFoodWorkers = 5;
        public float WoodWorkerProductionPerMin = 4.5f;
        public float StoneWorkerProductionPerMin = 3.0f;
        public float IronWorkerProductionPerMin = 2.0f;
        public float FoodWorkerProductionPerMin = 4.0f;
        [Range(0f, 1f)] public float WorkerEconomyRewardMultiplier = 0.45f;

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
                    SiegeDayIntensityMultiplier = math.max(0.01f, authoring.SiegeDayIntensityMultiplier),
                    SiegeDuskStartIntensityMultiplier = math.max(0.01f, authoring.SiegeDuskStartIntensityMultiplier),
                    SiegeDuskEndIntensityMultiplier = math.max(0.01f, authoring.SiegeDuskEndIntensityMultiplier),
                    SiegeNightIntensityMultiplier = math.max(0.01f, authoring.SiegeNightIntensityMultiplier),
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
                    RandomSeed = authoring.EconomyEventSeed == 0u ? 91273u : authoring.EconomyEventSeed
                });

                AddComponent(entity, new ContinuousSiegeCycleData
                {
                    Enabled = authoring.ContinuousSiegeEnabled,
                    CycleTimer = 0f,
                    CycleDuration = math.max(1f, authoring.SiegeCycleDuration),
                    DayDuration = math.max(0.1f, authoring.SiegeDayDuration),
                    DuskDuration = math.max(0.1f, authoring.SiegeDuskDuration),
                    NightDuration = math.max(0.1f, authoring.SiegeNightDuration),
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
