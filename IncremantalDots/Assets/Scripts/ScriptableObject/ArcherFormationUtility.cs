using System;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// 40 outside tile x 25 local slot formasyonunun deterministic matematik owner'i.
    /// Runtime entity veya Tilemap state'i tutmaz.
    /// </summary>
    public static class ArcherFormationUtility
    {
        public const int CurrentVersion = 1;
        public const int RequiredTileCount = 40;
        public const int SlotsPerTile = 25;
        public const int TotalCapacity = RequiredTileCount * SlotsPerTile;
        public const float DefaultSafeInset = 0.18f;
        public const float DefaultMinimumLocalDistance = 0.055f;
        public const int DefaultCandidateAttempts = 128;

        public static int NormalizeVersion(int version)
        {
            return version == CurrentVersion ? version : CurrentVersion;
        }

        public static int GetTileIndex(int archerIndex)
        {
            return archerIndex >= 0 && archerIndex < TotalCapacity
                ? archerIndex % RequiredTileCount
                : -1;
        }

        public static int GetLocalSlotIndex(int archerIndex)
        {
            return archerIndex >= 0 && archerIndex < TotalCapacity
                ? archerIndex / RequiredTileCount
                : -1;
        }

        public static Vector3Int[] CreateCanonicalV1TileCoordinates()
        {
            var coordinates = new Vector3Int[RequiredTileCount];
            int index = 0;
            coordinates[index++] = new Vector3Int(0, 0, 0);
            coordinates[index++] = new Vector3Int(1, 1, 0);

            for (int distance = 1; distance <= 19; distance++)
            {
                coordinates[index++] = new Vector3Int(-distance, -distance, 0);
                coordinates[index++] = new Vector3Int(distance + 1, distance + 1, 0);
            }

            return coordinates;
        }

        public static bool MatchesCanonicalV1(Vector3Int[] coordinates)
        {
            if (coordinates == null || coordinates.Length != RequiredTileCount)
                return false;

            Vector3Int[] expected = CreateCanonicalV1TileCoordinates();
            for (int i = 0; i < expected.Length; i++)
            {
                if (coordinates[i] != expected[i])
                    return false;
            }

            return true;
        }

        public static bool TryGenerateTileOffsets(
            Vector3Int tileCoordinate,
            Vector2 rightVertex,
            Vector2 topVertex,
            int version,
            int slotCount,
            float safeInset,
            float minimumDistance,
            int candidateAttempts,
            out Vector2[] offsets)
        {
            offsets = null;
            if (NormalizeVersion(version) != version
                || slotCount <= 0
                || rightVertex.sqrMagnitude <= 0.0000001f
                || topVertex.sqrMagnitude <= 0.0000001f)
            {
                return false;
            }

            float safeScale = 1f - Mathf.Clamp(safeInset, 0f, 0.95f);
            float minimumDistanceSq = Mathf.Max(0f, minimumDistance);
            minimumDistanceSq *= minimumDistanceSq;
            int safeAttempts = Mathf.Max(1, candidateAttempts);
            var result = new Vector2[slotCount];

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                int attemptCount = slotIndex == 0 ? 1 : safeAttempts;
                float bestDistanceSq = float.NegativeInfinity;
                Vector2 bestCandidate = default;

                for (int attempt = 0; attempt < attemptCount; attempt++)
                {
                    Vector2 normalized = GenerateNormalizedDiamondPoint(
                        tileCoordinate, slotIndex, attempt, version) * safeScale;
                    Vector2 candidate = rightVertex * normalized.x + topVertex * normalized.y;
                    float nearestDistanceSq = slotIndex == 0
                        ? float.PositiveInfinity
                        : FindNearestDistanceSq(result, slotIndex, candidate);

                    if (nearestDistanceSq <= bestDistanceSq)
                        continue;

                    bestDistanceSq = nearestDistanceSq;
                    bestCandidate = candidate;
                }

                if (slotIndex > 0 && bestDistanceSq + 0.0000001f < minimumDistanceSq)
                    return false;

                result[slotIndex] = bestCandidate;
            }

            offsets = result;
            return true;
        }

        public static bool IsInsideDiamond(
            Vector2 offset,
            Vector2 rightVertex,
            Vector2 topVertex,
            float safeInset,
            float epsilon = 0.0001f)
        {
            float determinant = rightVertex.x * topVertex.y - rightVertex.y * topVertex.x;
            if (Mathf.Abs(determinant) <= 0.0000001f)
                return false;

            float normalizedX = (offset.x * topVertex.y - offset.y * topVertex.x) / determinant;
            float normalizedY = (rightVertex.x * offset.y - rightVertex.y * offset.x) / determinant;
            float limit = 1f - Mathf.Clamp(safeInset, 0f, 0.95f);
            return Mathf.Abs(normalizedX) + Mathf.Abs(normalizedY) <= limit + Mathf.Max(0f, epsilon);
        }

        private static float FindNearestDistanceSq(Vector2[] offsets, int count, Vector2 candidate)
        {
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                nearest = Mathf.Min(nearest, (offsets[i] - candidate).sqrMagnitude);

            return nearest;
        }

        private static Vector2 GenerateNormalizedDiamondPoint(
            Vector3Int tileCoordinate, int slotIndex, int attempt, int version)
        {
            uint seed = BuildSeed(tileCoordinate, slotIndex, attempt, version);
            float x = ToSignedFloat(Hash(seed ^ 0xa511e9b3u));
            float y = ToSignedFloat(Hash(seed ^ 0x63d83595u));

            if (Mathf.Abs(x) + Mathf.Abs(y) > 1f)
            {
                float originalX = x;
                float originalY = y;
                x = Sign(originalX) * (1f - Mathf.Abs(originalY));
                y = Sign(originalY) * (1f - Mathf.Abs(originalX));
            }

            return new Vector2(x, y);
        }

        private static uint BuildSeed(
            Vector3Int tileCoordinate, int slotIndex, int attempt, int version)
        {
            unchecked
            {
                uint seed = (uint)tileCoordinate.x;
                seed ^= Hash((uint)tileCoordinate.y + 0x9e3779b9u);
                seed ^= Hash((uint)tileCoordinate.z + 0x165667b1u);
                seed ^= Hash((uint)slotIndex + 0x85ebca6bu);
                seed ^= Hash((uint)attempt + 0xc2b2ae35u);
                seed ^= Hash((uint)version + 0x27d4eb2fu);
                return Hash(seed);
            }
        }

        private static uint Hash(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return value;
            }
        }

        private static float ToSignedFloat(uint value)
        {
            return ((value & 0x00ffffffu) / 16777216f) * 2f - 1f;
        }

        private static float Sign(float value)
        {
            return value < 0f ? -1f : 1f;
        }
    }
}
