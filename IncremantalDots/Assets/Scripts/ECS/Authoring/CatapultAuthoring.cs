using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class CatapultAuthoring : MonoBehaviour
    {
        public float Damage = 40f;
        public float SplashRadius = 2f;
        public float FireRate = 0.2f;
        public float Range = 25f;
        public int StoneCostPerShot = 1;

        public class Baker : Baker<CatapultAuthoring>
        {
            public override void Bake(CatapultAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new CatapultUnit
                {
                    Damage = authoring.Damage,
                    SplashRadius = authoring.SplashRadius,
                    FireRate = authoring.FireRate,
                    FireTimer = 0f,
                    Range = authoring.Range,
                    StoneCostPerShot = authoring.StoneCostPerShot
                });
            }
        }
    }
}
