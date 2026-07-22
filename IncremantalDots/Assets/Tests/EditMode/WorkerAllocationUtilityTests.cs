using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class WorkerAllocationUtilityTests
    {
        [Test]
        public void InitializeTargetsFromCurrent_NormalizesToBasisPointsDeterministically()
        {
            var allocation = CreateInitialAllocation();

            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);

            Assert.That(allocation.WoodTargetRatioBps, Is.EqualTo(3774));
            Assert.That(allocation.StoneTargetRatioBps, Is.EqualTo(1887));
            Assert.That(allocation.IronTargetRatioBps, Is.EqualTo(1509));
            Assert.That(allocation.FoodTargetRatioBps, Is.EqualTo(2830));
            Assert.That(TargetRatioTotal(allocation), Is.EqualTo(WorkerAllocationUtility.RatioScale));
        }

        [Test]
        public void BeginPopulationUpdate_FirstObservationBaselinesWithoutConsumingExistingIdle()
        {
            var allocation = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);

            int addedPopulation = WorkerAllocationUtility.BeginPopulationUpdate(ref allocation, 60);

            Assert.That(addedPopulation, Is.Zero);
            Assert.That(allocation.AutoAllocationInitialized, Is.EqualTo(1));
            Assert.That(allocation.LastObservedPopulation, Is.EqualTo(60));
            Assert.That(WorkerAllocationUtility.TotalWorkers(allocation), Is.EqualTo(53));
        }

        [Test]
        public void WorkerAllocationContract_OwnsFourRatiosActualCountsCapsAndDerivedIdle()
        {
            var allocation = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);

            int idle = WorkerAllocationUtility.ResolveIdlePopulation(
                allocation,
                populationTotal: 60,
                archerCount: 4);

            Assert.That(TargetRatioTotal(allocation),
                Is.EqualTo(WorkerAllocationUtility.RatioScale));
            Assert.That(WorkerAllocationUtility.TotalWorkers(allocation), Is.EqualTo(53));
            Assert.That(allocation.WoodWorkerCapacity, Is.EqualTo(40));
            Assert.That(allocation.StoneWorkerCapacity, Is.EqualTo(30));
            Assert.That(allocation.IronWorkerCapacity, Is.EqualTo(24));
            Assert.That(allocation.FoodWorkerCapacity, Is.EqualTo(40));
            Assert.That(idle, Is.EqualTo(3));
            Assert.That(WorkerAllocationUtility.ResolveIdlePopulation(
                allocation,
                populationTotal: 40,
                archerCount: 4), Is.Zero);
        }

        [Test]
        public void AutoAssignNewPopulation_ReachesSameResultAndRespectsCaps()
        {
            var first = CreateInitialAllocation();
            var second = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref first);
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref second);

            int firstAssigned = WorkerAllocationUtility.AutoAssignNewPopulation(ref first, 15);
            int secondAssigned = WorkerAllocationUtility.AutoAssignNewPopulation(ref second, 15);

            Assert.That(firstAssigned, Is.EqualTo(15));
            Assert.That(secondAssigned, Is.EqualTo(firstAssigned));
            Assert.That(WorkerAllocationUtility.TotalWorkers(first), Is.EqualTo(68));
            Assert.That(first.WoodWorkers, Is.EqualTo(second.WoodWorkers));
            Assert.That(first.StoneWorkers, Is.EqualTo(second.StoneWorkers));
            Assert.That(first.IronWorkers, Is.EqualTo(second.IronWorkers));
            Assert.That(first.FoodWorkers, Is.EqualTo(second.FoodWorkers));
            Assert.That(first.WoodWorkers, Is.LessThanOrEqualTo(first.WoodWorkerCapacity));
            Assert.That(first.StoneWorkers, Is.LessThanOrEqualTo(first.StoneWorkerCapacity));
            Assert.That(first.IronWorkers, Is.LessThanOrEqualTo(first.IronWorkerCapacity));
            Assert.That(first.FoodWorkers, Is.LessThanOrEqualTo(first.FoodWorkerCapacity));
        }

        [Test]
        public void AutoAssignNewPopulation_WhenPositiveTargetIsFull_UsesNextAvailableResource()
        {
            var allocation = new MobilePopulationAllocation
            {
                WoodWorkers = 1,
                WoodWorkerCapacity = 1,
                StoneWorkerCapacity = 10,
                IronWorkerCapacity = 10,
                FoodWorkerCapacity = 10,
                WoodTargetRatioBps = WorkerAllocationUtility.RatioScale
            };

            int assigned = WorkerAllocationUtility.AutoAssignNewPopulation(ref allocation, 5);

            Assert.That(assigned, Is.EqualTo(5));
            Assert.That(WorkerAllocationUtility.TotalWorkers(allocation), Is.EqualTo(6));
            Assert.That(allocation.WoodWorkers, Is.EqualTo(1));
            Assert.That(allocation.StoneWorkers, Is.EqualTo(5));
        }

        [Test]
        public void SetTargetRatioBps_PreservesExactSelectionAndDeterministicTotal()
        {
            var first = CreateInitialAllocation();
            var second = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref first);
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref second);

            WorkerAllocationUtility.SetTargetRatioBps(ref first, 0, 5000);
            WorkerAllocationUtility.SetTargetRatioBps(ref second, 0, 5000);

            Assert.That(first.WoodTargetRatioBps, Is.EqualTo(5000));
            Assert.That(TargetRatioTotal(first), Is.EqualTo(WorkerAllocationUtility.RatioScale));
            Assert.That(first.StoneTargetRatioBps, Is.EqualTo(second.StoneTargetRatioBps));
            Assert.That(first.IronTargetRatioBps, Is.EqualTo(second.IronTargetRatioBps));
            Assert.That(first.FoodTargetRatioBps, Is.EqualTo(second.FoodTargetRatioBps));
        }

        [Test]
        public void SetTargetRatioBps_WhenOtherTargetsAreZero_DistributesRemainderEvenly()
        {
            var allocation = new MobilePopulationAllocation
            {
                WoodTargetRatioBps = WorkerAllocationUtility.RatioScale
            };

            WorkerAllocationUtility.SetTargetRatioBps(ref allocation, 0, 2500);

            Assert.That(allocation.WoodTargetRatioBps, Is.EqualTo(2500));
            Assert.That(allocation.StoneTargetRatioBps, Is.EqualTo(2500));
            Assert.That(allocation.IronTargetRatioBps, Is.EqualTo(2500));
            Assert.That(allocation.FoodTargetRatioBps, Is.EqualTo(2500));
        }

        [Test]
        public void SetTargetRatioBps_AtMaximumClearsOtherTargets()
        {
            var allocation = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);

            WorkerAllocationUtility.SetTargetRatioBps(ref allocation, 2,
                WorkerAllocationUtility.RatioScale);

            Assert.That(allocation.WoodTargetRatioBps, Is.Zero);
            Assert.That(allocation.StoneTargetRatioBps, Is.Zero);
            Assert.That(allocation.IronTargetRatioBps, Is.EqualTo(WorkerAllocationUtility.RatioScale));
            Assert.That(allocation.FoodTargetRatioBps, Is.Zero);
        }

        [Test]
        public void RebalanceAvailableWorkers_AssignsEveryCivilianWhenTargetsFitCapacities()
        {
            var allocation = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);
            WorkerAllocationUtility.SetTargetRatioBps(ref allocation, 0, 5000);

            int rebalanced = WorkerAllocationUtility.RebalanceAvailableWorkers(
                ref allocation,
                populationTotal: 57,
                archerCount: 4);

            Assert.That(rebalanced, Is.EqualTo(53));
            Assert.That(WorkerAllocationUtility.TotalWorkers(allocation), Is.EqualTo(53));
            Assert.That(allocation.WoodWorkers, Is.EqualTo(27));
            Assert.That(allocation.StoneWorkers + allocation.IronWorkers + allocation.FoodWorkers,
                Is.EqualTo(26));
        }

        [Test]
        public void RebalanceAvailableWorkers_AtMaximumSpillsCapacityOverflowToNextResource()
        {
            var allocation = CreateInitialAllocation();
            WorkerAllocationUtility.InitializeTargetsFromCurrent(ref allocation);
            WorkerAllocationUtility.SetTargetRatioBps(ref allocation, 2,
                WorkerAllocationUtility.RatioScale);

            int rebalanced = WorkerAllocationUtility.RebalanceAvailableWorkers(
                ref allocation,
                populationTotal: 57,
                archerCount: 4);

            Assert.That(rebalanced, Is.EqualTo(53));
            Assert.That(allocation.WoodWorkers, Is.EqualTo(29));
            Assert.That(allocation.StoneWorkers, Is.Zero);
            Assert.That(allocation.IronWorkers, Is.EqualTo(allocation.IronWorkerCapacity));
            Assert.That(allocation.FoodWorkers, Is.Zero);
        }

        [Test]
        public void RemoveWorkersInResourceOrder_ConsumesWoodThenStoneThenIronThenFood()
        {
            var allocation = new MobilePopulationAllocation
            {
                WoodWorkers = 2,
                StoneWorkers = 3,
                IronWorkers = 4,
                FoodWorkers = 5
            };

            int removed = WorkerAllocationUtility.RemoveWorkersInResourceOrder(
                ref allocation,
                amount: 7);

            Assert.That(removed, Is.EqualTo(7));
            Assert.That(allocation.WoodWorkers, Is.Zero);
            Assert.That(allocation.StoneWorkers, Is.Zero);
            Assert.That(allocation.IronWorkers, Is.EqualTo(2));
            Assert.That(allocation.FoodWorkers, Is.EqualTo(5));
        }

        private static MobilePopulationAllocation CreateInitialAllocation()
        {
            return new MobilePopulationAllocation
            {
                WoodWorkers = 20,
                StoneWorkers = 10,
                IronWorkers = 8,
                FoodWorkers = 15,
                WoodWorkerCapacity = 40,
                StoneWorkerCapacity = 30,
                IronWorkerCapacity = 24,
                FoodWorkerCapacity = 40
            };
        }

        private static int TargetRatioTotal(MobilePopulationAllocation allocation)
        {
            return allocation.WoodTargetRatioBps
                + allocation.StoneTargetRatioBps
                + allocation.IronTargetRatioBps
                + allocation.FoodTargetRatioBps;
        }
    }
}
