using NUnit.Framework;

namespace DeadWalls.Tests
{
    public class ContinuousSpawnBudgetUtilityTests
    {
        [Test]
        public void DayBaseAndPhaseMultiplier_ResolveAsSeparateBudgetChannels()
        {
            int demand = ContinuousSpawnBudgetUtility.ResolveDemandPerInterval(
                baseBatch: 2,
                growthPerCycle: 0.15f,
                currentWave: 5,
                dayQuantityMultiplier: 1.5f,
                phaseIntensityMultiplier: 0.2f,
                maxBatch: 16);
            float interval = ContinuousSpawnBudgetUtility.ResolveEffectiveInterval(
                dayBaseInterval: 0.5f,
                phaseIntensityMultiplier: 0.2f,
                minInterval: 0.1f);

            Assert.That(demand, Is.EqualTo(1));
            Assert.That(interval, Is.EqualTo(2.5f).Within(0.0001f));

            float nextDayBase = 0.4f;
            float nextDayInterval = ContinuousSpawnBudgetUtility.ResolveEffectiveInterval(
                nextDayBase, phaseIntensityMultiplier: 0.55f, minInterval: 0.1f);
            Assert.That(nextDayBase, Is.EqualTo(0.4f),
                "Dawn phase multiplier'i sonraki gunun day base interval'ini degistirmemeli.");
            Assert.That(nextDayInterval, Is.EqualTo(0.4f / 0.55f).Within(0.0001f));
        }

        [Test]
        public void ElapsedIntervals_AccumulateEveryDemandWhileCapIsBlocked()
        {
            float timerAfterDelta = -1.1f;
            float interval = 0.5f;
            int elapsed = ContinuousSpawnBudgetUtility.CountElapsedIntervals(timerAfterDelta, interval);
            long pending = ContinuousSpawnBudgetUtility.AddDemand(
                pendingEnemies: 7,
                demandPerInterval: 3,
                elapsedIntervals: elapsed);
            float nextTimer = ContinuousSpawnBudgetUtility.AdvanceTimer(
                timerAfterDelta, interval, elapsed);

            Assert.That(elapsed, Is.EqualTo(3));
            Assert.That(pending, Is.EqualTo(16));
            Assert.That(nextTimer, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void Drain_RespectsAliveCapAndPerFrameBatchLimit()
        {
            Assert.That(ContinuousSpawnBudgetUtility.ResolveDrainCount(
                pendingEnemies: 40,
                zombiesAlive: 10,
                maxAliveZombies: 10,
                maxDrainPerFrame: 16), Is.Zero);

            Assert.That(ContinuousSpawnBudgetUtility.ResolveDrainCount(
                pendingEnemies: 40,
                zombiesAlive: 4,
                maxAliveZombies: 10,
                maxDrainPerFrame: 16), Is.EqualTo(6));

            Assert.That(ContinuousSpawnBudgetUtility.ResolveDrainCount(
                pendingEnemies: 40,
                zombiesAlive: 0,
                maxAliveZombies: 100,
                maxDrainPerFrame: 16), Is.EqualTo(16));
        }
    }
}
