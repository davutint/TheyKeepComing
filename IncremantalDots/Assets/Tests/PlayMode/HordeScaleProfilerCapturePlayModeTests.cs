using System;
using System.Collections;
using System.IO;
using System.Reflection;
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
        private const int CaptureFrames = 120;

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
                stressSupply.CapacityLevel = 50;
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
            Assert.That(projectileQuery.CalculateEntityCount(), Is.GreaterThan(0),
                "Profiler capture sonunda aktif projectile kalmadi.");
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
    }
}
