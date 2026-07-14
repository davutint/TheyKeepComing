using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileBedCapacityUtility
    {
        public const int DefaultInitialCapacity = 60;

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
    }
}
