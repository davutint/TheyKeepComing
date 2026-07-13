using Unity.Mathematics;

namespace DeadWalls
{
    public enum WorkerVisualDensityLevel : byte
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public static class WorkerVisualRepresentationUtility
    {
        public const int LowActualWorkerMax = 12;
        public const int MediumActualWorkerMax = 60;
        public const int MediumActualWorkersPerVisual = 4;
        public const int HighActualWorkersPerVisual = 20;
        public const int MediumVisualWorkerMax = LowActualWorkerMax
            + (MediumActualWorkerMax - LowActualWorkerMax) / MediumActualWorkersPerVisual;
        public const int MaxVisualWorkersPerResource = 32;

        public static WorkerVisualDensityLevel GetDensityLevel(int actualWorkerCount)
        {
            actualWorkerCount = math.max(0, actualWorkerCount);
            if (actualWorkerCount == 0)
                return WorkerVisualDensityLevel.None;
            if (actualWorkerCount <= LowActualWorkerMax)
                return WorkerVisualDensityLevel.Low;
            if (actualWorkerCount <= MediumActualWorkerMax)
                return WorkerVisualDensityLevel.Medium;
            return WorkerVisualDensityLevel.High;
        }

        public static int GetRepresentativeCount(int actualWorkerCount)
        {
            actualWorkerCount = math.max(0, actualWorkerCount);
            if (actualWorkerCount <= LowActualWorkerMax)
                return actualWorkerCount;

            if (actualWorkerCount <= MediumActualWorkerMax)
            {
                return LowActualWorkerMax + CeilPositive(
                    actualWorkerCount - LowActualWorkerMax,
                    MediumActualWorkersPerVisual);
            }

            int highCount = MediumVisualWorkerMax + CeilPositive(
                actualWorkerCount - MediumActualWorkerMax,
                HighActualWorkersPerVisual);
            return math.min(MaxVisualWorkersPerResource, highCount);
        }

        public static int4 GetRepresentativeCounts(MobilePopulationAllocation allocation)
        {
            return new int4(
                GetRepresentativeCount(allocation.WoodWorkers),
                GetRepresentativeCount(allocation.StoneWorkers),
                GetRepresentativeCount(allocation.IronWorkers),
                GetRepresentativeCount(allocation.FoodWorkers));
        }

        public static int GetRepresentativeTotal(MobilePopulationAllocation allocation)
        {
            return math.csum(GetRepresentativeCounts(allocation));
        }

        private static int CeilPositive(int value, int divisor)
        {
            return (math.max(0, value) + divisor - 1) / divisor;
        }
    }
}
