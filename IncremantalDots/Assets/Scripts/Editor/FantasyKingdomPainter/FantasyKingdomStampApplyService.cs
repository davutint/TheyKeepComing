#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    internal sealed class FantasyKingdomApplyReport
    {
        public FantasyKingdomPreviewReport Preflight;
        public int AppliedTileCount;
        public int CreatedLayerCount;
        public int ReusedLayerCount;
        public string ManagedRootPath;
        public readonly List<string> LayerDetails = new List<string>();

        public string BuildSummary()
        {
            string summary = string.Format(
                "Applied: {0} tile  Created layers: {1}  Reused layers: {2}\nRoot: {3}",
                AppliedTileCount,
                CreatedLayerCount,
                ReusedLayerCount,
                ManagedRootPath);
            if (LayerDetails.Count > 0)
                summary += "\n" + string.Join("  |  ", LayerDetails.ToArray());
            return summary;
        }
    }

    /// <summary>
    /// Onaylanmis stamp'i yalniz tool-owned kalici katmanlara, tek Undo grubu icinde yazar.
    /// Mevcut elle boyanmis tilemap'leri silmez veya yeniden yapilandirmaz.
    /// </summary>
    internal static class FantasyKingdomStampApplyService
    {
        public const string StructureRootName = "FK_PaintedStructures";
        public const string BattlefieldRootName = "FK_PaintedBattlefield";
        private const string ManagedLayerPrefix = "FK_";

        private sealed class LayerRoute
        {
            public string TargetName;
            public string SortingLayer;
            public int SortingOrder;
        }

        public static FantasyKingdomApplyReport ApplySafely(
            FantasyKingdomStructureStamp stamp,
            Grid targetGrid,
            Vector3Int targetOrigin)
        {
            if (stamp == null)
                throw new ArgumentNullException(nameof(stamp));
            if (targetGrid == null)
                throw new ArgumentNullException(nameof(targetGrid));
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Safe Apply Play Mode disinda calistirilmalidir.");
            if (!targetGrid.gameObject.scene.IsValid() || !targetGrid.gameObject.scene.isLoaded)
                throw new InvalidOperationException("Target Grid yuklu bir sahnede olmalidir.");
            if (stamp.TotalTileCount <= 0)
                throw new InvalidOperationException("Bos stamp uygulanamaz.");

            ValidateTargetSceneContracts(targetGrid);
            FantasyKingdomStampPreviewService.ValidateGridCompatibility(stamp, targetGrid);
            FantasyKingdomPreviewReport preflight =
                FantasyKingdomStampPreviewService.AnalyzePlacement(stamp, targetGrid, targetOrigin);
            ValidatePreflight(preflight);

            IReadOnlyList<FantasyKingdomStampLayer> sourceLayers = stamp.Layers;
            var routes = new List<LayerRoute>(sourceLayers.Count);
            var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int layerIndex = 0; layerIndex < sourceLayers.Count; layerIndex++)
            {
                LayerRoute route = ResolveRoute(sourceLayers[layerIndex]);
                if (!routeNames.Add(route.TargetName))
                    throw new InvalidOperationException(
                        "Iki source layer ayni managed layer adina donusuyor: " + route.TargetName);
                EnsureSortingLayerExists(route.SortingLayer);
                routes.Add(route);
            }

            string undoName = "Apply Fantasy Kingdom Stamp: " + stamp.name;
            string managedRootName = ResolveManagedRootName(stamp.Purpose);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            try
            {
                Transform managedRoot = GetOrCreateManagedRoot(
                    targetGrid,
                    managedRootName,
                    undoName);

                var report = new FantasyKingdomApplyReport
                {
                    Preflight = preflight,
                    AppliedTileCount = stamp.TotalTileCount,
                    ManagedRootPath = targetGrid.name + "/" + managedRootName
                };

                for (int layerIndex = 0; layerIndex < sourceLayers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer sourceLayer = sourceLayers[layerIndex];
                    LayerRoute route = routes[layerIndex];
                    bool createdLayer;
                    Tilemap targetTilemap = GetOrCreateManagedLayer(
                        managedRoot,
                        targetGrid,
                        sourceLayer,
                        route,
                        undoName,
                        out createdLayer);

                    if (createdLayer)
                        report.CreatedLayerCount++;
                    else
                        report.ReusedLayerCount++;

                    Undo.RegisterCompleteObjectUndo(targetTilemap, undoName);
                    IReadOnlyList<FantasyKingdomStampCell> cells = sourceLayer.Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        FantasyKingdomStampCell sourceCell = cells[cellIndex];
                        Vector3Int targetCell = targetOrigin + sourceCell.LocalPosition;
                        if (targetTilemap.HasTile(targetCell))
                        {
                            throw new InvalidOperationException(
                                route.TargetName + " zaten " + targetCell + " hucresinde tile tasiyor.");
                        }

                        targetTilemap.SetTile(targetCell, sourceCell.Tile);
                        targetTilemap.SetTileFlags(targetCell, TileFlags.None);
                        targetTilemap.SetTransformMatrix(targetCell, sourceCell.TransformMatrix);
                        targetTilemap.SetColor(targetCell, sourceCell.Color);
                        targetTilemap.SetTileFlags(targetCell, sourceCell.Flags);
                    }

                    targetTilemap.CompressBounds();
                    EditorUtility.SetDirty(targetTilemap);
                    report.LayerDetails.Add(
                        sourceLayer.SourceName + "->" + route.TargetName + ":" + cells.Count);
                }

                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(targetGrid.gameObject.scene);
                FantasyKingdomStampPreviewService.ClearPreview(targetGrid);
                return report;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static void ValidatePreflight(FantasyKingdomPreviewReport preflight)
        {
            if (preflight.HasProtectedConflict)
            {
                string markerNames = preflight.VillageMarkerConflictNames.Count > 0
                    ? " Markers: " + string.Join(", ", preflight.VillageMarkerConflictNames.ToArray())
                    : string.Empty;
                throw new InvalidOperationException(
                    "Safe Apply protected alan veya yerlesim kurali ihlali nedeniyle durduruldu." +
                    markerNames + " " + preflight.PlacementRuleDetail);
            }

            if (preflight.HasBlockingConflict)
            {
                throw new InvalidOperationException(
                    "Safe Apply mevcut yapisal tile'larla " +
                    preflight.BlockingOverlapCellCount +
                    " hucrede cakistigi icin durduruldu.");
            }
        }

        private static Transform GetOrCreateManagedRoot(
            Grid targetGrid,
            string managedRootName,
            string undoName)
        {
            Transform existing = targetGrid.transform.Find(managedRootName);
            if (existing != null)
            {
                ValidateIdentityTransform(existing, managedRootName);
                ValidateManagedRoot(existing);
                return existing;
            }

            var rootObject = new GameObject(managedRootName)
            {
                layer = targetGrid.gameObject.layer
            };
            Undo.RegisterCreatedObjectUndo(rootObject, undoName);
            if (rootObject.scene != targetGrid.gameObject.scene)
                Undo.MoveGameObjectToScene(rootObject, targetGrid.gameObject.scene, undoName);
            Undo.SetTransformParent(rootObject.transform, targetGrid.transform, undoName);
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;
            return rootObject.transform;
        }

        private static Tilemap GetOrCreateManagedLayer(
            Transform managedRoot,
            Grid targetGrid,
            FantasyKingdomStampLayer sourceLayer,
            LayerRoute route,
            string undoName,
            out bool created)
        {
            Transform existing = managedRoot.Find(route.TargetName);
            Tilemap tilemap;
            TilemapRenderer renderer;

            if (existing == null)
            {
                var layerObject = new GameObject(route.TargetName)
                {
                    layer = targetGrid.gameObject.layer
                };
                Undo.RegisterCreatedObjectUndo(layerObject, undoName);
                if (layerObject.scene != targetGrid.gameObject.scene)
                    Undo.MoveGameObjectToScene(layerObject, targetGrid.gameObject.scene, undoName);
                Undo.SetTransformParent(layerObject.transform, managedRoot, undoName);
                layerObject.transform.localPosition = Vector3.zero;
                layerObject.transform.localRotation = Quaternion.identity;
                layerObject.transform.localScale = Vector3.one;
                tilemap = Undo.AddComponent<Tilemap>(layerObject);
                renderer = Undo.AddComponent<TilemapRenderer>(layerObject);
                created = true;
            }
            else
            {
                ValidateIdentityTransform(existing, route.TargetName);
                tilemap = existing.GetComponent<Tilemap>();
                renderer = existing.GetComponent<TilemapRenderer>();
                if (tilemap == null || renderer == null)
                {
                    throw new InvalidOperationException(
                        route.TargetName + " tool-owned Tilemap/TilemapRenderer kontratini karsilamiyor.");
                }

                ValidateManagedLayerMetadata(tilemap, renderer, sourceLayer, route);
                created = false;
            }

            if (created)
            {
                tilemap.tileAnchor = sourceLayer.TileAnchor;
                tilemap.color = sourceLayer.LayerColor;
                tilemap.orientation = sourceLayer.Orientation;
                tilemap.orientationMatrix = sourceLayer.OrientationMatrix;
                renderer.mode = sourceLayer.RendererMode;
                renderer.sortOrder = sourceLayer.SortOrder;
                renderer.sortingLayerName = route.SortingLayer;
                renderer.sortingOrder = route.SortingOrder;
                EditorUtility.SetDirty(renderer);
            }
            return tilemap;
        }

        private static LayerRoute ResolveRoute(FantasyKingdomStampLayer sourceLayer)
        {
            string normalized = sourceLayer.SourceName.ToLowerInvariant();
            string sortingLayer;
            int sortingOrder;

            if (normalized.Contains("lowershadow"))
            {
                sortingLayer = "Ground";
                sortingOrder = 3;
            }
            else if (normalized.Contains("ground"))
            {
                sortingLayer = "Ground";
                sortingOrder = normalized.Contains("3") ? 5 : 4;
            }
            else if (normalized.Contains("shadow"))
            {
                sortingLayer = "Ground";
                sortingOrder = normalized.Contains("2") ? 7 : 6;
            }
            else if (normalized.Contains("roof"))
            {
                sortingLayer = "Objects";
                sortingOrder = normalized.Contains("3") ? 14 : normalized.Contains("2") ? 13 : 12;
            }
            else if (normalized.Contains("brokenobject"))
            {
                sortingLayer = "Objects";
                sortingOrder = 11;
            }
            else if (normalized.Contains("walldetail"))
            {
                sortingLayer = "Objects";
                sortingOrder = 11;
            }
            else if (normalized.Contains("wall"))
            {
                sortingLayer = "Objects";
                sortingOrder = 10;
            }
            else if (normalized.Contains("object"))
            {
                sortingLayer = "Objects";
                sortingOrder = 11;
            }
            else
            {
                throw new InvalidOperationException(
                    "Bilinmeyen source layer icin explicit route gerekli: " + sourceLayer.SourceName);
            }

            return new LayerRoute
            {
                TargetName = ManagedLayerPrefix + SanitizeLayerName(sourceLayer.SourceName),
                SortingLayer = sortingLayer,
                SortingOrder = sortingOrder
            };
        }

        private static string SanitizeLayerName(string sourceName)
        {
            var builder = new StringBuilder();
            bool previousWasSeparator = false;
            for (int i = 0; i < sourceName.Length; i++)
            {
                char character = sourceName[i];
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    previousWasSeparator = false;
                }
                else if (!previousWasSeparator)
                {
                    builder.Append('_');
                    previousWasSeparator = true;
                }
            }

            string result = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "Layer" : result;
        }

        private static void EnsureSortingLayerExists(string sortingLayerName)
        {
            if (!SortingLayer.layers.Any(
                    layer => string.Equals(layer.name, sortingLayerName, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Gerekli sorting layer projede yok: " + sortingLayerName);
            }
        }

        private static string ResolveManagedRootName(FantasyKingdomStampPurpose purpose)
        {
            return purpose == FantasyKingdomStampPurpose.BattlefieldDecoration ||
                   purpose == FantasyKingdomStampPurpose.GroundDetail
                ? BattlefieldRootName
                : StructureRootName;
        }

        private static void ValidateTargetSceneContracts(Grid targetGrid)
        {
            if (!string.Equals(
                    targetGrid.gameObject.scene.path.Replace('\\', '/'),
                    "Assets/Scenes/NewGameScene.unity",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.Equals(targetGrid.name, "Grid", StringComparison.Ordinal))
                throw new InvalidOperationException("NewGameScene target'i kok Grid olmalidir.");

            Tilemap[] tilemaps = targetGrid.GetComponentsInChildren<Tilemap>(true);
            string[] protectedNames = { "outside", "outside0", "outside2" };
            for (int i = 0; i < protectedNames.Length; i++)
            {
                int matchCount = tilemaps.Count(map => string.Equals(
                    map.name,
                    protectedNames[i],
                    StringComparison.Ordinal));
                if (matchCount != 1)
                {
                    throw new InvalidOperationException(
                        "NewGameScene protected tilemap kontrati bozuk: " +
                        protectedNames[i] + " count=" + matchCount);
                }
            }

            Tilemap outside = tilemaps.First(map => string.Equals(map.name, "outside", StringComparison.Ordinal));
            int outsideSlots = 0;
            foreach (Vector3Int cell in outside.cellBounds.allPositionsWithin)
            {
                if (outside.HasTile(cell))
                    outsideSlots++;
            }
            if (outsideSlots != 40)
                throw new InvalidOperationException(
                    "outside okcu-slot kontrati bozulmus. Beklenen=40 Mevcut=" + outsideSlots);

            GameObject markerRoot = targetGrid.gameObject.scene.GetRootGameObjects()
                .FirstOrDefault(root => string.Equals(root.name, "VillageMarkers", StringComparison.Ordinal));
            if (markerRoot == null)
                throw new InvalidOperationException("VillageMarkers root bulunamadi.");
            if (markerRoot.transform.childCount != 5)
                throw new InvalidOperationException(
                    "VillageMarkers child kontrati bozuk. Beklenen=5 Mevcut=" +
                    markerRoot.transform.childCount);

            string[] requiredMarkers =
            {
                "CastleKeepMarker",
                "WoodSiteMarker",
                "StoneSiteMarker",
                "FoodSiteMarker",
                "IronSiteMarker"
            };
            for (int i = 0; i < requiredMarkers.Length; i++)
            {
                if (markerRoot.transform.Find(requiredMarkers[i]) == null)
                    throw new InvalidOperationException("Gameplay marker eksik: " + requiredMarkers[i]);
            }
        }

        private static void ValidateManagedLayerMetadata(
            Tilemap tilemap,
            TilemapRenderer renderer,
            FantasyKingdomStampLayer sourceLayer,
            LayerRoute route)
        {
            bool tilemapMismatch = (tilemap.tileAnchor - sourceLayer.TileAnchor).sqrMagnitude > 0.0001f ||
                                   tilemap.orientation != sourceLayer.Orientation ||
                                   !MatrixApproximately(tilemap.orientationMatrix, sourceLayer.OrientationMatrix) ||
                                   !ColorApproximately(tilemap.color, sourceLayer.LayerColor);
            bool rendererMismatch = renderer.mode != sourceLayer.RendererMode ||
                                    renderer.sortOrder != sourceLayer.SortOrder ||
                                    !string.Equals(
                                        renderer.sortingLayerName,
                                        route.SortingLayer,
                                        StringComparison.Ordinal) ||
                                    renderer.sortingOrder != route.SortingOrder;
            if (tilemapMismatch || rendererMismatch)
            {
                throw new InvalidOperationException(
                    route.TargetName + " metadata'si tool kontratindan sapmis; sessizce degistirilmeyecek.");
            }
        }

        private static void ValidateManagedRoot(Transform managedRoot)
        {
            Component[] rootComponents = managedRoot.GetComponents<Component>();
            if (rootComponents.Any(component => !(component is Transform)))
            {
                throw new InvalidOperationException(
                    managedRoot.name + " beklenmeyen component tasiyor; tool sahipligi belirsiz.");
            }

            for (int childIndex = 0; childIndex < managedRoot.childCount; childIndex++)
            {
                Transform child = managedRoot.GetChild(childIndex);
                bool validName = child.name.StartsWith(ManagedLayerPrefix, StringComparison.Ordinal);
                bool validComponents = child.GetComponent<Tilemap>() != null &&
                                       child.GetComponent<TilemapRenderer>() != null;
                if (!validName || !validComponents || child.childCount != 0)
                {
                    throw new InvalidOperationException(
                        managedRoot.name + " altinda beklenmeyen child var: " + child.name);
                }
            }
        }

        private static bool MatrixApproximately(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > 0.0001f)
                    return false;
            }
            return true;
        }

        private static bool ColorApproximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= 0.0001f &&
                   Mathf.Abs(a.g - b.g) <= 0.0001f &&
                   Mathf.Abs(a.b - b.b) <= 0.0001f &&
                   Mathf.Abs(a.a - b.a) <= 0.0001f;
        }

        private static void ValidateIdentityTransform(Transform transform, string ownerName)
        {
            if (transform.localPosition.sqrMagnitude > 0.0001f ||
                Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f ||
                (transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                throw new InvalidOperationException(
                    ownerName + " transform'u identity degil; tool guvenli sekilde devam edemez.");
            }
        }
    }
}
#endif
