using Unity.Burst;
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
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var statsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            foreach (var (arrow, arrowTransform, entity) in
                SystemAPI.Query<RefRO<ArrowProjectile>, RefRO<LocalTransform>>()
                    .WithAll<ArrowTag>()
                    .WithEntityAccess())
            {
                var target = arrow.ValueRO.Target;

                if (target == Entity.Null || !state.EntityManager.Exists(target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (!transformLookup.HasComponent(target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float dist = math.distance(arrowTransform.ValueRO.Position, transformLookup[target].Position);

                if (dist < 0.5f)
                {
                    if (statsLookup.HasComponent(target))
                    {
                        var zombieStats = statsLookup[target];
                        zombieStats.CurrentHP -= arrow.ValueRO.Damage;
                        statsLookup[target] = zombieStats;
                    }

                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
