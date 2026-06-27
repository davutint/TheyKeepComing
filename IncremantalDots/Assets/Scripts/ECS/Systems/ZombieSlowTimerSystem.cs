using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct ZombieSlowTimerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                var gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            float dt = SystemAPI.Time.DeltaTime;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var tintLookup = SystemAPI.GetComponentLookup<SpriteTint>(false);

            foreach (var (slow, zombieState, entity) in
                SystemAPI.Query<RefRW<ZombieSlow>, RefRO<ZombieState>>()
                    .WithAll<ZombieTag>()
                    .WithEntityAccess())
            {
                if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                {
                    if (tintLookup.HasComponent(entity))
                        tintLookup[entity] = new SpriteTint { Value = ArcherVisualStyle.NormalTint };

                    slow.ValueRW.Duration = 0f;
                    slow.ValueRW.SpeedMultiplier = 1f;
                    ecb.SetComponentEnabled<ZombieSlow>(entity, false);
                    continue;
                }

                slow.ValueRW.Duration -= dt;
                if (slow.ValueRO.Duration > 0f)
                {
                    if (tintLookup.HasComponent(entity))
                        tintLookup[entity] = new SpriteTint { Value = ArcherVisualStyle.SlowedZombieTint };

                    continue;
                }

                slow.ValueRW.Duration = 0f;
                slow.ValueRW.SpeedMultiplier = 1f;
                if (tintLookup.HasComponent(entity))
                    tintLookup[entity] = new SpriteTint { Value = ArcherVisualStyle.NormalTint };

                ecb.SetComponentEnabled<ZombieSlow>(entity, false);
            }
        }
    }
}
