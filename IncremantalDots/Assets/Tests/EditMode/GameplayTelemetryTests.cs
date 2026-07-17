using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class GameplayTelemetryTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_createdObjects[i]);
            _createdObjects.Clear();
        }

        [Test]
        public void RunStartedFactory_CapturesSortedMetaStartingResourcesAndHeartIdentity()
        {
            MetaUpgradeSO zeta = CreateUpgrade("zeta", 1);
            MetaUpgradeSO alpha = CreateUpgrade("alpha", 1);
            MetaUpgradeCatalogSO catalog = ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
            _createdObjects.Add(catalog);
            catalog.Upgrades = new[] { zeta, alpha };

            var meta = new MetaProgressState
            {
                Version = MetaProgressState.CurrentVersion,
                Upgrades = new List<MetaUpgradeLevel>
                {
                    new MetaUpgradeLevel { Id = "zeta", Level = 7 },
                    new MetaUpgradeLevel { Id = "alpha", Level = 3 }
                }
            };
            var heart = new HeartRuntimeTuningTelemetry(
                true, true, true, string.Empty, 11L, 0d, 0d,
                4, 9, 12345u, 20, 19, 4, 0, 0);

            RunStartedTelemetryPayload payload = RunStartedTelemetryFactory.Create(
                catalog,
                meta,
                new ResourceData { Wood = 160, Stone = 80, Iron = 50, Food = 120 },
                new ArrowSupply { Current = 175 },
                200,
                new PopulationState { Total = 60, Capacity = 75 },
                heart);

            Assert.That(payload.MetaCatalogConfigured, Is.True);
            Assert.That(payload.MetaCatalogDefinitionCount, Is.EqualTo(2));
            Assert.That(payload.MetaLevels.Count, Is.EqualTo(2));
            Assert.That(payload.MetaLevels[0].UpgradeId, Is.EqualTo("alpha"));
            Assert.That(payload.MetaLevels[0].Level, Is.EqualTo(3));
            Assert.That(payload.MetaLevels[1].UpgradeId, Is.EqualTo("zeta"));
            Assert.That(payload.MetaLevels[1].Level, Is.EqualTo(7));
            Assert.That(payload.StartingResources.Wood, Is.EqualTo(160));
            Assert.That(payload.StartingResources.Arrows, Is.EqualTo(175));
            Assert.That(payload.StartingResources.ArrowCapacity, Is.EqualTo(200));
            Assert.That(payload.StartingResources.GraveEssence, Is.EqualTo(11L));
            Assert.That(payload.StartingResources.Population, Is.EqualTo(60));
            Assert.That(payload.StartingResources.PopulationCapacity, Is.EqualTo(75));
            Assert.That(payload.Heart.GraphReady, Is.True);
            Assert.That(payload.Heart.GraphVersion, Is.EqualTo(4));
            Assert.That(payload.Heart.CatalogVersion, Is.EqualTo(9));
            Assert.That(payload.Heart.Seed, Is.EqualTo(12345u));
        }

        [Test]
        public void TryEmitRunStarted_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            var payload = new RunStartedTelemetryPayload
            {
                MetaProgressVersion = 3,
                MetaCatalogConfigured = true,
                MetaCatalogDefinitionCount = 1,
                MetaLevels = new List<TelemetryMetaLevelSnapshot>
                {
                    new TelemetryMetaLevelSnapshot { UpgradeId = "start_wood", Level = 2 }
                },
                StartingResources = new TelemetryStartingResources
                {
                    Wood = 310,
                    Stone = 80,
                    Iron = 50,
                    Food = 120,
                    Arrows = 200,
                    ArrowCapacity = 200,
                    Population = 60,
                    PopulationCapacity = 60
                },
                Heart = new TelemetryHeartGraphIdentity
                {
                    CatalogConfigured = false,
                    RuntimeAttempted = true,
                    GraphReady = false
                }
            };

            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitRunStarted(
                    " run_contract_01 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_contract_01"));
                Assert.That(emitted.EventName, Is.EqualTo("run_started"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                Assert.That(envelope.EventName, Is.EqualTo("run_started"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(envelope.RunId, Is.EqualTo("run_contract_01"));
                RunStartedTelemetryPayload decoded =
                    JsonUtility.FromJson<RunStartedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(decoded.MetaLevels[0].UpgradeId, Is.EqualTo("start_wood"));
                Assert.That(decoded.StartingResources.Wood, Is.EqualTo(310));
                Assert.That(decoded.Heart.RuntimeAttempted, Is.True);
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitRunStarted_RejectsDuplicateMetaIdentityAndIncompleteReadyGraph()
        {
            var duplicateMeta = new RunStartedTelemetryPayload
            {
                MetaLevels = new List<TelemetryMetaLevelSnapshot>
                {
                    new TelemetryMetaLevelSnapshot { UpgradeId = "same", Level = 1 },
                    new TelemetryMetaLevelSnapshot { UpgradeId = "same", Level = 2 }
                }
            };
            Assert.That(GameplayTelemetry.TryEmitRunStarted(
                "run_invalid_meta", duplicateMeta, out _, out string duplicateError), Is.False);
            Assert.That(duplicateError, Does.Contain("duplicate"));

            var invalidHeart = new RunStartedTelemetryPayload
            {
                MetaLevels = new List<TelemetryMetaLevelSnapshot>(),
                Heart = new TelemetryHeartGraphIdentity
                {
                    CatalogConfigured = true,
                    RuntimeAttempted = true,
                    GraphReady = true,
                    GraphVersion = 1,
                    CatalogVersion = 1,
                    Seed = 0u
                }
            };
            Assert.That(GameplayTelemetry.TryEmitRunStarted(
                "run_invalid_heart", invalidHeart, out _, out string heartError), Is.False);
            Assert.That(heartError, Does.Contain("Heart graph"));
        }

        [Test]
        public void PhaseChangedFactory_CapturesCanonicalDayPhaseAndHordeSnapshot()
        {
            PhaseChangedTelemetryPayload payload = PhaseChangedTelemetryFactory.Create(
                new ContinuousSiegeCycleData
                {
                    Enabled = true,
                    CycleIndex = 4,
                    Phase = SiegeCyclePhase.Night
                },
                new WaveStateData { ZombiesAlive = 237 },
                new ContinuousSpawnBudgetData { PendingEnemies = 9_123L });

            Assert.That(payload.Day, Is.EqualTo(5));
            Assert.That(payload.Phase, Is.EqualTo("night"));
            Assert.That(payload.AliveEnemies, Is.EqualTo(237));
            Assert.That(payload.SpawnBacklog, Is.EqualTo(9_123L));
        }

        [Test]
        public void TryEmitPhaseChanged_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            var payload = new PhaseChangedTelemetryPayload
            {
                Day = 8,
                Phase = "dusk",
                AliveEnemies = 640,
                SpawnBacklog = 2_048L
            };

            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitPhaseChanged(
                    " run_phase_08 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_phase_08"));
                Assert.That(emitted.EventName, Is.EqualTo("phase_changed"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                Assert.That(envelope.EventName, Is.EqualTo("phase_changed"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                PhaseChangedTelemetryPayload decoded =
                    JsonUtility.FromJson<PhaseChangedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(decoded.Day, Is.EqualTo(8));
                Assert.That(decoded.Phase, Is.EqualTo("dusk"));
                Assert.That(decoded.AliveEnemies, Is.EqualTo(640));
                Assert.That(decoded.SpawnBacklog, Is.EqualTo(2_048L));
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitPhaseChanged_RejectsInvalidDayPhaseAndHordeState()
        {
            var invalidDay = new PhaseChangedTelemetryPayload
            {
                Day = 0,
                Phase = "day"
            };
            Assert.That(GameplayTelemetry.TryEmitPhaseChanged(
                "run_invalid_day", invalidDay, out _, out string dayError), Is.False);
            Assert.That(dayError, Does.Contain("horde snapshot"));

            var invalidPhase = new PhaseChangedTelemetryPayload
            {
                Day = 1,
                Phase = "storm"
            };
            Assert.That(GameplayTelemetry.TryEmitPhaseChanged(
                "run_invalid_phase", invalidPhase, out _, out string phaseError), Is.False);
            Assert.That(phaseError, Does.Contain("phase kimligi"));

            var invalidBacklog = new PhaseChangedTelemetryPayload
            {
                Day = 1,
                Phase = "night",
                SpawnBacklog = -1L
            };
            Assert.That(GameplayTelemetry.TryEmitPhaseChanged(
                "run_invalid_backlog", invalidBacklog, out _, out string backlogError), Is.False);
            Assert.That(backlogError, Does.Contain("horde snapshot"));
        }

        [Test]
        public void ResourceSpentFactory_ExpandsMultiResourceCostInCanonicalOrder()
        {
            List<ResourceSpentTelemetryPayload> payloads =
                ResourceSpentTelemetryFactory.Create(
                    new ResourceCost(120, 0, 35, 20),
                    ResourceSpentTelemetryContract.ArcherRapidBuy,
                    0,
                    7);

            Assert.That(payloads.Count, Is.EqualTo(3));
            Assert.That(payloads[0].Resource, Is.EqualTo("wood"));
            Assert.That(payloads[0].Amount, Is.EqualTo(120L));
            Assert.That(payloads[1].Resource, Is.EqualTo("iron"));
            Assert.That(payloads[1].Amount, Is.EqualTo(35L));
            Assert.That(payloads[2].Resource, Is.EqualTo("food"));
            Assert.That(payloads[2].Amount, Is.EqualTo(20L));
            for (int i = 0; i < payloads.Count; i++)
            {
                Assert.That(payloads[i].PurchaseType,
                    Is.EqualTo(ResourceSpentTelemetryContract.ArcherRapidBuy));
                Assert.That(payloads[i].ResultingLevel, Is.Zero);
                Assert.That(payloads[i].ResultingCount, Is.EqualTo(7));
            }
        }

        [Test]
        public void TryEmitResourceSpent_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            ResourceSpentTelemetryPayload payload =
                ResourceSpentTelemetryFactory.CreateSingle(
                    ResourceSpentTelemetryContract.GraveEssence,
                    4_250L,
                    ResourceSpentTelemetryContract.HeartNode,
                    12,
                    0);

            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitResourceSpent(
                    " run_spend_12 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_spend_12"));
                Assert.That(emitted.EventName, Is.EqualTo("resource_spent"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                ResourceSpentTelemetryPayload decoded =
                    JsonUtility.FromJson<ResourceSpentTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("resource_spent"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.Resource, Is.EqualTo("grave_essence"));
                Assert.That(decoded.Amount, Is.EqualTo(4_250L));
                Assert.That(decoded.PurchaseType, Is.EqualTo("heart_node"));
                Assert.That(decoded.ResultingLevel, Is.EqualTo(12));
                Assert.That(decoded.ResultingCount, Is.Zero);
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitResourceSpent_RejectsInvalidIdentityAmountAndResult()
        {
            var unknownResource = new ResourceSpentTelemetryPayload
            {
                Resource = "gold",
                Amount = 10L,
                PurchaseType = ResourceSpentTelemetryContract.BedCapacity,
                ResultingCount = 61
            };
            Assert.That(GameplayTelemetry.TryEmitResourceSpent(
                "run_invalid_resource", unknownResource, out _, out string resourceError),
                Is.False);
            Assert.That(resourceError, Does.Contain("resource kimligi"));

            var invalidAmount = new ResourceSpentTelemetryPayload
            {
                Resource = ResourceSpentTelemetryContract.Wood,
                Amount = 0L,
                PurchaseType = ResourceSpentTelemetryContract.BedCapacity,
                ResultingCount = 61
            };
            Assert.That(GameplayTelemetry.TryEmitResourceSpent(
                "run_invalid_amount", invalidAmount, out _, out string amountError), Is.False);
            Assert.That(amountError, Does.Contain("amount"));

            var missingResult = new ResourceSpentTelemetryPayload
            {
                Resource = ResourceSpentTelemetryContract.Iron,
                Amount = 25L,
                PurchaseType = "legacy_upgrade"
            };
            Assert.That(GameplayTelemetry.TryEmitResourceSpent(
                "run_invalid_purchase", missingResult, out _, out string purchaseError), Is.False);
            Assert.That(purchaseError, Does.Contain("purchase type"));

            missingResult.PurchaseType = ResourceSpentTelemetryContract.ArrowCapacityUpgrade;
            Assert.That(GameplayTelemetry.TryEmitResourceSpent(
                "run_missing_result", missingResult, out _, out string resultError), Is.False);
            Assert.That(resultError, Does.Contain("resulting level/count"));
        }

        private MetaUpgradeSO CreateUpgrade(string id, int maxLevel)
        {
            MetaUpgradeSO upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            _createdObjects.Add(upgrade);
            upgrade.Id = id;
            upgrade.MaxLevel = maxLevel;
            return upgrade;
        }
    }
}
