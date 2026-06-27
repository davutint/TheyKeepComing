#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// NewGameScene icin mobil castle-defense sahne iskeletini kuran editor araci.
    /// Sahne YAML'i elle duzenlenmez; eksik objeler Unity Editor API ile olusturulur.
    /// </summary>
    public class MobileCastleSceneSetupWindow : EditorWindow
    {
        private const string TargetScenePath = "Assets/Scenes/NewGameScene.unity";
        private const string SubSceneFolder = "Assets/Scenes/NewGameScene";
        private const string SubScenePath = SubSceneFolder + "/MobileCastleCombatSubScene.unity";

        private const string ZombiePrefabPath = "Assets/Prefabs/Zombie.prefab";
        private const string ArrowPrefabPath = "Assets/Prefabs/Arrow.prefab";
        private const string ArcherPrefabPath = "Assets/Prefabs/Archer.prefab";
        private const string WorkerPrefabPath = "Assets/Prefabs/VillagerWorker.prefab";
        private const string WorkerMaterialPath = "Assets/Materials/Villager.mat";
        private const string WorkerIdleSpritesheetPath = "Assets/SmallScaleInt/Character creator - Fantasy/Created Spritesheets/Character_villager/Idle.png";
        private const string GeneratedHudPrefabPath = "Assets/Prefabs/UI/Generated/MobileCastleHudRoot.prefab";
        private const string SmallScaleTilesRoot = "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/Tiles";
        private const string ArrowMuzzleVfxPath = "Assets/VFX_Klaus/Prefabs/Stylized Shoot & Hit Vol.2/FX_Shoot_Arrow_muzzle.prefab";
        private const string ArrowHitVfxPath = "Assets/VFX_Klaus/Prefabs/Stylized Shoot & Hit Vol.2/FX_Shoot_Arrow_hit.prefab";
        private const string FrostHitVfxPath = "Assets/VFX_Klaus/Prefabs/Stylized Shoot & Hit Vol.2/FX_Shoot_Ice_hit.prefab";
        private const string HitFlipbookSpritesheetPath = "Assets/Art/Effects/fanfx2_cure_small_red/spritesheet.png";
        private const string FantasyUiSfxRoot = "Assets/Fantasy UI SFX - Lite Edition";
        private const string ArrowShootSfxPath = "Assets/Fantasy UI SFX - Lite Edition/Arrow & Bow 1-2.wav";
        private const string CastleHitSfxPath = "Assets/Fantasy UI SFX - Lite Edition/Rock Impact 37.wav";

        private string _status = "Hazir.";

        [MenuItem("Window/DeadWalls/Mobile Castle Scene Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<MobileCastleSceneSetupWindow>("Mobile Castle Setup");
            window.minSize = new Vector2(390f, 220f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Mobile Castle Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "NewGameScene icin landscape mobil sahne iskeletini kurar. Tool tekrar calistirildiginda ayni isimli objeleri cogaltmaz.",
                MessageType.Info);

            EditorGUILayout.LabelField("Target Scene", TargetScenePath);
            EditorGUILayout.LabelField("Combat SubScene", SubScenePath);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Setup NewGameScene", GUILayout.Height(34f)))
            {
                SetupScene();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_status, MessageType.None);
        }

        private void SetupScene()
        {
            if (!EnsureTargetSceneOpen())
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                _status = "Aktif scene gecersiz.";
                return;
            }

            SceneAsset combatSubSceneAsset = EnsureCombatSubSceneAsset();
            if (combatSubSceneAsset == null)
            {
                _status = "Combat SubScene asset olusturulamadi.";
                return;
            }

            EnsureMainCamera(scene);
            EnsureGlobalLight(scene);
            EnsureEventSystem(scene);

            var canvas = EnsureCanvas(scene);
            EnsureManagers(scene, canvas);
            EnsureCastleInteriorWorkerArea(scene);
            EnsureSubSceneRoot(scene, combatSubSceneAsset);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _status = "NewGameScene sahne altyapisi hazirlandi.";
            Debug.Log("[MobileCastleSceneSetup] NewGameScene sahne altyapisi hazirlandi.");
        }

        private static bool EnsureTargetSceneOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == TargetScenePath)
                return true;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null)
            {
                EditorUtility.DisplayDialog(
                    "NewGameScene bulunamadi",
                    TargetScenePath + " bulunamadi. Once sahne asset'ini olustur.",
                    "Tamam");
                return false;
            }

            bool openScene = EditorUtility.DisplayDialog(
                "NewGameScene acilsin mi?",
                "Tool aktif scene yerine " + TargetScenePath + " uzerinde calisir. Bu sahne acilsin mi?",
                "Ac",
                "Iptal");

            if (!openScene)
                return false;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                return true;
            }

            return false;
        }

        private static SceneAsset EnsureCombatSubSceneAsset()
        {
            EnsureAssetFolder();

            var existing = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);
            if (existing != null)
            {
                EnsureExistingCombatSubSceneContents();
                return existing;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(subScene);

            EnsureCombatSubSceneContents(subScene);

            EditorSceneManager.SaveScene(subScene, SubScenePath);
            SceneManager.SetActiveScene(previousActiveScene);
            EditorSceneManager.CloseScene(subScene, true);
            AssetDatabase.ImportAsset(SubScenePath);

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(SubScenePath);
        }

        private static void EnsureExistingCombatSubSceneContents()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene loadedScene = FindLoadedScene(SubScenePath);
            bool wasAlreadyLoaded = loadedScene.IsValid();

            Scene subScene = wasAlreadyLoaded
                ? loadedScene
                : EditorSceneManager.OpenScene(SubScenePath, OpenSceneMode.Additive);

            SceneManager.SetActiveScene(subScene);
            EnsureCombatSubSceneContents(subScene);
            EditorSceneManager.SaveScene(subScene);

            SceneManager.SetActiveScene(previousActiveScene);
            if (!wasAlreadyLoaded)
                EditorSceneManager.CloseScene(subScene, true);
        }

        private static Scene FindLoadedScene(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                    return scene;
            }

            return default;
        }

        private static void EnsureCombatSubSceneContents(Scene subScene)
        {
            var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath);
            var archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArcherPrefabPath);
            var workerPrefab = EnsureVillagerWorkerPrefab();

            var gameState = EnsureSceneRoot(subScene, "GameState");
            var gameStateAuthoring = EnsureComponent<GameStateAuthoring>(gameState);
            gameStateAuthoring.XPToNextLevel = 100;
            gameStateAuthoring.StressTestMode = false;
            gameStateAuthoring.InitialZombiesToSpawn = 30;
            gameStateAuthoring.InitialZombieHP = 20f;
            gameStateAuthoring.InitialZombieDamage = 5f;
            gameStateAuthoring.SpawnInterval = 0.8f;
            gameStateAuthoring.WaveStartDelay = 3f;
            gameStateAuthoring.BaseZombieSpeed = 0.85f;
            gameStateAuthoring.InitialWood = 150;
            gameStateAuthoring.InitialStone = 80;
            gameStateAuthoring.InitialIron = 45;
            gameStateAuthoring.InitialFood = 150;
            gameStateAuthoring.TestWoodProdRate = 90f;
            gameStateAuthoring.TestStoneProdRate = 30f;
            gameStateAuthoring.TestIronProdRate = 16f;
            gameStateAuthoring.TestFoodProdRate = 60f;
            gameStateAuthoring.InitialPopulation = 60;
            gameStateAuthoring.InitialCapacity = 999999;
            gameStateAuthoring.TestWorkers = 53;
            gameStateAuthoring.TestArchers = 1;
            gameStateAuthoring.FoodPerAssignedPerMin = 0.25f;
            gameStateAuthoring.InitialArrows = 200;

            var waveConfig = EnsureComponent<WaveConfigAuthoring>(gameState);
            waveConfig.ZombiePrefab = zombiePrefab;
            waveConfig.ArrowPrefab = arrowPrefab;
            waveConfig.ArcherPrefab = archerPrefab;
            waveConfig.WorkerPrefab = workerPrefab;

            var castle = EnsureSceneRoot(subScene, "CastleCore");
            castle.transform.position = Vector3.zero;
            var castleAuthoring = EnsureComponent<CastleAuthoring>(castle);
            castleAuthoring.WallHP = 200f;
            castleAuthoring.GateHP = 100f;
            castleAuthoring.CastleMaxHP = 500f;
            castleAuthoring.WallXPos = 0f;
            castleAuthoring.MaxUpgradeLevel = 5;
            castleAuthoring.CapacityPerLevel = 5;
            castleAuthoring.UpgradeWoodCost = 50;
            castleAuthoring.UpgradeStoneCost = 25;

            var mobileConfig = EnsureSceneRoot(subScene, "MobileCastleConfig");
            mobileConfig.transform.position = Vector3.zero;
            var mobileAuthoring = EnsureComponent<MobileCastleCombatAuthoring>(mobileConfig);
            mobileAuthoring.CastleCenter = Vector2.zero;
            mobileAuthoring.SpawnRadius = 11f;
            mobileAuthoring.AttackRadius = 1.35f;
            mobileAuthoring.BaseWaveEnemyCount = 30;
            mobileAuthoring.ExtraEnemiesPerWave = 10;
            mobileAuthoring.SpawnBatchSize = 3;
            mobileAuthoring.ZombieScale = 1.4f;
            mobileAuthoring.BaseZombieSpeed = 0.85f;
            mobileAuthoring.ZombieSpeedPerWave = 0.04f;
            mobileAuthoring.StressSpawnBatchSize = 25;
            mobileAuthoring.StressSpawnInterval = 0.1f;
            mobileAuthoring.StressMaxAliveZombies = 1500;
            mobileAuthoring.KillRewardWood = 1f;
            mobileAuthoring.KillRewardFood = 0.6f;
            mobileAuthoring.KillRewardStone = 0.25f;
            mobileAuthoring.KillRewardIron = 0.15f;
            mobileAuthoring.KillRewardWaveScale = 0.05f;
            mobileAuthoring.WaveClearWoodBase = 20;
            mobileAuthoring.WaveClearFoodBase = 15;
            mobileAuthoring.WaveClearStoneBase = 10;
            mobileAuthoring.WaveClearIronBase = 6;
            mobileAuthoring.WaveClearWoodPerWave = 6;
            mobileAuthoring.WaveClearFoodPerWave = 5;
            mobileAuthoring.WaveClearStonePerWave = 4;
            mobileAuthoring.WaveClearIronPerWave = 3;
            mobileAuthoring.BalancedPassiveMultiplier = 1.20f;
            mobileAuthoring.BalancedRewardMultiplier = 1.10f;
            mobileAuthoring.FocusedPassiveMultiplier = 1.60f;
            mobileAuthoring.FocusedPassiveFlatBonusPerMin = 60f;
            mobileAuthoring.FocusedKillRewardMultiplier = 2.00f;
            mobileAuthoring.FocusedWaveClearMultiplier = 1.75f;
            mobileAuthoring.InitialDayPrepDuration = 12f;
            mobileAuthoring.DayPrepDuration = 15f;
            mobileAuthoring.DayOverlayAlpha = 0f;
            mobileAuthoring.NightOverlayAlpha = 0.50f;
            mobileAuthoring.UnlimitedArrows = true;
            mobileAuthoring.BaseSpawnInterval = 0.8f;
            mobileAuthoring.SpawnIntervalWaveMultiplier = 0.96f;
            mobileAuthoring.MinSpawnInterval = 0.35f;
            mobileAuthoring.OpeningEnemyRatio = 0.20f;
            mobileAuthoring.FinalEnemyRatio = 0.20f;
            mobileAuthoring.OpeningIntervalMultiplier = 1.35f;
            mobileAuthoring.FinalIntervalMultiplier = 0.65f;
            mobileAuthoring.OpeningBatchDelta = -1;
            mobileAuthoring.FinalBatchDelta = 1;
            mobileAuthoring.PopulationGrowthPerDayPrep = 15;
            mobileAuthoring.InitialWoodWorkers = 20;
            mobileAuthoring.InitialStoneWorkers = 10;
            mobileAuthoring.InitialIronWorkers = 8;
            mobileAuthoring.InitialFoodWorkers = 15;
            mobileAuthoring.WoodWorkerProductionPerMin = 4.5f;
            mobileAuthoring.StoneWorkerProductionPerMin = 3f;
            mobileAuthoring.IronWorkerProductionPerMin = 2f;
            mobileAuthoring.FoodWorkerProductionPerMin = 4f;
            mobileAuthoring.WorkerEconomyRewardMultiplier = 0.45f;
            mobileAuthoring.EconomyEventChance = 0.15f;
            mobileAuthoring.EconomyEventCooldownWaves = 2;
            mobileAuthoring.EconomyEventSeed = 91273u;
            mobileAuthoring.FortifyDamageMultiplier = 0.70f;
            mobileAuthoring.RallyDuration = 10f;
            mobileAuthoring.RallyFireRateMultiplier = 1.25f;
            mobileAuthoring.ArcherSlots = Array.Empty<Transform>();

            EnsureBasicArcher(subScene, archerPrefab, Vector3.zero);
        }

        private static void EnsureBasicArcher(Scene subScene, GameObject archerPrefab, Vector3 position)
        {
            GameObject archer = FindRoot(subScene, "BasicArcher_01");
            if (archer == null)
            {
                archer = archerPrefab != null
                    ? PrefabUtility.InstantiatePrefab(archerPrefab, subScene) as GameObject
                    : null;

                if (archer == null)
                {
                    archer = new GameObject("BasicArcher_01");
                    SceneManager.MoveGameObjectToScene(archer, subScene);
                }

                Undo.RegisterCreatedObjectUndo(archer, "Create Basic Archer");
                archer.name = "BasicArcher_01";
            }

            archer.transform.position = position;
            archer.transform.localScale = Vector3.one;
            var archerAuthoring = EnsureComponent<ArcherAuthoring>(archer);
            archerAuthoring.Type = ArcherType.Basic;
            archerAuthoring.FireRate = 1.5f;
            archerAuthoring.ArrowDamage = 10f;
            archerAuthoring.Range = 15f;
            archerAuthoring.SlowDuration = 0f;
            archerAuthoring.SlowMultiplier = 1f;
            archerAuthoring.Tint = Color.white;

            var spriteSheet = archer.GetComponent<SpriteSheetAuthoring>();
            if (spriteSheet != null)
            {
                spriteSheet.DirectionRow = 24;
                spriteSheet.FrameCount = 15;
                spriteSheet.Tint = Color.white;
            }
        }

        private static GameObject EnsureVillagerWorkerPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerPrefabPath);
            if (prefab != null)
            {
                ConfigureVillagerWorkerPrefab(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                return prefab;
            }

            var temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            temp.name = "VillagerWorker";
            var collider = temp.GetComponent<Collider>();
            if (collider != null)
                DestroyImmediate(collider);

            ConfigureVillagerWorkerPrefab(temp);
            PrefabUtility.SaveAsPrefabAsset(temp, WorkerPrefabPath);
            DestroyImmediate(temp);
            return AssetDatabase.LoadAssetAtPath<GameObject>(WorkerPrefabPath);
        }

        private static void ConfigureVillagerWorkerPrefab(GameObject worker)
        {
            if (worker == null)
                return;

            worker.transform.localScale = Vector3.one * 2.2f;

            var meshFilter = EnsureComponent<MeshFilter>(worker);
            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var renderer = EnsureComponent<MeshRenderer>(worker);
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkerMaterialPath);
            if (material != null)
            {
                var idleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(WorkerIdleSpritesheetPath);
                if (idleTexture != null)
                {
                    material.SetTexture("_MainTex", idleTexture);
                    EditorUtility.SetDirty(material);
                }
                else
                {
                    Debug.LogWarning("[MobileCastleSceneSetup] Villager idle spritesheet bulunamadi: " + WorkerIdleSpritesheetPath);
                }

                renderer.sharedMaterial = material;
            }
            else
            {
                Debug.LogWarning("[MobileCastleSceneSetup] Villager worker material bulunamadi: " + WorkerMaterialPath);
            }

            renderer.sortingLayerName = "Wall";
            renderer.sortingOrder = 3;

            var spriteSheet = EnsureComponent<SpriteSheetAuthoring>(worker);
            spriteSheet.Columns = 15;
            spriteSheet.Rows = 8;
            spriteSheet.FPS = 8f;
            spriteSheet.DirectionRow = 2;
            spriteSheet.FrameCount = 15;
            spriteSheet.Tint = Color.white;

            var workerAuthoring = EnsureComponent<VillagerWorkerAuthoring>(worker);
            workerAuthoring.Resource = EconomyFocusType.Wood;
            workerAuthoring.Index = 0;

            EditorUtility.SetDirty(worker);
        }

        private static void EnsureAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(SubSceneFolder))
                AssetDatabase.CreateFolder("Assets/Scenes", "NewGameScene");
        }

        private static void EnsureMainCamera(Scene scene)
        {
            GameObject cameraObject = FindRoot(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
            }

            var camera = EnsureComponent<Camera>(cameraObject);
            EnsureComponent<AudioListener>(cameraObject);

            cameraObject.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.075f, 1f);
        }

        private static void EnsureWorldVisuals(Scene scene)
        {
            GameObject root = EnsureSceneRoot(scene, "WorldVisualRoot");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            GameObject gridObject = EnsureChild(root.transform, "MobileArenaGrid", false);
            gridObject.transform.localPosition = Vector3.zero;
            gridObject.transform.localRotation = Quaternion.identity;
            gridObject.transform.localScale = Vector3.one;

            var grid = EnsureComponent<Grid>(gridObject);
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = new Vector3(4f, 2f, 4f);

            Tilemap ground = EnsureVisualTilemap(gridObject.transform, "GroundTilemap", -50);
            Tilemap castleGround = EnsureVisualTilemap(gridObject.transform, "CastleGroundTilemap", -40);
            Tilemap castleWall = EnsureVisualTilemap(gridObject.transform, "CastleWallTilemap", -30);
            Tilemap castleProps = EnsureVisualTilemap(gridObject.transform, "CastlePropsTilemap", -20);

            PaintArenaGround(ground, castleGround);
            PaintCastle(castleGround, castleWall, castleProps);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(gridObject);
        }

        private static Tilemap EnsureVisualTilemap(Transform parent, string name, int sortingOrder)
        {
            GameObject tilemapObject = EnsureChild(parent, name, false);
            tilemapObject.transform.localPosition = Vector3.zero;
            tilemapObject.transform.localRotation = Quaternion.identity;
            tilemapObject.transform.localScale = Vector3.one;

            var tilemap = EnsureComponent<Tilemap>(tilemapObject);
            var renderer = EnsureComponent<TilemapRenderer>(tilemapObject);
            renderer.mode = TilemapRenderer.Mode.Individual;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void PaintArenaGround(Tilemap ground, Tilemap path)
        {
            TileBase baseGround = LoadTile("Ground A1_S");
            TileBase grassA = LoadTile("Ground G1_S", baseGround);
            TileBase grassB = LoadTile("Ground G4_S", grassA);
            TileBase dirt = LoadTile("Ground B1_S", baseGround);

            ground.ClearAllTiles();
            path.ClearAllTiles();

            for (int x = -13; x <= 13; x++)
            {
                for (int y = -13; y <= 13; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) > 22)
                        continue;

                    TileBase tile = ((x * 31 + y * 17) & 3) == 0 ? grassB : grassA;
                    if (((x + y) & 7) == 0)
                        tile = baseGround;

                    ground.SetTile(new Vector3Int(x, y, 0), tile);

                    bool cardinalPath = Mathf.Abs(x) <= 1 || Mathf.Abs(y) <= 1;
                    bool diagonalPath = Mathf.Abs(x - y) <= 1 || Mathf.Abs(x + y) <= 1;
                    bool outsideCastle = Mathf.Abs(x) > 2 || Mathf.Abs(y) > 2;
                    if (outsideCastle && (cardinalPath || diagonalPath) && Mathf.Abs(x) + Mathf.Abs(y) <= 18)
                        path.SetTile(new Vector3Int(x, y, 0), dirt);
                }
            }

            ground.CompressBounds();
            path.CompressBounds();
            EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(path);
        }

        private static void PaintCastle(Tilemap castleGround, Tilemap castleWall, Tilemap castleProps)
        {
            TileBase floor = LoadTile("Ground A3_S", LoadTile("Ground A1_S"));
            TileBase wallNorth = LoadTile("Wall A1_N");
            TileBase wallSouth = LoadTile("Wall A1_S", wallNorth);
            TileBase wallEast = LoadTile("Wall A1_E", wallSouth);
            TileBase wallWest = LoadTile("Wall A1_W", wallSouth);
            TileBase gate = LoadTile("Door C1_S", wallSouth);
            TileBase keep = LoadTile("Wall D1_S", wallSouth);
            TileBase brokenStone = LoadTile("BrokenStone1");
            TileBase smallStone = LoadTile("BrokenStone small1", brokenStone);
            TileBase brokenWall = LoadTile("BrokenWallStone1", brokenStone);
            TileBase treeShadow = LoadTile("Tree Shadow", smallStone);

            castleGround.ClearAllTiles();
            castleWall.ClearAllTiles();
            castleProps.ClearAllTiles();

            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                    castleGround.SetTile(new Vector3Int(x, y, 0), floor);
            }

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    bool perimeter = Mathf.Abs(x) == 1 || Mathf.Abs(y) == 1;
                    if (!perimeter)
                        continue;

                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (x == 0 && y == -1)
                    {
                        castleWall.SetTile(cell, gate);
                    }
                    else if (y == 1)
                    {
                        castleWall.SetTile(cell, wallNorth);
                    }
                    else if (y == -1)
                    {
                        castleWall.SetTile(cell, wallSouth);
                    }
                    else if (x == 1)
                    {
                        castleWall.SetTile(cell, wallEast);
                    }
                    else
                    {
                        castleWall.SetTile(cell, wallWest);
                    }
                }
            }

            castleProps.SetTile(new Vector3Int(0, 0, 0), keep);
            castleProps.SetTile(new Vector3Int(-4, 2, 0), brokenStone);
            castleProps.SetTile(new Vector3Int(4, -3, 0), smallStone);
            castleProps.SetTile(new Vector3Int(-5, -1, 0), brokenWall);
            castleProps.SetTile(new Vector3Int(5, 2, 0), treeShadow);

            castleGround.CompressBounds();
            castleWall.CompressBounds();
            castleProps.CompressBounds();
            EditorUtility.SetDirty(castleGround);
            EditorUtility.SetDirty(castleWall);
            EditorUtility.SetDirty(castleProps);
        }

        private static TileBase LoadTile(string tileName, TileBase fallback = null)
        {
            string path = SmallScaleTilesRoot + "/" + tileName + ".asset";
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null)
                return tile;

            Debug.LogWarning("[MobileCastleSceneSetup] Tile bulunamadi, fallback kullaniliyor: " + path);
            return fallback;
        }

        private static void EnsureGlobalLight(Scene scene)
        {
            GameObject lightObject = FindRoot(scene, "Global Light 2D");
            if (lightObject == null)
            {
                lightObject = new GameObject("Global Light 2D");
                Undo.RegisterCreatedObjectUndo(lightObject, "Create Global Light 2D");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
            }

            Type light2DType = FindComponentType("UnityEngine.Rendering.Universal.Light2D");
            if (light2DType == null)
            {
                Debug.LogWarning("[MobileCastleSceneSetup] Light2D type bulunamadi. URP 2D paketleri yuklendikten sonra tool tekrar calistirilabilir.");
                return;
            }

            var light2D = EnsureComponent(lightObject, light2DType);
            var serializedLight = new SerializedObject(light2D);
            SetSerializedInt(serializedLight, "m_LightType", 4);
            SetSerializedFloat(serializedLight, "m_Intensity", 1f);
            SetSerializedColor(serializedLight, "m_Color", Color.white);
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject eventSystemObject = FindRoot(scene, "EventSystem");
            if (eventSystemObject == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            }

            EnsureComponent<EventSystem>(eventSystemObject);
            if (eventSystemObject.GetComponent<BaseInputModule>() == null)
            {
                Type inputSystemModuleType = FindComponentType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                if (inputSystemModuleType != null)
                    Undo.AddComponent(eventSystemObject, inputSystemModuleType);
                else
                    Undo.AddComponent<StandaloneInputModule>(eventSystemObject);
            }
        }

        private static Canvas EnsureCanvas(Scene scene)
        {
            GameObject canvasObject = FindRoot(scene, "Canvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
            }

            SetLayerRecursive(canvasObject, LayerMask.NameToLayer("UI"));

            var canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasObject);
            return canvas;
        }

        private static void EnsureManagers(Scene scene, Canvas canvas)
        {
            GameObject gameManagerObject = FindRoot(scene, "GameManager");
            if (gameManagerObject == null)
            {
                gameManagerObject = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(gameManagerObject, "Create GameManager");
                SceneManager.MoveGameObjectToScene(gameManagerObject, scene);
            }
            EnsureComponent<GameManager>(gameManagerObject);

            var uiManager = EnsureComponent<UIManager>(canvas.gameObject);
            BuildCanvasPanels(canvas.transform, uiManager);
            EnsureCastleClickTarget(scene);
            EnsureArcherTilePlacement(scene);
            EnsureCombatFeedbackRoot(scene);
            NormalizeCastleTilemapSorting(scene);
        }

        private static void EnsureCastleInteriorWorkerArea(Scene scene)
        {
            GameObject root = FindRoot(scene, CastleInteriorWorkerPlacement.RootName);
            bool rootCreated = root == null;
            if (root == null)
            {
                root = new GameObject(CastleInteriorWorkerPlacement.RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create Castle Interior Economy Area");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            if (rootCreated)
                root.transform.position = new Vector3(-5.8f, 0f, 0f);

            var placement = EnsureComponent<CastleInteriorWorkerPlacement>(root);
            Transform hubDeliveryRoot = EnsureWorkerHub(root.transform);
            placement.HubDeliveryRoot = hubDeliveryRoot;
            placement.WoodWorkerSpawnRoot = EnsureWorkerSite(root.transform, "WoodSite", EconomyFocusType.Wood, hubDeliveryRoot, new Vector3(-1.25f, 1.25f, 0f));
            placement.StoneWorkerSpawnRoot = EnsureWorkerSite(root.transform, "StoneSite", EconomyFocusType.Stone, hubDeliveryRoot, new Vector3(1.25f, 1.25f, 0f));
            placement.IronWorkerSpawnRoot = EnsureWorkerSite(root.transform, "IronSite", EconomyFocusType.Iron, hubDeliveryRoot, new Vector3(-1.25f, -1.25f, 0f));
            placement.FoodWorkerSpawnRoot = EnsureWorkerSite(root.transform, "FoodSite", EconomyFocusType.Food, hubDeliveryRoot, new Vector3(1.25f, -1.25f, 0f));
            placement.SpawnZ = MobileCastleRenderDepth.UnitZ;
            placement.RepeatOffsetRadius = 0.12f;
            EditorUtility.SetDirty(placement);
        }

        private static Transform EnsureWorkerHub(Transform parent)
        {
            GameObject hub = FindDirectChild(parent, CastleInteriorWorkerPlacement.HubName);
            bool hubCreated = hub == null;
            if (hub == null)
            {
                hub = new GameObject(CastleInteriorWorkerPlacement.HubName);
                Undo.RegisterCreatedObjectUndo(hub, "Create Worker Hub");
                hub.transform.SetParent(parent, false);
            }

            if (hubCreated)
                hub.transform.localPosition = Vector3.zero;

            EnsureDirectChild(hub.transform, "VisualRoot");
            GameObject deliveryRoot = EnsureDirectChild(hub.transform, CastleInteriorWorkerPlacement.DeliveryRootName);
            if (deliveryRoot.transform.childCount == 0)
                CreateDefaultWorkerDeliveryMarkers(deliveryRoot.transform);

            return deliveryRoot.transform;
        }

        private static Transform EnsureWorkerSite(Transform parent, string siteName, EconomyFocusType resource, Transform hubDeliveryRoot, Vector3 defaultLocalPosition)
        {
            GameObject site = FindDirectChild(parent, siteName);
            bool siteCreated = site == null;
            if (site == null)
            {
                site = new GameObject(siteName);
                Undo.RegisterCreatedObjectUndo(site, "Create Worker Site");
                site.transform.SetParent(parent, false);
            }

            if (siteCreated)
                site.transform.localPosition = defaultLocalPosition;

            EnsureDirectChild(site.transform, "VisualRoot");
            GameObject spawnRoot = EnsureDirectChild(site.transform, CastleInteriorWorkerPlacement.SpawnRootName);
            if (spawnRoot.transform.childCount == 0)
                CreateDefaultWorkerSpawnMarkers(spawnRoot.transform);

            var gizmo = EnsureComponent<CastleInteriorWorkerSiteGizmo>(site);
            gizmo.Resource = resource;
            gizmo.WorkerSpawnRoot = spawnRoot.transform;
            gizmo.DeliveryRoot = hubDeliveryRoot;
            gizmo.SiteRadius = 0.7f;
            gizmo.MarkerRadius = 0.06f;
            EditorUtility.SetDirty(gizmo);

            return spawnRoot.transform;
        }

        private static void CreateDefaultWorkerSpawnMarkers(Transform parent)
        {
            const int columns = 4;
            const int rows = 3;
            const float spacingX = 0.28f;
            const float spacingY = 0.22f;
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var marker = new GameObject($"Spawn_{index:00}");
                    Undo.RegisterCreatedObjectUndo(marker, "Create Worker Spawn Marker");
                    marker.transform.SetParent(parent, false);
                    marker.transform.localPosition = new Vector3(
                        (col - (columns - 1) * 0.5f) * spacingX,
                        (row - (rows - 1) * 0.5f) * spacingY,
                        0f);
                    index++;
                }
            }
        }

        private static void CreateDefaultWorkerDeliveryMarkers(Transform parent)
        {
            const int columns = 3;
            const int rows = 2;
            const float spacingX = 0.24f;
            const float spacingY = 0.2f;
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var marker = new GameObject($"Delivery_{index:00}");
                    Undo.RegisterCreatedObjectUndo(marker, "Create Worker Delivery Marker");
                    marker.transform.SetParent(parent, false);
                    marker.transform.localPosition = new Vector3(
                        (col - (columns - 1) * 0.5f) * spacingX,
                        (row - (rows - 1) * 0.5f) * spacingY,
                        0f);
                    index++;
                }
            }
        }

        private static GameObject EnsureDirectChild(Transform parent, string name)
        {
            GameObject child = FindDirectChild(parent, name);
            if (child != null)
                return child;

            child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, "Create Child");
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child.gameObject;
            }

            return null;
        }

        private static void EnsureCombatFeedbackRoot(Scene scene)
        {
            GameObject root = FindRoot(scene, "CombatFeedbackRoot");
            if (root == null)
            {
                root = new GameObject("CombatFeedbackRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create Combat Feedback Root");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            root.transform.position = Vector3.zero;
            var bridge = EnsureComponent<CombatFeedbackBridge>(root);

            var arrowMuzzle = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowMuzzleVfxPath);
            var arrowHit = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowHitVfxPath);
            var frostHit = AssetDatabase.LoadAssetAtPath<GameObject>(FrostHitVfxPath);
            var arrowShoot = AssetDatabase.LoadAssetAtPath<AudioClip>(ArrowShootSfxPath);
            var castleHit = AssetDatabase.LoadAssetAtPath<AudioClip>(CastleHitSfxPath);

            if (arrowMuzzle != null) bridge.ArrowMuzzlePrefab = arrowMuzzle;
            if (arrowHit != null)
            {
                bridge.ArrowHitPrefab = arrowHit;
                if (bridge.CastleHitPrefab == null)
                    bridge.CastleHitPrefab = arrowHit;
            }
            if (frostHit != null) bridge.FrostHitPrefab = frostHit;
            if (arrowShoot != null) bridge.ArrowShootClip = arrowShoot;
            bridge.ArrowShootClips = FindArrowShootClips();
            bridge.HitFlipbookSprites = FindHitFlipbookSprites();
            if (castleHit != null) bridge.CastleHitClip = castleHit;

            bridge.HitFlipbookPoolSize = 1024;
            bridge.HitFlipbookFrameRate = 90f;
            bridge.HitFlipbookScale = 0.35f;
            bridge.HitFlipbookRotationOffsetDegrees = 0f;
            bridge.HitFlipbookSortingLayer = "Wall";
            bridge.HitFlipbookSortingOrder = 12;
            bridge.VfxPoolSizePerType = 24;
            bridge.MaxVfxPlayedPerFrame = 24;
            bridge.AudioPoolSize = 16;
            bridge.DisableInStressMode = true;
            bridge.ArrowMuzzleScale = 0.18f;
            bridge.ArrowHitScale = 0.08f;
            bridge.FrostHitScale = 0.11f;
            bridge.CastleHitScale = 0.35f;
            bridge.ArrowMuzzleRotationOffsetDegrees = 0f;
            bridge.ArrowHitRotationOffsetDegrees = 90f;
            bridge.FrostHitRotationOffsetDegrees = 0f;
            bridge.CastleHitRotationOffsetDegrees = 0f;
            bridge.ShootSfxMinInterval = 0.045f;
            bridge.HitSfxMinInterval = 0.08f;
            bridge.CastleHitSfxMinInterval = 0.18f;
            bridge.PitchRandomMin = 0.94f;
            bridge.PitchRandomMax = 1.06f;
            bridge.SpatialBlend = 0.45f;
            bridge.VfxSortingLayer = "Wall";
            bridge.VfxSortingOrder = 12;

            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(root);
        }

        private static Sprite[] FindHitFlipbookSprites()
        {
            EnsureHitFlipbookImportSettings();

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(HitFlipbookSpritesheetPath);
            var sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) =>
            {
                int compare = GetSpriteFrameIndex(a).CompareTo(GetSpriteFrameIndex(b));
                return compare != 0 ? compare : string.CompareOrdinal(a.name, b.name);
            });

            if (sprites.Count != 35)
            {
                Debug.LogWarning(
                    "fanfx2_cure_small_red spritesheet icin 35 frame bekleniyor, bulunan frame sayisi: "
                    + sprites.Count);
            }

            return sprites.ToArray();
        }

        private static int GetSpriteFrameIndex(Sprite sprite)
        {
            const string Prefix = "spritesheet_";
            if (sprite != null && sprite.name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(sprite.name.Substring(Prefix.Length), out int index))
            {
                return index;
            }

            return int.MaxValue;
        }

        private static void EnsureHitFlipbookImportSettings()
        {
            var importer = AssetImporter.GetAtPath(HitFlipbookSpritesheetPath) as TextureImporter;
            if (importer == null)
                return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.maxTextureSize < 4096)
            {
                importer.maxTextureSize = 4096;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        private static AudioClip[] FindArrowShootClips()
        {
            var clips = new List<AudioClip>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { FantasyUiSfxRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("Arrow & Bow", StringComparison.OrdinalIgnoreCase))
                    continue;

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    clips.Add(clip);
            }

            clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return clips.ToArray();
        }

        private static void EnsureCastleClickTarget(Scene scene)
        {
            GameObject clickTarget = FindRoot(scene, "CastleClickTarget");
            if (clickTarget == null)
            {
                clickTarget = new GameObject("CastleClickTarget");
                Undo.RegisterCreatedObjectUndo(clickTarget, "Create Castle Click Target");
                SceneManager.MoveGameObjectToScene(clickTarget, scene);
            }

            clickTarget.transform.position = Vector3.zero;
            var target = EnsureComponent<CastleInteriorClickTarget>(clickTarget);
            target.ClickRadius = 2f;
        }

        private static void EnsureArcherTilePlacement(Scene scene)
        {
            Tilemap outside = FindSceneTilemap(scene, MobileCastleArcherTilePlacement.DefaultSpawnTilemapName);
            if (outside == null)
            {
                Debug.LogWarning("[MobileCastleSceneSetup] Grid/outside tilemap bulunamadi; okcu tile placement baglanmadi.");
                return;
            }

            Grid grid = outside.GetComponentInParent<Grid>();
            GameObject host = grid != null ? grid.gameObject : outside.gameObject;
            var placement = EnsureComponent<MobileCastleArcherTilePlacement>(host);
            placement.Configure(outside);
            EditorUtility.SetDirty(placement);
        }

        private static void NormalizeCastleTilemapSorting(Scene scene)
        {
            NormalizeTilemapRenderer(scene, "inside", 1, MobileCastleRenderDepth.BackTilemapZ);
            NormalizeTilemapRenderer(scene, "outside0", 2, MobileCastleRenderDepth.BackTilemapZ);
            NormalizeTilemapRenderer(scene, "outside", 2, MobileCastleRenderDepth.BackTilemapZ);
            NormalizeTilemapRenderer(scene, "outside2", 4, MobileCastleRenderDepth.FrontOccluderZ);
        }

        private static void NormalizeTilemapRenderer(Scene scene, string tilemapName, int sortingOrder, float worldZ)
        {
            Tilemap tilemap = FindSceneTilemap(scene, tilemapName);
            if (tilemap == null)
                return;

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer == null)
                return;

            bool changed = false;
            if (renderer.sortingLayerName != "Wall")
            {
                renderer.sortingLayerName = "Wall";
                changed = true;
            }

            if (renderer.sortingOrder != sortingOrder)
            {
                renderer.sortingOrder = sortingOrder;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(renderer);

            Vector3 position = tilemap.transform.position;
            if (!Mathf.Approximately(position.z, worldZ))
            {
                Undo.RecordObject(tilemap.transform, "Normalize Castle Tilemap Depth");
                tilemap.transform.position = new Vector3(position.x, position.y, worldZ);
                EditorUtility.SetDirty(tilemap.transform);
            }
        }

        private static void BuildCanvasPanels(Transform canvasTransform, UIManager uiManager)
        {
            EnsureDayNightOverlay(canvasTransform);

            GameObject hudRoot = EnsureHudRoot(canvasTransform);
            ConfigureHudRoot(hudRoot);

            GameObject levelUpPanel = EnsurePanel(canvasTransform, "LevelUpPanel", false, new Color(0.04f, 0.05f, 0.07f, 0.92f));
            Center(levelUpPanel.GetComponent<RectTransform>(), new Vector2(820f, 430f));
            var levelUp = EnsureComponent<LevelUpUI>(levelUpPanel);
            levelUp.TitleText = EnsureText(levelUpPanel.transform, "TitleText", "Level Up!", 42,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-260f, -78f), new Vector2(260f, -20f));
            levelUp.AddArcherButton = EnsureButton(levelUpPanel.transform, "AddArcherButton",
                new Vector2(0.5f, 0.5f), new Vector2(-330f, -92f), new Vector2(-110f, 78f), out levelUp.AddArcherText);
            levelUp.ArrowDamageButton = EnsureButton(levelUpPanel.transform, "ArrowDamageButton",
                new Vector2(0.5f, 0.5f), new Vector2(-100f, -92f), new Vector2(100f, 78f), out levelUp.ArrowDamageText);
            levelUp.RepairGateButton = EnsureButton(levelUpPanel.transform, "RepairGateButton",
                new Vector2(0.5f, 0.5f), new Vector2(110f, -92f), new Vector2(330f, 78f), out levelUp.RepairGateText);
            levelUp.CardButtons = new[]
            {
                levelUp.AddArcherButton,
                levelUp.ArrowDamageButton,
                levelUp.RepairGateButton
            };
            levelUp.CardTexts = new[]
            {
                levelUp.AddArcherText,
                levelUp.ArrowDamageText,
                levelUp.RepairGateText
            };

            GameObject gameOverPanel = EnsurePanel(canvasTransform, "GameOverPanel", false, new Color(0.03f, 0.03f, 0.04f, 0.94f));
            Center(gameOverPanel.GetComponent<RectTransform>(), new Vector2(680f, 360f));
            var gameOver = EnsureComponent<GameOverUI>(gameOverPanel);
            gameOver.GameOverText = EnsureText(gameOverPanel.transform, "GameOverText", "GAME OVER", 48,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-260f, -86f), new Vector2(260f, -20f));
            gameOver.StatsText = EnsureText(gameOverPanel.transform, "StatsText", "Wave: 1\nLevel: 1", 26,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, -58f), new Vector2(240f, 62f));
            gameOver.RestartButton = EnsureButton(gameOverPanel.transform, "RestartButton",
                new Vector2(0.5f, 0f), new Vector2(-120f, 34f), new Vector2(120f, 96f), out var restartText);
            restartText.text = "Restart";

            uiManager.HUDPanel = hudRoot;
            uiManager.LevelUpPanel = levelUpPanel;
            uiManager.MarketPanel = hudRoot;
            uiManager.GameOverPanel = gameOverPanel;

            hudRoot.SetActive(true);
            levelUpPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        private static GameObject EnsureHudRoot(Transform canvasTransform)
        {
            DestroyChildIfExists(canvasTransform, "HUDPanel");
            DestroyChildIfExists(canvasTransform, "MarketPanel");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            Transform existing = canvasTransform.Find("MobileCastleHudRoot");

            if (prefab != null)
            {
                if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) != prefab)
                {
                    Undo.DestroyObjectImmediate(existing.gameObject);
                    existing = null;
                }

                if (existing == null)
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab, canvasTransform) as GameObject;
                    if (instance != null)
                    {
                        Undo.RegisterCreatedObjectUndo(instance, "Create Mobile Castle HUD");
                        instance.name = "MobileCastleHudRoot";
                        SetLayerRecursive(instance, LayerMask.NameToLayer("UI"));
                        return instance;
                    }
                }
            }

            return existing != null
                ? existing.gameObject
                : EnsurePanel(canvasTransform, "MobileCastleHudRoot", true, new Color(0f, 0f, 0f, 0f));
        }

        private static void ConfigureHudRoot(GameObject hudRoot)
        {
            RemoveGeneratedCanvasComponents(hudRoot);
            SetLayerRecursive(hudRoot, LayerMask.NameToLayer("UI"));
            Stretch(hudRoot.GetComponent<RectTransform>());

            var hud = EnsureComponent<HUDController>(hudRoot);
            hud.WoodText = FindOrCreateText(hudRoot.transform, "WoodText", "Wood: 150", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -42f), new Vector2(210f, -10f));
            hud.StoneText = FindOrCreateText(hudRoot.transform, "StoneText", "Stone: 80", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(224f, -42f), new Vector2(410f, -10f));
            hud.IronText = FindOrCreateText(hudRoot.transform, "IronText", "Iron: 45", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(424f, -42f), new Vector2(610f, -10f));
            hud.FoodText = FindOrCreateText(hudRoot.transform, "FoodText", "Food: 150", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(624f, -42f), new Vector2(810f, -10f));
            hud.PopulationText = FindOrCreateText(hudRoot.transform, "PopulationText", "Pop: 10/20", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(824f, -42f), new Vector2(1010f, -10f));
            hud.ArrowText = FindOrCreateText(hudRoot.transform, "ArrowText", "Arrows: 200", 22,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(1024f, -42f), new Vector2(1210f, -10f));
            hud.WaveText = FindOrCreateText(hudRoot.transform, "WaveText", "WAVE 01", 34,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-170f, -56f), new Vector2(170f, -8f));
            hud.KillsText = FindOrCreateText(hudRoot.transform, "KillsText", "KILLS 0 / 30", 24,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-180f, -92f), new Vector2(180f, -58f));
            hud.DefenseText = FindOrCreateText(hudRoot.transform, "DefenseText", "DEF 100%", 20,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(190f, -92f), new Vector2(360f, -58f));
            hud.WaveRewardText = FindOrCreateText(hudRoot.transform, "WaveRewardText", "Wave Cleared", 20,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-250f, -132f), new Vector2(250f, -98f));
            hud.WaveRewardText.gameObject.SetActive(false);
            BindDefenseHudFields(hudRoot, hud);
            hud.DamageFlashImage = EnsureDamageFlashOverlay(hudRoot.transform);
            DestroyChildIfExists(hudRoot.transform, "ArcherTypeText");
            hud.ArcherTypeText = null;
            HideEconomyFocus(hudRoot);
            ConfigureCastleEconomy(hudRoot);

            var market = EnsureComponent<MarketUI>(hudRoot);
            market.ArcherDrawerPanel = FindRectTransformByName(hudRoot, "ArcherDrawerPanel")
                ?? EnsureFallbackDrawer(hudRoot.transform);
            market.DrawerToggleButton = FindComponentInChildrenByName<Button>(hudRoot, "DrawerToggleButton");
            if (market.DrawerToggleButton == null)
            {
                market.DrawerToggleButton = EnsureButton(hudRoot.transform, "DrawerToggleButton",
                    new Vector2(1f, 0.5f), new Vector2(-96f, -42f), new Vector2(-16f, 42f), out _);
                SetButtonLabel(market.DrawerToggleButton, "Menu");
            }

            EnsureFallbackArcherRows(market.ArcherDrawerPanel);
            EnsureFallbackTechAndPrep(market.ArcherDrawerPanel);
            BindMarketFields(hudRoot, market);
            HidePlayerFacingPrepButtons(market);
        }

        private static void BindDefenseHudFields(GameObject hudRoot, HUDController hud)
        {
            hud.DefensePercentText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "DefensePercentText");
            hud.DefenseWallText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "DefenseWallText");
            hud.DefenseGateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "DefenseGateText");
            hud.DefenseCoreText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "DefenseCoreText");
            hud.DefenseWallFill = FindComponentInChildrenByName<Image>(hudRoot, "DefenseWallFill");
            hud.DefenseGateFill = FindComponentInChildrenByName<Image>(hudRoot, "DefenseGateFill");
            hud.DefenseCoreFill = FindComponentInChildrenByName<Image>(hudRoot, "DefenseCoreFill");
            hud.DefenseDamageGlow = FindComponentInChildrenByName<Image>(hudRoot, "DefenseDamageGlow");

            ConfigureDefenseFillImage(hud.DefenseWallFill);
            ConfigureDefenseFillImage(hud.DefenseGateFill);
            ConfigureDefenseFillImage(hud.DefenseCoreFill);

            if (hud.DefensePercentText != null && hud.DefenseText != null)
                hud.DefenseText.gameObject.SetActive(false);
        }

        private static void ConfigureDefenseFillImage(Image image)
        {
            if (image == null)
                return;

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
        }

        private static void EnsureDayNightOverlay(Transform canvasTransform)
        {
            GameObject overlay = EnsureChild(canvasTransform, "DayNightOverlay", true);
            overlay.transform.SetAsFirstSibling();
            SetLayerRecursive(overlay, LayerMask.NameToLayer("UI"));
            Stretch(EnsureComponent<RectTransform>(overlay));

            var image = EnsureComponent<Image>(overlay);
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            var controller = EnsureComponent<DayNightOverlayController>(overlay);
            controller.OverlayImage = image;
        }

        private static Image EnsureDamageFlashOverlay(Transform hudRoot)
        {
            GameObject overlay = EnsureChild(hudRoot, "DamageFlashOverlay", true);
            overlay.transform.SetAsFirstSibling();
            SetLayerRecursive(overlay, LayerMask.NameToLayer("UI"));
            Stretch(EnsureComponent<RectTransform>(overlay));

            var image = EnsureComponent<Image>(overlay);
            image.color = new Color(1f, 0f, 0f, 0f);
            image.raycastTarget = false;
            return image;
        }

        private static void HideEconomyFocus(GameObject hudRoot)
        {
            DestroyComponentIfExists<EconomyFocusUI>(hudRoot);
            SetOptionalChildActive(hudRoot, "EconomyFocusText", false);
            SetOptionalChildActive(hudRoot, "EconomyBalancedButton", false);
            SetOptionalChildActive(hudRoot, "EconomyWoodButton", false);
            SetOptionalChildActive(hudRoot, "EconomyStoneButton", false);
            SetOptionalChildActive(hudRoot, "EconomyIronButton", false);
            SetOptionalChildActive(hudRoot, "EconomyFoodButton", false);
            SetOptionalChildActive(hudRoot, "EconomyBalancedSelected", false);
            SetOptionalChildActive(hudRoot, "EconomyWoodSelected", false);
            SetOptionalChildActive(hudRoot, "EconomyStoneSelected", false);
            SetOptionalChildActive(hudRoot, "EconomyIronSelected", false);
            SetOptionalChildActive(hudRoot, "EconomyFoodSelected", false);
        }

        private static void ConfigureCastleEconomy(GameObject hudRoot)
        {
            var castleEconomy = EnsureComponent<CastleEconomyUI>(hudRoot);
            castleEconomy.CastleEconomyPanel = FindChildByName(hudRoot, "CastleEconomyPanel");
            castleEconomy.CloseCastleEconomyButton = FindComponentInChildrenByName<Button>(hudRoot, "CloseCastleEconomyButton");
            castleEconomy.ConfirmCastleEconomyButton = FindComponentInChildrenByName<Button>(hudRoot, "ConfirmCastleEconomyButton");
            castleEconomy.CastleTapHint = FindChildByName(hudRoot, "CastleTapHint");
            castleEconomy.CastleTapHintText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CastleTapHintText");
            castleEconomy.CastleTapHintPulse = FindChildByName(hudRoot, "CastleTapHintPulse");

            castleEconomy.PopulationTotalText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "PopulationTotalText");
            castleEconomy.PopulationIdleText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "PopulationIdleText");
            castleEconomy.PopulationArchersText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "PopulationArchersText");
            castleEconomy.PopulationGrowthText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "PopulationGrowthText");
            castleEconomy.WorkerBudgetText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WorkerBudgetText");

            castleEconomy.WoodWorkerSlider = FindComponentInChildrenByName<Slider>(hudRoot, "WoodWorkerSlider");
            castleEconomy.StoneWorkerSlider = FindComponentInChildrenByName<Slider>(hudRoot, "StoneWorkerSlider");
            castleEconomy.IronWorkerSlider = FindComponentInChildrenByName<Slider>(hudRoot, "IronWorkerSlider");
            castleEconomy.FoodWorkerSlider = FindComponentInChildrenByName<Slider>(hudRoot, "FoodWorkerSlider");
            castleEconomy.WoodAssignButton = FindComponentInChildrenByName<Button>(hudRoot, "WoodAssignButton");
            castleEconomy.StoneAssignButton = FindComponentInChildrenByName<Button>(hudRoot, "StoneAssignButton");
            castleEconomy.IronAssignButton = FindComponentInChildrenByName<Button>(hudRoot, "IronAssignButton");
            castleEconomy.FoodAssignButton = FindComponentInChildrenByName<Button>(hudRoot, "FoodAssignButton");
            castleEconomy.WoodWorkerText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerText");
            castleEconomy.StoneWorkerText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerText");
            castleEconomy.IronWorkerText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerText");
            castleEconomy.FoodWorkerText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerText");
            castleEconomy.WoodRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodRateText");
            castleEconomy.StoneRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneRateText");
            castleEconomy.IronRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronRateText");
            castleEconomy.FoodRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodRateText");

            castleEconomy.ProjectedIncomeText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "ProjectedIncomeText");
            castleEconomy.ProjectedWoodText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "ProjectedWoodText");
            castleEconomy.ProjectedStoneText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "ProjectedStoneText");
            castleEconomy.ProjectedIronText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "ProjectedIronText");
            castleEconomy.ProjectedFoodText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "ProjectedFoodText");

            castleEconomy.CastleRepairButton = FindComponentInChildrenByName<Button>(hudRoot, "CastleRepairButton");
            castleEconomy.CastleRepairStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CastleRepairStatusText");
            castleEconomy.CastleRepairCostText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CastleRepairCostText");

            castleEconomy.EconomyEventPanel = FindChildByName(hudRoot, "EconomyEventPanel");
            castleEconomy.EconomyEventTitleText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyEventTitleText");
            castleEconomy.EconomyEventDescriptionText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyEventDescriptionText");
            castleEconomy.EconomyEventChoiceAButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyEventChoiceAButton");
            castleEconomy.EconomyEventChoiceBButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyEventChoiceBButton");
            castleEconomy.EconomyEventChoiceAText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyEventChoiceAText");
            castleEconomy.EconomyEventChoiceBText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyEventChoiceBText");
            castleEconomy.EconomyEventBadge = FindChildByName(hudRoot, "EconomyEventBadge");
            castleEconomy.EconomyEventBadgeText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyEventBadgeText");
            castleEconomy.EconomyEventGlow = FindChildByName(hudRoot, "EconomyEventGlow");

            DisableRaycastTargets(castleEconomy.CastleTapHint);
            DisableRaycastTargets(castleEconomy.EconomyEventBadge);
            DisableRaycastTargets(castleEconomy.EconomyEventGlow);

            if (castleEconomy.CastleEconomyPanel != null)
                castleEconomy.CastleEconomyPanel.SetActive(false);
            if (castleEconomy.EconomyEventPanel != null)
                castleEconomy.EconomyEventPanel.SetActive(false);
            if (castleEconomy.CastleTapHint != null)
                castleEconomy.CastleTapHint.SetActive(false);
            if (castleEconomy.EconomyEventBadge != null)
                castleEconomy.EconomyEventBadge.SetActive(false);
            if (castleEconomy.EconomyEventGlow != null)
                castleEconomy.EconomyEventGlow.SetActive(false);
        }

        private static void SetOptionalChildActive(GameObject root, string name, bool active)
        {
            GameObject child = FindChildByName(root, name);
            if (child != null)
                child.SetActive(active);
        }

        private static void DisableRaycastTargets(GameObject root)
        {
            if (root == null)
                return;

            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private static void ConfigureEconomyFocus(GameObject hudRoot)
        {
            EnsureFallbackEconomyFocus(hudRoot.transform);

            var economyFocus = EnsureComponent<EconomyFocusUI>(hudRoot);
            economyFocus.EconomyFocusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "EconomyFocusText");
            economyFocus.EconomyBalancedButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyBalancedButton");
            economyFocus.EconomyWoodButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyWoodButton");
            economyFocus.EconomyStoneButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyStoneButton");
            economyFocus.EconomyIronButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyIronButton");
            economyFocus.EconomyFoodButton = FindComponentInChildrenByName<Button>(hudRoot, "EconomyFoodButton");
            economyFocus.EconomyBalancedSelected = FindChildByName(hudRoot, "EconomyBalancedSelected");
            economyFocus.EconomyWoodSelected = FindChildByName(hudRoot, "EconomyWoodSelected");
            economyFocus.EconomyStoneSelected = FindChildByName(hudRoot, "EconomyStoneSelected");
            economyFocus.EconomyIronSelected = FindChildByName(hudRoot, "EconomyIronSelected");
            economyFocus.EconomyFoodSelected = FindChildByName(hudRoot, "EconomyFoodSelected");
        }

        private static void EnsureFallbackEconomyFocus(Transform root)
        {
            FindOrCreateText(root, "EconomyFocusText", "FOCUS: BALANCED", 16,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -90f), new Vector2(260f, -62f));

            EnsureFocusButton(root, "EconomyBalancedButton", "BAL", 24f, 80f);
            EnsureFocusButton(root, "EconomyWoodButton", "WOOD", 112f, 198f);
            EnsureFocusButton(root, "EconomyStoneButton", "STONE", 206f, 304f);
            EnsureFocusButton(root, "EconomyIronButton", "IRON", 312f, 398f);
            EnsureFocusButton(root, "EconomyFoodButton", "FOOD", 406f, 492f);
        }

        private static void EnsureFocusButton(Transform root, string name, string label, float left, float right)
        {
            if (FindComponentInChildrenByName<Button>(root.gameObject, name) != null)
                return;

            var button = EnsureButton(root, name,
                new Vector2(0f, 1f), new Vector2(left, -132f), new Vector2(right, -94f), out _);
            SetButtonLabel(button, label);
        }

        private static TextMeshProUGUI FindOrCreateText(Transform root, string name, string value, int fontSize,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            return FindComponentInChildrenByName<TextMeshProUGUI>(root.gameObject, name)
                ?? EnsureText(root, name, value, fontSize, alignment, anchorMin, anchorMax, offsetMin, offsetMax);
        }

        private static RectTransform EnsureFallbackDrawer(Transform root)
        {
            GameObject drawer = EnsurePanel(root, "ArcherDrawerPanel", true, new Color(0.05f, 0.055f, 0.045f, 0.94f));
            SetRect(drawer.GetComponent<RectTransform>(),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-470f, -430f), new Vector2(-18f, 430f));
            return drawer.GetComponent<RectTransform>();
        }

        private static void EnsureFallbackArcherRows(RectTransform drawer)
        {
            EnsureArcherRow(drawer, "Basic", -120f);
            EnsureArcherRow(drawer, "Rapid", -270f);
            EnsureArcherRow(drawer, "Frost", -420f);
        }

        private static void EnsureArcherRow(RectTransform drawer, string prefix, float topOffset)
        {
            string rowName = prefix + "ArcherRow";
            if (FindChildByName(drawer.gameObject, rowName) != null)
                return;

            GameObject row = EnsurePanel(drawer, rowName, true, new Color(0.11f, 0.12f, 0.10f, 0.94f));
            SetRect(row.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(16f, topOffset - 118f), new Vector2(-16f, topOffset));

            EnsureText(row.transform, prefix + "CountText", "x0", 20,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -38f), new Vector2(92f, -8f));
            EnsureText(row.transform, prefix + "DpsText", "DPS 0", 18,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(96f, -38f), new Vector2(240f, -8f));
            EnsureText(row.transform, prefix + "LevelText", "Lv 1", 18,
                TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(244f, -38f), new Vector2(348f, -8f));
            EnsureText(row.transform, prefix + "CostText", "Buy", 16,
                TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(16f, 14f), new Vector2(-16f, 44f));

            var buy = EnsureButton(row.transform, prefix + "BuyButton",
                new Vector2(1f, 0f), new Vector2(-188f, 56f), new Vector2(-100f, 104f), out _);
            SetButtonLabel(buy, "Buy");
            var upgrade = EnsureButton(row.transform, prefix + "UpgradeButton",
                new Vector2(1f, 0f), new Vector2(-92f, 56f), new Vector2(-8f, 104f), out _);
            SetButtonLabel(upgrade, "Upgrade");
        }

        private static void EnsureFallbackTechAndPrep(RectTransform drawer)
        {
            if (FindChildByName(drawer.gameObject, "RapidTechUnlockButton") == null)
            {
                var rapid = EnsureButton(drawer, "RapidTechUnlockButton",
                    new Vector2(0f, 0f), new Vector2(18f, 222f), new Vector2(214f, 278f), out _);
                SetButtonLabel(rapid, "Unlock Rapid");
            }

            if (FindChildByName(drawer.gameObject, "FrostTechUnlockButton") == null)
            {
                var frost = EnsureButton(drawer, "FrostTechUnlockButton",
                    new Vector2(0f, 0f), new Vector2(232f, 222f), new Vector2(432f, 278f), out _);
                SetButtonLabel(frost, "Unlock Frost");
            }

            EnsurePrepButton(drawer, "RepairButton", "Repair", 150f);
            DestroyLegacyFallbackPrepButtonIfExists(drawer, "FortifyButton");
            DestroyLegacyFallbackPrepButtonIfExists(drawer, "RallyButton");
        }

        private static void EnsurePrepButton(RectTransform drawer, string name, string label, float bottom)
        {
            if (FindChildByName(drawer.gameObject, name) != null)
                return;

            var button = EnsureButton(drawer, name,
                new Vector2(0f, 0f), new Vector2(18f, bottom), new Vector2(432f, bottom + 52f), out _);
            SetButtonLabel(button, label);
        }

        private static void DestroyLegacyFallbackPrepButtonIfExists(RectTransform drawer, string name)
        {
            GameObject child = FindChildByName(drawer.gameObject, name);
            if (child == null || !IsLegacyFallbackButton(child))
                return;

            Undo.DestroyObjectImmediate(child);
        }

        private static bool IsLegacyFallbackButton(GameObject gameObject)
        {
            var image = gameObject.GetComponent<Image>();
            if (image == null)
                return false;

            Color c = image.color;
            bool fallbackBlue = Mathf.Abs(c.r - 0.20f) < 0.03f
                && Mathf.Abs(c.g - 0.28f) < 0.03f
                && Mathf.Abs(c.b - 0.36f) < 0.03f;

            return fallbackBlue && gameObject.transform.Find("Text") != null;
        }

        private static void BindMarketFields(GameObject root, MarketUI market)
        {
            market.BasicCountText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "BasicCountText");
            market.BasicDpsText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "BasicDpsText");
            market.BasicLevelText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "BasicLevelText");
            market.BasicCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "BasicCostText");
            market.BasicBuyButton = FindComponentInChildrenByName<Button>(root, "BasicBuyButton");
            market.BasicUpgradeButton = FindComponentInChildrenByName<Button>(root, "BasicUpgradeButton");

            market.RapidCountText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RapidCountText");
            market.RapidDpsText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RapidDpsText");
            market.RapidLevelText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RapidLevelText");
            market.RapidCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RapidCostText");
            market.RapidBuyButton = FindComponentInChildrenByName<Button>(root, "RapidBuyButton");
            market.RapidUpgradeButton = FindComponentInChildrenByName<Button>(root, "RapidUpgradeButton");

            market.FrostCountText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FrostCountText");
            market.FrostDpsText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FrostDpsText");
            market.FrostLevelText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FrostLevelText");
            market.FrostCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FrostCostText");
            market.FrostBuyButton = FindComponentInChildrenByName<Button>(root, "FrostBuyButton");
            market.FrostUpgradeButton = FindComponentInChildrenByName<Button>(root, "FrostUpgradeButton");

            market.RapidTechUnlockButton = FindComponentInChildrenByName<Button>(root, "RapidTechUnlockButton");
            market.FrostTechUnlockButton = FindComponentInChildrenByName<Button>(root, "FrostTechUnlockButton");
            market.RepairButton = FindComponentInChildrenByName<Button>(root, "RepairButton");
            market.RefillArrowsButton = FindComponentInChildrenByName<Button>(root, "RefillArrowsButton");
            market.StartNextWaveButton = FindComponentInChildrenByName<Button>(root, "StartNextWaveButton");
            market.FortifyButton = FindComponentInChildrenByName<Button>(root, "FortifyButton");
            market.RallyButton = FindComponentInChildrenByName<Button>(root, "RallyButton");
            market.RepairCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RepairCostText");
            market.FortifyCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FortifyCostText");
            market.RallyCostText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RallyCostText");
            market.RepairStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RepairStatusText");
            market.FortifyStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "FortifyStatusText");
            market.RallyStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "RallyStatusText");
        }

        private static void HidePlayerFacingPrepButtons(MarketUI market)
        {
            if (market.RepairButton != null)
            {
                market.RepairButton.interactable = false;
                market.RepairButton.gameObject.SetActive(false);
            }

            if (market.RepairCostText != null)
                market.RepairCostText.gameObject.SetActive(false);
            if (market.RepairStatusText != null)
                market.RepairStatusText.gameObject.SetActive(false);

            if (market.RefillArrowsButton != null)
            {
                market.RefillArrowsButton.interactable = false;
                market.RefillArrowsButton.gameObject.SetActive(false);
            }

            if (market.StartNextWaveButton != null)
            {
                market.StartNextWaveButton.interactable = false;
                market.StartNextWaveButton.gameObject.SetActive(false);
            }

            if (market.FortifyButton != null)
            {
                market.FortifyButton.interactable = false;
                market.FortifyButton.gameObject.SetActive(false);
            }

            if (market.RallyButton != null)
            {
                market.RallyButton.interactable = false;
                market.RallyButton.gameObject.SetActive(false);
            }
        }

        private static void EnsureSubSceneRoot(Scene scene, SceneAsset subSceneAsset)
        {
            GameObject subSceneObject = FindRoot(scene, "MobileCastleCombatSubScene");
            if (subSceneObject == null)
            {
                subSceneObject = new GameObject("MobileCastleCombatSubScene");
                Undo.RegisterCreatedObjectUndo(subSceneObject, "Create Mobile Castle Combat SubScene");
                SceneManager.MoveGameObjectToScene(subSceneObject, scene);
            }

            Type subSceneType = FindComponentType("Unity.Scenes.SubScene");
            if (subSceneType == null)
            {
                Debug.LogWarning("[MobileCastleSceneSetup] Unity.Scenes.SubScene type bulunamadi. Entities/Scenes paketleri yuklendikten sonra tool tekrar calistirilabilir.");
                return;
            }

            var subScene = EnsureComponent(subSceneObject, subSceneType);
            var serializedSubScene = new SerializedObject(subScene);
            SerializedProperty sceneAssetProperty = serializedSubScene.FindProperty("_SceneAsset");
            if (sceneAssetProperty != null)
                sceneAssetProperty.objectReferenceValue = subSceneAsset;

            SerializedProperty autoLoadProperty = serializedSubScene.FindProperty("_AutoLoadScene");
            if (autoLoadProperty != null)
                autoLoadProperty.boolValue = true;

            serializedSubScene.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
        }

        private static void RemoveGeneratedCanvasComponents(GameObject root)
        {
            DestroyComponentIfExists<GraphicRaycaster>(root);
            DestroyComponentIfExists<CanvasScaler>(root);
            DestroyComponentIfExists<Canvas>(root);
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                Undo.DestroyObjectImmediate(child.gameObject);
        }

        private static void DestroyComponentIfExists<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null)
                Undo.DestroyObjectImmediate(component);
        }

        private static T FindComponentInChildrenByName<T>(GameObject root, string name) where T : Component
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component.gameObject.name == name)
                    return component;
            }

            return null;
        }

        private static RectTransform FindRectTransformByName(GameObject root, string name)
        {
            GameObject child = FindChildByName(root, name);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        private static GameObject FindChildByName(GameObject root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.name == name)
                    return child.gameObject;
            }

            return null;
        }

        private static GameObject EnsurePanel(Transform parent, string name, bool active, Color color)
        {
            GameObject panel = EnsureChild(parent, name, true);
            SetLayerRecursive(panel, LayerMask.NameToLayer("UI"));

            var image = EnsureComponent<Image>(panel);
            image.color = color;
            panel.SetActive(active);
            return panel;
        }

        private static Slider EnsureSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color backgroundColor, Color fillColor)
        {
            GameObject sliderObject = EnsureChild(parent, name, true);
            SetRect(sliderObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            SetLayerRecursive(sliderObject, LayerMask.NameToLayer("UI"));

            var slider = EnsureComponent<Slider>(sliderObject);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.transition = Selectable.Transition.None;

            GameObject background = EnsureChild(sliderObject.transform, "Background", true);
            Stretch(background.GetComponent<RectTransform>());
            var backgroundImage = EnsureComponent<Image>(background);
            backgroundImage.color = backgroundColor;

            GameObject fillArea = EnsureChild(sliderObject.transform, "Fill Area", true);
            Stretch(fillArea.GetComponent<RectTransform>());

            GameObject fill = EnsureChild(fillArea.transform, "Fill", true);
            Stretch(fill.GetComponent<RectTransform>());
            var fillImage = EnsureComponent<Image>(fill);
            fillImage.color = fillColor;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.handleRect = null;
            return slider;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, string value, int fontSize,
            TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject textObject = EnsureChild(parent, name, true);
            SetRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
            SetLayerRecursive(textObject, LayerMask.NameToLayer("UI"));

            var text = EnsureComponent<TextMeshProUGUI>(textObject);
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, Vector2 anchor,
            Vector2 offsetMin, Vector2 offsetMax, out TMP_Text label)
        {
            GameObject buttonObject = EnsureChild(parent, name, true);
            SetRect(buttonObject.GetComponent<RectTransform>(), anchor, anchor, offsetMin, offsetMax);
            SetLayerRecursive(buttonObject, LayerMask.NameToLayer("UI"));

            var image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.20f, 0.28f, 0.36f, 0.95f);

            var button = EnsureComponent<Button>(buttonObject);
            button.targetGraphic = image;

            label = EnsureText(buttonObject.transform, "Text", name, 22,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            return null;
        }

        private static Tilemap FindSceneTilemap(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var tilemaps = root.GetComponentsInChildren<Tilemap>(true);
                foreach (Tilemap tilemap in tilemaps)
                {
                    if (tilemap != null && tilemap.name == name)
                        return tilemap;
                }
            }

            return null;
        }

        private static GameObject EnsureSceneRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null)
                return root;

            root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject EnsureChild(Transform parent, string name, bool rectTransform)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            GameObject childObject = rectTransform
                ? new GameObject(name, typeof(RectTransform))
                : new GameObject(name);

            Undo.RegisterCreatedObjectUndo(childObject, "Create " + name);
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null)
                return component;

            return Undo.AddComponent<T>(gameObject);
        }

        private static Component EnsureComponent(GameObject gameObject, Type componentType)
        {
            var component = gameObject.GetComponent(componentType);
            if (component != null)
                return component;

            return Undo.AddComponent(gameObject, componentType);
        }

        private static Type FindComponentType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            return null;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void Center(RectTransform rectTransform, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetLayerRecursive(GameObject gameObject, int layer)
        {
            if (layer < 0)
                return;

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        private static void SetSerializedInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetSerializedFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetSerializedColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.colorValue = value;
        }
    }
}
#endif
