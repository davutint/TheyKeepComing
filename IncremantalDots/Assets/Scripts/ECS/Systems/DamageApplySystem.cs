using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// TEK SYNC POINT: Tum physics + attack job'lari burada tamamlanir.
    /// ZombieAttackTimerSystem'in DamageQueue'sunu drain eder,
    /// hasari Wall -> Gate -> Castle sirasina gore uygular.
    /// </summary>
    // [BurstCompile] struct'tan kaldirildi — static field erisimi (ZombieAttackTimerSystem.DamageQueue)
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAnimationStateSystem))]
    [UpdateBefore(typeof(DamageCleanupSystem))]
    public partial struct DamageApplySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallSegment>();
            state.RequireForUpdate<GameStateData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // TEK SYNC POINT — tum physics + attack job'lari burada tamamlanir
            state.CompleteDependency();

            var damageQueue = ZombieAttackTimerSystem.DamageQueue;
            if (!damageQueue.IsCreated || damageQueue.Count == 0)
                return;

            var wallEntity = SystemAPI.GetSingletonEntity<WallSegment>();
            var wall = SystemAPI.GetComponentRW<WallSegment>(wallEntity);
            var gate = SystemAPI.GetComponentRW<GateComponent>(wallEntity);
            var castle = SystemAPI.GetComponentRW<CastleHP>(wallEntity);

            while (damageQueue.TryDequeue(out float damage))
            {
                // Oncelik: Duvar -> Kapi -> Kale
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
