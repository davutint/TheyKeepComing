using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class WorkerAllocationPlayModeTests
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
            yield return null;
            for (int frame = 0; frame < 120 && GameManager.Instance == null; frame++)
                yield return null;

            Assert.That(GameManager.Instance, Is.Not.Null, "NewGameScene GameManager olusturmadi.");

            int stableFrames = 0;
            for (int frame = 0; frame < 300; frame++)
            {
                World world = World.DefaultGameObjectInjectionWorld;
                bool frameReady = false;
                if (world != null && world.IsCreated)
                {
                    EntityManager entityManager = world.EntityManager;
                    using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                        typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
                    using EntityQuery populationQuery = entityManager.CreateEntityQuery(typeof(PopulationState));
                    frameReady = allocationQuery.CalculateEntityCount() == 1
                        && populationQuery.CalculateEntityCount() == 1;
                }

                stableFrames = frameReady ? stableFrames + 1 : 0;
                if (stableFrames >= 5)
                    break;
                yield return null;
            }
            Assert.That(stableFrames, Is.GreaterThanOrEqualTo(5),
                "Worker allocation singleton'lari 5 ardisik frame stabil olmadi.");
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
        public IEnumerator PopulationIncrease_AssignsOnlyNewPeopleToTarget_AndLeavesCapOverflowIdle()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
            using EntityQuery populationQuery = entityManager.CreateEntityQuery(typeof(PopulationState));
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            Entity populationEntity = populationQuery.GetSingletonEntity();

            var population = entityManager.GetComponentData<PopulationState>(populationEntity);
            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(allocationEntity);
            var allocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            int initialWood = allocation.WoodWorkers;
            int initialStone = allocation.StoneWorkers;
            int initialIron = allocation.IronWorkers;
            int initialFood = allocation.FoodWorkers;
            int initialIdle = population.Idle;

            config.FoodWorkerCap = initialFood + 5;
            entityManager.SetComponentData(allocationEntity, config);
            allocation.WoodTargetRatioBps = 0;
            allocation.StoneTargetRatioBps = 0;
            allocation.IronTargetRatioBps = 0;
            allocation.FoodTargetRatioBps = WorkerAllocationUtility.RatioScale;
            allocation.LastObservedPopulation = population.Total;
            allocation.AutoAllocationInitialized = 1;
            entityManager.SetComponentData(allocationEntity, allocation);

            population.Total += 5;
            entityManager.SetComponentData(populationEntity, population);
            yield return null;
            yield return null;

            allocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            population = entityManager.GetComponentData<PopulationState>(populationEntity);
            Assert.That(allocation.WoodWorkers, Is.EqualTo(initialWood));
            Assert.That(allocation.StoneWorkers, Is.EqualTo(initialStone));
            Assert.That(allocation.IronWorkers, Is.EqualTo(initialIron));
            Assert.That(allocation.FoodWorkers, Is.EqualTo(initialFood + 5));
            Assert.That(population.Workers,
                Is.EqualTo(initialWood + initialStone + initialIron + initialFood + 5));
            Assert.That(population.Idle, Is.EqualTo(initialIdle));

            config = entityManager.GetComponentData<MobileCastleCombatConfig>(allocationEntity);
            config.FoodWorkerCap = allocation.FoodWorkers;
            entityManager.SetComponentData(allocationEntity, config);
            allocation.LastObservedPopulation = population.Total;
            entityManager.SetComponentData(allocationEntity, allocation);

            population.Total += 3;
            entityManager.SetComponentData(populationEntity, population);
            yield return null;
            yield return null;

            var cappedAllocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            var cappedPopulation = entityManager.GetComponentData<PopulationState>(populationEntity);
            Assert.That(cappedAllocation.FoodWorkers, Is.EqualTo(allocation.FoodWorkers));
            Assert.That(cappedPopulation.Workers, Is.EqualTo(population.Workers));
            Assert.That(cappedPopulation.Idle, Is.EqualTo(population.Idle + 3));
        }

        [UnityTest]
        public IEnumerator WorkerDrawer_TargetButtonsAndDirectInputChangeRatiosWithoutMovingWorkers()
        {
            WorkerEconomyDrawerUI drawer = Object.FindFirstObjectByType<WorkerEconomyDrawerUI>();
            Assert.That(drawer, Is.Not.Null);
            Assert.That(drawer.WoodWorkerAddButton, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetPlus10Button, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetPlus100Button, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetInput, Is.Not.Null);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            var before = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            int expectedWoodTarget = Mathf.Min(WorkerAllocationUtility.RatioScale,
                before.WoodTargetRatioBps + 1000);

            drawer.WoodWorkerTargetPlus10Button.onClick.Invoke();
            yield return null;

            var afterQuickAdd = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            Assert.That(afterQuickAdd.WoodTargetRatioBps, Is.EqualTo(expectedWoodTarget));
            Assert.That(TargetRatioTotal(afterQuickAdd), Is.EqualTo(WorkerAllocationUtility.RatioScale));
            AssertWorkerCountsEqual(before, afterQuickAdd);

            drawer.WoodWorkerTargetInput.onEndEdit.Invoke("25");
            yield return null;

            var afterDirectInput = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            Assert.That(afterDirectInput.WoodTargetRatioBps, Is.EqualTo(2500));
            Assert.That(TargetRatioTotal(afterDirectInput), Is.EqualTo(WorkerAllocationUtility.RatioScale));
            AssertWorkerCountsEqual(before, afterDirectInput);

            drawer.WoodWorkerAddButton.onClick.Invoke();
            yield return null;

            var afterPlusOne = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            Assert.That(afterPlusOne.WoodTargetRatioBps, Is.EqualTo(2600));
            Assert.That(TargetRatioTotal(afterPlusOne), Is.EqualTo(WorkerAllocationUtility.RatioScale));
            AssertWorkerCountsEqual(before, afterPlusOne);

            drawer.WoodWorkerTargetPlus100Button.onClick.Invoke();
            yield return null;

            var afterPlusHundred = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            Assert.That(afterPlusHundred.WoodTargetRatioBps,
                Is.EqualTo(WorkerAllocationUtility.RatioScale));
            Assert.That(afterPlusHundred.StoneTargetRatioBps, Is.Zero);
            Assert.That(afterPlusHundred.IronTargetRatioBps, Is.Zero);
            Assert.That(afterPlusHundred.FoodTargetRatioBps, Is.Zero);
            AssertWorkerCountsEqual(before, afterPlusHundred);
        }

        [UnityTest]
        public IEnumerator WorkerVisuals_UseRepresentativeDensityWithoutChangingActualAllocation()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
            using EntityQuery populationQuery = entityManager.CreateEntityQuery(typeof(PopulationState));
            using EntityQuery waveQuery = entityManager.CreateEntityQuery(typeof(WaveStateData));
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            Entity populationEntity = populationQuery.GetSingletonEntity();
            Entity waveEntity = waveQuery.GetSingletonEntity();

            var wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            entityManager.SetComponentData(waveEntity, wave);

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 12);
            yield return WaitForWorkerVisualCount(entityManager, EconomyFocusType.Wood, 12);
            Assert.That(ReadWoodWorkerCount(entityManager, allocationEntity), Is.EqualTo(12));
            Assert.That(CountWorkerVisuals(entityManager, EconomyFocusType.Stone), Is.Zero);
            Assert.That(CountWorkerVisuals(entityManager, EconomyFocusType.Iron), Is.Zero);
            Assert.That(CountWorkerVisuals(entityManager, EconomyFocusType.Food), Is.Zero);

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 60);
            yield return WaitForWorkerVisualCount(entityManager, EconomyFocusType.Wood, 24);
            Assert.That(ReadWoodWorkerCount(entityManager, allocationEntity), Is.EqualTo(60));

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 1000);
            yield return WaitForWorkerVisualCount(entityManager, EconomyFocusType.Wood, 32);
            Assert.That(ReadWoodWorkerCount(entityManager, allocationEntity), Is.EqualTo(1000));

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 5000);
            yield return null;
            Assert.That(ReadWoodWorkerCount(entityManager, allocationEntity), Is.EqualTo(5000));
            Assert.That(CountWorkerVisuals(entityManager, EconomyFocusType.Wood), Is.EqualTo(32));

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 0);
            yield return WaitForWorkerVisualCount(entityManager, EconomyFocusType.Wood, 0);
            Assert.That(ReadWoodWorkerCount(entityManager, allocationEntity), Is.Zero);
        }

        private static int TargetRatioTotal(MobilePopulationAllocation allocation)
        {
            return allocation.WoodTargetRatioBps
                + allocation.StoneTargetRatioBps
                + allocation.IronTargetRatioBps
                + allocation.FoodTargetRatioBps;
        }

        private static void AssertWorkerCountsEqual(MobilePopulationAllocation expected,
            MobilePopulationAllocation actual)
        {
            Assert.That(actual.WoodWorkers, Is.EqualTo(expected.WoodWorkers));
            Assert.That(actual.StoneWorkers, Is.EqualTo(expected.StoneWorkers));
            Assert.That(actual.IronWorkers, Is.EqualTo(expected.IronWorkers));
            Assert.That(actual.FoodWorkers, Is.EqualTo(expected.FoodWorkers));
        }

        private static void SetWoodWorkerCount(EntityManager entityManager, Entity allocationEntity,
            Entity populationEntity, int woodWorkers)
        {
            var population = entityManager.GetComponentData<PopulationState>(populationEntity);
            population.Total = woodWorkers + population.Archers;
            population.Workers = woodWorkers;
            population.Idle = 0;
            population.Capacity = Mathf.Max(population.Capacity, population.Total);
            population.BaseCapacity = Mathf.Max(population.BaseCapacity, population.Capacity);
            entityManager.SetComponentData(populationEntity, population);

            var allocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            allocation.WoodWorkers = woodWorkers;
            allocation.StoneWorkers = 0;
            allocation.IronWorkers = 0;
            allocation.FoodWorkers = 0;
            allocation.WoodTargetRatioBps = WorkerAllocationUtility.RatioScale;
            allocation.StoneTargetRatioBps = 0;
            allocation.IronTargetRatioBps = 0;
            allocation.FoodTargetRatioBps = 0;
            allocation.IdlePopulation = 0;
            allocation.LastObservedPopulation = population.Total;
            allocation.AutoAllocationInitialized = 1;
            entityManager.SetComponentData(allocationEntity, allocation);
        }

        private static int ReadWoodWorkerCount(EntityManager entityManager, Entity allocationEntity)
        {
            return entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity).WoodWorkers;
        }

        private static IEnumerator WaitForWorkerVisualCount(EntityManager entityManager,
            EconomyFocusType resource, int expectedCount)
        {
            for (int frame = 0; frame < 180; frame++)
            {
                if (CountWorkerVisuals(entityManager, resource) == expectedCount)
                    yield break;
                yield return null;
            }

            Assert.That(CountWorkerVisuals(entityManager, resource), Is.EqualTo(expectedCount));
        }

        private static int CountWorkerVisuals(EntityManager entityManager, EconomyFocusType resource)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ResourceWorkerVisual>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<ResourceWorkerVisual> visuals =
                query.ToComponentDataArray<ResourceWorkerVisual>(Allocator.Temp);
            int count = 0;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (EconomyFocusUtility.Normalize(visuals[i].Resource) == resource)
                    count++;
            }
            return count;
        }
    }
}
