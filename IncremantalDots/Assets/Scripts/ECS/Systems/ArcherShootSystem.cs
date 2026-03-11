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
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (archer, archerTransform) in
                SystemAPI.Query<RefRW<ArcherUnit>, RefRO<LocalTransform>>())
            {
                archer.ValueRW.FireTimer -= dt;
                if (archer.ValueRO.FireTimer > 0f)
                    continue;

                float3 archerPos = archerTransform.ValueRO.Position;
                float rangeSq = archer.ValueRO.Range * archer.ValueRO.Range;

                Entity closestZombie = Entity.Null;
                float closestDistSq = float.MaxValue;

                // Brute-force — ~10 okcu x 6000 zombi = 60K distance check, Burst ile trivial
                // Spatial hash 3721 hucre taramasi bundan DAHA YAVAS
                foreach (var (zombieState, zombieTransform, zombieEntity) in
                    SystemAPI.Query<RefRO<ZombieState>, RefRO<LocalTransform>>()
                        .WithAll<ZombieTag>()
                        .WithNone<DeathTimer>()
                        .WithEntityAccess())
                {
                    if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                        continue;

                    float distSq = math.distancesq(archerPos, zombieTransform.ValueRO.Position);
                    if (distSq < closestDistSq && distSq <= rangeSq)
                    {
                        closestDistSq = distSq;
                        closestZombie = zombieEntity;
                    }
                }

                if (closestZombie == Entity.Null)
                    continue;

                archer.ValueRW.FireTimer = 1f / archer.ValueRO.FireRate;

                var arrow = ecb.Instantiate(arrowPrefab);
                ecb.SetComponent(arrow, LocalTransform.FromPosition(archerPos));
                ecb.SetComponent(arrow, new ArrowProjectile
                {
                    Speed = 12f,
                    Damage = archer.ValueRO.ArrowDamage,
                    Target = closestZombie
                });
            }
        }
    }
}
