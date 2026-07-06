using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct MobileCastleCombatConfig : IComponentData
    {
        public float2 CastleCenter;
        public float SpawnRadius;
        public float AttackRadius;
        public int BaseWaveEnemyCount;
        public int ExtraEnemiesPerWave;
        public int SpawnBatchSize;
        // Kutle-odakli eskalasyon: HP lineer buyur (sunger degil), batch cycle ile buyur (kalabalik artar)
        public float ZombieBaseHP;
        public float ZombieHpGrowthPerCycle;
        public float ZombieBaseDamage;
        public float ZombieDamagePerCycle;
        public float SpawnBatchGrowthPerCycle;
        public int MaxSpawnBatch;
        public int MaxAliveZombies;
        public float ZombieScale;
        public float BaseZombieSpeed;
        public float ZombieSpeedPerWave;
        public int StressSpawnBatchSize;
        public float StressSpawnInterval;
        public int StressMaxAliveZombies;
        public float KillRewardWood;
        public float KillRewardStone;
        public float KillRewardIron;
        public float KillRewardFood;
        public float KillRewardWaveScale;
        public int WaveClearWoodBase;
        public int WaveClearStoneBase;
        public int WaveClearIronBase;
        public int WaveClearFoodBase;
        public int WaveClearWoodPerWave;
        public int WaveClearStonePerWave;
        public int WaveClearIronPerWave;
        public int WaveClearFoodPerWave;
        public float BalancedPassiveMultiplier;
        public float BalancedRewardMultiplier;
        public float FocusedPassiveMultiplier;
        public float FocusedPassiveFlatBonusPerMin;
        public float FocusedKillRewardMultiplier;
        public float FocusedWaveClearMultiplier;
        public float InitialDayPrepDuration;
        public float DayPrepDuration;
        public float DayOverlayAlpha;
        public float NightOverlayAlpha;
        public bool UnlimitedArrows;
        public float BaseSpawnInterval;
        public float SpawnIntervalWaveMultiplier;
        public float MinSpawnInterval;
        public float OpeningEnemyRatio;
        public float FinalEnemyRatio;
        public float OpeningIntervalMultiplier;
        public float FinalIntervalMultiplier;
        public int OpeningBatchDelta;
        public int FinalBatchDelta;
        public int PopulationGrowthPerDayPrep;
        public int WoodWorkerCap;
        public int StoneWorkerCap;
        public int IronWorkerCap;
        public int FoodWorkerCap;
        public float WoodWorkerProductionPerMin;
        public float StoneWorkerProductionPerMin;
        public float IronWorkerProductionPerMin;
        public float FoodWorkerProductionPerMin;
        public float WorkerEconomyRewardMultiplier;
        public float EconomyEventChance;
        public int EconomyEventCooldownWaves;
        public bool ContinuousSiegeEnabled;
        public float SiegeCycleDuration;
        public float SiegeDayDuration;
        public float SiegeDuskDuration;
        public float SiegeNightDuration;
        public float SiegeDawnDuration;
        public float SiegeDayIntensityMultiplier;
        public float SiegeDuskStartIntensityMultiplier;
        public float SiegeDuskEndIntensityMultiplier;
        public float SiegeNightIntensityMultiplier;
        public float SiegeDawnIntensityMultiplier;
        // Repair maliyeti: tam kayipta odenen taban (kayip oranıyla olceklenir)
        public int RepairBaseWoodCost;
        public int RepairBaseStoneCost;
        // --- Tek Cephe (K4, M-0): dusmanlar yalniz SAGDAN gelir ---
        // true iken 360-ring spawn/hedefleme devre disi; false = eski davranis (geri alinabilir)
        public bool SingleFrontEnabled;
        // Savunma hattinin (duvarin) x konumu; zombiler bu hatta yaslanip vurur
        public float FrontlineX;
        // Spawn seridi: sag kenar x tabani (+ ileri jitter) ve dikey yarim-bant
        public float SpawnLineX;
        public float SpawnBandYHalf;
        // Hendek bandi (duvarin onunde): icindeyken yavaslatma, upgrade ile gecis hasari
        public float MoatXMin;
        public float MoatXMax;
        public float MoatSlowMultiplier;    // 1 = etkisiz; 0.55 = %45 yavaslatma
        public float MoatDamagePerSecond;   // 0 = kapali (tech ile acilir)
    }

    public enum SiegeCyclePhase : byte
    {
        Day,
        Dusk,
        Night,
        Dawn
    }

    public struct ContinuousSiegeCycleData : IComponentData
    {
        public bool Enabled;
        public float CycleTimer;
        public float CycleDuration;
        public float DayDuration;
        public float DuskDuration;
        public float NightDuration;
        public float DawnDuration;
        public float CycleProgress01;
        public float PhaseProgress01;
        public float SpawnIntensityMultiplier;
        public float HordePressure01;
        public int CycleIndex;
        public SiegeCyclePhase Phase;
    }

    public struct WaveClearRewardData : IComponentData
    {
        public int Sequence;
        public int Wave;
        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;
    }

    public struct CastleYardPrepState : IComponentData
    {
        public bool FortifyActive;
        public float FortifyDamageMultiplier;
        public float RallyTimer;
        public float RallyDuration;
        public float RallyFireRateMultiplier;
    }

    public enum EconomyFocusType : byte
    {
        Balanced,
        Wood,
        Stone,
        Iron,
        Food
    }

    public struct EconomyFocusState : IComponentData
    {
        public EconomyFocusType Type;
    }

    public struct MobilePopulationAllocation : IComponentData
    {
        public int WoodWorkers;
        public int StoneWorkers;
        public int IronWorkers;
        public int FoodWorkers;
        public int LastPopulationGrowthWave;
        public int LastPopulationGrowthCycle;
        public int LastEventPrepWave;
    }

    public struct MobilePrepPauseState : IComponentData
    {
        public bool IsPaused;
    }

    public enum MobileEconomyEventType : byte
    {
        None,
        ForestCache,
        QuarryCrew,
        RefugeeCart
    }

    public struct MobileEconomyEventState : IComponentData
    {
        public MobileEconomyEventType PendingEvent;
        public int EventWave;
        public int CooldownWavesRemaining;
        public EconomyFocusType ProductionBonusResource;
        public float ProductionBonusMultiplier;
        public int ProductionBonusExpiresAfterWave;
        public uint RandomSeed;
        // Council risk atomu: SONRAKI gece spawn yogunlugu carpani (1 = notr; 0 = ayarsiz/legacy).
        // WaveSpawnSystem yalniz Night fazinda uygular; expire continuous dalda islenir.
        public float NextNightSpawnMultiplier;
        public int NightSpawnExpiresAfterWave;
    }

    public struct ArcherSlotPosition : IBufferElementData
    {
        public float3 Value;
    }
}
