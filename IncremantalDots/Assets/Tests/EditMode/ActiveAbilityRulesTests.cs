using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ActiveAbilityRulesTests
    {
        [Test]
        public void Rally_IsGuardedOnlyByUnlockCooldownActiveStateAndGameState()
        {
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 0f, false, false), Is.True);
            Assert.That(ActiveAbilityRules.CanUseRally(false, 0f, 0f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 1f, 0f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 1f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseRally(true, 0f, 0f, true, false), Is.False);
        }

        [Test]
        public void EmergencyRepair_IsNightOnlyAndCannotReviveDestroyedWall()
        {
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 50f, 100f, false, false), Is.True);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Day, 50f, 100f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 0f, 100f, false, false), Is.False);
            Assert.That(ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, 100f, 100f, false, false), Is.False);
        }

        [Test]
        public void SameFrameLethalDamage_WinsBeforeEmergencyRepairGuard()
        {
            float afterDamage = SingleWallDefenseRules.ApplyDamage(25f, 25f);
            bool canRepair = ActiveAbilityRules.CanUseEmergencyRepair(
                true, 0f, SiegeCyclePhase.Night, afterDamage, 500f, false, false);
            float afterRepair = canRepair
                ? SingleWallDefenseRules.HealByMaxPercent(afterDamage, 500f, 0.20f)
                : afterDamage;

            Assert.That(canRepair, Is.False);
            Assert.That(afterRepair, Is.Zero);
        }
    }
}
