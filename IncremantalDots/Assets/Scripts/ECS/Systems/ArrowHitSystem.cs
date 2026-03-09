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

            foreach (var (arrow, transform, entity) in
                SystemAPI.Query<RefRO<ArrowProjectile>, RefRO<LocalTransform>>()
                    .WithAll<ArrowTag>()
                    .WithEntityAccess())
            {
                if (arrow.ValueRO.Target == Entity.Null ||
                    !state.EntityManager.Exists(arrow.ValueRO.Target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(arrow.ValueRO.Target);
                float dist = math.distance(transform.ValueRO.Position, targetTransform.Position);

                if (dist < 0.5f)
                {
                    // Hasar uygula
                    var zombieStats = state.EntityManager.GetComponentData<ZombieStats>(arrow.ValueRO.Target);
                    zombieStats.CurrentHP -= arrow.ValueRO.Damage;
                    state.EntityManager.SetComponentData(arrow.ValueRO.Target, zombieStats);

                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
