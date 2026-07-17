using System;
using System.Collections.Generic;

namespace DeadWalls
{
    [Serializable]
    public sealed class HeartGraphRuntimeSettings
    {
        public int MinimumBranchDepth = 4;
        public int MaximumBranchDepth = 5;
        public int MaximumCrossLinks = 2;
        public int KeystonePairCount = 1;
        public int MaximumAttempts = 8;
        public int StandardRarityWeight = 4;
        public int RareRarityWeight = 1;

        public HeartGraphGenerationRequest CreateRequest(HeartNodeCatalogSO catalog, uint seed)
        {
            return new HeartGraphGenerationRequest
            {
                Catalog = catalog,
                Seed = seed,
                MinimumBranchDepth = MinimumBranchDepth,
                MaximumBranchDepth = MaximumBranchDepth,
                MaximumCrossLinks = MaximumCrossLinks,
                KeystonePairCount = KeystonePairCount,
                MaximumAttempts = MaximumAttempts,
                StandardRarityWeight = StandardRarityWeight,
                RareRarityWeight = RareRarityWeight
            };
        }

        public HeartGraphRuntimeSettings Clone()
        {
            return new HeartGraphRuntimeSettings
            {
                MinimumBranchDepth = MinimumBranchDepth,
                MaximumBranchDepth = MaximumBranchDepth,
                MaximumCrossLinks = MaximumCrossLinks,
                KeystonePairCount = KeystonePairCount,
                MaximumAttempts = MaximumAttempts,
                StandardRarityWeight = StandardRarityWeight,
                RareRarityWeight = RareRarityWeight
            };
        }
    }

    /// <summary>
    /// Difficulty Tuner'in internal/hidden graph kimliklerini acmadan okuyabildigi aggregate Heart state'i.
    /// Bu veri presentation telemetrisidir; graph veya purchase state owner'i degildir.
    /// </summary>
    public readonly struct HeartRuntimeTuningTelemetry
    {
        public readonly bool HasCatalog;
        public readonly bool RuntimeAttempted;
        public readonly bool RuntimeReady;
        public readonly string RuntimeError;
        public readonly long GraveEssence;
        public readonly double MetaGainPercent;
        public readonly double MetaGainAccumulator;
        public readonly int GraphVersion;
        public readonly int CatalogVersion;
        public readonly uint Seed;
        public readonly int NodeCount;
        public readonly int EdgeCount;
        public readonly int RevealedNodeCount;
        public readonly int PurchasedNodeCount;
        public readonly int LockedNodeCount;

        public HeartRuntimeTuningTelemetry(
            bool hasCatalog,
            bool runtimeAttempted,
            bool runtimeReady,
            string runtimeError,
            long graveEssence,
            double metaGainPercent,
            double metaGainAccumulator,
            int graphVersion,
            int catalogVersion,
            uint seed,
            int nodeCount,
            int edgeCount,
            int revealedNodeCount,
            int purchasedNodeCount,
            int lockedNodeCount)
        {
            HasCatalog = hasCatalog;
            RuntimeAttempted = runtimeAttempted;
            RuntimeReady = runtimeReady;
            RuntimeError = runtimeError ?? string.Empty;
            GraveEssence = graveEssence;
            MetaGainPercent = metaGainPercent;
            MetaGainAccumulator = metaGainAccumulator;
            GraphVersion = graphVersion;
            CatalogVersion = catalogVersion;
            Seed = seed;
            NodeCount = nodeCount;
            EdgeCount = edgeCount;
            RevealedNodeCount = revealedNodeCount;
            PurchasedNodeCount = purchasedNodeCount;
            LockedNodeCount = lockedNodeCount;
        }
    }

    public interface IHeartScreenRuntime
    {
        long GraveEssenceAmount { get; }
        string HeartRuntimeError { get; }
        bool IsHeartRuntimeReady { get; }

        bool TryBuildHeartPresentation(
            out HeartGraphPresentation presentation,
            out IReadOnlyList<string> errors);

        HeartPurchaseEvaluation EvaluateHeartPurchase(
            string nodeId,
            HeartPurchaseQuantity quantity);

        HeartPurchaseResult TryPurchaseHeartNode(
            string nodeId,
            HeartPurchaseQuantity quantity);
    }
}
