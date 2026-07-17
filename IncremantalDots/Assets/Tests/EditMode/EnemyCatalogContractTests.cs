using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DeadWalls.Tests
{
    public class EnemyCatalogContractTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/Enemies/EnemyCatalog.asset";
        private const string DefinitionPath =
            "Assets/ScriptableObject/MobileCastle/Enemies/BasicZombie.asset";
        private const string EnemyContentRoot =
            "Assets/ScriptableObject/MobileCastle/Enemies";
        private const string CombatSubScenePath =
            "Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity";

        [Test]
        public void ActiveV1Catalog_ContainsOnlyCurrentZombiePrefab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalogSO>(CatalogPath);
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(DefinitionPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(definition, Is.Not.Null);
            Assert.That(catalog.ValidateV1Catalog(), Is.Empty);
            Assert.That(catalog.Definitions, Has.Length.EqualTo(1));
            Assert.That(catalog.GetActiveDefinition(), Is.SameAs(definition));
            Assert.That(definition.Id, Is.EqualTo("zombie_basic"));
            Assert.That(AssetDatabase.GetAssetPath(definition.Prefab),
                Is.EqualTo("Assets/Prefabs/Zombie.prefab"));
        }

        [Test]
        public void Definition_OwnsBaseStatsAndFuturePoolMetadata()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(DefinitionPath);

            Assert.That(definition.ValidateDefinition(), Is.Empty);
            Assert.That(definition.BaseHP, Is.EqualTo(20f));
            Assert.That(definition.BaseDamage, Is.EqualTo(5f));
            Assert.That(definition.BaseMoveSpeed, Is.EqualTo(0.85f));
            Assert.That(definition.Scale, Is.EqualTo(1.4f));
            Assert.That(definition.PoolPrewarm, Is.EqualTo(128));
            Assert.That(definition.PoolExpandBatch, Is.EqualTo(128));
        }

        [Test]
        public void ProductionContentAndSubScene_UseExactlyOneEnemyCatalogDefinitionAndPrefab()
        {
            string[] catalogGuids = AssetDatabase.FindAssets(
                "t:EnemyCatalogSO", new[] { EnemyContentRoot });
            string[] definitionGuids = AssetDatabase.FindAssets(
                "t:EnemyDefinitionSO", new[] { EnemyContentRoot });

            Assert.That(catalogGuids, Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.GUIDToAssetPath(catalogGuids[0]), Is.EqualTo(CatalogPath));
            Assert.That(definitionGuids, Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.GUIDToAssetPath(definitionGuids[0]), Is.EqualTo(DefinitionPath));

            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalogSO>(CatalogPath);
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(DefinitionPath);
            Assert.That(catalog.Definitions, Is.EqualTo(new[] { definition }));

            Scene scene = SceneManager.GetSceneByPath(CombatSubScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
                scene = EditorSceneManager.OpenScene(CombatSubScenePath, OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                WaveConfigAuthoring[] waveOwners = roots
                    .SelectMany(root => root.GetComponentsInChildren<WaveConfigAuthoring>(true))
                    .ToArray();
                MobileCastleCombatAuthoring[] combatOwners = roots
                    .SelectMany(root => root.GetComponentsInChildren<MobileCastleCombatAuthoring>(true))
                    .ToArray();

                Assert.That(waveOwners, Has.Length.EqualTo(1));
                Assert.That(combatOwners, Has.Length.EqualTo(1));
                Assert.That(waveOwners[0].EnemyCatalog, Is.SameAs(catalog));
                Assert.That(combatOwners[0].EnemyCatalog, Is.SameAs(catalog));
                Assert.That(waveOwners[0].ZombiePrefab, Is.SameAs(definition.Prefab));
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void RuntimeSelection_UsesCatalogIndexWithoutEnemyTypeBranching()
        {
            Assert.That(EnemyCatalogRuntimeUtility.ResolveActiveIndex(
                new EnemyCatalogRuntimeData { EntryCount = 1, ActiveEntryIndex = 0 }, 1), Is.Zero);
            Assert.That(EnemyCatalogRuntimeUtility.ResolveActiveIndex(
                new EnemyCatalogRuntimeData { EntryCount = 3, ActiveEntryIndex = 99 }, 3), Is.EqualTo(2));
            Assert.That(EnemyCatalogRuntimeUtility.ResolveActiveIndex(
                new EnemyCatalogRuntimeData { EntryCount = 0, ActiveEntryIndex = 0 }, 0), Is.EqualTo(-1));
        }
    }
}
