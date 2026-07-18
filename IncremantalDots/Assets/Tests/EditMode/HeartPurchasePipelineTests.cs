using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DeadWalls.Tests
{
    public class HeartPurchasePipelineTests
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
        public void Pricing_PlusTenEqualsTenSequentialPurchases()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "damage_sink", HeartNodeBranch.Army, HeartNodeType.Repeatable);
            definition.BaseGraveEssenceCost = 100L;
            definition.CostGrowthPerLevel = 0.25d;

            Assert.That(
                HeartPurchasePricing.TryGetTotalCost(definition, 0, 10, out long bulkCost),
                Is.True);

            long sequentialCost = 0L;
            for (int level = 0; level < 10; level++)
            {
                Assert.That(
                    HeartPurchasePricing.TryGetLevelCost(definition, level, out long levelCost),
                    Is.True);
                sequentialCost += levelCost;
            }

            Assert.That(bulkCost, Is.EqualTo(2125L));
            Assert.That(bulkCost, Is.EqualTo(sequentialCost));
        }

        [Test]
        public void Pricing_BuyMaxUsesBinarySearchWithoutPerLevelLoopContract()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "endless_sink", HeartNodeBranch.Army, HeartNodeType.Repeatable);
            definition.BaseGraveEssenceCost = 1L;
            definition.CostGrowthPerLevel = 0d;

            bool quoted = HeartPurchasePricing.TryGetAffordableLevels(
                definition,
                0,
                int.MaxValue,
                out int levelCount,
                out long totalCost);

            Assert.That(quoted, Is.True);
            Assert.That(levelCount, Is.EqualTo(int.MaxValue));
            Assert.That(totalCost, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void LargeValues_PreserveLongCostAndDoubleDamageMath()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "large_damage", HeartNodeBranch.Army, HeartNodeType.Repeatable);
            definition.BaseGraveEssenceCost = 3_000_000_000L;
            definition.CostGrowthPerLevel = 0.5d;
            var effect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ModifyArcherDamagePercent,
                Value = 1_000_000_000_000d
            };

            Assert.That(
                HeartPurchasePricing.TryGetLevelCost(definition, 100, out long cost),
                Is.True);
            Assert.That(cost, Is.EqualTo(153_000_000_000L));
            Assert.That(
                HeartEffectMath.TryCalculateActual(effect, 100d, effect.Value,
                    out double damage, out string error),
                Is.True,
                error);
            Assert.That(damage, Is.EqualTo(100_000_000_000_100d).Within(0.5d));
            Assert.That(typeof(IHeartGraveEssenceWallet).IsAssignableFrom(typeof(GameManager)), Is.True);
        }

        [Test]
        public void UnlockPurchase_SpendsOnlyGraveEssenceAndRevealsOutgoingNeighbor()
        {
            var unlockEffect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.UnlockArcherType,
                ArcherType = ArcherType.Rapid
            };
            HeartNodeDefinitionSO unlock = CreateDefinition(
                "rapid_unlock", HeartNodeBranch.Army, HeartNodeType.Unlock, unlockEffect);
            HeartNodeDefinitionSO child = CreateDefinition(
                "army_child", HeartNodeBranch.Army, HeartNodeType.Repeatable);
            HeartNodeCatalogSO catalog = CreateCatalog(unlock, child);
            GeneratedRunGraph graph = CreateGraph();
            AddNode(graph, unlock.Id, unlock.Branch, 1, HeartNodeVisibility.Revealed);
            AddNode(graph, child.Id, child.Branch, 2, HeartNodeVisibility.Hidden);
            AddEdge(graph, HeartGraphConstants.RootNodeId, unlock.Id);
            AddEdge(graph, unlock.Id, child.Id);
            var wallet = new FakeWallet(100L);
            var pipeline = new HeartEffectPipeline(new FakeBaselineProvider());

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, unlock.Id, HeartPurchaseQuantity.One, wallet, pipeline);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(90L));
            Assert.That(FindNode(graph, unlock.Id).Level, Is.EqualTo(1));
            Assert.That(FindNode(graph, child.Id).Visibility, Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(result.NewlyRevealedNodeIds, Is.EqualTo(new[] { child.Id }));
            Assert.That(pipeline.IsBehaviorEnabled(unlockEffect), Is.True);
        }

        [Test]
        public void RepeatablePlusTen_AppliesExactLevelsCostAndActualNumericEffect()
        {
            var damageEffect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ModifyArcherDamagePercent,
                ArcherType = ArcherType.Basic,
                Value = 0.10f
            };
            HeartNodeDefinitionSO definition = CreateDefinition(
                "bow_mastery", HeartNodeBranch.Army, HeartNodeType.Repeatable, damageEffect);
            definition.BaseGraveEssenceCost = 10L;
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var provider = new FakeBaselineProvider();
            provider.Add(damageEffect, new HeartEffectBaseline("Basic Damage", 100d, "", 0));
            var pipeline = new HeartEffectPipeline(provider);
            var wallet = new FakeWallet(100L);

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.Ten, wallet, pipeline);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Quote.LevelsToBuy, Is.EqualTo(10));
            Assert.That(result.Quote.TotalGraveEssenceCost, Is.EqualTo(100L));
            Assert.That(FindNode(graph, definition.Id).Level, Is.EqualTo(10));
            Assert.That(wallet.GraveEssenceAmount, Is.Zero);
            Assert.That(pipeline.TryGetActualValue(damageEffect, out double actual), Is.True);
            Assert.That(actual, Is.EqualTo(200d).Within(0.0001d));
            Assert.That(
                pipeline.TryResolve(definition, damageEffect, 10, out HeartResolvedEffectValue resolved),
                Is.True);
            Assert.That(resolved.CurrentValueText, Is.EqualTo("200"));
            Assert.That(resolved.AfterPurchaseValueText, Is.EqualTo("210"));
            Assert.That(resolved.DeltaText, Is.EqualTo("+10"));
        }

        [Test]
        public void RepeatableBuyMax_BuysOnlyExactlyAffordableLevels()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "wall_sink", HeartNodeBranch.Defense, HeartNodeType.Repeatable);
            definition.BaseGraveEssenceCost = 10L;
            definition.CostGrowthPerLevel = 0.5d;
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var wallet = new FakeWallet(46L);

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.BuyMax, wallet, null);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Quote.LevelsToBuy, Is.EqualTo(3));
            Assert.That(result.Quote.TotalGraveEssenceCost, Is.EqualTo(45L));
            Assert.That(FindNode(graph, definition.Id).Level, Is.EqualTo(3));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(1L));
        }

        [Test]
        public void InsufficientPlusTen_DoesNotSpendMutateOrApplyEffects()
        {
            var effect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ModifyArcherDamagePercent,
                Value = 0.10f
            };
            HeartNodeDefinitionSO definition = CreateDefinition(
                "expensive_sink", HeartNodeBranch.Army, HeartNodeType.Repeatable, effect);
            definition.BaseGraveEssenceCost = 10L;
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var provider = new FakeBaselineProvider();
            provider.Add(effect, new HeartEffectBaseline("Damage", 100d, "", 0));
            var pipeline = new HeartEffectPipeline(provider);
            var wallet = new FakeWallet(99L);

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.Ten, wallet, pipeline);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.InsufficientGraveEssence));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(99L));
            Assert.That(FindNode(graph, definition.Id).Level, Is.Zero);
            Assert.That(pipeline.TryGetActualValue(effect, out double actual), Is.True);
            Assert.That(actual, Is.EqualTo(100d));
        }

        [Test]
        public void SinglePurchaseNode_RejectsBulkAndSecondPurchase()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "fireball_unlock", HeartNodeBranch.HeartMagic, HeartNodeType.Unlock,
                new HeartNodeEffect { Type = HeartNodeEffectType.UnlockSpellcasting });
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var wallet = new FakeWallet(100L);
            var pipeline = new HeartEffectPipeline(new FakeBaselineProvider());

            HeartPurchaseResult bulk = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.Ten, wallet, pipeline);
            HeartPurchaseResult first = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One, wallet, pipeline);
            HeartPurchaseResult second = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One, wallet, pipeline);

            Assert.That(bulk.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.RepeatableRequired));
            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(second.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.AlreadyPurchased));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(90L));
        }

        [Test]
        public void EvolutionPurchase_EnablesAuthoredBehaviorOnce()
        {
            var evolution = new HeartNodeEffect { Type = HeartNodeEffectType.EnableSecondBlast };
            HeartNodeDefinitionSO definition = CreateDefinition(
                "second_blast", HeartNodeBranch.HeartMagic, HeartNodeType.Evolution, evolution);
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var pipeline = new HeartEffectPipeline(new FakeBaselineProvider());

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One,
                new FakeWallet(100L), pipeline);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(pipeline.IsBehaviorEnabled(evolution), Is.True);
        }

        [Test]
        public void KeystonePurchase_LocksOnlyExactPartner()
        {
            HeartNodeDefinitionSO first = CreateDefinition(
                "keystone_arrow", HeartNodeBranch.Army, HeartNodeType.Keystone);
            HeartNodeDefinitionSO second = CreateDefinition(
                "keystone_cadence", HeartNodeBranch.Army, HeartNodeType.Keystone);
            HeartNodeDefinitionSO normal = CreateDefinition(
                "normal_army", HeartNodeBranch.Army, HeartNodeType.Unlock);
            first.ConflictNodeIds = new[] { second.Id };
            second.ConflictNodeIds = new[] { first.Id };
            HeartNodeCatalogSO catalog = CreateCatalog(first, second, normal);
            GeneratedRunGraph graph = CreateGraph();
            AddNode(graph, first.Id, first.Branch, 2, HeartNodeVisibility.Revealed);
            AddNode(graph, second.Id, second.Branch, 3, HeartNodeVisibility.Revealed);
            AddNode(graph, normal.Id, normal.Branch, 4, HeartNodeVisibility.Hidden);
            AddEdge(graph, HeartGraphConstants.RootNodeId, first.Id);
            AddEdge(graph, first.Id, second.Id);
            AddEdge(graph, second.Id, normal.Id);
            var wallet = new FakeWallet(100L);

            HeartPurchaseResult firstResult = HeartPurchaseService.TryPurchase(
                graph, catalog, first.Id, HeartPurchaseQuantity.One, wallet, null);
            long afterFirstPurchase = wallet.GraveEssenceAmount;
            HeartPurchaseResult partnerResult = HeartPurchaseService.TryPurchase(
                graph, catalog, second.Id, HeartPurchaseQuantity.One, wallet, null);

            Assert.That(firstResult.Succeeded, Is.True, firstResult.Message);
            Assert.That(firstResult.KeystoneConflictApplied, Is.True);
            Assert.That(FindNode(graph, second.Id).LockState,
                Is.EqualTo(HeartNodeLockState.KeystoneConflict));
            Assert.That(FindNode(graph, second.Id).LockedByNodeId, Is.EqualTo(first.Id));
            Assert.That(FindNode(graph, normal.Id).LockState, Is.EqualTo(HeartNodeLockState.Available));
            Assert.That(FindNode(graph, normal.Id).Visibility, Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(firstResult.NewlyRevealedNodeIds, Does.Contain(normal.Id));
            Assert.That(partnerResult.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.KeystoneLocked));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(afterFirstPurchase));
        }

        [Test]
        public void KeystonePurchase_EitherChoiceRevealsTheSameBranchContinuation()
        {
            HeartNodeDefinitionSO first = CreateDefinition(
                "keystone_arrow", HeartNodeBranch.Army, HeartNodeType.Keystone);
            HeartNodeDefinitionSO second = CreateDefinition(
                "keystone_cadence", HeartNodeBranch.Army, HeartNodeType.Keystone);
            HeartNodeDefinitionSO continuation = CreateDefinition(
                "army_continuation", HeartNodeBranch.Army, HeartNodeType.Unlock);
            first.ConflictNodeIds = new[] { second.Id };
            second.ConflictNodeIds = new[] { first.Id };
            HeartNodeCatalogSO catalog = CreateCatalog(first, second, continuation);
            GeneratedRunGraph graph = CreateGraph();
            AddNode(graph, first.Id, first.Branch, 2, HeartNodeVisibility.Revealed);
            AddNode(graph, second.Id, second.Branch, 3, HeartNodeVisibility.Revealed);
            AddNode(graph, continuation.Id, continuation.Branch, 4, HeartNodeVisibility.Hidden);
            AddEdge(graph, HeartGraphConstants.RootNodeId, first.Id);
            AddEdge(graph, first.Id, second.Id);
            AddEdge(graph, second.Id, continuation.Id);

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph,
                catalog,
                second.Id,
                HeartPurchaseQuantity.One,
                new FakeWallet(100L),
                null);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.KeystoneConflictApplied, Is.True);
            Assert.That(FindNode(graph, first.Id).LockState,
                Is.EqualTo(HeartNodeLockState.KeystoneConflict));
            Assert.That(FindNode(graph, first.Id).LockedByNodeId, Is.EqualTo(second.Id));
            Assert.That(FindNode(graph, continuation.Id).Visibility,
                Is.EqualTo(HeartNodeVisibility.Revealed));
            Assert.That(result.NewlyRevealedNodeIds, Does.Contain(continuation.Id));
        }

        [Test]
        public void SoftCapResolver_ShowsPositiveButDiminishingActualGain()
        {
            var effect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ModifyArcherFireRatePercent,
                ArcherType = ArcherType.Rapid,
                Value = 0.50f,
                SoftCap = 1f
            };
            HeartNodeDefinitionSO definition = CreateDefinition(
                "rapid_cadence", HeartNodeBranch.Army, HeartNodeType.Repeatable, effect);
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var provider = new FakeBaselineProvider();
            provider.Add(effect, new HeartEffectBaseline("Rapid Fire Rate", 2d, "/s", 3));
            var pipeline = new HeartEffectPipeline(provider);
            var wallet = new FakeWallet(100L);

            Assert.That(pipeline.TryResolve(definition, effect, 0, out HeartResolvedEffectValue before), Is.True);
            HeartPurchaseResult purchase = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One, wallet, pipeline);
            Assert.That(purchase.Succeeded, Is.True, purchase.Message);
            Assert.That(pipeline.TryResolve(definition, effect, 1, out HeartResolvedEffectValue after), Is.True);

            double firstDelta = ParseNumber(before.DeltaText, "/s");
            double secondDelta = ParseNumber(after.DeltaText, "/s");
            Assert.That(firstDelta, Is.GreaterThan(0d));
            Assert.That(secondDelta, Is.GreaterThan(0d));
            Assert.That(secondDelta, Is.LessThan(firstDelta));
        }

        [Test]
        public void MissingActualBaseline_RejectsPurchaseBeforeSpendOrGraphMutation()
        {
            var effect = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.IncreaseArrowCapacity,
                Value = 100f
            };
            HeartNodeDefinitionSO definition = CreateDefinition(
                "arrow_capacity", HeartNodeBranch.Production, HeartNodeType.Repeatable, effect);
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            var wallet = new FakeWallet(100L);
            var pipeline = new HeartEffectPipeline(new FakeBaselineProvider());

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One, wallet, pipeline);

            Assert.That(result.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.EffectRejected));
            Assert.That(result.Message, Does.Contain("baseline"));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(100L));
            Assert.That(FindNode(graph, definition.Id).Level, Is.Zero);
        }

        [Test]
        public void CostOverflow_RejectsQuoteAndLeavesStateUntouched()
        {
            HeartNodeDefinitionSO definition = CreateDefinition(
                "overflow_sink", HeartNodeBranch.Army, HeartNodeType.Repeatable);
            definition.BaseGraveEssenceCost = long.MaxValue;
            definition.CostGrowthPerLevel = 1d;
            HeartNodeCatalogSO catalog = CreateCatalog(definition);
            GeneratedRunGraph graph = CreateGraphWithVisibleNode(definition);
            FindNode(graph, definition.Id).Level = 1;
            var wallet = new FakeWallet(long.MaxValue);

            HeartPurchaseResult result = HeartPurchaseService.TryPurchase(
                graph, catalog, definition.Id, HeartPurchaseQuantity.One, wallet, null);

            Assert.That(result.FailureReason, Is.EqualTo(HeartPurchaseFailureReason.CostOverflow));
            Assert.That(FindNode(graph, definition.Id).Level, Is.EqualTo(1));
            Assert.That(wallet.GraveEssenceAmount, Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void EffectMath_SupportsRangeSlowCooldownAndArrowEconomyTargets()
        {
            var range = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.AddArcherRange,
                Value = 1f,
                SoftCap = 5f
            };
            var slow = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ReduceFrostSlowMultiplier,
                ArcherType = ArcherType.Frost,
                Value = 0.05f,
                SoftCap = 0.40f
            };
            var cooldown = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.ReduceSpellCooldownPercent,
                Value = 0.10f,
                SoftCap = 0.75f
            };
            var arrows = new HeartNodeEffect
            {
                Type = HeartNodeEffectType.IncreaseArrowCapacity,
                Value = 50f
            };

            Assert.That(HeartEffectMath.TryCalculateActual(range, 15d, 100d,
                out double rangeActual, out _), Is.True);
            Assert.That(HeartEffectMath.TryCalculateActual(slow, 0.55d, 0.10d,
                out double slowActual, out _), Is.True);
            Assert.That(HeartEffectMath.TryCalculateActual(cooldown, 45d, 0.50d,
                out double cooldownActual, out _), Is.True);
            Assert.That(HeartEffectMath.TryCalculateActual(arrows, 100d, 50d,
                out double arrowActual, out _), Is.True);

            Assert.That(rangeActual, Is.GreaterThan(15d).And.LessThan(20d));
            Assert.That(slowActual, Is.GreaterThan(0.40d).And.LessThan(0.55d));
            Assert.That(cooldownActual, Is.GreaterThan(11.25d).And.LessThan(45d));
            Assert.That(arrowActual, Is.EqualTo(150d));
        }

        private HeartNodeDefinitionSO CreateDefinition(
            string id,
            HeartNodeBranch branch,
            HeartNodeType type,
            params HeartNodeEffect[] effects)
        {
            HeartNodeDefinitionSO definition = ScriptableObject.CreateInstance<HeartNodeDefinitionSO>();
            _createdObjects.Add(definition);
            definition.Id = id;
            definition.Title = id;
            definition.Description = id + " description";
            definition.Branch = branch;
            definition.Type = type;
            definition.MinimumDepth = 1;
            definition.MaximumDepth = 8;
            definition.BaseGraveEssenceCost = 10L;
            definition.CostGrowthPerLevel = 0d;
            definition.ConflictNodeIds = Array.Empty<string>();
            definition.Tags = Array.Empty<string>();
            definition.Effects = effects ?? Array.Empty<HeartNodeEffect>();
            return definition;
        }

        private HeartNodeCatalogSO CreateCatalog(params HeartNodeDefinitionSO[] definitions)
        {
            HeartNodeCatalogSO catalog = ScriptableObject.CreateInstance<HeartNodeCatalogSO>();
            _createdObjects.Add(catalog);
            catalog.Nodes = definitions ?? Array.Empty<HeartNodeDefinitionSO>();
            return catalog;
        }

        private static GeneratedRunGraph CreateGraph()
        {
            var graph = new GeneratedRunGraph
            {
                Seed = 123456u,
                RootNodeId = HeartGraphConstants.RootNodeId
            };
            AddNode(
                graph,
                HeartGraphConstants.RootNodeId,
                HeartNodeBranch.HeartMagic,
                0,
                HeartNodeVisibility.Revealed,
                1);
            return graph;
        }

        private static GeneratedRunGraph CreateGraphWithVisibleNode(HeartNodeDefinitionSO definition)
        {
            GeneratedRunGraph graph = CreateGraph();
            AddNode(graph, definition.Id, definition.Branch, 1, HeartNodeVisibility.Revealed);
            AddEdge(graph, HeartGraphConstants.RootNodeId, definition.Id);
            return graph;
        }

        private static GeneratedHeartNodeState AddNode(
            GeneratedRunGraph graph,
            string nodeId,
            HeartNodeBranch branch,
            int depth,
            HeartNodeVisibility visibility,
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

        private static double ParseNumber(string text, string suffix)
        {
            return double.Parse(
                text.Replace(suffix, string.Empty).Replace(",", string.Empty),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        private sealed class FakeWallet : IHeartGraveEssenceWallet
        {
            public long GraveEssenceAmount { get; private set; }

            public FakeWallet(long graveEssence)
            {
                GraveEssenceAmount = graveEssence;
            }

            public bool TrySpendGraveEssenceAtHeart(long cost)
            {
                if (cost <= 0L || cost > GraveEssenceAmount)
                    return false;
                GraveEssenceAmount -= cost;
                return true;
            }
        }

        private sealed class FakeBaselineProvider : IHeartEffectBaselineProvider
        {
            private readonly Dictionary<HeartEffectTargetKey, HeartEffectBaseline> _baselines =
                new Dictionary<HeartEffectTargetKey, HeartEffectBaseline>();

            public void Add(HeartNodeEffect effect, HeartEffectBaseline baseline)
            {
                Assert.That(
                    HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out string error),
                    Is.True,
                    error);
                _baselines[target] = baseline;
            }

            public bool TryGetBaseline(
                HeartEffectTargetKey target,
                out HeartEffectBaseline baseline)
            {
                return _baselines.TryGetValue(target, out baseline);
            }
        }
    }
}
