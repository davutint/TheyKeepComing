using System;
using System.Collections;
using System.IO;
using System.Text;
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
    public class HordeScalePlayModeTests
    {
        private const int EnemyTarget = 10_000;
        private const int ArcherTarget = 1_000;
        private const int WarmupFrames = 30;
        private const int SampleFrames = 120;

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
        public IEnumerator HordeScale_10K_WithHudFeedbackPoolFireballAndContinue_ProducesTelemetry()
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

            // Domain reload kapali tam PlayMode turunda onceki terminal-state testi ayni
            // process state'ini birakabilir. Stress harness aktif, calisan run onkosulunu
            // acikca kurar; aksi halde FireballStrikeSystem Game Over'da bilerek erken cikar.
            Time.timeScale = 1f;
            gameManager.enabled = true;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(GameStateData)).GetSingletonEntity();
            var gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            entityManager.SetComponentData(gameStateEntity, gameState);

            Entity poolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData), typeof(EnemyPoolAvailable),
                typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();

            Assert.That(UnityEngine.Object.FindObjectsByType<HUDController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThan(0));
            Assert.That(UnityEngine.Object.FindObjectsByType<CombatFeedbackBridge>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThan(0));

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            var nonPoolZombieQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                None = new[]
                {
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<EnemyPoolMember>()
                }
            });
            entityManager.DestroyEntity(nonPoolZombieQuery);
            nonPoolZombieQuery.Dispose();

            var activeQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            Assert.That(activeQuery.CalculateEntityCount(), Is.Zero);

            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MaxAliveZombies = EnemyTarget;
            config.StressMaxAliveZombies = EnemyTarget;
            entityManager.SetComponentData(configEntity, config);

            var budget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            budget.PendingEnemies = 777;
            budget.TotalDemandedEnemies = 10_777;
            budget.TotalSpawnedEnemies = 10_000;
            entityManager.SetComponentData(configEntity, budget);

            var wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 999f;
            entityManager.SetComponentData(waveEntity, wave);

            var catalog = entityManager.GetComponentData<EnemyCatalogRuntimeData>(poolEntity);
            var entries = entityManager.GetBuffer<EnemyCatalogEntryData>(poolEntity, true);
            int activeEntryIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(catalog, entries.Length);
            Assert.That(activeEntryIndex, Is.GreaterThanOrEqualTo(0));
            EnemyCatalogEntryData definition = entries[activeEntryIndex];

            double activationStarted = Time.realtimeSinceStartupAsDouble;
            Entity sampleZombie = Entity.Null;
            for (int i = 0; i < EnemyTarget; i++)
            {
                Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, poolEntity, out Entity zombie), Is.True);
                if (sampleZombie == Entity.Null)
                    sampleZombie = zombie;
                int column = i % 100;
                int row = i / 100;
                float3 position = new float3(
                    10f + column * 0.12f,
                    -6.5f + row * 0.13f,
                    MobileCastleRenderDepth.UnitZ);
                entityManager.SetComponentData(zombie,
                    LocalTransform.FromPositionRotationScale(position, quaternion.identity, definition.Scale));
                entityManager.SetComponentData(zombie, new ZombieStats
                {
                    MoveSpeed = definition.BaseMoveSpeed,
                    MaxHP = 1_000_000f,
                    CurrentHP = 1_000_000f,
                    AttackDamage = definition.BaseDamage,
                    AttackCooldown = 1f,
                    AttackTimer = 0f,
                    XPReward = definition.XPReward
                });
                entityManager.SetComponentData(zombie, new ZombieState { Value = ZombieStateType.Moving });
            }
            double activationMs = (Time.realtimeSinceStartupAsDouble - activationStarted) * 1000.0;

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = EnemyTarget;
            entityManager.SetComponentData(waveEntity, wave);

            var poolAtTenK = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            Assert.That(activeQuery.CalculateEntityCount(), Is.EqualTo(EnemyTarget));
            Assert.That(poolAtTenK.ActiveCount, Is.EqualTo(EnemyTarget));
            Assert.That(poolAtTenK.TotalCreated, Is.GreaterThanOrEqualTo(EnemyTarget));
            Assert.That(poolAtTenK.AvailableCount + poolAtTenK.ActiveCount,
                Is.EqualTo(poolAtTenK.TotalCreated));

            using (EntityQuery animationQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
                   {
                       All = new[]
                       {
                           ComponentType.ReadOnly<ZombieTag>(),
                           ComponentType.ReadOnly<SpriteAnimation>()
                       },
                       None = new[] { ComponentType.ReadOnly<Prefab>() }
                   }))
            using (NativeArray<SpriteAnimation> animations =
                   animationQuery.ToComponentDataArray<SpriteAnimation>(Allocator.Temp))
            {
                var occupiedFrames = new bool[15];
                var occupiedTimerSlices = new bool[HordeMotionCadenceUtility.TimerSlices];
                for (int i = 0; i < animations.Length; i++)
                {
                    SpriteAnimation animation = animations[i];
                    occupiedFrames[math.clamp(animation.CurrentFrame, 0, occupiedFrames.Length - 1)] = true;
                    int timerSlice = math.clamp(
                        (int)math.floor((animation.FrameTimer /
                            math.max(0.0001f, animation.FrameInterval)) *
                            HordeMotionCadenceUtility.TimerSlices),
                        0,
                        occupiedTimerSlices.Length - 1);
                    occupiedTimerSlices[timerSlice] = true;
                }

                int frameBands = 0;
                for (int i = 0; i < occupiedFrames.Length; i++)
                    frameBands += occupiedFrames[i] ? 1 : 0;
                int timerBands = 0;
                for (int i = 0; i < occupiedTimerSlices.Length; i++)
                    timerBands += occupiedTimerSlices[i] ? 1 : 0;

                Assert.That(frameBands, Is.EqualTo(15),
                    "10K horde authored frame bantlarinin tamamini kullanmali.");
                Assert.That(timerBands, Is.EqualTo(HordeMotionCadenceUtility.TimerSlices),
                    "10K horde timer cadence bantlarinin tamamini kullanmali.");
                Debug.Log($"[DW-I-HORDE-READ] frame_bands={frameBands}; timer_bands={timerBands}");
            }

            using (EntityQuery existingArcherQuery =
                   entityManager.CreateEntityQuery(typeof(ArcherUnit)))
                entityManager.DestroyEntity(existingArcherQuery);

            Entity archerPrefab = entityManager.GetComponentData<ArcherPrefabData>(
                entityManager.CreateEntityQuery(typeof(ArcherPrefabData))
                    .GetSingletonEntity()).ArcherPrefab;
            MobileCastleArcherTilePlacement placement =
                UnityEngine.Object.FindFirstObjectByType<MobileCastleArcherTilePlacement>();
            Assert.That(placement, Is.Not.Null);
            Assert.That(placement.FormationCapacity, Is.EqualTo(ArcherTarget));

            using (NativeArray<Entity> stressArchers =
                   entityManager.Instantiate(archerPrefab, ArcherTarget, Allocator.Temp))
            {
                for (int i = 0; i < stressArchers.Length; i++)
                {
                    Assert.That(placement.TryGetSpawnPosition(i, out float3 archerPosition), Is.True);
                    entityManager.SetComponentData(stressArchers[i], new ArcherUnit
                    {
                        FireRate = 1.5f,
                        FireTimer = 0f,
                        ArrowDamage = 10f,
                        Range = 15f,
                        Type = ArcherType.Basic,
                        SlowDuration = 0f,
                        SlowMultiplier = 1f,
                        FacingDirection = new float2(1f, 0f),
                        AttackAnimTimer = 0f
                    });
                    entityManager.SetComponentData(
                        stressArchers[i],
                        LocalTransform.FromPositionRotationScale(
                            archerPosition, quaternion.identity, 1f));
                }
            }

            using EntityQuery archerQuery = entityManager.CreateEntityQuery(typeof(ArcherUnit));
            using EntityQuery projectileQuery = entityManager.CreateEntityQuery(
                typeof(ArrowTag), typeof(ArrowProjectile), typeof(LocalTransform));
            Entity arrowPrefab = entityManager.GetComponentData<ArrowPrefabData>(
                entityManager.CreateEntityQuery(typeof(ArrowPrefabData)).GetSingletonEntity()).ArrowPrefab;
            Entity arrowPoolEntity = entityManager.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable)).GetSingletonEntity();
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);
            ArrowPoolRuntimeData arrowPoolBeforeSalvo =
                entityManager.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(archerQuery.CalculateEntityCount(), Is.EqualTo(ArcherTarget));

            using (EntityQuery arrowSupplyQuery = entityManager.CreateEntityQuery(typeof(ArrowSupply)))
            {
                Entity arrowSupplyEntity = arrowSupplyQuery.GetSingletonEntity();
                ArrowSupply stressSupply = entityManager.GetComponentData<ArrowSupply>(arrowSupplyEntity);
                stressSupply.CapacityLevel = 50;
                stressSupply.Current = ArrowEconomyUtility.GetCapacity(
                    stressSupply, gameManager.GetEconomyPriceTuning());
                entityManager.SetComponentData(arrowSupplyEntity, stressSupply);
            }

            Time.timeScale = 0f;
            int firstSalvoProjectileCount = 0;
            int firstSalvoVisibleCount = 0;
            for (int frame = 0; frame < 8; frame++)
            {
                yield return null;
                int activeProjectileCount = projectileQuery.CalculateEntityCount();
                if (activeProjectileCount > firstSalvoProjectileCount)
                {
                    firstSalvoProjectileCount = activeProjectileCount;
                    firstSalvoVisibleCount = CountVisibleProjectiles(projectileQuery);
                }

                if (firstSalvoProjectileCount == ArcherTarget)
                    break;
            }

            ArrowPoolRuntimeData arrowPoolAfterSalvo =
                entityManager.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            int salvoStride = ArcherSalvoPresentationUtility.GetSamplingStride(ArcherTarget);
            int minimumVisibleCount = firstSalvoProjectileCount / salvoStride;
            int maximumVisibleCount = ArcherSalvoPresentationUtility.GetMaximumRepresentativeCount(
                ArcherTarget,
                firstSalvoProjectileCount);
            Assert.That(arrowPoolAfterSalvo.TotalRentCount - arrowPoolBeforeSalvo.TotalRentCount,
                Is.EqualTo(ArcherTarget),
                "1K okcunun gameplay projectile salvosu eksiksiz rent edilmelidir.");
            Assert.That(firstSalvoProjectileCount, Is.EqualTo(ArcherTarget),
                "TimeScale 0 ilk salvoda 1K gameplay projectile aktif kalmalidir.");
            Assert.That(firstSalvoVisibleCount,
                Is.InRange(minimumVisibleCount, maximumVisibleCount),
                "1K gameplay projectile yalniz bounded temsilci oklarla gorunmelidir.");
            Assert.That(firstSalvoVisibleCount,
                Is.LessThanOrEqualTo(ArcherSalvoPresentationUtility.MaxVisibleProjectilesPerSalvo));

            Time.timeScale = 1f;
            for (int frame = 0; frame < 8; frame++)
                yield return null;
            Time.timeScale = 0f;

            string salvoCapturePath = Path.Combine(
                Application.temporaryCachePath,
                "DW_I_SALVO_RHYTHM_10K.png");
            if (File.Exists(salvoCapturePath))
                File.Delete(salvoCapturePath);
            ScreenCapture.CaptureScreenshot(salvoCapturePath);
            yield return new WaitForEndOfFrame();
            for (int frame = 0; frame < 30 && !File.Exists(salvoCapturePath); frame++)
                yield return null;
            Assert.That(File.Exists(salvoCapturePath), Is.True);
            Assert.That(new FileInfo(salvoCapturePath).Length, Is.GreaterThan(1024));
            Debug.Log(
                $"[DW-I-SALVO-RHYTHM] gameplay_projectiles={firstSalvoProjectileCount}; " +
                $"visual_representatives={firstSalvoVisibleCount}; stride={salvoStride}; " +
                $"capture={salvoCapturePath}");
            Time.timeScale = 1f;

            for (int frame = 0; frame < WarmupFrames; frame++)
                yield return null;

            int activeChunkCount = activeQuery.CalculateChunkCount();
            double averageActiveEntitiesPerChunk = activeChunkCount > 0
                ? (double)EnemyTarget / activeChunkCount
                : 0.0;
            Debug.Log(BuildArchetypeTelemetry(entityManager, sampleZombie, activeChunkCount));

            var frameTimes = new double[SampleFrames];
            double frameTotalMs = 0.0;
            double frameMaxMs = 0.0;
            double mainThreadTotalMs = 0.0;
            double mainThreadMaxMs = 0.0;
            long gcTotalBytes = 0;
            long gcMaxBytes = 0;
            long drawCallTotal = 0;
            long usedMemoryBytes = 0;

            using (var mainThread = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal, "Main Thread", 1))
            using (var gcAllocated = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            using (var drawCalls = ProfilerRecorder.StartNew(
                       ProfilerCategory.Render, "Draw Calls Count", 1))
            using (var usedMemory = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory, "Total Used Memory", 1))
            {
                for (int frame = 0; frame < SampleFrames; frame++)
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
                    if (drawCalls.Valid)
                        drawCallTotal += drawCalls.LastValue;
                    if (usedMemory.Valid)
                        usedMemoryBytes = Math.Max(usedMemoryBytes, usedMemory.LastValue);
                }
            }

            Assert.That(activeQuery.CalculateEntityCount(), Is.EqualTo(EnemyTarget),
                "Steady-state sample sirasinda aktif enemy sayisi degisti.");
            Assert.That(archerQuery.CalculateEntityCount(), Is.EqualTo(ArcherTarget),
                "Steady-state sample sirasinda archer sayisi degisti.");
            int projectileCountAfterSample = projectileQuery.CalculateEntityCount();
            Assert.That(projectileCountAfterSample, Is.GreaterThan(0),
                "1K archer hedefleme turu projectile uretmedi.");
            Assert.That(ArrowPoolRuntimeUtility.Maintain(
                entityManager, arrowPoolEntity, arrowPrefab), Is.True);
            ArrowPoolRuntimeData arrowPoolAfterSample =
                entityManager.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(arrowPoolAfterSample.TotalCreated,
                Is.GreaterThanOrEqualTo(arrowPoolAfterSample.PrewarmTarget));
            Assert.That(arrowPoolAfterSample.TotalRentCount, Is.GreaterThan(0));
            Assert.That(arrowPoolAfterSample.TotalReturnCount, Is.GreaterThan(0));
            Assert.That(arrowPoolAfterSample.ActiveCount, Is.EqualTo(projectileCountAfterSample));
            Array.Sort(frameTimes);
            double averageFrameMs = frameTotalMs / SampleFrames;
            double p95FrameMs = frameTimes[(int)Math.Ceiling(SampleFrames * 0.95) - 1];
            double averageMainThreadMs = mainThreadTotalMs / SampleFrames;
            long averageGcBytes = gcTotalBytes / SampleFrames;
            long averageDrawCalls = drawCallTotal / SampleFrames;

            double saveStarted = Time.realtimeSinceStartupAsDouble;
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            double saveMs = (Time.realtimeSinceStartupAsDouble - saveStarted) * 1000.0;
            long saveBytes = new FileInfo(_runSavePath).Length;
            RunSaveState compactCombatSave = RunPersistence.TryLoad();
            Assert.That(compactCombatSave, Is.Not.Null);
            Assert.That(compactCombatSave.HasCombatRebuild, Is.True);
            Assert.That(compactCombatSave.CombatRebuild, Is.Not.Null);
            Assert.That(compactCombatSave.CombatRebuild.TotalZombies, Is.EqualTo(EnemyTarget));
            Assert.That(compactCombatSave.ActiveZombies, Is.Empty,
                "v14 10K zombie pozisyonlarini entity basina legacy listede yazmamali.");
            Assert.That(compactCombatSave.CombatRebuild.Buckets.Count, Is.LessThan(EnemyTarget));
            Assert.That(saveBytes, Is.LessThan(2L * 1024L * 1024L),
                "10K horde + aktif projectile snapshot'i 2 MiB compact kabul budget'ini asmamali.");

            long returnsBeforeFireball = entityManager
                .GetComponentData<EnemyPoolRuntimeData>(poolEntity).TotalReturnCount;
            Entity strike = entityManager.CreateEntity(typeof(FireballStrike));
            entityManager.SetComponentData(strike, new FireballStrike
            {
                Position = new float2(16f, 0f),
                Radius = 1000f,
                Damage = 2_000_000f
            });

            double deathPeakFrameMs = 0.0;
            int deathPeakFrameIndex = -1;
            int deathPeakActiveCount = EnemyTarget;
            int deathFrames = 0;
            using (var mainThreadDeath = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal, "Main Thread", 1))
            {
                for (; deathFrames < 900; deathFrames++)
                {
                    yield return null;
                    double frameMs = Time.unscaledDeltaTime * 1000.0;
                    if (mainThreadDeath.Valid)
                        frameMs = Math.Max(frameMs, mainThreadDeath.LastValue / 1_000_000.0);
                    int activeCount = entityManager
                        .GetComponentData<EnemyPoolRuntimeData>(poolEntity).ActiveCount;
                    if (frameMs > deathPeakFrameMs)
                    {
                        deathPeakFrameMs = frameMs;
                        deathPeakFrameIndex = deathFrames;
                        deathPeakActiveCount = activeCount;
                    }
                    if (activeCount == 0)
                        break;
                }
            }

            var poolAfterFireball = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            var gameStateAfterFireball = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            using EntityQuery pendingStrikeQuery = entityManager.CreateEntityQuery(
                typeof(FireballStrike));
            int pendingStrikeCount = pendingStrikeQuery.CalculateEntityCount();
            float sampleHpAfterFireball = entityManager.Exists(sampleZombie)
                ? entityManager.GetComponentData<ZombieStats>(sampleZombie).CurrentHP
                : float.NaN;
            ZombieStateType sampleStateAfterFireball = entityManager.Exists(sampleZombie)
                ? entityManager.GetComponentData<ZombieState>(sampleZombie).Value
                : ZombieStateType.Dead;
            bool sampleDeathTimerEnabled = entityManager.Exists(sampleZombie)
                && entityManager.IsComponentEnabled<DeathTimer>(sampleZombie);
            float sampleDeathTimer = entityManager.Exists(sampleZombie)
                ? entityManager.GetComponentData<DeathTimer>(sampleZombie).Value
                : float.NaN;
            Assert.That(poolAfterFireball.ActiveCount, Is.Zero,
                $"Fireball toplu olumleri 900 frame icinde pool'a donmedi. " +
                $"pendingStrike={pendingStrikeCount}; sampleHp={sampleHpAfterFireball}; " +
                $"sampleState={sampleStateAfterFireball}; deathTimer={sampleDeathTimer}; " +
                $"deathTimerEnabled={sampleDeathTimerEnabled}; " +
                $"gameOver={gameStateAfterFireball.IsGameOver}; " +
                $"levelPending={gameStateAfterFireball.IsLevelUpPending}; timeScale={Time.timeScale}");
            Assert.That(poolAfterFireball.TotalReturnCount - returnsBeforeFireball,
                Is.EqualTo(EnemyTarget));
            Assert.That(poolAfterFireball.TotalCreated, Is.EqualTo(poolAtTenK.TotalCreated),
                "Death return sirasinda pool gereksiz genisledi.");

            double restoreStarted = Time.realtimeSinceStartupAsDouble;
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            double restoreMs = (Time.realtimeSinceStartupAsDouble - restoreStarted) * 1000.0;

            var restoredPool = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            var restoredBudget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            Assert.That(activeQuery.CalculateEntityCount(), Is.EqualTo(EnemyTarget));
            Assert.That(restoredPool.ActiveCount, Is.EqualTo(EnemyTarget));
            Assert.That(restoredPool.TotalCreated, Is.EqualTo(poolAtTenK.TotalCreated));
            Assert.That(restoredBudget.PendingEnemies, Is.EqualTo(777));
            ulong firstRestoreFingerprint = BuildActivePositionFingerprint(entityManager);

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            ulong secondRestoreFingerprint = BuildActivePositionFingerprint(entityManager);
            Assert.That(secondRestoreFingerprint, Is.EqualTo(firstRestoreFingerprint),
                "Ayni v14 snapshot iki Continue'da ayni rebuilt position multiset'ini uretmeli.");

            Debug.Log(
                "[DW-B-SCALE] " +
                $"enemy={EnemyTarget}; archer={ArcherTarget}; " +
                $"projectile_after_sample={projectileCountAfterSample}; " +
                $"arrow_pool_total={arrowPoolAfterSample.TotalCreated}; " +
                $"arrow_pool_available={arrowPoolAfterSample.AvailableCount}; " +
                $"arrow_pool_active={arrowPoolAfterSample.ActiveCount}; " +
                $"arrow_pool_expansions={arrowPoolAfterSample.ExpansionCount}; " +
                $"arrow_pool_rents={arrowPoolAfterSample.TotalRentCount}; " +
                $"arrow_pool_returns={arrowPoolAfterSample.TotalReturnCount}; " +
                $"pool_total={poolAtTenK.TotalCreated}; " +
                $"pool_available={poolAtTenK.AvailableCount}; expansions={poolAtTenK.ExpansionCount}; " +
                $"active_chunks={activeChunkCount}; entities_per_chunk={averageActiveEntitiesPerChunk:F2}; " +
                $"activation_ms={activationMs:F2}; frame_avg_ms={averageFrameMs:F2}; " +
                $"frame_p95_ms={p95FrameMs:F2}; frame_max_ms={frameMaxMs:F2}; " +
                $"main_avg_ms={averageMainThreadMs:F2}; main_max_ms={mainThreadMaxMs:F2}; " +
                $"gc_avg_bytes={averageGcBytes}; gc_max_bytes={gcMaxBytes}; " +
                $"draw_calls_avg={averageDrawCalls}; used_memory_bytes={usedMemoryBytes}; " +
                $"save_ms={saveMs:F2}; save_bytes={saveBytes}; " +
                $"rebuild_policy={compactCombatSave.CombatRebuild.PolicyVersion}; " +
                $"rebuild_buckets={compactCombatSave.CombatRebuild.Buckets.Count}; " +
                $"rebuild_deterministic={firstRestoreFingerprint == secondRestoreFingerprint}; " +
                $"fireball_return_frames={deathFrames + 1}; death_peak_ms={deathPeakFrameMs:F2}; " +
                $"death_peak_frame={deathPeakFrameIndex}; death_peak_active={deathPeakActiveCount}; " +
                $"restore_ms={restoreMs:F2}; backlog={restoredBudget.PendingEnemies}");

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);
            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);
            activeQuery.Dispose();
        }

        private static int CountVisibleProjectiles(EntityQuery projectileQuery)
        {
            using NativeArray<LocalTransform> transforms =
                projectileQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            int visibleCount = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].Scale > 0.0001f)
                    visibleCount++;
            }

            return visibleCount;
        }

        private static ulong BuildActivePositionFingerprint(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ZombieTag>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<LocalTransform> transforms =
                query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var packedPositions = new ulong[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                int x = Mathf.RoundToInt(transforms[i].Position.x * 10_000f);
                int y = Mathf.RoundToInt(transforms[i].Position.y * 10_000f);
                packedPositions[i] = ((ulong)unchecked((uint)x) << 32)
                                     | unchecked((uint)y);
            }

            Array.Sort(packedPositions);
            ulong fingerprint = 1469598103934665603UL;
            for (int i = 0; i < packedPositions.Length; i++)
            {
                fingerprint ^= packedPositions[i];
                fingerprint *= 1099511628211UL;
            }
            return fingerprint;
        }

        private static string BuildArchetypeTelemetry(
            EntityManager entityManager, Entity sampleEntity, int activeChunkCount)
        {
            ArchetypeChunk chunk = entityManager.GetChunk(sampleEntity);
            using var componentTypes = entityManager.GetComponentTypes(sampleEntity, Allocator.Temp);
            var builder = new StringBuilder(1024);
            builder.Append("[DW-B-SCALE-ARCHETYPE] active_chunks=")
                .Append(activeChunkCount)
                .Append("; sample_chunk_count=").Append(chunk.Count)
                .Append("; chunk_capacity=").Append(chunk.Capacity)
                .Append("; components=");

            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');

                ComponentType componentType = componentTypes[i];
                TypeManager.TypeInfo typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
                builder.Append(typeInfo.DebugTypeName)
                    .Append(':')
                    .Append(typeInfo.SizeInChunk);
            }

            return builder.ToString();
        }
    }
}
