using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DeadWalls.Tests
{
    public class MobileWaveUtilityTests
    {
        private const string DifficultyRoot =
            "Assets/ScriptableObject/MobileCastle/Difficulty";
        private const string DefaultDifficultyPath =
            "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset";
        private const string CombatSubScenePath =
            "Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity";

        [Test]
        public void ConfigureMobileWave_IgnoresStatGrowthFields_ButIncreasesQuantityPressure()
        {
            var config = new MobileCastleCombatConfig
            {
                BaseWaveEnemyCount = 30,
                ExtraEnemiesPerWave = 10,
                ZombieBaseHP = 20f,
                ZombieHpGrowthPerCycle = 9f,
                ZombieBaseDamage = 5f,
                ZombieDamagePerCycle = 9f,
                BaseZombieSpeed = 0.85f,
                ZombieSpeedPerWave = 9f,
                BaseSpawnInterval = 0.95f,
                SpawnIntervalWaveMultiplier = 0.96f,
                MinSpawnInterval = 0.35f
            };

            var dayOne = new WaveStateData { CurrentWave = 1 };
            var advanced = new WaveStateData { CurrentWave = 50 };
            MobileWaveUtility.ConfigureMobileWave(ref dayOne, config);
            MobileWaveUtility.ConfigureMobileWave(ref advanced, config);

            Assert.That(advanced.ZombieHP, Is.EqualTo(dayOne.ZombieHP));
            Assert.That(advanced.ZombieDamage, Is.EqualTo(dayOne.ZombieDamage));
            Assert.That(advanced.ZombieSpeed, Is.EqualTo(dayOne.ZombieSpeed));
            Assert.That(advanced.ZombiesToSpawn, Is.GreaterThan(dayOne.ZombiesToSpawn));
            Assert.That(advanced.SpawnInterval, Is.LessThan(dayOne.SpawnInterval));
        }

        [Test]
        public void ProductionProfileAndSubScene_KeepStatsFixedAndQuantityPressureActive()
        {
            string[] profileGuids = AssetDatabase.FindAssets(
                "t:DifficultyProfileSO", new[] { DifficultyRoot });
            Assert.That(profileGuids, Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.GUIDToAssetPath(profileGuids[0]),
                Is.EqualTo(DefaultDifficultyPath));

            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(DefaultDifficultyPath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.ZombieHpGrowthPerCycle, Is.Zero);
            Assert.That(profile.ZombieDamagePerCycle, Is.Zero);
            Assert.That(profile.ZombieHpMultByDay.Evaluate(1f), Is.EqualTo(1f));
            Assert.That(profile.ZombieHpMultByDay.Evaluate(profile.SampleDays), Is.EqualTo(1f));
            Assert.That(profile.SpawnBatchGrowthPerCycle, Is.GreaterThan(0f));
            Assert.That(profile.MaxSpawnBatch, Is.GreaterThan(profile.SpawnBatchSize));
            Assert.That(profile.BaseSpawnInterval, Is.GreaterThan(profile.MinSpawnInterval));
            Assert.That(profile.NightIntensityByDay.Evaluate(7f),
                Is.GreaterThan(profile.NightIntensityByDay.Evaluate(1f)));

            Scene scene = SceneManager.GetSceneByPath(CombatSubScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
                scene = EditorSceneManager.OpenScene(CombatSubScenePath, OpenSceneMode.Additive);

            try
            {
                MobileCastleCombatAuthoring[] owners = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MobileCastleCombatAuthoring>(true))
                    .ToArray();
                Assert.That(owners, Has.Length.EqualTo(1));

                MobileCastleCombatAuthoring authoring = owners[0];
                Assert.That(authoring.Profile, Is.SameAs(profile));
                Assert.That(authoring.EnemyCatalog, Is.Not.Null);
                Assert.That(authoring.EnemyCatalog.GetActiveDefinition(), Is.Not.Null);
                Assert.That(authoring.ZombieHpGrowthPerCycle, Is.Zero);
                Assert.That(authoring.ZombieDamagePerCycle, Is.Zero);
                Assert.That(authoring.ZombieSpeedPerWave, Is.Zero);
                Assert.That(authoring.ExtraEnemiesPerWave, Is.GreaterThan(0));

                var config = new MobileCastleCombatConfig
                {
                    BaseWaveEnemyCount = authoring.BaseWaveEnemyCount,
                    ExtraEnemiesPerWave = authoring.ExtraEnemiesPerWave,
                    BaseZombieSpeed = authoring.BaseZombieSpeed,
                    ZombieSpeedPerWave = authoring.ZombieSpeedPerWave,
                    BaseSpawnInterval = authoring.BaseSpawnInterval,
                    SpawnIntervalWaveMultiplier = authoring.SpawnIntervalWaveMultiplier,
                    MinSpawnInterval = authoring.MinSpawnInterval
                };
                MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, profile);
                EnemyCatalogRuntimeUtility.ApplyBaseStats(
                    ref config, authoring.EnemyCatalog.GetActiveDefinition());

                var dayOne = new WaveStateData { CurrentWave = 1 };
                var advanced = new WaveStateData { CurrentWave = 50 };
                MobileWaveUtility.ConfigureMobileWave(ref dayOne, config);
                MobileWaveUtility.ConfigureMobileWave(ref advanced, config);

                Assert.That(advanced.ZombieHP, Is.EqualTo(dayOne.ZombieHP));
                Assert.That(advanced.ZombieDamage, Is.EqualTo(dayOne.ZombieDamage));
                Assert.That(advanced.ZombieSpeed, Is.EqualTo(dayOne.ZombieSpeed));
                Assert.That(advanced.ZombiesToSpawn, Is.GreaterThan(dayOne.ZombiesToSpawn));
                Assert.That(advanced.SpawnInterval, Is.LessThan(dayOne.SpawnInterval));
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
