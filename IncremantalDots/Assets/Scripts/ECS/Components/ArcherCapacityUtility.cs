using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Basic, Rapid ve Frost toplam okcu kapasitesinin tek sayisal sozlesmesi.
    /// Population maliyeti ve type dagilimi bu ortak entity cap'inden bagimsizdir.
    /// </summary>
    public static class ArcherCapacityUtility
    {
        public const int MaxTotalArchers = 1000;

        public static int GetRemainingCapacity(int currentTotal)
        {
            return math.max(0, MaxTotalArchers - math.max(0, currentTotal));
        }

        public static bool CanAdd(int currentTotal, int requestedCount = 1)
        {
            return requestedCount > 0
                && requestedCount <= GetRemainingCapacity(currentTotal);
        }

        public static int GetAllowedAdditionalCount(int currentTotal, int requestedCount)
        {
            return math.min(math.max(0, requestedCount), GetRemainingCapacity(currentTotal));
        }
    }
}
