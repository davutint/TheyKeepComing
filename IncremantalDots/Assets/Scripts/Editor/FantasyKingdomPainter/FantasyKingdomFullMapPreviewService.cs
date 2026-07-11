#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    internal enum FantasyKingdomFullMapIssueSeverity
    {
        Warning = 0,
        Error = 1
    }

    internal sealed class FantasyKingdomFullMapIssue
    {
        public FantasyKingdomFullMapIssueSeverity Severity;
        public string PlacementId;
        public string Message;

        public override string ToString()
        {
            string prefix = Severity == FantasyKingdomFullMapIssueSeverity.Error
                ? "HARD"
                : "WARN";
            return string.IsNullOrEmpty(PlacementId)
                ? prefix + " — " + Message
                : prefix + " — " + PlacementId + ": " + Message;
        }
    }

    internal sealed class FantasyKingdomFullMapPreviewReport
    {
        public int EnabledPlacementCount;
        public int RenderablePlacementCount;
        public int StampTileCount;
        public int UniquePreviewCellCount;
        public int SolidPreviewCellCount;
        public int ProtectedOverlapCellCount;
        public int LegacyVisualOverlapCellCount;
        public int UnknownExistingOverlapCellCount;
        public int CanonicalLayerConflictCount;
        public int SolidFootprintConflictCount;
        public int GroundShadowOverlapCellCount;
        public int MarkerConflictCellCount;
        public int CorridorRiskCellCount;
        public int RestrictedZoneCellCount;
        public int MissingGroundSupportCellCount;
        public int OutOfCameraCellCount;
        public int ReferenceViewportOutsideCellCount;
        public int BoundGameplayAnchorCount;
        public int LivingForestTreeCount;
        public int EnemyForestBackTreeCount;
        public int EnemyForestFrontTreeCount;
        public int EnemyFrontCoveredYBandCount;
        public int CaravanRoadCellCount;
        public int CaravanRoadComponentCount;
        public int OpenBattlefieldSolidCellCount;
        public readonly List<FantasyKingdomFullMapIssue> Issues =
            new List<FantasyKingdomFullMapIssue>();

        public int ErrorCount => Issues.Count(issue =>
            issue.Severity == FantasyKingdomFullMapIssueSeverity.Error);

        public int WarningCount => Issues.Count(issue =>
            issue.Severity == FantasyKingdomFullMapIssueSeverity.Warning);

        public bool HasHardConflicts => ErrorCount > 0;

        public string BuildSummary()
        {
            string aggregate = string.Format(
                "Full preview: {0}/{1} placement, {2} tile, {3} unique / {4} solid cell\n" +
                "Hard: {5}  Warning: {6}  Protected: {7}  Marker: {8}  Zone: {9}\n" +
                "Inter-placement layer/solid/ground: {10}/{11}/{12}  Corridor risk: {13}  " +
                "Legacy visual overlap: {14}\n" +
                "Ground support missing: {15}  Camera disi: {16}  Ref 16:9 disi: {17}  " +
                "Gameplay anchor: {18}/5",
                RenderablePlacementCount,
                EnabledPlacementCount,
                StampTileCount,
                UniquePreviewCellCount,
                SolidPreviewCellCount,
                ErrorCount,
                WarningCount,
                ProtectedOverlapCellCount,
                MarkerConflictCellCount,
                RestrictedZoneCellCount,
                CanonicalLayerConflictCount,
                SolidFootprintConflictCount,
                GroundShadowOverlapCellCount,
                CorridorRiskCellCount,
                LegacyVisualOverlapCellCount,
                MissingGroundSupportCellCount,
                OutOfCameraCellCount,
                ReferenceViewportOutsideCellCount,
                BoundGameplayAnchorCount);
            return aggregate + string.Format(
                "\nV3 forest trees living/back/front: {0}/{1}/{2}  front Y bands: {3}/14  " +
                "road: {4} cell / {5} component  open-center solid: {6}",
                LivingForestTreeCount,
                EnemyForestBackTreeCount,
                EnemyForestFrontTreeCount,
                EnemyFrontCoveredYBandCount,
                CaravanRoadCellCount,
                CaravanRoadComponentCount,
                OpenBattlefieldSolidCellCount);
        }

        public void AddError(string placementId, string message)
        {
            Issues.Add(new FantasyKingdomFullMapIssue
            {
                Severity = FantasyKingdomFullMapIssueSeverity.Error,
                PlacementId = placementId,
                Message = message
            });
        }

        public void AddWarning(string placementId, string message)
        {
            Issues.Add(new FantasyKingdomFullMapIssue
            {
                Severity = FantasyKingdomFullMapIssueSeverity.Warning,
                PlacementId = placementId,
                Message = message
            });
        }
    }

    /// <summary>
    /// Bir layout'taki tum stamp'leri topluca analiz eder ve yalniz DontSave preview
    /// tilemap'lerine yazar. Gercek harita katmanlarina SetTile uygulamaz.
    /// </summary>
    [InitializeOnLoad]
    internal static class FantasyKingdomFullMapPreviewService
    {
        public const string PreviewRootName = "__FKFullMapPreviewRoot";
        private const string PersistentV3RootName = "FK_V3_Map";
        private const string OcclusionProbeRootName = "__FKZombieOcclusionProbeRoot";
        private const string ZombieAtlasPath = "Assets/Art/Atlases/skeleton_atlas.png";

        private const float BehindUnitsZ = 0f;
        private const float InFrontOfUnitsZ = -2f;
        private const float SupportedCameraMinX = -13.2f;
        private const float SupportedCameraMaxX = 25.2f;
        private const float ReferenceCameraMinX = -8.22f;
        private const float ReferenceCameraMaxX = 20.22f;
        private const float CameraMinY = -8f;
        private const float CameraMaxY = 8f;
        private const float SettlementSolidMaxX = -1.5f;
        private const float MoatMinX = 1.5f;
        private const float MoatMaxX = 4f;
        private const float BattlefieldMinX = 4f;
        private const float BattlefieldMaxX = 18f;
        private const float FarRightFrameMinX = 18f;
        private const float FarRightFrameMaxX = 29f;
        private const float SpawnMinX = 27f;
        private const float SpawnMaxX = 29f;
        private const float SpawnMaxAbsY = 6.5f;
        private const float CorridorRadius = 0.55f;
        private const float CorridorEndpointClearance = 1f;

        private static readonly HashSet<string> ProtectedLayerNames = new HashSet<string>(
            new[] { "outside", "outside0", "outside2" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> GroundSupportLayerNames = new HashSet<string>(
            new[] { "Grass", "Ground" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> LegacyVisualLayerNames = new HashSet<string>(
            new[]
            {
                "Structures",
                "OverlayProps",
                "RoofLow",
                "RoofHigh",
                "Roof1",
                "Roof2",
                "Roof3",
                "GroundDetail"
            },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> LegacyObjectLayerNames = new HashSet<string>(
            new[]
            {
                "Structures",
                "OverlayProps",
                "RoofLow",
                "RoofHigh",
                "Roof1",
                "Roof2",
                "Roof3",
                "GroundDetail"
            },
            StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<int, Dictionary<TilemapRenderer, bool>>
            LegacyRendererStatesByGrid =
                new Dictionary<int, Dictionary<TilemapRenderer, bool>>();

        private static readonly Dictionary<FantasyKingdomGameplayAnchor, string> MarkerNames =
            new Dictionary<FantasyKingdomGameplayAnchor, string>
            {
                { FantasyKingdomGameplayAnchor.CastleKeep, "CastleKeepMarker" },
                { FantasyKingdomGameplayAnchor.Wood, "WoodSiteMarker" },
                { FantasyKingdomGameplayAnchor.Stone, "StoneSiteMarker" },
                { FantasyKingdomGameplayAnchor.Food, "FoodSiteMarker" },
                { FantasyKingdomGameplayAnchor.Iron, "IronSiteMarker" }
            };

        static FantasyKingdomFullMapPreviewService()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= CleanupAllTransientPreviewState;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupAllTransientPreviewState;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting -= CleanupAllTransientPreviewState;
            EditorApplication.quitting += CleanupAllTransientPreviewState;
            EditorSceneManager.sceneSaving -= HandleSceneSaving;
            EditorSceneManager.sceneSaving += HandleSceneSaving;
            EditorSceneManager.sceneClosing -= HandleSceneClosing;
            EditorSceneManager.sceneClosing += HandleSceneClosing;
        }

        public static FantasyKingdomFullMapPreviewReport AnalyzeLayout(
            FantasyKingdomMapLayout layout,
            Grid targetGrid)
        {
            List<PlacementRuntime> unused;
            return BuildAnalysis(layout, targetGrid, out unused);
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Create Default V3 Preview")]
        private static void CreateDefaultV3PreviewFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(scene);
            FantasyKingdomMapLayout layout = FantasyKingdomMapLayoutFactory.LoadDefault();
            FantasyKingdomFullMapPreviewReport report = CreateOrUpdatePreview(layout, grid);
            Debug.Log(report.BuildSummary().Replace('\n', ' '), layout);
            for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
            {
                FantasyKingdomFullMapIssue issue = report.Issues[issueIndex];
                if (issue.Severity == FantasyKingdomFullMapIssueSeverity.Error)
                    Debug.LogError("FK V3 " + issue, layout);
                else
                    Debug.LogWarning("FK V3 " + issue, layout);
            }
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Create V3 Retouch Preview")]
        private static void CreateV3RetouchPreviewFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(scene);
            FantasyKingdomMapLayout layout = FantasyKingdomMapLayoutFactory.LoadRetouchCandidate();
            if (layout == null)
            {
                throw new InvalidOperationException(
                    "V3 retouch preview layout bulunamadi. Once Rebuild V3 Retouch Preview Assets calistirilmalidir.");
            }

            FantasyKingdomFullMapPreviewReport report = CreateOrUpdatePreview(layout, grid);
            Debug.Log(report.BuildSummary().Replace('\n', ' '), layout);
            for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
            {
                FantasyKingdomFullMapIssue issue = report.Issues[issueIndex];
                if (issue.Severity == FantasyKingdomFullMapIssueSeverity.Error)
                    Debug.LogError("FK V3 RETOUCH " + issue, layout);
                else
                    Debug.LogWarning("FK V3 RETOUCH " + issue, layout);
            }
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Clear Full Map Preview")]
        private static void ClearDefaultPreviewFromMenu()
        {
            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(
                SceneManager.GetActiveScene());
            ClearPreview(grid);
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Create Zombie Forest Occlusion Probe")]
        private static void CreateZombieForestOcclusionProbeFromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(scene);
            if (grid == null || grid.transform.Find(PreviewRootName) == null)
            {
                throw new InvalidOperationException(
                    "Occlusion probe'dan once Default V3 Preview olusturulmalidir.");
            }

            Sprite zombieFrame = AssetDatabase.LoadAllAssetsAtPath(ZombieAtlasPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => string.Equals(
                    sprite.name,
                    "skeleton_atlas_60",
                    StringComparison.Ordinal));
            if (zombieFrame == null)
                throw new InvalidOperationException(
                    "Zombie atlas probe frame bulunamadi: " + ZombieAtlasPath);

            bool sceneWasDirty = scene.isDirty;
            ClearOcclusionProbe(scene);
            var root = new GameObject(OcclusionProbeRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
            SceneManager.MoveGameObjectToScene(root, scene);

            Transform previewRoot = grid.transform.Find(PreviewRootName);
            Tilemap frontOccluder = previewRoot.GetComponentsInChildren<Tilemap>(true)
                .FirstOrDefault(map => map.name.IndexOf(
                    "EnemyForestFrontOccluder",
                    StringComparison.OrdinalIgnoreCase) >= 0);
            if (frontOccluder == null)
                throw new InvalidOperationException("Enemy forest front preview tilemap'i bulunamadi.");

            Vector3[] positions =
            {
                new Vector3(16f, 0f, -1f),
                FindOccluderProbePosition(grid, frontOccluder, -4f),
                FindOccluderProbePosition(grid, frontOccluder, 0f),
                FindOccluderProbePosition(grid, frontOccluder, 5f)
            };
            string[] names =
            {
                "00_OpenBattlefield_Control",
                "01_Forest_South",
                "02_Forest_Center",
                "03_Forest_North"
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var instance = new GameObject(names[i])
                {
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
                };
                instance.transform.SetParent(root.transform, true);
                instance.transform.position = positions[i];
                instance.transform.localScale = Vector3.one * 3f;
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = zombieFrame;
                renderer.sortingLayerName = SortingLayerExists("Entities")
                    ? "Entities"
                    : "Default";
                renderer.sortingOrder = 0;
            }

            if (!sceneWasDirty && scene.isDirty)
            {
                ClearOcclusionProbe(scene);
                throw new InvalidOperationException(
                    "Zombie occlusion probe temiz sahneyi dirty yapti; probe geri alindi.");
            }
            SceneView.RepaintAll();
        }

        private static Vector3 FindOccluderProbePosition(
            Grid grid,
            Tilemap frontOccluder,
            float targetY)
        {
            Vector3 best = Vector3.zero;
            float bestScore = float.MaxValue;
            foreach (Vector3Int cell in frontOccluder.cellBounds.allPositionsWithin)
            {
                if (!frontOccluder.HasTile(cell))
                    continue;
                Vector3 world = grid.GetCellCenterWorld(cell);
                float score = Mathf.Abs(world.y - targetY) + Mathf.Abs(world.x - 19.1f) * 0.2f;
                if (score >= bestScore)
                    continue;
                bestScore = score;
                best = new Vector3(world.x, world.y, -1f);
            }
            if (bestScore == float.MaxValue)
                throw new InvalidOperationException("Enemy forest front occluder bos.");
            return best;
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Clear Zombie Forest Occlusion Probe")]
        private static void ClearZombieForestOcclusionProbeFromMenu()
        {
            ClearOcclusionProbe(SceneManager.GetActiveScene());
            SceneView.RepaintAll();
        }

        public static FantasyKingdomFullMapPreviewReport CreateOrUpdatePreview(
            FantasyKingdomMapLayout layout,
            Grid targetGrid)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Full Map Preview Play Mode disinda calistirilmalidir.");

            ValidateTarget(layout, targetGrid);
            Scene targetScene = targetGrid.gameObject.scene;
            bool sceneWasDirty = targetScene.isDirty;

            try
            {
                FantasyKingdomStampPreviewService.ClearPreview(targetGrid);
                ClearPreviewInternal(targetGrid);

                List<PlacementRuntime> runtimes;
                FantasyKingdomFullMapPreviewReport report =
                    BuildAnalysis(layout, targetGrid, out runtimes);
                RenderPreview(targetGrid, runtimes);
                SuppressLegacyObjectRenderers(targetGrid);
                SceneView.RepaintAll();

                if (!sceneWasDirty && targetScene.isDirty)
                {
                    ClearPreviewInternal(targetGrid);
                    throw new InvalidOperationException(
                        "DontSave full preview temiz sahneyi dirty yapti. Tool scene'i kaydetmedi; " +
                        "kalici isleme gecilmedi.");
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
            SceneView.RepaintAll();
        }

        private static FantasyKingdomFullMapPreviewReport BuildAnalysis(
            FantasyKingdomMapLayout layout,
            Grid targetGrid,
            out List<PlacementRuntime> runtimes)
        {
            ValidateTarget(layout, targetGrid);

            var report = new FantasyKingdomFullMapPreviewReport();
            runtimes = new List<PlacementRuntime>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var allPreviewCells = new HashSet<Vector3Int>();
            var allSolidCells = new HashSet<Vector3Int>();
            var canonicalOwners = new Dictionary<LayerCellKey, string>();
            var solidOwners = new Dictionary<Vector3Int, string>();
            var groundOwners = new Dictionary<Vector3Int, string>();
            var canonicalConflictByPlacement = new Dictionary<string, int>();
            var solidConflictByPlacement = new Dictionary<string, int>();
            var groundOverlapByPlacement = new Dictionary<string, int>();
            var anchorOwners = new Dictionary<FantasyKingdomGameplayAnchor, string>();

            IReadOnlyList<FantasyKingdomMapPlacement> placements = layout.Placements;
            for (int placementIndex = 0; placementIndex < placements.Count; placementIndex++)
            {
                FantasyKingdomMapPlacement placement = placements[placementIndex];
                if (placement == null || !placement.Enabled)
                    continue;

                report.EnabledPlacementCount++;
                string placementId = string.IsNullOrWhiteSpace(placement.Id)
                    ? "placement[" + placementIndex + "]"
                    : placement.Id.Trim();

                if (string.IsNullOrWhiteSpace(placement.Id))
                {
                    report.AddError(placementId, "Stable placement id bos olamaz.");
                    continue;
                }
                if (!ids.Add(placementId))
                {
                    report.AddError(placementId, "Duplicate stable placement id.");
                    continue;
                }
                if (placement.Stamp == null)
                {
                    report.AddError(placementId, "Stamp asset atanmamis.");
                    continue;
                }
                if (placement.Stamp.TotalTileCount <= 0)
                {
                    report.AddError(placementId, "Stamp bos.");
                    continue;
                }
                if (layout.SchemaVersion >= FantasyKingdomMapLayout.CurrentSchemaVersion &&
                    placement.RenderBand == FantasyKingdomRenderBand.LegacyAuto)
                {
                    report.AddError(
                        placementId,
                        "V3 layout placement'i acik bir render band secmelidir.");
                }

                Vector3Int stampAnchor = placement.Stamp.AnchorLocalCell;
                Vector2Int stampSize = placement.Stamp.SourceRegionSize;
                if (stampAnchor.x < 0 || stampAnchor.y < 0 || stampAnchor.z != 0 ||
                    stampAnchor.x >= stampSize.x || stampAnchor.y >= stampSize.y)
                {
                    report.AddError(
                        placementId,
                        "Stamp AnchorLocalCell extraction bounds disinda. Anchor=" + stampAnchor +
                        " Size=" + stampSize);
                    continue;
                }

                try
                {
                    FantasyKingdomStampPreviewService.ValidateGridCompatibility(
                        placement.Stamp,
                        targetGrid);
                }
                catch (Exception exception)
                {
                    report.AddError(placementId, "Grid uyumsuz: " + exception.Message);
                    continue;
                }

                var runtime = new PlacementRuntime
                {
                    Placement = placement,
                    PlacementId = placementId,
                    PlacementIndex = placementIndex,
                    Origin = placement.TargetAnchorCell - placement.Stamp.AnchorLocalCell
                };

                IReadOnlyList<FantasyKingdomStampLayer> layers = placement.Stamp.Layers;
                bool containsSolidLayer = false;
                bool containsGroundLikeLayer = false;
                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer layer = layers[layerIndex];
                    bool solidLayer = IsSolidLayer(layer.SourceName);
                    containsSolidLayer |= solidLayer && layer.Cells.Count > 0;
                    containsGroundLikeLayer |= !solidLayer && layer.Cells.Count > 0;
                    IReadOnlyList<FantasyKingdomStampCell> cells = layer.Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        Vector3Int targetCell = runtime.Origin + cells[cellIndex].LocalPosition;
                        runtime.AllCells.Add(targetCell);
                        allPreviewCells.Add(targetCell);
                        if (solidLayer)
                        {
                            runtime.SolidCells.Add(targetCell);
                            allSolidCells.Add(targetCell);
                        }
                        else
                        {
                            runtime.GroundLikeCells.Add(targetCell);
                        }

                        var layerCellKey = new LayerCellKey(layer.SourceName, targetCell);
                        if (canonicalOwners.ContainsKey(layerCellKey))
                        {
                            report.CanonicalLayerConflictCount++;
                            Increment(canonicalConflictByPlacement, placementId);
                        }
                        else
                        {
                            canonicalOwners[layerCellKey] = placementId;
                        }
                    }
                }

                if (placement.RenderBand == FantasyKingdomRenderBand.Ground &&
                    containsSolidLayer)
                {
                    report.AddError(
                        placementId,
                        "Ground render band yalniz ground/shadow katmani tasiyabilir.");
                }
                else if ((placement.RenderBand == FantasyKingdomRenderBand.BehindUnits ||
                          placement.RenderBand == FantasyKingdomRenderBand.InFrontOfUnits) &&
                         containsGroundLikeLayer)
                {
                    report.AddError(
                        placementId,
                        placement.RenderBand +
                        " render band yalniz solid/gorsel obje katmani tasiyabilir.");
                }

                foreach (Vector3Int solidCell in runtime.SolidCells)
                {
                    if (solidOwners.TryGetValue(solidCell, out string solidOwner) &&
                        !string.Equals(solidOwner, placementId, StringComparison.Ordinal) &&
                        !IsIntentionalBackdropOverlap(solidOwner, placementId))
                    {
                        report.SolidFootprintConflictCount++;
                        Increment(solidConflictByPlacement, placementId);
                    }
                    else
                    {
                        solidOwners[solidCell] = placementId;
                    }
                }

                foreach (Vector3Int groundCell in runtime.GroundLikeCells)
                {
                    if (groundOwners.TryGetValue(groundCell, out string groundOwner) &&
                        !string.Equals(groundOwner, placementId, StringComparison.Ordinal))
                    {
                        report.GroundShadowOverlapCellCount++;
                        Increment(groundOverlapByPlacement, placementId);
                    }
                    else
                    {
                        groundOwners[groundCell] = placementId;
                    }
                }

                if (placement.GameplayAnchor != FantasyKingdomGameplayAnchor.None)
                {
                    if (anchorOwners.TryGetValue(placement.GameplayAnchor, out string anchorOwner))
                    {
                        report.AddError(
                            placementId,
                            placement.GameplayAnchor + " anchor'i zaten " + anchorOwner + " tarafindan kullaniliyor.");
                    }
                    else
                    {
                        anchorOwners.Add(placement.GameplayAnchor, placementId);
                    }
                }

                report.StampTileCount += placement.Stamp.TotalTileCount;
                report.RenderablePlacementCount++;
                runtimes.Add(runtime);
            }

            report.UniquePreviewCellCount = allPreviewCells.Count;
            report.SolidPreviewCellCount = allSolidCells.Count;

            if (report.EnabledPlacementCount == 0)
                report.AddError(null, "Layout'ta enabled placement yok.");

            foreach (KeyValuePair<string, int> conflict in canonicalConflictByPlacement)
            {
                report.AddError(
                    conflict.Key,
                    conflict.Value + " canonical source-layer/cell cakismasi var.");
            }
            foreach (KeyValuePair<string, int> conflict in solidConflictByPlacement)
            {
                report.AddError(
                    conflict.Key,
                    conflict.Value + " solid footprint hucresi baska placement ile cakismis.");
            }
            foreach (KeyValuePair<string, int> overlap in groundOverlapByPlacement)
            {
                report.AddWarning(
                    overlap.Key,
                    overlap.Value + " ground/shadow hucresi baska placement ile ortusuyor.");
            }

            AnalyzePlacementZones(targetGrid, runtimes, report);
            AnalyzeExistingTilemaps(targetGrid, runtimes, report);
            AnalyzeMarkersAndCorridors(targetGrid, runtimes, report, anchorOwners);
            if (layout.SchemaVersion >= FantasyKingdomMapLayout.CurrentSchemaVersion)
                AnalyzeV3SemanticContracts(targetGrid, runtimes, report);
            report.BoundGameplayAnchorCount = anchorOwners.Count;
            return report;
        }

        private static void AnalyzePlacementZones(
            Grid targetGrid,
            List<PlacementRuntime> runtimes,
            FantasyKingdomFullMapPreviewReport report)
        {
            for (int i = 0; i < runtimes.Count; i++)
            {
                PlacementRuntime runtime = runtimes[i];
                FantasyKingdomMapPlacement placement = runtime.Placement;
                int restricted = 0;
                int cameraOutside = 0;
                int referenceViewportOutside = 0;

                bool purposeValid = IsPurposeValidForZone(
                    placement.Stamp.Purpose,
                    placement.Zone);
                if (!purposeValid)
                {
                    report.AddError(
                        runtime.PlacementId,
                        placement.Stamp.Purpose + " purpose, " + placement.Zone + " zone ile uyumsuz.");
                }

                foreach (Vector3Int cell in runtime.AllCells)
                {
                    Vector3 world = targetGrid.GetCellCenterWorld(cell);
                    bool outsideSupportedCamera =
                        world.x < SupportedCameraMinX || world.x > SupportedCameraMaxX ||
                        world.y < CameraMinY || world.y > CameraMaxY;
                    if (outsideSupportedCamera &&
                        !IsIntentionalRightOverflow(placement.Zone, world))
                    {
                        cameraOutside++;
                    }

                    bool outsideReferenceViewport =
                        world.x < ReferenceCameraMinX || world.x > ReferenceCameraMaxX ||
                        world.y < CameraMinY || world.y > CameraMaxY;
                    if (placement.Zone == FantasyKingdomMapZone.Settlement &&
                        outsideReferenceViewport)
                    {
                        referenceViewportOutside++;
                    }

                    bool solid = runtime.SolidCells.Contains(cell);
                    if (!IsCellAllowedInZone(placement.Zone, world, solid))
                        restricted++;
                }

                if (restricted > 0)
                {
                    report.RestrictedZoneCellCount += restricted;
                    report.AddError(
                        runtime.PlacementId,
                        restricted + " cell " + placement.Zone + " zone kontratini ihlal ediyor.");
                }
                if (cameraOutside > 0)
                {
                    report.OutOfCameraCellCount += cameraOutside;
                    report.AddWarning(
                        runtime.PlacementId,
                        cameraOutside + " cell Game Camera gorunur sinirlarinin disinda.");
                }
                if (referenceViewportOutside > 0)
                {
                    report.ReferenceViewportOutsideCellCount += referenceViewportOutside;
                    report.AddWarning(
                        runtime.PlacementId,
                        referenceViewportOutside +
                        " settlement cell 16:9 referans viewport sinirlarinin disinda.");
                }
            }
        }

        private static void AnalyzeExistingTilemaps(
            Grid targetGrid,
            List<PlacementRuntime> runtimes,
            FantasyKingdomFullMapPreviewReport report)
        {
            var previewGroundSupportCells = new HashSet<Vector3Int>(
                runtimes.SelectMany(runtime => runtime.GroundLikeCells));
            Tilemap[] targetMaps = targetGrid.GetComponentsInChildren<Tilemap>(true)
                .Where(map => !IsInsideAnyPreviewRoot(map.transform) &&
                              !IsInsidePersistentV3Root(map.transform))
                .ToArray();

            for (int runtimeIndex = 0; runtimeIndex < runtimes.Count; runtimeIndex++)
            {
                PlacementRuntime runtime = runtimes[runtimeIndex];
                var protectedCells = new HashSet<Vector3Int>();
                var legacyVisualCells = new HashSet<Vector3Int>();
                var unknownCells = new HashSet<Vector3Int>();
                var groundSupportCells = new HashSet<Vector3Int>(
                    runtime.SolidCells.Where(previewGroundSupportCells.Contains));

                for (int mapIndex = 0; mapIndex < targetMaps.Length; mapIndex++)
                {
                    Tilemap map = targetMaps[mapIndex];
                    foreach (Vector3Int cell in runtime.AllCells)
                    {
                        if (!map.HasTile(cell))
                            continue;

                        if (GroundSupportLayerNames.Contains(map.name))
                        {
                            if (runtime.SolidCells.Contains(cell))
                                groundSupportCells.Add(cell);
                        }
                        else if (ProtectedLayerNames.Contains(map.name))
                        {
                            protectedCells.Add(cell);
                        }
                        else if (LegacyVisualLayerNames.Contains(map.name))
                        {
                            legacyVisualCells.Add(cell);
                        }
                        else
                        {
                            unknownCells.Add(cell);
                        }
                    }
                }

                if (protectedCells.Count > 0)
                {
                    report.ProtectedOverlapCellCount += protectedCells.Count;
                    report.AddError(
                        runtime.PlacementId,
                        protectedCells.Count + " cell outside* okcu-slot katmanlariyla cakismis.");
                }
                if (legacyVisualCells.Count > 0)
                {
                    report.LegacyVisualOverlapCellCount += legacyVisualCells.Count;
                    report.AddWarning(
                        runtime.PlacementId,
                        legacyVisualCells.Count +
                        " cell planli legacy gorsel emeklilik katmanlariyla cakismis.");
                }
                if (unknownCells.Count > 0)
                {
                    report.UnknownExistingOverlapCellCount += unknownCells.Count;
                    report.AddError(
                        runtime.PlacementId,
                        unknownCells.Count + " cell allowlist disi mevcut tilemap verisiyle cakismis.");
                }

                bool requiresSupport = runtime.Placement.Stamp.Purpose ==
                                           FantasyKingdomStampPurpose.Structure ||
                                       runtime.Placement.Stamp.Purpose ==
                                           FantasyKingdomStampPurpose.ResourceSite;
                if (!requiresSupport)
                    continue;

                int missingSupport = runtime.SolidCells.Count - groundSupportCells.Count;
                if (missingSupport > 0)
                {
                    report.MissingGroundSupportCellCount += missingSupport;
                    report.AddError(
                        runtime.PlacementId,
                        missingSupport + " solid cell mevcut Grass/Ground destegi disinda.");
                }
            }
        }

        private static void AnalyzeMarkersAndCorridors(
            Grid targetGrid,
            List<PlacementRuntime> runtimes,
            FantasyKingdomFullMapPreviewReport report,
            Dictionary<FantasyKingdomGameplayAnchor, string> anchorOwners)
        {
            Dictionary<FantasyKingdomGameplayAnchor, Transform> markers =
                FindGameplayMarkers(targetGrid.gameObject.scene);

            foreach (KeyValuePair<FantasyKingdomGameplayAnchor, string> expected in MarkerNames)
            {
                if (!markers.ContainsKey(expected.Key))
                    report.AddError(null, "Gameplay marker bulunamadi: " + expected.Value);
            }

            for (int runtimeIndex = 0; runtimeIndex < runtimes.Count; runtimeIndex++)
            {
                PlacementRuntime runtime = runtimes[runtimeIndex];
                var markerConflictCells = new HashSet<Vector3Int>();
                var markerConflictNames = new HashSet<string>(StringComparer.Ordinal);
                var corridorCells = new HashSet<Vector3Int>();
                var corridorNames = new HashSet<string>(StringComparer.Ordinal);

                FantasyKingdomGameplayAnchor ownedAnchor =
                    runtime.Placement.GameplayAnchor;
                if (ownedAnchor != FantasyKingdomGameplayAnchor.None &&
                    markers.TryGetValue(ownedAnchor, out Transform ownedMarker))
                {
                    Vector3Int markerCell = targetGrid.WorldToCell(ownedMarker.position);
                    Vector3Int placementCell = runtime.Placement.TargetAnchorCell;
                    int anchorDistance = Mathf.Max(
                        Mathf.Abs(markerCell.x - placementCell.x),
                        Mathf.Abs(markerCell.y - placementCell.y));
                    if (anchorDistance > 1)
                    {
                        report.AddError(
                            runtime.PlacementId,
                            ownedAnchor + " semantic anchor hedefi marker'dan " +
                            anchorDistance + " hucre uzakta. Marker=" + markerCell +
                            " Placement=" + placementCell);
                    }
                }

                foreach (Vector3Int solidCell in runtime.SolidCells)
                {
                    foreach (KeyValuePair<FantasyKingdomGameplayAnchor, Transform> marker in markers)
                    {
                        if (anchorOwners.TryGetValue(marker.Key, out string ownerId) &&
                            string.Equals(ownerId, runtime.PlacementId, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        if (IsIntentionalMarkerBackdropOverlap(
                                runtime.PlacementId,
                                marker.Key))
                        {
                            continue;
                        }

                        Vector3Int markerCell = targetGrid.WorldToCell(marker.Value.position);
                        if (Mathf.Abs(solidCell.x - markerCell.x) <= 1 &&
                            Mathf.Abs(solidCell.y - markerCell.y) <= 1)
                        {
                            markerConflictCells.Add(solidCell);
                            markerConflictNames.Add(marker.Value.name);
                        }
                    }
                }

                if (markers.TryGetValue(
                        FantasyKingdomGameplayAnchor.CastleKeep,
                        out Transform keepMarker))
                {
                    foreach (KeyValuePair<FantasyKingdomGameplayAnchor, Transform> marker in markers)
                    {
                        if (marker.Key == FantasyKingdomGameplayAnchor.CastleKeep)
                            continue;

                        Vector3 start = marker.Value.position;
                        Vector3 end = keepMarker.position;
                        TrimSegmentEndpoints(ref start, ref end, CorridorEndpointClearance);
                        foreach (Vector3Int solidCell in runtime.SolidCells)
                        {
                            Vector3 world = targetGrid.GetCellCenterWorld(solidCell);
                            if (DistancePointToSegment2D(world, start, end) > CorridorRadius)
                                continue;
                            corridorCells.Add(solidCell);
                            corridorNames.Add(marker.Key.ToString());
                        }
                    }
                }

                if (markerConflictCells.Count > 0)
                {
                    report.MarkerConflictCellCount += markerConflictCells.Count;
                    report.AddError(
                        runtime.PlacementId,
                        markerConflictCells.Count + " solid cell marker 3x3 koruma alaninda: " +
                        string.Join(", ", markerConflictNames.ToArray()));
                }
                if (corridorCells.Count > 0)
                {
                    report.CorridorRiskCellCount += corridorCells.Count;
                    report.AddWarning(
                        runtime.PlacementId,
                        corridorCells.Count + " solid cell marker->keep duz referans hattina yakin (" +
                        string.Join(", ", corridorNames.ToArray()) +
                        "). Tam yapi butunlugu korunacak; kalici apply oncesi route-bazli " +
                        "koridor kontrolu gerekli.");
                }
            }

            foreach (KeyValuePair<FantasyKingdomGameplayAnchor, string> expected in MarkerNames)
            {
                if (!anchorOwners.ContainsKey(expected.Key))
                {
                    report.AddWarning(
                        null,
                        expected.Key + " icin layout'ta semantik placement anchor'i yok.");
                }
            }
        }

        private static void AnalyzeV3SemanticContracts(
            Grid targetGrid,
            List<PlacementRuntime> runtimes,
            FantasyKingdomFullMapPreviewReport report)
        {
            PlacementRuntime livingForest = FindRuntime(runtimes, "left.wood.living_forest");
            PlacementRuntime castle = FindRuntime(runtimes, "left.castle.citadel");
            PlacementRuntime enemyShadow = FindRuntime(runtimes, "enemy_forest.shadow");
            PlacementRuntime enemyBack = FindRuntime(runtimes, "enemy_forest.back");
            PlacementRuntime enemyFront = FindRuntime(runtimes, "enemy_forest.front");

            report.LivingForestTreeCount = CountStampTilesByPrefix(livingForest, "Tree");
            report.EnemyForestBackTreeCount = CountStampTilesByPrefix(enemyBack, "Tree");
            report.EnemyForestFrontTreeCount = CountStampTilesByPrefix(enemyFront, "Tree");

            if (report.LivingForestTreeCount < 30)
                report.AddError("left.wood.living_forest", "Canli orman en az 30 agac tasimalidir.");
            if (report.EnemyForestBackTreeCount < 120)
                report.AddError("enemy_forest.back", "Dusman ormani back mass en az 120 agac tasimalidir.");
            if (report.EnemyForestFrontTreeCount < 40)
                report.AddError("enemy_forest.front", "Front occluder en az 40 agac tasimalidir.");

            bool isRetouchCandidate = castle != null &&
                                      castle.Placement.Stamp != null &&
                                      castle.Placement.Stamp.name.EndsWith(
                                          "_RetouchPreview",
                                          StringComparison.Ordinal);
            if (isRetouchCandidate)
            {
                ValidateRetouchCastleRoofCaps(castle, report);

                int castleForestOverlap = castle.SolidCells.Count(
                    cell => livingForest != null && livingForest.SolidCells.Contains(cell));
                if (castleForestOverlap > 0)
                {
                    report.AddError(
                        "left.wood.living_forest",
                        "Retouch candidate kale ile " + castleForestOverlap +
                        " ayni solid hucreyi paylasiyor; beklenen 0.");
                }

                int leaflessFrontTrees = CountStampTilesByPrefix(enemyFront, "Tree E1") +
                                         CountStampTilesByPrefix(enemyFront, "Tree E3");
                if (leaflessFrontTrees > 0)
                {
                    report.AddError(
                        "enemy_forest.front",
                        "Retouch front occluder yapraksiz Tree E1/E3 tasiyor: " +
                        leaflessFrontTrees);
                }

                int treeShadows = CountStampTilesByPrefix(enemyShadow, "Tree");
                if (treeShadows > 0)
                {
                    report.AddError(
                        "enemy_forest.shadow",
                        "Retouch shadow katmaninda siyah tree kopyasi var: " + treeShadows);
                }
            }

            var coveredBands = new HashSet<int>();
            if (enemyFront != null)
            {
                foreach (Vector3Int cell in enemyFront.SolidCells)
                {
                    float worldY = targetGrid.GetCellCenterWorld(cell).y;
                    int band = Mathf.FloorToInt(worldY);
                    if (band < -8 || band >= 8 || band == 2 || band == 3)
                        continue;
                    coveredBands.Add(band);
                }
            }
            report.EnemyFrontCoveredYBandCount = coveredBands.Count;
            if (coveredBands.Count < 12)
            {
                report.AddError(
                    "enemy_forest.front",
                    "Front occluder, yol agzi disindaki Y bantlarini yeterince kaplamiyor: " +
                    coveredBands.Count + "/14.");
            }

            var roadCells = new HashSet<Vector3Int>();
            int roadStraightRutCellCount = 0;
            for (int runtimeIndex = 0; runtimeIndex < runtimes.Count; runtimeIndex++)
            {
                PlacementRuntime runtime = runtimes[runtimeIndex];
                IReadOnlyList<FantasyKingdomStampLayer> layers = runtime.Placement.Stamp.Layers;
                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    IReadOnlyList<FantasyKingdomStampCell> cells = layers[layerIndex].Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        FantasyKingdomStampCell cell = cells[cellIndex];
                        if (cell.Tile == null || cell.Tile.name == null ||
                            !cell.Tile.name.StartsWith(
                                "Ground I",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        Vector3Int targetCell = runtime.Origin + cell.LocalPosition;
                        float minimumRoadWorldX = isRetouchCandidate ? -0.5f : 4f;
                        if (targetGrid.GetCellCenterWorld(targetCell).x < minimumRoadWorldX ||
                            !roadCells.Add(targetCell))
                        {
                            continue;
                        }

                        if (cell.Tile.name.StartsWith(
                                     "Ground I1_",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            roadStraightRutCellCount++;
                        }
                    }
                }
            }
            report.CaravanRoadCellCount = roadCells.Count;
            report.CaravanRoadComponentCount = CountConnectedComponents4(roadCells);
            int minimumRoadCellCount = isRetouchCandidate ? 40 : 70;
            if (roadCells.Count < minimumRoadCellCount)
            {
                report.AddError(
                    "battlefield.calm_ground",
                    "Kervan yolu en az " + minimumRoadCellCount +
                    " bagli hucresel iz tasimalidir.");
            }
            if (report.CaravanRoadComponentCount != 1)
            {
                report.AddError(
                    "battlefield.calm_ground",
                    "Kervan yolu tek bagli component olmalidir. Mevcut=" +
                    report.CaravanRoadComponentCount);
            }
            if (isRetouchCandidate && roadCells.Count > 0)
            {
                int straightRutPercent = Mathf.RoundToInt(
                    roadStraightRutCellCount * 100f / roadCells.Count);
                if (straightRutPercent < 65 || straightRutPercent > 95)
                {
                    report.AddError(
                        "battlefield.calm_ground",
                        "Retouch tek-iz yolunda Ground I1 orani referans dilinden sapti: %" +
                        straightRutPercent + " (beklenen %65..%95)." );
                }
            }

            var centerSolidCells = new HashSet<Vector3Int>();
            for (int runtimeIndex = 0; runtimeIndex < runtimes.Count; runtimeIndex++)
            {
                foreach (Vector3Int cell in runtimes[runtimeIndex].SolidCells)
                {
                    Vector3 world = targetGrid.GetCellCenterWorld(cell);
                    if (world.x >= 6f && world.x <= 17f &&
                        world.y >= -5f && world.y <= 5f)
                    {
                        centerSolidCells.Add(cell);
                    }
                }
            }
            report.OpenBattlefieldSolidCellCount = centerSolidCells.Count;
            if (centerSolidCells.Count > 0)
            {
                report.AddError(
                    null,
                    "Acik savas merkezi x=6..17/y=-5..5 icinde " +
                    centerSolidCells.Count + " solid hucresi var.");
            }
        }

        private static PlacementRuntime FindRuntime(
            List<PlacementRuntime> runtimes,
            string placementId)
        {
            return runtimes.FirstOrDefault(runtime => string.Equals(
                runtime.PlacementId,
                placementId,
                StringComparison.Ordinal));
        }

        private static int CountStampTilesByPrefix(PlacementRuntime runtime, string prefix)
        {
            if (runtime == null)
                return 0;
            int count = 0;
            IReadOnlyList<FantasyKingdomStampLayer> layers = runtime.Placement.Stamp.Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                IReadOnlyList<FantasyKingdomStampCell> cells = layers[layerIndex].Cells;
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    TileBase tile = cells[cellIndex].Tile;
                    if (tile != null && tile.name != null &&
                        tile.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private static void ValidateRetouchCastleRoofCaps(
            PlacementRuntime castle,
            FantasyKingdomFullMapPreviewReport report)
        {
            var roofCells = new HashSet<Vector3Int>();
            bool touchesExtractionEdge = false;
            Vector2Int sourceSize = castle.Placement.Stamp.SourceRegionSize;
            IReadOnlyList<FantasyKingdomStampLayer> layers = castle.Placement.Stamp.Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                FantasyKingdomStampLayer layer = layers[layerIndex];
                if (!string.Equals(layer.SourceName, "Roof1", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(layer.SourceName, "Roof2", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(layer.SourceName, "Roof3", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                IReadOnlyList<FantasyKingdomStampCell> cells = layer.Cells;
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    Vector3Int local = cells[cellIndex].LocalPosition;
                    roofCells.Add(castle.Origin + local);
                    if (local.x <= 0 || local.y <= 0 ||
                        local.x >= sourceSize.x - 1 || local.y >= sourceSize.y - 1)
                    {
                        touchesExtractionEdge = true;
                    }
                }
            }

            if (touchesExtractionEdge)
            {
                report.AddError(
                    "left.castle.citadel",
                    "Retouch kale roof hucreleri extraction sinirina degiyor; crop riski devam ediyor.");
            }

            Vector3Int[] lowerRightTowerCap =
            {
                new Vector3Int(8, 13, 0), new Vector3Int(9, 13, 0),
                new Vector3Int(8, 14, 0), new Vector3Int(9, 14, 0),
                new Vector3Int(8, 15, 0), new Vector3Int(9, 15, 0),
                new Vector3Int(10, 15, 0), new Vector3Int(11, 15, 0),
                new Vector3Int(10, 16, 0), new Vector3Int(10, 17, 0)
            };
            Vector3Int[] upperRightTowerCap =
            {
                new Vector3Int(8, 23, 0), new Vector3Int(9, 23, 0),
                new Vector3Int(8, 24, 0), new Vector3Int(9, 24, 0),
                new Vector3Int(8, 25, 0), new Vector3Int(9, 25, 0),
                new Vector3Int(10, 25, 0), new Vector3Int(11, 25, 0),
                new Vector3Int(10, 26, 0), new Vector3Int(10, 27, 0)
            };

            ValidateTowerCapCells(
                roofCells,
                lowerRightTowerCap,
                "alt sag kule",
                report);
            ValidateTowerCapCells(
                roofCells,
                upperRightTowerCap,
                "ust sag kule",
                report);
        }

        private static void ValidateTowerCapCells(
            HashSet<Vector3Int> roofCells,
            Vector3Int[] requiredCells,
            string towerLabel,
            FantasyKingdomFullMapPreviewReport report)
        {
            var missing = new List<Vector3Int>();
            for (int index = 0; index < requiredCells.Length; index++)
            {
                if (!roofCells.Contains(requiredCells[index]))
                    missing.Add(requiredCells[index]);
            }
            if (missing.Count == 0)
                return;

            report.AddError(
                "left.castle.citadel",
                "Retouch " + towerLabel + " roof-cap eksik: " + missing.Count +
                "/" + requiredCells.Length + " hucre bulunamadi (" +
                string.Join(", ", missing.Select(cell => cell.ToString()).ToArray()) + ").");
        }

        private static int CountConnectedComponents4(HashSet<Vector3Int> cells)
        {
            var unvisited = new HashSet<Vector3Int>(cells);
            int componentCount = 0;
            var queue = new Queue<Vector3Int>();
            while (unvisited.Count > 0)
            {
                Vector3Int seed = unvisited.First();
                unvisited.Remove(seed);
                queue.Enqueue(seed);
                componentCount++;
                while (queue.Count > 0)
                {
                    Vector3Int current = queue.Dequeue();
                    Vector3Int[] neighbors =
                    {
                        current + Vector3Int.up,
                        current + Vector3Int.right,
                        current + Vector3Int.down,
                        current + Vector3Int.left
                    };
                    for (int neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++)
                    {
                        if (!unvisited.Remove(neighbors[neighborIndex]))
                            continue;
                        queue.Enqueue(neighbors[neighborIndex]);
                    }
                }
            }
            return componentCount;
        }

        private static void RenderPreview(Grid targetGrid, List<PlacementRuntime> runtimes)
        {
            Scene targetScene = targetGrid.gameObject.scene;
            TilemapRenderer groundReference = FindReferenceRenderer(
                targetGrid,
                "GroundDetail",
                "Grass",
                "Ground");
            TilemapRenderer behindReference = FindReferenceRenderer(
                targetGrid,
                "Structures",
                "outside0",
                "outside");
            TilemapRenderer frontReference = FindReferenceRenderer(
                targetGrid,
                "outside2",
                "Structures");
            var root = new GameObject(PreviewRootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild,
                layer = targetGrid.gameObject.layer
            };
            SceneManager.MoveGameObjectToScene(root, targetScene);
            root.transform.SetParent(targetGrid.transform, false);

            for (int runtimeIndex = 0; runtimeIndex < runtimes.Count; runtimeIndex++)
            {
                PlacementRuntime runtime = runtimes[runtimeIndex];
                var placementObject = new GameObject(
                    string.Format("{0:00}_{1}", runtimeIndex, SanitizeName(runtime.PlacementId)))
                {
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild,
                    layer = targetGrid.gameObject.layer
                };
                placementObject.transform.SetParent(root.transform, false);
                placementObject.transform.localPosition = new Vector3(
                    0f,
                    0f,
                    ResolveRenderBandZ(runtime.Placement.RenderBand));

                IReadOnlyList<FantasyKingdomStampLayer> layers = runtime.Placement.Stamp.Layers;
                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer sourceLayer = layers[layerIndex];
                    var layerObject = new GameObject(
                        string.Format("{0:00}_{1}", layerIndex, sourceLayer.SourceName))
                    {
                        hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild,
                        layer = targetGrid.gameObject.layer
                    };
                    layerObject.transform.SetParent(placementObject.transform, false);

                    var tilemap = layerObject.AddComponent<Tilemap>();
                    var renderer = layerObject.AddComponent<TilemapRenderer>();
                    tilemap.tileAnchor = sourceLayer.TileAnchor;
                    tilemap.color = sourceLayer.LayerColor;
                    tilemap.orientation = sourceLayer.Orientation;
                    tilemap.orientationMatrix = sourceLayer.OrientationMatrix;
                    renderer.mode = runtime.Placement.RenderBand == FantasyKingdomRenderBand.BehindUnits ||
                                    runtime.Placement.RenderBand == FantasyKingdomRenderBand.InFrontOfUnits
                        ? TilemapRenderer.Mode.Individual
                        : sourceLayer.RendererMode;
                    renderer.sortOrder = sourceLayer.SortOrder;
                    ResolvePreviewSorting(
                        sourceLayer.SourceName,
                        sourceLayer.SortingLayerName,
                        sourceLayer.SortingOrder,
                        runtime.Placement.RenderBand,
                        runtime.PlacementIndex,
                        layerIndex,
                        out string sortingLayer,
                        out int sortingOrder);
                    renderer.sortingLayerName = sortingLayer;
                    renderer.sortingOrder = sortingOrder;
                    TilemapRenderer materialReference = runtime.Placement.RenderBand ==
                                                        FantasyKingdomRenderBand.Ground ||
                                                        (runtime.Placement.RenderBand ==
                                                         FantasyKingdomRenderBand.LegacyAuto &&
                                                         !IsSolidLayer(sourceLayer.SourceName))
                        ? groundReference
                        : runtime.Placement.RenderBand == FantasyKingdomRenderBand.InFrontOfUnits
                            ? frontReference
                            : behindReference;
                    if (materialReference != null)
                        renderer.sharedMaterial = materialReference.sharedMaterial;

                    IReadOnlyList<FantasyKingdomStampCell> cells = sourceLayer.Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        FantasyKingdomStampCell sourceCell = cells[cellIndex];
                        Vector3Int targetCell = runtime.Origin + sourceCell.LocalPosition;
                        tilemap.SetTile(targetCell, sourceCell.Tile);
                        tilemap.SetTileFlags(targetCell, TileFlags.None);
                        tilemap.SetTransformMatrix(targetCell, sourceCell.TransformMatrix);
                        tilemap.SetColor(targetCell, sourceCell.Color);
                        tilemap.SetTileFlags(targetCell, sourceCell.Flags);
                    }
                    tilemap.CompressBounds();
                }
            }
        }

        private static void ValidateTarget(FantasyKingdomMapLayout layout, Grid targetGrid)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (targetGrid == null)
                throw new ArgumentNullException(nameof(targetGrid));
            if (!targetGrid.gameObject.scene.IsValid() || !targetGrid.gameObject.scene.isLoaded)
                throw new InvalidOperationException("Target Grid yuklu bir sahnede olmalidir.");
            if (layout.SchemaVersion < FantasyKingdomMapLayout.MinimumSupportedSchemaVersion ||
                layout.SchemaVersion > FantasyKingdomMapLayout.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "Layout schema uyumsuz. Desteklenen=" +
                    FantasyKingdomMapLayout.MinimumSupportedSchemaVersion + ".." +
                    FantasyKingdomMapLayout.CurrentSchemaVersion +
                    " Mevcut=" + layout.SchemaVersion);
            if (string.IsNullOrWhiteSpace(layout.ProfileId))
                throw new InvalidOperationException("Layout profileId bos olamaz.");

            string targetScenePath = targetGrid.gameObject.scene.path.Replace('\\', '/');
            string layoutScenePath = layout.TargetScenePath.Replace('\\', '/');
            if (string.IsNullOrEmpty(layoutScenePath) ||
                !string.Equals(targetScenePath, layoutScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Layout/scene hedefi uyusmuyor. Layout=" + layoutScenePath +
                    " Active=" + targetScenePath);
            }

            string gridPath = GetHierarchyPath(targetGrid.transform);
            if (!string.Equals(gridPath, layout.TargetGridPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Layout/Grid path uyusmuyor. Layout=" + layout.TargetGridPath +
                    " Target=" + gridPath);
            }
        }

        private static bool IsPurposeValidForZone(
            FantasyKingdomStampPurpose purpose,
            FantasyKingdomMapZone zone)
        {
            switch (zone)
            {
                case FantasyKingdomMapZone.Settlement:
                    return purpose == FantasyKingdomStampPurpose.Structure ||
                           purpose == FantasyKingdomStampPurpose.ResourceSite ||
                           purpose == FantasyKingdomStampPurpose.GroundDetail;
                case FantasyKingdomMapZone.Battlefield:
                    return purpose == FantasyKingdomStampPurpose.BattlefieldDecoration ||
                           purpose == FantasyKingdomStampPurpose.GroundDetail;
                case FantasyKingdomMapZone.FarRightFrame:
                    return purpose == FantasyKingdomStampPurpose.BattlefieldDecoration ||
                           purpose == FantasyKingdomStampPurpose.GroundDetail;
                case FantasyKingdomMapZone.MoatGround:
                case FantasyKingdomMapZone.SpawnGround:
                case FantasyKingdomMapZone.FullMapGround:
                    return purpose == FantasyKingdomStampPurpose.GroundDetail;
                default:
                    return false;
            }
        }

        private static bool IsCellAllowedInZone(
            FantasyKingdomMapZone zone,
            Vector3 world,
            bool solid)
        {
            switch (zone)
            {
                case FantasyKingdomMapZone.Settlement:
                    return !solid || world.x <= SettlementSolidMaxX + 0.001f;
                case FantasyKingdomMapZone.Battlefield:
                    return world.x >= BattlefieldMinX - 0.001f &&
                           world.x <= BattlefieldMaxX + 0.001f;
                case FantasyKingdomMapZone.FarRightFrame:
                    return world.x >= FarRightFrameMinX - 0.001f &&
                           world.x <= FarRightFrameMaxX + 0.001f;
                case FantasyKingdomMapZone.MoatGround:
                    return !solid && world.x >= MoatMinX - 0.001f && world.x <= MoatMaxX + 0.001f;
                case FantasyKingdomMapZone.SpawnGround:
                    return !solid && world.x >= SpawnMinX - 0.001f &&
                           world.x <= SpawnMaxX + 0.001f &&
                           Mathf.Abs(world.y) <= SpawnMaxAbsY + 0.001f;
                case FantasyKingdomMapZone.FullMapGround:
                    return !solid && world.x >= -0.501f &&
                           world.x <= FarRightFrameMaxX + 0.001f &&
                           world.y >= CameraMinY - 0.5f &&
                           world.y <= CameraMaxY + 0.5f;
                default:
                    return false;
            }
        }

        private static bool IsIntentionalRightOverflow(
            FantasyKingdomMapZone zone,
            Vector3 world)
        {
            if (world.y < CameraMinY || world.y > CameraMaxY ||
                world.x <= SupportedCameraMaxX)
            {
                return false;
            }

            if (zone == FantasyKingdomMapZone.FarRightFrame)
                return world.x <= FarRightFrameMaxX + 0.001f;
            if (zone == FantasyKingdomMapZone.FullMapGround)
                return world.x <= FarRightFrameMaxX + 0.001f;
            if (zone == FantasyKingdomMapZone.SpawnGround)
                return world.x >= SpawnMinX - 0.001f &&
                       world.x <= SpawnMaxX + 0.001f;
            return false;
        }

        private static bool IsSolidLayer(string sourceName)
        {
            string normalized = sourceName != null
                ? sourceName.ToLowerInvariant()
                : string.Empty;
            return !normalized.Contains("ground") && !normalized.Contains("shadow");
        }

        private static bool IsIntentionalBackdropOverlap(string firstId, string secondId)
        {
            return IsPlacementPair(
                firstId,
                secondId,
                "left.castle.citadel",
                "left.wood.living_forest");
        }

        private static bool IsIntentionalMarkerBackdropOverlap(
            string placementId,
            FantasyKingdomGameplayAnchor markerAnchor)
        {
            return string.Equals(
                       placementId,
                       "left.castle.citadel",
                       StringComparison.Ordinal) &&
                   markerAnchor == FantasyKingdomGameplayAnchor.Wood ||
                   string.Equals(
                       placementId,
                       "left.wood.living_forest",
                       StringComparison.Ordinal) &&
                   markerAnchor == FantasyKingdomGameplayAnchor.CastleKeep;
        }

        private static bool IsPlacementPair(
            string firstId,
            string secondId,
            string expectedFirst,
            string expectedSecond)
        {
            return string.Equals(firstId, expectedFirst, StringComparison.Ordinal) &&
                   string.Equals(secondId, expectedSecond, StringComparison.Ordinal) ||
                   string.Equals(firstId, expectedSecond, StringComparison.Ordinal) &&
                   string.Equals(secondId, expectedFirst, StringComparison.Ordinal);
        }

        private static Dictionary<FantasyKingdomGameplayAnchor, Transform> FindGameplayMarkers(
            Scene scene)
        {
            var result = new Dictionary<FantasyKingdomGameplayAnchor, Transform>();
            GameObject root = scene.GetRootGameObjects()
                .FirstOrDefault(item => string.Equals(
                    item.name,
                    "VillageMarkers",
                    StringComparison.Ordinal));
            if (root == null)
                return result;

            foreach (KeyValuePair<FantasyKingdomGameplayAnchor, string> marker in MarkerNames)
            {
                Transform child = root.transform.Find(marker.Value);
                if (child != null)
                    result.Add(marker.Key, child);
            }
            return result;
        }

        private static void TrimSegmentEndpoints(ref Vector3 start, ref Vector3 end, float clearance)
        {
            Vector3 delta = end - start;
            delta.z = 0f;
            float length = delta.magnitude;
            if (length <= clearance * 2f + 0.001f)
                return;
            Vector3 direction = delta / length;
            start += direction * clearance;
            end -= direction * clearance;
        }

        private static float DistancePointToSegment2D(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector2 p = new Vector2(point.x, point.y);
            Vector2 a = new Vector2(start.x, start.y);
            Vector2 b = new Vector2(end.x, end.y);
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
                return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
            return Vector2.Distance(p, a + ab * t);
        }

        private static void ResolvePreviewSorting(
            string sourceName,
            string sourceSortingLayer,
            int sourceSortingOrder,
            FantasyKingdomRenderBand renderBand,
            int placementIndex,
            int layerIndex,
            out string sortingLayer,
            out int sortingOrder)
        {
            string normalized = sourceName != null
                ? sourceName.ToLowerInvariant()
                : string.Empty;
            bool groundLike = !IsSolidLayer(sourceName);

            int localOrder;
            if (normalized.Contains("lowershadow")) localOrder = 3;
            else if (normalized.Contains("ground")) localOrder = normalized.Contains("3") ? 5 : 4;
            else if (normalized.Contains("shadow")) localOrder = normalized.Contains("2") ? 7 : 6;
            else if (normalized.Contains("roof"))
                localOrder = normalized.Contains("3") ? 14 : normalized.Contains("2") ? 13 : 12;
            else if (normalized.Contains("wall")) localOrder = 10;
            else localOrder = 11;

            switch (renderBand)
            {
                case FantasyKingdomRenderBand.Ground:
                    sortingLayer = SortingLayerExists("Ground") ? "Ground" : "Default";
                    localOrder = Mathf.Clamp(localOrder, 1, 8);
                    sortingOrder = localOrder * 1000 + placementIndex * 10 + layerIndex;
                    return;
                case FantasyKingdomRenderBand.BehindUnits:
                    sortingLayer = SortingLayerExists("Objects") ? "Objects" :
                        SortingLayerExists(sourceSortingLayer) ? sourceSortingLayer : "Default";
                    localOrder = Mathf.Clamp(localOrder, 9, 14);
                    sortingOrder = localOrder * 1000 + placementIndex * 10 + layerIndex;
                    return;
                case FantasyKingdomRenderBand.InFrontOfUnits:
                    sortingLayer = SortingLayerExists("Wall") ? "Wall" :
                        SortingLayerExists(sourceSortingLayer) ? sourceSortingLayer : "Default";
                    sortingOrder = 4;
                    return;
                default:
                    sortingLayer = groundLike && SortingLayerExists("Ground") ? "Ground" :
                        SortingLayerExists("Objects") ? "Objects" :
                        SortingLayerExists(sourceSortingLayer) ? sourceSortingLayer : "Default";
                    sortingOrder = 100 + localOrder * 10 + placementIndex;
                    return;
            }
        }

        private static float ResolveRenderBandZ(FantasyKingdomRenderBand renderBand)
        {
            return renderBand == FantasyKingdomRenderBand.InFrontOfUnits
                ? InFrontOfUnitsZ
                : BehindUnitsZ;
        }

        private static TilemapRenderer FindReferenceRenderer(
            Grid targetGrid,
            params string[] preferredNames)
        {
            Tilemap[] maps = targetGrid.GetComponentsInChildren<Tilemap>(true)
                .Where(map => !IsInsideAnyPreviewRoot(map.transform) &&
                              !IsInsidePersistentV3Root(map.transform))
                .ToArray();
            for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
            {
                string preferredName = preferredNames[nameIndex];
                for (int mapIndex = 0; mapIndex < maps.Length; mapIndex++)
                {
                    if (!string.Equals(
                            maps[mapIndex].name,
                            preferredName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    TilemapRenderer renderer = maps[mapIndex].GetComponent<TilemapRenderer>();
                    if (renderer != null)
                        return renderer;
                }
            }
            return null;
        }

        private static bool SortingLayerExists(string layerName)
        {
            return SortingLayer.layers.Any(layer => string.Equals(
                layer.name,
                layerName,
                StringComparison.Ordinal));
        }

        private static void ClearPreviewInternal(Grid targetGrid)
        {
            RestoreLegacyObjectRenderers(targetGrid);
            ClearOcclusionProbe(targetGrid.gameObject.scene);
            DestroyPreviewRoots(targetGrid);
        }

        private static void ClearOcclusionProbe(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = roots.Length - 1; rootIndex >= 0; rootIndex--)
            {
                if (!string.Equals(
                        roots[rootIndex].name,
                        OcclusionProbeRootName,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                UnityEngine.Object.DestroyImmediate(roots[rootIndex]);
            }
        }

        private static void DestroyPreviewRoots(Grid targetGrid)
        {
            if (targetGrid == null)
                return;

            for (int childIndex = targetGrid.transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = targetGrid.transform.GetChild(childIndex);
                if (!string.Equals(child.name, PreviewRootName, StringComparison.Ordinal))
                    continue;

                Transform selected = Selection.activeTransform;
                if (selected == child || (selected != null && selected.IsChildOf(child)))
                    Selection.activeObject = null;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void SuppressLegacyObjectRenderers(Grid targetGrid)
        {
            RestoreLegacyObjectRenderers(targetGrid);

            var states = new Dictionary<TilemapRenderer, bool>();
            int gridId = targetGrid.GetInstanceID();
            LegacyRendererStatesByGrid[gridId] = states;

            try
            {
                Tilemap[] maps = targetGrid.GetComponentsInChildren<Tilemap>(true);
                for (int i = 0; i < maps.Length; i++)
                {
                    Tilemap map = maps[i];
                    bool managedPersistentV3 = IsInsidePersistentV3Root(map.transform);
                    if ((!LegacyObjectLayerNames.Contains(map.name) && !managedPersistentV3) ||
                        IsInsideAnyPreviewRoot(map.transform))
                    {
                        continue;
                    }

                    TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
                    if (renderer == null)
                        continue;

                    states[renderer] = renderer.forceRenderingOff;
                    renderer.forceRenderingOff = true;
                }
            }
            catch
            {
                RestoreLegacyObjectRenderers(targetGrid);
                throw;
            }

            if (states.Count == 0)
                LegacyRendererStatesByGrid.Remove(gridId);
        }

        private static void RestoreLegacyObjectRenderers(Grid targetGrid)
        {
            if (targetGrid == null ||
                !LegacyRendererStatesByGrid.TryGetValue(
                    targetGrid.GetInstanceID(),
                    out Dictionary<TilemapRenderer, bool> states))
            {
                return;
            }

            LegacyRendererStatesByGrid.Remove(targetGrid.GetInstanceID());
            RestoreRendererStates(states);
        }

        private static void RestoreAllLegacyObjectRenderers()
        {
            Dictionary<TilemapRenderer, bool>[] allStates =
                LegacyRendererStatesByGrid.Values.ToArray();
            LegacyRendererStatesByGrid.Clear();

            for (int i = 0; i < allStates.Length; i++)
                RestoreRendererStates(allStates[i]);
        }

        private static void RestoreRendererStates(Dictionary<TilemapRenderer, bool> states)
        {
            Exception firstFailure = null;
            foreach (KeyValuePair<TilemapRenderer, bool> state in states)
            {
                if (state.Key == null)
                    continue;

                try
                {
                    state.Key.forceRenderingOff = state.Value;
                }
                catch (Exception exception)
                {
                    if (firstFailure == null)
                        firstFailure = exception;
                }
            }

            if (firstFailure != null)
                Debug.LogException(firstFailure);
        }

        private static void CleanupAllTransientPreviewState()
        {
            RestoreAllLegacyObjectRenderers();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                ClearOcclusionProbe(scene);

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    Grid[] grids = roots[rootIndex].GetComponentsInChildren<Grid>(true);
                    for (int gridIndex = 0; gridIndex < grids.Length; gridIndex++)
                        DestroyPreviewRoots(grids[gridIndex]);
                }
            }
        }

        private static void HandleSceneSaving(Scene scene, string path)
        {
            CleanupAllTransientPreviewState();
        }

        private static void HandleSceneClosing(Scene scene, bool removingScene)
        {
            CleanupAllTransientPreviewState();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                CleanupAllTransientPreviewState();
        }

        private static bool IsInsideAnyPreviewRoot(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, PreviewRootName, StringComparison.Ordinal) ||
                    string.Equals(
                        current.name,
                        FantasyKingdomStampPreviewService.PreviewRootName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static bool IsInsidePersistentV3Root(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, PersistentV3RootName, StringComparison.Ordinal) &&
                    current.parent != null &&
                    current.parent.GetComponent<Grid>() != null)
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names.ToArray());
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Placement";
            char[] characters = value.Select(character =>
                char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_').ToArray();
            return new string(characters);
        }

        private static void Increment(Dictionary<string, int> values, string key)
        {
            if (values.TryGetValue(key, out int count))
                values[key] = count + 1;
            else
                values.Add(key, 1);
        }

        private sealed class PlacementRuntime
        {
            public FantasyKingdomMapPlacement Placement;
            public string PlacementId;
            public int PlacementIndex;
            public Vector3Int Origin;
            public readonly HashSet<Vector3Int> AllCells = new HashSet<Vector3Int>();
            public readonly HashSet<Vector3Int> SolidCells = new HashSet<Vector3Int>();
            public readonly HashSet<Vector3Int> GroundLikeCells = new HashSet<Vector3Int>();
        }

        private struct LayerCellKey : IEquatable<LayerCellKey>
        {
            private readonly string layerName;
            private readonly Vector3Int cell;

            public LayerCellKey(string sourceLayerName, Vector3Int targetCell)
            {
                layerName = sourceLayerName != null
                    ? sourceLayerName.ToLowerInvariant()
                    : string.Empty;
                cell = targetCell;
            }

            public bool Equals(LayerCellKey other)
            {
                return string.Equals(layerName, other.layerName, StringComparison.Ordinal) &&
                       cell.Equals(other.cell);
            }

            public override bool Equals(object obj)
            {
                return obj is LayerCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((layerName != null ? layerName.GetHashCode() : 0) * 397) ^
                           cell.GetHashCode();
                }
            }
        }
    }
}
#endif
