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
            RevealOutgoingTargets(graph, root.NodeId, nodesById, result);
            return result;
        }

        public static HeartGraphRevealResult RevealAfterFirstPurchase(
            GeneratedRunGraph graph,
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

            RevealOutgoingTargets(graph, purchasedNodeId, nodesById, result);
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
            }
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
