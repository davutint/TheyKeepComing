using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Olen zombileri temizler.
    ///
    /// Eski akis: Dead → aninda sil
    /// Yeni akis: Dead + DeathTimer → timer say → 0'a dusunce sil + odul ver
    ///
    /// DeathTimer, ZombieAnimationStateSystem tarafindan eklenir.
    /// Bu system sadece DeathTimer olan entity'leri isle.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageApplySystem))]
    public partial struct DamageCleanupSystem : ISystem
    {
        [BurstCompile]
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

                // Timer bitti → odul ver + sil
                gameState.ValueRW.XP += stats.ValueRO.XPReward;
                waveState.ValueRW.ZombiesAlive--;
                if (canApplyMobileReward)
                    AddKillReward(ref resourceAccumulator.ValueRW, mobileConfig, economyFocus,
                        waveState.ValueRO.CurrentWave, mobileRewardMultiplier);

                ecb.DestroyEntity(entity);
            }

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
    }
}
