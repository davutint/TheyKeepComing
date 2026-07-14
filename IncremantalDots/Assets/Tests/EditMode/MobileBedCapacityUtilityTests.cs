using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class MobileBedCapacityUtilityTests
    {
        [Test]
        public void CreateInitial_ClampsNegativeBaseAndStartsWithoutPurchasedBeds()
        {
            MobileBedCapacityState state = MobileBedCapacityUtility.CreateInitial(-5);

            Assert.That(state.BaseCapacity, Is.Zero);
            Assert.That(state.PurchasedCapacity, Is.Zero);
            Assert.That(MobileBedCapacityUtility.GetTotalCapacity(state), Is.Zero);
        }

        [Test]
        public void TryAddPurchasedCapacity_AccumulatesWithoutGameplayHardCap()
        {
            MobileBedCapacityState state = MobileBedCapacityUtility.CreateInitial(60);

            Assert.That(MobileBedCapacityUtility.TryAddPurchasedCapacity(ref state, 250_000,
                out int added), Is.True);

            Assert.That(added, Is.EqualTo(250_000));
            Assert.That(state.PurchasedCapacity, Is.EqualTo(250_000));
            Assert.That(MobileBedCapacityUtility.GetTotalCapacity(state), Is.EqualTo(250_060));
        }

        [Test]
        public void TryAddPurchasedCapacity_UsesIntSafetyCeilingWithoutOverflow()
        {
            var state = new MobileBedCapacityState
            {
                BaseCapacity = int.MaxValue - 2,
                PurchasedCapacity = 0
            };

            Assert.That(MobileBedCapacityUtility.TryAddPurchasedCapacity(ref state, 10,
                out int added), Is.True);

            Assert.That(added, Is.EqualTo(2));
            Assert.That(MobileBedCapacityUtility.GetTotalCapacity(state), Is.EqualTo(int.MaxValue));
            Assert.That(MobileBedCapacityUtility.TryAddPurchasedCapacity(ref state, 1, out _), Is.False);
        }

        [Test]
        public void NextPurchaseWoodCost_UsesApprovedOwnedCapacityCurve()
        {
            MobileBedCapacityState state = MobileBedCapacityUtility.CreateInitial(60);
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(100));

            state.PurchasedCapacity = 100;
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(2_500));

            state.PurchasedCapacity = 300;
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(16_900));

            state.PurchasedCapacity = 750;
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(96_100));
        }

        [Test]
        public void NextPurchaseWoodCost_CountsOwnedBaseCapacityAboveDefaultBaseline()
        {
            var state = new MobileBedCapacityState
            {
                BaseCapacity = 160,
                PurchasedCapacity = 0
            };

            Assert.That(MobileBedCapacityUtility.GetOwnedCapacityGrowthCount(state), Is.EqualTo(100));
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(2_500));
        }

        [Test]
        public void PurchaseWoodCost_SumsEverySequentialBedPrice()
        {
            MobileBedCapacityState state = MobileBedCapacityUtility.CreateInitial(60);

            Assert.That(MobileBedCapacityUtility.TryGetPurchaseWoodCost(state, 5, out int woodCost), Is.True);
            Assert.That(woodCost, Is.EqualTo(587));

            Assert.That(MobileBedCapacityUtility.TryAddPurchasedCapacity(ref state, 5, out _), Is.True);
            Assert.That(MobileBedCapacityUtility.GetNextPurchaseWoodCost(state), Is.EqualTo(144));
        }

        [Test]
        public void PurchaseWoodCost_RejectsUnrepresentableIntTransactionsWithoutOverflow()
        {
            MobileBedCapacityState state = MobileBedCapacityUtility.CreateInitial(60);

            Assert.That(MobileBedCapacityUtility.TryGetPurchaseWoodCost(state, 10_000,
                out int bulkCost), Is.False);
            Assert.That(bulkCost, Is.EqualTo(int.MaxValue));

            state.PurchasedCapacity = 120_000;
            Assert.That(MobileBedCapacityUtility.TryGetPurchaseWoodCost(state, 1,
                out int unitCost), Is.False);
            Assert.That(unitCost, Is.EqualTo(int.MaxValue));
        }
    }
}
