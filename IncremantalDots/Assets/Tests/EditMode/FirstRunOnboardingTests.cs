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
            Assert.That(copy, Does.Match("^[A-Z0-9 .+]+$"));
            Assert.That(FirstRunOnboardingUI.BasicArcherHint, Does.Match("^[A-Z0-9 .+]+$"));
            Assert.That(FirstRunOnboardingUI.LowAmmoHint, Does.Match("^[A-Z0-9 .+]+$"));
            Assert.That(FirstRunOnboardingUI.HeartEntryHint, Does.Match("^[A-Z0-9 .+]+$"));
            Assert.That(FirstRunOnboardingUI.HeartPauseHint, Does.Match("^[A-Z0-9 .+]+$"));
        }

        private static RectTransform FindUniqueRect(GameObject root, string objectName)
        {
            RectTransform[] matches = root.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect.gameObject.name == objectName)
                .ToArray();
            Assert.That(matches.Length, Is.EqualTo(1), objectName);
            return matches[0];
        }
    }
}
