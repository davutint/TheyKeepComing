using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class EnemyCatalogContractTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/Enemies/EnemyCatalog.asset";
        private const string DefinitionPath =
            "Assets/ScriptableObject/MobileCastle/Enemies/BasicZombie.asset";

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
