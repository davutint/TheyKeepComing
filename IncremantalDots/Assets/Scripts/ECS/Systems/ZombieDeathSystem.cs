using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    public partial struct ZombieDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DeathCheckJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct DeathCheckJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute([ChunkIndexInQuery] int sortKey, in ZombieStats stats,
                ref ZombieState zombieState, in LocalTransform transform)
            {
                if (zombieState.Value != ZombieStateType.Dead && stats.CurrentHP <= 0f)
                {
                    zombieState.Value = ZombieStateType.Dead;

                    // Olum ani SFX'i (M-D): bridge rate-limit'i kalabalik olumlerde yigilmayi keser
                    var sfxEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, sfxEvent, new CombatSfxEvent
                    {
                        Position = transform.Position,
                        Type = CombatSfxType.ZombieDeath,
                        Volume = 0.35f,
                        Pitch = 1f
                    });
                }
            }
        }
    }
}
