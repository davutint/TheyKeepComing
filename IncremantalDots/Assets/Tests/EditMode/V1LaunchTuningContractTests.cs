using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class V1LaunchTuningContractTests
    {
        private const string DifficultyPath =
            "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset";
        private const string ArcherFolder =
            "Assets/ScriptableObject/MobileCastle/Archers/";
        private const string MetaCatalogPath =
            "Assets/ScriptableObject/MobileCastle/Meta/MetaUpgradeCatalog.asset";
        private const string AuthorityDocumentName =
            "DEAD_WALLS_V1_LAUNCH_TUNING_AND_TELEMETRY_TARGETS.md";
        private const string ApprovedTargetFingerprint =
            "58fc60a01a2442fdeaf544f59560159c21ca0e5dff48c0e54f27d817f8059dd3";

        [Test]
        public void ProductionDifficulty_LocksExactQuantityPressureAndEconomyCurves()
        {
            DifficultyProfileSO profile =
                AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(DifficultyPath);

            Assert.That(profile, Is.Not.Null);
            AssertCurve(profile.NightIntensityByDay,
                new Vector2(1f, 0.5f),
                new Vector2(3f, 0.7f),
                new Vector2(5f, 0.85f),
                new Vector2(7f, 1f),
                new Vector2(60f, 1f));
            AssertCurve(profile.ZombieHpMultByDay,
                new Vector2(1f, 1f), new Vector2(60f, 1f));
            AssertCurve(profile.SpawnBatchMultByDay,
                new Vector2(1f, 1f), new Vector2(60f, 1f));

            Assert.That(profile.SampleDays, Is.EqualTo(60));
            Assert.That(profile.ZombieBaseHP, Is.EqualTo(20f));
            Assert.That(profile.ZombieHpGrowthPerCycle, Is.Zero);
            Assert.That(profile.ZombieBaseDamage, Is.EqualTo(5f));
            Assert.That(profile.ZombieDamagePerCycle, Is.Zero);
            Assert.That(profile.SpawnBatchSize, Is.EqualTo(2));
            Assert.That(profile.SpawnBatchGrowthPerCycle, Is.EqualTo(0.15f));
            Assert.That(profile.MaxSpawnBatch, Is.EqualTo(16));
            Assert.That(profile.MaxAliveZombies, Is.EqualTo(900));
            Assert.That(profile.BaseSpawnInterval, Is.EqualTo(0.95f));
            Assert.That(profile.MinSpawnInterval, Is.EqualTo(0.35f));
            Assert.That(profile.DayIntensity, Is.EqualTo(0.55f));
            Assert.That(profile.DuskStartIntensity, Is.EqualTo(1f));
            Assert.That(profile.DuskEndIntensity, Is.EqualTo(1.35f));
            Assert.That(profile.NightIntensity, Is.EqualTo(1.65f));
            Assert.That(profile.DawnIntensity, Is.EqualTo(0.15f));

            Assert.That(profile.WallBaseHp, Is.EqualTo(350f));
            Assert.That(profile.NormalRepairHealPercent, Is.EqualTo(0.25f));
            Assert.That(profile.RepairStonePerMissingHp, Is.EqualTo(0.10f));
            Assert.That(profile.RepairDayPriceMultiplier, Is.EqualTo(1f));
            Assert.That(profile.RallyCooldown, Is.EqualTo(60f));
            Assert.That(profile.EmergencyRepairHealPercent, Is.EqualTo(0.20f));
            Assert.That(profile.EmergencyRepairCooldown, Is.EqualTo(120f));

            Assert.That(profile.PopulationGrowthPerDayPrep, Is.EqualTo(15));
            Assert.That(profile.FoodCostPerArrival, Is.EqualTo(1));
            Assert.That(profile.WoodWorkerProductionPerMin, Is.EqualTo(8f));
            Assert.That(profile.StoneWorkerProductionPerMin, Is.EqualTo(5.5f));
            Assert.That(profile.IronWorkerProductionPerMin, Is.EqualTo(4.9f));
            Assert.That(profile.FoodWorkerProductionPerMin, Is.EqualTo(7f));
            Assert.That(profile.WorkerEfficiencyPercentPerLevel, Is.EqualTo(0.10f));
            Assert.That(profile.BedBaseWoodCost, Is.EqualTo(100));
            Assert.That(profile.BedCostGrowthCapacityInterval, Is.EqualTo(25));
            Assert.That(profile.WorkerCapacityBaseWoodCost, Is.EqualTo(100));
            Assert.That(profile.WorkerCapacityBaseIronCost, Is.EqualTo(25));
            Assert.That(profile.WorkerEfficiencyBaseWoodCost, Is.EqualTo(150));
            Assert.That(profile.WorkerEfficiencyBaseIronCost, Is.EqualTo(50));
            Assert.That(profile.WorkerBuildingCostGrowthMultiplier, Is.EqualTo(1.35d));
            Assert.That(profile.ArrowBaseCapacity, Is.EqualTo(200));
            Assert.That(profile.ArrowCapacityPerLevel, Is.EqualTo(200));
            Assert.That(profile.ArrowRefillPackageSize, Is.EqualTo(100));
            Assert.That(profile.ArrowBaseArrowsPerWood, Is.EqualTo(4));
            Assert.That(profile.ArrowArrowsPerWoodPerEfficiencyLevel, Is.EqualTo(1));
            Assert.That(profile.ArrowCapacityBaseWoodCost, Is.EqualTo(150));
            Assert.That(profile.ArrowCapacityBaseIronCost, Is.EqualTo(25));
            Assert.That(profile.ArrowEfficiencyBaseWoodCost, Is.EqualTo(200));
            Assert.That(profile.ArrowEfficiencyBaseIronCost, Is.EqualTo(50));
            Assert.That(profile.ArrowUpgradeCostGrowthMultiplier, Is.EqualTo(1.35d));
        }

        [Test]
        public void ProductionCombatAndMetaAssets_LockExactLaunchValues()
        {
            AssertArcher("BasicArcher.asset", "basic_archer", ArcherType.Basic,
                new ResourceCost(45, 0, 0, 20), ResourceCost.Zero,
                string.Empty, 10f, 1.5f, 15f, 0f, 1f);
            AssertArcher("RapidArcher.asset", "rapid_archer", ArcherType.Rapid,
                new ResourceCost(55, 0, 35, 20), new ResourceCost(55, 0, 35, 0),
                "rapid_volley", 6f, 3f, 14f, 0f, 1f);
            AssertArcher("FrostArcher.asset", "frost_archer", ArcherType.Frost,
                new ResourceCost(45, 55, 25, 0), new ResourceCost(45, 55, 25, 0),
                "frost_arrows", 5f, 1.2f, 14f, 2f, 0.55f);

            MetaUpgradeCatalogSO meta =
                AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(MetaCatalogPath);
            Assert.That(meta, Is.Not.Null);
            Assert.That(meta.ValidateCatalog(), Is.Empty);
            Assert.That(meta.Upgrades, Has.Length.EqualTo(11));
            Assert.That(meta.RewardSettings.FirstKillBandLimit, Is.EqualTo(100));
            Assert.That(meta.RewardSettings.SecondKillBandLimit, Is.EqualTo(1000));
            Assert.That(meta.RewardSettings.FirstBandSoulsPerKill, Is.EqualTo(1f));
            Assert.That(meta.RewardSettings.SecondBandSoulsPerKill, Is.EqualTo(1f));
            Assert.That(meta.RewardSettings.OverflowSoulsPerKill, Is.EqualTo(1f));
            Assert.That(meta.RewardSettings.SoulsPerDayReached, Is.EqualTo(10f));
            Assert.That(meta.RewardSettings.SoulsPerNightSurvived, Is.EqualTo(25f));
            Assert.That(meta.RewardSettings.SoulsPerPeakPopulation, Is.EqualTo(0.2f));
            Assert.That(meta.RewardSettings.NewRecordSoulsPerDay, Is.EqualTo(50f));
        }

        [Test]
        public void ProductionTelemetryTargets_AreValidCompleteAndFingerprintLocked()
        {
            V1LaunchTelemetryTargetsSO targets =
                AssetDatabase.LoadAssetAtPath<V1LaunchTelemetryTargetsSO>(
                    V1LaunchTelemetryTargetsSO.ProductionAssetPath);

            Assert.That(targets, Is.Not.Null);
            Assert.That(targets.Version, Is.EqualTo(V1LaunchTelemetryTargetsSO.CurrentVersion));
            Assert.That(targets.ProfileId, Is.EqualTo("dead_walls_v1_launch_targets"));
            Assert.That(targets.MinimumCompletedRuns, Is.EqualTo(100));
            Assert.That(targets.Targets, Has.Length.EqualTo(19));
            Assert.That(targets.ValidateProfile(), Is.Empty);
            Assert.That(targets.ComputeFingerprint(), Is.EqualTo(ApprovedTargetFingerprint));

            AssertBand(targets, "fresh_median_run_end_day", 6f, 12f, 100);
            AssertBand(targets, "positive_backlog_phase_rate", 0.05f, 0.30f, 400);
            AssertBand(targets, "median_unused_bed_ratio_at_death", 0f, 0.20f, 100);
            AssertBand(targets, "median_night_wall_damage_ratio", 0.15f, 0.35f, 400);
            AssertBand(targets, "council_expiry_rate", 0.05f, 0.20f, 100);
            AssertBand(targets, "post_meta_purchase_median_day_gain", 1f, 4f, 100);
        }

        [Test]
        public void LaunchAuthorityDocument_ReferencesProductionTargetFingerprint()
        {
            string path = Path.Combine(Application.dataPath, "Docs", AuthorityDocumentName);
            Assert.That(File.Exists(path), Is.True, path);
            string text = File.ReadAllText(path);

            StringAssert.Contains(ApprovedTargetFingerprint, text);
            StringAssert.Contains("V1LaunchTelemetryTargets.asset", text);
            StringAssert.Contains("run_ended v2", text);
            StringAssert.Contains("automatic retuning", text);
        }

        private static void AssertCurve(AnimationCurve curve, params Vector2[] expected)
        {
            Assert.That(curve, Is.Not.Null);
            Assert.That(curve.length, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(curve.keys[i].time, Is.EqualTo(expected[i].x), $"curve key {i} time");
                Assert.That(curve.keys[i].value, Is.EqualTo(expected[i].y), $"curve key {i} value");
            }
        }

        private static void AssertArcher(
            string fileName,
            string id,
            ArcherType type,
            ResourceCost buy,
            ResourceCost retrain,
            string requiredTechId,
            float damage,
            float fireRate,
            float range,
            float slowDuration,
            float slowMultiplier)
        {
            ArcherDefinitionSO definition =
                AssetDatabase.LoadAssetAtPath<ArcherDefinitionSO>(ArcherFolder + fileName);
            Assert.That(definition, Is.Not.Null, fileName);
            Assert.That(definition.Id, Is.EqualTo(id));
            Assert.That(definition.Type, Is.EqualTo(type));
            AssertCost(definition.BuyCost, buy);
            AssertCost(definition.RetrainCost, retrain);
            Assert.That(definition.PopulationCost, Is.EqualTo(1));
            Assert.That(definition.RequiredTechId, Is.EqualTo(requiredTechId));
            Assert.That(definition.CostGrowthInterval, Is.EqualTo(25));
            Assert.That(definition.CostGrowthExponent, Is.EqualTo(2f));
            Assert.That(definition.Damage, Is.EqualTo(damage));
            Assert.That(definition.FireRate, Is.EqualTo(fireRate));
            Assert.That(definition.Range, Is.EqualTo(range));
            Assert.That(definition.SlowDuration, Is.EqualTo(slowDuration));
            Assert.That(definition.SlowMultiplier, Is.EqualTo(slowMultiplier));
        }

        private static void AssertCost(ResourceCost actual, ResourceCost expected)
        {
            Assert.That(actual.Wood, Is.EqualTo(expected.Wood));
            Assert.That(actual.Stone, Is.EqualTo(expected.Stone));
            Assert.That(actual.Iron, Is.EqualTo(expected.Iron));
            Assert.That(actual.Food, Is.EqualTo(expected.Food));
        }

        private static void AssertBand(
            V1LaunchTelemetryTargetsSO targets,
            string id,
            float min,
            float max,
            int samples)
        {
            V1TelemetryTargetDefinition target = targets.GetTarget(id);
            Assert.That(target, Is.Not.Null, id);
            Assert.That(target.MinInclusive, Is.EqualTo(min), id);
            Assert.That(target.MaxInclusive, Is.EqualTo(max), id);
            Assert.That(target.MinimumSamples, Is.EqualTo(samples), id);
        }
    }
}
