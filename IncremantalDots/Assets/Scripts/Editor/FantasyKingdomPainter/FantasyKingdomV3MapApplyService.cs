#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    internal sealed class FantasyKingdomV3ApplyReport
    {
        public FantasyKingdomFullMapPreviewReport Preflight;
        public int PlacementCount;
        public int CreatedTilemapCount;
        public int AppliedTileCount;
        public int ClearedLegacyTileCount;
        public int PersistentTileCount;
        public string SceneDiskHashBeforeApply;
        public string ProtectedFingerprint;

        public string BuildSummary()
        {
            return string.Format(
                "Approved V3 applied (unsaved): {0} placement, {1} tilemap, {2} tile. " +
                "Legacy cleared: {3}. Persistent scene tiles: {4}. Root: Grid/{5}",
                PlacementCount,
                CreatedTilemapCount,
                AppliedTileCount,
                ClearedLegacyTileCount,
                PersistentTileCount,
                FantasyKingdomV3MapApplyService.ManagedRootName);
        }
    }

    /// <summary>
    /// Kullanici tarafindan gorsel olarak onaylanan V3 layout'u tek Undo grubu icinde
    /// kalici, placement-specific tilemap'lere yazar. Sahneyi kendisi kaydetmez; boylece
    /// gercek kamera kontrolu basarisiz olursa kayittan once Undo ile geri donulebilir.
    /// </summary>
    internal static class FantasyKingdomV3MapApplyService
    {
        public const string ManagedRootName = "FK_V3_Map";

        private const string StagingRootName = "__FK_V3_Map_Staging";
        private const string TargetScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string ApprovedProfileId = "NewGameScene-VisualRetouch-Candidate-v3.1";
        private const int ExpectedPlacementCount = 16;
        private const int ExpectedTileCount = 2695;
        private const int ExpectedUniqueCellCount = 2148;
        private const int ExpectedSolidCellCount = 448;
        private const int ExpectedPersistentTileCount = 5364;
        private const string PreviousApprovedProfileId = "NewGameScene-ApprovedVisualRebuild-v3";
        private const int PreviousExpectedPlacementCount = 19;
        private const int PreviousExpectedTileCount = 2488;
        private const int PreviousExpectedPersistentTileCount = 5157;
        private const string ApprovedProtectedFingerprint =
            "1D82353DF4CACF23FD5D1657C6358DA7E0671B2C09B62556394999350E9E098E";
        private const string ApprovedMarkerFingerprint =
            "707AFD6E5B200A9294E9F55B9E50928BF86C86DC4632C0F40F411778D1DC5033";

        private static readonly string[] RequiredLegacyLayerNames =
        {
            "GroundDetail",
            "Structures",
            "OverlayProps",
            "RoofLow",
            "RoofHigh"
        };

        private static readonly string[] OptionalLegacyLayerNames =
        {
            "Roof1",
            "Roof2",
            "Roof3"
        };

        private static readonly Dictionary<string, int> ApprovedLegacyTileCounts =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "GroundDetail", 240 },
                { "Structures", 186 },
                { "OverlayProps", 8 },
                { "RoofLow", 8 },
                { "RoofHigh", 5 }
            };

        private static readonly string[] ProtectedLayerNames =
        {
            "Grass",
            "outside",
            "outside0",
            "outside2"
        };

        public static FantasyKingdomV3ApplyReport LastApplyReport { get; private set; }

        [MenuItem(DeadWallsEditorMenuPaths.Maps + "Fantasy Kingdom/APPLY APPROVED V3 TO NEW GAME SCENE")]
        private static void ApplyApprovedV3FromMenu()
        {
            try
            {
                FantasyKingdomV3ApplyReport report = ApplyApprovedV3();
                Debug.Log("FK V3 APPLY OK — " + report.BuildSummary(),
                    FantasyKingdomMapLayoutFactory.LoadApproved());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem(DeadWallsEditorMenuPaths.Maps + "Fantasy Kingdom/Validate Persistent V3 Map")]
        private static void ValidatePersistentV3FromMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(scene);
            FantasyKingdomMapLayout layout = FantasyKingdomMapLayoutFactory.LoadApproved();
            ValidateLayoutIdentity(layout, grid);
            PersistentValidation validation = ValidatePersistentState(
                grid,
                layout,
                null,
                false,
                ExpectedPersistentTileCount);
            Debug.Log(validation.BuildSummary(), layout);
        }

        public static FantasyKingdomV3ApplyReport ApplyApprovedV3()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Approved V3 apply Play Mode disinda calistirilmalidir.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(NormalizePath(scene.path), TargetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Approved V3 yalniz aktif NewGameScene'e uygulanabilir. Active=" + scene.path);
            }
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "NewGameScene dirty. V3 apply mevcut kaydedilmemis isi ezmemek icin durdu.");

            Grid grid = FantasyKingdomStampPreviewService.FindDefaultTargetGrid(scene);
            if (grid == null || !string.Equals(grid.name, "Grid", StringComparison.Ordinal))
                throw new InvalidOperationException("NewGameScene kok Grid bulunamadi.");
            EnsureNoTransientRoots(scene, grid);

            FantasyKingdomMapLayout layout = FantasyKingdomMapLayoutFactory.LoadApproved();
            ValidateLayoutIdentity(layout, grid);

            Transform existingManagedRoot = grid.transform.Find(ManagedRootName);
            bool originallyHadManagedRoot = existingManagedRoot != null;
            FantasyKingdomMapLayout existingManagedLayout = null;
            if (existingManagedRoot != null)
                existingManagedLayout = ResolveKnownManagedLayout(existingManagedRoot, grid, layout);
            ValidateLegacyBaseline(grid, originallyHadManagedRoot);

            SceneContractSnapshot baseline = CaptureSceneContract(scene, grid);
            FantasyKingdomFullMapPreviewReport preflight =
                FantasyKingdomFullMapPreviewService.AnalyzeLayout(layout, grid);
            ValidateApprovedPreflight(preflight);

            RendererReferences references = CaptureRendererReferences(grid);
            string undoName = "Apply Approved Fantasy Kingdom V3 Map";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            try
            {
                Transform stagingRoot = CreateRoot(grid, StagingRootName, undoName);
                BuildManagedLayout(stagingRoot, grid, layout, references, undoName);
                ValidateManagedRoot(stagingRoot, layout, false);

                int clearedLegacyTiles = ClearLegacyVisualLayers(grid, undoName);
                if (existingManagedRoot != null)
                    Undo.DestroyObjectImmediate(existingManagedRoot.gameObject);

                Undo.RecordObject(stagingRoot.gameObject, undoName);
                stagingRoot.name = ManagedRootName;
                EditorUtility.SetDirty(stagingRoot.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);

                PersistentValidation validation =
                    ValidatePersistentState(
                        grid,
                        layout,
                        baseline,
                        true,
                        ExpectedPersistentTileCount);
                if (!scene.isDirty)
                    throw new InvalidOperationException("V3 apply sonrasi sahne dirty olmadi.");

                string diskHashAfterApply = ComputeSceneDiskHash(scene);
                if (!string.Equals(
                        baseline.SceneDiskHash,
                        diskHashAfterApply,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Apply servisi acikca Save istemeden scene dosyasini degistirdi.");
                }

                var report = new FantasyKingdomV3ApplyReport
                {
                    Preflight = preflight,
                    PlacementCount = validation.PlacementCount,
                    CreatedTilemapCount = validation.ManagedTilemapCount,
                    AppliedTileCount = validation.ManagedTileCount,
                    ClearedLegacyTileCount = clearedLegacyTiles,
                    PersistentTileCount = validation.PersistentTileCount,
                    SceneDiskHashBeforeApply = baseline.SceneDiskHash,
                    ProtectedFingerprint = baseline.ProtectedFingerprint
                };

                Undo.CollapseUndoOperations(undoGroup);
                LastApplyReport = report;
                SceneView.RepaintAll();
                return report;
            }
            catch (Exception applyException)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                LastApplyReport = null;
                try
                {
                    ValidateRollbackState(
                        scene,
                        grid,
                        existingManagedLayout,
                        baseline,
                        originallyHadManagedRoot);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "V3 apply basarisiz oldu ve rollback kontrati da saglanamadi.",
                        applyException,
                        rollbackException);
                }
                throw;
            }
        }

        internal static void ValidatePersistentV3ForBuilder(Grid grid)
        {
            Transform managedRoot = grid != null ? grid.transform.Find(ManagedRootName) : null;
            if (managedRoot == null)
                throw new InvalidOperationException("Grid/FK_V3_Map bulunamadi.");

            FantasyKingdomMapLayout approvedLayout = FantasyKingdomMapLayoutFactory.LoadApproved();
            FantasyKingdomMapLayout layout = ResolveKnownManagedLayout(managedRoot, grid, approvedLayout);
            int expectedPersistentTileCount = ReferenceEquals(layout, approvedLayout)
                ? ExpectedPersistentTileCount
                : PreviousExpectedPersistentTileCount;
            ValidatePersistentState(
                grid,
                layout,
                null,
                false,
                expectedPersistentTileCount);
        }

        private static FantasyKingdomMapLayout ResolveKnownManagedLayout(
            Transform managedRoot,
            Grid grid,
            FantasyKingdomMapLayout approvedLayout)
        {
            Exception approvedException;
            try
            {
                ValidateLayoutIdentity(approvedLayout, grid);
                ValidateManagedRoot(managedRoot, approvedLayout, true);
                return approvedLayout;
            }
            catch (Exception exception)
            {
                approvedException = exception;
            }

            FantasyKingdomMapLayout previousLayout =
                FantasyKingdomMapLayoutFactory.LoadPreviousApproved();
            try
            {
                ValidateLayoutContract(
                    previousLayout,
                    grid,
                    PreviousApprovedProfileId,
                    PreviousExpectedPlacementCount,
                    PreviousExpectedTileCount);
                ValidateManagedRoot(managedRoot, previousLayout, true);
                return previousLayout;
            }
            catch (Exception previousException)
            {
                throw new AggregateException(
                    "FK_V3_Map ne onayli v3.1 ne de onceki v3 kontratiyla eslesiyor.",
                    approvedException,
                    previousException);
            }
        }

        private static void ValidateLayoutIdentity(FantasyKingdomMapLayout layout, Grid grid)
        {
            ValidateLayoutContract(
                layout,
                grid,
                ApprovedProfileId,
                ExpectedPlacementCount,
                ExpectedTileCount);
        }

        private static void ValidateLayoutContract(
            FantasyKingdomMapLayout layout,
            Grid grid,
            string expectedProfileId,
            int expectedPlacementCount,
            int expectedTileCount)
        {
            if (layout == null)
                throw new InvalidOperationException("V3 layout asset'i bulunamadi.");
            if (grid == null)
                throw new InvalidOperationException("V3 layout target Grid bulunamadi.");
            if (layout.SchemaVersion != FantasyKingdomMapLayout.CurrentSchemaVersion ||
                !string.Equals(layout.ProfileId, expectedProfileId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "V3 schema/profile kontrati bozuk. Schema=" + layout.SchemaVersion +
                    " Profile=" + layout.ProfileId + " Beklenen=" + expectedProfileId);
            }
            if (!string.Equals(
                    NormalizePath(layout.TargetScenePath),
                    TargetScenePath,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(layout.TargetGridPath, "Grid", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("V3 layout target scene/Grid kontrati bozuk.");
            }
            if (layout.Placements.Count != expectedPlacementCount ||
                layout.Placements.Any(placement => placement == null || !placement.Enabled))
            {
                throw new InvalidOperationException(
                    "V3 placement kontrati bozuk. Beklenen enabled=" + expectedPlacementCount);
            }
            int actualTileCount = layout.Placements.Sum(placement => placement.Stamp.TotalTileCount);
            if (actualTileCount != expectedTileCount)
            {
                throw new InvalidOperationException(
                    "V3 stamp tile toplami sapmis. Beklenen=" + expectedTileCount +
                    " Mevcut=" + actualTileCount);
            }
            if (grid.transform.Find(StagingRootName) != null)
                throw new InvalidOperationException("Onceki apply'dan staging root kalmis.");
        }

        private static void ValidateApprovedPreflight(FantasyKingdomFullMapPreviewReport report)
        {
            if (report == null)
                throw new InvalidOperationException("V3 preflight raporu uretilmedi.");

            bool invalid = report.ErrorCount != 0 ||
                           report.EnabledPlacementCount != ExpectedPlacementCount ||
                           report.RenderablePlacementCount != ExpectedPlacementCount ||
                           report.StampTileCount != ExpectedTileCount ||
                           report.UniquePreviewCellCount != ExpectedUniqueCellCount ||
                           report.SolidPreviewCellCount != ExpectedSolidCellCount ||
                           report.ProtectedOverlapCellCount != 0 ||
                           report.UnknownExistingOverlapCellCount != 0 ||
                           report.CanonicalLayerConflictCount != 0 ||
                           report.SolidFootprintConflictCount != 0 ||
                           report.MarkerConflictCellCount != 0 ||
                           report.RestrictedZoneCellCount != 0 ||
                           report.MissingGroundSupportCellCount != 0 ||
                           report.BoundGameplayAnchorCount != 5 ||
                           report.LivingForestTreeCount != 32 ||
                           report.EnemyForestBackTreeCount != 140 ||
                           report.EnemyForestFrontTreeCount != 60 ||
                           report.EnemyFrontCoveredYBandCount != 13 ||
                           report.CaravanRoadCellCount != 43 ||
                           report.CaravanRoadComponentCount != 1 ||
                           report.OpenBattlefieldSolidCellCount != 0;
            if (invalid)
                throw new InvalidOperationException("Approved V3 preflight hard gate basarisiz.\n" + report.BuildSummary());
        }

        private static void ValidateLegacyBaseline(Grid grid, bool hasManagedRoot)
        {
            int totalLegacyTiles = 0;
            for (int i = 0; i < RequiredLegacyLayerNames.Length; i++)
            {
                string name = RequiredLegacyLayerNames[i];
                Tilemap map = FindDirectTilemap(grid, name, true);
                int count = CountTiles(map);
                totalLegacyTiles += count;
                if (!hasManagedRoot && count != ApprovedLegacyTileCounts[name])
                {
                    throw new InvalidOperationException(
                        name + " legacy baseline sapmis. Beklenen=" +
                        ApprovedLegacyTileCounts[name] + " Mevcut=" + count);
                }
            }
            for (int i = 0; i < OptionalLegacyLayerNames.Length; i++)
            {
                Tilemap map = FindDirectTilemap(grid, OptionalLegacyLayerNames[i], false);
                totalLegacyTiles += map != null ? CountTiles(map) : 0;
            }
            if (hasManagedRoot && totalLegacyTiles != 0)
                throw new InvalidOperationException("Reapply oncesi legacy V3 layer'lari bos olmalidir.");
        }

        private static RendererReferences CaptureRendererReferences(Grid grid)
        {
            TilemapRenderer ground = GetRenderer(FindDirectTilemap(grid, "GroundDetail", true));
            TilemapRenderer behind = GetRenderer(FindDirectTilemap(grid, "Structures", true));
            TilemapRenderer front = GetRenderer(FindDirectTilemap(grid, "outside2", true));
            if (ground.sharedMaterial == null || behind.sharedMaterial == null || front.sharedMaterial == null)
                throw new InvalidOperationException("V3 renderer material referansi eksik.");
            return new RendererReferences
            {
                GroundMaterial = ground.sharedMaterial,
                BehindMaterial = behind.sharedMaterial,
                FrontMaterial = front.sharedMaterial
            };
        }

        private static Transform CreateRoot(Grid grid, string rootName, string undoName)
        {
            var rootObject = new GameObject(rootName) { layer = grid.gameObject.layer };
            Undo.RegisterCreatedObjectUndo(rootObject, undoName);
            if (rootObject.scene != grid.gameObject.scene)
                Undo.MoveGameObjectToScene(rootObject, grid.gameObject.scene, undoName);
            Undo.SetTransformParent(rootObject.transform, grid.transform, undoName);
            Undo.RegisterCompleteObjectUndo(rootObject.transform, undoName);
            SetIdentity(rootObject.transform, Vector3.zero);
            return rootObject.transform;
        }

        private static void BuildManagedLayout(
            Transform stagingRoot,
            Grid grid,
            FantasyKingdomMapLayout layout,
            RendererReferences references,
            string undoName)
        {
            var bandRoots = new Dictionary<FantasyKingdomRenderBand, Transform>
            {
                { FantasyKingdomRenderBand.Ground,
                    CreateContainer(stagingRoot, grid, "00_Ground", 0f, undoName) },
                { FantasyKingdomRenderBand.BehindUnits,
                    CreateContainer(stagingRoot, grid, "10_BehindUnits", 0f, undoName) },
                { FantasyKingdomRenderBand.InFrontOfUnits,
                    CreateContainer(stagingRoot, grid, "20_FrontOccluders", -2f, undoName) }
            };

            for (int placementIndex = 0; placementIndex < layout.Placements.Count; placementIndex++)
            {
                FantasyKingdomMapPlacement placement = layout.Placements[placementIndex];
                if (!bandRoots.TryGetValue(placement.RenderBand, out Transform bandRoot))
                    throw new InvalidOperationException(placement.Id + " explicit render band tasimiyor.");

                Transform placementRoot = CreateContainer(
                    bandRoot,
                    grid,
                    string.Format("{0:00}_{1}", placementIndex, SanitizeName(placement.Id)),
                    0f,
                    undoName);
                Vector3Int origin = placement.TargetAnchorCell - placement.Stamp.AnchorLocalCell;
                IReadOnlyList<FantasyKingdomStampLayer> layers = placement.Stamp.Layers;

                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer sourceLayer = layers[layerIndex];
                    Transform layerTransform = CreateContainer(
                        placementRoot,
                        grid,
                        string.Format("{0:00}_{1}", layerIndex, SanitizeName(sourceLayer.SourceName)),
                        0f,
                        undoName);
                    Tilemap tilemap = Undo.AddComponent<Tilemap>(layerTransform.gameObject);
                    TilemapRenderer renderer = Undo.AddComponent<TilemapRenderer>(layerTransform.gameObject);
                    Undo.RegisterCompleteObjectUndo(
                        new UnityEngine.Object[] { tilemap, renderer },
                        undoName);

                    tilemap.tileAnchor = sourceLayer.TileAnchor;
                    tilemap.color = sourceLayer.LayerColor;
                    tilemap.orientation = sourceLayer.Orientation;
                    tilemap.orientationMatrix = sourceLayer.OrientationMatrix;
                    renderer.mode = placement.RenderBand == FantasyKingdomRenderBand.Ground
                        ? sourceLayer.RendererMode
                        : TilemapRenderer.Mode.Individual;
                    renderer.sortOrder = sourceLayer.SortOrder;
                    ResolveSorting(
                        sourceLayer.SourceName,
                        placement.RenderBand,
                        placementIndex,
                        layerIndex,
                        out string sortingLayer,
                        out int sortingOrder);
                    renderer.sortingLayerName = sortingLayer;
                    renderer.sortingOrder = sortingOrder;
                    renderer.sharedMaterial = placement.RenderBand == FantasyKingdomRenderBand.Ground
                        ? references.GroundMaterial
                        : placement.RenderBand == FantasyKingdomRenderBand.InFrontOfUnits
                            ? references.FrontMaterial
                            : references.BehindMaterial;
                    renderer.forceRenderingOff = false;

                    IReadOnlyList<FantasyKingdomStampCell> cells = sourceLayer.Cells;
                    for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    {
                        FantasyKingdomStampCell sourceCell = cells[cellIndex];
                        Vector3Int targetCell = origin + sourceCell.LocalPosition;
                        if (tilemap.HasTile(targetCell))
                            throw new InvalidOperationException(
                                placement.Id + "/" + sourceLayer.SourceName +
                                " duplicate cell=" + targetCell);
                        tilemap.SetTile(targetCell, sourceCell.Tile);
                        tilemap.SetTileFlags(targetCell, TileFlags.None);
                        tilemap.SetTransformMatrix(targetCell, sourceCell.TransformMatrix);
                        tilemap.SetColor(targetCell, sourceCell.Color);
                        tilemap.SetTileFlags(targetCell, sourceCell.Flags);
                    }
                    tilemap.CompressBounds();
                    EditorUtility.SetDirty(tilemap);
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Transform CreateContainer(
            Transform parent,
            Grid grid,
            string name,
            float localZ,
            string undoName)
        {
            var gameObject = new GameObject(name) { layer = grid.gameObject.layer };
            Undo.RegisterCreatedObjectUndo(gameObject, undoName);
            if (gameObject.scene != grid.gameObject.scene)
                Undo.MoveGameObjectToScene(gameObject, grid.gameObject.scene, undoName);
            Undo.SetTransformParent(gameObject.transform, parent, undoName);
            Undo.RegisterCompleteObjectUndo(gameObject.transform, undoName);
            SetIdentity(gameObject.transform, new Vector3(0f, 0f, localZ));
            return gameObject.transform;
        }

        private static int ClearLegacyVisualLayers(Grid grid, string undoName)
        {
            int cleared = 0;
            IEnumerable<string> names = RequiredLegacyLayerNames.Concat(OptionalLegacyLayerNames);
            foreach (string name in names)
            {
                Tilemap map = FindDirectTilemap(grid, name, false);
                if (map == null)
                    continue;
                int count = CountTiles(map);
                if (count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(map, undoName);
                    map.ClearAllTiles();
                    map.CompressBounds();
                    EditorUtility.SetDirty(map);
                    cleared += count;
                }
                TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
                if (renderer != null && renderer.forceRenderingOff)
                {
                    Undo.RecordObject(renderer, undoName);
                    renderer.forceRenderingOff = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
            return cleared;
        }

        private static PersistentValidation ValidatePersistentState(
            Grid grid,
            FantasyKingdomMapLayout layout,
            SceneContractSnapshot baseline,
            bool requireDirty,
            int expectedPersistentTileCount)
        {
            if (grid == null || layout == null)
                throw new InvalidOperationException("Persistent V3 validation target'i eksik.");
            Transform managedRoot = grid.transform.Find(ManagedRootName);
            if (managedRoot == null)
                throw new InvalidOperationException("Grid/FK_V3_Map bulunamadi.");

            PersistentValidation validation = ValidateManagedRoot(managedRoot, layout, true);
            int legacyTiles = RequiredLegacyLayerNames
                .Concat(OptionalLegacyLayerNames)
                .Select(name => FindDirectTilemap(grid, name, false))
                .Where(map => map != null)
                .Sum(CountTiles);
            if (legacyTiles != 0)
                throw new InvalidOperationException("Persistent V3 sonrasinda legacy visual tile kaldi: " + legacyTiles);

            validation.PersistentTileCount = grid.GetComponentsInChildren<Tilemap>(true).Sum(CountTiles);
            if (validation.PersistentTileCount != expectedPersistentTileCount)
            {
                throw new InvalidOperationException(
                    "Persistent scene tile toplami sapmis. Beklenen=" + expectedPersistentTileCount +
                    " Mevcut=" + validation.PersistentTileCount);
            }
            ValidateMarkerContract(grid.gameObject.scene);
            string approvedProtectedFingerprint = ComputeProtectedFingerprint(grid);
            if (!string.Equals(
                    ApprovedProtectedFingerprint,
                    approvedProtectedFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Grass/outside* approved fingerprint kontrati bozuk.");
            }
            string approvedMarkerFingerprint = ComputeMarkerFingerprint(grid.gameObject.scene);
            if (!string.Equals(
                    ApprovedMarkerFingerprint,
                    approvedMarkerFingerprint,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("VillageMarkers approved fingerprint kontrati bozuk.");
            }
            if (requireDirty && !grid.gameObject.scene.isDirty)
                throw new InvalidOperationException("Persistent apply scene dirty kontrati saglanmadi.");

            if (baseline != null)
            {
                string protectedFingerprint = ComputeProtectedFingerprint(grid);
                if (!string.Equals(
                        baseline.ProtectedFingerprint,
                        protectedFingerprint,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Grass/outside* korumali tilemap fingerprint'i degisti.");
                }
                string markerFingerprint = ComputeMarkerFingerprint(grid.gameObject.scene);
                if (!string.Equals(
                        baseline.MarkerFingerprint,
                        markerFingerprint,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("VillageMarkers transform kontrati degisti.");
                }
            }
            return validation;
        }

        private static void ValidateRollbackState(
            Scene scene,
            Grid grid,
            FantasyKingdomMapLayout layout,
            SceneContractSnapshot baseline,
            bool originallyHadManagedRoot)
        {
            if (grid.transform.Find(StagingRootName) != null)
                throw new InvalidOperationException("Rollback sonrasi staging root kaldi.");
            Transform managedRoot = grid.transform.Find(ManagedRootName);
            if (originallyHadManagedRoot)
            {
                if (managedRoot == null)
                    throw new InvalidOperationException("Rollback onceki managed root'u geri getirmedi.");
                ValidateManagedRoot(managedRoot, layout, true);
            }
            else if (managedRoot != null)
            {
                throw new InvalidOperationException("Rollback yeni managed root'u kaldirmadi.");
            }

            ValidateLegacyBaseline(grid, originallyHadManagedRoot);
            if (!string.Equals(
                    baseline.ProtectedFingerprint,
                    ComputeProtectedFingerprint(grid),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    baseline.MarkerFingerprint,
                    ComputeMarkerFingerprint(scene),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback protected/marker fingerprint'i geri getirmedi.");
            }
            if (!string.Equals(
                    baseline.SceneDiskHash,
                    ComputeSceneDiskHash(scene),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback scene disk hash'ini degistirdi.");
            }
            if (scene.isDirty)
                throw new InvalidOperationException("Rollback temiz scene'yi dirty birakti.");
        }

        private static PersistentValidation ValidateManagedRoot(
            Transform root,
            FantasyKingdomMapLayout layout,
            bool requireFinalName)
        {
            if (root.parent == null || root.parent.GetComponent<Grid>() == null)
                throw new InvalidOperationException(root.name + " dogrudan Grid altinda olmali.");
            if (requireFinalName && !string.Equals(root.name, ManagedRootName, StringComparison.Ordinal))
                throw new InvalidOperationException("Managed root adi bozuk: " + root.name);
            ValidateIdentity(root, Vector3.zero);
            if (root.GetComponents<Component>().Any(component => !(component is Transform)))
                throw new InvalidOperationException(root.name + " beklenmeyen component tasiyor.");

            string[] bandNames = { "00_Ground", "10_BehindUnits", "20_FrontOccluders" };
            float[] bandZ = { 0f, 0f, -2f };
            for (int i = 0; i < bandNames.Length; i++)
            {
                Transform band = root.Find(bandNames[i]);
                if (band == null)
                    throw new InvalidOperationException(root.name + "/" + bandNames[i] + " eksik.");
                ValidateIdentity(band, new Vector3(0f, 0f, bandZ[i]));
                if (band.GetComponents<Component>().Any(component => !(component is Transform)))
                    throw new InvalidOperationException(band.name + " beklenmeyen component tasiyor.");
            }
            if (root.childCount != 3)
                throw new InvalidOperationException(root.name + " yalniz uc render band root'u tasimali.");

            Tilemap[] maps = root.GetComponentsInChildren<Tilemap>(true);
            int expectedMapCount = layout.Placements.Sum(placement => placement.Stamp.Layers.Count);
            if (maps.Length != expectedMapCount)
                throw new InvalidOperationException(
                    "Managed tilemap sayisi sapmis. Beklenen=" + expectedMapCount + " Mevcut=" + maps.Length);
            if (root.GetComponentsInChildren<TilemapCollider2D>(true).Length != 0)
                throw new InvalidOperationException("Managed V3 altinda TilemapCollider2D olmamali.");

            int tileCount = maps.Sum(CountTiles);
            int expectedTileCount = layout.Placements.Sum(
                placement => placement.Stamp.TotalTileCount);
            if (tileCount != expectedTileCount)
                throw new InvalidOperationException(
                    "Managed V3 tile sayisi sapmis. Beklenen=" + expectedTileCount +
                    " Mevcut=" + tileCount);

            for (int bandIndex = 0; bandIndex < bandNames.Length; bandIndex++)
            {
                string bandName = bandNames[bandIndex];
                Transform band = root.Find(bandName);
                int expectedPlacementCount = layout.Placements.Count(
                    placement => string.Equals(
                        ResolveBandName(placement.RenderBand),
                        bandName,
                        StringComparison.Ordinal));
                if (band.childCount != expectedPlacementCount)
                {
                    throw new InvalidOperationException(
                        bandName + " placement child sayisi sapmis. Beklenen=" +
                        expectedPlacementCount + " Mevcut=" + band.childCount);
                }
            }

            Grid grid = root.parent.GetComponent<Grid>();
            for (int placementIndex = 0; placementIndex < layout.Placements.Count; placementIndex++)
            {
                FantasyKingdomMapPlacement placement = layout.Placements[placementIndex];
                string bandName = ResolveBandName(placement.RenderBand);
                Transform band = root.Find(bandName);
                string placementName = string.Format(
                    "{0:00}_{1}",
                    placementIndex,
                    SanitizeName(placement.Id));
                Transform placementRoot = band.Find(placementName);
                if (placementRoot == null)
                    throw new InvalidOperationException(bandName + "/" + placementName + " eksik.");
                ValidateIdentity(placementRoot, Vector3.zero);
                if (placementRoot.GetComponents<Component>().Any(
                        component => !(component is Transform)))
                {
                    throw new InvalidOperationException(placementName + " beklenmeyen component tasiyor.");
                }

                IReadOnlyList<FantasyKingdomStampLayer> layers = placement.Stamp.Layers;
                if (placementRoot.childCount != layers.Count)
                {
                    throw new InvalidOperationException(
                        placementName + " layer child sayisi sapmis. Beklenen=" +
                        layers.Count + " Mevcut=" + placementRoot.childCount);
                }
                Vector3Int origin = placement.TargetAnchorCell - placement.Stamp.AnchorLocalCell;

                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    FantasyKingdomStampLayer sourceLayer = layers[layerIndex];
                    string layerName = string.Format(
                        "{0:00}_{1}",
                        layerIndex,
                        SanitizeName(sourceLayer.SourceName));
                    Transform layerRoot = placementRoot.Find(layerName);
                    if (layerRoot == null || layerRoot.childCount != 0)
                        throw new InvalidOperationException(placementName + "/" + layerName + " kontrati bozuk.");
                    ValidateIdentity(layerRoot, Vector3.zero);

                    Component[] components = layerRoot.GetComponents<Component>();
                    Tilemap map = layerRoot.GetComponent<Tilemap>();
                    TilemapRenderer renderer = layerRoot.GetComponent<TilemapRenderer>();
                    if (components.Length != 3 || map == null || renderer == null)
                    {
                        throw new InvalidOperationException(
                            placementName + "/" + layerName +
                            " yalniz Transform+Tilemap+TilemapRenderer tasimali.");
                    }
                    ValidateManagedLayer(
                        grid,
                        map,
                        renderer,
                        placement,
                        sourceLayer,
                        origin,
                        placementIndex,
                        layerIndex);
                }
            }

            return new PersistentValidation
            {
                PlacementCount = layout.Placements.Count,
                ManagedTilemapCount = maps.Length,
                ManagedTileCount = tileCount
            };
        }

        private static void ValidateManagedLayer(
            Grid grid,
            Tilemap map,
            TilemapRenderer renderer,
            FantasyKingdomMapPlacement placement,
            FantasyKingdomStampLayer sourceLayer,
            Vector3Int origin,
            int placementIndex,
            int layerIndex)
        {
            bool tilemapMetadataMismatch =
                (map.tileAnchor - sourceLayer.TileAnchor).sqrMagnitude > 0.000001f ||
                map.orientation != sourceLayer.Orientation ||
                !MatrixApproximately(map.orientationMatrix, sourceLayer.OrientationMatrix) ||
                !ColorApproximately(map.color, sourceLayer.LayerColor);
            if (tilemapMetadataMismatch)
                throw new InvalidOperationException(map.name + " Tilemap metadata kontrati bozuk.");

            ResolveSorting(
                sourceLayer.SourceName,
                placement.RenderBand,
                placementIndex,
                layerIndex,
                out string expectedSortingLayer,
                out int expectedSortingOrder);
            TilemapRenderer.Mode expectedMode = placement.RenderBand == FantasyKingdomRenderBand.Ground
                ? sourceLayer.RendererMode
                : TilemapRenderer.Mode.Individual;
            Material expectedMaterial = ResolveReferenceMaterial(grid, placement.RenderBand);
            bool rendererMismatch = !renderer.enabled ||
                                    renderer.forceRenderingOff ||
                                    renderer.mode != expectedMode ||
                                    renderer.sortOrder != sourceLayer.SortOrder ||
                                    !string.Equals(
                                        renderer.sortingLayerName,
                                        expectedSortingLayer,
                                        StringComparison.Ordinal) ||
                                    renderer.sortingOrder != expectedSortingOrder ||
                                    renderer.sharedMaterial != expectedMaterial;
            if (rendererMismatch)
                throw new InvalidOperationException(map.name + " TilemapRenderer kontrati bozuk.");

            IReadOnlyList<FantasyKingdomStampCell> cells = sourceLayer.Cells;
            if (CountTiles(map) != cells.Count)
                throw new InvalidOperationException(map.name + " tile sayisi source stamp'ten sapmis.");
            for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
            {
                FantasyKingdomStampCell sourceCell = cells[cellIndex];
                Vector3Int targetCell = origin + sourceCell.LocalPosition;
                TileFlags actualFlags = map.GetTileFlags(targetCell);
                // TileFlags.None override'i scene reload'unda Tile asset'in lock flag'ine
                // donebilir; explicit source flag varsa yine birebir dogrulanir.
                bool flagsMatch = actualFlags == sourceCell.Flags ||
                                  sourceCell.Flags == TileFlags.None;
                bool cellMismatch = map.GetTile(targetCell) != sourceCell.Tile ||
                                    !MatrixApproximately(
                                        map.GetTransformMatrix(targetCell),
                                        sourceCell.TransformMatrix) ||
                                    !ColorApproximately(map.GetColor(targetCell), sourceCell.Color) ||
                                    !flagsMatch;
                if (cellMismatch)
                {
                    throw new InvalidOperationException(
                        map.name + " managed cell source stamp'ten sapmis: " + targetCell);
                }
            }
        }

        private static string ResolveBandName(FantasyKingdomRenderBand renderBand)
        {
            switch (renderBand)
            {
                case FantasyKingdomRenderBand.Ground:
                    return "00_Ground";
                case FantasyKingdomRenderBand.BehindUnits:
                    return "10_BehindUnits";
                case FantasyKingdomRenderBand.InFrontOfUnits:
                    return "20_FrontOccluders";
                default:
                    throw new InvalidOperationException("LegacyAuto persistent V3'te kullanilamaz.");
            }
        }

        private static Material ResolveReferenceMaterial(
            Grid grid,
            FantasyKingdomRenderBand renderBand)
        {
            string referenceName = renderBand == FantasyKingdomRenderBand.Ground
                ? "GroundDetail"
                : renderBand == FantasyKingdomRenderBand.InFrontOfUnits
                    ? "outside2"
                    : "Structures";
            TilemapRenderer renderer = GetRenderer(FindDirectTilemap(grid, referenceName, true));
            if (renderer.sharedMaterial == null)
                throw new InvalidOperationException(referenceName + " material referansi eksik.");
            return renderer.sharedMaterial;
        }

        private static bool MatrixApproximately(Matrix4x4 first, Matrix4x4 second)
        {
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(first[index] - second[index]) > 0.000001f)
                    return false;
            }
            return true;
        }

        private static bool ColorApproximately(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) <= 0.000001f &&
                   Mathf.Abs(first.g - second.g) <= 0.000001f &&
                   Mathf.Abs(first.b - second.b) <= 0.000001f &&
                   Mathf.Abs(first.a - second.a) <= 0.000001f;
        }

        private static void ResolveSorting(
            string sourceName,
            FantasyKingdomRenderBand renderBand,
            int placementIndex,
            int layerIndex,
            out string sortingLayer,
            out int sortingOrder)
        {
            if (renderBand == FantasyKingdomRenderBand.Ground)
                sortingLayer = "Ground";
            else if (renderBand == FantasyKingdomRenderBand.BehindUnits)
                sortingLayer = "Objects";
            else if (renderBand == FantasyKingdomRenderBand.InFrontOfUnits)
                sortingLayer = "Wall";
            else
                throw new InvalidOperationException("LegacyAuto persistent V3'te kullanilamaz.");

            string normalized = (sourceName ?? string.Empty).ToLowerInvariant();
            if (renderBand == FantasyKingdomRenderBand.InFrontOfUnits)
            {
                sortingOrder = ResolveFrontOccluderOrder(normalized);
                return;
            }
            int localOrder;
            if (normalized.Contains("lowershadow")) localOrder = 3;
            else if (normalized.Contains("ground")) localOrder = normalized.Contains("3") ? 5 : 4;
            else if (normalized.Contains("shadow")) localOrder = normalized.Contains("2") ? 7 : 6;
            else if (normalized.Contains("roof"))
                localOrder = normalized.Contains("3") ? 14 : normalized.Contains("2") ? 13 : 12;
            else if (normalized.Contains("wall")) localOrder = 10;
            else localOrder = 11;
            localOrder = renderBand == FantasyKingdomRenderBand.Ground
                ? Mathf.Clamp(localOrder, 1, 8)
                : Mathf.Clamp(localOrder, 9, 14);

            // Ayni semantic order'a sahip farkli placement renderer'lari Unity reload'unda
            // instance-id ile tie-break etmesin. Onayli preview'un creation sirasini kalici,
            // serialize edilen bir sortingOrder tie-break'ine ceviriyoruz.
            sortingOrder = localOrder * 1000 + placementIndex * 10 + layerIndex;
        }

        private static int ResolveFrontOccluderOrder(string normalizedSourceName)
        {
            if (normalizedSourceName.Contains("roof"))
                return normalizedSourceName.EndsWith("3", StringComparison.Ordinal) ? 8 :
                    normalizedSourceName.EndsWith("2", StringComparison.Ordinal) ? 7 : 6;
            if (normalizedSourceName.Contains("objects"))
                return 5;
            return 4;
        }

        private static SceneContractSnapshot CaptureSceneContract(Scene scene, Grid grid)
        {
            return new SceneContractSnapshot
            {
                SceneDiskHash = ComputeSceneDiskHash(scene),
                ProtectedFingerprint = ComputeProtectedFingerprint(grid),
                MarkerFingerprint = ComputeMarkerFingerprint(scene)
            };
        }

        private static string ComputeProtectedFingerprint(Grid grid)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < ProtectedLayerNames.Length; i++)
            {
                Tilemap map = FindDirectTilemap(grid, ProtectedLayerNames[i], true);
                builder.Append(ProtectedLayerNames[i]).Append(':')
                    .Append(ComputeTilemapFingerprint(map)).Append('|');
            }
            return HashText(builder.ToString());
        }

        private static string ComputeTilemapFingerprint(Tilemap map)
        {
            var rows = new List<string>();
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                TileBase tile = map.GetTile(cell);
                if (tile == null)
                    continue;
                string assetPath = AssetDatabase.GetAssetPath(tile);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                Matrix4x4 matrix = map.GetTransformMatrix(cell);
                Color color = map.GetColor(cell);
                TileFlags flags = map.GetTileFlags(cell);
                var row = new StringBuilder();
                row.Append(cell.x).Append(',').Append(cell.y).Append(',').Append(cell.z)
                    .Append(':').Append(guid).Append(':');
                for (int matrixIndex = 0; matrixIndex < 16; matrixIndex++)
                    row.Append(matrix[matrixIndex].ToString("R", CultureInfo.InvariantCulture)).Append(',');
                row.Append(color.r.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(color.g.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(color.b.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(color.a.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                    .Append((int)flags);
                rows.Add(row.ToString());
            }
            rows.Sort(StringComparer.Ordinal);
            return HashText(string.Join("\n", rows.ToArray()));
        }

        private static string ComputeMarkerFingerprint(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(
                item => string.Equals(item.name, "VillageMarkers", StringComparison.Ordinal));
            if (root == null)
                throw new InvalidOperationException("VillageMarkers bulunamadi.");
            var rows = new List<string>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                Vector3 position = child.position;
                rows.Add(child.name + ":" +
                         position.x.ToString("R", CultureInfo.InvariantCulture) + "," +
                         position.y.ToString("R", CultureInfo.InvariantCulture) + "," +
                         position.z.ToString("R", CultureInfo.InvariantCulture));
            }
            rows.Sort(StringComparer.Ordinal);
            return HashText(string.Join("|", rows.ToArray()));
        }

        private static void ValidateMarkerContract(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(
                item => string.Equals(item.name, "VillageMarkers", StringComparison.Ordinal));
            string[] expected =
            {
                "CastleKeepMarker",
                "WoodSiteMarker",
                "StoneSiteMarker",
                "FoodSiteMarker",
                "IronSiteMarker"
            };
            if (root == null || root.transform.childCount != expected.Length ||
                expected.Any(name => root.transform.Find(name) == null))
                throw new InvalidOperationException("VillageMarkers 5/5 kontrati bozuk.");
            var expectedPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal)
            {
                { "CastleKeepMarker", new Vector3(-4.4f, 3.2f, 0f) },
                { "WoodSiteMarker", new Vector3(-1.9f, 4.9f, 0f) },
                { "StoneSiteMarker", new Vector3(-7.2f, -1.2f, 0f) },
                { "FoodSiteMarker", new Vector3(-3.2f, -5.6f, 0f) },
                { "IronSiteMarker", new Vector3(-6.9f, -7.4f, 0f) }
            };
            foreach (KeyValuePair<string, Vector3> marker in expectedPositions)
            {
                Transform child = root.transform.Find(marker.Key);
                if ((child.position - marker.Value).sqrMagnitude > 0.000001f)
                    throw new InvalidOperationException(marker.Key + " world position kontrati bozuk.");
            }
            Tilemap outside = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(map => string.Equals(map.name, "outside", StringComparison.Ordinal));
            if (outside == null || CountTiles(outside) != 40)
                throw new InvalidOperationException("outside okcu-slot kontrati 40 degil.");
        }

        private static string ComputeSceneDiskHash(Scene scene)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(
                projectRoot,
                NormalizePath(scene.path).Replace('/', Path.DirectorySeparatorChar));
            using (SHA256 sha = SHA256.Create())
                return BytesToHex(sha.ComputeHash(File.ReadAllBytes(absolutePath)));
        }

        private static string HashText(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BytesToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        private static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty);
        }

        private static void EnsureNoTransientRoots(Scene scene, Grid grid)
        {
            if (grid.transform.Find(FantasyKingdomFullMapPreviewService.PreviewRootName) != null ||
                grid.transform.Find(FantasyKingdomStampPreviewService.PreviewRootName) != null ||
                scene.GetRootGameObjects().Any(root =>
                    string.Equals(root.name, "__FKZombieOcclusionProbeRoot", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Apply oncesi full/stamp preview ve zombie occlusion probe temizlenmelidir.");
            }
        }

        private static Tilemap FindDirectTilemap(Grid grid, string name, bool required)
        {
            Transform child = grid.transform.Find(name);
            Tilemap map = child != null ? child.GetComponent<Tilemap>() : null;
            if (required && map == null)
                throw new InvalidOperationException("Grid/" + name + " tilemap'i bulunamadi.");
            return map;
        }

        private static TilemapRenderer GetRenderer(Tilemap map)
        {
            TilemapRenderer renderer = map != null ? map.GetComponent<TilemapRenderer>() : null;
            if (renderer == null)
                throw new InvalidOperationException((map != null ? map.name : "<null>") + " renderer eksik.");
            return renderer;
        }

        private static int CountTiles(Tilemap map)
        {
            int count = 0;
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(cell))
                    count++;
            }
            return count;
        }

        private static void SetIdentity(Transform transform, Vector3 localPosition)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void ValidateIdentity(Transform transform, Vector3 expectedLocalPosition)
        {
            if ((transform.localPosition - expectedLocalPosition).sqrMagnitude > 0.000001f ||
                Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.001f ||
                (transform.localScale - Vector3.one).sqrMagnitude > 0.000001f)
            {
                throw new InvalidOperationException(transform.name + " transform kontrati bozuk.");
            }
        }

        private static string SanitizeName(string value)
        {
            var builder = new StringBuilder();
            bool separator = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    separator = false;
                }
                else if (!separator)
                {
                    builder.Append('_');
                    separator = true;
                }
            }
            return builder.ToString().Trim('_');
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
        }

        private sealed class RendererReferences
        {
            public Material GroundMaterial;
            public Material BehindMaterial;
            public Material FrontMaterial;
        }

        private sealed class SceneContractSnapshot
        {
            public string SceneDiskHash;
            public string ProtectedFingerprint;
            public string MarkerFingerprint;
        }

        private sealed class PersistentValidation
        {
            public int PlacementCount;
            public int ManagedTilemapCount;
            public int ManagedTileCount;
            public int PersistentTileCount;

            public string BuildSummary()
            {
                return string.Format(
                    "Persistent V3 OK — {0} placement, {1} tilemap, {2} managed tile, " +
                    "{3} total persistent tile; legacy visual=0; collider=0; anchors=5/5.",
                    PlacementCount,
                    ManagedTilemapCount,
                    ManagedTileCount,
                    PersistentTileCount);
            }
        }
    }
}
#endif
