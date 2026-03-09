using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class ArcherAuthoring : MonoBehaviour
    {
        public float FireRate = 1.5f;
        public float ArrowDamage = 10f;
        public float Range = 15f;

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
                    Range = authoring.Range
                });
            }
        }
    }
}
