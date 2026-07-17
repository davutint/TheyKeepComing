using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class GameplayTelemetryPlayModeTests
    {
        private readonly List<GameplayTelemetryRecord> _records =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _phaseRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _resourceSpentRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _archerChangedRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _heartNodeBoughtRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _councilResolvedRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _abilityCastRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _wallRepairedRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<GameplayTelemetryRecord> _runEndedRecords =
            new List<GameplayTelemetryRecord>();
        private readonly List<string> _purchaseEventOrder = new List<string>();
        private readonly List<UnityEngine.Object> _createdObjects =
            new List<UnityEngine.Object>();
        private byte[] _originalRunSave;
        private string _runSavePath;
        private byte[] _originalDeathReceipt;
        private string _deathReceiptPath;
        private byte[] _originalMetaSave;
        private string _metaSavePath;
        private FieldInfo _heartCatalogField;
        private HeartNodeCatalogSO _originalHeartCatalog;
        private FieldInfo _councilCatalogField;
        private CouncilEventCatalogSO _originalCouncilCatalog;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath)
                ? File.ReadAllBytes(_runSavePath)
                : null;
            _deathReceiptPath = Path.Combine(
                Application.persistentDataPath, "run_death_receipt.json");
            _originalDeathReceipt = File.Exists(_deathReceiptPath)
                ? File.ReadAllBytes(_deathReceiptPath)
                : null;
            _metaSavePath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
            _originalMetaSave = File.Exists(_metaSavePath)
                ? File.ReadAllBytes(_metaSavePath)
                : null;
            DeleteFileAndTemp(_runSavePath);
            DeleteFileAndTemp(_deathReceiptPath);
            DeleteFileAndTemp(_metaSavePath);
            MetaProgression.Load();
            _records.Clear();
            _phaseRecords.Clear();
            _resourceSpentRecords.Clear();
            _archerChangedRecords.Clear();
            _heartNodeBoughtRecords.Clear();
            _councilResolvedRecords.Clear();
            _abilityCastRecords.Clear();
            _wallRepairedRecords.Clear();
            _runEndedRecords.Clear();
            _purchaseEventOrder.Clear();
            GameplayTelemetry.Emitted += OnTelemetryEmitted;
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;

            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 300; frame++)
            {
                if (GameManager.Instance != null && GameManager.Instance.ContinuousSiegeCycle.Enabled)
                    break;
                yield return null;
            }
            Assert.That(GameManager.Instance, Is.Not.Null);
            Assert.That(GameManager.Instance.ContinuousSiegeCycle.Enabled, Is.True);
            _heartCatalogField = typeof(GameManager).GetField(
                "heartCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(_heartCatalogField, Is.Not.Null);
            _originalHeartCatalog =
                _heartCatalogField.GetValue(GameManager.Instance) as HeartNodeCatalogSO;
            _councilCatalogField = typeof(GameManager).GetField(
                "councilCatalog",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(_councilCatalogField, Is.Not.Null);
            _originalCouncilCatalog =
                _councilCatalogField.GetValue(GameManager.Instance) as CouncilEventCatalogSO;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameplayTelemetry.Emitted -= OnTelemetryEmitted;
            if (GameManager.Instance != null && _heartCatalogField != null)
            {
                _heartCatalogField.SetValue(GameManager.Instance, _originalHeartCatalog);
                MethodInfo resetHeartRuntime = typeof(GameManager).GetMethod(
                    "ResetHeartRuntime",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                resetHeartRuntime?.Invoke(GameManager.Instance, null);

                _councilCatalogField?.SetValue(GameManager.Instance, _originalCouncilCatalog);
                MethodInfo resetCouncilState = typeof(GameManager).GetMethod(
                    "ResetCouncilState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                resetCouncilState?.Invoke(GameManager.Instance, null);
            }
            RestoreFile(_runSavePath, _originalRunSave);
            RestoreFile(_deathReceiptPath, _originalDeathReceipt);
            RestoreFile(_metaSavePath, _originalMetaSave);
            MetaProgression.Load();
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_createdObjects[i]);
            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewRun_EmitsExactRunStartedOnce_AndContinueDoesNotDuplicate()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _phaseRecords.Clear();
            gameManager.RestartGame();

            for (int frame = 0; frame < 180
                && (_records.Count == 0 || _phaseRecords.Count == 0); frame++)
                yield return null;

            Assert.That(_records.Count, Is.EqualTo(1));
            Assert.That(_phaseRecords.Count, Is.EqualTo(1));
            GameplayTelemetryRecord record = _records[0];
            Assert.That(record.EventName, Is.EqualTo(GameplayTelemetry.RunStartedEventName));
            Assert.That(record.RunId, Is.EqualTo(gameManager.CurrentRunId));
            RunStartedTelemetryPayload payload =
                JsonUtility.FromJson<RunStartedTelemetryPayload>(record.PayloadJson);

            Assert.That(payload.MetaCatalogConfigured, Is.EqualTo(gameManager.MetaCatalog != null));
            Assert.That(payload.MetaCatalogDefinitionCount,
                Is.EqualTo(gameManager.MetaCatalog?.Upgrades?.Length ?? 0));
            Assert.That(payload.MetaLevels.Count,
                Is.EqualTo(gameManager.MetaCatalog?.Upgrades?.Length ?? 0));
            Assert.That(payload.StartingResources.Wood, Is.EqualTo(gameManager.Resources.Wood));
            Assert.That(payload.StartingResources.Stone, Is.EqualTo(gameManager.Resources.Stone));
            Assert.That(payload.StartingResources.Iron, Is.EqualTo(gameManager.Resources.Iron));
            Assert.That(payload.StartingResources.Food, Is.EqualTo(gameManager.Resources.Food));
            Assert.That(payload.StartingResources.Arrows, Is.EqualTo(gameManager.ArrowSupply.Current));
            Assert.That(payload.StartingResources.Population, Is.EqualTo(gameManager.Population.Total));

            HeartRuntimeTuningTelemetry heart = gameManager.GetHeartRuntimeTuningTelemetry();
            Assert.That(payload.Heart.CatalogConfigured, Is.EqualTo(heart.HasCatalog));
            Assert.That(payload.Heart.RuntimeAttempted, Is.True);
            Assert.That(payload.Heart.GraphReady, Is.EqualTo(heart.RuntimeReady));
            Assert.That(payload.Heart.GraphVersion, Is.EqualTo(heart.GraphVersion));
            Assert.That(payload.Heart.CatalogVersion, Is.EqualTo(heart.CatalogVersion));
            Assert.That(payload.Heart.Seed, Is.EqualTo(heart.Seed));

            string originalRunId = gameManager.CurrentRunId;
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.CurrentRunId, Is.EqualTo(originalRunId));
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Assert.That(_records.Count, Is.EqualTo(1),
                "Continue ayni run icin ikinci run_started uretmemeli.");
            Assert.That(_phaseRecords.Count, Is.EqualTo(1),
                "Continue ayni run/day/phase icin ikinci phase_changed uretmemeli.");
        }

        [UnityTest]
        public IEnumerator PhaseTransition_EmitsCanonicalHordeSnapshotOnce()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _phaseRecords.Clear();
            gameManager.RestartGame();

            for (int frame = 0; frame < 180 && _phaseRecords.Count == 0; frame++)
                yield return null;
            Assert.That(_phaseRecords.Count, Is.EqualTo(1));
            PhaseChangedTelemetryPayload initial =
                JsonUtility.FromJson<PhaseChangedTelemetryPayload>(_phaseRecords[0].PayloadJson);
            Assert.That(initial.Day, Is.EqualTo(1));
            Assert.That(initial.Phase, Is.EqualTo("day"));

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntityManager entityManager = world.EntityManager;
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData),
                typeof(ContinuousSpawnBudgetData));
            using EntityQuery waveQuery = entityManager.CreateEntityQuery(typeof(WaveStateData));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity waveEntity = waveQuery.GetSingletonEntity();
            ContinuousSiegeCycleData originalCycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            ContinuousSpawnBudgetData originalBudget =
                entityManager.GetComponentData<ContinuousSpawnBudgetData>(cycleEntity);
            WaveStateData originalWave =
                entityManager.GetComponentData<WaveStateData>(waveEntity);

            MethodInfo readEcsData = typeof(GameManager).GetMethod(
                "ReadECSData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo emitPhaseChanged = typeof(GameManager).GetMethod(
                "TryEmitPhaseChangedTelemetry",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(readEcsData, Is.Not.Null);
            Assert.That(emitPhaseChanged, Is.Not.Null);

            _phaseRecords.Clear();
            try
            {
                ContinuousSiegeCycleData transitionedCycle = originalCycle;
                transitionedCycle.Enabled = true;
                transitionedCycle.CycleIndex = 4;
                transitionedCycle.Phase = SiegeCyclePhase.Dusk;
                ContinuousSpawnBudgetData transitionedBudget = originalBudget;
                transitionedBudget.PendingEnemies = 321L;
                WaveStateData transitionedWave = originalWave;
                transitionedWave.ZombiesAlive = 123;
                entityManager.SetComponentData(cycleEntity, transitionedCycle);
                entityManager.SetComponentData(cycleEntity, transitionedBudget);
                entityManager.SetComponentData(waveEntity, transitionedWave);

                readEcsData.Invoke(gameManager, null);
                emitPhaseChanged.Invoke(gameManager, null);

                Assert.That(_phaseRecords.Count, Is.EqualTo(1));
                PhaseChangedTelemetryPayload payload =
                    JsonUtility.FromJson<PhaseChangedTelemetryPayload>(
                        _phaseRecords[0].PayloadJson);
                Assert.That(payload.Day, Is.EqualTo(5));
                Assert.That(payload.Phase, Is.EqualTo("dusk"));
                Assert.That(payload.AliveEnemies, Is.EqualTo(123));
                Assert.That(payload.SpawnBacklog, Is.EqualTo(321L));

                emitPhaseChanged.Invoke(gameManager, null);
                Assert.That(_phaseRecords.Count, Is.EqualTo(1),
                    "Ayni run/day/phase ikinci phase_changed uretmemeli.");
            }
            finally
            {
                entityManager.SetComponentData(cycleEntity, originalCycle);
                entityManager.SetComponentData(cycleEntity, originalBudget);
                entityManager.SetComponentData(waveEntity, originalWave);
                readEcsData.Invoke(gameManager, null);
            }
        }

        [UnityTest]
        public IEnumerator PurchaseTransactions_EmitExactCommittedDebits_AndRejectWithoutEvent()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _phaseRecords.Clear();
            _resourceSpentRecords.Clear();
            gameManager.RestartGame();

            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Purchase telemetry oncesi run identity kurulmalidir.");

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntityManager entityManager = world.EntityManager;
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity resourceEntity = resourceQuery.GetSingletonEntity();

            ResourceData funded = entityManager.GetComponentData<ResourceData>(resourceEntity);
            funded.Wood = 100_000;
            funded.Stone = 100_000;
            funded.Iron = 100_000;
            funded.Food = 100_000;
            entityManager.SetComponentData(resourceEntity, funded);

            _resourceSpentRecords.Clear();
            ResourceCost bedCost = gameManager.GetBedCapacityPurchaseCost(1);
            int expectedBedCount = gameManager.GetTotalBedCapacity() + 1;
            Assert.That(gameManager.TryBuyBedCapacity(1), Is.True);
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(1));
            ResourceSpentTelemetryPayload bedPayload =
                JsonUtility.FromJson<ResourceSpentTelemetryPayload>(
                    _resourceSpentRecords[0].PayloadJson);
            Assert.That(bedPayload.Resource, Is.EqualTo("wood"));
            Assert.That(bedPayload.Amount, Is.EqualTo(bedCost.Wood));
            Assert.That(bedPayload.PurchaseType, Is.EqualTo("bed_capacity"));
            Assert.That(bedPayload.ResultingLevel, Is.Zero);
            Assert.That(bedPayload.ResultingCount, Is.EqualTo(expectedBedCount));

            _resourceSpentRecords.Clear();
            ResourceCost workerCost = gameManager.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Wood,
                WorkerBuildingUpgradeType.Capacity);
            Assert.That(workerCost.Wood, Is.GreaterThan(0));
            Assert.That(workerCost.Iron, Is.GreaterThan(0));
            Assert.That(gameManager.TryBuyWorkerBuildingUpgrade(
                EconomyFocusType.Wood,
                WorkerBuildingUpgradeType.Capacity), Is.True);
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(2),
                "Wood + Iron tek purchase icin kaynak basina bir event uretmelidir.");

            ResourceSpentTelemetryPayload workerWood =
                JsonUtility.FromJson<ResourceSpentTelemetryPayload>(
                    _resourceSpentRecords[0].PayloadJson);
            ResourceSpentTelemetryPayload workerIron =
                JsonUtility.FromJson<ResourceSpentTelemetryPayload>(
                    _resourceSpentRecords[1].PayloadJson);
            Assert.That(workerWood.Resource, Is.EqualTo("wood"));
            Assert.That(workerWood.Amount, Is.EqualTo(workerCost.Wood));
            Assert.That(workerIron.Resource, Is.EqualTo("iron"));
            Assert.That(workerIron.Amount, Is.EqualTo(workerCost.Iron));
            Assert.That(workerWood.PurchaseType,
                Is.EqualTo("worker_wood_capacity_upgrade"));
            Assert.That(workerIron.PurchaseType,
                Is.EqualTo(workerWood.PurchaseType));
            Assert.That(workerWood.ResultingLevel, Is.EqualTo(1));
            Assert.That(workerIron.ResultingLevel, Is.EqualTo(1));

            _resourceSpentRecords.Clear();
            ResourceData empty = entityManager.GetComponentData<ResourceData>(resourceEntity);
            empty.Wood = 0;
            empty.Iron = 0;
            entityManager.SetComponentData(resourceEntity, empty);
            Assert.That(gameManager.TryBuyWorkerBuildingUpgrade(
                EconomyFocusType.Wood,
                WorkerBuildingUpgradeType.Capacity), Is.False);
            Assert.That(_resourceSpentRecords, Is.Empty,
                "Reddedilen transaction resource_spent uretmemeli.");
        }

        [UnityTest]
        public IEnumerator ArcherBuyAndRetrain_EmitCanonicalTransitions_AndRejectedBuyEmitsNothing()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _phaseRecords.Clear();
            _resourceSpentRecords.Clear();
            _archerChangedRecords.Clear();
            gameManager.RestartGame();

            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Archer telemetry oncesi run identity kurulmalidir.");

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntityManager entityManager = world.EntityManager;
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity resourceEntity = resourceQuery.GetSingletonEntity();
            ResourceData funded = entityManager.GetComponentData<ResourceData>(resourceEntity);
            funded.Wood = 1_000_000;
            funded.Stone = 1_000_000;
            funded.Iron = 1_000_000;
            funded.Food = 1_000_000;
            entityManager.SetComponentData(resourceEntity, funded);

            Assert.That(gameManager.IsArcherTypeUnlocked(ArcherType.Rapid), Is.False);
            Assert.That(gameManager.BuyArcher(ArcherType.Rapid), Is.False);
            Assert.That(_archerChangedRecords, Is.Empty,
                "Locked/rejected buy archer_changed uretmemeli.");

            int totalBeforeBuy = gameManager.GetTotalArcherCount();
            Assert.That(gameManager.BuyArcher(ArcherType.Basic), Is.True);
            Assert.That(_archerChangedRecords.Count, Is.EqualTo(1));
            ArcherChangedTelemetryPayload buy =
                JsonUtility.FromJson<ArcherChangedTelemetryPayload>(
                    _archerChangedRecords[0].PayloadJson);
            Assert.That(buy.ChangeType, Is.EqualTo("buy"));
            Assert.That(buy.TypeFrom, Is.EqualTo("none"));
            Assert.That(buy.TypeTo, Is.EqualTo("basic"));
            Assert.That(buy.TotalCapUsage, Is.EqualTo(totalBeforeBuy + 1));
            Assert.That(buy.TotalCapUsage, Is.EqualTo(gameManager.GetTotalArcherCount()));

            MethodInfo unlockFromTech = typeof(GameManager).GetMethod(
                "UnlockArcherTypeFromTech",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(unlockFromTech, Is.Not.Null);
            unlockFromTech.Invoke(gameManager, new object[] { ArcherType.Rapid });

            int totalBeforeRetrain = gameManager.GetTotalArcherCount();
            Assert.That(gameManager.RetrainBasicArcher(ArcherType.Rapid), Is.True);
            Assert.That(_archerChangedRecords.Count, Is.EqualTo(2));
            ArcherChangedTelemetryPayload retrain =
                JsonUtility.FromJson<ArcherChangedTelemetryPayload>(
                    _archerChangedRecords[1].PayloadJson);
            Assert.That(retrain.ChangeType, Is.EqualTo("retrain"));
            Assert.That(retrain.TypeFrom, Is.EqualTo("basic"));
            Assert.That(retrain.TypeTo, Is.EqualTo("rapid"));
            Assert.That(retrain.TotalCapUsage, Is.EqualTo(totalBeforeRetrain));
            Assert.That(retrain.TotalCapUsage, Is.EqualTo(gameManager.GetTotalArcherCount()));
        }

        [UnityTest]
        public IEnumerator HeartNodePurchase_EmitsCommittedGraphSnapshot_AndRejectedPurchaseEmitsNothing()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            gameManager.RestartGame();
            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Heart telemetry oncesi run identity kurulmalidir.");

            bool snapshotReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    snapshotReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(snapshotReady, Is.True, "Heart telemetry snapshot'i hazirlanamadi.");

            RunSaveState save = RunPersistence.TryLoad();
            Assert.That(save, Is.Not.Null);
            HeartNodeCatalogSO catalog = CreateTelemetryHeartCatalog();
            save.HasHeartGraph = true;
            save.HeartGraph = CreateTelemetryHeartGraph(catalog.CatalogVersion);
            save.GraveEssence = 500L;
            Assert.That(RunPersistence.Save(save), Is.True);
            _heartCatalogField.SetValue(gameManager, catalog);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.IsHeartRuntimeReady, Is.True);

            _resourceSpentRecords.Clear();
            _heartNodeBoughtRecords.Clear();
            _purchaseEventOrder.Clear();
            HeartPurchaseResult result = gameManager.TryPurchaseHeartNode(
                "rapid_unlock",
                HeartPurchaseQuantity.One);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.NodeDepth, Is.EqualTo(1));
            Assert.That(result.NewlyRevealedNodeIds, Is.EqualTo(new[] { "frost_unlock" }));
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(1));
            Assert.That(_heartNodeBoughtRecords.Count, Is.EqualTo(1));
            Assert.That(_purchaseEventOrder,
                Is.EqualTo(new[] { "resource_spent", "heart_node_bought" }));

            ResourceSpentTelemetryPayload resource =
                JsonUtility.FromJson<ResourceSpentTelemetryPayload>(
                    _resourceSpentRecords[0].PayloadJson);
            Assert.That(resource.Resource, Is.EqualTo("grave_essence"));
            Assert.That(resource.Amount, Is.EqualTo(10L));
            Assert.That(resource.PurchaseType, Is.EqualTo("heart_node"));
            Assert.That(resource.ResultingLevel, Is.EqualTo(1));

            GameplayTelemetryRecord record = _heartNodeBoughtRecords[0];
            Assert.That(record.SchemaVersion, Is.EqualTo(1));
            HeartNodeBoughtTelemetryPayload payload =
                JsonUtility.FromJson<HeartNodeBoughtTelemetryPayload>(record.PayloadJson);
            Assert.That(payload.NodeId, Is.EqualTo("rapid_unlock"));
            Assert.That(payload.Level, Is.EqualTo(1));
            Assert.That(payload.Depth, Is.EqualTo(1));
            Assert.That(payload.Cost, Is.EqualTo(10L));
            Assert.That(payload.RevealedChildren, Is.EqualTo(1));
            Assert.That(gameManager.GraveEssenceAmount, Is.EqualTo(490L));

            HeartPurchaseResult rejected = gameManager.TryPurchaseHeartNode(
                "rapid_unlock",
                HeartPurchaseQuantity.One);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.FailureReason,
                Is.EqualTo(HeartPurchaseFailureReason.AlreadyPurchased));
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(1));
            Assert.That(_heartNodeBoughtRecords.Count, Is.EqualTo(1));
            Assert.That(_purchaseEventOrder.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator CouncilChoiceAndExpiry_EmitCommittedDecisionOnce_AndContinueDoesNotDuplicate()
        {
            GameManager gameManager = GameManager.Instance;
            _councilCatalogField.SetValue(gameManager, CreateTelemetryCouncilCatalog());
            MethodInfo resetCouncilState = typeof(GameManager).GetMethod(
                "ResetCouncilState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(resetCouncilState, Is.Not.Null);
            resetCouncilState.Invoke(gameManager, null);

            _records.Clear();
            _councilResolvedRecords.Clear();
            gameManager.RestartGame();
            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Council telemetry oncesi run identity kurulmalidir.");

            SetTelemetryCouncilCycle(gameManager, 3, SiegeCyclePhase.Dawn);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            Assert.That(gameManager.ActiveCouncilEvent.TemplateId,
                Is.EqualTo("telemetry_council"));
            Assert.That(gameManager.ChooseCouncilOption(true), Is.True);
            Assert.That(gameManager.ActiveCouncilEvent, Is.Null);
            Assert.That(_councilResolvedRecords.Count, Is.EqualTo(1));

            CouncilResolvedTelemetryPayload selected =
                JsonUtility.FromJson<CouncilResolvedTelemetryPayload>(
                    _councilResolvedRecords[0].PayloadJson);
            Assert.That(selected.Day, Is.EqualTo(3));
            Assert.That(selected.TemplateId, Is.EqualTo("telemetry_council"));
            Assert.That(selected.Resolution, Is.EqualTo("option_a"));
            Assert.That(selected.Effects, Has.Count.EqualTo(1));
            Assert.That(selected.Effects[0].Kind, Is.EqualTo("gain_resource"));
            Assert.That(selected.Effects[0].Amount, Is.GreaterThan(0));
            Assert.That(selected.NextNightDelta, Is.Zero);

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            Assert.That(_councilResolvedRecords.Count, Is.EqualTo(1),
                "Cozulmus Council exact Continue sonrasinda duplicate event uretmemeli.");

            SetTelemetryCouncilCycle(gameManager, 6, SiegeCyclePhase.Dawn);
            Assert.That(gameManager.TryOpenRegularCouncilEvent(), Is.True);
            Assert.That(gameManager.ActiveCouncilEvent, Is.Not.Null);
            gameManager.ExpireCouncilEvent();
            Assert.That(_councilResolvedRecords.Count, Is.EqualTo(2));

            CouncilResolvedTelemetryPayload expired =
                JsonUtility.FromJson<CouncilResolvedTelemetryPayload>(
                    _councilResolvedRecords[1].PayloadJson);
            Assert.That(expired.Day, Is.EqualTo(6));
            Assert.That(expired.TemplateId, Is.EqualTo("telemetry_council"));
            Assert.That(expired.Resolution, Is.EqualTo("expired"));
            Assert.That(expired.Effects, Is.Empty);
            Assert.That(expired.NextNightDelta, Is.Zero);

            gameManager.ExpireCouncilEvent();
            Assert.That(_councilResolvedRecords.Count, Is.EqualTo(2),
                "Bos active state uzerindeki tekrar Expire duplicate event uretmemeli.");
        }

        [UnityTest]
        public IEnumerator AbilityTransactions_EmitCommittedResults_AndRejectedOrContinueEmitNothing()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _abilityCastRecords.Clear();
            gameManager.RestartGame();
            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Ability telemetry oncesi run identity kurulmalidir.");
            Assert.That(gameManager.TryEnableDevelopmentCombat(out string unlockMessage), Is.True,
                unlockMessage);

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntityManager entityManager = world.EntityManager;
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery wallQuery = entityManager.CreateEntityQuery(typeof(WallSegment));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity wallEntity = wallQuery.GetSingletonEntity();

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Day;
            entityManager.SetComponentData(cycleEntity, cycle);

            _abilityCastRecords.Clear();
            Assert.That(gameManager.TryCastFireball(Vector2.zero), Is.True);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(1));
            AbilityCastTelemetryPayload fireball =
                JsonUtility.FromJson<AbilityCastTelemetryPayload>(
                    _abilityCastRecords[0].PayloadJson);
            Assert.That(fireball.Ability, Is.EqualTo("fireball"));
            Assert.That(fireball.Phase, Is.EqualTo("day"));
            Assert.That(fireball.Cooldown,
                Is.EqualTo(gameManager.FireballCooldownDuration).Within(0.001f));
            Assert.That(fireball.Targets, Is.Zero,
                "Projectile kabul aninda speculative Fireball isabeti yazilmamali.");
            Assert.That(fireball.Repair, Is.Zero);
            Assert.That(gameManager.TryCastFireball(Vector2.one), Is.False);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(1));

            int rallyTargets = gameManager.GetTotalArcherCount();
            Assert.That(gameManager.TryUseRally(), Is.True);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(2));
            AbilityCastTelemetryPayload rally =
                JsonUtility.FromJson<AbilityCastTelemetryPayload>(
                    _abilityCastRecords[1].PayloadJson);
            Assert.That(rally.Ability, Is.EqualTo("rally"));
            Assert.That(rally.Phase, Is.EqualTo("day"));
            Assert.That(rally.Cooldown,
                Is.EqualTo(gameManager.RallyCooldownDuration).Within(0.001f));
            Assert.That(rally.Targets, Is.EqualTo(rallyTargets));
            Assert.That(rally.Repair, Is.Zero);
            Assert.That(gameManager.TryUseRally(), Is.False);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(2));

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);
            WallSegment wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            float expectedRepair = SingleWallDefenseRules.HealByMaxPercent(
                wall.CurrentHP,
                wall.MaxHP,
                gameManager.EmergencyRepairHealPercent) - wall.CurrentHP;

            Assert.That(gameManager.TryUseEmergencyRepair(), Is.True);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(3));
            AbilityCastTelemetryPayload repair =
                JsonUtility.FromJson<AbilityCastTelemetryPayload>(
                    _abilityCastRecords[2].PayloadJson);
            Assert.That(repair.Ability, Is.EqualTo("emergency_repair"));
            Assert.That(repair.Phase, Is.EqualTo("night"));
            Assert.That(repair.Cooldown,
                Is.EqualTo(gameManager.EmergencyRepairCooldownDuration).Within(0.001f));
            Assert.That(repair.Targets, Is.EqualTo(1));
            Assert.That(repair.Repair, Is.EqualTo(expectedRepair).Within(0.001f));
            Assert.That(gameManager.TryUseEmergencyRepair(), Is.False);
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(3));

            Assert.That(gameManager.CompleteDevelopmentTestSession(), Is.True);
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            Assert.That(_abilityCastRecords.Count, Is.EqualTo(3),
                "Exact Continue kabul edilmis ability transaction'larini tekrar yaymamali.");
        }

        [UnityTest]
        public IEnumerator WallRepair_EmitsExactCommittedResult_AndRejectedOrContinueEmitNothing()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _resourceSpentRecords.Clear();
            _wallRepairedRecords.Clear();
            _purchaseEventOrder.Clear();
            gameManager.RestartGame();
            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1),
                "Wall repair telemetry oncesi run identity kurulmalidir.");

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntityManager entityManager = world.EntityManager;
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            using EntityQuery wallQuery = entityManager.CreateEntityQuery(typeof(WallSegment));
            using EntityQuery resourceQuery = entityManager.CreateEntityQuery(typeof(ResourceData));
            Entity cycleEntity = cycleQuery.GetSingletonEntity();
            Entity wallEntity = wallQuery.GetSingletonEntity();
            Entity resourceEntity = resourceQuery.GetSingletonEntity();

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Enabled = true;
            cycle.Phase = SiegeCyclePhase.Day;
            entityManager.SetComponentData(cycleEntity, cycle);

            WallSegment wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            ResourceData resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Stone = 100_000;
            entityManager.SetComponentData(resourceEntity, resources);

            ResourceCost cost = gameManager.GetRepairCost();
            Assert.That(cost.Stone, Is.GreaterThan(0));
            float expectedHpAfter = SingleWallDefenseRules.HealByMaxPercent(
                wall.CurrentHP,
                wall.MaxHP,
                gameManager.GetNormalRepairHealPercent());

            _resourceSpentRecords.Clear();
            _wallRepairedRecords.Clear();
            _purchaseEventOrder.Clear();
            Assert.That(gameManager.RepairDefenseFull(), Is.True);
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(1));
            Assert.That(_wallRepairedRecords.Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(
                new[] { "resource_spent", "wall_repaired" },
                _purchaseEventOrder,
                "Stone debit eventi repair sonucundan once gelmelidir.");

            ResourceSpentTelemetryPayload debit =
                JsonUtility.FromJson<ResourceSpentTelemetryPayload>(
                    _resourceSpentRecords[0].PayloadJson);
            Assert.That(debit.Resource, Is.EqualTo("stone"));
            Assert.That(debit.Amount, Is.EqualTo(cost.Stone));
            Assert.That(debit.PurchaseType, Is.EqualTo("wall_repair"));

            WallRepairedTelemetryPayload repaired =
                JsonUtility.FromJson<WallRepairedTelemetryPayload>(
                    _wallRepairedRecords[0].PayloadJson);
            Assert.That(repaired.Phase, Is.EqualTo("day"));
            Assert.That(repaired.StoneCost, Is.EqualTo(cost.Stone));
            Assert.That(repaired.HpBefore, Is.EqualTo(wall.CurrentHP).Within(0.001f));
            Assert.That(repaired.HpAfter, Is.EqualTo(expectedHpAfter).Within(0.001f));
            ResourceData afterRepair = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(afterRepair.Stone, Is.EqualTo(resources.Stone - cost.Stone));

            wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP;
            entityManager.SetComponentData(wallEntity, wall);
            Assert.That(gameManager.RepairDefenseFull(), Is.False);
            Assert.That(_wallRepairedRecords.Count, Is.EqualTo(1));

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            Assert.That(gameManager.RepairDefenseFull(), Is.False);
            Assert.That(_wallRepairedRecords.Count, Is.EqualTo(1));

            cycle.Phase = SiegeCyclePhase.Day;
            entityManager.SetComponentData(cycleEntity, cycle);
            resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Stone = 0;
            entityManager.SetComponentData(resourceEntity, resources);
            Assert.That(gameManager.RepairDefenseFull(), Is.False);
            Assert.That(_wallRepairedRecords.Count, Is.EqualTo(1));
            Assert.That(_resourceSpentRecords.Count, Is.EqualTo(1));

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            for (int frame = 0; frame < 5; frame++)
                yield return null;
            Assert.That(_wallRepairedRecords.Count, Is.EqualTo(1),
                "Exact Continue kabul edilmis Wall repair transaction'ini tekrar yaymamali.");
        }

        [UnityTest]
        public IEnumerator RunEnded_PreservesAccumulatorsAcrossContinue_AndEmitsDurableSummaryOnce()
        {
            GameManager gameManager = GameManager.Instance;
            _records.Clear();
            _runEndedRecords.Clear();
            gameManager.RestartGame();
            for (int frame = 0; frame < 180 && _records.Count == 0; frame++)
                yield return null;
            Assert.That(_records.Count, Is.EqualTo(1));

            bool runtimeReady = false;
            for (int frame = 0; frame < 300; frame++)
            {
                if (gameManager.SaveRunSnapshot())
                {
                    runtimeReady = true;
                    break;
                }
                yield return null;
            }
            Assert.That(runtimeReady, Is.True);

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            using EntityQuery gameStateQuery = entityManager.CreateEntityQuery(
                typeof(GameStateData),
                typeof(WaveStateData),
                typeof(PopulationState),
                typeof(RunTelemetryData),
                typeof(RunWallDamageTelemetryElement));
            using EntityQuery cycleQuery = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData));
            Entity gameStateEntity = gameStateQuery.GetSingletonEntity();
            Entity cycleEntity = cycleQuery.GetSingletonEntity();

            entityManager.SetComponentData(gameStateEntity, new RunTelemetryData
            {
                PeakEnemies = 2_345
            });
            DynamicBuffer<RunWallDamageTelemetryElement> timeline =
                entityManager.GetBuffer<RunWallDamageTelemetryElement>(gameStateEntity);
            timeline.Clear();
            timeline.Add(new RunWallDamageTelemetryElement
            {
                Day = 1,
                Phase = SiegeCyclePhase.Night,
                Damage = 150.5f
            });
            timeline.Add(new RunWallDamageTelemetryElement
            {
                Day = 2,
                Phase = SiegeCyclePhase.Dusk,
                Damage = 25f
            });

            ContinuousSiegeCycleData cycle =
                entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleIndex = 2;
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);

            GameStateData gameState =
                entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.TotalKills = 4_321;
            entityManager.SetComponentData(gameStateEntity, gameState);
            PopulationState population =
                entityManager.GetComponentData<PopulationState>(gameStateEntity);
            population.Total = 91;
            population.Capacity = 100;
            population.BaseCapacity = 100;
            population.Idle = Math.Max(0, population.Total - population.Workers - population.Archers);
            entityManager.SetComponentData(gameStateEntity, population);

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            RunSaveState saved = RunPersistence.TryLoad();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.Version, Is.EqualTo(RunSaveState.CurrentVersion));
            Assert.That(saved.TelemetryPeakEnemies, Is.EqualTo(2_345));
            Assert.That(saved.WallDamageTimeline.Count, Is.EqualTo(2));

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            RunTelemetryData restoredTelemetry =
                entityManager.GetComponentData<RunTelemetryData>(gameStateEntity);
            DynamicBuffer<RunWallDamageTelemetryElement> restoredTimeline =
                entityManager.GetBuffer<RunWallDamageTelemetryElement>(gameStateEntity);
            Assert.That(restoredTelemetry.PeakEnemies, Is.EqualTo(2_345));
            Assert.That(restoredTimeline.Length, Is.EqualTo(2));
            Assert.That(restoredTimeline[0].Damage, Is.EqualTo(150.5f).Within(0.001f));

            _runEndedRecords.Clear();
            gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = true;
            entityManager.SetComponentData(gameStateEntity, gameState);

            Assert.That(gameManager.SaveRunSnapshot(), Is.False);
            Assert.That(_runEndedRecords.Count, Is.EqualTo(1));
            RunEndedTelemetryPayload payload =
                JsonUtility.FromJson<RunEndedTelemetryPayload>(
                    _runEndedRecords[0].PayloadJson);
            Assert.That(payload.Day, Is.EqualTo(3));
            Assert.That(payload.Kills, Is.EqualTo(4_321));
            Assert.That(payload.PeakEnemies, Is.EqualTo(2_345));
            Assert.That(payload.PeakPopulation, Is.EqualTo(91));
            Assert.That(payload.WallDamageTimeline.Count, Is.EqualTo(2));
            Assert.That(payload.WallDamageTimeline[0].Phase, Is.EqualTo("night"));
            Assert.That(payload.WallDamageTimeline[1].Phase, Is.EqualTo("dusk"));
            Assert.That(payload.MetaReward,
                Is.EqualTo(gameManager.LastRunResult.Reward.TotalSouls));
            Assert.That(gameManager.LastRunResult.Persisted, Is.True);

            Assert.That(gameManager.SaveRunSnapshot(), Is.False);
            Assert.That(_runEndedRecords.Count, Is.EqualTo(1),
                "Ayni durable death transaction'i ikinci run_ended uretmemeli.");
            yield return null;
        }

        private CouncilEventCatalogSO CreateTelemetryCouncilCatalog()
        {
            CouncilEffectAtomSO gain =
                ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            gain.Id = "telemetry_gain";
            gain.Kind = CouncilEffectKind.GainResource;
            gain.MinutesOfProduction = 1f;
            gain.BudgetMinutes = 1f;
            _createdObjects.Add(gain);

            CouncilEffectAtomSO boost =
                ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            boost.Id = "telemetry_boost";
            boost.Kind = CouncilEffectKind.TempProductionBoost;
            boost.Rate = 0.1f;
            boost.DurationDays = 1;
            boost.BudgetMinutes = 1f;
            _createdObjects.Add(boost);

            CouncilTemplateSO template =
                ScriptableObject.CreateInstance<CouncilTemplateSO>();
            template.Id = "telemetry_council";
            template.Title = "TELEMETRY COUNCIL";
            template.Body = "A regular Council on day {DAY}.";
            template.OutcomeA = "+{GAIN_N} {GAIN_RES}.";
            template.OutcomeB = "{BOOST_RES} +{BOOST_PCT}% for {BOOST_D} days.";
            template.Contrast = CouncilContrastType.NowVsLater;
            template.OptionAAtomIds = new[] { gain.Id };
            template.OptionBAtomIds = new[] { boost.Id };
            template.MinDay = 1;
            _createdObjects.Add(template);

            CouncilEventCatalogSO catalog =
                ScriptableObject.CreateInstance<CouncilEventCatalogSO>();
            catalog.Atoms = new[] { gain, boost };
            catalog.Templates = new[] { template };
            catalog.RecentTemplateMemory = 1;
            _createdObjects.Add(catalog);
            return catalog;
        }

        private static void SetTelemetryCouncilCycle(
            GameManager gameManager,
            int day,
            SiegeCyclePhase phase)
        {
            PropertyInfo cycleProperty = typeof(GameManager).GetProperty(
                "ContinuousSiegeCycle",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo cycleSetter = cycleProperty?.GetSetMethod(true);
            Assert.That(cycleSetter, Is.Not.Null);

            ContinuousSiegeCycleData cycle = gameManager.ContinuousSiegeCycle;
            cycle.Enabled = true;
            cycle.CycleIndex = day - 1;
            cycle.Phase = phase;
            cycleSetter.Invoke(gameManager, new object[] { cycle });
        }

        private HeartNodeCatalogSO CreateTelemetryHeartCatalog()
        {
            HeartNodeCatalogSO catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
            _createdObjects.Add(catalog);
            catalog.CatalogVersion = 77;
            catalog.Nodes = new[]
            {
                CreateTelemetryHeartDefinition("rapid_unlock", HeartNodeBranch.Army,
                    HeartNodeType.Unlock, HeartGraphConstants.RapidGuaranteeTag),
                CreateTelemetryHeartDefinition("frost_unlock", HeartNodeBranch.Army,
                    HeartNodeType.Unlock, HeartGraphConstants.FrostGuaranteeTag),
                CreateTelemetryHeartDefinition("army_sink", HeartNodeBranch.Army,
                    HeartNodeType.Repeatable, HeartGraphConstants.RepeatableSinkTag),
                CreateTelemetryHeartDefinition("wall_access", HeartNodeBranch.Defense,
                    HeartNodeType.Unlock, HeartGraphConstants.WallGuaranteeTag),
                CreateTelemetryHeartDefinition("defense_sink", HeartNodeBranch.Defense,
                    HeartNodeType.Repeatable, HeartGraphConstants.RepeatableSinkTag),
                CreateTelemetryHeartDefinition("production_sink", HeartNodeBranch.Production,
                    HeartNodeType.Repeatable, HeartGraphConstants.RepeatableSinkTag),
                CreateTelemetryHeartDefinition("fireball_unlock", HeartNodeBranch.HeartMagic,
                    HeartNodeType.Unlock, HeartGraphConstants.FireballGuaranteeTag),
                CreateTelemetryHeartDefinition("heart_sink", HeartNodeBranch.HeartMagic,
                    HeartNodeType.Repeatable, HeartGraphConstants.RepeatableSinkTag)
            };
            return catalog;
        }

        private HeartNodeDefinitionSO CreateTelemetryHeartDefinition(
            string id,
            HeartNodeBranch branch,
            HeartNodeType type,
            string tag)
        {
            HeartNodeDefinitionSO definition =
                ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            _createdObjects.Add(definition);
            definition.Id = id;
            definition.Title = id;
            definition.Description = id + " telemetry test";
            definition.Branch = branch;
            definition.Type = type;
            definition.MinimumDepth = 1;
            definition.MaximumDepth = 3;
            definition.BaseGraveEssenceCost = 10L;
            definition.CostGrowthPerLevel = 0d;
            definition.Tags = new[] { tag };
            definition.Effects = Array.Empty<HeartNodeEffect>();
            definition.ConflictNodeIds = Array.Empty<string>();
            return definition;
        }

        private static GeneratedRunGraph CreateTelemetryHeartGraph(int catalogVersion)
        {
            var graph = new GeneratedRunGraph
            {
                CatalogVersion = catalogVersion,
                Seed = 0xB017u,
                RootNodeId = HeartGraphConstants.RootNodeId
            };
            AddTelemetryHeartNode(graph, HeartGraphConstants.RootNodeId,
                HeartNodeBranch.HeartMagic, 0, HeartNodeVisibility.Revealed, 1);
            AddTelemetryHeartNode(graph, "rapid_unlock", HeartNodeBranch.Army, 1,
                HeartNodeVisibility.Revealed);
            AddTelemetryHeartNode(graph, "frost_unlock", HeartNodeBranch.Army, 2);
            AddTelemetryHeartNode(graph, "army_sink", HeartNodeBranch.Army, 3);
            AddTelemetryHeartNode(graph, "wall_access", HeartNodeBranch.Defense, 1,
                HeartNodeVisibility.Revealed);
            AddTelemetryHeartNode(graph, "defense_sink", HeartNodeBranch.Defense, 2);
            AddTelemetryHeartNode(graph, "production_sink", HeartNodeBranch.Production, 1,
                HeartNodeVisibility.Revealed);
            AddTelemetryHeartNode(graph, "fireball_unlock", HeartNodeBranch.HeartMagic, 1,
                HeartNodeVisibility.Revealed);
            AddTelemetryHeartNode(graph, "heart_sink", HeartNodeBranch.HeartMagic, 2);

            AddTelemetryHeartEdge(graph, HeartGraphConstants.RootNodeId, "rapid_unlock");
            AddTelemetryHeartEdge(graph, "rapid_unlock", "frost_unlock");
            AddTelemetryHeartEdge(graph, "frost_unlock", "army_sink");
            AddTelemetryHeartEdge(graph, HeartGraphConstants.RootNodeId, "wall_access");
            AddTelemetryHeartEdge(graph, "wall_access", "defense_sink");
            AddTelemetryHeartEdge(graph, HeartGraphConstants.RootNodeId, "production_sink");
            AddTelemetryHeartEdge(graph, HeartGraphConstants.RootNodeId, "fireball_unlock");
            AddTelemetryHeartEdge(graph, "fireball_unlock", "heart_sink");
            return graph;
        }

        private static void AddTelemetryHeartNode(
            GeneratedRunGraph graph,
            string nodeId,
            HeartNodeBranch branch,
            int depth,
            HeartNodeVisibility visibility = HeartNodeVisibility.Hidden,
            int level = 0)
        {
            graph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = nodeId,
                Branch = branch,
                Depth = depth,
                Visibility = visibility,
                Level = level,
                LockState = HeartNodeLockState.Available,
                LockedByNodeId = string.Empty
            });
        }

        private static void AddTelemetryHeartEdge(
            GeneratedRunGraph graph,
            string fromNodeId,
            string toNodeId)
        {
            graph.Edges.Add(new GeneratedHeartEdge
            {
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId
            });
        }

        private void OnTelemetryEmitted(GameplayTelemetryRecord record)
        {
            if (record.EventName == GameplayTelemetry.RunStartedEventName)
                _records.Add(record);
            else if (record.EventName == GameplayTelemetry.PhaseChangedEventName)
                _phaseRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.ResourceSpentEventName)
            {
                _resourceSpentRecords.Add(record);
                _purchaseEventOrder.Add(record.EventName);
            }
            else if (record.EventName == GameplayTelemetry.ArcherChangedEventName)
                _archerChangedRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.HeartNodeBoughtEventName)
            {
                _heartNodeBoughtRecords.Add(record);
                _purchaseEventOrder.Add(record.EventName);
            }
            else if (record.EventName == GameplayTelemetry.CouncilResolvedEventName)
                _councilResolvedRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.AbilityCastEventName)
                _abilityCastRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.WallRepairedEventName)
            {
                _wallRepairedRecords.Add(record);
                _purchaseEventOrder.Add(record.EventName);
            }
            else if (record.EventName == GameplayTelemetry.RunEndedEventName)
                _runEndedRecords.Add(record);
        }

        private static void DeleteFileAndTemp(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }

        private static void RestoreFile(string path, byte[] content)
        {
            DeleteFileAndTemp(path);
            if (content != null)
                File.WriteAllBytes(path, content);
        }
    }
}
