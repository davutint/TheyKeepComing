using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(WaveSpawnSystem))]
    public partial struct DayNightPrepSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<MobileCastleCombatConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var waveState = SystemAPI.GetSingletonRW<WaveStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending || waveState.ValueRO.StressTestMode)
                return;

            var mobileConfig = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
            if (TryApplyInitialDayPrep(ref waveState.ValueRW, mobileConfig))
                return;

            if (waveState.ValueRO.WaveActive || waveState.ValueRO.Phase != RunPhaseType.DayPrep)
                return;

            if (SystemAPI.HasSingleton<MobilePrepPauseState>()
                && SystemAPI.GetSingleton<MobilePrepPauseState>().IsPaused)
            {
                return;
            }

            if (waveState.ValueRO.PrepTimer > 0f)
            {
                float prepTimer = math.max(0f, waveState.ValueRO.PrepTimer - SystemAPI.Time.DeltaTime);
                waveState.ValueRW.PrepTimer = prepTimer;
                if (prepTimer > 0f)
                    return;
            }

            if (SystemAPI.HasSingleton<MobileEconomyEventState>())
            {
                var economyEvent = SystemAPI.GetSingletonRW<MobileEconomyEventState>();
                economyEvent.ValueRW.PendingEvent = MobileEconomyEventType.None;
                economyEvent.ValueRW.EventWave = 0;
            }

            MobileWaveUtility.StartNightWave(ref waveState.ValueRW, mobileConfig);
        }

        private static bool TryApplyInitialDayPrep(ref WaveStateData wave, MobileCastleCombatConfig config)
        {
            if (!wave.WaveActive || wave.Phase != RunPhaseType.NightCombat)
                return false;

            if (wave.WaveStartTimer <= 0f)
                return false;

            if (wave.CurrentWave > 1 || wave.ZombiesSpawned > 0 || wave.ZombiesAlive > 0)
                return false;

            float prepDuration = math.max(0f, config.InitialDayPrepDuration);
            wave.CurrentWave = 0;
            wave.ZombiesToSpawn = 0;
            wave.ZombiesSpawned = 0;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 0f;
            wave.WaveStartTimer = 0f;
            wave.WaveActive = false;
            wave.Phase = RunPhaseType.DayPrep;
            wave.PrepDuration = prepDuration;
            wave.PrepTimer = prepDuration;
            return true;
        }
    }
}
