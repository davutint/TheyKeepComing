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
        public const float EfficiencyPercentPerLevel = 0.10f;
        public const int CapacityBaseWoodCost = 100;
        public const int CapacityBaseIronCost = 25;
        public const int EfficiencyBaseWoodCost = 150;
        public const int EfficiencyBaseIronCost = 50;
        public const double CostGrowthMultiplier = 1.35d;

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
            resource = EconomyFocusUtility.Normalize(resource);
            if (resource == EconomyFocusType.Balanced)
            {
                cost = default;
                return false;
            }

            return TryGetCostForLevel(upgradeType, GetLevel(state, resource, upgradeType), out cost);
        }

        public static bool TryGetCostForLevel(WorkerBuildingUpgradeType upgradeType, int currentLevel,
            out WorkerBuildingUpgradeCost cost)
        {
            cost = default;
            if (currentLevel < 0
                || (upgradeType != WorkerBuildingUpgradeType.Capacity
                    && upgradeType != WorkerBuildingUpgradeType.Efficiency))
            {
                return false;
            }

            int baseWood = upgradeType == WorkerBuildingUpgradeType.Capacity
                ? CapacityBaseWoodCost
                : EfficiencyBaseWoodCost;
            int baseIron = upgradeType == WorkerBuildingUpgradeType.Capacity
                ? CapacityBaseIronCost
                : EfficiencyBaseIronCost;
            double multiplier = Math.Pow(CostGrowthMultiplier, currentLevel);
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
            return math.max(0, efficiencyLevel) * EfficiencyPercentPerLevel;
        }
    }
}
