using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class CouncilEffectGuardPlayModeTests
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

            GameManager previousGameManager = GameManager.Instance;
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            yield return null;
            for (int frame = 0; frame < 120; frame++)
            {
                if (GameManager.Instance != null
                    && !ReferenceEquals(GameManager.Instance, previousGameManager))
                {
                    break;
                }

                yield return null;
            }

            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(ReferenceEquals(GameManager.Instance, previousGameManager), Is.False,
                "Onceki PlayMode testinden kalan GameManager kullanilmamali.");
            bool runtimeReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance.SaveRunSnapshot())
                {
                    runtimeReady = true;
                    break;
                }

                yield return null;
            }

            Assert.That(runtimeReady, Is.True,
                "GameManager/SubScene 300 frame icinde hazir olmadi.");
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
        public IEnumerator Effects_RespectPopulationArcherWallAndCountOnlyGuards()
        {
            GameManager gameManager = GameManager.Instance;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            MethodInfo applyEffects = typeof(GameManager).GetMethod(
                "ApplyCouncilEffects", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyEffects, Is.Not.Null);

            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(ResourceData), typeof(PopulationState), typeof(WaveStateData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(MobilePopulationAllocation),
                typeof(MobileBedCapacityState),
                typeof(MobileEconomyEventState)).GetSingletonEntity();

            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.FoodCostPerArrival = 2;
            entityManager.SetComponentData(configEntity, config);

            var allocation = entityManager.GetComponentData<MobilePopulationAllocation>(configEntity);
            allocation.WoodWorkers = 0;
            allocation.StoneWorkers = 0;
            allocation.IronWorkers = 0;
            allocation.FoodWorkers = 0;
            allocation.IdlePopulation = 59;
            allocation.LastObservedPopulation = 59;
            entityManager.SetComponentData(configEntity, allocation);
            SetCachedProperty(gameManager, nameof(GameManager.PopulationAllocation), allocation);

            var beds = new MobileBedCapacityState { BaseCapacity = 60, PurchasedCapacity = 0 };
            entityManager.SetComponentData(configEntity, beds);
            SetCachedProperty(gameManager, nameof(GameManager.BedCapacity), beds);

            var population = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            population.Total = 59;
            population.Workers = 0;
            population.Archers = 0;
            population.Idle = 59;
            population.BaseCapacity = 60;
            population.Capacity = 60;
            entityManager.SetComponentData(gameStateEntity, population);
            SetCachedProperty(gameManager, nameof(GameManager.Population), population);

            var resources = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            resources.Food = 3;
            entityManager.SetComponentData(gameStateEntity, resources);
            SetCachedProperty(gameManager, nameof(GameManager.Resources), resources);

            var populationOption = new ComposedCouncilOption();
            populationOption.Effects.Add(new ComposedCouncilEffect
            {
                Kind = CouncilEffectKind.GainPopulation,
                Amount = 10
            });
            Assert.That(gameManager.CanAffordCouncilOption(populationOption), Is.False,
                "Kart, exact +10 sonucu yatak/Food karsilamiyorken secilebilir olmamali.");

            applyEffects.Invoke(gameManager, new object[] { populationOption.Effects });
            population = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            resources = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            Assert.That(population.Total, Is.EqualTo(60));
            Assert.That(population.Capacity, Is.EqualTo(60));
            Assert.That(population.BaseCapacity, Is.EqualTo(60));
            Assert.That(resources.Food, Is.EqualTo(1));

            int archerCountBefore = gameManager.GetTotalArcherCount();
            population.Total = archerCountBefore + 2;
            population.Workers = 0;
            population.Archers = archerCountBefore;
            population.Idle = 2;
            entityManager.SetComponentData(gameStateEntity, population);
            SetCachedProperty(gameManager, nameof(GameManager.Population), population);

            allocation.LastObservedPopulation = population.Total;
            allocation.IdlePopulation = 2;
            entityManager.SetComponentData(configEntity, allocation);
            SetCachedProperty(gameManager, nameof(GameManager.PopulationAllocation), allocation);

            var archerOption = new ComposedCouncilOption();
            archerOption.Effects.Add(new ComposedCouncilEffect
            {
                Kind = CouncilEffectKind.GainFreeArchers,
                Amount = 10
            });
            Assert.That(gameManager.CanAffordCouncilOption(archerOption), Is.False,
                "Kart, exact +10 sonucu idle population karsilamiyorken secilebilir olmamali.");

            applyEffects.Invoke(gameManager, new object[] { archerOption.Effects });
            population = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            Assert.That(gameManager.GetTotalArcherCount(), Is.EqualTo(archerCountBefore + 2));
            Assert.That(population.Total, Is.EqualTo(archerCountBefore + 2));
            Assert.That(population.Archers, Is.EqualTo(archerCountBefore + 2));
            Assert.That(population.Idle, Is.Zero);

            Entity wallEntity = entityManager.CreateEntityQuery(typeof(WallSegment)).GetSingletonEntity();
            if (!entityManager.HasComponent<GateComponent>(wallEntity))
                entityManager.AddComponentData(wallEntity, new GateComponent());
            if (!entityManager.HasComponent<CastleHP>(wallEntity))
                entityManager.AddComponentData(wallEntity, new CastleHP());

            var wall = new WallSegment { CurrentHP = 50f, MaxHP = 100f };
            var gate = new GateComponent { CurrentHP = 10f, MaxHP = 100f };
            var core = new CastleHP { CurrentHP = 20f, MaxHP = 200f };
            entityManager.SetComponentData(wallEntity, wall);
            entityManager.SetComponentData(wallEntity, gate);
            entityManager.SetComponentData(wallEntity, core);
            SetCachedProperty(gameManager, nameof(GameManager.Wall), wall);

            applyEffects.Invoke(gameManager, new object[]
            {
                new List<ComposedCouncilEffect>
                {
                    new ComposedCouncilEffect
                    {
                        Kind = CouncilEffectKind.HealDefensePercent,
                        Rate = 0.25f
                    }
                }
            });
            Assert.That(entityManager.GetComponentData<WallSegment>(wallEntity).CurrentHP,
                Is.EqualTo(75f));
            Assert.That(entityManager.GetComponentData<GateComponent>(wallEntity).CurrentHP,
                Is.EqualTo(10f));
            Assert.That(entityManager.GetComponentData<CastleHP>(wallEntity).CurrentHP,
                Is.EqualTo(20f));

            WaveStateData waveBefore = entityManager.GetComponentData<WaveStateData>(gameStateEntity);
            applyEffects.Invoke(gameManager, new object[]
            {
                new List<ComposedCouncilEffect>
                {
                    new ComposedCouncilEffect
                    {
                        Kind = CouncilEffectKind.NextNightSpawnDelta,
                        Rate = 100f
                    }
                }
            });
            MobileEconomyEventState eventState =
                entityManager.GetComponentData<MobileEconomyEventState>(configEntity);
            WaveStateData waveAfter = entityManager.GetComponentData<WaveStateData>(gameStateEntity);
            Assert.That(eventState.NextNightSpawnMultiplier,
                Is.EqualTo(CouncilEffectGuardUtility.MaximumNightCountMultiplier));
            Assert.That(waveAfter.ZombieHP, Is.EqualTo(waveBefore.ZombieHP));
            Assert.That(waveAfter.ZombieDamage, Is.EqualTo(waveBefore.ZombieDamage));
            Assert.That(waveAfter.ZombieSpeed, Is.EqualTo(waveBefore.ZombieSpeed));

            yield return null;
        }

        private static void SetCachedProperty<T>(GameManager gameManager, string propertyName, T value)
        {
            PropertyInfo property = typeof(GameManager).GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            property.SetValue(gameManager, value);
        }
    }
}
