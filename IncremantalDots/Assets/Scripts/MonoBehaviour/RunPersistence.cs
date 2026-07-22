using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// V1 run snapshot. Kritik kosu state'i exact; buyuk combat alani perceptually faithful
    /// deterministik rebuild policy ile korunur. JsonUtility Dictionary serilestirmedigi icin
    /// dictionary state'leri list pair olarak tutulur.
    /// </summary>
    [Serializable]
    public class RunSaveState
    {
        public const int CurrentVersion = 17;
        public const int MinimumSupportedVersion = 3;

        public int Version = CurrentVersion;
        public string RunId;
        public bool IsDead;

        // Gun / phase / exact cycle
        public int CycleIndex;
        public int CyclePhase;
        public float CycleTimer;
        public float CycleProgress01;
        public float PhaseProgress01;
        public float SpawnIntensityMultiplier;
        public float HordePressure01;
        public bool IsBloodMoonNight;

        // Game state
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public int TotalKills;

        // v15 run_ended telemetry: scalar peak ve day/phase bazli applied Wall damage.
        // Eski v14 snapshot historical telemetry uydurmadan 0/bos state ile migrate edilir.
        public int TelemetryPeakEnemies;
        public List<RunWallDamageTelemetrySaveState> WallDamageTimeline =
            new List<RunWallDamageTelemetrySaveState>();

        // Wave + deterministic spawn stream
        public int CurrentWave;
        public int ZombiesToSpawn;
        public int ZombiesSpawned;
        public float SpawnTimer;
        public float SpawnInterval;
        public float ZombieHP;
        public float ZombieDamage;
        public float ZombieSpeed;
        public bool WaveActive;
        public int WavePhase;
        public float PrepTimer;
        public float PrepDuration;
        public float WaveStartDelay;
        public float WaveStartTimer;
        public uint SpawnRandomState;

        // Continuous spawn budget + runtime telemetry
        public long SpawnBacklog;
        public long TotalDemandedEnemies;
        public long TotalBudgetSpawnedEnemies;
        public int DemandPerInterval;
        public int LastDemandedEnemies;
        public int LastBudgetSpawnedEnemies;
        public float DayQuantityMultiplier;
        public float DayBaseSpawnInterval;
        public float PhaseIntensityMultiplier;
        public float EffectiveSpawnInterval;

        // Kaynaklar + kesirli accumulator
        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;
        public float WoodAccumulator;
        public float StoneAccumulator;
        public float IronAccumulator;
        public float FoodAccumulator;
        public int ArrowCurrent;
        public float ArrowAccumulator;
        public int ArrowCapacityLevel;
        public int ArrowEfficiencyLevel;
        public long GraveEssence;
        public double GraveEssenceMetaGainAccumulator;

        // JsonUtility null nested class'i default object olarak yazabildigi icin explicit
        // discriminator otoritedir. False ise HeartGraph payload'i ignore edilir.
        public bool HasHeartGraph;
        public GeneratedRunGraph HeartGraph;

        // Nufus + isci dagilimi
        public int PopulationTotal;
        public int PopulationCapacity;
        public int PopulationBaseCapacity;
        public int BedBaseCapacity;
        public int PurchasedBedCapacity;
        public int WoodWorkers;
        public int StoneWorkers;
        public int IronWorkers;
        public int FoodWorkers;
        public int WoodWorkerTargetRatioBps;
        public int StoneWorkerTargetRatioBps;
        public int IronWorkerTargetRatioBps;
        public int FoodWorkerTargetRatioBps;
        public int WoodWorkerCapacity;
        public int StoneWorkerCapacity;
        public int IronWorkerCapacity;
        public int FoodWorkerCapacity;
        public int WorkerIdlePopulation;
        public int LastObservedPopulation;
        public int LastPopulationGrowthWave;
        public int LastPopulationGrowthCycle;
        public int LastEventPrepWave;

        // Hazir worker binalarinin run-ici kapasite/verimlilik yatirimlari
        public int WoodBuildingCapacityLevel;
        public int WoodBuildingEfficiencyLevel;
        public int StoneBuildingCapacityLevel;
        public int StoneBuildingEfficiencyLevel;
        public int IronBuildingCapacityLevel;
        public int IronBuildingEfficiencyLevel;
        public int FoodBuildingCapacityLevel;
        public int FoodBuildingEfficiencyLevel;

        // Savunma
        public float WallCurrentHP;
        public int CastleUpgradeLevel;

        // Okcular (formasyon stable algorithm ile count'tan yeniden kurulur)
        public int ArcherFormationVersion = ArcherFormationUtility.CurrentVersion;
        public int BasicArchers;
        public int RapidArchers;
        public int FrostArchers;
        // v3-v16 JSON uyumlulugu icin alan korunur. v17 migration listeyi temizler;
        // aktif okcu stat progression'i exact HeartGraph level state'idir.
        public List<ArcherLevelEntry> ArcherTypeLevels = new List<ArcherLevelEntry>();

        // Tech: level state otoritedir; reveal/effect aggregate level'lardan yeniden kurulur.
        public List<TechLevelEntry> TechNodeLevels = new List<TechLevelEntry>();

        // Legacy level-up state (V1 Heart migration'i tamamlanana kadar exact korunur)
        public List<UpgradeTierEntry> UpgradeTiers = new List<UpgradeTierEntry>();
        public float GlobalArrowDamageBonus;
        public float GlobalFireRateMultiplier;

        // Council run state: regular handled day, karar hafizasi ve deterministic salt.
        // Cozulmus sureli production/next-night etkileri asagidaki economy state alanlarindadir.
        public List<CouncilFlagEntry> CouncilFlags = new List<CouncilFlagEntry>();
        public List<string> RecentCouncilTemplates = new List<string>();
        public List<string> UsedOneShotCouncils = new List<string>();
        public int LastRegularCouncilDay = -1;

        // v10 chance/pity migration girdileri. v11 runtime bu alanlari kullanmaz.
        public int CouncilDaysSinceEvent;
        public int CouncilCooldownRemaining;
        public int LastCouncilRollDay;
        public uint CouncilRunSalt;
        public int CouncilWoodCapBonus;
        public int CouncilStoneCapBonus;
        public int CouncilIronCapBonus;
        public int CouncilFoodCapBonus;
        // JsonUtility null nested class'i bos event nesnesine cevirebildigi icin otorite.
        public bool HasActiveCouncilEvent;
        public ComposedCouncilEvent ActiveCouncilEvent;

        // Run ability / prep state. Unlock ve resolved tuning multiplier'lari burada
        // kopyalanmaz; TechNodeLevels + exact HeartGraph restore'u bunlari yeniden kurar.
        public float FireballCooldownRemaining;
        public bool FortifyActive;
        public float FortifyDamageMultiplier;
        public float RallyTimer;
        public float RallyDuration;
        public float RallyFireRateMultiplier;
        public float RallyCooldownRemaining;
        public float EmergencyRepairCooldownRemaining;

        // Council'in kullandigi sureli economy/horde effect state'i
        public int PendingEconomyEvent;
        public int EconomyEventWave;
        public int EconomyEventCooldownWaves;
        public int ProductionBonusResource;
        public float ProductionBonusMultiplier;
        public int ProductionBonusExpiresAfterWave;
        public uint EconomyRandomSeed;
        public float NextNightSpawnMultiplier;
        public int NightSpawnExpiresAfterWave;

        // Oyuncu secimleri
        public int EconomyFocus;

        // v14: 10K horde entity pozisyonlarini tek tek yazmaz. Discriminator false ise
        // v3-v13 legacy exact entity listesi restore edilir; true ise aggregate rebuild otoritedir.
        public bool HasCombatRebuild;
        public CombatRebuildRunSaveState CombatRebuild;

        // Legacy v3-v13 exact combat fallback. Yeni v14 capture bu listeyi bos birakir.
        public List<ZombieRunSaveState> ActiveZombies = new List<ZombieRunSaveState>();
        public List<ArrowRunSaveState> ActiveArrows = new List<ArrowRunSaveState>();
        public FireballRunSaveState ActiveFireball;
        public List<FireballStrikeRunSaveState> ActiveFireballStrikes =
            new List<FireballStrikeRunSaveState>();
        public List<FireballDelayedBlastRunSaveState> ActiveFireballDelayedBlasts =
            new List<FireballDelayedBlastRunSaveState>();
        public List<FireballBurningGroundRunSaveState> ActiveFireballBurningGrounds =
            new List<FireballBurningGroundRunSaveState>();
    }

    [Serializable] public class TechLevelEntry { public string Id; public int Level; }
    [Serializable] public class ArcherLevelEntry { public int Type; public int Level; }
    [Serializable] public class UpgradeTierEntry { public int Type; public int Tier; }
    [Serializable] public class CouncilFlagEntry { public string Flag; public int Day; }
    [Serializable]
    public class RunWallDamageTelemetrySaveState
    {
        public int Day;
        public int Phase;
        public float Damage;
    }

    [Serializable]
    public class CombatRebuildRunSaveState
    {
        public int PolicyVersion = CombatRebuildUtility.CurrentPolicyVersion;
        public uint Seed;
        public int TotalZombies;
        public int XCellCount;
        public int YCellCount;
        public int HealthBandCount;
        public float MinX, MaxX, MinY, MaxY;
        public List<CombatRebuildBucketRunSaveState> Buckets =
            new List<CombatRebuildBucketRunSaveState>();
    }

    [Serializable]
    public class CombatRebuildBucketRunSaveState
    {
        public int XCell;
        public int YCell;
        public int State;
        public int HealthBand;
        public int Count;
        public bool SlowEnabled;
        public bool HasDeathTimer;
        public float Z;
        public float Scale;
        public float MoveSpeed;
        public float MaxHP;
        public float CurrentHP;
        public float AttackDamage;
        public float AttackCooldown;
        public float AttackTimer;
        public int XPReward;
        public float SlowDuration;
        public float SlowMultiplier;
        public float VelocityX;
        public float VelocityY;
        public float ForceX;
        public float ForceY;
        public float DeathTimer;
    }

    [Serializable]
    public class ZombieRunSaveState
    {
        public float X, Y, Z, Scale;
        public float MoveSpeed, MaxHP, CurrentHP, AttackDamage, AttackCooldown, AttackTimer;
        public int XPReward;
        public int State;
        public bool SlowEnabled;
        public float SlowDuration, SlowMultiplier;
        public float VelocityX, VelocityY, ForceX, ForceY;
        public bool HasDeathTimer;
        public float DeathTimer;
    }

    [Serializable]
    public class ArrowRunSaveState
    {
        public float X, Y, Z, Scale;
        public float Speed, Damage;
        public int TargetZombieIndex = -1;
        public int TargetZombieBucketIndex = -1;
        public int ArcherType;
        public float SlowDuration, SlowMultiplier;
        public float RemainingLifetime;
    }

    [Serializable]
    public class FireballRunSaveState
    {
        public bool Active;
        public float X, Y, Z, Scale;
        public float TargetX, TargetY;
        public float Speed, Radius, Damage;
        public int Evolutions;
    }

    [Serializable]
    public class FireballStrikeRunSaveState
    {
        public float X, Y;
        public float Radius, Damage;
        public int Kind;
        public int Evolutions;
    }

    [Serializable]
    public class FireballDelayedBlastRunSaveState
    {
        public float X, Y;
        public float Radius, Damage;
        public float RemainingDelay;
    }

    [Serializable]
    public class FireballBurningGroundRunSaveState
    {
        public float X, Y;
        public float Radius, DamagePerTick;
        public float RemainingDuration, TimeUntilNextTick;
        public int RemainingTicks;
    }

    /// <summary>
    /// Olum transaction journal'i. Once yazilir, sonra run save silinir ve meta odulu
    /// idempotent uygulanir. Islem ortasinda force-close olursa sonraki acilis tamamlar.
    /// </summary>
    [Serializable]
    public class RunDeathReceipt
    {
        public const int CurrentVersion = 2;
        public const int MinimumSupportedVersion = 1;

        // Field initializer v1 kalir: test/legacy code yalniz Day+Kills verdiginde eski
        // yayinlanmis formul acik migration yoluyla uygulanir. Production GameManager v2 yazar.
        public int Version = 1;
        public string RunId;
        public int Day;
        public int Kills;
        public int PeakPopulation;
        public MetaRewardQuote Reward;
    }

    public static class RunPersistence
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "run_save.json");
        private static string DeathReceiptPath => Path.Combine(Application.persistentDataPath, "run_death_receipt.json");

        public static bool HasSave => TryLoad() != null;

        public static bool IsSupportedVersion(int version)
        {
            return version >= RunSaveState.MinimumSupportedVersion
                && version <= RunSaveState.CurrentVersion;
        }

        internal static bool IsLoadableState(
            RunSaveState state,
            RunDeathReceipt pendingDeath,
            bool hasPendingDeathMarker = false)
        {
            if (state == null || state.IsDead || string.IsNullOrEmpty(state.RunId))
                return false;

            if (!IsSupportedVersion(state.Version))
                return false;

            // Journal dosyasi var fakat payload parse edilemiyorsa fail-closed davran:
            // corrupt/yarim receipt oyuncuya olum oncesi snapshot'i geri veremez.
            if (hasPendingDeathMarker
                && (pendingDeath == null || string.IsNullOrEmpty(pendingDeath.RunId)))
                return false;

            return pendingDeath == null || pendingDeath.RunId != state.RunId;
        }

        public static RunSaveState TryLoad()
        {
            try
            {
                if (!AtomicJsonFile.TryRecoverOrphanedTemp(FilePath, out string recoveryError))
                    Debug.LogWarning($"[RunPersistence] Yetim run temp kaydi kurtarilamadi: {recoveryError}");
                if (!File.Exists(FilePath))
                    return null;

                var state = JsonUtility.FromJson<RunSaveState>(File.ReadAllText(FilePath));
                var pendingDeath = TryLoadPendingDeath();
                bool hasPendingDeathMarker = File.Exists(DeathReceiptPath)
                    || File.Exists(DeathReceiptPath + ".tmp");
                if (!IsLoadableState(state, pendingDeath, hasPendingDeathMarker))
                {
                    if (state != null && !IsSupportedVersion(state.Version))
                        Debug.LogWarning($"[RunPersistence] Run save v{state.Version} exact snapshot schema ile uyumlu degil; sessiz migration yapilmadi.");
                    return null;
                }

                UpgradeToCurrent(state);
                NormalizeRetiredArcherProgressionState(state);
                NormalizeFireballEvolutionState(state);
                NormalizeActiveCouncilEvent(state);
                if (!NormalizeCombatRebuild(state, out string combatError))
                {
                    Debug.LogWarning($"[RunPersistence] Combat rebuild snapshot gecersiz: {combatError}");
                    return null;
                }
                if (!NormalizeRunTelemetry(state, out string telemetryError))
                {
                    Debug.LogWarning($"[RunPersistence] Run telemetry snapshot gecersiz: {telemetryError}");
                    return null;
                }
                return state;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunPersistence] Kayit okunamadi: {e.Message}");
                return null;
            }
        }

        public static bool Save(RunSaveState state)
        {
            if (state == null || state.IsDead || string.IsNullOrEmpty(state.RunId))
                return false;

            state.Version = RunSaveState.CurrentVersion;
            state.ArcherFormationVersion = ArcherFormationUtility.NormalizeVersion(
                state.ArcherFormationVersion);
            NormalizeRetiredArcherProgressionState(state);
            NormalizeFireballEvolutionState(state);
            NormalizeActiveCouncilEvent(state);
            if (!NormalizeCombatRebuild(state, out string combatError))
            {
                Debug.LogWarning($"[RunPersistence] Combat rebuild snapshot yazilmadi: {combatError}");
                return false;
            }
            if (!NormalizeRunTelemetry(state, out string telemetryError))
            {
                Debug.LogWarning($"[RunPersistence] Run telemetry snapshot yazilmadi: {telemetryError}");
                return false;
            }
            return WriteJson(FilePath, state, "Run save");
        }

        private static void UpgradeToCurrent(RunSaveState state)
        {
            if (state == null || state.Version >= RunSaveState.CurrentVersion)
                return;

            if (state.Version == 3)
            {
                var allocation = new MobilePopulationAllocation
                {
                    WoodWorkers = state.WoodWorkers,
                    StoneWorkers = state.StoneWorkers,
                    IronWorkers = state.IronWorkers,
                    FoodWorkers = state.FoodWorkers,
                    WoodTargetRatioBps = state.WoodWorkerTargetRatioBps,
                    StoneTargetRatioBps = state.StoneWorkerTargetRatioBps,
                    IronTargetRatioBps = state.IronWorkerTargetRatioBps,
                    FoodTargetRatioBps = state.FoodWorkerTargetRatioBps
                };
                WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);
                state.WoodWorkerTargetRatioBps = allocation.WoodTargetRatioBps;
                state.StoneWorkerTargetRatioBps = allocation.StoneTargetRatioBps;
                state.IronWorkerTargetRatioBps = allocation.IronTargetRatioBps;
                state.FoodWorkerTargetRatioBps = allocation.FoodTargetRatioBps;
                state.WorkerIdlePopulation = Math.Max(0,
                    state.PopulationTotal - state.WoodWorkers - state.StoneWorkers
                    - state.IronWorkers - state.FoodWorkers
                    - state.BasicArchers - state.RapidArchers - state.FrostArchers);
                state.LastObservedPopulation = Math.Max(0, state.PopulationTotal);
                state.Version = 4;
            }

            if (state.Version == 4)
            {
                // v4 mobile loop gercek yatak state'i tasimiyordu; Capacity 999999
                // internal sentinel'iydi. Mevcut nufusu gecersiz kilmadan run'a gercek
                // bir baslangic yatak tabani verilir, satin alinmis yatak sifirdan baslar.
                state.BedBaseCapacity = Math.Max(
                    MobileBedCapacityUtility.DefaultInitialCapacity,
                    Math.Max(0, state.PopulationTotal));
                state.PurchasedBedCapacity = 0;
                state.Version = 5;
            }

            if (state.Version == 5)
            {
                // v5'te worker bina yatirimlari yoktu. JsonUtility eksik alanlari zaten
                // sifirlar; acik migration eski kosuyu sekiz temiz seviye ile devam ettirir.
                state.WoodBuildingCapacityLevel = 0;
                state.WoodBuildingEfficiencyLevel = 0;
                state.StoneBuildingCapacityLevel = 0;
                state.StoneBuildingEfficiencyLevel = 0;
                state.IronBuildingCapacityLevel = 0;
                state.IronBuildingEfficiencyLevel = 0;
                state.FoodBuildingCapacityLevel = 0;
                state.FoodBuildingEfficiencyLevel = 0;
                state.Version = 6;
            }

            if (state.Version == 6)
            {
                // v6 okcu sayilarini sakliyordu fakat formasyon algoritmasinin surumunu
                // tasimiyordu. Mevcut exact 40x25 V1 layout'una acik migration yapilir.
                state.ArcherFormationVersion = ArcherFormationUtility.CurrentVersion;
                state.Version = 7;
            }

            if (state.Version == 7)
            {
                // v7 finite ammo capacity/efficiency run seviyelerini tasimiyordu.
                // Legacy mobile stok hedefi 200 oldugu icin temiz level 0 migration'i exact'tir.
                state.ArrowCapacityLevel = 0;
                state.ArrowEfficiencyLevel = 0;
                state.Version = 8;
            }

            if (state.Version == 8)
            {
                // v8'de Grave Essence run state'i yoktu. Yeni currency meta'dan
                // beslenmez; eski exact snapshot temiz 0 bakiye ile devam eder.
                state.GraveEssence = 0;
                state.Version = 9;
            }

            if (state.Version == 9)
            {
                // v9 Grave Essence tasiyordu fakat generated graph'i tasimiyordu. Eksik
                // graph'i aktif catalog'dan sessizce yeniden uretmek yerine null migration
                // acikca korunur; runtime bu kosuda Heart'i exact-state gate'inde kilitler.
                state.HasHeartGraph = false;
                state.HeartGraph = null;
                state.Version = 10;
            }

            if (state.Version == 10)
            {
                // v10 regular Council'i chance/pity/cooldown ile her Dawn roll ediyordu.
                // Yalniz mevcut gunde gercekten uretilmis bir kart kanitlanabiliyorsa
                // o gun handled sayilir; eski chance fail'i yeni exact karti yutamaz.
                int currentDay = Math.Max(1, state.CycleIndex + 1);
                bool legacyHasActiveEvent = IsValidCouncilEventPayload(state.ActiveCouncilEvent);
                state.HasActiveCouncilEvent = legacyHasActiveEvent;
                if (!legacyHasActiveEvent)
                    state.ActiveCouncilEvent = null;
                state.LastRegularCouncilDay = CouncilRegularSchedule.MigrateLegacyHandledDay(
                    currentDay,
                    state.LastCouncilRollDay,
                    state.CouncilDaysSinceEvent,
                    legacyHasActiveEvent);
                state.Version = 11;
            }

            if (state.Version == 11)
            {
                // v11 aktif Rally etkisini tasiyordu fakat Rally/Emergency cooldown
                // state'leri yoktu. Eski kosular iki ability'yi hazir durumda devralir.
                state.RallyCooldownRemaining = 0f;
                state.EmergencyRepairCooldownRemaining = 0f;
                state.Version = 12;
            }

            if (state.Version == 12)
            {
                // v12 Grave Essence bakiyesini tasiyordu fakat meta gain'in kesirli payini
                // tasimiyordu. Eski kosu temiz 0 remainder ile exact devam eder.
                state.GraveEssenceMetaGainAccumulator = 0d;
                state.Version = 13;
            }

            if (state.Version == 13)
            {
                // v13 her zombie pozisyonunu exact listede tasiyordu. Migration bu veriyi
                // aggregate'e tahminle map etmez; ilk v14 save'e kadar legacy exact fallback kalir.
                state.HasCombatRebuild = false;
                state.CombatRebuild = null;
                state.Version = 14;
            }

            if (state.Version == 14)
            {
                // v14 peak enemy veya Wall damage history tasimiyordu. Historical deger
                // uydurulmaz; Continue sonrasi runtime mevcut alive count'tan itibaren izler.
                state.TelemetryPeakEnemies = 0;
                state.WallDamageTimeline = new List<RunWallDamageTelemetrySaveState>();
                state.Version = 15;
            }

            if (state.Version == 15)
            {
                // v15 Fireball projectile'ini tasiyordu fakat behavior evolution flag'leri,
                // pending second blast ve active Burning Ground state'leri yoktu. Bu content
                // v15 catalog'unda bulunmadigi icin temiz bos state exact migration'dir.
                state.ActiveFireballStrikes = new List<FireballStrikeRunSaveState>();
                state.ActiveFireballDelayedBlasts = new List<FireballDelayedBlastRunSaveState>();
                state.ActiveFireballBurningGrounds = new List<FireballBurningGroundRunSaveState>();
                state.Version = 16;
            }

            if (state.Version == 16)
            {
                // v16 ve oncesindeki player-facing Basic/Rapid/Frost level state'i Heart
                // graph disinda ikinci bir stat owner'iydi. Graph node/level veya Essence
                // uydurmadan retired liste temizlenir; mevcut exact Heart graph korunur.
                state.ArcherTypeLevels = new List<ArcherLevelEntry>();
                state.Version = 17;
            }
        }

        private static void NormalizeRetiredArcherProgressionState(RunSaveState state)
        {
            if (state == null)
                return;

            state.ArcherTypeLevels ??= new List<ArcherLevelEntry>();
            state.ArcherTypeLevels.Clear();
        }

        private static void NormalizeFireballEvolutionState(RunSaveState state)
        {
            if (state == null)
                return;

            state.ActiveFireballStrikes ??= new List<FireballStrikeRunSaveState>();
            state.ActiveFireballDelayedBlasts ??= new List<FireballDelayedBlastRunSaveState>();
            state.ActiveFireballBurningGrounds ??= new List<FireballBurningGroundRunSaveState>();
        }

        private static bool NormalizeCombatRebuild(RunSaveState state, out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Run state null.";
                return false;
            }

            if (!state.HasCombatRebuild)
            {
                state.CombatRebuild = null;
                return true;
            }

            return CombatRebuildUtility.IsValid(state.CombatRebuild, out error);
        }

        private static bool NormalizeRunTelemetry(RunSaveState state, out string error)
        {
            error = string.Empty;
            if (state == null)
            {
                error = "Run state null.";
                return false;
            }
            if (state.TelemetryPeakEnemies < 0)
            {
                error = "Peak enemy count negatif olamaz.";
                return false;
            }

            if (state.WallDamageTimeline == null)
                state.WallDamageTimeline = new List<RunWallDamageTelemetrySaveState>();

            int previousDay = 0;
            int previousPhase = -1;
            for (int i = 0; i < state.WallDamageTimeline.Count; i++)
            {
                RunWallDamageTelemetrySaveState entry = state.WallDamageTimeline[i];
                if (entry == null || entry.Day < 1
                    || entry.Phase < (int)SiegeCyclePhase.Day
                    || entry.Phase > (int)SiegeCyclePhase.Dawn
                    || entry.Damage <= 0f || float.IsNaN(entry.Damage)
                    || float.IsInfinity(entry.Damage))
                {
                    error = $"Wall damage timeline[{i}] gecersiz.";
                    return false;
                }

                if (entry.Day < previousDay
                    || (entry.Day == previousDay && entry.Phase <= previousPhase))
                {
                    error = $"Wall damage timeline[{i}] kronolojik veya unique degil.";
                    return false;
                }

                previousDay = entry.Day;
                previousPhase = entry.Phase;
            }

            return true;
        }

        private static void NormalizeActiveCouncilEvent(RunSaveState state)
        {
            if (state == null)
                return;

            if (!state.HasActiveCouncilEvent || !IsValidCouncilEventPayload(state.ActiveCouncilEvent))
            {
                state.HasActiveCouncilEvent = false;
                state.ActiveCouncilEvent = null;
            }
        }

        private static bool IsValidCouncilEventPayload(ComposedCouncilEvent composed)
        {
            return composed != null
                && !string.IsNullOrEmpty(composed.TemplateId)
                && composed.OptionA != null
                && composed.OptionB != null;
        }

        public static bool CommitDeath(RunDeathReceipt receipt)
        {
            if (receipt == null || string.IsNullOrEmpty(receipt.RunId))
                return false;

            RunDeathReceipt pending = TryLoadPendingDeath();
            if (pending != null && pending.RunId != receipt.RunId)
            {
                Debug.LogError(
                    $"[RunPersistence] Cozulmemis death receipt varken yeni run receipt'i yazilamaz. " +
                    $"pending={pending.RunId}, incoming={receipt.RunId}");
                return false;
            }

            // Journal once: bu dosya varsa ayni run artik Continue edilemez.
            if (pending == null && !WriteJson(DeathReceiptPath, receipt, "Death receipt"))
                return false;

            // Receipt authoritative olduktan sonra snapshot silinemese bile TryLoad matching
            // RunId'yi reddeder. Silme burada yalniz fiziksel cleanup'tir.
            Delete();
            return true;
        }

        public static RunDeathReceipt TryLoadPendingDeath()
        {
            try
            {
                if (!AtomicJsonFile.TryRecoverOrphanedTemp(DeathReceiptPath, out string recoveryError))
                    Debug.LogWarning($"[RunPersistence] Yetim death receipt temp kaydi kurtarilamadi: {recoveryError}");
                if (!File.Exists(DeathReceiptPath))
                    return null;

                return JsonUtility.FromJson<RunDeathReceipt>(File.ReadAllText(DeathReceiptPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunPersistence] Death receipt okunamadi: {e.Message}");
                return null;
            }
        }

        public static bool TryFinalizePendingDeathReward(out MetaRunResult result)
        {
            result = default;
            var receipt = TryLoadPendingDeath();
            if (receipt == null || string.IsNullOrEmpty(receipt.RunId))
                return false;

            if (receipt.Version < RunDeathReceipt.MinimumSupportedVersion
                || receipt.Version > RunDeathReceipt.CurrentVersion)
            {
                Debug.LogError(
                    $"[RunPersistence] Desteklenmeyen death receipt schema v{receipt.Version}.");
                return false;
            }

            if (receipt.Version >= 2)
            {
                if (receipt.Reward.Day != Math.Max(0, receipt.Day)
                    || receipt.Reward.Kills != Math.Max(0, receipt.Kills)
                    || receipt.Reward.PeakPopulation != Math.Max(0, receipt.PeakPopulation)
                    || !MetaRewardCalculator.IsStructurallyValid(receipt.Reward))
                {
                    Debug.LogError("[RunPersistence] Death receipt v2 reward payload'i gecersiz.");
                    return false;
                }

                result = MetaProgression.AddRunResult(receipt.RunId, receipt.Reward);
            }
            else
            {
                result = MetaProgression.AddRunResult(receipt.RunId, receipt.Day, receipt.Kills);
            }
            if (!result.Persisted || !MetaProgression.HasRewardedRun(receipt.RunId))
                return false;

            Delete();
            DeleteFile(DeathReceiptPath, "Death receipt");
            return true;
        }

        public static bool RecoverPendingDeathReward()
        {
            return TryFinalizePendingDeathReward(out _);
        }

        public static void Delete()
        {
            DeleteFile(FilePath, "Run save");
        }

        private static bool WriteJson<T>(string path, T state, string label)
        {
            string json = JsonUtility.ToJson(state, false);
            if (AtomicJsonFile.TryWrite(path, json, out string error))
            {
                return true;
            }

            Debug.LogError($"[RunPersistence] {label} yazilamadi: {error}");
            return false;
        }

        private static void DeleteFile(string path, string label)
        {
            if (!AtomicJsonFile.TryDelete(path, out string error))
                Debug.LogWarning($"[RunPersistence] {label} silinemedi: {error}");
        }
    }
}
