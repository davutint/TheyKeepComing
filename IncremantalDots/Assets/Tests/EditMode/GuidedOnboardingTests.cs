using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class GuidedOnboardingTests
    {
        [Test]
        public void CoreRule_RequiresExactEconomySliderCloseBarracksArcherSpeedSequence()
        {
            Assert.That(ResolveCore(false, false, false, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.EconomyOpen));
            Assert.That(ResolveCore(true, false, false, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.WorkerShare));
            Assert.That(ResolveCore(true, true, false, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.EconomyClose));
            Assert.That(ResolveCore(true, true, true, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.BarracksOpen));
            Assert.That(ResolveCore(true, true, true, true, false, false),
                Is.EqualTo(GuidedOnboardingStep.BasicArcher));
            Assert.That(ResolveCore(true, true, true, true, true, false),
                Is.EqualTo(GuidedOnboardingStep.SpeedTwo));
            Assert.That(ResolveCore(true, true, true, true, true, true),
                Is.EqualTo(GuidedOnboardingStep.None));
            Assert.That(GuidedOnboardingProgress.ResolveCoreStep(
                true, false, false, false, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.None));
        }

        [Test]
        public void ContextualRule_WaitsForCoreAndUsesDeterministicLivePriority()
        {
            Assert.That(ResolveContext(coreComplete: false),
                Is.EqualTo(GuidedOnboardingStep.None));
            Assert.That(ResolveContext(coreComplete: true),
                Is.EqualTo(GuidedOnboardingStep.CouncilChoice));
            Assert.That(ResolveContext(coreComplete: true, councilComplete: true),
                Is.EqualTo(GuidedOnboardingStep.Rally));
            Assert.That(ResolveContext(
                    coreComplete: true,
                    councilComplete: true,
                    rallyComplete: true),
                Is.EqualTo(GuidedOnboardingStep.WallRepair));
            Assert.That(ResolveContext(
                    coreComplete: true,
                    councilComplete: true,
                    rallyComplete: true,
                    repairComplete: true),
                Is.EqualTo(GuidedOnboardingStep.ArrowRefill));
            Assert.That(ResolveContext(
                    coreComplete: true,
                    councilComplete: true,
                    rallyComplete: true,
                    repairComplete: true,
                    arrowComplete: true),
                Is.EqualTo(GuidedOnboardingStep.CastleHeart));
            Assert.That(ResolveContext(
                    coreComplete: true,
                    councilComplete: true,
                    rallyComplete: true,
                    repairComplete: true,
                    arrowComplete: true,
                    heartComplete: true),
                Is.EqualTo(GuidedOnboardingStep.Housing));
        }

        [Test]
        public void CopyAndResetContract_AreEnglishSessionScopedAndUnique()
        {
            GuidedOnboardingStep[] steps =
            {
                GuidedOnboardingStep.EconomyOpen,
                GuidedOnboardingStep.WorkerShare,
                GuidedOnboardingStep.EconomyClose,
                GuidedOnboardingStep.BarracksOpen,
                GuidedOnboardingStep.BasicArcher,
                GuidedOnboardingStep.SpeedTwo,
                GuidedOnboardingStep.Rally,
                GuidedOnboardingStep.CouncilChoice,
                GuidedOnboardingStep.ArrowRefill,
                GuidedOnboardingStep.CastleHeart,
                GuidedOnboardingStep.Housing,
                GuidedOnboardingStep.WallRepair
            };

            foreach (GuidedOnboardingStep step in steps)
            {
                GuidedOnboardingCopy copy = GuidedOnboardingProgress.GetCopy(step);
                Assert.That(copy.Title, Is.Not.Empty, step.ToString());
                Assert.That(copy.Body, Is.Not.Empty, step.ToString());
                Assert.That(copy.Title, Does.Match("^[A-Z0-9 ]+$"), step.ToString());
                Assert.That(copy.Body.Length, Is.GreaterThanOrEqualTo(70), step.ToString());
            }

            Assert.That(GuidedOnboardingProgress.GetCopy(GuidedOnboardingStep.SpeedTwo).Body,
                Does.Contain("paused simulation will resume"));
            Assert.That(GuidedOnboardingProgress.GetCopy(GuidedOnboardingStep.Housing).Body,
                Does.Contain("no new people can arrive"));

            string[] guidedFlags = GuidedOnboardingProgress.GetProgressFlagIds();
            Assert.That(guidedFlags, Has.Length.EqualTo(13));
            Assert.That(guidedFlags.Distinct().Count(), Is.EqualTo(guidedFlags.Length));
            Assert.That(FirstRunOnboardingUI.GetTutorialProgressFlagIds(),
                Is.SupersetOf(guidedFlags));
            Assert.That(GuidedOnboardingProgress.IsCoreStep(GuidedOnboardingStep.SpeedTwo), Is.True);
            Assert.That(GuidedOnboardingProgress.IsCoreStep(GuidedOnboardingStep.Rally), Is.False);
        }

        [Test]
        public void TutorialProgress_EveryPlaySessionStartsFromTheFirstStepWithoutSaveData()
        {
            TutorialSessionProgress.BeginNewPlaySession();
            string[] allFlags = FirstRunOnboardingUI.GetTutorialProgressFlagIds();
            foreach (string flagId in allFlags)
                Assert.That(MetaProgression.SetTutorialFlag(flagId, true), Is.True, flagId);

            Assert.That(TutorialSessionProgress.CompletedFlagCount,
                Is.EqualTo(allFlags.Distinct().Count()));
            Assert.That(GuidedOnboardingProgress.IsCoreComplete(), Is.True);

            var legacySchema = new MetaProgressState
            {
                TutorialFlags = new List<string> { GuidedOnboardingProgress.CompleteFlagId }
            };
            string serializedMeta = JsonUtility.ToJson(legacySchema);
            Assert.That(serializedMeta, Does.Not.Contain("TutorialFlags"),
                "Tutorial progress meta save'e serialize edilmemelidir.");

            TutorialSessionProgress.BeginNewPlaySession();

            Assert.That(TutorialSessionProgress.CompletedFlagCount, Is.Zero);
            foreach (string flagId in allFlags)
                Assert.That(MetaProgression.HasTutorialFlag(flagId), Is.False, flagId);
            Assert.That(GuidedOnboardingProgress.IsCoreComplete(), Is.False);
            Assert.That(GuidedOnboardingProgress.ResolveCoreStep(
                    false, false, false, false, false, false, false),
                Is.EqualTo(GuidedOnboardingStep.EconomyOpen));
        }

        [Test]
        public void TutorialProgress_NewPlayResetIsRegisteredForSubsystemInitialization()
        {
            var method = typeof(TutorialSessionProgress).GetMethod(
                nameof(TutorialSessionProgress.BeginNewPlaySession));
            Assert.That(method, Is.Not.Null);

            var attribute = System.Attribute.GetCustomAttribute(method,
                typeof(RuntimeInitializeOnLoadMethodAttribute))
                as RuntimeInitializeOnLoadMethodAttribute;
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.loadType,
                Is.EqualTo(RuntimeInitializeLoadType.SubsystemRegistration));
        }

        [Test]
        public void FocusPulse_UsesUnscaledTimeAndBreathesAcrossFullRange()
        {
            float start = GameplayHUDToolkitUI.EvaluateGuidedFocusPulse(0f);
            float peakTime = 0.5f / 1.15f;
            float peak = GameplayHUDToolkitUI.EvaluateGuidedFocusPulse(peakTime);

            Assert.That(start, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(peak, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void AbilityFocusGeometry_RemainsStableWhileOtherTargetsCanExpand()
        {
            const float peakPulse = 1f;

            Assert.That(GameplayHUDToolkitUI.EvaluateGuidedFocusExpansion(
                    GuidedOnboardingStep.Rally,
                    peakPulse),
                Is.Zero);
            Assert.That(GameplayHUDToolkitUI.EvaluateGuidedFocusExpansion(
                    GuidedOnboardingStep.WallRepair,
                    peakPulse),
                Is.Zero);
            Assert.That(GameplayHUDToolkitUI.EvaluateGuidedFocusExpansion(
                    GuidedOnboardingStep.SpeedTwo,
                    peakPulse),
                Is.GreaterThan(0f));
        }

        [Test]
        public void GuidedRectCache_SkipsEquivalentGeometryButDetectsLayoutChanges()
        {
            Rect baseline = new Rect(14f, 28f, 192f, 84f);

            Assert.That(GameplayHUDToolkitUI.RectApproximatelyEqual(
                    baseline,
                    new Rect(14.005f, 27.995f, 192.005f, 83.995f)),
                Is.True);
            Assert.That(GameplayHUDToolkitUI.RectApproximatelyEqual(
                    baseline,
                    new Rect(14f, 28f, 193f, 84f)),
                Is.False);
        }

        private static GuidedOnboardingStep ResolveCore(
            bool economy,
            bool worker,
            bool close,
            bool barracks,
            bool archer,
            bool speed)
        {
            return GuidedOnboardingProgress.ResolveCoreStep(
                false,
                economy,
                worker,
                close,
                barracks,
                archer,
                speed);
        }

        private static GuidedOnboardingStep ResolveContext(
            bool coreComplete,
            bool councilComplete = false,
            bool rallyComplete = false,
            bool repairComplete = false,
            bool arrowComplete = false,
            bool heartComplete = false,
            bool housingComplete = false)
        {
            return GuidedOnboardingProgress.ResolveContextualStep(
                false,
                coreComplete,
                councilComplete,
                true,
                rallyComplete,
                true,
                repairComplete,
                true,
                arrowComplete,
                true,
                heartComplete,
                true,
                housingComplete,
                true);
        }
    }
}
