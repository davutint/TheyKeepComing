using System.Linq;
using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class CouncilRegularScheduleTests
    {
        [TestCase(-3, false)]
        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(5, true)]
        [TestCase(6, true)]
        [TestCase(9, true)]
        [TestCase(12, true)]
        [TestCase(99, true)]
        [TestCase(100, true)]
        public void IsRegularDay_MatchesEveryPositiveDay(int day, bool expected)
        {
            Assert.That(CouncilRegularSchedule.IsRegularDay(day), Is.EqualTo(expected));
        }

        [Test]
        public void ShouldOpen_RegularDayCanBeHandledOnlyOnce()
        {
            Assert.That(CouncilRegularSchedule.ShouldOpen(6, -1, SiegeCyclePhase.Dawn), Is.True);
            Assert.That(CouncilRegularSchedule.ShouldOpen(6, 3, SiegeCyclePhase.Dawn), Is.True);
            Assert.That(CouncilRegularSchedule.ShouldOpen(6, 6, SiegeCyclePhase.Dawn), Is.False);
            Assert.That(CouncilRegularSchedule.ShouldOpen(7, 6, SiegeCyclePhase.Dawn), Is.True);
        }

        [TestCase(SiegeCyclePhase.Day)]
        [TestCase(SiegeCyclePhase.Dusk)]
        [TestCase(SiegeCyclePhase.Night)]
        public void ShouldOpen_ScheduledDayOutsideDawn_IsRejected(SiegeCyclePhase phase)
        {
            Assert.That(CouncilRegularSchedule.ShouldOpen(6, -1, phase), Is.False);
        }

        [Test]
        public void FirstThirtyDays_ProduceOneMeetingPerDay()
        {
            int[] expected = System.Linq.Enumerable.Range(1, 30).ToArray();
            var actual = new System.Collections.Generic.List<int>();

            for (int day = 1; day <= 30; day++)
            {
                if (CouncilRegularSchedule.IsRegularDay(day))
                    actual.Add(day);
            }

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void LegacyMigration_ChanceFailureDoesNotConsumeRegularDay()
        {
            int migrated = CouncilRegularSchedule.MigrateLegacyHandledDay(
                currentDay: 6,
                legacyLastRollDay: 6,
                legacyDaysSinceEvent: 4,
                hasActiveEvent: false);

            Assert.That(migrated, Is.EqualTo(-1));
        }

        [TestCase(true, 4)]
        [TestCase(false, 0)]
        public void LegacyMigration_ProvenEventPreservesHandledRegularDay(
            bool hasActiveEvent,
            int daysSinceEvent)
        {
            int migrated = CouncilRegularSchedule.MigrateLegacyHandledDay(
                currentDay: 6,
                legacyLastRollDay: 6,
                legacyDaysSinceEvent: daysSinceEvent,
                hasActiveEvent: hasActiveEvent);

            Assert.That(migrated, Is.EqualTo(6));
        }

        [Test]
        public void LegacyMigration_ProvenEventOnAnyPositiveDayPreservesHandledIndex()
        {
            int migrated = CouncilRegularSchedule.MigrateLegacyHandledDay(
                currentDay: 5,
                legacyLastRollDay: 5,
                legacyDaysSinceEvent: 0,
                hasActiveEvent: true);

            Assert.That(migrated, Is.EqualTo(5));
        }
    }
}
