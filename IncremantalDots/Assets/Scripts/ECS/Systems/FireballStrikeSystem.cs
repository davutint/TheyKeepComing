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
                strikes.Add(strike.ValueRO);
                ecb.DestroyEntity(entity);
            }

            state.Dependency = new FireballDamageJob
            {
                Strikes = strikes.AsArray()
            }.ScheduleParallel(state.Dependency);
            strikes.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        partial struct FireballDamageJob : IJobEntity
        {
            [ReadOnly] public NativeArray<FireballStrike> Strikes;

            void Execute(ref ZombieStats stats, in LocalTransform transform)
            {
                for (int i = 0; i < Strikes.Length; i++)
                {
                    float radiusSq = Strikes[i].Radius * Strikes[i].Radius;
                    if (math.distancesq(transform.Position.xy, Strikes[i].Position) <= radiusSq)
                        stats.CurrentHP -= Strikes[i].Damage;
                }
            }
        }
    }
}
