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
            public EntityCommandBuffer ECB;

            public void Execute()
            {
                int deathCount = 0;
                float3 representativePosition = float3.zero;
                while (DeathPositions.TryDequeue(out float3 position))
                {
                    if (deathCount == 0)
                        representativePosition = position;
                    deathCount++;

                    Entity soulEvent = ECB.CreateEntity();
                    ECB.AddComponent(soulEvent, new SoulPickupEvent
                    {
                        Position = position,
                        Amount = 1
                    });
                }

                if (deathCount <= 0)
                    return;

                GameStateData gameState = GameStateLookup[GameStateEntity];
                long totalKills = (long)math.max(0, gameState.TotalKills) + deathCount;
                gameState.TotalKills = totalKills >= int.MaxValue
                    ? int.MaxValue
                    : (int)totalKills;
                GameStateLookup[GameStateEntity] = gameState;

                Entity sfxEvent = ECB.CreateEntity();
                ECB.AddComponent(sfxEvent, new CombatSfxEvent
                {
                    Position = representativePosition,
                    Type = CombatSfxType.ZombieDeath,
                    Volume = 0.35f,
                    Pitch = 1f,
                    Multiplicity = deathCount
                });
            }
        }
    }
}
