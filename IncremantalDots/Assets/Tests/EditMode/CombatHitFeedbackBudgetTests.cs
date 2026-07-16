using NUnit.Framework;
using Unity.Mathematics;

namespace DeadWalls.Tests
{
    public class CombatHitFeedbackBudgetTests
    {
        [Test]
        public void SpatialKey_CollapsesSameCellButKeepsHitTypesSeparate()
        {
            int3 firstArrow = CombatHitFeedbackBudget.GetSpatialKey(
                new float3(3.01f, -1.49f, 0f),
                CombatVfxType.ArrowHit);
            int3 secondArrow = CombatHitFeedbackBudget.GetSpatialKey(
                new float3(3.70f, -0.80f, 0f),
                CombatVfxType.ArrowHit);
            int3 frost = CombatHitFeedbackBudget.GetSpatialKey(
                new float3(3.01f, -1.49f, 0f),
                CombatVfxType.FrostHit);

            Assert.That(secondArrow, Is.EqualTo(firstArrow));
            Assert.That(frost.xy, Is.EqualTo(firstArrow.xy));
            Assert.That(frost.z, Is.Not.EqualTo(firstArrow.z));
        }

        [TestCase(100, 100, 16, 8)]
        [TestCase(100, 2, 22, 2)]
        [TestCase(2, 100, 2, 22)]
        [TestCase(1000, 0, 24, 0)]
        [TestCase(0, 1000, 0, 24)]
        public void VfxBudget_IsGloballyBoundedAndKeepsBothPresentTypesVisible(
            int arrowCandidates,
            int frostCandidates,
            int expectedArrowBudget,
            int expectedFrostBudget)
        {
            CombatHitFeedbackBudget.ResolveVfxBudgets(
                arrowCandidates,
                frostCandidates,
                out int arrowBudget,
                out int frostBudget);

            Assert.That(arrowBudget, Is.EqualTo(expectedArrowBudget));
            Assert.That(frostBudget, Is.EqualTo(expectedFrostBudget));
            Assert.That(arrowBudget + frostBudget,
                Is.LessThanOrEqualTo(CombatHitFeedbackBudget.MaxVfxEventsPerFrame));
            if (arrowCandidates > 0 && frostCandidates > 0)
            {
                Assert.That(arrowBudget, Is.GreaterThanOrEqualTo(
                    math.min(arrowCandidates, CombatHitFeedbackBudget.MinimumVfxPerPresentType)));
                Assert.That(frostBudget, Is.GreaterThanOrEqualTo(
                    math.min(frostCandidates, CombatHitFeedbackBudget.MinimumVfxPerPresentType)));
            }
        }

        [Test]
        public void BudgetContract_UsesSmallFixedCandidateAndPlaybackCaps()
        {
            Assert.That(CombatHitFeedbackBudget.SpatialCellSize, Is.InRange(0.5f, 1f));
            Assert.That(CombatHitFeedbackBudget.CandidateCapacity, Is.EqualTo(512));
            Assert.That(CombatHitFeedbackBudget.MaxVfxEventsPerFrame, Is.EqualTo(24));
        }
    }
}
