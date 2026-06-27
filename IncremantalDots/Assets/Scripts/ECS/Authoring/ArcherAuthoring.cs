using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class ArcherAuthoring : MonoBehaviour
    {
        public ArcherType Type = ArcherType.Basic;
        public float FireRate = 1.5f;
        public float ArrowDamage = 10f;
        public float Range = 15f;
        public float SlowDuration = 0f;
        public float SlowMultiplier = 1f;
        public Color Tint = Color.white;

        public class Baker : Baker<ArcherAuthoring>
        {
            public override void Bake(ArcherAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ArcherUnit
                {
                    FireRate = authoring.FireRate,
                    FireTimer = 0f,
                    ArrowDamage = authoring.ArrowDamage,
                    Range = authoring.Range,
                    Type = authoring.Type,
                    SlowDuration = authoring.SlowDuration,
                    SlowMultiplier = authoring.SlowMultiplier,
                    FacingDirection = new float2(1f, 0f),
                    AttackAnimTimer = 0f
                });

                if (authoring.GetComponent<SpriteSheetAuthoring>() == null)
                {
                    AddComponent(entity, new SpriteTint
                    {
                        Value = new float4(authoring.Tint.r, authoring.Tint.g, authoring.Tint.b, authoring.Tint.a)
                    });
                }
            }
        }
    }
}
