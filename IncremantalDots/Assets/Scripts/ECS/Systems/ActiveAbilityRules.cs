namespace DeadWalls
{
    /// <summary>
    /// V1 aktif ability guard'larinin saf otoritesi. Kaynak/mana parametresi bilerek yoktur:
    /// ability'ler yalniz unlock, cooldown, aktif etki, phase ve Wall state'iyle sinirlanir.
    /// </summary>
    public static class ActiveAbilityRules
    {
        public static bool CanUseRally(
            bool unlocked,
            float cooldownRemaining,
            float activeRemaining,
            bool isGameOver,
            bool isLevelUpPending)
        {
            return unlocked
                && cooldownRemaining <= 0f
                && activeRemaining <= 0f
                && !isGameOver
                && !isLevelUpPending;
        }

        public static bool CanUseEmergencyRepair(
            bool unlocked,
            float cooldownRemaining,
            SiegeCyclePhase phase,
            float currentHp,
            float maxHp,
            bool isGameOver,
            bool isLevelUpPending)
        {
            return unlocked
                && cooldownRemaining <= 0f
                && phase == SiegeCyclePhase.Night
                && currentHp > 0f
                && currentHp < maxHp
                && maxHp > 0f
                && !isGameOver
                && !isLevelUpPending;
        }
    }
}
