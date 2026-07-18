using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class CombatRewardFeedbackTests
    {
        [TestCase(12f, "12")]
        [TestCase(12.25f, "12.3")]
        [TestCase(0.5f, "0.5")]
        [TestCase(-4f, "0")]
        public void DamageNumber_FormatsAppliedDamageWithoutFalsePrecision(
            float damage,
            string expected)
        {
            Assert.That(CombatFeedbackBridge.FormatDamageNumber(damage), Is.EqualTo(expected));
        }

        [Test]
        public void DamageNumber_PlayerSourcesKeepDistinctReadabilityColors()
        {
            var basic = CombatFeedbackBridge.ResolveDamageNumberColor(
                PlayerDamageSourceType.BasicArrow);
            var frost = CombatFeedbackBridge.ResolveDamageNumberColor(
                PlayerDamageSourceType.FrostArrow);
            var fireball = CombatFeedbackBridge.ResolveDamageNumberColor(
                PlayerDamageSourceType.Fireball);

            Assert.That(frost, Is.Not.EqualTo(basic));
            Assert.That(fireball, Is.Not.EqualTo(basic));
            Assert.That(frost.a, Is.EqualTo(1f));
            Assert.That(fireball.a, Is.EqualTo(1f));
        }
    }
}
