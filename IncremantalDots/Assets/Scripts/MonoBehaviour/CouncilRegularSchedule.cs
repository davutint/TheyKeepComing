namespace DeadWalls
{
    /// <summary>
    /// Council takviminin tek owner'i. Her Dawn'da bir regular kart acilir;
    /// ayni gun save/Continue veya tekrar cagrida yalniz bir kez islenir.
    /// </summary>
    public static class CouncilRegularSchedule
    {
        public const int FirstRegularDay = 1;
        public const int IntervalDays = 1;

        public static bool IsRegularDay(int day)
        {
            return day >= FirstRegularDay;
        }

        public static bool ShouldOpen(
            int day,
            int lastHandledRegularDay,
            SiegeCyclePhase phase)
        {
            return phase == SiegeCyclePhase.Dawn
                && IsRegularDay(day)
                && lastHandledRegularDay != day;
        }

        /// <summary>
        /// v10 chance/pity save'inden v11 regular schedule state'ine gecis.
        /// Eski roll ancak mevcut regular gunde gercekten event urettigi kanitlanabiliyorsa
        /// islenmis kabul edilir. Chance fail'i regular Council'i sessizce yutmaz.
        /// </summary>
        public static int MigrateLegacyHandledDay(
            int currentDay,
            int legacyLastRollDay,
            int legacyDaysSinceEvent,
            bool hasActiveEvent)
        {
            if (!IsRegularDay(currentDay) || legacyLastRollDay != currentDay)
                return -1;

            return hasActiveEvent || legacyDaysSinceEvent == 0 ? currentDay : -1;
        }
    }
}
