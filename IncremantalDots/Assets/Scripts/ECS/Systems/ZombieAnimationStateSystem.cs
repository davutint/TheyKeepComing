using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            bool mobileMode = SystemAPI.HasSingleton<MobileCastleCombatConfig>();
            bool hasWallTarget = SystemAPI.HasSingleton<WallXPosition>();
            float2 targetPoint = float2.zero;

            if (mobileMode)
            {
                var mobileConfig = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
                // Tek cephe: hedef duvar hatti (sola bakis); 360 modda kale merkezi
                targetPoint = mobileConfig.SingleFrontEnabled
                    ? new float2(mobileConfig.FrontlineX, 0f)
                    : mobileConfig.CastleCenter;
            }
            else if (hasWallTarget)
            {
                targetPoint = new float2(SystemAPI.GetSingleton<WallXPosition>().Value, 0f);
            }

            new AnimationStateJob
            {
                HasTargetPoint = mobileMode || hasWallTarget,
                TargetPoint = targetPoint
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        [WithDisabled(typeof(DeathTimer))]
        partial struct AnimationStateJob : IJobEntity
        {
            public bool HasTargetPoint;
            public float2 TargetPoint;

            void Execute(in ZombieState zombieState,
                in LocalTransform transform,
                in PhysicsBody physicsBody,
                ref SpriteAnimation anim,
                ref DeathTimer deathTimer,
                EnabledRefRW<DeathTimer> deathTimerEnabled)
            {
                int dir = ResolveDirection(transform.Position.xy, physicsBody.Velocity, anim.DirectionRow % 8);

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
                        deathTimer.Value = DieFrameCount * anim.FrameInterval;
                        deathTimerEnabled.ValueRW = true;
                        break;
                    }
                }
            }

            private int ResolveDirection(float2 position, float2 velocity, int fallbackDirection)
            {
                float2 direction = velocity;
                if (math.lengthsq(direction) < 0.0001f && HasTargetPoint)
                    direction = TargetPoint - position;

                if (math.lengthsq(direction) < 0.0001f)
                    return fallbackDirection;

                float angle = math.atan2(direction.y, direction.x);
                int index = (int)math.round((-angle) / (math.PI * 0.25f));
                index %= 8;
                if (index < 0)
                    index += 8;

                return index;
            }
        }
    }
}
