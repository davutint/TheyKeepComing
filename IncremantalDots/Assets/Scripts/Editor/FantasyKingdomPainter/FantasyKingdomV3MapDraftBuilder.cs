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
                FantasyKingdomStructureStamp castleSolid = ExtractStamp(
                    "FK_V3_Castle_Citadel_Solid",
                    exampleGrid,
                    ExampleScenePath,
                    new RectInt(69, -34, 13, 15),
                    FantasyKingdomStampPurpose.Structure,
                    new Vector3Int(7, 1, 0),
                    Rule("Walls", (tile, cell) => !StartsWith(tile, "Tree C1")),
                    Rule("Objects"),
                    Rule("WallDetail2"),
                    Rule("Roof1"),
                    Rule("Roof2"),
                    Rule("Roof3"));

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

                Tilemap grassTemplate = FindTilemap(targetGrid, "Grass");
                if (grassTemplate == null)
                    throw new InvalidOperationException("V3 settlement support icin Grass bulunamadi.");
                var settlementSupportCells = new HashSet<Vector3Int>();
                settlementSupportCells.UnionWith(CollectSolidTargetCells(
                    castleSolid,
                    new Vector3Int(2, 10, 0)));
                settlementSupportCells.UnionWith(CollectSolidTargetCells(
                    woodForest,
                    new Vector3Int(7, 11, 0)));
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
                GeneratedStamp gateRoad = BuildRoadOnlyStamp(
                    "FK_V3_Gate_CaravanRoad",
                    targetGrid,
                    groundTemplate,
                    cell => Between(cell.x - cell.y, 0, 2) &&
                            Between(cell.x + cell.y, -33, 31),
                    protectedCells);
                GeneratedStamp moatRoad = BuildRoadOnlyStamp(
                    "FK_V3_Moat_CaravanRoad",
                    targetGrid,
                    groundTemplate,
                    cell => Between(cell.x - cell.y, 3, 7) &&
                            Between(cell.x + cell.y, -33, 31),
                    protectedCells);

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
                        new Vector3Int(7, 11, 0), FantasyKingdomMapZone.Settlement,
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
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.BehindUnits),
                    Placement("road.gate", "Caravan Road Gate Approach", gateRoad.Stamp,
                        gateRoad.AnchorCell, FantasyKingdomMapZone.Settlement,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("road.moat", "Caravan Road Moat Approach", moatRoad.Stamp,
                        moatRoad.AnchorCell, FantasyKingdomMapZone.MoatGround,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("battlefield.calm_ground", "Calm Open Battlefield and S Road",
                        battlefield.Stamp, battlefield.AnchorCell, FantasyKingdomMapZone.Battlefield,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("far_right.calm_ground", "Far Right Road Mouth Ground",
                        farRightGround.Stamp, farRightGround.AnchorCell,
                        FantasyKingdomMapZone.FarRightFrame,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("enemy_forest.shadow", "Enemy Forest Shadow Mass",
                        enemyForest.Shadow.Stamp, enemyForest.Shadow.AnchorCell,
                        FantasyKingdomMapZone.FarRightFrame,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.Ground),
                    Placement("enemy_forest.back", "Enemy Forest Behind Zombies",
                        enemyForest.Back.Stamp, enemyForest.Back.AnchorCell,
                        FantasyKingdomMapZone.FarRightFrame,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.BehindUnits),
                    Placement("enemy_forest.front", "Enemy Forest Front Occluder",
                        enemyForest.Front.Stamp, enemyForest.Front.AnchorCell,
                        FantasyKingdomMapZone.FarRightFrame,
                        FantasyKingdomGameplayAnchor.None, FantasyKingdomRenderBand.InFrontOfUnits)
                };

                var generatedLayout = ScriptableObject.CreateInstance<FantasyKingdomMapLayout>();
                generatedLayout.name = "FK_NewGameScene_FullMap_V3_Draft";
                generatedLayout.Initialize(
                    targetScene,
                    GetHierarchyPath(targetGrid.transform),
                    "NewGameScene-ApprovedVisualRebuild-v3",
                    Seed,
                    placements,
                    FantasyKingdomMapLayout.CurrentSchemaVersion);

                FantasyKingdomMapLayout layout = SaveOrReplace(
                    generatedLayout,
                    FantasyKingdomMapLayoutFactory.DefaultLayoutPath);
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

            var generated = ScriptableObject.CreateInstance<FantasyKingdomStructureStamp>();
            generated.name = assetName;
            generated.Initialize(
                sourceScenePath,
                GetHierarchyPath(sourceGrid.transform),
                sourceRegion,
                sourceGrid,
                purpose,
                extractedLayers);
            generated.SetAnchorLocalCell(anchorLocalCell);
            return SaveOrReplace(generated, StampFolder + "/" + assetName + ".asset");
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

        private static GeneratedStamp BuildGeneratedStamp(
            string assetName,
            FantasyKingdomStampPurpose purpose,
            Grid targetGrid,
            Tilemap template,
            string layerName,
            string sortingLayer,
            int sortingOrder,
            List<AbsoluteTileCell> absoluteCells)
        {
            if (absoluteCells == null || absoluteCells.Count == 0)
                throw new InvalidOperationException(assetName + " generated cell maskesi bos.");

            int minX = absoluteCells.Min(cell => cell.Position.x);
            int minY = absoluteCells.Min(cell => cell.Position.y);
            int maxX = absoluteCells.Max(cell => cell.Position.x);
            int maxY = absoluteCells.Max(cell => cell.Position.y);
            var origin = new Vector3Int(minX, minY, 0);
            var stampCells = absoluteCells.Select(cell => new FantasyKingdomStampCell(
                cell.Position - origin,
                cell.Tile,
                Matrix4x4.identity,
                Color.white,
                TileFlags.None)).ToList();

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
                template.color,
                template.orientation,
                template.orientationMatrix,
                stampCells);

            var generated = ScriptableObject.CreateInstance<FantasyKingdomStructureStamp>();
            generated.name = assetName;
            generated.Initialize(
                TargetScenePath,
                GetHierarchyPath(targetGrid.transform),
                new RectInt(0, 0, maxX - minX + 1, maxY - minY + 1),
                targetGrid,
                purpose,
                new List<FantasyKingdomStampLayer> { layer });
            generated.SetAnchorLocalCell(Vector3Int.zero);
            FantasyKingdomStructureStamp saved = SaveOrReplace(
                generated,
                StampFolder + "/" + assetName + ".asset");
            return new GeneratedStamp(saved, origin);
        }

        private static bool IsCaravanRoadCell(Grid grid, Vector3Int cell)
        {
            Vector3 point = grid.GetCellCenterWorld(cell);
            Vector3Int[] controlCells = GetRoadControlCells();
            for (int i = 0; i < controlCells.Length - 1; i++)
            {
                Vector3 start = grid.GetCellCenterWorld(controlCells[i]);
                Vector3 end = grid.GetCellCenterWorld(controlCells[i + 1]);
                if (DistancePointToSegment2D(point, start, end) <= 0.55f)
                    return true;
            }
            return false;
        }

        private static Vector3Int[] GetRoadControlCells()
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
            int segment = FindNearestRoadSegment(grid, cell);
            Vector3Int[] controls = GetRoadControlCells();
            Vector3 start = grid.GetCellCenterWorld(controls[segment]);
            Vector3 end = grid.GetCellCenterWorld(controls[segment + 1]);
            int offset = end.y >= start.y ? 6 : 0;
            int roll = hash % 100;
            if (roll < 75)
                return palette[offset + hash / 101 % 2];
            return palette[offset + 2 + hash / 211 % 2];
        }

        private static int FindNearestRoadSegment(Grid grid, Vector3Int cell)
        {
            Vector3 point = grid.GetCellCenterWorld(cell);
            Vector3Int[] controls = GetRoadControlCells();
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

            public AbsoluteTileCell(Vector3Int position, TileBase tile)
            {
                Position = position;
                Tile = tile;
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
