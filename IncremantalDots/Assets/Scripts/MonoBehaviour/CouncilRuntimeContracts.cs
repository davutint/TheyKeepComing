using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>Recent Council listesi icin tek, allocation-free boyutlandirma kurali.</summary>
    public static class CouncilRecentTemplateMemory
    {
        public static void TrimInPlace(List<string> recentTemplateIds, int memoryLimit)
        {
            if (recentTemplateIds == null)
                return;

            int limit = Mathf.Max(1, memoryLimit);
            int overflow = recentTemplateIds.Count - limit;
            if (overflow > 0)
                recentTemplateIds.RemoveRange(0, overflow);
        }
    }

    /// <summary>
    /// Difficulty Tuner'in okudugu aggregate Council state'i. Gizli secenek/flag icerigi acmaz
    /// ve yeni bir runtime owner yaratmaz.
    /// </summary>
    public struct CouncilRuntimeTuningTelemetry
    {
        public bool HasCatalog;
        public bool CatalogValid;
        public string CatalogProblem;
        public int TemplateCount;
        public int AtomCount;
        public int CuratedChainCount;
        public int RecentTemplateMemory;
        public int RecentTemplateCount;
        public int FlagCount;
        public int UsedOneShotCount;
        public int LastHandledRegularDay;
        public bool HasActiveEvent;
        public string ActiveTemplateId;
        public float OptionABudgetMinutes;
        public float OptionBBudgetMinutes;
        public int CurrentDay;
        public SiegeCyclePhase Phase;
        public float TotalDecisionSeconds;
        public float RemainingDecisionSeconds;
        public EconomyFocusType ProductionModifierResource;
        public float ProductionModifierMultiplier;
        public int ProductionModifierExpiresAfterWave;
        public float NextNightSpawnMultiplier;
        public int NightSpawnExpiresAfterWave;
    }
}
