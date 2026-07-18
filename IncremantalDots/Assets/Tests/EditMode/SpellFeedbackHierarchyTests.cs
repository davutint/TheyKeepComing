using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class SpellFeedbackHierarchyTests
    {
        [Test]
        public void SortingContract_PutsFireballAboveFrostAndFrostAboveOrdinaryHits()
        {
            Assert.That(SpellFeedbackHierarchy.FrostHitSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.OrdinaryHitSortingOrder));
            Assert.That(SpellFeedbackHierarchy.FireballProjectileAuraSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.FrostHitSortingOrder));
            Assert.That(SpellFeedbackHierarchy.FireballProjectileSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballProjectileAuraSortingOrder));
            Assert.That(SpellFeedbackHierarchy.BurningGroundFillSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballProjectileSortingOrder));
            Assert.That(SpellFeedbackHierarchy.BurningGroundRingSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.BurningGroundFillSortingOrder));
            Assert.That(SpellFeedbackHierarchy.FireballBlastSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.BurningGroundRingSortingOrder));
            Assert.That(SpellFeedbackHierarchy.FireballBlastCoreSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballBlastSortingOrder));
            Assert.That(SpellFeedbackHierarchy.FireballBlastRingSortingOrder,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballBlastCoreSortingOrder));
        }

        [Test]
        public void FrostHierarchy_ExpandsSampledImpactWithoutChangingGameplayRadius()
        {
            float hitScale = SpellFeedbackHierarchy.ResolveFrostHitScale(
                0.35f,
                SpellFeedbackHierarchy.FrostHitScaleMultiplier);
            float ringStart = SpellFeedbackHierarchy.ResolveFrostRingScale(
                0f,
                SpellFeedbackHierarchy.FrostRingStartScale,
                SpellFeedbackHierarchy.FrostRingEndScale);
            float ringEnd = SpellFeedbackHierarchy.ResolveFrostRingScale(
                1f,
                SpellFeedbackHierarchy.FrostRingStartScale,
                SpellFeedbackHierarchy.FrostRingEndScale);

            Assert.That(hitScale, Is.EqualTo(1.12f).Within(0.001f));
            Assert.That(ringStart, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(ringEnd, Is.EqualTo(2.2f).Within(0.001f));
            Assert.That(SpellFeedbackHierarchy.FrostRingColor.b,
                Is.GreaterThan(SpellFeedbackHierarchy.FrostRingColor.r));
        }

        [Test]
        public void FireballHierarchy_OuterRingStartsBeyondBlastAndFadesOut()
        {
            const float radius = 2.2f;
            float blastScale = SpellFeedbackHierarchy.ResolveFireballBlastScale(
                radius,
                1.6f,
                SpellFeedbackHierarchy.FireballBlastDiameterMultiplier);
            float blastDiameter = blastScale * 1.6f;
            float ringBaseDiameter = SpellFeedbackHierarchy.ResolveFireballBlastRingDiameter(
                radius,
                SpellFeedbackHierarchy.FireballBlastRingDiameterMultiplier);
            float ringStartDiameter = SpellFeedbackHierarchy.ResolveFireballBlastRingScale(
                ringBaseDiameter,
                0f,
                SpellFeedbackHierarchy.FireballBlastRingStartScale,
                SpellFeedbackHierarchy.FireballBlastRingEndScale);
            Color faded = SpellFeedbackHierarchy.ResolveFadingColor(
                SpellFeedbackHierarchy.FireballBlastRingColor,
                1f);

            Assert.That(blastDiameter, Is.EqualTo(radius * 2.4f).Within(0.001f));
            Assert.That(ringStartDiameter, Is.GreaterThan(blastDiameter));
            Assert.That(SpellFeedbackHierarchy.FireballBlastCoreColor.r,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballBlastCoreColor.b));
            Assert.That(SpellFeedbackHierarchy.FireballBlastCoreColor.a,
                Is.GreaterThanOrEqualTo(0.7f));
            Assert.That(SpellFeedbackHierarchy.FireballBlastRingColor.r,
                Is.GreaterThan(SpellFeedbackHierarchy.FireballBlastRingColor.b * 10f));
            Assert.That(faded.a, Is.Zero.Within(0.001f));
        }

        [Test]
        public void FireballEvolutions_LockExactCombatAndFixedRendererVfxDirection()
        {
            Assert.That(FireballEvolutionRules.SecondBlastDelaySeconds,
                Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(FireballEvolutionRules.SecondBlastDamageMultiplier,
                Is.EqualTo(0.60f).Within(0.001f));
            Assert.That(FireballEvolutionRules.SecondBlastRadiusMultiplier,
                Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(FireballEvolutionRules.BurningGroundDurationSeconds,
                Is.EqualTo(5f).Within(0.001f));
            Assert.That(FireballEvolutionRules.BurningGroundTickIntervalSeconds,
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(FireballEvolutionRules.BurningGroundTickCount, Is.EqualTo(5));
            Assert.That(FireballEvolutionRules.BurningGroundDamageMultiplierPerTick,
                Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(FireballEvolutionRules.BurningGroundRadiusMultiplier,
                Is.EqualTo(0.70f).Within(0.001f));

            float diameter = SpellFeedbackHierarchy.ResolveBurningGroundDiameter(2f, 0f);
            Color active = SpellFeedbackHierarchy.ResolveBurningGroundColor(
                SpellFeedbackHierarchy.BurningGroundRingColor,
                5f,
                5f);
            Color expired = SpellFeedbackHierarchy.ResolveBurningGroundColor(
                SpellFeedbackHierarchy.BurningGroundRingColor,
                0f,
                5f);
            Assert.That(diameter, Is.EqualTo(4f).Within(0.001f));
            Assert.That(active.a, Is.GreaterThan(0.5f));
            Assert.That(expired.a, Is.Zero.Within(0.001f));
            Assert.That(SpellFeedbackHierarchy.SecondBlastCoreColor.r,
                Is.GreaterThan(SpellFeedbackHierarchy.SecondBlastCoreColor.b));
        }
    }
}
