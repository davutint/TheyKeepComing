using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAttackSystem))]
    public partial struct ArcherShootSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPrefabData>();
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var arrowPrefab = SystemAPI.GetSingleton<ArrowPrefabData>().ArrowPrefab;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (archer, archerTransform) in
                SystemAPI.Query<RefRW<ArcherUnit>, RefRO<LocalTransform>>())
            {
                archer.ValueRW.FireTimer -= dt;
                if (archer.ValueRO.FireTimer > 0f)
                    continue;

                // En yakin zombiyi bul
                Entity closestZombie = Entity.Null;
                float closestDist = float.MaxValue;
                float3 archerPos = archerTransform.ValueRO.Position;

                foreach (var (zombieState, zombieTransform, zombieEntity) in
                    SystemAPI.Query<RefRO<ZombieState>, RefRO<LocalTransform>>()
                        .WithAll<ZombieTag>()
                        .WithEntityAccess())
                {
                    if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                        continue;

                    float dist = math.distance(archerPos, zombieTransform.ValueRO.Position);
                    if (dist < closestDist && dist <= archer.ValueRO.Range)
                    {
                        closestDist = dist;
                        closestZombie = zombieEntity;
                    }
                }

                if (closestZombie == Entity.Null)
                    continue;

                archer.ValueRW.FireTimer = 1f / archer.ValueRO.FireRate;

                // Ok spawn
                var arrow = ecb.Instantiate(arrowPrefab);
                ecb.SetComponent(arrow, LocalTransform.FromPosition(archerPos));
                ecb.SetComponent(arrow, new ArrowProjectile
                {
                    Speed = 12f,
                    Damage = archer.ValueRO.ArrowDamage,
                    Target = closestZombie
                });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
