using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class MobilePopulationArrivalUtilityTests
    {
        [Test]
        public void CalculateBudget_AcceptsFullRequestedCountWhenBedsAndFoodAllow()
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                requestedArrivals: 15,
                currentPopulation: 60,
                totalBedCapacity: 75,
                availableFood: 120,
                foodCostPerArrival: 1);

            Assert.That(budget.RequestedArrivals, Is.EqualTo(15));
            Assert.That(budget.AvailableBedSpace, Is.EqualTo(15));
            Assert.That(budget.AffordableArrivals, Is.EqualTo(120));
            Assert.That(budget.AcceptedArrivals, Is.EqualTo(15));
            Assert.That(budget.RequiredFood, Is.EqualTo(15));
        }

        [Test]
        public void CalculateBudget_LimitsAcceptedCountByAvailableBedSpace()
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                requestedArrivals: 15,
                currentPopulation: 70,
                totalBedCapacity: 75,
                availableFood: 120,
                foodCostPerArrival: 1);

            Assert.That(budget.AcceptedArrivals, Is.EqualTo(5));
            Assert.That(budget.RequiredFood, Is.EqualTo(5));
        }

        [Test]
        public void CalculateBudget_LimitsAcceptedCountByAffordableFood()
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                requestedArrivals: 15,
                currentPopulation: 60,
                totalBedCapacity: 100,
                availableFood: 4,
                foodCostPerArrival: 1);

            Assert.That(budget.AcceptedArrivals, Is.EqualTo(4));
            Assert.That(budget.RequiredFood, Is.EqualTo(4));
        }

        [Test]
        public void CalculateBudget_ZeroFoodKeepsExistingPopulationAndAcceptsNobody()
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                requestedArrivals: 15,
                currentPopulation: 60,
                totalBedCapacity: 75,
                availableFood: 0,
                foodCostPerArrival: 1);

            Assert.That(budget.AcceptedArrivals, Is.Zero);
            Assert.That(budget.RequiredFood, Is.Zero);
        }

        [Test]
        public void CalculateBudget_ClampsInvalidInputsWithoutOverflowOrNegativeArrivals()
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                requestedArrivals: int.MaxValue,
                currentPopulation: -5,
                totalBedCapacity: int.MaxValue,
                availableFood: int.MaxValue,
                foodCostPerArrival: 0);

            Assert.That(budget.FoodCostPerArrival, Is.EqualTo(1));
            Assert.That(budget.AcceptedArrivals, Is.EqualTo(int.MaxValue));
            Assert.That(budget.RequiredFood, Is.EqualTo(int.MaxValue));

            budget = MobilePopulationArrivalUtility.CalculateBudget(15, 80, 60, 100, 1);
            Assert.That(budget.AvailableBedSpace, Is.Zero);
            Assert.That(budget.AcceptedArrivals, Is.Zero);
        }
    }
}
