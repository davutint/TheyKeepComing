using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class GameStateAuthoring : MonoBehaviour
    {
        [Header("Game State")]
        public float ClickDamage = 10f;
        public int XPToNextLevel = 100;

        [Header("Wave Config — STRESS TEST")]
        public bool StressTestMode = true;
        public float SpawnInterval = 0.05f;
        public float WaveStartDelay = 2f;
        public float BaseZombieSpeed = 2f;

        public class Baker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GameStateData
                {
                    Gold = 0,
                    XP = 0,
                    Level = 1,
                    XPToNextLevel = authoring.XPToNextLevel,
                    ClickDamage = authoring.ClickDamage,
                    IsGameOver = false,
                    IsLevelUpPending = false
                });

                AddComponent(entity, new WaveStateData
                {
                    CurrentWave = 1,
                    ZombiesToSpawn = 500,
                    ZombiesSpawned = 0,
                    ZombiesAlive = 0,
                    SpawnTimer = 0f,
                    SpawnInterval = authoring.SpawnInterval,
                    ZombieHP = 20f,
                    ZombieDamage = 5f,
                    ZombieSpeed = authoring.BaseZombieSpeed,
                    WaveActive = true,
                    WaveStartDelay = authoring.WaveStartDelay,
                    WaveStartTimer = authoring.WaveStartDelay,
                    StressTestMode = authoring.StressTestMode
                });
            }
        }
    }
}
