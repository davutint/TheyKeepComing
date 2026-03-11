using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(IntegrateSystem))]
    [UpdateBefore(typeof(ZombieAttackSystem))]
    public partial struct BoundarySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallXPosition>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float wallX = SystemAPI.GetSingleton<WallXPosition>().Value;

            var spatialMap = BuildSpatialHashSystem.SpatialMap;
            bool hasSpatialMap = spatialMap.IsCreated && !spatialMap.IsEmpty;

            new BoundaryJob
            {
                WallX = wallX,
                MinY = -15f,
                MaxY = 15f,
                CellSize = SpatialHash.DefaultCellSize,
                HasSpatialMap = hasSpatialMap,
                SpatialMap = spatialMap,
                StateLookup = SystemAPI.GetComponentLookup<ZombieState>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                RadiusLookup = SystemAPI.GetComponentLookup<CollisionRadius>(true)
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct BoundaryJob : IJobEntity
        {
            public float WallX;
            public float MinY;
            public float MaxY;
            public float CellSize;
            public bool HasSpatialMap;

            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> SpatialMap;
            [ReadOnly] [NativeDisableContainerSafetyRestriction] public ComponentLookup<ZombieState> StateLookup;
            [ReadOnly] [NativeDisableContainerSafetyRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] [NativeDisableContainerSafetyRestriction] public ComponentLookup<CollisionRadius> RadiusLookup;

            bool HasStoppedNeighborOverlap(Entity self, float2 myPos, float myRadius)
            {
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

                            var otherState = StateLookup[other].Value;
                            if (otherState != ZombieStateType.Attacking && otherState != ZombieStateType.Queued)
                                continue;

                            float2 otherPos = TransformLookup[other].Position.xy;
                            float distSq = math.lengthsq(myPos - otherPos);
                            float otherRadius = RadiusLookup[other].Value;
                            float minDist = myRadius + otherRadius;

                            if (distSq < minDist * minDist && distSq > 0.00001f)
                                return true;

                        } while (SpatialMap.TryGetNextValue(out other, ref it));
                    }
                }

                return false;
            }

            void Execute(
                Entity self,
                ref LocalTransform transform,
                ref PhysicsBody body,
                ref ZombieState zombieState,
                in CollisionRadius radius)
            {
                float3 pos = transform.Position;

                switch (zombieState.Value)
                {
                    case ZombieStateType.Moving:
                        // 1. Duvara ulasti → Attacking
                        if (pos.x <= WallX)
                        {
                            zombieState.Value = ZombieStateType.Attacking;
                            body.Velocity = float2.zero;
                            break;
                        }

                        // 2. Domino kontrolu — komsuda Attacking/Queued zombi varsa ve cakisiyorsa Queued ol
                        if (HasSpatialMap && HasStoppedNeighborOverlap(self, pos.xy, radius.Value))
                        {
                            zombieState.Value = ZombieStateType.Queued;
                            body.Velocity = float2.zero;
                        }
                        break;

                    case ZombieStateType.Queued:
                        // 1. Duvara ulasti → Attacking
                        if (pos.x <= WallX)
                        {
                            zombieState.Value = ZombieStateType.Attacking;
                            body.Velocity = float2.zero;
                            break;
                        }

                        // 2. Onundeki blocker gitti mi? → Moving'e don
                        if (HasSpatialMap && !HasStoppedNeighborOverlap(self, pos.xy, radius.Value))
                        {
                            zombieState.Value = ZombieStateType.Moving;
                        }
                        break;

                    case ZombieStateType.Attacking:
                        // Duvar bariyeri
                        if (pos.x < WallX)
                        {
                            pos.x = WallX;
                            body.Velocity.x = math.max(body.Velocity.x, 0f);
                        }
                        break;

                    case ZombieStateType.Dead:
                        body.Velocity = float2.zero;
                        body.Force = float2.zero;
                        break;
                }

                // Y siniri
                pos.y = math.clamp(pos.y, MinY, MaxY);

                // Sag sinir (spawn alanindan cikmasin)
                pos.x = math.min(pos.x, 40f);

                transform.Position = pos;
            }
        }
    }
}
