using Unity.Burst;
using Unity.Entities;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClickDamageSystem))]
    public partial struct ZombieDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (stats, zombieState) in
                SystemAPI.Query<RefRO<ZombieStats>, RefRW<ZombieState>>()
                    .WithAll<ZombieTag>())
            {
                if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                    continue;

                if (stats.ValueRO.CurrentHP <= 0f)
                {
                    zombieState.ValueRW.Value = ZombieStateType.Dead;
                }
            }
        }
    }
}
