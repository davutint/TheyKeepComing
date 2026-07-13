using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class WorkerVisualRepresentationUtilityTests
    {
        [TestCase(-10, WorkerVisualDensityLevel.None, 0)]
        [TestCase(0, WorkerVisualDensityLevel.None, 0)]
        [TestCase(1, WorkerVisualDensityLevel.Low, 1)]
        [TestCase(12, WorkerVisualDensityLevel.Low, 12)]
        [TestCase(13, WorkerVisualDensityLevel.Medium, 13)]
        [TestCase(16, WorkerVisualDensityLevel.Medium, 13)]
        [TestCase(17, WorkerVisualDensityLevel.Medium, 14)]
        [TestCase(60, WorkerVisualDensityLevel.Medium, 24)]
        [TestCase(61, WorkerVisualDensityLevel.High, 25)]
        [TestCase(80, WorkerVisualDensityLevel.High, 25)]
        [TestCase(81, WorkerVisualDensityLevel.High, 26)]
        [TestCase(220, WorkerVisualDensityLevel.High, 32)]
        [TestCase(10000, WorkerVisualDensityLevel.High, 32)]
        public void RepresentativeCount_UsesStableLowMediumHighCurve(
            int actualWorkers, WorkerVisualDensityLevel expectedDensity, int expectedVisuals)
        {
            Assert.That(WorkerVisualRepresentationUtility.GetDensityLevel(actualWorkers),
                Is.EqualTo(expectedDensity));
            Assert.That(WorkerVisualRepresentationUtility.GetRepresentativeCount(actualWorkers),
                Is.EqualTo(expectedVisuals));
        }

        [Test]
        public void RepresentativeCount_IsMonotonicAndBounded()
        {
            int previous = 0;
            for (int actualWorkers = 0; actualWorkers <= 10000; actualWorkers++)
            {
                int visualCount = WorkerVisualRepresentationUtility.GetRepresentativeCount(actualWorkers);
                Assert.That(visualCount, Is.GreaterThanOrEqualTo(previous));
                Assert.That(visualCount,
                    Is.LessThanOrEqualTo(WorkerVisualRepresentationUtility.MaxVisualWorkersPerResource));
                previous = visualCount;
            }
        }

        [Test]
        public void RepresentativeCounts_InitialAllocationPreservesReadableDensity()
        {
            var allocation = new MobilePopulationAllocation
            {
                WoodWorkers = 20,
                StoneWorkers = 10,
                IronWorkers = 8,
                FoodWorkers = 15
            };

            var counts = WorkerVisualRepresentationUtility.GetRepresentativeCounts(allocation);

            Assert.That(counts.x, Is.EqualTo(14));
            Assert.That(counts.y, Is.EqualTo(10));
            Assert.That(counts.z, Is.EqualTo(8));
            Assert.That(counts.w, Is.EqualTo(13));
            Assert.That(WorkerVisualRepresentationUtility.GetRepresentativeTotal(allocation),
                Is.EqualTo(45));
        }
    }
}
