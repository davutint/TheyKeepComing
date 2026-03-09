using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Sprite sheet animasyonunu gunceller.
    /// Her frame'de timer ilerletir, frame degistirir, UV rect hesaplar.
    /// Burst compiled — 50K+ entity'de performansli.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SpriteAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (anim, uvRect) in
                SystemAPI.Query<RefRW<SpriteAnimation>, RefRW<SpriteUVRect>>())
            {
                // --- Timer guncelle ---
                anim.ValueRW.FrameTimer += dt;

                // --- Frame ilerlet ---
                if (anim.ValueRO.FrameTimer >= anim.ValueRO.FrameInterval)
                {
                    anim.ValueRW.FrameTimer -= anim.ValueRO.FrameInterval;
                    anim.ValueRW.CurrentFrame++;

                    // Dongu: son frame'den sonra basa don
                    if (anim.ValueRO.CurrentFrame >= anim.ValueRO.FrameCount)
                    {
                        anim.ValueRW.CurrentFrame = 0;
                    }
                }

                // --- UV Rect hesapla ---
                //
                //  Sprite sheet (gorsel):        UV space:
                //   Row 0 = Down (ust)           Row 0 = alt  (y=0)
                //   Row 1 = Left                 Row 1 = ...
                //   Row 2 = Right                Row 2 = ...
                //   Row 3 = Up   (alt)           Row 3 = ust  (y=0.75)
                //
                //  Flip: uvRow = (TotalRows - 1) - DirectionRow
                //
                int col = anim.ValueRO.CurrentFrame;
                int uvRow = (anim.ValueRO.TotalRows - 1) - anim.ValueRO.DirectionRow;

                float scaleX = 1f / anim.ValueRO.TotalColumns;
                float scaleY = 1f / anim.ValueRO.TotalRows;
                float offsetX = col * scaleX;
                float offsetY = uvRow * scaleY;

                uvRect.ValueRW.Value = new float4(offsetX, offsetY, scaleX, scaleY);
            }
        }
    }
}
