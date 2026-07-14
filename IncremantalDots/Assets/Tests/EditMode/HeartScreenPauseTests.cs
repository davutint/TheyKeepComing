using System;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartScreenPauseTests
    {
        [Test]
        public void PauseCoordinator_FirstLeasePausesAndDisposeRestoresExactState()
        {
            var backend = new FakeBackend
            {
                TimeScale = 0.35f,
                SimulationEnabled = true
            };
            var coordinator = new SimulationPauseCoordinator(backend);

            IDisposable lease = coordinator.Acquire("Heart");

            Assert.That(coordinator.IsPaused, Is.True);
            Assert.That(backend.TimeScale, Is.Zero);
            Assert.That(backend.SimulationEnabled, Is.False);

            lease.Dispose();

            Assert.That(coordinator.IsPaused, Is.False);
            Assert.That(backend.TimeScale, Is.EqualTo(0.35f));
            Assert.That(backend.SimulationEnabled, Is.True);
        }

        [Test]
        public void PauseCoordinator_NestedOwnersOnlyLastDisposeResumes()
        {
            var backend = new FakeBackend
            {
                TimeScale = 1f,
                SimulationEnabled = true
            };
            var coordinator = new SimulationPauseCoordinator(backend);
            IDisposable heart = coordinator.Acquire("Heart");
            IDisposable menu = coordinator.Acquire("PauseMenu");

            heart.Dispose();

            Assert.That(coordinator.ActiveLeaseCount, Is.EqualTo(1));
            Assert.That(backend.TimeScale, Is.Zero);
            Assert.That(backend.SimulationEnabled, Is.False);

            menu.Dispose();

            Assert.That(coordinator.ActiveLeaseCount, Is.Zero);
            Assert.That(backend.TimeScale, Is.EqualTo(1f));
            Assert.That(backend.SimulationEnabled, Is.True);
        }

        [Test]
        public void PauseCoordinator_EnforceRepairsExternalResumeWithoutLosingBaseline()
        {
            var backend = new FakeBackend
            {
                TimeScale = 0.75f,
                SimulationEnabled = true
            };
            var coordinator = new SimulationPauseCoordinator(backend);
            IDisposable lease = coordinator.Acquire("Heart");

            backend.TimeScale = 1f;
            backend.SimulationEnabled = true;
            coordinator.EnforcePausedState();

            Assert.That(backend.TimeScale, Is.Zero);
            Assert.That(backend.SimulationEnabled, Is.False);

            lease.Dispose();
            Assert.That(backend.TimeScale, Is.EqualTo(0.75f));
            Assert.That(backend.SimulationEnabled, Is.True);
        }

        [Test]
        public void PauseCoordinator_WhenSimulationGroupMissingStillRestoresTimeScale()
        {
            var backend = new FakeBackend
            {
                TimeScale = 0.5f,
                SimulationEnabled = true,
                HasSimulationState = false
            };
            var coordinator = new SimulationPauseCoordinator(backend);
            IDisposable lease = coordinator.Acquire("Heart");

            Assert.That(backend.TimeScale, Is.Zero);
            lease.Dispose();

            Assert.That(backend.TimeScale, Is.EqualTo(0.5f));
        }

        [Test]
        public void PauseCoordinator_BlankOwnerIsRejected()
        {
            var coordinator = new SimulationPauseCoordinator(new FakeBackend());
            Assert.Throws<ArgumentException>(() => coordinator.Acquire(" "));
        }

        [Test]
        public void HeartGraphLayout_UsesFourDeterministicCompassDirections()
        {
            Assert.That(
                HeartGraphLayoutUtility.GetPosition(HeartNodeBranch.Army, 2, 100f, 80f),
                Is.EqualTo(new Vector2(200f, 0f)));
            Assert.That(
                HeartGraphLayoutUtility.GetPosition(HeartNodeBranch.Defense, 2, 100f, 80f),
                Is.EqualTo(new Vector2(-200f, 0f)));
            Assert.That(
                HeartGraphLayoutUtility.GetPosition(HeartNodeBranch.Production, 2, 100f, 80f),
                Is.EqualTo(new Vector2(0f, 160f)));
            Assert.That(
                HeartGraphLayoutUtility.GetPosition(HeartNodeBranch.HeartMagic, 2, 100f, 80f),
                Is.EqualTo(new Vector2(0f, -160f)));
        }

        [Test]
        public void ArrowEconomy_HeartBonusesAffectRuntimeButNotPaidUpgradeLevels()
        {
            var tuning = MobileEconomyPriceTuningUtility.Default;
            var supply = new ArrowSupply
            {
                CapacityLevel = 2,
                EfficiencyLevel = 3,
                HeartCapacityBonus = 777,
                HeartEfficiencyBonus = 19
            };

            int capacityWithoutHeart = tuning.ArrowBaseCapacity
                                       + 2 * tuning.ArrowCapacityPerLevel;
            int efficiencyWithoutHeart = tuning.ArrowBaseArrowsPerWood
                                         + 3 * tuning.ArrowArrowsPerWoodPerEfficiencyLevel;

            Assert.That(ArrowEconomyUtility.GetCapacity(supply, tuning),
                Is.EqualTo(capacityWithoutHeart + 777));
            Assert.That(ArrowEconomyUtility.GetArrowsPerWood(supply, tuning),
                Is.EqualTo(efficiencyWithoutHeart + 19));
            Assert.That(ArrowEconomyUtility.GetUpgradeLevel(supply, ArrowUpgradeType.Capacity),
                Is.EqualTo(2));
            Assert.That(ArrowEconomyUtility.GetUpgradeLevel(supply, ArrowUpgradeType.Efficiency),
                Is.EqualTo(3));
        }

        [Test]
        public void HeartRuntimeSettings_CreateRequestCopiesAllAuthoredGenerationFields()
        {
            var settings = new HeartGraphRuntimeSettings
            {
                MinimumBranchDepth = 3,
                MaximumBranchDepth = 7,
                MaximumCrossLinks = 4,
                KeystonePairCount = 2,
                MaximumAttempts = 11,
                StandardRarityWeight = 8,
                RareRarityWeight = 3
            };

            HeartGraphGenerationRequest request = settings.CreateRequest(null, 91273u);

            Assert.That(request.Seed, Is.EqualTo(91273u));
            Assert.That(request.MinimumBranchDepth, Is.EqualTo(3));
            Assert.That(request.MaximumBranchDepth, Is.EqualTo(7));
            Assert.That(request.MaximumCrossLinks, Is.EqualTo(4));
            Assert.That(request.KeystonePairCount, Is.EqualTo(2));
            Assert.That(request.MaximumAttempts, Is.EqualTo(11));
            Assert.That(request.StandardRarityWeight, Is.EqualTo(8));
            Assert.That(request.RareRarityWeight, Is.EqualTo(3));
        }

        private sealed class FakeBackend : ISimulationPauseBackend
        {
            public float TimeScale { get; set; } = 1f;
            public bool SimulationEnabled { get; set; } = true;
            public bool HasSimulationState { get; set; } = true;

            public bool TryGetSimulationEnabled(out bool enabled)
            {
                enabled = SimulationEnabled;
                return HasSimulationState;
            }

            public void SetSimulationEnabled(bool enabled)
            {
                if (HasSimulationState)
                    SimulationEnabled = enabled;
            }
        }
    }
}
