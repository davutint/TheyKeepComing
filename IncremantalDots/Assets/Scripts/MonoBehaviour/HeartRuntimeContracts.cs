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
