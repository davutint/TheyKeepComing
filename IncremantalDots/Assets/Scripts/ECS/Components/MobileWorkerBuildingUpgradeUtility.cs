using System;
using Unity.Mathematics;

namespace DeadWalls
{
    public enum WorkerBuildingUpgradeType : byte
    {
        Capacity = 0,
        Efficiency = 1
    }

    public readonly struct WorkerBuildingUpgradeCost
    {
        public readonly int Wood;
        public readonly int Iron;

        public WorkerBuildingUpgradeCost(int wood, int iron)
        {
            Wood = math.max(0, wood);
            Iron = math.max(0, iron);
        }
    }

    public static class MobileWorkerBuildingUpgradeUtility
    {
        public const int CapacityPerLevel = 10;
        public const float EfficiencyPercentPerLevel =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyPercentPerLevel;
        public const int CapacityBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseWoodCost;
        public const int CapacityBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseIronCost;
        public const int EfficiencyBaseWoodCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseWoodCost;
        public const int EfficiencyBaseIronCost =
            MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseIronCost;
        public const double CostGrowthMultiplier =
            MobileEconomyPriceTuningUtility.DefaultWorkerBuildingCostGrowthMultiplier;

        public static int GetLevel(in MobileWorkerBuildingUpgradeState state,
            EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            bool efficiency = upgradeType == WorkerBuildingUpgradeType.Efficiency;
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    return math.max(0, efficiency ? state.WoodEfficiencyLevel : state.WoodCapacityLevel);
                case EconomyFocusType.Stone:
                    return math.max(0, efficiency ? state.StoneEfficiencyLevel : state.StoneCapacityLevel);
                case EconomyFocusType.Iron:
                    return math.max(0, efficiency ? state.IronEfficiencyLevel : state.IronCapacityLevel);
                case EconomyFocusType.Food:
                    return math.max(0, efficiency ? state.FoodEfficiencyLevel : state.FoodCapacityLevel);
                default:
                    return 0;
            }
        }

        public static bool TryIncreaseLevel(ref MobileWorkerBuildingUpgradeState state,
            EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced
                || (upgradeType != WorkerBuildingUpgradeType.Capacity
                    && upgradeType != WorkerBuildingUpgradeType.Efficiency))
            {
                return false;
            }

            int level = GetLevel(state, resource, upgradeType);
            if (level >= int.MaxValue)
                return false;

            int nextLevel = level + 1;
            bool efficiency = upgradeType == WorkerBuildingUpgradeType.Efficiency;
            switch (resource)
            {
                case EconomyFocusType.Wood:
                    if (efficiency) state.WoodEfficiencyLevel = nextLevel;
                    else state.WoodCapacityLevel = nextLevel;
                    return true;
                case EconomyFocusType.Stone:
                    if (efficiency) state.StoneEfficiencyLevel = nextLevel;
                    else state.StoneCapacityLevel = nextLevel;
                    return true;
                case EconomyFocusType.Iron:
                    if (efficiency) state.IronEfficiencyLevel = nextLevel;
                    else state.IronCapacityLevel = nextLevel;
                    return true;
                case EconomyFocusType.Food:
                    if (efficiency) state.FoodEfficiencyLevel = nextLevel;
                    else state.FoodCapacityLevel = nextLevel;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetNextCost(in MobileWorkerBuildingUpgradeState state,
            EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType,
            out WorkerBuildingUpgradeCost cost)
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return TryGetNextCost(state, resource, upgradeType, tuning, out cost);
        }

        public static bool TryGetNextCost(in MobileWorkerBuildingUpgradeState state,
            EconomyFocusType resource, WorkerBuildingUpgradeType upgradeType,
            in MobileEconomyPriceTuning tuning, out WorkerBuildingUpgradeCost cost)
        {
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced)
            {
                cost = default;
                return false;
            }

            return TryGetCostForLevel(upgradeType, GetLevel(state, resource, upgradeType),
                tuning, out cost);
        }

        public static bool TryGetCostForLevel(WorkerBuildingUpgradeType upgradeType, int currentLevel,
            out WorkerBuildingUpgradeCost cost)
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return TryGetCostForLevel(upgradeType, currentLevel, tuning, out cost);
        }

        public static bool TryGetCostForLevel(WorkerBuildingUpgradeType upgradeType, int currentLevel,
            in MobileEconomyPriceTuning tuning, out WorkerBuildingUpgradeCost cost)
        {
            cost = default;
            if (currentLevel < 0
                || (upgradeType != WorkerBuildingUpgradeType.Capacity
                    && upgradeType != WorkerBuildingUpgradeType.Efficiency))
            {
                return false;
            }

            var safeTuning = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            int baseWood = upgradeType == WorkerBuildingUpgradeType.Capacity
                ? safeTuning.WorkerCapacityBaseWoodCost
                : safeTuning.WorkerEfficiencyBaseWoodCost;
            int baseIron = upgradeType == WorkerBuildingUpgradeType.Capacity
                ? safeTuning.WorkerCapacityBaseIronCost
                : safeTuning.WorkerEfficiencyBaseIronCost;
            double multiplier = Math.Pow(safeTuning.WorkerBuildingCostGrowthMultiplier,
                currentLevel);
            if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
                return false;

            double wood = Math.Ceiling(baseWood * multiplier);
            double iron = Math.Ceiling(baseIron * multiplier);
            if (wood > int.MaxValue || iron > int.MaxValue || wood < 0d || iron < 0d)
                return false;

            cost = new WorkerBuildingUpgradeCost((int)wood, (int)iron);
            return true;
        }

        public static int GetCapacityBonus(int capacityLevel)
        {
            long bonus = (long)math.max(0, capacityLevel) * CapacityPerLevel;
            return bonus >= int.MaxValue ? int.MaxValue : (int)bonus;
        }

        public static float GetEfficiencyBonusPercent(int efficiencyLevel)
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return GetEfficiencyBonusPercent(efficiencyLevel, tuning);
        }

        public static float GetEfficiencyBonusPercent(int efficiencyLevel,
            in MobileEconomyPriceTuning tuning)
        {
            MobileEconomyPriceTuning safeTuning = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            double bonus = (double)math.max(0, efficiencyLevel)
                * safeTuning.WorkerEfficiencyPercentPerLevel;
            return bonus >= float.MaxValue ? float.MaxValue : (float)bonus;
        }
    }
}
