using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace DeadWalls
{
    /// <summary>
    /// Sprite sheet animasyon verisi.
    /// System tarafindan her frame guncellenir.
    ///
    /// Atlas yapisi (15 col x 32 row, Character Creator - Fantasy 2D):
    ///   Row  0- 7: Walk   (E, SE, S, SW, W, NW, N, NE)  ← Moving + Queued
    ///   Row  8-15: Attack (E, SE, S, SW, W, NW, N, NE)  ← Attacking (melee)
    ///   Row 16-23: Die    (E, SE, S, SW, W, NW, N, NE)  ← Dead
    ///   Row 24-31: Idle   (E, SE, S, SW, W, NW, N, NE)  ← Bosta
    ///
    /// Yon indeksleri (saat yonu, East'ten baslayarak):
    ///   0=E, 1=SE, 2=S, 3=SW, 4=W, 5=NW, 6=N, 7=NE
    ///
    /// DirectionRow = animOffset + directionIndex
    ///   Walk:   0 + dir (0-7)
    ///   Attack: 8 + dir (8-15)
    ///   Die:   16 + dir (16-23)
    ///   Idle:  24 + dir (24-31)
    ///
    /// UV flip: uvRow = (TotalRows - 1) - DirectionRow
    /// </summary>
    public struct SpriteAnimation : IComponentData
    {
        // Grid boyutlari
        public int TotalColumns;    // sprite sheet sutun sayisi (15)
        public int TotalRows;       // sprite sheet satir sayisi (32 = 4 anim x 8 yon)

        // Satir indeksi (animOffset + directionIndex, 0-31)
        public int DirectionRow;

        // Animasyon durumu
        public int FrameCount;      // bu animasyondaki frame sayisi (genelde 15)
        public int CurrentFrame;    // su an gosterilen frame (0-based)
        public float FrameTimer;    // gecen sure
        public float FrameInterval; // frame basina sure (1.0 / FPS)
    }

    /// <summary>
    /// GPU'ya gonderilen per-instance UV rect.
    /// Entities Graphics bu component'i otomatik olarak shader'a iletir.
    ///
    /// Value encoding:
    ///   x = UV offset X (frame sol kenari)
    ///   y = UV offset Y (frame alt kenari)
    ///   z = UV scale X  (frame genisligi, orn: 0.25)
    ///   w = UV scale Y  (frame yuksekligi, orn: 0.25)
    /// </summary>
    [MaterialProperty("_UVRect")]
    public struct SpriteUVRect : IComponentData
    {
        public float4 Value;
    }
}
