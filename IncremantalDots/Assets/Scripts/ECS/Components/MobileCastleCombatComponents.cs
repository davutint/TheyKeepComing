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
        public int FoodCostPerArrival;
        public int InitialBedCapacity;
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
        // V1 normal repair: Day/Dusk'ta MaxHP yuzdesi kadar paket heal; Stone maliyeti
        // gercek iyilestirilen HP * birim fiyat * gun carpani ile hesaplanir.
        public float NormalRepairHealPercent;
        public float RepairStonePerMissingHp;
        public float RepairDayPriceMultiplier;
        // V1 aktif ability tuning'i. Rally effect state'i CastleYardPrepState'te kalir;
        // cooldown ve Emergency Repair tuning'i bu profile-owned config'ten okunur.
        public float RallyCooldown;
        public float EmergencyRepairHealPercent;
        public float EmergencyRepairCooldown;
        // --- Tek Cephe (K4, M-0): dusmanlar yalniz SAGDAN gelir ---
        // true iken 360-ring spawn/hedefleme devre disi; false = eski davranis (geri alinabilir)
        public bool SingleFrontEnabled;
        // Savunma hattinin (duvarin) x konumu; zombiler bu hatta yaslanip vurur
        public float FrontlineX;
        // Spawn seridi: sag kenar x tabani (+ ileri jitter) ve dikey yarim-bant
        public float SpawnLineX;
        public float SpawnBandYHalf;
        // V1'de hendek gameplay'i dormant. Geometri/tuning alanlari content migration icin korunur.
        public bool MoatGameplayEnabled;
        public float MoatXMin;
        public float MoatXMax;
        public float MoatSlowMultiplier;
        public float MoatDamagePerSecond;
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
        // BU GUNUN gecesi kanli ay mi (tum fazlar boyunca true — gunduz uyarisi + gece etiketi ayni bayragi okur)
        public bool IsBloodMoonNight;
    }

    /// <summary>
    /// Continuous horde icin exact spawn talebi ve runtime telemetry state'i.
    /// Day tabani phase carpanindan ayridir; cap doluyken PendingEnemies silinmez.
    /// </summary>
    public struct ContinuousSpawnBudgetData : IComponentData
    {
        public long PendingEnemies;
        public long TotalDemandedEnemies;
        public long TotalSpawnedEnemies;
        public int DemandPerInterval;
        public int LastDemandedEnemies;
        public int LastSpawnedEnemies;
        public float DayQuantityMultiplier;
        public float DayBaseSpawnInterval;
        public float PhaseIntensityMultiplier;
        public float EffectiveSpawnInterval;
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
        public int WoodTargetRatioBps;
        public int StoneTargetRatioBps;
        public int IronTargetRatioBps;
        public int FoodTargetRatioBps;
        public int WoodWorkerCapacity;
        public int StoneWorkerCapacity;
        public int IronWorkerCapacity;
        public int FoodWorkerCapacity;
        public int IdlePopulation;
        public int LastObservedPopulation;
        public byte AutoAllocationInitialized;
        public int LastPopulationGrowthWave;
        public int LastPopulationGrowthCycle;
        public int LastArrivalRequestedCount;
        public int LastArrivalAcceptedCount;
        public int LastArrivalFoodCost;
        public int LastEventPrepWave;
    }

    /// <summary>
    /// Dört hazır worker binasının run içi ekonomik yatırım seviyeleri.
    /// Castle Heart etkileri bu state'i değiştirmez; config aggregate'inde seviyelerin üstüne eklenir.
    /// </summary>
    public struct MobileWorkerBuildingUpgradeState : IComponentData
    {
        public int WoodCapacityLevel;
        public int WoodEfficiencyLevel;
        public int StoneCapacityLevel;
        public int StoneEfficiencyLevel;
        public int IronCapacityLevel;
        public int IronEfficiencyLevel;
        public int FoodCapacityLevel;
        public int FoodEfficiencyLevel;
    }

    /// <summary>
    /// DifficultyProfileSO'dan bake edilen ekonomi fiyat parametreleri.
    /// Run state degildir; satin alinmis seviye/yatak state'i bu baseline'i kullanir.
    /// </summary>
    public struct MobileEconomyPriceTuning : IComponentData
    {
        public int BedBaseWoodCost;
        public int BedCostGrowthCapacityInterval;
        public int WorkerCapacityBaseWoodCost;
        public int WorkerCapacityBaseIronCost;
        public int WorkerEfficiencyBaseWoodCost;
        public int WorkerEfficiencyBaseIronCost;
        public double WorkerBuildingCostGrowthMultiplier;
        public int ArrowBaseCapacity;
        public int ArrowCapacityPerLevel;
        public int ArrowRefillPackageSize;
        public int ArrowBaseArrowsPerWood;
        public int ArrowArrowsPerWoodPerEfficiencyLevel;
        public int ArrowCapacityBaseWoodCost;
        public int ArrowCapacityBaseIronCost;
        public int ArrowEfficiencyBaseWoodCost;
        public int ArrowEfficiencyBaseIronCost;
        public double ArrowUpgradeCostGrowthMultiplier;
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

    /// <summary>
    /// DifficultyProfileSO gun-egrilerinin ECS'e ornekelnmis hali (AnimationCurve Burst'e
    /// giremez). Config entity'sinde buffer olarak yasar: index = gun-1 (gun 1-tabanli);
    /// gun buffer uzunlugunu asarsa SON eleman kullanilir. Buffer yok/bos = tum carpanlar 1
    /// (geriye uyumlu). Kaynak: baker (edit) veya Difficulty Tuner (play, canli).
    /// </summary>
    public struct DifficultyDaySample : IBufferElementData
    {
        public float NightIntensityMult;
        public float ZombieHpMult;
        public float SpawnBatchMult;
        // Kanli ay carpani: 1 = normal gece; >1 = o gece ozel (baker SpecialNights'tan hesaplar).
        // 0 (eski bake) calisma aninda 1 sayilir (geriye uyumlu).
        public float BloodMoonIntensityMult;
    }

    /// <summary>
    /// Oyuncunun attigi Ates Topu istegi (Mono -> ECS). GameManager.TryCastFireball yaratir;
    /// FireballStrikeSystem ayni/ertesi frame yaricap ici zombilere hasari uygular ve siler.
    /// </summary>
    public struct FireballStrike : IComponentData
    {
        public float2 Position;
        public float Radius;
        public float Damage;
    }

    /// <summary>
    /// Ucan Ates Topu mermisi (polish: fireball artik anlik degil, gorunur bir mermi).
    /// GameManager.TryCastFireball yaratir; FireballProjectileSystem hedefe tasir, varista
    /// FireballStrike uretir (hasar + SFX ayni kanaldan) ve mermiyi siler. Gorseli Mono'da
    /// SpellCastUI cizer (entity pozisyonunu takip eden flipbook — tek otorite ECS).
    /// </summary>
    public struct FireballProjectile : IComponentData
    {
        public float2 Target;
        public float Speed;
        public float Radius;
        public float Damage;
    }
}
