using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Zombi state degistiginde sprite animasyonunu gunceller.
    ///
    /// Vampire_atlas.png layout (4 col x 12 row):
    ///   Row 0-3:  Walk (4 frame/yon)  <- Moving state
    ///   Row 4-7:  Hit  (2 frame/yon)  <- Attacking state
    ///   Row 8-11: Die  (1 frame/yon)  <- Dead state
    ///
    /// Zombi sola bakar -> base DirectionRow = 1 (Left)
    ///   Walk Left = Row 1
    ///   Hit Left  = Row 5  (+4)
    ///   Die Left  = Row 9  (+8)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieDeathSystem))]
    [UpdateBefore(typeof(DamageApplySystem))]
    public partial struct ZombieAnimationStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new AnimationStateJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        [WithNone(typeof(DeathTimer))]
        partial struct AnimationStateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ZombieState zombieState, ref SpriteAnimation anim)
            {
                // Walk base row (0-3 arasi, zombi icin 1=Left)
                int baseRow = anim.DirectionRow % 4;

                switch (zombieState.Value)
                {
                    case ZombieStateType.Moving:
                        // Walk: Row 0-3, 4 frame
                        if (anim.DirectionRow != baseRow)
                        {
                            anim.DirectionRow = baseRow;
                            anim.FrameCount = 4;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;

                    case ZombieStateType.Attacking:
                        // Hit: Row 4-7, 2 frame
                        int hitRow = baseRow + 4;
                        if (anim.DirectionRow != hitRow)
                        {
                            anim.DirectionRow = hitRow;
                            anim.FrameCount = 2;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;

                    case ZombieStateType.Queued:
                        // Queued: Yuruyus animasyonu (Moving ile ayni)
                        if (anim.DirectionRow != baseRow)
                        {
                            anim.DirectionRow = baseRow;
                            anim.FrameCount = 4;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;

                    case ZombieStateType.Dead:
                        // Die: Row 8-11, 1 frame
                        anim.DirectionRow = baseRow + 8;
                        anim.FrameCount = 1;
                        anim.CurrentFrame = 0;
                        anim.FrameTimer = 0f;

                        ECB.AddComponent(sortKey, entity, new DeathTimer { Value = 0.5f });
                        break;
                }
            }
        }
    }
}
