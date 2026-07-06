using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileWaveUtility
    {
        public static void ConfigureMobileWave(ref WaveStateData wave, MobileCastleCombatConfig config)
        {
            int waveNumber = math.max(1, wave.CurrentWave);
            wave.ZombiesToSpawn = config.BaseWaveEnemyCount + (waveNumber - 1) * config.ExtraEnemiesPerWave;

            // Kutle-odakli eskalasyon: HP LINEER buyur (eski 20*w^1.2 ustel egri zombileri
            // "sungerlestiriyordu"; zorluk artisi kalabaliktan gelmeli — GDD pillar #1).
            // Eski bake'lerle uyumluluk icin config 0 ise legacy tabanlara duser.
            float baseHp = config.ZombieBaseHP > 0f ? config.ZombieBaseHP : 20f;
            float hpGrowth = math.max(0f, config.ZombieHpGrowthPerCycle);
            wave.ZombieHP = baseHp * (1f + (waveNumber - 1) * hpGrowth);

            float baseDamage = config.ZombieBaseDamage > 0f ? config.ZombieBaseDamage : 5f;
            wave.ZombieDamage = baseDamage + (waveNumber - 1) * math.max(0f, config.ZombieDamagePerCycle);

            wave.ZombieSpeed = config.BaseZombieSpeed + (waveNumber - 1) * config.ZombieSpeedPerWave;
            wave.SpawnInterval = math.max(
                config.MinSpawnInterval,
                config.BaseSpawnInterval * math.pow(config.SpawnIntervalWaveMultiplier, waveNumber - 1));
        }

        public static void StartNightWave(ref WaveStateData wave, MobileCastleCombatConfig config)
        {
            wave.CurrentWave = math.max(1, wave.CurrentWave + 1);
            ConfigureMobileWave(ref wave, config);
            wave.ZombiesSpawned = 0;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 0f;
            wave.WaveStartTimer = 0f;
            wave.WaveActive = true;
            wave.Phase = RunPhaseType.NightCombat;
            wave.PrepTimer = 0f;
            wave.PrepDuration = 0f;
        }
    }
}
