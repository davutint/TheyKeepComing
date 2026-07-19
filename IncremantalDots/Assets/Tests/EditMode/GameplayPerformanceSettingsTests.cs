using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class GameplayPerformanceSettingsTests
    {
        private ZombieLimitPreset _originalPreset;

        [SetUp]
        public void SetUp()
        {
            _originalPreset = GameplayPerformanceSettings.CurrentZombieLimitPreset;
        }

        [TearDown]
        public void TearDown()
        {
            GameplayPerformanceSettings.CurrentZombieLimitPreset = _originalPreset;
        }

        [Test]
        public void ZombieLimitPresets_PreserveReleaseDefaultAndApprovedStressTiers()
        {
            Assert.That(
                GameplayPerformanceSettings.GetLimit(ZombieLimitPreset.Balanced),
                Is.EqualTo(900));
            Assert.That(
                GameplayPerformanceSettings.GetLimit(ZombieLimitPreset.High),
                Is.EqualTo(2_000));
            Assert.That(
                GameplayPerformanceSettings.GetLimit(ZombieLimitPreset.Massive),
                Is.EqualTo(5_000));
            Assert.That(
                GameplayPerformanceSettings.GetLimit(ZombieLimitPreset.Extreme),
                Is.EqualTo(10_000));
        }

        [Test]
        public void ZombieLimitPreset_PersistsAndDrivesResolvedLimit()
        {
            GameplayPerformanceSettings.CurrentZombieLimitPreset = ZombieLimitPreset.Massive;

            Assert.That(
                GameplayPerformanceSettings.CurrentZombieLimitPreset,
                Is.EqualTo(ZombieLimitPreset.Massive));
            Assert.That(GameplayPerformanceSettings.MaxAliveZombies, Is.EqualTo(5_000));
        }

        [Test]
        public void ZombieLimitStepper_ClampsAtBothEnds()
        {
            Assert.That(
                GameplayPerformanceSettings.Step(ZombieLimitPreset.Balanced, -1),
                Is.EqualTo(ZombieLimitPreset.Balanced));
            Assert.That(
                GameplayPerformanceSettings.Step(ZombieLimitPreset.Balanced, 1),
                Is.EqualTo(ZombieLimitPreset.High));
            Assert.That(
                GameplayPerformanceSettings.Step(ZombieLimitPreset.Extreme, 1),
                Is.EqualTo(ZombieLimitPreset.Extreme));
        }

        [Test]
        public void EveryZombieLimitPreset_HasExplicitPerformanceCopy()
        {
            for (int i = 0; i < GameplayPerformanceSettings.PresetCount; i++)
            {
                ZombieLimitPreset preset = (ZombieLimitPreset)i;
                Assert.That(
                    GameplayPerformanceSettings.GetDisplayName(preset),
                    Is.Not.Empty);
                Assert.That(
                    GameplayPerformanceSettings.GetPerformanceHint(preset),
                    Is.Not.Empty);
            }
        }
    }
}
