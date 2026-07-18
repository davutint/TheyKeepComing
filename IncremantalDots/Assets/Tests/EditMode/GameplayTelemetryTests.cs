using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
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

        [Test]
        public void ArcherChangedFactory_CreatesCanonicalBuyAndRetrainTransitions()
        {
            ArcherChangedTelemetryPayload buy = ArcherChangedTelemetryFactory.CreateBuy(
                ArcherType.Frost,
                73);
            Assert.That(buy.ChangeType, Is.EqualTo("buy"));
            Assert.That(buy.TypeFrom, Is.EqualTo("none"));
            Assert.That(buy.TypeTo, Is.EqualTo("frost"));
            Assert.That(buy.TotalCapUsage, Is.EqualTo(73));

            ArcherChangedTelemetryPayload retrain =
                ArcherChangedTelemetryFactory.CreateRetrain(
                    ArcherType.Rapid,
                    ArcherCapacityUtility.MaxTotalArchers);
            Assert.That(retrain.ChangeType, Is.EqualTo("retrain"));
            Assert.That(retrain.TypeFrom, Is.EqualTo("basic"));
            Assert.That(retrain.TypeTo, Is.EqualTo("rapid"));
            Assert.That(retrain.TotalCapUsage,
                Is.EqualTo(ArcherCapacityUtility.MaxTotalArchers));
        }

        [Test]
        public void TryEmitArcherChanged_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            ArcherChangedTelemetryPayload payload =
                ArcherChangedTelemetryFactory.CreateRetrain(ArcherType.Frost, 994);
            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitArcherChanged(
                    " run_archer_994 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_archer_994"));
                Assert.That(emitted.EventName, Is.EqualTo("archer_changed"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                ArcherChangedTelemetryPayload decoded =
                    JsonUtility.FromJson<ArcherChangedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("archer_changed"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.ChangeType, Is.EqualTo("retrain"));
                Assert.That(decoded.TypeFrom, Is.EqualTo("basic"));
                Assert.That(decoded.TypeTo, Is.EqualTo("frost"));
                Assert.That(decoded.TotalCapUsage, Is.EqualTo(994));
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitArcherChanged_RejectsInvalidTransitionsAndCapUsage()
        {
            var invalidBuy = new ArcherChangedTelemetryPayload
            {
                ChangeType = ArcherChangedTelemetryContract.Buy,
                TypeFrom = ArcherChangedTelemetryContract.Basic,
                TypeTo = ArcherChangedTelemetryContract.Rapid,
                TotalCapUsage = 10
            };
            Assert.That(GameplayTelemetry.TryEmitArcherChanged(
                "run_invalid_buy", invalidBuy, out _, out string buyError), Is.False);
            Assert.That(buyError, Does.Contain("buy type transition"));

            var invalidRetrain = new ArcherChangedTelemetryPayload
            {
                ChangeType = ArcherChangedTelemetryContract.Retrain,
                TypeFrom = ArcherChangedTelemetryContract.Basic,
                TypeTo = ArcherChangedTelemetryContract.Basic,
                TotalCapUsage = 10
            };
            Assert.That(GameplayTelemetry.TryEmitArcherChanged(
                "run_invalid_retrain", invalidRetrain, out _, out string retrainError), Is.False);
            Assert.That(retrainError, Does.Contain("retrain type transition"));

            ArcherChangedTelemetryPayload overCap = ArcherChangedTelemetryFactory.CreateBuy(
                ArcherType.Basic,
                ArcherCapacityUtility.MaxTotalArchers + 1);
            Assert.That(GameplayTelemetry.TryEmitArcherChanged(
                "run_over_cap", overCap, out _, out string capError), Is.False);
            Assert.That(capError, Does.Contain("total cap usage"));
        }

        [Test]
        public void HeartNodeBoughtFactory_CreatesCanonicalCommittedPurchaseSnapshot()
        {
            var result = new HeartPurchaseResult
            {
                Quote = new HeartPurchaseQuote
                {
                    NodeId = "army_repeatable",
                    PreviousLevel = 2,
                    LevelsToBuy = 10,
                    NewLevel = 12,
                    TotalGraveEssenceCost = 875L
                },
                NodeDepth = 4,
                FailureReason = HeartPurchaseFailureReason.None
            };
            result.NewlyRevealedNodeIds.Add("army_child_a");
            result.NewlyRevealedNodeIds.Add("army_child_b");

            HeartNodeBoughtTelemetryPayload payload =
                HeartNodeBoughtTelemetryFactory.Create(result);
            Assert.That(payload.NodeId, Is.EqualTo("army_repeatable"));
            Assert.That(payload.Level, Is.EqualTo(12));
            Assert.That(payload.Depth, Is.EqualTo(4));
            Assert.That(payload.Cost, Is.EqualTo(875L));
            Assert.That(payload.RevealedChildren, Is.EqualTo(2));
        }

        [Test]
        public void TryEmitHeartNodeBought_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            var payload = new HeartNodeBoughtTelemetryPayload
            {
                NodeId = "defense_wall_core",
                Level = 3,
                Depth = 2,
                Cost = 240L,
                RevealedChildren = 1
            };
            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                    " run_heart_240 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_heart_240"));
                Assert.That(emitted.EventName, Is.EqualTo("heart_node_bought"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                HeartNodeBoughtTelemetryPayload decoded =
                    JsonUtility.FromJson<HeartNodeBoughtTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("heart_node_bought"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.NodeId, Is.EqualTo("defense_wall_core"));
                Assert.That(decoded.Level, Is.EqualTo(3));
                Assert.That(decoded.Depth, Is.EqualTo(2));
                Assert.That(decoded.Cost, Is.EqualTo(240L));
                Assert.That(decoded.RevealedChildren, Is.EqualTo(1));
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitHeartNodeBought_RejectsInvalidNodeLevelDepthCostAndRevealCount()
        {
            var payload = new HeartNodeBoughtTelemetryPayload
            {
                NodeId = "heart_valid",
                Level = 1,
                Depth = 1,
                Cost = 10L,
                RevealedChildren = 0
            };

            payload.NodeId = " ";
            Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                "run_invalid_node", payload, out _, out string nodeError), Is.False);
            Assert.That(nodeError, Does.Contain("node kimligi"));

            payload.NodeId = "heart_valid";
            payload.Level = 0;
            Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                "run_invalid_level", payload, out _, out string levelError), Is.False);
            Assert.That(levelError, Does.Contain("level"));

            payload.Level = 1;
            payload.Depth = 0;
            Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                "run_invalid_depth", payload, out _, out string depthError), Is.False);
            Assert.That(depthError, Does.Contain("depth"));

            payload.Depth = 1;
            payload.Cost = 0L;
            Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                "run_invalid_cost", payload, out _, out string costError), Is.False);
            Assert.That(costError, Does.Contain("cost"));

            payload.Cost = 10L;
            payload.RevealedChildren = -1;
            Assert.That(GameplayTelemetry.TryEmitHeartNodeBought(
                "run_invalid_reveal", payload, out _, out string revealError), Is.False);
            Assert.That(revealError, Does.Contain("revealed children"));
        }

        [Test]
        public void CouncilResolvedFactory_CapturesCanonicalEffectsAndResolvedNightDelta()
        {
            var councilEvent = new ComposedCouncilEvent
            {
                TemplateId = " night_bargain "
            };
            var option = new ComposedCouncilOption
            {
                Effects = new List<ComposedCouncilEffect>
                {
                    new ComposedCouncilEffect
                    {
                        Kind = CouncilEffectKind.GainResource,
                        Resource = EconomyFocusType.Wood,
                        Amount = 240
                    },
                    new ComposedCouncilEffect
                    {
                        Kind = CouncilEffectKind.NextNightSpawnDelta,
                        Resource = EconomyFocusType.Balanced,
                        Rate = 100f
                    }
                }
            };

            CouncilResolvedTelemetryPayload selected =
                CouncilResolvedTelemetryFactory.Create(
                    9,
                    councilEvent,
                    option,
                    CouncilResolvedTelemetryContract.OptionB);
            Assert.That(selected.Day, Is.EqualTo(9));
            Assert.That(selected.TemplateId, Is.EqualTo("night_bargain"));
            Assert.That(selected.Resolution, Is.EqualTo("option_b"));
            Assert.That(selected.Effects, Has.Count.EqualTo(2));
            Assert.That(selected.Effects[0].Kind, Is.EqualTo("gain_resource"));
            Assert.That(selected.Effects[0].Resource, Is.EqualTo("wood"));
            Assert.That(selected.Effects[0].Amount, Is.EqualTo(240));
            Assert.That(selected.Effects[1].Kind, Is.EqualTo("next_night_spawn_delta"));
            Assert.That(selected.Effects[1].Resource, Is.EqualTo("none"));
            Assert.That(selected.NextNightDelta,
                Is.EqualTo(CouncilEffectGuardUtility.MaximumNightCountMultiplier - 1f)
                    .Within(0.0001f));

            CouncilResolvedTelemetryPayload expired =
                CouncilResolvedTelemetryFactory.CreateExpired(12, councilEvent);
            Assert.That(expired.Resolution, Is.EqualTo("expired"));
            Assert.That(expired.Effects, Is.Empty);
            Assert.That(expired.NextNightDelta, Is.Zero);
        }

        [Test]
        public void TryEmitCouncilResolved_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            var payload = new CouncilResolvedTelemetryPayload
            {
                Day = 6,
                TemplateId = "granary_request",
                Resolution = CouncilResolvedTelemetryContract.OptionA,
                Effects = new List<CouncilResolvedTelemetryEffect>
                {
                    new CouncilResolvedTelemetryEffect
                    {
                        Kind = CouncilResolvedTelemetryContract.GainPopulation,
                        Resource = CouncilResolvedTelemetryContract.None,
                        Amount = 4
                    }
                },
                NextNightDelta = 0f
            };
            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitCouncilResolved(
                    " run_council_06 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_council_06"));
                Assert.That(emitted.EventName, Is.EqualTo("council_resolved"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                CouncilResolvedTelemetryPayload decoded =
                    JsonUtility.FromJson<CouncilResolvedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("council_resolved"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.Day, Is.EqualTo(6));
                Assert.That(decoded.TemplateId, Is.EqualTo("granary_request"));
                Assert.That(decoded.Resolution, Is.EqualTo("option_a"));
                Assert.That(decoded.Effects, Has.Count.EqualTo(1));
                Assert.That(decoded.Effects[0].Kind, Is.EqualTo("gain_population"));
                Assert.That(decoded.Effects[0].Amount, Is.EqualTo(4));
                Assert.That(decoded.NextNightDelta, Is.Zero);
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitCouncilResolved_RejectsInvalidIdentityExpiredEffectsAndNightDelta()
        {
            var payload = new CouncilResolvedTelemetryPayload
            {
                Day = 3,
                TemplateId = "valid_template",
                Resolution = CouncilResolvedTelemetryContract.Expired,
                Effects = new List<CouncilResolvedTelemetryEffect>(),
                NextNightDelta = 0f
            };

            payload.Day = 0;
            Assert.That(GameplayTelemetry.TryEmitCouncilResolved(
                "run_invalid_day", payload, out _, out string dayError), Is.False);
            Assert.That(dayError, Does.Contain("day veya template"));

            payload.Day = 3;
            payload.Effects.Add(new CouncilResolvedTelemetryEffect
            {
                Kind = CouncilResolvedTelemetryContract.GainResource,
                Resource = CouncilResolvedTelemetryContract.Wood,
                Amount = 10
            });
            Assert.That(GameplayTelemetry.TryEmitCouncilResolved(
                "run_expired_effect", payload, out _, out string expiredError), Is.False);
            Assert.That(expiredError, Does.Contain("expired sonucu"));

            payload.Resolution = CouncilResolvedTelemetryContract.OptionA;
            payload.Effects.Clear();
            payload.Effects.Add(new CouncilResolvedTelemetryEffect
            {
                Kind = CouncilResolvedTelemetryContract.NextNightSpawnDelta,
                Resource = CouncilResolvedTelemetryContract.None,
                Rate = 0.25f
            });
            payload.NextNightDelta = 0f;
            Assert.That(GameplayTelemetry.TryEmitCouncilResolved(
                "run_wrong_delta", payload, out _, out string deltaError), Is.False);
            Assert.That(deltaError, Does.Contain("next-night delta"));

            payload.NextNightDelta =
                CouncilEffectGuardUtility.ResolveNightCountMultiplier(0.25f) - 1f;
            payload.Effects[0].Kind = "boss_spawn";
            Assert.That(GameplayTelemetry.TryEmitCouncilResolved(
                "run_unknown_effect", payload, out _, out string effectError), Is.False);
            Assert.That(effectError, Does.Contain("Effects[0]"));
        }

        [Test]
        public void AbilityCastFactory_CapturesCanonicalAbilityResultSnapshots()
        {
            AbilityCastTelemetryPayload fireball = AbilityCastTelemetryFactory.CreateFireball(
                SiegeCyclePhase.Dusk,
                45f);
            Assert.That(fireball.Ability, Is.EqualTo("fireball"));
            Assert.That(fireball.Phase, Is.EqualTo("dusk"));
            Assert.That(fireball.Cooldown, Is.EqualTo(45f));
            Assert.That(fireball.Targets, Is.Zero);
            Assert.That(fireball.Repair, Is.Zero);

            AbilityCastTelemetryPayload rally = AbilityCastTelemetryFactory.CreateRally(
                SiegeCyclePhase.Day,
                60f,
                173);
            Assert.That(rally.Ability, Is.EqualTo("rally"));
            Assert.That(rally.Phase, Is.EqualTo("day"));
            Assert.That(rally.Cooldown, Is.EqualTo(60f));
            Assert.That(rally.Targets, Is.EqualTo(173));
            Assert.That(rally.Repair, Is.Zero);

            AbilityCastTelemetryPayload repair =
                AbilityCastTelemetryFactory.CreateEmergencyRepair(
                    SiegeCyclePhase.Night,
                    120f,
                    240f);
            Assert.That(repair.Ability, Is.EqualTo("emergency_repair"));
            Assert.That(repair.Phase, Is.EqualTo("night"));
            Assert.That(repair.Cooldown, Is.EqualTo(120f));
            Assert.That(repair.Targets, Is.EqualTo(1));
            Assert.That(repair.Repair, Is.EqualTo(240f));
        }

        [Test]
        public void TryEmitAbilityCast_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            AbilityCastTelemetryPayload payload = AbilityCastTelemetryFactory.CreateRally(
                SiegeCyclePhase.Dawn,
                60f,
                1000);
            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                    " run_ability_1000 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_ability_1000"));
                Assert.That(emitted.EventName, Is.EqualTo("ability_cast"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                AbilityCastTelemetryPayload decoded =
                    JsonUtility.FromJson<AbilityCastTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("ability_cast"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.Ability, Is.EqualTo("rally"));
                Assert.That(decoded.Phase, Is.EqualTo("dawn"));
                Assert.That(decoded.Cooldown, Is.EqualTo(60f));
                Assert.That(decoded.Targets, Is.EqualTo(1000));
                Assert.That(decoded.Repair, Is.Zero);
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitAbilityCast_RejectsInvalidIdentityPhaseCooldownAndResultShape()
        {
            var payload = new AbilityCastTelemetryPayload
            {
                Ability = AbilityCastTelemetryContract.Fireball,
                Phase = "day",
                Cooldown = 45f,
                Targets = 0,
                Repair = 0f
            };

            payload.Ability = "arrow_storm";
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_invalid_ability", payload, out _, out string abilityError), Is.False);
            Assert.That(abilityError, Does.Contain("ability kimligi"));

            payload.Ability = AbilityCastTelemetryContract.Fireball;
            payload.Phase = "blood_moon";
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_invalid_phase", payload, out _, out string phaseError), Is.False);
            Assert.That(phaseError, Does.Contain("phase kimligi"));

            payload.Phase = "night";
            payload.Cooldown = 0f;
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_invalid_cooldown", payload, out _, out string cooldownError), Is.False);
            Assert.That(cooldownError, Does.Contain("cooldown"));

            payload.Cooldown = 45f;
            payload.Targets = 1;
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_speculative_fireball", payload, out _, out string fireballError), Is.False);
            Assert.That(fireballError, Does.Contain("speculative"));

            payload.Ability = AbilityCastTelemetryContract.Rally;
            payload.Targets = ArcherCapacityUtility.MaxTotalArchers + 1;
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_invalid_rally", payload, out _, out string rallyError), Is.False);
            Assert.That(rallyError, Does.Contain("rally"));

            payload.Ability = AbilityCastTelemetryContract.EmergencyRepair;
            payload.Targets = 1;
            payload.Repair = 0f;
            Assert.That(GameplayTelemetry.TryEmitAbilityCast(
                "run_invalid_repair", payload, out _, out string repairError), Is.False);
            Assert.That(repairError, Does.Contain("emergency repair"));
        }

        [Test]
        public void WallRepairedFactory_CapturesCanonicalRepairResultSnapshot()
        {
            WallRepairedTelemetryPayload payload = WallRepairedTelemetryFactory.Create(
                SiegeCyclePhase.Dusk,
                37,
                420f,
                650f);

            Assert.That(payload.Phase, Is.EqualTo("dusk"));
            Assert.That(payload.StoneCost, Is.EqualTo(37));
            Assert.That(payload.HpBefore, Is.EqualTo(420f));
            Assert.That(payload.HpAfter, Is.EqualTo(650f));
        }

        [Test]
        public void RunTelemetryAccumulator_TracksPeakAndAggregatesChronologicalWallBuckets()
        {
            using var world = new World("RunTelemetryAccumulatorTests");
            EntityManager entityManager = world.EntityManager;
            Entity entity = entityManager.CreateEntity(typeof(RunTelemetryData));
            DynamicBuffer<RunWallDamageTelemetryElement> timeline =
                entityManager.AddBuffer<RunWallDamageTelemetryElement>(entity);

            RunTelemetryData telemetry = default;
            RunTelemetryAccumulator.ObserveEnemyCount(ref telemetry, 1_500);
            RunTelemetryAccumulator.ObserveEnemyCount(ref telemetry, 700);
            RunTelemetryAccumulator.RecordWallDamage(
                timeline, 1, SiegeCyclePhase.Night, 45.5f);
            RunTelemetryAccumulator.RecordWallDamage(
                timeline, 1, SiegeCyclePhase.Night, 4.5f);
            RunTelemetryAccumulator.RecordWallDamage(
                timeline, 2, SiegeCyclePhase.Dusk, 12f);

            Assert.That(telemetry.PeakEnemies, Is.EqualTo(1_500));
            Assert.That(timeline.Length, Is.EqualTo(2));
            Assert.That(timeline[0].Day, Is.EqualTo(1));
            Assert.That(timeline[0].Phase, Is.EqualTo(SiegeCyclePhase.Night));
            Assert.That(timeline[0].Damage, Is.EqualTo(50f).Within(0.001f));
            Assert.That(timeline[1].Day, Is.EqualTo(2));
            Assert.That(timeline[1].Phase, Is.EqualTo(SiegeCyclePhase.Dusk));
            Assert.That(timeline[1].Damage, Is.EqualTo(12f).Within(0.001f));
        }

        [Test]
        public void RunEndedFactory_CapturesFinalSummaryAndClonesWallDamageTimeline()
        {
            var source = new List<RunEndedWallDamageTelemetryEntry>
            {
                new RunEndedWallDamageTelemetryEntry
                {
                    Day = 1,
                    Phase = "night",
                    Damage = 150f
                },
                new RunEndedWallDamageTelemetryEntry
                {
                    Day = 2,
                    Phase = "dusk",
                    Damage = 25f
                }
            };

            RunEndedTelemetryPayload payload = RunEndedTelemetryFactory.Create(
                3,
                4_200,
                2_048,
                88,
                source,
                640,
                475f,
                new ResourceData { Wood = 120, Stone = 80, Iron = 40, Food = 60 },
                new ArrowSupply { Current = 75 },
                200,
                new PopulationState { Total = 88, Capacity = 100, Idle = 9 },
                40,
                20,
                10,
                35L);
            source[0].Damage = 999f;

            Assert.That(payload.Day, Is.EqualTo(3));
            Assert.That(payload.Kills, Is.EqualTo(4_200));
            Assert.That(payload.PeakEnemies, Is.EqualTo(2_048));
            Assert.That(payload.PeakPopulation, Is.EqualTo(88));
            Assert.That(payload.MetaReward, Is.EqualTo(640));
            Assert.That(payload.FinalWallMaxHp, Is.EqualTo(475f));
            Assert.That(payload.FinalResources.Wood, Is.EqualTo(120));
            Assert.That(payload.FinalArrows, Is.EqualTo(75));
            Assert.That(payload.FinalArrowCapacity, Is.EqualTo(200));
            Assert.That(payload.FinalPopulation, Is.EqualTo(88));
            Assert.That(payload.FinalPopulationCapacity, Is.EqualTo(100));
            Assert.That(payload.FinalIdlePopulation, Is.EqualTo(9));
            Assert.That(payload.FinalBasicArchers, Is.EqualTo(40));
            Assert.That(payload.FinalRapidArchers, Is.EqualTo(20));
            Assert.That(payload.FinalFrostArchers, Is.EqualTo(10));
            Assert.That(payload.UnspentGraveEssence, Is.EqualTo(35L));
            Assert.That(payload.WallDamageTimeline.Count, Is.EqualTo(2));
            Assert.That(payload.WallDamageTimeline[0].Damage, Is.EqualTo(150f));
        }

        [Test]
        public void TryEmitRunEnded_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            RunEndedTelemetryPayload payload = RunEndedTelemetryFactory.Create(
                4,
                8_000,
                3_100,
                120,
                new List<RunEndedWallDamageTelemetryEntry>
                {
                    new RunEndedWallDamageTelemetryEntry
                    {
                        Day = 3,
                        Phase = "night",
                        Damage = 325.25f
                    }
                },
                1_250,
                550f,
                new ResourceData { Wood = 300, Stone = 200, Iron = 100, Food = 50 },
                new ArrowSupply { Current = 25 },
                400,
                new PopulationState { Total = 120, Capacity = 140, Idle = 15 },
                60,
                25,
                20,
                125L);

            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitRunEnded(
                    " run_ended_contract_04 ", payload,
                    out GameplayTelemetryRecord emitted, out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_ended_contract_04"));
                Assert.That(emitted.EventName, Is.EqualTo("run_ended"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(2));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                RunEndedTelemetryPayload decoded =
                    JsonUtility.FromJson<RunEndedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("run_ended"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(2));
                Assert.That(decoded.Kills, Is.EqualTo(8_000));
                Assert.That(decoded.PeakEnemies, Is.EqualTo(3_100));
                Assert.That(decoded.WallDamageTimeline[0].Phase, Is.EqualTo("night"));
                Assert.That(decoded.MetaReward, Is.EqualTo(1_250));
                Assert.That(decoded.FinalWallMaxHp, Is.EqualTo(550f));
                Assert.That(decoded.FinalResources.Iron, Is.EqualTo(100));
                Assert.That(decoded.FinalArrowCapacity, Is.EqualTo(400));
                Assert.That(decoded.FinalPopulationCapacity, Is.EqualTo(140));
                Assert.That(decoded.FinalRapidArchers, Is.EqualTo(25));
                Assert.That(decoded.UnspentGraveEssence, Is.EqualTo(125L));
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitRunEnded_RejectsInvalidSummaryAndWallDamageTimeline()
        {
            var payload = new RunEndedTelemetryPayload
            {
                Day = 2,
                Kills = 10,
                PeakEnemies = 20,
                PeakPopulation = 30,
                MetaReward = 40,
                WallDamageTimeline = new List<RunEndedWallDamageTelemetryEntry>
                {
                    new RunEndedWallDamageTelemetryEntry
                    {
                        Day = 2,
                        Phase = "night",
                        Damage = 15f
                    }
                }
            };

            payload.PeakEnemies = -1;
            Assert.That(GameplayTelemetry.TryEmitRunEnded(
                "run_invalid_summary", payload, out _, out string summaryError), Is.False);
            Assert.That(summaryError, Does.Contain("summary"));

            payload.PeakEnemies = 20;
            payload.FinalWallMaxHp = 0f;
            Assert.That(GameplayTelemetry.TryEmitRunEnded(
                "run_invalid_wall_max", payload, out _, out string wallMaxError),
                Is.False);
            Assert.That(wallMaxError, Does.Contain("final economy/combat"));

            payload.FinalWallMaxHp = 550f;
            payload.FinalArrows = 2;
            payload.FinalArrowCapacity = 1;
            Assert.That(GameplayTelemetry.TryEmitRunEnded(
                "run_invalid_final_snapshot", payload, out _, out string finalSnapshotError),
                Is.False);
            Assert.That(finalSnapshotError, Does.Contain("final economy/combat"));

            payload.FinalArrows = 0;
            payload.FinalArrowCapacity = 0;
            payload.WallDamageTimeline[0].Phase = "storm";
            Assert.That(GameplayTelemetry.TryEmitRunEnded(
                "run_invalid_phase", payload, out _, out string phaseError), Is.False);
            Assert.That(phaseError, Does.Contain("WallDamageTimeline"));

            payload.WallDamageTimeline[0].Phase = "night";
            payload.WallDamageTimeline.Add(new RunEndedWallDamageTelemetryEntry
            {
                Day = 1,
                Phase = "day",
                Damage = 1f
            });
            Assert.That(GameplayTelemetry.TryEmitRunEnded(
                "run_invalid_order", payload, out _, out string orderError), Is.False);
            Assert.That(orderError, Does.Contain("kronolojik"));
        }

        [Test]
        public void TryEmitWallRepaired_ProducesVersionedMachineReadableEnvelope()
        {
            GameplayTelemetryRecord observed = default;
            bool received = false;
            void OnEmitted(GameplayTelemetryRecord record)
            {
                observed = record;
                received = true;
            }

            WallRepairedTelemetryPayload payload = WallRepairedTelemetryFactory.Create(
                SiegeCyclePhase.Day,
                25,
                500f,
                750f);
            GameplayTelemetry.Emitted += OnEmitted;
            try
            {
                Assert.That(GameplayTelemetry.TryEmitWallRepaired(
                    " run_repair_25 ", payload, out GameplayTelemetryRecord emitted,
                    out string error), Is.True, error);
                Assert.That(received, Is.True);
                Assert.That(observed.RunId, Is.EqualTo("run_repair_25"));
                Assert.That(emitted.EventName, Is.EqualTo("wall_repaired"));
                Assert.That(emitted.SchemaVersion, Is.EqualTo(1));

                GameplayTelemetryEnvelope envelope =
                    JsonUtility.FromJson<GameplayTelemetryEnvelope>(emitted.SerializedEnvelope);
                WallRepairedTelemetryPayload decoded =
                    JsonUtility.FromJson<WallRepairedTelemetryPayload>(envelope.PayloadJson);
                Assert.That(envelope.EventName, Is.EqualTo("wall_repaired"));
                Assert.That(envelope.SchemaVersion, Is.EqualTo(1));
                Assert.That(decoded.Phase, Is.EqualTo("day"));
                Assert.That(decoded.StoneCost, Is.EqualTo(25));
                Assert.That(decoded.HpBefore, Is.EqualTo(500f));
                Assert.That(decoded.HpAfter, Is.EqualTo(750f));
            }
            finally
            {
                GameplayTelemetry.Emitted -= OnEmitted;
            }
        }

        [Test]
        public void TryEmitWallRepaired_RejectsInvalidPhaseCostAndHpTransition()
        {
            var payload = new WallRepairedTelemetryPayload
            {
                Phase = "night",
                StoneCost = 25,
                HpBefore = 500f,
                HpAfter = 750f
            };

            Assert.That(GameplayTelemetry.TryEmitWallRepaired(
                "run_night_repair", payload, out _, out string phaseError), Is.False);
            Assert.That(phaseError, Does.Contain("Day/Dusk"));

            payload.Phase = "day";
            payload.StoneCost = 0;
            Assert.That(GameplayTelemetry.TryEmitWallRepaired(
                "run_free_repair", payload, out _, out string costError), Is.False);
            Assert.That(costError, Does.Contain("Stone cost"));

            payload.StoneCost = 25;
            payload.HpAfter = payload.HpBefore;
            Assert.That(GameplayTelemetry.TryEmitWallRepaired(
                "run_zero_heal", payload, out _, out string hpError), Is.False);
            Assert.That(hpError, Does.Contain("HP before/after"));

            payload.HpBefore = 0f;
            payload.HpAfter = 250f;
            Assert.That(GameplayTelemetry.TryEmitWallRepaired(
                "run_dead_wall", payload, out _, out string deadError), Is.False);
            Assert.That(deadError, Does.Contain("HP before/after"));
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
