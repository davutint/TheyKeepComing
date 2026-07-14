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

        [Test]
        public void RepresentedWorkerCounts_PreserveExactActualTotalAcrossDensityLevels()
        {
            int[] actualCounts = { 0, 1, 12, 13, 60, 61, 101, 119, 220, 10000 };
            for (int sample = 0; sample < actualCounts.Length; sample++)
            {
                int actualCount = actualCounts[sample];
                int visualCount = WorkerVisualRepresentationUtility.GetRepresentativeCount(actualCount);
                int representedTotal = 0;
                for (int index = 0; index < visualCount; index++)
                {
                    representedTotal += WorkerVisualRepresentationUtility.GetRepresentedWorkerCount(
                        actualCount, visualCount, index);
                }

                Assert.That(representedTotal, Is.EqualTo(actualCount),
                    $"Actual {actualCount}, {visualCount} visual arasinda exact dagilmadi.");
            }
        }

        [Test]
        public void ProductionFeedbackStrength_GrowsWithRepresentedWorkersAndStaysBounded()
        {
            float empty = WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(0);
            float single = WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(1);
            float group = WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(8);
            float largeGroup = WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(1000);

            Assert.That(empty, Is.Zero);
            Assert.That(single, Is.GreaterThan(0f));
            Assert.That(group, Is.GreaterThan(single));
            Assert.That(largeGroup, Is.GreaterThanOrEqualTo(group));
            Assert.That(largeGroup, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void LanternRule_IsActiveOnlyDuringDuskAndNight()
        {
            Assert.That(WorkerVisualRepresentationUtility.ShouldUseLantern(SiegeCyclePhase.Day), Is.False);
            Assert.That(WorkerVisualRepresentationUtility.ShouldUseLantern(SiegeCyclePhase.Dusk), Is.True);
            Assert.That(WorkerVisualRepresentationUtility.ShouldUseLantern(SiegeCyclePhase.Night), Is.True);
            Assert.That(WorkerVisualRepresentationUtility.ShouldUseLantern(SiegeCyclePhase.Dawn), Is.False);
        }
    }
}
