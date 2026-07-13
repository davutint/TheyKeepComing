using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
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
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
            Profiler.enabled = false;

            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        [Explicit("Targeted 10K allocation capture only; normal regression setinde calismaz.")]
        public IEnumerator HordeScale_10K_SteadyStateProfilerCapture_ProducesLoadableRaw()
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

            for (int frame = 0; frame < WarmupFrames; frame++)
                yield return null;

            string logsDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
            Directory.CreateDirectory(logsDirectory);
            string captureBasePath = Path.Combine(logsDirectory,
                "DW_B_SCALE_OPT_STEADY_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));

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
            Assert.That(File.Exists(rawPath), Is.True, "Profiler RAW dosyasi olusmadi.");
            Assert.That(new FileInfo(rawPath).Length, Is.GreaterThan(0),
                "Profiler RAW dosyasi bos olustu.");

            Debug.Log($"[DW-B-SCALE-PROFILE] path={rawPath}; frames={CaptureFrames}; " +
                      $"enemy={EnemyTarget}; bytes={new FileInfo(rawPath).Length}");

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);
            activeQuery.Dispose();
        }
    }
}
