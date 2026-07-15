namespace DeadWalls
{
    /// <summary>
    /// V1 Council takviminin tek owner'i. Regular kartlar ilk kez Day 3'te ve
    /// sonrasinda her uc gunde bir acilir; ayni scheduled gun yalniz bir kez islenir.
    /// </summary>
    public static class CouncilRegularSchedule
    {
        public const int FirstRegularDay = 3;
        public const int IntervalDays = 3;

        public static bool IsRegularDay(int day)
        {
            return day >= FirstRegularDay && day % IntervalDays == 0;
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
