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
                profile.RepairBaseStoneCost = 77;

                var config = new MobileCastleCombatConfig
                {
                    SpawnBatchSize = 2,
                    ZombieBaseHP = 20f,
                    ZombieHpGrowthPerCycle = 0.3f,
                    MaxSpawnBatch = 12,
                    SiegeDayIntensityMultiplier = 0.55f,
                    SiegeNightIntensityMultiplier = 1.65f,
                    RepairBaseStoneCost = 80,
                    SiegeCycleDuration = 91f,
                    SiegeDayDuration = 31f,
                    SpawnLineX = 27f,
                    IronWorkerProductionPerMin = 4.9f
                };

                MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);

                Assert.That(config.SpawnBatchSize, Is.EqualTo(7));
                Assert.That(config.ZombieBaseHP, Is.EqualTo(44f));
                Assert.That(config.ZombieHpGrowthPerCycle, Is.EqualTo(0.9f));
                Assert.That(config.MaxSpawnBatch, Is.EqualTo(23));
                Assert.That(config.SiegeDayIntensityMultiplier, Is.EqualTo(0.4f));
                Assert.That(config.SiegeNightIntensityMultiplier, Is.EqualTo(2.2f));
                Assert.That(config.RepairBaseStoneCost, Is.EqualTo(77));
                Assert.That(config.SiegeCycleDuration, Is.EqualTo(91f));
                Assert.That(config.SiegeDayDuration, Is.EqualTo(31f));
                Assert.That(config.SpawnLineX, Is.EqualTo(27f));
                Assert.That(config.IronWorkerProductionPerMin, Is.EqualTo(4.9f));
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
                    RepairBaseStoneCost = authoring.RepairBaseStoneCost,
                    SiegeDayDuration = authoring.SiegeDayDuration,
                    SiegeDuskDuration = authoring.SiegeDuskDuration,
                    SiegeNightDuration = authoring.SiegeNightDuration,
                    SiegeDawnDuration = authoring.SiegeDawnDuration,
                    SpawnLineX = authoring.SpawnLineX
                };

                MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);
                Assert.That(config.ZombieHpGrowthPerCycle, Is.Zero);
                Assert.That(config.MaxSpawnBatch, Is.EqualTo(profile.MaxSpawnBatch));
                Assert.That(config.RepairBaseStoneCost, Is.EqualTo(profile.RepairBaseStoneCost));
                Assert.That(config.SiegeDayDuration, Is.EqualTo(authoring.SiegeDayDuration));
                Assert.That(config.SiegeDuskDuration, Is.EqualTo(authoring.SiegeDuskDuration));
                Assert.That(config.SiegeNightDuration, Is.EqualTo(authoring.SiegeNightDuration));
                Assert.That(config.SiegeDawnDuration, Is.EqualTo(authoring.SiegeDawnDuration));
                Assert.That(config.SpawnLineX, Is.EqualTo(authoring.SpawnLineX));
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
    }
}
