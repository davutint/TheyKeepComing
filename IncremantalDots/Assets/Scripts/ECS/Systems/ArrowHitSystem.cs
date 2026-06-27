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

            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ArrowProjectile arrow, in LocalTransform arrowTransform)
            {
                var target = arrow.Target;

                if (target == Entity.Null || !TransformLookup.HasComponent(target))
                {
                    ECB.DestroyEntity(sortKey, entity);
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

                    ECB.DestroyEntity(sortKey, entity);
                }
            }
        }
    }
}
