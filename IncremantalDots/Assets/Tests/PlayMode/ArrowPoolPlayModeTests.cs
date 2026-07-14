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
    public class ArrowPoolPlayModeTests
    {
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
            // Onceki test scene'inin static GameManager.Instance referansi frame sonunda temizlenir.
            yield return null;
            bool runtimeReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance != null && GameManager.Instance.SaveRunSnapshot())
                {
                    runtimeReady = true;
                    break;
                }
                yield return null;
            }

            Assert.That(runtimeReady, Is.True,
                "NewGameScene GameManager/SubScene 300 frame icinde hazir olmadi.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpiredArrow_ReturnsToPoolAndSameEntityCanBeRentedAgain()
        {
            Time.timeScale = 1f;
            EntityManager entityManager = default;
            Entity arrowPrefab = Entity.Null;
            Entity arrowPool = Entity.Null;
            Entity enemyPool = Entity.Null;
            bool ecsReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                {
                    entityManager = world.EntityManager;
                    using EntityQuery arrowPrefabQuery =
                        entityManager.CreateEntityQuery(typeof(ArrowPrefabData));
                    using EntityQuery arrowPoolQuery = entityManager.CreateEntityQuery(
                        typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable));
                    using EntityQuery enemyPoolQuery = entityManager.CreateEntityQuery(
                        typeof(EnemyPoolRuntimeData), typeof(EnemyPoolAvailable));
                    if (arrowPrefabQuery.CalculateEntityCount() == 1
                        && arrowPoolQuery.CalculateEntityCount() == 1
                        && enemyPoolQuery.CalculateEntityCount() == 1)
                    {
                        arrowPrefab = entityManager.GetComponentData<ArrowPrefabData>(
                            arrowPrefabQuery.GetSingletonEntity()).ArrowPrefab;
                        arrowPool = arrowPoolQuery.GetSingletonEntity();
                        enemyPool = enemyPoolQuery.GetSingletonEntity();
                        ecsReady = true;
                        break;
                    }
                }

                yield return null;
            }

            Assert.That(ecsReady, Is.True,
                "Arrow/enemy pool singleton'lari 300 frame icinde hazir olmadi.");

            using (EntityQuery archerQuery = entityManager.CreateEntityQuery(typeof(ArcherUnit)))
                entityManager.DestroyEntity(archerQuery);
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPool);
            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, enemyPool);

            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            wave.SpawnTimer = 999f;
            wave.ZombiesAlive = 1;
            entityManager.SetComponentData(waveEntity, wave);

            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager, enemyPool, out Entity target), Is.True);
            entityManager.SetComponentData(target, new ZombieState { Value = ZombieStateType.Moving });
            entityManager.SetComponentData(target, new ZombieStats
            {
                MoveSpeed = 0f,
                MaxHP = 100f,
                CurrentHP = 100f,
                AttackCooldown = 999f
            });
            entityManager.SetComponentData(target,
                LocalTransform.FromPositionRotationScale(new float3(100f, 0f, -1f), quaternion.identity, 1f));

            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, arrowPool, arrowPrefab, out Entity arrow), Is.True);
            entityManager.SetComponentData(arrow,
                LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            entityManager.SetComponentData(arrow, new ArrowProjectile
            {
                Speed = 0f,
                Damage = 1f,
                Target = target,
                TargetPoolGeneration = entityManager.GetComponentData<EnemyPoolMember>(target).Generation,
                ArcherType = ArcherType.Basic,
                SlowMultiplier = 1f,
                RemainingLifetime = 0f
            });

            for (int frame = 0;
                 frame < 10 && entityManager.IsComponentEnabled<ArrowTag>(arrow);
                 frame++)
                yield return null;

            Assert.That(entityManager.Exists(arrow), Is.True,
                "Suresi dolan pooled ok DestroyEntity ile silindi.");
            Assert.That(entityManager.IsComponentEnabled<ArrowTag>(arrow), Is.False,
                "Suresi dolan ok inactive pool rezervine donmedi.");
            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, arrowPool, arrowPrefab, out Entity reused), Is.True);
            Assert.That(reused, Is.EqualTo(arrow));

            ArrowPoolRuntimeUtility.Return(entityManager, arrowPool, reused);
            EnemyPoolRuntimeUtility.Return(entityManager, enemyPool, target);
            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);
        }
    }
}
