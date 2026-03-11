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
            float wallX = SystemAPI.GetSingleton<WallXPosition>().Value;
            new NavSyncJob { WallX = wallX }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        [WithNone(typeof(DeathTimer))]
        partial struct NavSyncJob : IJobEntity
        {
            public float WallX;

            void Execute(ref AgentBody body, in LocalTransform transform)
            {
                // IsStopped her zaman true — PD locomotion devre disi
                if (!body.IsStopped)
                    body.IsStopped = true;

                // Destination'i guncel tut (CrowdSteering Force hesabi icin)
                body.Destination = new float3(WallX, transform.Position.y, -1f);
            }
        }
    }
}
