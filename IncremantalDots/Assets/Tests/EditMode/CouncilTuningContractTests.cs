using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class CouncilTuningContractTests
    {
        private const string ProductionCatalogPath =
            "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";

        [Test]
        public void ProductionCatalog_AuthorsLegacyEquivalentBandDefaultsAndValidMemory()
        {
            CouncilEventCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(ProductionCatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.EffectBands, Is.Not.Null);
            Assert.That(catalog.EffectBands.SmallMultiplier, Is.EqualTo(0.7f));
            Assert.That(catalog.EffectBands.FairMultiplier, Is.EqualTo(1f));
            Assert.That(catalog.EffectBands.GenerousMultiplier, Is.EqualTo(1.4f));
            Assert.That(catalog.EffectBands.SmallWeight, Is.EqualTo(0.35f));
            Assert.That(catalog.EffectBands.FairWeight, Is.EqualTo(0.5f));
            Assert.That(catalog.EffectBands.GenerousWeight, Is.EqualTo(0.15f));
            Assert.That(catalog.EffectBands.BudgetTolerance, Is.EqualTo(1.25f));
            Assert.That(catalog.RecentTemplateMemory, Is.GreaterThanOrEqualTo(1));
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
        }

        [Test]
        public void DifficultyTunerCouncilSurface_UsesCanonicalCatalogScheduleAndDerivedTimer()
        {
            string tunerSource = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/Editor/DifficultyTunerWindow.cs"));
            string runtimeSource = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scripts/MonoBehaviour/GameManager.cs"));
            string productionSubScene = File.ReadAllText(Path.Combine(
                Application.dataPath, "Scenes/NewGameScene/MobileCastleCombatSubScene.unity"));

            StringAssert.Contains("Council Runtime Contract", tunerSource);
            StringAssert.Contains("DefaultCouncilCatalogPath", tunerSource);
            StringAssert.Contains("catalogSO.FindProperty(\"EffectBands\")", tunerSource);
            StringAssert.Contains("CouncilRegularSchedule.FirstRegularDay", tunerSource);
            StringAssert.Contains("CouncilDecisionWindowUtility.GetTotalWindowSeconds", tunerSource);
            StringAssert.Contains("MobileCastleCombatSubScenePath", tunerSource);
            StringAssert.Contains("File.ReadLines(scenePath)", tunerSource);
            StringAssert.DoesNotContain("OpenPreviewScene", tunerSource);
            StringAssert.Contains("Emergency Council yoktur", tunerSource);
            StringAssert.Contains("TrimCouncilRecentTemplatesToCatalogMemory();", runtimeSource);
            StringAssert.Contains("SiegeDawnDuration: 5", productionSubScene);
            StringAssert.Contains("SiegeDayDuration: 30", productionSubScene);
        }
    }
}
