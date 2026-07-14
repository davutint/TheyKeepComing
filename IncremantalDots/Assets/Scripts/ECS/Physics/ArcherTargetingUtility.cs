using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public static class ArcherTargetingUtility
    {
        private const float DistanceTieEpsilon = 0.000001f;
        private const float HealthEpsilon = 0.0001f;

        public static int GetCellRadius(float range, float cellSize)
        {
            float safeCellSize = math.max(0.0001f, cellSize);
            return (int)math.ceil(math.max(0f, range) / safeCellSize) + 1;
        }

        public static bool HasUnreservedHealth(float currentHp, float reservedDamage)
        {
            return currentHp > math.max(0f, reservedDamage) + HealthEpsilon;
        }

        public static bool IsBetterCandidate(
            float distanceSq,
            Entity candidate,
            float bestDistanceSq,
            Entity best)
        {
            if (best == Entity.Null || distanceSq < bestDistanceSq - DistanceTieEpsilon)
                return true;

            if (math.abs(distanceSq - bestDistanceSq) > DistanceTieEpsilon)
                return false;

            return candidate.Index < best.Index
                || (candidate.Index == best.Index && candidate.Version < best.Version);
        }
    }
}
