using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    /// <summary>
    /// Normal regresyon setine dahil olmayan, yalnizca hedefli calistirilan profiler capture'i.
    /// Binary log PlayMode domain reload'u tamamlandiktan sonra acilir ve test bitmeden kapanir.
    /// </summary>
    public class HordeScaleProfilerCapturePlayModeTests
    {
        private const int EnemyTarget = 10_000;
        private const int ArcherTarget = 1_000;
        private const int WarmupFrames = 30;
        private const int FramePacingWarmupFrames = 180;
        private const int FramePacingSampleFrames = 600;
        private const int CaptureFrames = 120;
        private const double SixtyFpsBudgetMs = 1000d / 60d;

        [Serializable]
        private sealed class TargetHardwareFramePacingReport
        {
            public string capturedUtc;
            public string unityVersion;
            public string platform;
            public string operatingSystem;
            public string processor;
            public string graphicsDevice;
            public int systemMemoryMb;
            public int graphicsMemoryMb;
            public string qualityLevel;
            public int width;
            public int height;
            public int vSyncCount;
            public int targetFrameRate;
            public int warmupFrames;
            public int sampleFrames;
            public int enemyCount;
            public int archerCount;
            public int finalProjectileCount;
            public int peakProjectileCount;
            public int projectileSamples;
            public int projectilePositiveSamples;
            public double frameBudgetMs;
            public double averageMs;
            public double p95Ms;
            public double p99Ms;
            public double maxMs;
            public int overBudgetFrames;
            public int overThirtyFpsBudgetFrames;
            public int longestOverBudgetStreak;
            public bool accepted;
        }

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
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
            Profiler.enabled = false;

            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        [Explicit("Targeted 10K + canonical 1K Player allocation capture only; normal regression setinde calismaz.")]
        public IEnumerator HordeScale_10K_1K_CombinedProfilerCapture_ProducesLoadableRaw()
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

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity poolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData), typeof(EnemyPoolAvailable),
                typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();

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

            MobileCastleCombatConfig config = entityManager
                .GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MaxAliveZombies = EnemyTarget;
            config.StressMaxAliveZombies = EnemyTarget;
            entityManager.SetComponentData(configEntity, config);

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 999f;
            entityManager.SetComponentData(waveEntity, wave);

            EnemyCatalogRuntimeData catalog = entityManager
                .GetComponentData<EnemyCatalogRuntimeData>(poolEntity);
            DynamicBuffer<EnemyCatalogEntryData> entries = entityManager
                .GetBuffer<EnemyCatalogEntryData>(poolEntity, true);
            int activeEntryIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(catalog, entries.Length);
            Assert.That(activeEntryIndex, Is.GreaterThanOrEqualTo(0));
            EnemyCatalogEntryData definition = entries[activeEntryIndex];

            for (int i = 0; i < EnemyTarget; i++)
            {
                Assert.That(EnemyPoolRuntimeUtility.TryRent(
                    entityManager, poolEntity, out Entity zombie), Is.True);
                int column = i % 100;
                int row = i / 100;
                float3 position = new float3(
                    10f + column * 0.12f,
                    -6.5f + row * 0.13f,
                    MobileCastleRenderDepth.UnitZ);
                entityManager.SetComponentData(zombie,
                    LocalTransform.FromPositionRotationScale(
                        position, quaternion.identity, definition.Scale));
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
                entityManager.SetComponentData(zombie,
                    new ZombieState { Value = ZombieStateType.Moving });
            }

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = EnemyTarget;
            entityManager.SetComponentData(waveEntity, wave);
            Assert.That(activeQuery.CalculateEntityCount(), Is.EqualTo(EnemyTarget));

            MethodInfo restoreArcherCounts = typeof(GameManager).GetMethod(
                "RestoreArcherCountsWithinCapacity",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(restoreArcherCounts, Is.Not.Null);
            restoreArcherCounts.Invoke(gameManager, new object[] { ArcherTarget, 0, 0 });

            using EntityQuery archerQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ArcherUnit>(),
                    ComponentType.ReadOnly<LocalTransform>()
                },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
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

            using (EntityQuery arrowSupplyQuery = entityManager.CreateEntityQuery(typeof(ArrowSupply)))
            {
                Entity arrowSupplyEntity = arrowSupplyQuery.GetSingletonEntity();
                ArrowSupply stressSupply = entityManager.GetComponentData<ArrowSupply>(arrowSupplyEntity);
                // Bu fixture 1K okcunun olcumunu normal run Arrow kapasitesiyle
                // sinirlamaz; ammo depletion testinin konusu degildir.
                stressSupply.CapacityLevel = int.MaxValue;
                stressSupply.Current = ArrowEconomyUtility.GetCapacity(
                    stressSupply, gameManager.GetEconomyPriceTuning());
                entityManager.SetComponentData(arrowSupplyEntity, stressSupply);
            }

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

            for (int frame = 0; frame < WarmupFrames; frame++)
                yield return null;

            Assert.That(projectileQuery.CalculateEntityCount(), Is.GreaterThan(0),
                "Canonical 1K archer warmup sirasinda aktif projectile uretmedi.");

            TargetHardwareFramePacingReport framePacing = null;
            yield return CaptureTargetHardwareFramePacing(
                activeQuery,
                archerQuery,
                projectileQuery,
                report => framePacing = report);
            Assert.That(framePacing, Is.Not.Null);
            Assert.That(framePacing.enemyCount, Is.EqualTo(EnemyTarget));
            Assert.That(framePacing.archerCount, Is.EqualTo(ArcherTarget));
            Assert.That(framePacing.peakProjectileCount, Is.GreaterThan(0));
            Assert.That(framePacing.projectilePositiveSamples, Is.GreaterThan(0),
                "Frame-pacing penceresinde canonical okcu atesi gozlenmedi.");
            Assert.That(framePacing.averageMs, Is.LessThanOrEqualTo(SixtyFpsBudgetMs),
                $"Target hardware average 60 FPS budgetini asti: {framePacing.averageMs:F3} ms.");
            Assert.That(framePacing.p95Ms, Is.LessThanOrEqualTo(SixtyFpsBudgetMs),
                $"Target hardware P95 60 FPS budgetini asti: {framePacing.p95Ms:F3} ms.");
            Assert.That(framePacing.p99Ms, Is.LessThanOrEqualTo(1000d / 30d),
                $"Target hardware P99 30 FPS pacing floor'unu asti: {framePacing.p99Ms:F3} ms.");

            string capturesDirectory = Path.Combine(
                Application.persistentDataPath, "DeadWallsProfilerCaptures");
            Directory.CreateDirectory(capturesDirectory);
            string captureBasePath = Path.Combine(capturesDirectory,
                "DW_V1_PLAYER_COMBINED_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));

            bool previousEnabled = Profiler.enabled;
            bool previousBinaryLog = Profiler.enableBinaryLog;
            bool previousCallstacks = Profiler.enableAllocationCallstacks;
            string previousLogFile = Profiler.logFile;
            string rawPath = captureBasePath + ".raw";

            try
            {
                Profiler.enabled = true;
                Profiler.logFile = captureBasePath;
                Profiler.enableAllocationCallstacks = true;
                Profiler.enableBinaryLog = true;
                rawPath = Profiler.logFile;

                for (int frame = 0; frame < CaptureFrames; frame++)
                    yield return null;
            }
            finally
            {
                Profiler.enableBinaryLog = false;
                Profiler.enableAllocationCallstacks = previousCallstacks;
                Profiler.enabled = previousEnabled;
                Profiler.logFile = previousLogFile;
                if (previousBinaryLog)
                    Profiler.enableBinaryLog = true;
            }

            yield return null;
            Assert.That(activeQuery.CalculateEntityCount(), Is.EqualTo(EnemyTarget),
                "Profiler capture sirasinda aktif enemy sayisi degisti.");
            Assert.That(archerQuery.CalculateEntityCount(), Is.EqualTo(ArcherTarget),
                "Profiler capture sirasinda canonical archer sayisi degisti.");
            Assert.That(File.Exists(rawPath), Is.True, "Profiler RAW dosyasi olusmadi.");
            Assert.That(new FileInfo(rawPath).Length, Is.GreaterThan(0),
                "Profiler RAW dosyasi bos olustu.");

            Debug.Log($"[DW-V1-PLAYER-PROFILE-CAPTURE] path={rawPath}; " +
                      $"platform={Application.platform}; frames={CaptureFrames}; " +
                      $"enemy={EnemyTarget}; archer={ArcherTarget}; " +
                      $"projectile={projectileQuery.CalculateEntityCount()}; " +
                      $"bytes={new FileInfo(rawPath).Length}");

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);
            activeQuery.Dispose();
        }

        private static IEnumerator CaptureTargetHardwareFramePacing(
            EntityQuery activeQuery,
            EntityQuery archerQuery,
            EntityQuery projectileQuery,
            Action<TargetHardwareFramePacingReport> completed)
        {
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
            Profiler.enabled = false;

            for (int frame = 0; frame < FramePacingWarmupFrames; frame++)
                yield return null;

            var frameMilliseconds = new double[FramePacingSampleFrames];
            int overBudgetFrames = 0;
            int overThirtyFpsBudgetFrames = 0;
            int currentOverBudgetStreak = 0;
            int longestOverBudgetStreak = 0;
            int projectileSamples = 0;
            int projectilePositiveSamples = 0;
            int peakProjectileCount = 0;
            double sum = 0d;
            double max = 0d;
            double thirtyFpsBudgetMs = 1000d / 30d;

            for (int frame = 0; frame < FramePacingSampleFrames; frame++)
            {
                yield return null;
                double milliseconds = Math.Max(0d, Time.unscaledDeltaTime * 1000d);
                frameMilliseconds[frame] = milliseconds;
                sum += milliseconds;
                max = Math.Max(max, milliseconds);

                // Her kare EntityQuery sayimi olcume gereksiz debug maliyeti ekler.
                // 30 karelik araliklarla pencerede gercek combat projectile'i uretildigini kanitla.
                if (frame % 30 == 0 || frame == FramePacingSampleFrames - 1)
                {
                    int activeProjectiles = projectileQuery.CalculateEntityCount();
                    projectileSamples++;
                    peakProjectileCount = Math.Max(peakProjectileCount, activeProjectiles);
                    if (activeProjectiles > 0)
                        projectilePositiveSamples++;
                }

                if (milliseconds > SixtyFpsBudgetMs)
                {
                    overBudgetFrames++;
                    currentOverBudgetStreak++;
                    longestOverBudgetStreak = Math.Max(
                        longestOverBudgetStreak,
                        currentOverBudgetStreak);
                }
                else
                {
                    currentOverBudgetStreak = 0;
                }

                if (milliseconds > thirtyFpsBudgetMs)
                    overThirtyFpsBudgetFrames++;
            }

            double[] sorted = (double[])frameMilliseconds.Clone();
            Array.Sort(sorted);
            double average = sum / FramePacingSampleFrames;
            double p95 = Percentile(sorted, 0.95d);
            double p99 = Percentile(sorted, 0.99d);
            int enemyCount = activeQuery.CalculateEntityCount();
            int archerCount = archerQuery.CalculateEntityCount();
            int finalProjectileCount = projectileQuery.CalculateEntityCount();
            bool accepted = enemyCount == EnemyTarget
                            && archerCount == ArcherTarget
                            && projectilePositiveSamples > 0
                            && average <= SixtyFpsBudgetMs
                            && p95 <= SixtyFpsBudgetMs
                            && p99 <= thirtyFpsBudgetMs;
            var report = new TargetHardwareFramePacingReport
            {
                capturedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                systemMemoryMb = SystemInfo.systemMemorySize,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                width = Screen.width,
                height = Screen.height,
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                warmupFrames = FramePacingWarmupFrames,
                sampleFrames = FramePacingSampleFrames,
                enemyCount = enemyCount,
                archerCount = archerCount,
                finalProjectileCount = finalProjectileCount,
                peakProjectileCount = peakProjectileCount,
                projectileSamples = projectileSamples,
                projectilePositiveSamples = projectilePositiveSamples,
                frameBudgetMs = SixtyFpsBudgetMs,
                averageMs = average,
                p95Ms = p95,
                p99Ms = p99,
                maxMs = max,
                overBudgetFrames = overBudgetFrames,
                overThirtyFpsBudgetFrames = overThirtyFpsBudgetFrames,
                longestOverBudgetStreak = longestOverBudgetStreak,
                accepted = accepted
            };

            string directory = Path.Combine(
                Application.persistentDataPath,
                "DeadWallsProfilerCaptures");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(
                directory,
                "DW_V1_TARGET_HARDWARE_FRAME_PACING_"
                + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")
                + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            Debug.Log(
                $"[DW-V1-TARGET-HARDWARE] accepted={accepted}; "
                + $"average={average:F3}ms; p95={p95:F3}ms; p99={p99:F3}ms; max={max:F3}ms; "
                + $"over16_67={overBudgetFrames}/{FramePacingSampleFrames}; "
                + $"projectile_samples={projectilePositiveSamples}/{projectileSamples}; "
                + $"longest_streak={longestOverBudgetStreak}; path={path}");
            completed?.Invoke(report);
        }

        private static double Percentile(double[] sortedValues, double percentile)
        {
            if (sortedValues == null || sortedValues.Length == 0)
                return 0d;
            int index = (int)Math.Ceiling(percentile * sortedValues.Length) - 1;
            index = Math.Max(0, Math.Min(sortedValues.Length - 1, index));
            return sortedValues[index];
        }
    }
}
