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
        private byte[] _originalRunSave;
        private string _runSavePath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath)
                ? File.ReadAllBytes(_runSavePath)
                : null;
            RunPersistence.Delete();
            _records.Clear();
            _phaseRecords.Clear();
            _resourceSpentRecords.Clear();
            _archerChangedRecords.Clear();
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
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameplayTelemetry.Emitted -= OnTelemetryEmitted;
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;
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

        private void OnTelemetryEmitted(GameplayTelemetryRecord record)
        {
            if (record.EventName == GameplayTelemetry.RunStartedEventName)
                _records.Add(record);
            else if (record.EventName == GameplayTelemetry.PhaseChangedEventName)
                _phaseRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.ResourceSpentEventName)
                _resourceSpentRecords.Add(record);
            else if (record.EventName == GameplayTelemetry.ArcherChangedEventName)
                _archerChangedRecords.Add(record);
        }
    }
}
