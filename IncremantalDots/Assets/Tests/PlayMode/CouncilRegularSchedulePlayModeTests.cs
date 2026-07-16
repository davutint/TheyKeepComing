using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class CouncilRegularSchedulePlayModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();
        private FieldInfo _catalogField;
        private CouncilEventCatalogSO _originalCatalog;
        private MethodInfo _resetCouncilState;
        private MethodInfo _cycleSetter;
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
            int previousGameManagerId = GameManager.Instance != null
                ? GameManager.Instance.GetInstanceID()
                : 0;
            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 300; frame++)
            {
                GameManager current = GameManager.Instance;
                bool newSceneOwnerReady = current != null
                    && current.GetInstanceID() != previousGameManagerId
                    && SceneManager.GetActiveScene().name == "NewGameScene"
                    && current.ContinuousSiegeCycle.Enabled;
                if (newSceneOwnerReady)
                    break;
                yield return null;
            }

            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("NewGameScene"));
            Assert.That(GameManager.Instance.GetInstanceID(), Is.Not.EqualTo(previousGameManagerId),
                "Test eski sahnenin GameManager owner'ina baglandi.");
            Assert.That(GameManager.Instance.ContinuousSiegeCycle.Enabled, Is.True,
                "GameManager continuous cycle runtime'i hazir olmadi.");

            _catalogField = typeof(GameManager).GetField(
                "councilCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _resetCouncilState = typeof(GameManager).GetMethod(
                "ResetCouncilState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            _cycleSetter = cycleProperty?.GetSetMethod(true);

            Assert.That(_catalogField, Is.Not.Null);
            Assert.That(_resetCouncilState, Is.Not.Null);
            Assert.That(_cycleSetter, Is.Not.Null);

            _originalCatalog = _catalogField.GetValue(GameManager.Instance) as CouncilEventCatalogSO;
            _catalogField.SetValue(GameManager.Instance, CreateCatalog());
            _resetCouncilState.Invoke(GameManager.Instance, null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameManager.Instance != null)
            {
                _catalogField?.SetValue(GameManager.Instance, _originalCatalog);
                _resetCouncilState?.Invoke(GameManager.Instance, null);
            }

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                Object.Destroy(_createdObjects[i]);
            _createdObjects.Clear();
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
        public IEnumerator RegularCouncil_OpensExactlyOnThreeSixNineCadence_OncePerDay()
        {
            GameManager gameManager = GameManager.Instance;

            ContinuousSiegeCycleData offPhaseCycle = gameManager.ContinuousSiegeCycle;
            offPhaseCycle.Enabled = true;
            offPhaseCycle.CycleIndex = 2;
            offPhaseCycle.Phase = SiegeCyclePhase.Night;
            _cycleSetter.Invoke(gameManager, new object[] { offPhaseCycle });
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.False,
                "Day 3 Night fazinda regular Council acilmamali.");

            for (int day = 1; day <= 12; day++)
            {
                ContinuousSiegeCycleData cycle = gameManager.ContinuousSiegeCycle;
                cycle.Enabled = true;
                cycle.CycleIndex = day - 1;
                cycle.Phase = SiegeCyclePhase.Dawn;
                _cycleSetter.Invoke(gameManager, new object[] { cycle });

                bool opened = gameManager.TryOpenRegularCouncilEvent();
                bool expected = CouncilRegularSchedule.IsRegularDay(day);
                Assert.That(opened, Is.EqualTo(expected), $"Day {day} regular sonucu yanlis.");
                Assert.That(gameManager.ActiveCouncilEvent != null, Is.EqualTo(expected),
                    $"Day {day} active Council state'i yanlis.");

                Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.False,
                    $"Day {day} ikinci kez regular Council acildi.");

                if (opened)
                    gameManager.ExpireCouncilEvent();
            }

            FieldInfo lastRegularField = typeof(GameManager).GetField(
                "_lastRegularCouncilDay",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lastRegularField, Is.Not.Null);
            Assert.That(lastRegularField.GetValue(gameManager), Is.EqualTo(12));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApprovedCouncilChoice_WritesCuratedChainFlagInLiveGameManager()
        {
            GameManager gameManager = GameManager.Instance;
            ContinuousSiegeCycleData cycle = gameManager.ContinuousSiegeCycle;
            cycle.Enabled = true;
            cycle.CycleIndex = 2;
            cycle.Phase = SiegeCyclePhase.Dawn;
            _cycleSetter.Invoke(gameManager, new object[] { cycle });

            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            Assert.That(gameManager.ActiveCouncilEvent.TemplateId, Is.EqualTo("schedule_template"));
            Assert.That(gameManager.ChooseCouncilOption(true), Is.True);

            FieldInfo flagsField = typeof(GameManager).GetField(
                "_councilFlags",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(flagsField, Is.Not.Null);
            var flags = flagsField.GetValue(gameManager) as Dictionary<string, int>;
            Assert.That(flags, Is.Not.Null);
            Assert.That(flags.ContainsKey("schedule_followup_ready"), Is.True,
                "Approved source/branch flag'i live GameManager state'ine yazilmadi.");
            Assert.That(flags["schedule_followup_ready"], Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstRegularCouncilOnboarding_PulsesWholeExactCard_AndCompletesOnPlayerChoice()
        {
            GameManager gameManager = GameManager.Instance;
            FirstRunOnboardingUI onboarding =
                Object.FindFirstObjectByType<FirstRunOnboardingUI>();
            CouncilEventUI council = Object.FindFirstObjectByType<CouncilEventUI>();
            Assert.That(onboarding, Is.Not.Null);
            Assert.That(council, Is.Not.Null);
            Assert.That(onboarding.Council, Is.SameAs(council));

            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.WorkerRatioFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.BasicArcherFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.LowAmmoFlagId, true), Is.True);
            Assert.That(MetaProgression.SetTutorialFlag(
                FirstRunOnboardingUI.HeartEntryFlagId, true), Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId), Is.False);

            SetCycle(gameManager, 3, SiegeCyclePhase.Dawn, 2.25f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            ComposedCouncilEvent active = gameManager.ActiveCouncilEvent;
            Assert.That(active, Is.Not.Null);

            CouncilOptionPresentation optionA =
                gameManager.GetCouncilOptionPresentation(active.OptionA);
            CouncilOptionPresentation optionB =
                gameManager.GetCouncilOptionPresentation(active.OptionB);
            Assert.That(optionA.CanApplyExactly, Is.True);
            Assert.That(optionB.CanApplyExactly, Is.True);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId), Is.False,
                "Council kartinin acilmasi secim veya tutorial completion sayilmamali.");

            float presentationDeadline = Time.realtimeSinceStartup + 5f;
            while (!onboarding.IsCouncilExactStepVisible
                && Time.realtimeSinceStartup < presentationDeadline)
                yield return null;

            Assert.That(council.IsAwaitingPlayerChoice, Is.True);
            Assert.That(onboarding.IsCouncilExactStepVisible, Is.True);
            Assert.That(onboarding.HintText.text,
                Is.EqualTo(FirstRunOnboardingUI.CouncilExactHint));
            Assert.That(onboarding.HintText.text, Does.Contain("OUTCOMES"));
            Assert.That(onboarding.HintText.text, Does.Contain("COSTS"));
            Assert.That(onboarding.ActivePulseTarget, Is.SameAs(council.ChoiceCardRect),
                "Tutorial tek secenegi degil iki exact sonucu kapsayan tum Council kartini gostermeli.");
            Assert.That(council.CouncilOptionAText.text, Is.EqualTo(optionA.RichText));
            Assert.That(council.CouncilOptionBText.text, Is.EqualTo(optionB.RichText));
            Assert.That(council.CouncilOptionAButton.interactable, Is.True);
            Assert.That(council.CouncilOptionBButton.interactable, Is.True);
            Assert.That(gameManager.ActiveCouncilEvent, Is.SameAs(active),
                "Onboarding oyuncu adina Council secimi yapmamali.");

            Button chosenButton = council.CouncilOptionAButton;
            chosenButton.onClick.Invoke();
            yield return null;

            Assert.That(gameManager.ActiveCouncilEvent, Is.Null);
            Assert.That(MetaProgression.HasTutorialFlag(
                FirstRunOnboardingUI.CouncilExactFlagId), Is.True);
            Assert.That(onboarding.IsCouncilExactStepVisible, Is.False);
            Assert.That(onboarding.HintPanel.activeSelf, Is.False);
            Assert.That(onboarding.PulseFrame.gameObject.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        [UnityTest]
        public IEnumerator UnapprovedCouncilPayload_IsBlockedBeforeHeartOrRunStateMutation()
        {
            GameManager gameManager = GameManager.Instance;
            ContinuousSiegeCycleData cycle = gameManager.ContinuousSiegeCycle;
            cycle.Enabled = true;
            cycle.CycleIndex = 2;
            cycle.Phase = SiegeCyclePhase.Dawn;
            _cycleSetter.Invoke(gameManager, new object[] { cycle });

            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            ComposedCouncilEvent active = gameManager.ActiveCouncilEvent;
            Assert.That(active, Is.Not.Null);

            ComposedCouncilEffect invalid = active.OptionA.Effects[0];
            invalid.Kind = (CouncilEffectKind)999;
            active.OptionA.Effects[0] = invalid;

            long graveEssenceBefore = gameManager.GraveEssenceAmount;
            CouncilOptionPresentation quote = gameManager.GetCouncilOptionPresentation(active.OptionA);
            Assert.That(quote.CanApplyExactly, Is.False);
            Assert.That(quote.UnavailableReason,
                Does.StartWith(CouncilContentPolicy.BlockedReason));

            LogAssert.Expect(LogType.Error,
                new Regex("\\[GameManager\\] Council content gate karari reddetti:"));
            Assert.That(gameManager.ChooseCouncilOption(true), Is.False);
            Assert.That(gameManager.ActiveCouncilEvent, Is.SameAs(active));
            Assert.That(gameManager.GraveEssenceAmount, Is.EqualTo(graveEssenceBefore));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActiveRegularCouncil_ContinueRestoresExactPayloadMemoryAndHandledDay()
        {
            GameManager gameManager = GameManager.Instance;
            yield return WaitForSnapshotReady(gameManager);

            SetCycle(gameManager, 3, SiegeCyclePhase.Dawn, 2.25f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            string activePayloadJson = JsonUtility.ToJson(gameManager.ActiveCouncilEvent);

            Dictionary<string, int> flags = GetPrivateField<Dictionary<string, int>>(
                gameManager, "_councilFlags");
            List<string> recent = GetPrivateField<List<string>>(
                gameManager, "_recentCouncilTemplates");
            HashSet<string> usedOneShots = GetPrivateField<HashSet<string>>(
                gameManager, "_usedOneShotCouncils");
            flags["prior_choice_b"] = 1;
            usedOneShots.Add("used_one_shot_memory");

            var expectedRecent = new List<string>(recent);
            uint expectedRunSalt = GetPrivateField<uint>(gameManager, "_councilRunSalt");
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);

            _resetCouncilState.Invoke(gameManager, null);
            Assert.That(gameManager.ActiveCouncilEvent, Is.Null);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            Assert.That(gameManager.ActiveCouncilEvent, Is.Not.Null);
            Assert.That(JsonUtility.ToJson(gameManager.ActiveCouncilEvent), Is.EqualTo(activePayloadJson),
                "Continue aktif Council kartini yeniden compose etmemeli veya payload'i degistirmemeli.");
            Assert.That(GetPrivateField<int>(gameManager, "_lastRegularCouncilDay"), Is.EqualTo(3));
            Assert.That(GetPrivateField<uint>(gameManager, "_councilRunSalt"),
                Is.EqualTo(expectedRunSalt));
            Assert.That(GetPrivateField<Dictionary<string, int>>(gameManager, "_councilFlags")
                ["prior_choice_b"], Is.EqualTo(1));
            Assert.That(GetPrivateField<List<string>>(gameManager, "_recentCouncilTemplates"),
                Is.EqualTo(expectedRecent));
            Assert.That(GetPrivateField<HashSet<string>>(gameManager, "_usedOneShotCouncils"),
                Does.Contain("used_one_shot_memory"));

            gameManager.ExpireCouncilEvent();
            SetCycle(gameManager, 3, SiegeCyclePhase.Dawn, 2.25f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.False,
                "Continue sonrasi ayni scheduled gun ikinci Council kartini acmamali.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChosenRegularCouncil_ContinueRestoresDecisionAndTimedEffects()
        {
            GameManager gameManager = GameManager.Instance;
            yield return WaitForSnapshotReady(gameManager);

            SetCycle(gameManager, 3, SiegeCyclePhase.Dawn, 1.5f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            Assert.That(gameManager.ActiveCouncilEvent.TemplateId, Is.EqualTo("schedule_template"));
            Assert.That(gameManager.ChooseCouncilOption(false), Is.True,
                "Test catalog Option B sureli production etkisini uygulayamadi.");
            Assert.That(gameManager.ActiveCouncilEvent, Is.Null);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity configEntity = GetConfigEntity(entityManager);
            MobileEconomyEventState expectedEffect =
                entityManager.GetComponentData<MobileEconomyEventState>(configEntity);
            Assert.That(expectedEffect.ProductionBonusMultiplier, Is.GreaterThan(1f));
            Assert.That(expectedEffect.ProductionBonusExpiresAfterWave, Is.EqualTo(4));
            expectedEffect.NextNightSpawnMultiplier = 0.72f;
            expectedEffect.NightSpawnExpiresAfterWave = 5;
            entityManager.SetComponentData(configEntity, expectedEffect);

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);

            _resetCouncilState.Invoke(gameManager, null);
            MobileEconomyEventState mutatedEffect = expectedEffect;
            mutatedEffect.ProductionBonusResource = EconomyFocusType.Balanced;
            mutatedEffect.ProductionBonusMultiplier = 1f;
            mutatedEffect.ProductionBonusExpiresAfterWave = 0;
            mutatedEffect.NextNightSpawnMultiplier = 1f;
            mutatedEffect.NightSpawnExpiresAfterWave = 0;
            entityManager.SetComponentData(configEntity, mutatedEffect);

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            configEntity = GetConfigEntity(entityManager);
            MobileEconomyEventState restoredEffect =
                entityManager.GetComponentData<MobileEconomyEventState>(configEntity);

            Assert.That(gameManager.ActiveCouncilEvent, Is.Null,
                "Cozulmus Council karari Continue sonrasinda tekrar aktif karta donmemeli.");
            Dictionary<string, int> restoredFlags = GetPrivateField<Dictionary<string, int>>(
                gameManager, "_councilFlags");
            Assert.That(restoredFlags["council_schedule_template_b"], Is.EqualTo(3));
            Assert.That(GetPrivateField<int>(gameManager, "_lastRegularCouncilDay"), Is.EqualTo(3));
            Assert.That(restoredEffect.ProductionBonusResource,
                Is.EqualTo(expectedEffect.ProductionBonusResource));
            Assert.That(restoredEffect.ProductionBonusMultiplier,
                Is.EqualTo(expectedEffect.ProductionBonusMultiplier).Within(0.0001f));
            Assert.That(restoredEffect.ProductionBonusExpiresAfterWave,
                Is.EqualTo(expectedEffect.ProductionBonusExpiresAfterWave));
            Assert.That(restoredEffect.NextNightSpawnMultiplier,
                Is.EqualTo(expectedEffect.NextNightSpawnMultiplier).Within(0.0001f));
            Assert.That(restoredEffect.NightSpawnExpiresAfterWave,
                Is.EqualTo(expectedEffect.NightSpawnExpiresAfterWave));

            SetCycle(gameManager, 3, SiegeCyclePhase.Dawn, 1.5f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.False,
                "Cozulmus Day 3 karari Continue sonrasi tekrar acilmamali.");
            SetCycle(gameManager, 6, SiegeCyclePhase.Dawn, 0.5f);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True,
                "Continue future regular Day 6 toplantisini engellememeli.");
            gameManager.ExpireCouncilEvent();
            yield return null;
        }

        private IEnumerator WaitForSnapshotReady(GameManager gameManager)
        {
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                    yield break;
                yield return null;
            }

            Assert.Fail("GameManager/SubScene Council exact save testi icin hazir olmadi.");
        }

        private void SetCycle(
            GameManager gameManager,
            int day,
            SiegeCyclePhase phase,
            float cycleTimer)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity configEntity = GetConfigEntity(entityManager);
            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            cycle.Enabled = true;
            cycle.CycleIndex = day - 1;
            cycle.Phase = phase;
            cycle.CycleTimer = cycleTimer;
            entityManager.SetComponentData(configEntity, cycle);
            _cycleSetter.Invoke(gameManager, new object[] { cycle });
        }

        private static Entity GetConfigEntity(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(ContinuousSiegeCycleData),
                typeof(MobileEconomyEventState));
            return query.GetSingletonEntity();
        }

        private static T GetPrivateField<T>(GameManager gameManager, string fieldName)
        {
            FieldInfo field = typeof(GameManager).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"GameManager.{fieldName} bulunamadi.");
            return (T)field.GetValue(gameManager);
        }

        private CouncilEventCatalogSO CreateCatalog()
        {
            CouncilEffectAtomSO gain = ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            gain.Id = "schedule_gain";
            gain.Kind = CouncilEffectKind.GainResource;
            gain.MinutesOfProduction = 1f;
            gain.BudgetMinutes = 1f;
            _createdObjects.Add(gain);

            CouncilEffectAtomSO boost = ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            boost.Id = "schedule_boost";
            boost.Kind = CouncilEffectKind.TempProductionBoost;
            boost.Rate = 0.1f;
            boost.DurationDays = 1;
            boost.BudgetMinutes = 1f;
            _createdObjects.Add(boost);

            CouncilTemplateSO template = ScriptableObject.CreateInstance<CouncilTemplateSO>();
            template.Id = "schedule_template";
            template.Title = "SCHEDULE TEST";
            template.Body = "A regular Council on day {DAY}.";
            template.OutcomeA = "+{GAIN_N} {GAIN_RES}.";
            template.OutcomeB = "{BOOST_RES} +{BOOST_PCT}% for {BOOST_D} days.";
            template.Contrast = CouncilContrastType.NowVsLater;
            template.OptionAAtomIds = new[] { gain.Id };
            template.OptionBAtomIds = new[] { boost.Id };
            template.MinDay = 1;
            template.SetsFlagOnA = "schedule_followup_ready";
            _createdObjects.Add(template);

            CouncilTemplateSO followup = ScriptableObject.CreateInstance<CouncilTemplateSO>();
            followup.Id = "schedule_followup";
            followup.Title = "SCHEDULE FOLLOWUP";
            followup.Contrast = CouncilContrastType.NowVsLater;
            followup.OptionAAtomIds = new[] { gain.Id };
            followup.OptionBAtomIds = new[] { boost.Id };
            followup.RequiredFlags = new[] { "schedule_followup_ready" };
            followup.BaseWeight = 0f;
            _createdObjects.Add(followup);

            CouncilEventCatalogSO catalog = ScriptableObject.CreateInstance<CouncilEventCatalogSO>();
            catalog.Atoms = new[] { gain, boost };
            catalog.Templates = new[] { template, followup };
            catalog.RecentTemplateMemory = 1;
            catalog.CuratedChains = new[]
            {
                new CouncilCuratedChain
                {
                    SourceTemplateId = template.Id,
                    SourceBranch = CouncilChoiceBranch.OptionA,
                    Flag = "schedule_followup_ready",
                    TargetTemplateId = followup.Id,
                },
            };
            _createdObjects.Add(catalog);
            return catalog;
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
