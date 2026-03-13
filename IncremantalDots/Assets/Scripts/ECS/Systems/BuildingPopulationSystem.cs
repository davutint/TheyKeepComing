using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Bina nufus sistemi.
    /// Ev binalarinin nufus kapasitesini ve yemek giderini,
    /// kale yukseltmenin kapasite bonusunu ECS singleton'larina yansitir.
    /// BuildingProductionSystem'den SONRA, PopulationTickSystem'den ONCE calisir.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PopulationTickSystem))]
    [UpdateAfter(typeof(BuildingProductionSystem))]
    public partial struct BuildingPopulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<ResourceConsumptionRate>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            // Tum PopulationProvider entity'lerini tara → toplam kapasite
            int totalCapacity = 0;
            foreach (var provider in SystemAPI.Query<RefRO<PopulationProvider>>())
            {
                totalCapacity += provider.ValueRO.CapacityAmount;
            }

            // Tum BuildingFoodCost entity'lerini tara → toplam bina yemek gideri
            float totalBuildingFoodCost = 0f;
            foreach (var foodCost in SystemAPI.Query<RefRO<BuildingFoodCost>>())
            {
                totalBuildingFoodCost += foodCost.ValueRO.FoodPerMin;
            }

            // Kale yukseltme bonusu (varsa)
            int castleBonus = 0;
            if (SystemAPI.TryGetSingleton<CastleUpgradeData>(out var castleUpgrade))
            {
                castleBonus = castleUpgrade.Level * castleUpgrade.CapacityPerLevel;
            }

            // PopulationState.Capacity guncelle
            var popRW = SystemAPI.GetSingletonRW<PopulationState>();
            popRW.ValueRW.Capacity = popRW.ValueRO.BaseCapacity + totalCapacity + castleBonus;

            // ResourceConsumptionRate — bina yemek giderini yaz
            // PopulationTickSystem sonra calisacak ve nufus kismini += ile ekleyecek
            var consumptionRW = SystemAPI.GetSingletonRW<ResourceConsumptionRate>();
            consumptionRW.ValueRW.WoodPerMin = 0f;
            consumptionRW.ValueRW.StonePerMin = 0f;
            consumptionRW.ValueRW.IronPerMin = 0f;
            consumptionRW.ValueRW.FoodPerMin = totalBuildingFoodCost;
        }
    }
}
