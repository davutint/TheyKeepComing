using Unity.Mathematics;

namespace DeadWalls
{
    public struct MobilePopulationArrivalBudget
    {
        public int RequestedArrivals;
        public int AvailableBedSpace;
        public int AffordableArrivals;
        public int AcceptedArrivals;
        public int FoodCostPerArrival;
        public int RequiredFood;
    }

    public static class MobilePopulationArrivalUtility
    {
        public const int DefaultRequestedArrivalsPerDawn = 15;
        public const int DefaultFoodCostPerArrival = 1;

        public static int SanitizeRequestedArrivals(int requestedArrivals)
        {
            return math.max(0, requestedArrivals);
        }

        public static int SanitizeFoodCostPerArrival(int foodCostPerArrival)
        {
            return math.max(1, foodCostPerArrival);
        }

        public static MobilePopulationArrivalBudget CalculateBudget(int requestedArrivals,
            int currentPopulation, int totalBedCapacity, int availableFood, int foodCostPerArrival)
        {
            int requested = SanitizeRequestedArrivals(requestedArrivals);
            int population = math.max(0, currentPopulation);
            int beds = math.max(0, totalBedCapacity);
            int food = math.max(0, availableFood);
            int unitFoodCost = SanitizeFoodCostPerArrival(foodCostPerArrival);
            int availableBedSpace = math.max(0, beds - math.min(population, beds));
            int affordableArrivals = food / unitFoodCost;
            int acceptedArrivals = math.min(requested,
                math.min(availableBedSpace, affordableArrivals));

            return new MobilePopulationArrivalBudget
            {
                RequestedArrivals = requested,
                AvailableBedSpace = availableBedSpace,
                AffordableArrivals = affordableArrivals,
                AcceptedArrivals = acceptedArrivals,
                FoodCostPerArrival = unitFoodCost,
                RequiredFood = acceptedArrivals * unitFoodCost
            };
        }
    }
}
