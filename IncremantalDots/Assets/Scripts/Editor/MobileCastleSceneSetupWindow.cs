#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
        private const string ArcherDefinitionFolder = "Assets/ScriptableObject/MobileCastle/Archers";
        private const string ArcherCatalogPath = ArcherDefinitionFolder + "/ArcherRecruitmentCatalog.asset";
        private const string BasicArcherDefinitionPath = ArcherDefinitionFolder + "/BasicArcher.asset";
        private const string RapidArcherDefinitionPath = ArcherDefinitionFolder + "/RapidArcher.asset";
        private const string FrostArcherDefinitionPath = ArcherDefinitionFolder + "/FrostArcher.asset";
        private const string TechTreeFolder = "Assets/ScriptableObject/MobileCastle/TechTree";
        private const string TechTreeCatalogPath = TechTreeFolder + "/TechTreeCatalog.asset";
        private const string CouncilFolder = "Assets/ScriptableObject/MobileCastle/Council";
        private const string CouncilCatalogPath = CouncilFolder + "/CouncilEventCatalog.asset";
        private const string CouncilAppearSfxPath = FantasyUiSfxRoot + "/Book Handle 1-2.wav";
        private const string CouncilChooseSfxPath = FantasyUiSfxRoot + "/Card Place 1-1.wav";
        private const string TechBuySfxPath = FantasyUiSfxRoot + "/Coins 2-1.wav";
        private const string TechRevealSfxPath = FantasyUiSfxRoot + "/Magical Texture Chimes 1-1.wav";
        private const string TechDeniedSfxPath = FantasyUiSfxRoot + "/Key & Lock 1-1.wav";
        private const string TechPanelOpenSfxPath = FantasyUiSfxRoot + "/Book Page 1-2.wav";
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
            // Balance: baslangic kaynaklari ilk dakikalarda secim baskisi yaratacak duzeye cekildi
            gameStateAuthoring.InitialWood = 160;
            gameStateAuthoring.InitialStone = 80;
            gameStateAuthoring.InitialIron = 50;
            gameStateAuthoring.InitialFood = 120;
            gameStateAuthoring.TestWoodProdRate = 160f;
            gameStateAuthoring.TestStoneProdRate = 55f;
            gameStateAuthoring.TestIronProdRate = 30f;
            gameStateAuthoring.TestFoodProdRate = 105f;
            gameStateAuthoring.InitialPopulation = 60;
            gameStateAuthoring.InitialCapacity = 999999;
            gameStateAuthoring.TestWorkers = 53;
            gameStateAuthoring.TestArchers = 4;
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
            mobileAuthoring.SpawnBatchSize = 2;
            mobileAuthoring.ZombieBaseHP = 20f;
            mobileAuthoring.ZombieHpGrowthPerCycle = 0.30f;
            mobileAuthoring.ZombieBaseDamage = 5f;
            mobileAuthoring.ZombieDamagePerCycle = 0.5f;
            mobileAuthoring.SpawnBatchGrowthPerCycle = 0.10f;
            mobileAuthoring.MaxSpawnBatch = 12;
            mobileAuthoring.MaxAliveZombies = 900;
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
            mobileAuthoring.KillRewardWaveScale = 0f; // gelir/zorluk ayrismasi: kill odulu cycle ile buyumez
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
            mobileAuthoring.ContinuousSiegeEnabled = true;
            mobileAuthoring.SiegeCycleDuration = 60f;
            mobileAuthoring.SiegeDayDuration = 22f;
            mobileAuthoring.SiegeDuskDuration = 8f;
            mobileAuthoring.SiegeNightDuration = 22f;
            mobileAuthoring.SiegeDawnDuration = 8f;
            mobileAuthoring.SiegeDayIntensityMultiplier = 0.55f;
            mobileAuthoring.SiegeDuskStartIntensityMultiplier = 1f;
            mobileAuthoring.SiegeDuskEndIntensityMultiplier = 1.35f;
            mobileAuthoring.SiegeNightIntensityMultiplier = 1.65f;
            mobileAuthoring.SiegeDawnIntensityMultiplier = 0.15f;
            mobileAuthoring.RepairBaseWoodCost = 120;
            mobileAuthoring.RepairBaseStoneCost = 80;
            mobileAuthoring.BaseSpawnInterval = 0.95f;
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
            mobileAuthoring.WoodWorkerCap = 40;
            mobileAuthoring.StoneWorkerCap = 30;
            mobileAuthoring.IronWorkerCap = 24;
            mobileAuthoring.FoodWorkerCap = 40;
            mobileAuthoring.WoodWorkerProductionPerMin = 8f;
            mobileAuthoring.StoneWorkerProductionPerMin = 5.5f;
            mobileAuthoring.IronWorkerProductionPerMin = 3.8f;
            mobileAuthoring.FoodWorkerProductionPerMin = 7f;
            mobileAuthoring.WorkerEconomyRewardMultiplier = 0.25f;
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
            // Tek cephe (K4): sabit tek ekran — duvar hatti (x~0, owner tilemap'i) solda,
            // spawn seridi (x~13-15) sagda; solda koy icin ~8 birim alan kalir
            camera.transform.position = new Vector3(6f, 0f, -10f);
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
            ArcherRecruitmentCatalogSO archerCatalog = EnsureDefaultArcherRecruitmentCatalog();
            TechTreeCatalogSO techCatalog = EnsureDefaultTechTreeCatalog();
            CouncilEventCatalogSO councilEventCatalog = EnsureDefaultCouncilCatalog();
            GameObject gameManagerObject = FindRoot(scene, "GameManager");
            if (gameManagerObject == null)
            {
                gameManagerObject = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(gameManagerObject, "Create GameManager");
                SceneManager.MoveGameObjectToScene(gameManagerObject, scene);
            }
            var gameManager = EnsureComponent<GameManager>(gameManagerObject);
            AssignObjectReference(gameManager, "archerCatalog", archerCatalog);
            AssignObjectReference(gameManager, "techTreeCatalog", techCatalog);
            AssignObjectReference(gameManager, "councilCatalog", councilEventCatalog);

            var uiManager = EnsureComponent<UIManager>(canvas.gameObject);
            BuildCanvasPanels(canvas.transform, uiManager, archerCatalog);
            EnsureCastleClickTarget(scene);
            EnsureArcherTilePlacement(scene);
            EnsureCombatFeedbackRoot(scene);
            NormalizeCastleTilemapSorting(scene);
        }

        private static ArcherRecruitmentCatalogSO EnsureDefaultArcherRecruitmentCatalog()
        {
            EnsureAssetFolder(ArcherDefinitionFolder);

            ArcherDefinitionSO basic = EnsureArcherDefinitionAsset(BasicArcherDefinitionPath, ArcherType.Basic);
            ArcherDefinitionSO rapid = EnsureArcherDefinitionAsset(RapidArcherDefinitionPath, ArcherType.Rapid);
            ArcherDefinitionSO frost = EnsureArcherDefinitionAsset(FrostArcherDefinitionPath, ArcherType.Frost);

            var catalog = AssetDatabase.LoadAssetAtPath<ArcherRecruitmentCatalogSO>(ArcherCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ArcherRecruitmentCatalogSO>();
                AssetDatabase.CreateAsset(catalog, ArcherCatalogPath);
            }

            var definitions = catalog.Definitions ?? Array.Empty<ArcherDefinitionSO>();
            bool hasBasic = Array.IndexOf(definitions, basic) >= 0;
            bool hasRapid = Array.IndexOf(definitions, rapid) >= 0;
            bool hasFrost = Array.IndexOf(definitions, frost) >= 0;
            if (!hasBasic || !hasRapid || !hasFrost)
            {
                Undo.RecordObject(catalog, "Configure Archer Recruitment Catalog");
                var merged = new List<ArcherDefinitionSO>(definitions.Length + 3);
                for (int i = 0; i < definitions.Length; i++)
                {
                    if (definitions[i] != null && !merged.Contains(definitions[i]))
                        merged.Add(definitions[i]);
                }

                if (!hasBasic) merged.Add(basic);
                if (!hasRapid) merged.Add(rapid);
                if (!hasFrost) merged.Add(frost);

                catalog.Definitions = merged.ToArray();
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
        }

        private static ArcherDefinitionSO EnsureArcherDefinitionAsset(string path, ArcherType type)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ArcherDefinitionSO>(path);
            if (definition != null)
                return definition;

            definition = ScriptableObject.CreateInstance<ArcherDefinitionSO>();
            definition.ApplyDefaultValues(type);
            AssetDatabase.CreateAsset(definition, path);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string normalized = path.Replace('\\', '/').TrimEnd('/');
            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string folderName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                return;

            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(normalized))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        // ---------------------------------------------------------------------------------
        // Tech Tree SO seed (ArcherRecruitmentCatalog kalibi): default node'lar SADECE eksikse
        // olusturulur (mevcut asset degerlerine dokunulmaz), katalog merge-only calisir —
        // kullanicinin sonradan ekledigi ekstra tech node'lari ASLA silinmez.
        // ---------------------------------------------------------------------------------

        private struct TechNodeSeed
        {
            public string Id;
            public string Title;
            public string Description;
            public ResourceCost Cost;
            public int MaxLevel;
            public float CostGrowthPerLevel;
            public string[] Prerequisites;
            public string[] RevealChildren;
            public TechNodeEffect[] Effects;
        }

        private static TechNodeSeed[] GetDefaultTechNodeSeeds()
        {
            return new[]
            {
                new TechNodeSeed
                {
                    Id = "castle_heart", Title = "Castle Heart",
                    Description = "The living core of the castle. Every path grows from here.",
                    Cost = ResourceCost.Zero, MaxLevel = 1,
                    Prerequisites = new string[0],
                    RevealChildren = new[] { "basic_archer", "wood_camp", "wall_reinforcement", "frost_arrows" },
                    Effects = new TechNodeEffect[0]
                },
                new TechNodeSeed
                {
                    Id = "basic_archer", Title = "Basic Archer",
                    Description = "Garrison drills that open the archery discipline.",
                    Cost = new ResourceCost(40, 0, 0, 0), MaxLevel = 1,
                    Prerequisites = new[] { "castle_heart" },
                    RevealChildren = new[] { "bow_training", "rapid_volley" },
                    Effects = new TechNodeEffect[0]
                },
                new TechNodeSeed
                {
                    Id = "bow_training", Title = "Bow Training",
                    Description = "+15% archer damage per level.",
                    Cost = new ResourceCost(60, 0, 20, 0), MaxLevel = 3,
                    Prerequisites = new[] { "basic_archer" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ModifyArcherDamagePercent, Value = 0.15f } }
                },
                new TechNodeSeed
                {
                    Id = "rapid_volley", Title = "Rapid Volley",
                    Description = "+12% archer fire rate.",
                    Cost = new ResourceCost(90, 0, 50, 0), MaxLevel = 1,
                    Prerequisites = new[] { "basic_archer" },
                    RevealChildren = new[] { "rapid_archer" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ModifyArcherFireRatePercent, Value = 0.12f } }
                },
                new TechNodeSeed
                {
                    Id = "rapid_archer", Title = "Rapid Archer",
                    Description = "Unlocks Rapid Archer recruitment.",
                    Cost = new ResourceCost(120, 0, 60, 0), MaxLevel = 1,
                    Prerequisites = new[] { "rapid_volley" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.UnlockArcherType, ArcherType = ArcherType.Rapid } }
                },
                new TechNodeSeed
                {
                    Id = "wood_camp", Title = "Wood Camp",
                    Description = "+20% wood production.",
                    Cost = new ResourceCost(50, 0, 0, 0), MaxLevel = 1,
                    Prerequisites = new[] { "castle_heart" },
                    RevealChildren = new[] { "worker_camp", "food_stores" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreaseResourceProductionPercent, Value = 0.20f, Resource = EconomyFocusType.Wood } }
                },
                new TechNodeSeed
                {
                    Id = "worker_camp", Title = "Worker Camp",
                    Description = "+6 worker cap on every resource.",
                    Cost = new ResourceCost(80, 40, 0, 0), MaxLevel = 1,
                    Prerequisites = new[] { "wood_camp" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreaseWorkerCap, Value = 6f, Resource = EconomyFocusType.Balanced } }
                },
                new TechNodeSeed
                {
                    Id = "food_stores", Title = "Food Stores",
                    Description = "+20% food production.",
                    Cost = new ResourceCost(60, 0, 0, 30), MaxLevel = 1,
                    Prerequisites = new[] { "wood_camp" },
                    RevealChildren = new[] { "population_growth" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreaseResourceProductionPercent, Value = 0.20f, Resource = EconomyFocusType.Food } }
                },
                new TechNodeSeed
                {
                    Id = "population_growth", Title = "Population Growth",
                    Description = "+5 population each siege cycle.",
                    Cost = new ResourceCost(40, 0, 0, 90), MaxLevel = 1,
                    Prerequisites = new[] { "food_stores" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreasePopulationGrowth, Value = 5f } }
                },
                new TechNodeSeed
                {
                    Id = "wall_reinforcement", Title = "Wall Reinforcement",
                    Description = "+15% defense max HP.",
                    Cost = new ResourceCost(0, 70, 0, 0), MaxLevel = 1,
                    Prerequisites = new[] { "castle_heart" },
                    RevealChildren = new[] { "repair_crew" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreaseDefenseMaxHpPercent, Value = 0.15f } }
                },
                new TechNodeSeed
                {
                    Id = "repair_crew", Title = "Repair Crew",
                    Description = "Dedicated crews thicken every rampart. +20% defense max HP.",
                    Cost = new ResourceCost(0, 90, 40, 0), MaxLevel = 1,
                    Prerequisites = new[] { "wall_reinforcement" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.IncreaseDefenseMaxHpPercent, Value = 0.20f } }
                },
                new TechNodeSeed
                {
                    Id = "frost_arrows", Title = "Frost Arrows",
                    Description = "Chilling arrowheads open the frost path.",
                    Cost = new ResourceCost(0, 60, 30, 0), MaxLevel = 1,
                    Prerequisites = new[] { "castle_heart" },
                    RevealChildren = new[] { "frost_archer" },
                    Effects = new TechNodeEffect[0]
                },
                new TechNodeSeed
                {
                    Id = "frost_archer", Title = "Frost Archer",
                    Description = "Unlocks Frost Archer recruitment.",
                    Cost = new ResourceCost(0, 110, 60, 0), MaxLevel = 1,
                    Prerequisites = new[] { "frost_arrows" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.UnlockArcherType, ArcherType = ArcherType.Frost } }
                },
                // Tekrarlanabilir sink node'lari: yuksek MaxLevel + seviye basina buyuyen maliyet.
                // Gec oyunda kaynaklarin her zaman gidecegi bir yer olur; tech tree tukenmez.
                new TechNodeSeed
                {
                    Id = "bow_mastery", Title = "Bow Mastery",
                    Description = "+6% archer damage per level. Endless drills.",
                    Cost = new ResourceCost(70, 0, 30, 0), MaxLevel = 20, CostGrowthPerLevel = 0.40f,
                    Prerequisites = new[] { "bow_training" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ModifyArcherDamagePercent, Value = 0.06f } }
                },
                new TechNodeSeed
                {
                    Id = "volley_mastery", Title = "Volley Mastery",
                    Description = "+5% archer fire rate per level.",
                    Cost = new ResourceCost(80, 0, 40, 0), MaxLevel = 20, CostGrowthPerLevel = 0.40f,
                    Prerequisites = new[] { "rapid_volley" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ModifyArcherFireRatePercent, Value = 0.05f } }
                },
                new TechNodeSeed
                {
                    Id = "repair_efficiency", Title = "Repair Efficiency",
                    Description = "-20% repair cost per level.",
                    Cost = new ResourceCost(0, 80, 50, 0), MaxLevel = 2,
                    Prerequisites = new[] { "repair_crew" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ReduceRepairCostPercent, Value = 0.20f } }
                },
                // Hendek evrimi (K4 Tek Cephe): cukur derinlesir, sonra alev/dikene evrilir
                new TechNodeSeed
                {
                    Id = "moat_dig", Title = "Deeper Moat",
                    Description = "Dig the moat deeper: +10% slow per level.",
                    Cost = new ResourceCost(40, 90, 0, 0), MaxLevel = 3, CostGrowthPerLevel = 0.35f,
                    Prerequisites = new[] { "wall_reinforcement" },
                    RevealChildren = new[] { "moat_flame" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.DeepenMoatSlowPercent, Value = 0.10f } }
                },
                new TechNodeSeed
                {
                    Id = "moat_flame", Title = "Burning Moat",
                    Description = "Oil and stakes: crossing the moat deals 4 dmg/s per level.",
                    Cost = new ResourceCost(60, 60, 70, 0), MaxLevel = 3, CostGrowthPerLevel = 0.45f,
                    Prerequisites = new[] { "moat_dig" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.AddMoatDamagePerSecond, Value = 4f } }
                },
            };
        }

        /// <summary>
        /// Reveal iliskisi merge'u: seed tablosundaki parent -> child baglantilarini MEVCUT parent
        /// asset'lerine ADDITIVE ekler (silme yok, sadece eksik ekleme — owner editleri korunur).
        /// Bu olmadan sonradan eklenen seed node'lari asla reveal edilmezdi (parent asset'ler
        /// diskte eski listeleriyle durur, EnsureTechNodeAsset mevcut asset'e dokunmaz).
        /// </summary>
        private static readonly (string parentId, string childId)[] TechRevealLinks =
        {
            ("bow_training", "bow_mastery"),
            ("rapid_volley", "volley_mastery"),
            ("repair_crew", "repair_efficiency"),
            ("wall_reinforcement", "moat_dig"),
            ("moat_dig", "moat_flame"),
        };

        private static void EnsureTechRevealLinks(TechTreeCatalogSO catalog)
        {
            foreach (var (parentId, childId) in TechRevealLinks)
            {
                var parent = catalog.GetNode(parentId);
                if (parent == null || catalog.GetNode(childId) == null)
                    continue;

                var children = parent.RevealChildNodeIds ?? Array.Empty<string>();
                if (Array.IndexOf(children, childId) >= 0)
                    continue;

                Undo.RecordObject(parent, "Add Tech Reveal Link");
                var merged = new List<string>(children) { childId };
                parent.RevealChildNodeIds = merged.ToArray();
                EditorUtility.SetDirty(parent);
            }
        }

        private static TechTreeCatalogSO EnsureDefaultTechTreeCatalog()
        {
            EnsureAssetFolder(TechTreeFolder);

            var seeds = GetDefaultTechNodeSeeds();
            var seedAssets = new List<TechNodeDefinitionSO>(seeds.Length);
            foreach (var seed in seeds)
                seedAssets.Add(EnsureTechNodeAsset(seed));

            var catalog = AssetDatabase.LoadAssetAtPath<TechTreeCatalogSO>(TechTreeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TechTreeCatalogSO>();
                catalog.RootNodeId = "castle_heart";
                AssetDatabase.CreateAsset(catalog, TechTreeCatalogPath);
            }

            var nodes = catalog.Nodes ?? Array.Empty<TechNodeDefinitionSO>();
            bool changed = false;
            var merged = new List<TechNodeDefinitionSO>(nodes.Length + seedAssets.Count);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !merged.Contains(nodes[i]))
                    merged.Add(nodes[i]);
            }

            foreach (var seedAsset in seedAssets)
            {
                if (seedAsset != null && !merged.Contains(seedAsset))
                {
                    merged.Add(seedAsset);
                    changed = true;
                }
            }

            if (changed || merged.Count != nodes.Length)
            {
                Undo.RecordObject(catalog, "Configure Tech Tree Catalog");
                catalog.Nodes = merged.ToArray();
                EditorUtility.SetDirty(catalog);
            }

            if (string.IsNullOrEmpty(catalog.RootNodeId))
            {
                Undo.RecordObject(catalog, "Configure Tech Tree Catalog Root");
                catalog.RootNodeId = "castle_heart";
                EditorUtility.SetDirty(catalog);
            }

            EnsureTechRevealLinks(catalog);

            var problems = catalog.ValidateCatalog();
            foreach (var problem in problems)
                Debug.LogWarning($"[MobileCastleSceneSetup] TechTreeCatalog: {problem}", catalog);

            return catalog;
        }

        // ---------------------------------------------------------------------------------
        // Council event seed (safak meclisi): atomlar + sablonlar merge-only kurulur.
        // Somut event asset'i YOKTUR — CouncilComposer runtime'da uretir.
        // ---------------------------------------------------------------------------------

        private static CouncilEventCatalogSO EnsureDefaultCouncilCatalog()
        {
            EnsureAssetFolder(CouncilFolder);

            var atoms = new List<CouncilEffectAtomSO>
            {
                EnsureCouncilAtom("gain_resource", CouncilEffectKind.GainResource, a =>
                {
                    a.MinutesOfProduction = 1.5f; a.BudgetMinutes = 1.5f;
                    a.ScarcityWeightMult = 3f; a.LabelFormat = "+{N} {RES}";
                }),
                EnsureCouncilAtom("gain_cache", CouncilEffectKind.GainResource, a =>
                {
                    a.MinutesOfProduction = 2.2f; a.BudgetMinutes = 2.2f;
                    a.ScarcityWeightMult = 2f;
                }),
                EnsureCouncilAtom("pay_resource", CouncilEffectKind.PayResource, a =>
                {
                    a.MinutesOfProduction = 1.2f; a.BudgetMinutes = 1.2f;
                    a.AbundanceWeightMult = 1.5f;
                }),
                EnsureCouncilAtom("boost_production", CouncilEffectKind.TempProductionBoost, a =>
                {
                    a.Rate = 0.25f; a.DurationDays = 2; a.BudgetMinutes = 2f;
                }),
                EnsureCouncilAtom("penalty_production", CouncilEffectKind.TempProductionPenalty, a =>
                {
                    a.Rate = 0.20f; a.DurationDays = 1; a.BudgetMinutes = 1.2f;
                }),
                EnsureCouncilAtom("gain_population", CouncilEffectKind.GainPopulation, a =>
                {
                    a.Rate = 6f; a.PerDay = 0.5f; a.BudgetMinutes = 2.2f;
                }),
                EnsureCouncilAtom("free_archers", CouncilEffectKind.GainFreeArchers, a =>
                {
                    a.Rate = 1.4f; a.PerDay = 0.1f; a.BudgetMinutes = 2.5f;
                    a.LowDefenseWeightMult = 2f;
                }),
                EnsureCouncilAtom("heal_defense", CouncilEffectKind.HealDefensePercent, a =>
                {
                    a.Rate = 0.20f; a.BudgetMinutes = 1.8f;
                    a.LowDefenseWeightMult = 3f;
                }),
                EnsureCouncilAtom("calm_night", CouncilEffectKind.NextNightSpawnDelta, a =>
                {
                    a.Rate = 0.25f; a.BudgetMinutes = 1.5f;
                }),
                EnsureCouncilAtom("wild_night", CouncilEffectKind.NextNightSpawnDelta, a =>
                {
                    a.Rate = 0.20f; a.BudgetMinutes = 1.5f; a.AbundanceWeightMult = 1.5f;
                }),
                EnsureCouncilAtom("cap_bonus", CouncilEffectKind.WorkerCapBonus, a =>
                {
                    a.Rate = 3f; a.PerDay = 0.15f; a.BudgetMinutes = 1.6f;
                }),
            };

            var templates = new List<CouncilTemplateSO>
            {
                EnsureCouncilTemplate("merchant_caravan", t =>
                {
                    t.Title = "MERCHANT CARAVAN";
                    t.BodyVariants = new[]
                    {
                        "A dust-caked caravan halts at the gate. The master looks over our stockpiles with a practiced eye. 'You're sitting on {PAY_RES} and starving for {GAIN_RES}. I can fix that — for a price.'",
                        "Traders out of the burned valley. Their wagons carry good {GAIN_RES} — and they know exactly how badly we need it.",
                    };
                    t.OutcomeA = "Hands are shaken. Their crew unloads {GAIN_N} {GAIN_RES} while ours counts out {PAY_N} {PAY_RES}. The caravan is over the ridge before dusk.";
                    t.OutcomeB = "The master shrugs — 'Your loss.' They leave {GAIN_N} {GAIN_RES} at the gate anyway. Goodwill, or an advertisement.";
                    t.Contrast = CouncilContrastType.ResourceTrade;
                    t.OptionAAtomIds = new[] { "pay_resource" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.OptionAVerb = "Make the trade"; t.OptionBVerb = "Send them away";
                    t.SetsFlagOnA = "traded_with_merchant";
                }),
                EnsureCouncilTemplate("abandoned_cache", t =>
                {
                    t.Title = "ABANDONED CACHE";
                    t.BodyVariants = new[]
                    {
                        "Scouts found a supply depot beyond the treeline — abandoned in a hurry, still intact. We can strip it in one run, or put a crew on it and work it properly.",
                        "An old army cache, untouched since the fall. One big haul now, or a steady trickle if we man it.",
                    };
                    t.OutcomeA = "The crews work fast and ugly. {GAIN_N} {GAIN_RES} reaches the stores by midday; the rest is left for the crows.";
                    t.OutcomeB = "A crew digs in at the depot. {BOOST_RES} output is up {BOOST_PCT}% for the next {BOOST_D} days.";
                    t.Contrast = CouncilContrastType.NowVsLater;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.OptionAVerb = "Strip it now"; t.OptionBVerb = "Work it properly";
                    t.BaseWeight = 1.2f;
                }),
                EnsureCouncilTemplate("refugees_at_gate", t =>
                {
                    t.Title = "REFUGEES AT THE GATE";
                    t.BodyVariants = new[]
                    {
                        "{POP_N} survivors at the gate — gaunt, scared, begging for walls between them and the dark. More mouths to feed. More hands to work.",
                        "A column of refugees followed the smoke to our walls. Shelter costs food — but people are the one thing the dead can't give us.",
                    };
                    t.OutcomeA = "The gate opens. {POP_N} souls file in — by evening they're hauling timber like they were born here.";
                    t.OutcomeB = "We keep the gate shut and pass rations over the wall. Their leader leaves {GAIN_N} {GAIN_RES} in thanks — salvage they couldn't carry.";
                    t.Contrast = CouncilContrastType.PopulationVsResource;
                    t.OptionAAtomIds = new[] { "gain_population" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.OptionAVerb = "Open the gate"; t.OptionBVerb = "Rations only";
                    t.SetsFlagOnA = "refugees_taken";
                }),
                EnsureCouncilTemplate("wandering_veterans", t =>
                {
                    t.Title = "WANDERING VETERANS";
                    t.BodyVariants = new[]
                    {
                        "{ARCHER_N} bowmen in patched leathers, longbows wrapped in oilcloth. 'Feed us and we'll hold your wall. Or we patch your stonework for directions south, and walk.'",
                        "Old soldiers, road-worn but steady-eyed. They'll fight for a full stomach — or fix our defenses and move on.",
                    };
                    t.OutcomeA = "They eat like wolves — {PAY_N} {PAY_RES} gone — then take the wall without being asked. {ARCHER_N} bows join the watch.";
                    t.OutcomeB = "They spend the day on the stonework and leave at dusk. Defenses patched up by {HEAL_PCT}%.";
                    t.Contrast = CouncilContrastType.EconomyVsDefense;
                    t.OptionAAtomIds = new[] { "free_archers" };
                    t.OptionBAtomIds = new[] { "heal_defense" };
                    t.OptionAVerb = "Feed them"; t.OptionBVerb = "Repairs for directions";
                    t.BaseWeight = 0.9f;
                }),
                EnsureCouncilTemplate("strange_bonfires", t =>
                {
                    t.Title = "STRANGE BONFIRES";
                    t.BodyVariants = new[]
                    {
                        "Fires on the horizon — the horde's staging ground, close enough to reach. We could burn their nests and buy a quiet night... or loot the camps first and risk waking them early.",
                        "Scouts mapped the bonfire camps. Torching them thins tonight's assault. Picking them clean first is worth a fortune — if we're fast enough.",
                    };
                    t.OutcomeA = "The camps go up in oily smoke. Whatever was gathering out there scatters — tonight should be {NIGHT_PCT}% quieter.";
                    t.OutcomeB = "The crews grab {GAIN_N} {GAIN_RES} and everything else worth carrying — but the noise travels. Tonight comes {NIGHT_PCT}% harder.";
                    t.Contrast = CouncilContrastType.SafeVsRisky;
                    t.OptionAAtomIds = new[] { "calm_night" };
                    t.OptionBAtomIds = new[] { "wild_night" };
                    t.OptionAVerb = "Burn it all"; t.OptionBVerb = "Loot first, then burn";
                    t.MinDay = 2; t.BaseWeight = 0.9f;
                }),
                EnsureCouncilTemplate("cold_snap", t =>
                {
                    t.Title = "COLD SNAP";
                    t.BodyVariants = new[]
                    {
                        "Frost crept into the storehouses overnight. The workers are blue-fingered and slow. Either we burn extra {PAY_RES} to keep them warm, or we grit through it.",
                        "A hard freeze. Tools crack, hands stiffen. Warmth costs {PAY_RES} we'd rather spend on the walls.",
                    };
                    t.OutcomeA = "The crews work short shifts around what fires we have. {PEN_RES} output down {PEN_PCT}% for {PEN_D} day(s).";
                    t.OutcomeB = "Braziers roar through the night — {PAY_N} {PAY_RES} up in smoke, but the crews keep their pace.";
                    t.Contrast = CouncilContrastType.PayOrSuffer;
                    t.OptionAAtomIds = new[] { "penalty_production" };
                    t.OptionBAtomIds = new[] { "pay_resource" };
                    t.OptionAVerb = "Grit through it"; t.OptionBVerb = "Burn extra fuel";
                    t.MinDay = 3; t.BaseWeight = 0.8f;
                }),
                EnsureCouncilTemplate("quarry_crew", t =>
                {
                    t.Title = "IDLE WORK CREW";
                    t.BodyVariants = new[]
                    {
                        "A master's work crew, idle since the roads closed, offers their backs. One brutal contract — or keep them on and expand the yards.",
                        "Skilled hands looking for work. They'll do one big job for cheap, or settle in for good if we make room.",
                    };
                    t.OutcomeA = "They work through the night and hand over {GAIN_N} {GAIN_RES}, then drift on down the road.";
                    t.OutcomeB = "They raise their own bunkhouse by the yards. {CAP_RES} crew capacity up by {CAP_N}.";
                    t.Contrast = CouncilContrastType.NowVsLater;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "cap_bonus" };
                    t.OptionAVerb = "One big job"; t.OptionBVerb = "Keep them on";
                    t.MinDay = 2;
                }),
                EnsureCouncilTemplate("among_the_refugees", t =>
                {
                    t.Title = "AMONG THE REFUGEES";
                    t.BodyVariants = new[]
                    {
                        "One of the newcomers turns out to be a guild craftsman — the kind cities used to fight over. He owes us for the open gate, and he knows it.",
                    };
                    t.OutcomeA = "He works a week's worth in a single day: {GAIN_N} {GAIN_RES}, guild-stamped. 'Debt paid,' he says — and means it.";
                    t.OutcomeB = "He hangs his tools by the {BOOST_RES} yards. Output up {BOOST_PCT}% for {BOOST_D} days.";
                    t.Contrast = CouncilContrastType.NowVsLater;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.OptionAVerb = "A parting gift"; t.OptionBVerb = "Set up his workshop";
                    t.RequiredFlags = new[] { "refugees_taken" };
                    t.ChainDelayDays = 2; t.OneShot = true; t.BaseWeight = 2f;
                }),
                EnsureCouncilTemplate("an_old_friend", t =>
                {
                    t.Title = "AN OLD FRIEND";
                    t.BodyVariants = new[]
                    {
                        "The caravan master is back, grinning through the road dust. 'For my favorite customer — one last haul, or a standing arrangement. Dealer's choice.'",
                    };
                    t.OutcomeA = "The wagons empty at our gate: {GAIN_N} {GAIN_RES}. He tips his hat. 'Pleasure as always.'";
                    t.OutcomeB = "Papers signed over a shared drink. His runners will keep the {BOOST_RES} flowing — up {BOOST_PCT}% for {BOOST_D} days.";
                    t.Contrast = CouncilContrastType.NowVsLater;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.OptionAVerb = "One last haul"; t.OptionBVerb = "A standing contract";
                    t.RequiredFlags = new[] { "traded_with_merchant" };
                    t.ChainDelayDays = 2; t.OneShot = true; t.BaseWeight = 1.5f;
                }),
            };

            var catalog = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(CouncilCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CouncilEventCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CouncilCatalogPath);
            }

            MergeCouncilList(catalog, atoms, templates);

            var problems = catalog.ValidateCatalog();
            foreach (var problem in problems)
                Debug.LogWarning($"[MobileCastleSceneSetup] CouncilCatalog: {problem}", catalog);

            return catalog;
        }

        private static void MergeCouncilList(CouncilEventCatalogSO catalog,
            List<CouncilEffectAtomSO> atoms, List<CouncilTemplateSO> templates)
        {
            var mergedAtoms = new List<CouncilEffectAtomSO>();
            if (catalog.Atoms != null)
            {
                foreach (var atom in catalog.Atoms)
                {
                    if (atom != null && !mergedAtoms.Contains(atom))
                        mergedAtoms.Add(atom);
                }
            }
            bool changed = false;
            foreach (var atom in atoms)
            {
                if (atom != null && !mergedAtoms.Contains(atom)) { mergedAtoms.Add(atom); changed = true; }
            }

            var mergedTemplates = new List<CouncilTemplateSO>();
            if (catalog.Templates != null)
            {
                foreach (var template in catalog.Templates)
                {
                    if (template != null && !mergedTemplates.Contains(template))
                        mergedTemplates.Add(template);
                }
            }
            foreach (var template in templates)
            {
                if (template != null && !mergedTemplates.Contains(template)) { mergedTemplates.Add(template); changed = true; }
            }

            if (changed)
            {
                Undo.RecordObject(catalog, "Configure Council Catalog");
                catalog.Atoms = mergedAtoms.ToArray();
                catalog.Templates = mergedTemplates.ToArray();
                EditorUtility.SetDirty(catalog);
            }
        }

        private static CouncilEffectAtomSO EnsureCouncilAtom(string id, CouncilEffectKind kind,
            Action<CouncilEffectAtomSO> configure)
        {
            string path = CouncilFolder + "/Atom_" + id + ".asset";
            var atom = AssetDatabase.LoadAssetAtPath<CouncilEffectAtomSO>(path);
            if (atom != null)
                return atom; // mevcut asset degerlerine dokunma

            atom = ScriptableObject.CreateInstance<CouncilEffectAtomSO>();
            atom.Id = id;
            atom.Kind = kind;
            configure?.Invoke(atom);
            AssetDatabase.CreateAsset(atom, path);
            EditorUtility.SetDirty(atom);
            return atom;
        }

        private static CouncilTemplateSO EnsureCouncilTemplate(string id, Action<CouncilTemplateSO> configure)
        {
            string path = CouncilFolder + "/Template_" + id + ".asset";
            var template = AssetDatabase.LoadAssetAtPath<CouncilTemplateSO>(path);
            if (template != null)
            {
                // METIN MIGRATION: anlati alanlari hic doldurulmamissa (BodyVariants bos)
                // seed'in guncel metinlerini uygula. Kullanici metin girdiyse dokunulmaz;
                // mekanik alanlara (contrast/atom/flag/weight) HICBIR kosulda dokunulmaz.
                if (template.BodyVariants == null || template.BodyVariants.Length == 0)
                {
                    var fresh = ScriptableObject.CreateInstance<CouncilTemplateSO>();
                    fresh.Id = id;
                    configure?.Invoke(fresh);
                    if (fresh.BodyVariants != null && fresh.BodyVariants.Length > 0)
                    {
                        Undo.RecordObject(template, "Update Council Template Texts");
                        template.Title = fresh.Title;
                        template.BodyVariants = fresh.BodyVariants;
                        template.OutcomeA = fresh.OutcomeA;
                        template.OutcomeB = fresh.OutcomeB;
                        template.OptionAVerb = fresh.OptionAVerb;
                        template.OptionBVerb = fresh.OptionBVerb;
                        EditorUtility.SetDirty(template);
                    }
                    UnityEngine.Object.DestroyImmediate(fresh);
                }

                return template;
            }

            template = ScriptableObject.CreateInstance<CouncilTemplateSO>();
            template.Id = id;
            configure?.Invoke(template);
            AssetDatabase.CreateAsset(template, path);
            EditorUtility.SetDirty(template);
            return template;
        }

        private static TechNodeDefinitionSO EnsureTechNodeAsset(TechNodeSeed seed)
        {
            string path = TechTreeFolder + "/" + seed.Title.Replace(" ", string.Empty) + ".asset";
            var node = AssetDatabase.LoadAssetAtPath<TechNodeDefinitionSO>(path);
            if (node != null)
                return node; // mevcut asset degerlerine dokunma (owner editleri korunur)

            node = ScriptableObject.CreateInstance<TechNodeDefinitionSO>();
            node.Id = seed.Id;
            node.Title = seed.Title;
            node.Description = seed.Description;
            node.Icon = null; // simdilik bilerek bos; UI bas-harf placeholder gosterir
            node.Cost = seed.Cost;
            node.MaxLevel = Mathf.Max(1, seed.MaxLevel);
            node.CostGrowthPerLevel = Mathf.Max(0f, seed.CostGrowthPerLevel);
            node.PrerequisiteNodeIds = seed.Prerequisites ?? new string[0];
            node.RevealChildNodeIds = seed.RevealChildren ?? new string[0];
            node.Effects = seed.Effects ?? new TechNodeEffect[0];
            AssetDatabase.CreateAsset(node, path);
            EditorUtility.SetDirty(node);
            return node;
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

            ApplyVillageMarkers(scene, root);
        }

        /// <summary>
        /// Boyama oturumunun biraktigi `VillageMarkers` sozlesmesini (gorsel = otorite) worker
        /// ekonomi rotalarina uygular: CastleKeepMarker -> CastleWorkerHub, {Wood,Stone,Iron,Food}
        /// SiteMarker -> ilgili site koku. Marker yoksa mevcut duzen aynen korunur (opsiyonel).
        /// Cocuk pickup/delivery noktalari parent'la birlikte tasinir; z korunur.
        /// </summary>
        private static void ApplyVillageMarkers(Scene scene, GameObject economyRoot)
        {
            GameObject markersRoot = FindRoot(scene, "VillageMarkers");
            if (markersRoot == null)
                return;

            ApplyMarkerPosition(markersRoot.transform, "CastleKeepMarker",
                FindDirectChild(economyRoot.transform, CastleInteriorWorkerPlacement.HubName));
            ApplyMarkerPosition(markersRoot.transform, "WoodSiteMarker",
                FindDirectChild(economyRoot.transform, "WoodSite"));
            ApplyMarkerPosition(markersRoot.transform, "StoneSiteMarker",
                FindDirectChild(economyRoot.transform, "StoneSite"));
            ApplyMarkerPosition(markersRoot.transform, "IronSiteMarker",
                FindDirectChild(economyRoot.transform, "IronSite"));
            ApplyMarkerPosition(markersRoot.transform, "FoodSiteMarker",
                FindDirectChild(economyRoot.transform, "FoodSite"));
        }

        private static void ApplyMarkerPosition(Transform markersRoot, string markerName, GameObject target)
        {
            if (target == null)
                return;

            Transform marker = markersRoot.Find(markerName);
            if (marker == null)
                return;

            var t = target.transform;
            Vector3 current = t.position;
            Vector3 desired = new Vector3(marker.position.x, marker.position.y, current.z);
            if ((current - desired).sqrMagnitude < 0.0001f)
                return;

            Undo.RecordObject(t, "Apply Village Marker");
            t.position = desired;
            EditorUtility.SetDirty(t);
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

        private static void BuildCanvasPanels(Transform canvasTransform, UIManager uiManager, ArcherRecruitmentCatalogSO archerCatalog)
        {
            EnsureDayNightOverlay(canvasTransform);

            GameObject hudRoot = EnsureHudRoot(canvasTransform);
            ConfigureHudRoot(hudRoot, archerCatalog);

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

        private static void ConfigureHudRoot(GameObject hudRoot, ArcherRecruitmentCatalogSO archerCatalog)
        {
            RemoveGeneratedCanvasComponents(hudRoot);
            // Silinmis script kalintilarini temizle (orn. eski CastleTechTreeUI missing-script'i)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(hudRoot);
            SetLayerRecursive(hudRoot, LayerMask.NameToLayer("UI"));
            Stretch(hudRoot.GetComponent<RectTransform>());

            var hud = EnsureComponent<HUDController>(hudRoot);
            bool hasCycleHud = FindChildByName(hudRoot, "CyclePanel") != null;
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
            hud.WaveText = hasCycleHud
                ? FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WaveText")
                : FindOrCreateText(hudRoot.transform, "WaveText", "WAVE 01", 34,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-170f, -56f), new Vector2(170f, -8f));
            hud.KillsText = hasCycleHud
                ? FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "KillsText")
                : FindOrCreateText(hudRoot.transform, "KillsText", "KILLS 0 / 30", 24,
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
            BindContinuousSiegeHudFields(hudRoot, hud);
            hud.CycleDayCounterText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CycleDayCounterText");
            if (hasCycleHud)
            {
                SetOptionalChildActive(hudRoot, "WaveText", false);
                SetOptionalChildActive(hudRoot, "KillsText", false);
            }
            hud.DamageFlashImage = EnsureDamageFlashOverlay(hudRoot.transform);
            DestroyChildIfExists(hudRoot.transform, "ArcherTypeText");
            hud.ArcherTypeText = null;
            HideEconomyFocus(hudRoot);
            ConfigureCastleEconomy(hudRoot);
            ConfigureWorkerEconomyDrawer(hudRoot);

            var market = EnsureComponent<MarketUI>(hudRoot);
            market.ArcherCatalog = archerCatalog;
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
            HidePlayerFacingArcherProgressionControls(market);

            ConfigureTechTree(hudRoot);
            ConfigureDefenseRepair(hudRoot);
            ConfigureDawnToast(hudRoot);
            ConfigureCouncilUI(hudRoot);
        }

        /// <summary>
        /// Safak meclisi kartinin binding'leri. Kart objeleri prefabdadir (HUD yeniden kurulumunda
        /// kaybolmaz); tool yalnizca bulur, baglar ve SFX clip'lerini atar. Katalog GameManager'da.
        /// </summary>
        private static void ConfigureCouncilUI(GameObject hudRoot)
        {
            var council = EnsureComponent<CouncilEventUI>(hudRoot);
            council.CouncilPanel = FindChildByName(hudRoot, "CouncilEventPanel");
            council.CouncilTitleText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilTitleText");
            council.CouncilBodyText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilBodyText");
            council.CouncilTimerFill = FindComponentInChildrenByName<Image>(hudRoot, "CouncilTimerFill");
            council.CouncilOptionAButton = FindComponentInChildrenByName<Button>(hudRoot, "CouncilOptionAButton");
            council.CouncilOptionAText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilOptionAText");
            council.CouncilOptionBButton = FindComponentInChildrenByName<Button>(hudRoot, "CouncilOptionBButton");
            council.CouncilOptionBText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilOptionBText");
            council.CouncilEffectBadgeText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilEffectBadgeText");
            council.NightToastText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "SiegeToastText");
            council.AppearClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CouncilAppearSfxPath);
            council.ChooseClip = AssetDatabase.LoadAssetAtPath<AudioClip>(CouncilChooseSfxPath);

            if (council.CouncilPanel != null)
                council.CouncilPanel.SetActive(false);
        }

        /// <summary>Player-facing REPAIR butonu (CastleDefensePanel) + kayip-orantili maliyet etiketi.</summary>
        private static void ConfigureDefenseRepair(GameObject hudRoot)
        {
            var repairUi = EnsureComponent<DefenseRepairUI>(hudRoot);
            repairUi.RepairButton = FindComponentInChildrenByName<Button>(hudRoot, "DefenseRepairButton");
            repairUi.RepairCostText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "DefenseRepairCostText");
        }

        /// <summary>DAWN odul toast'u: faz Dawn'a gecince gorunur nufus odulu bildirimi.</summary>
        private static void ConfigureDawnToast(GameObject hudRoot)
        {
            var dawnToast = EnsureComponent<DawnRewardToastUI>(hudRoot);
            var toast = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "SiegeToastText");
            if (toast == null)
            {
                toast = EnsureText(hudRoot.transform, "SiegeToastText", "DAWN", 18,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-220f, 224f), new Vector2(232f, 258f));
                toast.raycastTarget = false;
            }
            dawnToast.ToastText = toast;
        }

        /// <summary>
        /// TechTreeUI component'ini HUD root'a ekler ve prefabdaki Tech Tree objelerini isimle bulup
        /// baglar. Prefabda eksikse minimal fallback kurar (prefab yeniden kurulum senaryosu).
        /// Katalog referansi GameManager'dadir (TechCatalog); UI runtime'da oradan okur.
        /// </summary>
        private static void ConfigureTechTree(GameObject hudRoot)
        {
            var techTree = EnsureComponent<TechTreeUI>(hudRoot);

            var panel = FindChildByName(hudRoot, "TechTreePanel");
            if (panel == null)
                panel = EnsureFallbackTechTreePanel(hudRoot.transform);
            techTree.TechTreePanel = panel;

            techTree.TechTreeOpenButton = FindComponentInChildrenByName<Button>(hudRoot, "TechTreeOpenButton");
            if (techTree.TechTreeOpenButton == null)
            {
                techTree.TechTreeOpenButton = EnsureButton(hudRoot.transform, "TechTreeOpenButton",
                    new Vector2(0f, 1f), new Vector2(232f, -168f), new Vector2(358f, -130f), out _);
                SetButtonLabel(techTree.TechTreeOpenButton, "TECH");
            }

            techTree.TechTreeCloseButton = FindComponentInChildrenByName<Button>(hudRoot, "TechTreeCloseButton");
            techTree.TechTreeViewport = FindRectTransformByName(hudRoot, "TechTreeViewport");
            techTree.TechTreeContent = FindRectTransformByName(hudRoot, "TechTreeContent");
            techTree.TechNodeTemplate = FindRectTransformByName(hudRoot, "TechNodeTemplate");
            techTree.TechConnectionTemplate = FindRectTransformByName(hudRoot, "TechConnectionTemplate");

            if (techTree.TechNodeTemplate != null)
                techTree.TechNodeTemplate.gameObject.SetActive(false);
            if (techTree.TechConnectionTemplate != null)
                techTree.TechConnectionTemplate.gameObject.SetActive(false);

            // Gezinme: viewport'a pan/zoom controller'i (enum Auto -> platforma gore Desktop/Mobile);
            // ScrollRect tekerlegi zoom'a devredildigi icin kapatilir, elastic + inertia acilir.
            if (techTree.TechTreeViewport != null)
            {
                var scroll = techTree.TechTreeViewport.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    scroll.movementType = ScrollRect.MovementType.Elastic;
                    scroll.elasticity = 0.08f;
                    scroll.inertia = true;
                    scroll.decelerationRate = 0.15f;
                    scroll.scrollSensitivity = 0f;
                }
                EnsureComponent<TechTreeViewController>(techTree.TechTreeViewport.gameObject);
            }

            // Fade acilisi icin CanvasGroup
            if (techTree.TechTreePanel != null)
                EnsureComponent<CanvasGroup>(techTree.TechTreePanel);

            // Juice binding'leri: badge (TECH butonu nabzi) + toast + SFX clip'leri
            techTree.TechTreeOpenBadge = FindChildByName(hudRoot, "TechTreeBadge");
            if (techTree.TechTreeOpenBadge == null && techTree.TechTreeOpenButton != null)
            {
                var badgeGo = EnsureChild(techTree.TechTreeOpenButton.transform, "TechTreeBadge", true);
                var badgeRect = badgeGo.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.anchoredPosition = new Vector2(-4f, -2f);
                badgeRect.sizeDelta = new Vector2(14f, 14f);
                var badgeImage = EnsureComponent<Image>(badgeGo);
                badgeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                badgeImage.color = new Color(0.949f, 0.788f, 0.298f, 1f);
                badgeImage.raycastTarget = false;
                techTree.TechTreeOpenBadge = badgeGo;
            }
            if (techTree.TechTreeOpenBadge != null)
                techTree.TechTreeOpenBadge.SetActive(false);

            var toast = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "TechTreeToastText");
            if (toast == null)
            {
                toast = EnsureText(hudRoot.transform, "TechTreeToastText", "TECH UNLOCKED", 18,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-220f, 258f), new Vector2(232f, 292f));
                toast.raycastTarget = false;
            }
            techTree.ToastText = toast;

            techTree.BuyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechBuySfxPath);
            techTree.RevealClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechRevealSfxPath);
            techTree.DeniedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechDeniedSfxPath);
            techTree.PanelOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechPanelOpenSfxPath);

            // Panel default kapali; state sahibi TechTreeUI.
            if (techTree.TechTreePanel != null)
                techTree.TechTreePanel.SetActive(false);
        }

        /// <summary>
        /// Prefabda Tech Tree UI'si hic yoksa calisan minimal iskelet kurar:
        /// fullscreen panel + baslik + close + ScrollRect viewport/content + inactive node/connection template.
        /// Normal akista prefab bu objeleri zaten icerir, bu yol devreye girmez.
        /// </summary>
        private static GameObject EnsureFallbackTechTreePanel(Transform hudRoot)
        {
            var panelGo = EnsurePanel(hudRoot, "TechTreePanel", false, new Color(0.043f, 0.055f, 0.067f, 0.96f));
            var panelRect = panelGo.GetComponent<RectTransform>();
            Stretch(panelRect);

            EnsureText(panelGo.transform, "TechTreeTitleText", "TECH TREE", 26, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(420f, -14f));

            var closeButton = EnsureButton(panelGo.transform, "TechTreeCloseButton",
                new Vector2(1f, 1f), new Vector2(-148f, -58f), new Vector2(-20f, -16f), out _);
            SetButtonLabel(closeButton, "CLOSE");

            var viewportGo = EnsureChild(panelGo.transform, "TechTreeViewport", true);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            SetRect(viewportRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 16f), new Vector2(-16f, -72f));
            EnsureComponent<RectMask2D>(viewportGo);

            var contentGo = EnsureChild(viewportGo.transform, "TechTreeContent", true);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(2400f, 1400f);

            var scroll = EnsureComponent<ScrollRect>(viewportGo);
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            // Connection template: node'lardan once (altta cizilsin)
            var connectionGo = EnsureChild(contentGo.transform, "TechConnectionTemplate", true);
            var connectionRect = connectionGo.GetComponent<RectTransform>();
            connectionRect.sizeDelta = new Vector2(120f, 3f);
            var connectionImage = EnsureComponent<Image>(connectionGo);
            connectionImage.color = new Color(0.42f, 0.47f, 0.52f, 0.85f);
            connectionImage.raycastTarget = false;
            connectionGo.SetActive(false);

            var nodeGo = EnsureChild(contentGo.transform, "TechNodeTemplate", true);
            var nodeRect = nodeGo.GetComponent<RectTransform>();
            nodeRect.sizeDelta = new Vector2(230f, 112f);
            var nodeImage = EnsureComponent<Image>(nodeGo);
            nodeImage.color = new Color(0.137f, 0.165f, 0.196f, 0.95f);

            var iconGo = EnsureChild(nodeGo.transform, "TechNodeIconImage", true);
            var iconRect = iconGo.GetComponent<RectTransform>();
            SetRect(iconRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -42f), new Vector2(42f, -10f));
            var iconImage = EnsureComponent<Image>(iconGo);
            iconImage.raycastTarget = false;
            EnsureText(iconGo.transform, "TechNodeIconFallbackText", "?", 13, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            EnsureText(nodeGo.transform, "TechNodeTitleText", "Tech Node", 14, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(50f, -34f), new Vector2(-64f, -8f));
            EnsureText(nodeGo.transform, "TechNodeLevelText", "LV 1", 10, TextAlignmentOptions.Right,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-62f, -30f), new Vector2(-8f, -10f));
            EnsureText(nodeGo.transform, "TechNodeDescriptionText", "Description", 9, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 34f), new Vector2(-84f, -42f));
            EnsureText(nodeGo.transform, "TechNodeCostText", "COST", 10, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.55f, 0f), new Vector2(12f, 8f), new Vector2(0f, 30f));
            EnsureText(nodeGo.transform, "TechNodeStatusText", "LOCKED", 10, TextAlignmentOptions.Center,
                new Vector2(0.38f, 0f), new Vector2(0.64f, 0f), new Vector2(0f, 8f), new Vector2(0f, 30f));

            var buyButton = EnsureButton(nodeGo.transform, "TechNodeBuyButton",
                new Vector2(1f, 0f), new Vector2(-78f, 8f), new Vector2(-8f, 46f), out var buyLabel);
            if (buyLabel != null)
            {
                buyLabel.gameObject.name = "TechNodeBuyButtonText";
                buyLabel.text = "BUY";
                buyLabel.fontSize = 12;
            }

            nodeGo.SetActive(false);
            return panelGo;
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

        private static void BindContinuousSiegeHudFields(GameObject hudRoot, HUDController hud)
        {
            hud.CyclePanel = FindChildByName(hudRoot, "CyclePanel");
            hud.CyclePhaseText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CyclePhaseText");
            hud.CycleDayLabelText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CycleDayLabelText");
            hud.CycleDuskLabelText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CycleDuskLabelText");
            hud.CycleNightLabelText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CycleNightLabelText");
            hud.CycleProgressFill = FindComponentInChildrenByName<Image>(hudRoot, "CycleProgressFill");
            hud.CycleProgressMarker = FindRectTransformByName(hudRoot, "CycleProgressMarker");

            hud.HordePressurePanel = FindChildByName(hudRoot, "HordePressurePanel");
            hud.HordePressureTitleText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HordePressureTitleText");
            hud.HordePressureFill = FindComponentInChildrenByName<Image>(hudRoot, "HordePressureFill");
            hud.HordePressureText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HordePressureText");
            hud.HordePressureLevelText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HordePressureLevelText");

            ConfigureCycleProgressLayout(hud.CycleProgressFill, hud.CycleProgressMarker);
            if (hud.HordePressurePanel != null)
                hud.HordePressurePanel.SetActive(false);

            if (hud.CyclePhaseText != null)
                hud.CyclePhaseText.text = "DAY";
            if (hud.CycleDayLabelText != null)
                hud.CycleDayLabelText.text = "DAY";
            if (hud.CycleDuskLabelText != null)
                hud.CycleDuskLabelText.text = "DUSK";
            if (hud.CycleNightLabelText != null)
                hud.CycleNightLabelText.text = "NIGHT";
            if (hud.HordePressureTitleText != null)
                hud.HordePressureTitleText.text = "HORDE PRESSURE";
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

        private static void ConfigureCycleProgressLayout(Image fill, RectTransform marker)
        {
            if (fill != null)
            {
                fill.type = Image.Type.Simple;
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0.5f);
                fillRect.anchorMax = new Vector2(0f, 0.5f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = new Vector2(0f, fillRect.anchoredPosition.y);
                fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
            }

            if (marker == null)
                return;

            marker.anchorMin = new Vector2(0f, 0.5f);
            marker.anchorMax = new Vector2(0f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0.5f);
            marker.anchoredPosition = new Vector2(0f, marker.anchoredPosition.y);
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
            DestroyChildIfExists(hudRoot.transform, "EconomyFocusPanel");
            DestroyChildIfExists(hudRoot.transform, "EconomyFocusText");
            DestroyChildIfExists(hudRoot.transform, "EconomyBalancedButton");
            DestroyChildIfExists(hudRoot.transform, "EconomyWoodButton");
            DestroyChildIfExists(hudRoot.transform, "EconomyStoneButton");
            DestroyChildIfExists(hudRoot.transform, "EconomyIronButton");
            DestroyChildIfExists(hudRoot.transform, "EconomyFoodButton");
            DestroyChildIfExists(hudRoot.transform, "EconomyBalancedSelected");
            DestroyChildIfExists(hudRoot.transform, "EconomyWoodSelected");
            DestroyChildIfExists(hudRoot.transform, "EconomyStoneSelected");
            DestroyChildIfExists(hudRoot.transform, "EconomyIronSelected");
            DestroyChildIfExists(hudRoot.transform, "EconomyFoodSelected");
        }

        private static void ConfigureCastleEconomy(GameObject hudRoot)
        {
            var castleEconomy = EnsureComponent<CastleEconomyUI>(hudRoot);
            castleEconomy.PlayerFacingPanelEnabled = false;
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

        private static void ConfigureWorkerEconomyDrawer(GameObject hudRoot)
        {
            var workerDrawer = EnsureComponent<WorkerEconomyDrawerUI>(hudRoot);
            workerDrawer.WorkerDrawerToggleButton = FindComponentInChildrenByName<Button>(hudRoot, "WorkerDrawerToggleButton");
            workerDrawer.WorkerEconomyDrawerPanel = FindChildByName(hudRoot, "WorkerEconomyDrawerPanel");
            workerDrawer.WorkerDrawerTitleText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WorkerDrawerTitleText");
            workerDrawer.WorkerIdlePopulationText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WorkerIdlePopulationText");
            workerDrawer.WorkerTotalText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WorkerTotalText");
            workerDrawer.WorkerArcherPopulationText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WorkerArcherPopulationText");

            workerDrawer.WoodWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerCountText");
            workerDrawer.WoodWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerRateText");
            workerDrawer.WoodWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "WoodWorkerAddButton");
            workerDrawer.WoodWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerStatusText");

            workerDrawer.StoneWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerCountText");
            workerDrawer.StoneWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerRateText");
            workerDrawer.StoneWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "StoneWorkerAddButton");
            workerDrawer.StoneWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerStatusText");

            workerDrawer.IronWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerCountText");
            workerDrawer.IronWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerRateText");
            workerDrawer.IronWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "IronWorkerAddButton");
            workerDrawer.IronWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerStatusText");

            workerDrawer.FoodWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerCountText");
            workerDrawer.FoodWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerRateText");
            workerDrawer.FoodWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "FoodWorkerAddButton");
            workerDrawer.FoodWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerStatusText");

            if (workerDrawer.WorkerDrawerToggleButton != null)
                workerDrawer.WorkerDrawerToggleButton.gameObject.SetActive(true);
            if (workerDrawer.WorkerEconomyDrawerPanel != null)
                workerDrawer.WorkerEconomyDrawerPanel.SetActive(false);

            EditorUtility.SetDirty(workerDrawer);
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
                new Vector2(1f, 0f), new Vector2(-128f, 56f), new Vector2(-8f, 104f), out _);
            SetButtonLabel(buy, "Buy");
        }

        private static void EnsureFallbackTechAndPrep(RectTransform drawer)
        {
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
            market.DrawerTitleText = FindComponentInChildrenByName<TextMeshProUGUI>(root, "DrawerTitleText");
            if (market.DrawerTitleText != null)
                market.DrawerTitleText.text = "ARCHER RECRUITMENT";

            market.ArcherRecruitmentListRoot = FindRectTransformByName(root, "ArcherRecruitmentListRoot");
            market.ArcherRecruitmentRowTemplate = FindRectTransformByName(root, "ArcherRecruitmentRowTemplate");
            if (market.ArcherRecruitmentRowTemplate != null)
                market.ArcherRecruitmentRowTemplate.gameObject.SetActive(false);

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

            market.ArrowTechPanel = FindChildByName(root, "ArrowTechPanel");
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

        private static void HidePlayerFacingArcherProgressionControls(MarketUI market)
        {
            HideButton(market.BasicUpgradeButton);
            HideButton(market.RapidUpgradeButton);
            HideButton(market.FrostUpgradeButton);
            HideButton(market.RapidTechUnlockButton);
            HideButton(market.FrostTechUnlockButton);

            if (market.ArrowTechPanel != null)
                market.ArrowTechPanel.SetActive(false);

            SetOptionalChildActive(market.gameObject, "RapidTechCard", false);
            SetOptionalChildActive(market.gameObject, "FrostTechCard", false);
            SetOptionalChildActive(market.gameObject, "ArrowTechTitleText", false);
            SetOptionalChildActive(market.gameObject, "ArrowTechHintText", false);
        }

        private static void HideButton(Button button)
        {
            if (button == null)
                return;

            button.interactable = false;
            button.gameObject.SetActive(false);
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

        private static void AssignObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (target == null)
                return;

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                return;

            if (property.objectReferenceValue == value)
                return;

            Undo.RecordObject(target, "Assign Object Reference");
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
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
