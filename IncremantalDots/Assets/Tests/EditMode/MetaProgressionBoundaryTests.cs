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
        public void EffectPolicy_AllowsOnlyRunStartAndAggregateEffects()
        {
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.StartingResource), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.StartingArchers), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.WallHpPercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.ArcherDamagePercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.ProductionPercent), Is.True);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect(MetaUpgradeEffectType.None), Is.False);
            Assert.That(MetaUpgradePolicy.IsRunGraphIsolatedEffect((MetaUpgradeEffectType)3), Is.False,
                "Legacy StartingTechLevel numeric value fail-closed kalmali.");
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
            Assert.That(catalog.Upgrades, Is.Not.Empty);
            Assert.That(catalog.Upgrades.All(upgrade =>
                upgrade != null && MetaUpgradePolicy.IsRunGraphIsolatedEffect(upgrade.EffectType)), Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(
                "Assets/ScriptableObject/MobileCastle/Meta/Meta_start_moat.asset"), Is.Null);
        }

        [Test]
        public void PublicContracts_ExposeNoStartingTechOrRunGraphMutationPath()
        {
            Assert.That(Enum.GetNames(typeof(MetaUpgradeEffectType)), Does.Not.Contain("StartingTechLevel"));
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
