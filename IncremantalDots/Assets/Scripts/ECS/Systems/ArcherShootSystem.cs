using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAttackSystem))]
    public partial struct ArcherShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPrefabData>();
            state.RequireForUpdate<GameStateData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            float dt = SystemAPI.Time.DeltaTime;
            var arrowPrefab = SystemAPI.GetSingleton<ArrowPrefabData>().ArrowPrefab;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            var spatialMap = BuildSpatialHashSystem.SpatialMap;
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var stateLookup = SystemAPI.GetComponentLookup<ZombieState>(true);

            foreach (var (archer, archerTransform) in
                SystemAPI.Query<RefRW<ArcherUnit>, RefRO<LocalTransform>>())
            {
                archer.ValueRW.FireTimer -= dt;
                if (archer.ValueRO.FireTimer > 0f)
                    continue;

                float3 archerPos = archerTransform.ValueRO.Position;
                float range = archer.ValueRO.Range;

                Entity closestZombie = Entity.Null;
                float closestDist = float.MaxValue;

                if (spatialMap.IsCreated && !spatialMap.IsEmpty)
                {
                    float cellSize = SpatialHash.DefaultCellSize;
                    int searchRadius = (int)math.ceil(range / cellSize);
                    int2 centerCell = SpatialHash.GetCell(archerPos.xy, cellSize);

                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        for (int dy = -searchRadius; dy <= searchRadius; dy++)
                        {
                            int key = SpatialHash.CellToKey(centerCell + new int2(dx, dy));

                            if (!spatialMap.TryGetFirstValue(key, out Entity zombie, out var it))
                                continue;

                            do
                            {
                                if (!transformLookup.HasComponent(zombie) || !stateLookup.HasComponent(zombie))
                                    continue;

                                if (stateLookup[zombie].Value == ZombieStateType.Dead)
                                    continue;

                                float dist = math.distance(archerPos, transformLookup[zombie].Position);
                                if (dist < closestDist && dist <= range)
                                {
                                    closestDist = dist;
                                    closestZombie = zombie;
                                }
                            } while (spatialMap.TryGetNextValue(out zombie, ref it));
                        }
                    }
                }
                else
                {
                    // Fallback: spatial hash yoksa brute-force
                    foreach (var (zombieState, zombieTransform, zombieEntity) in
                        SystemAPI.Query<RefRO<ZombieState>, RefRO<LocalTransform>>()
                            .WithAll<ZombieTag>()
                            .WithEntityAccess())
                    {
                        if (zombieState.ValueRO.Value == ZombieStateType.Dead)
                            continue;

                        float dist = math.distance(archerPos, zombieTransform.ValueRO.Position);
                        if (dist < closestDist && dist <= range)
                        {
                            closestDist = dist;
                            closestZombie = zombieEntity;
                        }
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

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
