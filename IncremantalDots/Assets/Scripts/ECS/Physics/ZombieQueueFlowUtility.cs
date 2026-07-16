using Unity.Mathematics;

namespace DeadWalls
{
    public static class ZombieQueueFlowUtility
    {
        public const float ForwardProgressEpsilon = 0.01f;
        public const float ForwardLaneRatio = 1f;

        public static bool ReceivesForwardPressure(ZombieStateType state)
        {
            return state == ZombieStateType.Moving || state == ZombieStateType.Queued;
        }

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

        /// <summary>
        /// Bir komsunun kuyrugu durdurabilmesi icin yalniz hedefe daha yakin olmasi yetmez;
        /// follower'in gercek ileri hareket koridorunda da bulunmasi gerekir. Fireball gibi
        /// dairesel temizlemelerde tegette kalan capraz Queued komsular boylece sahte bir
        /// duvar kurmaz; dogrudan arka arkaya duran gercek kuyruk korunur.
        /// </summary>
        public static bool CanBlockQueue(
            bool mobileMode,
            bool singleFront,
            float2 castleCenter,
            float2 followerPosition,
            float2 blockerPosition)
        {
            if (!IsAheadOf(
                    mobileMode,
                    singleFront,
                    castleCenter,
                    followerPosition,
                    blockerPosition))
            {
                return false;
            }

            float2 forward = mobileMode && !singleFront
                ? math.normalizesafe(castleCenter - followerPosition, new float2(-1f, 0f))
                : new float2(-1f, 0f);
            float2 toBlocker = blockerPosition - followerPosition;
            float forwardDistance = math.dot(toBlocker, forward);
            float lateralDistance = math.abs(forward.x * toBlocker.y - forward.y * toBlocker.x);

            return forwardDistance > ForwardProgressEpsilon
                   && lateralDistance <= forwardDistance * ForwardLaneRatio;
        }
    }
}
