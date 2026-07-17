using System;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>Difficulty Tuner icin read-only aggregate Meta runtime snapshot'i.</summary>
    [Serializable]
    public struct MetaRuntimeTelemetry
    {
        public bool RuntimeInitialized;
        public int Souls;
        public int TotalSoulsEarned;
        public int BestDay;
        public int TotalRuns;
        public long TotalKillsAllTime;
        public int CurrentRunDay;
        public int CurrentRunKills;
        public int CurrentRunPeakPopulation;
        public float AppliedWallHpPercent;
        public float AppliedProductionPercent;
        public int AppliedArrowEfficiencyBonus;
        public double AppliedEssenceGainPercent;
        public bool HasCurrentRewardQuote;
        public MetaRewardQuote CurrentRewardQuote;
    }

    public partial class GameManager
    {
        public MetaRuntimeTelemetry GetMetaRuntimeTelemetry()
        {
            MetaProgressState state = MetaProgression.State;
            int day = Mathf.Max(1, ContinuousSiegeCycle.CycleIndex + 1);
            int kills = Mathf.Max(0, GameState.TotalKills);
            int peakPopulation = Mathf.Max(0, Population.Total);
            MetaRewardQuote quote = default;
            bool hasQuote = metaUpgradeCatalog != null
                            && MetaRewardCalculator.TryCalculate(
                                metaUpgradeCatalog.RewardSettings,
                                day,
                                kills,
                                peakPopulation,
                                state.BestDay,
                                out quote);

            return new MetaRuntimeTelemetry
            {
                RuntimeInitialized = _initialized,
                Souls = Mathf.Max(0, state.Souls),
                TotalSoulsEarned = Mathf.Max(0, state.TotalSoulsEarned),
                BestDay = Mathf.Max(0, state.BestDay),
                TotalRuns = Mathf.Max(0, state.TotalRuns),
                TotalKillsAllTime = Math.Max(0L, state.TotalKillsAllTime),
                CurrentRunDay = day,
                CurrentRunKills = kills,
                CurrentRunPeakPopulation = peakPopulation,
                AppliedWallHpPercent = Mathf.Max(0f, _metaWallHpPercent),
                AppliedProductionPercent = Mathf.Max(0f, _metaProductionPercent),
                AppliedArrowEfficiencyBonus = Mathf.Max(0, _metaArrowEfficiencyBonus),
                AppliedEssenceGainPercent = Math.Max(0d, _metaEssenceGainPercent),
                HasCurrentRewardQuote = hasQuote,
                CurrentRewardQuote = quote
            };
        }
    }
}
