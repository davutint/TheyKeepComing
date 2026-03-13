using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Ok uretim sistemi.
    /// Fletcher binalarindaki isciler ok uretir ve ahsap tuketir.
    /// BuildingPopulationSystem'den SONRA, PopulationTickSystem'den ONCE calisir.
    /// WoodPerMin consumption'a += ile eklenir (BuildingPopulationSystem sifirladiktan sonra).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildingPopulationSystem))]
    [UpdateBefore(typeof(PopulationTickSystem))]
    public partial struct ArrowProductionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowSupply>();
            state.RequireForUpdate<ResourceConsumptionRate>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            float dt = SystemAPI.Time.DeltaTime;

            // Tum Fletcher'larin toplam ok uretim ve ahsap tuketim hizi
            float totalArrowRate = 0f;
            float totalWoodCost = 0f;

            foreach (var arrowProducer in SystemAPI.Query<RefRO<ArrowProducer>>())
            {
                int workers = arrowProducer.ValueRO.AssignedWorkers;
                if (workers <= 0) continue;

                totalArrowRate += arrowProducer.ValueRO.ArrowsPerWorkerPerMin * workers;
                totalWoodCost += arrowProducer.ValueRO.WoodCostPerBatchPerMin * workers;
            }

            // Ahsap tuketimi consumption'a ekle (BuildingPopulationSystem sifirladiktan sonra)
            if (totalWoodCost > 0f)
            {
                var consumptionRW = SystemAPI.GetSingletonRW<ResourceConsumptionRate>();
                consumptionRW.ValueRW.WoodPerMin += totalWoodCost;
            }

            // Ok uretimi — accumulator pattern (ResourceTickSystem benzeri)
            if (totalArrowRate > 0f)
            {
                var arrowRW = SystemAPI.GetSingletonRW<ArrowSupply>();
                arrowRW.ValueRW.Accumulator += totalArrowRate * dt / 60f;

                // Tam oklar transfer edilir
                if (arrowRW.ValueRO.Accumulator >= 1f)
                {
                    int transfer = (int)arrowRW.ValueRO.Accumulator;
                    arrowRW.ValueRW.Current += transfer;
                    arrowRW.ValueRW.Accumulator -= transfer;
                }
            }
        }
    }
}
