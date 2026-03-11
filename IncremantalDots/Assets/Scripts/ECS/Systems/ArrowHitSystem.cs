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
    [UpdateAfter(typeof(ArrowMoveSystem))]
    public partial struct ArrowHitSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ArrowHitJob
            {
                StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ArrowTag))]
        partial struct ArrowHitJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<ZombieStats> StatsLookup;

            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ArrowProjectile arrow, in LocalTransform arrowTransform)
            {
                var target = arrow.Target;

                if (target == Entity.Null || !TransformLookup.HasComponent(target))
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                float dist = math.distance(arrowTransform.Position, TransformLookup[target].Position);

                if (dist < 0.5f)
                {
                    if (StatsLookup.HasComponent(target))
                    {
                        var zombieStats = StatsLookup[target];
                        zombieStats.CurrentHP -= arrow.Damage;
                        StatsLookup[target] = zombieStats;
                    }

                    ECB.DestroyEntity(sortKey, entity);
                }
            }
        }
    }
}
