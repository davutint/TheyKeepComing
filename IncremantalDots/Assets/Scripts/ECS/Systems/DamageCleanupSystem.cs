using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Olen zombilerin olum animasyonunu tamamlar ve pool rezervine dondurur.
    ///
    /// Eski akis: Dead → aninda sil
    /// Yeni akis: Dead aninda Soul/kill odulu ZombieDeathSystem'de verilir;
    /// Dead + DeathTimer → timer say → 0'a dusunce XP/resource cleanup + pool return yapilir.
    ///
    /// DeathTimer, ZombieAnimationStateSystem tarafindan eklenir.
    /// Bu system sadece DeathTimer olan entity'leri isle.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageApplySystem))]
    public partial struct DamageCleanupSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<GameStateData>())
                return;

            var gameState = SystemAPI.GetSingletonRW<GameStateData>();
            if (gameState.ValueRO.IsGameOver || gameState.ValueRO.IsLevelUpPending)
                return;

            bool mobileMode = SystemAPI.HasSingleton<MobileCastleCombatConfig>();
            var waveState = SystemAPI.GetSingletonRW<WaveStateData>();
            bool canApplyMobileReward = mobileMode
                && !waveState.ValueRO.StressTestMode
                && SystemAPI.HasSingleton<ResourceAccumulator>();
            var mobileConfig = canApplyMobileReward
                ? SystemAPI.GetSingleton<MobileCastleCombatConfig>()
                : default;
            EconomyFocusType economyFocus = canApplyMobileReward && SystemAPI.HasSingleton<EconomyFocusState>()
                && !SystemAPI.HasSingleton<MobilePopulationAllocation>()
                ? SystemAPI.GetSingleton<EconomyFocusState>().Type
                : EconomyFocusType.Balanced;
            float mobileRewardMultiplier = canApplyMobileReward && SystemAPI.HasSingleton<MobilePopulationAllocation>()
                ? math.clamp(mobileConfig.WorkerEconomyRewardMultiplier, 0f, 1f)
                : 1f;
            var resourceAccumulator = canApplyMobileReward
                ? SystemAPI.GetSingletonRW<ResourceAccumulator>()
                : default;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            Entity poolEntity = SystemAPI.HasSingleton<EnemyPoolRuntimeData>()
                ? SystemAPI.GetSingletonEntity<EnemyPoolRuntimeData>()
                : Entity.Null;
            var returnedEntities = new NativeList<Entity>(Allocator.Temp);
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (stats, deathTimer, entity) in
                SystemAPI.Query<RefRO<ZombieStats>, RefRW<DeathTimer>>()
                    .WithAll<ZombieTag>()
                    .WithEntityAccess())
            {
                // Timer'i geri say
                deathTimer.ValueRW.Value -= dt;

                // Timer bitmedi → bekle
                if (deathTimer.ValueRO.Value > 0f)
                    continue;

                // Timer bitti → cleanup odulleri + pool return. TotalKills/Soul olum aninda
                // ZombieDeathSystem tarafindan tam bir kez yazilmistir.
                gameState.ValueRW.XP += stats.ValueRO.XPReward;
                waveState.ValueRW.ZombiesAlive--;
                if (canApplyMobileReward)
                    AddKillReward(ref resourceAccumulator.ValueRW, mobileConfig, economyFocus,
                        waveState.ValueRO.CurrentWave, mobileRewardMultiplier);

                returnedEntities.Add(entity);
            }

            var pooledEntities = new NativeList<Entity>(returnedEntities.Length, Allocator.TempJob);
            for (int i = 0; i < returnedEntities.Length; i++)
            {
                Entity entity = returnedEntities[i];
                if (poolEntity != Entity.Null
                    && state.EntityManager.HasComponent<EnemyPoolMember>(entity))
                    pooledEntities.Add(entity);
                else
                    ecb.DestroyEntity(entity);
            }
            returnedEntities.Dispose();

            if (pooledEntities.Length > 0)
            {
                var resetJob = new PoolReturnResetJob
                {
                    Entities = pooledEntities.AsArray(),
                    StatsLookup = SystemAPI.GetComponentLookup<ZombieStats>(false),
                    StateLookup = SystemAPI.GetComponentLookup<ZombieState>(false),
                    SlowLookup = SystemAPI.GetComponentLookup<ZombieSlow>(false),
                    DeathTimerLookup = SystemAPI.GetComponentLookup<DeathTimer>(false),
                    PhysicsLookup = SystemAPI.GetComponentLookup<PhysicsBody>(false),
                    TintLookup = SystemAPI.GetComponentLookup<SpriteTint>(false),
                    AnimationLookup = SystemAPI.GetComponentLookup<SpriteAnimation>(false),
                    TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                    ZombieTagLookup = SystemAPI.GetComponentLookup<ZombieTag>(false)
                };
                state.Dependency = resetJob.Schedule(pooledEntities.Length, 128, state.Dependency);
                state.Dependency.Complete();
                EnemyPoolRuntimeUtility.CommitBulkReturn(
                    state.EntityManager, poolEntity, pooledEntities.AsArray());
            }
            pooledEntities.Dispose();

            // Mobile castle loop artik level-up ile pause olmaz; XP sadece progress metric olarak kalir.
            if (!mobileMode && gameState.ValueRO.XP >= gameState.ValueRO.XPToNextLevel && !gameState.ValueRO.IsLevelUpPending)
            {
                gameState.ValueRW.IsLevelUpPending = true;
            }
        }

        private static void AddKillReward(ref ResourceAccumulator accumulator,
            MobileCastleCombatConfig config, EconomyFocusType focus, int currentWave, float rewardMultiplier)
        {
            int completedWaveSteps = currentWave > 1 ? currentWave - 1 : 0;
            float scale = (1f + completedWaveSteps * config.KillRewardWaveScale) * rewardMultiplier;
            accumulator.Wood += config.KillRewardWood * scale
                * EconomyFocusUtility.GetKillRewardMultiplier(config, focus, EconomyFocusType.Wood);
            accumulator.Stone += config.KillRewardStone * scale
                * EconomyFocusUtility.GetKillRewardMultiplier(config, focus, EconomyFocusType.Stone);
            accumulator.Iron += config.KillRewardIron * scale
                * EconomyFocusUtility.GetKillRewardMultiplier(config, focus, EconomyFocusType.Iron);
            accumulator.Food += config.KillRewardFood * scale
                * EconomyFocusUtility.GetKillRewardMultiplier(config, focus, EconomyFocusType.Food);
        }

        [BurstCompile]
        private struct PoolReturnResetJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> Entities;

            [NativeDisableParallelForRestriction] public ComponentLookup<ZombieStats> StatsLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<ZombieState> StateLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<ZombieSlow> SlowLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<DeathTimer> DeathTimerLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<PhysicsBody> PhysicsLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<SpriteTint> TintLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<SpriteAnimation> AnimationLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> TransformLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<ZombieTag> ZombieTagLookup;

            public void Execute(int index)
            {
                Entity entity = Entities[index];

                if (StateLookup.HasComponent(entity))
                    StateLookup[entity] = new ZombieState { Value = ZombieStateType.Moving };

                if (StatsLookup.HasComponent(entity))
                {
                    ZombieStats stats = StatsLookup[entity];
                    stats.CurrentHP = stats.MaxHP;
                    stats.AttackTimer = 0f;
                    StatsLookup[entity] = stats;
                }

                if (SlowLookup.HasComponent(entity))
                {
                    SlowLookup[entity] = new ZombieSlow { Duration = 0f, SpeedMultiplier = 1f };
                    SlowLookup.SetComponentEnabled(entity, false);
                }

                if (DeathTimerLookup.HasComponent(entity))
                {
                    DeathTimerLookup[entity] = new DeathTimer { Value = 0f };
                    DeathTimerLookup.SetComponentEnabled(entity, false);
                }

                if (PhysicsLookup.HasComponent(entity))
                {
                    PhysicsBody body = PhysicsLookup[entity];
                    body.Velocity = float2.zero;
                    body.Force = float2.zero;
                    PhysicsLookup[entity] = body;
                }

                if (TintLookup.HasComponent(entity))
                    TintLookup[entity] = new SpriteTint { Value = ArcherVisualStyle.NormalTint };

                if (AnimationLookup.HasComponent(entity))
                {
                    SpriteAnimation animation = AnimationLookup[entity];
                    animation.DirectionRow = 0;
                    animation.FrameCount = 15;
                    animation.CurrentFrame = 0;
                    animation.FrameTimer = 0f;
                    AnimationLookup[entity] = animation;
                }

                if (TransformLookup.HasComponent(entity))
                    TransformLookup[entity] = LocalTransform.FromPositionRotationScale(
                        float3.zero, quaternion.identity, 0f);

                ZombieTagLookup.SetComponentEnabled(entity, false);
            }
        }
    }
}
