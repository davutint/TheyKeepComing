using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Bina kaynak uretim sistemi.
    /// Tum ResourceProducer entity'lerini tarar, toplam uretim hizini
    /// ResourceProductionRate singleton'ina yazar ve toplam isciyi PopulationState.Workers'a yazar.
    /// PopulationTickSystem'den ONCE calisir — Workers ve FoodPerMin guncel olur.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PopulationTickSystem))]
    public partial struct BuildingProductionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ResourceProductionRate>();
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            // Tum bina uretim hizlarini topla
            float woodRate = 0f;
            float stoneRate = 0f;
            float ironRate = 0f;
            float foodRate = 0f;
            int totalWorkers = 0;

            foreach (var producer in SystemAPI.Query<RefRO<ResourceProducer>>())
            {
                float rate = producer.ValueRO.RatePerWorkerPerMin * producer.ValueRO.AssignedWorkers;
                totalWorkers += producer.ValueRO.AssignedWorkers;

                switch (producer.ValueRO.ResourceType)
                {
                    case ResourceType.Wood:  woodRate  += rate; break;
                    case ResourceType.Stone: stoneRate += rate; break;
                    case ResourceType.Iron:  ironRate  += rate; break;
                    case ResourceType.Food:  foodRate  += rate; break;
                }
            }

            // Uretim hizlarini singleton'a yaz
            var prodRW = SystemAPI.GetSingletonRW<ResourceProductionRate>();
            prodRW.ValueRW.WoodPerMin = woodRate;
            prodRW.ValueRW.StonePerMin = stoneRate;
            prodRW.ValueRW.IronPerMin = ironRate;
            prodRW.ValueRW.FoodPerMin = foodRate;

            // ArrowProducer (Fletcher) iscilerini de say
            foreach (var arrowProducer in SystemAPI.Query<RefRO<ArrowProducer>>())
            {
                totalWorkers += arrowProducer.ValueRO.AssignedWorkers;
            }

            // Toplam isci sayisini nufusa yaz
            var popRW = SystemAPI.GetSingletonRW<PopulationState>();
            popRW.ValueRW.Workers = totalWorkers;
        }
    }
}
