using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class VillagerWorkerAuthoring : MonoBehaviour
    {
        public EconomyFocusType Resource = EconomyFocusType.Wood;
        public int Index;

        public class Baker : Baker<VillagerWorkerAuthoring>
        {
            public override void Bake(VillagerWorkerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ResourceWorkerVisual
                {
                    Resource = EconomyFocusUtility.Normalize(authoring.Resource),
                    Index = authoring.Index
                });
            }
        }
    }
}
