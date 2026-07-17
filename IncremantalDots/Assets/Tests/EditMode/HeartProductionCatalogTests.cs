#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace DeadWalls.Tests
{
    public sealed class HeartProductionCatalogTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/CastleHeart/HeartNodeCatalog.asset";
        private HeartNodeCatalogSO _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<HeartNodeCatalogSO>(
                CatalogPath);
        }

        [Test]
        public void ProductionCatalog_IsCanonicalValidAndExcludesLegacyScope()
        {
            Assert.That(_catalog, Is.Not.Null);
            Assert.That(_catalog.CatalogVersion, Is.EqualTo(1));
            Assert.That(_catalog.RootNodeId, Is.EqualTo(HeartGraphConstants.RootNodeId));
            Assert.That(_catalog.Nodes, Has.Length.EqualTo(35));

            var errors = new List<string>();
            _catalog.CollectValidationErrors(errors);
            Assert.That(errors, Is.Empty, string.Join(" | ", errors));

            string[] forbiddenIds =
            {
                HeartGraphConstants.RootNodeId,
                "basic_archer",
                "burning_moat",
                "deeper_moat"
            };
            for (int i = 0; i < forbiddenIds.Length; i++)
                Assert.That(_catalog.GetNode(forbiddenIds[i]), Is.Null, forbiddenIds[i]);

            foreach (HeartNodeBranch branch in Enum.GetValues(typeof(HeartNodeBranch)))
            {
                Assert.That(_catalog.Nodes.Any(node => node.Branch == branch
                    && node.Type == HeartNodeType.Repeatable
                    && HeartNodeTagUtility.HasTag(node, HeartGraphConstants.RepeatableSinkTag)),
                    Is.True, branch + " repeatable sink eksik.");
            }
        }

        [Test]
        public void ProductionCatalog_HasFourKeystonePairsAndRealFireballEvolutions()
        {
            HeartNodeDefinitionSO[] keystones = _catalog.Nodes
                .Where(node => node.Type == HeartNodeType.Keystone)
                .ToArray();
            Assert.That(keystones, Has.Length.EqualTo(8));

            int symmetricPairs = keystones.Count(node =>
                string.CompareOrdinal(node.Id, node.ConflictNodeIds[0]) < 0
                && _catalog.GetNode(node.ConflictNodeIds[0]) is HeartNodeDefinitionSO partner
                && partner.ConflictNodeIds.Length == 1
                && partner.ConflictNodeIds[0] == node.Id);
            Assert.That(symmetricPairs, Is.EqualTo(4));

            int realFireballEvolutions = _catalog.Nodes.Count(node =>
                node.Branch == HeartNodeBranch.HeartMagic
                && node.Type == HeartNodeType.Evolution
                && node.Effects.Any(effect =>
                    effect.Type == HeartNodeEffectType.ModifySpellDamagePercent
                    || effect.Type == HeartNodeEffectType.AddSpellRadius
                    || effect.Type == HeartNodeEffectType.ReduceSpellCooldownPercent));
            Assert.That(realFireballEvolutions, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void ProductionCatalog_GeneratesValidatedGraphsAcrossSeedSweep()
        {
            var settings = new HeartGraphRuntimeSettings();
            for (uint seed = 1; seed <= 64; seed++)
            {
                bool generated = HeartGraphGenerator.TryGenerate(
                    settings.CreateRequest(_catalog, seed),
                    out GeneratedRunGraph graph,
                    out HeartGraphGenerationReport report);
                Assert.That(generated, Is.True,
                    $"Seed {seed}: {string.Join(" | ", report.Errors)}");
                Assert.That(graph, Is.Not.Null);
                Assert.That(graph.CatalogVersion, Is.EqualTo(_catalog.CatalogVersion));
                Assert.That(graph.Nodes.Count, Is.InRange(17, 21));
            }
        }
    }
}
#endif
