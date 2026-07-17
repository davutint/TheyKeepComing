using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public partial class GameManager : IHeartEffectBaselineProvider, IHeartRuntimeEffectSink,
        IHeartScreenRuntime
    {
        [Header("Castle Heart V1")]
        [SerializeField] private HeartNodeCatalogSO heartCatalog;
        [SerializeField] private HeartGraphRuntimeSettings heartGraphSettings = new HeartGraphRuntimeSettings();

        private readonly Dictionary<HeartEffectTargetKey, HeartEffectBaseline> _heartBaselines =
            new Dictionary<HeartEffectTargetKey, HeartEffectBaseline>();
        private readonly Dictionary<HeartEffectTargetKey, double> _heartActualValues =
            new Dictionary<HeartEffectTargetKey, double>();
        private readonly HashSet<HeartEffectTargetKey> _heartBehaviors =
            new HashSet<HeartEffectTargetKey>();

        private GeneratedRunGraph _generatedHeartGraph;
        private HeartEffectPipeline _heartEffectPipeline;
        private bool _heartRuntimeAttempted;
        private bool _heartRuntimeRestoreInProgress;
        private string _heartRuntimeError = string.Empty;

        public string HeartRuntimeError => _heartRuntimeError;
        public bool IsHeartRuntimeReady => _generatedHeartGraph != null
                                           && _heartEffectPipeline != null
                                           && heartCatalog != null;
        public HeartNodeCatalogSO HeartCatalog => heartCatalog;
        public HeartGraphRuntimeSettings GetHeartGraphSettingsSnapshot()
        {
            return (heartGraphSettings ?? new HeartGraphRuntimeSettings()).Clone();
        }

        public HeartRuntimeTuningTelemetry GetHeartRuntimeTuningTelemetry()
        {
            int revealedNodeCount = 0;
            int purchasedNodeCount = 0;
            int lockedNodeCount = 0;
            List<GeneratedHeartNodeState> nodes = _generatedHeartGraph?.Nodes;
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    GeneratedHeartNodeState node = nodes[i];
                    if (node == null)
                        continue;
                    if (node.Visibility == HeartNodeVisibility.Revealed)
                        revealedNodeCount++;
                    if (node.Level > 0)
                        purchasedNodeCount++;
                    if (node.LockState != HeartNodeLockState.Available)
                        lockedNodeCount++;
                }
            }

            GraveEssence essence = TryGetGraveEssence(out _, out GraveEssence currentEssence)
                ? currentEssence
                : HeartEssence;
            double gainPercent = double.IsFinite(_metaEssenceGainPercent)
                ? Math.Max(0d, _metaEssenceGainPercent)
                : 0d;
            double gainAccumulator = double.IsFinite(essence.MetaGainAccumulator)
                ? Math.Max(0d, essence.MetaGainAccumulator)
                : 0d;
            string runtimeError = _heartRuntimeError;
            if (string.IsNullOrWhiteSpace(runtimeError) && heartCatalog == null)
                runtimeError = "Production HeartNodeCatalogSO atanmamis.";

            return new HeartRuntimeTuningTelemetry(
                heartCatalog != null,
                _heartRuntimeAttempted,
                IsHeartRuntimeReady,
                runtimeError,
                Math.Max(0L, essence.Current),
                gainPercent,
                gainAccumulator,
                _generatedHeartGraph?.GraphVersion ?? 0,
                _generatedHeartGraph?.CatalogVersion ?? heartCatalog?.CatalogVersion ?? 0,
                _generatedHeartGraph?.Seed ?? 0u,
                nodes?.Count ?? 0,
                _generatedHeartGraph?.Edges?.Count ?? 0,
                revealedNodeCount,
                purchasedNodeCount,
                lockedNodeCount);
        }

        public bool HeartSplitShotEnabled => IsHeartBehaviorEnabled(HeartNodeEffectType.EnableSplitShot);
        public bool HeartBurningGroundEnabled => IsHeartBehaviorEnabled(HeartNodeEffectType.EnableBurningGround);
        public bool HeartSecondBlastEnabled => IsHeartBehaviorEnabled(HeartNodeEffectType.EnableSecondBlast);

        private bool EnsureHeartRuntime()
        {
            if (IsHeartRuntimeReady)
                return true;
            if (_heartRuntimeAttempted)
                return false;

            _heartRuntimeAttempted = true;
            _heartRuntimeError = string.Empty;
            if (heartCatalog == null)
            {
                _heartRuntimeError = "Production HeartNodeCatalogSO atanmamis.";
                return false;
            }

            HeartGraphRuntimeSettings settings = heartGraphSettings ?? new HeartGraphRuntimeSettings();
            if (!HeartGraphGenerator.TryGenerate(
                    settings.CreateRequest(heartCatalog, ResolveHeartRunSeed()),
                    out _generatedHeartGraph,
                    out HeartGraphGenerationReport report))
            {
                _heartRuntimeError = report.Errors.Count > 0
                    ? string.Join(" | ", report.Errors)
                    : "Castle Heart graph uretilemedi.";
                _generatedHeartGraph = null;
                return false;
            }

            HeartGraphRevealResult reveal = HeartGraphRevealService.InitializeRunVisibility(
                _generatedHeartGraph);
            if (!reveal.Succeeded)
            {
                _heartRuntimeError = string.Join(" | ", reveal.Errors);
                _generatedHeartGraph = null;
                return false;
            }

            _heartEffectPipeline = new HeartEffectPipeline(this, this);
            return true;
        }

        public bool TryBuildHeartPresentation(
            out HeartGraphPresentation presentation,
            out IReadOnlyList<string> errors)
        {
            if (!EnsureHeartRuntime())
            {
                presentation = new HeartGraphPresentation();
                errors = new[]
                {
                    string.IsNullOrWhiteSpace(_heartRuntimeError)
                        ? "Castle Heart runtime hazir degil."
                        : _heartRuntimeError
                };
                return false;
            }

            bool succeeded = HeartGraphPresentationBuilder.TryBuild(
                _generatedHeartGraph,
                heartCatalog,
                _heartEffectPipeline,
                out presentation,
                out List<string> buildErrors);
            errors = buildErrors;
            if (!succeeded)
                _heartRuntimeError = string.Join(" | ", buildErrors);
            return succeeded;
        }

        public HeartPurchaseEvaluation EvaluateHeartPurchase(
            string nodeId,
            HeartPurchaseQuantity quantity)
        {
            if (!EnsureHeartRuntime())
            {
                return new HeartPurchaseEvaluation
                {
                    FailureReason = HeartPurchaseFailureReason.InvalidRequest,
                    Message = _heartRuntimeError
                };
            }

            return HeartPurchaseService.Evaluate(
                _generatedHeartGraph,
                heartCatalog,
                nodeId,
                quantity,
                GraveEssenceAmount);
        }

        public HeartPurchaseResult TryPurchaseHeartNode(
            string nodeId,
            HeartPurchaseQuantity quantity)
        {
            if (!EnsureHeartRuntime())
            {
                return new HeartPurchaseResult
                {
                    FailureReason = HeartPurchaseFailureReason.InvalidRequest,
                    Message = _heartRuntimeError
                };
            }

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                _generatedHeartGraph,
                heartCatalog,
                nodeId,
                quantity,
                this,
                _heartEffectPipeline);
            if (result.Succeeded)
            {
                TryEmitResourceSpentTelemetry(
                    ResourceSpentTelemetryContract.GraveEssence,
                    result.Quote.TotalGraveEssenceCost,
                    ResourceSpentTelemetryContract.HeartNode,
                    result.Quote.NewLevel,
                    0);
                OnGameStateChanged?.Invoke();
            }
            return result;
        }

        public bool TryGetBaseline(
            HeartEffectTargetKey target,
            out HeartEffectBaseline baseline)
        {
            if (_heartBaselines.TryGetValue(target, out baseline))
                return true;
            if (!TryCreateHeartBaseline(target, out baseline))
                return false;

            _heartBaselines[target] = baseline;
            return true;
        }

        public void ApplyNumericEffect(HeartEffectTargetKey target, double actualValue)
        {
            if (!double.IsFinite(actualValue) || actualValue < 0d
                || !_heartBaselines.ContainsKey(target))
            {
                return;
            }

            _heartActualValues[target] = actualValue;
            switch (target.Type)
            {
                case HeartNodeEffectType.ModifyArcherDamagePercent:
                case HeartNodeEffectType.ModifyArcherFireRatePercent:
                case HeartNodeEffectType.AddArcherRange:
                case HeartNodeEffectType.ReduceFrostSlowMultiplier:
                    ApplyScaledStatsToArchers(target.ArcherType, false);
                    break;

                case HeartNodeEffectType.ModifyWallMaxHpPercent:
                    ApplyTechDefenseAggregates();
                    break;

                case HeartNodeEffectType.IncreaseWorkerCapacity:
                case HeartNodeEffectType.IncreaseResourceProductionPercent:
                case HeartNodeEffectType.IncreasePopulationGrowth:
                    ApplyTechEconomyAggregates();
                    break;

                case HeartNodeEffectType.IncreaseArrowCapacity:
                case HeartNodeEffectType.IncreaseArrowEfficiency:
                    ApplyHeartArrowEffect(target, actualValue);
                    break;
            }
        }

        public void EnableBehaviorEffect(HeartNodeEffect effect)
        {
            if (!HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out _)
                || !_heartBehaviors.Add(target))
            {
                return;
            }

            switch (effect.Type)
            {
                case HeartNodeEffectType.UnlockArcherType:
                    if (effect.ArcherType != ArcherType.Basic)
                        _unlockedArcherTypes.Add(effect.ArcherType);
                    break;
                case HeartNodeEffectType.UnlockSpellcasting:
                    _fireballUnlocked = true;
                    break;
            }
        }

        private bool TryCreateHeartBaseline(
            HeartEffectTargetKey target,
            out HeartEffectBaseline baseline)
        {
            baseline = default;
            switch (target.Type)
            {
                case HeartNodeEffectType.ModifyArcherDamagePercent:
                    baseline = new HeartEffectBaseline(
                        $"{target.ArcherType} Damage",
                        GetHeartFreeScaledArcherStats(target.ArcherType).Damage,
                        string.Empty,
                        2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.ModifyArcherFireRatePercent:
                    baseline = new HeartEffectBaseline(
                        $"{target.ArcherType} Fire Rate",
                        GetHeartFreeScaledArcherStats(target.ArcherType).FireRate,
                        "/s",
                        2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.AddArcherRange:
                    baseline = new HeartEffectBaseline(
                        $"{target.ArcherType} Range",
                        GetHeartFreeScaledArcherStats(target.ArcherType).Range,
                        string.Empty,
                        2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.ReduceFrostSlowMultiplier:
                    if (target.ArcherType != ArcherType.Frost)
                        return false;
                    baseline = new HeartEffectBaseline(
                        "Frost Move Speed",
                        GetHeartFreeScaledArcherStats(ArcherType.Frost).SlowMultiplier,
                        string.Empty,
                        1,
                        true);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.ModifyWallMaxHpPercent:
                    if (!CanAccessEntityManager() || !_entityManager.Exists(_castleEntity))
                        return false;
                    baseline = new HeartEffectBaseline(
                        "Wall Max HP",
                        _entityManager.GetComponentData<WallSegment>(_castleEntity).MaxHP,
                        string.Empty,
                        0);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.ReduceWallRepairCostPercent:
                    baseline = new HeartEffectBaseline(
                        "Repair Cost",
                        Math.Max(0.05d, _techRepairCostMultiplier),
                        string.Empty,
                        1,
                        true);
                    return true;

                case HeartNodeEffectType.IncreaseWorkerCapacity:
                    if (!TryGetHeartEconomyValue(target, true, out double capacity))
                        return false;
                    baseline = new HeartEffectBaseline(
                        $"{target.Resource} Worker Capacity", capacity, string.Empty, 0);
                    return true;

                case HeartNodeEffectType.IncreaseResourceProductionPercent:
                    if (!TryGetHeartEconomyValue(target, false, out double production))
                        return false;
                    baseline = new HeartEffectBaseline(
                        $"{target.Resource} / Worker", production, "/min", 2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.IncreasePopulationGrowth:
                    if (!TryGetMobileCombatConfig(out MobileCastleCombatConfig config))
                        return false;
                    baseline = new HeartEffectBaseline(
                        "Dawn Population", config.PopulationGrowthPerDayPrep, string.Empty, 0);
                    return true;

                case HeartNodeEffectType.IncreaseArrowCapacity:
                    baseline = new HeartEffectBaseline(
                        "Arrow Capacity", GetHeartFreeArrowCapacity(), string.Empty, 0);
                    return true;

                case HeartNodeEffectType.IncreaseArrowEfficiency:
                    baseline = new HeartEffectBaseline(
                        "Arrows / Wood", GetHeartFreeArrowEfficiency(), string.Empty, 0);
                    return true;

                case HeartNodeEffectType.ModifySpellDamagePercent:
                    baseline = new HeartEffectBaseline(
                        "Fireball Damage", FireballBaseDamage * _spellDamageMultiplier,
                        string.Empty, 2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.AddSpellRadius:
                    baseline = new HeartEffectBaseline(
                        "Fireball Radius", FireballBaseRadius + _spellRadiusBonus,
                        string.Empty, 2);
                    return baseline.Value > 0d;

                case HeartNodeEffectType.ReduceSpellCooldownPercent:
                    baseline = new HeartEffectBaseline(
                        "Fireball Cooldown", FireballBaseCooldown * _spellCooldownMultiplier,
                        "s", 2);
                    return baseline.Value > 0d;

                default:
                    return false;
            }
        }

        private bool TryGetHeartEconomyValue(
            HeartEffectTargetKey target,
            bool capacity,
            out double value)
        {
            value = 0d;
            if (target.Resource == EconomyFocusType.Balanced
                || !TryGetMobileCombatConfig(out MobileCastleCombatConfig config))
            {
                return false;
            }

            switch (target.Resource)
            {
                case EconomyFocusType.Wood:
                    value = capacity ? config.WoodWorkerCap : config.WoodWorkerProductionPerMin;
                    return true;
                case EconomyFocusType.Stone:
                    value = capacity ? config.StoneWorkerCap : config.StoneWorkerProductionPerMin;
                    return true;
                case EconomyFocusType.Iron:
                    value = capacity ? config.IronWorkerCap : config.IronWorkerProductionPerMin;
                    return true;
                case EconomyFocusType.Food:
                    value = capacity ? config.FoodWorkerCap : config.FoodWorkerProductionPerMin;
                    return true;
                default:
                    return false;
            }
        }

        private void ApplyHeartArrowEffect(HeartEffectTargetKey target, double actualValue)
        {
            if (!TryGetArrowSupply(out Unity.Entities.Entity entity, out ArrowSupply supply)
                || !_heartBaselines.TryGetValue(target, out HeartEffectBaseline baseline))
            {
                return;
            }

            long bonus = (long)Math.Round(
                Math.Max(0d, actualValue - baseline.Value),
                MidpointRounding.AwayFromZero);
            int safeBonus = bonus >= int.MaxValue ? int.MaxValue : (int)bonus;
            if (target.Type == HeartNodeEffectType.IncreaseArrowCapacity)
                supply.HeartCapacityBonus = safeBonus;
            else
                supply.HeartEfficiencyBonus = safeBonus;

            if (!_heartRuntimeRestoreInProgress)
            {
                int capacity = ArrowEconomyUtility.GetCapacity(supply, GetEconomyPriceTuning());
                supply.Current = math.clamp(supply.Current, 0, capacity);
            }
            _entityManager.SetComponentData(entity, supply);
            ArrowSupply = supply;
        }

        private int GetHeartFreeArrowCapacity()
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out ArrowSupply current)
                ? current
                : ArrowSupply;
            supply.HeartCapacityBonus = 0;
            return ArrowEconomyUtility.GetCapacity(supply, GetEconomyPriceTuning());
        }

        private int GetHeartFreeArrowEfficiency()
        {
            ArrowSupply supply = TryGetArrowSupply(out _, out ArrowSupply current)
                ? current
                : ArrowSupply;
            supply.HeartEfficiencyBonus = 0;
            return ArrowEconomyUtility.GetArrowsPerWood(supply, GetEconomyPriceTuning());
        }

        private uint ResolveHeartRunSeed()
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offset;
            string value = string.IsNullOrWhiteSpace(_currentRunId)
                ? "deadwalls-heart"
                : _currentRunId;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= prime;
            }
            return hash == 0u ? 1u : hash;
        }

        private bool IsHeartBehaviorEnabled(HeartNodeEffectType type)
        {
            foreach (HeartEffectTargetKey target in _heartBehaviors)
            {
                if (target.Type == type)
                    return true;
            }
            return false;
        }

        private bool TryCaptureHeartGraphForSave(
            out GeneratedRunGraph graphSnapshot,
            out string error)
        {
            graphSnapshot = null;
            error = string.Empty;
            if (heartCatalog == null)
                return true;
            if (!EnsureHeartRuntime())
            {
                error = string.IsNullOrWhiteSpace(_heartRuntimeError)
                    ? "Castle Heart runtime save icin hazirlanamadi."
                    : _heartRuntimeError;
                return false;
            }
            if (!HeartGraphPersistenceUtility.TryValidateForRestore(
                    _generatedHeartGraph,
                    heartCatalog,
                    out List<string> validationErrors))
            {
                error = string.Join(" | ", validationErrors);
                return false;
            }

            graphSnapshot = HeartGraphPersistenceUtility.CloneExact(_generatedHeartGraph);
            return true;
        }

        private bool TryValidateSavedHeartGraphForRestore(
            GeneratedRunGraph savedGraph,
            out string error)
        {
            error = string.Empty;
            if (savedGraph == null)
                return true;
            if (!HeartGraphPersistenceUtility.TryValidateForRestore(
                    savedGraph,
                    heartCatalog,
                    out List<string> validationErrors))
            {
                error = string.Join(" | ", validationErrors);
                return false;
            }
            return true;
        }

        private bool TryRestoreHeartRuntime(
            GeneratedRunGraph savedGraph,
            out string error)
        {
            ResetHeartRuntime();
            error = string.Empty;
            if (savedGraph == null)
            {
                _heartRuntimeAttempted = true;
                _heartRuntimeError = heartCatalog == null
                    ? "Production HeartNodeCatalogSO atanmamis."
                    : "Kayitli kosu exact Castle Heart graph'i tasimiyor; yeni graph uretilmedi.";
                return true;
            }

            GeneratedRunGraph restoredGraph = HeartGraphPersistenceUtility.CloneExact(savedGraph);
            _generatedHeartGraph = restoredGraph;
            _heartRuntimeRestoreInProgress = true;
            bool restored;
            HeartEffectPipeline restoredPipeline;
            try
            {
                restored = HeartGraphPersistenceUtility.TryCreateRestoredPipeline(
                    restoredGraph,
                    heartCatalog,
                    this,
                    this,
                    out restoredPipeline,
                    out error);
            }
            finally
            {
                _heartRuntimeRestoreInProgress = false;
            }

            if (!restored)
            {
                ResetHeartRuntime();
                _heartRuntimeAttempted = true;
                _heartRuntimeError = error;
                return false;
            }

            _heartEffectPipeline = restoredPipeline;
            _heartRuntimeAttempted = true;
            _heartRuntimeError = string.Empty;
            return true;
        }

        private void ClampRestoredArrowSupplyToEffectiveCapacity()
        {
            if (!TryGetArrowSupply(out Unity.Entities.Entity entity, out ArrowSupply supply))
                return;

            int capacity = ArrowEconomyUtility.GetCapacity(supply, GetEconomyPriceTuning());
            supply.Current = math.clamp(supply.Current, 0, capacity);
            _entityManager.SetComponentData(entity, supply);
            ArrowSupply = supply;
        }

        private void ResetHeartRuntime()
        {
            _generatedHeartGraph = null;
            _heartEffectPipeline = null;
            _heartRuntimeAttempted = false;
            _heartRuntimeRestoreInProgress = false;
            _heartRuntimeError = string.Empty;
            _heartBaselines.Clear();
            _heartActualValues.Clear();
            _heartBehaviors.Clear();
        }

        private void ApplyHeartArcherEffects(ArcherType type, ref ArcherStats stats)
        {
            ApplyHeartRatio(
                new HeartEffectTargetKey(HeartNodeEffectType.ModifyArcherDamagePercent, type, default),
                ref stats.Damage);
            ApplyHeartRatio(
                new HeartEffectTargetKey(HeartNodeEffectType.ModifyArcherFireRatePercent, type, default),
                ref stats.FireRate);
            ApplyHeartAdditive(
                new HeartEffectTargetKey(HeartNodeEffectType.AddArcherRange, type, default),
                ref stats.Range);
            if (type == ArcherType.Frost)
            {
                ApplyHeartRatio(
                    new HeartEffectTargetKey(
                        HeartNodeEffectType.ReduceFrostSlowMultiplier,
                        ArcherType.Frost,
                        default),
                    ref stats.SlowMultiplier);
            }
        }

        private void ApplyHeartEconomyOverrides(ref MobileCastleCombatConfig config)
        {
            ApplyHeartCapacity(EconomyFocusType.Wood, ref config.WoodWorkerCap);
            ApplyHeartCapacity(EconomyFocusType.Stone, ref config.StoneWorkerCap);
            ApplyHeartCapacity(EconomyFocusType.Iron, ref config.IronWorkerCap);
            ApplyHeartCapacity(EconomyFocusType.Food, ref config.FoodWorkerCap);
            ApplyHeartProduction(EconomyFocusType.Wood, ref config.WoodWorkerProductionPerMin);
            ApplyHeartProduction(EconomyFocusType.Stone, ref config.StoneWorkerProductionPerMin);
            ApplyHeartProduction(EconomyFocusType.Iron, ref config.IronWorkerProductionPerMin);
            ApplyHeartProduction(EconomyFocusType.Food, ref config.FoodWorkerProductionPerMin);

            var growthTarget = new HeartEffectTargetKey(
                HeartNodeEffectType.IncreasePopulationGrowth,
                default,
                default);
            if (TryGetHeartDelta(growthTarget, out double growthBonus))
                config.PopulationGrowthPerDayPrep = SaturatingAdd(config.PopulationGrowthPerDayPrep, growthBonus);
        }

        private float ApplyHeartWallMultiplier(float heartFreeValue)
        {
            var target = new HeartEffectTargetKey(
                HeartNodeEffectType.ModifyWallMaxHpPercent,
                default,
                default);
            return ApplyHeartRatio(target, heartFreeValue);
        }

        private float GetHeartRepairCostMultiplier()
        {
            var target = new HeartEffectTargetKey(
                HeartNodeEffectType.ReduceWallRepairCostPercent,
                default,
                default);
            return TryGetHeartRatio(target, out double ratio)
                ? (float)Math.Max(0d, ratio)
                : 1f;
        }

        private float GetHeartAdjustedSpellValue(HeartNodeEffectType type, float heartFreeValue)
        {
            var target = new HeartEffectTargetKey(type, default, default);
            return type == HeartNodeEffectType.AddSpellRadius
                ? ApplyHeartAdditive(target, heartFreeValue)
                : ApplyHeartRatio(target, heartFreeValue);
        }

        private void ApplyHeartCapacity(EconomyFocusType resource, ref int value)
        {
            var target = new HeartEffectTargetKey(
                HeartNodeEffectType.IncreaseWorkerCapacity,
                default,
                resource);
            if (TryGetHeartDelta(target, out double bonus))
                value = SaturatingAdd(value, bonus);
        }

        private void ApplyHeartProduction(EconomyFocusType resource, ref float value)
        {
            var target = new HeartEffectTargetKey(
                HeartNodeEffectType.IncreaseResourceProductionPercent,
                default,
                resource);
            value = ApplyHeartRatio(target, value);
        }

        private void ApplyHeartRatio(HeartEffectTargetKey target, ref float value)
        {
            value = ApplyHeartRatio(target, value);
        }

        private float ApplyHeartRatio(HeartEffectTargetKey target, float value)
        {
            if (!TryGetHeartRatio(target, out double ratio))
                return value;
            double adjusted = value * ratio;
            return double.IsFinite(adjusted)
                ? (float)Math.Min(float.MaxValue, Math.Max(0d, adjusted))
                : value;
        }

        private void ApplyHeartAdditive(HeartEffectTargetKey target, ref float value)
        {
            value = ApplyHeartAdditive(target, value);
        }

        private float ApplyHeartAdditive(HeartEffectTargetKey target, float value)
        {
            if (!TryGetHeartDelta(target, out double delta))
                return value;
            double adjusted = value + delta;
            return double.IsFinite(adjusted)
                ? (float)Math.Min(float.MaxValue, Math.Max(0d, adjusted))
                : value;
        }

        private bool TryGetHeartRatio(HeartEffectTargetKey target, out double ratio)
        {
            ratio = 1d;
            if (!_heartActualValues.TryGetValue(target, out double actual)
                || !_heartBaselines.TryGetValue(target, out HeartEffectBaseline baseline)
                || baseline.Value <= 0d)
            {
                return false;
            }
            ratio = actual / baseline.Value;
            return double.IsFinite(ratio) && ratio >= 0d;
        }

        private bool TryGetHeartDelta(HeartEffectTargetKey target, out double delta)
        {
            delta = 0d;
            if (!_heartActualValues.TryGetValue(target, out double actual)
                || !_heartBaselines.TryGetValue(target, out HeartEffectBaseline baseline))
            {
                return false;
            }
            delta = actual - baseline.Value;
            return double.IsFinite(delta);
        }

        private static int SaturatingAdd(int value, double bonus)
        {
            double total = value + Math.Round(bonus, MidpointRounding.AwayFromZero);
            if (total <= 0d)
                return 0;
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }
    }
}
