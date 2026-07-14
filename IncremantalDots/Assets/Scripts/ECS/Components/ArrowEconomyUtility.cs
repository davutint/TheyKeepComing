using System;
using Unity.Mathematics;

namespace DeadWalls
{
    public enum ArrowUpgradeType : byte
    {
        Capacity = 0,
        Efficiency = 1
    }

    public readonly struct ArrowRefillQuote
    {
        public readonly int ArrowAmount;
        public readonly int WoodCost;
        public readonly int PackageCount;

        public ArrowRefillQuote(int arrowAmount, int woodCost, int packageCount)
        {
            ArrowAmount = math.max(0, arrowAmount);
            WoodCost = math.max(0, woodCost);
            PackageCount = math.max(0, packageCount);
        }

        public bool IsValid => ArrowAmount > 0 && WoodCost > 0;
    }

    public readonly struct ArrowUpgradeCost
    {
        public readonly int Wood;
        public readonly int Iron;

        public ArrowUpgradeCost(int wood, int iron)
        {
            Wood = math.max(0, wood);
            Iron = math.max(0, iron);
        }
    }

    /// <summary>
    /// Finite Arrow ekonomisinin saf matematik owner'i. Refill sayisi birim fiyati
    /// buyutmez; yalniz run capacity/efficiency seviyeleri sonucu degistirir.
    /// </summary>
    public static class ArrowEconomyUtility
    {
        public static int GetCapacity(in ArrowSupply supply, in MobileEconomyPriceTuning tuning)
        {
            MobileEconomyPriceTuning safe = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            long capacity = (long)safe.ArrowBaseCapacity
                + (long)math.max(0, supply.CapacityLevel) * safe.ArrowCapacityPerLevel
                + math.max(0, supply.HeartCapacityBonus);
            return capacity >= int.MaxValue ? int.MaxValue : (int)capacity;
        }

        public static int GetArrowsPerWood(in ArrowSupply supply, in MobileEconomyPriceTuning tuning)
        {
            MobileEconomyPriceTuning safe = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            long rate = (long)safe.ArrowBaseArrowsPerWood
                + (long)math.max(0, supply.EfficiencyLevel) * safe.ArrowArrowsPerWoodPerEfficiencyLevel
                + math.max(0, supply.HeartEfficiencyBonus);
            return rate >= int.MaxValue ? int.MaxValue : (int)rate;
        }

        public static bool TryGetPackageQuote(in ArrowSupply supply,
            in MobileEconomyPriceTuning tuning, int packageCount, out ArrowRefillQuote quote)
        {
            quote = default;
            if (packageCount <= 0)
                return false;

            MobileEconomyPriceTuning safe = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            int capacity = GetCapacity(supply, safe);
            int missing = math.max(0, capacity - math.clamp(supply.Current, 0, capacity));
            if (missing <= 0)
                return false;

            long requested = (long)safe.ArrowRefillPackageSize * packageCount;
            int arrowAmount = (int)math.min((long)missing, requested);
            int woodCost = DivideRoundUp(arrowAmount, GetArrowsPerWood(supply, safe));
            if (arrowAmount <= 0 || woodCost <= 0)
                return false;

            quote = new ArrowRefillQuote(arrowAmount, woodCost, packageCount);
            return true;
        }

        public static bool TryGetBuyMaxQuote(in ArrowSupply supply,
            in MobileEconomyPriceTuning tuning, int availableWood, out ArrowRefillQuote quote)
        {
            quote = default;
            if (availableWood <= 0)
                return false;

            MobileEconomyPriceTuning safe = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            int capacity = GetCapacity(supply, safe);
            int missing = math.max(0, capacity - math.clamp(supply.Current, 0, capacity));
            if (missing <= 0)
                return false;

            int rate = GetArrowsPerWood(supply, safe);
            long affordableArrows = (long)availableWood * rate;
            int arrowAmount = (int)math.min((long)missing, affordableArrows);
            int woodCost = DivideRoundUp(arrowAmount, rate);
            if (arrowAmount <= 0 || woodCost <= 0 || woodCost > availableWood)
                return false;

            int packageCount = DivideRoundUp(arrowAmount, safe.ArrowRefillPackageSize);
            quote = new ArrowRefillQuote(arrowAmount, woodCost, packageCount);
            return true;
        }

        public static bool TryApplyRefill(ref ArrowSupply supply,
            in MobileEconomyPriceTuning tuning, in ArrowRefillQuote quote)
        {
            if (!quote.IsValid)
                return false;

            int capacity = GetCapacity(supply, tuning);
            int current = math.clamp(supply.Current, 0, capacity);
            if (current >= capacity)
                return false;

            long next = (long)current + quote.ArrowAmount;
            supply.Current = (int)math.min((long)capacity, next);
            supply.Accumulator = 0f;
            return supply.Current > current;
        }

        public static int GetUpgradeLevel(in ArrowSupply supply, ArrowUpgradeType type)
        {
            return type == ArrowUpgradeType.Capacity
                ? math.max(0, supply.CapacityLevel)
                : math.max(0, supply.EfficiencyLevel);
        }

        public static bool TryGetUpgradeCost(in ArrowSupply supply,
            ArrowUpgradeType type, in MobileEconomyPriceTuning tuning, out ArrowUpgradeCost cost)
        {
            cost = default;
            if (type != ArrowUpgradeType.Capacity && type != ArrowUpgradeType.Efficiency)
                return false;

            MobileEconomyPriceTuning safe = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            int level = GetUpgradeLevel(supply, type);
            int baseWood = type == ArrowUpgradeType.Capacity
                ? safe.ArrowCapacityBaseWoodCost
                : safe.ArrowEfficiencyBaseWoodCost;
            int baseIron = type == ArrowUpgradeType.Capacity
                ? safe.ArrowCapacityBaseIronCost
                : safe.ArrowEfficiencyBaseIronCost;
            double multiplier = Math.Pow(safe.ArrowUpgradeCostGrowthMultiplier, level);
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return false;

            double wood = Math.Ceiling(baseWood * multiplier);
            double iron = Math.Ceiling(baseIron * multiplier);
            if (wood > int.MaxValue || iron > int.MaxValue || wood <= 0d || iron <= 0d)
                return false;

            cost = new ArrowUpgradeCost((int)wood, (int)iron);
            return true;
        }

        public static bool TryIncreaseUpgradeLevel(ref ArrowSupply supply, ArrowUpgradeType type)
        {
            switch (type)
            {
                case ArrowUpgradeType.Capacity when supply.CapacityLevel < int.MaxValue:
                    supply.CapacityLevel = math.max(0, supply.CapacityLevel) + 1;
                    return true;
                case ArrowUpgradeType.Efficiency when supply.EfficiencyLevel < int.MaxValue:
                    supply.EfficiencyLevel = math.max(0, supply.EfficiencyLevel) + 1;
                    return true;
                default:
                    return false;
            }
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            if (value <= 0 || divisor <= 0)
                return 0;

            return (int)(((long)value + divisor - 1L) / divisor);
        }
    }
}
