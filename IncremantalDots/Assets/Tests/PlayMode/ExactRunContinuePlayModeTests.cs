using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DeadWalls.Tests
{
    public class ExactRunContinuePlayModeTests
    {
        private string _runSavePath;
        private byte[] _originalRunSave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _runSavePath = Path.Combine(Application.persistentDataPath, "run_save.json");
            _originalRunSave = File.Exists(_runSavePath) ? File.ReadAllBytes(_runSavePath) : null;
            RunPersistence.Delete();
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
            RunPersistence.Delete();
            if (_originalRunSave != null)
                File.WriteAllBytes(_runSavePath, _originalRunSave);
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
            Entity cycleEntity = entityManager.CreateEntityQuery(typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            Entity resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData), typeof(WaveStateData)).GetSingletonEntity();

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

            Assert.That(gameManager.TryRestoreRunFromCheckpoint(), Is.True);

            cycleEntity = entityManager.CreateEntityQuery(typeof(ContinuousSiegeCycleData)).GetSingletonEntity();
            resourceEntity = entityManager.CreateEntityQuery(typeof(ResourceData), typeof(WaveStateData)).GetSingletonEntity();
            var restoredCycle = entityManager.GetComponentData<ContinuousSiegeCycleData>(cycleEntity);
            var restoredResources = entityManager.GetComponentData<ResourceData>(resourceEntity);
            var restoredWave = entityManager.GetComponentData<WaveStateData>(resourceEntity);

            Assert.That(gameManager.CurrentRunId, Is.EqualTo(savedRunId));
            Assert.That(restoredCycle.CycleIndex, Is.EqualTo(9));
            Assert.That(restoredCycle.Phase, Is.EqualTo(SiegeCyclePhase.Night));
            Assert.That(restoredCycle.CycleTimer, Is.EqualTo(47.25f).Within(0.001f));
            Assert.That(restoredResources.Wood, Is.EqualTo(4321));
            Assert.That(restoredResources.Stone, Is.EqualTo(3210));
            Assert.That(restoredResources.Iron, Is.EqualTo(2109));
            Assert.That(restoredResources.Food, Is.EqualTo(1098));
            Assert.That(restoredWave.SpawnRandomState, Is.EqualTo(987654321u));
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
            Assert.That(gameManager.RepairDefenseFull(), Is.True);
            var repairedWall = entityManager.GetComponentData<WallSegment>(wallEntity);
            var resourcesAfter = entityManager.GetComponentData<ResourceData>(resourceEntity);
            Assert.That(repairedWall.CurrentHP, Is.EqualTo(repairedWall.MaxHP));
            Assert.That(resourcesAfter.Wood, Is.Zero);
            Assert.That(resourcesAfter.Stone, Is.EqualTo(stoneBefore - cost.Stone));
        }
    }
}
