using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
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
            Time.timeScale = 1f;
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
            ResourceData resourcesBeforePurchase =
                entityManager.GetComponentData<ResourceData>(resourceEntity);
            basicBuyButton.onClick.Invoke();
            ResourceData resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(gameManager.GetArcherTypeCount(ArcherType.Basic), Is.EqualTo(countBefore + 1));
            Assert.That(resourcesAfter.Wood, Is.EqualTo(resourcesBeforePurchase.Wood - cost.Wood));
            Assert.That(resourcesAfter.Stone, Is.EqualTo(resourcesBeforePurchase.Stone - cost.Stone));
            Assert.That(resourcesAfter.Iron, Is.EqualTo(resourcesBeforePurchase.Iron - cost.Iron));
            Assert.That(resourcesAfter.Food, Is.EqualTo(resourcesBeforePurchase.Food - cost.Food));
            yield return null;

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
            ResourceData resourcesBeforeRefill =
                entityManager.GetComponentData<ResourceData>(gameStateEntity);
            ammoSupply.PackageButton.onClick.Invoke();
            Assert.That(entityManager.GetComponentData<ArrowSupply>(gameStateEntity).Current,
                Is.EqualTo(threshold + quote.ArrowAmount));
            Assert.That(entityManager.GetComponentData<ResourceData>(gameStateEntity).Wood,
                Is.EqualTo(resourcesBeforeRefill.Wood - quote.WoodCost));
            yield return null;

            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId), Is.True);
            Assert.That(onboarding.IsLowAmmoStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            Assert.That(ammoSupply.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstEssenceHeartOnboarding_PulsesEntryAndTeachesFullPauseUntilPlayerCloses()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            HeartScreenUI heart = Object.FindFirstObjectByType<HeartScreenUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(heart, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.False);

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
            Assert.That(gameManager.GraveEssenceAmount, Is.Zero);
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.False);

            Assert.That(gameManager.GrantGraveEssence(1L), Is.True);
            for (int frame = 0; frame < 60 && !onboarding.IsHeartEntryStepVisible; frame++)
                yield return null;

            Assert.That(onboarding.IsHeartEntryStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.HeartEntryHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(heart.HeartOpenButton.GetComponent<RectTransform>()));
            Assert.That(heart.IsOpen, Is.False,
                "Heart onboarding paneli oyuncu adina acmamalidir.");

            heart.HeartOpenButton.onClick.Invoke();
            yield return null;

            Assert.That(heart.IsOpen, Is.True);
            Assert.That(SimulationPauseService.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.HeartPauseHint));
            Assert.That(onboarding.ActivePulseTarget, Is.Null);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.False,
                "Full-pause hint gorulmeden Heart adimi tamamlanmamalidir.");

            heart.HeartCloseButton.onClick.Invoke();
            yield return null;

            Assert.That(heart.IsOpen, Is.False);
            Assert.That(SimulationPauseService.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.True);
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.False);
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstRunOnboarding_HeartActionBeforeEssenceStillCompletesWithoutLaterPrompt()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            HeartScreenUI heart = Object.FindFirstObjectByType<HeartScreenUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(heart, Is.Not.Null);

            string[] completedOtherSteps =
            {
                FirstRunOnboardingUI.WorkerRatioFlagId,
                FirstRunOnboardingUI.BasicArcherFlagId,
                FirstRunOnboardingUI.LowAmmoFlagId,
                FirstRunOnboardingUI.CouncilExactFlagId,
                FirstRunOnboardingUI.DaytimeRepairFlagId,
                FirstRunOnboardingUI.NightAbilityKeyFlagId
            };
            foreach (string flagId in completedOtherSteps)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);

            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.False);
            Assert.That(gameManager.GraveEssenceAmount, Is.Zero);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.False);
            yield return null;
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);

            heart.HeartOpenButton.onClick.Invoke();
            yield return null;

            Assert.That(heart.IsOpen, Is.True);
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.EqualTo(1));
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.True,
                "Essence prompt'i gelmeden yapilan gercek Heart action'i pause dersini gostermelidir.");
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.False,
                "Heart close edilmeden iki asamali ders tamamlanmamalidir.");

            heart.HeartCloseButton.onClick.Invoke();
            yield return null;

            Assert.That(heart.IsOpen, Is.False);
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.True,
                "Son accepted action global tutorial complete flag'ini yazmalidir.");
            MetaProgression.Load();
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.True,
                "Global tutorial complete flag meta save reload sonrasinda kalmalidir.");
            Assert.That(gameManager.GrantGraveEssence(1L), Is.True);
            yield return null;
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.False,
                "Preemptive action ile tamamlanan Heart prompt'i Essence gelince tekrar acilmamalidir.");
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstRunOnboarding_LegacyStepFlagsBackfillGlobalComplete()
        {
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            Assert.That(onboarding, Is.Not.Null);

            string[] requiredStepFlags =
            {
                FirstRunOnboardingUI.WorkerRatioFlagId,
                FirstRunOnboardingUI.BasicArcherFlagId,
                FirstRunOnboardingUI.LowAmmoFlagId,
                FirstRunOnboardingUI.HeartEntryFlagId,
                FirstRunOnboardingUI.CouncilExactFlagId,
                FirstRunOnboardingUI.DaytimeRepairFlagId,
                FirstRunOnboardingUI.NightAbilityKeyFlagId
            };
            foreach (string flagId in requiredStepFlags)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);

            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.False,
                "Legacy meta save global complete flag'ini tasimiyor olmali.");
            for (int frame = 0; frame < 30 && !MetaProgression.HasTutorialFlag(
                    FirstRunOnboardingUI.TutorialCompleteFlagId); frame++)
            {
                yield return null;
            }

            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.True);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            MetaProgression.Load();
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.TutorialCompleteFlagId), Is.True,
                "Backfill edilen global flag meta save reload sonrasinda kalmalidir.");
        }

        [UnityTest]
        public IEnumerator FirstRunOnboarding_HeartCloseEndsPauseBeforeNextNonModalCue()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            HeartScreenUI heart = Object.FindFirstObjectByType<HeartScreenUI>();
            PauseMenuUI pauseMenu = Object.FindFirstObjectByType<PauseMenuUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(heart, Is.Not.Null);
            Assert.That(pauseMenu, Is.Not.Null);
            Assert.That(pauseMenu.PausePanel, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId), Is.False);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId), Is.False);

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
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery wallQuery = entityManager.CreateEntityQuery(typeof(WallSegment));
            Assert.That(cycleQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(wallQuery.CalculateEntityCount(), Is.EqualTo(1));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity wallEntity = wallQuery.GetSingletonEntity();
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            Assert.That(cycleSetter, Is.Not.Null);
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(pauseMenu.PausePanel.activeSelf, Is.False);

            Assert.That(gameManager.GrantGraveEssence(1L), Is.True);
            for (int frame = 0; frame < 60 && !onboarding.IsHeartEntryStepVisible; frame++)
                yield return null;
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.True);

            heart.HeartOpenButton.onClick.Invoke();
            yield return null;
            Assert.That(heart.IsOpen, Is.True);
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.True);
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.EqualTo(1),
                "Tutorial Heart owner'ina ek pause lease'i bindirmemelidir.");
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(pauseMenu.PausePanel.activeSelf, Is.False);

            WallSegment wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Day);
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.True);
            Assert.That(onboarding.IsDaytimeRepairStepVisible, Is.False,
                "Heart modal acikken sonraki onboarding cue zincirlenmemelidir.");
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.EqualTo(1));

            heart.HeartCloseButton.onClick.Invoke();
            yield return null;
            for (int frame = 0; frame < 60 && !onboarding.IsDaytimeRepairStepVisible; frame++)
                yield return null;

            Assert.That(heart.IsOpen, Is.False);
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.False);
            Assert.That(onboarding.IsDaytimeRepairStepVisible, Is.True,
                "Pause kapandiktan sonra sonraki cue non-modal olarak devam etmelidir.");
            Assert.That(onboarding.HintPanel.activeSelf, Is.True);
            Assert.That(SimulationPauseService.ActiveLeaseCount, Is.Zero);
            Assert.That(SimulationPauseService.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(pauseMenu.PausePanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstDamagedWallDayRepairOnboarding_PulsesRealRepairAction_AndCompletesOnSuccessfulPlayerRepair()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            DefenseRepairUI repair = Object.FindFirstObjectByType<DefenseRepairUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(repair, Is.Not.Null);
            Assert.That(repair.RepairButton, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId), Is.False);

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
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            using EntityQuery wallQuery = entityManager.CreateEntityQuery(typeof(WallSegment));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity resourceEntity = resourceQuery.GetSingletonEntity();
            Entity wallEntity = wallQuery.GetSingletonEntity();

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Day;
            entityManager.SetComponentData(cycleEntity, cycle);

            WallSegment damagedWall = entityManager.GetComponentData<WallSegment>(wallEntity);
            damagedWall.CurrentHP = damagedWall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, damagedWall);

            ResourceData resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Stone = 0;
            entityManager.SetComponentData(resourceEntity, resources);

            for (int frame = 0; frame < 90 && !onboarding.IsDaytimeRepairStepVisible; frame++)
                yield return null;

            Assert.That(onboarding.IsDaytimeRepairStepVisible, Is.True);
            Assert.That(onboarding.HintPanel.activeSelf, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.DaytimeRepairHint));
            Assert.That(onboarding.ActivePulseTarget, Is.SameAs(repair.RepairActionRect));
            Assert.That(repair.RepairButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(onboarding.HintPanel.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(new Vector2(0f, -294f)));

            resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Stone = 0;
            entityManager.SetComponentData(resourceEntity, resources);
            WallSegment beforeFailedRepair =
                entityManager.GetComponentData<WallSegment>(wallEntity);
            repair.RepairButton.onClick.Invoke();

            Assert.That(entityManager.GetComponentData<WallSegment>(wallEntity).CurrentHP,
                Is.EqualTo(beforeFailedRepair.CurrentHP));
            Assert.That(entityManager.GetComponentData<ResourceData>(resourceEntity).Stone, Is.Zero);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId), Is.False,
                "Basarisiz repair denemesi tutorial'i tamamlamamalidir.");

            resources.Stone = 10_000;
            entityManager.SetComponentData(resourceEntity, resources);
            for (int frame = 0; frame < 90 && !repair.RepairButton.interactable; frame++)
                yield return null;

            Assert.That(repair.RepairButton.interactable, Is.True);
            ResourceCost repairCost = gameManager.GetRepairCost();
            Assert.That(repairCost.Wood, Is.Zero);
            Assert.That(repairCost.Stone, Is.GreaterThan(0));
            WallSegment beforeRepair = entityManager.GetComponentData<WallSegment>(wallEntity);
            int stoneBefore = entityManager.GetComponentData<ResourceData>(resourceEntity).Stone;
            float expectedHp = Mathf.Min(
                beforeRepair.MaxHP,
                beforeRepair.CurrentHP
                    + beforeRepair.MaxHP * gameManager.GetNormalRepairHealPercent());

            repair.RepairButton.onClick.Invoke();

            WallSegment repairedWall = entityManager.GetComponentData<WallSegment>(wallEntity);
            ResourceData resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(repairedWall.CurrentHP, Is.EqualTo(expectedHp).Within(0.01f));
            Assert.That(resourcesAfter.Stone, Is.EqualTo(stoneBefore - repairCost.Stone));
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId), Is.True);
            yield return null;
            Assert.That(onboarding.IsDaytimeRepairStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstNightAbilityKeyOnboarding_PulsesFirstReadySlot_AndCompletesOnlyOnAcceptedHotkey()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            SpellCastUI abilities = Object.FindFirstObjectByType<SpellCastUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(abilities, Is.Not.Null);
            Assert.That(abilities.RallyButton, Is.Not.Null);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.NightAbilityKeyFlagId), Is.False);

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
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery prepQuery = entityManager.CreateEntityQuery(typeof(CastleYardPrepState));
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity prepEntity = prepQuery.GetSingletonEntity();
            Entity resourceEntity = resourceQuery.GetSingletonEntity();

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleIndex = 0;
            cycle.CycleTimer = cycle.DayDuration + cycle.DuskDuration + 0.5f;
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);

            FieldInfo rallyCooldownField = typeof(GameManager).GetField(
                "_rallyCooldownRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo prepProperty = typeof(GameManager).GetProperty(
                "CastleYardPrep",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo hotkeyMethod = typeof(SpellCastUI).GetMethod(
                "TryActivateHotkey",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(rallyCooldownField, Is.Not.Null);
            Assert.That(prepProperty, Is.Not.Null);
            Assert.That(hotkeyMethod, Is.Not.Null);

            CastleYardPrepState prep =
                entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            prep.RallyTimer = 0f;
            entityManager.SetComponentData(prepEntity, prep);
            rallyCooldownField.SetValue(gameManager, 0f);
            prepProperty.SetValue(gameManager, prep);

            for (int frame = 0; frame < 120 && !onboarding.IsNightAbilityKeyStepVisible; frame++)
                yield return null;

            Assert.That(gameManager.FireballUnlocked, Is.False,
                "Ilk kosuda tech-acilmamis Fireball key-hint hedefi olmamalidir.");
            Assert.That(gameManager.RallyReady, Is.True);
            Assert.That(onboarding.IsNightAbilityKeyStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.RallyAbilityKeyHint));
            Assert.That(onboarding.ActivePulseTarget,
                Is.SameAs(abilities.RallyButton.GetComponent<RectTransform>()));
            Assert.That(onboarding.HintPanel.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(new Vector2(0f, 170f)));

            bool lockedFireballAccepted = (bool)hotkeyMethod.Invoke(
                abilities,
                new object[] { AbilityHotkeySlot.Fireball, gameManager });
            Assert.That(lockedFireballAccepted, Is.False);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.NightAbilityKeyFlagId), Is.False);

            ResourceData resourcesBeforeButton =
                entityManager.GetComponentData<ResourceData>(resourceEntity);
            abilities.RallyButton.onClick.Invoke();
            Assert.That(gameManager.RallyActive, Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.NightAbilityKeyFlagId), Is.False,
                "Mouse button kullanimi key-hint tutorial'ini tamamlamamalidir.");
            ResourceData resourcesAfterButton =
                entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(resourcesAfterButton.Wood, Is.EqualTo(resourcesBeforeButton.Wood));
            Assert.That(resourcesAfterButton.Stone, Is.EqualTo(resourcesBeforeButton.Stone));
            Assert.That(resourcesAfterButton.Iron, Is.EqualTo(resourcesBeforeButton.Iron));
            Assert.That(resourcesAfterButton.Food, Is.EqualTo(resourcesBeforeButton.Food));

            prep = entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            prep.RallyTimer = 0f;
            entityManager.SetComponentData(prepEntity, prep);
            rallyCooldownField.SetValue(gameManager, 0f);
            prepProperty.SetValue(gameManager, prep);
            for (int frame = 0;
                frame < 90 && (!gameManager.RallyReady || !onboarding.IsNightAbilityKeyStepVisible);
                frame++)
            {
                yield return null;
            }

            Assert.That(gameManager.RallyReady, Is.True);
            Assert.That(onboarding.IsNightAbilityKeyStepVisible, Is.True);
            ResourceData resourcesBeforeHotkey =
                entityManager.GetComponentData<ResourceData>(resourceEntity);
            bool rallyHotkeyAccepted = (bool)hotkeyMethod.Invoke(
                abilities,
                new object[] { AbilityHotkeySlot.Rally, gameManager });
            Assert.That(rallyHotkeyAccepted, Is.True);
            Assert.That(gameManager.RallyActive, Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.NightAbilityKeyFlagId), Is.True);
            Assert.That(entityManager.GetComponentData<ResourceData>(resourceEntity).Wood,
                Is.EqualTo(resourcesBeforeHotkey.Wood));
            Assert.That(entityManager.GetComponentData<ResourceData>(resourceEntity).Stone,
                Is.EqualTo(resourcesBeforeHotkey.Stone));
            Assert.That(entityManager.GetComponentData<ResourceData>(resourceEntity).Iron,
                Is.EqualTo(resourcesBeforeHotkey.Iron));
            Assert.That(entityManager.GetComponentData<ResourceData>(resourceEntity).Food,
                Is.EqualTo(resourcesBeforeHotkey.Food));
            yield return null;
            Assert.That(onboarding.IsNightAbilityKeyStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstRunOnboarding_AllSevenCuesRemainPresentationOnlyAndTransactionFree()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            WorkerEconomyDrawerUI workerDrawer =
                Object.FindFirstObjectByType<WorkerEconomyDrawerUI>();
            MarketUI archerMarket = Object.FindFirstObjectByType<MarketUI>();
            ArrowSupplyUI ammoSupply = Object.FindFirstObjectByType<ArrowSupplyUI>();
            HeartScreenUI heart = Object.FindFirstObjectByType<HeartScreenUI>();
            CouncilEventUI council = Object.FindFirstObjectByType<CouncilEventUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(workerDrawer, Is.Not.Null);
            Assert.That(archerMarket, Is.Not.Null);
            Assert.That(ammoSupply, Is.Not.Null);
            Assert.That(heart, Is.Not.Null);
            Assert.That(council, Is.Not.Null);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery gameStateQuery = entityManager.CreateEntityQuery(
                typeof(ResourceData),
                typeof(ArrowSupply),
                typeof(GraveEssence),
                typeof(PopulationState));
            using EntityQuery allocationQuery = entityManager.CreateEntityQuery(
                typeof(MobilePopulationAllocation),
                typeof(MobileBedCapacityState),
                typeof(MobileWorkerBuildingUpgradeState));
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery prepQuery = entityManager.CreateEntityQuery(
                typeof(CastleYardPrepState));
            using EntityQuery pauseQuery = entityManager.CreateEntityQuery(
                typeof(MobilePrepPauseState));
            using EntityQuery wallQuery = entityManager.CreateEntityQuery(typeof(WallSegment));
            Assert.That(gameStateQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(allocationQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(cycleQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(prepQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(pauseQuery.CalculateEntityCount(), Is.EqualTo(1));
            Assert.That(wallQuery.CalculateEntityCount(), Is.EqualTo(1));

            Entity gameStateEntity = gameStateQuery.GetSingletonEntity();
            Entity allocationEntity = allocationQuery.GetSingletonEntity();
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity prepEntity = prepQuery.GetSingletonEntity();
            Entity pauseEntity = pauseQuery.GetSingletonEntity();
            Entity wallEntity = wallQuery.GetSingletonEntity();
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            FieldInfo rallyCooldownField = typeof(GameManager).GetField(
                "_rallyCooldownRemaining",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo prepProperty = typeof(GameManager).GetProperty(
                "CastleYardPrep",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(cycleSetter, Is.Not.Null);
            Assert.That(rallyCooldownField, Is.Not.Null);
            Assert.That(prepProperty, Is.Not.Null);

            ResourceData fundedResources =
                entityManager.GetComponentData<ResourceData>(gameStateEntity);
            fundedResources.Wood = 1_000_000;
            fundedResources.Stone = 1_000_000;
            fundedResources.Iron = 1_000_000;
            fundedResources.Food = 1_000_000;
            entityManager.SetComponentData(gameStateEntity, fundedResources);
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Day);
            MobilePrepPauseState pause =
                entityManager.GetComponentData<MobilePrepPauseState>(pauseEntity);
            pause.IsPaused = true;
            entityManager.SetComponentData(pauseEntity, pause);
            Time.timeScale = 1f;

            yield return AssertCueIsTransactionFree(
                () => onboarding.IsWorkerRatioStepVisible,
                "Worker ratio cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(workerDrawer.IsOpen, Is.False,
                "Worker onboarding drawer'i oyuncu adina acmamalidir.");

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            yield return AssertCueIsTransactionFree(
                () => onboarding.IsBasicArcherStepVisible,
                "Basic Archer cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(archerMarket.IsDrawerOpen, Is.False,
                "Archer onboarding drawer'i oyuncu adina acmamalidir.");

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            ArrowSupply arrows = entityManager.GetComponentData<ArrowSupply>(gameStateEntity);
            arrows.Current = 0;
            entityManager.SetComponentData(gameStateEntity, arrows);
            yield return AssertCueIsTransactionFree(
                () => onboarding.IsLowAmmoStepVisible,
                "Low ammo cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(ammoSupply.IsOpen, Is.False,
                "Ammo onboarding paneli oyuncu adina acmamalidir.");

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            GraveEssence essence =
                entityManager.GetComponentData<GraveEssence>(gameStateEntity);
            essence.Current = 1;
            entityManager.SetComponentData(gameStateEntity, essence);
            yield return AssertCueIsTransactionFree(
                () => onboarding.IsHeartEntryStepVisible,
                "Castle Heart cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(heart.IsOpen, Is.False,
                "Heart onboarding modal'i oyuncu adina acmamalidir.");

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId, true), Is.True);
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                2,
                SiegeCyclePhase.Dawn);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            ComposedCouncilEvent activeCouncil = gameManager.ActiveCouncilEvent;
            for (int frame = 0; frame < 3; frame++)
                yield return null;

            yield return AssertCueIsTransactionFree(
                () => onboarding.IsCouncilExactStepVisible,
                "Regular Council cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(gameManager.ActiveCouncilEvent, Is.SameAs(activeCouncil),
                "Council onboarding oyuncu adina option secmemelidir.");

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId, true), Is.True);
            gameManager.ExpireCouncilEvent();
            WallSegment wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                2,
                SiegeCyclePhase.Day);
            yield return AssertCueIsTransactionFree(
                () => onboarding.IsDaytimeRepairStepVisible,
                "Daytime repair cue",
                entityManager,
                gameStateEntity,
                allocationEntity);

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.DaytimeRepairFlagId, true), Is.True);
            wall.CurrentHP = wall.MaxHP;
            entityManager.SetComponentData(wallEntity, wall);
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Night);
            CastleYardPrepState prep =
                entityManager.GetComponentData<CastleYardPrepState>(prepEntity);
            prep.RallyTimer = 0f;
            entityManager.SetComponentData(prepEntity, prep);
            rallyCooldownField.SetValue(gameManager, 0f);
            prepProperty.SetValue(gameManager, prep);
            yield return AssertCueIsTransactionFree(
                () => onboarding.IsNightAbilityKeyStepVisible,
                "Night ability-key cue",
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.NightAbilityKeyFlagId), Is.False,
                "Ability cue accepted keyboard action olmadan tamamlanmamalidir.");
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

        private readonly struct OnboardingEconomySnapshot
        {
            public readonly ResourceData Resources;
            public readonly ArrowSupply Arrows;
            public readonly GraveEssence Essence;
            public readonly PopulationState Population;
            public readonly MobilePopulationAllocation Allocation;
            public readonly MobileBedCapacityState Beds;
            public readonly MobileWorkerBuildingUpgradeState WorkerBuildings;

            public OnboardingEconomySnapshot(
                ResourceData resources,
                ArrowSupply arrows,
                GraveEssence essence,
                PopulationState population,
                MobilePopulationAllocation allocation,
                MobileBedCapacityState beds,
                MobileWorkerBuildingUpgradeState workerBuildings)
            {
                Resources = resources;
                Arrows = arrows;
                Essence = essence;
                Population = population;
                Allocation = allocation;
                Beds = beds;
                WorkerBuildings = workerBuildings;
            }
        }

        private static IEnumerator AssertCueIsTransactionFree(
            Func<bool> cueVisible,
            string cueName,
            EntityManager entityManager,
            Entity gameStateEntity,
            Entity allocationEntity)
        {
            OnboardingEconomySnapshot before = CaptureOnboardingEconomy(
                entityManager,
                gameStateEntity,
                allocationEntity);
            float presentationDeadline = Time.realtimeSinceStartup + 5f;
            while (!cueVisible() && Time.realtimeSinceStartup < presentationDeadline)
                yield return null;

            Assert.That(cueVisible(), Is.True, $"{cueName} gorunur olmadi.");
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            OnboardingEconomySnapshot after = CaptureOnboardingEconomy(
                entityManager,
                gameStateEntity,
                allocationEntity);
            Assert.That(after.Resources, Is.EqualTo(before.Resources),
                $"{cueName} Wood/Stone/Iron/Food state'ini degistirdi.");
            Assert.That(after.Arrows, Is.EqualTo(before.Arrows),
                $"{cueName} Arrow state'ini degistirdi.");
            Assert.That(after.Essence, Is.EqualTo(before.Essence),
                $"{cueName} Grave Essence state'ini degistirdi.");
            Assert.That(after.Population, Is.EqualTo(before.Population),
                $"{cueName} population/worker toplamlarini degistirdi.");
            AssertWorkerAllocationUnchanged(before.Allocation, after.Allocation, cueName);
            Assert.That(after.Beds, Is.EqualTo(before.Beds),
                $"{cueName} bed state'ini degistirdi.");
            Assert.That(after.WorkerBuildings, Is.EqualTo(before.WorkerBuildings),
                $"{cueName} worker building yatirim state'ini degistirdi.");
        }

        private static void AssertWorkerAllocationUnchanged(
            MobilePopulationAllocation before,
            MobilePopulationAllocation after,
            string cueName)
        {
            Assert.That(after.WoodWorkers, Is.EqualTo(before.WoodWorkers), cueName);
            Assert.That(after.StoneWorkers, Is.EqualTo(before.StoneWorkers), cueName);
            Assert.That(after.IronWorkers, Is.EqualTo(before.IronWorkers), cueName);
            Assert.That(after.FoodWorkers, Is.EqualTo(before.FoodWorkers), cueName);
            Assert.That(after.WoodTargetRatioBps, Is.EqualTo(before.WoodTargetRatioBps), cueName);
            Assert.That(after.StoneTargetRatioBps, Is.EqualTo(before.StoneTargetRatioBps), cueName);
            Assert.That(after.IronTargetRatioBps, Is.EqualTo(before.IronTargetRatioBps), cueName);
            Assert.That(after.FoodTargetRatioBps, Is.EqualTo(before.FoodTargetRatioBps), cueName);
            Assert.That(after.WoodWorkerCapacity, Is.EqualTo(before.WoodWorkerCapacity), cueName);
            Assert.That(after.StoneWorkerCapacity, Is.EqualTo(before.StoneWorkerCapacity), cueName);
            Assert.That(after.IronWorkerCapacity, Is.EqualTo(before.IronWorkerCapacity), cueName);
            Assert.That(after.FoodWorkerCapacity, Is.EqualTo(before.FoodWorkerCapacity), cueName);
            Assert.That(after.IdlePopulation, Is.EqualTo(before.IdlePopulation), cueName);
        }

        private static OnboardingEconomySnapshot CaptureOnboardingEconomy(
            EntityManager entityManager,
            Entity gameStateEntity,
            Entity allocationEntity)
        {
            return new OnboardingEconomySnapshot(
                entityManager.GetComponentData<ResourceData>(gameStateEntity),
                entityManager.GetComponentData<ArrowSupply>(gameStateEntity),
                entityManager.GetComponentData<GraveEssence>(gameStateEntity),
                entityManager.GetComponentData<PopulationState>(gameStateEntity),
                entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity),
                entityManager.GetComponentData<MobileBedCapacityState>(allocationEntity),
                entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(allocationEntity));
        }

        private static void SetOnboardingCycle(
            EntityManager entityManager,
            Entity cycleEntity,
            GameManager gameManager,
            MethodInfo cycleSetter,
            int cycleIndex,
            SiegeCyclePhase phase)
        {
            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Enabled = true;
            cycle.CycleIndex = cycleIndex;
            cycle.Phase = phase;
            cycle.CycleTimer = phase switch
            {
                SiegeCyclePhase.Day => 0.5f,
                SiegeCyclePhase.Dusk => cycle.DayDuration + 0.5f,
                SiegeCyclePhase.Night => cycle.DayDuration + cycle.DuskDuration + 0.5f,
                _ => cycle.DayDuration + cycle.DuskDuration + cycle.NightDuration + 0.5f
            };
            cycle.CycleProgress01 = cycle.CycleTimer
                / Mathf.Max(0.01f, cycle.CycleDuration);
            entityManager.SetComponentData(cycleEntity, cycle);
            cycleSetter.Invoke(gameManager, new object[] { cycle });
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
