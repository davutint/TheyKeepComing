using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ArrowEconomyUtilityTests
    {
        [Test]
        public void BaseSupply_UsesFiniteCapacityAndConstantRefillRatio()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply { Current = 0 };

            Assert.That(ArrowEconomyUtility.GetCapacity(supply, tuning), Is.EqualTo(200));
            Assert.That(ArrowEconomyUtility.GetArrowsPerWood(supply, tuning), Is.EqualTo(4));
            Assert.That(ArrowEconomyUtility.TryGetPackageQuote(
                supply, tuning, 1, out var first), Is.True);
            Assert.That(first.ArrowAmount, Is.EqualTo(100));
            Assert.That(first.WoodCost, Is.EqualTo(25));

            supply.Current = 100;
            Assert.That(ArrowEconomyUtility.TryGetPackageQuote(
                supply, tuning, 1, out var second), Is.True);
            Assert.That(second.ArrowAmount, Is.EqualTo(100));
            Assert.That(second.WoodCost, Is.EqualTo(25),
                "Refill sayisi birim Arrow fiyatini buyutmemeli.");
        }

        [Test]
        public void PackageQuote_ClampsToMissingCapacityWithoutChargingForWaste()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply { Current = 197 };

            Assert.That(ArrowEconomyUtility.TryGetPackageQuote(
                supply, tuning, 5, out var quote), Is.True);
            Assert.That(quote.ArrowAmount, Is.EqualTo(3));
            Assert.That(quote.WoodCost, Is.EqualTo(1));
            Assert.That(ArrowEconomyUtility.TryApplyRefill(ref supply, tuning, quote), Is.True);
            Assert.That(supply.Current, Is.EqualTo(200));
        }

        [Test]
        public void BuyMax_UsesAvailableWoodAndNeverExceedsCapacity()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply { Current = 20 };

            Assert.That(ArrowEconomyUtility.TryGetBuyMaxQuote(
                supply, tuning, 10, out var quote), Is.True);
            Assert.That(quote.ArrowAmount, Is.EqualTo(40));
            Assert.That(quote.WoodCost, Is.EqualTo(10));
            Assert.That(ArrowEconomyUtility.TryApplyRefill(ref supply, tuning, quote), Is.True);
            Assert.That(supply.Current, Is.EqualTo(60));
        }

        [Test]
        public void EfficiencyLevel_GivesMoreArrowsForSameWood()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var baseSupply = new ArrowSupply { Current = 0 };
            var efficientSupply = new ArrowSupply { Current = 0, EfficiencyLevel = 2 };

            Assert.That(ArrowEconomyUtility.TryGetBuyMaxQuote(
                baseSupply, tuning, 10, out var baseQuote), Is.True);
            Assert.That(ArrowEconomyUtility.TryGetBuyMaxQuote(
                efficientSupply, tuning, 10, out var efficientQuote), Is.True);
            Assert.That(baseQuote.ArrowAmount, Is.EqualTo(40));
            Assert.That(efficientQuote.ArrowAmount, Is.EqualTo(60));
            Assert.That(efficientQuote.WoodCost, Is.EqualTo(baseQuote.WoodCost));
        }

        [Test]
        public void MetaEfficiency_IsAdditiveWithoutAdvancingPaidRunLevel()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply
            {
                Current = 0,
                EfficiencyLevel = 0,
                MetaEfficiencyBonus = 3,
                HeartEfficiencyBonus = 2
            };

            Assert.That(ArrowEconomyUtility.GetArrowsPerWood(supply, tuning), Is.EqualTo(9));
            Assert.That(supply.EfficiencyLevel, Is.Zero);
            Assert.That(ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Efficiency, tuning, out var firstPaidCost), Is.True);
            Assert.That(firstPaidCost.Wood, Is.EqualTo(200));
            Assert.That(firstPaidCost.Iron, Is.EqualTo(50));
        }

        [Test]
        public void CapacityAndEfficiencyUpgrades_HaveIndependentGrowingCosts()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply();

            Assert.That(ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Capacity, tuning, out var capacity), Is.True);
            Assert.That(capacity.Wood, Is.EqualTo(150));
            Assert.That(capacity.Iron, Is.EqualTo(25));
            Assert.That(ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Efficiency, tuning, out var efficiency), Is.True);
            Assert.That(efficiency.Wood, Is.EqualTo(200));
            Assert.That(efficiency.Iron, Is.EqualTo(50));

            Assert.That(ArrowEconomyUtility.TryIncreaseUpgradeLevel(
                ref supply, ArrowUpgradeType.Capacity), Is.True);
            Assert.That(ArrowEconomyUtility.GetCapacity(supply, tuning), Is.EqualTo(400));
            Assert.That(ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Capacity, tuning, out var nextCapacity), Is.True);
            Assert.That(nextCapacity.Wood, Is.EqualTo(203));
            Assert.That(nextCapacity.Iron, Is.EqualTo(34));
            Assert.That(supply.EfficiencyLevel, Is.Zero);
        }

        [Test]
        public void UnrepresentableUpgradeCost_IsRejectedSafely()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply { CapacityLevel = 1000 };
            Assert.That(ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Capacity, tuning, out _), Is.False);
        }
    }
}
