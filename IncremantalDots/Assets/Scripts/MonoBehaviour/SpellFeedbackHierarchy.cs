using UnityEngine;

namespace DeadWalls
{
    public static class SpellFeedbackHierarchy
    {
        public const string SortingLayer = "Wall";

        public const int OrdinaryHitSortingOrder = 12;
        public const int FrostHitSortingOrder = 48;
        public const int FireballProjectileAuraSortingOrder = 219;
        public const int FireballProjectileSortingOrder = 220;
        public const int BurningGroundFillSortingOrder = 227;
        public const int BurningGroundRingSortingOrder = 228;
        public const int FireballBlastSortingOrder = 230;
        public const int FireballBlastCoreSortingOrder = 231;
        public const int FireballBlastRingSortingOrder = 232;

        public const float FrostHitScaleMultiplier = 3.2f;
        public const float FrostRingStartScale = 1.05f;
        public const float FrostRingEndScale = 2.2f;
        public const float FireballProjectileAuraDiameter = 3.4f;
        public const float FireballProjectileAuraPulse = 0.08f;
        public const float FireballBlastDiameterMultiplier = 2.4f;
        public const float FireballBlastCoreDiameterMultiplier = 2.05f;
        public const float FireballBlastRingDiameterMultiplier = 2.8f;
        public const float FireballBlastRingStartScale = 0.9f;
        public const float FireballBlastRingEndScale = 1.18f;

        public static Color FrostHitColor => new Color(0.72f, 0.94f, 1f, 1f);
        public static Color FrostRingColor => new Color(0.12f, 0.78f, 1f, 0.88f);
        public static Color FireballProjectileAuraColor => new Color(1f, 0.38f, 0.06f, 0.32f);
        public static Color FireballBlastCoreColor => new Color(1f, 0.12f, 0.01f, 0.72f);
        public static Color FireballBlastRingColor => new Color(1f, 0.42f, 0.05f, 1f);
        public static Color SecondBlastCoreColor => new Color(1f, 0.72f, 0.24f, 0.82f);
        public static Color SecondBlastRingColor => new Color(1f, 0.82f, 0.32f, 1f);
        public static Color BurningGroundFillColor => new Color(0.48f, 0.07f, 0.015f, 0.26f);
        public static Color BurningGroundRingColor => new Color(1f, 0.28f, 0.025f, 0.72f);

        public static float ResolveFrostHitScale(float baseScale, float multiplier)
        {
            return Mathf.Max(0.001f, baseScale) * Mathf.Max(1f, multiplier);
        }

        public static float ResolveFrostRingScale(
            float progress01,
            float startScale,
            float endScale)
        {
            return Mathf.Lerp(
                Mathf.Max(0.01f, startScale),
                Mathf.Max(startScale, endScale),
                Mathf.Clamp01(progress01));
        }

        public static float ResolveProjectileAuraDiameter(
            float baseDiameter,
            float pulseDepth,
            float time)
        {
            float pulse = 1f + Mathf.Sin(time * 9f) * Mathf.Clamp(pulseDepth, 0f, 0.25f);
            return Mathf.Max(0.1f, baseDiameter) * pulse;
        }

        public static float ResolveFireballBlastScale(
            float radius,
            float spriteWorldSize,
            float diameterMultiplier)
        {
            if (spriteWorldSize <= 0.01f)
                return 1f;

            return Mathf.Max(0.1f, radius)
                * Mathf.Max(1f, diameterMultiplier)
                / spriteWorldSize;
        }

        public static float ResolveFireballBlastRingDiameter(
            float radius,
            float diameterMultiplier)
        {
            return Mathf.Max(0.1f, radius) * Mathf.Max(1f, diameterMultiplier);
        }

        public static float ResolveFireballBlastRingScale(
            float baseDiameter,
            float progress01,
            float startScale,
            float endScale)
        {
            return Mathf.Max(0.1f, baseDiameter) * Mathf.Lerp(
                Mathf.Max(0.01f, startScale),
                Mathf.Max(startScale, endScale),
                Mathf.Clamp01(progress01));
        }

        public static Color ResolveFadingColor(Color baseColor, float progress01)
        {
            baseColor.a *= 1f - Mathf.Clamp01(progress01);
            return baseColor;
        }

        public static float ResolveBurningGroundDiameter(float radius, float time)
        {
            float pulse = 1f + Mathf.Sin(time * 4.5f) * 0.035f;
            return Mathf.Max(0.1f, radius) * 2f * pulse;
        }

        public static Color ResolveBurningGroundColor(
            Color baseColor,
            float remainingDuration,
            float totalDuration)
        {
            float normalized = Mathf.Clamp01(
                remainingDuration / Mathf.Max(0.01f, totalDuration));
            float fade = Mathf.Clamp01(normalized / 0.22f);
            baseColor.a *= fade;
            return baseColor;
        }
    }
}
