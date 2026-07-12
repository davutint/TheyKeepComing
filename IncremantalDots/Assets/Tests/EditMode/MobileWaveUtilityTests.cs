using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class MobileWaveUtilityTests
    {
        [Test]
        public void ConfigureMobileWave_IgnoresStatGrowthFields_ButIncreasesQuantityPressure()
        {
            var config = new MobileCastleCombatConfig
            {
                BaseWaveEnemyCount = 30,
                ExtraEnemiesPerWave = 10,
                ZombieBaseHP = 20f,
                ZombieHpGrowthPerCycle = 9f,
                ZombieBaseDamage = 5f,
                ZombieDamagePerCycle = 9f,
                BaseZombieSpeed = 0.85f,
                ZombieSpeedPerWave = 9f,
                BaseSpawnInterval = 0.95f,
                SpawnIntervalWaveMultiplier = 0.96f,
                MinSpawnInterval = 0.35f
            };

            var dayOne = new WaveStateData { CurrentWave = 1 };
            var advanced = new WaveStateData { CurrentWave = 50 };
            MobileWaveUtility.ConfigureMobileWave(ref dayOne, config);
            MobileWaveUtility.ConfigureMobileWave(ref advanced, config);

            Assert.That(advanced.ZombieHP, Is.EqualTo(dayOne.ZombieHP));
            Assert.That(advanced.ZombieDamage, Is.EqualTo(dayOne.ZombieDamage));
            Assert.That(advanced.ZombieSpeed, Is.EqualTo(dayOne.ZombieSpeed));
            Assert.That(advanced.ZombiesToSpawn, Is.GreaterThan(dayOne.ZombiesToSpawn));
            Assert.That(advanced.SpawnInterval, Is.LessThan(dayOne.SpawnInterval));
        }
    }
}
