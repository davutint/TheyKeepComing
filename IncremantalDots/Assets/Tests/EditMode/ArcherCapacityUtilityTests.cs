using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ArcherCapacityUtilityTests
    {
        [TestCase(-5, 1000)]
        [TestCase(0, 1000)]
        [TestCase(999, 1)]
        [TestCase(1000, 0)]
        [TestCase(1001, 0)]
        public void RemainingCapacity_UsesCommonOneThousandCap(int currentTotal, int expected)
        {
            Assert.That(ArcherCapacityUtility.GetRemainingCapacity(currentTotal),
                Is.EqualTo(expected));
        }

        [Test]
        public void CanAdd_AllowsThousandthAndRejectsThousandFirst()
        {
            Assert.That(ArcherCapacityUtility.CanAdd(999), Is.True);
            Assert.That(ArcherCapacityUtility.CanAdd(1000), Is.False);
            Assert.That(ArcherCapacityUtility.CanAdd(998, 2), Is.True);
            Assert.That(ArcherCapacityUtility.CanAdd(999, 2), Is.False);
            Assert.That(ArcherCapacityUtility.CanAdd(0, 0), Is.False);
        }

        [Test]
        public void AllowedAdditionalCount_ClampsInvalidAndOversizedRequests()
        {
            Assert.That(ArcherCapacityUtility.GetAllowedAdditionalCount(990, 25),
                Is.EqualTo(10));
            Assert.That(ArcherCapacityUtility.GetAllowedAdditionalCount(1000, 25),
                Is.Zero);
            Assert.That(ArcherCapacityUtility.GetAllowedAdditionalCount(10, -5),
                Is.Zero);
        }
    }
}
