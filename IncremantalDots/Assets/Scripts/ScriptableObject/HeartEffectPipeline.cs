using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeadWalls
{
    public readonly struct HeartEffectTargetKey : IEquatable<HeartEffectTargetKey>
    {
        public readonly HeartNodeEffectType Type;
        public readonly ArcherType ArcherType;
        public readonly EconomyFocusType Resource;

        public HeartEffectTargetKey(
            HeartNodeEffectType type,
            ArcherType archerType,
            EconomyFocusType resource)
        {
            Type = type;
            ArcherType = archerType;
            Resource = resource;
        }

        public bool Equals(HeartEffectTargetKey other)
        {
            return Type == other.Type
                   && ArcherType == other.ArcherType
                   && Resource == other.Resource;
        }

        public override bool Equals(object obj)
        {
            return obj is HeartEffectTargetKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Type;
                hash = (hash * 397) ^ (int)ArcherType;
                hash = (hash * 397) ^ (int)Resource;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Type}/{ArcherType}/{Resource}";
        }
    }

    public readonly struct HeartEffectBaseline
    {
        public readonly string Label;
        public readonly double Value;
        public readonly string Suffix;
        public readonly int DecimalPlaces;
        public readonly bool DisplayAsPercent;

        public HeartEffectBaseline(
            string label,
            double value,
            string suffix = "",
            int decimalPlaces = 2,
            bool displayAsPercent = false)
        {
            Label = label ?? string.Empty;
            Value = value;
            Suffix = suffix ?? string.Empty;
            DecimalPlaces = Math.Max(0, Math.Min(4, decimalPlaces));
            DisplayAsPercent = displayAsPercent;
        }
    }

    /// <summary>
    /// Heart-free gercek runtime baseline'larini saglar. Provider, daha once uygulanmis
    /// Heart bonusunu baseline'a tekrar katmamalidir; aksi halde compound drift olusur.
    /// </summary>
    public interface IHeartEffectBaselineProvider
    {
        bool TryGetBaseline(
            HeartEffectTargetKey target,
            out HeartEffectBaseline baseline);
    }

    /// <summary>
    /// Hazirlanmis effect sonucu gercek runtime owner'larina iletilir. Sink metotlari
    /// validation yapmaz veya fail etmez; butun preflight TryPrepare sirasinda biter.
    /// </summary>
    public interface IHeartRuntimeEffectSink
    {
        void ApplyNumericEffect(HeartEffectTargetKey target, double actualValue);
        void EnableBehaviorEffect(HeartNodeEffect effect);
    }

    public interface IHeartPreparedEffectTransaction
    {
        void Commit();
    }

    public interface IHeartEffectTransactionPlanner
    {
        bool TryPrepare(
            HeartNodeDefinitionSO definition,
            int previousLevel,
            int newLevel,
            out IHeartPreparedEffectTransaction preparedTransaction,
            out string error);
    }

    /// <summary>
    /// Heart numeric/behavior effect'lerinin tek hesap owner'i. Ayni raw investment
    /// hem runtime sink'e hem player-facing current/after/delta resolver'ina gider.
    /// </summary>
    public sealed class HeartEffectPipeline : IHeartEffectTransactionPlanner, IHeartEffectValueResolver
    {
        private readonly IHeartEffectBaselineProvider _baselineProvider;
        private readonly IHeartRuntimeEffectSink _sink;
        private readonly Dictionary<HeartEffectTargetKey, double> _rawInvestments =
            new Dictionary<HeartEffectTargetKey, double>();
        private readonly Dictionary<HeartEffectTargetKey, HeartNodeEffect> _targetPolicies =
            new Dictionary<HeartEffectTargetKey, HeartNodeEffect>();
        private readonly Dictionary<string, int> _nodeLevels =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<HeartEffectTargetKey> _enabledBehaviors =
            new HashSet<HeartEffectTargetKey>();

        public HeartEffectPipeline(
            IHeartEffectBaselineProvider baselineProvider,
            IHeartRuntimeEffectSink sink = null)
        {
            _baselineProvider = baselineProvider;
            _sink = sink;
        }

        public bool TryPrepare(
            HeartNodeDefinitionSO definition,
            int previousLevel,
            int newLevel,
            out IHeartPreparedEffectTransaction preparedTransaction,
            out string error)
        {
            preparedTransaction = null;
            error = string.Empty;
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                error = "Heart effect definition gecersiz.";
                return false;
            }
            if (previousLevel < 0 || newLevel <= previousLevel)
            {
                error = $"Heart effect level gecisi gecersiz: {previousLevel} -> {newLevel}.";
                return false;
            }

            int trackedLevel = _nodeLevels.TryGetValue(definition.Id, out int current)
                ? current
                : 0;
            if (trackedLevel != previousLevel)
            {
                error = $"Heart effect state '{definition.Id}' icin graph ile senkron degil: "
                        + $"pipeline {trackedLevel}, graph {previousLevel}.";
                return false;
            }

            int addedLevels = newLevel - previousLevel;
            var nextRawValues = new Dictionary<HeartEffectTargetKey, double>();
            var nextPolicies = new Dictionary<HeartEffectTargetKey, HeartNodeEffect>();
            var actualValues = new Dictionary<HeartEffectTargetKey, double>();
            var behaviorEffects = new Dictionary<HeartEffectTargetKey, HeartNodeEffect>();
            HeartNodeEffect[] effects = definition.Effects ?? Array.Empty<HeartNodeEffect>();

            for (int i = 0; i < effects.Length; i++)
            {
                HeartNodeEffect effect = effects[i];
                if (!HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out error))
                {
                    error = $"Heart node '{definition.Id}' Effects[{i}]: {error}";
                    return false;
                }

                if (HeartEffectMath.IsBehaviorEffect(effect.Type))
                {
                    behaviorEffects[target] = effect;
                    continue;
                }

                if (!TryGetCompatiblePolicy(target, effect, nextPolicies, out error))
                {
                    error = $"Heart node '{definition.Id}' Effects[{i}]: {error}";
                    return false;
                }
                if (!TryGetBaseline(target, out HeartEffectBaseline baseline, out error))
                {
                    error = $"Heart node '{definition.Id}' Effects[{i}]: {error}";
                    return false;
                }

                double currentRaw = nextRawValues.TryGetValue(target, out double pendingRaw)
                    ? pendingRaw
                    : GetRawInvestment(target);
                double addedRaw = effect.Value * addedLevels;
                double nextRaw = currentRaw + addedRaw;
                if (!HeartEffectMath.IsFinite(nextRaw) || nextRaw < 0d)
                {
                    error = $"Heart effect raw investment overflow: {target}.";
                    return false;
                }
                if (!HeartEffectMath.TryCalculateActual(effect, baseline.Value, nextRaw,
                        out double actualValue, out error))
                {
                    error = $"Heart node '{definition.Id}' Effects[{i}]: {error}";
                    return false;
                }

                nextRawValues[target] = nextRaw;
                nextPolicies[target] = effect;
                actualValues[target] = actualValue;
            }

            preparedTransaction = new PreparedTransaction(
                this,
                definition.Id,
                newLevel,
                nextRawValues,
                nextPolicies,
                actualValues,
                behaviorEffects);
            return true;
        }

        public bool TryResolve(
            HeartNodeDefinitionSO definition,
            HeartNodeEffect effect,
            int currentLevel,
            out HeartResolvedEffectValue resolvedValue)
        {
            resolvedValue = default;
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                return false;

            int trackedLevel = _nodeLevels.TryGetValue(definition.Id, out int level)
                ? level
                : 0;
            if (trackedLevel != currentLevel
                || !HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out _)
                || HeartEffectMath.IsBehaviorEffect(effect.Type)
                || !TryGetCompatiblePolicy(target, effect, null, out _)
                || !TryGetBaseline(target, out HeartEffectBaseline baseline, out _))
            {
                return false;
            }

            double currentRaw = GetRawInvestment(target);
            double afterRaw = currentRaw + effect.Value;
            if (!HeartEffectMath.TryCalculateActual(
                    effect, baseline.Value, currentRaw, out double currentValue, out _)
                || !HeartEffectMath.TryCalculateActual(
                    effect, baseline.Value, afterRaw, out double afterValue, out _))
            {
                return false;
            }

            resolvedValue = new HeartResolvedEffectValue
            {
                Label = baseline.Label,
                CurrentValueText = HeartEffectValueFormatter.Format(baseline, currentValue, false),
                AfterPurchaseValueText = HeartEffectValueFormatter.Format(baseline, afterValue, false),
                DeltaText = HeartEffectValueFormatter.Format(
                    baseline, afterValue - currentValue, true)
            };
            return true;
        }

        public bool TryGetActualValue(HeartNodeEffect effect, out double actualValue)
        {
            actualValue = 0d;
            return HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out _)
                   && !HeartEffectMath.IsBehaviorEffect(effect.Type)
                   && TryGetCompatiblePolicy(target, effect, null, out _)
                   && TryGetBaseline(target, out HeartEffectBaseline baseline, out _)
                   && HeartEffectMath.TryCalculateActual(
                       effect, baseline.Value, GetRawInvestment(target), out actualValue, out _);
        }

        public bool IsBehaviorEnabled(HeartNodeEffect effect)
        {
            return HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out _)
                   && HeartEffectMath.IsBehaviorEffect(effect.Type)
                   && _enabledBehaviors.Contains(target);
        }

        private bool TryGetCompatiblePolicy(
            HeartEffectTargetKey target,
            HeartNodeEffect candidate,
            Dictionary<HeartEffectTargetKey, HeartNodeEffect> pendingPolicies,
            out string error)
        {
            error = string.Empty;
            HeartNodeEffect existing = default;
            bool hasExisting = pendingPolicies != null
                               && pendingPolicies.TryGetValue(target, out existing);
            if (!hasExisting)
                hasExisting = _targetPolicies.TryGetValue(target, out existing);
            if (!hasExisting)
                return true;

            if (!MathfApproximately(existing.SoftCap, candidate.SoftCap))
            {
                error = $"Ayni Heart effect target'i farkli SoftCap tasiyor: {target}.";
                return false;
            }

            return true;
        }

        private bool TryGetBaseline(
            HeartEffectTargetKey target,
            out HeartEffectBaseline baseline,
            out string error)
        {
            baseline = default;
            error = string.Empty;
            if (_baselineProvider == null
                || !_baselineProvider.TryGetBaseline(target, out baseline))
            {
                error = $"Gercek runtime baseline'i bulunamadi: {target}.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(baseline.Label)
                || !HeartEffectMath.IsFinite(baseline.Value)
                || baseline.Value < 0d)
            {
                error = $"Runtime baseline gecersiz: {target}.";
                return false;
            }

            return true;
        }

        private double GetRawInvestment(HeartEffectTargetKey target)
        {
            return _rawInvestments.TryGetValue(target, out double raw) ? raw : 0d;
        }

        private static bool MathfApproximately(double first, double second)
        {
            return Math.Abs(first - second) <= 0.000000001d;
        }

        private sealed class PreparedTransaction : IHeartPreparedEffectTransaction
        {
            private readonly HeartEffectPipeline _owner;
            private readonly string _nodeId;
            private readonly int _newLevel;
            private readonly Dictionary<HeartEffectTargetKey, double> _nextRawValues;
            private readonly Dictionary<HeartEffectTargetKey, HeartNodeEffect> _nextPolicies;
            private readonly Dictionary<HeartEffectTargetKey, double> _actualValues;
            private readonly Dictionary<HeartEffectTargetKey, HeartNodeEffect> _behaviorEffects;
            private bool _committed;

            public PreparedTransaction(
                HeartEffectPipeline owner,
                string nodeId,
                int newLevel,
                Dictionary<HeartEffectTargetKey, double> nextRawValues,
                Dictionary<HeartEffectTargetKey, HeartNodeEffect> nextPolicies,
                Dictionary<HeartEffectTargetKey, double> actualValues,
                Dictionary<HeartEffectTargetKey, HeartNodeEffect> behaviorEffects)
            {
                _owner = owner;
                _nodeId = nodeId;
                _newLevel = newLevel;
                _nextRawValues = nextRawValues;
                _nextPolicies = nextPolicies;
                _actualValues = actualValues;
                _behaviorEffects = behaviorEffects;
            }

            public void Commit()
            {
                if (_committed)
                    return;
                _committed = true;

                foreach (KeyValuePair<HeartEffectTargetKey, double> pair in _nextRawValues)
                    _owner._rawInvestments[pair.Key] = pair.Value;
                foreach (KeyValuePair<HeartEffectTargetKey, HeartNodeEffect> pair in _nextPolicies)
                    _owner._targetPolicies[pair.Key] = pair.Value;
                _owner._nodeLevels[_nodeId] = _newLevel;

                foreach (KeyValuePair<HeartEffectTargetKey, double> pair in _actualValues)
                    _owner._sink?.ApplyNumericEffect(pair.Key, pair.Value);
                foreach (KeyValuePair<HeartEffectTargetKey, HeartNodeEffect> pair in _behaviorEffects)
                {
                    if (_owner._enabledBehaviors.Add(pair.Key))
                        _owner._sink?.EnableBehaviorEffect(pair.Value);
                }
            }
        }
    }

    public static class HeartEffectMath
    {
        public static bool IsBehaviorEffect(HeartNodeEffectType type)
        {
            return type == HeartNodeEffectType.UnlockArcherType
                   || type == HeartNodeEffectType.UnlockSpellcasting
                   || type == HeartNodeEffectType.EnableSplitShot
                   || type == HeartNodeEffectType.EnableBurningGround
                   || type == HeartNodeEffectType.EnableSecondBlast;
        }

        public static bool TryCreateTarget(
            HeartNodeEffect effect,
            out HeartEffectTargetKey target,
            out string error)
        {
            target = new HeartEffectTargetKey(effect.Type, effect.ArcherType, effect.Resource);
            error = string.Empty;
            if (effect.Type == HeartNodeEffectType.None)
            {
                error = "None effect uygulanamaz.";
                return false;
            }
            if (!IsBehaviorEffect(effect.Type)
                && (!IsFinite(effect.Value) || effect.Value <= 0f))
            {
                error = $"{effect.Type} Value sonlu ve sifirdan buyuk olmali.";
                return false;
            }
            if (RequiresPositiveSoftCap(effect.Type)
                && (!IsFinite(effect.SoftCap) || effect.SoftCap <= 0f))
            {
                error = $"{effect.Type} pozitif SoftCap gerektirir.";
                return false;
            }
            if ((effect.Type == HeartNodeEffectType.ReduceSpellCooldownPercent
                 || effect.Type == HeartNodeEffectType.ReduceFrostSlowMultiplier)
                && effect.SoftCap >= 1f)
            {
                error = $"{effect.Type} SoftCap degeri 1'den kucuk olmali.";
                return false;
            }
            if (effect.Type == HeartNodeEffectType.ReduceFrostSlowMultiplier
                && effect.ArcherType != ArcherType.Frost)
            {
                error = "Frost slow effect'i ArcherType.Frost hedeflemeli.";
                return false;
            }
            if (!IsBehaviorEffect(effect.Type) && !IsNumericEffect(effect.Type))
            {
                error = $"Desteklenmeyen Heart effect: {effect.Type}.";
                return false;
            }

            return true;
        }

        public static bool TryCalculateActual(
            HeartNodeEffect effect,
            double baseline,
            double rawInvestment,
            out double actual,
            out string error)
        {
            actual = 0d;
            error = string.Empty;
            if (!IsFinite(baseline) || baseline < 0d
                || !IsFinite(rawInvestment) || rawInvestment < 0d)
            {
                error = "Heart effect baseline/raw degeri gecersiz.";
                return false;
            }

            switch (effect.Type)
            {
                case HeartNodeEffectType.ModifyArcherDamagePercent:
                case HeartNodeEffectType.ModifyWallMaxHpPercent:
                case HeartNodeEffectType.IncreaseResourceProductionPercent:
                case HeartNodeEffectType.ModifySpellDamagePercent:
                    actual = baseline * (1d + rawInvestment);
                    break;

                case HeartNodeEffectType.ReduceWallRepairCostPercent:
                    actual = baseline * Math.Max(0d, 1d - rawInvestment);
                    break;

                case HeartNodeEffectType.IncreaseWorkerCapacity:
                case HeartNodeEffectType.IncreasePopulationGrowth:
                case HeartNodeEffectType.IncreaseArrowCapacity:
                case HeartNodeEffectType.IncreaseArrowEfficiency:
                    actual = baseline + rawInvestment;
                    break;

                case HeartNodeEffectType.ModifyArcherFireRatePercent:
                    actual = baseline * (1d + DiminishingBonus(rawInvestment, effect.SoftCap));
                    break;

                case HeartNodeEffectType.AddArcherRange:
                case HeartNodeEffectType.AddSpellRadius:
                    actual = baseline + DiminishingBonus(rawInvestment, effect.SoftCap);
                    break;

                case HeartNodeEffectType.ReduceSpellCooldownPercent:
                    actual = baseline * (1d - DiminishingBonus(rawInvestment, effect.SoftCap));
                    break;

                case HeartNodeEffectType.ReduceFrostSlowMultiplier:
                {
                    double minimumMultiplier = effect.SoftCap;
                    if (baseline < minimumMultiplier)
                    {
                        error = "Frost slow baseline'i authored minimum multiplier'dan kucuk.";
                        return false;
                    }

                    double distance = baseline - minimumMultiplier;
                    actual = distance <= 0d
                        ? baseline
                        : minimumMultiplier + distance * Math.Exp(-rawInvestment / distance);
                    break;
                }

                default:
                    error = $"Numeric olmayan Heart effect hesaplanamaz: {effect.Type}.";
                    return false;
            }

            if (!IsFinite(actual) || actual < 0d)
            {
                error = $"Heart effect actual degeri overflow/gecersiz: {effect.Type}.";
                return false;
            }

            return true;
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double DiminishingBonus(double rawInvestment, double softCap)
        {
            return softCap * (1d - Math.Exp(-rawInvestment / softCap));
        }

        private static bool RequiresPositiveSoftCap(HeartNodeEffectType type)
        {
            return type == HeartNodeEffectType.ModifyArcherFireRatePercent
                   || type == HeartNodeEffectType.AddArcherRange
                   || type == HeartNodeEffectType.ReduceFrostSlowMultiplier
                   || type == HeartNodeEffectType.AddSpellRadius
                   || type == HeartNodeEffectType.ReduceSpellCooldownPercent;
        }

        private static bool IsNumericEffect(HeartNodeEffectType type)
        {
            switch (type)
            {
                case HeartNodeEffectType.ModifyArcherDamagePercent:
                case HeartNodeEffectType.ModifyArcherFireRatePercent:
                case HeartNodeEffectType.ModifyWallMaxHpPercent:
                case HeartNodeEffectType.ReduceWallRepairCostPercent:
                case HeartNodeEffectType.IncreaseWorkerCapacity:
                case HeartNodeEffectType.IncreaseResourceProductionPercent:
                case HeartNodeEffectType.IncreasePopulationGrowth:
                case HeartNodeEffectType.ModifySpellDamagePercent:
                case HeartNodeEffectType.AddSpellRadius:
                case HeartNodeEffectType.ReduceSpellCooldownPercent:
                case HeartNodeEffectType.AddArcherRange:
                case HeartNodeEffectType.ReduceFrostSlowMultiplier:
                case HeartNodeEffectType.IncreaseArrowCapacity:
                case HeartNodeEffectType.IncreaseArrowEfficiency:
                    return true;
                default:
                    return false;
            }
        }
    }

    public static class HeartEffectValueFormatter
    {
        public static string Format(
            HeartEffectBaseline baseline,
            double value,
            bool includeDeltaSign)
        {
            double displayValue = baseline.DisplayAsPercent ? value * 100d : value;
            if (Math.Abs(displayValue) < 0.0000001d)
                displayValue = 0d;

            string pattern = baseline.DecimalPlaces == 0
                ? "#,0"
                : "#,0." + new string('#', baseline.DecimalPlaces);
            string formatted = displayValue.ToString(pattern, CultureInfo.InvariantCulture);
            if (includeDeltaSign && displayValue > 0d)
                formatted = "+" + formatted;
            return formatted + baseline.Suffix;
        }
    }
}
