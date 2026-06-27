using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileWaveUtility
    {
        public static void ConfigureMobileWave(ref WaveStateData wave, MobileCastleCombatConfig config)
        {
            int waveNumber = math.max(1, wave.CurrentWave);
            wave.ZombiesToSpawn = config.BaseWaveEnemyCount + (waveNumber - 1) * config.ExtraEnemiesPerWave;
            wave.ZombieHP = 20f * math.pow(waveNumber, 1.2f);
            wave.ZombieDamage = 5f + (waveNumber - 1) * 0.5f;
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
