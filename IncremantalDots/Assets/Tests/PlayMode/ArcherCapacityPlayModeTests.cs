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
    public class ArcherCapacityPlayModeTests
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

            Assert.That(GameManager.Instance, Is.Not.Null,
                "NewGameScene GameManager olusturmadi.");
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
        public IEnumerator CommonCap_BlocksPurchaseCouncilRestoreAndCentralSpawnWithoutSpending()
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
            Assert.That(runtimeReady, Is.True,
                "GameManager/SubScene 300 frame icinde hazir olmadi.");

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            int initialCount = gameManager.GetTotalArcherCount();
            Assert.That(initialCount, Is.LessThanOrEqualTo(999));

            for (int i = initialCount; i < 999; i++)
                entityManager.CreateEntity(typeof(ArcherUnit));

            MethodInfo spawnArcher = typeof(GameManager).GetMethod(
                "SpawnArcher", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo applyCouncilEffects = typeof(GameManager).GetMethod(
                "ApplyCouncilEffects", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo restoreCounts = typeof(GameManager).GetMethod(
                "RestoreArcherCountsWithinCapacity", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(spawnArcher, Is.Not.Null);
            Assert.That(applyCouncilEffects, Is.Not.Null);
            Assert.That(restoreCounts, Is.Not.Null);

            Assert.That((bool)spawnArcher.Invoke(
                gameManager, new object[] { ArcherType.Basic }), Is.True,
                "1000. okcu ortak spawn owner'i tarafindan kabul edilmeliydi.");
            Assert.That(gameManager.GetTotalArcherCount(), Is.EqualTo(1000));
            Assert.That(gameManager.GetRemainingArcherCapacity(), Is.Zero);

            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(ResourceData), typeof(PopulationState)).GetSingletonEntity();
            ResourceData resourcesBefore = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            PopulationState populationBefore = entityManager.GetComponentData<PopulationState>(gameStateEntity);

            Assert.That(gameManager.CanBuyArcher(ArcherType.Basic), Is.False);
            Assert.That(gameManager.BuyArcher(ArcherType.Basic), Is.False);
            Assert.That((bool)spawnArcher.Invoke(
                gameManager, new object[] { ArcherType.Rapid }), Is.False);

            var councilEffects = new List<ComposedCouncilEffect>
            {
                new ComposedCouncilEffect
                {
                    Kind = CouncilEffectKind.GainFreeArchers,
                    Amount = int.MaxValue
                }
            };
            applyCouncilEffects.Invoke(gameManager, new object[] { councilEffects });
            restoreCounts.Invoke(gameManager,
                new object[] { int.MaxValue, int.MaxValue, int.MaxValue });

            Assert.That(gameManager.GetTotalArcherCount(), Is.EqualTo(1000));
            ResourceData resourcesAfter = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            PopulationState populationAfter = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            Assert.That(resourcesAfter.Wood, Is.EqualTo(resourcesBefore.Wood));
            Assert.That(resourcesAfter.Stone, Is.EqualTo(resourcesBefore.Stone));
            Assert.That(resourcesAfter.Iron, Is.EqualTo(resourcesBefore.Iron));
            Assert.That(resourcesAfter.Food, Is.EqualTo(resourcesBefore.Food));
            Assert.That(populationAfter.Total, Is.EqualTo(populationBefore.Total));
            Assert.That(populationAfter.Workers, Is.EqualTo(populationBefore.Workers));
            Assert.That(populationAfter.Archers, Is.EqualTo(populationBefore.Archers));
            Assert.That(populationAfter.Idle, Is.EqualTo(populationBefore.Idle));

            var cleanupQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(ArcherUnit) },
                None = new ComponentType[] { typeof(Prefab) }
            });
            entityManager.DestroyEntity(cleanupQuery);
            yield return null;
        }
    }
}
