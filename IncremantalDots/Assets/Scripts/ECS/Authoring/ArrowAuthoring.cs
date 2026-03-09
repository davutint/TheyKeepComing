using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class ArrowAuthoring : MonoBehaviour
    {
        public float Speed = 12f;
        public float Damage = 10f;

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
                    Target = Entity.Null
                });
            }
        }
    }
}
