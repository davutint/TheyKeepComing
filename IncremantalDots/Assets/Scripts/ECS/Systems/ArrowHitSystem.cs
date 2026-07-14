using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowMoveSystem))]
    public partial struct ArrowHitSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArrowPoolRuntimeData>();
            state.RequireForUpdate<ArrowPoolAvailable>();
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

            new ArrowHitJob
            {
                StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false),
                SlowLookup = SystemAPI.GetComponentLookup<ZombieSlow>(false),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(true),
                PoolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true),
                ArrowPoolMemberLookup = SystemAPI.GetComponentLookup<ArrowPoolMember>(true),
                ArrowPoolEntity = SystemAPI.GetSingletonEntity<ArrowPoolRuntimeData>(),
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
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

                    var vfxEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, vfxEvent, new CombatVfxEvent
                    {
                        Position = hitPosition,
                        Direction = new float3(hitDirection.x, hitDirection.y, 0f),
                        Type = frostHit ? CombatVfxType.FrostHit : CombatVfxType.ArrowHit,
                        Scale = frostHit ? 0.11f : 0.08f
                    });

                    var sfxEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, sfxEvent, new CombatSfxEvent
                    {
                        Position = hitPosition,
                        Type = frostHit ? CombatSfxType.FrostHit : CombatSfxType.ArrowHit,
                        Volume = frostHit ? 0.30f : 0.25f,
                        Pitch = 1f
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
    }
}
