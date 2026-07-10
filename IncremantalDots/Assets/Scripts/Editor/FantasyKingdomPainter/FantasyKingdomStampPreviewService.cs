#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    internal sealed class FantasyKingdomPreviewReport
    {
        public int StampTileCount;
        public int UniquePreviewCellCount;
        public int ExistingOverlapCellCount;
        public int BlockingOverlapCellCount;
        public int ProtectedOverlapCellCount;
        public int VillageMarkerConflictCount;
        public int RestrictedZoneCellCount;
        public int GroundSupportCellCount;
        public int MissingGroundSupportCellCount;
        public readonly List<string> LayerOverlapDetails = new List<string>();
        public readonly List<string> VillageMarkerConflictNames = new List<string>();
        public string PlacementRuleDetail;

        public bool HasProtectedConflict =>
            ProtectedOverlapCellCount > 0 ||
            VillageMarkerConflictCount > 0 ||
            RestrictedZoneCellCount > 0 ||
            MissingGroundSupportCellCount > 0;

        public bool HasBlockingConflict => BlockingOverlapCellCount > 0;

        public string BuildSummary()
        {
            string summary = string.Format(
                "Preview: {0} tile / {1} unique cell\n" +
                "Existing overlap: {2}  Blocking: {3}  Protected: {4}  " +
                "Marker conflict: {5}  Restricted zone: {6}\n" +
                "Ground support: {7}/{8}  Missing support: {9}",
                StampTileCount,
                UniquePreviewCellCount,
                ExistingOverlapCellCount,
                BlockingOverlapCellCount,
                ProtectedOverlapCellCount,
                VillageMarkerConflictCount,
                RestrictedZoneCellCount,
                GroundSupportCellCount,
                UniquePreviewCellCount,
                MissingGroundSupportCellCount);

            if (LayerOverlapDetails.Count > 0)
                summary += "\n" + string.Join("  |  ", LayerOverlapDetails.ToArray());
            if (VillageMarkerConflictNames.Count > 0)
                summary += "\nMarkers: " + string.Join(", ", VillageMarkerConflictNames.ToArray());
            if (!string.IsNullOrEmpty(PlacementRuleDetail))
                summary += "\n" + PlacementRuleDetail;
            return summary;
        }
    }

    /// <summary>
    /// Stamp'i gercek target tilemap'lere dokunmadan, DontSave preview katmanlarina yazar.
    /// </summary>
    internal static class FantasyKingdomStampPreviewService
    {
        public const string PreviewRootName = "__FKPreviewRoot";

        private static readonly HashSet<string> NonBlockingLayerNames = new HashSet<string>(
            new[] { "Grass", "Ground", "GroundDetail" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> ProtectedLayerNames = new HashSet<string>(
            new[] { "outside", "outside0", "outside2" },
            StringComparer.OrdinalIgnoreCase);

        public static FantasyKingdomPreviewReport CreateOrUpdatePreview(
            FantasyKingdomStructureStamp stamp,
            Grid targetGrid,
            Vector3Int targetOrigin,
            Color previewTint,
            string previewSortingLayer,
            int previewBaseSortingOrder)
        {
            if (stamp == null)
                throw new ArgumentNullException(nameof(stamp));
            if (targetGrid == null)
                throw new ArgumentNullException(nameof(targetGrid));
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stamp preview Play Mode disinda calistirilmalidir.");
            if (!targetGrid.gameObject.scene.IsValid() || !targetGrid.gameObject.scene.isLoaded)
                throw new InvalidOperationException("Target Grid yuklu bir sahnede olmalidir.");

            ValidateGridCompatibility(stamp, targetGrid);

            Scene targetScene = targetGrid.gameObject.scene;

            try
            {
                ClearPreviewInternal(targetGrid);
                FantasyKingdomPreviewReport report = AnalyzePlacement(stamp, targetGrid, targetOrigin);

                var root = new GameObject(PreviewRootName)
                {
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                };
                SceneManager.MoveGameObjectToScene(root, targetScene);
                root.transform.SetParent(targetGrid.transform, false);
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                string resolvedSortingLayer = ResolveSortingLayer(previewSortingLayer);
                IReadOnlyList<FantasyKingdomStampLayer> layers = stamp.Layers;
                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer sourceLayer = layers[layerIndex];
                    var layerObject = new GameObject(
                        string.Format("__FKPreview_{0:00}_{1}", layerIndex, sourceLayer.SourceName))
                    {
                        hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild,
                        layer = targetGrid.gameObject.layer
                    };
                    layerObject.transform.SetParent(root.transform, false);

                    var tilemap = layerObject.AddComponent<Tilemap>();
                    var renderer = layerObject.AddComponent<TilemapRenderer>();
                    tilemap.tileAnchor = sourceLayer.TileAnchor;
                    tilemap.color = Multiply(sourceLayer.LayerColor, previewTint);
                    tilemap.orientation = sourceLayer.Orientation;
                    tilemap.orientationMatrix = sourceLayer.OrientationMatrix;

                    renderer.mode = sourceLayer.RendererMode;
                    renderer.sortOrder = sourceLayer.SortOrder;
                    renderer.sortingLayerName = resolvedSortingLayer;
                    renderer.sortingOrder = previewBaseSortingOrder +
                                            sourceLayer.SortingOrder * 10 +
                                            layerIndex;

                    IReadOnlyList<FantasyKingdomStampCell> cells = sourceLayer.Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        FantasyKingdomStampCell sourceCell = cells[cellIndex];
                        Vector3Int targetCell = targetOrigin + sourceCell.LocalPosition;
                        tilemap.SetTile(targetCell, sourceCell.Tile);
                        tilemap.SetTileFlags(targetCell, TileFlags.None);
                        tilemap.SetTransformMatrix(targetCell, sourceCell.TransformMatrix);
                        tilemap.SetColor(targetCell, sourceCell.Color);
                        tilemap.SetTileFlags(targetCell, sourceCell.Flags);
                    }

                    tilemap.CompressBounds();
                }

                return report;
            }
            catch
            {
                ClearPreviewInternal(targetGrid);
                throw;
            }
        }

        public static void ClearPreview(Grid targetGrid)
        {
            if (targetGrid == null || !targetGrid.gameObject.scene.IsValid())
                return;

            ClearPreviewInternal(targetGrid);
        }

        public static Grid FindDefaultTargetGrid(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (!string.Equals(roots[i].name, "Grid", StringComparison.Ordinal))
                    continue;
                Grid directGrid = roots[i].GetComponent<Grid>();
                if (directGrid != null)
                    return directGrid;
            }

            Grid best = null;
            int bestTilemapCount = -1;
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Grid[] grids = roots[rootIndex].GetComponentsInChildren<Grid>(true);
                for (int gridIndex = 0; gridIndex < grids.Length; gridIndex++)
                {
                    int tilemapCount = grids[gridIndex].GetComponentsInChildren<Tilemap>(true).Length;
                    if (tilemapCount <= bestTilemapCount)
                        continue;
                    best = grids[gridIndex];
                    bestTilemapCount = tilemapCount;
                }
            }
            return best;
        }

        public static FantasyKingdomPreviewReport AnalyzePlacement(
            FantasyKingdomStructureStamp stamp,
            Grid targetGrid,
            Vector3Int targetOrigin)
        {
            if (stamp == null)
                throw new ArgumentNullException(nameof(stamp));
            if (targetGrid == null)
                throw new ArgumentNullException(nameof(targetGrid));

            ValidateGridCompatibility(stamp, targetGrid);

            var report = new FantasyKingdomPreviewReport
            {
                StampTileCount = stamp.TotalTileCount
            };

            var previewCells = new HashSet<Vector3Int>();
            IReadOnlyList<FantasyKingdomStampLayer> layers = stamp.Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                IReadOnlyList<FantasyKingdomStampCell> cells = layers[layerIndex].Cells;
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    previewCells.Add(targetOrigin + cells[cellIndex].LocalPosition);
            }
            report.UniquePreviewCellCount = previewCells.Count;

            Tilemap[] targetMaps = targetGrid.GetComponentsInChildren<Tilemap>(true)
                .Where(map => !IsInsidePreviewRoot(map.transform))
                .ToArray();

            var existingOverlapCells = new HashSet<Vector3Int>();
            var blockingOverlapCells = new HashSet<Vector3Int>();
            var protectedOverlapCells = new HashSet<Vector3Int>();
            var groundSupportCells = new HashSet<Vector3Int>();

            for (int mapIndex = 0; mapIndex < targetMaps.Length; mapIndex++)
            {
                Tilemap targetMap = targetMaps[mapIndex];
                int overlapCount = 0;
                foreach (Vector3Int cell in previewCells)
                {
                    if (!targetMap.HasTile(cell))
                        continue;
                    overlapCount++;
                    existingOverlapCells.Add(cell);
                    if (string.Equals(targetMap.name, "Grass", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(targetMap.name, "Ground", StringComparison.OrdinalIgnoreCase))
                    {
                        groundSupportCells.Add(cell);
                    }

                    if (ProtectedLayerNames.Contains(targetMap.name))
                        protectedOverlapCells.Add(cell);
                    else if (!NonBlockingLayerNames.Contains(targetMap.name))
                        blockingOverlapCells.Add(cell);
                }

                if (overlapCount > 0)
                    report.LayerOverlapDetails.Add(targetMap.name + ":" + overlapCount);
            }

            report.ExistingOverlapCellCount = existingOverlapCells.Count;
            report.BlockingOverlapCellCount = blockingOverlapCells.Count;
            report.ProtectedOverlapCellCount = protectedOverlapCells.Count;
            report.GroundSupportCellCount = groundSupportCells.Count;
            bool requiresGroundSupport = stamp.Purpose == FantasyKingdomStampPurpose.Structure ||
                                         stamp.Purpose == FantasyKingdomStampPurpose.ResourceSite;
            report.MissingGroundSupportCellCount = requiresGroundSupport
                ? Mathf.Max(0, previewCells.Count - groundSupportCells.Count)
                : 0;
            report.VillageMarkerConflictCount = CollectVillageMarkerConflicts(
                targetGrid,
                previewCells,
                targetGrid.gameObject.scene,
                report.VillageMarkerConflictNames);
            report.RestrictedZoneCellCount = CountRestrictedZoneCells(
                stamp,
                targetGrid,
                previewCells,
                targetMaps,
                out string placementRuleDetail);
            if (report.MissingGroundSupportCellCount > 0)
            {
                string groundDetail = string.Format(
                    "{0} structure cell mevcut Grass/Ground tabani disinda kaliyor.",
                    report.MissingGroundSupportCellCount);
                placementRuleDetail = string.IsNullOrEmpty(placementRuleDetail)
                    ? groundDetail
                    : placementRuleDetail + " " + groundDetail;
            }
            report.PlacementRuleDetail = placementRuleDetail;
            return report;
        }

        private static int CollectVillageMarkerConflicts(
            Grid grid,
            HashSet<Vector3Int> previewCells,
            Scene scene,
            List<string> conflictNames)
        {
            GameObject markersRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(root.name, "VillageMarkers", StringComparison.Ordinal));
            if (markersRoot == null)
                return 0;

            int conflicts = 0;
            for (int childIndex = 0; childIndex < markersRoot.transform.childCount; childIndex++)
            {
                Transform marker = markersRoot.transform.GetChild(childIndex);
                Vector3Int markerCell = grid.WorldToCell(marker.position);
                bool markerConflict = false;
                for (int dx = -1; dx <= 1 && !markerConflict; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (!previewCells.Contains(new Vector3Int(markerCell.x + dx, markerCell.y + dy, 0)))
                            continue;
                        markerConflict = true;
                        break;
                    }
                }
                if (markerConflict)
                {
                    conflicts++;
                    conflictNames.Add(marker.name);
                }
            }
            return conflicts;
        }

        private static int CountRestrictedZoneCells(
            FantasyKingdomStructureStamp stamp,
            Grid grid,
            HashSet<Vector3Int> previewCells,
            Tilemap[] targetMaps,
            out string detail)
        {
            detail = null;
            bool leftSidePurpose = stamp.Purpose == FantasyKingdomStampPurpose.Structure ||
                                   stamp.Purpose == FantasyKingdomStampPurpose.ResourceSite;
            if (!leftSidePurpose || !TryFindWallBoundaryX(grid, targetMaps, out float wallBoundaryX))
                return 0;

            int conflicts = 0;
            foreach (Vector3Int cell in previewCells)
            {
                if (grid.GetCellCenterWorld(cell).x > wallBoundaryX + 0.001f)
                    conflicts++;
            }

            if (conflicts > 0)
            {
                detail = string.Format(
                    "{0} stamp wall line x={1:0.##} sagina tasiyor ({2} cell). " +
                    "Sag cephe sanati icin BattlefieldDecoration purpose kullan.",
                    stamp.Purpose,
                    wallBoundaryX,
                    conflicts);
            }
            return conflicts;
        }

        private static bool TryFindWallBoundaryX(
            Grid grid,
            Tilemap[] targetMaps,
            out float wallBoundaryX)
        {
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;

            for (int mapIndex = 0; mapIndex < targetMaps.Length; mapIndex++)
            {
                Tilemap tilemap = targetMaps[mapIndex];
                if (!ProtectedLayerNames.Contains(tilemap.name))
                    continue;

                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell))
                        continue;
                    float worldX = grid.GetCellCenterWorld(cell).x;
                    minX = Mathf.Min(minX, worldX);
                    maxX = Mathf.Max(maxX, worldX);
                }
            }

            if (float.IsInfinity(minX) || float.IsInfinity(maxX))
            {
                wallBoundaryX = 0f;
                return false;
            }

            wallBoundaryX = (minX + maxX) * 0.5f;
            return true;
        }

        public static void ValidateGridCompatibility(
            FantasyKingdomStructureStamp stamp,
            Grid targetGrid)
        {
            if (stamp.SourceCellLayout != targetGrid.cellLayout)
                throw new InvalidOperationException(
                    "Grid CellLayout uyusmuyor. Stamp=" + stamp.SourceCellLayout +
                    " Target=" + targetGrid.cellLayout);
            if (stamp.SourceCellSwizzle != targetGrid.cellSwizzle)
                throw new InvalidOperationException(
                    "Grid CellSwizzle uyusmuyor. Stamp=" + stamp.SourceCellSwizzle +
                    " Target=" + targetGrid.cellSwizzle);
            if ((stamp.SourceCellSize - targetGrid.cellSize).sqrMagnitude > 0.0001f)
                throw new InvalidOperationException(
                    "Grid CellSize uyusmuyor. Stamp=" + stamp.SourceCellSize +
                    " Target=" + targetGrid.cellSize);
            if ((stamp.SourceCellGap - targetGrid.cellGap).sqrMagnitude > 0.0001f)
                throw new InvalidOperationException(
                    "Grid CellGap uyusmuyor. Stamp=" + stamp.SourceCellGap +
                    " Target=" + targetGrid.cellGap);
        }

        private static void ClearPreviewInternal(Grid targetGrid)
        {
            Transform previewRoot = targetGrid.transform.Find(PreviewRootName);
            if (previewRoot != null)
            {
                Transform selectedTransform = Selection.activeTransform;
                if (selectedTransform == previewRoot ||
                    (selectedTransform != null && selectedTransform.IsChildOf(previewRoot)))
                {
                    Selection.activeObject = null;
                }
                UnityEngine.Object.DestroyImmediate(previewRoot.gameObject);
            }
        }

        private static bool IsInsidePreviewRoot(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, PreviewRootName, StringComparison.Ordinal))
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static string ResolveSortingLayer(string requested)
        {
            if (!string.IsNullOrEmpty(requested) &&
                SortingLayer.layers.Any(layer => string.Equals(layer.name, requested, StringComparison.Ordinal)))
                return requested;
            return "Default";
        }

        private static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }
    }
}
#endif
