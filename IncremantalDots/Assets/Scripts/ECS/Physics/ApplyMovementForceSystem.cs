using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(BuildSpatialHashSystem))]
    public partial struct ApplyMovementForceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
            state.RequireForUpdate<WallXPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float wallX = SystemAPI.GetSingleton<WallXPosition>().Value;
            new ApplyForceJob { WallX = wallX }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyForceJob : IJobEntity
        {
            public float WallX;

            void Execute(
                ref PhysicsBody physicsBody,
                in ZombieStats stats,
                in ZombieState zombieState,
                in LocalTransform transform)
            {
                // Dead veya Attacking → kuvvet sifir
                if (zombieState.Value != ZombieStateType.Moving)
                {
                    physicsBody.Force = float2.zero;
                    return;
                }

                // Duvara dogru yatay yon (Y=0 cunku hedef hep zombinin Y'si)
                float2 desiredDir = math.normalizesafe(new float2(WallX - transform.Position.x, 0f));

                // Fallback: yon sifirsa sola git
                if (math.lengthsq(desiredDir) < 0.001f)
                    desiredDir = new float2(-1f, 0f);

                float moveForce = stats.MoveSpeed * 3f;
                physicsBody.Force = desiredDir * moveForce;
            }
        }
    }
}
