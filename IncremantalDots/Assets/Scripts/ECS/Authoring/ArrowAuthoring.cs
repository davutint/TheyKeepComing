using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class ArrowAuthoring : MonoBehaviour
    {
        public float Speed = 12f;
        public float Damage = 10f;
        public ArcherType ArcherType = ArcherType.Basic;
        public float SlowDuration = 0f;
        public float SlowMultiplier = 1f;
        public Color Tint = Color.white;

        public class Baker : Baker<ArrowAuthoring>
        {
            public override void Bake(ArrowAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ArrowTag());
                AddComponent(entity, new ArrowProjectile
                {
                    Speed = authoring.Speed,
                    Damage = authoring.Damage,
                    Target = Entity.Null,
                    ArcherType = authoring.ArcherType,
                    SlowDuration = authoring.SlowDuration,
                    SlowMultiplier = authoring.SlowMultiplier
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
