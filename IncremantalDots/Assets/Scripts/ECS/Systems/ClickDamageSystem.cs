using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    [UpdateBefore(typeof(ZombieDeathSystem))]
    public partial struct ClickDamageSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var spatialMap = BuildSpatialHashSystem.SpatialMap;
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var statsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false);
            var stateLookup = SystemAPI.GetComponentLookup<ZombieState>(true);

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<ClickDamageRequest>>()
                    .WithEntityAccess())
            {
                float3 clickPos = request.ValueRO.WorldPosition;
                float damage = request.ValueRO.Damage;
                float maxDist = 2f;

                Entity closestZombie = Entity.Null;
                float closestDist = maxDist;

                if (spatialMap.IsCreated && !spatialMap.IsEmpty)
                {
                    float cellSize = SpatialHash.DefaultCellSize;
                    int searchRadius = (int)math.ceil(maxDist / cellSize);
                    int2 centerCell = SpatialHash.GetCell(clickPos.xy, cellSize);

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

                                float dist = math.distance(clickPos.xy, transformLookup[zombie].Position.xy);
                                if (dist < closestDist)
                                {
                                    closestDist = dist;
                                    closestZombie = zombie;
                                }
                            } while (spatialMap.TryGetNextValue(out zombie, ref it));
                        }
                    }
                }

                if (closestZombie != Entity.Null && statsLookup.HasComponent(closestZombie))
                {
                    var stats = statsLookup[closestZombie];
                    stats.CurrentHP -= damage;
                    statsLookup[closestZombie] = stats;
                }

                ecb.DestroyEntity(entity);
            }
        }
    }
}
