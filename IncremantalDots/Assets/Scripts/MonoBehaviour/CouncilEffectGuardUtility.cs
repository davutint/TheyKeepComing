using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Council etkilerinin ana population, archer ve quantity-only difficulty
    /// sozlesmelerini asamayacagi saf sayisal kapilar.
    /// </summary>
    public static class CouncilEffectGuardUtility
    {
        public const float MinimumNightCountMultiplier = 0.25f;
        public const float MaximumNightCountMultiplier = 2f;

        public static MobilePopulationArrivalBudget CalculatePopulationGain(
            int requestedPopulation,
            int currentPopulation,
            int totalBedCapacity,
            int availableFood,
            int foodCostPerArrival)
        {
            return MobilePopulationArrivalUtility.CalculateBudget(
                requestedPopulation,
                currentPopulation,
                totalBedCapacity,
                availableFood,
                foodCostPerArrival);
        }

        public static int GetAllowedFreeArcherGain(
            int requestedArchers,
            int currentTotalArchers,
            int availableWorkers)
        {
            int capacityAllowed = ArcherCapacityUtility.GetAllowedAdditionalCount(
                currentTotalArchers,
                requestedArchers);
            return math.min(capacityAllowed, math.max(0, availableWorkers));
        }

        public static float ResolveNightCountMultiplier(float rateDelta)
        {
            if (!math.isfinite(rateDelta))
                return 1f;

            return math.clamp(
                1f + rateDelta,
                MinimumNightCountMultiplier,
                MaximumNightCountMultiplier);
        }
    }
}
