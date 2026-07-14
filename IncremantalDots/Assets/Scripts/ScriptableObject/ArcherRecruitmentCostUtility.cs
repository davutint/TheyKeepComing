using System;

namespace DeadWalls
{
    /// <summary>
    /// Okcu satin alma ve Basic retrain maliyetlerinin hedef tur sayisina gore
    /// buyuyen, int-safe ortak matematik owner'i.
    /// </summary>
    public static class ArcherRecruitmentCostUtility
    {
        public const int DefaultGrowthInterval = 25;
        public const float DefaultGrowthExponent = 2f;

        public static ResourceCost GetScaledCost(
            ResourceCost baseCost,
            int currentTargetTypeCount,
            int growthInterval,
            float growthExponent)
        {
            int safeCount = Math.Max(0, currentTargetTypeCount);
            int safeInterval = Math.Max(1, growthInterval);
            double safeExponent = double.IsNaN(growthExponent)
                || double.IsInfinity(growthExponent)
                || growthExponent <= 0f
                    ? 1d
                    : growthExponent;
            double scale = Math.Pow(1d + (double)safeCount / safeInterval, safeExponent);

            return new ResourceCost(
                ScaleComponent(baseCost.Wood, scale),
                ScaleComponent(baseCost.Stone, scale),
                ScaleComponent(baseCost.Iron, scale),
                ScaleComponent(baseCost.Food, scale));
        }

        private static int ScaleComponent(int baseValue, double scale)
        {
            if (baseValue <= 0)
                return 0;

            double scaled = Math.Ceiling(baseValue * scale);
            return double.IsNaN(scaled) || scaled >= int.MaxValue
                ? int.MaxValue
                : (int)scaled;
        }
    }
}
