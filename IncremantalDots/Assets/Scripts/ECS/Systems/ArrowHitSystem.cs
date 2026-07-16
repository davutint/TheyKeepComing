using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowMoveSystem))]
    public partial struct ArrowHitSystem : ISystem
    {
        private NativeParallelHashMap<int3, CombatHitFeedbackCandidate> _hitCandidates;
        private Entity _telemetryEntity;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPoolRuntimeData>();
            state.RequireForUpdate<ArrowPoolAvailable>();

            _hitCandidates = new NativeParallelHashMap<int3, CombatHitFeedbackCandidate>(
                CombatHitFeedbackBudget.CandidateCapacity,
                Allocator.Persistent);
            _telemetryEntity = state.EntityManager.CreateEntity(
                typeof(CombatFeedbackBudgetTelemetryData));
        }

        public void OnDestroy(ref SystemState state)
        {
            state.Dependency.Complete();
            if (_hitCandidates.IsCreated)
                _hitCandidates.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                var gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            JobHandle clearHandle = new ClearHitCandidatesJob
            {
                Candidates = _hitCandidates
            }.Schedule(state.Dependency);

            EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton =
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            JobHandle hitHandle = new ArrowHitJob
            {
                StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false),
                SlowLookup = SystemAPI.GetComponentLookup<ZombieSlow>(false),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(true),
                PoolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true),
                ArrowPoolMemberLookup = SystemAPI.GetComponentLookup<ArrowPoolMember>(true),
                ArrowPoolEntity = SystemAPI.GetSingletonEntity<ArrowPoolRuntimeData>(),
                HitCandidates = _hitCandidates.AsParallelWriter(),
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(clearHandle);

            CombatFeedbackBudgetTelemetryData previousTelemetry =
                state.EntityManager.GetComponentData<CombatFeedbackBudgetTelemetryData>(
                    _telemetryEntity);
            state.Dependency = new EmitHitFeedbackJob
            {
                Candidates = _hitCandidates,
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
                TelemetryEntity = _telemetryEntity,
                PreviousTelemetry = previousTelemetry
            }.Schedule(hitHandle);
        }

        [BurstCompile]
        private struct ClearHitCandidatesJob : IJob
        {
            public NativeParallelHashMap<int3, CombatHitFeedbackCandidate> Candidates;

            public void Execute()
            {
                Candidates.Clear();
            }
        }

        [BurstCompile]
        [WithAll(typeof(ArrowTag))]
        partial struct ArrowHitJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<ZombieStats> StatsLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<ZombieSlow> SlowLookup;

            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<ZombieTag> ZombieTagLookup;
            [ReadOnly] public ComponentLookup<EnemyPoolMember> PoolMemberLookup;
            [ReadOnly] public ComponentLookup<ArrowPoolMember> ArrowPoolMemberLookup;

            public Entity ArrowPoolEntity;

            public NativeParallelHashMap<int3, CombatHitFeedbackCandidate>.ParallelWriter HitCandidates;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ArrowProjectile arrow, in LocalTransform arrowTransform)
            {
                var target = arrow.Target;

                if (arrow.RemainingLifetime <= 0f || !IsValidTarget(arrow))
                {
                    ReturnToPool(entity, sortKey);
                    return;
                }

                float3 targetPosition = TransformLookup[target].Position;
                float dist = math.distance(arrowTransform.Position.xy, targetPosition.xy);

                if (dist < 0.5f)
                {
                    if (StatsLookup.HasComponent(target))
                    {
                        var zombieStats = StatsLookup[target];
                        zombieStats.CurrentHP -= arrow.Damage;
                        StatsLookup[target] = zombieStats;
                    }

                    if (arrow.ArcherType == ArcherType.Frost &&
                        arrow.SlowDuration > 0f &&
                        arrow.SlowMultiplier > 0f &&
                        SlowLookup.HasComponent(target))
                    {
                        SlowLookup[target] = new ZombieSlow
                        {
                            Duration = arrow.SlowDuration,
                            SpeedMultiplier = math.clamp(arrow.SlowMultiplier, 0.05f, 1f)
                        };
                        SlowLookup.SetComponentEnabled(target, true);
                    }

                    bool frostHit = arrow.ArcherType == ArcherType.Frost;
                    float2 hitDirection = math.normalizesafe(
                        targetPosition.xy - arrowTransform.Position.xy,
                        new float2(1f, 0f));
                    float3 hitPosition = new float3(targetPosition.x, targetPosition.y, MobileCastleRenderDepth.ProjectileZ);

                    CombatVfxType vfxType = frostHit
                        ? CombatVfxType.FrostHit
                        : CombatVfxType.ArrowHit;
                    HitCandidates.TryAdd(
                        CombatHitFeedbackBudget.GetSpatialKey(hitPosition, vfxType),
                        new CombatHitFeedbackCandidate
                    {
                        Position = hitPosition,
                        Direction = new float3(hitDirection.x, hitDirection.y, 0f),
                        Type = vfxType,
                        Scale = frostHit ? 0.11f : 0.08f
                    });

                    ReturnToPool(entity, sortKey);
                }
            }

            private void ReturnToPool(Entity entity, int sortKey)
            {
                if (ArrowPoolEntity == Entity.Null || !ArrowPoolMemberLookup.HasComponent(entity))
                {
                    // Legacy/non-pool projectile compatibility fallback'i.
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                ECB.SetComponent(sortKey, entity, new ArrowProjectile
                {
                    Target = Entity.Null,
                    SlowMultiplier = 1f,
                    RemainingLifetime = 0f
                });
                ECB.SetComponent(sortKey, entity,
                    LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 0f));
                ECB.SetComponent(sortKey, entity,
                    new SpriteTint { Value = ArcherVisualStyle.NormalTint });
                ECB.SetComponentEnabled<ArrowTag>(sortKey, entity, false);
                ECB.AppendToBuffer(sortKey, ArrowPoolEntity,
                    new ArrowPoolAvailable { Entity = entity });
            }

            private bool IsValidTarget(ArrowProjectile arrow)
            {
                if (arrow.Target == Entity.Null
                    || !TransformLookup.HasComponent(arrow.Target)
                    || !ZombieTagLookup.HasComponent(arrow.Target)
                    || !ZombieTagLookup.IsComponentEnabled(arrow.Target))
                    return false;

                return !PoolMemberLookup.HasComponent(arrow.Target)
                    || PoolMemberLookup[arrow.Target].Generation == arrow.TargetPoolGeneration;
            }
        }

        [BurstCompile]
        private struct EmitHitFeedbackJob : IJob
        {
            [ReadOnly] public NativeParallelHashMap<int3, CombatHitFeedbackCandidate> Candidates;
            public EntityCommandBuffer ECB;
            public Entity TelemetryEntity;
            public CombatFeedbackBudgetTelemetryData PreviousTelemetry;

            public void Execute()
            {
                int arrowCandidateCount = 0;
                int frostCandidateCount = 0;
                float3 arrowPositionSum = float3.zero;
                float3 frostPositionSum = float3.zero;

                var countEnumerator = Candidates.GetEnumerator();
                while (countEnumerator.MoveNext())
                {
                    CombatHitFeedbackCandidate candidate = countEnumerator.Current.Value;
                    if (candidate.Type == CombatVfxType.FrostHit)
                    {
                        frostCandidateCount++;
                        frostPositionSum += candidate.Position;
                    }
                    else
                    {
                        arrowCandidateCount++;
                        arrowPositionSum += candidate.Position;
                    }
                }

                CombatHitFeedbackBudget.ResolveVfxBudgets(
                    arrowCandidateCount,
                    frostCandidateCount,
                    out int arrowBudget,
                    out int frostBudget);

                int frostVfxEmitted = EmitVfxType(CombatVfxType.FrostHit, frostBudget);
                int arrowVfxEmitted = EmitVfxType(CombatVfxType.ArrowHit, arrowBudget);
                int sfxEventsEmitted = 0;

                if (frostCandidateCount > 0)
                {
                    EmitSfx(
                        CombatSfxType.FrostHit,
                        frostPositionSum / frostCandidateCount,
                        0.30f,
                        frostCandidateCount);
                    sfxEventsEmitted++;
                }

                if (arrowCandidateCount > 0)
                {
                    EmitSfx(
                        CombatSfxType.ArrowHit,
                        arrowPositionSum / arrowCandidateCount,
                        0.25f,
                        arrowCandidateCount);
                    sfxEventsEmitted++;
                }

                int spatialCandidateCount = arrowCandidateCount + frostCandidateCount;
                int vfxEventsEmitted = arrowVfxEmitted + frostVfxEmitted;
                int vfxCandidatesDropped = math.max(
                    0,
                    spatialCandidateCount - vfxEventsEmitted);
                ECB.SetComponent(TelemetryEntity, new CombatFeedbackBudgetTelemetryData
                {
                    LastSpatialCandidateCount = spatialCandidateCount,
                    LastVfxEventsEmitted = vfxEventsEmitted,
                    LastSfxEventsEmitted = sfxEventsEmitted,
                    LastVfxCandidatesDropped = vfxCandidatesDropped,
                    TotalSpatialCandidateCount = PreviousTelemetry.TotalSpatialCandidateCount
                        + spatialCandidateCount,
                    TotalVfxEventsEmitted = PreviousTelemetry.TotalVfxEventsEmitted
                        + vfxEventsEmitted,
                    TotalSfxEventsEmitted = PreviousTelemetry.TotalSfxEventsEmitted
                        + sfxEventsEmitted,
                    TotalVfxCandidatesDropped = PreviousTelemetry.TotalVfxCandidatesDropped
                        + vfxCandidatesDropped
                });
            }

            private int EmitVfxType(CombatVfxType type, int budget)
            {
                if (budget <= 0)
                    return 0;

                int emitted = 0;
                var enumerator = Candidates.GetEnumerator();
                while (enumerator.MoveNext() && emitted < budget)
                {
                    CombatHitFeedbackCandidate candidate = enumerator.Current.Value;
                    if (candidate.Type != type)
                        continue;

                    Entity eventEntity = ECB.CreateEntity();
                    ECB.AddComponent(eventEntity, new CombatVfxEvent
                    {
                        Position = candidate.Position,
                        Direction = candidate.Direction,
                        Type = candidate.Type,
                        Scale = candidate.Scale
                    });
                    emitted++;
                }

                return emitted;
            }

            private void EmitSfx(
                CombatSfxType type,
                float3 position,
                float volume,
                int multiplicity)
            {
                Entity eventEntity = ECB.CreateEntity();
                ECB.AddComponent(eventEntity, new CombatSfxEvent
                {
                    Position = position,
                    Type = type,
                    Volume = volume,
                    Pitch = 1f,
                    Multiplicity = multiplicity
                });
            }
        }
    }
}
