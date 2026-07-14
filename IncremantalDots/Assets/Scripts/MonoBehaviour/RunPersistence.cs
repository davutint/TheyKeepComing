using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// V1 exact-run snapshot. JsonUtility Dictionary serilestirmedigi icin dictionary state'leri
    /// list pair olarak tutulur. Definition asset'lerden turetilebilen carpanlar kaydedilmez;
    /// oyuncunun Continue sonrasinda ayni ani algilamasini etkileyen runtime state kaydedilir.
    /// </summary>
    [Serializable]
    public class RunSaveState
    {
        public const int CurrentVersion = 8;
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
        public List<ArcherLevelEntry> ArcherTypeLevels = new List<ArcherLevelEntry>();

        // Tech: level state otoritedir; reveal/effect aggregate level'lardan yeniden kurulur.
        public List<TechLevelEntry> TechNodeLevels = new List<TechLevelEntry>();

        // Legacy level-up state (V1 Heart migration'i tamamlanana kadar exact korunur)
        public List<UpgradeTierEntry> UpgradeTiers = new List<UpgradeTierEntry>();
        public float GlobalArrowDamageBonus;
        public float GlobalFireRateMultiplier;

        // Council state
        public List<CouncilFlagEntry> CouncilFlags = new List<CouncilFlagEntry>();
        public List<string> RecentCouncilTemplates = new List<string>();
        public List<string> UsedOneShotCouncils = new List<string>();
        public int CouncilDaysSinceEvent;
        public int CouncilCooldownRemaining;
        public int LastCouncilRollDay;
        public uint CouncilRunSalt;
        public int CouncilWoodCapBonus;
        public int CouncilStoneCapBonus;
        public int CouncilIronCapBonus;
        public int CouncilFoodCapBonus;
        public ComposedCouncilEvent ActiveCouncilEvent;

        // Run ability / prep state
        public float FireballCooldownRemaining;
        public bool FortifyActive;
        public float FortifyDamageMultiplier;
        public float RallyTimer;
        public float RallyDuration;
        public float RallyFireRateMultiplier;

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

        // Compact combat snapshot. Formation ve worker world entity'leri sayisal state'ten kurulur.
        public List<ZombieRunSaveState> ActiveZombies = new List<ZombieRunSaveState>();
        public List<ArrowRunSaveState> ActiveArrows = new List<ArrowRunSaveState>();
        public FireballRunSaveState ActiveFireball;
    }

    [Serializable] public class TechLevelEntry { public string Id; public int Level; }
    [Serializable] public class ArcherLevelEntry { public int Type; public int Level; }
    [Serializable] public class UpgradeTierEntry { public int Type; public int Tier; }
    [Serializable] public class CouncilFlagEntry { public string Flag; public int Day; }

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
    }

    /// <summary>
    /// Olum transaction journal'i. Once yazilir, sonra run save silinir ve meta odulu
    /// idempotent uygulanir. Islem ortasinda force-close olursa sonraki acilis tamamlar.
    /// </summary>
    [Serializable]
    public class RunDeathReceipt
    {
        public int Version = 1;
        public string RunId;
        public int Day;
        public int Kills;
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

        internal static bool IsLoadableState(RunSaveState state, RunDeathReceipt pendingDeath)
        {
            if (state == null || state.IsDead || string.IsNullOrEmpty(state.RunId))
                return false;

            if (!IsSupportedVersion(state.Version))
                return false;

            return pendingDeath == null || pendingDeath.RunId != state.RunId;
        }

        public static RunSaveState TryLoad()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                var state = JsonUtility.FromJson<RunSaveState>(File.ReadAllText(FilePath));
                var pendingDeath = TryLoadPendingDeath();
                if (!IsLoadableState(state, pendingDeath))
                {
                    if (state != null && !IsSupportedVersion(state.Version))
                        Debug.LogWarning($"[RunPersistence] Run save v{state.Version} exact snapshot schema ile uyumlu degil; sessiz migration yapilmadi.");
                    return null;
                }

                UpgradeToCurrent(state);
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
        }

        public static void CommitDeath(RunDeathReceipt receipt)
        {
            if (receipt == null || string.IsNullOrEmpty(receipt.RunId))
                return;

            // Journal once: bu dosya varsa ayni run artik Continue edilemez.
            WriteJson(DeathReceiptPath, receipt, "Death receipt");
            Delete();
        }

        public static RunDeathReceipt TryLoadPendingDeath()
        {
            try
            {
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

        public static void RecoverPendingDeathReward()
        {
            var receipt = TryLoadPendingDeath();
            if (receipt == null || string.IsNullOrEmpty(receipt.RunId))
                return;

            MetaProgression.AddRunResult(receipt.RunId, receipt.Day, receipt.Kills);
            if (MetaProgression.HasRewardedRun(receipt.RunId))
            {
                Delete();
                DeleteFile(DeathReceiptPath, "Death receipt");
            }
        }

        public static void ClearPendingDeath(string runId)
        {
            var receipt = TryLoadPendingDeath();
            if (receipt != null && receipt.RunId == runId)
                DeleteFile(DeathReceiptPath, "Death receipt");
        }

        public static void Delete()
        {
            DeleteFile(FilePath, "Run save");
        }

        private static bool WriteJson<T>(string path, T state, string label)
        {
            try
            {
                // Exact snapshot oyuncuya gosterilen bir dosya degildir. Pretty-print,
                // 10K combat state'inde milyonlarca gereksiz whitespace byte'i ve ek
                // serialization/I/O maliyeti uretir; compact JSON ayni schema'yi korur.
                File.WriteAllText(path, JsonUtility.ToJson(state, false));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RunPersistence] {label} yazilamadi: {e.Message}");
                return false;
            }
        }

        private static void DeleteFile(string path, string label)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunPersistence] {label} silinemedi: {e.Message}");
            }
        }
    }
}
