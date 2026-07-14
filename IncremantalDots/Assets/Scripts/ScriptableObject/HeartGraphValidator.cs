using System;
using System.Collections.Generic;

namespace DeadWalls
{
    /// <summary>
    /// Generated Castle Heart graph'inin structural ve Blueprint guarantee kurallarini denetler.
    /// Validation state degistirmez; broken graph'i runtime'a sessizce gecirmez.
    /// </summary>
    public static class HeartGraphValidator
    {
        private static readonly HeartNodeBranch[] Branches =
        {
            HeartNodeBranch.Army,
            HeartNodeBranch.Defense,
            HeartNodeBranch.Production,
            HeartNodeBranch.HeartMagic
        };

        public static void Validate(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            HeartGraphGenerationRequest request,
            List<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));
            if (graph == null)
            {
                errors.Add("Generated graph bos olamaz.");
                return;
            }
            if (catalog == null)
            {
                errors.Add("Heart catalog bos olamaz.");
                return;
            }
            if (request == null)
            {
                errors.Add("Generation request bos olamaz.");
                return;
            }

            if (graph.GraphVersion != GeneratedRunGraph.CurrentGraphVersion)
                errors.Add($"Desteklenmeyen graph version: {graph.GraphVersion}.");
            if (graph.CatalogVersion != catalog.CatalogVersion)
            {
                errors.Add($"Graph catalog version {graph.CatalogVersion}, aktif catalog version "
                           + $"{catalog.CatalogVersion} ile ayni degil.");
            }
            if (graph.Seed != request.Seed)
                errors.Add($"Graph seed {graph.Seed}, request seed {request.Seed} ile ayni degil.");
            if (!string.Equals(graph.RootNodeId, HeartGraphConstants.RootNodeId, StringComparison.Ordinal))
                errors.Add($"Graph root Id '{HeartGraphConstants.RootNodeId}' olmali.");

            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();
            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            var nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            var definitionsById = BuildDefinitionMap(catalog);

            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add($"Nodes[{i}] gecerli bir NodeId tasimiyor.");
                    continue;
                }

                if (!nodesById.TryAdd(node.NodeId, node))
                {
                    errors.Add($"Graph duplicate node tasiyor: {node.NodeId}");
                    continue;
                }

                bool isRoot = string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal);
                if (isRoot)
                {
                    ValidateRootState(node, errors);
                    continue;
                }

                if (!definitionsById.TryGetValue(node.NodeId, out HeartNodeDefinitionSO definition))
                {
                    errors.Add($"Graph node '{node.NodeId}' catalog'da yok.");
                    continue;
                }

                if (node.Branch != definition.Branch)
                    errors.Add($"Node '{node.NodeId}' branch'i definition ile ayni degil.");
                if (node.Depth < 1
                    || node.Depth < definition.MinimumDepth
                    || node.Depth > definition.MaximumDepth)
                {
                    errors.Add($"Node '{node.NodeId}' izinli olmayan depth {node.Depth} konumunda.");
                }

                if (node.Visibility != HeartNodeVisibility.Hidden)
                    errors.Add($"Run baslangicinda non-root node '{node.NodeId}' Hidden olmali.");
                if (node.Level != 0)
                    errors.Add($"Run baslangicinda non-root node '{node.NodeId}' level 0 olmali.");
                if (node.LockState != HeartNodeLockState.Available
                    || !string.IsNullOrEmpty(node.LockedByNodeId))
                {
                    errors.Add($"Run baslangicinda node '{node.NodeId}' lock tasiyamaz.");
                }
            }

            if (!nodesById.ContainsKey(graph.RootNodeId))
                errors.Add("Graph root node'u tasimiyor.");

            var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var uniqueEdges = new HashSet<string>(StringComparer.Ordinal);
            int crossLinkCount = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                if (edge == null
                    || string.IsNullOrWhiteSpace(edge.FromNodeId)
                    || string.IsNullOrWhiteSpace(edge.ToNodeId))
                {
                    errors.Add($"Edges[{i}] gecerli node Id'leri tasimiyor.");
                    continue;
                }

                string edgeKey = edge.FromNodeId + "\n" + edge.ToNodeId;
                if (!uniqueEdges.Add(edgeKey))
                    errors.Add($"Duplicate edge: {edge.FromNodeId} -> {edge.ToNodeId}");
                if (string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                    errors.Add($"Self edge yasak: {edge.FromNodeId}");
                if (!nodesById.TryGetValue(edge.FromNodeId, out GeneratedHeartNodeState from))
                {
                    errors.Add($"Edge source graph'ta yok: {edge.FromNodeId}");
                    continue;
                }
                if (!nodesById.TryGetValue(edge.ToNodeId, out GeneratedHeartNodeState to))
                {
                    errors.Add($"Edge target graph'ta yok: {edge.ToNodeId}");
                    continue;
                }
                if (to.Depth != from.Depth + 1)
                    errors.Add($"Edge depth ileri bir adim olmali: {edge.FromNodeId} -> {edge.ToNodeId}");

                if (!outgoing.TryGetValue(edge.FromNodeId, out List<string> targets))
                {
                    targets = new List<string>();
                    outgoing.Add(edge.FromNodeId, targets);
                }
                targets.Add(edge.ToNodeId);

                if (!string.Equals(edge.FromNodeId, graph.RootNodeId, StringComparison.Ordinal)
                    && from.Branch != to.Branch)
                {
                    crossLinkCount++;
                }
            }

            if (crossLinkCount > request.MaximumCrossLinks)
            {
                errors.Add(
                    $"Cross-link sayisi {crossLinkCount}, izinli max {request.MaximumCrossLinks} degerini asiyor.");
            }

            ValidateBranchSpines(graph, nodesById, uniqueEdges, request, errors);
            ValidateReachability(graph.RootNodeId, nodesById, outgoing, errors);
            ValidateGuarantees(nodesById, definitionsById, errors);
            ValidateRepeatableSinks(nodesById, definitionsById, errors);
            ValidateKeystonePairs(nodesById, definitionsById, request.KeystonePairCount, errors);
        }

        private static void ValidateRootState(GeneratedHeartNodeState root, List<string> errors)
        {
            if (root.Depth != 0)
                errors.Add("Root depth 0 olmali.");
            if (root.Visibility != HeartNodeVisibility.Revealed)
                errors.Add("Root run baslangicinda Revealed olmali.");
            if (root.Level != 1)
                errors.Add("Root run baslangicinda level 1 olmali.");
            if (root.LockState != HeartNodeLockState.Available
                || !string.IsNullOrEmpty(root.LockedByNodeId))
            {
                errors.Add("Root lock tasiyamaz.");
            }
        }

        private static void ValidateBranchSpines(
            GeneratedRunGraph graph,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            HashSet<string> uniqueEdges,
            HeartGraphGenerationRequest request,
            List<string> errors)
        {
            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                var byDepth = new Dictionary<int, GeneratedHeartNodeState>();
                foreach (KeyValuePair<string, GeneratedHeartNodeState> pair in nodesById)
                {
                    GeneratedHeartNodeState node = pair.Value;
                    if (string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal)
                        || node.Branch != branch)
                    {
                        continue;
                    }

                    if (byDepth.ContainsKey(node.Depth))
                        errors.Add($"{branch} branch'inde depth {node.Depth} duplicate node tasiyor.");
                    else
                        byDepth.Add(node.Depth, node);
                }

                if (byDepth.Count < request.MinimumBranchDepth
                    || byDepth.Count > request.MaximumBranchDepth)
                {
                    errors.Add(
                        $"{branch} branch uzunlugu {byDepth.Count}; izinli aralik "
                        + $"{request.MinimumBranchDepth}-{request.MaximumBranchDepth}.");
                }

                for (int depth = 1; depth <= byDepth.Count; depth++)
                {
                    if (!byDepth.TryGetValue(depth, out GeneratedHeartNodeState current))
                    {
                        errors.Add($"{branch} core path depth {depth} eksik.");
                        continue;
                    }

                    string previousNodeId = depth == 1
                        ? graph.RootNodeId
                        : byDepth.TryGetValue(depth - 1, out GeneratedHeartNodeState previous)
                            ? previous.NodeId
                            : string.Empty;
                    if (string.IsNullOrEmpty(previousNodeId)
                        || !uniqueEdges.Contains(previousNodeId + "\n" + current.NodeId))
                    {
                        errors.Add($"{branch} core edge depth {depth} icin eksik.");
                    }
                }
            }
        }

        private static void ValidateReachability(
            string rootNodeId,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, List<string>> outgoing,
            List<string> errors)
        {
            if (!nodesById.ContainsKey(rootNodeId))
                return;

            var visited = new HashSet<string>(StringComparer.Ordinal) { rootNodeId };
            var queue = new Queue<string>();
            queue.Enqueue(rootNodeId);
            while (queue.Count > 0)
            {
                string nodeId = queue.Dequeue();
                if (!outgoing.TryGetValue(nodeId, out List<string> targets))
                    continue;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (visited.Add(targets[i]))
                        queue.Enqueue(targets[i]);
                }
            }

            foreach (string nodeId in nodesById.Keys)
            {
                if (!visited.Contains(nodeId))
                    errors.Add($"Disconnected/unreachable node: {nodeId}");
            }
        }

        private static void ValidateGuarantees(
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, HeartNodeDefinitionSO> definitionsById,
            List<string> errors)
        {
            string[] guaranteeTags =
            {
                HeartGraphConstants.RapidGuaranteeTag,
                HeartGraphConstants.FrostGuaranteeTag,
                HeartGraphConstants.FireballGuaranteeTag,
                HeartGraphConstants.WallGuaranteeTag
            };

            for (int tagIndex = 0; tagIndex < guaranteeTags.Length; tagIndex++)
            {
                bool found = false;
                foreach (string nodeId in nodesById.Keys)
                {
                    if (definitionsById.TryGetValue(nodeId, out HeartNodeDefinitionSO definition)
                        && HeartNodeTagUtility.HasTag(definition, guaranteeTags[tagIndex]))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    errors.Add($"Reachable guarantee graph'ta yok: {guaranteeTags[tagIndex]}");
            }
        }

        private static void ValidateRepeatableSinks(
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, HeartNodeDefinitionSO> definitionsById,
            List<string> errors)
        {
            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                bool found = false;
                foreach (KeyValuePair<string, GeneratedHeartNodeState> pair in nodesById)
                {
                    if (pair.Value.Branch != branch
                        || !definitionsById.TryGetValue(pair.Key, out HeartNodeDefinitionSO definition))
                    {
                        continue;
                    }

                    if (definition.Type == HeartNodeType.Repeatable
                        && HeartNodeTagUtility.HasTag(definition, HeartGraphConstants.RepeatableSinkTag))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    errors.Add($"{branch} branch'inde repeatable sink yok.");
            }
        }

        private static void ValidateKeystonePairs(
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, HeartNodeDefinitionSO> definitionsById,
            int requiredPairCount,
            List<string> errors)
        {
            int keystoneNodeCount = 0;
            foreach (string nodeId in nodesById.Keys)
            {
                if (!definitionsById.TryGetValue(nodeId, out HeartNodeDefinitionSO definition)
                    || definition.Type != HeartNodeType.Keystone)
                {
                    continue;
                }

                keystoneNodeCount++;
                string[] conflicts = definition.ConflictNodeIds ?? Array.Empty<string>();
                if (conflicts.Length != 1 || !nodesById.ContainsKey(conflicts[0]))
                {
                    errors.Add($"Keystone '{nodeId}' es Keystone olmadan graph'a yerlestirilmis.");
                    continue;
                }

                if (!definitionsById.TryGetValue(conflicts[0], out HeartNodeDefinitionSO partner)
                    || partner.Type != HeartNodeType.Keystone)
                {
                    errors.Add($"Keystone '{nodeId}' conflict hedefi Keystone degil.");
                    continue;
                }

                string[] partnerConflicts = partner.ConflictNodeIds ?? Array.Empty<string>();
                if (partnerConflicts.Length != 1
                    || !string.Equals(partnerConflicts[0], nodeId, StringComparison.Ordinal))
                {
                    errors.Add($"Graph'taki Keystone cifti simetrik degil: '{nodeId}'.");
                }
            }

            int requiredKeystoneNodeCount = requiredPairCount * 2;
            if (keystoneNodeCount != requiredKeystoneNodeCount)
            {
                errors.Add(
                    $"Graph {keystoneNodeCount} Keystone node tasiyor; "
                    + $"request {requiredKeystoneNodeCount} node ({requiredPairCount} cift) istiyor.");
            }
        }

        private static Dictionary<string, HeartNodeDefinitionSO> BuildDefinitionMap(HeartNodeCatalogSO catalog)
        {
            var result = new Dictionary<string, HeartNodeDefinitionSO>(StringComparer.Ordinal);
            HeartNodeDefinitionSO[] definitions = catalog.Nodes ?? Array.Empty<HeartNodeDefinitionSO>();
            for (int i = 0; i < definitions.Length; i++)
            {
                HeartNodeDefinitionSO definition = definitions[i];
                if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                    result[definition.Id] = definition;
            }
            return result;
        }
    }
}
