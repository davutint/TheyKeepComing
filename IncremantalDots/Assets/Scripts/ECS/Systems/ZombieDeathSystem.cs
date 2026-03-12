using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    public partial struct ZombieDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DeathCheckJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct DeathCheckJob : IJobEntity
        {
            void Execute(in ZombieStats stats, ref ZombieState zombieState)
            {
                if (zombieState.Value != ZombieStateType.Dead && stats.CurrentHP <= 0f)
                    zombieState.Value = ZombieStateType.Dead;
            }
        }
    }
}
