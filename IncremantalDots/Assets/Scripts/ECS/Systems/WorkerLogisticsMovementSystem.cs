using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WorkerLogisticsMovementSystem : ISystem
    {
        private const int WalkOffset = 0;
        private const int IdleOffset = 24;
        private const int FrameCount = 15;
        private const float ArrivalDistance = 0.035f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            new WorkerLogisticsMoveJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct WorkerLogisticsMoveJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref LocalTransform transform, ref WorkerLogisticsRoute route, ref SpriteAnimation anim)
            {
                if (route.WaitTimer > 0f)
                {
                    route.WaitTimer = math.max(0f, route.WaitTimer - DeltaTime);
                    SetAnimation(ref anim, IdleOffset + ResolveDirection(route.LastDirection, anim.DirectionRow % 8));
                    return;
                }

                float3 target = route.MovingToHub != 0 ? route.DeliveryPosition : route.PickupPosition;
                float3 delta = target - transform.Position;
                float2 delta2 = delta.xy;
                float distanceSq = math.lengthsq(delta2);

                if (distanceSq <= ArrivalDistance * ArrivalDistance)
                {
                    bool arrivedAtHub = route.MovingToHub != 0;
                    route.MovingToHub = arrivedAtHub ? (byte)0 : (byte)1;
                    route.WaitTimer = arrivedAtHub ? route.DeliveryDuration : route.WorkDuration;
                    SetAnimation(ref anim, IdleOffset + ResolveDirection(route.LastDirection, anim.DirectionRow % 8));
                    return;
                }

                float distance = math.sqrt(distanceSq);
                float2 direction = delta2 / math.max(0.0001f, distance);
                float step = math.min(distance, math.max(0f, route.Speed) * DeltaTime);
                transform.Position += new float3(direction * step, target.z - transform.Position.z);
                route.LastDirection = direction;
                SetAnimation(ref anim, WalkOffset + ResolveDirection(direction, anim.DirectionRow % 8));
            }

            private static void SetAnimation(ref SpriteAnimation anim, int targetRow)
            {
                if (anim.DirectionRow == targetRow && anim.FrameCount == FrameCount)
                    return;

                anim.DirectionRow = targetRow;
                anim.FrameCount = FrameCount;
                anim.CurrentFrame = 0;
                anim.FrameTimer = 0f;
            }

            private static int ResolveDirection(float2 direction, int fallbackDirection)
            {
                if (math.lengthsq(direction) < 0.0001f)
                    return fallbackDirection;

                float angle = math.atan2(direction.y, direction.x);
                int index = (int)math.round((-angle) / (math.PI * 0.25f));
                index %= 8;
                if (index < 0)
                    index += 8;

                return index;
            }
        }
    }
}
