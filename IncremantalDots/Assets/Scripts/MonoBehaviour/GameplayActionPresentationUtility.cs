using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeadWalls
{
    /// <summary>
    /// Reddedilen oyuncu eylemlerini genel hata yerine tam ve eyleme donuk bir nedene cevirir.
    /// Karar vermez; canli owner'lardan gelen maliyet, bakiye ve kapasite snapshot'ini sunar.
    /// </summary>
    public static class GameplayActionFeedbackUtility
    {
        public static string BuildMissingResourceReason(ResourceCost cost, ResourceData available)
        {
            ResourceCost missing = cost.GetMissing(available);
            var parts = new List<string>(4);
            AddMissing(parts, missing.Wood, "WOOD");
            AddMissing(parts, missing.Stone, "STONE");
            AddMissing(parts, missing.Iron, "IRON");
            AddMissing(parts, missing.Food, "FOOD");
            return parts.Count == 0
                ? string.Empty
                : "NOT ENOUGH RESOURCES  ·  NEED " + string.Join("  ·  ", parts);
        }

        public static string BuildResourcePurchaseFailure(
            ResourceCost cost,
            ResourceData available,
            string fallback)
        {
            string missing = BuildMissingResourceReason(cost, available);
            return string.IsNullOrEmpty(missing) ? fallback : missing;
        }

        public static string BuildArcherRecruitmentFailure(
            bool unlocked,
            int remainingArcherCapacity,
            int availableWorkers,
            int populationCost,
            ResourceCost cost,
            ResourceData available)
        {
            if (!unlocked)
                return "ARCHER TYPE LOCKED  ·  RESEARCH IT IN CASTLE HEART";
            if (remainingArcherCapacity <= 0)
                return "GARRISON FULL  ·  MAXIMUM ARCHER CAPACITY REACHED";

            int safePopulationCost = Math.Max(0, populationCost);
            int safeAvailableWorkers = Math.Max(0, availableWorkers);
            if (safeAvailableWorkers < safePopulationCost)
            {
                int missingWorkers = safePopulationCost - safeAvailableWorkers;
                return $"NOT ENOUGH WORKERS  ·  NEED {missingWorkers:N0} MORE";
            }

            return BuildResourcePurchaseFailure(
                cost,
                available,
                "RECRUITMENT FAILED  ·  TRY AGAIN");
        }

        public static bool CanExplainArcherRecruitmentFailure(
            bool unlocked,
            int remainingArcherCapacity)
        {
            return unlocked && remainingArcherCapacity > 0;
        }

        public static string BuildArcherRetrainingFailure(
            bool unlocked,
            int basicArcherCount,
            ResourceCost cost,
            ResourceData available)
        {
            if (!unlocked)
                return "ARCHER TYPE LOCKED  ·  RESEARCH IT IN CASTLE HEART";
            if (basicArcherCount <= 0)
                return "NO BASIC ARCHER AVAILABLE  ·  RECRUIT ONE FIRST";

            return BuildResourcePurchaseFailure(
                cost,
                available,
                "RETRAINING FAILED  ·  TRY AGAIN");
        }

        public static bool CanExplainArcherRetrainingFailure(
            bool unlocked,
            int basicArcherCount)
        {
            return unlocked && basicArcherCount > 0;
        }

        public static string BuildArrowRefillFailure(
            bool deliveryInProgress,
            float deliveryRemainingSeconds,
            bool reserveFull,
            ArrowRefillQuote quote,
            ResourceData available)
        {
            if (deliveryInProgress)
            {
                string remaining = Math.Max(0d, deliveryRemainingSeconds)
                    .ToString("0.0", CultureInfo.InvariantCulture);
                return $"SUPPLY DELIVERY IN PROGRESS  ·  {remaining}S REMAINING";
            }
            if (reserveFull)
                return "ARROW RESERVE FULL  ·  NO FREE CAPACITY";
            if (!quote.IsValid)
                return "ARROW RESTOCK UNAVAILABLE  ·  NO VALID PACKAGE";

            return BuildResourcePurchaseFailure(
                new ResourceCost(quote.WoodCost, 0, 0, 0),
                available,
                "ARROW RESTOCK FAILED  ·  TRY AGAIN");
        }

        public static string BuildMetaUpgradeFailure(
            bool shopPurchaseAllowed,
            bool maxed,
            int cost,
            int balance,
            string currency)
        {
            if (maxed)
                return "UPGRADE COMPLETE  ·  MAXIMUM BENEFIT ACTIVE";
            if (!shopPurchaseAllowed)
                return "META SHOP UNAVAILABLE  ·  RUN REWARD WAS NOT SAVED";

            int missing = Math.Max(0, cost) - Math.Max(0, balance);
            if (missing > 0)
            {
                string name = string.IsNullOrWhiteSpace(currency)
                    ? "CURRENCY"
                    : currency.Trim().ToUpperInvariant();
                return $"NOT ENOUGH {name}  ·  NEED {missing:N0} MORE {name}";
            }

            return "PURCHASE FAILED  ·  PROGRESS COULD NOT BE SAVED";
        }

        public static string BuildTechResearchFailure(
            string reasonCode,
            ResourceCost cost,
            ResourceData available)
        {
            string missing = BuildMissingResourceReason(cost, available);
            if (!string.IsNullOrEmpty(missing))
                return missing;

            switch (reasonCode?.Trim().ToUpperInvariant())
            {
                case "WAIT":
                    return "RESEARCH UNAVAILABLE  ·  GAME STATE NOT READY";
                case "HIDDEN":
                    return "TECHNOLOGY HIDDEN  ·  REVEAL A CONNECTED NODE FIRST";
                case "MAX":
                    return "RESEARCH COMPLETE  ·  MAXIMUM LEVEL REACHED";
                case "LOCKED":
                    return "TECHNOLOGY LOCKED  ·  RESEARCH ITS PREREQUISITES FIRST";
                default:
                    return "DOCTRINE RESEARCH FAILED  ·  TRY AGAIN";
            }
        }

        public static string BuildHeartPurchaseFailure(
            HeartPurchaseFailureReason reason,
            HeartPurchaseQuote quote,
            long availableGraveEssence)
        {
            switch (reason)
            {
                case HeartPurchaseFailureReason.RootCannotBePurchased:
                    return "CASTLE HEART ORIGIN  ·  CANNOT BE RESEARCHED";
                case HeartPurchaseFailureReason.Hidden:
                    return "TECHNOLOGY HIDDEN  ·  REVEAL A CONNECTED NODE FIRST";
                case HeartPurchaseFailureReason.KeystoneLocked:
                    return "DOCTRINE LOCKED  ·  OPPOSING PATH COMMITTED";
                case HeartPurchaseFailureReason.AlreadyPurchased:
                    return "RESEARCH COMPLETE  ·  BENEFIT ALREADY ACTIVE";
                case HeartPurchaseFailureReason.RepeatableRequired:
                    return "BULK RESEARCH UNAVAILABLE  ·  SELECT A REPEATABLE TECHNOLOGY";
                case HeartPurchaseFailureReason.TechnicalLevelLimit:
                    return "RESEARCH COMPLETE  ·  MAXIMUM LEVEL REACHED";
                case HeartPurchaseFailureReason.CostOverflow:
                    return "RESEARCH UNAVAILABLE  ·  COST LIMIT REACHED";
                case HeartPurchaseFailureReason.InsufficientGraveEssence:
                {
                    long required = Math.Max(0L, quote?.TotalGraveEssenceCost ?? 0L);
                    long missing = Math.Max(0L, required - Math.Max(0L, availableGraveEssence));
                    return missing > 0L
                        ? $"NOT ENOUGH GRAVE ESSENCE  ·  NEED {missing:N0} MORE"
                        : "NOT ENOUGH GRAVE ESSENCE  ·  EARN MORE FROM ENEMIES";
                }
                case HeartPurchaseFailureReason.EffectRejected:
                    return "RESEARCH FAILED  ·  EFFECT COULD NOT BE APPLIED";
                case HeartPurchaseFailureReason.SpendRejected:
                    return "RESEARCH FAILED  ·  GRAVE ESSENCE BALANCE CHANGED";
                case HeartPurchaseFailureReason.None:
                    return "RESEARCH FAILED  ·  TRY AGAIN";
                default:
                    return "HEART RESEARCH UNAVAILABLE  ·  INVALID GAME STATE";
            }
        }

        private static void AddMissing(List<string> parts, int amount, string resource)
        {
            if (amount > 0)
                parts.Add($"{amount.ToString("N0", CultureInfo.InvariantCulture)} MORE {resource}");
        }
    }

    /// <summary>
    /// Meta kartinda asset aciklamasinin yaninda exact mevcut ve satin alma sonrasi etkiyi sunar.
    /// </summary>
    public static class MetaUpgradePresentationUtility
    {
        public static string BuildEffectProgression(MetaUpgradeSO upgrade, int currentLevel)
        {
            if (upgrade == null)
                return "EFFECT UNAVAILABLE";

            int level = Math.Max(0, currentLevel);
            double current = upgrade.GetTotalEffect(level);
            double next = upgrade.GetTotalEffect(level + 1);
            bool maxed = upgrade.IsMaxLevel(level);
            switch (upgrade.EffectType)
            {
                case MetaUpgradeEffectType.StartingResource:
                    if (maxed)
                        return $"NEXT RUN STARTING {ResourceName(upgrade.Resource)}: "
                               + $"{FormatNumber(current)}  ·  MAXIMUM ACTIVE";
                    return $"NEXT RUN STARTING {ResourceName(upgrade.Resource)}: "
                           + $"{FormatNumber(current)}  →  {FormatNumber(next)}";
                case MetaUpgradeEffectType.StartingArchers:
                    if (maxed)
                        return $"NEXT RUN BASIC ARCHERS: {FormatNumber(current)}  ·  MAXIMUM ACTIVE";
                    return $"NEXT RUN BASIC ARCHERS: {FormatNumber(current)}  →  {FormatNumber(next)}";
                case MetaUpgradeEffectType.WallHpPercent:
                    if (maxed)
                        return $"WALL MAX HP: {FormatPercent(current)}  ·  MAXIMUM ACTIVE";
                    return $"WALL MAX HP: {FormatPercent(current)}  →  {FormatPercent(next)}";
                case MetaUpgradeEffectType.ProductionPercent:
                    if (maxed)
                        return $"ALL WORKER PRODUCTION: {FormatPercent(current)}  ·  MAXIMUM ACTIVE";
                    return $"ALL WORKER PRODUCTION: {FormatPercent(current)}  →  {FormatPercent(next)}";
                case MetaUpgradeEffectType.StartingBeds:
                    if (maxed)
                        return $"NEXT RUN BED CAPACITY: {FormatNumber(current)}  ·  MAXIMUM ACTIVE";
                    return $"NEXT RUN BED CAPACITY: {FormatNumber(current)}  →  {FormatNumber(next)}";
                case MetaUpgradeEffectType.ArrowEfficiency:
                    if (maxed)
                        return $"EXTRA ARROWS PER WOOD: {FormatNumber(current)}  ·  MAXIMUM ACTIVE";
                    return $"EXTRA ARROWS PER WOOD: {FormatNumber(current)}  →  {FormatNumber(next)}";
                case MetaUpgradeEffectType.EssenceGainPercent:
                    if (maxed)
                        return $"GRAVE ESSENCE GAIN: {FormatPercent(current)}  ·  MAXIMUM ACTIVE";
                    return $"GRAVE ESSENCE GAIN: {FormatPercent(current)}  →  {FormatPercent(next)}";
                case MetaUpgradeEffectType.NodePoolUnlock:
                    return level > 0
                        ? "FUTURE CASTLE HEART OPTIONS: UNLOCKED  ·  ACTIVE"
                        : "FUTURE CASTLE HEART OPTIONS: LOCKED  →  UNLOCKED";
                default:
                    return "EFFECT UNAVAILABLE";
            }
        }

        private static string ResourceName(EconomyFocusType resource)
        {
            switch (resource)
            {
                case EconomyFocusType.Wood: return "WOOD";
                case EconomyFocusType.Stone: return "STONE";
                case EconomyFocusType.Iron: return "IRON";
                case EconomyFocusType.Food: return "FOOD";
                default: return "RESOURCE";
            }
        }

        private static string FormatNumber(double value)
        {
            return "+" + Math.Max(0d, value).ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string FormatPercent(double value)
        {
            double percent = Math.Max(0d, value) * 100d;
            return "+" + percent.ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }
    }
}
