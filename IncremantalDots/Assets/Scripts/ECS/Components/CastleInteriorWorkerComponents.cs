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

    /// <summary>
    /// Dawn'da kabul edilen nüfusun geçici world-space temsilidir. Gameplay population truth'u değildir.
    /// </summary>
    public struct SurvivorArrivalVisual : IComponentData
    {
        public float3 TargetPosition;
        public float Speed;
        public float StartDelay;
        public float ArrivalDistance;
        public int RepresentedSurvivorCount;
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

    public static class SurvivorArrivalVisualUtility
    {
        public const int MaxVisualCount = 15;
        public const float SpawnDistanceFromWall = 9.5f;
        public const float TargetDistanceBehindWall = 0.8f;
        public const float LaneSpacing = 0.55f;
        public const float BaseMoveSpeed = 3f;
        public const float DefaultArrivalDistance = 0.08f;

        public static int GetVisualCount(int acceptedSurvivors)
        {
            return math.clamp(acceptedSurvivors, 0, MaxVisualCount);
        }

        public static int GetRepresentedSurvivorCount(int acceptedSurvivors, int visualCount, int index)
        {
            if (acceptedSurvivors <= 0 || visualCount <= 0 || index < 0 || index >= visualCount)
                return 0;

            int baseCount = acceptedSurvivors / visualCount;
            int remainder = acceptedSurvivors % visualCount;
            return baseCount + (index < remainder ? 1 : 0);
        }

        public static float3 GetSpawnPosition(float frontlineX, float castleCenterY, int index)
        {
            int lane = index % 5;
            int row = index / 5;
            float laneOffset = (lane - 2) * LaneSpacing;
            float rowOffset = (row - 1) * 0.16f;
            float xOffset = (index % 3) * 0.42f;
            return new float3(
                frontlineX + SpawnDistanceFromWall + xOffset,
                castleCenterY + laneOffset + rowOffset,
                MobileCastleRenderDepth.UnitZ);
        }

        public static float3 GetTargetPosition(float frontlineX, float castleCenterY, int index)
        {
            int lane = index % 5;
            float laneOffset = (lane - 2) * LaneSpacing * 0.22f;
            return new float3(
                frontlineX - TargetDistanceBehindWall,
                castleCenterY + laneOffset,
                MobileCastleRenderDepth.UnitZ);
        }

        public static float GetMoveSpeed(int index)
        {
            return BaseMoveSpeed + (math.max(0, index) % 3) * 0.12f;
        }

        public static float GetStartDelay(int index)
        {
            index = math.max(0, index);
            return (index % 5) * 0.08f + (index / 5) * 0.16f;
        }

        public static float4 GetTint()
        {
            return new float4(0.82f, 0.95f, 1f, 1f);
        }
    }
}
