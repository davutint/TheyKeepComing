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
    [UpdateAfter(typeof(ZombieAttackTimerSystem))]
    public partial struct ArrowMoveSystem : ISystem
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

            new ArrowMoveJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(true),
                PoolMemberLookup = SystemAPI.GetComponentLookup<EnemyPoolMember>(true),
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ArrowTag))]
        partial struct ArrowMoveJob : IJobEntity
        {
            public float Dt;

            [ReadOnly] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly] public ComponentLookup<ZombieTag> ZombieTagLookup;
            [ReadOnly] public ComponentLookup<EnemyPoolMember> PoolMemberLookup;

            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int sortKey,
                in ArrowProjectile arrow, ref LocalTransform transform)
            {
                // Hedef hala var mi?
                if (!IsValidTarget(arrow))
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }

                float3 targetPos = TransformLookup[arrow.Target].Position;
                targetPos.z = transform.Position.z;

                float3 toTarget = targetPos - transform.Position;
                if (math.lengthsq(toTarget) <= 0.0001f)
                    return;

                float3 direction = math.normalize(toTarget);
                transform.Position += direction * arrow.Speed * Dt;

                // Yon hesaplasindan rotation
                if (math.lengthsq(direction) > 0.001f)
                {
                    float angle = math.atan2(direction.y, direction.x);
                    transform.Rotation = quaternion.Euler(0f, 0f, angle);
                }
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
