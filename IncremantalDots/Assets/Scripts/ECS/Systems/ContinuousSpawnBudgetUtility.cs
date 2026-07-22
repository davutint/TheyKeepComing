using Unity.Mathematics;

namespace DeadWalls
{
    /// <summary>
    /// Continuous horde spawn budget matematiginin saf ve Burst-uyumlu owner'i.
    /// </summary>
    public static class ContinuousSpawnBudgetUtility
    {
        public static bool HasNightEnemiesRemaining(long pendingEnemies, int zombiesAlive)
        {
            return pendingEnemies > 0L || zombiesAlive > 0;
        }

        public static bool ShouldHoldAtNightEnd(
            SiegeCyclePhase previousPhase,
            float timer,
            float nightEnd,
            long pendingEnemies,
            int zombiesAlive)
        {
            if (!HasNightEnemiesRemaining(pendingEnemies, zombiesAlive))
                return false;

            // Eski continuous-save'lerde Day/Dusk/Dawn sirasinda canli veya backlog dusman
            // bulunabilir. Yeni night-only sozlesmesinde bunlar gunduz savasamaz; ilk frame'de
            // ayni dusmanlari kaybetmeden Night clearance kapisina tasinir.
            return previousPhase != SiegeCyclePhase.Night || timer >= nightEnd;
        }

        public static bool CanGenerateDemand(ContinuousSiegeCycleData cycle)
        {
            return cycle.Phase == SiegeCyclePhase.Night
                && cycle.SpawnIntensityMultiplier > 0f
                && cycle.PhaseProgress01 < 1f;
        }

        public static bool CanDrainPending(ContinuousSiegeCycleData cycle)
        {
            return cycle.Phase == SiegeCyclePhase.Night;
        }

        public static bool IsNightClearance(ContinuousSiegeCycleData cycle)
        {
            return cycle.Enabled
                && cycle.Phase == SiegeCyclePhase.Night
                && cycle.PhaseProgress01 >= 0.999f
                && cycle.SpawnIntensityMultiplier <= 0f;
        }

        public static int ResolveDemandPerInterval(
            int baseBatch,
            float growthPerCycle,
            int currentWave,
            float dayQuantityMultiplier,
            float phaseIntensityMultiplier,
            int maxBatch)
        {
            float cycleGrowth = 1f
                + (math.max(1, currentWave) - 1) * math.max(0f, growthPerCycle);
            float demand = math.max(1, baseBatch)
                * cycleGrowth
                * math.max(0.01f, dayQuantityMultiplier)
                * math.max(0.01f, phaseIntensityMultiplier);
            int resolved = math.max(1, (int)math.round(demand));
            return maxBatch > 0 ? math.min(resolved, maxBatch) : resolved;
        }

        public static float ResolveEffectiveInterval(
            float dayBaseInterval,
            float phaseIntensityMultiplier,
            float minInterval)
        {
            return math.max(
                math.max(0.001f, minInterval),
                math.max(0.001f, dayBaseInterval) / math.max(0.01f, phaseIntensityMultiplier));
        }

        public static int CountElapsedIntervals(float timerAfterDelta, float effectiveInterval)
        {
            if (timerAfterDelta > 0f)
                return 0;

            float interval = math.max(0.001f, effectiveInterval);
            return math.max(1, 1 + (int)math.floor(-timerAfterDelta / interval));
        }

        public static float AdvanceTimer(float timerAfterDelta, float effectiveInterval, int elapsedIntervals)
        {
            if (elapsedIntervals <= 0)
                return timerAfterDelta;

            return timerAfterDelta + math.max(0.001f, effectiveInterval) * elapsedIntervals;
        }

        public static long AddDemand(long pendingEnemies, int demandPerInterval, int elapsedIntervals)
        {
            if (demandPerInterval <= 0 || elapsedIntervals <= 0)
                return math.max(0L, pendingEnemies);

            long demand = (long)demandPerInterval * elapsedIntervals;
            long pending = math.max(0L, pendingEnemies);
            return pending > long.MaxValue - demand ? long.MaxValue : pending + demand;
        }

        public static long AddTelemetry(long currentTotal, long amount)
        {
            long safeCurrent = math.max(0L, currentTotal);
            long safeAmount = math.max(0L, amount);
            return safeCurrent > long.MaxValue - safeAmount ? long.MaxValue : safeCurrent + safeAmount;
        }

        public static int ResolveDrainCount(
            long pendingEnemies,
            int zombiesAlive,
            int maxAliveZombies,
            int maxDrainPerFrame)
        {
            if (pendingEnemies <= 0 || maxDrainPerFrame <= 0)
                return 0;

            int available = maxAliveZombies > 0
                ? math.max(0, maxAliveZombies - math.max(0, zombiesAlive))
                : int.MaxValue;
            if (available <= 0)
                return 0;

            long drain = math.min(pendingEnemies, (long)math.min(available, maxDrainPerFrame));
            return (int)math.max(0L, drain);
        }
    }
}
