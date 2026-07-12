using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Safak-checkpoint kayit govdesi (M-E, JsonUtility uyumlu). Yalniz koridor-disi
    /// (recompute EDILEMEYEN) durum kaydedilir; tech carpanlari, reveal listesi, spell
    /// unlock'lari, meta bonuslari gibi turetilebilir her sey restore'da yeniden hesaplanir
    /// (otorite: RUN_SAVE_ARCHITECTURE.md). Zombiler/oklar kaydedilmez — safak temiz alandir.
    /// JsonUtility Dictionary serilestirmez: tum dict'ler List&lt;pair&gt; olarak tutulur.
    /// </summary>
    [Serializable]
    public class RunSaveState
    {
        public int Version = 2;

        // Gun / ilerleme (CycleIndex = kayit ANINDAKI gun index'i; DAY = CycleIndex+1)
        public int CycleIndex;
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public int TotalKills;

        // Kaynaklar
        public int Wood;
        public int Stone;
        public int Iron;
        public int Food;

        // Nufus + isci dagilimi
        public int PopulationTotal;
        public int PopulationCapacity;
        public int PopulationBaseCapacity;
        public int WoodWorkers;
        public int StoneWorkers;
        public int IronWorkers;
        public int FoodWorkers;

        // Savunma (MaxHP kaydedilmez — tech/meta aggregate'lerinden yeniden kurulur)
        // Sonuc otoritesi olan tek Wall'in anlik HP degeri.
        public float WallCurrentHP;
        public int CastleUpgradeLevel;

        // Okcular (pozisyon kaydedilmez — tilemap slot sirasina yeniden dizilir)
        public int BasicArchers;
        public int RapidArchers;
        public int FrostArchers;
        public List<ArcherLevelEntry> ArcherTypeLevels = new List<ArcherLevelEntry>();

        // Tech: TEK otorite satin-alma seviyeleri; gerisi recompute
        public List<TechLevelEntry> TechNodeLevels = new List<TechLevelEntry>();

        // Level-up kart ilerlemesi
        public List<UpgradeTierEntry> UpgradeTiers = new List<UpgradeTierEntry>();
        public float GlobalArrowDamageBonus;
        public float GlobalFireRateMultiplier;

        // Council hafizasi (recompute edilemez)
        public List<CouncilFlagEntry> CouncilFlags = new List<CouncilFlagEntry>();
        public List<string> RecentCouncilTemplates = new List<string>();
        public List<string> UsedOneShotCouncils = new List<string>();
        public int CouncilDaysSinceEvent;
        public int CouncilCooldownRemaining;
        public uint CouncilRunSalt;
        public int CouncilWoodCapBonus;
        public int CouncilStoneCapBonus;
        public int CouncilIronCapBonus;
        public int CouncilFoodCapBonus;

        // Oyuncu secimleri
        public int EconomyFocus; // EconomyFocusType
    }

    [Serializable] public class TechLevelEntry { public string Id; public int Level; }
    [Serializable] public class ArcherLevelEntry { public int Type; public int Level; }
    [Serializable] public class UpgradeTierEntry { public int Type; public int Tier; }
    [Serializable] public class CouncilFlagEntry { public string Flag; public int Day; }

    /// <summary>
    /// Safak-checkpoint dosya katmani (MetaProgression kalibi): persistentDataPath/run_save.json.
    /// Kayit her DAWN'a giriste alinir (GameManager.SaveRunCheckpoint); olumde ve NEW RUN'da
    /// silinir (roguelite: kosu bitti). Ana menu CONTINUE bunu okur.
    /// </summary>
    public static class RunPersistence
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "run_save.json");

        public static bool HasSave => File.Exists(FilePath);

        public static RunSaveState TryLoad()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonUtility.FromJson<RunSaveState>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunPersistence] Kayit okunamadi: {e.Message}");
            }

            return null;
        }

        public static void Save(RunSaveState state)
        {
            if (state == null)
                return;

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(state, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[RunPersistence] Kayit yazilamadi: {e.Message}");
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunPersistence] Kayit silinemedi: {e.Message}");
            }
        }
    }
}
