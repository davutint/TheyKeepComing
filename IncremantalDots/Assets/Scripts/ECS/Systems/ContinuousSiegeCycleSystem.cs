using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(DayNightPrepSystem))]
    public partial struct ContinuousSiegeCycleSystem : ISystem
    {
        private EntityQuery _difficultySampleQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<MobileCastleCombatConfig>();
            state.RequireForUpdate<ContinuousSiegeCycleData>();
            state.RequireForUpdate<ContinuousSpawnBudgetData>();
            _difficultySampleQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<DifficultyDaySample>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var wave = SystemAPI.GetSingletonRW<WaveStateData>();
            var config = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
            var cycle = SystemAPI.GetSingletonRW<ContinuousSiegeCycleData>();
            var budget = SystemAPI.GetSingleton<ContinuousSpawnBudgetData>();

            if (!cycle.ValueRO.Enabled || gameState.IsGameOver || gameState.IsLevelUpPending || wave.ValueRO.StressTestMode)
                return;

            float dayDuration = math.max(0.1f, config.SiegeDayDuration);
            float duskDuration = math.max(0.1f, config.SiegeDuskDuration);
            float nightDuration = math.max(0.1f, config.SiegeNightDuration);
            // Dawn: gece sonrasi odul/nefes fazi (GDD v5 4-faz dongusu). Eski bake'lerde alan 0
            // gelirse dawn'siz 3-faz davranisina duser (geriye uyumlu).
            float dawnDuration = math.max(0f, config.SiegeDawnDuration);
            float authoredDuration = math.max(0.1f, dayDuration + duskDuration + nightDuration + dawnDuration);
            float cycleDuration = math.max(1f, config.SiegeCycleDuration);
            float durationScale = cycleDuration / authoredDuration;

            dayDuration *= durationScale;
            duskDuration *= durationScale;
            nightDuration *= durationScale;
            dawnDuration *= durationScale;

            float timer = cycle.ValueRO.CycleTimer + SystemAPI.Time.DeltaTime;
            float nightEnd = dayDuration + duskDuration + nightDuration;
            bool nightClearance = ContinuousSpawnBudgetUtility.ShouldHoldAtNightEnd(
                cycle.ValueRO.Phase,
                timer,
                nightEnd,
                budget.PendingEnemies,
                wave.ValueRO.ZombiesAlive);
            int wrappedCycles = 0;
            if (nightClearance)
            {
                // Night suresi bitti; yeni demand uretilmez. Backlog sahaya akip son dusman
                // olene kadar timer Dawn'a gecemez.
                timer = nightEnd;
            }
            else
            {
                while (timer >= cycleDuration)
                {
                    timer -= cycleDuration;
                    wrappedCycles++;
                }
            }

            if (timer < 0f)
                timer = 0f;

            // Difficulty profile gun carpani: erken oyun rampi vb. — gece (ve dusk-sonu)
            // siddetini gunun orneklemiyle carpar. Buffer yok/bos = 1 (geriye uyumlu).
            int currentCycleIndex = math.max(0, cycle.ValueRO.CycleIndex);
            float dayNightMult = 1f;
            if (!_difficultySampleQuery.IsEmptyIgnoreFilter)
            {
                var difficultySamples = _difficultySampleQuery
                    .GetSingletonBuffer<DifficultyDaySample>(true);
                if (difficultySamples.Length > 0)
                {
                    var sample = difficultySamples[math.min(currentCycleIndex, difficultySamples.Length - 1)];
                    dayNightMult = math.max(0.01f, sample.NightIntensityMult);
                }
            }

            SiegeCyclePhase phase;
            float phaseProgress;
            float intensity;
            if (timer < dayDuration)
            {
                phase = SiegeCyclePhase.Day;
                phaseProgress = math.saturate(timer / math.max(0.01f, dayDuration));
                intensity = 0f;
            }
            else if (timer < dayDuration + duskDuration)
            {
                phase = SiegeCyclePhase.Dusk;
                phaseProgress = math.saturate((timer - dayDuration) / math.max(0.01f, duskDuration));
                intensity = 0f;
            }
            else if (timer < nightEnd || nightClearance || dawnDuration <= 0f)
            {
                phase = SiegeCyclePhase.Night;
                phaseProgress = math.saturate((timer - dayDuration - duskDuration) / math.max(0.01f, nightDuration));
                intensity = nightClearance
                    ? 0f
                    : math.max(0.01f, config.SiegeNightIntensityMultiplier * dayNightMult);
            }
            else
            {
                phase = SiegeCyclePhase.Dawn;
                phaseProgress = math.saturate((timer - dayDuration - duskDuration - nightDuration) / math.max(0.01f, dawnDuration));
                intensity = 0f;
            }

            int cycleIndex = math.max(0, cycle.ValueRO.CycleIndex + wrappedCycles);
            cycle.ValueRW.Enabled = true;
            cycle.ValueRW.CycleTimer = timer;
            cycle.ValueRW.CycleDuration = cycleDuration;
            cycle.ValueRW.DayDuration = dayDuration;
            cycle.ValueRW.DuskDuration = duskDuration;
            cycle.ValueRW.NightDuration = nightDuration;
            cycle.ValueRW.DawnDuration = dawnDuration;
            cycle.ValueRW.CycleProgress01 = math.saturate(timer / cycleDuration);
            cycle.ValueRW.PhaseProgress01 = phaseProgress;
            cycle.ValueRW.SpawnIntensityMultiplier = intensity;
            cycle.ValueRW.HordePressure01 = ResolvePressure01(intensity, config);
            cycle.ValueRW.CycleIndex = cycleIndex;
            cycle.ValueRW.Phase = phase;
            cycle.ValueRW.IsBloodMoonNight = false;

            wave.ValueRW.CurrentWave = math.max(1, cycleIndex + 1);
            MobileWaveUtility.ConfigureMobileWave(ref wave.ValueRW, config);
            if (phase == SiegeCyclePhase.Night && cycle.ValueRO.Phase != SiegeCyclePhase.Night)
                wave.ValueRW.SpawnTimer = 0f;
            wave.ValueRW.WaveActive = true;
            wave.ValueRW.Phase = RunPhaseType.NightCombat;
            wave.ValueRW.PrepTimer = 0f;
            wave.ValueRW.PrepDuration = 0f;
            wave.ValueRW.WaveStartTimer = 0f;
        }

        private static float ResolvePressure01(float intensity, MobileCastleCombatConfig config)
        {
            if (intensity <= 0f)
                return 0f;

            float minIntensity = math.max(0.01f, config.SiegeDayIntensityMultiplier);
            float maxIntensity = math.max(config.SiegeNightIntensityMultiplier,
                math.max(config.SiegeDuskEndIntensityMultiplier, config.SiegeDuskStartIntensityMultiplier));
            return math.saturate((intensity - minIntensity) / math.max(0.01f, maxIntensity - minIntensity));
        }
    }
}
