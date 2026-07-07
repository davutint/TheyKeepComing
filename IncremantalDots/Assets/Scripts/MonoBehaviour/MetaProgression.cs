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
        public int Version = 1;
        public int Souls;                 // harcanabilir bakiye (1 kill = 1 Ruh)
        public int TotalSoulsEarned;
        public int BestDay;
        public int TotalRuns;
        public long TotalKillsAllTime;
        public List<MetaUpgradeLevel> Upgrades = new List<MetaUpgradeLevel>();
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
        public const string CurrencyName = "RUH"; // isim tek yerden degisir (owner henuz kesinlestirmedi)
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
        public static MetaRunResult AddRunResult(int day, int kills)
        {
            var s = State;
            bool newRecord = day > s.BestDay;
            int earned = Mathf.Max(0, kills) + (newRecord ? day * RecordBonusPerDay : 0);

            s.Souls += earned;
            s.TotalSoulsEarned += earned;
            s.TotalKillsAllTime += Mathf.Max(0, kills);
            s.TotalRuns++;
            if (newRecord)
                s.BestDay = day;

            Save();
            return new MetaRunResult { Day = day, Kills = kills, SoulsEarned = earned, NewRecord = newRecord };
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
