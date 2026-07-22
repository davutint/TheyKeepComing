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

        public static int ResolveIdlePopulation(
            MobilePopulationAllocation allocation,
            int populationTotal,
            int archerCount)
        {
            int availableAfterArchers = math.max(
                0,
                math.max(0, populationTotal) - math.max(0, archerCount));
            return math.max(0, availableAfterArchers - TotalWorkers(allocation));
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

        public static void SetTargetRatioBps(ref MobilePopulationAllocation allocation,
            int resourceIndex, int targetRatioBps)
        {
            resourceIndex = math.clamp(resourceIndex, 0, 3);
            targetRatioBps = math.clamp(targetRatioBps, 0, RatioScale);
            int remainingRatioBps = RatioScale - targetRatioBps;

            long woodWeight = resourceIndex == 0 ? 0L : math.max(0, allocation.WoodTargetRatioBps);
            long stoneWeight = resourceIndex == 1 ? 0L : math.max(0, allocation.StoneTargetRatioBps);
            long ironWeight = resourceIndex == 2 ? 0L : math.max(0, allocation.IronTargetRatioBps);
            long foodWeight = resourceIndex == 3 ? 0L : math.max(0, allocation.FoodTargetRatioBps);
            long totalWeight = woodWeight + stoneWeight + ironWeight + foodWeight;

            if (totalWeight <= 0 && remainingRatioBps > 0)
            {
                woodWeight = resourceIndex == 0 ? 0L : 1L;
                stoneWeight = resourceIndex == 1 ? 0L : 1L;
                ironWeight = resourceIndex == 2 ? 0L : 1L;
                foodWeight = resourceIndex == 3 ? 0L : 1L;
                totalWeight = woodWeight + stoneWeight + ironWeight + foodWeight;
            }

            int woodRatio = totalWeight > 0 ? (int)(woodWeight * remainingRatioBps / totalWeight) : 0;
            int stoneRatio = totalWeight > 0 ? (int)(stoneWeight * remainingRatioBps / totalWeight) : 0;
            int ironRatio = totalWeight > 0 ? (int)(ironWeight * remainingRatioBps / totalWeight) : 0;
            int foodRatio = totalWeight > 0 ? (int)(foodWeight * remainingRatioBps / totalWeight) : 0;
            long woodRemainder = totalWeight > 0 ? woodWeight * remainingRatioBps % totalWeight : -1L;
            long stoneRemainder = totalWeight > 0 ? stoneWeight * remainingRatioBps % totalWeight : -1L;
            long ironRemainder = totalWeight > 0 ? ironWeight * remainingRatioBps % totalWeight : -1L;
            long foodRemainder = totalWeight > 0 ? foodWeight * remainingRatioBps % totalWeight : -1L;
            int undistributed = remainingRatioBps - woodRatio - stoneRatio - ironRatio - foodRatio;

            for (int i = 0; i < undistributed; i++)
            {
                int best = -1;
                long bestRemainder = long.MinValue;
                SelectRemainderCandidate(0, resourceIndex, woodRemainder, ref best, ref bestRemainder);
                SelectRemainderCandidate(1, resourceIndex, stoneRemainder, ref best, ref bestRemainder);
                SelectRemainderCandidate(2, resourceIndex, ironRemainder, ref best, ref bestRemainder);
                SelectRemainderCandidate(3, resourceIndex, foodRemainder, ref best, ref bestRemainder);

                switch (best)
                {
                    case 0: woodRatio++; woodRemainder = -1L; break;
                    case 1: stoneRatio++; stoneRemainder = -1L; break;
                    case 2: ironRatio++; ironRemainder = -1L; break;
                    case 3: foodRatio++; foodRemainder = -1L; break;
                }
            }

            allocation.WoodTargetRatioBps = resourceIndex == 0 ? targetRatioBps : woodRatio;
            allocation.StoneTargetRatioBps = resourceIndex == 1 ? targetRatioBps : stoneRatio;
            allocation.IronTargetRatioBps = resourceIndex == 2 ? targetRatioBps : ironRatio;
            allocation.FoodTargetRatioBps = resourceIndex == 3 ? targetRatioBps : foodRatio;
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

                // Hedeflenen resource'lar dolduysa kimseyi asker rezervi olarak bos birakma.
                // Kalan kisi ilk musait resource'a sabit Wood -> Stone -> Iron -> Food
                // sirasiyla overflow worker olarak atanir.
                if (best < 0)
                    best = FindFirstAvailableResource(allocation);

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

        public static int RebalanceAvailableWorkers(
            ref MobilePopulationAllocation allocation,
            int populationTotal,
            int archerCount)
        {
            int availableWorkers = math.max(
                0,
                math.max(0, populationTotal) - math.max(0, archerCount));
            allocation.WoodWorkers = 0;
            allocation.StoneWorkers = 0;
            allocation.IronWorkers = 0;
            allocation.FoodWorkers = 0;
            return AutoAssignNewPopulation(ref allocation, availableWorkers);
        }

        public static int RemoveWorkersInResourceOrder(
            ref MobilePopulationAllocation allocation,
            int amount)
        {
            int remaining = math.max(0, amount);
            Remove(ref allocation.WoodWorkers, ref remaining);
            Remove(ref allocation.StoneWorkers, ref remaining);
            Remove(ref allocation.IronWorkers, ref remaining);
            Remove(ref allocation.FoodWorkers, ref remaining);
            return math.max(0, amount) - remaining;
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

        private static int FindFirstAvailableResource(MobilePopulationAllocation allocation)
        {
            if (HasCapacity(allocation.WoodWorkers, allocation.WoodWorkerCapacity)) return 0;
            if (HasCapacity(allocation.StoneWorkers, allocation.StoneWorkerCapacity)) return 1;
            if (HasCapacity(allocation.IronWorkers, allocation.IronWorkerCapacity)) return 2;
            if (HasCapacity(allocation.FoodWorkers, allocation.FoodWorkerCapacity)) return 3;
            return -1;
        }

        private static bool HasCapacity(int workers, int capacity)
        {
            return capacity <= 0 || math.max(0, workers) < capacity;
        }

        private static void Remove(ref int workers, ref int remaining)
        {
            if (remaining <= 0 || workers <= 0)
                return;

            int removed = math.min(workers, remaining);
            workers -= removed;
            remaining -= removed;
        }

        private static void SelectRemainderCandidate(int index, int excludedIndex, long remainder,
            ref int best, ref long bestRemainder)
        {
            if (index == excludedIndex || remainder <= bestRemainder)
                return;

            best = index;
            bestRemainder = remainder;
        }
    }
}
