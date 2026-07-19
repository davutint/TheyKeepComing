using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Dusman olumundeki Grave Essence roll'unun saf, Burst-uyumlu ve test edilebilir owner'i.
    /// Roll kill ordinal + run spawn stream state + authored seed ile stateless hesaplanir;
    /// boylece parallel death sirasi toplam drop sayisini degistirmez.
    /// </summary>
    public static class GraveEssenceDropUtility
    {
        private const float UInt24ToUnit = 1f / 16777216f;
        private const uint FallbackSeed = 0x6E624EB7u;

        public static bool ShouldDrop(
            float chance,
            uint configuredSeed,
            uint spawnRandomState,
            int killOrdinal)
        {
            if (chance <= 0f || killOrdinal <= 0)
                return false;
            if (chance >= 1f)
                return true;

            uint seed = configuredSeed == 0u ? FallbackSeed : configuredSeed;
            uint ordinal = (uint)killOrdinal;
            uint hash = math.hash(new uint4(
                seed,
                spawnRandomState,
                ordinal,
                ordinal * 0x9E3779B9u));
            float unit = (hash >> 8) * UInt24ToUnit;
            return unit < math.saturate(chance);
        }
    }
}
