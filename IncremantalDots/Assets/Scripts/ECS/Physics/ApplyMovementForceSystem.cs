using ProjectDawn.Navigation;
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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ApplyForceJob().ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyForceJob : IJobEntity
        {
            void Execute(
                ref PhysicsBody physicsBody,
                in ZombieStats stats,
                in ZombieState zombieState,
                in AgentBody agentBody,
                in LocalTransform transform)
            {
                // Dead veya Attacking → kuvvet sifir
                if (zombieState.Value != ZombieStateType.Moving)
                {
                    physicsBody.Force = float2.zero;
                    return;
                }

                // Hedefe dogru yon hesapla
                float2 toTarget = agentBody.Destination.xy - transform.Position.xy;
                float2 desiredDir = math.normalizesafe(toTarget);

                // Fallback: yon sifirsa sola git
                if (math.lengthsq(desiredDir) < 0.001f)
                    desiredDir = new float2(-1f, 0f);

                // ProjectDawn Force varsa onu tercih et (flow field akilli yon)
                float2 pdForce = math.normalizesafe(agentBody.Force.xy);
                if (math.lengthsq(pdForce) > 0.001f)
                    desiredDir = pdForce;

                float moveForce = stats.MoveSpeed * 3f;
                physicsBody.Force = desiredDir * moveForce;
            }
        }
    }
}
