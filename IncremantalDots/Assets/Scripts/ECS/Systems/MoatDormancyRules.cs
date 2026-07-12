namespace DeadWalls
{
    /// <summary>
    /// V1 Blueprint'te Moat gameplay'i yoktur. Eski scene, save, tech veya meta verisi
    /// kalsa bile runtime config bu neutral sozlesmeye cekilir.
    /// </summary>
    public static class MoatDormancyRules
    {
        public const string DeeperMoatNodeId = "moat_dig";
        public const string BurningMoatNodeId = "moat_flame";
        public const string StartingMoatMetaId = "start_moat";

        public static void ApplyV1(ref MobileCastleCombatConfig config)
        {
            config.MoatGameplayEnabled = false;
            config.MoatSlowMultiplier = 1f;
            config.MoatDamagePerSecond = 0f;
        }

        public static bool IsGameplayEnabled(in MobileCastleCombatConfig config)
        {
            return config.MoatGameplayEnabled;
        }

        public static bool IsDormantTechNodeId(string id)
        {
            return id == DeeperMoatNodeId || id == BurningMoatNodeId;
        }

        public static bool IsDormantMetaUpgradeId(string id)
        {
            return id == StartingMoatMetaId;
        }
    }
}
