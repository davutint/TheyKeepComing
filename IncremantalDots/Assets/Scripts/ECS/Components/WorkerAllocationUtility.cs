using Unity.Mathematics;

namespace DeadWalls
{
    public static class WorkerAllocationUtility
    {
        public const int RatioScale = 10_000;

        public static int TotalWorkers(MobilePopulationAllocation allocation)
        {
            return math.max(0, allocation.WoodWorkers)
                + math.max(0, allocation.StoneWorkers)
                + math.max(0, allocation.IronWorkers)
                + math.max(0, allocation.FoodWorkers);
        }

        public static void InitializeTargetsFromCurrent(ref MobilePopulationAllocation allocation)
        {
            allocation.WoodTargetRatioBps = math.max(0, allocation.WoodWorkers);
            allocation.StoneTargetRatioBps = math.max(0, allocation.StoneWorkers);
            allocation.IronTargetRatioBps = math.max(0, allocation.IronWorkers);
            allocation.FoodTargetRatioBps = math.max(0, allocation.FoodWorkers);
            NormalizeTargetRatios(ref allocation);
        }

        public static void NormalizeTargetRatios(ref MobilePopulationAllocation allocation)
        {
            long woodWeight = math.max(0, allocation.WoodTargetRatioBps);
            long stoneWeight = math.max(0, allocation.StoneTargetRatioBps);
            long ironWeight = math.max(0, allocation.IronTargetRatioBps);
            long foodWeight = math.max(0, allocation.FoodTargetRatioBps);
            long totalWeight = woodWeight + stoneWeight + ironWeight + foodWeight;

            if (totalWeight <= 0)
            {
                allocation.WoodTargetRatioBps = RatioScale / 4;
                allocation.StoneTargetRatioBps = RatioScale / 4;
                allocation.IronTargetRatioBps = RatioScale / 4;
                allocation.FoodTargetRatioBps = RatioScale / 4;
                return;
            }

            allocation.WoodTargetRatioBps = (int)(woodWeight * RatioScale / totalWeight);
            allocation.StoneTargetRatioBps = (int)(stoneWeight * RatioScale / totalWeight);
            allocation.IronTargetRatioBps = (int)(ironWeight * RatioScale / totalWeight);
            allocation.FoodTargetRatioBps = (int)(foodWeight * RatioScale / totalWeight);

            long woodRemainder = woodWeight * RatioScale % totalWeight;
            long stoneRemainder = stoneWeight * RatioScale % totalWeight;
            long ironRemainder = ironWeight * RatioScale % totalWeight;
            long foodRemainder = foodWeight * RatioScale % totalWeight;
            int remaining = RatioScale
                - allocation.WoodTargetRatioBps
                - allocation.StoneTargetRatioBps
                - allocation.IronTargetRatioBps
                - allocation.FoodTargetRatioBps;

            for (int i = 0; i < remaining; i++)
            {
                int best = 0;
                long bestRemainder = woodRemainder;
                if (stoneRemainder > bestRemainder)
                {
                    best = 1;
                    bestRemainder = stoneRemainder;
                }
                if (ironRemainder > bestRemainder)
                {
                    best = 2;
                    bestRemainder = ironRemainder;
                }
                if (foodRemainder > bestRemainder)
                    best = 3;

                switch (best)
                {
                    case 0:
                        allocation.WoodTargetRatioBps++;
                        woodRemainder = -1;
                        break;
                    case 1:
                        allocation.StoneTargetRatioBps++;
                        stoneRemainder = -1;
                        break;
                    case 2:
                        allocation.IronTargetRatioBps++;
                        ironRemainder = -1;
                        break;
                    default:
                        allocation.FoodTargetRatioBps++;
                        foodRemainder = -1;
                        break;
                }
            }
        }

        public static int BeginPopulationUpdate(ref MobilePopulationAllocation allocation, int populationTotal)
        {
            populationTotal = math.max(0, populationTotal);
            if (allocation.WoodTargetRatioBps
                + allocation.StoneTargetRatioBps
                + allocation.IronTargetRatioBps
                + allocation.FoodTargetRatioBps <= 0)
            {
                InitializeTargetsFromCurrent(ref allocation);
            }
            else
            {
                NormalizeTargetRatios(ref allocation);
            }

            if (allocation.AutoAllocationInitialized == 0)
            {
                allocation.AutoAllocationInitialized = 1;
                allocation.LastObservedPopulation = populationTotal;
                return 0;
            }

            int addedPopulation = math.max(0, populationTotal - allocation.LastObservedPopulation);
            allocation.LastObservedPopulation = populationTotal;
            return addedPopulation;
        }

        public static int AutoAssignNewPopulation(ref MobilePopulationAllocation allocation, int amount)
        {
            amount = math.max(0, amount);
            int assigned = 0;
            for (int i = 0; i < amount; i++)
            {
                int totalAfterAssignment = TotalWorkers(allocation) + 1;
                int best = -1;
                long bestScore = long.MinValue;

                EvaluateCandidate(0, allocation.WoodWorkers, allocation.WoodWorkerCapacity,
                    allocation.WoodTargetRatioBps, totalAfterAssignment, ref best, ref bestScore);
                EvaluateCandidate(1, allocation.StoneWorkers, allocation.StoneWorkerCapacity,
                    allocation.StoneTargetRatioBps, totalAfterAssignment, ref best, ref bestScore);
                EvaluateCandidate(2, allocation.IronWorkers, allocation.IronWorkerCapacity,
                    allocation.IronTargetRatioBps, totalAfterAssignment, ref best, ref bestScore);
                EvaluateCandidate(3, allocation.FoodWorkers, allocation.FoodWorkerCapacity,
                    allocation.FoodTargetRatioBps, totalAfterAssignment, ref best, ref bestScore);

                if (best < 0)
                    break;

                switch (best)
                {
                    case 0: allocation.WoodWorkers++; break;
                    case 1: allocation.StoneWorkers++; break;
                    case 2: allocation.IronWorkers++; break;
                    default: allocation.FoodWorkers++; break;
                }
                assigned++;
            }

            return assigned;
        }

        private static void EvaluateCandidate(int index, int workers, int capacity, int ratio,
            int totalAfterAssignment, ref int best, ref long bestScore)
        {
            if (ratio <= 0 || (capacity > 0 && workers >= capacity))
                return;

            long score = (long)ratio * totalAfterAssignment - (long)math.max(0, workers) * RatioScale;
            if (score <= bestScore)
                return;

            best = index;
            bestScore = score;
        }
    }
}
