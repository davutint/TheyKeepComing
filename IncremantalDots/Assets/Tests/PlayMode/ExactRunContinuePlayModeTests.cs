using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Transforms;

namespace DeadWalls.Tests
{
    public class ExactRunContinuePlayModeTests
    {
        private string _runSavePath;
        private string _deathReceiptPath;
        private string _metaSavePath;
        private byte[] _originalRunSave;
        private byte[] _originalDeathReceipt;
        private byte[] _originalMetaSave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _deathReceiptPath = Path.Combine(Application.persistentDataPath, "run_death_receipt.json");
            _metaSavePath = Path.Combine(Application.persistentDataPath, "meta_progress.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            _originalDeathReceipt = File.Exists(_deathReceiptPath)
                ? File.ReadAllBytes(_deathReceiptPath)
                : null;
            _originalMetaSave = File.Exists(_metaSavePath) ? File.ReadAllBytes(_metaSavePath) : null;
            DeleteFileAndTemp(_runSavePath);
            DeleteFileAndTemp(_deathReceiptPath);
            DeleteFileAndTemp(_metaSavePath);
            MetaProgression.Load();
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;

            SceneManager.LoadScene("NewGameScene", LoadSceneMode.Single);
            for (int frame = 0; frame < 120 && GameManager.Instance == null; frame++)
                yield return null;

            Assert.That(GameManager.Instance, Is.Not.Null, "NewGameScene GameManager olusturmadi.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            RestoreFile(_runSavePath, _originalRunSave);
            RestoreFile(_deathReceiptPath, _originalDeathReceipt);
            RestoreFile(_metaSavePath, _originalMetaSave);
            MetaProgression.Load();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Continue_RestoresSameCyclePhaseTimerResourcesAndSpawnRng()
        {
            var gameManager = GameManager.Instance;
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
            Assert.That(runtimeReady, Is.True, "GameManager/SubScene 300 frame icinde snapshot icin hazir olmadi.");

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity cycleEntity = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData), typeof(WaveStateData)).GetSingletonEntity();
            Entity allocationEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation)).GetSingletonEntity();
            Entity wallEntity = entityManager.CreateEntityQuery(typeof(WallSegment)).GetSingletonEntity();

            var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.CycleIndex = 9;
            cycle.Phase = SiegeCyclePhase.Night;
            cycle.CycleTimer = 47.25f;
            cycle.CycleProgress01 = 0.7875f;
            cycle.PhaseProgress01 = 0.3625f;
            entityManager.SetComponentData(cycleEntity, cycle);

            var resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Wood = 4321;
            resources.Stone = 3210;
            resources.Iron = 2109;
            resources.Food = 1098;
            entityManager.SetComponentData(resourceEntity, resources);

            var wave = entityManager.GetComponentData<WaveStateData>(resourceEntity);
            wave.SpawnRandomState = 987654321u;
            entityManager.SetComponentData(resourceEntity, wave);

            var spawnBudget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(cycleEntity);
            spawnBudget.PendingEnemies = 73;
            spawnBudget.TotalDemandedEnemies = 500;
            spawnBudget.TotalSpawnedEnemies = 427;
            spawnBudget.DemandPerInterval = 11;
            spawnBudget.DayQuantityMultiplier = 1.4f;
            spawnBudget.DayBaseSpawnInterval = 0.42f;
            spawnBudget.PhaseIntensityMultiplier = 1.65f;
            spawnBudget.EffectiveSpawnInterval = 0.2545f;
            entityManager.SetComponentData(cycleEntity, spawnBudget);

            var workerAllocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            int savedWoodWorkers = workerAllocation.WoodWorkers;
            int savedStoneWorkers = workerAllocation.StoneWorkers;
            int savedIronWorkers = workerAllocation.IronWorkers;
            int savedFoodWorkers = workerAllocation.FoodWorkers;
            workerAllocation.WoodTargetRatioBps = 1000;
            workerAllocation.StoneTargetRatioBps = 2000;
            workerAllocation.IronTargetRatioBps = 3000;
            workerAllocation.FoodTargetRatioBps = 4000;
            workerAllocation.LastObservedPopulation = 58;
            workerAllocation.LastPopulationGrowthCycle = 10;
            entityManager.SetComponentData(allocationEntity, workerAllocation);

            // Package G: Rally/Emergency kaynak harcamadan calisir ve iki cooldown exact save olur.
            var wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.50f;
            entityManager.SetComponentData(wallEntity, wall);
            ResourceData resourcesBeforeAbilities = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(gameManager.RepairDefenseFull(), Is.False,
                "Night normal repair kapali kalmali ve Stone harcamamali.");
            Assert.That(gameManager.TryUseRally(), Is.True);
            Assert.That(gameManager.TryUseEmergencyRepair(), Is.True);
            float savedRallyCooldown = gameManager.RallyCooldownRemaining;
            float savedEmergencyCooldown = gameManager.EmergencyRepairCooldownRemaining;
            float savedWallHp = entityManager.GetComponentData<WallSegment>(wallEntity).CurrentHP;
            ResourceData resourcesAfterAbilities = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(resourcesAfterAbilities.Wood, Is.EqualTo(resourcesBeforeAbilities.Wood));
            Assert.That(resourcesAfterAbilities.Stone, Is.EqualTo(resourcesBeforeAbilities.Stone));
            Assert.That(resourcesAfterAbilities.Iron, Is.EqualTo(resourcesBeforeAbilities.Iron));
            Assert.That(resourcesAfterAbilities.Food, Is.EqualTo(resourcesBeforeAbilities.Food));

            Assert.That(gameManager.SaveRunSnapshot(), Is.True);
            string savedRunId = gameManager.CurrentRunId;

            cycle.Phase = SiegeCyclePhase.Day;
            cycle.CycleTimer = 0f;
            cycle.CycleIndex = 0;
            entityManager.SetComponentData(cycleEntity, cycle);
            resources.Wood = resources.Stone = resources.Iron = resources.Food = 0;
            entityManager.SetComponentData(resourceEntity, resources);
            wave.SpawnRandomState = 1u;
            entityManager.SetComponentData(resourceEntity, wave);
            spawnBudget.PendingEnemies = 0;
            spawnBudget.TotalDemandedEnemies = 0;
            spawnBudget.TotalSpawnedEnemies = 0;
            entityManager.SetComponentData(cycleEntity, spawnBudget);
            workerAllocation.WoodWorkers = 0;
            workerAllocation.StoneWorkers = 0;
            workerAllocation.IronWorkers = 0;
            workerAllocation.FoodWorkers = 0;
            workerAllocation.WoodTargetRatioBps = 0;
            workerAllocation.StoneTargetRatioBps = 0;
            workerAllocation.IronTargetRatioBps = 0;
            workerAllocation.FoodTargetRatioBps = WorkerAllocationUtility.RatioScale;
            entityManager.SetComponentData(allocationEntity, workerAllocation);

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            cycleEntity = entityManager.CreateEntityQuery(
                typeof(ContinuousSiegeCycleData), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData), typeof(WaveStateData)).GetSingletonEntity();
            allocationEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobilePopulationAllocation)).GetSingletonEntity();
            wallEntity = entityManager.CreateEntityQuery(typeof(WallSegment)).GetSingletonEntity();
            var restoredCycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            var restoredBudget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(cycleEntity);
            var restoredResources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            var restoredWave = entityManager.GetComponentData<WaveStateData>(resourceEntity);
            var restoredAllocation = entityManager.GetComponentData<MobilePopulationAllocation>(allocationEntity);
            var restoredPrep = entityManager.GetComponentData<CastleYardPrepState>(allocationEntity);
            var restoredWall = entityManager.GetComponentData<WallSegment>(wallEntity);

            Assert.That(gameManager.CurrentRunId, Is.EqualTo(savedRunId));
            Assert.That(restoredCycle.CycleIndex, Is.EqualTo(9));
            Assert.That(restoredCycle.Phase, Is.EqualTo(SiegeCyclePhase.Night));
            Assert.That(restoredCycle.CycleTimer, Is.EqualTo(47.25f).Within(0.001f));
            Assert.That(restoredResources.Wood, Is.EqualTo(4321));
            Assert.That(restoredResources.Stone, Is.EqualTo(3210));
            Assert.That(restoredResources.Iron, Is.EqualTo(2109));
            Assert.That(restoredResources.Food, Is.EqualTo(1098));
            Assert.That(restoredWave.SpawnRandomState, Is.EqualTo(987654321u));
            Assert.That(restoredBudget.PendingEnemies, Is.EqualTo(73));
            Assert.That(restoredBudget.TotalDemandedEnemies, Is.EqualTo(500));
            Assert.That(restoredBudget.TotalSpawnedEnemies, Is.EqualTo(427));
            Assert.That(restoredBudget.DayBaseSpawnInterval, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(restoredAllocation.WoodWorkers, Is.EqualTo(savedWoodWorkers));
            Assert.That(restoredAllocation.StoneWorkers, Is.EqualTo(savedStoneWorkers));
            Assert.That(restoredAllocation.IronWorkers, Is.EqualTo(savedIronWorkers));
            Assert.That(restoredAllocation.FoodWorkers, Is.EqualTo(savedFoodWorkers));
            Assert.That(restoredAllocation.WoodTargetRatioBps, Is.EqualTo(1000));
            Assert.That(restoredAllocation.StoneTargetRatioBps, Is.EqualTo(2000));
            Assert.That(restoredAllocation.IronTargetRatioBps, Is.EqualTo(3000));
            Assert.That(restoredAllocation.FoodTargetRatioBps, Is.EqualTo(4000));
            Assert.That(restoredAllocation.LastObservedPopulation, Is.EqualTo(58));
            Assert.That(restoredAllocation.LastPopulationGrowthCycle, Is.EqualTo(10));
            Assert.That(restoredPrep.RallyTimer, Is.GreaterThan(0f));
            Assert.That(gameManager.RallyCooldownRemaining, Is.EqualTo(savedRallyCooldown).Within(0.05f));
            Assert.That(gameManager.EmergencyRepairCooldownRemaining,
                Is.EqualTo(savedEmergencyCooldown).Within(0.05f));
            Assert.That(restoredWall.CurrentHP, Is.EqualTo(savedWallHp).Within(0.01f));
            yield return null;

            using EntityQuery arrivalVisualQuery = entityManager.CreateEntityQuery(
                typeof(SurvivorArrivalVisual));
            Assert.That(arrivalVisualQuery.IsEmpty, Is.True,
                "Exact Continue tamamlanmis Dawn arrival gorselini yeniden oynatmamali.");
        }

        [UnityTest]
        public IEnumerator GraveEssence_UsesHeartTransactionPersistsOnContinueAndResetsWithRun()
        {
            var gameManager = GameManager.Instance;
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
            Assert.That(runtimeReady, Is.True,
                "GameManager/SubScene 300 frame icinde Grave Essence testi icin hazir olmadi.");

            Assert.That(gameManager.GraveEssenceAmount, Is.Zero);
            Assert.That(gameManager.GrantGraveEssence(5_000_000_000L), Is.True);
            Assert.That(gameManager.TrySpendGraveEssenceAtHeart(0), Is.False);
            Assert.That(gameManager.TrySpendGraveEssenceAtHeart(6_000_000_000L), Is.False);
            Assert.That(gameManager.TrySpendGraveEssenceAtHeart(1_250_000_000L), Is.True);
            Assert.That(gameManager.GraveEssenceAmount, Is.EqualTo(3_750_000_000L));
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);

            Assert.That(gameManager.GrantGraveEssence(125), Is.True);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);
            Assert.That(gameManager.GraveEssenceAmount, Is.EqualTo(3_750_000_000L));

            gameManager.RestartGame();
            Assert.That(gameManager.GraveEssenceAmount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Continue_RejectsCouncilPayloadOutsideRolePolicyBeforeRestart()
        {
            GameManager gameManager = GameManager.Instance;
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
            Assert.That(runtimeReady, Is.True,
                "GameManager/SubScene Council Continue preflight testi icin hazir olmadi.");

            RunSaveState save = RunPersistence.TryLoad();
            Assert.That(save, Is.Not.Null);
            Assert.That(gameManager.CouncilCatalog, Is.Not.Null);
            Assert.That(gameManager.CouncilCatalog.Templates, Is.Not.Empty);
            CouncilTemplateSO template = gameManager.CouncilCatalog.Templates[0];

            save.HasActiveCouncilEvent = true;
            save.ActiveCouncilEvent = new ComposedCouncilEvent
            {
                TemplateId = template.Id,
                SetsFlagOnA = template.SetsFlagOnA,
                SetsFlagOnB = template.SetsFlagOnB,
                OptionA = new ComposedCouncilOption
                {
                    Effects = new System.Collections.Generic.List<ComposedCouncilEffect>
                    {
                        new ComposedCouncilEffect { Kind = (CouncilEffectKind)999 },
                    },
                },
                OptionB = new ComposedCouncilOption
                {
                    Effects = new System.Collections.Generic.List<ComposedCouncilEffect>
                    {
                        new ComposedCouncilEffect { Kind = CouncilEffectKind.GainResource, Amount = 1 },
                    },
                },
            };
            Assert.That(RunPersistence.Save(save), Is.True);

            string runIdBefore = gameManager.CurrentRunId;
            long graveEssenceBefore = gameManager.GraveEssenceAmount;
            LogAssert.Expect(LogType.Error,
                new Regex("\\[GameManager\\] Council Continue content preflight reddedildi:"));
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.False);
            Assert.That(gameManager.CurrentRunId, Is.EqualTo(runIdBefore));
            Assert.That(gameManager.GraveEssenceAmount, Is.EqualTo(graveEssenceBefore));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BedCapacityPurchase_SpendsWoodAndPersistsAcrossExactContinue()
        {
            var gameManager = GameManager.Instance;
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
            Assert.That(runtimeReady, Is.True, "Bed capacity snapshot runtime'i hazir olmadi.");

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData)).GetSingletonEntity();
            Entity bedEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobileBedCapacityState)).GetSingletonEntity();

            var resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Wood = 2_000;
            entityManager.SetComponentData(resourceEntity, resources);

            MobileBedCapacityState before = entityManager.GetComponentData<MobileBedCapacityState>(bedEntity);
            ResourceCost cost = gameManager.GetBedCapacityPurchaseCost(5);
            Assert.That(cost.Wood, Is.EqualTo(587));
            Assert.That(gameManager.TryBuyBedCapacity(5), Is.True);

            MobileBedCapacityState purchased = entityManager.GetComponentData<MobileBedCapacityState>(bedEntity);
            ResourceData resourcesAfterPurchase = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(purchased.BaseCapacity, Is.EqualTo(before.BaseCapacity));
            Assert.That(purchased.PurchasedCapacity, Is.EqualTo(before.PurchasedCapacity + 5));
            Assert.That(resourcesAfterPurchase.Wood, Is.EqualTo(1_413));
            Assert.That(gameManager.GetBedCapacityPurchaseCost().Wood, Is.EqualTo(144));
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);

            purchased.PurchasedCapacity = 0;
            entityManager.SetComponentData(bedEntity, purchased);
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            bedEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobileBedCapacityState)).GetSingletonEntity();
            MobileBedCapacityState restored = entityManager.GetComponentData<MobileBedCapacityState>(bedEntity);
            Assert.That(restored.BaseCapacity, Is.EqualTo(before.BaseCapacity));
            Assert.That(restored.PurchasedCapacity, Is.EqualTo(before.PurchasedCapacity + 5));
            Assert.That(MobileBedCapacityUtility.GetTotalCapacity(restored),
                Is.EqualTo(MobileBedCapacityUtility.GetTotalCapacity(before) + 5));
            Assert.That(gameManager.GetBedCapacityPurchaseCost().Wood, Is.EqualTo(144));
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorkerBuildingInvestments_SpendBothResourcesAndPersistAcrossExactContinue()
        {
            var gameManager = GameManager.Instance;
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
            Assert.That(runtimeReady, Is.True, "Worker building snapshot runtime'i hazir olmadi.");

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData)).GetSingletonEntity();
            Entity buildingEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobileWorkerBuildingUpgradeState))
                .GetSingletonEntity();

            var resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Wood = 10_000;
            resources.Iron = 10_000;
            entityManager.SetComponentData(resourceEntity, resources);

            var configBefore = entityManager.GetComponentData<MobileCastleCombatConfig>(buildingEntity);
            Assert.That(gameManager.TryBuyWorkerBuildingUpgrade(
                EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity), Is.True);
            Assert.That(gameManager.TryBuyWorkerBuildingUpgrade(
                EconomyFocusType.Wood, WorkerBuildingUpgradeType.Efficiency), Is.True);
            Assert.That(gameManager.TryBuyWorkerBuildingUpgrade(
                EconomyFocusType.Stone, WorkerBuildingUpgradeType.Capacity), Is.True);

            var purchased = entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(buildingEntity);
            var configPurchased = entityManager.GetComponentData<MobileCastleCombatConfig>(buildingEntity);
            var resourcesPurchased = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(purchased.WoodCapacityLevel, Is.EqualTo(1));
            Assert.That(purchased.WoodEfficiencyLevel, Is.EqualTo(1));
            Assert.That(purchased.StoneCapacityLevel, Is.EqualTo(1));
            Assert.That(configPurchased.WoodWorkerCap, Is.EqualTo(configBefore.WoodWorkerCap + 10));
            Assert.That(configPurchased.StoneWorkerCap, Is.EqualTo(configBefore.StoneWorkerCap + 10));
            Assert.That(configPurchased.WoodWorkerProductionPerMin
                - configBefore.WoodWorkerProductionPerMin, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(resourcesPurchased.Wood, Is.EqualTo(9_650));
            Assert.That(resourcesPurchased.Iron, Is.EqualTo(9_900));
            Assert.That(gameManager.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity).Wood, Is.EqualTo(135));
            Assert.That(gameManager.SaveRunSnapshot(), Is.True);

            entityManager.SetComponentData(buildingEntity, new MobileWorkerBuildingUpgradeState());
            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            buildingEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobileWorkerBuildingUpgradeState))
                .GetSingletonEntity();
            var restored = entityManager.GetComponentData<MobileWorkerBuildingUpgradeState>(buildingEntity);
            var configRestored = entityManager.GetComponentData<MobileCastleCombatConfig>(buildingEntity);
            Assert.That(restored.WoodCapacityLevel, Is.EqualTo(1));
            Assert.That(restored.WoodEfficiencyLevel, Is.EqualTo(1));
            Assert.That(restored.StoneCapacityLevel, Is.EqualTo(1));
            Assert.That(restored.StoneEfficiencyLevel, Is.Zero);
            Assert.That(configRestored.WoodWorkerCap, Is.EqualTo(configPurchased.WoodWorkerCap));
            Assert.That(configRestored.StoneWorkerCap, Is.EqualTo(configPurchased.StoneWorkerCap));
            Assert.That(configRestored.WoodWorkerProductionPerMin,
                Is.EqualTo(configPurchased.WoodWorkerProductionPerMin).Within(0.001f));
            Assert.That(gameManager.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity).Wood, Is.EqualTo(135));
            yield return null;
        }

        [UnityTest]
        public IEnumerator V1CastleLoop_DoesNotApplyPassiveMainResourceConsumption()
        {
            var gameManager = GameManager.Instance;
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
            Entity resourceEntity = entityManager.CreateEntityQuery(
                typeof(ResourceData),
                typeof(ResourceConsumptionRate),
                typeof(ResourceAccumulator)).GetSingletonEntity();

            var resources = new ResourceData
            {
                Wood = 5000,
                Stone = 5000,
                Iron = 5000,
                Food = 5000
            };
            entityManager.SetComponentData(resourceEntity, resources);
            entityManager.SetComponentData(resourceEntity, new ResourceConsumptionRate
            {
                WoodPerMin = 60000f,
                StonePerMin = 60000f,
                IronPerMin = 60000f,
                FoodPerMin = 60000f
            });
            entityManager.SetComponentData(resourceEntity, new ResourceAccumulator());

            yield return null;
            yield return null;

            var after = entityManager.GetComponentData<ResourceData>(resourceEntity);
            var consumption = entityManager.GetComponentData<ResourceConsumptionRate>(resourceEntity);
            Assert.That(after.Wood, Is.GreaterThanOrEqualTo(5000));
            Assert.That(after.Stone, Is.GreaterThanOrEqualTo(5000));
            Assert.That(after.Iron, Is.GreaterThanOrEqualTo(5000));
            Assert.That(after.Food, Is.GreaterThanOrEqualTo(5000));
            Assert.That(consumption.WoodPerMin, Is.Zero);
            Assert.That(consumption.StonePerMin, Is.Zero);
            Assert.That(consumption.IronPerMin, Is.Zero);
            Assert.That(consumption.FoodPerMin, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Repair_IsStoneOnly_AndAllowedOnlyDuringDayOrDusk()
        {
            var gameManager = GameManager.Instance;
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
            Entity cycleEntity = entityManager.CreateEntityQuery(typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData)).GetSingletonEntity();
            Entity wallEntity = entityManager.CreateEntityQuery(typeof(WallSegment)).GetSingletonEntity();

            var wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.CurrentHP = wall.MaxHP * 0.5f;
            entityManager.SetComponentData(wallEntity, wall);
            yield return null;

            var resources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            resources.Wood = 0;
            resources.Stone = 10000;
            entityManager.SetComponentData(resourceEntity, resources);

            var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            cycle.Phase = SiegeCyclePhase.Night;
            entityManager.SetComponentData(cycleEntity, cycle);

            ResourceCost cost = gameManager.GetRepairCost();
            Assert.That(cost.Wood, Is.Zero);
            Assert.That(cost.Stone, Is.GreaterThan(0));
            Assert.That(gameManager.CanRepairDefenseFull(), Is.False);
            Assert.That(gameManager.RepairDefenseFull(), Is.False);
            Assert.That(entityManager.GetComponentData<WallSegment>(wallEntity).CurrentHP,
                Is.EqualTo(wall.CurrentHP));

            cycle.Phase = SiegeCyclePhase.Dusk;
            entityManager.SetComponentData(cycleEntity, cycle);
            Assert.That(gameManager.CanRepairDefenseFull(), Is.True);

            int stoneBefore = entityManager.GetComponentData<ResourceData>(resourceEntity).Stone;
            float expectedHp = Mathf.Min(
                wall.MaxHP,
                wall.CurrentHP + wall.MaxHP * gameManager.GetNormalRepairHealPercent());
            Assert.That(gameManager.RepairDefenseFull(), Is.True);
            var repairedWall = entityManager.GetComponentData<WallSegment>(wallEntity);
            var resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(repairedWall.CurrentHP, Is.EqualTo(expectedHp).Within(0.01f));
            Assert.That(resourcesAfter.Wood, Is.Zero);
            Assert.That(resourcesAfter.Stone, Is.EqualTo(stoneBefore - cost.Stone));
        }

        [UnityTest]
        public IEnumerator RuntimeTuning_UsesProfileDifficulty_AndAuthoringCycleDurations()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig)).GetSingletonEntity();
            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            var samples = entityManager.GetBuffer<DifficultyDaySample>(configEntity);

            Assert.That(config.ZombieHpGrowthPerCycle, Is.Zero);
            Assert.That(config.SpawnBatchGrowthPerCycle, Is.EqualTo(0.15f));
            Assert.That(config.MaxSpawnBatch, Is.EqualTo(16));
            Assert.That(config.RepairBaseStoneCost, Is.EqualTo(50));
            Assert.That(config.SiegeDayDuration, Is.EqualTo(30f));
            Assert.That(config.SiegeDuskDuration, Is.EqualTo(5f));
            Assert.That(config.SiegeNightDuration, Is.EqualTo(20f));
            Assert.That(config.SiegeDawnDuration, Is.EqualTo(5f));
            Assert.That(config.SpawnLineX, Is.EqualTo(27f));
            Assert.That(config.MoatGameplayEnabled, Is.False);
            Assert.That(config.MoatSlowMultiplier, Is.EqualTo(1f));
            Assert.That(config.MoatDamagePerSecond, Is.Zero);
            Assert.That(entityManager.HasComponent<MobileEconomyPriceTuning>(configEntity), Is.True);
            var economyPriceTuning =
                entityManager.GetComponentData<MobileEconomyPriceTuning>(configEntity);
            Assert.That(economyPriceTuning.BedBaseWoodCost, Is.EqualTo(100));
            Assert.That(economyPriceTuning.BedCostGrowthCapacityInterval, Is.EqualTo(25));
            Assert.That(economyPriceTuning.WorkerCapacityBaseWoodCost, Is.EqualTo(100));
            Assert.That(economyPriceTuning.WorkerCapacityBaseIronCost, Is.EqualTo(25));
            Assert.That(economyPriceTuning.WorkerEfficiencyBaseWoodCost, Is.EqualTo(150));
            Assert.That(economyPriceTuning.WorkerEfficiencyBaseIronCost, Is.EqualTo(50));
            Assert.That(economyPriceTuning.WorkerBuildingCostGrowthMultiplier,
                Is.EqualTo(1.35d));
            Assert.That(samples.Length, Is.EqualTo(60));
            Assert.That(samples[0].NightIntensityMult, Is.EqualTo(0.5f));
            Assert.That(samples[4].BloodMoonIntensityMult, Is.EqualTo(1f));

            Entity enemyCatalogEntity = entityManager.CreateEntityQuery(
                typeof(EnemyCatalogRuntimeData), typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            var enemyCatalog = entityManager.GetComponentData<EnemyCatalogRuntimeData>(enemyCatalogEntity);
            var enemyEntries = entityManager.GetBuffer<EnemyCatalogEntryData>(enemyCatalogEntity);
            Assert.That(enemyCatalog.EntryCount, Is.EqualTo(1));
            Assert.That(enemyCatalog.ActiveEntryIndex, Is.Zero);
            Assert.That(enemyEntries.Length, Is.EqualTo(1));
            Assert.That(enemyEntries[0].Id.ToString(), Is.EqualTo("zombie_basic"));
            Assert.That(config.ZombieBaseHP, Is.EqualTo(enemyEntries[0].BaseHP));
            Assert.That(config.ZombieBaseDamage, Is.EqualTo(enemyEntries[0].BaseDamage));
            Assert.That(config.BaseZombieSpeed, Is.EqualTo(enemyEntries[0].BaseMoveSpeed));
            Assert.That(config.ZombieScale, Is.EqualTo(enemyEntries[0].Scale));
            Assert.That(entityManager.GetComponentData<ZombiePrefabData>(enemyCatalogEntity).ZombiePrefab,
                Is.EqualTo(enemyEntries[0].Prefab));
        }

        [UnityTest]
        public IEnumerator EconomyPriceTuning_RuntimePurchaseApisReadBakedComponent()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(MobileEconomyPriceTuning))
                .GetSingletonEntity();
            var tuning = new MobileEconomyPriceTuning
            {
                BedBaseWoodCost = 333,
                BedCostGrowthCapacityInterval = 10,
                WorkerCapacityBaseWoodCost = 222,
                WorkerCapacityBaseIronCost = 33,
                WorkerEfficiencyBaseWoodCost = 444,
                WorkerEfficiencyBaseIronCost = 55,
                WorkerBuildingCostGrowthMultiplier = 2d
            };
            entityManager.SetComponentData(configEntity, tuning);

            Assert.That(gameManager.GetBedCapacityPurchaseCost().Wood, Is.EqualTo(333));
            ResourceCost capacityCost = gameManager.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Wood, WorkerBuildingUpgradeType.Capacity);
            ResourceCost efficiencyCost = gameManager.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Stone, WorkerBuildingUpgradeType.Efficiency);
            Assert.That(capacityCost.Wood, Is.EqualTo(222));
            Assert.That(capacityCost.Iron, Is.EqualTo(33));
            Assert.That(efficiencyCost.Wood, Is.EqualTo(444));
            Assert.That(efficiencyCost.Iron, Is.EqualTo(55));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeDefense_IgnoresInjectedGateCore_AndEndsOnlyWhenWallDies()
        {
            var gameManager = GameManager.Instance;
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

            var hudControllers = Object.FindObjectsByType<HUDController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hud in hudControllers)
            {
                if (!hud.gameObject.activeInHierarchy)
                    continue;

                if (hud.GateHPBar != null)
                    Assert.That(hud.GateHPBar.gameObject.activeSelf, Is.False);
                if (hud.CastleHPBar != null)
                    Assert.That(hud.CastleHPBar.gameObject.activeSelf, Is.False);
                if (hud.DefenseGateText != null)
                    Assert.That(hud.DefenseGateText.gameObject.activeSelf, Is.False);
                if (hud.DefenseCoreText != null)
                    Assert.That(hud.DefenseCoreText.gameObject.activeSelf, Is.False);
            }

            // GameManager'in meta/death transaction'ini bu ECS owner testinden ayir.
            gameManager.enabled = false;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity wallEntity = entityManager.CreateEntityQuery(typeof(WallSegment)).GetSingletonEntity();
            Entity gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateData)).GetSingletonEntity();

            if (!entityManager.HasComponent<GateComponent>(wallEntity))
                entityManager.AddComponentData(wallEntity, new GateComponent());
            if (!entityManager.HasComponent<CastleHP>(wallEntity))
                entityManager.AddComponentData(wallEntity, new CastleHP());

            var wall = entityManager.GetComponentData<WallSegment>(wallEntity);
            wall.MaxHP = 100000f;
            wall.CurrentHP = 100000f;
            entityManager.SetComponentData(wallEntity, wall);
            entityManager.SetComponentData(wallEntity, new GateComponent { MaxHP = 100f, CurrentHP = 0f });
            entityManager.SetComponentData(wallEntity, new CastleHP { MaxHP = 500f, CurrentHP = 0f });

            var gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            entityManager.SetComponentData(gameStateEntity, gameState);

            yield return null;
            yield return null;
            Assert.That(entityManager.GetComponentData<GameStateData>(gameStateEntity).IsGameOver, Is.False);

            wall.CurrentHP = 1f;
            entityManager.SetComponentData(wallEntity, wall);
            entityManager.SetComponentData(wallEntity, new GateComponent { MaxHP = 100f, CurrentHP = 100f });
            entityManager.SetComponentData(wallEntity, new CastleHP { MaxHP = 500f, CurrentHP = 500f });

            Entity attacker = entityManager.CreateEntity(typeof(ZombieTag), typeof(ZombieStats), typeof(ZombieState));
            entityManager.SetComponentData(attacker, new ZombieStats
            {
                CurrentHP = 10f,
                MaxHP = 10f,
                AttackDamage = 10f,
                AttackCooldown = 999f,
                AttackTimer = 0f
            });
            entityManager.SetComponentData(attacker, new ZombieState { Value = ZombieStateType.Attacking });

            yield return null;

            Assert.That(entityManager.GetComponentData<WallSegment>(wallEntity).CurrentHP, Is.Zero);
            Assert.That(entityManager.GetComponentData<GameStateData>(gameStateEntity).IsGameOver, Is.True);
            Assert.That(entityManager.GetComponentData<GateComponent>(wallEntity).CurrentHP, Is.EqualTo(100f));
            Assert.That(entityManager.GetComponentData<CastleHP>(wallEntity).CurrentHP, Is.EqualTo(500f));

            // Enter Play Mode domain reload kapaliyken sonraki test ayni ECS world'u
            // gorebilir. Bu testin urettiği terminal state ve attacker disari sizmasin.
            entityManager.DestroyEntity(attacker);
            wall.CurrentHP = wall.MaxHP;
            entityManager.SetComponentData(wallEntity, wall);
            gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.IsGameOver = false;
            entityManager.SetComponentData(gameStateEntity, gameState);
            gameManager.enabled = true;
        }

        [UnityTest]
        public IEnumerator ContinuousCycle_UsesThirtyFiveTwentyFive_AndNeverZeroIntensity()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);

            Assert.That(config.SiegeCycleDuration, Is.EqualTo(60f));
            Assert.That(config.SiegeDayDuration, Is.EqualTo(30f));
            Assert.That(config.SiegeDuskDuration, Is.EqualTo(5f));
            Assert.That(config.SiegeNightDuration, Is.EqualTo(20f));
            Assert.That(config.SiegeDawnDuration, Is.EqualTo(5f));
            Assert.That(config.SiegeDayDuration + config.SiegeDuskDuration
                + config.SiegeNightDuration + config.SiegeDawnDuration, Is.EqualTo(60f));

            float[] timers = { 1f, 31f, 40f, 57f };
            SiegeCyclePhase[] phases =
            {
                SiegeCyclePhase.Day,
                SiegeCyclePhase.Dusk,
                SiegeCyclePhase.Night,
                SiegeCyclePhase.Dawn
            };

            for (int i = 0; i < timers.Length; i++)
            {
                var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
                cycle.CycleTimer = timers[i];
                entityManager.SetComponentData(configEntity, cycle);
                yield return null;

                cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
                Assert.That(cycle.Phase, Is.EqualTo(phases[i]));
                Assert.That(cycle.SpawnIntensityMultiplier, Is.GreaterThan(0f));
            }
        }

        [UnityTest]
        public IEnumerator AdvancedCycle_IncreasesQuantityButKeepsEnemyStatsFixed()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();

            var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            cycle.CycleIndex = 0;
            cycle.CycleTimer = 1f;
            entityManager.SetComponentData(configEntity, cycle);
            yield return null;
            var dayOne = entityManager.GetComponentData<WaveStateData>(waveEntity);

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            cycle.CycleIndex = 49;
            cycle.CycleTimer = 1f;
            entityManager.SetComponentData(configEntity, cycle);
            yield return null;
            var advanced = entityManager.GetComponentData<WaveStateData>(waveEntity);

            Assert.That(advanced.ZombieHP, Is.EqualTo(dayOne.ZombieHP));
            Assert.That(advanced.ZombieDamage, Is.EqualTo(dayOne.ZombieDamage));
            Assert.That(advanced.ZombieSpeed, Is.EqualTo(dayOne.ZombieSpeed));
            Assert.That(advanced.ZombiesToSpawn, Is.GreaterThan(dayOne.ZombiesToSpawn));
            Assert.That(advanced.SpawnInterval, Is.LessThanOrEqualTo(dayOne.SpawnInterval));
        }

        [UnityTest]
        public IEnumerator StaleSpecialNightSample_CannotCreateRuntimeSpecialNight()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            var samples = entityManager.GetBuffer<DifficultyDaySample>(configEntity);
            var stale = samples[4];
            stale.BloodMoonIntensityMult = 9f;
            samples[4] = stale;

            var cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            cycle.CycleIndex = 4;
            cycle.CycleTimer = 40f;
            cycle.IsBloodMoonNight = true;
            entityManager.SetComponentData(configEntity, cycle);
            yield return null;

            cycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            Assert.That(cycle.Phase, Is.EqualTo(SiegeCyclePhase.Night));
            Assert.That(cycle.IsBloodMoonNight, Is.False);
            Assert.That(cycle.SpawnIntensityMultiplier,
                Is.LessThanOrEqualTo(config.SiegeNightIntensityMultiplier + 0.001f));

            var warnings = Object.FindObjectsByType<BloodMoonWarningUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var warning in warnings)
            {
                if (warning.WarningText != null)
                    Assert.That(warning.WarningText.gameObject.activeSelf, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ContinuousSpawnBudget_AccumulatesAtCap_AndDrainsWhenCapacityOpens()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(ContinuousSiegeCycleData),
                typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();

            var zombieQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            Entity poolEntity = entityManager.CreateEntityQuery(typeof(EnemyPoolRuntimeData)).GetSingletonEntity();
            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);

            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MaxAliveZombies = 1;
            config.MaxSpawnBatch = 4;
            config.MinSpawnInterval = 0.05f;
            entityManager.SetComponentData(configEntity, config);

            var wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 1;
            wave.SpawnTimer = 0f;
            wave.SpawnInterval = 0.05f;
            entityManager.SetComponentData(waveEntity, wave);

            entityManager.SetComponentData(configEntity, new ContinuousSpawnBudgetData());
            yield return null;

            var blockedBudget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            Assert.That(blockedBudget.PendingEnemies, Is.GreaterThan(0));
            Assert.That(blockedBudget.TotalSpawnedEnemies, Is.Zero);

            wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 999f;
            entityManager.SetComponentData(waveEntity, wave);
            long pendingBeforeDrain = blockedBudget.PendingEnemies;

            yield return null;

            var drainedBudget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            var drainedWave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            Assert.That(drainedBudget.LastSpawnedEnemies, Is.EqualTo(1));
            Assert.That(drainedBudget.PendingEnemies, Is.EqualTo(pendingBeforeDrain - 1));
            Assert.That(drainedBudget.TotalSpawnedEnemies, Is.EqualTo(1));
            Assert.That(drainedWave.ZombiesAlive, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StaleMoatTuning_CannotSlowOrDamageZombieInV1Runtime()
        {
            var gameManager = GameManager.Instance;
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
            Entity configEntity = entityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig)).GetSingletonEntity();
            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MoatGameplayEnabled = false;
            config.MoatXMin = -1f;
            config.MoatXMax = 1f;
            config.MoatSlowMultiplier = 0.05f;
            config.MoatDamagePerSecond = 100000f;
            entityManager.SetComponentData(configEntity, config);

            Entity zombie = entityManager.CreateEntity(
                typeof(ZombieTag),
                typeof(ZombieStats),
                typeof(ZombieState),
                typeof(ZombieSlow),
                typeof(LocalTransform));
            entityManager.SetComponentData(zombie, new ZombieStats
            {
                MoveSpeed = 3f,
                MaxHP = 100f,
                CurrentHP = 100f,
                AttackDamage = 0f,
                AttackCooldown = 999f,
                AttackTimer = 999f
            });
            entityManager.SetComponentData(zombie, new ZombieState { Value = ZombieStateType.Queued });
            entityManager.SetComponentData(zombie, new ZombieSlow { Duration = 0f, SpeedMultiplier = 1f });
            entityManager.SetComponentEnabled<ZombieSlow>(zombie, false);
            entityManager.SetComponentData(zombie,
                LocalTransform.FromPositionRotationScale(new float3(0f, 100f, 0f), quaternion.identity, 1f));

            yield return null;
            yield return null;

            var stats = entityManager.GetComponentData<ZombieStats>(zombie);
            Assert.That(stats.CurrentHP, Is.EqualTo(100f));
            Assert.That(stats.MoveSpeed, Is.EqualTo(3f));
            Assert.That(entityManager.IsComponentEnabled<ZombieSlow>(zombie), Is.False);
        }

        [UnityTest]
        public IEnumerator EnemyCatalog_SpawnsRegisteredPrefabWithDefinitionStats()
        {
            var gameManager = GameManager.Instance;
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
            Entity catalogEntity = entityManager.CreateEntityQuery(
                typeof(EnemyCatalogRuntimeData), typeof(EnemyCatalogEntryData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();

            var zombieQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ZombieTag>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, catalogEntity);

            var config = entityManager.GetComponentData<MobileCastleCombatConfig>(configEntity);
            config.MaxAliveZombies = 1;
            entityManager.SetComponentData(configEntity, config);

            var budget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            budget.PendingEnemies = 1;
            budget.TotalDemandedEnemies = 1;
            budget.TotalSpawnedEnemies = 0;
            entityManager.SetComponentData(configEntity, budget);

            var wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 0;
            wave.SpawnTimer = 999f;
            entityManager.SetComponentData(waveEntity, wave);

            yield return null;

            using var zombies = zombieQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.That(zombies.Length, Is.EqualTo(1));

            var runtimeCatalog = entityManager.GetComponentData<EnemyCatalogRuntimeData>(catalogEntity);
            var entries = entityManager.GetBuffer<EnemyCatalogEntryData>(catalogEntity);
            int activeIndex = EnemyCatalogRuntimeUtility.ResolveActiveIndex(runtimeCatalog, entries.Length);
            var definition = entries[activeIndex];
            Entity zombie = zombies[0];
            var stats = entityManager.GetComponentData<ZombieStats>(zombie);
            var transform = entityManager.GetComponentData<LocalTransform>(zombie);

            Assert.That(definition.Id.ToString(), Is.EqualTo("zombie_basic"));
            Assert.That(stats.MaxHP, Is.EqualTo(definition.BaseHP));
            Assert.That(stats.CurrentHP, Is.EqualTo(definition.BaseHP));
            Assert.That(stats.AttackDamage, Is.EqualTo(definition.BaseDamage));
            Assert.That(stats.MoveSpeed, Is.EqualTo(definition.BaseMoveSpeed));
            Assert.That(stats.XPReward, Is.EqualTo(definition.XPReward));
            Assert.That(transform.Scale, Is.EqualTo(definition.Scale));
        }

        [UnityTest]
        public IEnumerator EnemyPool_DeathReturnsEntityAndRejectsStaleArrowGeneration()
        {
            var gameManager = GameManager.Instance;
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
            Entity poolEntity = entityManager.CreateEntityQuery(
                typeof(EnemyPoolRuntimeData), typeof(EnemyPoolAvailable)).GetSingletonEntity();
            Entity waveEntity = entityManager.CreateEntityQuery(typeof(WaveStateData)).GetSingletonEntity();
            Entity configEntity = entityManager.CreateEntityQuery(
                typeof(MobileCastleCombatConfig), typeof(ContinuousSpawnBudgetData)).GetSingletonEntity();

            EnemyPoolRuntimeUtility.ReturnAllActive(entityManager, poolEntity);
            var budget = entityManager.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            budget.PendingEnemies = 0;
            entityManager.SetComponentData(configEntity, budget);

            var wave = entityManager.GetComponentData<WaveStateData>(waveEntity);
            wave.ZombiesAlive = 1;
            wave.SpawnTimer = 999f;
            entityManager.SetComponentData(waveEntity, wave);

            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, poolEntity, out Entity zombie), Is.True);
            uint firstGeneration = entityManager.GetComponentData<EnemyPoolMember>(zombie).Generation;
            entityManager.SetComponentData(zombie,
                LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1.4f));
            entityManager.SetComponentData(zombie, new ZombieStats
            {
                MoveSpeed = 0.85f,
                MaxHP = 20f,
                CurrentHP = 0f,
                AttackDamage = 5f,
                AttackCooldown = 1f,
                AttackTimer = 0f,
                XPReward = 0
            });
            entityManager.SetComponentData(zombie, new ZombieState { Value = ZombieStateType.Dead });
            entityManager.SetComponentData(zombie, new DeathTimer { Value = -1f });
            entityManager.SetComponentEnabled<DeathTimer>(zombie, true);

            Entity arrowPrefab = entityManager.GetComponentData<ArrowPrefabData>(
                entityManager.CreateEntityQuery(typeof(ArrowPrefabData)).GetSingletonEntity()).ArrowPrefab;
            Entity arrowPoolEntity = entityManager.CreateEntityQuery(
                typeof(ArrowPoolRuntimeData), typeof(ArrowPoolAvailable)).GetSingletonEntity();
            ArrowPoolRuntimeUtility.ReturnAllActive(entityManager, arrowPoolEntity);
            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, arrowPoolEntity, arrowPrefab, out Entity arrow), Is.True);
            entityManager.SetComponentData(arrow, LocalTransform.FromPosition(new float3(-3f, 0f, 0f)));
            entityManager.SetComponentData(arrow, new ArrowProjectile
            {
                Speed = 12f,
                Damage = 1f,
                Target = zombie,
                TargetPoolGeneration = firstGeneration,
                ArcherType = ArcherType.Basic,
                SlowDuration = 0f,
                SlowMultiplier = 1f,
                RemainingLifetime = ArrowProjectile.DefaultLifetimeSeconds
            });

            var poolBeforeReturn = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            yield return null;

            Assert.That(entityManager.Exists(zombie), Is.True);
            Assert.That(entityManager.IsComponentEnabled<ZombieTag>(zombie), Is.False);
            Assert.That(entityManager.IsComponentEnabled<DeathTimer>(zombie), Is.False);
            var poolAfterReturn = entityManager.GetComponentData<EnemyPoolRuntimeData>(poolEntity);
            Assert.That(poolAfterReturn.TotalCreated, Is.EqualTo(poolBeforeReturn.TotalCreated));
            Assert.That(poolAfterReturn.TotalReturnCount, Is.EqualTo(poolBeforeReturn.TotalReturnCount + 1));
            Assert.That(entityManager.GetComponentData<WaveStateData>(waveEntity).ZombiesAlive, Is.Zero);

            Assert.That(EnemyPoolRuntimeUtility.TryRent(entityManager, poolEntity, out Entity reused), Is.True);
            Assert.That(reused, Is.EqualTo(zombie));
            uint secondGeneration = entityManager.GetComponentData<EnemyPoolMember>(reused).Generation;
            Assert.That(secondGeneration, Is.Not.EqualTo(firstGeneration));
            entityManager.SetComponentData(reused,
                LocalTransform.FromPositionRotationScale(new float3(3f, 0f, 0f), quaternion.identity, 1.4f));

            yield return null;
            Assert.That(entityManager.Exists(arrow), Is.True);
            Assert.That(entityManager.IsComponentEnabled<ArrowTag>(arrow), Is.False);
            Assert.That(ArrowPoolRuntimeUtility.TryRent(
                entityManager, arrowPoolEntity, arrowPrefab, out Entity reusedArrow), Is.True);
            Assert.That(reusedArrow, Is.EqualTo(arrow));
            ArrowPoolRuntimeUtility.Return(entityManager, arrowPoolEntity, reusedArrow);
            EnemyPoolRuntimeUtility.Return(entityManager, poolEntity, reused);
        }

        [UnityTest]
        public IEnumerator MetaPurchase_ActiveRunRejectedAndDurableDeathAllowsCanonicalUpgrade()
        {
            var gameManager = GameManager.Instance;
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

            MetaUpgradeSO upgrade = gameManager.MetaCatalog.GetUpgrade("start_wood");
            Assert.That(upgrade, Is.Not.Null);
            MetaProgression.State.Souls = 1_000;
            Assert.That(MetaProgression.Save(), Is.True);

            int levelBefore = MetaProgression.GetUpgradeLevel(upgrade.Id);
            int activeRunSouls = MetaProgression.State.Souls;
            Assert.That(gameManager.IsMetaShopPurchaseAllowed, Is.False);
            Assert.That(gameManager.TryBuyMetaUpgrade(upgrade), Is.False);
            Assert.That(MetaProgression.GetUpgradeLevel(upgrade.Id), Is.EqualTo(levelBefore));
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(activeRunSouls));

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateData)).GetSingletonEntity();
            var gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.TotalKills = 17;
            gameState.IsGameOver = true;
            entityManager.SetComponentData(gameStateEntity, gameState);

            Assert.That(gameManager.SaveRunSnapshot(), Is.False);
            Assert.That(gameManager.LastRunResult.Persisted, Is.True);
            Assert.That(gameManager.IsMetaShopPurchaseAllowed, Is.True);

            var spoofed = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            spoofed.Id = upgrade.Id;
            spoofed.EffectType = upgrade.EffectType;
            spoofed.BaseCost = 1;
            Assert.That(gameManager.TryBuyMetaUpgrade(spoofed), Is.False,
                "Catalog disi ayni Id asset'i canonical fiyat/definition owner'ini bypass etmemeli.");
            UnityEngine.Object.Destroy(spoofed);

            int soulsBeforePurchase = MetaProgression.State.Souls;
            int expectedCost = upgrade.GetCost(levelBefore);
            Assert.That(gameManager.TryBuyMetaUpgrade(upgrade), Is.True);
            Assert.That(MetaProgression.GetUpgradeLevel(upgrade.Id), Is.EqualTo(levelBefore + 1));
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(soulsBeforePurchase - expectedCost));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveRunSnapshot_LethalEcsState_CannotRewriteContinueAfterDeath()
        {
            var gameManager = GameManager.Instance;
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

            string runId = gameManager.CurrentRunId;
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity gameStateEntity = entityManager.CreateEntityQuery(typeof(GameStateData)).GetSingletonEntity();
            var gameState = entityManager.GetComponentData<GameStateData>(gameStateEntity);
            gameState.TotalKills = 17;
            gameState.IsGameOver = true;
            entityManager.SetComponentData(gameStateEntity, gameState);

            // Cached Mono state henuz alive olsa bile SaveRunSnapshot ECS truth'ini yeniden
            // okur; death transaction kazanir ve ikinci bir canli snapshot yazilamaz.
            Assert.That(gameManager.SaveRunSnapshot(), Is.False);
            Assert.That(File.Exists(_runSavePath), Is.False);
            Assert.That(File.Exists(_deathReceiptPath), Is.False,
                "Durable meta yazimi basariliysa receipt cleanup tamamlanmali.");
            Assert.That(RunPersistence.TryLoad(), Is.Null);
            Assert.That(MetaProgression.HasRewardedRun(runId), Is.True);

            int soulsAfterDeath = MetaProgression.State.Souls;
            int runsAfterDeath = MetaProgression.State.TotalRuns;
            MetaRunResult duplicate = MetaProgression.AddRunResult(runId, 1, 17);
            Assert.That(duplicate.AlreadyRewarded, Is.True);
            Assert.That(duplicate.Persisted, Is.True);
            Assert.That(MetaProgression.State.Souls, Is.EqualTo(soulsAfterDeath));
            Assert.That(MetaProgression.State.TotalRuns, Is.EqualTo(runsAfterDeath));
            yield return null;
        }

        private static void DeleteFileAndTemp(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }

        private static void RestoreFile(string path, byte[] contents)
        {
            DeleteFileAndTemp(path);
            if (contents != null)
                File.WriteAllBytes(path, contents);
        }
    }
}
