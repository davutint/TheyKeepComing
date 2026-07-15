using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class CouncilRegularSchedulePlayModeTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();
        private FieldInfo _catalogField;
        private CouncilEventCatalogSO _originalCatalog;
        private MethodInfo _resetCouncilState;
        private MethodInfo _cycleSetter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
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
    }
}
