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

        [Test]
        public void NightClearance_HoldsForEitherBacklogOrLivingEnemy()
        {
            const float nightEnd = 55f;

            Assert.That(ContinuousSpawnBudgetUtility.ShouldHoldAtNightEnd(
                SiegeCyclePhase.Night, 55f, nightEnd, 1L, 0), Is.True);
            Assert.That(ContinuousSpawnBudgetUtility.ShouldHoldAtNightEnd(
                SiegeCyclePhase.Night, 58f, nightEnd, 0L, 1), Is.True);
            Assert.That(ContinuousSpawnBudgetUtility.ShouldHoldAtNightEnd(
                SiegeCyclePhase.Night, 58f, nightEnd, 0L, 0), Is.False);
            Assert.That(ContinuousSpawnBudgetUtility.ShouldHoldAtNightEnd(
                SiegeCyclePhase.Day, 2f, nightEnd, 3L, 0), Is.True,
                "Legacy non-night backlog Night clearance'a alinmali.");
        }

        [Test]
        public void SpawnDemand_IsGeneratedOnlyDuringTimedNight_ButClearanceCanDrain()
        {
            var day = new ContinuousSiegeCycleData
            {
                Enabled = true,
                Phase = SiegeCyclePhase.Day,
                PhaseProgress01 = 0.5f,
                SpawnIntensityMultiplier = 0f
            };
            var night = day;
            night.Phase = SiegeCyclePhase.Night;
            night.PhaseProgress01 = 0.5f;
            night.SpawnIntensityMultiplier = 1f;
            var clearance = night;
            clearance.PhaseProgress01 = 1f;
            clearance.SpawnIntensityMultiplier = 0f;

            Assert.That(ContinuousSpawnBudgetUtility.CanGenerateDemand(day), Is.False);
            Assert.That(ContinuousSpawnBudgetUtility.CanDrainPending(day), Is.False);
            Assert.That(ContinuousSpawnBudgetUtility.CanGenerateDemand(night), Is.True);
            Assert.That(ContinuousSpawnBudgetUtility.CanDrainPending(night), Is.True);
            Assert.That(ContinuousSpawnBudgetUtility.CanGenerateDemand(clearance), Is.False);
            Assert.That(ContinuousSpawnBudgetUtility.CanDrainPending(clearance), Is.True);
            Assert.That(ContinuousSpawnBudgetUtility.IsNightClearance(clearance), Is.True);
        }
    }
}
