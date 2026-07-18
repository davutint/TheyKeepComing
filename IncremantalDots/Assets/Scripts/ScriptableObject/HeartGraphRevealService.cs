using System;
using System.Collections.Generic;

namespace DeadWalls
{
    public sealed class HeartGraphRevealResult
    {
        public readonly List<string> NewlyRevealedNodeIds = new List<string>();
        public readonly List<string> Errors = new List<string>();

        public bool Succeeded => Errors.Count == 0;
    }

    /// <summary>
    /// Generated Heart graph'i uzerindeki visibility gecislerinin tek owner'idir.
    /// Graph icerigini secmez ve RNG kullanmaz; yalniz run basinda uretilmis edge'leri tuketir.
    /// </summary>
    public static class HeartGraphRevealService
    {
        public static HeartGraphRevealResult InitializeRunVisibility(GeneratedRunGraph graph)
        {
            return InitializeRunVisibility(graph, null);
        }

        public static HeartGraphRevealResult InitializeRunVisibility(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog)
        {
            var result = new HeartGraphRevealResult();
            if (!TryBuildLookup(graph, result.Errors, out Dictionary<string, GeneratedHeartNodeState> nodesById))
                return result;

            if (!nodesById.TryGetValue(graph.RootNodeId, out GeneratedHeartNodeState root))
            {
                result.Errors.Add($"Heart root node graph'ta yok: {graph.RootNodeId}");
                return result;
            }

            if (root.Depth != 0 || root.Level != 1)
            {
                result.Errors.Add("Heart root initial visibility kurulurken depth 0 ve level 1 olmali.");
                return result;
            }

            RevealNode(root, result.NewlyRevealedNodeIds);
            RevealOutgoingTargets(graph, root.NodeId, nodesById, catalog, result);
            return result;
        }

        public static HeartGraphRevealResult RevealAfterFirstPurchase(
            GeneratedRunGraph graph,
            string purchasedNodeId,
            int previousLevel)
        {
            return RevealAfterFirstPurchase(graph, null, purchasedNodeId, previousLevel);
        }

        public static HeartGraphRevealResult RevealAfterFirstPurchase(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            string purchasedNodeId,
            int previousLevel)
        {
            var result = new HeartGraphRevealResult();
            if (!TryBuildLookup(graph, result.Errors, out Dictionary<string, GeneratedHeartNodeState> nodesById))
                return result;

            if (string.IsNullOrWhiteSpace(purchasedNodeId)
                || !nodesById.TryGetValue(purchasedNodeId, out GeneratedHeartNodeState purchasedNode))
            {
                result.Errors.Add($"Satin alinan Heart node graph'ta yok: {purchasedNodeId}");
                return result;
            }

            if (purchasedNode.Visibility != HeartNodeVisibility.Revealed)
            {
                result.Errors.Add($"Hidden Heart node reveal kaynagi olamaz: {purchasedNodeId}");
                return result;
            }

            if (previousLevel < 0 || previousLevel > purchasedNode.Level)
            {
                result.Errors.Add(
                    $"Heart node level gecisi gecersiz: {purchasedNodeId} "
                    + $"{previousLevel} -> {purchasedNode.Level}");
                return result;
            }

            if (purchasedNode.Level < 1)
            {
                result.Errors.Add($"Satin alinmamis Heart node reveal kaynagi olamaz: {purchasedNodeId}");
                return result;
            }

            // Bulk alimda 0 -> 10 gibi bir gecis de ilk satin alimdir.
            // Repeatable sonraki transaction'larda previousLevel > 0 oldugu icin no-op'tur.
            if (previousLevel > 0)
                return result;

            RevealOutgoingTargets(graph, purchasedNodeId, nodesById, catalog, result);
            return result;
        }

        /// <summary>
        /// Eski exact save'lerde tek tarafi acilmis bir Keystone secimini deterministic olarak
        /// cift gorunurlugune tasir. Node/edge/level/lock state'ini veya RNG sonucunu degistirmez.
        /// </summary>
        public static HeartGraphRevealResult NormalizeKeystonePairVisibility(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog)
        {
            var result = new HeartGraphRevealResult();
            if (!TryBuildLookup(graph, result.Errors, out Dictionary<string, GeneratedHeartNodeState> nodesById))
                return result;
            if (catalog == null)
            {
                result.Errors.Add("Keystone reveal normalization icin Heart catalog gerekli.");
                return result;
            }

            var revealedNodeIds = new List<string>();
            foreach (KeyValuePair<string, GeneratedHeartNodeState> pair in nodesById)
            {
                if (pair.Value.Visibility == HeartNodeVisibility.Revealed)
                    revealedNodeIds.Add(pair.Key);
            }

            for (int i = 0; i < revealedNodeIds.Count; i++)
            {
                if (!nodesById.TryGetValue(revealedNodeIds[i], out GeneratedHeartNodeState revealed))
                    continue;
                RevealKeystonePartner(revealed, nodesById, catalog, result);
            }

            return result;
        }

        private static bool TryBuildLookup(
            GeneratedRunGraph graph,
            List<string> errors,
            out Dictionary<string, GeneratedHeartNodeState> nodesById)
        {
            nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            if (graph == null)
            {
                errors.Add("Generated Heart graph bos olamaz.");
                return false;
            }

            if (!string.Equals(graph.RootNodeId, HeartGraphConstants.RootNodeId, StringComparison.Ordinal))
                errors.Add($"Heart root Id '{HeartGraphConstants.RootNodeId}' olmali.");

            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add($"Heart graph Nodes[{i}] gecerli bir Id tasimiyor.");
                    continue;
                }

                if (!nodesById.TryAdd(node.NodeId, node))
                    errors.Add($"Heart graph duplicate node tasiyor: {node.NodeId}");
            }

            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                if (edge == null
                    || !nodesById.ContainsKey(edge.FromNodeId)
                    || !nodesById.ContainsKey(edge.ToNodeId))
                {
                    errors.Add($"Heart graph Edges[{i}] bilinmeyen node tasiyor.");
                }
            }

            return errors.Count == 0;
        }

        private static void RevealOutgoingTargets(
            GeneratedRunGraph graph,
            string sourceNodeId,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            HeartNodeCatalogSO catalog,
            HeartGraphRevealResult result)
        {
            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                if (!string.Equals(edge.FromNodeId, sourceNodeId, StringComparison.Ordinal))
                    continue;

                if (!nodesById.TryGetValue(edge.ToNodeId, out GeneratedHeartNodeState target))
                {
                    result.Errors.Add($"Reveal edge target graph'ta yok: {edge.ToNodeId}");
                    continue;
                }

                RevealNode(target, result.NewlyRevealedNodeIds);
                RevealKeystonePartner(target, nodesById, catalog, result);
            }
        }

        private static void RevealKeystonePartner(
            GeneratedHeartNodeState source,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            HeartNodeCatalogSO catalog,
            HeartGraphRevealResult result)
        {
            if (source == null || catalog == null)
                return;

            HeartNodeDefinitionSO definition = catalog.GetNode(source.NodeId);
            if (definition == null || definition.Type != HeartNodeType.Keystone)
                return;

            string[] conflictIds = definition.ConflictNodeIds ?? Array.Empty<string>();
            if (conflictIds.Length != 1
                || !nodesById.TryGetValue(conflictIds[0], out GeneratedHeartNodeState partner)
                || catalog.GetNode(partner.NodeId) is not HeartNodeDefinitionSO partnerDefinition
                || partnerDefinition.Type != HeartNodeType.Keystone
                || partnerDefinition.ConflictNodeIds == null
                || partnerDefinition.ConflictNodeIds.Length != 1
                || !string.Equals(
                    partnerDefinition.ConflictNodeIds[0],
                    source.NodeId,
                    StringComparison.Ordinal))
            {
                result.Errors.Add($"Keystone '{source.NodeId}' exact ve simetrik reveal partneri tasimiyor.");
                return;
            }

            RevealNode(partner, result.NewlyRevealedNodeIds);
        }

        private static void RevealNode(
            GeneratedHeartNodeState node,
            List<string> newlyRevealedNodeIds)
        {
            if (node.Visibility == HeartNodeVisibility.Revealed)
                return;

            node.Visibility = HeartNodeVisibility.Revealed;
            newlyRevealedNodeIds.Add(node.NodeId);
        }
    }
}
