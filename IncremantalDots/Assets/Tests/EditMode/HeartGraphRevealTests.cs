using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartGraphRevealTests
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
        public void InitializeRunVisibility_RevealsRootNeighborsAndLeavesRemoteNodesHidden()
        {
            GeneratedRunGraph graph = CreateGraph();

            HeartGraphRevealResult result = HeartGraphRevealService.InitializeRunVisibility(graph);

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(result.NewlyRevealedNodeIds, Has.Count.EqualTo(4));
            Assert.That(
                graph.Nodes.Where(node => node.Depth == 1),
                Has.All.Matches<GeneratedHeartNodeState>(node =>
                    node.Visibility == HeartNodeVisibility.Revealed));
            Assert.That(
                graph.Nodes.Where(node => node.Depth >= 2),
                Has.All.Matches<GeneratedHeartNodeState>(node =>
                    node.Visibility == HeartNodeVisibility.Hidden));

            HeartGraphRevealResult secondInitialization =
                HeartGraphRevealService.InitializeRunVisibility(graph);
            Assert.That(secondInitialization.Succeeded, Is.True);
            Assert.That(secondInitialization.NewlyRevealedNodeIds, Is.Empty);
        }

        [Test]
        public void FirstPurchase_RevealsOnlyOutgoingNeighborsIncludingControlledCrossLink()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartGraphRevealService.InitializeRunVisibility(graph);
            GeneratedHeartNodeState purchased = FindNode(graph, "army_unlock");
            purchased.Level = 10;

            HeartGraphRevealResult result =
                HeartGraphRevealService.RevealAfterFirstPurchase(graph, purchased.NodeId, 0);

            Assert.That(result.Succeeded, Is.True, string.Join(" | ", result.Errors));
            Assert.That(
                result.NewlyRevealedNodeIds,
                Is.EquivalentTo(new[] { "army_numeric", "production_deep" }));
            Assert.That(FindNode(graph, "army_numeric").Visibility, Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(FindNode(graph, "production_deep").Visibility, Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(FindNode(graph, "keystone_army").Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
            Assert.That(FindNode(graph, "keystone_defense").Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
        }

        [Test]
        public void RepeatableLaterLevel_DoesNotRevealAgain()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartGraphRevealService.InitializeRunVisibility(graph);
            GeneratedHeartNodeState purchased = FindNode(graph, "army_unlock");
            purchased.Level = 2;

            HeartGraphRevealResult result =
                HeartGraphRevealService.RevealAfterFirstPurchase(graph, purchased.NodeId, 1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.NewlyRevealedNodeIds, Is.Empty);
            Assert.That(FindNode(graph, "army_numeric").Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
            Assert.That(FindNode(graph, "production_deep").Visibility, Is.EqualTo(HeartNodeVisibility.Hidden));
        }

        [Test]
        public void HiddenPresentation_ExposesOnlySafeSlotBranchDepthAndVeinEdges()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartNodeCatalogSO catalog = CreateCatalog();

            bool built = HeartGraphPresentationBuilder.TryBuild(
                graph,
                catalog,
                null,
                out HeartGraphPresentation presentation,
                out List<string> errors);

            Assert.That(built, Is.True, string.Join(" | ", errors));
            HeartGraphNodePresentation hidden = presentation.Nodes.Single(node =>
                node.SlotId == "army:2");
            Assert.That(hidden.IsExactContentVisible, Is.False);
            Assert.That(hidden.ExactNodeId, Is.Null);
            Assert.That(hidden.Title, Is.Empty);
            Assert.That(hidden.Description, Is.Empty);
            Assert.That(hidden.Icon, Is.Null);
            Assert.That(hidden.Type, Is.Null);
            Assert.That(hidden.Rarity, Is.Null);
            Assert.That(hidden.Effects, Is.Empty);

            string[] internalHiddenIds = graph.Nodes
                .Where(node => node.Visibility == HeartNodeVisibility.Hidden)
                .Select(node => node.NodeId)
                .ToArray();
            Assert.That(
                presentation.Nodes.Where(node => !node.IsExactContentVisible).Select(node => node.ExactNodeId),
                Has.All.Null);
            foreach (HeartGraphEdgePresentation edge in presentation.Edges)
            {
                Assert.That(internalHiddenIds, Does.Not.Contain(edge.FromSlotId));
                Assert.That(internalHiddenIds, Does.Not.Contain(edge.ToSlotId));
            }
        }

        [Test]
        public void RevealedNumericEffect_RequiresResolverAndPublishesOnlyResolvedValues()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartNodeCatalogSO catalog = CreateCatalog();
            FindNode(graph, "army_numeric").Visibility = HeartNodeVisibility.Revealed;

            bool builtWithoutResolver = HeartGraphPresentationBuilder.TryBuild(
                graph,
                catalog,
                null,
                out HeartGraphPresentation incomplete,
                out List<string> incompleteErrors);
            bool builtWithResolver = HeartGraphPresentationBuilder.TryBuild(
                graph,
                catalog,
                new FixedEffectResolver(),
                out HeartGraphPresentation complete,
                out List<string> completeErrors);

            Assert.That(builtWithoutResolver, Is.False);
            Assert.That(incompleteErrors, Has.Some.Contains("gercek numeric sonuc"));
            Assert.That(
                incomplete.Nodes.Single(node => node.ExactNodeId == "army_numeric")
                    .EffectInformationComplete,
                Is.False);

            Assert.That(builtWithResolver, Is.True, string.Join(" | ", completeErrors));
            HeartEffectPresentation effect = complete.Nodes
                .Single(node => node.ExactNodeId == "army_numeric")
                .Effects.Single();
            Assert.That(effect.IsResolved, Is.True);
            Assert.That(effect.Label, Is.EqualTo("Archer Damage"));
            Assert.That(effect.CurrentValueText, Is.EqualTo("100"));
            Assert.That(effect.AfterPurchaseValueText, Is.EqualTo("112"));
            Assert.That(effect.DeltaText, Is.EqualTo("+12"));
        }

        [Test]
        public void VisibleKeystone_MarksOpposingSafeSlotWithoutLeakingHiddenPartnerId()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartNodeCatalogSO catalog = CreateCatalog();
            FindNode(graph, "keystone_army").Visibility = HeartNodeVisibility.Revealed;

            bool built = HeartGraphPresentationBuilder.TryBuild(
                graph,
                catalog,
                null,
                out HeartGraphPresentation presentation,
                out List<string> errors);

            Assert.That(built, Is.True, string.Join(" | ", errors));
            HeartGraphNodePresentation visibleKeystone = presentation.Nodes.Single(node =>
                node.ExactNodeId == "keystone_army");
            HeartGraphNodePresentation hiddenPartner = presentation.Nodes.Single(node =>
                node.SlotId == "defense:2");

            Assert.That(visibleKeystone.KeystoneConflict, Is.Not.Null);
            Assert.That(
                visibleKeystone.KeystoneConflict.ConflictingChoiceSlotId,
                Is.EqualTo(hiddenPartner.SlotId));
            Assert.That(
                visibleKeystone.KeystoneConflict.ConflictingChoiceTitle,
                Is.EqualTo("Stone Doctrine"));
            Assert.That(visibleKeystone.KeystoneConflict.ConflictingChoiceIsRevealed, Is.False);
            Assert.That(visibleKeystone.KeystoneConflict.WillLockOnPurchase, Is.True);
            Assert.That(visibleKeystone.KeystoneConflict.IsAlreadyLockedByThisChoice, Is.False);
            Assert.That(visibleKeystone.KeystoneConflict.SourceIsLockedByConflictingChoice, Is.False);
            Assert.That(hiddenPartner.IsKeystoneConflictTarget, Is.True);
            Assert.That(hiddenPartner.ExactNodeId, Is.Null);
            Assert.That(hiddenPartner.Title, Is.Empty);
        }

        [Test]
        public void PurchasedKeystone_MarksPartnerAsAlreadyLockedWithoutExposingLockedById()
        {
            GeneratedRunGraph graph = CreateGraph();
            HeartNodeCatalogSO catalog = CreateCatalog();
            GeneratedHeartNodeState source = FindNode(graph, "keystone_army");
            GeneratedHeartNodeState partner = FindNode(graph, "keystone_defense");
            source.Visibility = HeartNodeVisibility.Revealed;
            source.Level = 1;
            partner.LockState = HeartNodeLockState.KeystoneConflict;
            partner.LockedByNodeId = source.NodeId;

            bool built = HeartGraphPresentationBuilder.TryBuild(
                graph,
                catalog,
                null,
                out HeartGraphPresentation presentation,
                out List<string> errors);

            Assert.That(built, Is.True, string.Join(" | ", errors));
            HeartGraphNodePresentation visibleKeystone = presentation.Nodes.Single(node =>
                node.ExactNodeId == "keystone_army");
            HeartGraphNodePresentation hiddenPartner = presentation.Nodes.Single(node =>
                node.SlotId == "defense:2");

            Assert.That(visibleKeystone.KeystoneConflict.WillLockOnPurchase, Is.False);
            Assert.That(visibleKeystone.KeystoneConflict.IsAlreadyLockedByThisChoice, Is.True);
            Assert.That(hiddenPartner.ExactNodeId, Is.Null);
            Assert.That(hiddenPartner.LockState, Is.EqualTo(HeartNodeLockState.Available));
        }

        [Test]
        public void HiddenOrUnpurchasedNode_CannotActAsRevealSource()
        {
            GeneratedRunGraph graph = CreateGraph();
            GeneratedHeartNodeState hidden = FindNode(graph, "army_numeric");

            HeartGraphRevealResult hiddenResult =
                HeartGraphRevealService.RevealAfterFirstPurchase(graph, hidden.NodeId, 0);
            hidden.Visibility = HeartNodeVisibility.Revealed;
            HeartGraphRevealResult unpurchasedResult =
                HeartGraphRevealService.RevealAfterFirstPurchase(graph, hidden.NodeId, 0);

            Assert.That(hiddenResult.Succeeded, Is.False);
            Assert.That(hiddenResult.Errors, Has.Some.Contains("Hidden"));
            Assert.That(unpurchasedResult.Succeeded, Is.False);
            Assert.That(unpurchasedResult.Errors, Has.Some.Contains("Satin alinmamis"));
        }

        private GeneratedRunGraph CreateGraph()
        {
            var graph = new GeneratedRunGraph
            {
                Seed = 445566u,
                RootNodeId = HeartGraphConstants.RootNodeId
            };
            AddNode(graph, HeartGraphConstants.RootNodeId, HeartNodeBranch.HeartMagic, 0,
                HeartNodeVisibility.Revealed, 1);
            AddNode(graph, "army_unlock", HeartNodeBranch.Army, 1);
            AddNode(graph, "army_numeric", HeartNodeBranch.Army, 2);
            AddNode(graph, "keystone_army", HeartNodeBranch.Army, 3);
            AddNode(graph, "defense_entry", HeartNodeBranch.Defense, 1);
            AddNode(graph, "keystone_defense", HeartNodeBranch.Defense, 2);
            AddNode(graph, "production_entry", HeartNodeBranch.Production, 1);
            AddNode(graph, "production_deep", HeartNodeBranch.Production, 2);
            AddNode(graph, "heart_entry", HeartNodeBranch.HeartMagic, 1);

            AddEdge(graph, HeartGraphConstants.RootNodeId, "army_unlock");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "defense_entry");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "production_entry");
            AddEdge(graph, HeartGraphConstants.RootNodeId, "heart_entry");
            AddEdge(graph, "army_unlock", "army_numeric");
            AddEdge(graph, "army_unlock", "production_deep");
            AddEdge(graph, "army_numeric", "keystone_army");
            AddEdge(graph, "defense_entry", "keystone_defense");
            AddEdge(graph, "production_entry", "production_deep");
            return graph;
        }

        private HeartNodeCatalogSO CreateCatalog()
        {
            HeartNodeCatalogSO catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
            _createdObjects.Add(catalog);
            HeartNodeDefinitionSO armyKeystone = CreateDefinition(
                "keystone_army",
                "Arrow Doctrine",
                HeartNodeBranch.Army,
                HeartNodeType.Keystone);
            HeartNodeDefinitionSO defenseKeystone = CreateDefinition(
                "keystone_defense",
                "Stone Doctrine",
                HeartNodeBranch.Defense,
                HeartNodeType.Keystone);
            armyKeystone.ConflictNodeIds = new[] { defenseKeystone.Id };
            defenseKeystone.ConflictNodeIds = new[] { armyKeystone.Id };

            catalog.Nodes = new[]
            {
                CreateDefinition(
                    "army_unlock",
                    "Rapid Muster",
                    HeartNodeBranch.Army,
                    HeartNodeType.Unlock,
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.UnlockArcherType,
                        ArcherType = ArcherType.Rapid
                    }),
                CreateDefinition(
                    "army_numeric",
                    "Bow Mastery",
                    HeartNodeBranch.Army,
                    HeartNodeType.Repeatable,
                    new HeartNodeEffect
                    {
                        Type = HeartNodeEffectType.ModifyArcherDamagePercent,
                        Value = 0.12f
                    }),
                armyKeystone,
                CreateDefinition("defense_entry", "Wall Path", HeartNodeBranch.Defense, HeartNodeType.Unlock),
                defenseKeystone,
                CreateDefinition("production_entry", "Workshop Path", HeartNodeBranch.Production, HeartNodeType.Unlock),
                CreateDefinition("production_deep", "Deep Workshop", HeartNodeBranch.Production, HeartNodeType.Evolution),
                CreateDefinition(
                    "heart_entry",
                    "Fireball",
                    HeartNodeBranch.HeartMagic,
                    HeartNodeType.Unlock,
                    new HeartNodeEffect { Type = HeartNodeEffectType.UnlockSpellcasting })
            };
            return catalog;
        }

        private HeartNodeDefinitionSO CreateDefinition(
            string id,
            string title,
            HeartNodeBranch branch,
            HeartNodeType type,
            params HeartNodeEffect[] effects)
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            _createdObjects.Add(definition);
            definition.Id = id;
            definition.Title = title;
            definition.Description = title + " description";
            definition.Branch = branch;
            definition.Type = type;
            definition.MinimumDepth = 1;
            definition.MaximumDepth = 5;
            definition.BaseGraveEssenceCost = 10;
            definition.Effects = effects ?? Array.Empty<HeartNodeEffect>();
            definition.Tags = Array.Empty<string>();
            definition.ConflictNodeIds = Array.Empty<string>();
            return definition;
        }

        private static GeneratedHeartNodeState AddNode(
            GeneratedRunGraph graph,
            string nodeId,
            HeartNodeBranch branch,
            int depth,
            HeartNodeVisibility visibility = HeartNodeVisibility.Hidden,
            int level = 0)
        {
            var node = new GeneratedHeartNodeState
            {
                NodeId = nodeId,
                Branch = branch,
                Depth = depth,
                Visibility = visibility,
                Level = level,
                LockState = HeartNodeLockState.Available,
                LockedByNodeId = string.Empty
            };
            graph.Nodes.Add(node);
            return node;
        }

        private static void AddEdge(GeneratedRunGraph graph, string fromNodeId, string toNodeId)
        {
            graph.Edges.Add(new GeneratedHeartEdge
            {
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId
            });
        }

        private static GeneratedHeartNodeState FindNode(GeneratedRunGraph graph, string nodeId)
        {
            return graph.Nodes.Single(node => node.NodeId == nodeId);
        }

        private sealed class FixedEffectResolver : IHeartEffectValueResolver
        {
            public bool TryResolve(
                HeartNodeDefinitionSO definition,
                HeartNodeEffect effect,
                int currentLevel,
                out HeartResolvedEffectValue resolvedValue)
            {
                resolvedValue = new HeartResolvedEffectValue
                {
                    Label = "Archer Damage",
                    CurrentValueText = "100",
                    AfterPurchaseValueText = "112",
                    DeltaText = "+12"
                };
                return effect.Type == HeartNodeEffectType.ModifyArcherDamagePercent;
            }
        }
    }
}
