using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace DeadWalls
{
    public enum WorkerLogisticsActivity : byte
    {
        Working = 0,
        Carrying = 1,
        Delivering = 2,
        Returning = 3
    }

    public enum WorkerAnimationKind : byte
    {
        Idle = 0,
        Walk = 1,
        Work = 2,
        Celebrate = 3
    }

    public struct WorkerPrefabData : IComponentData
    {
        public Entity WorkerPrefab;
    }

    public struct ResourceWorkerVisual : IComponentData
    {
        public EconomyFocusType Resource;
        public int Index;
        public int RepresentedWorkerCount;
    }

    public struct WorkerLogisticsRoute : IComponentData
    {
        public float3 PickupPosition;
        public float3 SiteApproachPosition;
        public float3 HubApproachPosition;
        public float3 DeliveryPosition;
        public float Speed;
        public float WorkDuration;
        public float DeliveryDuration;
        public float WaitTimer;
        public byte MovingToHub;
        public byte RouteLeg;
        public float2 LastDirection;
    }

    public struct WorkerLogisticsFeedbackState : IComponentData
    {
        public WorkerLogisticsActivity Activity;
        public byte IsCarrying;
        public byte LanternActive;
        public float DeliveryPulse01;
    }

    [MaterialProperty("_WorkerAnimation")]
    public struct WorkerAnimationMaterialProperty : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_WorkerFeedback")]
    public struct WorkerFeedbackMaterialProperty : IComponentData
    {
        // x = cargo, y = lantern, z = delivery pulse, w = represented production strength.
        public float4 Value;
    }

    [MaterialProperty("_WorkerCargoColor")]
    public struct WorkerCargoColorMaterialProperty : IComponentData
    {
        public float4 Value;
    }

    public static class ResourceWorkerVisualStyle
    {
        public static float4 GetTint(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Stone:
                    return new float4(0.82f, 0.84f, 0.86f, 1f);
                case EconomyFocusType.Iron:
                    return new float4(0.72f, 0.78f, 0.88f, 1f);
                case EconomyFocusType.Food:
                    return new float4(0.90f, 1.00f, 0.72f, 1f);
                default:
                    return new float4(1.00f, 0.88f, 0.68f, 1f);
            }
        }

        public static float4 GetCargoTint(EconomyFocusType resource)
        {
            switch (EconomyFocusUtility.Normalize(resource))
            {
                case EconomyFocusType.Stone:
                    return new float4(0.46f, 0.50f, 0.54f, 1f);
                case EconomyFocusType.Iron:
                    return new float4(0.28f, 0.40f, 0.62f, 1f);
                case EconomyFocusType.Food:
                    return new float4(0.38f, 0.62f, 0.20f, 1f);
                default:
                    return new float4(0.50f, 0.27f, 0.10f, 1f);
            }
        }
    }
}
