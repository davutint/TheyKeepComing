using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class CouncilEffectGuardUtilityTests
    {
        [Test]
        public void PopulationGain_IsBoundedByBedsAndOneTimeFood()
        {
            MobilePopulationArrivalBudget bedBound =
                CouncilEffectGuardUtility.CalculatePopulationGain(
                    requestedPopulation: 10,
                    currentPopulation: 59,
                    totalBedCapacity: 60,
                    availableFood: 100,
                    foodCostPerArrival: 2);
            Assert.That(bedBound.AcceptedArrivals, Is.EqualTo(1));
            Assert.That(bedBound.RequiredFood, Is.EqualTo(2));

            MobilePopulationArrivalBudget foodBound =
                CouncilEffectGuardUtility.CalculatePopulationGain(
                    requestedPopulation: 10,
                    currentPopulation: 40,
                    totalBedCapacity: 60,
                    availableFood: 5,
                    foodCostPerArrival: 2);
            Assert.That(foodBound.AcceptedArrivals, Is.EqualTo(2));
            Assert.That(foodBound.RequiredFood, Is.EqualTo(4));
        }

        [Test]
        public void FreeArcherGain_UsesIdlePopulationAndCommonCap()
        {
            Assert.That(CouncilEffectGuardUtility.GetAllowedFreeArcherGain(
                requestedArchers: 10,
                currentTotalArchers: 998,
                idlePopulation: 1), Is.EqualTo(1));
            Assert.That(CouncilEffectGuardUtility.GetAllowedFreeArcherGain(
                requestedArchers: 10,
                currentTotalArchers: 1000,
                idlePopulation: 100), Is.Zero);
            Assert.That(CouncilEffectGuardUtility.GetAllowedFreeArcherGain(
                requestedArchers: int.MaxValue,
                currentTotalArchers: 999,
                idlePopulation: int.MaxValue), Is.EqualTo(1));
        }

        [Test]
        public void NightEffect_ResolvesOnlyBoundedCountMultiplier()
        {
            Assert.That(CouncilEffectGuardUtility.ResolveNightCountMultiplier(-0.25f),
                Is.EqualTo(0.75f));
            Assert.That(CouncilEffectGuardUtility.ResolveNightCountMultiplier(0.4f),
                Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(CouncilEffectGuardUtility.ResolveNightCountMultiplier(100f),
                Is.EqualTo(CouncilEffectGuardUtility.MaximumNightCountMultiplier));
            Assert.That(CouncilEffectGuardUtility.ResolveNightCountMultiplier(float.NaN),
                Is.EqualTo(1f));
        }
    }
}
