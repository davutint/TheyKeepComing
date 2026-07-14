using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class ArcherTargetingPlayModeTests
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
            for (int frame = 0; frame < 120 && GameManager.Instance == null; frame++)
                yield return null;

            Assert.That(GameManager.Instance, Is.Not.Null, "NewGameScene GameManager olusturmadi.");
            yield return null;
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
        public IEnumerator BasicRapidFrost_UseSharedNearestPolicy_AndReserveLethalIncomingDamage()
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

            Entity gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateData))
                .GetSingletonEntity();
            GameStateData gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            entityManager.SetComponentData(gameStateEntity, gameState);

            ArrowSupply arrowSupply = entityManager.GetComponentData<ArrowSupply>(gameStateEntity);
            arrowSupply.Current = 100;
            entityManager.SetComponentData(gameStateEntity, arrowSupply);

            Entity poolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData),
                typeof(EnemyPoolAvailable),
                typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            Entity arrowPoolEntity = entityManager.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable)).GetSingletonEntity();
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);

            using (EntityQuery archerQuery = entityManager.CreateEntityQuery(typeof(ArcherUnit)))
                entityManager.DestroyEntity(archerQuery);

            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData))
                .GetSingletonEntity();
            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            wave.SpawnTimer = 999f;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);

            Entity[] targets = new Entity[3];
            float[] targetY = { 0f, -0.3f, 0.3f };
            for (int i = 0; i < targets.Length; i++)
            {
                Assert.That(EnemyPoolRuntimeUtility.TryRent(
                    entityManager, poolEntity, out targets[i]), Is.True);
                ConfigureTarget(entityManager, targets[i], new float3(4f, targetY[i], -1f));
            }

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = targets.Length;
            entityManager.SetComponentData(waveEntity, wave);

            Entity archerPrefab = entityManager.GetComponentData<ArcherPrefabData>(
                entityManager.CreateEntityQuery(typeof(ArcherPrefabData)).GetSingletonEntity()).ArcherPrefab;
            Entity[] archers = new Entity[3];
            ArcherType[] types = { ArcherType.Basic, ArcherType.Rapid, ArcherType.Frost };
            for (int i = 0; i < archers.Length; i++)
            {
                archers[i] = entityManager.Instantiate(archerPrefab);
                entityManager.SetComponentData(archers[i], new ArcherUnit
                {
                    FireRate = 0.01f,
                    FireTimer = 999f,
                    ArrowDamage = 10f,
                    Range = 15f,
                    Type = types[i],
                    SlowDuration = types[i] == ArcherType.Frost ? 2f : 0f,
                    SlowMultiplier = types[i] == ArcherType.Frost ? 0.55f : 1f,
                    FacingDirection = new float2(1f, 0f),
                    AttackAnimTimer = 0f
                });
                entityManager.SetComponentData(
                    archers[i],
                    LocalTransform.FromPositionRotationScale(
                        new float3(0f, 0f, -1f), quaternion.identity, 1f));
            }

            // Double-buffer target grid'in yeni rent edilen entity'leri read snapshot'a almasini bekle.
            yield return null;
            yield return null;

            for (int i = 0; i < archers.Length; i++)
            {
                ArcherUnit archer = entityManager.GetComponentData<ArcherUnit>(archers[i]);
                archer.FireTimer = 0f;
                entityManager.SetComponentData(archers[i], archer);
            }

            yield return null;

            using EntityQuery projectileQuery = entityManager.CreateEntityQuery(
                typeof(ArrowTag), typeof(ArrowProjectile));
            using NativeArray<ArrowProjectile> projectiles =
                projectileQuery.ToComponentDataArray<ArrowProjectile>(Allocator.Temp);
            Assert.That(projectiles.Length, Is.EqualTo(3));

            var selectedTargets = new HashSet<Entity>();
            var selectedTypes = new HashSet<ArcherType>();
            for (int i = 0; i < projectiles.Length; i++)
            {
                selectedTargets.Add(projectiles[i].Target);
                selectedTypes.Add(projectiles[i].ArcherType);
            }

            Assert.That(selectedTargets.Count, Is.EqualTo(3),
                "Lethal incoming damage rezerve edilmis hedefe ikinci ok yigildi.");
            Assert.That(selectedTypes.SetEquals(types), Is.True,
                "Basic/Rapid/Frost ortak targeting policy'sinden atis uretmedi.");
            for (int i = 0; i < targets.Length; i++)
                Assert.That(selectedTargets.Contains(targets[i]), Is.True);

            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);
            for (int i = 0; i < archers.Length; i++)
                if (entityManager.Exists(archers[i]))
                    entityManager.DestroyEntity(archers[i]);
            for (int i = 0; i < targets.Length; i++)
                EnemyPoolRuntimeUtility.Return(entityManager, poolEntity, targets[i]);

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            entityManager.SetComponentData(waveEntity, wave);
        }

        private static void ConfigureTarget(
            EntityManager entityManager,
            Entity target,
            float3 position)
        {
            entityManager.SetComponentData(
                target,
                LocalTransform.FromPositionRotationScale(position, quaternion.identity, 1f));
            entityManager.SetComponentData(target, new ZombieStats
            {
                MoveSpeed = 0f,
                MaxHP = 10f,
                CurrentHP = 10f,
                AttackDamage = 0f,
                AttackCooldown = 999f,
                AttackTimer = 0f,
                XPReward = 0
            });
            entityManager.SetComponentData(
                target, new ZombieState { Value = ZombieStateType.Moving });
        }
    }
}
