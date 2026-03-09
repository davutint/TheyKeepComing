using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(IntegrateSystem))]
    [UpdateBefore(typeof(ZombieAttackSystem))]
    public partial struct BoundarySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallXPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float wallX = SystemAPI.GetSingleton<WallXPosition>().Value;

            new BoundaryJob
            {
                WallX = wallX,
                MinY = -15f,
                MaxY = 15f
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct BoundaryJob : IJobEntity
        {
            public float WallX;
            public float MinY;
            public float MaxY;

            void Execute(
                ref LocalTransform transform,
                ref PhysicsBody body,
                ref ZombieState zombieState,
                in ZombieStopOffset stopOffset)
            {
                float3 pos = transform.Position;

                switch (zombieState.Value)
                {
                    case ZombieStateType.Moving:
                        // Duvara yeterince yaklasti → Attacking
                        if (pos.x <= WallX + stopOffset.Value)
                        {
                            zombieState.Value = ZombieStateType.Attacking;
                            body.Velocity = float2.zero;
                        }
                        break;

                    case ZombieStateType.Attacking:
                        // Duvar bariyeri
                        if (pos.x < WallX)
                        {
                            pos.x = WallX;
                            body.Velocity.x = math.max(body.Velocity.x, 0f);
                        }
                        break;

                    case ZombieStateType.Dead:
                        body.Velocity = float2.zero;
                        body.Force = float2.zero;
                        break;
                }

                // Y siniri
                pos.y = math.clamp(pos.y, MinY, MaxY);

                // Sag sinir (spawn alanindan cikmasin)
                pos.x = math.min(pos.x, 40f);

                transform.Position = pos;
            }
        }
    }
}
