using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ArcherRecruitmentCostUtilityTests
    {
        [Test]
        public void GetScaledCost_UsesTargetTypeCountCurve()
        {
            var baseCost = new ResourceCost(100, 50, 25, 10);

            ResourceCost first = ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost, 0, 25, 2f);
            ResourceCost afterTwentyFive = ArcherRecruitmentCostUtility.GetScaledCost(
                baseCost, 25, 25, 2f);

            Assert.That(first.Wood, Is.EqualTo(100));
            Assert.That(first.Stone, Is.EqualTo(50));
            Assert.That(first.Iron, Is.EqualTo(25));
            Assert.That(first.Food, Is.EqualTo(10));
            Assert.That(afterTwentyFive.Wood, Is.EqualTo(400));
            Assert.That(afterTwentyFive.Stone, Is.EqualTo(200));
            Assert.That(afterTwentyFive.Iron, Is.EqualTo(100));
            Assert.That(afterTwentyFive.Food, Is.EqualTo(40));
        }

        [Test]
        public void GetScaledCost_SanitizesInvalidInputsAndSaturatesOverflow()
        {
            ResourceCost sanitized = ArcherRecruitmentCostUtility.GetScaledCost(
                new ResourceCost(-10, 20, 0, 5), -100, 0, float.NaN);
            ResourceCost saturated = ArcherRecruitmentCostUtility.GetScaledCost(
                new ResourceCost(int.MaxValue, 1, 0, 0), 1000, 1, 10f);

            Assert.That(sanitized.Wood, Is.Zero);
            Assert.That(sanitized.Stone, Is.EqualTo(20));
            Assert.That(sanitized.Iron, Is.Zero);
            Assert.That(sanitized.Food, Is.EqualTo(5));
            Assert.That(saturated.Wood, Is.EqualTo(int.MaxValue));
            Assert.That(saturated.Stone, Is.EqualTo(int.MaxValue));
        }
    }
}
