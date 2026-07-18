#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DeadWalls.Tests
{
    public sealed class HeartProductionCatalogTests
    {
        private const string CatalogPath =
            "Assets/ScriptableObject/MobileCastle/CastleHeart/HeartNodeCatalog.asset";
        private const string LaunchSpecPath =
            "Assets/Docs/DEAD_WALLS_V1_CASTLE_HEART_LAUNCH_CATALOG.md";
        private const string ExpectedLaunchFingerprint =
            "e360c3312da560ae5ce4a2b2bb10aeb71555ad7b19f7a028b193a1f9139e00eb";
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

        [Test]
        public void ProductionCatalog_LocksExactLaunchContentAndClassification()
        {
            Assert.That(_catalog.Nodes.Count(node => node.Type == HeartNodeType.Unlock), Is.EqualTo(4));
            Assert.That(_catalog.Nodes.Count(node => node.Type == HeartNodeType.Repeatable), Is.EqualTo(8));
            Assert.That(_catalog.Nodes.Count(node => node.Type == HeartNodeType.Evolution), Is.EqualTo(15));
            Assert.That(_catalog.Nodes.Count(node => node.Type == HeartNodeType.Keystone), Is.EqualTo(8));

            Assert.That(_catalog.Nodes.Count(node => node.Branch == HeartNodeBranch.Army), Is.EqualTo(9));
            Assert.That(_catalog.Nodes.Count(node => node.Branch == HeartNodeBranch.Defense), Is.EqualTo(8));
            Assert.That(_catalog.Nodes.Count(node => node.Branch == HeartNodeBranch.Production), Is.EqualTo(10));
            Assert.That(_catalog.Nodes.Count(node => node.Branch == HeartNodeBranch.HeartMagic), Is.EqualTo(8));

            Assert.That(CreateLaunchFingerprint(_catalog), Is.EqualTo(ExpectedLaunchFingerprint));
        }

        [Test]
        public void ProductionCatalog_RecordsCleanLegacyMigrationProvenanceOnce()
        {
            string[] expectedMigratedLegacyIds =
            {
                "rapid_archer", "frost_archer", "bow_mastery", "volley_mastery",
                "rapid_volley", "frost_arrows", "bow_training", "wall_reinforcement",
                "repair_efficiency", "repair_crew", "wood_camp", "food_stores",
                "worker_camp", "population_growth", "arcane_tower", "fire_power",
                "fire_radius", "fire_cooldown"
            };

            string[] actualMigratedLegacyIds = _catalog.Nodes
                .SelectMany(node => node.LegacySourceNodeIds ?? Array.Empty<string>())
                .ToArray();
            Assert.That(actualMigratedLegacyIds, Is.Unique);
            Assert.That(actualMigratedLegacyIds, Is.EquivalentTo(expectedMigratedLegacyIds));
            Assert.That(actualMigratedLegacyIds,
                Has.None.EqualTo(HeartGraphConstants.RootNodeId));
            Assert.That(actualMigratedLegacyIds, Has.None.EqualTo("basic_archer"));
            Assert.That(actualMigratedLegacyIds, Has.None.EqualTo("moat_flame"));
            Assert.That(actualMigratedLegacyIds, Has.None.EqualTo("moat_dig"));
        }

        [Test]
        public void ProductionCatalog_PlayerCopyAndLaunchSpecStayInSync()
        {
            TextAsset launchSpec = AssetDatabase.LoadAssetAtPath<TextAsset>(LaunchSpecPath);
            Assert.That(launchSpec, Is.Not.Null);

            foreach (HeartNodeDefinitionSO node in _catalog.Nodes)
            {
                Assert.That(launchSpec.text, Does.Contain($"`{node.Id}`"), node.Id);
                if (node.Type != HeartNodeType.Keystone)
                    continue;

                HeartNodeDefinitionSO partner = _catalog.GetNode(node.ConflictNodeIds[0]);
                Assert.That(partner, Is.Not.Null);
                Assert.That(node.Description.IndexOf(
                        partner.Title,
                        StringComparison.OrdinalIgnoreCase),
                    Is.EqualTo(-1),
                    $"{node.Id} partner lock copy'sini tekrar ediyor.");
            }
        }

        private static string CreateLaunchFingerprint(HeartNodeCatalogSO catalog)
        {
            var payload = new StringBuilder();
            foreach (HeartNodeDefinitionSO node in catalog.Nodes.OrderBy(node => node.Id, StringComparer.Ordinal))
            {
                payload.Append(node.Id).Append('\u001f')
                    .Append(node.Title).Append('\u001f')
                    .Append(node.Description).Append('\u001f')
                    .Append((int)node.Type).Append('\u001f')
                    .Append((int)node.Branch).Append('\u001f')
                    .Append((int)node.Rarity).Append('\u001f')
                    .Append(node.MinimumDepth).Append('\u001f')
                    .Append(node.MaximumDepth).Append('\u001f')
                    .Append(node.BaseGraveEssenceCost).Append('\u001f')
                    .Append(node.CostGrowthPerLevel.ToString("R", CultureInfo.InvariantCulture))
                    .Append('\u001f')
                    .Append(string.Join(",", node.Tags ?? Array.Empty<string>())).Append('\u001f')
                    .Append(string.Join(",", node.LegacySourceNodeIds ?? Array.Empty<string>())).Append('\u001f')
                    .Append(string.Join(",", node.ConflictNodeIds ?? Array.Empty<string>())).Append('\u001f');

                HeartNodeEffect[] effects = node.Effects ?? Array.Empty<HeartNodeEffect>();
                for (int i = 0; i < effects.Length; i++)
                {
                    HeartNodeEffect effect = effects[i];
                    payload.Append((int)effect.Type).Append(':')
                        .Append(effect.Value.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                        .Append((int)effect.ArcherType).Append(':')
                        .Append((int)effect.Resource).Append(':')
                        .Append(effect.SoftCap.ToString("R", CultureInfo.InvariantCulture)).Append(';');
                }
                payload.Append('\u001e');
            }

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString()));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
#endif
