using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    public partial struct ZombieDeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            GameStateData currentGameState = SystemAPI.GetSingleton<GameStateData>();
            if (currentGameState.IsGameOver || currentGameState.IsLevelUpPending)
                return;

            bool hasWaveState = SystemAPI.TryGetSingleton(out WaveStateData waveState);
            bool hasMobileConfig = SystemAPI.TryGetSingleton(out MobileCastleCombatConfig mobileConfig);
            bool graveEssenceDropsEnabled = hasWaveState
                && hasMobileConfig
                && !waveState.StressTestMode
                && mobileConfig.GraveEssenceDropChance > 0f
                && mobileConfig.GraveEssencePerDrop > 0;

            var deathPositions = new NativeQueue<float3>(Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            JobHandle deathHandle = new DeathCheckJob
            {
                DeathPositions = deathPositions.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            JobHandle rewardHandle = new ApplyDeathRewardsJob
            {
                DeathPositions = deathPositions,
                GameStateEntity = SystemAPI.GetSingletonEntity<GameStateData>(),
                GameStateLookup = SystemAPI.GetComponentLookup<GameStateData>(false),
                GraveEssenceDropsEnabled = graveEssenceDropsEnabled,
                GraveEssenceDropChance = graveEssenceDropsEnabled
                    ? math.saturate(mobileConfig.GraveEssenceDropChance)
                    : 0f,
                GraveEssencePerDrop = graveEssenceDropsEnabled
                    ? math.max(1, mobileConfig.GraveEssencePerDrop)
                    : 0,
                GraveEssenceDropSeed = graveEssenceDropsEnabled
                    ? mobileConfig.GraveEssenceDropSeed
                    : 0u,
                SpawnRandomState = graveEssenceDropsEnabled
                    ? waveState.SpawnRandomState
                    : 0u,
                ECB = ecb
            }.Schedule(deathHandle);

            state.Dependency = deathPositions.Dispose(rewardHandle);
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct DeathCheckJob : IJobEntity
        {
            public NativeQueue<float3>.ParallelWriter DeathPositions;

            void Execute(in ZombieStats stats,
                ref ZombieState zombieState, in LocalTransform transform)
            {
                if (zombieState.Value != ZombieStateType.Dead && stats.CurrentHP <= 0f)
                {
                    zombieState.Value = ZombieStateType.Dead;
                    DeathPositions.Enqueue(transform.Position);
                }
            }
        }

        [BurstCompile]
        private struct ApplyDeathRewardsJob : IJob
        {
            public NativeQueue<float3> DeathPositions;
            public Entity GameStateEntity;
            public ComponentLookup<GameStateData> GameStateLookup;
            public bool GraveEssenceDropsEnabled;
            public float GraveEssenceDropChance;
            public int GraveEssencePerDrop;
            public uint GraveEssenceDropSeed;
            public uint SpawnRandomState;
            public EntityCommandBuffer ECB;

            public void Execute()
            {
                int deathCount = 0;
                GameStateData gameState = GameStateLookup[GameStateEntity];
                int baseKills = math.max(0, gameState.TotalKills);
                while (DeathPositions.TryDequeue(out float3 position))
                {
                    deathCount++;

                    Entity soulEvent = ECB.CreateEntity();
                    ECB.AddComponent(soulEvent, new SoulPickupEvent
                    {
                        Position = position,
                        Amount = 1
                    });

                    long ordinalLong = (long)baseKills + deathCount;
                    int killOrdinal = ordinalLong >= int.MaxValue
                        ? int.MaxValue
                        : (int)ordinalLong;
                    if (GraveEssenceDropsEnabled
                        && GraveEssenceDropUtility.ShouldDrop(
                            GraveEssenceDropChance,
                            GraveEssenceDropSeed,
                            SpawnRandomState,
                            killOrdinal))
                    {
                        Entity essenceEvent = ECB.CreateEntity();
                        ECB.AddComponent(essenceEvent, new GraveEssenceDropEvent
                        {
                            Position = position,
                            Amount = GraveEssencePerDrop
                        });
                    }
                }

                if (deathCount <= 0)
                    return;

                long totalKills = (long)baseKills + deathCount;
                gameState.TotalKills = totalKills >= int.MaxValue
                    ? int.MaxValue
                    : (int)totalKills;
                GameStateLookup[GameStateEntity] = gameState;
            }
        }
    }
}
