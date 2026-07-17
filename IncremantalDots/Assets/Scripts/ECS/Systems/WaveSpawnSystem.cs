using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(ContinuousSiegeCycleSystem))]
    public partial struct WaveSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<EnemyCatalogRuntimeData>();
            state.RequireForUpdate<EnemyPoolRuntimeData>();
            state.RequireForUpdate<GameStateData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var waveState = SystemAPI.GetSingletonRW<WaveStateData>();
            bool mobileMode = SystemAPI.HasSingleton<MobileCastleCombatConfig>();
            var mobileConfig = mobileMode
                ? SystemAPI.GetSingleton<MobileCastleCombatConfig>()
                : default;
            bool workerEconomyMode = mobileMode && SystemAPI.HasSingleton<MobilePopulationAllocation>();
            EconomyFocusType economyFocus = mobileMode && !workerEconomyMode && SystemAPI.HasSingleton<EconomyFocusState>()
                ? SystemAPI.GetSingleton<EconomyFocusState>().Type
                : EconomyFocusType.Balanced;
            float mobileRewardMultiplier = workerEconomyMode
                ? math.clamp(mobileConfig.WorkerEconomyRewardMultiplier, 0f, 1f)
                : 1f;
            float dt = SystemAPI.Time.DeltaTime;

            // Stress Test Mode: wave/gameover kontrolu yok, surekli spawn
            if (waveState.ValueRO.StressTestMode)
            {
                waveState.ValueRW.SpawnTimer -= dt;
                int maxAlive = mobileMode ? math.max(1, mobileConfig.StressMaxAliveZombies) : int.MaxValue;
                if (waveState.ValueRO.SpawnTimer <= 0f && waveState.ValueRO.ZombiesAlive < maxAlive)
                {
                    waveState.ValueRW.SpawnTimer = mobileMode
                        ? mobileConfig.StressSpawnInterval
                        : waveState.ValueRO.SpawnInterval;

                    int desiredBatch = mobileMode ? math.max(1, mobileConfig.StressSpawnBatchSize) : 20;
                    int stressBatch = math.min(desiredBatch, maxAlive - waveState.ValueRO.ZombiesAlive);
                    SpawnZombieBatch(ref state, ref waveState.ValueRW, stressBatch, mobileMode, mobileConfig);
                }
                return;
            }

            // --- Normal wave mantigi ---
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            if (mobileMode && SystemAPI.HasSingleton<ContinuousSiegeCycleData>())
            {
                var cycle = SystemAPI.GetSingleton<ContinuousSiegeCycleData>();
                if (cycle.Enabled)
                {
                    if (SystemAPI.HasSingleton<ContinuousSpawnBudgetData>())
                    {
                        var budget = SystemAPI.GetSingletonRW<ContinuousSpawnBudgetData>();
                        HandleContinuousSiegeSpawn(ref state, ref waveState.ValueRW,
                            ref budget.ValueRW, mobileConfig, cycle, dt);
                    }
                    return;
                }
            }

            if (!waveState.ValueRO.WaveActive)
                return;

            // Wave baslama gecikmesi
            if (waveState.ValueRO.WaveStartTimer > 0f)
            {
                waveState.ValueRW.WaveStartTimer -= dt;
                return;
            }

            // Tum zombiler spawn edildi ve oldu → yeni wave
            if (waveState.ValueRO.ZombiesSpawned >= waveState.ValueRO.ZombiesToSpawn
                && waveState.ValueRO.ZombiesAlive <= 0)
            {
                if (mobileMode && SystemAPI.HasSingleton<ResourceData>())
                {
                    var resources = SystemAPI.GetSingletonRW<ResourceData>();
                    var reward = CalculateWaveClearBonus(mobileConfig, economyFocus,
                        waveState.ValueRO.CurrentWave, mobileRewardMultiplier);
                    resources.ValueRW.Wood += reward.Wood;
                    resources.ValueRW.Stone += reward.Stone;
                    resources.ValueRW.Iron += reward.Iron;
                    resources.ValueRW.Food += reward.Food;
                    if (SystemAPI.HasSingleton<WaveClearRewardData>())
                    {
                        var rewardData = SystemAPI.GetSingletonRW<WaveClearRewardData>();
                        reward.Sequence = rewardData.ValueRO.Sequence + 1;
                        rewardData.ValueRW.Sequence = reward.Sequence;
                        rewardData.ValueRW.Wave = reward.Wave;
                        rewardData.ValueRW.Wood = reward.Wood;
                        rewardData.ValueRW.Stone = reward.Stone;
                        rewardData.ValueRW.Iron = reward.Iron;
                        rewardData.ValueRW.Food = reward.Food;
                    }
                }

                if (mobileMode && SystemAPI.HasSingleton<CastleYardPrepState>())
                {
                    var prep = SystemAPI.GetSingletonRW<CastleYardPrepState>();
                    prep.ValueRW.FortifyActive = false;
                    prep.ValueRW.RallyTimer = 0f;
                }

                waveState.ValueRW.WaveActive = false;
                if (mobileMode)
                {
                    float prepDuration = math.max(1f, mobileConfig.DayPrepDuration);
                    waveState.ValueRW.Phase = RunPhaseType.DayPrep;
                    waveState.ValueRW.PrepDuration = prepDuration;
                    waveState.ValueRW.PrepTimer = prepDuration;
                }
                waveState.ValueRW.SpawnTimer = 0f;
                waveState.ValueRW.WaveStartTimer = 0f;
                return;
            }

            // Spawn zamani — frame basina batch spawn
            if (waveState.ValueRO.ZombiesSpawned < waveState.ValueRO.ZombiesToSpawn)
            {
                waveState.ValueRW.SpawnTimer -= dt;
                if (waveState.ValueRO.SpawnTimer <= 0f)
                {
                    float intervalMultiplier = mobileMode
                        ? GetMobileSpawnIntervalMultiplier(waveState.ValueRO, mobileConfig)
                        : 1f;
                    waveState.ValueRW.SpawnTimer = math.max(0.01f,
                        waveState.ValueRO.SpawnInterval * intervalMultiplier);

                    int desiredBatch = mobileMode
                        ? GetMobileSpawnBatchSize(waveState.ValueRO, mobileConfig)
                        : 20;
                    int batchSize = math.min(desiredBatch, waveState.ValueRO.ZombiesToSpawn - waveState.ValueRO.ZombiesSpawned);
                    SpawnZombieBatch(ref state, ref waveState.ValueRW, batchSize, mobileMode, mobileConfig);
                }
            }
        }

        private int SpawnZombieBatch(ref SystemState state, ref WaveStateData wave, int count,
            bool mobileMode, MobileCastleCombatConfig mobileConfig)
        {
            var catalog = SystemAPI.GetSingleton<EnemyCatalogRuntimeData>();
            var enemyEntries = SystemAPI.GetSingletonBuffer<EnemyCatalogEntryData>(true);
            int activeEnemyIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(catalog, enemyEntries.Length);
            if (activeEnemyIndex < 0)
                return 0;

            EnemyCatalogEntryData enemy = enemyEntries[activeEnemyIndex];
            Entity poolEntity = SystemAPI.GetSingletonEntity<EnemyPoolRuntimeData>();
            var random = new Random(wave.SpawnRandomState != 0u ? wave.SpawnRandomState : 42u);
            int spawned = 0;

            // Prefab'dan scale degerini oku — Inspector'da ayarlanan deger kullanilir
            float prefabScale = state.EntityManager
                .GetComponentData<LocalTransform>(enemy.Prefab).Scale;
            float spawnScale = mobileMode ? enemy.Scale : prefabScale;

            for (int i = 0; i < count; i++)
            {
                if (!EnemyPoolRuntimeUtility.TryRent(state.EntityManager, poolEntity, out Entity zombie))
                    break;

                float spawnX;
                float spawnY;

                if (mobileMode)
                {
                    if (mobileConfig.SingleFrontEnabled)
                    {
                        // Tek cephe (K4): sag kenar seridinden, dikey bantta rastgele
                        spawnX = mobileConfig.SpawnLineX + random.NextFloat(0f, 2f);
                        spawnY = random.NextFloat(-mobileConfig.SpawnBandYHalf, mobileConfig.SpawnBandYHalf);
                    }
                    else
                    {
                        float angle = random.NextFloat(0f, math.PI * 2f);
                        float2 dir = new float2(math.cos(angle), math.sin(angle));
                        float2 spawnPos = mobileConfig.CastleCenter + dir * mobileConfig.SpawnRadius;
                        spawnX = spawnPos.x;
                        spawnY = spawnPos.y;
                    }
                }
                else
                {
                    // Sag tarafta random Y pozisyonunda, X de biraz dagitilmis
                    spawnX = 28f + random.NextFloat(0f, 5f);
                    spawnY = random.NextFloat(-12f, 12f);
                }

                var transform = LocalTransform.FromPositionRotationScale(
                    new float3(spawnX, spawnY, MobileCastleRenderDepth.UnitZ),
                    quaternion.identity,
                    spawnScale
                );
                state.EntityManager.SetComponentData(zombie, transform);
                state.EntityManager.SetComponentData(zombie, new ZombieState { Value = ZombieStateType.Moving });
                state.EntityManager.SetComponentData(zombie, new ZombieSlow
                {
                    Duration = 0f,
                    SpeedMultiplier = 1f
                });
                state.EntityManager.SetComponentEnabled<ZombieSlow>(zombie, false);
                state.EntityManager.SetComponentData(zombie, new ZombieStats
                {
                    MoveSpeed = mobileMode ? enemy.BaseMoveSpeed : wave.ZombieSpeed,
                    MaxHP = mobileMode ? enemy.BaseHP : wave.ZombieHP,
                    CurrentHP = mobileMode ? enemy.BaseHP : wave.ZombieHP,
                    AttackDamage = mobileMode ? enemy.BaseDamage : wave.ZombieDamage,
                    AttackCooldown = 1f,
                    AttackTimer = 0f,
                    XPReward = mobileMode ? enemy.XPReward : 10
                });

                wave.ZombiesSpawned++;
                wave.ZombiesAlive++;
                spawned++;
            }

            wave.SpawnRandomState = random.state;

            if (spawned > 0 && !wave.StressTestMode
                && SystemAPI.HasSingleton<RunTelemetryData>())
            {
                var telemetry = SystemAPI.GetSingletonRW<RunTelemetryData>();
                RunTelemetryData value = telemetry.ValueRO;
                RunTelemetryAccumulator.ObserveEnemyCount(ref value, wave.ZombiesAlive);
                telemetry.ValueRW = value;
            }

            return spawned;
        }

        private void HandleContinuousSiegeSpawn(ref SystemState state, ref WaveStateData wave,
            ref ContinuousSpawnBudgetData budget, MobileCastleCombatConfig mobileConfig,
            ContinuousSiegeCycleData cycle, float dt)
        {
            wave.WaveActive = true;
            wave.Phase = RunPhaseType.NightCombat;
            wave.PrepTimer = 0f;
            wave.PrepDuration = 0f;
            wave.WaveStartTimer = 0f;

            float phaseIntensity = math.max(0.01f, cycle.SpawnIntensityMultiplier);

            // Council risk atomu: "sonraki gece" carpani yalniz NIGHT fazinda uygulanir
            if (cycle.Phase == SiegeCyclePhase.Night
                && SystemAPI.HasSingleton<MobileEconomyEventState>())
            {
                float nightMult = SystemAPI.GetSingleton<MobileEconomyEventState>().NextNightSpawnMultiplier;
                if (nightMult > 0f)
                    phaseIntensity *= nightMult;
            }

            // Gun tabani ile faz temposu ayri tutulur. Dawn'in dusuk phase intensity'si
            // sonraki gunun day curve/interval tabanini geriye yazamaz.
            float dayQuantityMultiplier = 1f;
            if (SystemAPI.TryGetSingletonBuffer<DifficultyDaySample>(out var difficultySamples, true)
                && difficultySamples.Length > 0)
            {
                int dayIndex = math.min(math.max(0, wave.CurrentWave - 1), difficultySamples.Length - 1);
                dayQuantityMultiplier = math.max(0.01f, difficultySamples[dayIndex].SpawnBatchMult);
            }

            float dayBaseInterval = wave.SpawnInterval > 0f
                ? wave.SpawnInterval
                : math.max(mobileConfig.MinSpawnInterval, mobileConfig.BaseSpawnInterval);
            float effectiveInterval = ContinuousSpawnBudgetUtility.ResolveEffectiveInterval(
                dayBaseInterval, phaseIntensity, mobileConfig.MinSpawnInterval);
            int demandPerInterval = ContinuousSpawnBudgetUtility.ResolveDemandPerInterval(
                mobileConfig.SpawnBatchSize,
                mobileConfig.SpawnBatchGrowthPerCycle,
                wave.CurrentWave,
                dayQuantityMultiplier,
                phaseIntensity,
                mobileConfig.MaxSpawnBatch);

            budget.DayQuantityMultiplier = dayQuantityMultiplier;
            budget.DayBaseSpawnInterval = dayBaseInterval;
            budget.PhaseIntensityMultiplier = phaseIntensity;
            budget.EffectiveSpawnInterval = effectiveInterval;
            budget.DemandPerInterval = demandPerInterval;
            budget.LastDemandedEnemies = 0;
            budget.LastSpawnedEnemies = 0;

            wave.SpawnTimer -= dt;
            int elapsedIntervals = ContinuousSpawnBudgetUtility.CountElapsedIntervals(
                wave.SpawnTimer, effectiveInterval);
            if (elapsedIntervals > 0)
            {
                long pendingBefore = budget.PendingEnemies;
                budget.PendingEnemies = ContinuousSpawnBudgetUtility.AddDemand(
                    budget.PendingEnemies, demandPerInterval, elapsedIntervals);
                long demanded = budget.PendingEnemies - math.max(0L, pendingBefore);
                budget.LastDemandedEnemies = (int)math.min(int.MaxValue, demanded);
                budget.TotalDemandedEnemies = ContinuousSpawnBudgetUtility.AddTelemetry(
                    budget.TotalDemandedEnemies, demanded);
                wave.SpawnTimer = ContinuousSpawnBudgetUtility.AdvanceTimer(
                    wave.SpawnTimer, effectiveInterval, elapsedIntervals);
            }

            int maxDrainPerFrame = mobileConfig.MaxSpawnBatch > 0
                ? mobileConfig.MaxSpawnBatch
                : demandPerInterval;
            int spawnCount = ContinuousSpawnBudgetUtility.ResolveDrainCount(
                budget.PendingEnemies,
                wave.ZombiesAlive,
                mobileConfig.MaxAliveZombies,
                math.max(1, maxDrainPerFrame));
            if (spawnCount <= 0)
                return;

            int spawned = SpawnZombieBatch(ref state, ref wave, spawnCount, true, mobileConfig);
            budget.PendingEnemies -= spawned;
            budget.LastSpawnedEnemies = spawned;
            budget.TotalSpawnedEnemies = ContinuousSpawnBudgetUtility.AddTelemetry(
                budget.TotalSpawnedEnemies, spawned);
        }

        private static WaveClearRewardData CalculateWaveClearBonus(
            MobileCastleCombatConfig config, EconomyFocusType focus, int currentWave, float rewardMultiplier)
        {
            int wave = math.max(1, currentWave);
            return new WaveClearRewardData
            {
                Sequence = 0,
                Wave = wave,
                Wood = ApplyWaveClearFocus(config.WaveClearWoodBase + config.WaveClearWoodPerWave * wave,
                    config, focus, EconomyFocusType.Wood, rewardMultiplier),
                Stone = ApplyWaveClearFocus(config.WaveClearStoneBase + config.WaveClearStonePerWave * wave,
                    config, focus, EconomyFocusType.Stone, rewardMultiplier),
                Iron = ApplyWaveClearFocus(config.WaveClearIronBase + config.WaveClearIronPerWave * wave,
                    config, focus, EconomyFocusType.Iron, rewardMultiplier),
                Food = ApplyWaveClearFocus(config.WaveClearFoodBase + config.WaveClearFoodPerWave * wave,
                    config, focus, EconomyFocusType.Food, rewardMultiplier)
            };
        }

        private static int ApplyWaveClearFocus(int baseValue, MobileCastleCombatConfig config,
            EconomyFocusType focus, EconomyFocusType resource, float rewardMultiplier)
        {
            float multiplier = EconomyFocusUtility.GetWaveClearMultiplier(config, focus, resource);
            return (int)math.ceil(baseValue * multiplier * rewardMultiplier);
        }

        private static float GetMobileSpawnProgress(WaveStateData wave)
        {
            int total = math.max(1, wave.ZombiesToSpawn);
            return math.saturate((float)wave.ZombiesSpawned / total);
        }

        private static float GetMobileSpawnIntervalMultiplier(WaveStateData wave, MobileCastleCombatConfig config)
        {
            float progress = GetMobileSpawnProgress(wave);
            if (progress < config.OpeningEnemyRatio)
                return math.max(0.01f, config.OpeningIntervalMultiplier);

            if (progress >= 1f - config.FinalEnemyRatio)
                return math.max(0.01f, config.FinalIntervalMultiplier);

            return 1f;
        }

        private static int GetMobileSpawnBatchSize(WaveStateData wave, MobileCastleCombatConfig config)
        {
            int batchSize = math.max(1, config.SpawnBatchSize);
            float progress = GetMobileSpawnProgress(wave);
            if (progress < config.OpeningEnemyRatio)
                batchSize += config.OpeningBatchDelta;
            else if (progress >= 1f - config.FinalEnemyRatio)
                batchSize += config.FinalBatchDelta;

            return math.max(1, batchSize);
        }
    }
}
