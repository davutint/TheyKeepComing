using Unity.Entities;
using UnityEngine;

namespace DeadWalls
{
    public class GameStateAuthoring : MonoBehaviour
    {
        [Header("Game State")]
        public int XPToNextLevel = 100;

        [Header("Wave Config — STRESS TEST")]
        public bool StressTestMode = false;
        public int InitialZombiesToSpawn = 500;
        public float InitialZombieHP = 20f;
        public float InitialZombieDamage = 5f;
        public float SpawnInterval = 0.05f;
        public float WaveStartDelay = 2f;
        public float BaseZombieSpeed = 2f;

        [Header("Resources — Baslangic")]
        public int InitialWood = 100;
        public int InitialStone = 50;
        public int InitialIron = 20;
        public int InitialFood = 100;

        [Header("Resources — Test Uretim (dk basina)")]
        public float TestWoodProdRate = 0f;
        public float TestStoneProdRate = 0f;
        public float TestIronProdRate = 0f;
        public float TestFoodProdRate = 0f;

        [Header("Resources — Test Tuketim (dk basina)")]
        public float TestWoodConsRate = 0f;
        public float TestStoneConsRate = 0f;
        public float TestIronConsRate = 0f;
        public float TestFoodConsRate = 0f;

        [Header("Population — Baslangic")]
        public int InitialPopulation = 10;
        public int InitialCapacity = 20;

        [Header("Population — Test Atama")]
        public int TestWorkers = 0;
        public int TestArchers = 0;

        [Header("Population — Tuketim")]
        public float FoodPerAssignedPerMin = 2f;

        [Header("Arrow Supply — Baslangic")]
        public int InitialArrows = 50;

        [Header("Castle Heart — Baslangic")]
        public long InitialGraveEssence = 0;

        public class Baker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GameStateData
                {
                    XP = 0,
                    Level = 1,
                    XPToNextLevel = authoring.XPToNextLevel,
                    IsGameOver = false,
                    IsLevelUpPending = false
                });
                AddComponent(entity, new RunTelemetryData { PeakEnemies = 0 });
                AddBuffer<RunWallDamageTelemetryElement>(entity);

                AddComponent(entity, new WaveStateData
                {
                    CurrentWave = 1,
                    ZombiesToSpawn = authoring.InitialZombiesToSpawn,
                    ZombiesSpawned = 0,
                    ZombiesAlive = 0,
                    SpawnTimer = 0f,
                    SpawnInterval = authoring.SpawnInterval,
                    ZombieHP = authoring.InitialZombieHP,
                    ZombieDamage = authoring.InitialZombieDamage,
                    ZombieSpeed = authoring.BaseZombieSpeed,
                    WaveActive = true,
                    Phase = RunPhaseType.NightCombat,
                    PrepTimer = 0f,
                    PrepDuration = 0f,
                    WaveStartDelay = authoring.WaveStartDelay,
                    WaveStartTimer = authoring.WaveStartDelay,
                    StressTestMode = authoring.StressTestMode,
                    SpawnRandomState = 42u
                });

                AddComponent(entity, new ResourceData
                {
                    Wood = authoring.InitialWood,
                    Stone = authoring.InitialStone,
                    Iron = authoring.InitialIron,
                    Food = authoring.InitialFood
                });

                AddComponent(entity, new ResourceProductionRate
                {
                    WoodPerMin = authoring.TestWoodProdRate,
                    StonePerMin = authoring.TestStoneProdRate,
                    IronPerMin = authoring.TestIronProdRate,
                    FoodPerMin = authoring.TestFoodProdRate
                });

                AddComponent(entity, new ResourceConsumptionRate
                {
                    WoodPerMin = authoring.TestWoodConsRate,
                    StonePerMin = authoring.TestStoneConsRate,
                    IronPerMin = authoring.TestIronConsRate,
                    FoodPerMin = authoring.TestFoodConsRate
                });

                AddComponent(entity, new ResourceAccumulator
                {
                    Wood = 0f,
                    Stone = 0f,
                    Iron = 0f,
                    Food = 0f
                });

                AddComponent(entity, new PopulationState
                {
                    Total = authoring.InitialPopulation,
                    Workers = authoring.TestWorkers,
                    Archers = authoring.TestArchers,
                    Idle = authoring.InitialPopulation - authoring.TestWorkers - authoring.TestArchers,
                    Capacity = authoring.InitialCapacity,
                    BaseCapacity = authoring.InitialCapacity,
                    FoodPerAssignedPerMin = authoring.FoodPerAssignedPerMin
                });

                AddComponent(entity, new ArrowSupply
                {
                    Current = authoring.InitialArrows,
                    CapacityLevel = 0,
                    EfficiencyLevel = 0,
                    Accumulator = 0f
                });

                AddComponent(entity, new GraveEssence
                {
                    Current = authoring.InitialGraveEssence < 0
                        ? 0
                        : authoring.InitialGraveEssence
                });
            }
        }
    }
}
