using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartTuningContractTests
    {
        [Test]
        public void ProductionEssenceGain_UsesApprovedProbabilisticEnemyDeathOwner()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] scripts = Directory.GetFiles(
                scriptsRoot,
                "*.cs",
                SearchOption.AllDirectories);
            int grantReferences = 0;
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i].Contains(
                        Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar))
                    continue;

                if (Path.GetFileName(scripts[i]) == "GameManagerDevelopmentTools.cs")
                    continue;

                string source = File.ReadAllText(scripts[i]);
                grantReferences += Regex.Matches(source, @"\bGrantGraveEssence\s*\(").Count;
            }

            Assert.That(grantReferences, Is.EqualTo(2),
                "Production'da yalniz declaration ve GraveEssenceDropEvent consumer grant kapisini kullanmali.");

            string deathSource = File.ReadAllText(Path.Combine(
                scriptsRoot, "ECS/Systems/ZombieDeathSystem.cs"));
            StringAssert.Contains("GraveEssenceDropUtility.ShouldDrop", deathSource);
            StringAssert.Contains("GraveEssenceDropEvent", deathSource);
            StringAssert.Contains("!waveState.StressTestMode", deathSource);

            DifficultyProfileSO profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(
                "Assets/ScriptableObject/MobileCastle/Difficulty/DefaultDifficulty.asset");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.GraveEssenceDropChance, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(profile.GraveEssencePerDrop, Is.EqualTo(1));
        }

        [Test]
        public void DifficultyTunerHeartSurface_UsesCanonicalPricingGeneratorAndFutureRunBoundary()
        {
            string source = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Editor/DifficultyTunerWindow.cs"));

            StringAssert.Contains("Heart Runtime Contract", source);
            StringAssert.Contains("HeartPurchasePricing.TryGetLevelCost", source);
            StringAssert.Contains("HeartPurchasePricing.TryGetTotalCost", source);
            StringAssert.Contains("HeartGraphGenerator.TryGenerate", source);
            StringAssert.Contains("heartGraphSettings", source);
            StringAssert.Contains(
                "Bu alanlar yalniz yeni bir run graph'i uretilirken okunur.",
                source);
            StringAssert.Contains("Aktif veya Continue ile ", source);
            StringAssert.Contains(
                "restore edilen exact graph reroll edilmez; mevcut node/level/reveal/lock state'i degismez.",
                source);
            StringAssert.Contains("GraveEssenceDropChance", source);
            StringAssert.Contains("GraveEssencePerDrop", source);
            StringAssert.Contains("ZombieDeathSystem -> GraveEssenceDropEvent -> GameManager", source);
            StringAssert.DoesNotContain("Production drop source\", \"UNCONFIGURED", source);
            StringAssert.DoesNotContain("TryBuyTechNode", source);
        }
    }
}
