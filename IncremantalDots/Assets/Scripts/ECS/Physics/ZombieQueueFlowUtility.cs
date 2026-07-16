using Unity.Mathematics;

namespace DeadWalls
{
    public static class ZombieQueueFlowUtility
    {
        public const float ForwardProgressEpsilon = 0.01f;

        public static bool IsAheadOf(
            bool mobileMode,
            bool singleFront,
            float2 castleCenter,
            float2 followerPosition,
            float2 blockerPosition)
        {
            if (mobileMode && !singleFront)
            {
                float followerDistance = math.distance(followerPosition, castleCenter);
                float blockerDistance = math.distance(blockerPosition, castleCenter);
                return blockerDistance + ForwardProgressEpsilon < followerDistance;
            }

            return blockerPosition.x + ForwardProgressEpsilon < followerPosition.x;
        }
    }
}
