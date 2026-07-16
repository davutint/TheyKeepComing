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
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class WorkerAllocationPlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;
        private string _metaPath;
        private string _metaTempPath;
        private byte[] _originalMeta;
        private byte[] _originalMetaTemp;
        private bool _hadMeta;
        private bool _hadMetaTemp;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            _metaPath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
            _metaTempPath = _metaPath + ".tmp";
            _hadMeta = File.Exists(_metaPath);
            _hadMetaTemp = File.Exists(_metaTempPath);
            _originalMeta = _hadMeta ? File.ReadAllBytes(_metaPath) : null;
            _originalMetaTemp = _hadMetaTemp ? File.ReadAllBytes(_metaTempPath) : null;
            DeleteIfExists(_metaPath);
            DeleteIfExists(_metaTempPath);
            MetaProgression.Load();
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
            DeleteIfExists(_metaPath);
            DeleteIfExists(_metaTempPath);
            RestoreIfNeeded(_metaPath, _hadMeta, _originalMeta);
            RestoreIfNeeded(_metaTempPath, _hadMetaTemp, _originalMetaTemp);
            MetaProgression.Load();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstDayWorkerRatioOnboarding_PulsesRealControlAndCompletesOnPlayerAction()
        {
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            WorkerEconomyDrawerUI drawer =
                Object.FindFirstObjectByType<WorkerEconomyDrawerUI>();
            MarketUI market = Object.FindFirstObjectByType<MarketUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(drawer, Is.Not.Null);
            Assert.That(market, Is.Not.Null);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId), Is.False);

            for (int frame = 0; frame < 90 && !onboarding.IsWorkerRatioStepVisible; frame++)
                yield return null;

            Assert.That(onboarding.IsWorkerRatioStepVisible, Is.True);
            Assert.That(onboarding.HintPanel.activeSelf, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.WorkerRatioHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(drawer.WorkerDrawerToggleButton.GetComponent<RectTransform>()));

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation));
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            Entity resourceEntity = resourceQuery.GetSingletonEntity();
            MobilePopulationAllocation beforeAllocation =
                entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            ResourceData beforeResources = entityManager.GetComponentData<ResourceData>(resourceEntity);

            drawer.SetOpen(true);
            yield return null;
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(drawer.WoodWorkerTargetPlus10Button.GetComponent<RectTransform>()));

            drawer.WoodWorkerTargetPlus10Button.onClick.Invoke();
            yield return null;

            MobilePopulationAllocation afterAllocation =
                entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            ResourceData afterResources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(afterAllocation.WoodTargetRatioBps,
                Is.Not.EqualTo(beforeAllocation.WoodTargetRatioBps));
            AssertWorkerCountsEqual(beforeAllocation, afterAllocation);
            Assert.That(afterResources.Wood, Is.EqualTo(beforeResources.Wood));
            Assert.That(afterResources.Stone, Is.EqualTo(beforeResources.Stone));
            Assert.That(afterResources.Iron, Is.EqualTo(beforeResources.Iron));
            Assert.That(afterResources.Food, Is.EqualTo(beforeResources.Food));
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId), Is.True);
            Assert.That(onboarding.IsWorkerRatioStepVisible, Is.False);
            Assert.That(onboarding.IsBasicArcherStepVisible, Is.True);
            Assert.That(onboarding.HintPanel.activeSelf, Is.True);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.BasicArcherHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(market.DrawerToggleButton.GetComponent<RectTransform>()));
        }

        [UnityTest]
        public IEnumerator FirstAffordableBasicArcherOnboarding_PulsesRealBuyControlAndCompletesOnPurchase()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            MarketUI market = Object.FindFirstObjectByType<MarketUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(market, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId), Is.False);

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
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity resourceEntity = resourceQuery.GetSingletonEntity();
            entityManager.SetComponentData(resourceEntity, new ResourceData());
            market.SetDrawerOpen(false, true);
            market.Refresh();
            yield return null;

            Assert.That(gameManager.CanBuyArcher(ArcherType.Basic), Is.False);
            Assert.That(onboarding.IsBasicArcherStepVisible, Is.False);

            var fundedResources = new ResourceData
            {
                Wood = 1_000_000,
                Stone = 1_000_000,
                Iron = 1_000_000,
                Food = 1_000_000
            };
            entityManager.SetComponentData(resourceEntity, fundedResources);
            for (int frame = 0; frame < 60 && !onboarding.IsBasicArcherStepVisible; frame++)
                yield return null;

            Assert.That(gameManager.CanBuyArcher(ArcherType.Basic), Is.True);
            Assert.That(onboarding.IsBasicArcherStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.BasicArcherHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(market.DrawerToggleButton.GetComponent<RectTransform>()));

            market.SetDrawerOpen(true, true);
            market.Refresh();
            yield return null;
            Button basicBuyButton = market.GetArcherBuyButton(ArcherType.Basic);
            Assert.That(basicBuyButton, Is.Not.Null);
            Assert.That(basicBuyButton.interactable, Is.True);
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(basicBuyButton.GetComponent<RectTransform>()));

            int countBefore = gameManager.GetArcherTypeCount(ArcherType.Basic);
            ResourceCost cost = gameManager.GetArcherBuyCost(ArcherType.Basic);
            basicBuyButton.onClick.Invoke();
            yield return null;

            ResourceData resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Basic), Is.EqualTo(countBefore + 1));
            Assert.That(resourcesAfter.Wood, Is.EqualTo(fundedResources.Wood - cost.Wood));
            Assert.That(resourcesAfter.Stone, Is.EqualTo(fundedResources.Stone - cost.Stone));
            Assert.That(resourcesAfter.Iron, Is.EqualTo(fundedResources.Iron - cost.Iron));
            Assert.That(resourcesAfter.Food, Is.EqualTo(fundedResources.Food - cost.Food));
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId), Is.True);
            Assert.That(onboarding.IsBasicArcherStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstLowAmmoOnboarding_PulsesArrowChipWithoutOpeningPanel_AndCompletesOnRefill()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            ArrowSupplyUI ammoSupply = Object.FindFirstObjectByType<ArrowSupplyUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(ammoSupply, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId), Is.False);

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
            using EntityQuery gameStateQuery = entityManager.CreateEntityQuery(
                typeof(ArrowSupply), typeof(ResourceData));
            Entity gameStateEntity = gameStateQuery.GetSingletonEntity();
            int capacity = gameManager.GetArrowCapacity();
            int threshold = capacity * FirstRunOnboardingUI.LowAmmoThresholdPercent / 100;

            ArrowSupply supply = entityManager.GetComponentData<ArrowSupply>(gameStateEntity);
            supply.Current = threshold + 1;
            entityManager.SetComponentData(gameStateEntity, supply);
            ammoSupply.SetOpen(false);
            yield return null;

            Assert.That(onboarding.IsLowAmmoStepVisible, Is.False);
            Assert.That(ammoSupply.IsOpen, Is.False);

            supply.Current = threshold;
            entityManager.SetComponentData(gameStateEntity, supply);
            for (int frame = 0; frame < 60 && !onboarding.IsLowAmmoStepVisible; frame++)
                yield return null;

            Assert.That(onboarding.IsLowAmmoStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.LowAmmoHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(ammoSupply.ToggleButton.GetComponent<RectTransform>()));
            Assert.That(ammoSupply.IsOpen, Is.False,
                "Low-ammo onboarding ammo panelini oyuncu adina acmamalidir.");

            ResourceData resources = entityManager.GetComponentData<ResourceData>(gameStateEntity);
            resources.Wood = 0;
            entityManager.SetComponentData(gameStateEntity, resources);
            yield return null;
            ammoSupply.PackageButton.onClick.Invoke();
            yield return null;

            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId), Is.False,
                "Basarisiz refill denemesi tutorial'i tamamlamamalidir.");
            Assert.That(entityManager.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                Is.EqualTo(threshold));

            resources.Wood = 1_000_000;
            entityManager.SetComponentData(gameStateEntity, resources);
            yield return null;
            ArrowRefillQuote quote = gameManager.GetArrowRefillQuote(1);
            Assert.That(quote.IsValid, Is.True);
            ammoSupply.Refresh();
            Assert.That(ammoSupply.PackageButton.interactable, Is.True);
            ammoSupply.PackageButton.onClick.Invoke();
            yield return null;

            Assert.That(entityManager.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                Is.EqualTo(threshold + quote.ArrowAmount));
            Assert.That(entityManager.GetComponentData<ResourceData>(gameStateEntity).Wood,
                Is.EqualTo(resources.Wood - quote.WoodCost));
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId), Is.True);
            Assert.That(onboarding.IsLowAmmoStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            Assert.That(ammoSupply.IsOpen, Is.False);
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

            Entity completedArrival;
            using (EntityQuery arrivalQuery = entityManager.CreateEntityQuery(
                       typeof(SurvivorArrivalVisual), typeof(LocalTransform)))
            using (NativeArray<Entity> arrivalEntities =
                   arrivalQuery.ToEntityArray(Allocator.Temp))
            using (NativeArray<SurvivorArrivalVisual> arrivalVisuals =
                   arrivalQuery.ToComponentDataArray<SurvivorArrivalVisual>(Allocator.Temp))
            {
                Assert.That(arrivalEntities.Length, Is.EqualTo(3));

                int representedSurvivors = 0;
                for (int index = 0; index < arrivalEntities.Length; index++)
                {
                    Entity entity = arrivalEntities[index];
                    SurvivorArrivalVisual visual = arrivalVisuals[index];
                    LocalTransform transform = entityManager.GetComponentData<LocalTransform>(entity);
                    representedSurvivors += visual.RepresentedSurvivorCount;
                    Assert.That(transform.Position.x, Is.GreaterThan(visual.TargetPosition.x));
                    Assert.That(visual.TargetPosition.x,
                        Is.EqualTo(config.FrontlineX - SurvivorArrivalVisualUtility.TargetDistanceBehindWall)
                            .Within(0.001f));
                    Assert.That(entityManager.HasComponent<ResourceWorkerVisual>(entity), Is.False,
                        "Arrival visual gameplay worker sorgularina dahil olmamali.");
                }
                Assert.That(representedSurvivors, Is.EqualTo(3));

                completedArrival = arrivalEntities[0];
            }

            SurvivorArrivalVisual completedVisual =
                entityManager.GetComponentData<SurvivorArrivalVisual>(completedArrival);
            LocalTransform completedTransform =
                entityManager.GetComponentData<LocalTransform>(completedArrival);
            completedVisual.StartDelay = 0f;
            completedVisual.Speed = 100f;
            completedVisual.TargetPosition = completedTransform.Position + new float3(-0.01f, 0f, 0f);
            entityManager.SetComponentData(completedArrival, completedVisual);

            yield return null;
            yield return null;
            Assert.That(entityManager.Exists(completedArrival), Is.False,
                "Duvar girisine varan survivor visual entity temizlenmeli.");
        }

        [UnityTest]
        public IEnumerator WorkerDrawer_TargetControlsAndBuildingUpgradesUseBoundRuntimeState()
        {
            WorkerEconomyDrawerUI drawer = Object.FindFirstObjectByType<WorkerEconomyDrawerUI>();
            Assert.That(drawer, Is.Not.Null);
            Assert.That(drawer.WoodWorkerAddButton, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetPlus10Button, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetPlus100Button, Is.Not.Null);
            Assert.That(drawer.WoodWorkerTargetInput, Is.Not.Null);
            Assert.That(drawer.WoodCapacityUpgradeButton, Is.Not.Null);
            Assert.That(drawer.WoodEfficiencyUpgradeButton, Is.Not.Null);

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

            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData)).GetSingletonEntity();
            var resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Wood = 1_000;
            resources.Iron = 1_000;
            entityManager.SetComponentData(resourceEntity, resources);

            var configBefore = entityManager.GetComponentData<MobileCastleCombatConfig>(allocationEntity);
            drawer.WoodCapacityUpgradeButton.onClick.Invoke();
            drawer.WoodEfficiencyUpgradeButton.onClick.Invoke();
            yield return null;

            var upgrades = entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(allocationEntity);
            var configAfter = entityManager.GetComponentData<MobileCastleCombatConfig>(allocationEntity);
            var resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(upgrades.WoodCapacityLevel, Is.EqualTo(1));
            Assert.That(upgrades.WoodEfficiencyLevel, Is.EqualTo(1));
            Assert.That(upgrades.StoneCapacityLevel, Is.Zero);
            Assert.That(configAfter.WoodWorkerCap, Is.EqualTo(configBefore.WoodWorkerCap + 10));
            Assert.That(configAfter.WoodWorkerProductionPerMin - configBefore.WoodWorkerProductionPerMin,
                Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(resourcesAfter.Wood, Is.EqualTo(750));
            Assert.That(resourcesAfter.Iron, Is.EqualTo(925));
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

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void RestoreIfNeeded(string path, bool existed, byte[] contents)
        {
            if (existed && contents != null)
                File.WriteAllBytes(path, contents);
        }
    }
}
