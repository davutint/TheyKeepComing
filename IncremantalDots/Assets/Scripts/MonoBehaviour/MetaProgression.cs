using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeadWalls
{
    public enum MetaProgressLoadStatus
    {
        NotLoaded = 0,
        CreatedNew = 1,
        Loaded = 2,
        Migrated = 3,
        UnsupportedVersion = 4,
        Corrupt = 5
    }

    /// <summary>Kalici meta durumunun serilestirilebilir govdesi (JsonUtility uyumlu).</summary>
    [Serializable]
    public class MetaProgressState
    {
        public const int CurrentVersion = 3;
        public const int MinimumSupportedVersion = 1;

        public int Version = CurrentVersion;
        public int Souls;                 // harcanabilir kalici bakiye (weighted death reward)
        public int TotalSoulsEarned;
        public int BestDay;
        public int TotalRuns;
        public long TotalKillsAllTime;
        public List<MetaUpgradeLevel> Upgrades = new List<MetaUpgradeLevel>();
        // Meta yalniz olasi content havuzunu genisletir; aktif run graph'ini degistirmez.
        public List<string> UnlockedPoolIds = new List<string>();
        // Package I onboarding adimlari kendi stable flag Id'lerini bu listede saklar.
        public List<string> TutorialFlags = new List<string>();
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
        public MetaRewardQuote Reward;
        /// <summary>RewardedRunIds dahil meta state diske durable yazildi.</summary>
        public bool Persisted;
    }

    /// <summary>Meta magazasinin yalniz durable olum sonucu sonrasinda acilmasini tanimlar.</summary>
    public static class MetaPurchaseRules
    {
        public static bool CanPurchase(bool isGameOver, bool deathCollected, bool rewardPersisted,
            bool canPersist)
        {
            return isGameOver && deathCollected && rewardPersisted && canPersist;
        }
    }

    /// <summary>
    /// Roguelite meta-progression'in kalici katmani (K2 karari). Kosular ARASI yasar:
    /// olumde run sonucu production MetaRewardSettings ile Souls'a cevrilir,
    /// Ruh olum ekrani magazasinda kalici yukseltmelere harcanir. Depo: persistentDataPath/
    /// meta_progress.json (JsonUtility) — M-E save sisteminin ilk tugla'si.
    /// Kosu-ICI hicbir sey burada tutulmaz (o M-E'nin isi).
    /// </summary>
    public static class MetaProgression
    {
        // Save alanlari yayinlanmis uyumluluk nedeniyle Souls adini korur. Player-facing
        // kimlik MetaUpgradeCatalogSO.Presentation v2'de LAST EMBERS olarak catalog-owned'dir.
        public const string CurrencyName = "LAST EMBERS";
        public const string LegacyCurrencyName = "SOULS";
        // Yalniz v1 death receipt migration'i icin yayinlanmis eski sabit.
        public const int RecordBonusPerDay = MetaRewardCalculator.LegacyRecordBonusPerDay;
        private const int MaxRewardReceipts = 128;

        private static MetaProgressState _state;
        private static bool _persistenceBlocked;
        private static string FilePath => Path.Combine(Application.persistentDataPath, "meta_progress.json");

        public static MetaProgressLoadStatus LoadStatus { get; private set; } = MetaProgressLoadStatus.NotLoaded;
        public static bool CanPersist => !_persistenceBlocked;

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
            _persistenceBlocked = false;
            LoadStatus = MetaProgressLoadStatus.NotLoaded;
            try
            {
                if (!AtomicJsonFile.TryRecoverOrphanedTemp(FilePath, out string recoveryError))
                    Debug.LogWarning($"[MetaProgression] Yetim temp meta kaydi kurtarilamadi: {recoveryError}");

                if (!File.Exists(FilePath))
                {
                    _state = CreateDefaultState();
                    LoadStatus = MetaProgressLoadStatus.CreatedNew;
                    return;
                }

                string json = File.ReadAllText(FilePath);
                if (!TryDeserializeState(
                        json,
                        out MetaProgressState loaded,
                        out MetaProgressLoadStatus status,
                        out string loadError))
                {
                    // Bilinmeyen/corrupt schema mevcut dosyanin ustune sessizce yazilamaz.
                    // In-memory temiz state UI'nin acik kalmasini saglar; Save fail-closed olur.
                    _state = CreateDefaultState();
                    _persistenceBlocked = true;
                    LoadStatus = status;
                    Debug.LogError($"[MetaProgression] Meta save fail-closed kilitlendi: {loadError}");
                    return;
                }

                _state = loaded;
                LoadStatus = status;
                if (status == MetaProgressLoadStatus.Migrated && !Save())
                    Debug.LogError("[MetaProgression] Migrated meta schema durable yazilamadi.");
            }
            catch (Exception e)
            {
                _state = CreateDefaultState();
                _persistenceBlocked = true;
                LoadStatus = MetaProgressLoadStatus.Corrupt;
                Debug.LogError($"[MetaProgression] Meta save okunamadi; yazma fail-closed kilitlendi: {e.Message}");
            }
        }

        public static bool Save()
        {
            if (_state == null || _persistenceBlocked)
            {
                if (_persistenceBlocked)
                    Debug.LogError($"[MetaProgression] Save reddedildi; load status: {LoadStatus}.");
                return false;
            }

            NormalizeState(_state);

            if (AtomicJsonFile.TryWrite(FilePath, JsonUtility.ToJson(_state, true), out string error))
                return true;

            Debug.LogError($"[MetaProgression] Kayit yazilamadi: {error}");
            return false;
        }

        internal static bool TryDeserializeState(
            string json,
            out MetaProgressState state,
            out MetaProgressLoadStatus status,
            out string error)
        {
            state = null;
            status = MetaProgressLoadStatus.Corrupt;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Meta JSON bos.";
                return false;
            }

            try
            {
                var header = JsonUtility.FromJson<MetaVersionHeader>(json);
                int version = header != null ? header.Version : 0;
                if (!IsSupportedVersion(version))
                {
                    status = MetaProgressLoadStatus.UnsupportedVersion;
                    error = $"Desteklenmeyen meta schema v{version}; desteklenen aralik "
                            + $"v{MetaProgressState.MinimumSupportedVersion}-v{MetaProgressState.CurrentVersion}.";
                    return false;
                }

                state = JsonUtility.FromJson<MetaProgressState>(json);
                if (state == null)
                {
                    error = "Meta JSON deserialize sonucu null.";
                    return false;
                }

                int sourceVersion = state.Version;
                if (!TryUpgradeToCurrent(state, out error))
                {
                    status = MetaProgressLoadStatus.UnsupportedVersion;
                    state = null;
                    return false;
                }

                NormalizeState(state);
                status = sourceVersion == MetaProgressState.CurrentVersion
                    ? MetaProgressLoadStatus.Loaded
                    : MetaProgressLoadStatus.Migrated;
                return true;
            }
            catch (Exception exception)
            {
                state = null;
                status = MetaProgressLoadStatus.Corrupt;
                error = exception.Message;
                return false;
            }
        }

        internal static bool IsSupportedVersion(int version)
        {
            return version >= MetaProgressState.MinimumSupportedVersion
                && version <= MetaProgressState.CurrentVersion;
        }

        private static bool TryUpgradeToCurrent(MetaProgressState state, out string error)
        {
            error = null;
            if (state == null || !IsSupportedVersion(state.Version))
            {
                error = state == null
                    ? "Meta state null."
                    : $"Desteklenmeyen meta schema v{state.Version}.";
                return false;
            }

            if (state.Version == 1)
            {
                // v1 Souls/istatistik/upgrades tasiyordu; death receipt gecmisi yoktu.
                state.RewardedRunIds = new List<string>();
                state.Version = 2;
            }

            if (state.Version == 2)
            {
                // v2 idempotent death receipt gecmisini tasiyordu fakat future pool ve
                // onboarding state'inin canonical sahipleri yoktu.
                state.UnlockedPoolIds = new List<string>();
                state.TutorialFlags = new List<string>();
                state.Version = 3;
            }

            return state.Version == MetaProgressState.CurrentVersion;
        }

        private static MetaProgressState CreateDefaultState()
        {
            var state = new MetaProgressState();
            NormalizeState(state);
            return state;
        }

        private static void NormalizeState(MetaProgressState state)
        {
            if (state == null)
                return;

            state.Version = MetaProgressState.CurrentVersion;
            state.Souls = Mathf.Max(0, state.Souls);
            state.TotalSoulsEarned = Mathf.Max(0, state.TotalSoulsEarned);
            state.BestDay = Mathf.Max(0, state.BestDay);
            state.TotalRuns = Mathf.Max(0, state.TotalRuns);
            state.TotalKillsAllTime = Math.Max(0L, state.TotalKillsAllTime);
            state.Upgrades = NormalizeUpgradeLevels(state.Upgrades);
            state.UnlockedPoolIds = NormalizeIds(state.UnlockedPoolIds, 0);
            state.TutorialFlags = NormalizeIds(state.TutorialFlags, 0);
            state.RewardedRunIds = NormalizeIds(state.RewardedRunIds, MaxRewardReceipts);
        }

        private static List<MetaUpgradeLevel> NormalizeUpgradeLevels(List<MetaUpgradeLevel> source)
        {
            var normalized = new List<MetaUpgradeLevel>();
            var byId = new Dictionary<string, MetaUpgradeLevel>(StringComparer.Ordinal);
            if (source == null)
                return normalized;

            foreach (var entry in source)
            {
                string id = entry?.Id?.Trim();
                int level = entry != null ? Mathf.Max(0, entry.Level) : 0;
                if (string.IsNullOrEmpty(id) || level == 0)
                    continue;

                if (byId.TryGetValue(id, out MetaUpgradeLevel existing))
                {
                    existing.Level = Mathf.Max(existing.Level, level);
                    continue;
                }

                var copy = new MetaUpgradeLevel { Id = id, Level = level };
                byId.Add(id, copy);
                normalized.Add(copy);
            }

            return normalized;
        }

        private static List<string> NormalizeIds(List<string> source, int maxCount)
        {
            var reversed = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (source != null)
            {
                for (int i = source.Count - 1; i >= 0; i--)
                {
                    string id = source[i]?.Trim();
                    if (!string.IsNullOrEmpty(id) && seen.Add(id))
                        reversed.Add(id);
                }
            }

            reversed.Reverse();
            if (maxCount > 0 && reversed.Count > maxCount)
                reversed.RemoveRange(0, reversed.Count - maxCount);
            return reversed;
        }

        [Serializable]
        private class MetaVersionHeader
        {
            public int Version;
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
            MetaRewardQuote legacyQuote = MetaRewardCalculator.CalculateLegacy(
                day, kills, s.BestDay);
            var result = ApplyRunResult(s, runId, legacyQuote);
            // Duplicate in-memory receipt, onceki Save basarisiz oldugu icin olusmus olabilir.
            // Bu nedenle AlreadyRewarded olsa bile state tekrar durable yazilir.
            result.Persisted = Save();
            return result;
        }

        public static MetaRunResult AddRunResult(string runId, MetaRewardQuote reward)
        {
            var s = State;
            if (!TryApplyRunResult(s, runId, reward, out MetaRunResult result, out string error))
            {
                Debug.LogError($"[MetaProgression] Quoted death reward reddedildi: {error}");
                return result;
            }

            // Quote death receipt icinde durable oldugu icin Save basarisizsa ayni exact
            // sonuc sonraki process acilisinda yeniden denenebilir.
            result.Persisted = Save();
            return result;
        }

        internal static MetaRunResult ApplyRunResult(MetaProgressState s, string runId, int day, int kills)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            MetaRewardQuote legacyQuote = MetaRewardCalculator.CalculateLegacy(
                day, kills, s.BestDay);
            return ApplyRunResult(s, runId, legacyQuote);
        }

        internal static MetaRunResult ApplyRunResult(
            MetaProgressState s,
            string runId,
            MetaRewardQuote reward)
        {
            if (!TryApplyRunResult(s, runId, reward, out MetaRunResult result, out string error))
                throw new ArgumentException(error, nameof(reward));
            return result;
        }

        private static bool TryApplyRunResult(
            MetaProgressState s,
            string runId,
            MetaRewardQuote reward,
            out MetaRunResult result,
            out string error)
        {
            result = default;
            error = null;
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            s.RewardedRunIds ??= new List<string>();
            if (string.IsNullOrEmpty(runId) || s.RewardedRunIds.Contains(runId))
            {
                result = new MetaRunResult
                {
                    Day = reward.Day,
                    Kills = reward.Kills,
                    SoulsEarned = 0,
                    NewRecord = false,
                    AlreadyRewarded = true,
                    Reward = reward
                };
                return true;
            }

            if (!MetaRewardCalculator.IsStructurallyValid(reward))
            {
                error = "Reward quote yapisal olarak gecersiz.";
                result = BuildRejectedRunResult(reward);
                return false;
            }

            int currentBestDay = Mathf.Max(0, s.BestDay);
            if (reward.PreviousBestDay != currentBestDay
                || reward.NewRecord != (reward.Day > currentBestDay))
            {
                error = $"Reward quote record snapshot'i meta state ile uyusmuyor "
                        + $"(quote best={reward.PreviousBestDay}, state best={currentBestDay}).";
                result = BuildRejectedRunResult(reward);
                return false;
            }

            int earned = reward.TotalSouls;
            s.Souls = SaturatingAddNonNegative(s.Souls, earned);
            s.TotalSoulsEarned = SaturatingAddNonNegative(s.TotalSoulsEarned, earned);
            s.TotalKillsAllTime = s.TotalKillsAllTime > long.MaxValue - reward.Kills
                ? long.MaxValue
                : s.TotalKillsAllTime + reward.Kills;
            s.TotalRuns = SaturatingAddNonNegative(s.TotalRuns, 1);
            if (reward.NewRecord)
                s.BestDay = reward.Day;

            s.RewardedRunIds.Add(runId);
            const int MaxRewardReceipts = 128;
            if (s.RewardedRunIds.Count > MaxRewardReceipts)
                s.RewardedRunIds.RemoveRange(0, s.RewardedRunIds.Count - MaxRewardReceipts);

            result = new MetaRunResult
            {
                Day = reward.Day,
                Kills = reward.Kills,
                SoulsEarned = earned,
                NewRecord = reward.NewRecord,
                AlreadyRewarded = false,
                Reward = reward
            };
            return true;
        }

        private static MetaRunResult BuildRejectedRunResult(MetaRewardQuote reward)
        {
            return new MetaRunResult
            {
                Day = reward.Day,
                Kills = reward.Kills,
                SoulsEarned = 0,
                NewRecord = false,
                AlreadyRewarded = false,
                Reward = reward,
                Persisted = false
            };
        }

        private static int SaturatingAddNonNegative(int current, int amount)
        {
            int safeCurrent = Mathf.Max(0, current);
            int safeAmount = Mathf.Max(0, amount);
            return safeCurrent > int.MaxValue - safeAmount
                ? int.MaxValue
                : safeCurrent + safeAmount;
        }

        public static int GetUpgradeLevel(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;

            foreach (var u in State.Upgrades)
            {
                if (u != null && string.Equals(u.Id, id, StringComparison.Ordinal))
                    return u.Level;
            }

            return 0;
        }

        public static bool HasPoolUnlock(string poolId)
        {
            return ContainsId(State.UnlockedPoolIds, poolId);
        }

        public static bool TryUnlockPoolContent(string poolId)
        {
            string id = poolId?.Trim();
            if (string.IsNullOrEmpty(id) || !CanPersist)
                return false;
            if (ContainsId(State.UnlockedPoolIds, id))
                return true;

            State.UnlockedPoolIds.Add(id);
            if (Save())
                return true;

            State.UnlockedPoolIds.Remove(id);
            return false;
        }

        public static bool HasTutorialFlag(string flagId)
        {
            return ContainsId(State.TutorialFlags, flagId);
        }

        public static bool SetTutorialFlag(string flagId, bool enabled)
        {
            string id = flagId?.Trim();
            if (string.IsNullOrEmpty(id) || !CanPersist)
                return false;

            bool current = ContainsId(State.TutorialFlags, id);
            if (current == enabled)
                return true;

            if (enabled)
                State.TutorialFlags.Add(id);
            else
                State.TutorialFlags.Remove(id);

            if (Save())
                return true;

            if (enabled)
                State.TutorialFlags.Remove(id);
            else
                State.TutorialFlags.Add(id);
            return false;
        }

        /// <summary>
        /// Verilen tutorial flag grubunu tek durable save icinde temizler. Save basarisizsa
        /// onceki listeyi geri yukler; diger tutorial/content/meta state'ine dokunmaz.
        /// </summary>
        public static bool ResetTutorialFlags(IEnumerable<string> flagIds)
        {
            if (flagIds == null)
                return false;

            MetaProgressState state = State;
            if (!CanPersist || state?.TutorialFlags == null)
                return false;

            var normalizedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string flagId in flagIds)
            {
                string id = flagId?.Trim();
                if (!string.IsNullOrEmpty(id))
                    normalizedIds.Add(id);
            }

            if (normalizedIds.Count == 0)
                return false;

            var previousFlags = new List<string>(state.TutorialFlags);
            int removedCount = state.TutorialFlags.RemoveAll(
                flag => !string.IsNullOrWhiteSpace(flag)
                    && normalizedIds.Contains(flag.Trim()));
            if (removedCount == 0)
                return true;

            if (Save())
                return true;

            state.TutorialFlags = previousFlags;
            return false;
        }

        private static bool ContainsId(List<string> source, string id)
        {
            if (source == null || string.IsNullOrWhiteSpace(id))
                return false;

            string normalized = id.Trim();
            foreach (string candidate in source)
            {
                if (string.Equals(candidate, normalized, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Satin alma: bakiye/cap/content kontrolu tek disk transaction'inda commit edilir.
        /// Node pool unlock, upgrade seviyesiyle ayni atomik Save icinde yazilir.
        /// </summary>
        internal static bool TryBuyUpgrade(MetaUpgradeSO upgrade)
        {
            if (upgrade == null
                || string.IsNullOrWhiteSpace(upgrade.Id)
                || !upgrade.IsConfigurationValid()
                || !CanPersist)
                return false;

            int level = GetUpgradeLevel(upgrade.Id);
            if (upgrade.IsMaxLevel(level))
                return false;

            bool unlocksContent = MetaUpgradePolicy.IsContentUnlockEffect(upgrade.EffectType);
            string poolId = unlocksContent ? upgrade.PoolContentId.Trim() : null;
            if (unlocksContent && ContainsId(State.UnlockedPoolIds, poolId))
                return false;

            int cost = upgrade.GetCost(level);
            if (State.Souls < cost)
                return false;

            int previousSouls = State.Souls;
            State.Souls -= cost;
            SetUpgradeLevel(upgrade.Id, level + 1);
            if (unlocksContent)
                State.UnlockedPoolIds.Add(poolId);
            if (Save())
                return true;

            // Disk transaction basarisizsa Souls, seviye ve pool unlock birlikte geri alinir.
            State.Souls = previousSouls;
            SetUpgradeLevel(upgrade.Id, level);
            if (unlocksContent)
                State.UnlockedPoolIds.Remove(poolId);
            return false;
        }

        private static void SetUpgradeLevel(string id, int level)
        {
            for (int i = State.Upgrades.Count - 1; i >= 0; i--)
            {
                var u = State.Upgrades[i];
                if (u != null && string.Equals(u.Id, id, StringComparison.Ordinal))
                {
                    if (level <= 0)
                        State.Upgrades.RemoveAt(i);
                    else
                        u.Level = level;
                    return;
                }
            }

            if (level > 0)
                State.Upgrades.Add(new MetaUpgradeLevel { Id = id, Level = level });
        }

        /// <summary>Test/debug: tum meta ilerlemeyi siler (oyuncu-yuzeyinde KULLANILMAZ).</summary>
        public static void ResetAll()
        {
            _persistenceBlocked = false;
            LoadStatus = MetaProgressLoadStatus.CreatedNew;
            _state = CreateDefaultState();
            Save();
        }
    }
}
