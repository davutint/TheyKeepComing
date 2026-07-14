using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class ArcherRetrainPlayModeTests
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
        public IEnumerator RetrainButtons_ConvertBasicToRapidAndFrostWithoutPopulationChange()
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
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Basic), Is.GreaterThan(0));

            MethodInfo unlockFromTech = typeof(GameManager).GetMethod(
                "UnlockArcherTypeFromTech", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(unlockFromTech, Is.Not.Null);
            unlockFromTech.Invoke(gameManager, new object[] { ArcherType.Rapid });

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(
                typeof(ResourceData), typeof(PopulationState)).GetSingletonEntity();
            var fundedResources = new ResourceData
            {
                Wood = 1_000_000,
                Stone = 1_000_000,
                Iron = 1_000_000,
                Food = 1_000_000
            };
            entityManager.SetComponentData(gameStateEntity, fundedResources);
            yield return null;

            int totalBefore = gameManager.GetTotalArcherCount();
            int basicBefore = gameManager.GetArcherTypeCount(ArcherType.Basic);
            int rapidBefore = gameManager.GetArcherTypeCount(ArcherType.Rapid);
            PopulationState populationBefore = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            ResourceCost retrainCost = gameManager.GetArcherRetrainCost(ArcherType.Rapid);
            ResourceCost buyCostBefore = gameManager.GetArcherBuyCost(ArcherType.Rapid);

            Assert.That(gameManager.CanRetrainBasicArcher(ArcherType.Basic), Is.False);
            Assert.That(gameManager.CanRetrainBasicArcher(ArcherType.Rapid), Is.True);

            MarketUI market = Object.FindFirstObjectByType<MarketUI>(FindObjectsInactive.Include);
            Assert.That(market, Is.Not.Null);
            market.Refresh();
            Button retrainButton = FindButton(
                market.gameObject, "ArcherRecruitmentRow_rapid_archer", "ArcherRetrainButton");
            Assert.That(retrainButton, Is.Not.Null,
                "Rapid dynamic row retrain butonu olusturmadi.");
            Assert.That(retrainButton.gameObject.activeSelf, Is.True);
            Assert.That(retrainButton.interactable, Is.True);

            retrainButton.onClick.Invoke();
            yield return null;

            Assert.That(gameManager.GetTotalArcherCount(), Is.EqualTo(totalBefore));
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Basic), Is.EqualTo(basicBefore - 1));
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Rapid), Is.EqualTo(rapidBefore + 1));

            PopulationState populationAfter = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            AssertPopulationUnchanged(populationBefore, populationAfter);

            ResourceData resourcesAfter = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            Assert.That(resourcesAfter.Wood, Is.EqualTo(fundedResources.Wood - retrainCost.Wood));
            Assert.That(resourcesAfter.Stone, Is.EqualTo(fundedResources.Stone - retrainCost.Stone));
            Assert.That(resourcesAfter.Iron, Is.EqualTo(fundedResources.Iron - retrainCost.Iron));
            Assert.That(resourcesAfter.Food, Is.EqualTo(fundedResources.Food - retrainCost.Food));

            ResourceCost buyCostAfter = gameManager.GetArcherBuyCost(ArcherType.Rapid);
            ResourceCost retrainCostAfter = gameManager.GetArcherRetrainCost(ArcherType.Rapid);
            Assert.That(buyCostAfter.Wood, Is.GreaterThan(buyCostBefore.Wood));
            Assert.That(retrainCostAfter.Wood, Is.GreaterThan(retrainCost.Wood));

            unlockFromTech.Invoke(gameManager, new object[] { ArcherType.Frost });
            market.Refresh();
            int basicBeforeFrost = gameManager.GetArcherTypeCount(ArcherType.Basic);
            int frostBefore = gameManager.GetArcherTypeCount(ArcherType.Frost);
            PopulationState populationBeforeFrost = entityManager.GetComponentData<PopulationState>(gameStateEntity);
            Button frostRetrainButton = FindButton(
                market.gameObject, "ArcherRecruitmentRow_frost_archer", "ArcherRetrainButton");
            Assert.That(frostRetrainButton, Is.Not.Null);
            Assert.That(frostRetrainButton.interactable, Is.True);

            frostRetrainButton.onClick.Invoke();
            yield return null;

            Assert.That(gameManager.GetTotalArcherCount(), Is.EqualTo(totalBefore));
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Basic), Is.EqualTo(basicBeforeFrost - 1));
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Frost), Is.EqualTo(frostBefore + 1));
            AssertPopulationUnchanged(
                populationBeforeFrost,
                entityManager.GetComponentData<PopulationState>(gameStateEntity));
        }

        private static Button FindButton(GameObject root, string rowName, string buttonName)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            Transform row = null;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == rowName)
                {
                    row = transforms[i];
                    break;
                }
            }

            if (row == null)
                return null;

            Transform button = row.Find(buttonName);
            return button != null ? button.GetComponent<Button>() : null;
        }

        private static void AssertPopulationUnchanged(
            PopulationState before, PopulationState after)
        {
            Assert.That(after.Total, Is.EqualTo(before.Total));
            Assert.That(after.Workers, Is.EqualTo(before.Workers));
            Assert.That(after.Archers, Is.EqualTo(before.Archers));
            Assert.That(after.Idle, Is.EqualTo(before.Idle));
            Assert.That(after.Capacity, Is.EqualTo(before.Capacity));
            Assert.That(after.BaseCapacity, Is.EqualTo(before.BaseCapacity));
            Assert.That(after.FoodPerAssignedPerMin, Is.EqualTo(before.FoodPerAssignedPerMin));
        }
    }
}
