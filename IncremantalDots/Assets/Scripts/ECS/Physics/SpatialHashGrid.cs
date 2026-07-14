using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct SpatialHashGridSingleton : IComponentData
    {
        public float CellSize;
    }

    public static class SpatialHash
    {
        public const float DefaultCellSize = 0.35f;
        public const float TargetCellSize = 2f;

        public static int2 GetCell(float2 pos, float cellSize)
        {
            return new int2(
                (int)math.floor(pos.x / cellSize),
                (int)math.floor(pos.y / cellSize)
            );
        }

        public static int CellToKey(int2 cell)
        {
            unchecked
            {
                return (int)((uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663));
            }
        }

        public static int Hash(float2 pos, float cellSize)
        {
            return CellToKey(GetCell(pos, cellSize));
        }

        public static float DistanceSqToCell(float2 position, int2 cell, float cellSize)
        {
            float safeCellSize = math.max(0.0001f, cellSize);
            float2 min = new float2(cell.x * safeCellSize, cell.y * safeCellSize);
            float2 max = min + safeCellSize;
            float2 delta = math.max(math.max(min - position, position - max), float2.zero);
            return math.lengthsq(delta);
        }
    }
}
