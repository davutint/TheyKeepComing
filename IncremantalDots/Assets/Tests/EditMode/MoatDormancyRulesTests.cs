using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public class MoatDormancyRulesTests
    {
        [Test]
        public void ApplyV1_NeutralizesStaleMoatRuntimeValues()
        {
            var config = new MobileCastleCombatConfig
            {
                MoatGameplayEnabled = true,
                MoatSlowMultiplier = 0.05f,
                MoatDamagePerSecond = 9999f
            };

            MoatDormancyRules.ApplyV1(ref config);

            Assert.That(config.MoatGameplayEnabled, Is.False);
            Assert.That(config.MoatSlowMultiplier, Is.EqualTo(1f));
            Assert.That(config.MoatDamagePerSecond, Is.Zero);
        }

        [Test]
        public void ActiveCatalogs_ExcludeMoatTechAndMetaContent()
        {
            var techCatalog = AssetDatabase.LoadAssetAtPath<TechTreeCatalogSO>(
                "Assets/ScriptableObject/MobileCastle/TechTree/TechTreeCatalog.asset");
            var metaCatalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(
                "Assets/ScriptableObject/MobileCastle/Meta/MetaUpgradeCatalog.asset");

            Assert.That(techCatalog, Is.Not.Null);
            Assert.That(metaCatalog, Is.Not.Null);
            Assert.That(techCatalog.GetNode(MoatDormancyRules.DeeperMoatNodeId), Is.Null);
            Assert.That(techCatalog.GetNode(MoatDormancyRules.BurningMoatNodeId), Is.Null);
            Assert.That(metaCatalog.GetUpgrade(MoatDormancyRules.StartingMoatMetaId), Is.Null);
            Assert.That(techCatalog.ValidateCatalog(), Is.Empty);
            Assert.That(metaCatalog.ValidateCatalog(), Is.Empty);
        }
    }
}
