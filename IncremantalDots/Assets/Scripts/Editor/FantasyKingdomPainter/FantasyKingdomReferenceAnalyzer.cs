#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    internal sealed class FantasyKingdomLayerAnalysis
    {
        public string HierarchyPath;
        public string Name;
        public int OccupiedCellCount;
        public int UniqueTileCount;
        public BoundsInt CellBounds;
        public string SortingLayerName;
        public int SortingOrder;
        public TilemapRenderer.Mode RendererMode;
        public bool Selected;
    }

    internal sealed class FantasyKingdomStructureCandidate
    {
        public RectInt RoofBounds;
        public int RoofTileCount;
        public Vector3 WorldCenter;

        public string DisplayName(int index)
        {
            return string.Format(
                "#{0:00}  roof:{1}  cells:[{2},{3}] {4}x{5}  world:{6:F1},{7:F1}",
                index + 1,
                RoofTileCount,
                RoofBounds.xMin,
                RoofBounds.yMin,
                RoofBounds.width,
                RoofBounds.height,
                WorldCenter.x,
                WorldCenter.y);
        }
    }

    internal sealed class FantasyKingdomAnalysisResult
    {
        public string ScenePath;
        public string GridPath;
        public GridLayout.CellLayout CellLayout;
        public GridLayout.CellSwizzle CellSwizzle;
        public Vector3 CellSize;
        public Vector3 CellGap;
        public readonly List<FantasyKingdomLayerAnalysis> Layers = new List<FantasyKingdomLayerAnalysis>();
        public readonly List<FantasyKingdomStructureCandidate> Candidates = new List<FantasyKingdomStructureCandidate>();

        public int TotalOccupiedCells => Layers.Sum(layer => layer.OccupiedCellCount);
    }

    /// <summary>
    /// Referans sahneyi gecici additive acip analiz eder. Aktif sahneyi ve sahne dosyasini degistirmez.
    /// </summary>
    internal static class FantasyKingdomReferenceAnalyzer
    {
        private static readonly HashSet<string> DefaultLayerNames = new HashSet<string>(
            new[]
            {
                "Walls",
                "Roof1",
                "Roof2",
                "Roof3",
                "WallDetail1",
                "WallDetail2",
                "Objects",
                "BrokenObjects",
                "Shadows1",
                "Shadows2",
                "LowerShadows",
                "Ground 2",
                "Ground 3"
            },
            StringComparer.OrdinalIgnoreCase);

        public static FantasyKingdomAnalysisResult Analyze(
            string scenePath,
            int minimumRoofComponentTiles,
            int candidateMergeDistance)
        {
            ValidateEditorState(scenePath);

            using (var scope = new ReferenceSceneScope(scenePath))
            {
                Grid referenceGrid = FindReferenceGrid(scope.Scene);
                if (referenceGrid == null)
                    throw new InvalidOperationException("Referans sahnede Tilemap tasiyan bir Grid bulunamadi.");

                var result = new FantasyKingdomAnalysisResult
                {
                    ScenePath = scenePath,
                    GridPath = GetHierarchyPath(referenceGrid.transform),
                    CellLayout = referenceGrid.cellLayout,
                    CellSwizzle = referenceGrid.cellSwizzle,
                    CellSize = referenceGrid.cellSize,
                    CellGap = referenceGrid.cellGap
                };

                Tilemap[] tilemaps = referenceGrid.GetComponentsInChildren<Tilemap>(true);
                for (int i = 0; i < tilemaps.Length; i++)
                    result.Layers.Add(AnalyzeLayer(tilemaps[i]));

                result.Layers.Sort((a, b) =>
                {
                    int order = a.SortingOrder.CompareTo(b.SortingOrder);
                    return order != 0
                        ? order
                        : string.Compare(a.HierarchyPath, b.HierarchyPath, StringComparison.OrdinalIgnoreCase);
                });

                List<CandidateBounds> rawCandidates = FindRoofCandidates(
                    tilemaps,
                    Mathf.Max(1, minimumRoofComponentTiles));

                List<CandidateBounds> merged = MergeCandidateBounds(
                    rawCandidates,
                    Mathf.Max(0, candidateMergeDistance));

                for (int i = 0; i < merged.Count; i++)
                {
                    CandidateBounds candidate = merged[i];
                    Vector3Int centerCell = new Vector3Int(
                        candidate.Bounds.xMin + candidate.Bounds.width / 2,
                        candidate.Bounds.yMin + candidate.Bounds.height / 2,
                        0);

                    result.Candidates.Add(new FantasyKingdomStructureCandidate
                    {
                        RoofBounds = candidate.Bounds,
                        RoofTileCount = candidate.TileCount,
                        WorldCenter = referenceGrid.CellToWorld(centerCell)
                    });
                }

                result.Candidates.Sort((a, b) =>
                {
                    int count = b.RoofTileCount.CompareTo(a.RoofTileCount);
                    if (count != 0)
                        return count;
                    int x = a.RoofBounds.xMin.CompareTo(b.RoofBounds.xMin);
                    return x != 0 ? x : a.RoofBounds.yMin.CompareTo(b.RoofBounds.yMin);
                });

                return result;
            }
        }

        public static string ExtractStamp(
            FantasyKingdomAnalysisResult analysis,
            RectInt sourceRegion,
            string stampName,
            FantasyKingdomStampPurpose stampPurpose,
            string outputFolder)
        {
            if (analysis == null)
                throw new ArgumentNullException(nameof(analysis));
            if (sourceRegion.width <= 0 || sourceRegion.height <= 0)
                throw new ArgumentException("Extraction region pozitif genislik ve yukseklik tasimali.");

            List<FantasyKingdomLayerAnalysis> selectedLayers = analysis.Layers
                .Where(layer => layer.Selected)
                .ToList();

            if (selectedLayers.Count == 0)
                throw new InvalidOperationException("En az bir tilemap katmani secilmelidir.");

            ValidateEditorState(analysis.ScenePath);
            outputFolder = NormalizeAssetFolder(outputFolder);
            EnsureAssetFolder(outputFolder);

            using (var scope = new ReferenceSceneScope(analysis.ScenePath))
            {
                Grid referenceGrid = FindTransformByPath(scope.Scene, analysis.GridPath)?.GetComponent<Grid>();
                if (referenceGrid == null)
                    throw new InvalidOperationException("Analizde bulunan referans Grid yeniden bulunamadi.");

                var extractedLayers = new List<FantasyKingdomStampLayer>();
                int totalTileCount = 0;

                for (int layerIndex = 0; layerIndex < selectedLayers.Count; layerIndex++)
                {
                    FantasyKingdomLayerAnalysis layerInfo = selectedLayers[layerIndex];
                    Transform layerTransform = FindTransformByPath(scope.Scene, layerInfo.HierarchyPath);
                    Tilemap tilemap = layerTransform != null ? layerTransform.GetComponent<Tilemap>() : null;
                    if (tilemap == null)
                        continue;

                    var cells = new List<FantasyKingdomStampCell>();
                    for (int y = sourceRegion.yMin; y < sourceRegion.yMax; y++)
                    {
                        for (int x = sourceRegion.xMin; x < sourceRegion.xMax; x++)
                        {
                            var sourceCell = new Vector3Int(x, y, 0);
                            TileBase tile = tilemap.GetTile(sourceCell);
                            if (tile == null)
                                continue;

                            var relativeCell = new Vector3Int(
                                x - sourceRegion.xMin,
                                y - sourceRegion.yMin,
                                sourceCell.z);

                            cells.Add(new FantasyKingdomStampCell(
                                relativeCell,
                                tile,
                                tilemap.GetTransformMatrix(sourceCell),
                                tilemap.GetColor(sourceCell),
                                tilemap.GetTileFlags(sourceCell)));
                        }
                    }

                    if (cells.Count == 0)
                        continue;

                    totalTileCount += cells.Count;
                    extractedLayers.Add(new FantasyKingdomStampLayer(
                        layerInfo.HierarchyPath,
                        tilemap,
                        tilemap.GetComponent<TilemapRenderer>(),
                        cells));
                }

                if (totalTileCount == 0)
                    throw new InvalidOperationException("Secilen bolge ve katmanlarda tile bulunamadi.");

                string safeName = SanitizeFileName(stampName);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    outputFolder + "/" + safeName + ".asset");

                var stamp = ScriptableObject.CreateInstance<FantasyKingdomStructureStamp>();
                stamp.name = safeName;
                stamp.Initialize(
                    analysis.ScenePath,
                    analysis.GridPath,
                    sourceRegion,
                    referenceGrid,
                    stampPurpose,
                    extractedLayers);

                AssetDatabase.CreateAsset(stamp, assetPath);
                EditorUtility.SetDirty(stamp);
                AssetDatabase.SaveAssetIfDirty(stamp);
                AssetDatabase.ImportAsset(assetPath);
                return assetPath;
            }
        }

        private static FantasyKingdomLayerAnalysis AnalyzeLayer(Tilemap tilemap)
        {
            var uniqueTiles = new HashSet<TileBase>();
            int occupied = 0;
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = tilemap.GetTile(cell);
                if (tile == null)
                    continue;
                occupied++;
                uniqueTiles.Add(tile);
            }

            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            return new FantasyKingdomLayerAnalysis
            {
                HierarchyPath = GetHierarchyPath(tilemap.transform),
                Name = tilemap.name,
                OccupiedCellCount = occupied,
                UniqueTileCount = uniqueTiles.Count,
                CellBounds = tilemap.cellBounds,
                SortingLayerName = renderer != null ? renderer.sortingLayerName : "Default",
                SortingOrder = renderer != null ? renderer.sortingOrder : 0,
                RendererMode = renderer != null ? renderer.mode : TilemapRenderer.Mode.Chunk,
                Selected = DefaultLayerNames.Contains(tilemap.name)
            };
        }

        private static List<CandidateBounds> FindRoofCandidates(Tilemap[] tilemaps, int minimumTiles)
        {
            var result = new List<CandidateBounds>();
            for (int tilemapIndex = 0; tilemapIndex < tilemaps.Length; tilemapIndex++)
            {
                Tilemap tilemap = tilemaps[tilemapIndex];
                if (tilemap.name.IndexOf("Roof", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var unvisited = new HashSet<Vector3Int>();
                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (tilemap.HasTile(cell))
                        unvisited.Add(cell);
                }

                while (unvisited.Count > 0)
                {
                    Vector3Int seed = unvisited.First();
                    unvisited.Remove(seed);

                    var queue = new Queue<Vector3Int>();
                    queue.Enqueue(seed);
                    int count = 0;
                    int minX = seed.x;
                    int maxX = seed.x;
                    int minY = seed.y;
                    int maxY = seed.y;

                    while (queue.Count > 0)
                    {
                        Vector3Int cell = queue.Dequeue();
                        count++;
                        minX = Mathf.Min(minX, cell.x);
                        maxX = Mathf.Max(maxX, cell.x);
                        minY = Mathf.Min(minY, cell.y);
                        maxY = Mathf.Max(maxY, cell.y);

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0)
                                    continue;
                                var neighbor = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                                if (!unvisited.Remove(neighbor))
                                    continue;
                                queue.Enqueue(neighbor);
                            }
                        }
                    }

                    if (count < minimumTiles)
                        continue;

                    result.Add(new CandidateBounds
                    {
                        Bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1),
                        TileCount = count
                    });
                }
            }

            return result;
        }

        private static List<CandidateBounds> MergeCandidateBounds(
            List<CandidateBounds> rawCandidates,
            int mergeDistance)
        {
            var merged = new List<CandidateBounds>();
            for (int i = 0; i < rawCandidates.Count; i++)
            {
                CandidateBounds candidate = rawCandidates[i];
                bool absorbed;
                do
                {
                    absorbed = false;
                    for (int existingIndex = merged.Count - 1; existingIndex >= 0; existingIndex--)
                    {
                        if (!Expanded(candidate.Bounds, mergeDistance)
                            .Overlaps(Expanded(merged[existingIndex].Bounds, mergeDistance)))
                            continue;

                        candidate = new CandidateBounds
                        {
                            Bounds = Union(candidate.Bounds, merged[existingIndex].Bounds),
                            TileCount = candidate.TileCount + merged[existingIndex].TileCount
                        };
                        merged.RemoveAt(existingIndex);
                        absorbed = true;
                    }
                } while (absorbed);

                merged.Add(candidate);
            }

            return merged;
        }

        private static RectInt Expanded(RectInt rect, int amount)
        {
            return new RectInt(
                rect.xMin - amount,
                rect.yMin - amount,
                rect.width + amount * 2,
                rect.height + amount * 2);
        }

        private static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax);
            int yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Grid FindReferenceGrid(Scene scene)
        {
            Grid bestGrid = null;
            int bestTilemapCount = -1;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Grid[] grids = roots[rootIndex].GetComponentsInChildren<Grid>(true);
                for (int gridIndex = 0; gridIndex < grids.Length; gridIndex++)
                {
                    int tilemapCount = grids[gridIndex].GetComponentsInChildren<Tilemap>(true).Length;
                    if (tilemapCount <= bestTilemapCount)
                        continue;
                    bestGrid = grids[gridIndex];
                    bestTilemapCount = tilemapCount;
                }
            }
            return bestGrid;
        }

        private static Transform FindTransformByPath(Scene scene, string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return null;

            string[] parts = hierarchyPath.Split('/');
            GameObject root = scene.GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == parts[0]);
            if (root == null)
                return null;

            Transform current = root.transform;
            for (int i = 1; i < parts.Length && current != null; i++)
                current = current.Find(parts[i]);
            return current;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private static void ValidateEditorState(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Referans analizi Play Mode disinda calistirilmalidir.");
            if (string.IsNullOrWhiteSpace(scenePath) || !scenePath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Gecerli bir proje ici Scene asseti secilmelidir.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new FileNotFoundException("Referans Scene asseti bulunamadi.", scenePath);
        }

        private static string NormalizeAssetFolder(string folder)
        {
            string normalized = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(normalized))
                normalized = "Assets/Editor/FantasyKingdomPainter/Stamps";
            if (!normalized.StartsWith("Assets", StringComparison.Ordinal))
                throw new ArgumentException("Output folder Assets/ altinda olmalidir.");
            return normalized;
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value)
                ? "FantasyKingdomStructureStamp"
                : value.Trim();

            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                result = result.Replace(invalidChars[i], '_');
            return result.Replace(' ', '_');
        }

        private sealed class ReferenceSceneScope : IDisposable
        {
            private readonly bool openedByTool;
            private readonly Scene previousActiveScene;
            public Scene Scene { get; }

            public ReferenceSceneScope(string scenePath)
            {
                previousActiveScene = SceneManager.GetActiveScene();
                Scene existing = SceneManager.GetSceneByPath(scenePath);
                if (existing.IsValid() && existing.isLoaded)
                {
                    Scene = existing;
                    openedByTool = false;
                }
                else
                {
                    Scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    openedByTool = true;
                }
            }

            public void Dispose()
            {
                if (openedByTool && Scene.IsValid() && Scene.isLoaded)
                    EditorSceneManager.CloseScene(Scene, true);

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private struct CandidateBounds
        {
            public RectInt Bounds;
            public int TileCount;
        }
    }
}
#endif
