using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildSpatialHashSystem))]
    [UpdateBefore(typeof(IntegrateSystem))]
    public partial struct PhysicsCollisionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spatialMap = BuildSpatialHashSystem.ReadMap;

            new CollisionJob
            {
                CellSize = SpatialHash.DefaultCellSize,
                SpatialMap = spatialMap,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                RadiusLookup = SystemAPI.GetComponentLookup<CollisionRadius>(true)
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct CollisionJob : IJobEntity
        {
            public float CellSize;

            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
            [ReadOnly] [NativeDisableContainerSafetyRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<CollisionRadius> RadiusLookup;

            void Execute(
                Entity self,
                ref LocalTransform transform,
                ref PhysicsBody body,
                in CollisionRadius radius)
            {
                float2 myPos = transform.Position.xy;
                float myRadius = radius.Value;
                float2 totalPosCorrection = float2.zero;
                int collisionCount = 0;

                int2 myCell = SpatialHash.GetCell(myPos, CellSize);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int key = SpatialHash.CellToKey(myCell + new int2(dx, dy));

                        if (!SpatialMap.TryGetFirstValue(key, out Entity other, out var it))
                            continue;

                        do
                        {
                            if (other == self)
                                continue;

                            // Restart sonrasi yok edilmis entity kontrolu
                            if (!TransformLookup.HasComponent(other))
                                continue;

                            float2 otherPos = TransformLookup[other].Position.xy;
                            float2 delta = myPos - otherPos;
                            float distSq = math.lengthsq(delta);
                            float otherRadius = RadiusLookup[other].Value;
                            float minDist = myRadius + otherRadius;
                            float minDistSq = minDist * minDist;

                            if (distSq >= minDistSq || distSq < 0.00001f)
                                continue;

                            float dist = math.sqrt(distSq);
                            float overlap = minDist - dist;
                            float2 normal = delta / dist;

                            // Yumusak pozisyon duzeltme (0.3 = yavas iteratif cozum)
                            totalPosCorrection += normal * overlap * 0.3f;
                            collisionCount++;

                            // Velocity impulse — overlap'i hizlandirarak cozer, gelecek frame'lerde daha az overlap
                            body.Velocity += new float2(normal.x, normal.y) * overlap * 2.0f;

                        } while (SpatialMap.TryGetNextValue(out other, ref it));
                    }
                }

                // Cok fazla carpisma varsa duzeltmeyi olcekle (yigin icinde esit dagitim)
                if (collisionCount > 6)
                    totalPosCorrection *= 6f / collisionCount;

                // Toplam duzeltmeyi sinirla — patlama onleme
                float maxCorrection = myRadius * 2f;
                float corrLen = math.length(totalPosCorrection);
                if (corrLen > maxCorrection)
                    totalPosCorrection = totalPosCorrection / corrLen * maxCorrection;

                var pos = transform.Position;
                pos.x += totalPosCorrection.x;
                pos.y += totalPosCorrection.y;
                transform.Position = pos;
            }
        }
    }
}
