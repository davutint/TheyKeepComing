using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Nufus tick sistemi. Her frame:
    /// 1. Idle = Total - Workers - Archers (clamp >= 0)
    /// 2. ResourceConsumptionRate.FoodPerMin = assigned * FoodPerAssignedPerMin
    /// ResourceTickSystem'den ONCE calisir — yemek tuketim hizi guncel olur.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ResourceTickSystem))]
    public partial struct PopulationTickSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            var popRW = SystemAPI.GetSingletonRW<PopulationState>();

            // Idle hesapla — negatif olamaz
            int assigned = popRW.ValueRO.Workers + popRW.ValueRO.Archers;
            popRW.ValueRW.Idle = math.max(0, popRW.ValueRO.Total - assigned);

            // Yemek tuketim hizini guncelle — sadece atanmis bireyler tuketir
            var consumptionRW = SystemAPI.GetSingletonRW<ResourceConsumptionRate>();
            consumptionRW.ValueRW.FoodPerMin += assigned * popRW.ValueRO.FoodPerAssignedPerMin;
        }
    }
}
