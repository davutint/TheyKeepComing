using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct WaveSpawnSystem : ISystem
    {
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _random = Random.CreateFromIndex(42);
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<ZombiePrefabData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WallXPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var waveState = SystemAPI.GetSingletonRW<WaveStateData>();
            float dt = SystemAPI.Time.DeltaTime;

            // Stress Test Mode: wave/gameover kontrolu yok, surekli spawn
            if (waveState.ValueRO.StressTestMode)
            {
                waveState.ValueRW.SpawnTimer -= dt;
                if (waveState.ValueRO.SpawnTimer <= 0f)
                {
                    waveState.ValueRW.SpawnTimer = waveState.ValueRO.SpawnInterval;
                    SpawnZombieBatch(ref state, ref waveState.ValueRW, 20);
                }
                return;
            }

            // --- Normal wave mantigi ---
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            // Wave baslama gecikmesi
            if (waveState.ValueRO.WaveStartTimer > 0f)
            {
                waveState.ValueRW.WaveStartTimer -= dt;
                return;
            }

            // Tum zombiler spawn edildi ve oldu → yeni wave
            if (waveState.ValueRO.ZombiesSpawned >= waveState.ValueRO.ZombiesToSpawn
                && waveState.ValueRO.ZombiesAlive <= 0)
            {
                StartNextWave(ref waveState.ValueRW);
                return;
            }

            // Spawn zamani — frame basina batch spawn
            if (waveState.ValueRO.ZombiesSpawned < waveState.ValueRO.ZombiesToSpawn)
            {
                waveState.ValueRW.SpawnTimer -= dt;
                if (waveState.ValueRO.SpawnTimer <= 0f)
                {
                    waveState.ValueRW.SpawnTimer = waveState.ValueRO.SpawnInterval;

                    int batchSize = math.min(20, waveState.ValueRO.ZombiesToSpawn - waveState.ValueRO.ZombiesSpawned);
                    SpawnZombieBatch(ref state, ref waveState.ValueRW, batchSize);
                }
            }
        }

        private void StartNextWave(ref WaveStateData wave)
        {
            wave.CurrentWave++;
            int w = wave.CurrentWave;

            // STRESS TEST: ZombiSayisi = 500 * wave
            wave.ZombiesToSpawn = 500 * w;
            wave.ZombieHP = 20f * math.pow(w, 1.4f);
            wave.ZombieDamage = 0f; // TEST: hasar kapatildi
            wave.ZombieSpeed = 1.5f + (w - 1) * 0.1f;
            wave.ZombiesSpawned = 0;
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 0f;
            wave.WaveStartTimer = wave.WaveStartDelay;
        }

        private void SpawnZombieBatch(ref SystemState state, ref WaveStateData wave, int count)
        {
            var prefabData = SystemAPI.GetSingleton<ZombiePrefabData>();
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                var zombie = ecb.Instantiate(prefabData.ZombiePrefab);

                // Sag tarafta random Y pozisyonunda, X de biraz dagitilmis
                float spawnX = 28f + _random.NextFloat(0f, 5f);
                float spawnY = _random.NextFloat(-12f, 12f);
                var transform = LocalTransform.FromPositionRotationScale(
                    new float3(spawnX, spawnY, -1f),
                    quaternion.identity,
                    0.3f // kucuk zombi
                );
                ecb.SetComponent(zombie, transform);
                ecb.SetComponent(zombie, new ZombieState { Value = ZombieStateType.Moving });

                wave.ZombiesSpawned++;
                wave.ZombiesAlive++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
