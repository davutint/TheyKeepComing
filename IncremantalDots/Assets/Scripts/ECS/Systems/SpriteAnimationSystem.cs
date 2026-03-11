using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Sprite sheet animasyonunu gunceller.
    /// Her frame'de timer ilerletir, frame degistirir, UV rect hesaplar.
    /// Burst compiled IJobEntity — 50K+ entity'de performansli.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SpriteAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new AnimateJob { Dt = SystemAPI.Time.DeltaTime }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct AnimateJob : IJobEntity
        {
            public float Dt;

            void Execute(ref SpriteAnimation anim, ref SpriteUVRect uvRect)
            {
                // Timer guncelle
                anim.FrameTimer += Dt;

                // Frame ilerlet
                if (anim.FrameTimer >= anim.FrameInterval)
                {
                    anim.FrameTimer -= anim.FrameInterval;
                    anim.CurrentFrame++;

                    // Dongu: son frame'den sonra basa don
                    if (anim.CurrentFrame >= anim.FrameCount)
                        anim.CurrentFrame = 0;
                }

                // UV Rect hesapla
                //  Sprite sheet (gorsel):        UV space:
                //   Row 0 = Down (ust)           Row 0 = alt  (y=0)
                //   Row 1 = Left                 Row 1 = ...
                //   Row 2 = Right                Row 2 = ...
                //   Row 3 = Up   (alt)           Row 3 = ust  (y=0.75)
                //  Flip: uvRow = (TotalRows - 1) - DirectionRow
                int col = anim.CurrentFrame;
                int uvRow = (anim.TotalRows - 1) - anim.DirectionRow;

                float scaleX = 1f / anim.TotalColumns;
                float scaleY = 1f / anim.TotalRows;
                float offsetX = col * scaleX;
                float offsetY = uvRow * scaleY;

                uvRect.Value = new float4(offsetX, offsetY, scaleX, scaleY);
            }
        }
    }
}
