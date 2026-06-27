namespace DeadWalls
{
    public static class EconomyFocusUtility
    {
        private const float DefaultBalancedPassiveMultiplier = 1.20f;
        private const float DefaultBalancedRewardMultiplier = 1.10f;
        private const float DefaultFocusedPassiveMultiplier = 1.60f;
        private const float DefaultFocusedPassiveFlatBonusPerMin = 60f;
        private const float DefaultFocusedKillRewardMultiplier = 2.00f;
        private const float DefaultFocusedWaveClearMultiplier = 1.75f;

        public static EconomyFocusType Normalize(EconomyFocusType focus)
        {
            switch (focus)
            {
                case EconomyFocusType.Wood:
                case EconomyFocusType.Stone:
                case EconomyFocusType.Iron:
                case EconomyFocusType.Food:
                    return focus;
                default:
                    return EconomyFocusType.Balanced;
            }
        }

        public static ResourceProductionRate ApplyPassiveFocus(ResourceProductionRate production,
            MobileCastleCombatConfig config, EconomyFocusType focus)
        {
            focus = Normalize(focus);
            if (focus == EconomyFocusType.Balanced)
            {
                float multiplier = PositiveOrDefault(config.BalancedPassiveMultiplier,
                    DefaultBalancedPassiveMultiplier);
                production.WoodPerMin *= multiplier;
                production.StonePerMin *= multiplier;
                production.IronPerMin *= multiplier;
                production.FoodPerMin *= multiplier;
                return production;
            }

            ApplyFocusedPassive(ref production, focus,
                PositiveOrDefault(config.FocusedPassiveMultiplier, DefaultFocusedPassiveMultiplier),
                PositiveOrDefault(config.FocusedPassiveFlatBonusPerMin, DefaultFocusedPassiveFlatBonusPerMin));
            return production;
        }

        public static float GetKillRewardMultiplier(MobileCastleCombatConfig config,
            EconomyFocusType activeFocus, EconomyFocusType resource)
        {
            return GetRewardMultiplier(
                PositiveOrDefault(config.BalancedRewardMultiplier, DefaultBalancedRewardMultiplier),
                PositiveOrDefault(config.FocusedKillRewardMultiplier, DefaultFocusedKillRewardMultiplier),
                activeFocus, resource);
        }

        public static float GetWaveClearMultiplier(MobileCastleCombatConfig config,
            EconomyFocusType activeFocus, EconomyFocusType resource)
        {
            return GetRewardMultiplier(
                PositiveOrDefault(config.BalancedRewardMultiplier, DefaultBalancedRewardMultiplier),
                PositiveOrDefault(config.FocusedWaveClearMultiplier, DefaultFocusedWaveClearMultiplier),
                activeFocus, resource);
        }

        private static float GetRewardMultiplier(float balancedMultiplier, float focusedMultiplier,
            EconomyFocusType activeFocus, EconomyFocusType resource)
        {
            activeFocus = Normalize(activeFocus);
            if (activeFocus == EconomyFocusType.Balanced)
                return balancedMultiplier;

            return activeFocus == resource ? focusedMultiplier : 1f;
        }

        private static void ApplyFocusedPassive(ref ResourceProductionRate production,
            EconomyFocusType focus, float multiplier, float flatBonusPerMin)
        {
            switch (focus)
            {
                case EconomyFocusType.Wood:
                    production.WoodPerMin = production.WoodPerMin * multiplier + flatBonusPerMin;
                    break;
                case EconomyFocusType.Stone:
                    production.StonePerMin = production.StonePerMin * multiplier + flatBonusPerMin;
                    break;
                case EconomyFocusType.Iron:
                    production.IronPerMin = production.IronPerMin * multiplier + flatBonusPerMin;
                    break;
                case EconomyFocusType.Food:
                    production.FoodPerMin = production.FoodPerMin * multiplier + flatBonusPerMin;
                    break;
            }
        }

        private static float PositiveOrDefault(float value, float defaultValue)
        {
            return value > 0f ? value : defaultValue;
        }
    }
}
