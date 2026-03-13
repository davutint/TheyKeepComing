using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Mancinik atis sistemi — ArcherShootSystem pattern'i.
    /// Her CatapultUnit icin: timer dus → Stone kontrol → en yakin zombie bul → mermi spawn.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct CatapultShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CatapultProjectilePrefabData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<ResourceData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var projectilePrefab = SystemAPI.GetSingleton<CatapultProjectilePrefabData>().CatapultProjectilePrefab;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var resourceRW = SystemAPI.GetSingletonRW<ResourceData>();

            foreach (var (catapult, catapultTransform) in
                SystemAPI.Query<RefRW<CatapultUnit>, RefRO<LocalTransform>>())
            {
                catapult.ValueRW.FireTimer -= dt;
                if (catapult.ValueRO.FireTimer > 0f)
                    continue;

                float3 catapultPos = catapultTransform.ValueRO.Position;
                float rangeSq = catapult.ValueRO.Range * catapult.ValueRO.Range;

                Entity closestZombie = Entity.Null;
                float closestDistSq = float.MaxValue;
                float3 closestPos = float3.zero;

                // Brute-force en yakin zombie — ArcherShootSystem ile ayni pattern
                foreach (var (zombieState, zombieTransform, zombieEntity) in
                    SystemAPI.Query<RefRO<ZombieState>, RefRO<LocalTransform>>()
                        .WithAll<ZombieTag>()
                        .WithNone<DeathTimer>()
                        .WithEntityAccess())
                {
                    if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                        continue;

                    float distSq = math.distancesq(catapultPos, zombieTransform.ValueRO.Position);
                    if (distSq < closestDistSq && distSq <= rangeSq)
                    {
                        closestDistSq = distSq;
                        closestZombie = zombieEntity;
                        closestPos = zombieTransform.ValueRO.Position;
                    }
                }

                if (closestZombie == Entity.Null)
                    continue;

                // Tas kontrolu — tas yoksa ates etme
                if (resourceRW.ValueRO.Stone < catapult.ValueRO.StoneCostPerShot)
                    break;

                resourceRW.ValueRW.Stone -= catapult.ValueRO.StoneCostPerShot;
                catapult.ValueRW.FireTimer = 1f / catapult.ValueRO.FireRate;

                // Mermi spawn — parabolik ucus icin baslangic + hedef pozisyon set
                var projectile = ecb.Instantiate(projectilePrefab);
                ecb.SetComponent(projectile, LocalTransform.FromPosition(catapultPos));
                ecb.SetComponent(projectile, new CatapultProjectile
                {
                    Damage = catapult.ValueRO.Damage,
                    SplashRadius = catapult.ValueRO.SplashRadius,
                    StartPos = catapultPos,
                    TargetPos = closestPos,
                    FlightDuration = 1.2f,
                    FlightTimer = 0f,
                    ArcHeight = 5f
                });
            }
        }
    }
}
