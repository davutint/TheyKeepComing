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
    }
}
