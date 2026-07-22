using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DeadWalls
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ArrowProductionSystem))]
    [UpdateAfter(typeof(BuildingPopulationSystem))]
    [UpdateBefore(typeof(PopulationTickSystem))]
    public partial struct MobilePopulationEconomySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<WaveStateData>();
            state.RequireForUpdate<MobileCastleCombatConfig>();
            state.RequireForUpdate<MobileBedCapacityState>();
            state.RequireForUpdate<MobilePopulationAllocation>();
            state.RequireForUpdate<MobileEconomyEventState>();
            state.RequireForUpdate<PopulationState>();
            state.RequireForUpdate<ResourceData>();
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
            var bedCapacity = SystemAPI.GetSingleton<MobileBedCapacityState>();
            var allocationRW = SystemAPI.GetSingletonRW<MobilePopulationAllocation>();
            var eventRW = SystemAPI.GetSingletonRW<MobileEconomyEventState>();
            var populationRW = SystemAPI.GetSingletonRW<PopulationState>();
            var productionRW = SystemAPI.GetSingletonRW<ResourceProductionRate>();
            var resourcesRW = SystemAPI.GetSingletonRW<ResourceData>();

            SyncBedCapacity(ref populationRW.ValueRW, bedCapacity);

            if (config.ContinuousSiegeEnabled && SystemAPI.HasSingleton<ContinuousSiegeCycleData>())
            {
                var cycle = SystemAPI.GetSingleton<ContinuousSiegeCycleData>();
                ApplyContinuousCycleGrowth(ref allocationRW.ValueRW, ref populationRW.ValueRW,
                    ref resourcesRW.ValueRW, config, cycle, bedCapacity);
                ExpireContinuousEventEffects(ref eventRW.ValueRW, cycle);
            }
            else if (!wave.WaveActive && wave.Phase == RunPhaseType.DayPrep && wave.CurrentWave > 0)
            {
                ApplyDayPrepStart(ref allocationRW.ValueRW, ref eventRW.ValueRW,
                    ref populationRW.ValueRW, ref resourcesRW.ValueRW,
                    config, wave.CurrentWave, bedCapacity);
            }

            SyncWorkerCapacities(ref allocationRW.ValueRW, config);
            NormalizeAllocation(ref allocationRW.ValueRW, ref populationRW.ValueRW);
            WorkerAllocationUtility.BeginPopulationUpdate(
                ref allocationRW.ValueRW, populationRW.ValueRO.Total);
            int unassignedPopulation = WorkerAllocationUtility.ResolveIdlePopulation(
                allocationRW.ValueRO,
                populationRW.ValueRO.Total,
                populationRW.ValueRO.Archers);
            if (unassignedPopulation > 0)
            {
                WorkerAllocationUtility.AutoAssignNewPopulation(
                    ref allocationRW.ValueRW,
                    unassignedPopulation);
                NormalizeAllocation(ref allocationRW.ValueRW, ref populationRW.ValueRW);
            }
            SyncBedCapacity(ref populationRW.ValueRW, bedCapacity);
            WriteProductionRates(ref productionRW.ValueRW, allocationRW.ValueRO, eventRW.ValueRO, config);
        }

        private static void ApplyContinuousCycleGrowth(ref MobilePopulationAllocation allocation,
            ref PopulationState population, ref ResourceData resources,
            MobileCastleCombatConfig config, ContinuousSiegeCycleData cycle,
            MobileBedCapacityState bedCapacity)
        {
            if (!cycle.Enabled)
                return;

            // Odul ani DAWN'a tasindi (GDD 4-faz): geceyi atlatinca gorunur nufus odulu.
            // Dawn fazi yoksa (legacy 3-faz bake) eski davranis: cycle wrap'inde ver.
            bool dawnConfigured = cycle.DawnDuration > 0f;
            if (dawnConfigured)
            {
                // Isaret degeri: Dawn sirasinda BU cycle'in odulu (CycleIndex+1, cunku CycleIndex
                // Dawn'da henuz artmadi); diger fazlarda EN SON TAMAMLANAN cycle'in odulu (CycleIndex).
                // Monotonik >= kontrolu hem cift-odulu engeller hem de cok buyuk dt / cok kisa Dawn
                // yuzunden Dawn frame'i hic gorulmezse odulu bir sonraki fazda TELAFI eder.
                int rewardCycle = cycle.Phase == SiegeCyclePhase.Dawn ? cycle.CycleIndex + 1 : cycle.CycleIndex;
                if (rewardCycle <= 0 || allocation.LastPopulationGrowthCycle >= rewardCycle)
                    return;

                allocation.LastPopulationGrowthCycle = rewardCycle;
            }
            else
            {
                if (cycle.CycleIndex <= 0 || allocation.LastPopulationGrowthCycle == cycle.CycleIndex)
                    return;

                allocation.LastPopulationGrowthCycle = cycle.CycleIndex;
            }

            ApplyArrivalTransaction(ref allocation, ref population, ref resources, config, bedCapacity);
        }

        /// <summary>
        /// Continuous modda sureli event etkilerinin son kullanma kontrolu (legacy ApplyDayPrepStart
        /// continuous'ta hic kosulamadigi icin buradan islenir). Wave sayisi = CycleIndex + 1.
        /// </summary>
        private static void ExpireContinuousEventEffects(ref MobileEconomyEventState economyEvent,
            ContinuousSiegeCycleData cycle)
        {
            int currentWave = math.max(1, cycle.CycleIndex + 1);

            if (economyEvent.ProductionBonusExpiresAfterWave > 0
                && currentWave >= economyEvent.ProductionBonusExpiresAfterWave)
            {
                economyEvent.ProductionBonusResource = EconomyFocusType.Balanced;
                economyEvent.ProductionBonusMultiplier = 1f;
                economyEvent.ProductionBonusExpiresAfterWave = 0;
            }

            if (economyEvent.NightSpawnExpiresAfterWave > 0
                && currentWave >= economyEvent.NightSpawnExpiresAfterWave)
            {
                economyEvent.NextNightSpawnMultiplier = 1f;
                economyEvent.NightSpawnExpiresAfterWave = 0;
            }
        }

        private static void ApplyDayPrepStart(ref MobilePopulationAllocation allocation,
            ref MobileEconomyEventState economyEvent, ref PopulationState population,
            ref ResourceData resources, MobileCastleCombatConfig config,
            int currentWave, MobileBedCapacityState bedCapacity)
        {
            if (allocation.LastPopulationGrowthWave != currentWave)
            {
                ApplyArrivalTransaction(ref allocation, ref population, ref resources, config, bedCapacity);
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

        private static void ApplyArrivalTransaction(ref MobilePopulationAllocation allocation,
            ref PopulationState population, ref ResourceData resources,
            MobileCastleCombatConfig config, MobileBedCapacityState bedCapacity)
        {
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                config.PopulationGrowthPerDayPrep,
                population.Total,
                MobileBedCapacityUtility.GetTotalCapacity(bedCapacity),
                resources.Food,
                config.FoodCostPerArrival);

            allocation.LastArrivalRequestedCount = budget.RequestedArrivals;
            allocation.LastArrivalAcceptedCount = budget.AcceptedArrivals;
            allocation.LastArrivalFoodCost = budget.RequiredFood;
            resources.Food = math.max(0, resources.Food - budget.RequiredFood);
            population.Total += budget.AcceptedArrivals;
        }

        private static void SyncBedCapacity(ref PopulationState population,
            MobileBedCapacityState bedCapacity)
        {
            population.BaseCapacity = math.max(0, bedCapacity.BaseCapacity);
            population.Capacity = MobileBedCapacityUtility.GetTotalCapacity(bedCapacity);
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

        private static void SyncWorkerCapacities(ref MobilePopulationAllocation allocation,
            MobileCastleCombatConfig config)
        {
            allocation.WoodWorkerCapacity = math.max(0, config.WoodWorkerCap);
            allocation.StoneWorkerCapacity = math.max(0, config.StoneWorkerCap);
            allocation.IronWorkerCapacity = math.max(0, config.IronWorkerCap);
            allocation.FoodWorkerCapacity = math.max(0, config.FoodWorkerCap);
        }

        private static void NormalizeAllocation(ref MobilePopulationAllocation allocation, ref PopulationState population)
        {
            allocation.WoodWorkers = ClampWorkerCount(allocation.WoodWorkers, allocation.WoodWorkerCapacity);
            allocation.StoneWorkers = ClampWorkerCount(allocation.StoneWorkers, allocation.StoneWorkerCapacity);
            allocation.IronWorkers = ClampWorkerCount(allocation.IronWorkers, allocation.IronWorkerCapacity);
            allocation.FoodWorkers = ClampWorkerCount(allocation.FoodWorkers, allocation.FoodWorkerCapacity);

            int availableForWorkers = math.max(0, population.Total - population.Archers);
            int totalWorkers = allocation.WoodWorkers + allocation.StoneWorkers + allocation.IronWorkers + allocation.FoodWorkers;
            int overflow = totalWorkers - availableForWorkers;
            if (overflow > 0)
                WorkerAllocationUtility.RemoveWorkersInResourceOrder(ref allocation, overflow);

            totalWorkers = allocation.WoodWorkers + allocation.StoneWorkers + allocation.IronWorkers + allocation.FoodWorkers;
            population.Workers = totalWorkers;
            population.Idle = WorkerAllocationUtility.ResolveIdlePopulation(
                allocation,
                population.Total,
                population.Archers);
            allocation.IdlePopulation = population.Idle;
        }

        private static int ClampWorkerCount(int value, int cap)
        {
            value = math.max(0, value);
            return cap > 0 ? math.min(value, cap) : value;
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
