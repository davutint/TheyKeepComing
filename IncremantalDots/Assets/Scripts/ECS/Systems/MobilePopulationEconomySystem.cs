using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowProductionSystem))]
    [UpdateBefore(typeof(PopulationTickSystem))]
    public partial struct MobilePopulationEconomySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<MobileCastleCombatConfig>();
            state.RequireForUpdate<MobilePopulationAllocation>();
            state.RequireForUpdate<MobileEconomyEventState>();
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<ResourceProductionRate>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameStateData>();
            var wave = SystemAPI.GetSingleton<WaveStateData>();
            if (gameState.IsGameOver || wave.StressTestMode)
                return;

            var config = SystemAPI.GetSingleton<MobileCastleCombatConfig>();
            var allocationRW = SystemAPI.GetSingletonRW<MobilePopulationAllocation>();
            var eventRW = SystemAPI.GetSingletonRW<MobileEconomyEventState>();
            var populationRW = SystemAPI.GetSingletonRW<PopulationState>();
            var productionRW = SystemAPI.GetSingletonRW<ResourceProductionRate>();

            if (!wave.WaveActive && wave.Phase == RunPhaseType.DayPrep && wave.CurrentWave > 0)
                ApplyDayPrepStart(ref allocationRW.ValueRW, ref eventRW.ValueRW, ref populationRW.ValueRW, config, wave.CurrentWave);

            NormalizeAllocation(ref allocationRW.ValueRW, ref populationRW.ValueRW);
            WriteProductionRates(ref productionRW.ValueRW, allocationRW.ValueRO, eventRW.ValueRO, config);
        }

        private static void ApplyDayPrepStart(ref MobilePopulationAllocation allocation,
            ref MobileEconomyEventState economyEvent, ref PopulationState population,
            MobileCastleCombatConfig config, int currentWave)
        {
            if (allocation.LastPopulationGrowthWave != currentWave)
            {
                population.Total += math.max(0, config.PopulationGrowthPerDayPrep);
                population.Capacity = math.max(population.Capacity, population.Total);
                population.BaseCapacity = math.max(population.BaseCapacity, population.Capacity);
                allocation.LastPopulationGrowthWave = currentWave;
            }

            if (economyEvent.ProductionBonusExpiresAfterWave > 0
                && currentWave >= economyEvent.ProductionBonusExpiresAfterWave)
            {
                economyEvent.ProductionBonusResource = EconomyFocusType.Balanced;
                economyEvent.ProductionBonusMultiplier = 1f;
                economyEvent.ProductionBonusExpiresAfterWave = 0;
            }

            if (allocation.LastEventPrepWave == currentWave)
                return;

            allocation.LastEventPrepWave = currentWave;
            if (economyEvent.PendingEvent != MobileEconomyEventType.None)
                return;

            if (economyEvent.CooldownWavesRemaining > 0)
            {
                economyEvent.CooldownWavesRemaining--;
                return;
            }

            if (!ShouldRollEvent(economyEvent.RandomSeed, currentWave, config.EconomyEventChance))
                return;

            economyEvent.PendingEvent = PickEvent(economyEvent.RandomSeed, currentWave);
            economyEvent.EventWave = currentWave;
            economyEvent.CooldownWavesRemaining = math.max(0, config.EconomyEventCooldownWaves);
        }

        private static bool ShouldRollEvent(uint seed, int currentWave, float chance)
        {
            if (chance <= 0f)
                return false;

            uint hash = math.hash(new uint3(seed == 0u ? 91273u : seed, (uint)currentWave, 0x9E3779B9u));
            float roll = (hash & 0x00FFFFFFu) / 16777215f;
            return roll < math.saturate(chance);
        }

        private static MobileEconomyEventType PickEvent(uint seed, int currentWave)
        {
            uint hash = math.hash(new uint3(seed == 0u ? 91273u : seed, (uint)currentWave, 0x85EBCA6Bu));
            switch (hash % 3u)
            {
                case 0u:
                    return MobileEconomyEventType.ForestCache;
                case 1u:
                    return MobileEconomyEventType.QuarryCrew;
                default:
                    return MobileEconomyEventType.RefugeeCart;
            }
        }

        private static void NormalizeAllocation(ref MobilePopulationAllocation allocation, ref PopulationState population)
        {
            allocation.WoodWorkers = math.max(0, allocation.WoodWorkers);
            allocation.StoneWorkers = math.max(0, allocation.StoneWorkers);
            allocation.IronWorkers = math.max(0, allocation.IronWorkers);
            allocation.FoodWorkers = math.max(0, allocation.FoodWorkers);

            int availableForWorkers = math.max(0, population.Total - population.Archers);
            int totalWorkers = allocation.WoodWorkers + allocation.StoneWorkers + allocation.IronWorkers + allocation.FoodWorkers;
            int overflow = totalWorkers - availableForWorkers;
            if (overflow > 0)
            {
                Reduce(ref allocation.IronWorkers, ref overflow);
                Reduce(ref allocation.StoneWorkers, ref overflow);
                Reduce(ref allocation.WoodWorkers, ref overflow);
                Reduce(ref allocation.FoodWorkers, ref overflow);
            }

            totalWorkers = allocation.WoodWorkers + allocation.StoneWorkers + allocation.IronWorkers + allocation.FoodWorkers;
            population.Workers = totalWorkers;
            population.Idle = math.max(0, population.Total - population.Workers - population.Archers);
            population.Capacity = math.max(population.Capacity, population.Total);
            population.BaseCapacity = math.max(population.BaseCapacity, population.Capacity);
        }

        private static void Reduce(ref int value, ref int overflow)
        {
            if (overflow <= 0 || value <= 0)
                return;

            int amount = math.min(value, overflow);
            value -= amount;
            overflow -= amount;
        }

        private static void WriteProductionRates(ref ResourceProductionRate production,
            MobilePopulationAllocation allocation,
            MobileEconomyEventState economyEvent, MobileCastleCombatConfig config)
        {
            production.WoodPerMin = ApplyProductionBonus(
                allocation.WoodWorkers * config.WoodWorkerProductionPerMin,
                economyEvent, EconomyFocusType.Wood);
            production.StonePerMin = ApplyProductionBonus(
                allocation.StoneWorkers * config.StoneWorkerProductionPerMin,
                economyEvent, EconomyFocusType.Stone);
            production.IronPerMin = ApplyProductionBonus(
                allocation.IronWorkers * config.IronWorkerProductionPerMin,
                economyEvent, EconomyFocusType.Iron);
            production.FoodPerMin = ApplyProductionBonus(
                allocation.FoodWorkers * config.FoodWorkerProductionPerMin,
                economyEvent, EconomyFocusType.Food);
        }

        private static float ApplyProductionBonus(float value, MobileEconomyEventState economyEvent,
            EconomyFocusType resource)
        {
            if (economyEvent.ProductionBonusResource != resource || economyEvent.ProductionBonusMultiplier <= 0f)
                return value;

            return value * economyEvent.ProductionBonusMultiplier;
        }
    }
}
