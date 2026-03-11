using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAttackTimerSystem))]
    public partial struct ArrowMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ArrowMoveJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ArrowTag))]
        partial struct ArrowMoveJob : IJobEntity
        {
            public float Dt;

            [ReadOnly] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> TransformLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ArrowProjectile arrow, ref LocalTransform transform)
            {
                // Hedef hala var mi?
                if (arrow.Target == Entity.Null || !TransformLookup.HasComponent(arrow.Target))
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                float3 targetPos = TransformLookup[arrow.Target].Position;
                float3 direction = math.normalize(targetPos - transform.Position);
                transform.Position += direction * arrow.Speed * Dt;

                // Yon hesaplasindan rotation
                if (math.lengthsq(direction) > 0.001f)
                {
                    float angle = math.atan2(direction.y, direction.x);
                    transform.Rotation = quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }
}
