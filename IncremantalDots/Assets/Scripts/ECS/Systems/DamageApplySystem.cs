using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// TEK SYNC POINT: Tum physics + attack job'lari burada tamamlanir.
    /// ZombieAttackTimerSystem'in DamageQueue'sunu drain eder,
    /// hasari yalniz Wall'a uygular; Wall sifirlandiginda Game Over tetikler.
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

            if (SystemAPI.GetSingleton<GameStateData>().IsGameOver)
            {
                damageQueue.Clear();
                return;
            }

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

            float totalAppliedDamage = 0f;
            while (damageQueue.TryDequeue(out float damage))
            {
                float previousHp = wall.ValueRO.CurrentHP;
                float nextHp = SingleWallDefenseRules.ApplyDamage(previousHp, damage, damageMultiplier);
                totalAppliedDamage += math.max(0f, previousHp - nextHp);
                wall.ValueRW.CurrentHP = nextHp;
            }

            // Structural change oncesinde Wall HP lokale alinir.
            float remainingWallHp = wall.ValueRO.CurrentHP;

            if (totalAppliedDamage > 0f)
            {
                RecordRunWallDamage(ref state, totalAppliedDamage);

                float2 feedbackCenter = float2.zero;
                if (SystemAPI.HasSingleton<MobileCastleCombatConfig>())
                {
                    var mobileConfig = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
                    feedbackCenter = mobileConfig.SingleFrontEnabled
                        ? new float2(mobileConfig.FrontlineX, 0f)
                        : mobileConfig.CastleCenter;
                }

                EmitCastleHitFeedback(ref state, feedbackCenter);
            }

            // Tek sonuc otoritesi Wall'dir. Sifir HP, tek yonlu ve kesin Game Over'dir.
            if (SingleWallDefenseRules.IsDestroyed(remainingWallHp))
            {
                var gameState = SystemAPI.GetSingletonRW<GameStateData>();
                if (!gameState.ValueRO.IsGameOver)
                    gameState.ValueRW.IsGameOver = true;
            }
        }

        private void RecordRunWallDamage(ref SystemState state, float damage)
        {
            if (!SystemAPI.HasSingleton<RunTelemetryData>())
                return;

            Entity telemetryEntity = SystemAPI.GetSingletonEntity<RunTelemetryData>();
            if (!state.EntityManager.HasBuffer<RunWallDamageTelemetryElement>(telemetryEntity))
                return;

            int day = 1;
            SiegeCyclePhase phase = SiegeCyclePhase.Night;
            if (SystemAPI.HasSingleton<ContinuousSiegeCycleData>())
            {
                ContinuousSiegeCycleData cycle = SystemAPI.GetSingleton<ContinuousSiegeCycleData>();
                day = cycle.CycleIndex >= int.MaxValue
                    ? int.MaxValue
                    : math.max(1, cycle.CycleIndex + 1);
                phase = cycle.Phase;
            }
            else if (SystemAPI.HasSingleton<WaveStateData>()
                     && SystemAPI.GetSingleton<WaveStateData>().Phase == RunPhaseType.DayPrep)
            {
                phase = SiegeCyclePhase.Day;
            }

            DynamicBuffer<RunWallDamageTelemetryElement> timeline =
                state.EntityManager.GetBuffer<RunWallDamageTelemetryElement>(telemetryEntity);
            RunTelemetryAccumulator.RecordWallDamage(timeline, day, phase, damage);
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
