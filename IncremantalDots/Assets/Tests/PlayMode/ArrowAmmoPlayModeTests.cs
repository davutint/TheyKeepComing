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

namespace DeadWalls.Tests
{
    public class ArrowAmmoPlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;
        private float _originalCaptureDeltaTime;
        private float _originalTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            _originalCaptureDeltaTime = Time.captureDeltaTime;
            _originalTimeScale = Time.timeScale;
            Time.captureDeltaTime = 1f / 60f;
            Time.timeScale = 1f;
            TutorialSessionProgress.BeginNewPlaySession();
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId,
                true), Is.True,
                "Arrow ammo fixture guided tutorial pause'undan izole edilemedi.");
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
            Time.timeScale = _originalTimeScale;
            Time.captureDeltaTime = _originalCaptureDeltaTime;
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZeroSupply_HoldsFireForFullDelivery_ThenResumesAfterAtomicArrival()
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

            ArrowRefillQuote quote = GameManager.Instance.GetArrowRefillQuote(1);
            Assert.That(quote.IsValid, Is.True);
            Assert.That(GameManager.Instance.TryBuyArrowRefill(1), Is.True);
            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.True);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "Arrow stoku transaction frame'inde aninda dolmamalidir.");

            Time.timeScale = 1f;
            for (int frame = 0; frame < 90; frame++)
                yield return null;

            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.True,
                "Siparis 3 simulation saniyesi dolmadan tamamlanmamalidir.");
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "Teslimat surerken siparisten tek bir Arrow bile kullanilabilir stoga girmemelidir.");
            Assert.That(em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount, Is.EqualTo(stopped.TotalRentCount),
                "Okcular teslimat tamamlanmadan projectile rent etmemelidir.");
            Assert.That(GameManager.Instance.ArrowRefillDeliveryProgress01,
                Is.InRange(0.4f, 0.6f));

            for (int frame = 0;
                 frame < 240 && GameManager.Instance.IsArrowRefillDeliveryActive;
                 frame++)
            {
                yield return null;
            }
            yield return null;

            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.False,
                "3 simulation saniyelik teslimat 240 test frame'i icinde tamamlanmadi.");
            ArrowPoolRuntimeData resumed = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            long deliveredShotCount = resumed.TotalRentCount - stopped.TotalRentCount;
            ArrowSupply deliveredSupply = em.GetComponentData<ArrowSupply>(gameStateEntity);
            Assert.That(deliveredShotCount, Is.GreaterThan(0),
                "Arrow siparisi tamamen teslim edildikten sonra okcular atisa devam etmelidir.");
            Assert.That(deliveredSupply.Current + deliveredShotCount,
                Is.EqualTo(quote.ArrowAmount),
                "Atomik teslim edilen stok ile gercek pooled projectile tuketimi birlikte alinan pakete esit olmalidir.");

            Cleanup(em, waveEntity, enemyPoolEntity, arrowPoolEntity, target, archer);
        }

        [UnityTest]
        public IEnumerator V1CastleCombat_OnlyArrowStockHasContinuousDrain()
        {
            yield return WaitForRuntime();
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            using (EntityQuery arrowProducerQuery = em.CreateEntityQuery(typeof(ArrowProducer)))
            using (EntityQuery archerTrainerQuery = em.CreateEntityQuery(typeof(ArcherTrainer)))
            {
                Assert.That(arrowProducerQuery.CalculateEntityCount(), Is.Zero,
                    "Production castle world Fletcher/ArrowProducer tasimamali.");
                Assert.That(archerTrainerQuery.CalculateEntityCount(), Is.Zero,
                    "Production castle world legacy Barracks trainer tasimamali.");
            }

            SetupIsolatedCombat(em, out Entity gameStateEntity, out Entity waveEntity,
                out Entity enemyPoolEntity, out Entity arrowPoolEntity, out Entity target);
            Entity archer = CreateArcher(em, ArcherType.Basic, 1f, 0f);

            // Broadphase/target cache'ini kur; olcumden once olasi warm-up projectile'ini temizle.
            yield return null;
            yield return null;
            ArrowPoolRuntimeUtility.ReturnAllActive(em, arrowPoolEntity);

            var baseline = new ResourceData
            {
                Wood = 5000,
                Stone = 5000,
                Iron = 5000,
                Food = 5000
            };
            em.SetComponentData(gameStateEntity, baseline);
            em.SetComponentData(gameStateEntity, new ResourceConsumptionRate
            {
                WoodPerMin = 60000f,
                StonePerMin = 60000f,
                IronPerMin = 60000f,
                FoodPerMin = 60000f
            });
            em.SetComponentData(gameStateEntity, new ResourceAccumulator());
            SetSupply(em, gameStateEntity, 10);
            SetReady(em, archer);

            long rentsBefore = em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount;
            yield return null;

            ResourceData after = em.GetComponentData<ResourceData>(gameStateEntity);
            ResourceConsumptionRate consumption =
                em.GetComponentData<ResourceConsumptionRate>(gameStateEntity);
            ArrowSupply arrows = em.GetComponentData<ArrowSupply>(gameStateEntity);
            long rentsAfter = em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount;

            Assert.That(rentsAfter - rentsBefore, Is.EqualTo(1),
                "Hazir tek okcu gercek projectile rent etmelidir.");
            Assert.That(arrows.Current, Is.EqualTo(9),
                "Basarili tek projectile rent'i tam 1 Arrow tuketmelidir.");
            Assert.That(after.Wood, Is.GreaterThanOrEqualTo(baseline.Wood));
            Assert.That(after.Stone, Is.GreaterThanOrEqualTo(baseline.Stone));
            Assert.That(after.Iron, Is.GreaterThanOrEqualTo(baseline.Iron));
            Assert.That(after.Food, Is.GreaterThanOrEqualTo(baseline.Food));
            Assert.That(consumption.WoodPerMin, Is.Zero);
            Assert.That(consumption.StonePerMin, Is.Zero);
            Assert.That(consumption.IronPerMin, Is.Zero);
            Assert.That(consumption.FoodPerMin, Is.Zero);

            Cleanup(em, waveEntity, enemyPoolEntity, arrowPoolEntity, target, archer);
        }

        [UnityTest]
        public IEnumerator ThousandArchers_HoldFireUntilAtomicBulkDeliveryCompletes()
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

            Assert.That(GameManager.Instance.TryBuyArrowRefill(refillPackageCount), Is.True);
            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.True);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero);
            Assert.That(em.GetComponentData<ResourceData>(gameStateEntity).Wood,
                Is.EqualTo(woodBefore - quote.WoodCost));

            for (int frame = 0; frame < 3; frame++)
                yield return null;
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "Pause durumunda teslimat ilerlememelidir.");
            Assert.That(em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount, Is.EqualTo(stoppedRentCount));

            Time.timeScale = 1f;
            for (int frame = 0; frame < 90; frame++)
                yield return null;

            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.True);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "1K Arrow siparisi teslimat surerken kullanilabilir stoga sizmamalidir.");
            Assert.That(em.GetComponentData<ArrowPoolRuntimeData>(
                arrowPoolEntity).TotalRentCount, Is.EqualTo(stoppedRentCount),
                "1K okcu 3 saniyelik teslimat tamamlanmadan ates etmemelidir.");

            int deliveryFrames = 0;
            while (deliveryFrames < 240 && GameManager.Instance.IsArrowRefillDeliveryActive)
            {
                deliveryFrames++;
                yield return null;
            }
            // GameManager siparisin tamamini MonoBehaviour Update'te atomik ekler; ECS shoot
            // sistemi yeni stogu takip eden simulation tick'inde tuketebilir.
            yield return null;

            ArrowPoolRuntimeData poolAfter = em.GetComponentData<ArrowPoolRuntimeData>(arrowPoolEntity);
            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.False,
                "3 simulation saniyelik teslimat tamamlanmadi.");
            Assert.That(poolAfter.TotalRentCount - stoppedRentCount, Is.EqualTo(archerCount),
                "Atomik teslim edilen 1K Arrow sonunda tam 1K pooled gameplay projectile uretmelidir.");
            Assert.That(poolAfter.TotalCreated, Is.EqualTo(poolBefore.TotalCreated),
                "1K atomik teslimat prewarm pool'u genisletmemeli.");
            Assert.That(poolAfter.ExpansionCount, Is.EqualTo(poolBefore.ExpansionCount));
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero,
                "1K gercek projectile tam 1K Arrow tuketmeli.");

            Debug.Log(
                $"[DW-P17-ARROW-DELIVERY-1K] archers={archerCount}; refill_arrows={quote.ArrowAmount}; " +
                $"wood_cost={quote.WoodCost}; delivery_frames={deliveryFrames}; " +
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
        public IEnumerator PendingDelivery_SaveSnapshotFlushesPurchasedArrowsWithoutLoss()
        {
            yield return WaitForRuntime();
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = em.CreateEntityQuery(
                typeof(ArrowSupply), typeof(ResourceData)).GetSingletonEntity();

            ArrowSupply supply = em.GetComponentData<ArrowSupply>(gameStateEntity);
            supply.Current = 0;
            em.SetComponentData(gameStateEntity, supply);
            ResourceData resources = em.GetComponentData<ResourceData>(gameStateEntity);
            resources.Wood = 1_000;
            em.SetComponentData(gameStateEntity, resources);
            yield return null;

            ArrowRefillQuote quote = GameManager.Instance.GetArrowRefillQuote(1);
            Assert.That(quote.IsValid, Is.True);
            Assert.That(GameManager.Instance.TryBuyArrowRefill(1), Is.True);
            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.True);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current, Is.Zero);

            Assert.That(GameManager.Instance.SaveRunSnapshot(), Is.True);
            Assert.That(GameManager.Instance.IsArrowRefillDeliveryActive, Is.False);
            Assert.That(em.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                Is.EqualTo(quote.ArrowAmount));

            RunSaveState saved = RunPersistence.TryLoad();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.ArrowCurrent, Is.EqualTo(quote.ArrowAmount),
                "Snapshot alinirken odemesi yapilmis bekleyen teslimat kaybolmamalidir.");
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
