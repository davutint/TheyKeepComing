using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class MetaProgressionBoundaryTests
    {
        [Test]
        public void PurchaseRules_RequireDurableDeathAndWritableMetaState()
        {
            Assert.That(MetaPurchaseRules.CanPurchase(true, true, true, true), Is.True);
            Assert.That(MetaPurchaseRules.CanPurchase(false, true, true, true), Is.False);
            Assert.That(MetaPurchaseRules.CanPurchase(true, false, true, true), Is.False);
            Assert.That(MetaPurchaseRules.CanPurchase(true, true, false, true), Is.False);
            Assert.That(MetaPurchaseRules.CanPurchase(true, true, true, false), Is.False);
        }

        [Test]
        public void EffectPolicy_AllowsOnlyBlueprintRunStartAndFuturePoolEffects()
        {
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.StartingResource), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.StartingArchers), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.StartingBeds), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.WallHpPercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.ProductionPercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.ArrowEfficiency), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.EssenceGainPercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.NodePoolUnlock), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunStartEffect(MetaUpgradeEffectType.NodePoolUnlock), Is.False);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.None), Is.False);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect((MetaUpgradeEffectType)3), Is.False,
                "Legacy StartingTechLevel numeric value fail-closed kalmali.");
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect((MetaUpgradeEffectType)5), Is.False,
                "Blueprint disi legacy ArcherDamage numeric value fail-closed kalmali.");
        }

        [Test]
        public void Catalog_RejectsLegacyOrFutureGraphMutatingEffect()
        {
            var upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            var catalog = ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
            try
            {
                upgrade.Id = "legacy_starting_tech";
                upgrade.EffectType = (MetaUpgradeEffectType)3;
                catalog.Upgrades = new[] { upgrade };

                string problems = string.Join("\n", catalog.ValidateCatalog());
                StringAssert.Contains("run graph isolation", problems);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(upgrade);
            }
        }

        [Test]
        public void ProductionCatalog_ContainsOnlyGraphIsolatedEffects()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(
                "Assets/ScriptableObject/MobileCastle/Meta/MetaUpgradeCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.Upgrades.Select(upgrade => upgrade.Id), Is.EqualTo(new[]
            {
                "start_wood", "start_stone", "start_iron", "start_food",
                "start_archers", "start_beds", "wall_hp", "production",
                "arrow_efficiency", "essence_gain", "node_pool_unlock"
            }));
            Assert.That(catalog.Upgrades.All(upgrade =>
                upgrade != null && MetaUpgradePolicy.IsRunGraphIsolatedEffect(upgrade.EffectType)), Is.True);
            Assert.That(catalog.GetUpgrade("start_wood").IsRepeatable, Is.True);
            Assert.That(catalog.GetUpgrade("start_beds").IsRepeatable, Is.True);
            Assert.That(catalog.GetUpgrade("node_pool_unlock").MaxLevel, Is.EqualTo(1));
            Assert.That(catalog.GetUpgrade("node_pool_unlock").PoolContentId, Is.Not.Empty);
            Assert.That(AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(
                "Assets/ScriptableObject/MobileCastle/Meta/Meta_start_moat.asset"), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(
                "Assets/ScriptableObject/MobileCastle/Meta/Meta_archer_damage.asset"), Is.Null);
        }

        [Test]
        public void RepeatableCostCurve_GrowsExponentiallyAndSaturatesSafely()
        {
            var upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            try
            {
                upgrade.Id = "repeatable_test";
                upgrade.BaseCost = 100;
                upgrade.CostGrowthPerLevel = 0.5f;
                upgrade.MaxLevel = 0;
                upgrade.ValuePerLevel = 1f;

                Assert.That(upgrade.IsConfigurationValid(), Is.True);
                Assert.That(upgrade.GetCost(0), Is.EqualTo(100));
                Assert.That(upgrade.GetCost(1), Is.EqualTo(150));
                Assert.That(upgrade.GetCost(2), Is.EqualTo(225));
                Assert.That(upgrade.GetCost(20), Is.GreaterThan(100_000));
                Assert.That(upgrade.GetCost(10_000), Is.EqualTo(int.MaxValue));
                Assert.That(upgrade.IsMaxLevel(10_000), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(upgrade);
            }
        }

        [Test]
        public void NodePoolUnlock_RequiresStableIdAndSinglePurchaseCap()
        {
            var upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            try
            {
                upgrade.Id = "pool_test";
                upgrade.EffectType = MetaUpgradeEffectType.NodePoolUnlock;
                upgrade.BaseCost = 100;
                upgrade.MaxLevel = 0;
                upgrade.PoolContentId = string.Empty;
                Assert.That(upgrade.IsConfigurationValid(), Is.False);

                upgrade.MaxLevel = 1;
                upgrade.PoolContentId = "heart.test_pool";
                Assert.That(upgrade.IsConfigurationValid(), Is.True);
                Assert.That(upgrade.IsMaxLevel(0), Is.False);
                Assert.That(upgrade.IsMaxLevel(1), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(upgrade);
            }
        }

        [Test]
        public void PublicContracts_ExposeNoStartingTechOrRunGraphMutationPath()
        {
            Assert.That(Enum.GetNames(typeof(MetaUpgradeEffectType)), Does.Not.Contain("StartingTechLevel"));
            Assert.That(Enum.GetNames(typeof(MetaUpgradeEffectType)), Does.Not.Contain("ArcherDamagePercent"));
            Assert.That(typeof(MetaUpgradeSO).GetField("TechNodeId"), Is.Null);
            Assert.That(typeof(MetaProgressState).GetField("GeneratedRunGraph"), Is.Null);
            Assert.That(typeof(GameManager).GetMethod("GrantTechNodeLevelsFromMeta",
                BindingFlags.Instance | BindingFlags.NonPublic), Is.Null);
            Assert.That(typeof(MetaProgression).GetMethod("TryBuyUpgrade",
                BindingFlags.Static | BindingFlags.Public), Is.Null,
                "Kalici store mutation'i GameManager olum sinirini bypass etmemeli.");
            Assert.That(typeof(GameManager).GetMethod(nameof(GameManager.TryBuyMetaUpgrade)), Is.Not.Null);
        }
    }
}
