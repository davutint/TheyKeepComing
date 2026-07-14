using Unity.Entities;
using Unity.Mathematics;
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
                    Index = authoring.Index,
                    RepresentedWorkerCount = 1
                });
                AddComponent(entity, new WorkerLogisticsFeedbackState
                {
                    Activity = WorkerLogisticsActivity.Working,
                    IsCarrying = 1
                });
                AddComponent(entity, new WorkerAnimationMaterialProperty
                {
                    Value = (float)WorkerAnimationKind.Work
                });
                AddComponent(entity, new WorkerFeedbackMaterialProperty
                {
                    Value = new float4(1f, 0f, 0f, 0.6f)
                });
                AddComponent(entity, new WorkerCargoColorMaterialProperty
                {
                    Value = ResourceWorkerVisualStyle.GetCargoTint(authoring.Resource)
                });
            }
        }
    }
}
