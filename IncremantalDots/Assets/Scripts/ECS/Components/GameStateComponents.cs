using Unity.Entities;

namespace DeadWalls
{
    public struct GameStateData : IComponentData
    {
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public bool IsGameOver;
        public bool IsLevelUpPending;
        // Kosu boyunca oldurulen toplam zombi — meta-progression kazanim kaynagi
        // (1 kill = 1 Ruh; DamageCleanupSystem sayar, GameOver'da MetaProgression toplar)
        public int TotalKills;
    }

    public enum RunPhaseType : byte
    {
        DayPrep,
        NightCombat
    }

    public struct WaveStateData : IComponentData
    {
        public int CurrentWave;
        public int ZombiesToSpawn;
        public int ZombiesSpawned;
        public int ZombiesAlive;
        public float SpawnTimer;
        public float SpawnInterval;
        public float ZombieHP;
        public float ZombieDamage;
        public float ZombieSpeed;
        public bool WaveActive;
        public RunPhaseType Phase;
        public float PrepTimer;
        public float PrepDuration;
        public float WaveStartDelay;
        public float WaveStartTimer;
        public bool StressTestMode;
        // Exact save/Continue icin spawn RNG stream state'i. 0 ise default seed kullanilir.
        public uint SpawnRandomState;
    }
}
