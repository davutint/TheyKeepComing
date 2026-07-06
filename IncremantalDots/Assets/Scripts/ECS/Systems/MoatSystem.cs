using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DeadWalls
{
    /// <summary>
    /// Hendek (moat) sistemi — Tek Cephe (K4) savunma katmani. Hendek x-bandinin icindeki
    /// Moving/Queued zombilere yavaslatma uygular (frost ile ayni ZombieSlow kanali:
    /// en dusuk carpan kazanir, sure kisa tutulup her frame tazelenir; banttan cikinca
    /// ZombieSlowTimerSystem dogal sonumler). MoatDamagePerSecond > 0 ise (moat_flame tech'i)
    /// banttaki zombiler surekli hasar alir; olum ZombieDeathSystem'de islenir.
    /// NOT: Frost'un uzun suresi aktifken hendekten gecen zombi, frost suresi boyunca
    /// hendek carpanini tasiyabilir — oyuncu lehine kucuk kayma, bilinçli kabul (V1).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ZombieSlowTimerSystem))]
    [UpdateBefore(typeof(ApplyMovementForceSystem))]
    public partial struct MoatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MobileCastleCombatConfig>();
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

            var config = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
            bool slowActive = config.MoatSlowMultiplier < 0.999f;
            bool damageActive = config.MoatDamagePerSecond > 0f;
            if (!config.SingleFrontEnabled || (!slowActive && !damageActive))
                return;

            new MoatJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                MoatXMin = config.MoatXMin,
                MoatXMax = config.MoatXMax,
                SlowMultiplier = config.MoatSlowMultiplier,
                DamagePerSecond = config.MoatDamagePerSecond,
                SlowActive = slowActive,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ZombieTag))]
        [WithPresent(typeof(ZombieSlow))]
        partial struct MoatJob : IJobEntity
        {
            public float Dt;
            public float MoatXMin;
            public float MoatXMax;
            public float SlowMultiplier;
            public float DamagePerSecond;
            public bool SlowActive;

            void Execute(
                ref ZombieSlow slow,
                EnabledRefRW<ZombieSlow> slowEnabled,
                ref ZombieStats stats,
                in ZombieState zombieState,
                in LocalTransform transform)
            {
                if (zombieState.Value == ZombieStateType.Dead)
                    return;

                float x = transform.Position.x;
                if (x < MoatXMin || x > MoatXMax)
                    return;

                if (SlowActive)
                {
                    // Frost ile birlesme: en guclu (en dusuk) carpan kazanir; sure kisa
                    // tazelenir ki banttan cikinca hizli sonumlensin (frost suresi ezilmez)
                    float multiplier = slowEnabled.ValueRO
                        ? math.min(slow.SpeedMultiplier, SlowMultiplier)
                        : SlowMultiplier;
                    slow.SpeedMultiplier = multiplier;
                    slow.Duration = math.max(slow.Duration, 0.15f);
                    slowEnabled.ValueRW = true;
                }

                if (DamagePerSecond > 0f)
                    stats.CurrentHP -= DamagePerSecond * Dt;
            }
        }
    }
}
