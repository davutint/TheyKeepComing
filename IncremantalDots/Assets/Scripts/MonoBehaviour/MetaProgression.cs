using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>Kalici meta durumunun serilestirilebilir govdesi (JsonUtility uyumlu).</summary>
    [Serializable]
    public class MetaProgressState
    {
        public int Version = 2;
        public int Souls;                 // harcanabilir bakiye (1 kill = 1 Ruh)
        public int TotalSoulsEarned;
        public int BestDay;
        public int TotalRuns;
        public long TotalKillsAllTime;
        public List<MetaUpgradeLevel> Upgrades = new List<MetaUpgradeLevel>();
        // Death journal recovery ayni kosuya ikinci kez odul yazamasin.
        public List<string> RewardedRunIds = new List<string>();
    }

    [Serializable]
    public class MetaUpgradeLevel
    {
        public string Id;
        public int Level;
    }

    /// <summary>Tek bir kosunun kapanis ozeti (olum ekrani bunu gosterir).</summary>
    public struct MetaRunResult
    {
        public int Day;
        public int Kills;
        public int SoulsEarned;
        public bool NewRecord;
        public bool AlreadyRewarded;
    }

    /// <summary>
    /// Roguelite meta-progression'in kalici katmani (K2 karari). Kosular ARASI yasar:
    /// olumde kill'ler Ruh'a cevrilir (1 kill = 1 Ruh + yeni rekorda gun x RecordBonusPerDay),
    /// Ruh olum ekrani magazasinda kalici yukseltmelere harcanir. Depo: persistentDataPath/
    /// meta_progress.json (JsonUtility) — M-E save sisteminin ilk tugla'si.
    /// Kosu-ICI hicbir sey burada tutulmaz (o M-E'nin isi).
    /// </summary>
    public static class MetaProgression
    {
        // Owner karari (2026-07-08): kavram RUH, oyun dili INGILIZCE -> ekranda "SOULS"
        public const string CurrencyName = "SOULS";
        public const int RecordBonusPerDay = 50;  // yeni rekor: bonus = yeniBestDay * bu

        private static MetaProgressState _state;
        private static string FilePath => Path.Combine(Application.persistentDataPath, "meta_progress.json");

        public static MetaProgressState State
        {
            get
            {
                if (_state == null)
                    Load();
                return _state;
            }
        }

        public static void Load()
        {
            _state = null;
            try
            {
                if (File.Exists(FilePath))
                    _state = JsonUtility.FromJson<MetaProgressState>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MetaProgression] Kayit okunamadi, sifirdan baslaniyor: {e.Message}");
            }

            if (_state == null)
                _state = new MetaProgressState();

            _state.Version = 2;
            _state.Upgrades ??= new List<MetaUpgradeLevel>();
            _state.RewardedRunIds ??= new List<string>();
        }

        public static void Save()
        {
            if (_state == null)
                return;

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_state, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaProgression] Kayit yazilamadi: {e.Message}");
            }
        }

        /// <summary>
        /// Kosu kapanisi: kill'leri Ruh'a cevirir, rekoru gunceller, kaydeder.
        /// Ayni kosu icin bir kez cagrilmali (cagiran GameOver-gecisini izler).
        /// </summary>
        public static bool HasRewardedRun(string runId)
        {
            if (string.IsNullOrEmpty(runId))
                return false;

            return State.RewardedRunIds.Contains(runId);
        }

        public static MetaRunResult AddRunResult(string runId, int day, int kills)
        {
            var s = State;
            var result = ApplyRunResult(s, runId, day, kills);
            if (!result.AlreadyRewarded)
                Save();
            return result;
        }

        internal static MetaRunResult ApplyRunResult(MetaProgressState s, string runId, int day, int kills)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            s.RewardedRunIds ??= new List<string>();
            if (string.IsNullOrEmpty(runId) || s.RewardedRunIds.Contains(runId))
            {
                return new MetaRunResult
                {
                    Day = day,
                    Kills = kills,
                    SoulsEarned = 0,
                    NewRecord = false,
                    AlreadyRewarded = true
                };
            }

            bool newRecord = day > s.BestDay;
            int earned = Mathf.Max(0, kills) + (newRecord ? day * RecordBonusPerDay : 0);

            s.Souls += earned;
            s.TotalSoulsEarned += earned;
            s.TotalKillsAllTime += Mathf.Max(0, kills);
            s.TotalRuns++;
            if (newRecord)
                s.BestDay = day;

            s.RewardedRunIds.Add(runId);
            const int MaxRewardReceipts = 128;
            if (s.RewardedRunIds.Count > MaxRewardReceipts)
                s.RewardedRunIds.RemoveRange(0, s.RewardedRunIds.Count - MaxRewardReceipts);

            return new MetaRunResult
            {
                Day = day,
                Kills = kills,
                SoulsEarned = earned,
                NewRecord = newRecord,
                AlreadyRewarded = false
            };
        }

        public static int GetUpgradeLevel(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;

            foreach (var u in State.Upgrades)
            {
                if (u.Id == id)
                    return u.Level;
            }

            return 0;
        }

        /// <summary>Satin alma: bakiye + MaxLevel kontrolu; basarida seviye artar ve kaydedilir.</summary>
        public static bool TryBuyUpgrade(MetaUpgradeSO upgrade)
        {
            if (upgrade == null)
                return false;

            int level = GetUpgradeLevel(upgrade.Id);
            if (level >= upgrade.MaxLevel)
                return false;

            int cost = upgrade.GetCost(level);
            if (State.Souls < cost)
                return false;

            State.Souls -= cost;
            SetUpgradeLevel(upgrade.Id, level + 1);
            Save();
            return true;
        }

        private static void SetUpgradeLevel(string id, int level)
        {
            foreach (var u in State.Upgrades)
            {
                if (u.Id == id)
                {
                    u.Level = level;
                    return;
                }
            }

            State.Upgrades.Add(new MetaUpgradeLevel { Id = id, Level = level });
        }

        /// <summary>Test/debug: tum meta ilerlemeyi siler (oyuncu-yuzeyinde KULLANILMAZ).</summary>
        public static void ResetAll()
        {
            _state = new MetaProgressState();
            Save();
        }
    }
}
