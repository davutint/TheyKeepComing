using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class MobileWorkerBuildingUpgradeUtilityTests
    {
        [Test]
        public void LevelZeroCosts_UseApprovedWoodAndIronBases()
        {
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                WorkerBuildingUpgradeType.Capacity, 0, out var capacity), Is.True);
            Assert.That(capacity.Wood, Is.EqualTo(100));
            Assert.That(capacity.Iron, Is.EqualTo(25));

            Assert.That(MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                WorkerBuildingUpgradeType.Efficiency, 0, out var efficiency), Is.True);
            Assert.That(efficiency.Wood, Is.EqualTo(150));
            Assert.That(efficiency.Iron, Is.EqualTo(50));
        }

        [Test]
        public void CostCurve_LevelTen_UsesCeilingOfBaseTimesOnePointThirtyFivePowerLevel()
        {
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                WorkerBuildingUpgradeType.Capacity, 10, out var capacity), Is.True);
            Assert.That(capacity.Wood, Is.EqualTo(2011));
            Assert.That(capacity.Iron, Is.EqualTo(503));

            Assert.That(MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                WorkerBuildingUpgradeType.Efficiency, 10, out var efficiency), Is.True);
            Assert.That(efficiency.Wood, Is.EqualTo(3016));
            Assert.That(efficiency.Iron, Is.EqualTo(1006));
        }

        [Test]
        public void Levels_AreIndependentPerResourceAndUpgradeType()
        {
            var state = new MobileWorkerBuildingUpgradeState();

            Assert.That(MobileWorkerBuildingUpgradeUtility.TryIncreaseLevel(
                ref state, EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity), Is.True);
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryIncreaseLevel(
                ref state, EconomyFocusType.Wood, WorkerBuildingUpgradeType.Efficiency), Is.True);
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryIncreaseLevel(
                ref state, EconomyFocusType.Wood, WorkerBuildingUpgradeType.Efficiency), Is.True);
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryIncreaseLevel(
                ref state, EconomyFocusType.Iron, WorkerBuildingUpgradeType.Capacity), Is.True);

            Assert.That(state.WoodCapacityLevel, Is.EqualTo(1));
            Assert.That(state.WoodEfficiencyLevel, Is.EqualTo(2));
            Assert.That(state.IronCapacityLevel, Is.EqualTo(1));
            Assert.That(state.StoneCapacityLevel, Is.Zero);
            Assert.That(state.FoodEfficiencyLevel, Is.Zero);
        }

        [Test]
        public void Effects_AreAdditiveAndUnrepresentableCostIsRejectedSafely()
        {
            Assert.That(MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(7), Is.EqualTo(70));
            Assert.That(MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(7),
                Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                WorkerBuildingUpgradeType.Capacity, 1000, out _), Is.False);
        }
    }
}
