using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ArcherShootSystem))]
    public partial struct CastleYardPrepSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<CastleYardPrepState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var waveState = SystemAPI.GetSingleton<WaveStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending || waveState.StressTestMode)
                return;

            var prep = SystemAPI.GetSingletonRW<CastleYardPrepState>();
            if (prep.ValueRO.RallyTimer <= 0f)
                return;

            prep.ValueRW.RallyTimer = math.max(0f, prep.ValueRO.RallyTimer - SystemAPI.Time.DeltaTime);
        }
    }
}
