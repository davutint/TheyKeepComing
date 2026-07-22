using System.Linq;
using NUnit.Framework;

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
        public void CopyAndResetContract_AreEnglishDurableAndUnique()
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
            }

            string[] guidedFlags = GuidedOnboardingProgress.GetProgressFlagIds();
            Assert.That(guidedFlags, Has.Length.EqualTo(13));
            Assert.That(guidedFlags.Distinct().Count(), Is.EqualTo(guidedFlags.Length));
            Assert.That(FirstRunOnboardingUI.GetTutorialProgressFlagIds(),
                Is.SupersetOf(guidedFlags));
            Assert.That(GuidedOnboardingProgress.IsCoreStep(GuidedOnboardingStep.SpeedTwo), Is.True);
            Assert.That(GuidedOnboardingProgress.IsCoreStep(GuidedOnboardingStep.Rally), Is.False);
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
