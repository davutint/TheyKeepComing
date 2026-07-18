using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadWalls.Tests
{
    public class CombatRewardFeedbackPlayModeTests
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

            Assert.That(GameManager.Instance, Is.Not.Null);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                EntityManager entityManager = world.EntityManager;
                using EntityQuery poolQuery = entityManager.CreateEntityQuery(
                    typeof(EnemyPoolRuntimeData),
                    typeof(EnemyPoolAvailable));
                if (poolQuery.CalculateEntityCount() == 1)
                    EnemyPoolRuntimeUtility.ReturnAllActive(
                        entityManager,
                        poolQuery.GetSingletonEntity());
            }

            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamageNumberBridge_ConsumesEveryPlayerDamageSource()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CombatFeedbackBridge bridge =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(bridge, Is.Not.Null);

            Time.timeScale = 0f;
            long baseline = bridge.TotalDamageNumbersPlayedCount;
            PlayerDamageSourceType[] sources =
            {
                PlayerDamageSourceType.BasicArrow,
                PlayerDamageSourceType.RapidArrow,
                PlayerDamageSourceType.FrostArrow,
                PlayerDamageSourceType.Fireball,
                PlayerDamageSourceType.FireballSecondBlast,
                PlayerDamageSourceType.FireballBurningGround
            };
            for (int i = 0; i < sources.Length; i++)
            {
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new CombatDamageNumberEvent
                {
                    Position = new float3(6f + i * 0.25f, -1f, 0f),
                    AppliedDamage = 10f + i,
                    Source = sources[i]
                });
            }

            yield return null;

            Assert.That(
                bridge.TotalDamageNumbersPlayedCount - baseline,
                Is.EqualTo(sources.Length));
            Assert.That(bridge.ActiveDamageNumberCount, Is.GreaterThanOrEqualTo(sources.Length));
            using EntityQuery eventQuery = entityManager.CreateEntityQuery(
                typeof(CombatDamageNumberEvent));
            Assert.That(eventQuery.CalculateEntityCount(), Is.Zero,
                "Presentation bridge butun gercek damage event'lerini tuketmeli.");
        }

        [UnityTest]
        public IEnumerator SkeletonDeath_AwardsOneSoulImmediatelyAndStartsHudTravel()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery poolQuery = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData),
                typeof(EnemyPoolAvailable));
            for (int frame = 0;
                 frame < 180 && (poolQuery.CalculateEntityCount() != 1
                                 || entityManager.GetBuffer<EnemyPoolAvailable>(
                                     poolQuery.GetSingletonEntity()).Length == 0);
                 frame++)
                yield return null;

            Assert.That(poolQuery.CalculateEntityCount(), Is.EqualTo(1));
            Entity poolEntity = poolQuery.GetSingletonEntity();
            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager,
                poolEntity,
                out Entity skeleton), Is.True);

            ZombieStats stats = entityManager.GetComponentData<ZombieStats>(skeleton);
            stats.CurrentHP = 0f;
            stats.MaxHP = Mathf.Max(1f, stats.MaxHP);
            entityManager.SetComponentData(skeleton, stats);
            entityManager.SetComponentData(skeleton,
                new ZombieState { Value = ZombieStateType.Moving });
            entityManager.SetComponentData(skeleton,
                LocalTransform.FromPositionRotationScale(
                    new float3(8f, 0f, MobileCastleRenderDepth.UnitZ),
                    quaternion.identity,
                    1f));

            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(GameStateData)).GetSingletonEntity();
            GameStateData gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            int baselineKills = gameState.TotalKills;
            entityManager.SetComponentData(gameStateEntity, gameState);

            Entity waveEntity = entityManager.CreateEntityQuery(
                typeof(WaveStateData)).GetSingletonEntity();
            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 1;
            wave.SpawnTimer = 999f;
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);

            SoulCounterUI soulCounter = Object.FindFirstObjectByType<SoulCounterUI>();
            Assert.That(soulCounter, Is.Not.Null);
            long baselineVisuals = soulCounter.TotalSoulVisualsPlayedCount;
            Time.timeScale = 0f;

            for (int frame = 0; frame < 8; frame++)
                yield return null;

            GameStateData after = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            Assert.That(after.TotalKills, Is.EqualTo(baselineKills + 1),
                "Skeleton olum frame'inde tam 1 Soul/kill gameplay state'ine yazilmali.");
            Assert.That(
                soulCounter.TotalSoulVisualsPlayedCount - baselineVisuals,
                Is.EqualTo(1),
                "Ayni olum HUD'a giden tam bir Soul gorseli baslatmali.");

            using EntityQuery soulEventQuery = entityManager.CreateEntityQuery(
                typeof(SoulPickupEvent));
            Assert.That(soulEventQuery.CalculateEntityCount(), Is.Zero,
                "Soul presentation event'i bridge tarafindan tuketilmeli.");
        }
    }
}
