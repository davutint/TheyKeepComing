using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class CatapultProjectileAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CatapultProjectileAuthoring>
        {
            public override void Bake(CatapultProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new CatapultProjectileTag());
                AddComponent(entity, new CatapultProjectile());
            }
        }
    }
}
