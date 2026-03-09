using Unity.Burst;
using Unity.Entities;

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
    [UpdateAfter(typeof(ZombieAnimationStateSystem))]
    public partial struct DamageCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<GameStateData>())
                return;

            var gameState = SystemAPI.GetSingletonRW<GameStateData>();
            var waveState = SystemAPI.GetSingletonRW<WaveStateData>();
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
                gameState.ValueRW.Gold += stats.ValueRO.GoldReward;
                gameState.ValueRW.XP += stats.ValueRO.XPReward;
                waveState.ValueRW.ZombiesAlive--;

                ecb.DestroyEntity(entity);
            }

            // Level up kontrolu
            if (gameState.ValueRO.XP >= gameState.ValueRO.XPToNextLevel && !gameState.ValueRO.IsLevelUpPending)
            {
                gameState.ValueRW.IsLevelUpPending = true;
            }
        }
    }
}
