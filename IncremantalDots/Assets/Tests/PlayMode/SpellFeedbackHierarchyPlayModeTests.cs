using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DeadWalls.Tests
{
    public class SpellFeedbackHierarchyPlayModeTests
    {
        private const int EnemyTarget = 10_000;
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
                DestroyEntitiesWith<FireballProjectile>(entityManager);
                DestroyEntitiesWith<FireballStrike>(entityManager);
                DestroyEntitiesWith<FireballDelayedBlast>(entityManager);
                DestroyEntitiesWith<FireballBurningGround>(entityManager);
            }
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FireballAndFrostHierarchy_RemainsOrderedInsideTenThousandEnemyHorde()
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
            Assert.That(runtimeReady, Is.True);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(GameStateData)).GetSingletonEntity();
            GameStateData gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            gameState.IsLevelUpPending = false;
            entityManager.SetComponentData(gameStateEntity, gameState);

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
            MobileCastleCombatConfig config =
                entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MaxAliveZombies = EnemyTarget;
            config.StressMaxAliveZombies = EnemyTarget;
            entityManager.SetComponentData(configEntity, config);

            EnemyCatalogRuntimeData catalog =
                entityManager.GetComponentData<EnemyCatalogRuntimeData>(poolEntity);
            DynamicBuffer<EnemyCatalogEntryData> entries =
                entityManager.GetBuffer<EnemyCatalogEntryData>(poolEntity, true);
            int activeEntryIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(
                catalog,
                entries.Length);
            Assert.That(activeEntryIndex, Is.GreaterThanOrEqualTo(0));
            EnemyCatalogEntryData definition = entries[activeEntryIndex];

            for (int i = 0; i < EnemyTarget; i++)
            {
                Assert.That(EnemyPoolRuntimeUtility.TryRent(
                    entityManager,
                    poolEntity,
                    out Entity zombie), Is.True);
                int column = i % 100;
                int row = i / 100;
                float3 position = new float3(
                    10f + column * 0.12f,
                    -6.5f + row * 0.13f,
                    MobileCastleRenderDepth.UnitZ);
                entityManager.SetComponentData(zombie,
                    LocalTransform.FromPositionRotationScale(
                        position,
                        quaternion.identity,
                        definition.Scale));
                entityManager.SetComponentData(zombie, new ZombieStats
                {
                    MoveSpeed = 0f,
                    MaxHP = 1_000_000f,
                    CurrentHP = 1_000_000f,
                    AttackDamage = definition.BaseDamage,
                    AttackCooldown = 1f,
                    XPReward = definition.XPReward
                });
                entityManager.SetComponentData(zombie,
                    new ZombieState { Value = ZombieStateType.Moving });
            }

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = EnemyTarget;
            wave.SpawnTimer = 999f;
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);
            Time.timeScale = 0f;

            SpellCastUI spell = Object.FindFirstObjectByType<SpellCastUI>();
            CombatFeedbackBridge bridge =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(spell, Is.Not.Null);
            Assert.That(bridge, Is.Not.Null);
            Assert.That(spell.FireballBlastSortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballBlastSortingOrder));
            Assert.That(bridge.FrostHitSortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FrostHitSortingOrder));

            using EntityQuery vfxQuery = entityManager.CreateEntityQuery(
                typeof(CombatVfxEvent));
            if (!vfxQuery.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(vfxQuery);
            FieldInfo hitVfxTimeField = typeof(CombatFeedbackBridge).GetField(
                "_lastHitVfxPlaybackTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(hitVfxTimeField, Is.Not.Null);
            hitVfxTimeField.SetValue(bridge, -999f);

            const int frostHitCount = 6;
            const int ordinaryHitCount = 6;
            for (int i = 0; i < frostHitCount + ordinaryHitCount; i++)
            {
                bool frost = i < frostHitCount;
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new CombatVfxEvent
                {
                    Position = new float3(13f + i * 0.55f, -1.5f + (i % 3) * 1.5f, 0f),
                    Direction = new float3(1f, 0f, 0f),
                    Type = frost ? CombatVfxType.FrostHit : CombatVfxType.ArrowHit,
                    Scale = frost ? 0.11f : 0.08f
                });
            }

            PropertyInfo activeFireballProperty = typeof(GameManager).GetProperty(
                nameof(GameManager.ActiveFireballProjectile),
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo activeFireballSetter = activeFireballProperty?.GetSetMethod(true);
            Assert.That(activeFireballSetter, Is.Not.Null);
            Entity projectile = entityManager.CreateEntity(
                typeof(FireballProjectile),
                typeof(LocalTransform));
            entityManager.SetComponentData(projectile, new FireballProjectile
            {
                Target = new float2(13f, -4f),
                Speed = 0.01f,
                Radius = 2.2f,
                Damage = 60f
            });
            entityManager.SetComponentData(projectile,
                LocalTransform.FromPosition(new float3(13f, 4f, 0f)));
            activeFireballSetter.Invoke(gameManager, new object[] { projectile });

            yield return null;

            Assert.That(bridge.LastFrameFrostHitVfxPlayedCount,
                Is.EqualTo(frostHitCount));
            Assert.That(bridge.ActiveHitFlipbookCount,
                Is.EqualTo(frostHitCount + ordinaryHitCount));
            AssertFrostHierarchy(bridge, frostHitCount, ordinaryHitCount);

            GameObject projectileVisual = GameObject.Find("FireballProjectileVisual");
            GameObject projectileAura = GameObject.Find("FireballProjectileHierarchyAura");
            Assert.That(projectileVisual, Is.Not.Null);
            Assert.That(projectileAura, Is.Not.Null);
            SpriteRenderer projectileRenderer = projectileVisual.GetComponent<SpriteRenderer>();
            SpriteRenderer auraRenderer = projectileAura.GetComponent<SpriteRenderer>();
            Assert.That(projectileRenderer.sortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballProjectileSortingOrder));
            Assert.That(auraRenderer.sortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballProjectileAuraSortingOrder));
            Assert.That(auraRenderer.bounds.size.x,
                Is.GreaterThan(projectileRenderer.bounds.size.x));
            Assert.That(projectileVisual.transform.position.z,
                Is.EqualTo(MobileCastleRenderDepth.ProjectileZ).Within(0.001f));

            entityManager.DestroyEntity(projectile);
            yield return null;

            GameObject blastVisual = GameObject.Find("FireballBlastVisual");
            GameObject blastCore = GameObject.Find("FireballBlastHierarchyCore");
            GameObject blastRing = GameObject.Find("FireballBlastHierarchyRing");
            Assert.That(blastVisual, Is.Not.Null);
            Assert.That(blastCore, Is.Not.Null);
            Assert.That(blastRing, Is.Not.Null);
            SpriteRenderer blastRenderer = blastVisual.GetComponent<SpriteRenderer>();
            SpriteRenderer blastCoreRenderer = blastCore.GetComponent<SpriteRenderer>();
            SpriteRenderer blastRingRenderer = blastRing.GetComponent<SpriteRenderer>();
            Assert.That(blastRenderer.sortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballBlastSortingOrder));
            Assert.That(blastRingRenderer.sortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballBlastRingSortingOrder));
            Assert.That(blastCoreRenderer.sortingOrder,
                Is.EqualTo(SpellFeedbackHierarchy.FireballBlastCoreSortingOrder));
            Assert.That(blastCoreRenderer.sortingOrder,
                Is.GreaterThan(blastRenderer.sortingOrder));
            Assert.That(blastRingRenderer.sortingOrder,
                Is.GreaterThan(blastCoreRenderer.sortingOrder));
            Assert.That(blastCoreRenderer.color.r,
                Is.GreaterThan(blastCoreRenderer.color.b));
            Assert.That(blastRingRenderer.bounds.size.x,
                Is.GreaterThan(blastRenderer.bounds.size.x));
            Assert.That(blastRenderer.sortingOrder,
                Is.GreaterThan(bridge.FrostHitSortingOrder));
            Assert.That(blastVisual.transform.position.z,
                Is.EqualTo(MobileCastleRenderDepth.ProjectileZ).Within(0.001f));
            Assert.That(blastCore.transform.position.z,
                Is.EqualTo(MobileCastleRenderDepth.ProjectileZ).Within(0.001f));
            Assert.That(blastRing.transform.position.z,
                Is.EqualTo(MobileCastleRenderDepth.ProjectileZ).Within(0.001f));

            string capturePath = Path.Combine(
                Application.temporaryCachePath,
                "DW_I_SPELL_HIERARCHY_10K.png");
            if (File.Exists(capturePath))
                File.Delete(capturePath);
            ScreenCapture.CaptureScreenshot(capturePath);
            yield return new WaitForEndOfFrame();
            for (int frame = 0; frame < 30 && !File.Exists(capturePath); frame++)
                yield return null;

            Assert.That(File.Exists(capturePath), Is.True);
            Assert.That(new FileInfo(capturePath).Length, Is.GreaterThan(1024));
            Debug.Log(
                $"[DW-I-SPELL-HIERARCHY] enemy={EnemyTarget}; " +
                $"frost_hits={frostHitCount}; ordinary_hits={ordinaryHitCount}; " +
                $"frost_order={bridge.FrostHitSortingOrder}; " +
                $"projectile_order={projectileRenderer.sortingOrder}; " +
                $"blast_order={blastRenderer.sortingOrder}; capture={capturePath}");

            activeFireballSetter.Invoke(gameManager, new object[] { Entity.Null });
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator FireballEvolutions_ApplyExactAggregateDamageAndFixedGroundPresentation()
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
            Assert.That(runtimeReady, Is.True);
            Assert.That(gameManager.TryEnableDevelopmentCombat(out string unlockMessage),
                Is.True,
                unlockMessage);

            gameManager.EnableBehaviorEffect(new HeartNodeEffect
            {
                Type = HeartNodeEffectType.EnableBurningGround
            });
            gameManager.EnableBehaviorEffect(new HeartNodeEffect
            {
                Type = HeartNodeEffectType.EnableSecondBlast
            });
            Assert.That(gameManager.FireballEvolutions,
                Is.EqualTo(FireballEvolutionFlags.BurningGround
                           | FireballEvolutionFlags.SecondBlast));

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var target = new Vector2(8f, -2f);
            const float initialHp = 10_000f;
            Entity victim = entityManager.CreateEntity(
                typeof(ZombieTag),
                typeof(ZombieStats),
                typeof(LocalTransform));
            entityManager.SetComponentData(victim, new ZombieStats
            {
                MaxHP = initialHp,
                CurrentHP = initialHp,
                MoveSpeed = 0f,
                AttackDamage = 0f,
                AttackCooldown = 1f,
                XPReward = 0
            });
            entityManager.SetComponentData(victim,
                LocalTransform.FromPosition(new float3(target.x, target.y, 0f)));

            Assert.That(gameManager.TryCastFireball(target), Is.True);
            Entity projectileEntity = gameManager.ActiveFireballProjectile;
            Assert.That(entityManager.Exists(projectileEntity), Is.True);
            FireballProjectile projectile =
                entityManager.GetComponentData<FireballProjectile>(projectileEntity);
            Assert.That(projectile.Evolutions,
                Is.EqualTo(FireballEvolutionFlags.BurningGround
                           | FireballEvolutionFlags.SecondBlast));
            entityManager.SetComponentData(projectileEntity,
                LocalTransform.FromPosition(new float3(target.x, target.y, 0f)));

            using EntityQuery delayedQuery = entityManager.CreateEntityQuery(
                typeof(FireballDelayedBlast));
            using EntityQuery groundQuery = entityManager.CreateEntityQuery(
                typeof(FireballBurningGround));
            for (int frame = 0;
                 frame < 60 && (delayedQuery.IsEmptyIgnoreFilter || groundQuery.IsEmptyIgnoreFilter);
                 frame++)
            {
                yield return null;
            }

            Assert.That(delayedQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(groundQuery.CalculateEntityCount(), Is.EqualTo(1));
            FireballDelayedBlast delayed = delayedQuery.GetSingleton<FireballDelayedBlast>();
            FireballBurningGround ground = groundQuery.GetSingleton<FireballBurningGround>();
            Assert.That(delayed.Damage,
                Is.EqualTo(projectile.Damage
                           * FireballEvolutionRules.SecondBlastDamageMultiplier).Within(0.001f));
            Assert.That(delayed.Radius,
                Is.EqualTo(projectile.Radius
                           * FireballEvolutionRules.SecondBlastRadiusMultiplier).Within(0.001f));
            Assert.That(ground.DamagePerTick,
                Is.EqualTo(projectile.Damage
                           * FireballEvolutionRules.BurningGroundDamageMultiplierPerTick)
                    .Within(0.001f));
            Assert.That(ground.Radius,
                Is.EqualTo(projectile.Radius
                           * FireballEvolutionRules.BurningGroundRadiusMultiplier).Within(0.001f));
            Assert.That(ground.RemainingTicks,
                Is.EqualTo(FireballEvolutionRules.BurningGroundTickCount));

            SpellCastUI spell = Object.FindFirstObjectByType<SpellCastUI>();
            Assert.That(spell, Is.Not.Null);
            for (int frame = 0;
                 frame < 30 && spell.ActiveBurningGroundVisualCount == 0;
                 frame++)
            {
                yield return null;
            }
            Assert.That(spell.ActiveBurningGroundVisualCount, Is.EqualTo(1));
            Assert.That(entityManager.GetComponentData<ZombieStats>(victim).CurrentHP,
                Is.EqualTo(initialHp - projectile.Damage).Within(0.05f));

            Time.timeScale = 20f;
            for (int frame = 0;
                 frame < 180 && (!delayedQuery.IsEmptyIgnoreFilter || !groundQuery.IsEmptyIgnoreFilter);
                 frame++)
            {
                yield return null;
            }
            yield return null;
            yield return null;
            Time.timeScale = 1f;

            Assert.That(delayedQuery.IsEmptyIgnoreFilter, Is.True);
            Assert.That(groundQuery.IsEmptyIgnoreFilter, Is.True);
            float totalDamageMultiplier = 1f
                                          + FireballEvolutionRules.SecondBlastDamageMultiplier
                                          + FireballEvolutionRules.BurningGroundTickCount
                                          * FireballEvolutionRules.BurningGroundDamageMultiplierPerTick;
            Assert.That(entityManager.GetComponentData<ZombieStats>(victim).CurrentHP,
                Is.EqualTo(initialHp - projectile.Damage * totalDamageMultiplier).Within(0.1f));
            Assert.That(spell.ActiveBurningGroundVisualCount, Is.Zero);

            if (entityManager.Exists(victim))
                entityManager.DestroyEntity(victim);
        }

        [UnityTest]
        public IEnumerator FireballEvolutionRuntimeState_SaveAndContinueRestoresExactTimersAndTicks()
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
            Assert.That(runtimeReady, Is.True);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity delayedEntity = entityManager.CreateEntity(typeof(FireballDelayedBlast));
            entityManager.SetComponentData(delayedEntity, new FireballDelayedBlast
            {
                Position = new float2(7f, -1f),
                Radius = 1.87f,
                Damage = 36f,
                RemainingDelay = 0.42f
            });
            Entity groundEntity = entityManager.CreateEntity(typeof(FireballBurningGround));
            entityManager.SetComponentData(groundEntity, new FireballBurningGround
            {
                Position = new float2(7f, -1f),
                Radius = 1.54f,
                DamagePerTick = 7.2f,
                RemainingDuration = 3.6f,
                TimeUntilNextTick = 0.6f,
                RemainingTicks = 4
            });

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState saved = RunPersistence.TryLoad();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.ActiveFireballDelayedBlasts, Has.Count.EqualTo(1));
            Assert.That(saved.ActiveFireballBurningGrounds, Has.Count.EqualTo(1));
            Assert.That(saved.ActiveFireballDelayedBlasts[0].RemainingDelay,
                Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(saved.ActiveFireballBurningGrounds[0].RemainingDuration,
                Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(saved.ActiveFireballBurningGrounds[0].TimeUntilNextTick,
                Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(saved.ActiveFireballBurningGrounds[0].RemainingTicks, Is.EqualTo(4));

            entityManager.DestroyEntity(delayedEntity);
            entityManager.DestroyEntity(groundEntity);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            using EntityQuery delayedQuery = entityManager.CreateEntityQuery(
                typeof(FireballDelayedBlast));
            using EntityQuery groundQuery = entityManager.CreateEntityQuery(
                typeof(FireballBurningGround));
            Assert.That(delayedQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(groundQuery.CalculateEntityCount(), Is.EqualTo(1));
            FireballDelayedBlast restoredDelayed =
                delayedQuery.GetSingleton<FireballDelayedBlast>();
            FireballBurningGround restoredGround =
                groundQuery.GetSingleton<FireballBurningGround>();
            Assert.That(restoredDelayed.RemainingDelay, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(restoredGround.RemainingDuration, Is.EqualTo(3.6f).Within(0.001f));
            Assert.That(restoredGround.TimeUntilNextTick, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(restoredGround.RemainingTicks, Is.EqualTo(4));
        }

        private static void AssertFrostHierarchy(
            CombatFeedbackBridge bridge,
            int expectedFrostCount,
            int expectedOrdinaryCount)
        {
            SpriteRenderer[] renderers = bridge.GetComponentsInChildren<SpriteRenderer>(true);
            int frostMainCount = 0;
            int frostRingCount = 0;
            int ordinaryMainCount = 0;
            float largestFrostScale = 0f;
            float largestOrdinaryScale = 0f;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (renderer.gameObject.name == "FrostHierarchyRing")
                {
                    frostRingCount++;
                    Assert.That(renderer.sortingOrder,
                        Is.EqualTo(SpellFeedbackHierarchy.FrostHitSortingOrder - 1));
                    continue;
                }

                if (!renderer.gameObject.name.StartsWith("HitFlipbook_"))
                    continue;

                if (renderer.sortingOrder == SpellFeedbackHierarchy.FrostHitSortingOrder)
                {
                    frostMainCount++;
                    largestFrostScale = Mathf.Max(
                        largestFrostScale,
                        renderer.transform.localScale.x);
                }
                else if (renderer.sortingOrder == SpellFeedbackHierarchy.OrdinaryHitSortingOrder)
                {
                    ordinaryMainCount++;
                    largestOrdinaryScale = Mathf.Max(
                        largestOrdinaryScale,
                        renderer.transform.localScale.x);
                }
            }

            Assert.That(frostMainCount, Is.EqualTo(expectedFrostCount));
            Assert.That(frostRingCount, Is.EqualTo(expectedFrostCount));
            Assert.That(ordinaryMainCount, Is.EqualTo(expectedOrdinaryCount));
            Assert.That(largestFrostScale, Is.GreaterThan(largestOrdinaryScale * 3f));
        }

        private static void DestroyEntitiesWith<T>(EntityManager entityManager)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<T>());
            if (!query.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(query);
        }
    }
}
