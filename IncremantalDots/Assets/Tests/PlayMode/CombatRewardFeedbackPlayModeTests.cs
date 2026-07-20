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
        public IEnumerator DamageNumberBridge_DenseBurstAggregatesSpatiallyWithoutLosingEventsOrDamage()
        {
            const int eventCount = 2_000;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CombatFeedbackBridge bridge =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(bridge, Is.Not.Null);

            Time.timeScale = 0f;
            long baseline = bridge.TotalDamageNumbersPlayedCount;
            double expectedDamage = 0d;
            for (int i = 0; i < eventCount; i++)
            {
                float damage = 10f + i % 3;
                expectedDamage += damage;
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new CombatDamageNumberEvent
                {
                    Position = new float3(
                        6f + (i % 100) * 0.12f,
                        -2f + (i / 100) * 0.13f,
                        0f),
                    AppliedDamage = damage,
                    Source = i % 2 == 0
                        ? PlayerDamageSourceType.Fireball
                        : PlayerDamageSourceType.FireballSecondBlast
                });
            }

            yield return null;

            Assert.That(bridge.LastProcessedDamageNumberEventCount, Is.EqualTo(eventCount));
            Assert.That(bridge.TotalDamageNumbersPlayedCount - baseline, Is.EqualTo(eventCount));
            Assert.That(bridge.ActiveDamageNumberCount, Is.GreaterThanOrEqualTo(eventCount));
            Assert.That(bridge.LastDamageNumberPresentationCount, Is.LessThan(eventCount));
            Assert.That(bridge.ActiveDamageNumberBatchCount,
                Is.LessThanOrEqualTo(
                    Mathf.CeilToInt(
                        bridge.LastDamageNumberPresentationCount
                        / (float)Mathf.Max(1, bridge.DamageNumberBatchCapacity))));
            Assert.That(bridge.LastProcessedDamageNumberTotal,
                Is.EqualTo(expectedDamage).Within(0.01d));
            Assert.That(bridge.LastPresentedDamageNumberTotal,
                Is.EqualTo(expectedDamage).Within(0.01d));

            using EntityQuery eventQuery = entityManager.CreateEntityQuery(
                typeof(CombatDamageNumberEvent));
            Assert.That(eventQuery.CalculateEntityCount(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator SoulCounter_DenseBurstAggregatesFlightsWithoutLosingEventsOrSoulAmount()
        {
            const int eventCount = 2_000;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            SoulCounterUI soulCounter = Object.FindFirstObjectByType<SoulCounterUI>();
            Assert.That(soulCounter, Is.Not.Null);

            Time.timeScale = 0f;
            for (int i = 0; i < eventCount; i++)
            {
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new SoulPickupEvent
                {
                    Position = new float3(
                        3f + (i % 100) * 0.16f,
                        -4f + (i / 100) * 0.22f,
                        MobileCastleRenderDepth.UnitZ),
                    Amount = 1
                });
            }

            yield return null;

            Assert.That(soulCounter.LastProcessedSoulEventCount, Is.EqualTo(eventCount));
            Assert.That(soulCounter.LastSoulPresentationCount,
                Is.LessThanOrEqualTo(
                    Mathf.Max(1, soulCounter.MaxSoulPickupPresentationsPerBurst)));
            Assert.That(soulCounter.LastSoulPresentationCount, Is.LessThan(eventCount));
            Assert.That(soulCounter.LastProcessedSoulAmount, Is.EqualTo(eventCount));
            Assert.That(soulCounter.LastPresentedSoulAmount, Is.EqualTo(eventCount));

            using EntityQuery eventQuery = entityManager.CreateEntityQuery(
                typeof(SoulPickupEvent));
            Assert.That(eventQuery.CalculateEntityCount(), Is.Zero,
                "Dense Soul presentation event'lerinin tamami tuketilmeli.");
        }

        [UnityTest]
        public IEnumerator SkeletonDeath_AwardsSoulAndConfiguredGraveEssenceDropImmediately()
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

            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig)).GetSingletonEntity();
            MobileCastleCombatConfig config =
                entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.GraveEssenceDropChance = 1f;
            config.GraveEssencePerDrop = 1;
            config.GraveEssenceDropSeed = 91273u;
            entityManager.SetComponentData(configEntity, config);

            long baselineEssence = GameManager.Instance.GraveEssenceAmount;

            SoulCounterUI soulCounter = Object.FindFirstObjectByType<SoulCounterUI>();
            Assert.That(soulCounter, Is.Not.Null);
            long baselineVisuals = soulCounter.TotalSoulVisualsPlayedCount;
            GameplayHUDToolkitUI toolkit = Object.FindFirstObjectByType<GameplayHUDToolkitUI>();
            Assert.That(toolkit, Is.Not.Null);
            long baselineEssenceFlights = toolkit.TotalGraveEssenceFlightsStartedCount;
            long baselineCurrencyArrivalSfx = toolkit.TotalCurrencyArrivalSfxPlayedCount;
            CombatFeedbackBridge combatFeedback =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(combatFeedback, Is.Not.Null);
            long baselineCombatSfx = combatFeedback.TotalSfxPlayedCount;
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
            Assert.That(GameManager.Instance.GraveEssenceAmount,
                Is.GreaterThanOrEqualTo(baselineEssence + 1L),
                "%100 test roll'u canonical GrantGraveEssence kapisindan en az +1 vermeli.");
            Assert.That(
                toolkit.TotalGraveEssenceFlightsStartedCount - baselineEssenceFlights,
                Is.EqualTo(1L),
                "Basarili Grave Essence drop'u olum konumundan HUD sayacina bir UI Toolkit ucusu baslatmali.");
            Assert.That(combatFeedback.TotalSfxPlayedCount, Is.EqualTo(baselineCombatSfx),
                "Skeleton olumu bireysel/aggregate death SFX uretmemeli.");

            for (int frame = 0;
                 frame < 180
                 && toolkit.TotalCurrencyArrivalSfxPlayedCount - baselineCurrencyArrivalSfx < 2L;
                 frame++)
            {
                yield return null;
            }
            Assert.That(
                toolkit.TotalCurrencyArrivalSfxPlayedCount - baselineCurrencyArrivalSfx,
                Is.EqualTo(2L),
                "Soul ve Essence gameplay odul aninda degil, kendi HUD ucuslari tamamlandiginda birer bounded ses uretmeli.");

            using EntityQuery soulEventQuery = entityManager.CreateEntityQuery(
                typeof(SoulPickupEvent));
            Assert.That(soulEventQuery.CalculateEntityCount(), Is.Zero,
                "Soul presentation event'i bridge tarafindan tuketilmeli.");
            using EntityQuery essenceEventQuery = entityManager.CreateEntityQuery(
                typeof(GraveEssenceDropEvent));
            Assert.That(essenceEventQuery.CalculateEntityCount(), Is.Zero,
                "Grave Essence drop event'i GameManager transaction bridge tarafindan tuketilmeli.");

            Assert.That(EnemyPoolRuntimeUtility.TryRent(
                entityManager,
                poolEntity,
                out Entity stressSkeleton), Is.True);
            ZombieStats stressStats = entityManager.GetComponentData<ZombieStats>(stressSkeleton);
            stressStats.CurrentHP = 0f;
            stressStats.MaxHP = Mathf.Max(1f, stressStats.MaxHP);
            entityManager.SetComponentData(stressSkeleton, stressStats);
            entityManager.SetComponentData(stressSkeleton,
                new ZombieState { Value = ZombieStateType.Moving });
            entityManager.SetComponentData(stressSkeleton,
                LocalTransform.FromPositionRotationScale(
                    new float3(9f, 0f, MobileCastleRenderDepth.UnitZ),
                    quaternion.identity,
                    1f));

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive++;
            wave.StressTestMode = true;
            entityManager.SetComponentData(waveEntity, wave);
            long beforeStressDeath = GameManager.Instance.GraveEssenceAmount;

            for (int frame = 0; frame < 8; frame++)
                yield return null;

            Assert.That(GameManager.Instance.GraveEssenceAmount, Is.EqualTo(beforeStressDeath),
                "Stress-test olumleri chance %100 olsa bile Grave Essence uretmemeli.");
        }
    }
}
