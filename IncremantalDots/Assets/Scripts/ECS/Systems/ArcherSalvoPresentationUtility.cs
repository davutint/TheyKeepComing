namespace DeadWalls
{
    public static class ArcherSalvoPresentationUtility
    {
        public const int MaxVisibleProjectilesPerSalvo = 48;
        public const float VisibleProjectileScale = 1f;
        public const float HiddenProjectileScale = 0f;

        public static int GetSamplingStride(int sourceCount)
        {
            int sanitizedCount = sourceCount < 1 ? 1 : sourceCount;
            return (int)(((long)sanitizedCount + MaxVisibleProjectilesPerSalvo - 1L) /
                         MaxVisibleProjectilesPerSalvo);
        }

        public static bool IsVisualRepresentative(int sourceCount, long shotSequence)
        {
            int stride = GetSamplingStride(sourceCount);
            if (stride <= 1)
                return true;

            ulong normalizedSequence = shotSequence > 0L
                ? (ulong)(shotSequence - 1L)
                : 0UL;
            return normalizedSequence % (ulong)stride == 0UL;
        }

        public static float ResolveProjectileScale(int sourceCount, long shotSequence)
        {
            return IsVisualRepresentative(sourceCount, shotSequence)
                ? VisibleProjectileScale
                : HiddenProjectileScale;
        }

        public static int GetMaximumRepresentativeCount(int sourceCount, int projectileCount)
        {
            if (projectileCount <= 0)
                return 0;

            int stride = GetSamplingStride(sourceCount);
            return (int)(((long)projectileCount + stride - 1L) / stride);
        }
    }
}
