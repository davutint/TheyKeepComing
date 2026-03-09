using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Stop-offset bazli durma sistemi.
    ///
    /// Moving state:
    ///   Crowd steering aktif — flow field ile duvara yonlendirilir.
    ///   pos.x <= wallX + stopOffset olunca → Attacking'e gecer.
    ///
    /// Attacking state:
    ///   Tamamen durdurulmus (IsStopped=true).
    ///   Duvar bariyeri: zombi duvardan iceri giremez.
    ///
    /// Dead state:
    ///   Hareketi durdur.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ZombieAttackSystem))]
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

            foreach (var (body, zombieState, transform, stopOffset) in
                SystemAPI.Query<RefRW<AgentBody>, RefRW<ZombieState>, RefRW<LocalTransform>, RefRO<ZombieStopOffset>>()
                    .WithAll<ZombieTag>()
                    .WithNone<DeathTimer>())
            {
                switch (zombieState.ValueRO.Value)
                {
                    case ZombieStateType.Moving:
                        if (transform.ValueRO.Position.x <= wallX + stopOffset.ValueRO.Value)
                        {
                            zombieState.ValueRW.Value = ZombieStateType.Attacking;
                            body.ValueRW.IsStopped = true;
                            body.ValueRW.Velocity = float3.zero;
                        }
                        else if (body.ValueRO.IsStopped)
                        {
                            body.ValueRW.IsStopped = false;
                        }
                        break;

                    case ZombieStateType.Attacking:
                        if (!body.ValueRO.IsStopped)
                        {
                            body.ValueRW.IsStopped = true;
                            body.ValueRW.Velocity = float3.zero;
                        }

                        // Duvar bariyeri: zombi duvardan iceri giremez
                        if (transform.ValueRO.Position.x < wallX)
                        {
                            var pos = transform.ValueRO.Position;
                            pos.x = wallX;
                            transform.ValueRW.Position = pos;
                        }
                        break;

                    case ZombieStateType.Dead:
                        body.ValueRW.IsStopped = true;
                        body.ValueRW.Velocity = float3.zero;
                        break;
                }
            }
        }
    }
}
