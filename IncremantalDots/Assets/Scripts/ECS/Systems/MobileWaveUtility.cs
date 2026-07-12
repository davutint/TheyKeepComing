using Unity.Mathematics;

namespace DeadWalls
{
    public static class MobileWaveUtility
    {
        public static void ConfigureMobileWave(ref WaveStateData wave, MobileCastleCombatConfig config)
        {
            int waveNumber = math.max(1, wave.CurrentWave);
            wave.ZombiesToSpawn = config.BaseWaveEnemyCount + (waveNumber - 1) * config.ExtraEnemiesPerWave;

            // V1 quantity-only difficulty: enemy HP/damage/speed gun veya cycle ile BUYUMEZ.
            // Zorluk yalniz count, batch ve spawn interval/budget uzerinden artar.
            float baseHp = config.ZombieBaseHP > 0f ? config.ZombieBaseHP : 20f;
            wave.ZombieHP = baseHp;

            float baseDamage = config.ZombieBaseDamage > 0f ? config.ZombieBaseDamage : 5f;
            wave.ZombieDamage = baseDamage;

            wave.ZombieSpeed = math.max(0.05f, config.BaseZombieSpeed);
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
