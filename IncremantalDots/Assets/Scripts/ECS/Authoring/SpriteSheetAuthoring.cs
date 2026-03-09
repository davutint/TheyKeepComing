using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Prefab'a eklenen sprite sheet animasyon ayarlari.
    /// Baker, bu verileri SpriteAnimation + SpriteUVRect component'larina donusturur.
    ///
    /// Inspector'da gorunecek alanlar:
    ///   Columns      — sprite sheet sutun sayisi (4)
    ///   Rows         — sprite sheet satir sayisi (4)
    ///   FPS          — animasyon hizi (7 = piksel art icin iyi)
    ///   DirectionRow — hangi yon satiri (0=Down, 1=Left, 2=Right, 3=Up)
    ///   FrameCount   — bu yondeki frame sayisi (genelde Columns kadar)
    /// </summary>
    public class SpriteSheetAuthoring : MonoBehaviour
    {
        [Header("Sprite Sheet Grid")]
        [Tooltip("Sprite sheet'teki sutun sayisi")]
        public int Columns = 4;

        [Tooltip("Sprite sheet'teki satir sayisi")]
        public int Rows = 4;

        [Header("Animasyon")]
        [Tooltip("Saniyedeki frame sayisi")]
        public float FPS = 7f;

        [Tooltip("Yon satiri: 0=Down, 1=Left, 2=Right, 3=Up")]
        [Range(0, 3)]
        public int DirectionRow = 1;

        [Tooltip("Bu yondeki frame sayisi (genelde Columns ile ayni)")]
        public int FrameCount = 4;

        public class Baker : Baker<SpriteSheetAuthoring>
        {
            public override void Bake(SpriteSheetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Animasyon verisi
                AddComponent(entity, new SpriteAnimation
                {
                    TotalColumns = authoring.Columns,
                    TotalRows = authoring.Rows,
                    DirectionRow = authoring.DirectionRow,
                    FrameCount = authoring.FrameCount,
                    CurrentFrame = 0,
                    FrameTimer = 0f,
                    FrameInterval = 1f / authoring.FPS
                });

                // Baslangic UV rect (ilk frame)
                int uvRow = (authoring.Rows - 1) - authoring.DirectionRow;
                float scaleX = 1f / authoring.Columns;
                float scaleY = 1f / authoring.Rows;

                AddComponent(entity, new SpriteUVRect
                {
                    Value = new float4(0f, uvRow * scaleY, scaleX, scaleY)
                });
            }
        }
    }
}
