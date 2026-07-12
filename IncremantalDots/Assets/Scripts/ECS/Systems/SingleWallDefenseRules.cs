using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Tek Wall savunma modelinin saf kurallari.
    /// Wall sifira dustukten sonra hicbir onarim veya iyilestirme onu diriltemez.
    /// </summary>
    public static class SingleWallDefenseRules
    {
        public static float ApplyDamage(float currentHp, float damage, float multiplier = 1f)
        {
            float safeCurrentHp = math.max(0f, currentHp);
            float appliedDamage = math.max(0f, damage) * math.max(0f, multiplier);
            return math.max(0f, safeCurrentHp - appliedDamage);
        }

        public static bool IsDestroyed(float currentHp)
        {
            return currentHp <= 0f;
        }

        public static bool IsRepairPhaseAllowed(SiegeCyclePhase phase)
        {
            return phase == SiegeCyclePhase.Day || phase == SiegeCyclePhase.Dusk;
        }

        public static float GetHealthRatio(float currentHp, float maxHp)
        {
            if (maxHp <= 0f)
                return 1f;

            return math.saturate(currentHp / maxHp);
        }

        public static float RepairToFull(float currentHp, float maxHp)
        {
            return IsDestroyed(currentHp) ? 0f : math.max(0f, maxHp);
        }

        public static float HealByMaxPercent(float currentHp, float maxHp, float percent)
        {
            if (IsDestroyed(currentHp))
                return 0f;

            float safeMaxHp = math.max(0f, maxHp);
            float healAmount = safeMaxHp * math.max(0f, percent);
            return math.min(safeMaxHp, math.max(0f, currentHp) + healAmount);
        }
    }
}
