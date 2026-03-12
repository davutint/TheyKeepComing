using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Kaynak uretim/tuketim tick sistemi.
    /// Net hiz = (Production - Consumption) per-minute → * dt / 60f → accumulator → int transfer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(WaveSpawnSystem))]
    public partial struct ResourceTickSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ResourceData>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            float dtPerMin = dt / 60f;

            var production = SystemAPI.GetSingleton<ResourceProductionRate>();
            var consumption = SystemAPI.GetSingleton<ResourceConsumptionRate>();

            var resourceRW = SystemAPI.GetSingletonRW<ResourceData>();
            var accumulatorRW = SystemAPI.GetSingletonRW<ResourceAccumulator>();

            // Net hiz hesapla ve accumulator'a ekle
            accumulatorRW.ValueRW.Wood += (production.WoodPerMin - consumption.WoodPerMin) * dtPerMin;
            accumulatorRW.ValueRW.Stone += (production.StonePerMin - consumption.StonePerMin) * dtPerMin;
            accumulatorRW.ValueRW.Iron += (production.IronPerMin - consumption.IronPerMin) * dtPerMin;
            accumulatorRW.ValueRW.Food += (production.FoodPerMin - consumption.FoodPerMin) * dtPerMin;

            // Accumulator → int transfer
            TransferAccumulator(ref resourceRW.ValueRW.Wood, ref accumulatorRW.ValueRW.Wood);
            TransferAccumulator(ref resourceRW.ValueRW.Stone, ref accumulatorRW.ValueRW.Stone);
            TransferAccumulator(ref resourceRW.ValueRW.Iron, ref accumulatorRW.ValueRW.Iron);
            TransferAccumulator(ref resourceRW.ValueRW.Food, ref accumulatorRW.ValueRW.Food);
        }

        /// <summary>
        /// Accumulator ±1.0 gecince int'e transfer eder.
        /// Kaynak 0'in altina dusmez — negatif birikim sifirlanir.
        /// </summary>
        private static void TransferAccumulator(ref int resource, ref float accumulator)
        {
            if (accumulator >= 1f)
            {
                int transfer = (int)accumulator;
                resource += transfer;
                accumulator -= transfer;
            }
            else if (accumulator <= -1f)
            {
                int transfer = (int)math.ceil(math.abs(accumulator));
                if (resource >= transfer)
                {
                    resource -= transfer;
                    accumulator += transfer;
                }
                else
                {
                    // Kaynak yeterli degil — mevcut kaynak kadar dus, accumulator sifirla
                    resource = 0;
                    accumulator = 0f;
                }
            }
        }
    }
}
