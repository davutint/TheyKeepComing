using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace DeadWalls
{
    /// <summary>
    /// Sprite sheet animasyon verisi.
    /// System tarafindan her frame guncellenir.
    ///
    /// Sprite sheet yapisi:
    ///   ┌───┬───┬───┬───┐
    ///   │ 0 │ 1 │ 2 │ 3 │  Row 0 (Down)   → UV Row 3
    ///   ├───┼───┼───┼───┤
    ///   │ 4 │ 5 │ 6 │ 7 │  Row 1 (Left)   → UV Row 2  ← Zombi
    ///   ├───┼───┼───┼───┤
    ///   │ 8 │ 9 │10 │11 │  Row 2 (Right)  → UV Row 1  ← Okcu
    ///   ├───┼───┼───┼───┤
    ///   │12 │13 │14 │15 │  Row 3 (Up)     → UV Row 0
    ///   └───┴───┴───┴───┘
    /// </summary>
    public struct SpriteAnimation : IComponentData
    {
        // Grid boyutlari
        public int TotalColumns;    // sprite sheet sutun sayisi (genelde 4)
        public int TotalRows;       // sprite sheet satir sayisi (genelde 4)

        // Yön satiri (sprite sheet'teki satir, 0=Down, 1=Left, 2=Right, 3=Up)
        public int DirectionRow;

        // Animasyon durumu
        public int FrameCount;      // bu yondeki frame sayisi (genelde 4)
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
