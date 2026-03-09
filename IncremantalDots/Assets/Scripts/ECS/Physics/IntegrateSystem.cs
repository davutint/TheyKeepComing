using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsCollisionSystem))]
    [UpdateBefore(typeof(BoundarySystem))]
    public partial struct IntegrateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            new IntegrateJob
            {
                DeltaTime = dt
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct IntegrateJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref LocalTransform transform, ref PhysicsBody body)
            {
                // Semi-implicit Euler: once velocity guncelle, sonra pozisyon
                body.Velocity += body.Force / body.Mass * DeltaTime;

                var pos = transform.Position;
                pos.x += body.Velocity.x * DeltaTime;
                pos.y += body.Velocity.y * DeltaTime;
                transform.Position = pos;

                // Damping (surturme)
                body.Velocity *= math.saturate(1f - body.Damping * DeltaTime);

                // Force sifirla
                body.Force = float2.zero;
            }
        }
    }
}
