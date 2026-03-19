using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    /// <summary>
    /// Zombi state degistiginde sprite animasyonunu gunceller.
    ///
    /// Atlas layout (15 col x 32 row — Character Creator - Fantasy 2D):
    ///   Row  0- 7: Walk   (8 yon, 15 frame)  ← Moving + Queued
    ///   Row  8-15: Attack (8 yon, 15 frame)  ← Attacking (melee swing)
    ///   Row 16-23: Die    (8 yon, 15 frame)  ← Dead
    ///   Row 24-31: Idle   (8 yon, 15 frame)  ← Bosta
    ///
    /// Yon indeksleri (saat yonu):
    ///   0=E, 1=SE, 2=S, 3=SW, 4=W, 5=NW, 6=N, 7=NE
    ///
    /// Animasyon offset'leri:
    ///   Walk=0, Attack=8, Die=16, Idle=24
    ///
    /// DirectionRow = animOffset + directionIndex
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieDeathSystem))]
    [UpdateBefore(typeof(DamageApplySystem))]
    public partial struct ZombieAnimationStateSystem : ISystem
    {
        // Animasyon blogu baslangiclari (her biri 8 satir)
        const int WalkOffset = 0;
        const int AttackOffset = 8;
        const int DieOffset = 16;
        // const int IdleOffset = 24; // ileride kullanilacak

        // Her animasyondaki frame sayisi
        const int WalkFrameCount = 15;
        const int AttackFrameCount = 15;
        const int DieFrameCount = 15;

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
                // Yon indeksini extract et (0-7: E, SE, S, SW, W, NW, N, NE)
                int dir = anim.DirectionRow % 8;

                switch (zombieState.Value)
                {
                    case ZombieStateType.Moving:
                    {
                        // Walk: Row 0-7, 15 frame
                        int targetRow = WalkOffset + dir;
                        if (anim.DirectionRow != targetRow)
                        {
                            anim.DirectionRow = targetRow;
                            anim.FrameCount = WalkFrameCount;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;
                    }

                    case ZombieStateType.Attacking:
                    {
                        // Attack: Row 8-15, 15 frame
                        int targetRow = AttackOffset + dir;
                        if (anim.DirectionRow != targetRow)
                        {
                            anim.DirectionRow = targetRow;
                            anim.FrameCount = AttackFrameCount;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;
                    }

                    case ZombieStateType.Queued:
                    {
                        // Queued: Walk animasyonu (Moving ile ayni)
                        int targetRow = WalkOffset + dir;
                        if (anim.DirectionRow != targetRow)
                        {
                            anim.DirectionRow = targetRow;
                            anim.FrameCount = WalkFrameCount;
                            anim.CurrentFrame = 0;
                            anim.FrameTimer = 0f;
                        }
                        break;
                    }

                    case ZombieStateType.Dead:
                    {
                        // Die: Row 16-23, 15 frame, loop yok
                        anim.DirectionRow = DieOffset + dir;
                        anim.FrameCount = DieFrameCount;
                        anim.CurrentFrame = 0;
                        anim.FrameTimer = 0f;

                        // Olum animasyonu suresi: 15 frame * FrameInterval
                        float deathDuration = DieFrameCount * anim.FrameInterval;
                        ECB.AddComponent(sortKey, entity,
                            new DeathTimer { Value = deathDuration });
                        break;
                    }
                }
            }
        }
    }
}
