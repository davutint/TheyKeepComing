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
        public const float DefaultCellSize = 0.5f;

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
    }
}
