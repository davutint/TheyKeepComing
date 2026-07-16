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

        [UnityTest]
        public IEnumerator QueuedDiagonalArc_AfterFireballLaneClears_ResumesForwardFlow()
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
            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager, poolEntity, out Entity third), Is.True);

            MobileCastleCombatConfig config =
                entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            Assert.That(config.SingleFrontEnabled, Is.True,
                "Regression scenario active single-front Wall akisini gerektirir.");

            float startX = config.FrontlineX + config.AttackRadius + 4f;
            SetQueuedZombie(entityManager, first,
                new float3(startX, 0f, MobileCastleRenderDepth.UnitZ));
            SetQueuedZombie(entityManager, second,
                new float3(startX + 0.05f, 0.20f, MobileCastleRenderDepth.UnitZ));
            SetQueuedZombie(entityManager, third,
                new float3(startX + 0.10f, 0.40f, MobileCastleRenderDepth.UnitZ));

            config.MaxAliveZombies = 3;
            config.StressMaxAliveZombies = 3;
            entityManager.SetComponentData(configEntity, config);

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 3;
            wave.ZombiesSpawned = 3;
            wave.ZombiesToSpawn = 3;
            wave.SpawnTimer = float.MaxValue;
            wave.WaveActive = true;
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);

            for (int frame = 0; frame < 30; frame++)
                yield return null;

            Entity[] zombies = { first, second, third };
            float[] initialX = { startX, startX + 0.05f, startX + 0.10f };
            for (int i = 0; i < zombies.Length; i++)
            {
                Assert.That(entityManager.GetComponentData<ZombieState>(zombies[i]).Value,
                    Is.Not.EqualTo(ZombieStateType.Queued));
                Assert.That(entityManager.GetComponentData<ZombieState>(zombies[i]).Value,
                    Is.Not.EqualTo(ZombieStateType.Dead));
                Assert.That(entityManager.GetComponentData<LocalTransform>(zombies[i]).Position.x,
                    Is.LessThan(initialX[i] - 0.01f));
            }

            gameManager.ClearDevelopmentHorde();
            Assert.That(gameManager.CompleteDevelopmentTestSession(), Is.True);
        }

        [UnityTest]
        public IEnumerator Exact2KHorde_FireballGapRefillsUnderQueuedPressure()
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
            Assert.That(gameManager.TrySpawnDevelopmentHorde(
                DevelopmentTestRules.Horde2K,
                out int spawned,
                out string spawnMessage), Is.True, spawnMessage);
            Assert.That(spawned, Is.EqualTo(DevelopmentTestRules.Horde2K));

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.CompleteAllTrackedJobs();
            using (EntityQuery statsQuery = entityManager.CreateEntityQuery(
                       typeof(ZombieTag),
                       typeof(ZombieStats)))
            using (var zombies = statsQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < zombies.Length; i++)
                {
                    ZombieStats stats = entityManager.GetComponentData<ZombieStats>(zombies[i]);
                    stats.MaxHP = 1_000_000_000f;
                    stats.CurrentHP = stats.MaxHP;
                    stats.AttackDamage = 0f;
                    entityManager.SetComponentData(zombies[i], stats);
                }
            }

            yield return new WaitForSecondsRealtime(15f);

            entityManager.CompleteAllTrackedJobs();
            float2 strikeCenter = float2.zero;
            int queuedCount = 0;
            using (EntityQuery centerQuery = entityManager.CreateEntityQuery(
                       typeof(ZombieTag),
                       typeof(ZombieState),
                       typeof(LocalTransform)))
            using (var states = centerQuery.ToComponentDataArray<ZombieState>(
                       Unity.Collections.Allocator.Temp))
            using (var transforms = centerQuery.ToComponentDataArray<LocalTransform>(
                       Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i].Value != ZombieStateType.Queued)
                        continue;

                    strikeCenter += transforms[i].Position.xy;
                    queuedCount++;
                }
            }

            Assert.That(queuedCount, Is.GreaterThan(0));
            strikeCenter /= queuedCount;
            float strikeRadius = gameManager.FireballRadius;
            Entity strike = entityManager.CreateEntity(typeof(FireballStrike));
            entityManager.SetComponentData(strike, new FireballStrike
            {
                Position = strikeCenter,
                Radius = strikeRadius,
                Damage = 10_000_000_000f
            });

            yield return new WaitForSecondsRealtime(4.5f);

            entityManager.CompleteAllTrackedJobs();
            int activeAfter;
            int livingInside = 0;
            float queuedSpeedSum = 0f;
            int queuedAfter = 0;
            using (EntityQuery resultQuery = entityManager.CreateEntityQuery(
                       typeof(ZombieTag),
                       typeof(ZombieState),
                       typeof(LocalTransform),
                       typeof(PhysicsBody)))
            using (var states = resultQuery.ToComponentDataArray<ZombieState>(
                       Unity.Collections.Allocator.Temp))
            using (var transforms = resultQuery.ToComponentDataArray<LocalTransform>(
                       Unity.Collections.Allocator.Temp))
            using (var bodies = resultQuery.ToComponentDataArray<PhysicsBody>(
                       Unity.Collections.Allocator.Temp))
            {
                activeAfter = states.Length;
                float radiusSq = strikeRadius * strikeRadius;
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i].Value == ZombieStateType.Dead)
                        continue;

                    if (math.distancesq(transforms[i].Position.xy, strikeCenter) <= radiusSq)
                        livingInside++;

                    if (states[i].Value == ZombieStateType.Queued)
                    {
                        queuedSpeedSum += math.length(bodies[i].Velocity);
                        queuedAfter++;
                    }
                }
            }

            int killed = DevelopmentTestRules.Horde2K - activeAfter;
            float queuedAverageSpeed = queuedSpeedSum / math.max(1, queuedAfter);
            gameManager.ClearDevelopmentHorde();
            Assert.That(gameManager.CompleteDevelopmentTestSession(), Is.True);

            Assert.That(killed, Is.GreaterThan(100));
            Assert.That(livingInside, Is.GreaterThanOrEqualTo(math.ceil(killed * 0.75f)));
            Assert.That(queuedAverageSpeed, Is.GreaterThan(0.05f));
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
