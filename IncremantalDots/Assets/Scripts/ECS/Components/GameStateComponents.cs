using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct GameStateData : IComponentData
    {
        public int Gold;
        public int XP;
        public int Level;
        public int XPToNextLevel;
        public float ClickDamage;
        public bool IsGameOver;
        public bool IsLevelUpPending;
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
        public float WaveStartDelay;
        public float WaveStartTimer;
        public bool StressTestMode;
    }

    public struct ClickDamageRequest : IComponentData
    {
        public float3 WorldPosition;
        public float Damage;
    }
}
