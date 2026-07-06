using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WaveSpawnSystem))]
    [UpdateBefore(typeof(BuildSpatialHashSystem))]
    public partial struct ApplyMovementForceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZombieTag>();
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

            bool mobileMode = SystemAPI.HasSingleton<MobileCastleCombatConfig>();
            if (!mobileMode && !SystemAPI.HasSingleton<WallXPosition>())
                return;

            float wallX = mobileMode ? 0f : SystemAPI.GetSingleton<WallXPosition>().Value;
            var mobileConfig = mobileMode
                ? SystemAPI.GetSingleton<MobileCastleCombatConfig>()
                : default;

            new ApplyForceJob
            {
                MobileMode = mobileMode,
                SingleFront = mobileMode && mobileConfig.SingleFrontEnabled,
                FrontlineX = mobileConfig.FrontlineX,
                WallX = wallX,
                CastleCenter = mobileConfig.CastleCenter,
                SlowLookup = SystemAPI.GetComponentLookup<ZombieSlow>(true)
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ApplyForceJob : IJobEntity
        {
            public bool MobileMode;
            public bool SingleFront;
            public float FrontlineX;
            public float WallX;
            public float2 CastleCenter;
            [Unity.Collections.ReadOnly] public ComponentLookup<ZombieSlow> SlowLookup;

            void Execute(
                Entity entity,
                ref PhysicsBody physicsBody,
                in ZombieStats stats,
                in ZombieState zombieState,
                in LocalTransform transform)
            {
                // Dead veya Attacking → kuvvet sifir
                if (zombieState.Value != ZombieStateType.Moving)
                {
                    physicsBody.Force = float2.zero;
                    return;
                }

                // Tek cephe (K4): hedef duvarin x hatti (duz sola akis); 360 modda kale merkezi
                float2 desiredDir = MobileMode
                    ? (SingleFront
                        ? math.normalizesafe(new float2(FrontlineX - transform.Position.x, 0f))
                        : math.normalizesafe(CastleCenter - transform.Position.xy))
                    : math.normalizesafe(new float2(WallX - transform.Position.x, 0f));

                // Fallback: yon sifirsa sola git
                if (math.lengthsq(desiredDir) < 0.001f)
                    desiredDir = new float2(-1f, 0f);

                float speedMultiplier = 1f;
                if (SlowLookup.HasComponent(entity) && SlowLookup.IsComponentEnabled(entity))
                    speedMultiplier = math.clamp(SlowLookup[entity].SpeedMultiplier, 0.05f, 1f);

                float moveForce = stats.MoveSpeed * speedMultiplier * 3f;
                physicsBody.Force = desiredDir * moveForce;
            }
        }
    }
}
