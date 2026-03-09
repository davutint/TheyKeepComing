using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Artik sadece AgentBody senkronizasyonu yapar.
    /// State transition ve duvar bariyeri BoundarySystem'e taşındı.
    /// ProjectDawn locomotion devre disi (IsStopped=true) oldugu icin
    /// bu sistem sadece AgentBody.Destination'i guncel tutar.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct ZombieNavigationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallXPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var wallX = SystemAPI.GetSingleton<WallXPosition>().Value;

            foreach (var (body, zombieState, transform) in
                SystemAPI.Query<RefRW<AgentBody>, RefRO<ZombieState>, RefRO<LocalTransform>>()
                    .WithAll<ZombieTag>()
                    .WithNone<DeathTimer>())
            {
                // IsStopped her zaman true — PD locomotion devre disi
                if (!body.ValueRO.IsStopped)
                    body.ValueRW.IsStopped = true;

                // Destination'i guncel tut (CrowdSteering Force hesabi icin)
                body.ValueRW.Destination = new float3(wallX, transform.ValueRO.Position.y, -1f);
            }
        }
    }
}
