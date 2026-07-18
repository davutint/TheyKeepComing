using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Echoing Detonation timer'ini ilerletir ve tek secondary strike uretir.
    /// Component yokken sistem RequireForUpdate nedeniyle calismaz.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FireballProjectileSystem))]
    [UpdateBefore(typeof(FireballStrikeSystem))]
    public partial struct FireballSecondBlastSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FireballDelayedBlast>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                GameStateData gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (blast, entity) in
                     SystemAPI.Query<RefRW<FireballDelayedBlast>>().WithEntityAccess())
            {
                blast.ValueRW.RemainingDelay -= deltaTime;
                if (blast.ValueRO.RemainingDelay > 0f)
                    continue;

                Entity strike = ecb.CreateEntity();
                ecb.AddComponent(strike, new FireballStrike
                {
                    Position = blast.ValueRO.Position,
                    Radius = blast.ValueRO.Radius,
                    Damage = blast.ValueRO.Damage,
                    Kind = FireballStrikeKind.SecondBlast,
                    Evolutions = FireballEvolutionFlags.None
                });
                ecb.DestroyEntity(entity);
            }
        }
    }

    /// <summary>
    /// Scorched Earth alanlarini sabit bir saniyelik tick ritmiyle ilerletir. Her tick tek
    /// aggregate AoE strike'tir; dusman basina entity, particle veya event uretilmez.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FireballProjectileSystem))]
    [UpdateBefore(typeof(FireballStrikeSystem))]
    public partial struct FireballBurningGroundSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FireballBurningGround>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameStateData>())
            {
                GameStateData gameState = SystemAPI.GetSingleton<GameStateData>();
                if (gameState.IsGameOver || gameState.IsLevelUpPending)
                    return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (ground, entity) in
                     SystemAPI.Query<RefRW<FireballBurningGround>>().WithEntityAccess())
            {
                float activeDelta = math.min(
                    math.max(0f, ground.ValueRO.RemainingDuration),
                    math.max(0f, deltaTime));
                ground.ValueRW.RemainingDuration -= activeDelta;
                ground.ValueRW.TimeUntilNextTick -= activeDelta;
                bool durationExpired = ground.ValueRO.RemainingDuration <= 0f;

                for (int tick = 0;
                     tick < FireballEvolutionRules.BurningGroundTickCount
                     && ground.ValueRO.RemainingTicks > 0
                     && (ground.ValueRO.TimeUntilNextTick <= 0f || durationExpired);
                     tick++)
                {
                    Entity strike = ecb.CreateEntity();
                    ecb.AddComponent(strike, new FireballStrike
                    {
                        Position = ground.ValueRO.Position,
                        Radius = ground.ValueRO.Radius,
                        Damage = ground.ValueRO.DamagePerTick,
                        Kind = FireballStrikeKind.BurningGroundPulse,
                        Evolutions = FireballEvolutionFlags.None
                    });
                    ground.ValueRW.TimeUntilNextTick +=
                        FireballEvolutionRules.BurningGroundTickIntervalSeconds;
                    ground.ValueRW.RemainingTicks--;
                }

                if (ground.ValueRO.RemainingTicks <= 0 || durationExpired)
                    ecb.DestroyEntity(entity);
            }
        }
    }
}
