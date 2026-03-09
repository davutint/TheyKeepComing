using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BoundarySystem))]
    public partial struct ZombieAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallSegment>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            var wallEntity = SystemAPI.GetSingletonEntity<WallSegment>();
            var wall = SystemAPI.GetComponentRW<WallSegment>(wallEntity);
            var gate = SystemAPI.GetComponentRW<GateComponent>(wallEntity);
            var castle = SystemAPI.GetComponentRW<CastleHP>(wallEntity);

            foreach (var (stats, zombieState) in
                SystemAPI.Query<RefRW<ZombieStats>, RefRO<ZombieState>>()
                    .WithAll<ZombieTag>())
            {
                if (zombieState.ValueRO.Value != ZombieStateType.Attacking)
                    continue;

                stats.ValueRW.AttackTimer -= dt;
                if (stats.ValueRW.AttackTimer > 0f)
                    continue;

                stats.ValueRW.AttackTimer = stats.ValueRO.AttackCooldown;
                float damage = stats.ValueRO.AttackDamage;

                // Oncelik: Duvar → Kapi → Kale
                if (wall.ValueRO.CurrentHP > 0f)
                {
                    wall.ValueRW.CurrentHP = math.max(0f, wall.ValueRO.CurrentHP - damage);
                }
                else if (gate.ValueRO.CurrentHP > 0f)
                {
                    gate.ValueRW.CurrentHP = math.max(0f, gate.ValueRO.CurrentHP - damage);
                }
                else
                {
                    castle.ValueRW.CurrentHP = math.max(0f, castle.ValueRO.CurrentHP - damage);
                }
            }

            // Game Over kontrolu
            if (castle.ValueRO.CurrentHP <= 0f)
            {
                var gameState = SystemAPI.GetSingletonRW<GameStateData>();
                gameState.ValueRW.IsGameOver = true;
            }
        }
    }
}
