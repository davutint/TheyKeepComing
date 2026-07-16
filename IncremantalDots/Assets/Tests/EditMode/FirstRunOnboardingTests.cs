using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class FirstRunOnboardingTests
    {
        private const string HudPrefabPath =
            "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";
        private const string ControllerScriptPath =
            "Assets/Scripts/MonoBehaviour/FirstRunOnboardingUI.cs";

        [Test]
        public void WorkerRatioRule_ShowsOnlyDuringIncompleteFirstDay()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, true, false, true, 0, SiegeCyclePhase.Day), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                true, true, false, true, 0, SiegeCyclePhase.Day), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, false, false, true, 0, SiegeCyclePhase.Day), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, true, true, true, 0, SiegeCyclePhase.Day), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, true, false, false, 0, SiegeCyclePhase.Day), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, true, false, true, 1, SiegeCyclePhase.Day), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowWorkerRatioStep(
                false, true, false, true, 0, SiegeCyclePhase.Night), Is.False);
        }

        [Test]
        public void BasicArcherRule_ShowsOnlyAtFirstRealAffordability()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                false, true, false, true), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                true, true, false, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                false, false, false, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                false, true, true, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowBasicArcherStep(
                false, true, false, false), Is.False);
        }

        [Test]
        public void LowAmmoRule_UsesInclusiveTwentyFivePercentCapacityThreshold()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, true, false, 50, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.True);
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, true, false, 51, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, true, false, 0, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                true, true, false, 50, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, false, false, 50, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, true, true, 50, 200,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowLowAmmoStep(
                false, true, false, 0, 0,
                FirstRunOnboardingUI.LowAmmoThresholdPercent), Is.False);
        }

        [Test]
        public void HeartEntryRule_UsesFirstPositiveGraveEssenceBalance()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                false, true, false, 1L), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                true, true, false, 1L), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                false, false, false, 1L), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                false, true, true, 1L), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                false, true, false, 0L), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowHeartEntryStep(
                false, true, false, -1L), Is.False);
        }

        [Test]
        public void CouncilExactRule_ShowsOnlyForAnIncompleteLivePlayerChoiceCard()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowCouncilExactStep(
                false, true, false, true), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowCouncilExactStep(
                true, true, false, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowCouncilExactStep(
                false, false, false, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowCouncilExactStep(
                false, true, true, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowCouncilExactStep(
                false, true, false, false), Is.False);
        }

        [Test]
        public void DaytimeRepairRule_ShowsForAnyLivingDamagedWallOnlyDuringDay()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Day, 0.5f), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                true, true, false, true, SiegeCyclePhase.Day, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, false, false, true, SiegeCyclePhase.Day, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, true, true, SiegeCyclePhase.Day, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, false, SiegeCyclePhase.Day, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Dusk, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Night, 0.5f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Day, 0f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Day, 0.995f), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowDaytimeRepairStep(
                false, true, false, true, SiegeCyclePhase.Day, 1f), Is.False);
        }

        [Test]
        public void NightAbilityKeyRule_ShowsOnlyForAReadyAbilityDuringTheFirstNight()
        {
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, true, 0, SiegeCyclePhase.Night, true), Is.True);

            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                true, true, false, true, 0, SiegeCyclePhase.Night, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, false, false, true, 0, SiegeCyclePhase.Night, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, true, true, 0, SiegeCyclePhase.Night, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, false, 0, SiegeCyclePhase.Night, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, true, 1, SiegeCyclePhase.Night, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, true, 0, SiegeCyclePhase.Day, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, true, 0, SiegeCyclePhase.Dusk, true), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldShowNightAbilityKeyStep(
                false, true, false, true, 0, SiegeCyclePhase.Night, false), Is.False);

            Assert.That(FirstRunOnboardingUI.GetAbilityKeyHint(AbilityHotkeySlot.Fireball),
                Is.EqualTo(FirstRunOnboardingUI.FireballAbilityKeyHint));
            Assert.That(FirstRunOnboardingUI.GetAbilityKeyHint(AbilityHotkeySlot.Rally),
                Is.EqualTo(FirstRunOnboardingUI.RallyAbilityKeyHint));
            Assert.That(FirstRunOnboardingUI.GetAbilityKeyHint(AbilityHotkeySlot.EmergencyRepair),
                Is.EqualTo(FirstRunOnboardingUI.EmergencyRepairAbilityKeyHint));
        }

        [Test]
        public void BlockingPauseRule_AllowsOnlyActiveHeartPauseTeaching()
        {
            Assert.That(FirstRunOnboardingRules.ShouldSuppressForBlockingPause(
                false, false, false), Is.False);
            Assert.That(FirstRunOnboardingRules.ShouldSuppressForBlockingPause(
                true, false, false), Is.True);
            Assert.That(FirstRunOnboardingRules.ShouldSuppressForBlockingPause(
                true, true, false), Is.True);
            Assert.That(FirstRunOnboardingRules.ShouldSuppressForBlockingPause(
                true, true, true), Is.False);
        }

        [Test]
        public void TutorialCompleteRule_RequiresEveryRequiredStepExactlyOnce()
        {
            Assert.That(FirstRunOnboardingRules.ShouldPersistTutorialComplete(
                false, true, true, true, true, true, true, true), Is.True);
            Assert.That(FirstRunOnboardingRules.ShouldPersistTutorialComplete(
                true, true, true, true, true, true, true, true), Is.False);

            for (int missingIndex = 0; missingIndex < 7; missingIndex++)
            {
                bool[] steps = { true, true, true, true, true, true, true };
                steps[missingIndex] = false;
                Assert.That(FirstRunOnboardingRules.ShouldPersistTutorialComplete(
                    false,
                    steps[0],
                    steps[1],
                    steps[2],
                    steps[3],
                    steps[4],
                    steps[5],
                    steps[6]), Is.False, $"missingIndex={missingIndex}");
            }

            Assert.That(FirstRunOnboardingUI.TutorialCompleteFlagId,
                Is.EqualTo("tutorial.v1.complete"));
        }

        [Test]
        public void TutorialResetContract_ContainsEveryStepAndGlobalFlagExactlyOnce()
        {
            string[] expectedFlags =
            {
                FirstRunOnboardingUI.WorkerRatioFlagId,
                FirstRunOnboardingUI.BasicArcherFlagId,
                FirstRunOnboardingUI.LowAmmoFlagId,
                FirstRunOnboardingUI.HeartEntryFlagId,
                FirstRunOnboardingUI.CouncilExactFlagId,
                FirstRunOnboardingUI.DaytimeRepairFlagId,
                FirstRunOnboardingUI.NightAbilityKeyFlagId,
                FirstRunOnboardingUI.TutorialCompleteFlagId
            };

            string[] actualFlags = FirstRunOnboardingUI.GetTutorialProgressFlagIds();
            Assert.That(actualFlags, Is.EqualTo(expectedFlags));
            Assert.That(actualFlags.Distinct().Count(), Is.EqualTo(expectedFlags.Length));

            actualFlags[0] = "tampered";
            Assert.That(FirstRunOnboardingUI.GetTutorialProgressFlagIds(),
                Is.EqualTo(expectedFlags),
                "Settings consumer canonical reset listesini mutate edememelidir.");
        }

        [Test]
        public void ActiveHudPrefab_HasSingleNonBlockingEnglishWorkerRatioPresentation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform visualRoot = prefab.transform.Find("MobileCastleHudRoot")
                as RectTransform;
            Assert.That(visualRoot, Is.Not.Null);

            RectTransform hint = FindUniqueRect(prefab, "OnboardingHintPanel");
            Assert.That(hint.parent, Is.SameAs(visualRoot));
            Assert.That(hint.gameObject.activeSelf, Is.False);
            Assert.That(hint.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(hint.anchorMax, Is.EqualTo(Vector2.zero));
            Assert.That(hint.pivot, Is.EqualTo(Vector2.zero));
            Assert.That(hint.anchoredPosition, Is.EqualTo(new Vector2(24f, 96f)));
            Assert.That(hint.sizeDelta, Is.EqualTo(new Vector2(360f, 42f)));
            Assert.That(hint.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(hint.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(hint.GetComponent<Canvas>().overrideSorting, Is.False);

            RectTransform pulse = FindUniqueRect(prefab, "OnboardingPulseFrame");
            Assert.That(pulse.parent, Is.SameAs(visualRoot));
            Assert.That(pulse.gameObject.activeSelf, Is.False);
            Assert.That(pulse.GetComponent<Image>().raycastTarget, Is.False);
            Assert.That(pulse.GetComponent<Outline>(), Is.Not.Null);

            Component text = prefab.GetComponentsInChildren<Component>(true)
                .Single(component => component.gameObject.name == "OnboardingHintText"
                    && component.GetType().Name == "TextMeshProUGUI");
            string copy = (string)text.GetType().GetProperty("text").GetValue(text);
            Assert.That(copy, Is.EqualTo(FirstRunOnboardingUI.WorkerRatioHint));
            string[] actualCopy =
            {
                FirstRunOnboardingUI.WorkerRatioHint,
                FirstRunOnboardingUI.BasicArcherHint,
                FirstRunOnboardingUI.LowAmmoHint,
                FirstRunOnboardingUI.HeartEntryHint,
                FirstRunOnboardingUI.HeartPauseHint,
                FirstRunOnboardingUI.CouncilExactHint,
                FirstRunOnboardingUI.DaytimeRepairHint,
                FirstRunOnboardingUI.FireballAbilityKeyHint,
                FirstRunOnboardingUI.RallyAbilityKeyHint,
                FirstRunOnboardingUI.EmergencyRepairAbilityKeyHint
            };
            string[] approvedEnglishCopy =
            {
                "ADJUST A WORKER TARGET RATIO.",
                "RECRUIT A BASIC ARCHER.",
                "RESTOCK YOUR ARROWS.",
                "OPEN THE CASTLE HEART.",
                "THE CASTLE HEART FULLY PAUSES THE BATTLE.",
                "COMPARE BOTH EXACT OUTCOMES AND THEIR COSTS.",
                "REPAIR THE WALL DURING THE DAY.",
                "PRESS 1 TO TARGET FIREBALL.",
                "PRESS 2 TO USE RALLY.",
                "PRESS 3 TO REPAIR THE WALL."
            };

            Assert.That(actualCopy, Is.EqualTo(approvedEnglishCopy));
            foreach (string approvedCopy in actualCopy)
                Assert.That(approvedCopy, Does.Match("^[A-Z0-9 .+]+$"), approvedCopy);
        }

        [Test]
        public void ControllerSource_HasNoGameplayTransactionOrWorkerAssignmentCalls()
        {
            MonoScript controller = AssetDatabase.LoadAssetAtPath<MonoScript>(ControllerScriptPath);
            Assert.That(controller, Is.Not.Null);
            string source = controller.text;

            string[] forbiddenCalls =
            {
                ".BuyArcher(",
                ".TryPurchase(",
                ".TrySpend(",
                ".SpendResources(",
                ".GrantGraveEssence(",
                ".RepairDefenseFull(",
                ".TryUseRally(",
                ".TryUseEmergencyRepair(",
                ".TryCastFireball(",
                ".ChooseCouncilOption(",
                ".SetWorkerTargetRatio(",
                ".SetWorkerCount(",
                ".AssignWorker(",
                ".SetComponentData(",
                ".SetOpen(",
                ".onClick.Invoke("
            };

            foreach (string forbiddenCall in forbiddenCalls)
            {
                Assert.That(source, Does.Not.Contain(forbiddenCall), forbiddenCall);
            }

            Assert.That(source, Does.Contain("MetaProgression.SetTutorialFlag"),
                "Controller yalniz tutorial completion persistence'i yazabilmelidir.");
        }

        [Test]
        public void ControllerSource_HasNoPauseLeaseOrModalOpenCalls()
        {
            MonoScript controller = AssetDatabase.LoadAssetAtPath<MonoScript>(ControllerScriptPath);
            Assert.That(controller, Is.Not.Null);
            string source = controller.text;

            string[] forbiddenCalls =
            {
                "SimulationPauseService.Acquire(",
                "SimulationPauseService.EnforcePausedState(",
                ".OpenPanel(",
                ".TogglePanel(",
                ".OpenPause(",
                ".Settings.Open("
            };

            foreach (string forbiddenCall in forbiddenCalls)
                Assert.That(source, Does.Not.Contain(forbiddenCall), forbiddenCall);
        }

        [Test]
        public void AcceptedPlayerActionHandlers_AreIndependentFromPromptVisibility()
        {
            MonoScript controller = AssetDatabase.LoadAssetAtPath<MonoScript>(ControllerScriptPath);
            Assert.That(controller, Is.Not.Null);
            string source = controller.text;

            (string Handler, string NextHandler, string FlagId)[] completionHandlers =
            {
                ("HandleWorkerTargetRatioChanged", "HandleArcherPurchased", "WorkerRatioFlagId"),
                ("HandleArcherPurchased", "HandleArrowRefillPurchased", "BasicArcherFlagId"),
                ("HandleArrowRefillPurchased", "HandleHeartOpenedByPlayer", "LowAmmoFlagId"),
                ("HandleHeartClosedByPlayer", "HandleCouncilChoiceCommitted", "HeartEntryFlagId"),
                ("HandleCouncilChoiceCommitted", "HandleNormalRepairCommitted", "CouncilExactFlagId"),
                ("HandleNormalRepairCommitted", "HandleAbilityHotkeyAccepted", "DaytimeRepairFlagId"),
                ("HandleAbilityHotkeyAccepted", "GetAbilityKeyHint", "NightAbilityKeyFlagId")
            };

            foreach ((string handler, string nextHandler, string flagId) in completionHandlers)
            {
                string handlerSource = ExtractMethodSource(source, handler, nextHandler);
                AssertPromptIndependent(handlerSource, handler);
                Assert.That(handlerSource, Does.Contain("MetaProgression.SetTutorialFlag"), handler);
                Assert.That(handlerSource, Does.Contain(flagId), handler);
            }

            string heartOpenSource = ExtractMethodSource(
                source,
                "HandleHeartOpenedByPlayer",
                "HandleHeartClosedByPlayer");
            AssertPromptIndependent(heartOpenSource, "HandleHeartOpenedByPlayer");
            Assert.That(heartOpenSource, Does.Not.Contain("GraveEssenceAmount"));
            Assert.That(heartOpenSource, Does.Not.Contain("GameState"));
            Assert.That(heartOpenSource, Does.Contain("_heartPauseTeachingActive = true"));
        }

        private static RectTransform FindUniqueRect(GameObject root, string objectName)
        {
            RectTransform[] matches = root.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect.gameObject.name == objectName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1), objectName);
            return matches[0];
        }

        private static string ExtractMethodSource(
            string source,
            string methodName,
            string nextMethodName)
        {
            string startToken = $"private void {methodName}";
            string nextToken = nextMethodName == "GetAbilityKeyHint"
                ? "public static string GetAbilityKeyHint"
                : $"private void {nextMethodName}";
            int start = source.IndexOf(startToken, StringComparison.Ordinal);
            int end = source.IndexOf(nextToken, start + startToken.Length,
                StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), methodName);
            Assert.That(end, Is.GreaterThan(start), nextMethodName);
            return source.Substring(start, end - start);
        }

        private static void AssertPromptIndependent(string handlerSource, string handlerName)
        {
            string[] forbiddenPresentationGates =
            {
                "_activeStep",
                "StepVisible",
                "ShouldShow",
                "HintPanel.activeSelf",
                "PulseFrame.gameObject.activeSelf"
            };

            foreach (string gate in forbiddenPresentationGates)
                Assert.That(handlerSource, Does.Not.Contain(gate), $"{handlerName}: {gate}");
        }
    }
}
