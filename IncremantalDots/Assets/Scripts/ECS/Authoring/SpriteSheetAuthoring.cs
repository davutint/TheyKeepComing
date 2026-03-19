using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Prefab'a eklenen sprite sheet animasyon ayarlari.
    /// Baker, bu verileri SpriteAnimation + SpriteUVRect component'larina donusturur.
    ///
    /// Atlas yapisi (Character Creator - Fantasy 2D):
    ///   15 sutun (frame), 32 satir (4 animasyon x 8 yon)
    ///   Row  0- 7: Walk    Row  8-15: Attack
    ///   Row 16-23: Die     Row 24-31: Idle
    ///   Yon: 0=E, 1=SE, 2=S, 3=SW, 4=W, 5=NW, 6=N, 7=NE
    ///
    /// Inspector'da gorunecek alanlar:
    ///   Columns      — sutun sayisi (15)
    ///   Rows         — satir sayisi (32)
    ///   FPS          — animasyon hizi
    ///   DirectionRow — baslangic satiri (animOffset + yonIndex)
    ///   FrameCount   — bu animasyondaki frame sayisi (15)
    /// </summary>
    public class SpriteSheetAuthoring : MonoBehaviour
    {
        [Header("Sprite Sheet Grid")]
        [Tooltip("Atlas sutun sayisi (Character Creator: 15)")]
        public int Columns = 15;

        [Tooltip("Atlas satir sayisi (4 anim x 8 yon = 32)")]
        public int Rows = 32;

        [Header("Animasyon")]
        [Tooltip("Saniyedeki frame sayisi")]
        public float FPS = 10f;

        [Tooltip("Baslangic satiri: animOffset + yonIndex (Walk+West = 0+4 = 4)")]
        [Range(0, 31)]
        public int DirectionRow = 4;

        [Tooltip("Bu animasyondaki frame sayisi (Character Creator: 15)")]
        public int FrameCount = 15;

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
