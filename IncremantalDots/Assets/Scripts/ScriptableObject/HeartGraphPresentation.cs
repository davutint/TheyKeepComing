using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    public interface IHeartEffectValueResolver
    {
        bool TryResolve(
            HeartNodeDefinitionSO definition,
            HeartNodeEffect effect,
            int currentLevel,
            out HeartResolvedEffectValue resolvedValue);
    }

    public struct HeartResolvedEffectValue
    {
        public string Label;
        public string CurrentValueText;
        public string AfterPurchaseValueText;
        public string DeltaText;
    }

    public sealed class HeartGraphPresentation
    {
        public readonly List<HeartGraphNodePresentation> Nodes = new List<HeartGraphNodePresentation>();
        public readonly List<HeartGraphEdgePresentation> Edges = new List<HeartGraphEdgePresentation>();
    }

    public sealed class HeartGraphNodePresentation
    {
        public string SlotId;
        public HeartNodeBranch Branch;
        public int Depth;
        public bool IsRoot;
        public bool IsExactContentVisible;
        public bool IsKeystoneConflictTarget;

        // Hidden node'larda bu alanlar bos/null kalir. UI internal graph Id'sine erisemez.
        public string ExactNodeId;
        public string Title;
        public string Description;
        public Sprite Icon;
        public HeartNodeType? Type;
        public HeartNodeRarity? Rarity;
        public int Level;
        public HeartNodeLockState LockState;
        public bool EffectInformationComplete;
        public readonly List<HeartEffectPresentation> Effects = new List<HeartEffectPresentation>();
        public HeartKeystoneConflictPresentation KeystoneConflict;
    }

    public sealed class HeartEffectPresentation
    {
        public HeartNodeEffectType Type;
        public string Label;
        public string CurrentValueText;
        public string AfterPurchaseValueText;
        public string DeltaText;
        public bool IsResolved;
    }

    public sealed class HeartKeystoneConflictPresentation
    {
        public string ConflictingChoiceSlotId;
        public string ConflictingChoiceTitle;
        public bool ConflictingChoiceIsRevealed;
        public bool WillLockOnPurchase;
        public bool IsAlreadyLockedByThisChoice;
        public bool SourceIsLockedByConflictingChoice;
    }

    public sealed class HeartGraphEdgePresentation
    {
        public string FromSlotId;
        public string ToSlotId;
        public HeartNodeBranch FromBranch;
        public HeartNodeBranch ToBranch;
    }

    public static class HeartGraphSlotUtility
    {
        public const string RootSlotId = "heart:root";

        public static string GetSlotId(GeneratedHeartNodeState node, string rootNodeId)
        {
            if (node == null)
                return string.Empty;
            if (string.Equals(node.NodeId, rootNodeId, StringComparison.Ordinal))
                return RootSlotId;
            return node.Branch.ToString().ToLowerInvariant() + ":" + node.Depth;
        }
    }

    /// <summary>
    /// Internal graph state'ini UI'nin tuketebilecegi hidden-safe bir modele cevirir.
    /// Hidden node Id/title/effect bilgisini redakte eder; numeric effect icin E4 resolver'i zorunludur.
    /// </summary>
    public static class HeartGraphPresentationBuilder
    {
        public static bool TryBuild(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            IHeartEffectValueResolver effectValueResolver,
            out HeartGraphPresentation presentation,
            out List<string> errors)
        {
            presentation = new HeartGraphPresentation();
            errors = new List<string>();
            if (graph == null)
            {
                errors.Add("Generated Heart graph bos olamaz.");
                return false;
            }
            if (catalog == null)
            {
                errors.Add("Heart catalog bos olamaz.");
                return false;
            }

            var nodesById = new Dictionary<string, GeneratedHeartNodeState>(StringComparer.Ordinal);
            var slotsByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);
            var presentationsBySlot = new Dictionary<string, HeartGraphNodePresentation>(StringComparer.Ordinal);
            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();

            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
                {
                    errors.Add($"Heart presentation Nodes[{i}] gecerli bir Id tasimiyor.");
                    continue;
                }
                if (!nodesById.TryAdd(node.NodeId, node))
                {
                    errors.Add($"Heart presentation duplicate node tasiyor: {node.NodeId}");
                    continue;
                }

                string slotId = HeartGraphSlotUtility.GetSlotId(node, graph.RootNodeId);
                if (string.IsNullOrEmpty(slotId) || presentationsBySlot.ContainsKey(slotId))
                {
                    errors.Add($"Heart presentation duplicate/gecersiz slot tasiyor: {slotId}");
                    continue;
                }

                var nodePresentation = CreateNodePresentation(
                    graph,
                    catalog,
                    node,
                    slotId,
                    effectValueResolver,
                    errors);
                presentation.Nodes.Add(nodePresentation);
                slotsByNodeId.Add(node.NodeId, slotId);
                presentationsBySlot.Add(slotId, nodePresentation);
            }

            BuildSafeEdges(graph, nodesById, slotsByNodeId, presentation, errors);
            BuildVisibleKeystoneConflicts(
                catalog,
                nodesById,
                slotsByNodeId,
                presentationsBySlot,
                errors);
            return errors.Count == 0;
        }

        private static HeartGraphNodePresentation CreateNodePresentation(
            GeneratedRunGraph graph,
            HeartNodeCatalogSO catalog,
            GeneratedHeartNodeState node,
            string slotId,
            IHeartEffectValueResolver effectValueResolver,
            List<string> errors)
        {
            bool isRoot = string.Equals(node.NodeId, graph.RootNodeId, StringComparison.Ordinal);
            bool exactContentVisible = isRoot || node.Visibility == HeartNodeVisibility.Revealed;
            var result = new HeartGraphNodePresentation
            {
                SlotId = slotId,
                Branch = node.Branch,
                Depth = node.Depth,
                IsRoot = isRoot,
                IsExactContentVisible = exactContentVisible,
                ExactNodeId = exactContentVisible ? node.NodeId : null,
                Title = isRoot ? "Castle Heart" : string.Empty,
                Description = string.Empty,
                Icon = null,
                Type = null,
                Rarity = null,
                Level = exactContentVisible ? node.Level : 0,
                LockState = exactContentVisible ? node.LockState : HeartNodeLockState.Available,
                EffectInformationComplete = true
            };

            if (!exactContentVisible || isRoot)
                return result;

            HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
            if (definition == null)
            {
                errors.Add($"Revealed Heart node catalog'da yok: {node.NodeId}");
                result.EffectInformationComplete = false;
                return result;
            }

            result.Title = definition.Title;
            result.Description = definition.Description;
            result.Icon = definition.Icon;
            result.Type = definition.Type;
            result.Rarity = definition.Rarity;
            BuildEffectPresentations(
                definition,
                node.Level,
                effectValueResolver,
                result,
                errors);
            return result;
        }

        private static void BuildEffectPresentations(
            HeartNodeDefinitionSO definition,
            int currentLevel,
            IHeartEffectValueResolver effectValueResolver,
            HeartGraphNodePresentation nodePresentation,
            List<string> errors)
        {
            HeartNodeEffect[] effects = definition.Effects ?? Array.Empty<HeartNodeEffect>();
            for (int i = 0; i < effects.Length; i++)
            {
                HeartNodeEffect effect = effects[i];
                if (TryBuildBehaviorEffect(effect, out HeartEffectPresentation behaviorPresentation))
                {
                    nodePresentation.Effects.Add(behaviorPresentation);
                    continue;
                }

                if (effect.Type == HeartNodeEffectType.None)
                {
                    errors.Add($"Revealed Heart node '{definition.Id}' None effect tasiyor.");
                    nodePresentation.EffectInformationComplete = false;
                    continue;
                }

                if (effectValueResolver == null
                    || !effectValueResolver.TryResolve(
                        definition,
                        effect,
                        currentLevel,
                        out HeartResolvedEffectValue resolved))
                {
                    errors.Add(
                        $"Revealed Heart node '{definition.Id}' effect '{effect.Type}' icin "
                        + "gercek numeric sonuc cozumlenemedi.");
                    nodePresentation.EffectInformationComplete = false;
                    nodePresentation.Effects.Add(new HeartEffectPresentation
                    {
                        Type = effect.Type,
                        Label = effect.Type.ToString(),
                        IsResolved = false
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(resolved.Label)
                    || string.IsNullOrWhiteSpace(resolved.AfterPurchaseValueText))
                {
                    errors.Add(
                        $"Heart effect resolver '{definition.Id}/{effect.Type}' icin eksik sunum dondurdu.");
                    nodePresentation.EffectInformationComplete = false;
                }

                nodePresentation.Effects.Add(new HeartEffectPresentation
                {
                    Type = effect.Type,
                    Label = resolved.Label ?? string.Empty,
                    CurrentValueText = resolved.CurrentValueText ?? string.Empty,
                    AfterPurchaseValueText = resolved.AfterPurchaseValueText ?? string.Empty,
                    DeltaText = resolved.DeltaText ?? string.Empty,
                    IsResolved = !string.IsNullOrWhiteSpace(resolved.Label)
                                 && !string.IsNullOrWhiteSpace(resolved.AfterPurchaseValueText)
                });
            }
        }

        private static bool TryBuildBehaviorEffect(
            HeartNodeEffect effect,
            out HeartEffectPresentation presentation)
        {
            string summary;
            switch (effect.Type)
            {
                case HeartNodeEffectType.UnlockArcherType:
                    summary = $"Unlock {effect.ArcherType} Archer";
                    break;
                case HeartNodeEffectType.UnlockSpellcasting:
                    summary = "Unlock Spellcasting";
                    break;
                case HeartNodeEffectType.EnableSplitShot:
                    summary = "Enable Split Shot";
                    break;
                case HeartNodeEffectType.EnableBurningGround:
                    summary = $"Burning Ground · {FireballEvolutionRules.BurningGroundDurationSeconds:0}s · "
                              + $"{FireballEvolutionRules.BurningGroundTickCount} × "
                              + $"{FireballEvolutionRules.BurningGroundDamageMultiplierPerTick * 100f:0}% damage";
                    break;
                case HeartNodeEffectType.EnableSecondBlast:
                    summary = $"Second Blast · {FireballEvolutionRules.SecondBlastDelaySeconds:0.00}s · "
                              + $"{FireballEvolutionRules.SecondBlastDamageMultiplier * 100f:0}% damage";
                    break;
                default:
                    presentation = null;
                    return false;
            }

            presentation = new HeartEffectPresentation
            {
                Type = effect.Type,
                Label = summary,
                AfterPurchaseValueText = summary,
                IsResolved = true
            };
            return true;
        }

        private static void BuildSafeEdges(
            GeneratedRunGraph graph,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, string> slotsByNodeId,
            HeartGraphPresentation presentation,
            List<string> errors)
        {
            List<GeneratedHeartEdge> edges = graph.Edges ?? new List<GeneratedHeartEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                GeneratedHeartEdge edge = edges[i];
                if (edge == null
                    || !nodesById.TryGetValue(edge.FromNodeId, out GeneratedHeartNodeState from)
                    || !nodesById.TryGetValue(edge.ToNodeId, out GeneratedHeartNodeState to)
                    || !slotsByNodeId.TryGetValue(edge.FromNodeId, out string fromSlotId)
                    || !slotsByNodeId.TryGetValue(edge.ToNodeId, out string toSlotId))
                {
                    errors.Add($"Heart presentation Edges[{i}] bilinmeyen node tasiyor.");
                    continue;
                }

                presentation.Edges.Add(new HeartGraphEdgePresentation
                {
                    FromSlotId = fromSlotId,
                    ToSlotId = toSlotId,
                    FromBranch = from.Branch,
                    ToBranch = to.Branch
                });
            }
        }

        private static void BuildVisibleKeystoneConflicts(
            HeartNodeCatalogSO catalog,
            Dictionary<string, GeneratedHeartNodeState> nodesById,
            Dictionary<string, string> slotsByNodeId,
            Dictionary<string, HeartGraphNodePresentation> presentationsBySlot,
            List<string> errors)
        {
            foreach (KeyValuePair<string, GeneratedHeartNodeState> pair in nodesById)
            {
                GeneratedHeartNodeState node = pair.Value;
                if (node.Visibility != HeartNodeVisibility.Revealed)
                    continue;

                HeartNodeDefinitionSO definition = catalog.GetNode(node.NodeId);
                if (definition == null || definition.Type != HeartNodeType.Keystone)
                    continue;

                string[] conflictIds = definition.ConflictNodeIds ?? Array.Empty<string>();
                if (conflictIds.Length != 1
                    || !nodesById.TryGetValue(conflictIds[0], out GeneratedHeartNodeState conflictNode)
                    || !slotsByNodeId.TryGetValue(conflictNode.NodeId, out string conflictSlotId)
                    || !slotsByNodeId.TryGetValue(node.NodeId, out string sourceSlotId)
                    || !presentationsBySlot.TryGetValue(sourceSlotId, out HeartGraphNodePresentation sourcePresentation)
                    || !presentationsBySlot.TryGetValue(conflictSlotId, out HeartGraphNodePresentation conflictPresentation))
                {
                    errors.Add($"Visible Keystone '{node.NodeId}' graph'ta gecerli conflict slotu bulamadi.");
                    continue;
                }

                HeartNodeDefinitionSO conflictDefinition = catalog.GetNode(conflictNode.NodeId);
                if (conflictDefinition == null || conflictDefinition.Type != HeartNodeType.Keystone)
                {
                    errors.Add($"Visible Keystone '{node.NodeId}' conflict definition'i gecersiz.");
                    continue;
                }

                // Keystone, remote exact-node gizliliginin Blueprint'teki tek acik istisnasidir:
                // karsi secimin basligi ve kapanacak safe slot gosterilir; internal Id disari verilmez.
                sourcePresentation.KeystoneConflict = new HeartKeystoneConflictPresentation
                {
                    ConflictingChoiceSlotId = conflictSlotId,
                    ConflictingChoiceTitle = conflictDefinition.Title,
                    ConflictingChoiceIsRevealed = conflictNode.Visibility == HeartNodeVisibility.Revealed,
                    WillLockOnPurchase = node.Level == 0
                                         && node.LockState == HeartNodeLockState.Available,
                    IsAlreadyLockedByThisChoice =
                        conflictNode.LockState == HeartNodeLockState.KeystoneConflict
                        && string.Equals(
                            conflictNode.LockedByNodeId,
                            node.NodeId,
                            StringComparison.Ordinal),
                    SourceIsLockedByConflictingChoice =
                        node.LockState == HeartNodeLockState.KeystoneConflict
                        && string.Equals(
                            node.LockedByNodeId,
                            conflictNode.NodeId,
                            StringComparison.Ordinal)
                };
                conflictPresentation.IsKeystoneConflictTarget = true;
            }
        }
    }
}
