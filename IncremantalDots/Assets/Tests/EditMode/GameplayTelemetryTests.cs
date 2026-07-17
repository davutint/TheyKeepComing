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
