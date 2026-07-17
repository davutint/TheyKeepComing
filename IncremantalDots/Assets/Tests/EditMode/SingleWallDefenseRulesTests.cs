using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class SingleWallDefenseRulesTests
    {
        [Test]
        public void ApplyDamage_UsesOnlyCurrentWallHp_AndClampsAtZero()
        {
            Assert.That(SingleWallDefenseRules.ApplyDamage(100f, 25f), Is.EqualTo(75f));
            Assert.That(SingleWallDefenseRules.ApplyDamage(20f, 50f), Is.EqualTo(0f));
            Assert.That(SingleWallDefenseRules.ApplyDamage(20f, -10f), Is.EqualTo(20f));
        }

        [Test]
        public void DamageMultiplier_IsClampedAndAppliedToWallDamage()
        {
            Assert.That(SingleWallDefenseRules.ApplyDamage(100f, 20f, 0.5f), Is.EqualTo(90f));
            Assert.That(SingleWallDefenseRules.ApplyDamage(100f, 20f, -2f), Is.EqualTo(100f));
        }

        [Test]
        public void DestroyedWall_CannotBeRevivedByRepair()
        {
            Assert.That(SingleWallDefenseRules.RepairToFull(0f, 500f), Is.EqualTo(0f));
            Assert.That(SingleWallDefenseRules.RepairToFull(-10f, 500f), Is.EqualTo(0f));
        }

        [Test]
        public void DestroyedWall_CannotBeRevivedByCouncilHeal()
        {
            Assert.That(SingleWallDefenseRules.HealByMaxPercent(0f, 500f, 0.25f), Is.EqualTo(0f));
        }

        [Test]
        public void SameFrameLethalDamage_WinsAgainstRepair()
        {
            float afterDamage = SingleWallDefenseRules.ApplyDamage(25f, 25f);
            float afterRepair = SingleWallDefenseRules.RepairToFull(afterDamage, 500f);

            Assert.That(afterDamage, Is.EqualTo(0f));
            Assert.That(afterRepair, Is.EqualTo(0f));
            Assert.That(SingleWallDefenseRules.IsDestroyed(afterRepair), Is.True);
        }

        [Test]
        public void RepairPhase_AllowsOnlyDayAndDusk()
        {
            Assert.That(SingleWallDefenseRules.IsRepairPhaseAllowed(SiegeCyclePhase.Day), Is.True);
            Assert.That(SingleWallDefenseRules.IsRepairPhaseAllowed(SiegeCyclePhase.Dusk), Is.True);
            Assert.That(SingleWallDefenseRules.IsRepairPhaseAllowed(SiegeCyclePhase.Night), Is.False);
            Assert.That(SingleWallDefenseRules.IsRepairPhaseAllowed(SiegeCyclePhase.Dawn), Is.False);
        }

        [Test]
        public void HealthRatio_RepresentsSingleWallOnly()
        {
            Assert.That(SingleWallDefenseRules.GetHealthRatio(125f, 500f), Is.EqualTo(0.25f));
            Assert.That(SingleWallDefenseRules.GetHealthRatio(750f, 500f), Is.EqualTo(1f));
            Assert.That(SingleWallDefenseRules.GetHealthRatio(0f, 0f), Is.EqualTo(1f));
        }

        [Test]
        public void RepairStoneCost_UsesActualHealPackage_UnitPriceAndDayMultiplier()
        {
            Assert.That(SingleWallDefenseRules.GetRepairHealAmount(175f, 350f, 0.25f),
                Is.EqualTo(87.5f));
            Assert.That(SingleWallDefenseRules.CalculateRepairStoneCost(
                175f, 350f, 0.25f, 0.10f, 1f), Is.EqualTo(9));
            Assert.That(SingleWallDefenseRules.CalculateRepairStoneCost(
                315f, 350f, 0.25f, 0.10f, 1.5f), Is.EqualTo(6));
            Assert.That(SingleWallDefenseRules.CalculateRepairStoneCost(
                0f, 350f, 0.25f, 0.10f, 1f), Is.Zero);
        }
    }
}
