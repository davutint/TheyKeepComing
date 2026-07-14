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
        public IEnumerator DawnArrivalTransaction_SpendsFoodOnceForAcceptedSurvivors()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery mobileQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(MobilePopulationAllocation),
                typeof(MobileBedCapacityState),
                typeof(ContinuousSiegeCycleData));
            using EntityQuery populationQuery = entityManager.CreateEntityQuery(
                typeof(PopulationState), typeof(ResourceData));
            Entity mobileEntity = mobileQuery.GetSingletonEntity();
            Entity populationEntity = populationQuery.GetSingletonEntity();

            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(mobileEntity);
            config.PopulationGrowthPerDayPrep = 15;
            config.FoodCostPerArrival = 1;
            config.WoodWorkerProductionPerMin = 0f;
            config.StoneWorkerProductionPerMin = 0f;
            config.IronWorkerProductionPerMin = 0f;
            config.FoodWorkerProductionPerMin = 0f;
            entityManager.SetComponentData(mobileEntity, config);

            entityManager.SetComponentData(mobileEntity, new MobileBedCapacityState
            {
                BaseCapacity = 65,
                PurchasedCapacity = 0
            });

            var population = entityManager.GetComponentData<PopulationState>(populationEntity);
            population.Total = 60;
            population.Capacity = 65;
            population.BaseCapacity = 65;
            entityManager.SetComponentData(populationEntity, population);

            var resources = entityManager.GetComponentData<ResourceData>(populationEntity);
            resources.Food = 3;
            entityManager.SetComponentData(populationEntity, resources);

            var allocation = entityManager.GetComponentData<MobilePopulationAllocation>(mobileEntity);
            allocation.LastObservedPopulation = population.Total;
            allocation.AutoAllocationInitialized = 1;
            allocation.LastPopulationGrowthCycle = 0;
            allocation.LastArrivalRequestedCount = 0;
            allocation.LastArrivalAcceptedCount = 0;
            allocation.LastArrivalFoodCost = 0;
            entityManager.SetComponentData(mobileEntity, allocation);

            var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(mobileEntity);
            cycle.Enabled = true;
            cycle.CycleIndex = 0;
            cycle.CycleTimer = config.SiegeDayDuration + config.SiegeDuskDuration
                + config.SiegeNightDuration + 0.5f;
            cycle.Phase = SiegeCyclePhase.Dawn;
            entityManager.SetComponentData(mobileEntity, cycle);

            yield return null;
            yield return null;

            population = entityManager.GetComponentData<PopulationState>(populationEntity);
            resources = entityManager.GetComponentData<ResourceData>(populationEntity);
            allocation = entityManager.GetComponentData<MobilePopulationAllocation>(mobileEntity);

            Assert.That(population.Total, Is.EqualTo(63));
            Assert.That(population.Capacity, Is.EqualTo(65));
            Assert.That(population.BaseCapacity, Is.EqualTo(65));
            Assert.That(resources.Food, Is.Zero,
                "Kabul edilen 3 survivor icin 3 Food ayni Dawn'da yalniz bir kez harcanmali.");
            Assert.That(allocation.LastPopulationGrowthCycle, Is.EqualTo(1));
            Assert.That(allocation.LastArrivalRequestedCount, Is.EqualTo(15));
            Assert.That(allocation.LastArrivalAcceptedCount, Is.EqualTo(3));
            Assert.That(allocation.LastArrivalFoodCost, Is.EqualTo(3));
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

        [UnityTest]
        public IEnumerator WorkerFeedback_TracksActualWeightRouteDeliveryAndNightLantern()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
            using EntityQuery populationQuery = entityManager.CreateEntityQuery(typeof(PopulationState));
            using EntityQuery waveQuery = entityManager.CreateEntityQuery(typeof(WaveStateData));
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(typeof(ContinuousSiegeCycleData));
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            Entity populationEntity = populationQuery.GetSingletonEntity();
            Entity waveEntity = waveQuery.GetSingletonEntity();
            Entity cycleEntity = cycleQuery.GetSingletonEntity();

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.StressTestMode = true;
            entityManager.SetComponentData(waveEntity, wave);

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 101);
            yield return WaitForWorkerVisualCount(entityManager, EconomyFocusType.Wood, 27);
            yield return null;
            Assert.That(SumRepresentedWorkers(entityManager, EconomyFocusType.Wood), Is.EqualTo(101));

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 119);
            yield return null;
            yield return null;
            Assert.That(CountWorkerVisuals(entityManager, EconomyFocusType.Wood), Is.EqualTo(27),
                "Ayni density bucket'inda visual entity sayisi degismemeli.");
            Assert.That(SumRepresentedWorkers(entityManager, EconomyFocusType.Wood), Is.EqualTo(119),
                "Actual count degisimi ayni visual bucket'inda representation weight'e yansimadi.");

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);
            yield return null;

            Entity worker = FindWorkerVisual(entityManager, EconomyFocusType.Wood);
            WorkerLogisticsFeedbackState feedback =
                entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            WorkerFeedbackMaterialProperty materialFeedback =
                entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            Assert.That(feedback.LanternActive, Is.EqualTo(1));
            Assert.That(materialFeedback.Value.y, Is.EqualTo(1f).Within(0.001f));

            WorkerLogisticsRoute route = entityManager.GetComponentData<WorkerLogisticsRoute>(worker);
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(worker);
            route.MovingToHub = 0;
            route.RouteLeg = 2;
            route.WaitTimer = 0f;
            transform.Position = route.PickupPosition;
            feedback.IsCarrying = 0;
            entityManager.SetComponentData(worker, route);
            entityManager.SetComponentData(worker, transform);
            entityManager.SetComponentData(worker, feedback);
            yield return null;

            feedback = entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            materialFeedback = entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            WorkerAnimationMaterialProperty animation =
                entityManager.GetComponentData<WorkerAnimationMaterialProperty>(worker);
            Assert.That(feedback.Activity, Is.EqualTo(WorkerLogisticsActivity.Working));
            Assert.That(feedback.IsCarrying, Is.EqualTo(1));
            Assert.That(animation.Value, Is.EqualTo((float)WorkerAnimationKind.Work));
            Assert.That(materialFeedback.Value.x, Is.EqualTo(1f).Within(0.001f));

            route = entityManager.GetComponentData<WorkerLogisticsRoute>(worker);
            transform = entityManager.GetComponentData<LocalTransform>(worker);
            route.MovingToHub = 1;
            route.RouteLeg = 2;
            route.WaitTimer = 0f;
            transform.Position = route.DeliveryPosition;
            feedback.IsCarrying = 1;
            entityManager.SetComponentData(worker, route);
            entityManager.SetComponentData(worker, transform);
            entityManager.SetComponentData(worker, feedback);
            yield return null;

            feedback = entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            materialFeedback = entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            animation = entityManager.GetComponentData<WorkerAnimationMaterialProperty>(worker);
            Assert.That(feedback.Activity, Is.EqualTo(WorkerLogisticsActivity.Delivering));
            Assert.That(feedback.IsCarrying, Is.Zero);
            Assert.That(feedback.DeliveryPulse01, Is.GreaterThan(0.8f));
            Assert.That(animation.Value, Is.EqualTo((float)WorkerAnimationKind.Celebrate));
            Assert.That(materialFeedback.Value.z, Is.GreaterThan(0.8f));

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Day;
            entityManager.SetComponentData(cycleEntity, cycle);
            yield return null;
            feedback = entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            Assert.That(feedback.LanternActive, Is.Zero);
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

        private static int SumRepresentedWorkers(EntityManager entityManager,
            EconomyFocusType resource)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ResourceWorkerVisual>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<ResourceWorkerVisual> visuals =
                query.ToComponentDataArray<ResourceWorkerVisual>(Allocator.Temp);
            int total = 0;
            for (int i = 0; i < visuals.Length; i++)
            {
                if (EconomyFocusUtility.Normalize(visuals[i].Resource) == resource)
                    total += math.max(0, visuals[i].RepresentedWorkerCount);
            }

            return total;
        }

        private static Entity FindWorkerVisual(EntityManager entityManager, EconomyFocusType resource)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ResourceWorkerVisual>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<ResourceWorkerVisual> visuals =
                query.ToComponentDataArray<ResourceWorkerVisual>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (EconomyFocusUtility.Normalize(visuals[i].Resource) == resource)
                    return entities[i];
            }

            Assert.Fail($"{resource} worker visual bulunamadi.");
            return Entity.Null;
        }
    }
}
