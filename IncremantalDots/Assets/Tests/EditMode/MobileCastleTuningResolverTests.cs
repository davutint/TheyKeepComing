using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeadWalls.Tests
{
    public class MobileCastleTuningResolverTests
    {
        [Test]
        public void DifficultyProfile_OverridesOnlyProfileOwnedFields()
        {
            var profile = ScriptableObject.CreateInstance<DifficultyProfileSO>();
            try
            {
                profile.SpawnBatchSize = 7;
                profile.ZombieBaseHP = 44f;
                profile.ZombieHpGrowthPerCycle = 0.9f;
                profile.MaxSpawnBatch = 23;
                profile.DayIntensity = 0.4f;
                profile.NightIntensity = 2.2f;
                profile.WallBaseHp = 555f;
                profile.RepairBaseStoneCost = 77;
                profile.NormalRepairHealPercent = 0.30f;
                profile.RepairStonePerMissingHp = 0.42f;
                profile.RepairDayPriceMultiplier = 1.3f;
                profile.RallyCooldown = 48f;
                profile.EmergencyRepairHealPercent = 0.35f;
                profile.EmergencyRepairCooldown = 95f;
                profile.PopulationGrowthPerDayPrep = 23;
                profile.FoodCostPerArrival = 4;
                profile.WoodWorkerProductionPerMin = 11f;
                profile.StoneWorkerProductionPerMin = 12f;
                profile.IronWorkerProductionPerMin = 13f;
                profile.FoodWorkerProductionPerMin = 14f;

                var config = new MobileCastleCombatConfig
                {
                    SpawnBatchSize = 2,
                    ZombieBaseHP = 20f,
                    ZombieHpGrowthPerCycle = 0.3f,
                    MaxSpawnBatch = 12,
                    SiegeDayIntensityMultiplier = 0.55f,
                    SiegeNightIntensityMultiplier = 1.65f,
                    WallBaseHp = 222f,
                    RepairBaseStoneCost = 80,
                    SiegeCycleDuration = 91f,
                    SiegeDayDuration = 31f,
                    SpawnLineX = 27f,
                    PopulationGrowthPerDayPrep = 15,
                    FoodCostPerArrival = 1,
                    InitialBedCapacity = 71,
                    WoodWorkerProductionPerMin = 8f,
                    StoneWorkerProductionPerMin = 5.5f,
                    IronWorkerProductionPerMin = 4.9f,
                    FoodWorkerProductionPerMin = 7f
                };

                MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);

                Assert.That(config.SpawnBatchSize, Is.EqualTo(7));
                Assert.That(config.ZombieBaseHP, Is.EqualTo(44f));
                Assert.That(config.ZombieHpGrowthPerCycle, Is.EqualTo(0.9f));
                Assert.That(config.MaxSpawnBatch, Is.EqualTo(23));
                Assert.That(config.SiegeDayIntensityMultiplier, Is.EqualTo(0.4f));
                Assert.That(config.SiegeNightIntensityMultiplier, Is.EqualTo(2.2f));
                Assert.That(config.WallBaseHp, Is.EqualTo(555f));
                Assert.That(config.RepairBaseStoneCost, Is.EqualTo(77));
                Assert.That(config.NormalRepairHealPercent, Is.EqualTo(0.30f));
                Assert.That(config.RepairStonePerMissingHp, Is.EqualTo(0.42f));
                Assert.That(config.RepairDayPriceMultiplier, Is.EqualTo(1.3f));
                Assert.That(config.RallyCooldown, Is.EqualTo(48f));
                Assert.That(config.EmergencyRepairHealPercent, Is.EqualTo(0.35f));
                Assert.That(config.EmergencyRepairCooldown, Is.EqualTo(95f));
                Assert.That(config.PopulationGrowthPerDayPrep, Is.EqualTo(23));
                Assert.That(config.FoodCostPerArrival, Is.EqualTo(4));
                Assert.That(config.InitialBedCapacity, Is.EqualTo(71));
                Assert.That(config.SiegeCycleDuration, Is.EqualTo(91f));
                Assert.That(config.SiegeDayDuration, Is.EqualTo(31f));
                Assert.That(config.SpawnLineX, Is.EqualTo(27f));
                Assert.That(config.WoodWorkerProductionPerMin, Is.EqualTo(11f));
                Assert.That(config.StoneWorkerProductionPerMin, Is.EqualTo(12f));
                Assert.That(config.IronWorkerProductionPerMin, Is.EqualTo(13f));
                Assert.That(config.FoodWorkerProductionPerMin, Is.EqualTo(14f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ActiveSubScene_AssignsDefaultProfile_AndResolvesItsDivergentValues()
        {
            const string profilePath = "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset";
            const string subScenePath = "Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity";
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(profilePath);
            Assert.That(profile, Is.Not.Null);

            Scene scene = EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Additive);
            try
            {
                MobileCastleCombatAuthoring authoring = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    authoring = root.GetComponentInChildren<MobileCastleCombatAuthoring>(true);
                    if (authoring != null)
                        break;
                }

                Assert.That(authoring, Is.Not.Null);
                Assert.That(authoring.Profile, Is.SameAs(profile));
                Assert.That(authoring.ZombieHpGrowthPerCycle, Is.EqualTo(0f));
                Assert.That(profile.ZombieHpGrowthPerCycle, Is.EqualTo(0f));
                Assert.That(authoring.MaxSpawnBatch, Is.Not.EqualTo(profile.MaxSpawnBatch));
                Assert.That(authoring.RepairBaseStoneCost, Is.Not.EqualTo(profile.RepairBaseStoneCost));

                var config = new MobileCastleCombatConfig
                {
                    ZombieHpGrowthPerCycle = authoring.ZombieHpGrowthPerCycle,
                    MaxSpawnBatch = authoring.MaxSpawnBatch,
                    WallBaseHp = 0f,
                    RepairBaseStoneCost = authoring.RepairBaseStoneCost,
                    SiegeDayDuration = authoring.SiegeDayDuration,
                    SiegeDuskDuration = authoring.SiegeDuskDuration,
                    SiegeNightDuration = authoring.SiegeNightDuration,
                    SiegeDawnDuration = authoring.SiegeDawnDuration,
                    SpawnLineX = authoring.SpawnLineX,
                    PopulationGrowthPerDayPrep = authoring.PopulationGrowthPerDayPrep,
                    FoodCostPerArrival = authoring.FoodCostPerArrival,
                    InitialBedCapacity = authoring.InitialBedCapacity,
                    WoodWorkerProductionPerMin = authoring.WoodWorkerProductionPerMin,
                    StoneWorkerProductionPerMin = authoring.StoneWorkerProductionPerMin,
                    IronWorkerProductionPerMin = authoring.IronWorkerProductionPerMin,
                    FoodWorkerProductionPerMin = authoring.FoodWorkerProductionPerMin
                };

                MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);
                Assert.That(config.ZombieHpGrowthPerCycle, Is.Zero);
                Assert.That(config.MaxSpawnBatch, Is.EqualTo(profile.MaxSpawnBatch));
                Assert.That(profile.WallBaseHp, Is.EqualTo(350f));
                Assert.That(config.WallBaseHp, Is.EqualTo(profile.WallBaseHp));
                Assert.That(config.RepairBaseStoneCost, Is.EqualTo(profile.RepairBaseStoneCost));
                Assert.That(config.SiegeDayDuration, Is.EqualTo(authoring.SiegeDayDuration));
                Assert.That(config.SiegeDuskDuration, Is.EqualTo(authoring.SiegeDuskDuration));
                Assert.That(config.SiegeNightDuration, Is.EqualTo(authoring.SiegeNightDuration));
                Assert.That(config.SiegeDawnDuration, Is.EqualTo(authoring.SiegeDawnDuration));
                Assert.That(config.SpawnLineX, Is.EqualTo(authoring.SpawnLineX));
                Assert.That(profile.PopulationGrowthPerDayPrep,
                    Is.EqualTo(MobilePopulationArrivalUtility.DefaultRequestedArrivalsPerDawn));
                Assert.That(profile.FoodCostPerArrival,
                    Is.EqualTo(MobilePopulationArrivalUtility.DefaultFoodCostPerArrival));
                Assert.That(config.PopulationGrowthPerDayPrep,
                    Is.EqualTo(profile.PopulationGrowthPerDayPrep));
                Assert.That(config.FoodCostPerArrival,
                    Is.EqualTo(profile.FoodCostPerArrival));
                Assert.That(config.InitialBedCapacity, Is.EqualTo(authoring.InitialBedCapacity));
                Assert.That(profile.IronWorkerProductionPerMin, Is.EqualTo(4.9f));
                Assert.That(config.WoodWorkerProductionPerMin,
                    Is.EqualTo(profile.WoodWorkerProductionPerMin));
                Assert.That(config.StoneWorkerProductionPerMin,
                    Is.EqualTo(profile.StoneWorkerProductionPerMin));
                Assert.That(config.IronWorkerProductionPerMin,
                    Is.EqualTo(profile.IronWorkerProductionPerMin));
                Assert.That(config.FoodWorkerProductionPerMin,
                    Is.EqualTo(profile.FoodWorkerProductionPerMin));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DaySample_UsesSameCurves_ButKeepsSpecialNightsDormantInV1()
        {
            var profile = ScriptableObject.CreateInstance<DifficultyProfileSO>();
            try
            {
                profile.SampleDays = 10;
                profile.NightIntensityByDay = AnimationCurve.Linear(1f, 0.5f, 10f, 1.4f);
                profile.ZombieHpMultByDay = AnimationCurve.Constant(1f, 10f, 1.25f);
                profile.SpawnBatchMultByDay = AnimationCurve.Constant(1f, 10f, 1.5f);
                profile.SpecialNights = new[]
                {
                    new SpecialNightEntry { EveryNDays = 5, IntensityBonus = 0.5f }
                };

                DifficultyDaySample normal = MobileCastleTuningResolver.ResolveDaySample(profile, 4);
                DifficultyDaySample special = MobileCastleTuningResolver.ResolveDaySample(profile, 5);

                Assert.That(normal.BloodMoonIntensityMult, Is.EqualTo(1f));
                Assert.That(special.BloodMoonIntensityMult, Is.EqualTo(1f));
                Assert.That(special.ZombieHpMult, Is.EqualTo(1.25f));
                Assert.That(special.SpawnBatchMult, Is.EqualTo(1.5f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void RunDifficultyProfile_ClosesSpawnCurvePhaseCapAndPreservedBacklogContract()
        {
            const string profilePath =
                "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset";
            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(profilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.SpawnBatchMultByDay, Is.Not.Null);
            Assert.That(profile.SpawnBatchMultByDay.length, Is.GreaterThan(0));

            DifficultyDaySample firstDay =
                MobileCastleTuningResolver.ResolveDaySample(profile, 1);
            Assert.That(firstDay.SpawnBatchMult,
                Is.EqualTo(profile.EvaluateCurve(profile.SpawnBatchMultByDay, 1)));

            var config = new MobileCastleCombatConfig
            {
                MaxAliveZombies = -1,
                MaxSpawnBatch = -1,
                BaseSpawnInterval = -1f,
                MinSpawnInterval = -1f
            };
            MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);

            Assert.That(config.SiegeDayIntensityMultiplier, Is.EqualTo(profile.DayIntensity));
            Assert.That(config.SiegeDuskStartIntensityMultiplier,
                Is.EqualTo(profile.DuskStartIntensity));
            Assert.That(config.SiegeDuskEndIntensityMultiplier,
                Is.EqualTo(profile.DuskEndIntensity));
            Assert.That(config.SiegeNightIntensityMultiplier, Is.EqualTo(profile.NightIntensity));
            Assert.That(config.SiegeDawnIntensityMultiplier, Is.EqualTo(profile.DawnIntensity));
            Assert.That(config.MaxAliveZombies, Is.EqualTo(profile.MaxAliveZombies));
            Assert.That(config.MaxSpawnBatch, Is.EqualTo(profile.MaxSpawnBatch));
            Assert.That(config.BaseSpawnInterval, Is.EqualTo(profile.BaseSpawnInterval));
            Assert.That(config.MinSpawnInterval, Is.EqualTo(profile.MinSpawnInterval));

            long pending = ContinuousSpawnBudgetUtility.AddDemand(
                pendingEnemies: 0,
                demandPerInterval: 7,
                elapsedIntervals: 3);
            Assert.That(pending, Is.EqualTo(21));
            Assert.That(ContinuousSpawnBudgetUtility.ResolveDrainCount(
                pending,
                zombiesAlive: config.MaxAliveZombies,
                maxAliveZombies: config.MaxAliveZombies,
                maxDrainPerFrame: config.MaxSpawnBatch), Is.Zero);

            int drained = ContinuousSpawnBudgetUtility.ResolveDrainCount(
                pending,
                zombiesAlive: config.MaxAliveZombies - 5,
                maxAliveZombies: config.MaxAliveZombies,
                maxDrainPerFrame: config.MaxSpawnBatch);
            Assert.That(drained, Is.EqualTo(5));
            Assert.That(pending - drained, Is.EqualTo(16));
        }

        [Test]
        public void EconomyPriceTuning_UsesProfileValuesAndSanitizesInvalidInputs()
        {
            MobileEconomyPriceTuning fallback =
                MobileCastleTuningResolver.ResolveEconomyPriceTuning(null);
            Assert.That(fallback.BedBaseWoodCost, Is.EqualTo(100));
            Assert.That(fallback.WorkerBuildingCostGrowthMultiplier, Is.EqualTo(1.35d));
            Assert.That(fallback.WorkerEfficiencyPercentPerLevel, Is.EqualTo(0.10f));

            var profile = ScriptableObject.CreateInstance<DifficultyProfileSO>();
            try
            {
                profile.BedBaseWoodCost = 220;
                profile.BedCostGrowthCapacityInterval = 0;
                profile.WorkerCapacityBaseWoodCost = -5;
                profile.WorkerCapacityBaseIronCost = 40;
                profile.WorkerEfficiencyBaseWoodCost = 500;
                profile.WorkerEfficiencyBaseIronCost = 0;
                profile.WorkerBuildingCostGrowthMultiplier = double.NaN;
                profile.WorkerEfficiencyPercentPerLevel = float.NaN;

                MobileEconomyPriceTuning tuning =
                    MobileCastleTuningResolver.ResolveEconomyPriceTuning(profile);

                Assert.That(tuning.BedBaseWoodCost, Is.EqualTo(220));
                Assert.That(tuning.BedCostGrowthCapacityInterval, Is.EqualTo(1));
                Assert.That(tuning.WorkerCapacityBaseWoodCost, Is.EqualTo(1));
                Assert.That(tuning.WorkerCapacityBaseIronCost, Is.EqualTo(40));
                Assert.That(tuning.WorkerEfficiencyBaseWoodCost, Is.EqualTo(500));
                Assert.That(tuning.WorkerEfficiencyBaseIronCost, Is.EqualTo(1));
                Assert.That(tuning.WorkerBuildingCostGrowthMultiplier, Is.EqualTo(1.35d));
                Assert.That(tuning.WorkerEfficiencyPercentPerLevel, Is.EqualTo(0.10f));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PopulationRuntimeTuning_UsesProfileValuesAndSanitizesInvalidInputs()
        {
            var fallback = new MobileCastleCombatConfig
            {
                PopulationGrowthPerDayPrep = 19,
                FoodCostPerArrival = 3,
                InitialBedCapacity = 72
            };
            MobileCastleTuningResolver.ApplyDifficultyProfile(ref fallback, null);
            Assert.That(fallback.PopulationGrowthPerDayPrep, Is.EqualTo(19));
            Assert.That(fallback.FoodCostPerArrival, Is.EqualTo(3));
            Assert.That(fallback.InitialBedCapacity, Is.EqualTo(72));

            var profile = ScriptableObject.CreateInstance<DifficultyProfileSO>();
            try
            {
                profile.PopulationGrowthPerDayPrep = -5;
                profile.FoodCostPerArrival = 0;

                MobileCastleTuningResolver.ApplyDifficultyProfile(ref fallback, profile);

                Assert.That(fallback.PopulationGrowthPerDayPrep, Is.Zero);
                Assert.That(fallback.FoodCostPerArrival, Is.EqualTo(1));
                Assert.That(fallback.InitialBedCapacity, Is.EqualTo(72));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
