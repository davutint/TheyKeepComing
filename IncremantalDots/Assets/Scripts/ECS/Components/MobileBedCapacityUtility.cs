using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileBedCapacityUtility
    {
        public const int DefaultInitialCapacity = 60;
        public const int BaseWoodCost = 100;
        public const int CostGrowthCapacityInterval = 25;

        public static MobileBedCapacityState CreateInitial(int baseCapacity)
        {
            return new MobileBedCapacityState
            {
                BaseCapacity = math.max(0, baseCapacity),
                PurchasedCapacity = 0
            };
        }

        public static int GetTotalCapacity(in MobileBedCapacityState state)
        {
            long total = (long)math.max(0, state.BaseCapacity)
                + math.max(0, state.PurchasedCapacity);
            return total >= int.MaxValue ? int.MaxValue : (int)total;
        }

        public static int GetPurchasableIncrement(in MobileBedCapacityState state, int requestedCapacity)
        {
            if (requestedCapacity <= 0)
                return 0;

            int remaining = int.MaxValue - GetTotalCapacity(state);
            return math.min(requestedCapacity, math.max(0, remaining));
        }

        public static int GetOwnedCapacityGrowthCount(in MobileBedCapacityState state)
        {
            return math.max(0, GetTotalCapacity(state) - DefaultInitialCapacity);
        }

        public static int GetNextPurchaseWoodCost(in MobileBedCapacityState state)
        {
            return TryCalculateUnitWoodCost(GetOwnedCapacityGrowthCount(state), out int woodCost)
                ? woodCost
                : int.MaxValue;
        }

        public static int GetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity)
        {
            TryGetPurchaseWoodCost(state, requestedCapacity, out int woodCost);
            return woodCost;
        }

        public static bool TryGetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity,
            out int woodCost)
        {
            woodCost = 0;
            int addedCapacity = GetPurchasableIncrement(state, requestedCapacity);
            if (addedCapacity <= 0)
                return false;

            long totalCost = 0;
            long startingGrowthCount = GetOwnedCapacityGrowthCount(state);
            for (int index = 0; index < addedCapacity; index++)
            {
                if (!TryCalculateUnitWoodCost(startingGrowthCount + index, out int unitCost)
                    || totalCost > int.MaxValue - (long)unitCost)
                {
                    woodCost = int.MaxValue;
                    return false;
                }

                totalCost += unitCost;
            }

            woodCost = (int)totalCost;
            return true;
        }

        public static bool TryAddPurchasedCapacity(ref MobileBedCapacityState state, int requestedCapacity,
            out int addedCapacity)
        {
            addedCapacity = GetPurchasableIncrement(state, requestedCapacity);
            if (addedCapacity <= 0)
                return false;

            long purchased = (long)math.max(0, state.PurchasedCapacity) + addedCapacity;
            state.PurchasedCapacity = purchased >= int.MaxValue ? int.MaxValue : (int)purchased;
            state.BaseCapacity = math.max(0, state.BaseCapacity);
            return true;
        }

        private static bool TryCalculateUnitWoodCost(long ownedCapacityGrowthCount, out int woodCost)
        {
            long growthCount = ownedCapacityGrowthCount < 0 ? 0 : ownedCapacityGrowthCount;
            long scaleNumerator = CostGrowthCapacityInterval + growthCount;
            long squaredScaleNumerator = scaleNumerator * scaleNumerator;
            long scaleDenominator = (long)CostGrowthCapacityInterval * CostGrowthCapacityInterval;
            long maximumRepresentableSquaredNumerator = (long)int.MaxValue * scaleDenominator / BaseWoodCost;
            if (squaredScaleNumerator > maximumRepresentableSquaredNumerator)
            {
                woodCost = int.MaxValue;
                return false;
            }

            long costNumerator = BaseWoodCost * squaredScaleNumerator;
            woodCost = (int)((costNumerator + scaleDenominator - 1) / scaleDenominator);
            return true;
        }
    }
}
