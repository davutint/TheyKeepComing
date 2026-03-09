using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Zombi state degistiginde sprite animasyonunu gunceller.
    ///
    /// Vampire_atlas.png layout (4 col x 12 row):
    ///   Row 0-3:  Walk (4 frame/yon)  ← Moving state
    ///   Row 4-7:  Hit  (2 frame/yon)  ← Attacking state
    ///   Row 8-11: Die  (1 frame/yon)  ← Dead state
    ///
    /// Zombi sola bakar → base DirectionRow = 1 (Left)
    ///   Walk Left = Row 1
    ///   Hit Left  = Row 5  (+4)
    ///   Die Left  = Row 9  (+8)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieDeathSystem))]
    [UpdateBefore(typeof(DamageCleanupSystem))]
    public partial struct ZombieAnimationStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (zombieState, anim, entity) in
                SystemAPI.Query<RefRO<ZombieState>, RefRW<SpriteAnimation>>()
                    .WithAll<ZombieTag>()
                    .WithNone<DeathTimer>()
                    .WithEntityAccess())
            {
                // Walk base row (0-3 arasi, zombi icin 1=Left)
                int baseRow = anim.ValueRO.DirectionRow % 4;

                switch (zombieState.ValueRO.Value)
                {
                    case ZombieStateType.Moving:
                        // Walk: Row 0-3, 4 frame
                        if (anim.ValueRO.DirectionRow != baseRow)
                        {
                            anim.ValueRW.DirectionRow = baseRow;
                            anim.ValueRW.FrameCount = 4;
                            anim.ValueRW.CurrentFrame = 0;
                            anim.ValueRW.FrameTimer = 0f;
                        }
                        break;

                    case ZombieStateType.Attacking:
                        // Hit: Row 4-7, 2 frame
                        int hitRow = baseRow + 4;
                        if (anim.ValueRO.DirectionRow != hitRow)
                        {
                            anim.ValueRW.DirectionRow = hitRow;
                            anim.ValueRW.FrameCount = 2;
                            anim.ValueRW.CurrentFrame = 0;
                            anim.ValueRW.FrameTimer = 0f;
                        }
                        break;

                    case ZombieStateType.Dead:
                        // Die: Row 8-11, 1 frame
                        anim.ValueRW.DirectionRow = baseRow + 8;
                        anim.ValueRW.FrameCount = 1;
                        anim.ValueRW.CurrentFrame = 0;
                        anim.ValueRW.FrameTimer = 0f;

                        ecb.AddComponent(entity, new DeathTimer { Value = 0.5f });
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
