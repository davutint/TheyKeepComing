using System;
using System.Collections.Generic;

namespace DeadWalls
{
    public enum HeartPurchaseQuantity
    {
        One = 1,
        Ten = 10,
        BuyMax = 100
    }

    public enum HeartPurchaseFailureReason
    {
        None = 0,
        InvalidRequest = 1,
        InvalidGraph = 2,
        InvalidCatalog = 3,
        UnknownNode = 4,
        RootCannotBePurchased = 5,
        Hidden = 6,
        KeystoneLocked = 7,
        AlreadyPurchased = 8,
        RepeatableRequired = 9,
        TechnicalLevelLimit = 10,
        CostOverflow = 11,
        InsufficientGraveEssence = 12,
        EffectRejected = 13,
        SpendRejected = 14
    }

    public sealed class HeartPurchaseQuote
    {
        public string NodeId;
        public HeartPurchaseQuantity Quantity;
        public int PreviousLevel;
        public int LevelsToBuy;
        public int NewLevel;
        public long TotalGraveEssenceCost;
        public long GraveEssenceBeforePurchase;
        public long GraveEssenceAfterPurchase;
    }

    public sealed class HeartPurchaseEvaluation
    {
        public HeartPurchaseQuote Quote;
        public HeartPurchaseFailureReason FailureReason;
        public string Message = string.Empty;

        public bool CanPurchase => FailureReason == HeartPurchaseFailureReason.None
                                   && Quote != null
                                   && Quote.LevelsToBuy > 0;
    }

    public sealed class HeartPurchaseResult
    {
        public HeartPurchaseQuote Quote;
        public int NodeDepth;
        public HeartPurchaseFailureReason FailureReason;
        public string Message = string.Empty;
        public bool KeystoneConflictApplied;
        public readonly List<string> NewlyRevealedNodeIds = new List<string>();

        public bool Succeeded => FailureReason == HeartPurchaseFailureReason.None
                                 && Quote != null;
    }

    /// <summary>
    /// Heart transaction'inin kullanabildigi tek currency contract'i. GameManager bu
    /// interface'i mevcut GraveEssenceAmount/TrySpendGraveEssenceAtHeart kapisiyla uygular.
    /// </summary>
    public interface IHeartGraveEssenceWallet
    {
        long GraveEssenceAmount { get; }
        bool TrySpendGraveEssenceAtHeart(long cost);
    }

    public static class HeartPurchasePricing
    {
        /// <summary>
        /// Linear ve bulk-safe fiyat: growthStep = ceil(base * growth),
        /// unit(level) = base + level * growthStep.
        /// </summary>
        public static bool TryGetLevelCost(
            HeartNodeDefinitionSO definition,
            int currentLevel,
            out long cost)
        {
            return TryGetTotalCost(definition, currentLevel, 1, out cost);
        }

        /// <summary>
        /// Exact arithmetic-series toplamidir; +10 ve Buy Max, ayni seviyelerde arka arkaya
        /// +1 almaktan farkli fiyat uretmez ve level sayisi kadar loop calistirmaz.
        /// </summary>
        public static bool TryGetTotalCost(
            HeartNodeDefinitionSO definition,
            int currentLevel,
            int levelCount,
            out long totalCost)
        {
            totalCost = 0L;
            if (definition == null
                || definition.BaseGraveEssenceCost <= 0L
                || currentLevel < 0
                || levelCount <= 0
                || levelCount > int.MaxValue - currentLevel
                || double.IsNaN(definition.CostGrowthPerLevel)
                || double.IsInfinity(definition.CostGrowthPerLevel)
                || definition.CostGrowthPerLevel < 0d)
            {
                return false;
            }

            try
            {
                decimal baseCost = definition.BaseGraveEssenceCost;
                decimal growth = (decimal)definition.CostGrowthPerLevel;
                decimal growthStep = decimal.Ceiling(baseCost * growth);
                decimal count = levelCount;
                decimal level = currentLevel;
                decimal levelIndexSum = count * (2m * level + count - 1m) / 2m;
                decimal total = count * baseCost + growthStep * levelIndexSum;
                if (total <= 0m || total > long.MaxValue)
                    return false;

                totalCost = (long)total;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        public static bool TryGetAffordableLevels(
            HeartNodeDefinitionSO definition,
            int currentLevel,
            long availableGraveEssence,
            out int levelCount,
            out long totalCost)
        {
            levelCount = 0;
            totalCost = 0L;
            if (definition == null
                || currentLevel < 0
                || currentLevel >= int.MaxValue
                || availableGraveEssence <= 0L
                || !TryGetLevelCost(definition, currentLevel, out long firstCost)
                || firstCost > availableGraveEssence)
            {
                return false;
            }

            int low = 1;
            int high = int.MaxValue - currentLevel;
            int bestCount = 0;
            long bestCost = 0L;
            while (low <= high)
            {
                int middle = low + (int)(((long)high - low) / 2L);
                if (TryGetTotalCost(definition, currentLevel, middle, out long candidateCost)
                    && candidateCost <= availableGraveEssence)
                {
                    bestCount = middle;
                    bestCost = candidateCost;
                    if (middle == int.MaxValue)
                        break;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            levelCount = bestCount;
            totalCost = bestCost;
            return levelCount > 0;
        }
    }

    /// <summary>
    /// Generated Heart graph uzerindeki quote, Grave Essence spend, level, effect, reveal
    /// ve Keystone lock gecislerinin tek transaction owner'idir.
    /// </summary>
    public static class HeartPurchaseService
    {
        public static HeartPurchaseEvaluation Evaluate(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            string nodeId,
            HeartPurchaseQuantity quantity,
            long availableGraveEssence)
        {
            TryBuildPlan(
                graph,
                catalog,
                nodeId,
                quantity,
                availableGraveEssence,
                out _,
                out HeartPurchaseEvaluation evaluation);
            return evaluation;
        }

        public static HeartPurchaseResult TryPurchase(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            string nodeId,
            HeartPurchaseQuantity quantity,
            IHeartGraveEssenceWallet wallet,
            IHeartEffectTransactionPlanner effectPlanner)
        {
            if (wallet == null)
            {
                return Failed(
                    HeartPurchaseFailureReason.InvalidRequest,
                    "Grave Essence wallet bos olamaz.");
            }

            if (!TryBuildPlan(
                    graph,
                    catalog,
                    nodeId,
                    quantity,
                    wallet.GraveEssenceAmount,
                    out PurchasePlan plan,
                    out HeartPurchaseEvaluation evaluation))
            {
                return Failed(evaluation.FailureReason, evaluation.Message, evaluation.Quote);
            }

            IHeartPreparedEffectTransaction preparedEffects = NoOpPreparedEffectTransaction.Instance;
            HeartNodeEffect[] effects = plan.Definition.Effects ?? Array.Empty<HeartNodeEffect>();
            if (effects.Length > 0)
            {
                string effectError = string.Empty;
                if (effectPlanner == null
                    || !effectPlanner.TryPrepare(
                        plan.Definition,
                        plan.Quote.PreviousLevel,
                        plan.Quote.NewLevel,
                        out preparedEffects,
                        out effectError)
                    || preparedEffects == null)
                {
                    return Failed(
                        HeartPurchaseFailureReason.EffectRejected,
                        string.IsNullOrWhiteSpace(effectError)
                            ? "Heart effect transaction hazirlanamadi."
                            : effectError,
                        plan.Quote);
                }
            }

            if (!wallet.TrySpendGraveEssenceAtHeart(plan.Quote.TotalGraveEssenceCost))
            {
                return Failed(
                    HeartPurchaseFailureReason.SpendRejected,
                    "Grave Essence bakiyesi transaction commit aninda degisti.",
                    plan.Quote);
            }

            // Preflight'tan sonra bu adimlar fail etmez. Boylece harcama sonrasi yarim state kalmaz.
            plan.Node.Level = plan.Quote.NewLevel;
            if (plan.KeystonePartner != null)
            {
                plan.KeystonePartner.LockState = HeartNodeLockState.KeystoneConflict;
                plan.KeystonePartner.LockedByNodeId = plan.Node.NodeId;
            }
            preparedEffects.Commit();

            var result = new HeartPurchaseResult
            {
                Quote = plan.Quote,
                NodeDepth = plan.Node.Depth,
                FailureReason = HeartPurchaseFailureReason.None,
                KeystoneConflictApplied = plan.KeystonePartner != null
            };
            for (int i = 0; i < plan.RevealTargets.Count; i++)
            {
                GeneratedHeartNodeState revealTarget = plan.RevealTargets[i];
                if (revealTarget.Visibility == HeartNodeVisibility.Revealed)
                    continue;

                revealTarget.Visibility = HeartNodeVisibility.Revealed;
                result.NewlyRevealedNodeIds.Add(revealTarget.NodeId);
            }

            return result;
        }

        private static bool TryBuildPlan(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            string nodeId,
            HeartPurchaseQuantity quantity,
            long availableGraveEssence,
            out PurchasePlan plan,
            out HeartPurchaseEvaluation evaluation)
        {
            plan = null;
            evaluation = new HeartPurchaseEvaluation();
            if (graph == null || catalog == null || string.IsNullOrWhiteSpace(nodeId)
                || availableGraveEssence < 0L)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidRequest,
                    "Heart purchase request gecersiz.");
            }
            if (quantity != HeartPurchaseQuantity.One
                && quantity != HeartPurchaseQuantity.Ten
                && quantity != HeartPurchaseQuantity.BuyMax)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidRequest,
                    "Heart purchase quantity gecersiz.");
            }

            var catalogErrors = new List<string>();
            catalog.CollectValidationErrors(catalogErrors);
            if (catalogErrors.Count > 0)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidCatalog,
                    string.Join(" | ", catalogErrors));
            }

            if (!TryBuildGraphLookup(
                    graph,
                    out Dictionary<string, GeneratedHeartNodeState> nodesById,
                    out string graphError))
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidGraph,
                    graphError);
            }
            if (string.Equals(nodeId, graph.RootNodeId, StringComparison.Ordinal))
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.RootCannotBePurchased,
                    "Castle Heart root satin alinamaz.");
            }
            if (!nodesById.TryGetValue(nodeId, out GeneratedHeartNodeState node)
                || catalog.GetNode(nodeId) is not HeartNodeDefinitionSO definition)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.UnknownNode,
                    $"Heart node graph/catalog'da yok: {nodeId}.");
            }
            if (definition.Branch != node.Branch)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidGraph,
                    $"Heart node branch uyusmuyor: {nodeId}.");
            }
            if (node.Visibility != HeartNodeVisibility.Revealed)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.Hidden,
                    "Hidden Heart node satin alinamaz.");
            }
            if (node.LockState != HeartNodeLockState.Available)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.KeystoneLocked,
                    "Heart node karsi Keystone tarafindan kilitli.");
            }
            if (node.Level < 0)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidGraph,
                    "Heart node level negatif olamaz.");
            }

            bool repeatable = definition.Type == HeartNodeType.Repeatable;
            if (!repeatable && node.Level > 0)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.AlreadyPurchased,
                    "Tek seferlik Heart node zaten satin alinmis.");
            }
            if (!repeatable && quantity != HeartPurchaseQuantity.One)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.RepeatableRequired,
                    "+10 ve Buy Max yalniz Repeatable Heart node'larda kullanilir.");
            }
            if (node.Level >= int.MaxValue)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.TechnicalLevelLimit,
                    "Heart node teknik level limitine ulasti.");
            }

            int levelCount;
            long totalCost;
            if (quantity == HeartPurchaseQuantity.One)
            {
                levelCount = 1;
                if (!HeartPurchasePricing.TryGetTotalCost(
                        definition, node.Level, levelCount, out totalCost))
                {
                    return FailEvaluation(
                        evaluation,
                        HeartPurchaseFailureReason.CostOverflow,
                        "Heart node maliyeti long sinirini asti.");
                }
            }
            else if (quantity == HeartPurchaseQuantity.Ten)
            {
                levelCount = 10;
                if (levelCount > int.MaxValue - node.Level)
                {
                    return FailEvaluation(
                        evaluation,
                        HeartPurchaseFailureReason.TechnicalLevelLimit,
                        "+10 teknik level limitini asiyor.");
                }
                if (!HeartPurchasePricing.TryGetTotalCost(
                        definition, node.Level, levelCount, out totalCost))
                {
                    return FailEvaluation(
                        evaluation,
                        HeartPurchaseFailureReason.CostOverflow,
                        "Heart +10 maliyeti long sinirini asti.");
                }
            }
            else
            {
                if (!HeartPurchasePricing.TryGetAffordableLevels(
                        definition,
                        node.Level,
                        availableGraveEssence,
                        out levelCount,
                        out totalCost))
                {
                    HeartPurchasePricing.TryGetLevelCost(definition, node.Level, out long nextCost);
                    evaluation.Quote = BuildQuote(
                        nodeId, quantity, node.Level, 0, nextCost, availableGraveEssence);
                    return FailEvaluation(
                        evaluation,
                        HeartPurchaseFailureReason.InsufficientGraveEssence,
                        "Sonraki Heart level'i icin Grave Essence yetersiz.");
                }
            }

            HeartPurchaseQuote quote = BuildQuote(
                nodeId,
                quantity,
                node.Level,
                levelCount,
                totalCost,
                availableGraveEssence);
            evaluation.Quote = quote;
            if (totalCost > availableGraveEssence)
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InsufficientGraveEssence,
                    $"{totalCost} Grave Essence gerekiyor.");
            }

            GeneratedHeartNodeState keystonePartner = null;
            if (definition.Type == HeartNodeType.Keystone
                && !TryGetKeystonePartner(
                    definition,
                    catalog,
                    nodesById,
                    out keystonePartner,
                    out string keystoneError))
            {
                return FailEvaluation(
                    evaluation,
                    HeartPurchaseFailureReason.InvalidCatalog,
                    keystoneError);
            }

            var revealTargets = new List<GeneratedHeartNodeState>();
            if (node.Level == 0)
            {
                var seenTargets = new HashSet<string>(StringComparer.Ordinal);
                AddCoupledRevealTargets(
                    graph,
                    catalog,
                    nodesById,
                    nodeId,
                    revealTargets,
                    seenTargets);

                // Generated v1 graph pairleri compatibility icin branch spine'inda ardisik
                // tutulur. Keystone secimi iki tarafin da outgoing hedeflerini acar; boylece
                // hangi doctrine secilirse secilsin partner disinda ayni dal devam eder.
                if (keystonePartner != null)
                {
                    AddCoupledRevealTargets(
                        graph,
                        catalog,
                        nodesById,
                        keystonePartner.NodeId,
                        revealTargets,
                        seenTargets);
                }
            }

            plan = new PurchasePlan(definition, node, quote, keystonePartner, revealTargets);
            evaluation.FailureReason = HeartPurchaseFailureReason.None;
            return true;
        }

        private static void AddCoupledRevealTargets(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            string sourceNodeId,
            List<GeneratedHeartNodeState> revealTargets,
            HashSet<string> seenTargets)
        {
            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                if (!string.Equals(edge.FromNodeId, sourceNodeId, StringComparison.Ordinal)
                    || !nodesById.TryGetValue(edge.ToNodeId, out GeneratedHeartNodeState target))
                {
                    continue;
                }

                AddRevealTarget(target, revealTargets, seenTargets);

                HeartNodeDefinitionSO targetDefinition = catalog.GetNode(target.NodeId);
                if (targetDefinition == null || targetDefinition.Type != HeartNodeType.Keystone)
                    continue;

                string[] conflictIds = targetDefinition.ConflictNodeIds ?? Array.Empty<string>();
                if (conflictIds.Length == 1
                    && nodesById.TryGetValue(conflictIds[0], out GeneratedHeartNodeState partner))
                {
                    AddRevealTarget(partner, revealTargets, seenTargets);
                }
            }
        }

        private static void AddRevealTarget(
            GeneratedHeartNodeState target,
            List<GeneratedHeartNodeState> revealTargets,
            HashSet<string> seenTargets)
        {
            if (target != null && seenTargets.Add(target.NodeId))
                revealTargets.Add(target);
        }

        private static bool TryBuildGraphLookup(
            GeneratedRunGraph graph,
            out Dictionary<string, GeneratedHeartNodeState> nodesById,
            out string error)
        {
            nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            error = string.Empty;
            if (!string.Equals(graph.RootNodeId, HeartGraphConstants.RootNodeId, StringComparison.Ordinal))
            {
                error = $"Heart root Id '{HeartGraphConstants.RootNodeId}' olmali.";
                return false;
            }

            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId)
                    || !nodesById.TryAdd(node.NodeId, node))
                {
                    error = $"Heart graph Nodes[{i}] duplicate/gecersiz.";
                    return false;
                }
            }
            if (!nodesById.TryGetValue(graph.RootNodeId, out GeneratedHeartNodeState root)
                || root.Depth != 0
                || root.Level != 1)
            {
                error = "Heart root graph'ta depth 0 / level 1 olmali.";
                return false;
            }

            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            var seenEdges = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                string edgeKey = edge == null ? string.Empty : edge.FromNodeId + "\n" + edge.ToNodeId;
                if (edge == null
                    || !nodesById.ContainsKey(edge.FromNodeId)
                    || !nodesById.ContainsKey(edge.ToNodeId)
                    || !seenEdges.Add(edgeKey))
                {
                    error = $"Heart graph Edges[{i}] duplicate/bilinmeyen node tasiyor.";
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetKeystonePartner(
            HeartNodeDefinitionSO definition,
            HeartNodeCatalogSO catalog,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            out GeneratedHeartNodeState partnerNode,
            out string error)
        {
            partnerNode = null;
            error = string.Empty;
            string[] conflictIds = definition.ConflictNodeIds ?? Array.Empty<string>();
            if (conflictIds.Length != 1
                || !nodesById.TryGetValue(conflictIds[0], out partnerNode)
                || catalog.GetNode(conflictIds[0]) is not HeartNodeDefinitionSO partnerDefinition
                || partnerDefinition.Type != HeartNodeType.Keystone
                || partnerDefinition.ConflictNodeIds == null
                || partnerDefinition.ConflictNodeIds.Length != 1
                || !string.Equals(
                    partnerDefinition.ConflictNodeIds[0], definition.Id, StringComparison.Ordinal))
            {
                error = $"Keystone '{definition.Id}' exact ve simetrik partner tasimiyor.";
                return false;
            }
            if (partnerNode.Level > 0)
            {
                error = $"Keystone partner '{partnerDefinition.Id}' zaten satin alinmis.";
                return false;
            }
            if (partnerNode.LockState != HeartNodeLockState.Available
                || !string.IsNullOrEmpty(partnerNode.LockedByNodeId))
            {
                error = $"Keystone partner '{partnerDefinition.Id}' satin alim oncesi available olmali.";
                return false;
            }

            return true;
        }

        private static HeartPurchaseQuote BuildQuote(
            string nodeId,
            HeartPurchaseQuantity quantity,
            int previousLevel,
            int levelCount,
            long totalCost,
            long availableGraveEssence)
        {
            return new HeartPurchaseQuote
            {
                NodeId = nodeId,
                Quantity = quantity,
                PreviousLevel = previousLevel,
                LevelsToBuy = levelCount,
                NewLevel = previousLevel + levelCount,
                TotalGraveEssenceCost = totalCost,
                GraveEssenceBeforePurchase = availableGraveEssence,
                GraveEssenceAfterPurchase = totalCost <= availableGraveEssence
                    ? availableGraveEssence - totalCost
                    : availableGraveEssence
            };
        }

        private static bool FailEvaluation(
            HeartPurchaseEvaluation evaluation,
            HeartPurchaseFailureReason reason,
            string message)
        {
            evaluation.FailureReason = reason;
            evaluation.Message = message ?? string.Empty;
            return false;
        }

        private static HeartPurchaseResult Failed(
            HeartPurchaseFailureReason reason,
            string message,
            HeartPurchaseQuote quote = null)
        {
            return new HeartPurchaseResult
            {
                Quote = quote,
                FailureReason = reason,
                Message = message ?? string.Empty
            };
        }

        private sealed class PurchasePlan
        {
            public readonly HeartNodeDefinitionSO Definition;
            public readonly GeneratedHeartNodeState Node;
            public readonly HeartPurchaseQuote Quote;
            public readonly GeneratedHeartNodeState KeystonePartner;
            public readonly List<GeneratedHeartNodeState> RevealTargets;

            public PurchasePlan(
                HeartNodeDefinitionSO definition,
                GeneratedHeartNodeState node,
                HeartPurchaseQuote quote,
                GeneratedHeartNodeState keystonePartner,
                List<GeneratedHeartNodeState> revealTargets)
            {
                Definition = definition;
                Node = node;
                Quote = quote;
                KeystonePartner = keystonePartner;
                RevealTargets = revealTargets;
            }
        }

        private sealed class NoOpPreparedEffectTransaction : IHeartPreparedEffectTransaction
        {
            public static readonly NoOpPreparedEffectTransaction Instance =
                new NoOpPreparedEffectTransaction();

            public void Commit()
            {
            }
        }
    }
}
