#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Onayli V3 harita recetesini kaynak sahnelerden ve deterministik hedef maskelerinden
    /// yeniden uretir. Yalniz editor asset'lerini yazar; NewGameScene tilemap'lerine dokunmaz.
    /// </summary>
    internal static class FantasyKingdomV3MapDraftBuilder
    {
        private const string TargetScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string ExampleScenePath =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Example scene.unity";
        private const string StampFolder = "Assets/Editor/FantasyKingdomPainter/Stamps/V3";
        private const int Seed = 1072026;
        private const float RoadHalfWidth = 0.38f;
        private const float ForestRoadClearance = 1.35f;

        private static readonly Vector2[] RoadControlPoints =
        {
            new Vector2(0f, 0f),
            new Vector2(5f, -2.5f),
            new Vector2(10f, -5f),
            new Vector2(13.5f, -4.75f),
            new Vector2(16.5f, -4.25f),
            new Vector2(20.5f, -2.25f)
        };

        private static readonly Vector2[] RoadCurveSamples = BuildRoadCurveSamples();
        private const string RetouchAssetSuffix = "_RetouchPreview";
        private static bool writeRetouchCandidate;

        private const string EnvironmentTileFolder =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles/";
        private const string ApprovedOriginalSceneHash =
            "DECEE2A3DCE50346F26D6BEBA778E39A70EAE2293994092501BF67016EB38D0E";

        private static readonly Dictionary<string, int> ApprovedLegacySourceCounts =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "GroundDetail", 240 },
                { "Structures", 186 },
                { "OverlayProps", 8 },
                { "RoofLow", 8 },
                { "RoofHigh", 5 }
            };

        private static readonly Dictionary<string, string> ApprovedFrozenTargetStampHashes =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "FK_V3_Wood_LivingForest_Back", "03E431F746BD0BC0E370840227B18BFD92D29C52C1D412302BCDB8CBACF1849B" },
                { "FK_V3_Stone_Quarry_Ground", "CB92C45CC0C885E941B605ECE0D3A6145134FB1DDAEBCAB42E7BD500F42BB800" },
                { "FK_V3_Stone_Quarry_Solid", "128610F487FA13B8D0A66078F25995E57310131108AF50D17D362EA6191B4C2C" },
                { "FK_V3_Iron_Mine_Ground", "8728932E62CB30BEE490B50ACBFCF9D7AFCB3651AD9F27C7FA5F8AC4F5D2C55C" },
                { "FK_V3_Iron_Mine_Solid", "91D7D7A25ACB1DC5BAA8E3935326F9E72881ADA06610188BE56D5E8641AD9C3A" },
                { "FK_V3_Food_Field_Ground", "E364306105B247868BF6FAFA62A127C3C22E22330A465A12DD50E5807FA0326C" },
                { "FK_V3_Food_Field_Hedge", "417F587A7D75EDAA718F12374D5F5EF95189BB7169A26A80793BE5C3B2DA560C" }
            };

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Rebuild V3 Draft Assets")]
        private static void RebuildFromMenu()
        {
            try
            {
                FantasyKingdomMapLayout layout = CreateOrRefreshDraft();
                Selection.activeObject = layout;
                EditorGUIUtility.PingObject(layout);
                Debug.Log(
                    "Fantasy Kingdom V3 draft asset'leri yeniden uretildi. " +
                    "NewGameScene tilemap verisi degistirilmedi.",
                    layout);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Window/DeadWalls/Fantasy Kingdom/Rebuild V3 Retouch Preview Assets")]
        private static void RebuildRetouchPreviewFromMenu()
        {
            try
            {
                FantasyKingdomMapLayout layout = CreateOrRefreshRetouchCandidate();
                Selection.activeObject = layout;
                EditorGUIUtility.PingObject(layout);
                Debug.Log(
                    "Fantasy Kingdom V3 retouch preview asset'leri ayri candidate profile'a uretildi. " +
                    "Onayli V3 asset'leri ve NewGameScene degistirilmedi.",
                    layout);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public static FantasyKingdomMapLayout CreateOrRefreshRetouchCandidate()
        {
            bool previousMode = writeRetouchCandidate;
            writeRetouchCandidate = true;
            try
            {
                return CreateOrRefreshDraft();
            }
            finally
            {
                writeRetouchCandidate = previousMode;
            }
        }

        public static FantasyKingdomMapLayout CreateOrRefreshDraft()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("V3 draft Play Mode disinda uretilmelidir.");

            EnsureAssetFolder(StampFolder);
            EnsureAssetFolder("Assets/Editor/FantasyKingdomPainter/Layouts");

            Scene previousActiveScene = SceneManager.GetActiveScene();
            using (var targetScope = new ReadOnlySceneScope(TargetScenePath))
            using (var exampleScope = new ReadOnlySceneScope(ExampleScenePath))
            {
                Grid targetGrid = FindPrimaryGrid(targetScope.Scene);
                Grid exampleGrid = FindPrimaryGrid(exampleScope.Scene);
                if (targetGrid == null || exampleGrid == null)
                    throw new InvalidOperationException("V3 kaynak veya hedef Grid bulunamadi.");
                if (targetScope.Scene.isDirty)
                    throw new InvalidOperationException(
                        "NewGameScene dirty. V3 asset builder sahne kaydi riski almamak icin durdu.");

                FantasyKingdomFullMapPreviewService.ClearPreview(targetGrid);
                HashSet<Vector3Int> protectedCells = CollectProtectedCells(targetGrid);
                int targetLegacyTileCount = CountLegacySourceTiles(targetGrid);
                bool hasManagedRoot = targetGrid.transform.Find(
                    FantasyKingdomV3MapApplyService.ManagedRootName) != null;
                bool useFrozenTargetStamps = hasManagedRoot && targetLegacyTileCount == 0;
                if (useFrozenTargetStamps)
                {
                    FantasyKingdomV3MapApplyService.ValidatePersistentV3ForBuilder(targetGrid);
                }
                else
                {
                    ValidateLegacySourceSnapshot(targetGrid, targetLegacyTileCount);
                }
                HashSet<Vector3Int> ironGroundSourceCells = useFrozenTargetStamps
                    ? new HashSet<Vector3Int>()
                    : CollectTileCells(
                        FindTilemap(targetGrid, "GroundDetail"),
                        new RectInt(-23, -9, 5, 5));

                FantasyKingdomStructureStamp castleShadow = ExtractStamp(
                    "FK_V3_Castle_Citadel_Shadow",
                    exampleGrid,
                    ExampleScenePath,
                    new RectInt(69, -34, 13, 15),
                    FantasyKingdomStampPurpose.GroundDetail,
                    new Vector3Int(7, 1, 0),
                    Rule("Shadows1", (tile, cell) => !StartsWith(tile, "Tree C1")));
                RectInt castleSolidRegion = writeRetouchCandidate
                    ? new RectInt(69, -34, 18, 20)
                    : new RectInt(69, -34, 13, 15);
                FantasyKingdomStructureStamp castleSolid = ExtractStamp(
                    "FK_V3_Castle_Citadel_Solid",
                    exampleGrid,
                    ExampleScenePath,
                    castleSolidRegion,
                    FantasyKingdomStampPurpose.Structure,
                    new Vector3Int(7, 1, 0),
                    Rule("Walls", (tile, cell) =>
                        !StartsWith(tile, "Tree C1") &&
                        (!writeRetouchCandidate || IsCandidateCastleBaseCell(cell))),
                    Rule("Objects", (tile, cell) =>
                        !writeRetouchCandidate || IsCandidateCastleBaseCell(cell)),
                    Rule("WallDetail2", (tile, cell) =>
                        !writeRetouchCandidate || IsCandidateCastleBaseCell(cell)),
                    Rule("Roof1", (tile, cell) =>
                        !writeRetouchCandidate || IsCandidateCastleBaseCell(cell)),
                    Rule("Roof2", (tile, cell) =>
                        !writeRetouchCandidate ||
                        IsCandidateCastleBaseCell(cell) ||
                        IsCandidateTowerRoof2Cell(cell)),
                    Rule("Roof3", (tile, cell) =>
                        !writeRetouchCandidate ||
                        IsCandidateCastleBaseCell(cell) ||
                        IsCandidateTowerRoof3Cell(cell)));

                FantasyKingdomStructureStamp woodForest;
                FantasyKingdomStructureStamp stoneGround;
                FantasyKingdomStructureStamp stoneSolid;
                FantasyKingdomStructureStamp ironGround;
                FantasyKingdomStructureStamp ironSolid;
                FantasyKingdomStructureStamp foodGround;
                FantasyKingdomStructureStamp foodHedge;

                if (useFrozenTargetStamps)
                {
                    woodForest = LoadFrozenTargetStamp("FK_V3_Wood_LivingForest_Back", 32);
                    stoneGround = LoadFrozenTargetStamp("FK_V3_Stone_Quarry_Ground", 10);
                    stoneSolid = LoadFrozenTargetStamp("FK_V3_Stone_Quarry_Solid", 13);
                    ironGround = LoadFrozenTargetStamp("FK_V3_Iron_Mine_Ground", 10);
                    ironSolid = LoadFrozenTargetStamp("FK_V3_Iron_Mine_Solid", 12);
                    foodGround = LoadFrozenTargetStamp("FK_V3_Food_Field_Ground", 35);
                    foodHedge = LoadFrozenTargetStamp("FK_V3_Food_Field_Hedge", 5);
                }
                else
                {
                    woodForest = ExtractStamp(
                        "FK_V3_Wood_LivingForest_Back",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(8, 11, 11, 18),
                        FantasyKingdomStampPurpose.ResourceSite,
                        new Vector3Int(7, 0, 0),
                        Rule("Structures", (tile, cell) =>
                        {
                            var targetCell = new Vector3Int(cell.x - 8, cell.y, 0);
                            return StartsWith(tile, "Tree") &&
                                   !protectedCells.Contains(targetCell) &&
                                   targetGrid.GetCellCenterWorld(targetCell).x <= -1.5f;
                        }));

                    stoneGround = ExtractStamp(
                        "FK_V3_Stone_Quarry_Ground",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-11, 4, 5, 5),
                        FantasyKingdomStampPurpose.GroundDetail,
                        new Vector3Int(1, 0, 0),
                        Rule("GroundDetail"));
                    stoneSolid = ExtractStamp(
                        "FK_V3_Stone_Quarry_Solid",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-11, 4, 5, 5),
                        FantasyKingdomStampPurpose.ResourceSite,
                        new Vector3Int(1, 0, 0),
                        Rule("Structures"));

                    ironGround = ExtractStamp(
                        "FK_V3_Iron_Mine_Ground",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-23, -9, 5, 5),
                        FantasyKingdomStampPurpose.GroundDetail,
                        new Vector3Int(1, 1, 0),
                        Rule("GroundDetail"));
                    ironSolid = ExtractStamp(
                        "FK_V3_Iron_Mine_Solid",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-23, -9, 5, 5),
                        FantasyKingdomStampPurpose.ResourceSite,
                        new Vector3Int(1, 1, 0),
                        Rule("Structures"));

                    foodGround = ExtractStamp(
                        "FK_V3_Food_Field_Ground",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-20, -11, 11, 7),
                        FantasyKingdomStampPurpose.ResourceSite,
                        new Vector3Int(5, 3, 0),
                        Rule("GroundDetail", (tile, cell) =>
                            !protectedCells.Contains(cell) &&
                            !ironGroundSourceCells.Contains(cell)));
                    foodHedge = ExtractStamp(
                        "FK_V3_Food_Field_Hedge",
                        targetGrid,
                        TargetScenePath,
                        new RectInt(-20, -11, 11, 7),
                        FantasyKingdomStampPurpose.ResourceSite,
                        new Vector3Int(5, 3, 0),
                        Rule("Structures", (tile, cell) =>
                            (StartsWith(tile, "Tree B2") || StartsWith(tile, "Tree B3")) &&
                            !protectedCells.Contains(cell) &&
                            targetGrid.GetCellCenterWorld(cell).x <= -1.5f));

                    ValidateTargetDerivedStamp(woodForest, 32);
                    ValidateTargetDerivedStamp(stoneGround, 10);
                    ValidateTargetDerivedStamp(stoneSolid, 13);
                    ValidateTargetDerivedStamp(ironGround, 10);
                    ValidateTargetDerivedStamp(ironSolid, 12);
                    ValidateTargetDerivedStamp(foodGround, 35);
                    ValidateTargetDerivedStamp(foodHedge, 5);
                }

                FantasyKingdomStructureStamp granaryShadow = ExtractStamp(
                    "FK_V3_Food_Granary_Shadow",
                    exampleGrid,
                    ExampleScenePath,
                    new RectInt(-34, -51, 7, 9),
                    FantasyKingdomStampPurpose.GroundDetail,
                    new Vector3Int(3, 4, 0),
                    Rule("Shadows2"));
                FantasyKingdomStructureStamp granarySolid = ExtractStamp(
                    "FK_V3_Food_Granary_Solid",
                    exampleGrid,
                    ExampleScenePath,
                    new RectInt(-34, -51, 7, 9),
                    FantasyKingdomStampPurpose.Structure,
                    new Vector3Int(3, 4, 0),
                    Rule("Walls", (tile, cell) => !StartsWith(tile, "Tree")),
                    Rule("Objects", (tile, cell) => StartsWith(tile, "Misc B59")),
                    Rule("Roof1"),
                    Rule("Roof2"));

                Tilemap groundTemplate = FindTilemap(targetGrid, "GroundDetail") ??
                                         FindTilemap(targetGrid, "Grass");
                Tilemap objectTemplate = FindTilemap(exampleGrid, "Walls");
                Tilemap shadowTemplate = FindTilemap(exampleGrid, "Shadows1");
                if (groundTemplate == null || objectTemplate == null || shadowTemplate == null)
                    throw new InvalidOperationException("V3 generated stamp template tilemap'i bulunamadi.");

                Vector3Int woodForestTargetAnchor = new Vector3Int(7, 11, 0);
                if (writeRetouchCandidate)
                {
                    GeneratedStamp cleanedWoodForest = BuildRetouchedLivingForestStamp(
                        targetGrid,
                        objectTemplate,
                        woodForest,
                        woodForestTargetAnchor,
                        castleSolid,
                        new Vector3Int(2, 10, 0),
                        protectedCells);
                    woodForest = cleanedWoodForest.Stamp;
                    woodForestTargetAnchor = cleanedWoodForest.AnchorCell;
                }

                Tilemap grassTemplate = FindTilemap(targetGrid, "Grass");
                if (grassTemplate == null)
                    throw new InvalidOperationException("V3 settlement support icin Grass bulunamadi.");
                var settlementSupportCells = new HashSet<Vector3Int>();
                settlementSupportCells.UnionWith(CollectSolidTargetCells(
                    castleSolid,
                    new Vector3Int(2, 10, 0)));
                settlementSupportCells.UnionWith(CollectSolidTargetCells(
                    woodForest,
                    woodForestTargetAnchor));
                settlementSupportCells.ExceptWith(CollectGroundSupportCells(targetGrid));
                TileBase settlementGroundTile = FindFirstTile(grassTemplate);
                GeneratedStamp settlementSupport = BuildGeneratedStamp(
                    "FK_V3_Settlement_GroundSupport",
                    FantasyKingdomStampPurpose.GroundDetail,
                    targetGrid,
                    grassTemplate,
                    "Ground V3 Settlement Support",
                    "Ground",
                    1,
                    settlementSupportCells
                        .Select(cell => new AbsoluteTileCell(cell, settlementGroundTile))
                        .ToList());
                GeneratedStamp battlefield = BuildTerrainStamp(
                    "FK_V3_Battlefield_CalmGround_And_CaravanRoad",
                    targetGrid,
                    groundTemplate,
                    cell => Between(cell.x - cell.y, 8, 36) &&
                            Between(cell.x + cell.y, -33, 31),
                    protectedCells);
                GeneratedStamp farRightGround = BuildTerrainStamp(
                    "FK_V3_FarRight_CalmGround_And_RoadMouth",
                    targetGrid,
                    groundTemplate,
                    cell => Between(cell.x - cell.y, 37, 58) &&
                            Between(cell.x + cell.y, -33, 31),
                    protectedCells);
                Func<Vector3Int, bool> gateZone = cell =>
                    Between(cell.x - cell.y, 0, 2) &&
                    Between(cell.x + cell.y, -33, 31);
                Func<Vector3Int, bool> moatZone = cell =>
                    Between(cell.x - cell.y, 3, 7) &&
                    Between(cell.x + cell.y, -33, 31);
                GeneratedStamp gateRoad = writeRetouchCandidate
                    ? BuildTerrainStamp(
                        "FK_V3_Gate_CaravanRoad",
                        targetGrid,
                        groundTemplate,
                        gateZone,
                        protectedCells)
                    : BuildRoadOnlyStamp(
                        "FK_V3_Gate_CaravanRoad",
                        targetGrid,
                        groundTemplate,
                        gateZone,
                        protectedCells);
                GeneratedStamp moatRoad = writeRetouchCandidate
                    ? BuildTerrainStamp(
                        "FK_V3_Moat_CaravanRoad",
                        targetGrid,
                        groundTemplate,
                        moatZone,
                        protectedCells)
                    : BuildRoadOnlyStamp(
                        "FK_V3_Moat_CaravanRoad",
                        targetGrid,
                        groundTemplate,
                        moatZone,
                        protectedCells);

                GeneratedStamp retouchFullGround = null;
                if (writeRetouchCandidate)
                {
                    retouchFullGround = BuildRetouchGroundStamp(
                        "FK_V3_FullMap_CalmGround_And_CaravanRoad",
                        targetGrid,
                        groundTemplate,
                        cell => Between(cell.x - cell.y, -1, 58) &&
                                Between(cell.x + cell.y, -33, 31),
                        protectedCells);
                }

                EnemyForestStamps enemyForest = BuildEnemyForestStamps(
                    targetGrid,
                    objectTemplate,
                    shadowTemplate);

                SceneAsset targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
                var placements = new List<FantasyKingdomMapPlacement>
                {
                    Placement("left.settlement.ground_support", "Castle and Forest Ground Support",
                        settlementSupport.Stamp, settlementSupport.AnchorCell,
                        FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("left.castle.shadow", "Stone Citadel Shadow", castleShadow,
                        new Vector3Int(2, 10, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("left.castle.citadel", "All-Stone Citadel", castleSolid,
                        new Vector3Int(2, 10, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.CastleKeep, FantasyKingdomRenderBand.BehindUnits),
                    Placement("left.wood.living_forest", "Dense Living Wood Forest", woodForest,
                        woodForestTargetAnchor, FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.Wood, FantasyKingdomRenderBand.BehindUnits),
                    Placement("left.stone.ground", "Stone Quarry Ground", stoneGround,
                        new Vector3Int(-10, 4, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("left.stone.quarry", "Readable Stone Quarry", stoneSolid,
                        new Vector3Int(-10, 4, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.Stone, FantasyKingdomRenderBand.BehindUnits),
                    Placement("left.iron.ground", "Iron Mine Ground", ironGround,
                        new Vector3Int(-22, -8, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("left.iron.mine", "Readable Iron Mine", ironSolid,
                        new Vector3Int(-22, -8, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.Iron, FantasyKingdomRenderBand.BehindUnits),
                    Placement("left.food.field", "Dominant Food Field", foodGround,
                        new Vector3Int(-15, -8, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.Food, FantasyKingdomRenderBand.Ground),
                    Placement("left.food.hedge", "Food Field Hedge", foodHedge,
                        new Vector3Int(-15, -8, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.BehindUnits),
                    Placement("left.food.granary_shadow", "Granary Shadow", granaryShadow,
                        new Vector3Int(-8, 0, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("left.food.granary", "Field-Edge Granary", granarySolid,
                        new Vector3Int(-8, 0, 0), FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.BehindUnits)
                };

                if (writeRetouchCandidate)
                {
                    placements.Add(Placement(
                        "battlefield.calm_ground",
                        "Seamless Full Ground and Smooth Caravan Road",
                        retouchFullGround.Stamp,
                        retouchFullGround.AnchorCell,
                        FantasyKingdomMapZone.FullMapGround,
                        FantasyKingdomGameplayAnchor.None,
                        FantasyKingdomRenderBand.Ground));
                }
                else
                {
                    placements.Add(Placement("road.gate", "Caravan Road Gate Approach", gateRoad.Stamp,
                        gateRoad.AnchorCell, FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground));
                    placements.Add(Placement("road.moat", "Caravan Road Moat Approach", moatRoad.Stamp,
                        moatRoad.AnchorCell, FantasyKingdomMapZone.MoatGround,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground));
                    placements.Add(Placement("battlefield.calm_ground", "Calm Open Battlefield and S Road",
                        battlefield.Stamp, battlefield.AnchorCell, FantasyKingdomMapZone.Battlefield,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground));
                    placements.Add(Placement("far_right.calm_ground", "Far Right Road Mouth Ground",
                        farRightGround.Stamp, farRightGround.AnchorCell,
                        FantasyKingdomMapZone.FarRightFrame,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground));
                }

                placements.Add(Placement("enemy_forest.shadow", "Enemy Forest Soft Ground Shadows",
                    enemyForest.Shadow.Stamp, enemyForest.Shadow.AnchorCell,
                    FantasyKingdomMapZone.FarRightFrame,
                    FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground));
                placements.Add(Placement("enemy_forest.back", "Enemy Forest Behind Zombies",
                    enemyForest.Back.Stamp, enemyForest.Back.AnchorCell,
                    FantasyKingdomMapZone.FarRightFrame,
                    FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.BehindUnits));
                placements.Add(Placement("enemy_forest.front", "Enemy Forest Front Occluder",
                    enemyForest.Front.Stamp, enemyForest.Front.AnchorCell,
                    FantasyKingdomMapZone.FarRightFrame,
                    FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.InFrontOfUnits));

                var generatedLayout = ScriptableObject.CreateInstance<FantasyKingdomMapLayout>();
                generatedLayout.name = writeRetouchCandidate
                    ? "FK_NewGameScene_FullMap_V3_RetouchPreview"
                    : "FK_NewGameScene_FullMap_V3_Draft";
                generatedLayout.Initialize(
                    targetScene,
                    GetHierarchyPath(targetGrid.transform),
                    writeRetouchCandidate
                        ? "NewGameScene-VisualRetouch-Candidate-v3.1"
                        : "NewGameScene-ApprovedVisualRebuild-v3",
                    Seed,
                    placements,
                    FantasyKingdomMapLayout.CurrentSchemaVersion);

                FantasyKingdomMapLayout layout = SaveOrReplace(
                    generatedLayout,
                    writeRetouchCandidate
                        ? FantasyKingdomMapLayoutFactory.RetouchCandidateLayoutPath
                        : FantasyKingdomMapLayoutFactory.DefaultLayoutPath);
                AssetDatabase.SaveAssets();

                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
                return layout;
            }
        }

        private static FantasyKingdomMapPlacement Placement(
            string id,
            string label,
            FantasyKingdomStructureStamp stamp,
            Vector3Int targetAnchor,
            FantasyKingdomMapZone zone,
            FantasyKingdomGameplayAnchor gameplayAnchor,
            FantasyKingdomRenderBand renderBand)
        {
            return new FantasyKingdomMapPlacement(
                id,
                label,
                stamp,
                targetAnchor,
                zone,
                gameplayAnchor,
                renderBand);
        }

        private static FantasyKingdomStructureStamp ExtractStamp(
            string assetName,
            Grid sourceGrid,
            string sourceScenePath,
            RectInt sourceRegion,
            FantasyKingdomStampPurpose purpose,
            Vector3Int anchorLocalCell,
            params LayerRule[] rules)
        {
            var extractedLayers = new List<FantasyKingdomStampLayer>();
            Tilemap[] maps = sourceGrid.GetComponentsInChildren<Tilemap>(true);
            for (int ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                LayerRule rule = rules[ruleIndex];
                Tilemap map = maps.FirstOrDefault(candidate => string.Equals(
                    candidate.name,
                    rule.LayerName,
                    StringComparison.OrdinalIgnoreCase));
                if (map == null)
                    throw new InvalidOperationException(
                        assetName + " kaynak layer bulunamadi: " + rule.LayerName);

                var cells = new List<FantasyKingdomStampCell>();
                for (int y = sourceRegion.yMin; y < sourceRegion.yMax; y++)
                {
                    for (int x = sourceRegion.xMin; x < sourceRegion.xMax; x++)
                    {
                        var sourceCell = new Vector3Int(x, y, 0);
                        TileBase tile = map.GetTile(sourceCell);
                        if (tile == null || (rule.Filter != null && !rule.Filter(tile, sourceCell)))
                            continue;

                        cells.Add(new FantasyKingdomStampCell(
                            new Vector3Int(x - sourceRegion.xMin, y - sourceRegion.yMin, 0),
                            tile,
                            map.GetTransformMatrix(sourceCell),
                            map.GetColor(sourceCell),
                            map.GetTileFlags(sourceCell)));
                    }
                }

                if (cells.Count > 0)
                {
                    extractedLayers.Add(new FantasyKingdomStampLayer(
                        GetHierarchyPath(map.transform),
                        map,
                        map.GetComponent<TilemapRenderer>(),
                        cells));
                }
            }

            if (extractedLayers.Count == 0)
                throw new InvalidOperationException(assetName + " icin hic tile cikarilamadi.");

            string outputAssetName = GetOutputAssetName(assetName);
            var generated = ScriptableObject.CreateInstance<FantasyKingdomStructureStamp>();
            generated.name = outputAssetName;
            generated.Initialize(
                sourceScenePath,
                GetHierarchyPath(sourceGrid.transform),
                sourceRegion,
                sourceGrid,
                purpose,
                extractedLayers);
            generated.SetAnchorLocalCell(anchorLocalCell);
            return SaveOrReplace(generated, StampFolder + "/" + outputAssetName + ".asset");
        }

        private static GeneratedStamp BuildRetouchedLivingForestStamp(
            Grid targetGrid,
            Tilemap objectTemplate,
            FantasyKingdomStructureStamp sourceForest,
            Vector3Int sourceTargetAnchor,
            FantasyKingdomStructureStamp castleSolid,
            Vector3Int castleTargetAnchor,
            HashSet<Vector3Int> protectedCells)
        {
            Vector3Int sourceOrigin = sourceTargetAnchor - sourceForest.AnchorLocalCell;
            var originalTrees = new List<AbsoluteTileCell>();
            IReadOnlyList<FantasyKingdomStampLayer> sourceLayers = sourceForest.Layers;
            for (int layerIndex = 0; layerIndex < sourceLayers.Count; layerIndex++)
            {
                IReadOnlyList<FantasyKingdomStampCell> sourceCells = sourceLayers[layerIndex].Cells;
                for (int cellIndex = 0; cellIndex < sourceCells.Count; cellIndex++)
                {
                    FantasyKingdomStampCell sourceCell = sourceCells[cellIndex];
                    if (!StartsWith(sourceCell.Tile, "Tree"))
                        continue;
                    originalTrees.Add(new AbsoluteTileCell(
                        sourceOrigin + sourceCell.LocalPosition,
                        sourceCell.Tile,
                        sourceCell.TransformMatrix,
                        sourceCell.Color,
                        sourceCell.Flags));
                }
            }

            if (originalTrees.Count != 32)
            {
                throw new InvalidOperationException(
                    "Retouch living forest beklenen 32 kaynak agaci bulamadi: " +
                    originalTrees.Count);
            }

            HashSet<Vector3Int> castleCells = CollectSolidTargetCells(
                castleSolid,
                castleTargetAnchor);
            var result = originalTrees
                .Where(tree => !IsInsideCastleTreeBuffer(tree.Position, castleCells) &&
                               !protectedCells.Contains(tree.Position))
                .ToList();
            var used = new HashSet<Vector3Int>(result.Select(tree => tree.Position));

            List<Vector3Int> replacementCandidates = EnumerateCandidateCells()
                .Where(cell => cell.x >= -4 && cell.x <= 12 &&
                               cell.y >= 11 && cell.y <= 34 &&
                               targetGrid.GetCellCenterWorld(cell).x <= -1.5f &&
                               !protectedCells.Contains(cell) &&
                               !used.Contains(cell) &&
                               !IsInsideCastleTreeBuffer(cell, castleCells))
                .OrderBy(cell => PositiveHash(cell, Seed + 1701))
                .ToList();

            for (int candidateIndex = 0;
                 result.Count < 32 && candidateIndex < replacementCandidates.Count;
                 candidateIndex++)
            {
                Vector3Int position = replacementCandidates[candidateIndex];
                AbsoluteTileCell template = originalTrees[
                    PositiveHash(position, Seed + 1723) % originalTrees.Count];
                result.Add(new AbsoluteTileCell(
                    position,
                    template.Tile,
                    template.TransformMatrix,
                    template.Color,
                    template.Flags));
                used.Add(position);
            }

            if (result.Count != 32)
            {
                throw new InvalidOperationException(
                    "Retouch living forest kale buffer'i disinda 32 agaca tamamlanamadi: " +
                    result.Count);
            }

            return BuildGeneratedStamp(
                "FK_V3_Wood_LivingForest_Back",
                FantasyKingdomStampPurpose.ResourceSite,
                targetGrid,
                objectTemplate,
                "Structures",
                "Objects",
                10,
                result,
                requiredTargetAnchor: sourceTargetAnchor);
        }

        private static bool IsInsideCastleTreeBuffer(
            Vector3Int treeCell,
            HashSet<Vector3Int> castleCells)
        {
            foreach (Vector3Int castleCell in castleCells)
            {
                if (Mathf.Max(
                        Mathf.Abs(treeCell.x - castleCell.x),
                        Mathf.Abs(treeCell.y - castleCell.y)) < 2)
                {
                    return true;
                }
            }
            return false;
        }

        private static GeneratedStamp BuildTerrainStamp(
            string assetName,
            Grid targetGrid,
            Tilemap template,
            Func<Vector3Int, bool> zonePredicate,
            HashSet<Vector3Int> protectedCells)
        {
            TileBase calmNorth = LoadEnvironmentTile("Ground A1_N");
            TileBase calmEast = LoadEnvironmentTile("Ground A1_E");
            TileBase[] roadPalette = LoadRoadPalette();
            var cells = new List<AbsoluteTileCell>();

            foreach (Vector3Int cell in EnumerateCandidateCells())
            {
                if (!zonePredicate(cell) || protectedCells.Contains(cell))
                    continue;

                int hash = PositiveHash(cell, Seed);
                TileBase tile;
                if (IsCaravanRoadCell(targetGrid, cell))
                {
                    tile = SelectRoadTile(targetGrid, cell, roadPalette, hash);
                }
                else
                {
                    int variation = hash % 100;
                    tile = variation < 10 ? calmEast : calmNorth;
                }
                cells.Add(new AbsoluteTileCell(cell, tile));
            }

            return BuildGeneratedStamp(
                assetName,
                FantasyKingdomStampPurpose.GroundDetail,
                targetGrid,
                template,
                "Ground V3 Calm Terrain",
                "Ground",
                2,
                cells);
        }

        private static GeneratedStamp BuildRetouchGroundStamp(
            string assetName,
            Grid targetGrid,
            Tilemap template,
            Func<Vector3Int, bool> zonePredicate,
            HashSet<Vector3Int> protectedCells)
        {
            TileBase calmNorth = LoadEnvironmentTile("Ground A1_N");
            TileBase calmEast = LoadEnvironmentTile("Ground A1_E");
            Dictionary<Vector3Int, TileBase> roadRecipe =
                BuildReferenceCaravanRoadRecipe();
            var cells = new List<AbsoluteTileCell>();

            foreach (Vector3Int cell in EnumerateCandidateCells())
            {
                if (!zonePredicate(cell) || protectedCells.Contains(cell))
                    continue;

                int hash = PositiveHash(cell, Seed + 2401);
                if (roadRecipe.TryGetValue(cell, out TileBase roadTile))
                {
                    cells.Add(new AbsoluteTileCell(cell, roadTile));
                    continue;
                }

                int horizontalBand = cell.x - cell.y;
                cells.Add(new AbsoluteTileCell(
                    cell,
                    horizontalBand <= 1
                        ? calmEast
                        : hash % 100 < 8 ? calmEast : calmNorth));
            }

            return BuildGeneratedStamp(
                assetName,
                FantasyKingdomStampPurpose.GroundDetail,
                targetGrid,
                template,
                "Ground V3 Retouch Terrain",
                "Ground",
                2,
                cells);
        }

        private static Dictionary<Vector3Int, TileBase> BuildReferenceCaravanRoadRecipe()
        {
            var road = new Dictionary<Vector3Int, TileBase>();

            // Example Scene uzun yolunun x=45, y=-123..-103 omurgasi.
            for (int step = 0; step <= 20; step++)
            {
                int sourceY = -103 - step;
                string tileName = sourceY <= -115
                    ? "Ground I1_S"
                    : "Ground I1_N";
                SetReferenceRoadCell(road, 0, -step, tileName);
            }

            // Example Scene organik alt yolundaki vertical->horizontal bend parcasi.
            // Source offset: (14,-115) -> target (0,-20).
            SetReferenceRoadCell(road, 0, -20, "Ground I1_S");
            SetReferenceRoadCell(road, 0, -21, "Ground I1_S");
            SetReferenceRoadCell(road, 0, -22, "Ground I10_N");
            SetReferenceRoadCell(road, 1, -22, "Ground I9_N");
            SetReferenceRoadCell(road, 0, -23, "Ground I9_S");
            SetReferenceRoadCell(road, 1, -23, "Ground I8_W");
            SetReferenceRoadCell(road, 2, -23, "Ground I9_N");
            SetReferenceRoadCell(road, 1, -24, "Ground I9_S");
            SetReferenceRoadCell(road, 2, -24, "Ground I8_W");
            SetReferenceRoadCell(road, 3, -24, "Ground I9_N");
            SetReferenceRoadCell(road, 2, -25, "Ground I9_S");
            SetReferenceRoadCell(road, 3, -25, "Ground I7_E");
            SetReferenceRoadCell(road, 4, -25, "Ground I10_W");
            SetReferenceRoadCell(road, 5, -25, "Ground I9_E");
            SetReferenceRoadCell(road, 6, -25, "Ground I9_S");
            SetReferenceRoadCell(road, 7, -25, "Ground I7_E");
            SetReferenceRoadCell(road, 8, -25, "Ground I9_E");

            // Reference duz omurgasinin 90 derece CW yon eslemesi: I1_S -> I1_W.
            for (int x = 5; x <= 16; x++)
                SetReferenceRoadCell(road, x, -25, "Ground I1_W");

            return road;
        }

        private static void SetReferenceRoadCell(
            Dictionary<Vector3Int, TileBase> road,
            int x,
            int y,
            string tileName)
        {
            road[new Vector3Int(x, y, 0)] = LoadEnvironmentTile(tileName);
        }

        private static GeneratedStamp BuildRoadOnlyStamp(
            string assetName,
            Grid targetGrid,
            Tilemap template,
            Func<Vector3Int, bool> zonePredicate,
            HashSet<Vector3Int> protectedCells)
        {
            TileBase[] roadPalette = LoadRoadPalette();
            var cells = new List<AbsoluteTileCell>();
            foreach (Vector3Int cell in EnumerateCandidateCells())
            {
                if (!zonePredicate(cell) || protectedCells.Contains(cell) ||
                    !IsCaravanRoadCell(targetGrid, cell))
                {
                    continue;
                }
                cells.Add(new AbsoluteTileCell(
                    cell,
                    SelectRoadTile(
                        targetGrid,
                        cell,
                        roadPalette,
                        PositiveHash(cell, Seed + 41))));
            }

            return BuildGeneratedStamp(
                assetName,
                FantasyKingdomStampPurpose.GroundDetail,
                targetGrid,
                template,
                "Ground V3 Caravan Road",
                "Ground",
                3,
                cells);
        }

        private static EnemyForestStamps BuildEnemyForestStamps(
            Grid targetGrid,
            Tilemap objectTemplate,
            Tilemap shadowTemplate)
        {
            if (writeRetouchCandidate)
            {
                return BuildRetouchedEnemyForestStamps(
                    targetGrid,
                    objectTemplate,
                    shadowTemplate);
            }

            List<Vector3Int> allBandCells = EnumerateCandidateCells()
                .Where(cell => Between(cell.x - cell.y, 36, 58) &&
                               Between(cell.x + cell.y, -33, 31))
                .ToList();

            Func<Vector3Int, bool> mouthClear = cell =>
            {
                Vector3 world = targetGrid.GetCellCenterWorld(cell);
                return world.x <= 21f && world.y >= 1.75f && world.y <= 4.25f;
            };

            List<Vector3Int> lipCandidates = allBandCells
                .Where(cell => Between(cell.x - cell.y, 36, 41) && !mouthClear(cell))
                .ToList();
            var frontLip = new List<Vector3Int>();
            for (int band = -8; band < 8; band++)
            {
                List<Vector3Int> bandCells = lipCandidates
                    .Where(cell =>
                    {
                        float y = targetGrid.GetCellCenterWorld(cell).y;
                        return y >= band && y < band + 1f;
                    })
                    .OrderBy(cell => PositiveHash(cell, Seed + band * 13))
                    .Take(2)
                    .ToList();
                frontLip.AddRange(bandCells);
            }
            frontLip.AddRange(lipCandidates
                .Where(cell => !frontLip.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 101))
                .Take(42 - frontLip.Count));

            var used = new HashSet<Vector3Int>(frontLip);
            List<Vector3Int> frontDeep = allBandCells
                .Where(cell => Between(cell.x - cell.y, 42, 58) &&
                               !mouthClear(cell) && !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 211))
                .Take(18)
                .ToList();
            used.UnionWith(frontDeep);

            List<Vector3Int> visibleBack = allBandCells
                .Where(cell => Between(cell.x - cell.y, 37, 44) &&
                               !mouthClear(cell) && !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 307))
                .Take(110)
                .ToList();
            used.UnionWith(visibleBack);
            List<Vector3Int> deepBack = allBandCells
                .Where(cell => Between(cell.x - cell.y, 45, 58) &&
                               !mouthClear(cell) && !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 353))
                .Take(30)
                .ToList();
            var backCells = new List<Vector3Int>(visibleBack);
            backCells.AddRange(deepBack);

            var frontCells = new List<Vector3Int>(frontLip);
            frontCells.AddRange(frontDeep);
            if (frontCells.Count != 60 || backCells.Count != 140)
                throw new InvalidOperationException(
                    "Enemy forest mask beklenen 60 front / 140 back hucresini uretemedi.");

            List<TileBase> backPalette = ExpandPalette(
                Pair("Tree D3_W", 20), Pair("Tree D1_S", 16), Pair("Tree D3_E", 14),
                Pair("Tree D1_N", 13), Pair("Tree D1_E", 12), Pair("Tree D2_W", 11),
                Pair("Tree D3_N", 11), Pair("Tree D2_E", 10), Pair("Tree D2_N", 9),
                Pair("Tree D3_S", 9), Pair("Tree D1_W", 8), Pair("Tree D2_S", 7));
            List<TileBase> frontPalette = ExpandPalette(
                Pair("Tree E1_W", 37), Pair("Tree E3_S", 23));

            backCells = backCells
                .OrderBy(cell => PositiveHash(cell, Seed + 401))
                .ToList();
            frontCells = frontCells
                .OrderBy(cell => PositiveHash(cell, Seed + 503))
                .ToList();

            var backTiles = new List<AbsoluteTileCell>();
            var frontTiles = new List<AbsoluteTileCell>();
            var shadowTiles = new List<AbsoluteTileCell>();
            for (int i = 0; i < backCells.Count; i++)
            {
                var item = new AbsoluteTileCell(backCells[i], backPalette[i]);
                backTiles.Add(item);
                shadowTiles.Add(item);
            }
            for (int i = 0; i < frontCells.Count; i++)
            {
                var item = new AbsoluteTileCell(frontCells[i], frontPalette[i]);
                frontTiles.Add(item);
                shadowTiles.Add(item);
            }

            return new EnemyForestStamps
            {
                Shadow = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_Shadow",
                    FantasyKingdomStampPurpose.GroundDetail,
                    targetGrid,
                    shadowTemplate,
                    "EnemyForestShadow",
                    "Ground",
                    6,
                    shadowTiles),
                Back = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_Back",
                    FantasyKingdomStampPurpose.BattlefieldDecoration,
                    targetGrid,
                    objectTemplate,
                    "EnemyForestBack",
                    "Objects",
                    11,
                    backTiles),
                Front = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_FrontOccluder",
                    FantasyKingdomStampPurpose.BattlefieldDecoration,
                    targetGrid,
                    objectTemplate,
                    "EnemyForestFrontOccluder",
                    "Wall",
                    4,
                    frontTiles)
            };
        }

        private static EnemyForestStamps BuildRetouchedEnemyForestStamps(
            Grid targetGrid,
            Tilemap objectTemplate,
            Tilemap shadowTemplate)
        {
            List<Vector3Int> allBandCells = EnumerateCandidateCells()
                .Where(cell => Between(cell.x - cell.y, 36, 58) &&
                               Between(cell.x + cell.y, -33, 31))
                .ToList();

            List<Vector3Int> lipCandidates = allBandCells
                .Where(cell => Between(cell.x - cell.y, 36, 41) &&
                               !IsForestRoadCorridor(targetGrid, cell))
                .ToList();
            var frontLip = new List<Vector3Int>();
            for (int band = -8; band < 8; band++)
            {
                List<Vector3Int> bandCells = lipCandidates
                    .Where(cell =>
                    {
                        float y = targetGrid.GetCellCenterWorld(cell).y;
                        return y >= band && y < band + 1f;
                    })
                    .OrderBy(cell => PositiveHash(cell, Seed + band * 13))
                    .Take(2)
                    .ToList();
                frontLip.AddRange(bandCells);
            }
            frontLip.AddRange(lipCandidates
                .Where(cell => !frontLip.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 2101))
                .Take(42 - frontLip.Count));

            var used = new HashSet<Vector3Int>(frontLip);
            List<Vector3Int> frontDeep = allBandCells
                .Where(cell => Between(cell.x - cell.y, 42, 58) &&
                               !IsForestRoadCorridor(targetGrid, cell) &&
                               !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 2113))
                .Take(18)
                .ToList();
            used.UnionWith(frontDeep);

            List<Vector3Int> visibleBack = allBandCells
                .Where(cell => Between(cell.x - cell.y, 37, 44) &&
                               !IsForestRoadCorridor(targetGrid, cell) &&
                               !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 2129))
                .Take(110)
                .ToList();
            used.UnionWith(visibleBack);
            List<Vector3Int> deepBack = allBandCells
                .Where(cell => Between(cell.x - cell.y, 45, 58) &&
                               !IsForestRoadCorridor(targetGrid, cell) &&
                               !used.Contains(cell))
                .OrderBy(cell => PositiveHash(cell, Seed + 2141))
                .Take(30)
                .ToList();

            var frontCells = new List<Vector3Int>(frontLip);
            frontCells.AddRange(frontDeep);
            var backCells = new List<Vector3Int>(visibleBack);
            backCells.AddRange(deepBack);
            if (frontCells.Count != 60 || backCells.Count != 140)
            {
                throw new InvalidOperationException(
                    "Retouch enemy forest beklenen 60 front / 140 back agaci uretemedi.");
            }

            List<TileBase> backPalette = ExpandPalette(
                Pair("Tree D3_W", 20), Pair("Tree D1_S", 16), Pair("Tree D3_E", 14),
                Pair("Tree D1_N", 13), Pair("Tree D1_E", 12), Pair("Tree D2_W", 11),
                Pair("Tree D3_N", 11), Pair("Tree D2_E", 10), Pair("Tree D2_N", 9),
                Pair("Tree D3_S", 9), Pair("Tree D1_W", 8), Pair("Tree D2_S", 7));
            List<TileBase> frontPalette = ExpandPalette(
                Pair("Tree D3_W", 14), Pair("Tree D1_S", 10), Pair("Tree D3_E", 8),
                Pair("Tree D1_N", 8), Pair("Tree D2_W", 7), Pair("Tree D3_N", 5),
                Pair("Tree D2_E", 4), Pair("Tree D1_E", 4));

            backCells = backCells
                .OrderBy(cell => PositiveHash(cell, Seed + 2003))
                .ToList();
            frontCells = frontCells
                .OrderBy(cell => PositiveHash(cell, Seed + 2017))
                .ToList();

            var backTiles = new List<AbsoluteTileCell>();
            var frontTiles = new List<AbsoluteTileCell>();
            var shadowTiles = new List<AbsoluteTileCell>();
            TileBase softShadowTile = LoadEnvironmentTile("Misc C7_N");
            Matrix4x4 softShadowTransform = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.Euler(0f, 0f, 85f),
                new Vector3(1f, 0.4f, 1f));

            for (int i = 0; i < backCells.Count; i++)
            {
                backTiles.Add(new AbsoluteTileCell(backCells[i], backPalette[i]));
                shadowTiles.Add(new AbsoluteTileCell(
                    backCells[i],
                    softShadowTile,
                    softShadowTransform,
                    Color.white,
                    TileFlags.None));
            }
            for (int i = 0; i < frontCells.Count; i++)
            {
                frontTiles.Add(new AbsoluteTileCell(frontCells[i], frontPalette[i]));
                shadowTiles.Add(new AbsoluteTileCell(
                    frontCells[i],
                    softShadowTile,
                    softShadowTransform,
                    Color.white,
                    TileFlags.None));
            }

            return new EnemyForestStamps
            {
                Shadow = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_Shadow",
                    FantasyKingdomStampPurpose.GroundDetail,
                    targetGrid,
                    shadowTemplate,
                    "EnemyForestShadow",
                    "Ground",
                    6,
                    shadowTiles,
                    new Color(0f, 0f, 0f, 0.1f)),
                Back = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_Back",
                    FantasyKingdomStampPurpose.BattlefieldDecoration,
                    targetGrid,
                    objectTemplate,
                    "EnemyForestBack",
                    "Objects",
                    11,
                    backTiles),
                Front = BuildGeneratedStamp(
                    "FK_V3_EnemyForest_FrontOccluder",
                    FantasyKingdomStampPurpose.BattlefieldDecoration,
                    targetGrid,
                    objectTemplate,
                    "EnemyForestFrontOccluder",
                    "Wall",
                    4,
                    frontTiles)
            };
        }

        private static List<Vector3Int> SelectDenseForestCells(
            Grid targetGrid,
            List<Vector3Int> candidates,
            float minimumWorldX,
            float maximumWorldX,
            int desiredCount,
            int seed,
            HashSet<Vector3Int> excluded)
        {
            List<Vector3Int> available = candidates
                .Where(cell =>
                {
                    Vector3 world = targetGrid.GetCellCenterWorld(cell);
                    return world.x >= minimumWorldX && world.x <= maximumWorldX &&
                           !excluded.Contains(cell) &&
                           !IsForestRoadCorridor(targetGrid, cell);
                })
                .ToList();
            var selected = new List<Vector3Int>();
            var selectedSet = new HashSet<Vector3Int>();
            const int bandCount = 16;
            int basePerBand = desiredCount / bandCount;
            int remainder = desiredCount % bandCount;

            for (int bandIndex = 0; bandIndex < bandCount; bandIndex++)
            {
                int band = -8 + bandIndex;
                int targetInBand = basePerBand + (bandIndex < remainder ? 1 : 0);
                List<Vector3Int> row = available
                    .Where(cell =>
                    {
                        float worldY = targetGrid.GetCellCenterWorld(cell).y;
                        return worldY >= band && worldY < band + 1f;
                    })
                    .ToList();

                for (int slot = 0; slot < targetInBand && row.Count > 0; slot++)
                {
                    float targetX = Mathf.Lerp(
                        minimumWorldX,
                        maximumWorldX,
                        (slot + 0.5f) / targetInBand);
                    List<Vector3Int> remaining = row
                        .Where(cell => !selectedSet.Contains(cell))
                        .ToList();
                    if (remaining.Count == 0)
                        break;
                    Vector3Int chosen = remaining
                        .OrderBy(cell => Mathf.Abs(
                            targetGrid.GetCellCenterWorld(cell).x - targetX))
                        .ThenBy(cell => PositiveHash(cell, seed + bandIndex * 31 + slot))
                        .FirstOrDefault();
                    if (selectedSet.Add(chosen))
                        selected.Add(chosen);
                }
            }

            foreach (Vector3Int candidate in available
                         .Where(cell => !selectedSet.Contains(cell))
                         .OrderBy(cell => PositiveHash(cell, seed + 701)))
            {
                if (selected.Count >= desiredCount)
                    break;
                selectedSet.Add(candidate);
                selected.Add(candidate);
            }

            if (selected.Count != desiredCount)
            {
                throw new InvalidOperationException(
                    "Retouch enemy forest maskesi " + desiredCount +
                    " agaca tamamlanamadi: " + selected.Count);
            }
            return selected;
        }

        private static GeneratedStamp BuildGeneratedStamp(
            string assetName,
            FantasyKingdomStampPurpose purpose,
            Grid targetGrid,
            Tilemap template,
            string layerName,
            string sortingLayer,
            int sortingOrder,
            List<AbsoluteTileCell> absoluteCells,
            Color? layerTintOverride = null,
            Vector3Int? requiredTargetAnchor = null)
        {
            if (absoluteCells == null || absoluteCells.Count == 0)
                throw new InvalidOperationException(assetName + " generated cell maskesi bos.");

            int minX = absoluteCells.Min(cell => cell.Position.x);
            int minY = absoluteCells.Min(cell => cell.Position.y);
            int maxX = absoluteCells.Max(cell => cell.Position.x);
            int maxY = absoluteCells.Max(cell => cell.Position.y);
            if (requiredTargetAnchor.HasValue)
            {
                Vector3Int required = requiredTargetAnchor.Value;
                minX = Mathf.Min(minX, required.x);
                minY = Mathf.Min(minY, required.y);
                maxX = Mathf.Max(maxX, required.x);
                maxY = Mathf.Max(maxY, required.y);
            }
            var origin = new Vector3Int(minX, minY, 0);
            var stampCells = absoluteCells.Select(cell => new FantasyKingdomStampCell(
                cell.Position - origin,
                cell.Tile,
                cell.TransformMatrix,
                cell.Color,
                cell.Flags)).ToList();

            TilemapRenderer templateRenderer = template.GetComponent<TilemapRenderer>();
            var layer = new FantasyKingdomStampLayer(
                "Generated/V3/" + layerName,
                layerName,
                sortingLayer,
                sortingOrder,
                templateRenderer != null ? templateRenderer.mode : TilemapRenderer.Mode.Chunk,
                templateRenderer != null
                    ? templateRenderer.sortOrder
                    : TilemapRenderer.SortOrder.BottomLeft,
                template.tileAnchor,
                layerTintOverride ?? template.color,
                template.orientation,
                template.orientationMatrix,
                stampCells);

            string outputAssetName = GetOutputAssetName(assetName);
            var generated = ScriptableObject.CreateInstance<FantasyKingdomStructureStamp>();
            generated.name = outputAssetName;
            generated.Initialize(
                TargetScenePath,
                GetHierarchyPath(targetGrid.transform),
                new RectInt(0, 0, maxX - minX + 1, maxY - minY + 1),
                targetGrid,
                purpose,
                new List<FantasyKingdomStampLayer> { layer });
            Vector3Int anchorLocal = requiredTargetAnchor.HasValue
                ? requiredTargetAnchor.Value - origin
                : Vector3Int.zero;
            generated.SetAnchorLocalCell(anchorLocal);
            FantasyKingdomStructureStamp saved = SaveOrReplace(
                generated,
                StampFolder + "/" + outputAssetName + ".asset");
            return new GeneratedStamp(
                saved,
                requiredTargetAnchor ?? origin);
        }

        private static string GetOutputAssetName(string assetName)
        {
            return writeRetouchCandidate ? assetName + RetouchAssetSuffix : assetName;
        }

        private static bool IsCandidateCastleBaseCell(Vector3Int cell)
        {
            return cell.x <= 81 && cell.y <= -20;
        }

        private static bool IsCandidateTowerRoof2Cell(Vector3Int cell)
        {
            bool isLeftTowerColumn = cell.x == 73 || cell.x == 74;
            bool isRightTowerColumn = cell.x == 82 || cell.x == 83;
            bool isLowerCapBand = cell.y >= -30 && cell.y <= -28;
            bool isUpperCapBand = cell.y >= -20 && cell.y <= -18;
            return (isLeftTowerColumn || isRightTowerColumn) &&
                   (isLowerCapBand || isUpperCapBand);
        }

        private static bool IsCandidateTowerRoof3Cell(Vector3Int cell)
        {
            return IsCandidateRoof3Patch(cell, 75, -28) ||
                   IsCandidateRoof3Patch(cell, 75, -18) ||
                   IsCandidateRoof3Patch(cell, 84, -28) ||
                   IsCandidateRoof3Patch(cell, 84, -18);
        }

        private static bool IsCandidateRoof3Patch(Vector3Int cell, int anchorX, int anchorY)
        {
            return cell.x == anchorX &&
                   (cell.y == anchorY || cell.y == anchorY + 1 || cell.y == anchorY + 2) ||
                   cell.x == anchorX + 1 && cell.y == anchorY;
        }

        private static bool IsCaravanRoadCell(Grid grid, Vector3Int cell)
        {
            if (!writeRetouchCandidate)
                return IsLegacyCaravanRoadCell(grid, cell);

            Vector3 point = grid.GetCellCenterWorld(cell);
            float distance;
            Vector2 tangent;
            GetNearestRoadInfo(point, out distance, out tangent);
            return distance <= RoadHalfWidth;
        }

        private static bool IsLegacyCaravanRoadCell(Grid grid, Vector3Int cell)
        {
            Vector3 point = grid.GetCellCenterWorld(cell);
            Vector3Int[] controlCells = GetLegacyRoadControlCells();
            for (int i = 0; i < controlCells.Length - 1; i++)
            {
                Vector3 start = grid.GetCellCenterWorld(controlCells[i]);
                Vector3 end = grid.GetCellCenterWorld(controlCells[i + 1]);
                if (DistancePointToSegment2D(point, start, end) <= 0.55f)
                    return true;
            }
            return false;
        }

        private static Vector3Int[] GetLegacyRoadControlCells()
        {
            return new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -8, 0),
                new Vector3Int(3, -13, 0),
                new Vector3Int(11, -11, 0),
                new Vector3Int(19, -9, 0),
                new Vector3Int(20, -14, 0),
                new Vector3Int(25, -14, 0)
            };
        }

        private static Vector2[] BuildRoadCurveSamples()
        {
            const int subdivisionsPerSegment = 12;
            var samples = new List<Vector2>();
            for (int segment = 0; segment < RoadControlPoints.Length - 1; segment++)
            {
                Vector2 p0 = RoadControlPoints[Mathf.Max(0, segment - 1)];
                Vector2 p1 = RoadControlPoints[segment];
                Vector2 p2 = RoadControlPoints[segment + 1];
                Vector2 p3 = RoadControlPoints[Mathf.Min(
                    RoadControlPoints.Length - 1,
                    segment + 2)];
                int firstStep = segment == 0 ? 0 : 1;
                for (int step = firstStep; step <= subdivisionsPerSegment; step++)
                {
                    float t = step / (float)subdivisionsPerSegment;
                    samples.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            return samples.ToArray();
        }

        private static Vector2 CatmullRom(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static void GetNearestRoadInfo(
            Vector2 point,
            out float nearestDistance,
            out Vector2 nearestTangent)
        {
            float signedDistance;
            float progress;
            GetNearestRoadInfo(
                point,
                out nearestDistance,
                out nearestTangent,
                out signedDistance,
                out progress);
        }

        private static void GetNearestRoadInfo(
            Vector2 point,
            out float nearestDistance,
            out Vector2 nearestTangent,
            out float nearestSignedDistance,
            out float nearestProgress)
        {
            nearestDistance = float.MaxValue;
            nearestTangent = Vector2.right;
            nearestSignedDistance = 0f;
            nearestProgress = 0f;
            for (int sampleIndex = 0; sampleIndex < RoadCurveSamples.Length - 1; sampleIndex++)
            {
                Vector2 start = RoadCurveSamples[sampleIndex];
                Vector2 end = RoadCurveSamples[sampleIndex + 1];
                Vector2 delta = end - start;
                float segmentLengthSquared = delta.sqrMagnitude;
                float segmentProgress = segmentLengthSquared > 0.000001f
                    ? Mathf.Clamp01(Vector2.Dot(point - start, delta) / segmentLengthSquared)
                    : 0f;
                Vector2 nearestPoint = start + delta * segmentProgress;
                Vector2 offset = point - nearestPoint;
                float distance = offset.magnitude;
                if (distance >= nearestDistance)
                    continue;
                nearestDistance = distance;
                nearestTangent = delta.sqrMagnitude > 0.000001f
                    ? delta.normalized
                    : Vector2.right;
                nearestSignedDistance = Vector2.Dot(
                    offset,
                    new Vector2(-nearestTangent.y, nearestTangent.x));
                nearestProgress = (sampleIndex + segmentProgress) /
                                  (RoadCurveSamples.Length - 1f);
            }
        }

        private static bool IsForestRoadCorridor(Grid grid, Vector3Int cell)
        {
            Vector2 point = grid.GetCellCenterWorld(cell);
            if (point.x < 17.5f || point.x > 22.5f)
                return false;
            float distance;
            Vector2 tangent;
            GetNearestRoadInfo(point, out distance, out tangent);
            return distance <= ForestRoadClearance;
        }

        private static float DistancePointToSegment2D(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector2 p = point;
            Vector2 a = start;
            Vector2 b = end;
            Vector2 delta = b - a;
            if (delta.sqrMagnitude <= 0.000001f)
                return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude);
            return Vector2.Distance(p, a + delta * t);
        }

        private static IEnumerable<Vector3Int> EnumerateCandidateCells()
        {
            for (int y = -64; y <= 48; y++)
            {
                for (int x = -48; x <= 72; x++)
                    yield return new Vector3Int(x, y, 0);
            }
        }

        private static HashSet<Vector3Int> CollectProtectedCells(Grid grid)
        {
            var protectedNames = new HashSet<string>(
                new[] { "outside", "outside0", "outside2" },
                StringComparer.OrdinalIgnoreCase);
            var result = new HashSet<Vector3Int>();
            foreach (Tilemap map in grid.GetComponentsInChildren<Tilemap>(true))
            {
                if (!protectedNames.Contains(map.name))
                    continue;
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    if (map.HasTile(cell))
                        result.Add(cell);
                }
            }
            return result;
        }

        private static HashSet<Vector3Int> CollectTileCells(Tilemap map, RectInt region)
        {
            var result = new HashSet<Vector3Int>();
            if (map == null)
                return result;
            for (int y = region.yMin; y < region.yMax; y++)
            {
                for (int x = region.xMin; x < region.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (map.HasTile(cell))
                        result.Add(cell);
                }
            }
            return result;
        }

        private static HashSet<Vector3Int> CollectSolidTargetCells(
            FantasyKingdomStructureStamp stamp,
            Vector3Int targetAnchorCell)
        {
            var result = new HashSet<Vector3Int>();
            Vector3Int origin = targetAnchorCell - stamp.AnchorLocalCell;
            IReadOnlyList<FantasyKingdomStampLayer> layers = stamp.Layers;
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                FantasyKingdomStampLayer layer = layers[layerIndex];
                string normalized = (layer.SourceName ?? string.Empty).ToLowerInvariant();
                if (normalized.Contains("ground") || normalized.Contains("shadow"))
                    continue;
                IReadOnlyList<FantasyKingdomStampCell> cells = layer.Cells;
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    result.Add(origin + cells[cellIndex].LocalPosition);
            }
            return result;
        }

        private static HashSet<Vector3Int> CollectGroundSupportCells(Grid grid)
        {
            var supportNames = new HashSet<string>(
                new[] { "Grass", "Ground" },
                StringComparer.OrdinalIgnoreCase);
            var result = new HashSet<Vector3Int>();
            foreach (Tilemap map in grid.GetComponentsInChildren<Tilemap>(true))
            {
                if (!supportNames.Contains(map.name))
                    continue;
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    if (map.HasTile(cell))
                        result.Add(cell);
                }
            }
            return result;
        }

        private static TileBase FindFirstTile(Tilemap map)
        {
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                TileBase tile = map.GetTile(cell);
                if (tile != null)
                    return tile;
            }
            throw new InvalidOperationException(map.name + " tilemap'i bos.");
        }

        private static TileBase[] LoadRoadPalette()
        {
            if (writeRetouchCandidate)
            {
                string[] directions = { "N", "S", "E", "W" };
                var tiles = new List<TileBase>(40);
                for (int family = 1; family <= 10; family++)
                {
                    for (int direction = 0; direction < directions.Length; direction++)
                    {
                        tiles.Add(LoadEnvironmentTile(
                            "Ground I" + family + "_" + directions[direction]));
                    }
                }
                return tiles.ToArray();
            }

            return new[]
            {
                LoadEnvironmentTile("Ground I1_N"),
                LoadEnvironmentTile("Ground I1_S"),
                LoadEnvironmentTile("Ground I10_N"),
                LoadEnvironmentTile("Ground I10_S"),
                LoadEnvironmentTile("Ground I9_N"),
                LoadEnvironmentTile("Ground I9_S"),
                LoadEnvironmentTile("Ground I1_E"),
                LoadEnvironmentTile("Ground I1_W"),
                LoadEnvironmentTile("Ground I10_E"),
                LoadEnvironmentTile("Ground I10_W"),
                LoadEnvironmentTile("Ground I9_E"),
                LoadEnvironmentTile("Ground I9_W")
            };
        }

        private static TileBase SelectRoadTile(
            Grid grid,
            Vector3Int cell,
            TileBase[] palette,
            int hash)
        {
            if (writeRetouchCandidate)
            {
                float distance;
                Vector2 tangent;
                float signedDistance;
                float progress;
                Vector2 point = grid.GetCellCenterWorld(cell);
                GetNearestRoadInfo(
                    point,
                    out distance,
                    out tangent,
                    out signedDistance,
                    out progress);

                int orientation = GetRoadOrientationIndex(grid, point, tangent);
                int tileFamily;
                if (progress <= 0.025f)
                {
                    tileFamily = 2;
                    orientation = GetRoadOrientationIndex(grid, point, -tangent);
                }
                else if (progress >= 0.975f)
                {
                    tileFamily = 3;
                    orientation = GetRoadOrientationIndex(grid, point, tangent);
                }
                else if (DoesRoadAxisChangeNear(grid, point, progress))
                {
                    tileFamily = 9;
                    orientation = SelectAxisVariant(orientation, hash);
                }
                else if (PositiveHash(cell, Seed + 2707) % 23 == 0)
                {
                    tileFamily = 6;
                    orientation = SelectAxisVariant(orientation, hash);
                }
                else
                {
                    tileFamily = 1;
                    orientation = SelectAxisVariant(orientation, hash);
                }

                return GetRetouchRoadTile(palette, tileFamily, orientation);
            }

            int segment = FindNearestRoadSegment(grid, cell);
            Vector3Int[] controls = GetLegacyRoadControlCells();
            Vector3 start = grid.GetCellCenterWorld(controls[segment]);
            Vector3 end = grid.GetCellCenterWorld(controls[segment + 1]);
            int offset = end.y >= start.y ? 6 : 0;
            int roll = hash % 100;
            if (roll < 75)
                return palette[offset + hash / 101 % 2];
            return palette[offset + 2 + hash / 211 % 2];
        }

        private static TileBase GetRetouchRoadTile(
            TileBase[] palette,
            int family,
            int orientation)
        {
            if (palette == null || palette.Length != 40)
                throw new InvalidOperationException("Retouch road palette 40 tile tasimalidir.");
            int safeFamily = Mathf.Clamp(family, 1, 10);
            int safeOrientation = Mathf.Clamp(orientation, 0, 3);
            return palette[(safeFamily - 1) * 4 + safeOrientation];
        }

        private static int GetRoadOrientationIndex(
            Grid grid,
            Vector2 point,
            Vector2 worldDirection)
        {
            Vector3Int startCell = grid.WorldToCell(new Vector3(point.x, point.y, 0f));
            Vector3Int endCell = grid.WorldToCell(new Vector3(
                point.x + worldDirection.x * 2f,
                point.y + worldDirection.y * 2f,
                0f));
            Vector3Int delta = endCell - startCell;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x >= 0 ? 2 : 3;
            return delta.y >= 0 ? 0 : 1;
        }

        private static int SelectAxisVariant(int orientation, int hash)
        {
            bool useOpposite = PositiveHash(
                new Vector3Int(hash & 255, (hash >> 8) & 255, 0),
                Seed + 2903) % 2 == 0;
            if (!useOpposite)
                return orientation;
            switch (orientation)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return 3;
                default: return 2;
            }
        }

        private static bool DoesRoadAxisChangeNear(
            Grid grid,
            Vector2 point,
            float progress)
        {
            Vector2 before = GetRoadTangentAtProgress(progress - 0.035f);
            Vector2 after = GetRoadTangentAtProgress(progress + 0.035f);
            int beforeOrientation = GetRoadOrientationIndex(grid, point, before);
            int afterOrientation = GetRoadOrientationIndex(grid, point, after);
            return beforeOrientation / 2 != afterOrientation / 2;
        }

        private static Vector2 GetRoadTangentAtProgress(float progress)
        {
            int segmentIndex = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Clamp01(progress) * (RoadCurveSamples.Length - 1)),
                0,
                RoadCurveSamples.Length - 2);
            Vector2 tangent = RoadCurveSamples[segmentIndex + 1] -
                              RoadCurveSamples[segmentIndex];
            return tangent.sqrMagnitude > 0.000001f
                ? tangent.normalized
                : Vector2.right;
        }

        private static int FindNearestRoadSegment(Grid grid, Vector3Int cell)
        {
            Vector3 point = grid.GetCellCenterWorld(cell);
            Vector3Int[] controls = GetLegacyRoadControlCells();
            int nearest = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < controls.Length - 1; i++)
            {
                float distance = DistancePointToSegment2D(
                    point,
                    grid.GetCellCenterWorld(controls[i]),
                    grid.GetCellCenterWorld(controls[i + 1]));
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                nearest = i;
            }
            return nearest;
        }

        private static TileBase LoadEnvironmentTile(string tileName)
        {
            string path = EnvironmentTileFolder + tileName + ".asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
                throw new InvalidOperationException("Fantasy Kingdom tile bulunamadi: " + path);
            return tile;
        }

        private static List<TileBase> ExpandPalette(params TileCount[] entries)
        {
            var result = new List<TileBase>();
            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++)
            {
                TileBase tile = LoadEnvironmentTile(entries[entryIndex].Name);
                for (int i = 0; i < entries[entryIndex].Count; i++)
                    result.Add(tile);
            }
            return result;
        }

        private static TileCount Pair(string name, int count)
        {
            return new TileCount(name, count);
        }

        private static LayerRule Rule(
            string layerName,
            Func<TileBase, Vector3Int, bool> filter = null)
        {
            return new LayerRule(layerName, filter);
        }

        private static bool StartsWith(TileBase tile, string prefix)
        {
            return tile != null && tile.name != null &&
                   tile.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Between(int value, int minimum, int maximum)
        {
            return value >= minimum && value <= maximum;
        }

        private static int PositiveHash(Vector3Int cell, int seed)
        {
            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ (cell.x * 73856093);
                hash = (hash * 397) ^ (cell.y * 19349663);
                return hash & 0x7fffffff;
            }
        }

        private static Tilemap FindTilemap(Grid grid, string name)
        {
            return grid.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(map =>
                string.Equals(map.name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static int CountLegacySourceTiles(Grid grid)
        {
            string[] names =
            {
                "GroundDetail",
                "Structures",
                "OverlayProps",
                "RoofLow",
                "RoofHigh",
                "Roof1",
                "Roof2",
                "Roof3"
            };
            int total = 0;
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                Transform directChild = grid.transform.Find(names[nameIndex]);
                Tilemap map = directChild != null ? directChild.GetComponent<Tilemap>() : null;
                if (map == null)
                    continue;
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    if (map.HasTile(cell))
                        total++;
                }
            }
            return total;
        }

        private static FantasyKingdomStructureStamp LoadFrozenTargetStamp(
            string assetName,
            int expectedTileCount)
        {
            ValidateFrozenTargetStampFile(assetName);
            FantasyKingdomStructureStamp stamp = AssetDatabase.LoadAssetAtPath<FantasyKingdomStructureStamp>(
                StampFolder + "/" + assetName + ".asset");
            ValidateTargetDerivedStamp(stamp, expectedTileCount);
            return stamp;
        }

        private static void ValidateTargetDerivedStamp(
            FantasyKingdomStructureStamp stamp,
            int expectedTileCount)
        {
            if (stamp == null || stamp.TotalTileCount != expectedTileCount)
            {
                throw new InvalidOperationException(
                    "Stable V3 target-derived stamp kontrati bozuk: " +
                    (stamp != null ? stamp.name : "<null>") +
                    " Beklenen=" + expectedTileCount +
                    " Mevcut=" + (stamp != null ? stamp.TotalTileCount : 0));
            }
        }

        private static void ValidateLegacySourceSnapshot(Grid grid, int totalTileCount)
        {
            int expectedTotal = ApprovedLegacySourceCounts.Values.Sum();
            if (totalTileCount != expectedTotal)
            {
                throw new InvalidOperationException(
                    "NewGameScene V3 legacy source toplami sapmis. Beklenen=" +
                    expectedTotal + " Mevcut=" + totalTileCount +
                    ". Stable V3 asset'leri overwrite edilmedi.");
            }

            foreach (KeyValuePair<string, int> expected in ApprovedLegacySourceCounts)
            {
                Transform directChild = grid.transform.Find(expected.Key);
                Tilemap map = directChild != null ? directChild.GetComponent<Tilemap>() : null;
                int actual = map != null ? CountTiles(map) : -1;
                if (actual != expected.Value)
                {
                    throw new InvalidOperationException(
                        expected.Key + " legacy source sayisi sapmis. Beklenen=" +
                        expected.Value + " Mevcut=" + actual +
                        ". Ayni toplam tile sayisi bu guard'i gecemez.");
                }
            }
            string[] optionalNames = { "Roof1", "Roof2", "Roof3" };
            for (int i = 0; i < optionalNames.Length; i++)
            {
                Transform directChild = grid.transform.Find(optionalNames[i]);
                Tilemap map = directChild != null ? directChild.GetComponent<Tilemap>() : null;
                if (map != null && CountTiles(map) != 0)
                    throw new InvalidOperationException(optionalNames[i] + " legacy source bos olmali.");
            }

            string sceneHash = ComputeProjectFileHash(TargetScenePath);
            if (!string.Equals(sceneHash, ApprovedOriginalSceneHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "NewGameScene legacy source semantic snapshot'i onayli disk hash'inden sapmis. " +
                    "Stable V3 asset'leri overwrite edilmedi.");
            }
        }

        private static void ValidateFrozenTargetStampFile(string assetName)
        {
            if (!ApprovedFrozenTargetStampHashes.TryGetValue(assetName, out string expectedHash))
                throw new InvalidOperationException(assetName + " frozen hash allowlist'inde yok.");
            string assetPath = StampFolder + "/" + assetName + ".asset";
            string actualHash = ComputeProjectFileHash(assetPath);
            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    assetName + " frozen stamp disk hash'i onayli snapshot'tan sapmis. " +
                    "Rebuild mevcut assetleri overwrite etmeden durdu.");
            }
        }

        private static string ComputeProjectFileHash(string projectRelativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(
                projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Hash target bulunamadi.", absolutePath);
            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(absolutePath)))
                    .Replace("-", string.Empty);
            }
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

        private static Grid FindPrimaryGrid(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Grid>(true))
                .OrderByDescending(grid => grid.GetComponentsInChildren<Tilemap>(true).Length)
                .FirstOrDefault();
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

        private static T SaveOrReplace<T>(T generated, string assetPath)
            where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, assetPath);
                EditorUtility.SetDirty(generated);
                AssetDatabase.SaveAssetIfDirty(generated);
                return generated;
            }

            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }

        private sealed class ReadOnlySceneScope : IDisposable
        {
            private readonly bool openedByTool;
            private readonly Scene previousActiveScene;
            public Scene Scene { get; }

            public ReadOnlySceneScope(string scenePath)
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

        private sealed class LayerRule
        {
            public readonly string LayerName;
            public readonly Func<TileBase, Vector3Int, bool> Filter;

            public LayerRule(string layerName, Func<TileBase, Vector3Int, bool> filter)
            {
                LayerName = layerName;
                Filter = filter;
            }
        }

        private struct AbsoluteTileCell
        {
            public readonly Vector3Int Position;
            public readonly TileBase Tile;
            public readonly Matrix4x4 TransformMatrix;
            public readonly Color Color;
            public readonly TileFlags Flags;

            public AbsoluteTileCell(Vector3Int position, TileBase tile)
                : this(position, tile, Matrix4x4.identity, Color.white, TileFlags.None)
            {
            }

            public AbsoluteTileCell(
                Vector3Int position,
                TileBase tile,
                Matrix4x4 transformMatrix,
                Color color,
                TileFlags flags)
            {
                Position = position;
                Tile = tile;
                TransformMatrix = transformMatrix;
                Color = color;
                Flags = flags;
            }
        }

        private struct TileCount
        {
            public readonly string Name;
            public readonly int Count;

            public TileCount(string name, int count)
            {
                Name = name;
                Count = count;
            }
        }

        private sealed class GeneratedStamp
        {
            public readonly FantasyKingdomStructureStamp Stamp;
            public readonly Vector3Int AnchorCell;

            public GeneratedStamp(FantasyKingdomStructureStamp stamp, Vector3Int anchorCell)
            {
                Stamp = stamp;
                AnchorCell = anchorCell;
            }
        }

        private sealed class EnemyForestStamps
        {
            public GeneratedStamp Shadow;
            public GeneratedStamp Back;
            public GeneratedStamp Front;
        }
    }
}
#endif
