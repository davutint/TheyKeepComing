using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Oyuncunun Ates Topu vuruslarini isler (M-C buyuculuk). FireballStrike entity'lerini
    /// (Mono -> GameManager.TryCastFireball yaratir) toplar, yaricap ici TUM zombilere tek
    /// seferlik hasar uygular ve strike'lari siler. Olum akisi degismez (HP<=0 ->
    /// ZombieDeathSystem). Patlama gorseli Mono tarafta (SpellCastUI) oynar.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowHitSystem))]
    [UpdateBefore(typeof(ZombieDeathSystem))]
    public partial struct FireballStrikeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FireballStrike>();
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

            // Strike'lar cok nadir (cooldown'lu oyuncu aksiyonu) — main-thread toplama ucuz
            var strikes = new NativeList<FireballStrike>(4, Allocator.TempJob);
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (strike, entity) in SystemAPI.Query<RefRO<FireballStrike>>().WithEntityAccess())
            {
                FireballStrike value = strike.ValueRO;
                strikes.Add(value);
                ecb.DestroyEntity(entity);

                if (value.Kind == FireballStrikeKind.Primary)
                {
                    if (FireballEvolutionRules.Has(
                            value.Evolutions,
                            FireballEvolutionFlags.SecondBlast))
                    {
                        Entity delayedBlast = ecb.CreateEntity();
                        ecb.AddComponent(delayedBlast, new FireballDelayedBlast
                        {
                            Position = value.Position,
                            Radius = value.Radius * FireballEvolutionRules.SecondBlastRadiusMultiplier,
                            Damage = value.Damage * FireballEvolutionRules.SecondBlastDamageMultiplier,
                            RemainingDelay = FireballEvolutionRules.SecondBlastDelaySeconds
                        });
                    }

                    if (FireballEvolutionRules.Has(
                            value.Evolutions,
                            FireballEvolutionFlags.BurningGround))
                    {
                        Entity burningGround = ecb.CreateEntity();
                        ecb.AddComponent(burningGround, new FireballBurningGround
                        {
                            Position = value.Position,
                            Radius = value.Radius * FireballEvolutionRules.BurningGroundRadiusMultiplier,
                            DamagePerTick = value.Damage
                                            * FireballEvolutionRules.BurningGroundDamageMultiplierPerTick,
                            RemainingDuration = FireballEvolutionRules.BurningGroundDurationSeconds,
                            TimeUntilNextTick = FireballEvolutionRules.BurningGroundTickIntervalSeconds,
                            RemainingTicks = FireballEvolutionRules.BurningGroundTickCount
                        });
                    }
                }

                // Burning Ground pulse'lari per-enemy veya per-tick ses uretmez. Primary ve
                // Echoing Detonation ayni rate-limited patlama kanalini farkli pitch ile kullanir.
                if (value.Kind != FireballStrikeKind.BurningGroundPulse)
                {
                    Entity sfxEvent = ecb.CreateEntity();
                    ecb.AddComponent(sfxEvent, new CombatSfxEvent
                    {
                        Position = new float3(value.Position.x, value.Position.y, 0f),
                        Type = CombatSfxType.FireballBlast,
                        Volume = value.Kind == FireballStrikeKind.SecondBlast ? 0.68f : 0.9f,
                        Pitch = value.Kind == FireballStrikeKind.SecondBlast ? 1.16f : 1f
                    });
                }
            }

            state.Dependency = new FireballDamageJob
            {
                Strikes = strikes.AsArray(),
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            strikes.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct FireballDamageJob : IJobEntity
        {
            [ReadOnly] public NativeArray<FireballStrike> Strikes;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute([ChunkIndexInQuery] int sortKey,
                ref ZombieStats stats, in LocalTransform transform)
            {
                for (int i = 0; i < Strikes.Length; i++)
                {
                    float radiusSq = Strikes[i].Radius * Strikes[i].Radius;
                    if (math.distancesq(transform.Position.xy, Strikes[i].Position) <= radiusSq)
                    {
                        float previousHp = math.max(0f, stats.CurrentHP);
                        float appliedDamage = math.min(
                            previousHp,
                            math.max(0f, Strikes[i].Damage));
                        if (appliedDamage <= 0f)
                            continue;

                        stats.CurrentHP = previousHp - appliedDamage;
                        Entity damageNumberEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, damageNumberEvent, new CombatDamageNumberEvent
                        {
                            Position = new float3(
                                transform.Position.x,
                                transform.Position.y,
                                MobileCastleRenderDepth.ProjectileZ),
                            AppliedDamage = appliedDamage,
                            Source = ResolveDamageSource(Strikes[i].Kind)
                        });
                    }
                }
            }

            private static PlayerDamageSourceType ResolveDamageSource(FireballStrikeKind kind)
            {
                return kind switch
                {
                    FireballStrikeKind.SecondBlast =>
                        PlayerDamageSourceType.FireballSecondBlast,
                    FireballStrikeKind.BurningGroundPulse =>
                        PlayerDamageSourceType.FireballBurningGround,
                    _ => PlayerDamageSourceType.Fireball
                };
            }
        }
    }
}
