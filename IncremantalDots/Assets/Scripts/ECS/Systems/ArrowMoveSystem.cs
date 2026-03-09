using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArcherShootSystem))]
    public partial struct ArrowMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (arrow, transform, entity) in
                SystemAPI.Query<RefRO<ArrowProjectile>, RefRW<LocalTransform>>()
                    .WithAll<ArrowTag>()
                    .WithEntityAccess())
            {
                // Hedef hala var mi?
                if (arrow.ValueRO.Target == Entity.Null ||
                    !state.EntityManager.Exists(arrow.ValueRO.Target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(arrow.ValueRO.Target);
                float3 direction = math.normalize(targetTransform.Position - transform.ValueRO.Position);
                transform.ValueRW.Position += direction * arrow.ValueRO.Speed * dt;

                // Yon hesaplasindan rotation
                if (math.lengthsq(direction) > 0.001f)
                {
                    float angle = math.atan2(direction.y, direction.x);
                    transform.ValueRW.Rotation = quaternion.Euler(0f, 0f, angle);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
