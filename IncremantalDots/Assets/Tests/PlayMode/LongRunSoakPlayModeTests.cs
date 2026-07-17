using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    /// <summary>
    /// Release active-cap/backlog sozlesmesini uzun sureli, birlesik runtime yuku altinda olcer.
    /// Normal regresyon setine dahil degildir; yalnizca hedefli V1 performans kabul kosusunda calisir.
    /// </summary>
    public class LongRunSoakPlayModeTests
    {
        private const int ReleaseActiveCap = 900;
        private const int ArcherTarget = 1_000;
        private const long SeedBacklog = 10_000L;
        private const int ProjectileWarmupFrames = 360;
        // 60 FPS kabulunde tam bir dakikalik steady-state pencere.
        private const int SoakFrames = 3_600;
        private const int CapacityReleaseBatch = 128;
        private const int FillTimeoutFrames = 240;
        private const int ProjectileStartTimeoutFrames = 300;
        private const int DrainTimeoutFrames = 2_000;

        private string _runSavePath;
        private byte[] _originalRunSave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            RunPersistence.Delete();
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;

            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 120 && GameManager.Instance == null; frame++)
                yield return null;

            Assert.That(GameManager.Instance, Is.Not.Null, "NewGameScene GameManager olusturmadi.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        [Explicit("Targeted release-cap/backlog long-run soak only; normal regression setinde calismaz.")]
        public IEnumerator LongRunSoak_ReleaseCapAndBacklog_StayBoundedAndDrainThroughPools()
        {
            GameManager gameManager = GameManager.Instance;
            bool runtimeReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    runtimeReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(runtimeReady, Is.True, "GameManager/SubScene 300 frame icinde hazir olmadi.");

            Time.timeScale = 1f;
            gameManager.enabled = true;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(ContinuousSiegeCycleData),
                typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            Entity enemyPoolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData),
                typeof(EnemyPoolAvailable),
                typeof(EnemyCatalogRuntimeData),
                typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            Entity arrowPoolEntity = entityManager.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData),
                typeof(ArrowPoolAvailable)).GetSingletonEntity();

            GameStateData gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            entityManager.SetComponentData(gameStateEntity, gameState);

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, enemyPoolEntity);
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);
            using (EntityQuery nonPoolZombieQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
                   {
                       All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                       None = new[]
                       {
                           ComponentType.ReadOnly<Prefab>(),
                           ComponentType.ReadOnly<EnemyPoolMember>()
                       }
                   }))
            {
                entityManager.DestroyEntity(nonPoolZombieQuery);
            }

            using EntityQuery activeZombieQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZombieTag>(),
                    ComponentType.ReadOnly<EnemyPoolMember>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using EntityQuery archerQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ArcherUnit>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using EntityQuery projectileQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ArrowTag>(),
                    ComponentType.ReadOnly<ArrowProjectile>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            Assert.That(activeZombieQuery.CalculateEntityCount(), Is.Zero);

            MobileCastleCombatConfig config = entityManager
                .GetComponentData<MobileCastleCombatConfig>(configEntity);
            Assert.That(config.MaxAliveZombies, Is.EqualTo(ReleaseActiveCap),
                "Soak release active-cap degerini olcmeli; test override'i kullanmamalidir.");
            Assert.That(config.MaxSpawnBatch, Is.GreaterThan(0));
            Assert.That(config.ContinuousSiegeEnabled, Is.True);

            EnemyCatalogRuntimeData catalog = entityManager
                .GetComponentData<EnemyCatalogRuntimeData>(enemyPoolEntity);
            DynamicBuffer<EnemyCatalogEntryData> enemyEntries = entityManager
                .GetBuffer<EnemyCatalogEntryData>(enemyPoolEntity);
            int activeEntryIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(catalog, enemyEntries.Length);
            Assert.That(activeEntryIndex, Is.GreaterThanOrEqualTo(0));
            EnemyCatalogEntryData soakEnemy = enemyEntries[activeEntryIndex];
            soakEnemy.BaseHP = 1_000_000_000f;
            soakEnemy.BaseDamage = 0f;
            enemyEntries[activeEntryIndex] = soakEnemy;

            ContinuousSiegeCycleData cycle = entityManager
                .GetComponentData<ContinuousSiegeCycleData>(configEntity);
            float authoredDuration = math.max(0.1f,
                math.max(0.1f, config.SiegeDayDuration)
                + math.max(0.1f, config.SiegeDuskDuration)
                + math.max(0.1f, config.SiegeNightDuration)
                + math.max(0f, config.SiegeDawnDuration));
            float durationScale = math.max(1f, config.SiegeCycleDuration) / authoredDuration;
            cycle.Enabled = true;
            cycle.CycleIndex = 59;
            cycle.CycleTimer = (math.max(0.1f, config.SiegeDayDuration)
                                + math.max(0.1f, config.SiegeDuskDuration)) * durationScale + 0.1f;
            entityManager.SetComponentData(configEntity, cycle);

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            wave.ZombiesSpawned = 0;
            wave.SpawnTimer = float.MaxValue;
            entityManager.SetComponentData(waveEntity, wave);

            entityManager.SetComponentData(configEntity, new ContinuousSpawnBudgetData
            {
                PendingEnemies = SeedBacklog,
                TotalDemandedEnemies = SeedBacklog,
                TotalSpawnedEnemies = 0L
            });

            int fillFrames = 0;
            for (; fillFrames < FillTimeoutFrames; fillFrames++)
            {
                yield return null;
                wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
                Assert.That(wave.ZombiesAlive, Is.LessThanOrEqualTo(ReleaseActiveCap));
                if (wave.ZombiesAlive == ReleaseActiveCap)
                    break;
            }

            Assert.That(wave.ZombiesAlive, Is.EqualTo(ReleaseActiveCap),
                $"Release cap {FillTimeoutFrames} frame icinde dolmadi.");
            ContinuousSpawnBudgetData filledBudget = entityManager
                .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            EnemyPoolRuntimeData filledEnemyPool = entityManager
                .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
            Assert.That(filledBudget.LastDemandedEnemies, Is.Zero);
            Assert.That(filledBudget.TotalSpawnedEnemies, Is.EqualTo(ReleaseActiveCap));
            Assert.That(filledBudget.PendingEnemies, Is.EqualTo(SeedBacklog - ReleaseActiveCap));
            Assert.That(filledEnemyPool.ActiveCount, Is.EqualTo(ReleaseActiveCap));
            Assert.That(filledEnemyPool.AvailableCount + filledEnemyPool.ActiveCount,
                Is.EqualTo(filledEnemyPool.TotalCreated));
            Assert.That(activeZombieQuery.CalculateEntityCount(), Is.EqualTo(ReleaseActiveCap));

            MethodInfo restoreArcherCounts = typeof(GameManager).GetMethod(
                "RestoreArcherCountsWithinCapacity",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(restoreArcherCounts, Is.Not.Null);
            restoreArcherCounts.Invoke(gameManager, new object[] { ArcherTarget, 0, 0 });
            Assert.That(archerQuery.CalculateEntityCount(), Is.EqualTo(ArcherTarget));
            Assert.That(gameManager.BasicArcherCount, Is.EqualTo(ArcherTarget));

            using (NativeArray<Entity> archers = archerQuery.ToEntityArray(Allocator.Temp))
            {
                for (int i = 0; i < archers.Length; i++)
                {
                    ArcherUnit archer = entityManager.GetComponentData<ArcherUnit>(archers[i]);
                    archer.FireTimer = 0f;
                    entityManager.SetComponentData(archers[i], archer);
                }
            }

            Entity arrowSupplyEntity = entityManager.CreateEntityQuery(typeof(ArrowSupply)).GetSingletonEntity();
            ArrowSupply arrowSupply = entityManager.GetComponentData<ArrowSupply>(arrowSupplyEntity);
            arrowSupply.Current = 10_000_000;
            entityManager.SetComponentData(arrowSupplyEntity, arrowSupply);

            using (NativeArray<Entity> zombies = activeZombieQuery.ToEntityArray(Allocator.Temp))
            {
                float targetX = config.FrontlineX + math.max(2f, config.AttackRadius + 0.5f);
                for (int i = 0; i < zombies.Length; i++)
                {
                    Entity zombie = zombies[i];
                    LocalTransform transform = entityManager.GetComponentData<LocalTransform>(zombie);
                    transform.Position.x = targetX + (i % 8) * 0.04f;
                    entityManager.SetComponentData(zombie, transform);

                    ZombieStats stats = entityManager.GetComponentData<ZombieStats>(zombie);
                    stats.MaxHP = soakEnemy.BaseHP;
                    stats.CurrentHP = soakEnemy.BaseHP;
                    stats.AttackDamage = 0f;
                    entityManager.SetComponentData(zombie, stats);
                }
            }

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.SpawnTimer = 0f;
            entityManager.SetComponentData(waveEntity, wave);

            int projectileStartFrames = 0;
            for (; projectileStartFrames < ProjectileStartTimeoutFrames; projectileStartFrames++)
            {
                yield return null;
                if (projectileQuery.CalculateEntityCount() > 0)
                    break;
            }
            Assert.That(projectileQuery.CalculateEntityCount(), Is.GreaterThan(0),
                "Release-cap horde icinde 1K canonical archer projectile uretmedi.");

            for (int frame = 0; frame < ProjectileWarmupFrames; frame++)
                yield return null;

            ContinuousSpawnBudgetData budgetBeforeSoak = entityManager
                .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            EnemyPoolRuntimeData enemyPoolBeforeSoak = entityManager
                .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
            ArrowPoolRuntimeData arrowPoolBeforeSoak = entityManager
                .GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(budgetBeforeSoak.PendingEnemies, Is.GreaterThan(0));
            Assert.That(enemyPoolBeforeSoak.ActiveCount, Is.EqualTo(ReleaseActiveCap));
            Assert.That(arrowPoolBeforeSoak.TotalRentCount, Is.GreaterThan(0));
            Assert.That(arrowPoolBeforeSoak.TotalReturnCount, Is.GreaterThan(0));

            var frameTimes = new double[SoakFrames];
            double frameTotalMs = 0.0;
            double frameMaxMs = 0.0;
            double mainThreadTotalMs = 0.0;
            double mainThreadMaxMs = 0.0;
            long gcTotalBytes = 0L;
            long gcMaxBytes = 0L;
            long usedMemoryStartBytes = 0L;
            long usedMemoryEndBytes = 0L;
            long usedMemoryPeakBytes = 0L;
            long backlogPeak = budgetBeforeSoak.PendingEnemies;
            long previousBacklog = budgetBeforeSoak.PendingEnemies;
            int maxObservedAlive = ReleaseActiveCap;
            bool capViolation = false;
            bool backlogRegression = false;
            bool demandAccountingMismatch = false;
            bool enemyPoolMismatch = false;
            bool arrowPoolMismatch = false;
            bool entityCountMismatch = false;
            bool gameOverObserved = false;

            using (var mainThread = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal, "Main Thread", 1))
            using (var gcAllocated = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            using (var usedMemory = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory, "Total Used Memory", 1))
            {
                for (int frame = 0; frame < SoakFrames; frame++)
                {
                    yield return null;

                    double frameMs = Time.unscaledDeltaTime * 1000.0;
                    frameTimes[frame] = frameMs;
                    frameTotalMs += frameMs;
                    frameMaxMs = Math.Max(frameMaxMs, frameMs);

                    if (mainThread.Valid)
                    {
                        double mainMs = mainThread.LastValue / 1_000_000.0;
                        mainThreadTotalMs += mainMs;
                        mainThreadMaxMs = Math.Max(mainThreadMaxMs, mainMs);
                    }
                    if (gcAllocated.Valid)
                    {
                        gcTotalBytes += gcAllocated.LastValue;
                        gcMaxBytes = Math.Max(gcMaxBytes, gcAllocated.LastValue);
                    }
                    if (usedMemory.Valid)
                    {
                        long currentUsedMemory = usedMemory.LastValue;
                        if (usedMemoryStartBytes == 0L)
                            usedMemoryStartBytes = currentUsedMemory;
                        usedMemoryEndBytes = currentUsedMemory;
                        usedMemoryPeakBytes = Math.Max(usedMemoryPeakBytes, currentUsedMemory);
                    }

                    WaveStateData currentWave = entityManager.GetComponentData<WaveStateData>(waveEntity);
                    ContinuousSpawnBudgetData currentBudget = entityManager
                        .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
                    EnemyPoolRuntimeData currentEnemyPool = entityManager
                        .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
                    ArrowPoolRuntimeData currentArrowPool = entityManager
                        .GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
                    GameStateData currentGameState = entityManager
                        .GetComponentData<GameStateData>(gameStateEntity);

                    maxObservedAlive = Math.Max(maxObservedAlive, currentWave.ZombiesAlive);
                    capViolation |= currentWave.ZombiesAlive > ReleaseActiveCap;
                    backlogRegression |= currentBudget.PendingEnemies < previousBacklog;
                    demandAccountingMismatch |= currentBudget.TotalDemandedEnemies
                                                - currentBudget.TotalSpawnedEnemies
                                                != currentBudget.PendingEnemies;
                    enemyPoolMismatch |= currentEnemyPool.ActiveCount != currentWave.ZombiesAlive
                                         || currentEnemyPool.AvailableCount + currentEnemyPool.ActiveCount
                                         != currentEnemyPool.TotalCreated;
                    arrowPoolMismatch |= currentArrowPool.AvailableCount + currentArrowPool.ActiveCount
                                         != currentArrowPool.TotalCreated;
                    gameOverObserved |= currentGameState.IsGameOver;
                    backlogPeak = Math.Max(backlogPeak, currentBudget.PendingEnemies);
                    previousBacklog = currentBudget.PendingEnemies;

                    if ((frame + 1) % 60 == 0)
                    {
                        entityCountMismatch |= activeZombieQuery.CalculateEntityCount()
                                               != currentWave.ZombiesAlive;
                        entityCountMismatch |= archerQuery.CalculateEntityCount() != ArcherTarget;
                        arrowPoolMismatch |= projectileQuery.CalculateEntityCount()
                                             != currentArrowPool.ActiveCount;
                    }
                }
            }

            ContinuousSpawnBudgetData budgetAfterSoak = entityManager
                .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            EnemyPoolRuntimeData enemyPoolAfterSoak = entityManager
                .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
            ArrowPoolRuntimeData arrowPoolAfterSoak = entityManager
                .GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);

            Assert.That(capViolation, Is.False, "Long-run soak release active-cap degerini asti.");
            Assert.That(backlogRegression, Is.False,
                "Cap doluyken ve kill yokken backlog geriye gitti.");
            Assert.That(demandAccountingMismatch, Is.False,
                "TotalDemanded - TotalSpawned = Pending exact muhasebesi bozuldu.");
            Assert.That(enemyPoolMismatch, Is.False, "Enemy pool active/available muhasebesi drift etti.");
            Assert.That(arrowPoolMismatch, Is.False, "Arrow pool active/available muhasebesi drift etti.");
            Assert.That(entityCountMismatch, Is.False, "Runtime entity sayilari state owner'larindan koptu.");
            Assert.That(gameOverObserved, Is.False);
            Assert.That(maxObservedAlive, Is.EqualTo(ReleaseActiveCap));
            Assert.That(budgetAfterSoak.PendingEnemies,
                Is.GreaterThan(budgetBeforeSoak.PendingEnemies),
                "Cap doluyken gercek continuous demand backlog'a eklenmedi.");
            Assert.That(budgetAfterSoak.TotalSpawnedEnemies,
                Is.EqualTo(budgetBeforeSoak.TotalSpawnedEnemies),
                "Cap doluyken yeni enemy rent edilmemelidir.");
            Assert.That(enemyPoolAfterSoak.TotalRentCount,
                Is.EqualTo(enemyPoolBeforeSoak.TotalRentCount));
            Assert.That(enemyPoolAfterSoak.TotalCreated,
                Is.EqualTo(enemyPoolBeforeSoak.TotalCreated));
            Assert.That(enemyPoolAfterSoak.ExpansionCount,
                Is.EqualTo(enemyPoolBeforeSoak.ExpansionCount));
            Assert.That(arrowPoolAfterSoak.TotalRentCount,
                Is.GreaterThan(arrowPoolBeforeSoak.TotalRentCount));
            Assert.That(arrowPoolAfterSoak.TotalReturnCount,
                Is.GreaterThan(arrowPoolBeforeSoak.TotalReturnCount));
            Assert.That(arrowPoolAfterSoak.TotalCreated,
                Is.EqualTo(arrowPoolBeforeSoak.TotalCreated),
                "Projectile pool warmup sonrasi soak boyunca buyumemelidir.");
            Assert.That(arrowPoolAfterSoak.ExpansionCount,
                Is.EqualTo(arrowPoolBeforeSoak.ExpansionCount));

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.SpawnTimer = float.MaxValue;
            entityManager.SetComponentData(waveEntity, wave);
            long backlogBeforeDrain = budgetAfterSoak.PendingEnemies;
            int drainFrames = 0;
            int maxDrainPerFrame = 0;
            int returnedForDrain = 0;
            bool drainDemandObserved = false;
            bool drainBatchViolation = false;
            bool drainAccountingMismatch = false;
            bool drainPoolMismatch = false;

            while (entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity).PendingEnemies > 0L
                   && drainFrames < DrainTimeoutFrames)
            {
                wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
                int releaseCount = math.min(CapacityReleaseBatch, math.max(0, wave.ZombiesAlive));
                Assert.That(releaseCount, Is.GreaterThan(0), "Backlog varken acilacak active kapasite kalmadi.");

                int returnedThisRound = 0;
                using (NativeArray<Entity> active = activeZombieQuery.ToEntityArray(Allocator.Temp))
                {
                    int returnTarget = math.min(releaseCount, active.Length);
                    for (int i = 0; i < returnTarget; i++)
                    {
                        if (EnemyPoolRuntimeUtility.Return(entityManager, enemyPoolEntity, active[i]))
                            returnedThisRound++;
                    }
                }
                Assert.That(returnedThisRound, Is.EqualTo(releaseCount));
                returnedForDrain += returnedThisRound;
                wave.ZombiesAlive -= returnedThisRound;
                wave.SpawnTimer = float.MaxValue;
                entityManager.SetComponentData(waveEntity, wave);

                while (wave.ZombiesAlive < ReleaseActiveCap
                       && entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity).PendingEnemies > 0L
                       && drainFrames < DrainTimeoutFrames)
                {
                    ContinuousSpawnBudgetData beforeFrame = entityManager
                        .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
                    WaveStateData waveBeforeFrame = entityManager.GetComponentData<WaveStateData>(waveEntity);
                    long spawnedBeforeFrame = beforeFrame.TotalSpawnedEnemies;

                    yield return null;
                    drainFrames++;

                    ContinuousSpawnBudgetData afterFrame = entityManager
                        .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
                    wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
                    EnemyPoolRuntimeData poolDuringDrain = entityManager
                        .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
                    int spawnedThisFrame = (int)(afterFrame.TotalSpawnedEnemies - spawnedBeforeFrame);

                    maxDrainPerFrame = Math.Max(maxDrainPerFrame, spawnedThisFrame);
                    drainDemandObserved |= afterFrame.LastDemandedEnemies != 0;
                    drainBatchViolation |= spawnedThisFrame < 0
                                           || spawnedThisFrame > config.MaxSpawnBatch;
                    drainAccountingMismatch |= afterFrame.PendingEnemies
                                               != beforeFrame.PendingEnemies - spawnedThisFrame;
                    drainAccountingMismatch |= wave.ZombiesAlive
                                               != waveBeforeFrame.ZombiesAlive + spawnedThisFrame;
                    drainAccountingMismatch |= afterFrame.TotalDemandedEnemies
                                               - afterFrame.TotalSpawnedEnemies
                                               != afterFrame.PendingEnemies;
                    drainPoolMismatch |= poolDuringDrain.ActiveCount != wave.ZombiesAlive
                                         || poolDuringDrain.AvailableCount + poolDuringDrain.ActiveCount
                                         != poolDuringDrain.TotalCreated;
                }
            }

            ContinuousSpawnBudgetData finalBudget = entityManager
                .GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            WaveStateData finalWave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            EnemyPoolRuntimeData finalEnemyPool = entityManager
                .GetComponentData<EnemyPoolRuntimeData>(enemyPoolEntity);
            ArrowPoolRuntimeData finalArrowPool = entityManager
                .GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);

            Assert.That(drainFrames, Is.LessThan(DrainTimeoutFrames),
                "Backlog kontrollu capacity churn ile timeout icinde erimedi.");
            Assert.That(finalBudget.PendingEnemies, Is.Zero);
            Assert.That(finalBudget.TotalDemandedEnemies, Is.EqualTo(finalBudget.TotalSpawnedEnemies));
            Assert.That(drainDemandObserved, Is.False,
                "Drain fazinda SpawnTimer dondurulmusken yeni demand uretilmemelidir.");
            Assert.That(drainBatchViolation, Is.False,
                "Backlog drain tek frame MaxSpawnBatch limitini asti.");
            Assert.That(drainAccountingMismatch, Is.False,
                "Backlog drain exact muhasebe veya wave alive state'ini bozdu.");
            Assert.That(drainPoolMismatch, Is.False, "Backlog drain enemy pool muhasebesini bozdu.");
            Assert.That(maxDrainPerFrame, Is.EqualTo(config.MaxSpawnBatch));
            Assert.That(finalEnemyPool.TotalCreated, Is.EqualTo(enemyPoolBeforeSoak.TotalCreated));
            Assert.That(finalEnemyPool.ExpansionCount, Is.EqualTo(enemyPoolBeforeSoak.ExpansionCount));
            Assert.That(finalEnemyPool.ActiveCount, Is.EqualTo(finalWave.ZombiesAlive));
            Assert.That(finalEnemyPool.AvailableCount + finalEnemyPool.ActiveCount,
                Is.EqualTo(finalEnemyPool.TotalCreated));
            Assert.That(finalEnemyPool.TotalRentCount - finalEnemyPool.TotalReturnCount,
                Is.EqualTo(finalEnemyPool.ActiveCount));
            Assert.That(activeZombieQuery.CalculateEntityCount(), Is.EqualTo(finalWave.ZombiesAlive));
            Assert.That(archerQuery.CalculateEntityCount(), Is.EqualTo(ArcherTarget));
            Assert.That(finalArrowPool.AvailableCount + finalArrowPool.ActiveCount,
                Is.EqualTo(finalArrowPool.TotalCreated));
            Assert.That(projectileQuery.CalculateEntityCount(), Is.EqualTo(finalArrowPool.ActiveCount));

            Array.Sort(frameTimes);
            double averageFrameMs = frameTotalMs / SoakFrames;
            double p95FrameMs = frameTimes[(int)Math.Ceiling(SoakFrames * 0.95) - 1];
            double averageMainThreadMs = mainThreadTotalMs / SoakFrames;
            long averageGcBytes = gcTotalBytes / SoakFrames;
            long usedMemoryDeltaBytes = usedMemoryEndBytes - usedMemoryStartBytes;

            Debug.Log(
                $"[DW-V1-PERF-LONG-RUN-SOAK] cap={ReleaseActiveCap}; seed_backlog={SeedBacklog}; " +
                $"fill_frames={fillFrames + 1}; projectile_start_frames={projectileStartFrames + 1}; " +
                $"warmup_frames={ProjectileWarmupFrames}; soak_frames={SoakFrames}; " +
                $"backlog_before_soak={budgetBeforeSoak.PendingEnemies}; " +
                $"backlog_after_soak={budgetAfterSoak.PendingEnemies}; backlog_peak={backlogPeak}; " +
                $"backlog_before_drain={backlogBeforeDrain}; drain_frames={drainFrames}; " +
                $"returned_for_drain={returnedForDrain}; max_drain_per_frame={maxDrainPerFrame}; " +
                $"demanded={finalBudget.TotalDemandedEnemies}; spawned={finalBudget.TotalSpawnedEnemies}; " +
                $"final_alive={finalWave.ZombiesAlive}; enemy_pool_created={finalEnemyPool.TotalCreated}; " +
                $"enemy_pool_expansions={finalEnemyPool.ExpansionCount}; " +
                $"enemy_pool_rents={finalEnemyPool.TotalRentCount}; " +
                $"enemy_pool_returns={finalEnemyPool.TotalReturnCount}; " +
                $"arrow_pool_created={finalArrowPool.TotalCreated}; " +
                $"arrow_pool_expansions={finalArrowPool.ExpansionCount}; " +
                $"arrow_pool_rents={finalArrowPool.TotalRentCount}; " +
                $"arrow_pool_returns={finalArrowPool.TotalReturnCount}; " +
                $"frame_avg_ms={averageFrameMs:F3}; frame_p95_ms={p95FrameMs:F3}; " +
                $"frame_max_ms={frameMaxMs:F3}; main_avg_ms={averageMainThreadMs:F3}; " +
                $"main_max_ms={mainThreadMaxMs:F3}; gc_avg_bytes={averageGcBytes}; " +
                $"gc_max_bytes={gcMaxBytes}; used_memory_start={usedMemoryStartBytes}; " +
                $"used_memory_end={usedMemoryEndBytes}; used_memory_delta={usedMemoryDeltaBytes}; " +
                $"used_memory_peak={usedMemoryPeakBytes}");
        }
    }
}
