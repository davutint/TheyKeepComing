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
        public IEnumerator SettingsTutorialReset_ConfirmsThenRestartsOnboardingDurably()
        {
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            SettingsUI settings = Object.FindFirstObjectByType<SettingsUI>();
            PauseMenuUI pauseMenu = Object.FindFirstObjectByType<PauseMenuUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            Assert.That(pauseMenu, Is.Not.Null);
            Assert.That(settings.TutorialResetButton, Is.Not.Null);
            Assert.That(settings.TutorialResetLabel, Is.Not.Null);
            Assert.That(settings.TutorialResetStatusText, Is.Not.Null);

            string[] tutorialFlags = FirstRunOnboardingUI.GetTutorialProgressFlagIds();
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);
            const string futureTutorialFlag = "tutorial.future.keep";
            Assert.That(MetaProgression.SetTutorialFlag(futureTutorialFlag, true), Is.True);
            yield return null;
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);

            pauseMenu.PauseButton.onClick.Invoke();
            yield return null;
            Assert.That(SimulationPauseService.IsPaused, Is.True);
            pauseMenu.SettingsButton.onClick.Invoke();
            yield return null;
            Assert.That(settings.SettingsPanel.activeSelf, Is.True);
            Assert.That(settings.TutorialResetLabel.text,
                Is.EqualTo(SettingsUI.TutorialResetDefaultLabel));
            Assert.That(settings.TutorialResetStatusText.text,
                Is.EqualTo(SettingsUI.TutorialResetDefaultStatus));

            settings.TutorialResetButton.onClick.Invoke();
            yield return null;
            Assert.That(settings.TutorialResetLabel.text,
                Is.EqualTo(SettingsUI.TutorialResetConfirmLabel));
            Assert.That(settings.TutorialResetStatusText.text,
                Is.EqualTo(SettingsUI.TutorialResetConfirmStatus));
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.True, flagId);

            settings.TutorialResetButton.onClick.Invoke();
            yield return null;
            Assert.That(settings.TutorialResetLabel.text,
                Is.EqualTo(SettingsUI.TutorialResetDefaultLabel));
            Assert.That(settings.TutorialResetStatusText.text,
                Is.EqualTo(SettingsUI.TutorialResetSuccessStatus));
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.False, flagId);
            Assert.That(MetaProgression.HasTutorialFlag(futureTutorialFlag), Is.True);

            MetaProgression.Load();
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.False, flagId);
            Assert.That(MetaProgression.HasTutorialFlag(futureTutorialFlag), Is.True);

            settings.CloseButton.onClick.Invoke();
            pauseMenu.ResumeButton.onClick.Invoke();
            for (int frame = 0; frame < 90 && !onboarding.IsWorkerRatioStepVisible; frame++)
                yield return null;

            Assert.That(SimulationPauseService.IsPaused, Is.False);
            Assert.That(onboarding.IsWorkerRatioStepVisible, Is.True,
                "Reset sonrasinda ilk uygun onboarding adimi yeniden baslamalidir.");
        }

        [UnityTest]
        public IEnumerator CompletedTutorial_RealSecondRunRestartKeepsEveryCueClosed()
        {
            GameManager gameManager = GameManager.Instance;
            UIManager uiManager = UIManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(uiManager, Is.Not.Null);
            Assert.That(onboarding, Is.Not.Null);

            string[] tutorialFlags = FirstRunOnboardingUI.GetTutorialProgressFlagIds();
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);

            bool firstRunSnapshotReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    firstRunSnapshotReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(firstRunSnapshotReady, Is.True);

            string firstRunId = gameManager.CurrentRunId;
            Assert.That(firstRunId, Is.Not.Empty);
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery gameStateQuery =
                entityManager.CreateEntityQuery(typeof(GameStateData));
            Entity gameStateEntity = gameStateQuery.GetSingletonEntity();
            GameStateData lethalState =
                entityManager.GetComponentData<GameStateData>(gameStateEntity);
            lethalState.IsGameOver = true;
            lethalState.TotalKills = 3;
            entityManager.SetComponentData(gameStateEntity, lethalState);

            Assert.That(gameManager.SaveRunSnapshot(), Is.False,
                "Lethal first run canli Continue snapshot'i yazamamali.");
            Assert.That(MetaProgression.HasRewardedRun(firstRunId), Is.True,
                "Ikinci run oncesinde ilk run durable death transaction'i tamamlanmali.");
            Assert.That(RunPersistence.HasSave, Is.False);

            uiManager.OnRestart();
            yield return null;

            string secondRunId = gameManager.CurrentRunId;
            Assert.That(secondRunId, Is.Not.Empty);
            Assert.That(secondRunId, Is.Not.EqualTo(firstRunId),
                "GameOver restart yeni authoritative run identity uretmelidir.");
            Assert.That(gameManager.GameState.IsGameOver, Is.False);
            Assert.That(gameManager.ContinuousSiegeCycle.CycleIndex, Is.Zero);
            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Day));
            Assert.That(gameManager.IsMobilePopulationEconomyEnabled(), Is.True,
                "Second run ilk worker cue'sunun normalde eligible oldugu gercek oyun modu olmali.");

            MetaProgression.Load();
            foreach (string flagId in tutorialFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.True, flagId);

            for (int frame = 0; frame < 120; frame++)
                yield return null;

            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            Assert.That(onboarding.ActivePulseTarget, Is.Null);
            Assert.That(onboarding.IsWorkerRatioStepVisible, Is.False);
            Assert.That(onboarding.IsBasicArcherStepVisible, Is.False);
            Assert.That(onboarding.IsLowAmmoStepVisible, Is.False);
            Assert.That(onboarding.IsHeartEntryStepVisible, Is.False);
            Assert.That(onboarding.IsHeartPauseStepVisible, Is.False);
            Assert.That(onboarding.IsCouncilExactStepVisible, Is.False);
            Assert.That(onboarding.IsDaytimeRepairStepVisible, Is.False);
            Assert.That(onboarding.IsNightAbilityKeyStepVisible, Is.False);
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
            DawnRewardToastUI dawnPresentation =
                Object.FindFirstObjectByType<DawnRewardToastUI>();
            AmbientAudioController ambient =
                Object.FindFirstObjectByType<AmbientAudioController>();
            Assert.That(dawnPresentation, Is.Not.Null);
            Assert.That(dawnPresentation.GateTilemap, Is.Not.Null);
            Assert.That(dawnPresentation.ClosedGateTile, Is.Not.Null);
            Assert.That(dawnPresentation.OpenGateTile, Is.Not.Null);
            Assert.That(dawnPresentation.GateGlow, Is.Not.Null);
            Assert.That(ambient, Is.Not.Null);
            Assert.That(ambient.DawnCue, Is.Not.Null);
            Assert.That(ambient.DawnCueSource, Is.Not.Null);
            Assert.That(ambient.DawnCueSource.spatialBlend, Is.Zero.Within(0.001f));

            dawnPresentation.GateOpenDelay = 0f;
            dawnPresentation.GateOpenDuration = 10f;
            for (int frame = 0; frame < 20; frame++)
                yield return null;
            int baselineGateOpenCount = dawnPresentation.GateOpenCount;
            int baselineDawnPresentationCount = dawnPresentation.DawnPresentationPlayCount;
            int baselineDawnCueCount = ambient.DawnCuePlayCount;

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

            for (int frame = 0; frame < 120; frame++)
            {
                MobilePopulationAllocation currentAllocation =
                    entityManager.GetComponentData<MobilePopulationAllocation>(mobileEntity);
                if (currentAllocation.LastArrivalAcceptedCount == 3
                    && dawnPresentation.GateOpenCount > baselineGateOpenCount
                    && ambient.DawnCuePlayCount > baselineDawnCueCount)
                {
                    break;
                }
                yield return null;
            }

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
            Assert.That(dawnPresentation.LastDisplayedGrowth, Is.EqualTo(3));
            Assert.That(dawnPresentation.DawnPresentationPlayCount,
                Is.EqualTo(baselineDawnPresentationCount + 1));
            Assert.That(dawnPresentation.GateOpenCount, Is.EqualTo(baselineGateOpenCount + 1));
            Assert.That(dawnPresentation.IsGateOpen, Is.True);
            Assert.That(dawnPresentation.GateTilemap.GetTile(dawnPresentation.GateCell),
                Is.SameAs(dawnPresentation.OpenGateTile));
            Assert.That(dawnPresentation.GateGlow.intensity, Is.GreaterThan(0f));
            Assert.That(ambient.DawnCuePlayCount, Is.EqualTo(baselineDawnCueCount + 1));
            Assert.That(ambient.DawnCueSource.pitch,
                Is.EqualTo(ambient.DawnCuePitch).Within(0.001f));

            int singleDawnCueCount = ambient.DawnCuePlayCount;
            int singlePresentationCount = dawnPresentation.DawnPresentationPlayCount;
            for (int frame = 0; frame < 30; frame++)
                yield return null;
            Assert.That(ambient.DawnCuePlayCount, Is.EqualTo(singleDawnCueCount),
                "Ayni Dawn icindeki polling new-day cue'yu tekrar oynatmamali.");
            Assert.That(dawnPresentation.DawnPresentationPlayCount,
                Is.EqualTo(singlePresentationCount),
                "Ayni Dawn icindeki polling gate/toast sunumunu tekrar baslatmamali.");

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
        public IEnumerator DawnPresentation_FirstObservationDoesNotReplayButNextEdgePlaysOnce()
        {
            GameManager gameManager = GameManager.Instance;
            AmbientAudioController sceneAmbient =
                Object.FindFirstObjectByType<AmbientAudioController>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(sceneAmbient, Is.Not.Null);
            Assert.That(sceneAmbient.DawnCue, Is.Not.Null);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            Assert.That(cycleSetter, Is.Not.Null);

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Dawn);

            var presentationObject = new GameObject("DawnFirstObservationPresentationTest");
            var audioObject = new GameObject("DawnFirstObservationAudioTest");
            DawnRewardToastUI freshPresentation =
                presentationObject.AddComponent<DawnRewardToastUI>();
            AmbientAudioController freshAmbient =
                audioObject.AddComponent<AmbientAudioController>();
            freshAmbient.DawnCue = sceneAmbient.DawnCue;

            for (int frame = 0; frame < 30; frame++)
                yield return null;

            Assert.That(freshPresentation.DawnPresentationPlayCount, Is.Zero,
                "Dawn icine scene-load/Continue benzeri ilk gozlem sunumu yeniden oynatmamali.");
            Assert.That(freshAmbient.DawnCuePlayCount, Is.Zero,
                "Dawn icine scene-load/Continue benzeri ilk gozlem cue'yu yeniden oynatmamali.");

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Day);
            for (int frame = 0; frame < 30; frame++)
                yield return null;

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Dawn);
            for (int frame = 0; frame < 90
                && (freshPresentation.DawnPresentationPlayCount == 0
                    || freshAmbient.DawnCuePlayCount == 0); frame++)
            {
                yield return null;
            }

            Assert.That(freshPresentation.DawnPresentationPlayCount, Is.EqualTo(1));
            Assert.That(freshAmbient.DawnCuePlayCount, Is.EqualTo(1));

            Object.Destroy(presentationObject);
            Object.Destroy(audioObject);
            yield return null;
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

        [UnityTest]
        public IEnumerator WorkerWorldFeedback_AllResourcesPreserveRepresentativeTruth()
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

            int4 actualCounts = new int4(12, 60, 101, 1000);
            int4 expectedVisualCounts = new int4(12, 24, 27, 32);
            EconomyFocusType[] resources =
            {
                EconomyFocusType.Wood,
                EconomyFocusType.Stone,
                EconomyFocusType.Iron,
                EconomyFocusType.Food
            };
            SetWorkerCounts(entityManager, allocationEntity, populationEntity, actualCounts);

            for (int resourceIndex = 0; resourceIndex < resources.Length; resourceIndex++)
            {
                yield return WaitForWorkerVisualCount(
                    entityManager,
                    resources[resourceIndex],
                    expectedVisualCounts[resourceIndex]);
            }

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);
            yield return null;

            int4 representedTotals = int4.zero;
            bool[][] seenIndices =
            {
                new bool[expectedVisualCounts.x],
                new bool[expectedVisualCounts.y],
                new bool[expectedVisualCounts.z],
                new bool[expectedVisualCounts.w]
            };
            using EntityQuery visualQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ResourceWorkerVisual>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            using NativeArray<Entity> visualEntities = visualQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<ResourceWorkerVisual> visuals =
                visualQuery.ToComponentDataArray<ResourceWorkerVisual>(Allocator.Temp);

            Assert.That(visualEntities.Length, Is.EqualTo(math.csum(expectedVisualCounts)));
            for (int visualIndex = 0; visualIndex < visualEntities.Length; visualIndex++)
            {
                Entity entity = visualEntities[visualIndex];
                ResourceWorkerVisual visual = visuals[visualIndex];
                int resourceIndex = GetTestWorkerResourceIndex(visual.Resource);
                Assert.That(visual.Index,
                    Is.InRange(0, expectedVisualCounts[resourceIndex] - 1));
                Assert.That(seenIndices[resourceIndex][visual.Index], Is.False,
                    $"{visual.Resource} visual index {visual.Index} birden fazla kez kullanildi.");
                seenIndices[resourceIndex][visual.Index] = true;
                Assert.That(visual.RepresentedWorkerCount, Is.GreaterThan(0));
                representedTotals[resourceIndex] += visual.RepresentedWorkerCount;

                Assert.That(entityManager.HasComponent<WorkerLogisticsRoute>(entity), Is.True);
                Assert.That(entityManager.HasComponent<WorkerLogisticsFeedbackState>(entity), Is.True);
                Assert.That(entityManager.HasComponent<WorkerAnimationMaterialProperty>(entity), Is.True);
                Assert.That(entityManager.HasComponent<WorkerFeedbackMaterialProperty>(entity), Is.True);
                Assert.That(entityManager.HasComponent<WorkerCargoColorMaterialProperty>(entity), Is.True);

                WorkerLogisticsRoute route = entityManager.GetComponentData<WorkerLogisticsRoute>(entity);
                WorkerLogisticsFeedbackState feedback =
                    entityManager.GetComponentData<WorkerLogisticsFeedbackState>(entity);
                WorkerFeedbackMaterialProperty materialFeedback =
                    entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(entity);
                WorkerCargoColorMaterialProperty cargoColor =
                    entityManager.GetComponentData<WorkerCargoColorMaterialProperty>(entity);
                Assert.That(route.Speed, Is.GreaterThan(0f));
                Assert.That(math.distance(route.PickupPosition, route.DeliveryPosition),
                    Is.GreaterThan(0.1f));
                Assert.That(feedback.LanternActive, Is.EqualTo(1));
                Assert.That(materialFeedback.Value.y, Is.EqualTo(1f).Within(0.001f));
                Assert.That(materialFeedback.Value.w,
                    Is.EqualTo(WorkerVisualRepresentationUtility.GetProductionFeedbackStrength(
                        visual.RepresentedWorkerCount)).Within(0.001f));
                Assert.That(math.distance(
                        cargoColor.Value,
                        ResourceWorkerVisualStyle.GetCargoTint(visual.Resource)),
                    Is.LessThan(0.001f));
            }

            Assert.That(representedTotals, Is.EqualTo(actualCounts));
            MobilePopulationAllocation allocation =
                entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            Assert.That(new int4(
                    allocation.WoodWorkers,
                    allocation.StoneWorkers,
                    allocation.IronWorkers,
                    allocation.FoodWorkers),
                Is.EqualTo(actualCounts),
                "World feedback actual worker truth'ini degistirmemeli.");
        }

        [UnityTest]
        public IEnumerator DayPresentation_WarmLightKeepsProductionReadableAndWorkerAmbienceScalesWithWorkers()
        {
            GameManager gameManager = GameManager.Instance;
            DayNightOverlayController dayPresentation =
                Object.FindFirstObjectByType<DayNightOverlayController>();
            AmbientAudioController ambient =
                Object.FindFirstObjectByType<AmbientAudioController>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(dayPresentation, Is.Not.Null);
            Assert.That(dayPresentation.GlobalLight, Is.Not.Null);
            Assert.That(ambient, Is.Not.Null);
            Assert.That(ambient.WorkerFoleyClips, Has.Length.EqualTo(4));
            Assert.That(ambient.WorkerFoleyClips, Has.None.Null);

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
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 101);
            for (int frame = 0; frame < 10; frame++)
                yield return null;
            int authoritativeWoodWorkers = ReadWoodWorkerCount(entityManager, allocationEntity);
            int expectedWoodVisuals = WorkerVisualRepresentationUtility.GetRepresentativeCount(
                authoritativeWoodWorkers);
            Assert.That(authoritativeWoodWorkers, Is.GreaterThan(0));
            yield return WaitForWorkerVisualCount(
                entityManager,
                EconomyFocusType.Wood,
                expectedWoodVisuals);

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = cycle.DayDuration + cycle.DuskDuration + cycle.NightDuration + 0.1f;
            entityManager.SetComponentData(cycleEntity, cycle);
            for (int frame = 0; frame < 20; frame++)
                yield return null;

            ambient.WorkerFoleyMinInterval = 0.10f;
            ambient.WorkerFoleyMaxInterval = 0.10f;
            dayPresentation.LightMoveSpeed = 100f;
            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = 0f;
            entityManager.SetComponentData(cycleEntity, cycle);

            for (int frame = 0; frame < 10; frame++)
                yield return null;

            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Day));
            Assert.That(dayPresentation.GlobalLight.color.r,
                Is.EqualTo(dayPresentation.DayLightColor.r).Within(0.01f));
            Assert.That(dayPresentation.GlobalLight.color.g,
                Is.EqualTo(dayPresentation.DayLightColor.g).Within(0.01f));
            Assert.That(dayPresentation.GlobalLight.color.b,
                Is.EqualTo(dayPresentation.DayLightColor.b).Within(0.01f));
            Assert.That(dayPresentation.GlobalLight.intensity,
                Is.EqualTo(dayPresentation.DayLightIntensity).Within(0.01f));
            Assert.That(dayPresentation.DayLightColor.r,
                Is.GreaterThan(dayPresentation.DayLightColor.b + 0.10f));
            Assert.That(dayPresentation.DayLightIntensity, Is.GreaterThanOrEqualTo(1f));

            Entity worker = FindWorkerVisual(entityManager, EconomyFocusType.Wood);
            WorkerFeedbackMaterialProperty materialFeedback =
                entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            Assert.That(materialFeedback.Value.w, Is.GreaterThan(0.5f),
                "Day production readability gerçek represented worker weight'ini kullanmali.");

            WorkerLogisticsRoute route = entityManager.GetComponentData<WorkerLogisticsRoute>(worker);
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(worker);
            WorkerLogisticsFeedbackState feedback =
                entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            route.MovingToHub = 1;
            route.RouteLeg = 2;
            route.WaitTimer = 0f;
            transform.Position = route.DeliveryPosition;
            feedback.IsCarrying = 1;
            entityManager.SetComponentData(worker, route);
            entityManager.SetComponentData(worker, transform);
            entityManager.SetComponentData(worker, feedback);
            yield return null;

            materialFeedback = entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            Assert.That(materialFeedback.Value.z, Is.GreaterThan(0.8f),
                "Day delivery pulse production teslimini okunur gostermeli.");

            int baselineFoleyCount = ambient.WorkerFoleyPlayCount;
            for (int frame = 0; frame < 180
                && ambient.WorkerFoleyPlayCount == baselineFoleyCount; frame++)
            {
                yield return null;
            }

            Assert.That(ambient.WorkerActivity01, Is.GreaterThan(0f));
            Assert.That(ambient.WorkerFoleyPlayCount, Is.GreaterThan(baselineFoleyCount));
            Assert.That(ambient.WorkerFoleySource, Is.Not.Null);
            Assert.That(ambient.WorkerFoleySource.spatialBlend, Is.Zero.Within(0.001f));
            Assert.That(ambient.WorkerFoleySource.volume, Is.EqualTo(1f).Within(0.001f),
                "One-shot worker ambience source gain'i sifir olmamali.");
        }

        [UnityTest]
        public IEnumerator DuskPresentation_CrossesAmberIntoIndigo_LightsLanternsAndPlaysOneRiser()
        {
            GameManager gameManager = GameManager.Instance;
            DayNightOverlayController presentation =
                Object.FindFirstObjectByType<DayNightOverlayController>();
            AmbientAudioController ambient =
                Object.FindFirstObjectByType<AmbientAudioController>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.GlobalLight, Is.Not.Null);
            Assert.That(ambient, Is.Not.Null);
            Assert.That(ambient.DuskRiser, Is.Not.Null);
            Assert.That(ambient.DuskRiserSource, Is.Not.Null);
            Assert.That(ambient.DuskRiserSource.spatialBlend, Is.Zero.Within(0.001f));
            Assert.That(ambient.DuskRiserSource.volume, Is.EqualTo(1f).Within(0.001f),
                "One-shot Dusk riser source gain'i sifir olmamali.");

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
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);

            SetWoodWorkerCount(entityManager, allocationEntity, populationEntity, 101);
            for (int frame = 0; frame < 10; frame++)
                yield return null;
            int expectedVisuals = WorkerVisualRepresentationUtility.GetRepresentativeCount(
                ReadWoodWorkerCount(entityManager, allocationEntity));
            yield return WaitForWorkerVisualCount(
                entityManager,
                EconomyFocusType.Wood,
                expectedVisuals);

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = 0f;
            entityManager.SetComponentData(cycleEntity, cycle);
            for (int frame = 0; frame < 20; frame++)
                yield return null;
            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Day));
            int baselineRiserCount = ambient.DuskRiserPlayCount;

            presentation.LightMoveSpeed = 100f;
            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = cycle.DayDuration
                + cycle.DuskDuration * DayNightOverlayController.DuskAmberPeakProgress;
            entityManager.SetComponentData(cycleEntity, cycle);
            float duskDeadline = Time.realtimeSinceStartup + 2f;
            while ((gameManager.ContinuousSiegeCycle.Phase != SiegeCyclePhase.Dusk
                    || ambient.DuskRiserPlayCount == baselineRiserCount)
                   && Time.realtimeSinceStartup < duskDeadline)
            {
                yield return null;
            }

            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Dusk),
                "Amber checkpoint Dusk fazi icinde kalmali.");
            Assert.That(ambient.DuskRiserPlayCount, Is.EqualTo(baselineRiserCount + 1),
                "Dusk faz girisi canonical riser'i tam bir kez oynatmali.");
            Assert.That(ambient.DuskRiserSource.pitch,
                Is.EqualTo(ambient.DuskRiserPitch).Within(0.001f));

            Entity worker = FindWorkerVisual(entityManager, EconomyFocusType.Wood);
            WorkerLogisticsFeedbackState feedback =
                entityManager.GetComponentData<WorkerLogisticsFeedbackState>(worker);
            WorkerFeedbackMaterialProperty materialFeedback =
                entityManager.GetComponentData<WorkerFeedbackMaterialProperty>(worker);
            Assert.That(feedback.LanternActive, Is.EqualTo(1),
                "Dusk worker temsilinde lantern state aktif olmali.");
            Assert.That(materialFeedback.Value.y, Is.EqualTo(1f).Within(0.001f));

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = cycle.DayDuration + cycle.DuskDuration * 0.80f;
            entityManager.SetComponentData(cycleEntity, cycle);
            for (int frame = 0; frame < 8; frame++)
                yield return null;

            ContinuousSiegeCycleData runtimeCycle = gameManager.ContinuousSiegeCycle;
            Assert.That(runtimeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Dusk),
                "Indigo checkpoint Dusk fazi icinde kalmali.");
            presentation.ResolvePhaseLightTarget(
                runtimeCycle.Phase,
                runtimeCycle.PhaseProgress01,
                out Color expectedColor,
                out float expectedIntensity);
            Assert.That(expectedColor.b, Is.GreaterThan(expectedColor.r),
                "Dusk'un son bolumu amber'den indigo tarafa gecmis olmali.");
            Assert.That(presentation.GlobalLight.color.r,
                Is.EqualTo(expectedColor.r).Within(0.02f));
            Assert.That(presentation.GlobalLight.color.g,
                Is.EqualTo(expectedColor.g).Within(0.02f));
            Assert.That(presentation.GlobalLight.color.b,
                Is.EqualTo(expectedColor.b).Within(0.02f));
            Assert.That(presentation.GlobalLight.intensity,
                Is.EqualTo(expectedIntensity).Within(0.02f));

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleTimer = cycle.DayDuration + cycle.DuskDuration * 0.10f;
            entityManager.SetComponentData(cycleEntity, cycle);
            for (int frame = 0; frame < 30; frame++)
                yield return null;
            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Dusk),
                "Repeated-poll checkpoint Dusk fazi icinde kalmali.");
            Assert.That(ambient.DuskRiserPlayCount, Is.EqualTo(baselineRiserCount + 1),
                "Ayni Dusk fazinda polling riser'i tekrar oynatmamalidir.");
        }

        [UnityTest]
        public IEnumerator NightPresentation_UsesColdMoonWindowsDensityBedAndAggregatedSalvoBudget()
        {
            GameManager gameManager = GameManager.Instance;
            DayNightOverlayController presentation =
                Object.FindFirstObjectByType<DayNightOverlayController>();
            AmbientAudioController ambient =
                Object.FindFirstObjectByType<AmbientAudioController>();
            CombatFeedbackBridge feedback =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.GlobalLight, Is.Not.Null);
            Assert.That(presentation.CastleWindowLights, Is.Not.Null);
            Assert.That(presentation.CastleWindowLights.Length, Is.EqualTo(4),
                "NewGameScene tam dort canonical castle-window glow binding'i tasimali.");
            Assert.That(ambient, Is.Not.Null);
            Assert.That(ambient.NightHordeLoop, Is.Not.Null);
            Assert.That(ambient.NightHordeSource, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.ArrowShootClip, Is.Not.Null);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery waveQuery = entityManager.CreateEntityQuery(typeof(WaveStateData));
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(typeof(ContinuousSiegeCycleData));
            using EntityQuery sfxQuery = entityManager.CreateEntityQuery(typeof(CombatSfxEvent));
            Entity waveEntity = waveQuery.GetSingletonEntity();
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo waveProperty = typeof(GameManager).GetProperty(
                "WaveState",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            MethodInfo waveSetter = waveProperty?.GetSetMethod(true);
            Assert.That(cycleSetter, Is.Not.Null);
            Assert.That(waveSetter, Is.Not.Null);

            Time.timeScale = 0f;
            presentation.LightMoveSpeed = 100f;
            presentation.WindowLightMoveSpeed = 100f;
            presentation.WindowLightFlickerAmount = 0f;
            ambient.NightHordeFadeSpeed = 100f;
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Night);

            WaveStateData wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 10_000;
            wave.StressTestMode = false;
            entityManager.SetComponentData(waveEntity, wave);
            waveSetter.Invoke(gameManager, new object[] { wave });

            for (int frame = 0; frame < 20; frame++)
                yield return null;
            // AmbientAudioController authoritative cycle'i 0.2s unscaled cadence ile poll eder.
            // Test runner frame suresi degisken oldugu icin cadence'in dolmasini explicit bekle.
            yield return new WaitForSecondsRealtime(0.25f);
            yield return null;

            Assert.That(gameManager.ContinuousSiegeCycle.Phase, Is.EqualTo(SiegeCyclePhase.Night));
            Assert.That(presentation.GlobalLight.color.r,
                Is.EqualTo(presentation.NightLightColor.r).Within(0.02f));
            Assert.That(presentation.GlobalLight.color.g,
                Is.EqualTo(presentation.NightLightColor.g).Within(0.02f));
            Assert.That(presentation.GlobalLight.color.b,
                Is.EqualTo(presentation.NightLightColor.b).Within(0.02f));
            Assert.That(presentation.GlobalLight.intensity,
                Is.EqualTo(presentation.NightLightIntensity).Within(0.02f));
            Assert.That(presentation.GlobalLight.color.b,
                Is.GreaterThan(presentation.GlobalLight.color.r + 0.40f),
                "Night global light soguk-ay siluetini mavi kanalda tasimali.");

            for (int i = 0; i < presentation.CastleWindowLights.Length; i++)
            {
                var windowLight = presentation.CastleWindowLights[i];
                Assert.That(windowLight, Is.Not.Null);
                Assert.That(windowLight.lightType,
                    Is.EqualTo(UnityEngine.Rendering.Universal.Light2D.LightType.Point));
                Assert.That(windowLight.overlapOperation,
                    Is.EqualTo(UnityEngine.Rendering.Universal.Light2D.OverlapOperation.Additive));
                Assert.That(windowLight.intensity,
                    Is.EqualTo(presentation.WindowLightIntensity).Within(0.02f));
            }

            float expectedNightHordeActivity = AmbientAudioController.ResolveNightHordeActivity01(
                gameManager.ContinuousSiegeCycle.Phase,
                gameManager.ContinuousSiegeCycle.HordePressure01,
                gameManager.WaveState.ZombiesAlive);
            Assert.That(ambient.NightHordeActivity01,
                Is.EqualTo(expectedNightHordeActivity).Within(0.01f));
            Assert.That(ambient.NightHordeActivity01, Is.GreaterThan(0.5f));
            Assert.That(ambient.NightHordeActivity01, Is.LessThanOrEqualTo(1f));
            Assert.That(ambient.NightHordeSource.loop, Is.True);
            Assert.That(ambient.NightHordeSource.spatialBlend, Is.Zero.Within(0.001f));
            Assert.That(ambient.NightHordeSource.isPlaying, Is.True);
            Assert.That(ambient.NightHordeSource.volume,
                Is.EqualTo(ambient.NightHordeVolume
                    * ambient.NightHordeActivity01
                    * SoundSettings.AmbienceVolume).Within(0.02f));

            if (!sfxQuery.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(sfxQuery);
            FieldInfo lastSfxTimesField = typeof(CombatFeedbackBridge).GetField(
                "_lastSfxTimes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lastSfxTimesField, Is.Not.Null);
            var lastSfxTimes = (float[])lastSfxTimesField.GetValue(feedback);
            lastSfxTimes[(int)CombatSfxType.ArrowShoot] = -999f;

            int audioSourceCount = feedback.GetComponentsInChildren<AudioSource>(true).Length;
            int baselineSalvos = feedback.TotalArrowSalvosPlayed;
            const int archerCapSalvo = 1_000;
            for (int i = 0; i < archerCapSalvo; i++)
            {
                Entity sfxEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(sfxEntity, new CombatSfxEvent
                {
                    Position = new float3(i % 40, i / 40, 0f),
                    Type = CombatSfxType.ArrowShoot,
                    Volume = 0.35f,
                    Pitch = 1f
                });
            }

            for (int frame = 0; frame < 30
                && feedback.TotalArrowSalvosPlayed == baselineSalvos; frame++)
            {
                yield return null;
            }

            Assert.That(feedback.LastProcessedSfxEventCount, Is.EqualTo(archerCapSalvo));
            Assert.That(feedback.LastArrowSalvoSize, Is.EqualTo(archerCapSalvo));
            Assert.That(feedback.TotalArrowSalvosPlayed, Is.EqualTo(baselineSalvos + 1),
                "Bir frame'deki 1000 archer shoot event'i tek salvo cue olmali.");
            Assert.That(feedback.LastFrameSfxPlayedCount,
                Is.LessThanOrEqualTo(feedback.MaxSfxPlayedPerFrame));
            Assert.That(sfxQuery.CalculateEntityCount(), Is.Zero);
            Assert.That(feedback.GetComponentsInChildren<AudioSource>(true).Length,
                Is.EqualTo(audioSourceCount),
                "Salvo yogunlugu yeni AudioSource uretmemeli; sabit pool korunmali.");

            int firstSalvoTotal = feedback.TotalArrowSalvosPlayed;
            for (int i = 0; i < 16; i++)
            {
                Entity sfxEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(sfxEntity, new CombatSfxEvent
                {
                    Position = float3.zero,
                    Type = CombatSfxType.ArrowShoot,
                    Volume = 0.35f,
                    Pitch = 1f
                });
            }

            for (int frame = 0; frame < 10
                && feedback.LastProcessedSfxEventCount != 16; frame++)
            {
                yield return null;
            }

            Assert.That(feedback.LastProcessedSfxEventCount, Is.EqualTo(16));
            Assert.That(feedback.LastArrowSalvoSize, Is.EqualTo(16));
            Assert.That(feedback.TotalArrowSalvosPlayed, Is.EqualTo(firstSalvoTotal),
                "Night shoot rate-limit ayni scaled-time anindaki ikinci salvolari yutmali.");
            Assert.That(feedback.LastFrameSfxPlayedCount, Is.Zero);
            Assert.That(sfxQuery.CalculateEntityCount(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator DenseArrowHits_EmitSpatiallySampledVfxAndAggregatedSfx()
        {
            CombatFeedbackBridge feedback =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(feedback, Is.Not.Null);
            feedback.enabled = false;
            Time.timeScale = 0f;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery vfxQuery = entityManager.CreateEntityQuery(typeof(CombatVfxEvent));
            using EntityQuery sfxQuery = entityManager.CreateEntityQuery(typeof(CombatSfxEvent));
            using EntityQuery telemetryQuery = entityManager.CreateEntityQuery(
                typeof(CombatFeedbackBudgetTelemetryData));
            if (!vfxQuery.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(vfxQuery);
            if (!sfxQuery.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(sfxQuery);
            Assert.That(telemetryQuery.CalculateEntityCount(), Is.EqualTo(1));

            const int hitCount = 1_000;
            const int spatialCellCount = 40;
            const int frostCellCount = 6;
            var targets = new Entity[hitCount];
            for (int i = 0; i < hitCount; i++)
            {
                int cellIndex = i % spatialCellCount;
                float3 position = new float3(
                    120f + (cellIndex % 10) * 1.5f,
                    10f + (cellIndex / 10) * 1.5f,
                    MobileCastleRenderDepth.ProjectileZ);
                bool frost = cellIndex < frostCellCount;

                Entity target = entityManager.CreateEntity(
                    typeof(ZombieTag),
                    typeof(ZombieStats),
                    typeof(LocalTransform));
                entityManager.SetComponentData(target, new ZombieStats
                {
                    MoveSpeed = 0f,
                    MaxHP = 100_000f,
                    CurrentHP = 100_000f
                });
                entityManager.SetComponentData(target,
                    LocalTransform.FromPosition(position));
                targets[i] = target;

                Entity arrow = entityManager.CreateEntity(
                    typeof(ArrowTag),
                    typeof(ArrowProjectile),
                    typeof(LocalTransform));
                entityManager.SetComponentData(arrow, new ArrowProjectile
                {
                    Speed = 0f,
                    Damage = 1f,
                    Target = target,
                    ArcherType = frost ? ArcherType.Frost : ArcherType.Basic,
                    SlowMultiplier = 1f,
                    RemainingLifetime = 1f
                });
                entityManager.SetComponentData(arrow,
                    LocalTransform.FromPosition(position));
            }

            yield return null;

            Assert.That(vfxQuery.CalculateEntityCount(),
                Is.EqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(sfxQuery.CalculateEntityCount(), Is.EqualTo(2),
                "1000 raw hit yalniz ArrowHit ve FrostHit icin iki toplu SFX event'i uretmeli.");
            CombatFeedbackBudgetTelemetryData telemetry =
                telemetryQuery.GetSingleton<CombatFeedbackBudgetTelemetryData>();
            Assert.That(telemetry.LastSpatialCandidateCount, Is.EqualTo(spatialCellCount));
            Assert.That(telemetry.LastVfxEventsEmitted,
                Is.EqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(telemetry.LastSfxEventsEmitted, Is.EqualTo(2));
            Assert.That(telemetry.LastVfxCandidatesDropped,
                Is.EqualTo(spatialCellCount - CombatHitFeedbackBudget.MaxVfxEventsPerFrame));

            using NativeArray<CombatVfxEvent> vfxEvents =
                vfxQuery.ToComponentDataArray<CombatVfxEvent>(Allocator.Temp);
            int arrowVfxCount = 0;
            int frostVfxCount = 0;
            for (int i = 0; i < vfxEvents.Length; i++)
            {
                if (vfxEvents[i].Type == CombatVfxType.FrostHit)
                    frostVfxCount++;
                else if (vfxEvents[i].Type == CombatVfxType.ArrowHit)
                    arrowVfxCount++;
            }
            Assert.That(frostVfxCount, Is.EqualTo(frostCellCount));
            Assert.That(arrowVfxCount,
                Is.EqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame - frostCellCount));

            using NativeArray<CombatSfxEvent> sfxEvents =
                sfxQuery.ToComponentDataArray<CombatSfxEvent>(Allocator.Temp);
            int representedHitCells = 0;
            for (int i = 0; i < sfxEvents.Length; i++)
                representedHitCells += sfxEvents[i].Multiplicity;
            Assert.That(representedHitCells, Is.EqualTo(spatialCellCount));

            entityManager.DestroyEntity(vfxQuery);
            entityManager.DestroyEntity(sfxQuery);
            for (int i = 0; i < targets.Length; i++)
            {
                if (entityManager.Exists(targets[i]))
                    entityManager.DestroyEntity(targets[i]);
            }
            feedback.enabled = true;
        }

        [UnityTest]
        public IEnumerator HitFeedbackBridge_EnforcesPlaybackBudgetAndRateLimit()
        {
            CombatFeedbackBridge feedback =
                Object.FindFirstObjectByType<CombatFeedbackBridge>();
            Assert.That(feedback, Is.Not.Null);
            Assert.That(feedback.HitFlipbookSprites, Is.Not.Null.And.Not.Empty);
            Assert.That(feedback.HitFlipbookPoolSize, Is.EqualTo(128));
            Assert.That(feedback.MaxHitVfxPlayedPerFrame,
                Is.EqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Time.timeScale = 0f;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery vfxQuery = entityManager.CreateEntityQuery(typeof(CombatVfxEvent));
            if (!vfxQuery.IsEmptyIgnoreFilter)
                entityManager.DestroyEntity(vfxQuery);

            FieldInfo hitVfxTimeField = typeof(CombatFeedbackBridge).GetField(
                "_lastHitVfxPlaybackTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(hitVfxTimeField, Is.Not.Null);
            hitVfxTimeField.SetValue(feedback, -999f);

            long baselinePlayed = feedback.TotalHitVfxPlayedCount;
            long baselineDropped = feedback.TotalHitVfxDroppedCount;
            const int firstBatchSize = 80;
            for (int i = 0; i < firstBatchSize; i++)
            {
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new CombatVfxEvent
                {
                    Position = new float3(i * 0.1f, 0f, MobileCastleRenderDepth.ProjectileZ),
                    Direction = new float3(1f, 0f, 0f),
                    Type = CombatVfxType.ArrowHit,
                    Scale = 0.08f
                });
            }

            yield return null;

            Assert.That(feedback.LastProcessedVfxEventCount, Is.EqualTo(firstBatchSize));
            Assert.That(feedback.LastFrameHitVfxPlayedCount,
                Is.EqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(feedback.LastFrameHitVfxDroppedCount,
                Is.EqualTo(firstBatchSize - CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(feedback.ActiveHitFlipbookCount,
                Is.LessThanOrEqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(vfxQuery.CalculateEntityCount(), Is.Zero);

            const int secondBatchSize = 8;
            for (int i = 0; i < secondBatchSize; i++)
            {
                Entity eventEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(eventEntity, new CombatVfxEvent
                {
                    Position = float3.zero,
                    Direction = new float3(1f, 0f, 0f),
                    Type = CombatVfxType.ArrowHit,
                    Scale = 0.08f
                });
            }

            yield return null;

            Assert.That(feedback.LastProcessedVfxEventCount, Is.EqualTo(secondBatchSize));
            Assert.That(feedback.LastFrameHitVfxPlayedCount, Is.Zero,
                "Ayni scaled-time penceresindeki ikinci hit burst'u rate-limit tarafindan yutulmali.");
            Assert.That(feedback.LastFrameHitVfxDroppedCount, Is.EqualTo(secondBatchSize));
            Assert.That(feedback.TotalHitVfxPlayedCount,
                Is.EqualTo(baselinePlayed + CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            Assert.That(feedback.TotalHitVfxDroppedCount,
                Is.EqualTo(baselineDropped + firstBatchSize
                    - CombatHitFeedbackBudget.MaxVfxEventsPerFrame + secondBatchSize));
            Assert.That(vfxQuery.CalculateEntityCount(), Is.Zero);
        }

        [UnityTest]
        public IEnumerator PhaseWorldReadability_UsesSkyBoundedParticlesAndNoLargePhaseText()
        {
            GameManager gameManager = GameManager.Instance;
            MomentVignetteUI atmosphere =
                Object.FindFirstObjectByType<MomentVignetteUI>();
            DayNightOverlayController grading =
                Object.FindFirstObjectByType<DayNightOverlayController>();
            AmbientAudioController audio =
                Object.FindFirstObjectByType<AmbientAudioController>();
            Assert.That(gameManager, Is.Not.Null);
            Assert.That(atmosphere, Is.Not.Null);
            Assert.That(grading, Is.Not.Null);
            Assert.That(audio, Is.Not.Null);
            Assert.That(atmosphere.SkyCamera, Is.SameAs(Camera.main));
            Assert.That(atmosphere.AtmosphereParticles, Is.Not.Null);
            Assert.That(atmosphere.AtmosphereParticles.main.maxParticles,
                Is.EqualTo(MomentVignetteUI.DefaultMaxParticles));
            Assert.That(atmosphere.AtmosphereParticles.GetComponent<ParticleSystemRenderer>()
                .sharedMaterial.name, Is.EqualTo("PhaseAtmosphereParticles"));
            Assert.That(atmosphere.DawnPeak, Is.Zero);

            ParticleSystem[] particleSystems = Object.FindObjectsByType<ParticleSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int sceneParticleCount = 0;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i].gameObject.scene == SceneManager.GetActiveScene())
                    sceneParticleCount++;
            }
            Assert.That(sceneParticleCount, Is.EqualTo(1),
                "Phase readability tek bounded atmosphere ParticleSystem kullanmali.");

            TMPro.TMP_Text[] texts = Object.FindObjectsByType<TMPro.TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            string[] legacyPhaseObjectNames =
            {
                "CyclePhaseText",
                "CycleDayLabelText",
                "CycleDuskLabelText",
                "CycleNightLabelText"
            };
            for (int i = 0; i < texts.Length; i++)
            {
                for (int n = 0; n < legacyPhaseObjectNames.Length; n++)
                {
                    if (texts[i].name != legacyPhaseObjectNames[n])
                        continue;

                    Assert.That(texts[i].gameObject.activeInHierarchy, Is.False,
                        texts[i].name + " player-facing olmamali.");
                }
            }

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            Assert.That(cycleSetter, Is.Not.Null);

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Day);
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            Assert.That(atmosphere.TransitionBurstPlayCount, Is.Zero,
                "Ilk scene gozlemi transition burst uretmemeli.");

            atmosphere.SkyColorMoveSpeed = 100f;
            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Dusk);
            for (int frame = 0; frame < 12; frame++)
                yield return null;

            PhaseAtmosphereProfile duskProfile = atmosphere.ResolvePhaseProfile(
                SiegeCyclePhase.Dusk,
                gameManager.ContinuousSiegeCycle.PhaseProgress01);
            Assert.That(atmosphere.TransitionBurstPlayCount, Is.EqualTo(1));
            Assert.That(atmosphere.LastTransitionBurstCount,
                Is.EqualTo(MomentVignetteUI.ResolveTransitionBurstCount(
                    SiegeCyclePhase.Dusk)));
            Assert.That(atmosphere.CurrentEmissionRate,
                Is.EqualTo(duskProfile.EmissionRate).Within(0.01f));
            Assert.That(atmosphere.CurrentParticleColor.r,
                Is.EqualTo(duskProfile.ParticleColor.r).Within(0.01f));
            Assert.That(atmosphere.SkyCamera.backgroundColor.r,
                Is.EqualTo(duskProfile.SkyColor.r).Within(0.02f));
            Assert.That(atmosphere.AtmosphereParticles.particleCount,
                Is.LessThanOrEqualTo(MomentVignetteUI.DefaultMaxParticles));

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Night);
            for (int frame = 0; frame < 8; frame++)
                yield return null;
            Assert.That(atmosphere.TransitionBurstPlayCount, Is.EqualTo(2));
            Assert.That(atmosphere.CurrentEmissionRate,
                Is.EqualTo(atmosphere.NightEmissionRate).Within(0.01f));

            SetOnboardingCycle(
                entityManager,
                cycleEntity,
                gameManager,
                cycleSetter,
                0,
                SiegeCyclePhase.Dawn);
            for (int frame = 0; frame < 8; frame++)
                yield return null;
            Assert.That(atmosphere.TransitionBurstPlayCount, Is.EqualTo(3));
            Assert.That(atmosphere.LastTransitionBurstCount,
                Is.EqualTo(MomentVignetteUI.ResolveTransitionBurstCount(
                    SiegeCyclePhase.Dawn)));
            Assert.That(atmosphere.AtmosphereParticles.particleCount,
                Is.LessThanOrEqualTo(MomentVignetteUI.DefaultMaxParticles));
            Assert.That(grading.GlobalLight, Is.Not.Null);
            Assert.That(audio.DawnCue, Is.Not.Null);
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

        private static void SetWorkerCounts(EntityManager entityManager, Entity allocationEntity,
            Entity populationEntity, int4 workerCounts)
        {
            workerCounts = math.max(workerCounts, int4.zero);
            int workerTotal = math.csum(workerCounts);
            PopulationState population = entityManager.GetComponentData<PopulationState>(populationEntity);
            population.Total = workerTotal + population.Archers;
            population.Workers = workerTotal;
            population.Idle = 0;
            population.Capacity = Mathf.Max(population.Capacity, population.Total);
            population.BaseCapacity = Mathf.Max(population.BaseCapacity, population.Capacity);
            entityManager.SetComponentData(populationEntity, population);

            MobilePopulationAllocation allocation =
                entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            allocation.WoodWorkers = workerCounts.x;
            allocation.StoneWorkers = workerCounts.y;
            allocation.IronWorkers = workerCounts.z;
            allocation.FoodWorkers = workerCounts.w;
            allocation.WoodTargetRatioBps = 1000;
            allocation.StoneTargetRatioBps = 2000;
            allocation.IronTargetRatioBps = 3000;
            allocation.FoodTargetRatioBps = 4000;
            allocation.IdlePopulation = 0;
            allocation.LastObservedPopulation = population.Total;
            allocation.AutoAllocationInitialized = 1;
            entityManager.SetComponentData(allocationEntity, allocation);
        }

        private static int GetTestWorkerResourceIndex(EconomyFocusType resource)
        {
            return EconomyFocusUtility.Normalize(resource) switch
            {
                EconomyFocusType.Stone => 1,
                EconomyFocusType.Iron => 2,
                EconomyFocusType.Food => 3,
                _ => 0
            };
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
