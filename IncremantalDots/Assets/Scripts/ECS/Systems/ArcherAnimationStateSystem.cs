using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArcherShootSystem))]
    public partial struct ArcherAnimationStateSystem : ISystem
    {
        private const int AttackOffset = 8;
        private const int IdleOffset = 24;
        private const int AttackFrameCount = 15;
        private const int IdleFrameCount = 15;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ArcherAnimationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ArcherAnimationJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref ArcherUnit archer, ref SpriteAnimation anim)
            {
                int direction = ResolveDirection(archer.FacingDirection, anim.DirectionRow % 8);
                if (archer.AttackAnimTimer > 0f)
                {
                    archer.AttackAnimTimer = math.max(0f, archer.AttackAnimTimer - DeltaTime);
                    SetAnimation(ref anim, AttackOffset + direction, AttackFrameCount);
                    return;
                }

                SetAnimation(ref anim, IdleOffset + direction, IdleFrameCount);
            }

            private static void SetAnimation(ref SpriteAnimation anim, int targetRow, int frameCount)
            {
                if (anim.DirectionRow == targetRow && anim.FrameCount == frameCount)
                    return;

                anim.DirectionRow = targetRow;
                anim.FrameCount = frameCount;
                anim.CurrentFrame = 0;
                anim.FrameTimer = 0f;
            }

            private static int ResolveDirection(float2 direction, int fallbackDirection)
            {
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
