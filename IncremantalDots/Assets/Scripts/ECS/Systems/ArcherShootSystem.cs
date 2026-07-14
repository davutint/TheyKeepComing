using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct ArcherShootSystem : ISystem
    {
        private const int InitialReservationCapacity = 16384;

        private NativeParallelMultiHashMap<int, Entity> _targetMap;
        private NativeParallelHashMap<Entity, float> _incomingDamage;
        private EntityQuery _targetQuery;
        private int _targetMapCapacity;
        private int _reservationCapacity;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPrefabData>();
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<ArrowSupply>();

            _targetMap = new NativeParallelMultiHashMap<int, Entity>(
                InitialReservationCapacity, Allocator.Persistent);
            _incomingDamage = new NativeParallelHashMap<Entity, float>(
                InitialReservationCapacity, Allocator.Persistent);
            _targetMapCapacity = InitialReservationCapacity;
            _reservationCapacity = InitialReservationCapacity;
            _targetQuery = SystemAPI.QueryBuilder()
                .WithAll<ZombieTag, ZombieStats, LocalTransform>()
                .WithNone<DeathTimer>()
                .Build();
        }

        public void OnDestroy(ref SystemState state)
        {
            state.Dependency.Complete();
            if (_targetMap.IsCreated)
                _targetMap.Dispose();
            if (_incomingDamage.IsCreated)
                _incomingDamage.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            GameStateData gameState = SystemAPI.GetSingleton<GameStateData>();
            if (gameState.IsGameOver || gameState.IsLevelUpPending)
                return;

            EnsureContainerCapacity(ref state, _targetQuery.CalculateEntityCount());

            bool unlimitedArrows = SystemAPI.HasSingleton<MobileCastleCombatConfig>()
                && SystemAPI.GetSingleton<MobileCastleCombatConfig>().UnlimitedArrows;
            float fireRateMultiplier = 1f;
            if (SystemAPI.HasSingleton<CastleYardPrepState>())
            {
                CastleYardPrepState prep = SystemAPI.GetSingleton<CastleYardPrepState>();
                if (prep.RallyTimer > 0f)
                    fireRateMultiplier = math.max(0.01f, prep.RallyFireRateMultiplier);
            }

            JobHandle clearTargetHandle = new ClearTargetMapJob
            {
                TargetMap = _targetMap
            }.Schedule(state.Dependency);

            JobHandle buildTargetHandle = new BuildTargetMapJob
            {
                CellSize = SpatialHash.TargetCellSize,
                TargetMap = _targetMap.AsParallelWriter()
            }.ScheduleParallel(_targetQuery, clearTargetHandle);

            JobHandle clearHandle = new ClearIncomingDamageJob
            {
                Reservations = _incomingDamage
            }.Schedule(buildTargetHandle);

            JobHandle seedHandle = new SeedIncomingDamageJob
            {
                Reservations = _incomingDamage,
                StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(true),
                StateLookup = SystemAPI.GetComponentLookup<ZombieState>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(true),
                DeathTimerLookup = SystemAPI.GetComponentLookup<DeathTimer>(true),
                PoolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true)
            }.Schedule(clearHandle);

            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ArcherShootJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                FireRateMultiplier = fireRateMultiplier,
                UnlimitedArrows = unlimitedArrows,
                TargetCellSize = SpatialHash.TargetCellSize,
                ArrowPrefab = SystemAPI.GetSingleton<ArrowPrefabData>().ArrowPrefab,
                ArrowSupplyEntity = SystemAPI.GetSingletonEntity<ArrowSupply>(),
                TargetMap = _targetMap.AsReadOnly(),
                Reservations = _incomingDamage,
                StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(true),
                StateLookup = SystemAPI.GetComponentLookup<ZombieState>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(true),
                DeathTimerLookup = SystemAPI.GetComponentLookup<DeathTimer>(true),
                PoolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true),
                ArrowSupplyLookup = SystemAPI.GetComponentLookup<ArrowSupply>(false),
                ECB = ecb
            }.Schedule(seedHandle);
        }

        private void EnsureContainerCapacity(ref SystemState state, int targetCount)
        {
            if (targetCount <= _reservationCapacity && targetCount <= _targetMapCapacity)
                return;

            state.Dependency.Complete();
            int capacity = (int)math.ceilpow2(math.max(targetCount, 1024));
            if (capacity > _targetMapCapacity)
            {
                _targetMap.Capacity = capacity;
                _targetMapCapacity = capacity;
            }
            if (capacity > _reservationCapacity)
            {
                _incomingDamage.Capacity = capacity;
                _reservationCapacity = capacity;
            }
        }

        [BurstCompile]
        private struct ClearTargetMapJob : IJob
        {
            public NativeParallelMultiHashMap<int, Entity> TargetMap;

            public void Execute()
            {
                TargetMap.Clear();
            }
        }

        [BurstCompile]
        private partial struct BuildTargetMapJob : IJobEntity
        {
            public float CellSize;
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter TargetMap;

            private void Execute(Entity entity, in LocalTransform transform)
            {
                TargetMap.Add(SpatialHash.Hash(transform.Position.xy, CellSize), entity);
            }
        }

        [BurstCompile]
        private struct ClearIncomingDamageJob : IJob
        {
            public NativeParallelHashMap<Entity, float> Reservations;

            public void Execute()
            {
                Reservations.Clear();
            }
        }

        [BurstCompile]
        [WithAll(typeof(ArrowTag))]
        private partial struct SeedIncomingDamageJob : IJobEntity
        {
            public NativeParallelHashMap<Entity, float> Reservations;

            [ReadOnly] public ComponentLookup<ZombieStats> StatsLookup;
            [ReadOnly] public ComponentLookup<ZombieState> StateLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ZombieTag> ZombieTagLookup;
            [ReadOnly] public ComponentLookup<DeathTimer> DeathTimerLookup;
            [ReadOnly] public ComponentLookup<EnemyPoolMember> PoolMemberLookup;

            private void Execute(in ArrowProjectile arrow)
            {
                if (!IsValidTarget(arrow.Target, arrow.TargetPoolGeneration))
                    return;

                AddReservation(arrow.Target, math.max(0f, arrow.Damage));
            }

            private bool IsValidTarget(Entity target, uint generation)
            {
                if (target == Entity.Null
                    || !TransformLookup.HasComponent(target)
                    || !StatsLookup.HasComponent(target)
                    || !StateLookup.HasComponent(target)
                    || StateLookup[target].Value == ZombieStateType.Dead
                    || StatsLookup[target].CurrentHP <= 0f
                    || !ZombieTagLookup.HasComponent(target)
                    || !ZombieTagLookup.IsComponentEnabled(target)
                    || (DeathTimerLookup.HasComponent(target)
                        && DeathTimerLookup.IsComponentEnabled(target)))
                    return false;

                return !PoolMemberLookup.HasComponent(target)
                    || PoolMemberLookup[target].Generation == generation;
            }

            private void AddReservation(Entity target, float damage)
            {
                if (Reservations.TryGetValue(target, out float reserved))
                    Reservations[target] = reserved + damage;
                else
                    Reservations.TryAdd(target, damage);
            }
        }

        [BurstCompile]
        private partial struct ArcherShootJob : IJobEntity
        {
            public float Dt;
            public float FireRateMultiplier;
            public bool UnlimitedArrows;
            public float TargetCellSize;
            public Entity ArrowPrefab;
            public Entity ArrowSupplyEntity;

            [ReadOnly] public NativeParallelMultiHashMap<int, Entity>.ReadOnly TargetMap;
            public NativeParallelHashMap<Entity, float> Reservations;

            [ReadOnly] public ComponentLookup<ZombieStats> StatsLookup;
            [ReadOnly] public ComponentLookup<ZombieState> StateLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ZombieTag> ZombieTagLookup;
            [ReadOnly] public ComponentLookup<DeathTimer> DeathTimerLookup;
            [ReadOnly] public ComponentLookup<EnemyPoolMember> PoolMemberLookup;
            public ComponentLookup<ArrowSupply> ArrowSupplyLookup;

            public EntityCommandBuffer ECB;

            private void Execute(ref ArcherUnit archer, in LocalTransform archerTransform)
            {
                archer.FireTimer -= Dt;
                if (archer.FireTimer > 0f || !TargetMap.IsCreated || TargetMap.IsEmpty)
                    return;

                float3 archerPosition = archerTransform.Position;
                if (!TryFindNearestAvailableTarget(
                        archerPosition.xy,
                        archer.Range,
                        out Entity target,
                        out float3 targetPosition))
                    return;

                if (!UnlimitedArrows)
                {
                    ArrowSupply supply = ArrowSupplyLookup[ArrowSupplyEntity];
                    if (supply.Current <= 0)
                        return;

                    supply.Current--;
                    ArrowSupplyLookup[ArrowSupplyEntity] = supply;
                }

                AddReservation(target, math.max(0f, archer.ArrowDamage));

                float effectiveFireRate = math.max(0.01f, archer.FireRate * FireRateMultiplier);
                archer.FireTimer = 1f / effectiveFireRate;
                archer.FacingDirection = ResolveFacingDirection(
                    targetPosition.xy - archerPosition.xy,
                    archer.FacingDirection);
                archer.AttackAnimTimer = GetAttackAnimDuration(effectiveFireRate);

                Entity arrow = ECB.Instantiate(ArrowPrefab);
                float3 arrowPosition = new float3(
                    archerPosition.x,
                    archerPosition.y,
                    MobileCastleRenderDepth.ProjectileZ);
                ECB.SetComponent(arrow, LocalTransform.FromPosition(arrowPosition));
                ECB.SetComponent(arrow, new ArrowProjectile
                {
                    Speed = 12f,
                    Damage = archer.ArrowDamage,
                    Target = target,
                    TargetPoolGeneration = PoolMemberLookup.HasComponent(target)
                        ? PoolMemberLookup[target].Generation
                        : 0u,
                    ArcherType = archer.Type,
                    SlowDuration = archer.SlowDuration,
                    SlowMultiplier = archer.SlowMultiplier
                });
                ECB.SetComponent(arrow, new SpriteTint
                {
                    Value = ArcherVisualStyle.GetTint(archer.Type)
                });

                Entity sfxEvent = ECB.CreateEntity();
                ECB.AddComponent(sfxEvent, new CombatSfxEvent
                {
                    Position = arrowPosition,
                    Type = CombatSfxType.ArrowShoot,
                    Volume = 0.35f,
                    Pitch = 1f
                });
            }

            private bool TryFindNearestAvailableTarget(
                float2 archerPosition,
                float range,
                out Entity bestTarget,
                out float3 bestPosition)
            {
                bestTarget = Entity.Null;
                bestPosition = float3.zero;

                float safeRange = math.max(0f, range);
                float rangeSq = safeRange * safeRange;
                float bestDistanceSq = float.MaxValue;
                int2 centerCell = SpatialHash.GetCell(archerPosition, TargetCellSize);
                int cellRadius = ArcherTargetingUtility.GetCellRadius(safeRange, TargetCellSize);

                for (int ring = 0; ring <= cellRadius; ring++)
                {
                    if (ring == 0)
                    {
                        EvaluateCell(
                            centerCell,
                            archerPosition,
                            rangeSq,
                            ref bestTarget,
                            ref bestPosition,
                            ref bestDistanceSq);
                        continue;
                    }

                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        EvaluateCell(
                            centerCell + new int2(dx, -ring),
                            archerPosition,
                            rangeSq,
                            ref bestTarget,
                            ref bestPosition,
                            ref bestDistanceSq);
                        EvaluateCell(
                            centerCell + new int2(dx, ring),
                            archerPosition,
                            rangeSq,
                            ref bestTarget,
                            ref bestPosition,
                            ref bestDistanceSq);
                    }

                    for (int dy = -ring + 1; dy <= ring - 1; dy++)
                    {
                        EvaluateCell(
                            centerCell + new int2(-ring, dy),
                            archerPosition,
                            rangeSq,
                            ref bestTarget,
                            ref bestPosition,
                            ref bestDistanceSq);
                        EvaluateCell(
                            centerCell + new int2(ring, dy),
                            archerPosition,
                            rangeSq,
                            ref bestTarget,
                            ref bestPosition,
                            ref bestDistanceSq);
                    }
                }

                return bestTarget != Entity.Null;
            }

            private void EvaluateCell(
                int2 cell,
                float2 archerPosition,
                float rangeSq,
                ref Entity bestTarget,
                ref float3 bestPosition,
                ref float bestDistanceSq)
            {
                float minimumCellDistanceSq = SpatialHash.DistanceSqToCell(
                    archerPosition, cell, TargetCellSize);
                if (minimumCellDistanceSq > rangeSq || minimumCellDistanceSq > bestDistanceSq)
                    return;

                int key = SpatialHash.CellToKey(cell);
                if (!TargetMap.TryGetFirstValue(key, out Entity candidate, out var iterator))
                    return;

                do
                {
                    EvaluateCandidate(
                        candidate,
                        archerPosition,
                        rangeSq,
                        ref bestTarget,
                        ref bestPosition,
                        ref bestDistanceSq);
                }
                while (TargetMap.TryGetNextValue(out candidate, ref iterator));
            }

            private void EvaluateCandidate(
                Entity candidate,
                float2 archerPosition,
                float rangeSq,
                ref Entity bestTarget,
                ref float3 bestPosition,
                ref float bestDistanceSq)
            {
                if (!IsValidTarget(candidate))
                    return;

                ZombieStats stats = StatsLookup[candidate];
                Reservations.TryGetValue(candidate, out float reservedDamage);
                if (!ArcherTargetingUtility.HasUnreservedHealth(stats.CurrentHP, reservedDamage))
                    return;

                float3 candidatePosition = TransformLookup[candidate].Position;
                float distanceSq = math.distancesq(archerPosition, candidatePosition.xy);
                if (distanceSq > rangeSq
                    || !ArcherTargetingUtility.IsBetterCandidate(
                        distanceSq, candidate, bestDistanceSq, bestTarget))
                    return;

                bestTarget = candidate;
                bestPosition = candidatePosition;
                bestDistanceSq = distanceSq;
            }

            private bool IsValidTarget(Entity target)
            {
                return target != Entity.Null
                    && TransformLookup.HasComponent(target)
                    && StatsLookup.HasComponent(target)
                    && StatsLookup[target].CurrentHP > 0f
                    && StateLookup.HasComponent(target)
                    && StateLookup[target].Value != ZombieStateType.Dead
                    && ZombieTagLookup.HasComponent(target)
                    && ZombieTagLookup.IsComponentEnabled(target)
                    && (!DeathTimerLookup.HasComponent(target)
                        || !DeathTimerLookup.IsComponentEnabled(target));
            }

            private void AddReservation(Entity target, float damage)
            {
                if (Reservations.TryGetValue(target, out float reserved))
                    Reservations[target] = reserved + damage;
                else
                    Reservations.TryAdd(target, damage);
            }

            private static float2 ResolveFacingDirection(float2 aimDirection, float2 fallback)
            {
                if (math.lengthsq(aimDirection) > 0.0001f)
                    return math.normalize(aimDirection);

                if (math.lengthsq(fallback) > 0.0001f)
                    return math.normalize(fallback);

                return new float2(1f, 0f);
            }

            private static float GetAttackAnimDuration(float fireRate)
            {
                float shotInterval = 1f / math.max(0.01f, fireRate);
                return math.clamp(shotInterval * 0.75f, 0.18f, 0.45f);
            }
        }
    }
}
