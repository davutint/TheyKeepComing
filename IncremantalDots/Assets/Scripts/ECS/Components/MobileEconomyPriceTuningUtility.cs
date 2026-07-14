using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileEconomyPriceTuningUtility
    {
        public const int DefaultBedBaseWoodCost = 100;
        public const int DefaultBedCostGrowthCapacityInterval = 25;
        public const int DefaultWorkerCapacityBaseWoodCost = 100;
        public const int DefaultWorkerCapacityBaseIronCost = 25;
        public const int DefaultWorkerEfficiencyBaseWoodCost = 150;
        public const int DefaultWorkerEfficiencyBaseIronCost = 50;
        public const double DefaultWorkerBuildingCostGrowthMultiplier = 1.35d;

        public static MobileEconomyPriceTuning Default => new MobileEconomyPriceTuning
        {
            BedBaseWoodCost = DefaultBedBaseWoodCost,
            BedCostGrowthCapacityInterval = DefaultBedCostGrowthCapacityInterval,
            WorkerCapacityBaseWoodCost = DefaultWorkerCapacityBaseWoodCost,
            WorkerCapacityBaseIronCost = DefaultWorkerCapacityBaseIronCost,
            WorkerEfficiencyBaseWoodCost = DefaultWorkerEfficiencyBaseWoodCost,
            WorkerEfficiencyBaseIronCost = DefaultWorkerEfficiencyBaseIronCost,
            WorkerBuildingCostGrowthMultiplier = DefaultWorkerBuildingCostGrowthMultiplier
        };

        public static MobileEconomyPriceTuning Sanitize(in MobileEconomyPriceTuning tuning)
        {
            double growth = math.isfinite(tuning.WorkerBuildingCostGrowthMultiplier)
                ? math.max(1d, tuning.WorkerBuildingCostGrowthMultiplier)
                : DefaultWorkerBuildingCostGrowthMultiplier;

            return new MobileEconomyPriceTuning
            {
                BedBaseWoodCost = math.max(1, tuning.BedBaseWoodCost),
                BedCostGrowthCapacityInterval = math.max(1, tuning.BedCostGrowthCapacityInterval),
                WorkerCapacityBaseWoodCost = math.max(1, tuning.WorkerCapacityBaseWoodCost),
                WorkerCapacityBaseIronCost = math.max(1, tuning.WorkerCapacityBaseIronCost),
                WorkerEfficiencyBaseWoodCost = math.max(1, tuning.WorkerEfficiencyBaseWoodCost),
                WorkerEfficiencyBaseIronCost = math.max(1, tuning.WorkerEfficiencyBaseIronCost),
                WorkerBuildingCostGrowthMultiplier = growth
            };
        }
    }
}
