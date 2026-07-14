using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SurvivorArrivalVisualSystem : ISystem
    {
        private const int FrameCount = 15;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SurvivorArrivalVisual>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SurvivorArrivalMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged)
                    .AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct SurvivorArrivalMoveJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                ref LocalTransform transform,
                ref SurvivorArrivalVisual arrival,
                ref SpriteAnimation animation,
                ref WorkerAnimationMaterialProperty animationProperty,
                ref WorkerFeedbackMaterialProperty feedbackProperty)
            {
                if (arrival.StartDelay > 0f)
                {
                    arrival.StartDelay = math.max(0f, arrival.StartDelay - DeltaTime);
                    SetAnimation(ref animation, ref animationProperty,
                        animation.DirectionRow % 8, WorkerAnimationKind.Idle);
                    feedbackProperty.Value = float4.zero;
                    return;
                }

                float2 delta = arrival.TargetPosition.xy - transform.Position.xy;
                float distanceSq = math.lengthsq(delta);
                float arrivalDistance = math.max(0.01f, arrival.ArrivalDistance);
                if (distanceSq <= arrivalDistance * arrivalDistance)
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                float distance = math.sqrt(distanceSq);
                float2 direction = delta / math.max(0.0001f, distance);
                float step = math.min(distance, math.max(0f, arrival.Speed) * DeltaTime);
                transform.Position += new float3(direction * step, 0f);
                transform.Position.z = MobileCastleRenderDepth.UnitZ;

                SetAnimation(ref animation, ref animationProperty,
                    ResolveDirection(direction, animation.DirectionRow % 8),
                    WorkerAnimationKind.Walk);
                feedbackProperty.Value = float4.zero;
            }

            private static void SetAnimation(ref SpriteAnimation animation,
                ref WorkerAnimationMaterialProperty animationProperty,
                int directionRow, WorkerAnimationKind kind)
            {
                float targetAnimation = (float)kind;
                if (animation.DirectionRow == directionRow
                    && animation.FrameCount == FrameCount
                    && animationProperty.Value == targetAnimation)
                {
                    return;
                }

                animation.DirectionRow = directionRow;
                animation.FrameCount = FrameCount;
                animation.CurrentFrame = 0;
                animation.FrameTimer = 0f;
                animationProperty.Value = targetAnimation;
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
