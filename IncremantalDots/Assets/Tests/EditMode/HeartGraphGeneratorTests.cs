using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartGraphGeneratorTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            _createdObjects.Clear();
        }

        [Test]
        public void SameSeedAndCatalog_ProduceByteEquivalentGraphJson()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 912345u);

            GeneratedRunGraph first = HeartGraphGenerator.GenerateOrThrow(request);
            GeneratedRunGraph second = HeartGraphGenerator.GenerateOrThrow(request);

            Assert.That(first.CatalogVersion, Is.EqualTo(catalog.CatalogVersion));
            Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(second)));
        }

        [Test]
        public void PersistedGraph_ExactClonePreservesHiddenLevelsLocksAndDoesNotAliasSource()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(CreateRequest(catalog, 731u));
            HeartGraphRevealService.InitializeRunVisibility(graph);
            GeneratedHeartNodeState sink = graph.Nodes.Single(node => node.NodeId == "army_sink");
            sink.Visibility = HeartNodeVisibility.Revealed;
            sink.Level = 17;
            GeneratedHeartNodeState source = graph.Nodes.Single(node => node.NodeId == "keystone_army");
            GeneratedHeartNodeState partner = graph.Nodes.Single(node => node.NodeId == "keystone_defense");
            source.Visibility = HeartNodeVisibility.Revealed;
            source.Level = 1;
            partner.LockState = HeartNodeLockState.KeystoneConflict;
            partner.LockedByNodeId = source.NodeId;

            GeneratedRunGraph clone = HeartGraphPersistenceUtility.CloneExact(graph);
            bool valid = HeartGraphPersistenceUtility.TryValidateForRestore(
                clone,
                catalog,
                out List<string> errors);

            Assert.That(valid, Is.True, string.Join(" | ", errors));
            Assert.That(JsonUtility.ToJson(clone), Is.EqualTo(JsonUtility.ToJson(graph)));
            Assert.That(clone, Is.Not.SameAs(graph));
            Assert.That(clone.Nodes, Is.Not.SameAs(graph.Nodes));
            clone.Nodes.Single(node => node.NodeId == "army_sink").Level = 99;
            Assert.That(sink.Level, Is.EqualTo(17));
        }

        [Test]
        public void PersistedGraph_CatalogVersionMismatchFailsWithoutRegeneration()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            catalog.CatalogVersion = 7;
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(CreateRequest(catalog, 991u));
            string exactJson = JsonUtility.ToJson(graph);
            catalog.CatalogVersion = 8;

            bool valid = HeartGraphPersistenceUtility.TryValidateForRestore(
                graph,
                catalog,
                out List<string> errors);

            Assert.That(valid, Is.False);
            Assert.That(errors, Has.Some.Contains("uyusmuyor"));
            Assert.That(JsonUtility.ToJson(graph), Is.EqualTo(exactJson));
        }

        [Test]
        public void PersistedProductionV1Graph_MigratesCatalogIdentityWithoutInjectingV2Evolutions()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            catalog.CatalogVersion = 1;
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(
                CreateRequest(catalog, 1781u));
            GeneratedRunGraph expected = HeartGraphPersistenceUtility.CloneExact(graph);
            expected.CatalogVersion = 2;

            var definitions = catalog.Nodes.ToList();
            definitions.Add(CreateDefinition(
                "scorched_earth",
                HeartNodeBranch.HeartMagic,
                HeartNodeType.Evolution,
                null,
                new HeartNodeEffect { Type = HeartNodeEffectType.EnableBurningGround }));
            definitions.Add(CreateDefinition(
                "echoing_detonation",
                HeartNodeBranch.HeartMagic,
                HeartNodeType.Evolution,
                null,
                new HeartNodeEffect { Type = HeartNodeEffectType.EnableSecondBlast }));
            catalog.Nodes = definitions.ToArray();
            catalog.CatalogVersion = 2;

            bool valid = HeartGraphPersistenceUtility.TryValidateForRestore(
                graph,
                catalog,
                out List<string> errors);

            Assert.That(valid, Is.True, string.Join(" | ", errors));
            Assert.That(graph.CatalogVersion, Is.EqualTo(2));
            Assert.That(graph.Nodes, Has.None.Matches<GeneratedHeartNodeState>(node =>
                node.NodeId == "scorched_earth" || node.NodeId == "echoing_detonation"));
            Assert.That(JsonUtility.ToJson(graph), Is.EqualTo(JsonUtility.ToJson(expected)));
        }

        [Test]
        public void PersistedGraph_ReplaysPurchasedNumericAndBehaviorEffectsBeforeActivation()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(CreateRequest(catalog, 1199u));
            GeneratedHeartNodeState rapid = graph.Nodes.Single(node => node.NodeId == "rapid_unlock");
            GeneratedHeartNodeState wall = graph.Nodes.Single(node => node.NodeId == "wall_access");
            rapid.Visibility = HeartNodeVisibility.Revealed;
            rapid.Level = 1;
            wall.Visibility = HeartNodeVisibility.Revealed;
            wall.Level = 1;
            var provider = new AnyBaselineProvider();
            var sink = new RecordingEffectSink();

            bool restored = HeartGraphPersistenceUtility.TryCreateRestoredPipeline(
                graph,
                catalog,
                provider,
                sink,
                out HeartEffectPipeline pipeline,
                out string error);

            Assert.That(restored, Is.True, error);
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(sink.EnabledBehaviorTypes, Does.Contain(HeartNodeEffectType.UnlockArcherType));
            var wallTarget = new HeartEffectTargetKey(
                HeartNodeEffectType.ModifyWallMaxHpPercent,
                default,
                default);
            Assert.That(sink.NumericValues.ContainsKey(wallTarget), Is.True);
            Assert.That(sink.NumericValues[wallTarget], Is.GreaterThan(100d));
        }

        [Test]
        public void GeneratedGraph_HasFixedRootFourConnectedSpinesGuaranteesAndSinks()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 17u);

            bool succeeded = HeartGraphGenerator.TryGenerate(
                request,
                out GeneratedRunGraph graph,
                out HeartGraphGenerationReport report);
            var validationErrors = new List<string>();
            HeartGraphValidator.Validate(graph, catalog, request, validationErrors);

            Assert.That(succeeded, Is.True, string.Join(" | ", report.Errors));
            Assert.That(validationErrors, Is.Empty);
            Assert.That(graph.RootNodeId, Is.EqualTo(HeartGraphConstants.RootNodeId));
            Assert.That(graph.Nodes.Single(node => node.NodeId == graph.RootNodeId).Depth, Is.Zero);

            foreach (HeartNodeBranch branch in Enum.GetValues(typeof(HeartNodeBranch)))
            {
                GeneratedHeartNodeState[] branchNodes = graph.Nodes
                    .Where(node => node.NodeId != graph.RootNodeId && node.Branch == branch)
                    .OrderBy(node => node.Depth)
                    .ToArray();
                Assert.That(branchNodes.Length, Is.InRange(request.MinimumBranchDepth, request.MaximumBranchDepth));
                Assert.That(branchNodes.Select(node => node.Depth), Is.EqualTo(Enumerable.Range(1, branchNodes.Length)));
                Assert.That(
                    branchNodes.Any(node =>
                    {
                        HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
                        return definition.Type == HeartNodeType.Repeatable
                               && HeartNodeTagUtility.HasTag(
                                   definition,
                                   HeartGraphConstants.RepeatableSinkTag);
                    }),
                    Is.True,
                    $"{branch} repeatable sink tasimali.");
            }

            AssertGraphContainsTag(graph, catalog, HeartGraphConstants.RapidGuaranteeTag);
            AssertGraphContainsTag(graph, catalog, HeartGraphConstants.FrostGuaranteeTag);
            AssertGraphContainsTag(graph, catalog, HeartGraphConstants.FireballGuaranteeTag);
            AssertGraphContainsTag(graph, catalog, HeartGraphConstants.WallGuaranteeTag);
        }

        [Test]
        public void GeneratedGraph_PlacesCompleteKeystonePairWithoutInitialLocks()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 222u);
            request.KeystonePairCount = 1;

            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(request);
            GeneratedHeartNodeState first = graph.Nodes.Single(node => node.NodeId == "keystone_army");
            GeneratedHeartNodeState second = graph.Nodes.Single(node => node.NodeId == "keystone_defense");

            Assert.That(first.LockState, Is.EqualTo(HeartNodeLockState.Available));
            Assert.That(second.LockState, Is.EqualTo(HeartNodeLockState.Available));
            Assert.That(first.LockedByNodeId, Is.Empty);
            Assert.That(second.LockedByNodeId, Is.Empty);
            Assert.That(
                graph.Nodes.Where(node => node.NodeId != graph.RootNodeId),
                Has.All.Matches<GeneratedHeartNodeState>(node =>
                    node.LockState == HeartNodeLockState.Available
                    && string.IsNullOrEmpty(node.LockedByNodeId)));
        }

        [Test]
        public void CrossLinks_AreBoundedAndAlwaysAdvanceOneDepthAcrossBranches()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 3456u);
            request.MaximumCrossLinks = 3;

            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(request);
            Dictionary<string, GeneratedHeartNodeState> nodesById = graph.Nodes.ToDictionary(node => node.NodeId);
            GeneratedHeartEdge[] crossLinks = graph.Edges
                .Where(edge => edge.FromNodeId != graph.RootNodeId
                               && nodesById[edge.FromNodeId].Branch != nodesById[edge.ToNodeId].Branch)
                .ToArray();

            Assert.That(crossLinks.Length, Is.LessThanOrEqualTo(request.MaximumCrossLinks));
            Assert.That(crossLinks, Is.Not.Empty);
            Assert.That(
                crossLinks,
                Has.All.Matches<GeneratedHeartEdge>(edge =>
                    nodesById[edge.ToNodeId].Depth == nodesById[edge.FromNodeId].Depth + 1));
            Assert.That(crossLinks.Select(edge => edge.FromNodeId).Distinct().Count(), Is.EqualTo(crossLinks.Length));
            Assert.That(crossLinks.Select(edge => edge.ToNodeId).Distinct().Count(), Is.EqualTo(crossLinks.Length));
        }

        [Test]
        public void CatalogWithDuplicateNodeId_IsRejectedBeforeAnyAttempt()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartNodeDefinitionSO duplicate = CreateDefinition(
                "army_filler_1",
                HeartNodeBranch.Army,
                HeartNodeType.Unlock);
            catalog.Nodes = catalog.Nodes.Concat(new[] { duplicate }).ToArray();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 5u);

            bool succeeded = HeartGraphGenerator.TryGenerate(
                request,
                out GeneratedRunGraph graph,
                out HeartGraphGenerationReport report);

            Assert.That(succeeded, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(report.AttemptsUsed, Is.Zero);
            Assert.That(report.Errors, Has.Some.Contains("Tekrarlanan node Id"));
        }

        [Test]
        public void GuaranteeOutsideAllowedDepth_FailsExplicitlyAndThrowingApiDoesNotReturnBrokenGraph()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartNodeDefinitionSO rapid = catalog.Nodes.Single(definition =>
                HeartNodeTagUtility.HasTag(definition, HeartGraphConstants.RapidGuaranteeTag));
            rapid.MinimumDepth = 7;
            rapid.MaximumDepth = 8;
            HeartGraphGenerationRequest request = CreateRequest(catalog, 678u);

            bool succeeded = HeartGraphGenerator.TryGenerate(
                request,
                out GeneratedRunGraph graph,
                out HeartGraphGenerationReport report);

            Assert.That(succeeded, Is.False);
            Assert.That(graph, Is.Null);
            Assert.That(report.Errors, Has.Some.Contains("max depth"));
            Assert.Throws<InvalidOperationException>(() => HeartGraphGenerator.GenerateOrThrow(request));
        }

        [Test]
        public void Validator_ReportsDisconnectedBranchAndMissingCoreEdge()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 876u);
            request.MaximumCrossLinks = 0;
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(request);
            GeneratedHeartNodeState armyEntry = graph.Nodes.Single(node =>
                node.NodeId != graph.RootNodeId
                && node.Branch == HeartNodeBranch.Army
                && node.Depth == 1);
            graph.Edges.RemoveAll(edge =>
                edge.FromNodeId == graph.RootNodeId && edge.ToNodeId == armyEntry.NodeId);
            var errors = new List<string>();

            HeartGraphValidator.Validate(graph, catalog, request, errors);

            Assert.That(errors, Has.Some.Contains("core edge"));
            Assert.That(errors, Has.Some.Contains("Disconnected/unreachable"));
        }

        [Test]
        public void Validator_RejectsKeystoneCountThatDoesNotMatchGenerationRequest()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 877u);
            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(request);
            request.KeystonePairCount = 0;
            var errors = new List<string>();

            HeartGraphValidator.Validate(graph, catalog, request, errors);

            Assert.That(errors, Has.Some.Contains("Keystone node"));
        }

        [Test]
        public void InitialGraph_RevealsOnlyRootAndDoesNotPrePurchaseOrLockNormalNodes()
        {
            HeartNodeCatalogSO catalog = CreateValidCatalog();
            HeartGraphGenerationRequest request = CreateRequest(catalog, 444u);

            GeneratedRunGraph graph = HeartGraphGenerator.GenerateOrThrow(request);
            GeneratedHeartNodeState root = graph.Nodes.Single(node => node.NodeId == graph.RootNodeId);
            GeneratedHeartNodeState[] nonRoot = graph.Nodes.Where(node => node.NodeId != graph.RootNodeId).ToArray();

            Assert.That(root.Visibility, Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(root.Level, Is.EqualTo(1));
            Assert.That(nonRoot, Has.All.Matches<GeneratedHeartNodeState>(node =>
                node.Visibility == HeartNodeVisibility.Hidden
                && node.Level == 0
                && node.LockState == HeartNodeLockState.Available
                && string.IsNullOrEmpty(node.LockedByNodeId)));
        }

        private HeartNodeCatalogSO CreateValidCatalog()
        {
            HeartNodeCatalogSO catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
            _createdObjects.Add(catalog);
            var definitions = new List<HeartNodeDefinitionSO>
            {
                CreateDefinition(
                    "rapid_unlock",
                    HeartNodeBranch.Army,
                    HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.RapidGuaranteeTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.UnlockArcherType,
                        ArcherType = ArcherType.Rapid
                    }),
                CreateDefinition(
                    "frost_unlock",
                    HeartNodeBranch.Army,
                    HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.FrostGuaranteeTag },
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.UnlockArcherType,
                        ArcherType = ArcherType.Frost
                    }),
                CreateDefinition(
                    "army_sink",
                    HeartNodeBranch.Army,
                    HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("army_filler_1", HeartNodeBranch.Army, HeartNodeType.Unlock),
                CreateDefinition("army_filler_2", HeartNodeBranch.Army, HeartNodeType.Evolution),
                CreateDefinition(
                    "wall_access",
                    HeartNodeBranch.Defense,
                    HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.WallGuaranteeTag },
                    new HeartNodeEffect { Type = HeartNodeEffectType.ModifyWallMaxHpPercent, Value = 10f }),
                CreateDefinition(
                    "defense_sink",
                    HeartNodeBranch.Defense,
                    HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("defense_filler_1", HeartNodeBranch.Defense, HeartNodeType.Unlock),
                CreateDefinition("defense_filler_2", HeartNodeBranch.Defense, HeartNodeType.Evolution),
                CreateDefinition("defense_filler_3", HeartNodeBranch.Defense, HeartNodeType.Unlock),
                CreateDefinition(
                    "production_sink",
                    HeartNodeBranch.Production,
                    HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("production_filler_1", HeartNodeBranch.Production, HeartNodeType.Unlock),
                CreateDefinition("production_filler_2", HeartNodeBranch.Production, HeartNodeType.Evolution),
                CreateDefinition("production_filler_3", HeartNodeBranch.Production, HeartNodeType.Unlock),
                CreateDefinition("production_filler_4", HeartNodeBranch.Production, HeartNodeType.Unlock),
                CreateDefinition(
                    "fireball_unlock",
                    HeartNodeBranch.HeartMagic,
                    HeartNodeType.Unlock,
                    new[] { HeartGraphConstants.FireballGuaranteeTag },
                    new HeartNodeEffect { Type = HeartNodeEffectType.UnlockSpellcasting }),
                CreateDefinition(
                    "heart_sink",
                    HeartNodeBranch.HeartMagic,
                    HeartNodeType.Repeatable,
                    new[] { HeartGraphConstants.RepeatableSinkTag }),
                CreateDefinition("heart_filler_1", HeartNodeBranch.HeartMagic, HeartNodeType.Unlock),
                CreateDefinition("heart_filler_2", HeartNodeBranch.HeartMagic, HeartNodeType.Evolution),
                CreateDefinition("heart_filler_3", HeartNodeBranch.HeartMagic, HeartNodeType.Unlock)
            };

            HeartNodeDefinitionSO armyKeystone = CreateDefinition(
                "keystone_army",
                HeartNodeBranch.Army,
                HeartNodeType.Keystone);
            HeartNodeDefinitionSO defenseKeystone = CreateDefinition(
                "keystone_defense",
                HeartNodeBranch.Defense,
                HeartNodeType.Keystone);
            armyKeystone.ConflictNodeIds = new[] { defenseKeystone.Id };
            defenseKeystone.ConflictNodeIds = new[] { armyKeystone.Id };
            definitions.Add(armyKeystone);
            definitions.Add(defenseKeystone);

            catalog.Nodes = definitions.ToArray();
            return catalog;
        }

        private HeartNodeDefinitionSO CreateDefinition(
            string id,
            HeartNodeBranch branch,
            HeartNodeType type,
            string[] tags = null,
            params HeartNodeEffect[] effects)
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            _createdObjects.Add(definition);
            definition.Id = id;
            definition.Title = id;
            definition.Branch = branch;
            definition.Type = type;
            definition.Rarity = id.EndsWith("2", StringComparison.Ordinal)
                ? HeartNodeRarity.Rare
                : HeartNodeRarity.Standard;
            definition.MinimumDepth = 1;
            definition.MaximumDepth = 5;
            definition.BaseGraveEssenceCost = 10;
            definition.Tags = tags ?? Array.Empty<string>();
            definition.Effects = effects ?? Array.Empty<HeartNodeEffect>();
            definition.ConflictNodeIds = Array.Empty<string>();
            return definition;
        }

        private static HeartGraphGenerationRequest CreateRequest(HeartNodeCatalogSO catalog, uint seed)
        {
            return new HeartGraphGenerationRequest
            {
                Catalog = catalog,
                Seed = seed,
                MinimumBranchDepth = 4,
                MaximumBranchDepth = 5,
                MaximumCrossLinks = 2,
                KeystonePairCount = 1,
                MaximumAttempts = 8,
                StandardRarityWeight = 4,
                RareRarityWeight = 1
            };
        }

        private static void AssertGraphContainsTag(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            string tag)
        {
            Assert.That(
                graph.Nodes.Any(node =>
                {
                    HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
                    return HeartNodeTagUtility.HasTag(definition, tag);
                }),
                Is.True,
                $"Graph '{tag}' guarantee node'unu tasimali.");
        }

        private sealed class AnyBaselineProvider : IHeartEffectBaselineProvider
        {
            public bool TryGetBaseline(
                HeartEffectTargetKey target,
                out HeartEffectBaseline baseline)
            {
                baseline = new HeartEffectBaseline(target.ToString(), 100d, string.Empty, 2);
                return true;
            }
        }

        private sealed class RecordingEffectSink : IHeartRuntimeEffectSink
        {
            public readonly Dictionary<HeartEffectTargetKey, double> NumericValues =
                new Dictionary<HeartEffectTargetKey, double>();
            public readonly List<HeartNodeEffectType> EnabledBehaviorTypes =
                new List<HeartNodeEffectType>();

            public void ApplyNumericEffect(HeartEffectTargetKey target, double actualValue)
            {
                NumericValues[target] = actualValue;
            }

            public void EnableBehaviorEffect(HeartNodeEffect effect)
            {
                EnabledBehaviorTypes.Add(effect.Type);
            }
        }
    }
}
