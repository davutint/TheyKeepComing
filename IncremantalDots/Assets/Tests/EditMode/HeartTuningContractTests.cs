using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartTuningContractTests
    {
        [Test]
        public void ProductionEssenceGain_RemainsExplicitOwnerGateWithoutInventedDropCaller()
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
                string source = File.ReadAllText(scripts[i]);
                grantReferences += Regex.Matches(source, @"\bGrantGraveEssence\s*\(").Count;
            }

            Assert.That(grantReferences, Is.EqualTo(1),
                "Owner onayi olmadan production Essence drop caller'i eklenmemeli; "
                + "tek referans GameManager transaction declaration'i kalmali.");
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
            StringAssert.Contains("Production drop source\", \"UNCONFIGURED", source);
            StringAssert.DoesNotContain("TryBuyTechNode", source);
        }
    }
}
