using UnityEngine;

namespace DeadWalls
{
    public partial class GameManager
    {
        public CouncilRuntimeTuningTelemetry GetCouncilRuntimeTuningTelemetry()
        {
            CouncilEventCatalogSO catalog = councilCatalog;
            string catalogProblem = "Production CouncilEventCatalogSO atanmamis.";
            bool catalogValid = catalog != null
                                && catalog.TryValidateRuntimeContent(out catalogProblem);

            ContinuousSiegeCycleData cycle = ContinuousSiegeCycle;
            ComposedCouncilEvent active = _activeCouncilEvent;
            MobileEconomyEventState timedEffects = EconomyEvent;
            return new CouncilRuntimeTuningTelemetry
            {
                HasCatalog = catalog != null,
                CatalogValid = catalogValid,
                CatalogProblem = catalogValid ? string.Empty : catalogProblem,
                TemplateCount = catalog?.Templates?.Length ?? 0,
                AtomCount = catalog?.Atoms?.Length ?? 0,
                CuratedChainCount = catalog?.CuratedChains?.Length ?? 0,
                RecentTemplateMemory = Mathf.Max(1, catalog?.RecentTemplateMemory ?? 1),
                RecentTemplateCount = _recentCouncilTemplates.Count,
                FlagCount = _councilFlags.Count,
                UsedOneShotCount = _usedOneShotCouncils.Count,
                LastHandledRegularDay = _lastRegularCouncilDay,
                HasActiveEvent = active != null,
                ActiveTemplateId = active?.TemplateId ?? string.Empty,
                OptionABudgetMinutes = active?.OptionA?.BudgetMinutes ?? 0f,
                OptionBBudgetMinutes = active?.OptionB?.BudgetMinutes ?? 0f,
                CurrentDay = Mathf.Max(1, cycle.CycleIndex + 1),
                Phase = cycle.Phase,
                TotalDecisionSeconds = CouncilDecisionWindowUtility.GetTotalWindowSeconds(cycle),
                RemainingDecisionSeconds = active == null
                    ? 0f
                    : CouncilDecisionWindowUtility.GetRemainingSeconds(cycle),
                ProductionModifierResource = timedEffects.ProductionBonusResource,
                ProductionModifierMultiplier = timedEffects.ProductionBonusMultiplier,
                ProductionModifierExpiresAfterWave = timedEffects.ProductionBonusExpiresAfterWave,
                NextNightSpawnMultiplier = timedEffects.NextNightSpawnMultiplier,
                NightSpawnExpiresAfterWave = timedEffects.NightSpawnExpiresAfterWave,
            };
        }

        private void TrimCouncilRecentTemplatesToCatalogMemory()
        {
            CouncilRecentTemplateMemory.TrimInPlace(
                _recentCouncilTemplates,
                councilCatalog != null ? councilCatalog.RecentTemplateMemory : 1);
        }
    }
}
