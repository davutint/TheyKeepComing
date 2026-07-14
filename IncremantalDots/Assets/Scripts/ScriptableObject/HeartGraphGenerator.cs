using System;
using System.Collections.Generic;

namespace DeadWalls
{
    public sealed class HeartGraphGenerationRequest
    {
        public HeartNodeCatalogSO Catalog;
        public uint Seed;
        public int MinimumBranchDepth;
        public int MaximumBranchDepth;
        public int MaximumCrossLinks;
        public int KeystonePairCount;
        public int MaximumAttempts;
        public int StandardRarityWeight;
        public int RareRarityWeight;
    }

    public sealed class HeartGraphGenerationReport
    {
        public int AttemptsUsed;
        public int SuccessfulAttempt;
        public uint SuccessfulAttemptSeed;
        public readonly List<string> Errors = new List<string>();

        public bool Succeeded => SuccessfulAttempt > 0 && Errors.Count == 0;
    }

    /// <summary>
    /// Authored Heart catalog'undan run basinda tam ve deterministic graph uretir.
    /// Reveal sirasinda RNG kullanilmaz; uretilen graph exact run state olarak saklanir.
    /// </summary>
    public static class HeartGraphGenerator
    {
        private static readonly HeartNodeBranch[] Branches =
        {
            HeartNodeBranch.Army,
            HeartNodeBranch.Defense,
            HeartNodeBranch.Production,
            HeartNodeBranch.HeartMagic
        };

        public static GeneratedRunGraph GenerateOrThrow(HeartGraphGenerationRequest request)
        {
            if (TryGenerate(request, out GeneratedRunGraph graph, out HeartGraphGenerationReport report))
                return graph;

            string detail = report.Errors.Count > 0
                ? string.Join(" | ", report.Errors)
                : "Bilinmeyen graph generation hatasi.";
            throw new InvalidOperationException($"Castle Heart graph uretilemedi: {detail}");
        }

        public static bool TryGenerate(
            HeartGraphGenerationRequest request,
            out GeneratedRunGraph graph,
            out HeartGraphGenerationReport report)
        {
            graph = null;
            report = new HeartGraphGenerationReport();

            var preflightErrors = new List<string>();
            ValidateRequest(request, preflightErrors);
            if (preflightErrors.Count > 0)
            {
                report.Errors.AddRange(preflightErrors);
                return false;
            }

            HeartNodeDefinitionSO[] sortedDefinitions = CopyAndSortDefinitions(request.Catalog.Nodes);
            ValidateRequiredCatalogContent(request, sortedDefinitions, preflightErrors);
            if (preflightErrors.Count > 0)
            {
                report.Errors.AddRange(preflightErrors);
                return false;
            }

            List<KeystonePair> keystonePairs = CollectKeystonePairs(sortedDefinitions, request.MaximumBranchDepth);
            var lastAttemptErrors = new List<string>();

            for (int attempt = 1; attempt <= request.MaximumAttempts; attempt++)
            {
                uint attemptSeed = DeriveAttemptSeed(request.Seed, attempt);
                var random = new StableRandom(attemptSeed);
                report.AttemptsUsed = attempt;

                if (!TryGenerateAttempt(
                        request,
                        sortedDefinitions,
                        keystonePairs,
                        ref random,
                        out GeneratedRunGraph candidate,
                        out string attemptError))
                {
                    lastAttemptErrors.Clear();
                    lastAttemptErrors.Add($"Attempt {attempt}: {attemptError}");
                    continue;
                }

                var validationErrors = new List<string>();
                HeartGraphValidator.Validate(candidate, request.Catalog, request, validationErrors);
                if (validationErrors.Count == 0)
                {
                    graph = candidate;
                    report.SuccessfulAttempt = attempt;
                    report.SuccessfulAttemptSeed = attemptSeed;
                    return true;
                }

                lastAttemptErrors.Clear();
                for (int i = 0; i < validationErrors.Count; i++)
                    lastAttemptErrors.Add($"Attempt {attempt}: {validationErrors[i]}");
            }

            report.Errors.Add(
                $"{request.MaximumAttempts} deterministic attempt sonunda valid Castle Heart graph uretilemedi.");
            report.Errors.AddRange(lastAttemptErrors);
            return false;
        }

        private static bool TryGenerateAttempt(
            HeartGraphGenerationRequest request,
            HeartNodeDefinitionSO[] definitions,
            List<KeystonePair> availableKeystonePairs,
            ref StableRandom random,
            out GeneratedRunGraph graph,
            out string error)
        {
            graph = null;
            error = string.Empty;

            var mandatoryByBranch = CreateBranchLists();
            if (!TryAddGuarantees(definitions, mandatoryByBranch, out error))
                return false;
            if (!TryAddRepeatableSinks(request, definitions, mandatoryByBranch, ref random, out error))
                return false;
            if (!TryAddKeystonePairs(request, availableKeystonePairs, mandatoryByBranch, ref random, out error))
                return false;

            var placements = new Dictionary<HeartNodeBranch, Dictionary<int, HeartNodeDefinitionSO>>();
            var branchLengths = new Dictionary<HeartNodeBranch, int>();
            var usedNodeIds = new HashSet<string>(StringComparer.Ordinal);

            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                List<HeartNodeDefinitionSO> mandatory = mandatoryByBranch[branch];
                int requestedLength = random.NextInclusive(
                    request.MinimumBranchDepth,
                    request.MaximumBranchDepth);
                int minimumRequiredLength = mandatory.Count;
                for (int i = 0; i < mandatory.Count; i++)
                    minimumRequiredLength = Math.Max(minimumRequiredLength, mandatory[i].MinimumDepth);

                int branchLength = Math.Max(requestedLength, minimumRequiredLength);
                if (branchLength > request.MaximumBranchDepth)
                {
                    error = $"{branch} zorunlu node'lari max depth {request.MaximumBranchDepth} icine sigmiyor.";
                    return false;
                }

                var branchPlacements = new Dictionary<int, HeartNodeDefinitionSO>();
                if (!TryPlaceMandatoryNodes(mandatory, branchLength, branchPlacements, ref random))
                {
                    error = $"{branch} zorunlu node'lari izinli depth slotlarina yerlestirilemedi.";
                    return false;
                }

                foreach (KeyValuePair<int, HeartNodeDefinitionSO> placement in branchPlacements)
                    usedNodeIds.Add(placement.Value.Id);

                if (!TryFillBranch(
                        branch,
                        branchLength,
                        definitions,
                        branchPlacements,
                        usedNodeIds,
                        request,
                        ref random,
                        out error))
                {
                    return false;
                }

                placements.Add(branch, branchPlacements);
                branchLengths.Add(branch, branchLength);
            }

            graph = BuildGraph(request.Seed, placements, branchLengths);
            AddControlledCrossLinks(graph, request.MaximumCrossLinks, ref random);
            return true;
        }

        private static void ValidateRequest(HeartGraphGenerationRequest request, List<string> errors)
        {
            if (request == null)
            {
                errors.Add("Generation request bos olamaz.");
                return;
            }

            if (request.Catalog == null)
            {
                errors.Add("Heart catalog bos olamaz.");
                return;
            }

            request.Catalog.CollectValidationErrors(errors);

            if (request.MinimumBranchDepth < 1)
                errors.Add("MinimumBranchDepth en az 1 olmalidir.");
            if (request.MaximumBranchDepth < request.MinimumBranchDepth)
                errors.Add("MaximumBranchDepth, MinimumBranchDepth degerinden kucuk olamaz.");
            if (request.MaximumCrossLinks < 0)
                errors.Add("MaximumCrossLinks negatif olamaz.");
            if (request.KeystonePairCount < 0)
                errors.Add("KeystonePairCount negatif olamaz.");
            if (request.MaximumAttempts < 1)
                errors.Add("MaximumAttempts en az 1 olmalidir.");
            if (request.StandardRarityWeight < 1)
                errors.Add("StandardRarityWeight en az 1 olmalidir.");
            if (request.RareRarityWeight < 1)
                errors.Add("RareRarityWeight en az 1 olmalidir.");
        }

        private static void ValidateRequiredCatalogContent(
            HeartGraphGenerationRequest request,
            HeartNodeDefinitionSO[] definitions,
            List<string> errors)
        {
            ValidateGuarantee(
                definitions,
                HeartGraphConstants.RapidGuaranteeTag,
                HeartNodeBranch.Army,
                definition => HasArcherUnlock(definition, ArcherType.Rapid),
                request.MaximumBranchDepth,
                errors);
            ValidateGuarantee(
                definitions,
                HeartGraphConstants.FrostGuaranteeTag,
                HeartNodeBranch.Army,
                definition => HasArcherUnlock(definition, ArcherType.Frost),
                request.MaximumBranchDepth,
                errors);
            ValidateGuarantee(
                definitions,
                HeartGraphConstants.FireballGuaranteeTag,
                HeartNodeBranch.HeartMagic,
                definition => HasEffect(definition, HeartNodeEffectType.UnlockSpellcasting),
                request.MaximumBranchDepth,
                errors);
            ValidateGuarantee(
                definitions,
                HeartGraphConstants.WallGuaranteeTag,
                HeartNodeBranch.Defense,
                definition => HasEffect(definition, HeartNodeEffectType.ModifyWallMaxHpPercent),
                request.MaximumBranchDepth,
                errors);

            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                bool hasSink = false;
                for (int i = 0; i < definitions.Length; i++)
                {
                    HeartNodeDefinitionSO definition = definitions[i];
                    if (definition.Branch == branch
                        && definition.Type == HeartNodeType.Repeatable
                        && HeartNodeTagUtility.HasTag(definition, HeartGraphConstants.RepeatableSinkTag)
                        && HasAllowedDepth(definition, request.MaximumBranchDepth))
                    {
                        hasSink = true;
                        break;
                    }
                }

                if (!hasSink)
                    errors.Add($"{branch} branch'i izinli depth icinde repeatable sink tasimiyor.");
            }

            List<KeystonePair> pairs = CollectKeystonePairs(definitions, request.MaximumBranchDepth);
            if (pairs.Count < request.KeystonePairCount)
            {
                errors.Add(
                    $"Istenen {request.KeystonePairCount} Keystone cifti icin yalniz {pairs.Count} uygun cift var.");
            }
        }

        private static void ValidateGuarantee(
            HeartNodeDefinitionSO[] definitions,
            string tag,
            HeartNodeBranch requiredBranch,
            Predicate<HeartNodeDefinitionSO> effectPredicate,
            int maximumBranchDepth,
            List<string> errors)
        {
            int taggedCount = 0;
            HeartNodeDefinitionSO taggedDefinition = null;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (!HeartNodeTagUtility.HasTag(definitions[i], tag))
                    continue;

                taggedCount++;
                taggedDefinition = definitions[i];
            }

            if (taggedCount != 1)
            {
                errors.Add($"Guarantee tag '{tag}' tam olarak bir node'da olmali; bulunan: {taggedCount}.");
                return;
            }

            if (taggedDefinition.Branch != requiredBranch)
                errors.Add($"Guarantee '{tag}' {requiredBranch} branch'inde olmali.");
            if (!effectPredicate(taggedDefinition))
                errors.Add($"Guarantee '{tag}' beklenen gameplay effect'ini tasimiyor.");
            if (!HasAllowedDepth(taggedDefinition, maximumBranchDepth))
                errors.Add($"Guarantee '{tag}' max depth {maximumBranchDepth} icinde yerlestirilemez.");
        }

        private static bool TryAddGuarantees(
            HeartNodeDefinitionSO[] definitions,
            Dictionary<HeartNodeBranch, List<HeartNodeDefinitionSO>> mandatoryByBranch,
            out string error)
        {
            error = string.Empty;
            string[] guaranteeTags =
            {
                HeartGraphConstants.RapidGuaranteeTag,
                HeartGraphConstants.FrostGuaranteeTag,
                HeartGraphConstants.FireballGuaranteeTag,
                HeartGraphConstants.WallGuaranteeTag
            };

            for (int tagIndex = 0; tagIndex < guaranteeTags.Length; tagIndex++)
            {
                HeartNodeDefinitionSO definition = FindSingleTaggedDefinition(definitions, guaranteeTags[tagIndex]);
                if (definition == null)
                {
                    error = $"Guarantee node bulunamadi: {guaranteeTags[tagIndex]}";
                    return false;
                }

                AddUnique(mandatoryByBranch[definition.Branch], definition);
            }

            return true;
        }

        private static bool TryAddRepeatableSinks(
            HeartGraphGenerationRequest request,
            HeartNodeDefinitionSO[] definitions,
            Dictionary<HeartNodeBranch, List<HeartNodeDefinitionSO>> mandatoryByBranch,
            ref StableRandom random,
            out string error)
        {
            error = string.Empty;
            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                var candidates = new List<HeartNodeDefinitionSO>();
                for (int i = 0; i < definitions.Length; i++)
                {
                    HeartNodeDefinitionSO definition = definitions[i];
                    if (definition.Branch == branch
                        && definition.Type == HeartNodeType.Repeatable
                        && HeartNodeTagUtility.HasTag(definition, HeartGraphConstants.RepeatableSinkTag)
                        && HasAllowedDepth(definition, request.MaximumBranchDepth))
                    {
                        candidates.Add(definition);
                    }
                }

                if (candidates.Count == 0)
                {
                    error = $"{branch} repeatable sink adayi bulunamadi.";
                    return false;
                }

                HeartNodeDefinitionSO selected = candidates[random.NextExclusive(candidates.Count)];
                AddUnique(mandatoryByBranch[branch], selected);
            }

            return true;
        }

        private static bool TryAddKeystonePairs(
            HeartGraphGenerationRequest request,
            List<KeystonePair> availablePairs,
            Dictionary<HeartNodeBranch, List<HeartNodeDefinitionSO>> mandatoryByBranch,
            ref StableRandom random,
            out string error)
        {
            error = string.Empty;
            if (request.KeystonePairCount == 0)
                return true;

            var shuffledPairs = new List<KeystonePair>(availablePairs);
            Shuffle(shuffledPairs, ref random);
            if (shuffledPairs.Count < request.KeystonePairCount)
            {
                error = "Yeterli Keystone cifti yok.";
                return false;
            }

            for (int i = 0; i < request.KeystonePairCount; i++)
            {
                KeystonePair pair = shuffledPairs[i];
                AddUnique(mandatoryByBranch[pair.First.Branch], pair.First);
                AddUnique(mandatoryByBranch[pair.Second.Branch], pair.Second);
            }

            return true;
        }

        private static bool TryPlaceMandatoryNodes(
            List<HeartNodeDefinitionSO> mandatory,
            int branchLength,
            Dictionary<int, HeartNodeDefinitionSO> placements,
            ref StableRandom random)
        {
            var ordered = new List<HeartNodeDefinitionSO>(mandatory);
            ordered.Sort((left, right) =>
            {
                int leftSlots = CountAllowedDepths(left, branchLength);
                int rightSlots = CountAllowedDepths(right, branchLength);
                int slotComparison = leftSlots.CompareTo(rightSlots);
                return slotComparison != 0
                    ? slotComparison
                    : string.CompareOrdinal(left.Id, right.Id);
            });

            return TryPlaceMandatoryNodeAtIndex(ordered, 0, branchLength, placements, ref random);
        }

        private static bool TryPlaceMandatoryNodeAtIndex(
            List<HeartNodeDefinitionSO> ordered,
            int index,
            int branchLength,
            Dictionary<int, HeartNodeDefinitionSO> placements,
            ref StableRandom random)
        {
            if (index >= ordered.Count)
                return true;

            HeartNodeDefinitionSO definition = ordered[index];
            var allowedDepths = new List<int>();
            int firstDepth = Math.Max(1, definition.MinimumDepth);
            int lastDepth = Math.Min(branchLength, definition.MaximumDepth);
            for (int depth = firstDepth; depth <= lastDepth; depth++)
            {
                if (!placements.ContainsKey(depth))
                    allowedDepths.Add(depth);
            }

            Shuffle(allowedDepths, ref random);
            for (int i = 0; i < allowedDepths.Count; i++)
            {
                int depth = allowedDepths[i];
                placements.Add(depth, definition);
                if (TryPlaceMandatoryNodeAtIndex(ordered, index + 1, branchLength, placements, ref random))
                    return true;
                placements.Remove(depth);
            }

            return false;
        }

        private static bool TryFillBranch(
            HeartNodeBranch branch,
            int branchLength,
            HeartNodeDefinitionSO[] definitions,
            Dictionary<int, HeartNodeDefinitionSO> placements,
            HashSet<string> usedNodeIds,
            HeartGraphGenerationRequest request,
            ref StableRandom random,
            out string error)
        {
            error = string.Empty;
            for (int depth = 1; depth <= branchLength; depth++)
            {
                if (placements.ContainsKey(depth))
                    continue;

                var candidates = new List<HeartNodeDefinitionSO>();
                var weights = new List<int>();
                for (int definitionIndex = 0; definitionIndex < definitions.Length; definitionIndex++)
                {
                    HeartNodeDefinitionSO definition = definitions[definitionIndex];
                    if (definition.Branch != branch
                        || definition.Type == HeartNodeType.Keystone
                        || usedNodeIds.Contains(definition.Id)
                        || depth < definition.MinimumDepth
                        || depth > definition.MaximumDepth)
                    {
                        continue;
                    }

                    candidates.Add(definition);
                    weights.Add(definition.Rarity == HeartNodeRarity.Rare
                        ? request.RareRarityWeight
                        : request.StandardRarityWeight);
                }

                if (candidates.Count == 0)
                {
                    error = $"{branch} depth {depth} icin duplicate olmayan uygun filler node yok.";
                    return false;
                }

                int selectedIndex = PickWeightedIndex(weights, ref random);
                HeartNodeDefinitionSO selected = candidates[selectedIndex];
                placements.Add(depth, selected);
                usedNodeIds.Add(selected.Id);
            }

            return true;
        }

        private static GeneratedRunGraph BuildGraph(
            uint runSeed,
            Dictionary<HeartNodeBranch, Dictionary<int, HeartNodeDefinitionSO>> placements,
            Dictionary<HeartNodeBranch, int> branchLengths)
        {
            var graph = new GeneratedRunGraph
            {
                Seed = runSeed,
                RootNodeId = HeartGraphConstants.RootNodeId
            };

            graph.Nodes.Add(new GeneratedHeartNodeState
            {
                NodeId = HeartGraphConstants.RootNodeId,
                Branch = HeartNodeBranch.HeartMagic,
                Depth = 0,
                Visibility = HeartNodeVisibility.Revealed,
                Level = 1,
                LockState = HeartNodeLockState.Available,
                LockedByNodeId = string.Empty
            });

            for (int branchIndex = 0; branchIndex < Branches.Length; branchIndex++)
            {
                HeartNodeBranch branch = Branches[branchIndex];
                int branchLength = branchLengths[branch];
                string previousNodeId = HeartGraphConstants.RootNodeId;
                for (int depth = 1; depth <= branchLength; depth++)
                {
                    HeartNodeDefinitionSO definition = placements[branch][depth];
                    graph.Nodes.Add(new GeneratedHeartNodeState
                    {
                        NodeId = definition.Id,
                        Branch = branch,
                        Depth = depth,
                        Visibility = HeartNodeVisibility.Hidden,
                        Level = 0,
                        LockState = HeartNodeLockState.Available,
                        LockedByNodeId = string.Empty
                    });
                    graph.Edges.Add(new GeneratedHeartEdge
                    {
                        FromNodeId = previousNodeId,
                        ToNodeId = definition.Id
                    });
                    previousNodeId = definition.Id;
                }
            }

            return graph;
        }

        private static void AddControlledCrossLinks(
            GeneratedRunGraph graph,
            int maximumCrossLinks,
            ref StableRandom random)
        {
            if (maximumCrossLinks <= 0)
                return;

            var candidates = new List<CrossLinkCandidate>();
            for (int fromIndex = 1; fromIndex < graph.Nodes.Count; fromIndex++)
            {
                GeneratedHeartNodeState from = graph.Nodes[fromIndex];
                for (int toIndex = 1; toIndex < graph.Nodes.Count; toIndex++)
                {
                    GeneratedHeartNodeState to = graph.Nodes[toIndex];
                    if (from.Branch != to.Branch && to.Depth == from.Depth + 1)
                    {
                        candidates.Add(new CrossLinkCandidate(from.NodeId, to.NodeId));
                    }
                }
            }

            candidates.Sort((left, right) =>
            {
                int fromComparison = string.CompareOrdinal(left.FromNodeId, right.FromNodeId);
                return fromComparison != 0
                    ? fromComparison
                    : string.CompareOrdinal(left.ToNodeId, right.ToNodeId);
            });
            Shuffle(candidates, ref random);

            var usedFrom = new HashSet<string>(StringComparer.Ordinal);
            var usedTo = new HashSet<string>(StringComparer.Ordinal);
            int added = 0;
            for (int i = 0; i < candidates.Count && added < maximumCrossLinks; i++)
            {
                CrossLinkCandidate candidate = candidates[i];
                if (!usedFrom.Add(candidate.FromNodeId) || !usedTo.Add(candidate.ToNodeId))
                    continue;

                graph.Edges.Add(new GeneratedHeartEdge
                {
                    FromNodeId = candidate.FromNodeId,
                    ToNodeId = candidate.ToNodeId
                });
                added++;
            }
        }

        private static HeartNodeDefinitionSO[] CopyAndSortDefinitions(HeartNodeDefinitionSO[] source)
        {
            HeartNodeDefinitionSO[] copy = source == null
                ? Array.Empty<HeartNodeDefinitionSO>()
                : (HeartNodeDefinitionSO[])source.Clone();
            Array.Sort(copy, (left, right) => string.CompareOrdinal(left?.Id, right?.Id));
            return copy;
        }

        private static Dictionary<HeartNodeBranch, List<HeartNodeDefinitionSO>> CreateBranchLists()
        {
            var result = new Dictionary<HeartNodeBranch, List<HeartNodeDefinitionSO>>();
            for (int i = 0; i < Branches.Length; i++)
                result.Add(Branches[i], new List<HeartNodeDefinitionSO>());
            return result;
        }

        private static HeartNodeDefinitionSO FindSingleTaggedDefinition(
            HeartNodeDefinitionSO[] definitions,
            string tag)
        {
            HeartNodeDefinitionSO result = null;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (!HeartNodeTagUtility.HasTag(definitions[i], tag))
                    continue;
                if (result != null)
                    return null;
                result = definitions[i];
            }

            return result;
        }

        private static List<KeystonePair> CollectKeystonePairs(
            HeartNodeDefinitionSO[] definitions,
            int maximumBranchDepth)
        {
            var definitionsById = new Dictionary<string, HeartNodeDefinitionSO>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && !string.IsNullOrWhiteSpace(definitions[i].Id))
                    definitionsById[definitions[i].Id] = definitions[i];
            }

            var pairs = new List<KeystonePair>();
            for (int i = 0; i < definitions.Length; i++)
            {
                HeartNodeDefinitionSO first = definitions[i];
                if (first == null
                    || first.Type != HeartNodeType.Keystone
                    || !HasAllowedDepth(first, maximumBranchDepth))
                {
                    continue;
                }

                string[] conflicts = first.ConflictNodeIds ?? Array.Empty<string>();
                if (conflicts.Length != 1
                    || string.CompareOrdinal(first.Id, conflicts[0]) >= 0
                    || !definitionsById.TryGetValue(conflicts[0], out HeartNodeDefinitionSO second)
                    || second.Type != HeartNodeType.Keystone
                    || !HasAllowedDepth(second, maximumBranchDepth))
                {
                    continue;
                }

                string[] secondConflicts = second.ConflictNodeIds ?? Array.Empty<string>();
                if (secondConflicts.Length == 1
                    && string.Equals(secondConflicts[0], first.Id, StringComparison.Ordinal))
                {
                    pairs.Add(new KeystonePair(first, second));
                }
            }

            pairs.Sort((left, right) => string.CompareOrdinal(left.First.Id, right.First.Id));
            return pairs;
        }

        private static bool HasArcherUnlock(HeartNodeDefinitionSO definition, ArcherType archerType)
        {
            HeartNodeEffect[] effects = definition.Effects ?? Array.Empty<HeartNodeEffect>();
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].Type == HeartNodeEffectType.UnlockArcherType
                    && effects[i].ArcherType == archerType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEffect(HeartNodeDefinitionSO definition, HeartNodeEffectType effectType)
        {
            HeartNodeEffect[] effects = definition.Effects ?? Array.Empty<HeartNodeEffect>();
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].Type == effectType)
                    return true;
            }

            return false;
        }

        private static bool HasAllowedDepth(HeartNodeDefinitionSO definition, int maximumBranchDepth)
        {
            return definition.MaximumDepth >= 1 && definition.MinimumDepth <= maximumBranchDepth;
        }

        private static int CountAllowedDepths(HeartNodeDefinitionSO definition, int branchLength)
        {
            int first = Math.Max(1, definition.MinimumDepth);
            int last = Math.Min(branchLength, definition.MaximumDepth);
            return Math.Max(0, last - first + 1);
        }

        private static void AddUnique(List<HeartNodeDefinitionSO> definitions, HeartNodeDefinitionSO definition)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(definitions[i].Id, definition.Id, StringComparison.Ordinal))
                    return;
            }

            definitions.Add(definition);
        }

        private static int PickWeightedIndex(List<int> weights, ref StableRandom random)
        {
            int totalWeight = 0;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            int roll = random.NextExclusive(totalWeight);
            for (int i = 0; i < weights.Count; i++)
            {
                if (roll < weights[i])
                    return i;
                roll -= weights[i];
            }

            return weights.Count - 1;
        }

        private static uint DeriveAttemptSeed(uint runSeed, int attempt)
        {
            uint value = runSeed ^ (uint)attempt * 0x9E3779B9u;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value == 0u ? 0xA341316Cu : value;
        }

        private static void Shuffle<T>(List<T> values, ref StableRandom random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.NextExclusive(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        private readonly struct KeystonePair
        {
            public readonly HeartNodeDefinitionSO First;
            public readonly HeartNodeDefinitionSO Second;

            public KeystonePair(HeartNodeDefinitionSO first, HeartNodeDefinitionSO second)
            {
                First = first;
                Second = second;
            }
        }

        private readonly struct CrossLinkCandidate
        {
            public readonly string FromNodeId;
            public readonly string ToNodeId;

            public CrossLinkCandidate(string fromNodeId, string toNodeId)
            {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
            }
        }

        private struct StableRandom
        {
            private uint _state;

            public StableRandom(uint seed)
            {
                _state = seed == 0u ? 0xA341316Cu : seed;
            }

            public int NextExclusive(int maximumExclusive)
            {
                if (maximumExclusive <= 0)
                    throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
                return (int)(NextUInt() % (uint)maximumExclusive);
            }

            public int NextInclusive(int minimumInclusive, int maximumInclusive)
            {
                if (maximumInclusive < minimumInclusive)
                    throw new ArgumentOutOfRangeException(nameof(maximumInclusive));
                return minimumInclusive + NextExclusive(maximumInclusive - minimumInclusive + 1);
            }

            private uint NextUInt()
            {
                uint value = _state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                _state = value;
                return value;
            }
        }
    }
}
