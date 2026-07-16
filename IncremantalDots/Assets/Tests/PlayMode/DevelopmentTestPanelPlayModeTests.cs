#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class DevelopmentTestPanelPlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath)
                ? File.ReadAllBytes(_runSavePath)
                : null;
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ClearDevelopmentHorde();
                GameManager.Instance.CompleteDevelopmentTestSession();
            }

            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DevelopmentControls_UnlockCombatAndSpawnExact2K5K10K()
        {
            GameManager gameManager = null;
            for (int frame = 0; frame < 300; frame++)
            {
                gameManager = GameManager.Instance;
                if (gameManager != null && gameManager.IsMobileMode)
                    break;
                yield return null;
            }

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(gameManager.IsMobileMode, Is.True);
            Assert.That(gameManager.TryEnableDevelopmentCombat(out string unlockMessage), Is.True,
                unlockMessage);
            Assert.That(gameManager.FireballUnlocked, Is.True);
            Assert.That(gameManager.FireballReady, Is.True);
            Assert.That(gameManager.IsArcherTypeUnlocked(ArcherType.Rapid), Is.True);
            Assert.That(gameManager.IsArcherTypeUnlocked(ArcherType.Frost), Is.True);
            Assert.That(gameManager.IsFreeEconomyTestMode, Is.True);
            Assert.That(gameManager.SaveRunSnapshot(), Is.False,
                "Development test state must never overwrite the exact run save.");

            World world = World.DefaultGameObjectInjectionWorld;
            EntityManager entityManager = world.EntityManager;
            using EntityQuery activeZombies = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });

            int[] counts =
            {
                DevelopmentTestRules.Horde2K,
                DevelopmentTestRules.Horde5K,
                DevelopmentTestRules.Horde10K
            };
            foreach (int count in counts)
            {
                Assert.That(gameManager.TrySpawnDevelopmentHorde(
                    count, out int spawned, out string spawnMessage), Is.True, spawnMessage);
                Assert.That(spawned, Is.EqualTo(count));
                Assert.That(activeZombies.CalculateEntityCount(), Is.EqualTo(count));

                Entity waveEntity = entityManager.CreateEntityQuery(
                    typeof(WaveStateData)).GetSingletonEntity();
                WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
                Assert.That(wave.ZombiesAlive, Is.EqualTo(count));
                Assert.That(wave.ZombiesSpawned, Is.EqualTo(count));
                Assert.That(wave.StressTestMode, Is.False,
                    "Manual presentation tests must keep combat VFX/SFX enabled.");
            }

            int cleared = gameManager.ClearDevelopmentHorde();
            Assert.That(cleared, Is.EqualTo(DevelopmentTestRules.Horde10K));
            Assert.That(activeZombies.CalculateEntityCount(), Is.Zero);
            Assert.That(gameManager.CompleteDevelopmentTestSession(), Is.True);
            Assert.That(gameManager.DevelopmentTestSessionActive, Is.False);
        }

        [UnityTest]
        public IEnumerator QueuedSideNeighbors_AfterFrontClears_ResumeMovementInsteadOfLeavingCavity()
        {
            GameManager gameManager = null;
            for (int frame = 0; frame < 300; frame++)
            {
                gameManager = GameManager.Instance;
                if (gameManager != null && gameManager.IsMobileMode)
                    break;
                yield return null;
            }

            Assert.That(gameManager, Is.Not.Null);
            Assert.That(gameManager.TryEnableDevelopmentCombat(out string unlockMessage), Is.True,
                unlockMessage);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity poolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData),
                typeof(EnemyPoolAvailable),
                typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(
                typeof(WaveStateData)).GetSingletonEntity();

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager, poolEntity, out Entity first), Is.True);
            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager, poolEntity, out Entity second), Is.True);

            MobileCastleCombatConfig config =
                entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            Assert.That(config.SingleFrontEnabled, Is.True,
                "Regression scenario active single-front Wall akisini gerektirir.");

            float startX = config.FrontlineX + config.AttackRadius + 4f;
            SetQueuedZombie(entityManager, first, new float3(startX, -0.10f, MobileCastleRenderDepth.UnitZ));
            SetQueuedZombie(entityManager, second, new float3(startX, 0.10f, MobileCastleRenderDepth.UnitZ));

            config.MaxAliveZombies = 2;
            config.StressMaxAliveZombies = 2;
            entityManager.SetComponentData(configEntity, config);

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 2;
            wave.ZombiesSpawned = 2;
            wave.ZombiesToSpawn = 2;
            wave.SpawnTimer = float.MaxValue;
            wave.WaveActive = true;
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);

            for (int frame = 0; frame < 30; frame++)
                yield return null;

            Assert.That(entityManager.GetComponentData<ZombieState>(first).Value,
                Is.Not.EqualTo(ZombieStateType.Queued));
            Assert.That(entityManager.GetComponentData<ZombieState>(first).Value,
                Is.Not.EqualTo(ZombieStateType.Dead));
            Assert.That(entityManager.GetComponentData<ZombieState>(second).Value,
                Is.Not.EqualTo(ZombieStateType.Queued));
            Assert.That(entityManager.GetComponentData<ZombieState>(second).Value,
                Is.Not.EqualTo(ZombieStateType.Dead));
            Assert.That(entityManager.GetComponentData<LocalTransform>(first).Position.x,
                Is.LessThan(startX - 0.01f));
            Assert.That(entityManager.GetComponentData<LocalTransform>(second).Position.x,
                Is.LessThan(startX - 0.01f));

            gameManager.ClearDevelopmentHorde();
            Assert.That(gameManager.CompleteDevelopmentTestSession(), Is.True);
        }

        private static void SetQueuedZombie(
            EntityManager entityManager,
            Entity zombie,
            float3 position)
        {
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(zombie);
            transform.Position = position;
            entityManager.SetComponentData(zombie, transform);
            entityManager.SetComponentData(
                zombie,
                new ZombieState { Value = ZombieStateType.Queued });

            ZombieStats stats = entityManager.GetComponentData<ZombieStats>(zombie);
            stats.MaxHP = 1_000_000_000f;
            stats.CurrentHP = stats.MaxHP;
            stats.AttackDamage = 0f;
            entityManager.SetComponentData(zombie, stats);

            PhysicsBody body = entityManager.GetComponentData<PhysicsBody>(zombie);
            body.Velocity = float2.zero;
            body.Force = float2.zero;
            entityManager.SetComponentData(zombie, body);
        }
    }
}
#endif
