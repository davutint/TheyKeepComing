using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Mancinik mermisi isabet sistemi — AoE hasar.
    /// FlightTimer >= FlightDuration olan mermiler icin spatial hash uzerinden
    /// yaricap icindeki tum zombilere hasar uygular.
    /// BurstCompile struct'ta YOK — static field erisimi (BuildSpatialHashSystem.ReadMap).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CatapultProjectileMoveSystem))]
    public partial struct CatapultProjectileHitSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var spatialMap = BuildSpatialHashSystem.ReadMap;
            bool hasSpatialMap = spatialMap.IsCreated && !spatialMap.IsEmpty;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var statsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            float cellSize = SpatialHash.DefaultCellSize;

            foreach (var (projectile, entity) in
                SystemAPI.Query<RefRO<CatapultProjectile>>()
                    .WithAll<CatapultProjectileTag>()
                    .WithEntityAccess())
            {
                // Ucus tamamlanmamissa atla
                if (projectile.ValueRO.FlightTimer < projectile.ValueRO.FlightDuration)
                    continue;

                float3 hitPos = projectile.ValueRO.TargetPos;
                float splashRadius = projectile.ValueRO.SplashRadius;
                float damage = projectile.ValueRO.Damage;
                float splashRadiusSq = splashRadius * splashRadius;

                // AoE hasar — spatial hash uzerinden yakin zombileri tara
                if (hasSpatialMap)
                {
                    int cellRange = (int)math.ceil(splashRadius / cellSize);
                    int2 centerCell = SpatialHash.GetCell(hitPos.xy, cellSize);

                    for (int dx = -cellRange; dx <= cellRange; dx++)
                    {
                        for (int dy = -cellRange; dy <= cellRange; dy++)
                        {
                            int key = SpatialHash.CellToKey(centerCell + new int2(dx, dy));

                            if (!spatialMap.TryGetFirstValue(key, out Entity zombie, out var it))
                                continue;

                            do
                            {
                                if (!statsLookup.HasComponent(zombie))
                                    continue;
                                if (!transformLookup.HasComponent(zombie))
                                    continue;

                                float3 zombiePos = transformLookup[zombie].Position;
                                float distSq = math.distancesq(hitPos, zombiePos);

                                if (distSq <= splashRadiusSq)
                                {
                                    var stats = statsLookup[zombie];
                                    stats.CurrentHP -= damage;
                                    statsLookup[zombie] = stats;
                                }
                            } while (spatialMap.TryGetNextValue(out zombie, ref it));
                        }
                    }
                }

                // Mermiyi sil
                ecb.DestroyEntity(entity);
            }
        }
    }
}
