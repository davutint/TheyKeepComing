using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    public struct WorkerPrefabData : IComponentData
    {
        public Entity WorkerPrefab;
    }

    public struct ResourceWorkerVisual : IComponentData
    {
        public EconomyFocusType Resource;
        public int Index;
    }

    public struct WorkerLogisticsRoute : IComponentData
    {
        public float3 PickupPosition;
        public float3 DeliveryPosition;
        public float Speed;
        public float WorkDuration;
        public float DeliveryDuration;
        public float WaitTimer;
        public byte MovingToHub;
        public float2 LastDirection;
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
    }
}
