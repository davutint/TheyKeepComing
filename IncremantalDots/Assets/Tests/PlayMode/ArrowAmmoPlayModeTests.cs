using System.Collections;
using System.IO;
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
    public class ArrowAmmoPlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;
        private float _originalCaptureDeltaTime;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            _originalCaptureDeltaTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
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
            Time.captureDeltaTime = _originalCaptureDeltaTime;
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZeroSupply_StopsShot_AndInstantRefillResumesOnNextSimulationTick()
        {
            yield return WaitForRuntime();
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            SetupIsolatedCombat(em, out Entity gameStateEntity, out Entity waveEntity,
                out Entity enemyPoolEntity, out Entity arrowPoolEntity, out Entity target);

            Entity archer = CreateArcher(em, ArcherType.Basic, 0.01f, 10f);
            ArrowSupply supply = em.GetComponentData<ArrowSupply>(gameStateEntity);
            supply.Current = 0;
            em.SetComponentData(gameStateEntity, supply);

            yield return null;
            yield return null;
            SetReady(em, archer);
            ArrowPoolRuntimeData before = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            yield return null;

            ArrowPoolRuntimeData stopped = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(stopped.TotalRentCount, Is.EqualTo(before.TotalRentCount));
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero);

            ResourceData resources = em.GetComponentData<ResourceData>(gameStateEntity);
            resources.Wood = 1000;
            em.SetComponentData(gameStateEntity, resources);
            yield return null;

            Assert.That(GameManager.Instance.TryBuyArrowRefill(1), Is.True);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.EqualTo(100));
            yield return null;

            ArrowPoolRuntimeData resumed = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(resumed.TotalRentCount - stopped.TotalRentCount, Is.EqualTo(1));
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.EqualTo(99),
                "Gercek pooled projectile basina tam 1 Arrow dusmeli.");

            Cleanup(em, waveEntity, enemyPoolEntity, arrowPoolEntity, target, archer);
        }

        [UnityTest]
        public IEnumerator ThousandArchers_ZeroSupplyThenBulkRefill_RestartsPooledSalvoNextTick()
        {
            const int archerCount = 1_000;
            const int refillPackageCount = 10;

            yield return WaitForRuntime();
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            SetupIsolatedCombat(em, out Entity gameStateEntity, out Entity waveEntity,
                out Entity enemyPoolEntity, out Entity arrowPoolEntity, out Entity target);

            Entity archerPrefab = em.GetComponentData<ArcherPrefabData>(
                em.CreateEntityQuery(typeof(ArcherPrefabData)).GetSingletonEntity()).ArcherPrefab;
            using (NativeArray<Entity> archers =
                   em.Instantiate(archerPrefab, archerCount, Allocator.Temp))
            {
                for (int i = 0; i < archers.Length; i++)
                {
                    em.SetComponentData(archers[i], new ArcherUnit
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
                    em.SetComponentData(archers[i], LocalTransform.FromPositionRotationScale(
                        new float3(0f, 0f, MobileCastleRenderDepth.UnitZ),
                        quaternion.identity,
                        1f));
                }
            }

            Entity arrowPrefab = em.GetComponentData<ArrowPrefabData>(
                em.CreateEntityQuery(typeof(ArrowPrefabData)).GetSingletonEntity()).ArrowPrefab;
            Assert.That(ArrowPoolRuntimeUtility.Maintain(em, arrowPoolEntity, arrowPrefab), Is.True);
            ArrowPoolRuntimeData poolBefore = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(poolBefore.AvailableCount, Is.GreaterThanOrEqualTo(archerCount),
                "1K restart olcumunden once projectile pool prewarm tamamlanmis olmali.");

            ArrowSupply supply = em.GetComponentData<ArrowSupply>(gameStateEntity);
            supply.CapacityLevel = 50;
            supply.Current = 0;
            em.SetComponentData(gameStateEntity, supply);

            ResourceData resources = em.GetComponentData<ResourceData>(gameStateEntity);
            resources.Wood = 100_000;
            em.SetComponentData(gameStateEntity, resources);
            yield return null;

            Time.timeScale = 0f;
            long stoppedRentCount = em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount;
            yield return null;
            Assert.That(em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity).TotalRentCount,
                Is.EqualTo(stoppedRentCount),
                "1K hazir okcu Arrow stoku sifirken projectile rent etmemeli.");

            ArrowRefillQuote quote = GameManager.Instance.GetArrowRefillQuote(refillPackageCount);
            Assert.That(quote.IsValid, Is.True);
            Assert.That(quote.ArrowAmount, Is.EqualTo(archerCount));
            int woodBefore = em.GetComponentData<ResourceData>(gameStateEntity).Wood;

            double refillTransactionMs;
            double restartMainThreadMs;
            double restartWallFrameMs;
            long restartGcBytes;
            using (var mainThread = ProfilerRecorder.StartNew(
                       ProfilerCategory.Internal, "Main Thread", 1))
            using (var gcAllocated = ProfilerRecorder.StartNew(
                       ProfilerCategory.Memory, "GC Allocated In Frame", 1))
            {
                Assert.That(mainThread.Valid, Is.True);
                Assert.That(gcAllocated.Valid, Is.True);

                // Recorder'in ilk sample'ini sifir stoklu frame ile isit; refill ve 1K salvo
                // takip eden tek kayitli frame'de birlikte olculur.
                yield return null;
                Assert.That(em.GetComponentData<ArrowPoolRuntimeData>(
                    arrowPoolEntity).TotalRentCount, Is.EqualTo(stoppedRentCount));

                double refillStarted = Time.realtimeSinceStartupAsDouble;
                Assert.That(GameManager.Instance.TryBuyArrowRefill(refillPackageCount), Is.True);
                refillTransactionMs =
                    (Time.realtimeSinceStartupAsDouble - refillStarted) * 1000.0;
                Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                    Is.EqualTo(archerCount));
                Assert.That(em.GetComponentData<ResourceData>(gameStateEntity).Wood,
                    Is.EqualTo(woodBefore - quote.WoodCost));

                double restartFrameStarted = Time.realtimeSinceStartupAsDouble;
                yield return null;
                restartWallFrameMs =
                    (Time.realtimeSinceStartupAsDouble - restartFrameStarted) * 1000.0;
                restartMainThreadMs = mainThread.LastValue / 1_000_000.0;
                restartGcBytes = gcAllocated.LastValue;
            }

            ArrowPoolRuntimeData poolAfter = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            using EntityQuery projectileQuery = em.CreateEntityQuery(
                typeof(ArrowTag), typeof(ArrowProjectile), typeof(LocalTransform));
            Assert.That(poolAfter.TotalRentCount - stoppedRentCount, Is.EqualTo(archerCount),
                "Refill sonrasi ilk simulation tick'i 1K pooled gameplay projectile uretmeli.");
            Assert.That(poolAfter.ActiveCount, Is.EqualTo(archerCount));
            Assert.That(projectileQuery.CalculateEntityCount(), Is.EqualTo(archerCount));
            Assert.That(poolAfter.TotalCreated, Is.EqualTo(poolBefore.TotalCreated),
                "1K refill restart frame'i prewarm pool'u genisletmemeli.");
            Assert.That(poolAfter.ExpansionCount, Is.EqualTo(poolBefore.ExpansionCount));
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "1K gercek projectile tam 1K Arrow tuketmeli.");
            Assert.That(restartMainThreadMs, Is.GreaterThan(0.0),
                "Main Thread profiler sample'i olculebilir olmali.");
            Assert.That(restartMainThreadMs, Is.LessThan(50.0),
                $"1K refill restart Editor main-thread safety budget'ini asti: {restartMainThreadMs:F2} ms.");
            Assert.That(restartWallFrameMs, Is.LessThan(100.0),
                $"1K refill restart wall-frame safety budget'ini asti: {restartWallFrameMs:F2} ms.");

            Debug.Log(
                $"[DW-V1-ARROW-REFILL-1K] archers={archerCount}; refill_arrows={quote.ArrowAmount}; " +
                $"wood_cost={quote.WoodCost}; refill_transaction_ms={refillTransactionMs:F3}; " +
                $"restart_main_ms={restartMainThreadMs:F3}; " +
                $"restart_wall_frame_ms={restartWallFrameMs:F3}; restart_gc_bytes={restartGcBytes}; " +
                $"rents={poolAfter.TotalRentCount - stoppedRentCount}; " +
                $"pool_expansions={poolAfter.ExpansionCount - poolBefore.ExpansionCount}");

            Time.timeScale = 1f;
            ArrowPoolRuntimeUtility.ReturnAllActive(em, arrowPoolEntity);
            using (EntityQuery archerQuery = em.CreateEntityQuery(typeof(ArcherUnit)))
                em.DestroyEntity(archerQuery);
            if (em.Exists(target))
                EnemyPoolRuntimeUtility.Return(em, enemyPoolEntity, target);
            WaveStateData wave = em.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            em.SetComponentData(waveEntity, wave);
        }

        [UnityTest]
        public IEnumerator RapidFireRate_ConsumesMoreFiniteArrowsThanBasic()
        {
            yield return WaitForRuntime();
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            SetupIsolatedCombat(em, out Entity gameStateEntity, out Entity waveEntity,
                out Entity enemyPoolEntity, out Entity arrowPoolEntity, out Entity target);

            int basicShots = 0;
            Entity basic = CreateArcher(em, ArcherType.Basic, 2f, 0f);
            yield return null;
            yield return null;
            SetSupply(em, gameStateEntity, 200);
            long basicStart = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity).TotalRentCount;
            for (int frame = 0; frame < 120; frame++)
                yield return null;
            basicShots = (int)(em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity).TotalRentCount - basicStart);

            em.DestroyEntity(basic);
            ArrowPoolRuntimeUtility.ReturnAllActive(em, arrowPoolEntity);
            Entity rapid = CreateArcher(em, ArcherType.Rapid, 8f, 0f);
            SetSupply(em, gameStateEntity, 200);
            long rapidStart = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity).TotalRentCount;
            for (int frame = 0; frame < 120; frame++)
                yield return null;
            int rapidShots = (int)(em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity).TotalRentCount - rapidStart);

            Assert.That(basicShots, Is.GreaterThan(0));
            Assert.That(rapidShots, Is.GreaterThan(basicShots),
                $"Rapid ammo bedeli gorunur olmali. Basic={basicShots}, Rapid={rapidShots}");
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                Is.EqualTo(200 - rapidShots));

            Cleanup(em, waveEntity, enemyPoolEntity, arrowPoolEntity, target, rapid);
        }

        private static IEnumerator WaitForRuntime()
        {
            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance.SaveRunSnapshot())
                    yield break;
                yield return null;
            }

            Assert.Fail("GameManager/SubScene 300 frame icinde hazir olmadi.");
        }

        private static void SetupIsolatedCombat(EntityManager em, out Entity gameStateEntity,
            out Entity waveEntity, out Entity enemyPoolEntity, out Entity arrowPoolEntity, out Entity target)
        {
            Time.timeScale = 1f;
            GameManager.Instance.enabled = true;
            gameStateEntity = em.CreateEntityQuery(typeof(GameStateData)).GetSingletonEntity();
            GameStateData gameState = em.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            em.SetComponentData(gameStateEntity, gameState);

            enemyPoolEntity = em.CreateEntityQuery(typeof(EnemyPoolRuntimeData),
                typeof(EnemyPoolAvailable), typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            EnemyPoolRuntimeUtility.ReturnAllActive(em, enemyPoolEntity);
            arrowPoolEntity = em.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable)).GetSingletonEntity();
            ArrowPoolRuntimeUtility.ReturnAllActive(em, arrowPoolEntity);

            using (EntityQuery archerQuery = em.CreateEntityQuery(typeof(ArcherUnit)))
                em.DestroyEntity(archerQuery);

            waveEntity = em.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            WaveStateData wave = em.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            wave.SpawnTimer = 999f;
            wave.ZombiesAlive = 1;
            em.SetComponentData(waveEntity, wave);

            Assert.That(EnemyPoolRuntimeUtility.TryRent(em, enemyPoolEntity, out target), Is.True);
            em.SetComponentData(target,
                LocalTransform.FromPositionRotationScale(new float3(4f, 0f, -1f), quaternion.identity, 1f));
            em.SetComponentData(target, new ZombieStats
            {
                MoveSpeed = 0f,
                MaxHP = 1000000f,
                CurrentHP = 1000000f,
                AttackDamage = 0f,
                AttackCooldown = 999f,
                AttackTimer = 0f,
                XPReward = 0
            });
            em.SetComponentData(target, new ZombieState { Value = ZombieStateType.Moving });
        }

        private static Entity CreateArcher(EntityManager em, ArcherType type, float fireRate, float damage)
        {
            Entity prefab = em.GetComponentData<ArcherPrefabData>(
                em.CreateEntityQuery(typeof(ArcherPrefabData)).GetSingletonEntity()).ArcherPrefab;
            Entity archer = em.Instantiate(prefab);
            em.SetComponentData(archer, new ArcherUnit
            {
                FireRate = fireRate,
                FireTimer = 0f,
                ArrowDamage = damage,
                Range = 15f,
                Type = type,
                SlowDuration = 0f,
                SlowMultiplier = 1f,
                FacingDirection = new float2(1f, 0f),
                AttackAnimTimer = 0f
            });
            em.SetComponentData(archer,
                LocalTransform.FromPositionRotationScale(new float3(0f, 0f, -1f), quaternion.identity, 1f));
            return archer;
        }

        private static void SetReady(EntityManager em, Entity archer)
        {
            ArcherUnit unit = em.GetComponentData<ArcherUnit>(archer);
            unit.FireTimer = 0f;
            em.SetComponentData(archer, unit);
        }

        private static void SetSupply(EntityManager em, Entity gameStateEntity, int current)
        {
            ArrowSupply supply = em.GetComponentData<ArrowSupply>(gameStateEntity);
            supply.Current = current;
            em.SetComponentData(gameStateEntity, supply);
        }

        private static void Cleanup(EntityManager em, Entity waveEntity, Entity enemyPoolEntity,
            Entity arrowPoolEntity, Entity target, Entity archer)
        {
            ArrowPoolRuntimeUtility.ReturnAllActive(em, arrowPoolEntity);
            if (em.Exists(archer))
                em.DestroyEntity(archer);
            if (em.Exists(target))
                EnemyPoolRuntimeUtility.Return(em, enemyPoolEntity, target);
            WaveStateData wave = em.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = false;
            wave.ZombiesAlive = 0;
            em.SetComponentData(waveEntity, wave);
        }
    }
}
