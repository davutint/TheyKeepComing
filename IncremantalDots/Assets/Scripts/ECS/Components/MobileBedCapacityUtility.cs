using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileBedCapacityUtility
    {
        public const int DefaultInitialCapacity = 60;
        public const int BaseWoodCost = MobileEconomyPriceTuningUtility.DefaultBedBaseWoodCost;
        public const int CostGrowthCapacityInterval =
            MobileEconomyPriceTuningUtility.DefaultBedCostGrowthCapacityInterval;

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
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return GetNextPurchaseWoodCost(state, tuning);
        }

        public static int GetNextPurchaseWoodCost(in MobileBedCapacityState state,
            in MobileEconomyPriceTuning tuning)
        {
            var safeTuning = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            return TryCalculateUnitWoodCost(GetOwnedCapacityGrowthCount(state), safeTuning,
                out int woodCost)
                ? woodCost
                : int.MaxValue;
        }

        public static int GetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity)
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return GetPurchaseWoodCost(state, requestedCapacity, tuning);
        }

        public static int GetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity,
            in MobileEconomyPriceTuning tuning)
        {
            TryGetPurchaseWoodCost(state, requestedCapacity, tuning, out int woodCost);
            return woodCost;
        }

        public static bool TryGetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity,
            out int woodCost)
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            return TryGetPurchaseWoodCost(state, requestedCapacity, tuning, out woodCost);
        }

        public static bool TryGetPurchaseWoodCost(in MobileBedCapacityState state, int requestedCapacity,
            in MobileEconomyPriceTuning tuning, out int woodCost)
        {
            woodCost = 0;
            int addedCapacity = GetPurchasableIncrement(state, requestedCapacity);
            if (addedCapacity <= 0)
                return false;

            var safeTuning = MobileEconomyPriceTuningUtility.Sanitize(tuning);
            long totalCost = 0;
            long startingGrowthCount = GetOwnedCapacityGrowthCount(state);
            for (int index = 0; index < addedCapacity; index++)
            {
                if (!TryCalculateUnitWoodCost(startingGrowthCount + index, safeTuning,
                        out int unitCost)
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

        private static bool TryCalculateUnitWoodCost(long ownedCapacityGrowthCount,
            in MobileEconomyPriceTuning tuning, out int woodCost)
        {
            long growthCount = ownedCapacityGrowthCount < 0 ? 0 : ownedCapacityGrowthCount;
            decimal interval = tuning.BedCostGrowthCapacityInterval;
            decimal scale = 1m + growthCount / interval;
            decimal cost = tuning.BedBaseWoodCost * scale * scale;
            if (cost > int.MaxValue)
            {
                woodCost = int.MaxValue;
                return false;
            }

            woodCost = (int)decimal.Ceiling(cost);
            return true;
        }
    }
}
