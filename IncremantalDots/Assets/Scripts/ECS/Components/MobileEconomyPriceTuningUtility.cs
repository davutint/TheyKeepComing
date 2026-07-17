using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileEconomyPriceTuningUtility
    {
        public const float DefaultWoodWorkerProductionPerMin = 8f;
        public const float DefaultStoneWorkerProductionPerMin = 5.5f;
        public const float DefaultIronWorkerProductionPerMin = 4.9f;
        public const float DefaultFoodWorkerProductionPerMin = 7f;
        public const float DefaultWorkerEfficiencyPercentPerLevel = 0.10f;
        public const int DefaultBedBaseWoodCost = 100;
        public const int DefaultBedCostGrowthCapacityInterval = 25;
        public const int DefaultWorkerCapacityBaseWoodCost = 100;
        public const int DefaultWorkerCapacityBaseIronCost = 25;
        public const int DefaultWorkerEfficiencyBaseWoodCost = 150;
        public const int DefaultWorkerEfficiencyBaseIronCost = 50;
        public const double DefaultWorkerBuildingCostGrowthMultiplier = 1.35d;
        public const int DefaultArrowBaseCapacity = 200;
        public const int DefaultArrowCapacityPerLevel = 200;
        public const int DefaultArrowRefillPackageSize = 100;
        public const int DefaultArrowBaseArrowsPerWood = 4;
        public const int DefaultArrowArrowsPerWoodPerEfficiencyLevel = 1;
        public const int DefaultArrowCapacityBaseWoodCost = 150;
        public const int DefaultArrowCapacityBaseIronCost = 25;
        public const int DefaultArrowEfficiencyBaseWoodCost = 200;
        public const int DefaultArrowEfficiencyBaseIronCost = 50;
        public const double DefaultArrowUpgradeCostGrowthMultiplier = 1.35d;

        public static MobileEconomyPriceTuning Default => new MobileEconomyPriceTuning
        {
            BedBaseWoodCost = DefaultBedBaseWoodCost,
            BedCostGrowthCapacityInterval = DefaultBedCostGrowthCapacityInterval,
            WorkerCapacityBaseWoodCost = DefaultWorkerCapacityBaseWoodCost,
            WorkerCapacityBaseIronCost = DefaultWorkerCapacityBaseIronCost,
            WorkerEfficiencyBaseWoodCost = DefaultWorkerEfficiencyBaseWoodCost,
            WorkerEfficiencyBaseIronCost = DefaultWorkerEfficiencyBaseIronCost,
            WorkerBuildingCostGrowthMultiplier = DefaultWorkerBuildingCostGrowthMultiplier,
            WorkerEfficiencyPercentPerLevel = DefaultWorkerEfficiencyPercentPerLevel,
            ArrowBaseCapacity = DefaultArrowBaseCapacity,
            ArrowCapacityPerLevel = DefaultArrowCapacityPerLevel,
            ArrowRefillPackageSize = DefaultArrowRefillPackageSize,
            ArrowBaseArrowsPerWood = DefaultArrowBaseArrowsPerWood,
            ArrowArrowsPerWoodPerEfficiencyLevel = DefaultArrowArrowsPerWoodPerEfficiencyLevel,
            ArrowCapacityBaseWoodCost = DefaultArrowCapacityBaseWoodCost,
            ArrowCapacityBaseIronCost = DefaultArrowCapacityBaseIronCost,
            ArrowEfficiencyBaseWoodCost = DefaultArrowEfficiencyBaseWoodCost,
            ArrowEfficiencyBaseIronCost = DefaultArrowEfficiencyBaseIronCost,
            ArrowUpgradeCostGrowthMultiplier = DefaultArrowUpgradeCostGrowthMultiplier
        };

        public static MobileEconomyPriceTuning Sanitize(in MobileEconomyPriceTuning tuning)
        {
            double growth = math.isfinite(tuning.WorkerBuildingCostGrowthMultiplier)
                ? math.max(1d, tuning.WorkerBuildingCostGrowthMultiplier)
                : DefaultWorkerBuildingCostGrowthMultiplier;
            double arrowGrowth = math.isfinite(tuning.ArrowUpgradeCostGrowthMultiplier)
                ? math.max(1d, tuning.ArrowUpgradeCostGrowthMultiplier)
                : DefaultArrowUpgradeCostGrowthMultiplier;
            float efficiencyPercent = math.isfinite(tuning.WorkerEfficiencyPercentPerLevel)
                && tuning.WorkerEfficiencyPercentPerLevel > 0f
                    ? math.max(0.001f, tuning.WorkerEfficiencyPercentPerLevel)
                    : DefaultWorkerEfficiencyPercentPerLevel;

            return new MobileEconomyPriceTuning
            {
                BedBaseWoodCost = math.max(1, tuning.BedBaseWoodCost),
                BedCostGrowthCapacityInterval = math.max(1, tuning.BedCostGrowthCapacityInterval),
                WorkerCapacityBaseWoodCost = math.max(1, tuning.WorkerCapacityBaseWoodCost),
                WorkerCapacityBaseIronCost = math.max(1, tuning.WorkerCapacityBaseIronCost),
                WorkerEfficiencyBaseWoodCost = math.max(1, tuning.WorkerEfficiencyBaseWoodCost),
                WorkerEfficiencyBaseIronCost = math.max(1, tuning.WorkerEfficiencyBaseIronCost),
                WorkerBuildingCostGrowthMultiplier = growth,
                WorkerEfficiencyPercentPerLevel = efficiencyPercent,
                ArrowBaseCapacity = math.max(1, tuning.ArrowBaseCapacity),
                ArrowCapacityPerLevel = math.max(1, tuning.ArrowCapacityPerLevel),
                ArrowRefillPackageSize = math.max(1, tuning.ArrowRefillPackageSize),
                ArrowBaseArrowsPerWood = math.max(1, tuning.ArrowBaseArrowsPerWood),
                ArrowArrowsPerWoodPerEfficiencyLevel = math.max(1,
                    tuning.ArrowArrowsPerWoodPerEfficiencyLevel),
                ArrowCapacityBaseWoodCost = math.max(1, tuning.ArrowCapacityBaseWoodCost),
                ArrowCapacityBaseIronCost = math.max(1, tuning.ArrowCapacityBaseIronCost),
                ArrowEfficiencyBaseWoodCost = math.max(1, tuning.ArrowEfficiencyBaseWoodCost),
                ArrowEfficiencyBaseIronCost = math.max(1, tuning.ArrowEfficiencyBaseIronCost),
                ArrowUpgradeCostGrowthMultiplier = arrowGrowth
            };
        }
    }
}
