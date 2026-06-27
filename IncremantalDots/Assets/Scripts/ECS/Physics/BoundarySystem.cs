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
    [UpdateBefore(typeof(ZombieAttackTimerSystem))]
    public partial struct BoundarySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                var gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            bool mobileMode = SystemAPI.HasSingleton<MobileCastleCombatConfig>();
            if (!mobileMode && !SystemAPI.HasSingleton<WallXPosition>())
                return;

            float wallX = mobileMode ? 0f : SystemAPI.GetSingleton<WallXPosition>().Value;
            var mobileConfig = mobileMode
                ? SystemAPI.GetSingleton<MobileCastleCombatConfig>()
                : default;

            float mobileLimit = mobileConfig.SpawnRadius + 2f;

            var spatialMap = BuildSpatialHashSystem.ReadMap;
            bool hasSpatialMap = spatialMap.IsCreated && !spatialMap.IsEmpty;

            new BoundaryJob
            {
                MobileMode = mobileMode,
                WallX = wallX,
                CastleCenter = mobileConfig.CastleCenter,
                AttackRadius = mobileConfig.AttackRadius,
                MinX = mobileMode ? mobileConfig.CastleCenter.x - mobileLimit : -100000f,
                MaxX = mobileMode ? mobileConfig.CastleCenter.x + mobileLimit : 40f,
                MinY = mobileMode ? mobileConfig.CastleCenter.y - mobileLimit : -15f,
                MaxY = mobileMode ? mobileConfig.CastleCenter.y + mobileLimit : 15f,
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
            public bool MobileMode;
            public float WallX;
            public float2 CastleCenter;
            public float AttackRadius;
            public float MinX;
            public float MaxX;
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

                            // Restart sonrasi yok edilmis entity kontrolu
                            if (!StateLookup.HasComponent(other))
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
                        if (MobileMode)
                        {
                            float attackRadiusSq = AttackRadius * AttackRadius;
                            if (math.distancesq(pos.xy, CastleCenter) <= attackRadiusSq)
                            {
                                zombieState.Value = ZombieStateType.Attacking;
                                body.Velocity = float2.zero;
                                break;
                            }
                        }
                        else if (pos.x <= WallX)
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
                        if (MobileMode)
                        {
                            float attackRadiusSq = AttackRadius * AttackRadius;
                            if (math.distancesq(pos.xy, CastleCenter) <= attackRadiusSq)
                            {
                                zombieState.Value = ZombieStateType.Attacking;
                                body.Velocity = float2.zero;
                                break;
                            }
                        }
                        else if (pos.x <= WallX)
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
                        if (MobileMode)
                        {
                            body.Velocity = float2.zero;
                            float2 fromCenter = pos.xy - CastleCenter;
                            float2 dir = math.normalizesafe(fromCenter, new float2(1f, 0f));
                            float2 clamped = CastleCenter + dir * AttackRadius;
                            pos.x = clamped.x;
                            pos.y = clamped.y;
                        }
                        // Duvar bariyeri
                        else if (pos.x < WallX)
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

                pos.x = math.clamp(pos.x, MinX, MaxX);
                pos.y = math.clamp(pos.y, MinY, MaxY);

                transform.Position = pos;
            }
        }
    }
}
