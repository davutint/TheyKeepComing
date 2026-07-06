using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// TEK SYNC POINT: Tum physics + attack job'lari burada tamamlanir.
    /// ZombieAttackTimerSystem'in DamageQueue'sunu drain eder,
    /// hasari Wall -> Gate -> Castle sirasina gore uygular.
    /// </summary>
    // [BurstCompile] struct'tan kaldirildi — static field erisimi (ZombieAttackTimerSystem.DamageQueue)
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieAnimationStateSystem))]
    [UpdateBefore(typeof(DamageCleanupSystem))]
    public partial struct DamageApplySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WallSegment>();
            state.RequireForUpdate<GameStateData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // TEK SYNC POINT — tum physics + attack job'lari burada tamamlanir
            state.CompleteDependency();

            var damageQueue = ZombieAttackTimerSystem.DamageQueue;
            if (!damageQueue.IsCreated || damageQueue.Count == 0)
                return;

            if (SystemAPI.HasSingleton<WaveStateData>() &&
                SystemAPI.GetSingleton<WaveStateData>().StressTestMode)
            {
                damageQueue.Clear();
                return;
            }

            float damageMultiplier = 1f;
            if (SystemAPI.HasSingleton<MobileCastleCombatConfig>()
                && SystemAPI.HasSingleton<CastleYardPrepState>())
            {
                var prep = SystemAPI.GetSingleton<CastleYardPrepState>();
                if (prep.FortifyActive)
                    damageMultiplier = math.max(0f, prep.FortifyDamageMultiplier);
            }

            var wallEntity = SystemAPI.GetSingletonEntity<WallSegment>();
            var wall = SystemAPI.GetComponentRW<WallSegment>(wallEntity);
            var gate = SystemAPI.GetComponentRW<GateComponent>(wallEntity);
            var castle = SystemAPI.GetComponentRW<CastleHP>(wallEntity);

            float totalAppliedDamage = 0f;
            while (damageQueue.TryDequeue(out float damage))
            {
                float finalDamage = damage * damageMultiplier;
                totalAppliedDamage += finalDamage;
                // Oncelik: Duvar -> Kapi -> Kale
                if (wall.ValueRO.CurrentHP > 0f)
                {
                    wall.ValueRW.CurrentHP = math.max(0f, wall.ValueRO.CurrentHP - finalDamage);
                }
                else if (gate.ValueRO.CurrentHP > 0f)
                {
                    gate.ValueRW.CurrentHP = math.max(0f, gate.ValueRO.CurrentHP - finalDamage);
                }
                else
                {
                    castle.ValueRW.CurrentHP = math.max(0f, castle.ValueRO.CurrentHP - finalDamage);
                }
            }

            // DIKKAT: EmitCastleHitFeedback CreateEntity yapar (structural change) ve yukaridaki
            // wall/gate/castle RefRW'lerini GECERSIZ kilar. Kale HP'si structural change'den ONCE
            // lokale alinir; aksi halde castle.ValueRO ObjectDisposedException firlatir ve
            // GameOver hic tetiklenmezdi.
            float remainingCastleHp = castle.ValueRO.CurrentHP;

            if (totalAppliedDamage > 0f)
            {
                float2 feedbackCenter = float2.zero;
                if (SystemAPI.HasSingleton<MobileCastleCombatConfig>())
                {
                    var mobileConfig = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
                    // Tek cephe: vurus geri bildirimi duvar hattinda; 360 modda kale merkezinde
                    feedbackCenter = mobileConfig.SingleFrontEnabled
                        ? new float2(mobileConfig.FrontlineX, 0f)
                        : mobileConfig.CastleCenter;
                }
                EmitCastleHitFeedback(ref state, feedbackCenter);
            }

            // Game Over kontrolu (structural change sonrasi taze RefRW alinir)
            if (remainingCastleHp <= 0f)
            {
                var gameState = SystemAPI.GetSingletonRW<GameStateData>();
                gameState.ValueRW.IsGameOver = true;
            }
        }

        private static void EmitCastleHitFeedback(ref SystemState state, float2 center)
        {
            float3 position = new float3(center.x, center.y, MobileCastleRenderDepth.ProjectileZ);

            Entity vfxEvent = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(vfxEvent, new CombatVfxEvent
            {
                Position = position,
                Direction = new float3(0f, 1f, 0f),
                Type = CombatVfxType.CastleHit,
                Scale = 0.35f
            });

            Entity sfxEvent = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(sfxEvent, new CombatSfxEvent
            {
                Position = position,
                Type = CombatSfxType.CastleHit,
                Volume = 0.45f,
                Pitch = 1f
            });
        }
    }
}
