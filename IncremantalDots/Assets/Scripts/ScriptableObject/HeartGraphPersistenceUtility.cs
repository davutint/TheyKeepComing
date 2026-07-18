using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadWalls
{
    /// <summary>
    /// Generated Castle Heart graph'inin exact save clone, restore validation ve effect replay owner'idir.
    /// Source catalog'dan yeni graph uretmez; uyumsuz save'i acik hata ile reddeder.
    /// </summary>
    public static class HeartGraphPersistenceUtility
    {
        private static readonly HeartNodeBranch[] Branches =
        {
            HeartNodeBranch.Army,
            HeartNodeBranch.Defense,
            HeartNodeBranch.Production,
            HeartNodeBranch.HeartMagic
        };

        public static GeneratedRunGraph CloneExact(GeneratedRunGraph source)
        {
            if (source == null)
                return null;

            var clone = new GeneratedRunGraph
            {
                GraphVersion = source.GraphVersion,
                CatalogVersion = source.CatalogVersion,
                Seed = source.Seed,
                RootNodeId = source.RootNodeId,
                Nodes = new List<GeneratedHeartNodeState>(),
                Edges = new List<GeneratedHeartEdge>()
            };

            List<GeneratedHeartNodeState> nodes = source.Nodes ?? new List<GeneratedHeartNodeState>();
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                clone.Nodes.Add(node == null
                    ? null
                    : new GeneratedHeartNodeState
                    {
                        NodeId = node.NodeId,
                        Branch = node.Branch,
                        Depth = node.Depth,
                        Visibility = node.Visibility,
                        Level = node.Level,
                        LockState = node.LockState,
                        LockedByNodeId = node.LockedByNodeId
                    });
            }

            List<GeneratedHeartEdge> edges = source.Edges ?? new List<GeneratedHeartEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                clone.Edges.Add(edge == null
                    ? null
                    : new GeneratedHeartEdge
                    {
                        FromNodeId = edge.FromNodeId,
                        ToNodeId = edge.ToNodeId
                    });
            }

            return clone;
        }

        public static bool TryValidateForRestore(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            out List<string> errors)
        {
            errors = new List<string>();
            if (graph == null)
            {
                errors.Add("Kayitli Castle Heart graph'i bos.");
                return false;
            }
            if (catalog == null)
            {
                errors.Add("Kayitli Castle Heart graph'i icin production catalog atanmamis.");
                return false;
            }
            if (graph.GraphVersion != GeneratedRunGraph.CurrentGraphVersion)
                errors.Add($"Desteklenmeyen saved Heart graph version: {graph.GraphVersion}.");
            int originalCatalogVersion = graph.CatalogVersion;
            bool migratedCatalog = false;
            if (graph.CatalogVersion != catalog.CatalogVersion)
            {
                migratedCatalog = TryMigrateProductionCatalogV1ToV2(
                    graph,
                    catalog,
                    out string migrationError);
                if (!migratedCatalog)
                {
                    errors.Add($"Saved Heart catalog version {graph.CatalogVersion}, aktif catalog version "
                               + $"{catalog.CatalogVersion} ile uyusmuyor; graph yeniden uretilmedi. "
                               + migrationError);
                }
            }

            ValidateRuntimeState(graph, catalog, errors);

            GeneratedRunGraph initialState = CloneExact(graph);
            NormalizeToInitialState(initialState);
            HeartGraphGenerationRequest validationRequest = CreateStructuralValidationRequest(
                initialState,
                catalog);
            HeartGraphValidator.Validate(initialState, catalog, validationRequest, errors);
            if (errors.Count > 0 && migratedCatalog)
                graph.CatalogVersion = originalCatalogVersion;
            return errors.Count == 0;
        }

        private static bool TryMigrateProductionCatalogV1ToV2(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            out string error)
        {
            error = string.Empty;
            if (graph == null
                || catalog == null
                || graph.CatalogVersion != 1
                || catalog.CatalogVersion != 2
                || catalog.GetNode("scorched_earth") == null
                || catalog.GetNode("echoing_detonation") == null)
            {
                error = "Desteklenen production catalog v1 -> v2 migration kosullari saglanmadi.";
                return false;
            }

            foreach (GeneratedHeartNodeState node in graph.Nodes ?? new List<GeneratedHeartNodeState>())
            {
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    continue;
                if (string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal))
                    continue;
                if (string.Equals(node.NodeId, "scorched_earth", StringComparison.Ordinal)
                    || string.Equals(node.NodeId, "echoing_detonation", StringComparison.Ordinal)
                    || catalog.GetNode(node.NodeId) == null)
                {
                    error = $"Saved v1 graph node '{node.NodeId}' production v2 migration'ina uygun degil.";
                    return false;
                }
            }

            // Yeni evolution'lar devam eden run'a enjekte edilmez. Yalniz catalog kimligi
            // yukseltilir; seed, node/edge listesi, reveal, level ve lock state exact kalir.
            graph.CatalogVersion = catalog.CatalogVersion;
            return true;
        }

        public static bool TryCreateRestoredPipeline(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            IHeartEffectBaselineProvider baselineProvider,
            IHeartRuntimeEffectSink runtimeSink,
            out HeartEffectPipeline pipeline,
            out string error)
        {
            pipeline = null;
            error = string.Empty;
            if (!TryValidateForRestore(graph, catalog, out List<string> validationErrors))
            {
                error = string.Join(" | ", validationErrors);
                return false;
            }

            var deferredSink = new DeferredRuntimeEffectSink();
            var restoredPipeline = new HeartEffectPipeline(baselineProvider, deferredSink);
            GeneratedHeartNodeState[] purchasedNodes = (graph.Nodes ?? new List<GeneratedHeartNodeState>())
                .Where(node => node != null
                               && node.Level > 0
                               && !string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal))
                .OrderBy(node => node.Depth)
                .ThenBy(node => node.NodeId, StringComparer.Ordinal)
                .ToArray();

            for (int i = 0; i < purchasedNodes.Length; i++)
            {
                GeneratedHeartNodeState node = purchasedNodes[i];
                HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
                if (!restoredPipeline.TryPrepare(
                        definition,
                        0,
                        node.Level,
                        out IHeartPreparedEffectTransaction transaction,
                        out string replayError))
                {
                    error = $"Heart effect replay '{node.NodeId}' level {node.Level}: {replayError}";
                    return false;
                }
                transaction.Commit();
            }

            pipeline = restoredPipeline;
            deferredSink.Activate(runtimeSink);
            return true;
        }

        private static void ValidateRuntimeState(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            List<string> errors)
        {
            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();
            var nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    continue;
                nodesById.TryAdd(node.NodeId, node);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                    continue;

                bool isRoot = string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal);
                if (isRoot)
                {
                    if (node.Visibility != HeartNodeVisibility.Revealed
                        || node.Level != 1
                        || node.LockState != HeartNodeLockState.Available
                        || !string.IsNullOrEmpty(node.LockedByNodeId))
                    {
                        errors.Add("Saved Heart root Revealed/level 1/unlocked olmali.");
                    }
                    continue;
                }

                HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
                if (definition == null)
                    continue;
                if (!Enum.IsDefined(typeof(HeartNodeVisibility), node.Visibility))
                    errors.Add($"Saved node '{node.NodeId}' visibility gecersiz.");
                if (!Enum.IsDefined(typeof(HeartNodeLockState), node.LockState))
                    errors.Add($"Saved node '{node.NodeId}' lock state gecersiz.");
                if (node.Level < 0)
                    errors.Add($"Saved node '{node.NodeId}' level negatif olamaz.");
                if (definition.Type != HeartNodeType.Repeatable && node.Level > 1)
                    errors.Add($"Saved non-repeatable node '{node.NodeId}' level 1'i asamaz.");
                if (node.Level > 0 && node.Visibility != HeartNodeVisibility.Revealed)
                    errors.Add($"Purchased saved node '{node.NodeId}' hidden olamaz.");

                if (node.LockState == HeartNodeLockState.Available)
                {
                    if (!string.IsNullOrEmpty(node.LockedByNodeId))
                        errors.Add($"Available saved node '{node.NodeId}' LockedByNodeId tasiyamaz.");
                    ValidatePurchasedKeystonePartner(node, definition, nodesById, catalog, errors);
                    continue;
                }

                ValidateKeystoneLock(node, definition, nodesById, catalog, errors);
            }
        }

        private static void ValidatePurchasedKeystonePartner(
            GeneratedHeartNodeState node,
            HeartNodeDefinitionSO definition,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            HeartNodeCatalogSO catalog,
            List<string> errors)
        {
            if (definition.Type != HeartNodeType.Keystone || node.Level <= 0)
                return;

            string[] conflicts = definition.ConflictNodeIds ?? Array.Empty<string>();
            if (conflicts.Length != 1
                || !nodesById.TryGetValue(conflicts[0], out GeneratedHeartNodeState partner)
                || partner.LockState != HeartNodeLockState.KeystoneConflict
                || !string.Equals(partner.LockedByNodeId, node.NodeId, StringComparison.Ordinal)
                || catalog.GetNode(partner.NodeId)?.Type != HeartNodeType.Keystone)
            {
                errors.Add($"Purchased Keystone '{node.NodeId}' exact partner lock state'i tasimiyor.");
            }
        }

        private static void ValidateKeystoneLock(
            GeneratedHeartNodeState node,
            HeartNodeDefinitionSO definition,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            HeartNodeCatalogSO catalog,
            List<string> errors)
        {
            if (node.LockState != HeartNodeLockState.KeystoneConflict
                || definition.Type != HeartNodeType.Keystone
                || node.Level != 0
                || string.IsNullOrWhiteSpace(node.LockedByNodeId)
                || !nodesById.TryGetValue(node.LockedByNodeId, out GeneratedHeartNodeState source)
                || source.Level <= 0
                || catalog.GetNode(source.NodeId)?.Type != HeartNodeType.Keystone)
            {
                errors.Add($"Saved node '{node.NodeId}' gecersiz Keystone lock state'i tasiyor.");
                return;
            }

            string[] conflicts = definition.ConflictNodeIds ?? Array.Empty<string>();
            HeartNodeDefinitionSO sourceDefinition = catalog.GetNode(source.NodeId);
            string[] sourceConflicts = sourceDefinition?.ConflictNodeIds ?? Array.Empty<string>();
            if (conflicts.Length != 1
                || sourceConflicts.Length != 1
                || !string.Equals(conflicts[0], source.NodeId, StringComparison.Ordinal)
                || !string.Equals(sourceConflicts[0], node.NodeId, StringComparison.Ordinal))
            {
                errors.Add($"Saved Keystone lock '{source.NodeId}' -> '{node.NodeId}' exact partner degil.");
            }
        }

        private static void NormalizeToInitialState(GeneratedRunGraph graph)
        {
            if (graph?.Nodes == null)
                return;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                GeneratedHeartNodeState node = graph.Nodes[i];
                if (node == null)
                    continue;
                bool isRoot = string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal);
                node.Visibility = isRoot ? HeartNodeVisibility.Revealed : HeartNodeVisibility.Hidden;
                node.Level = isRoot ? 1 : 0;
                node.LockState = HeartNodeLockState.Available;
                node.LockedByNodeId = string.Empty;
            }
        }

        private static HeartGraphGenerationRequest CreateStructuralValidationRequest(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog)
        {
            int minimumDepth = int.MaxValue;
            int maximumDepth = 1;
            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                int branchDepth = 0;
                List<GeneratedHeartNodeState> nodes = graph?.Nodes ?? new List<GeneratedHeartNodeState>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    GeneratedHeartNodeState node = nodes[i];
                    if (node != null
                        && !string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal)
                        && node.Branch == branch)
                    {
                        branchDepth = Math.Max(branchDepth, node.Depth);
                    }
                }
                minimumDepth = Math.Min(minimumDepth, branchDepth);
                maximumDepth = Math.Max(maximumDepth, branchDepth);
            }

            var nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            foreach (GeneratedHeartNodeState node in graph?.Nodes ?? new List<GeneratedHeartNodeState>())
            {
                if (node != null && !string.IsNullOrWhiteSpace(node.NodeId))
                    nodesById.TryAdd(node.NodeId, node);
            }

            int crossLinks = 0;
            foreach (GeneratedHeartEdge edge in graph?.Edges ?? new List<GeneratedHeartEdge>())
            {
                if (edge != null
                    && nodesById.TryGetValue(edge.FromNodeId, out GeneratedHeartNodeState from)
                    && nodesById.TryGetValue(edge.ToNodeId, out GeneratedHeartNodeState to)
                    && !string.Equals(from.NodeId, graph.RootNodeId, StringComparison.Ordinal)
                    && from.Branch != to.Branch)
                {
                    crossLinks++;
                }
            }

            int keystoneNodes = 0;
            foreach (GeneratedHeartNodeState node in nodesById.Values)
            {
                if (catalog.GetNode(node.NodeId)?.Type == HeartNodeType.Keystone)
                    keystoneNodes++;
            }

            return new HeartGraphGenerationRequest
            {
                Catalog = catalog,
                Seed = graph?.Seed ?? 0u,
                MinimumBranchDepth = Math.Max(1, minimumDepth == int.MaxValue ? 1 : minimumDepth),
                MaximumBranchDepth = Math.Max(1, maximumDepth),
                MaximumCrossLinks = Math.Max(0, crossLinks),
                KeystonePairCount = Math.Max(0, keystoneNodes / 2),
                MaximumAttempts = 1,
                StandardRarityWeight = 1,
                RareRarityWeight = 1
            };
        }

        private sealed class DeferredRuntimeEffectSink : IHeartRuntimeEffectSink
        {
            private readonly Dictionary<HeartEffectTargetKey, double> _numericValues =
                new Dictionary<HeartEffectTargetKey, double>();
            private readonly Dictionary<HeartEffectTargetKey, HeartNodeEffect> _behaviors =
                new Dictionary<HeartEffectTargetKey, HeartNodeEffect>();
            private IHeartRuntimeEffectSink _activeSink;

            public void ApplyNumericEffect(HeartEffectTargetKey target, double actualValue)
            {
                if (_activeSink != null)
                    _activeSink.ApplyNumericEffect(target, actualValue);
                else
                    _numericValues[target] = actualValue;
            }

            public void EnableBehaviorEffect(HeartNodeEffect effect)
            {
                if (_activeSink != null)
                {
                    _activeSink.EnableBehaviorEffect(effect);
                    return;
                }

                if (HeartEffectMath.TryCreateTarget(effect, out HeartEffectTargetKey target, out _))
                    _behaviors[target] = effect;
            }

            public void Activate(IHeartRuntimeEffectSink sink)
            {
                _activeSink = sink;
                if (sink == null)
                    return;

                foreach (KeyValuePair<HeartEffectTargetKey, double> pair in _numericValues
                             .OrderBy(pair => pair.Key.Type)
                             .ThenBy(pair => pair.Key.ArcherType)
                             .ThenBy(pair => pair.Key.Resource))
                {
                    sink.ApplyNumericEffect(pair.Key, pair.Value);
                }
                foreach (KeyValuePair<HeartEffectTargetKey, HeartNodeEffect> pair in _behaviors
                             .OrderBy(pair => pair.Key.Type)
                             .ThenBy(pair => pair.Key.ArcherType)
                             .ThenBy(pair => pair.Key.Resource))
                {
                    sink.EnableBehaviorEffect(pair.Value);
                }
                _numericValues.Clear();
                _behaviors.Clear();
            }
        }
    }
}
