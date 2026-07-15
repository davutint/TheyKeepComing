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
        private const string EnemyDefinitionFolder = "Assets/ScriptableObject/MobileCastle/Enemies";
        private const string BasicZombieDefinitionPath = EnemyDefinitionFolder + "/BasicZombie.asset";
        private const string EnemyCatalogPath = EnemyDefinitionFolder + "/EnemyCatalog.asset";
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
        private const string ArcherFormationDefinitionPath = ArcherDefinitionFolder + "/ArcherFormationV1.asset";
        private const string TechTreeFolder = "Assets/ScriptableObject/MobileCastle/TechTree";
        private const string TechTreeCatalogPath = TechTreeFolder + "/TechTreeCatalog.asset";
        private const string MetaFolder = "Assets/ScriptableObject/MobileCastle/Meta";
        private const string MetaCatalogPath = MetaFolder + "/MetaUpgradeCatalog.asset";
        private const string DifficultyFolder = "Assets/ScriptableObject/MobileCastle/Difficulty";
        public const string DifficultyProfilePath = DifficultyFolder + "/DefaultDifficulty.asset";
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

        [MenuItem("Window/DeadWalls/Repair Worker Drawer Target Controls")]
        public static void RepairWorkerDrawerTargetControls()
        {
            EnsureWorkerDrawerTargetControlsInPrefab();
            AssetDatabase.ImportAsset(GeneratedHudPrefabPath, ImportAssetOptions.ForceUpdate);

            Scene activeScene = SceneManager.GetActiveScene();
            bool sceneRepaired = false;
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    var workerDrawer = root.GetComponentInChildren<WorkerEconomyDrawerUI>(true);
                    if (workerDrawer == null)
                        continue;

                    EnsureWorkerDrawerTargetControls(workerDrawer.gameObject);
                    ConfigureWorkerEconomyDrawer(workerDrawer.gameObject);
                    sceneRepaired = true;
                }

                if (sceneRepaired)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            Debug.Log(sceneRepaired
                ? "[MobileCastleSceneSetup] Worker drawer target + building upgrade controls prefab ve sahnede onarildi."
                : "[MobileCastleSceneSetup] Worker drawer controls prefabda onarildi; NewGameScene aktif olmadigi icin sahne degismedi.");
        }

        [MenuItem("Window/DeadWalls/Repair Archer Retrain Control")]
        public static void RepairArcherRetrainControl()
        {
            EnsureArcherRetrainControlInPrefab();
            AssetDatabase.ImportAsset(GeneratedHudPrefabPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[MobileCastleSceneSetup] Archer retrain kontrolu HUD prefabinda onarildi.");
        }

        [MenuItem("Window/DeadWalls/Repair Finite Arrow Ammo Panel")]
        public static void RepairFiniteArrowAmmoPanel()
        {
            EnsureArrowAmmoPanelInPrefab();
            AssetDatabase.ImportAsset(GeneratedHudPrefabPath, ImportAssetOptions.ForceUpdate);

            Scene activeScene = SceneManager.GetActiveScene();
            bool sceneRepaired = false;
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    var market = root.GetComponentInChildren<MarketUI>(true);
                    if (market == null)
                        continue;

                    EnsureArrowAmmoPanel(market.gameObject);
                    ConfigureArrowAmmo(market.gameObject);
                    sceneRepaired = true;
                }

                if (sceneRepaired)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(sceneRepaired
                ? "[MobileCastleSceneSetup] Finite Arrow ammo paneli prefab ve NewGameScene'de onarildi."
                : "[MobileCastleSceneSetup] Finite Arrow ammo paneli prefabda onarildi; NewGameScene aktif degildi.");
        }

        [MenuItem("Window/DeadWalls/Repair Council Exact Decision UI")]
        public static void RepairCouncilExactDecisionUI()
        {
            EnsureCouncilDecisionUIInPrefab();
            AssetDatabase.ImportAsset(GeneratedHudPrefabPath, ImportAssetOptions.ForceUpdate);

            Scene activeScene = SceneManager.GetActiveScene();
            bool sceneRepaired = false;
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    CouncilEventUI council = root.GetComponentInChildren<CouncilEventUI>(true);
                    if (council == null)
                        continue;

                    ConfigureCouncilUI(council.gameObject);
                    sceneRepaired = true;
                }

                if (sceneRepaired)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(sceneRepaired
                ? "[MobileCastleSceneSetup] Council exact karar ozeti ve timer prefab/sahne binding'i onarildi."
                : "[MobileCastleSceneSetup] Council exact karar UI prefabda onarildi; NewGameScene aktif degildi.");
        }

        [MenuItem("Window/DeadWalls/Repair Unified Ability Bar")]
        public static void RepairUnifiedAbilityBar()
        {
            EnsureUnifiedAbilityBarInPrefab();
            EnsureActiveAbilityTuning();
            AssetDatabase.ImportAsset(GeneratedHudPrefabPath, ImportAssetOptions.ForceUpdate);

            Scene activeScene = SceneManager.GetActiveScene();
            bool sceneRepaired = false;
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                GameObject hudRoot = null;
                foreach (GameObject root in activeScene.GetRootGameObjects())
                {
                    HUDController hud = root.GetComponentInChildren<HUDController>(true);
                    if (hud != null)
                    {
                        hudRoot = hud.gameObject;
                        break;
                    }
                }

                if (hudRoot != null)
                {
                    ConfigureUnifiedAbilityBar(hudRoot);
                    foreach (GameObject root in activeScene.GetRootGameObjects())
                    {
                        var controllers = root.GetComponentsInChildren<SpellCastUI>(true);
                        foreach (SpellCastUI controller in controllers)
                        {
                            if (controller != null && controller.gameObject != hudRoot)
                                Undo.DestroyObjectImmediate(controller.gameObject);
                        }
                    }

                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                    sceneRepaired = true;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(sceneRepaired
                ? "[MobileCastleSceneSetup] Unified ability bar prefab ve NewGameScene'de onarildi."
                : "[MobileCastleSceneSetup] Unified ability bar prefabda onarildi; NewGameScene aktif degildi.");
        }

        [MenuItem("Window/DeadWalls/Repair Council Curated Context Contract")]
        public static void RepairCouncilCuratedContextContract()
        {
            EnsureDefaultCouncilCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CouncilCatalogPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[MobileCastleSceneSetup] Council context memory ve curated chain contract'i onarildi.");
        }

        [MenuItem("Window/DeadWalls/Repair Archer Formation V1")]
        public static void RepairArcherFormationV1()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != TargetScenePath)
                throw new InvalidOperationException("Archer formation repair icin NewGameScene aktif olmali.");

            ArcherFormationDefinitionSO definition = EnsureDefaultArcherFormationDefinition();
            EnsureArcherTilePlacement(scene, definition);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MobileCastleSceneSetup] 40x25 Archer Formation V1 asset ve scene binding onarildi.");
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
            var enemyCatalog = EnsureDefaultEnemyCatalog(zombiePrefab);
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
            gameStateAuthoring.InitialCapacity = MobileBedCapacityUtility.DefaultInitialCapacity;
            gameStateAuthoring.TestWorkers = 53;
            gameStateAuthoring.TestArchers = 4;
            gameStateAuthoring.FoodPerAssignedPerMin = 0.25f;
            gameStateAuthoring.InitialArrows = 200;

            var waveConfig = EnsureComponent<WaveConfigAuthoring>(gameState);
            waveConfig.EnemyCatalog = enemyCatalog;
            waveConfig.ZombiePrefab = zombiePrefab;
            waveConfig.ArrowPrefab = arrowPrefab;
            waveConfig.ArcherPrefab = archerPrefab;
            waveConfig.WorkerPrefab = workerPrefab;

            var castle = EnsureSceneRoot(subScene, "CastleCore");
            castle.transform.position = Vector3.zero;
            var castleAuthoring = EnsureComponent<CastleAuthoring>(castle);
            castleAuthoring.WallHP = 350f; // M-A balance: erken geceler tek atista yikmasin (200 -> 350)
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
            mobileAuthoring.EnemyCatalog = enemyCatalog;
            mobileAuthoring.CastleCenter = Vector2.zero;
            mobileAuthoring.SpawnRadius = 11f;
            mobileAuthoring.AttackRadius = 1.35f;
            mobileAuthoring.SpawnLineX = 27f; // K4: max 2.4 aspect'te zombi dogumu ekran disinda.
            mobileAuthoring.BaseWaveEnemyCount = 30;
            mobileAuthoring.ExtraEnemiesPerWave = 10;
            mobileAuthoring.SpawnBatchSize = 2;
            mobileAuthoring.ZombieBaseHP = 20f;
            mobileAuthoring.ZombieHpGrowthPerCycle = 0f;
            mobileAuthoring.ZombieBaseDamage = 5f;
            mobileAuthoring.ZombieDamagePerCycle = 0f;
            mobileAuthoring.SpawnBatchGrowthPerCycle = 0.10f;
            mobileAuthoring.MaxSpawnBatch = 12;
            mobileAuthoring.MaxAliveZombies = 900;
            mobileAuthoring.ZombieScale = 1.4f;
            mobileAuthoring.BaseZombieSpeed = 0.85f;
            mobileAuthoring.ZombieSpeedPerWave = 0f;
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
            mobileAuthoring.ContinuousSiegeEnabled = true;
            mobileAuthoring.SiegeCycleDuration = 60f;
            mobileAuthoring.SiegeDayDuration = 30f;
            mobileAuthoring.SiegeDuskDuration = 5f;
            mobileAuthoring.SiegeNightDuration = 20f;
            mobileAuthoring.SiegeDawnDuration = 5f;
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
            mobileAuthoring.FoodCostPerArrival = MobilePopulationArrivalUtility.DefaultFoodCostPerArrival;
            mobileAuthoring.InitialBedCapacity = MobileBedCapacityUtility.DefaultInitialCapacity;
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
            mobileAuthoring.IronWorkerProductionPerMin = 4.9f; // M-A balance: iron darbogazi (+%30)
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

        private static EnemyCatalogSO EnsureDefaultEnemyCatalog(GameObject zombiePrefab)
        {
            EnsureAssetFolder(EnemyDefinitionFolder);

            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(BasicZombieDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                definition.Id = "zombie_basic";
                definition.Prefab = zombiePrefab;
                definition.BaseHP = 20f;
                definition.BaseDamage = 5f;
                definition.BaseMoveSpeed = 0.85f;
                definition.Scale = 1.4f;
                definition.XPReward = 10;
                definition.SpawnWeight = 1f;
                definition.PoolPrewarm = 128;
                definition.PoolExpandBatch = 128;
                AssetDatabase.CreateAsset(definition, BasicZombieDefinitionPath);
                EditorUtility.SetDirty(definition);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalogSO>(EnemyCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EnemyCatalogSO>();
                AssetDatabase.CreateAsset(catalog, EnemyCatalogPath);
            }

            if (catalog.ActiveEnemyId != definition.Id
                || catalog.Definitions == null
                || catalog.Definitions.Length != 1
                || catalog.Definitions[0] != definition)
            {
                Undo.RecordObject(catalog, "Configure V1 Enemy Catalog");
                catalog.ActiveEnemyId = definition.Id;
                catalog.Definitions = new[] { definition };
                EditorUtility.SetDirty(catalog);
            }

            return catalog;
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

            // K4 boyanmis dunya korumasi (2026-07-07): kok 'Grid' haritasi varsa fallback 360 arena
            // BOYANMAZ — koy/kale/duvar/hendek boyamasi ezilmesin (bkz. STRUCTURE_SPRITE_BAKER_CAPABILITIES.md).
            bool hasPaintedWorld = GameObject.Find("Grid") != null;
            if (!hasPaintedWorld)
            {
                PaintArenaGround(ground, castleGround);
                PaintCastle(castleGround, castleWall, castleProps);
            }

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

            // Sigorta: Canvas kazara kapali kaydedilmisse oyun UI'siz kalir (bir kez yasandi)
            if (!canvasObject.activeSelf)
                canvasObject.SetActive(true);

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
            ArcherFormationDefinitionSO archerFormation = EnsureDefaultArcherFormationDefinition();
            TechTreeCatalogSO techCatalog = EnsureDefaultTechTreeCatalog();
            CouncilEventCatalogSO councilEventCatalog = EnsureDefaultCouncilCatalog();
            AssignDifficultyProfileToAuthoring(EnsureDefaultDifficultyProfile());
            GameObject gameManagerObject = FindRoot(scene, "GameManager");
            if (gameManagerObject == null)
            {
                gameManagerObject = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(gameManagerObject, "Create GameManager");
                SceneManager.MoveGameObjectToScene(gameManagerObject, scene);
            }
            var gameManager = EnsureComponent<GameManager>(gameManagerObject);
            EnsureComponent<RunBootstrap>(gameManagerObject); // menuden gelen Continue/NewRun'i uygular
            AssignObjectReference(gameManager, "archerCatalog", archerCatalog);
            AssignObjectReference(gameManager, "techTreeCatalog", techCatalog);
            AssignObjectReference(gameManager, "councilCatalog", councilEventCatalog);
            AssignObjectReference(gameManager, "metaUpgradeCatalog", EnsureDefaultMetaUpgradeCatalog());

            var uiManager = EnsureComponent<UIManager>(canvas.gameObject);
            BuildCanvasPanels(canvas.transform, uiManager, archerCatalog);
            EnsureCastleClickTarget(scene);
            EnsureArcherTilePlacement(scene, archerFormation);
            EnsureCombatFeedbackRoot(scene);
            EnsureCameraShaker(scene);
            ConfigureMenuSystem(canvas.transform);
            EnsureDamageFlash(canvas.transform); // flash her seyin (menu dahil) ustunde kalir
            ApplyGameUiSkin(canvas.transform); // Polish 2: menu dili oyun ici panellere
            EnsureAmbientAudio(scene);
            EnsureMainMenuScene(); // ayri menu sahnesi (additive kur/kaydet) + Build Settings
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

        private static ArcherFormationDefinitionSO EnsureDefaultArcherFormationDefinition()
        {
            EnsureAssetFolder(ArcherDefinitionFolder);
            var definition = AssetDatabase.LoadAssetAtPath<ArcherFormationDefinitionSO>(
                ArcherFormationDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<ArcherFormationDefinitionSO>();
                definition.ApplyV1Defaults();
                AssetDatabase.CreateAsset(definition, ArcherFormationDefinitionPath);
                EditorUtility.SetDirty(definition);
            }

            if (!definition.ValidateV1(out string problem))
                throw new InvalidOperationException("ArcherFormationV1 asset gecersiz: " + problem);

            return definition;
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
                // ---- Buyuculuk dali (M-C): oyuncunun aktif savas gucu ----
                new TechNodeSeed
                {
                    Id = "arcane_tower", Title = "Arcane Tower",
                    Description = "Unlock the Fireball spell: blast an area of your choosing.",
                    Cost = new ResourceCost(100, 0, 80, 0), MaxLevel = 1, CostGrowthPerLevel = 0f,
                    Prerequisites = new[] { "castle_heart" },
                    RevealChildren = new[] { "fire_power", "fire_radius", "fire_cooldown" },
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.UnlockSpellcasting } }
                },
                new TechNodeSeed
                {
                    Id = "fire_power", Title = "Searing Flames",
                    Description = "+20% fireball damage per level.",
                    Cost = new ResourceCost(60, 0, 50, 0), MaxLevel = 5, CostGrowthPerLevel = 0.5f,
                    Prerequisites = new[] { "arcane_tower" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ModifySpellDamagePercent, Value = 0.20f } }
                },
                new TechNodeSeed
                {
                    Id = "fire_radius", Title = "Greater Blast",
                    Description = "+0.4 fireball blast radius per level.",
                    Cost = new ResourceCost(70, 0, 60, 0), MaxLevel = 3, CostGrowthPerLevel = 0.6f,
                    Prerequisites = new[] { "arcane_tower" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.AddSpellRadius, Value = 0.4f } }
                },
                new TechNodeSeed
                {
                    Id = "fire_cooldown", Title = "Arcane Focus",
                    Description = "-10% fireball cooldown per level.",
                    Cost = new ResourceCost(50, 0, 60, 0), MaxLevel = 5, CostGrowthPerLevel = 0.5f,
                    Prerequisites = new[] { "arcane_tower" },
                    RevealChildren = new string[0],
                    Effects = new[] { new TechNodeEffect { Type = TechNodeEffectType.ReduceSpellCooldownPercent, Value = 0.10f } }
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
            // castle_heart asset'i diskte mevcut — buyuculuk dalinin reveal'i ancak link merge ile acilir
            ("castle_heart", "arcane_tower"),
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
                if (nodes[i] != null
                    && !MoatDormancyRules.IsDormantTechNodeId(nodes[i].Id)
                    && !merged.Contains(nodes[i]))
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

        // ---------------------------------------------------------------------------------
        // Blueprint v1.0 sabit meta upgrade katalogu. Tuning assetlerde kalir; bu arac yalniz
        // eksik default assetleri olusturur ve katalog sirasini authoritative listeye ceker.
        // ---------------------------------------------------------------------------------

        private static MetaUpgradeCatalogSO EnsureDefaultMetaUpgradeCatalog()
        {
            EnsureAssetFolder(MetaFolder);

            var upgrades = new List<MetaUpgradeSO>
            {
                EnsureMetaUpgrade("start_wood", u =>
                {
                    u.Title = "Starting Wood";
                    u.Description = "Start each run with extra wood.";
                    u.EffectType = MetaUpgradeEffectType.StartingResource;
                    u.Resource = EconomyFocusType.Wood;
                    u.ValuePerLevel = 75f; u.MaxLevel = 0; u.BaseCost = 150; u.CostGrowthPerLevel = 0.6f;
                }),
                EnsureMetaUpgrade("start_stone", u =>
                {
                    u.Title = "Starting Stone";
                    u.Description = "Start each run with extra stone.";
                    u.EffectType = MetaUpgradeEffectType.StartingResource;
                    u.Resource = EconomyFocusType.Stone;
                    u.ValuePerLevel = 50f; u.MaxLevel = 0; u.BaseCost = 175; u.CostGrowthPerLevel = 0.65f;
                }),
                EnsureMetaUpgrade("start_iron", u =>
                {
                    u.Title = "Starting Iron";
                    u.Description = "Start each run with extra iron.";
                    u.EffectType = MetaUpgradeEffectType.StartingResource;
                    u.Resource = EconomyFocusType.Iron;
                    u.ValuePerLevel = 30f; u.MaxLevel = 0; u.BaseCost = 225; u.CostGrowthPerLevel = 0.7f;
                }),
                EnsureMetaUpgrade("start_food", u =>
                {
                    u.Title = "Starting Food";
                    u.Description = "Start each run with extra food.";
                    u.EffectType = MetaUpgradeEffectType.StartingResource;
                    u.Resource = EconomyFocusType.Food;
                    u.ValuePerLevel = 60f; u.MaxLevel = 0; u.BaseCost = 150; u.CostGrowthPerLevel = 0.6f;
                }),
                EnsureMetaUpgrade("start_archers", u =>
                {
                    u.Title = "Starting Basic Archers";
                    u.Description = "Start each run with extra Basic Archers. Does not unlock other types.";
                    u.EffectType = MetaUpgradeEffectType.StartingArchers;
                    u.ValuePerLevel = 1f; u.MaxLevel = ArcherCapacityUtility.MaxTotalArchers;
                    u.BaseCost = 400; u.CostGrowthPerLevel = 1.0f;
                }),
                EnsureMetaUpgrade("start_beds", u =>
                {
                    u.Title = "Starting Beds";
                    u.Description = "Start each run with extra beds. Run bed costs still grow.";
                    u.EffectType = MetaUpgradeEffectType.StartingBeds;
                    u.ValuePerLevel = 5f; u.MaxLevel = 0; u.BaseCost = 250; u.CostGrowthPerLevel = 0.75f;
                }),
                EnsureMetaUpgrade("wall_hp", u =>
                {
                    u.Title = "Base Wall HP";
                    u.Description = "+5% Wall max HP per level.";
                    u.EffectType = MetaUpgradeEffectType.WallHpPercent;
                    u.ValuePerLevel = 0.05f; u.MaxLevel = 5; u.BaseCost = 300; u.CostGrowthPerLevel = 0.8f;
                }),
                EnsureMetaUpgrade("production", u =>
                {
                    u.Title = "Worker Production";
                    u.Description = "+3% worker production per level. Run building upgrades remain separate.";
                    u.EffectType = MetaUpgradeEffectType.ProductionPercent;
                    u.ValuePerLevel = 0.03f; u.MaxLevel = 5; u.BaseCost = 350; u.CostGrowthPerLevel = 0.8f;
                }),
                EnsureMetaUpgrade("arrow_efficiency", u =>
                {
                    u.Title = "Arrow Efficiency";
                    u.Description = "+1 arrow per Wood per level. Run Arrow upgrades remain separate.";
                    u.EffectType = MetaUpgradeEffectType.ArrowEfficiency;
                    u.ValuePerLevel = 1f; u.MaxLevel = 10; u.BaseCost = 500; u.CostGrowthPerLevel = 0.9f;
                }),
                EnsureMetaUpgrade("essence_gain", u =>
                {
                    u.Title = "Essence Gain";
                    u.Description = "+5% Grave Essence gained per level.";
                    u.EffectType = MetaUpgradeEffectType.EssenceGainPercent;
                    u.ValuePerLevel = 0.05f; u.MaxLevel = 10; u.BaseCost = 600; u.CostGrowthPerLevel = 0.9f;
                }),
                EnsureMetaUpgrade("node_pool_unlock", u =>
                {
                    u.Title = "Additional Heart Pool";
                    u.Description = "Adds the approved bonus content pool to future Heart graphs.";
                    u.EffectType = MetaUpgradeEffectType.NodePoolUnlock;
                    u.ValuePerLevel = 0f; u.MaxLevel = 1; u.BaseCost = 2000; u.CostGrowthPerLevel = 0f;
                    u.PoolContentId = "heart.approved_bonus_pool.v1";
                }),
            };

            var catalog = AssetDatabase.LoadAssetAtPath<MetaUpgradeCatalogSO>(MetaCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MetaUpgradeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, MetaCatalogPath);
            }

            bool changed = catalog.Upgrades == null || catalog.Upgrades.Length != upgrades.Count;
            if (!changed)
            {
                for (int i = 0; i < upgrades.Count; i++)
                {
                    if (catalog.Upgrades[i] != upgrades[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }
            if (changed)
            {
                Undo.RecordObject(catalog, "Configure Meta Upgrade Catalog");
                catalog.Upgrades = upgrades.ToArray();
                EditorUtility.SetDirty(catalog);
            }

            foreach (var problem in catalog.ValidateCatalog())
                Debug.LogWarning($"[MobileCastleSceneSetup] MetaCatalog: {problem}", catalog);

            return catalog;
        }

        private static MetaUpgradeSO EnsureMetaUpgrade(string id, Action<MetaUpgradeSO> configure)
        {
            string path = MetaFolder + "/Meta_" + id + ".asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<MetaUpgradeSO>(path);
            if (upgrade != null)
                return upgrade; // mevcut asset degerlerine dokunma

            upgrade = ScriptableObject.CreateInstance<MetaUpgradeSO>();
            upgrade.Id = id;
            configure?.Invoke(upgrade);
            AssetDatabase.CreateAsset(upgrade, path);
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        // ---------------------------------------------------------------------------------
        // Difficulty profile seed: zorlugun tek dogruluk kaynagi (M-A olcum bulgularina gore
        // duzeltme paketi degerleriyle dogar). Mevcut asset'e ASLA dokunulmaz (merge-only).
        // Duzenleme yeri: Window > DeadWalls > Difficulty Tuner.
        // ---------------------------------------------------------------------------------

        public static DifficultyProfileSO EnsureDefaultDifficultyProfile()
        {
            EnsureAssetFolder(DifficultyFolder);

            var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(DifficultyProfilePath);
            if (profile != null)
                return profile;

            profile = ScriptableObject.CreateInstance<DifficultyProfileSO>();
            // Erken olum kamburu duzeltmesi (M-A): ilk geceler kademeli siddet rampi
            profile.NightIntensityByDay = new AnimationCurve(
                new Keyframe(1f, 0.60f), new Keyframe(2f, 0.80f),
                new Keyframe(3f, 1.00f), new Keyframe(60f, 1.00f));
            profile.ZombieHpMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
            profile.SpawnBatchMultByDay = AnimationCurve.Constant(1f, 60f, 1f);
            profile.SampleDays = 60;
            // V1 quantity-only difficulty: stat growth yok, baski kalabaliktan gelir.
            profile.ZombieHpGrowthPerCycle = 0f;
            profile.ZombieDamagePerCycle = 0f;
            profile.SpawnBatchGrowthPerCycle = 0.15f;
            profile.MaxSpawnBatch = 16;
            // Erken kurtulus yolu: repair'in stone bagimliligi dusuruldu
            profile.RepairBaseStoneCost = 50;
            profile.NormalRepairHealPercent = 0.25f;
            profile.RepairStonePerMissingHp = 0.10f;
            profile.RepairDayPriceMultiplier = 1f;
            profile.RallyCooldown = 60f;
            profile.EmergencyRepairHealPercent = 0.20f;
            profile.EmergencyRepairCooldown = 120f;
            profile.BedBaseWoodCost = MobileEconomyPriceTuningUtility.DefaultBedBaseWoodCost;
            profile.BedCostGrowthCapacityInterval =
                MobileEconomyPriceTuningUtility.DefaultBedCostGrowthCapacityInterval;
            profile.WorkerCapacityBaseWoodCost =
                MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseWoodCost;
            profile.WorkerCapacityBaseIronCost =
                MobileEconomyPriceTuningUtility.DefaultWorkerCapacityBaseIronCost;
            profile.WorkerEfficiencyBaseWoodCost =
                MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseWoodCost;
            profile.WorkerEfficiencyBaseIronCost =
                MobileEconomyPriceTuningUtility.DefaultWorkerEfficiencyBaseIronCost;
            profile.WorkerBuildingCostGrowthMultiplier =
                MobileEconomyPriceTuningUtility.DefaultWorkerBuildingCostGrowthMultiplier;
            profile.ArrowBaseCapacity = MobileEconomyPriceTuningUtility.DefaultArrowBaseCapacity;
            profile.ArrowCapacityPerLevel = MobileEconomyPriceTuningUtility.DefaultArrowCapacityPerLevel;
            profile.ArrowRefillPackageSize = MobileEconomyPriceTuningUtility.DefaultArrowRefillPackageSize;
            profile.ArrowBaseArrowsPerWood = MobileEconomyPriceTuningUtility.DefaultArrowBaseArrowsPerWood;
            profile.ArrowArrowsPerWoodPerEfficiencyLevel =
                MobileEconomyPriceTuningUtility.DefaultArrowArrowsPerWoodPerEfficiencyLevel;
            profile.ArrowCapacityBaseWoodCost =
                MobileEconomyPriceTuningUtility.DefaultArrowCapacityBaseWoodCost;
            profile.ArrowCapacityBaseIronCost =
                MobileEconomyPriceTuningUtility.DefaultArrowCapacityBaseIronCost;
            profile.ArrowEfficiencyBaseWoodCost =
                MobileEconomyPriceTuningUtility.DefaultArrowEfficiencyBaseWoodCost;
            profile.ArrowEfficiencyBaseIronCost =
                MobileEconomyPriceTuningUtility.DefaultArrowEfficiencyBaseIronCost;
            profile.ArrowUpgradeCostGrowthMultiplier =
                MobileEconomyPriceTuningUtility.DefaultArrowUpgradeCostGrowthMultiplier;
            AssetDatabase.CreateAsset(profile, DifficultyProfilePath);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Subscene'deki combat authoring'ine profili baglar (yalniz BOSSA; owner atamasina dokunmaz).</summary>
        private static void AssignDifficultyProfileToAuthoring(DifficultyProfileSO profile)
        {
            if (profile == null)
                return;

            var authoring = UnityEngine.Object.FindFirstObjectByType<MobileCastleCombatAuthoring>(FindObjectsInactive.Include);
            if (authoring == null || authoring.Profile != null)
                return;

            Undo.RecordObject(authoring, "Assign Difficulty Profile");
            authoring.Profile = profile;
            EditorUtility.SetDirty(authoring);
            EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
            EditorSceneManager.SaveScene(authoring.gameObject.scene);
        }

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
                    t.OutcomeB = "We decline the full trade. The master leaves {GAIN_N} {GAIN_RES} as a sample of what his next caravan could bring.";
                    t.Contrast = CouncilContrastType.ResourceTrade;
                    t.OptionAAtomIds = new[] { "pay_resource" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.OptionAVerb = "Make the trade"; t.OptionBVerb = "Take the sample";
                    t.SetsFlagOnA = "traded_with_merchant";
                    t.ForbiddenFlags = new[] { "traded_with_merchant" };
                    t.MinDay = 3;
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
                    t.BaseWeight = 1.2f; t.MinDay = 3;
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
                    t.OutcomeB = "The gate stays shut. Their leader trades {GAIN_N} {GAIN_RES} for a marked route away from the horde.";
                    t.Contrast = CouncilContrastType.PopulationVsResource;
                    t.OptionAAtomIds = new[] { "gain_population" };
                    t.OptionBAtomIds = new[] { "gain_resource" };
                    t.OptionAVerb = "Open the gate"; t.OptionBVerb = "Trade at the gate";
                    t.SetsFlagOnA = "refugees_taken";
                    t.ForbiddenFlags = new[] { "refugees_taken" };
                    t.MinDay = 6;
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
                    t.BaseWeight = 0.9f; t.MinDay = 6;
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
                    t.OutcomeB = "The crews strip two stockpiles before torching the camps. Both hauls reach the wall, but tonight comes {NIGHT_PCT}% harder.";
                    t.Contrast = CouncilContrastType.SafeVsRisky;
                    t.OptionAAtomIds = new[] { "calm_night" };
                    t.OptionBAtomIds = new[] { "wild_night" };
                    t.OptionAVerb = "Burn it all"; t.OptionBVerb = "Loot first, then burn";
                    t.MinDay = 9; t.BaseWeight = 0.9f;
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
                    t.MinDay = 6; t.BaseWeight = 0.8f;
                }),
                EnsureCouncilTemplate("quarry_crew", t =>
                {
                    t.Title = "IDLE WORK CREW";
                    t.BodyVariants = new[]
                    {
                        "A quarry crew, cut off from its old contracts, offers one hard night's labor. Take the haul now, or have them reorganize the yards before they leave.",
                        "Skilled quarry hands are waiting for the roads to reopen. They can fill the stores tonight, or spend their time improving the yard crews' output.",
                    };
                    t.OutcomeA = "They work through the night and hand over {GAIN_N} {GAIN_RES}, then drift on down the road.";
                    t.OutcomeB = "Before leaving, the crew redraws every route through the yards. {BOOST_RES} output is up {BOOST_PCT}% for the next {BOOST_D} days.";
                    t.Contrast = CouncilContrastType.NowVsLater;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.OptionAVerb = "One big job"; t.OptionBVerb = "Rework the yards";
                    t.MinDay = 3;
                }),
                EnsureCouncilTemplate("among_the_refugees", t =>
                {
                    t.Title = "AMONG THE REFUGEES";
                    t.BodyVariants = new[]
                    {
                        "One of the newcomers is a guild craftsman — the kind cities used to fight over. He can spend the day resetting the Wall, or reorganize one yard before opening his workshop.",
                        "The newcomers have been inside for two days when the yard foreman recognizes a guild mason among them. He offers one favor: restore the Wall, or put one production yard back in order.",
                    };
                    t.OutcomeA = "He spends the day at the Wall, resetting stone and braces. The damage is repaired by {HEAL_PCT}%.";
                    t.OutcomeB = "He reorganizes the {BOOST_RES} yards before opening his workshop. Output is up {BOOST_PCT}% for {BOOST_D} days.";
                    t.Contrast = CouncilContrastType.DefenseVsProduction;
                    t.OptionAAtomIds = new[] { "heal_defense" };
                    t.OptionBAtomIds = new[] { "boost_production" };
                    t.OptionAVerb = "Repair the Wall"; t.OptionBVerb = "Rework the yards";
                    t.RequiredFlags = new[] { "refugees_taken" };
                    t.ChainDelayDays = 2; t.OneShot = true; t.BaseWeight = 2f;
                }),
                EnsureCouncilTemplate("an_old_friend", t =>
                {
                    t.Title = "AN OLD FRIEND";
                    t.BodyVariants = new[]
                    {
                        "The caravan master is back with a final offer: one last haul, or seats for skilled families searching for walls. People will need beds and food.",
                        "The same caravan rolls back under our banner. Its master can unload a final cache, or bring skilled families willing to settle behind the Wall. They will need beds and food.",
                    };
                    t.OutcomeA = "The wagons empty at our gate: {GAIN_N} {GAIN_RES}. He tips his hat. 'Pleasure as always.'";
                    t.OutcomeB = "The wagons arrive full of people instead of goods. {POP_N} skilled settlers enter the gate and join the idle workforce.";
                    t.Contrast = CouncilContrastType.ResourceVsPopulation;
                    t.OptionAAtomIds = new[] { "gain_cache" };
                    t.OptionBAtomIds = new[] { "gain_population" };
                    t.OptionAVerb = "One last haul"; t.OptionBVerb = "Bring the settlers";
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
            MergeCouncilCuratedChains(catalog, new[]
            {
                new CouncilCuratedChain
                {
                    SourceTemplateId = "refugees_at_gate",
                    SourceBranch = CouncilChoiceBranch.OptionA,
                    Flag = "refugees_taken",
                    TargetTemplateId = "among_the_refugees",
                },
                new CouncilCuratedChain
                {
                    SourceTemplateId = "merchant_caravan",
                    SourceBranch = CouncilChoiceBranch.OptionA,
                    Flag = "traded_with_merchant",
                    TargetTemplateId = "an_old_friend",
                },
            });

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

        private static void MergeCouncilCuratedChains(CouncilEventCatalogSO catalog,
            CouncilCuratedChain[] defaults)
        {
            var merged = new List<CouncilCuratedChain>();
            if (catalog.CuratedChains != null)
                merged.AddRange(catalog.CuratedChains);

            bool changed = false;
            foreach (CouncilCuratedChain candidate in defaults)
            {
                bool exists = false;
                foreach (CouncilCuratedChain current in merged)
                {
                    if (current.SourceTemplateId == candidate.SourceTemplateId
                        && current.SourceBranch == candidate.SourceBranch
                        && current.Flag == candidate.Flag
                        && current.TargetTemplateId == candidate.TargetTemplateId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists)
                    continue;

                merged.Add(candidate);
                changed = true;
            }

            if (!changed)
                return;

            Undo.RecordObject(catalog, "Configure Council Curated Chains");
            catalog.CuratedChains = merged.ToArray();
            EditorUtility.SetDirty(catalog);
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
            placement.RouteCorridorX = -0.9f;
            placement.HubApproachY = 0.6f;
            placement.RouteLaneSpacing = 0.1f;
            placement.RouteLaneCount = 5;
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
            if (arrowHit != null) bridge.ArrowHitPrefab = arrowHit;
            // Polish fix: kale vurusu OK gorseliyle OYNAMAZ (duvara ok saplanmasi bug'i) —
            // eski fallback'le atanmis arrowHit temizlenir; owner ileride kendi impact
            // prefab'ini atarsa dokunulmaz.
            if (bridge.CastleHitPrefab == arrowHit)
                bridge.CastleHitPrefab = null;
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

            // M-D his katmani: yeni SFX clip'leri (RPG Magic ELEMENTAL paketi; yalniz-bossa ata)
            if (bridge.ZombieDeathClips == null || bridge.ZombieDeathClips.Length == 0)
            {
                bridge.ZombieDeathClips = new[]
                {
                    LoadSfx("UI, Pads, Enchantments and Misc/RPG3_MONSTER_Hurt01.wav"),
                    LoadSfx("UI, Pads, Enchantments and Misc/RPG3_MONSTER_Hurt02.wav"),
                };
            }
            if (bridge.FireballBlastClip == null)
                bridge.FireballBlastClip = LoadSfx("Fire Magic/RPG3_FireMagic_Explosion02.wav");
            if (bridge.ArrowHitClip == null)
                bridge.ArrowHitClip = LoadSfx("Generic Magic and Impacts/RPG3_GenericArrow_Impact01.wav");
            if (bridge.FrostHitClip == null)
                bridge.FrostHitClip = LoadSfx("Ice Magic/RPG3_IceMagic2_IceBreak01.wav");
            if (bridge.CastleHitClip == null)
                bridge.CastleHitClip = LoadSfx("Generic Magic and Impacts/RPG3_GenericCannon_LowImpact01.wav");

            EditorUtility.SetDirty(bridge);
            EditorUtility.SetDirty(root);
        }

        private const string SfxPackRoot = "Assets/RPG Magic Sound Effects Pack 3 [ELEMENTAL]/";

        private static AudioClip LoadSfx(string relativePath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxPackRoot + relativePath);
            if (clip == null)
                Debug.LogWarning($"[MobileCastleSceneSetup] SFX bulunamadi: {SfxPackRoot + relativePath}");
            return clip;
        }

        /// <summary>
        /// Menu sistemi (M-E): MenuUiRoot (hep aktif; MainMenuUI + PauseMenuUI + SettingsUI
        /// controller'lari burada) + MainMenuPanel (tam ekran; acilis) + PauseButton (sag ust)
        /// + PausePanel + SettingsPanel. Isim sozlesmeleri controller binding'leridir.
        /// GameOverPanel'den SONRA kurulur (menu paneller olum ekraninin da ustunde acilabilir).
        /// </summary>
        private static void ConfigureMenuSystem(Transform canvasTransform)
        {
            GameObject root = FindDirectChild(canvasTransform, "MenuUiRoot");
            if (root == null)
            {
                root = new GameObject("MenuUiRoot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "Create Menu UI Root");
                root.layer = canvasTransform.gameObject.layer;
                root.transform.SetParent(canvasTransform, false);
            }
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            root.transform.SetAsLastSibling();

            var settings = EnsureComponent<SettingsUI>(root);
            var pauseMenu = EnsureComponent<PauseMenuUI>(root);

            // M-E v2: panel-menu ayri sahneye tasindi — eski kalintilari temizle
            DestroyChildIfExists(root.transform, "MainMenuPanel");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

            // --- Pause butonu (sag ust; oyun sirasinda gorunur) ---
            var pauseButton = EnsureButton(root.transform, "PauseButton",
                new Vector2(1f, 1f), new Vector2(-64f, -64f), new Vector2(-16f, -16f), out var pauseLabel);
            pauseLabel.text = "II";
            pauseLabel.fontSize = 20;
            pauseMenu.PauseButton = pauseButton;

            // --- SOUL sayaci (Polish 2): pause butonunun altinda kucuk kosu-birikimi kutusu ---
            var soulCounter = EnsureComponent<SoulCounterUI>(root);
            GameObject soulPanel = EnsurePanel(root.transform, "SoulCounterPanel", false, new Color(0.10f, 0.07f, 0.16f, 0.85f));
            var soulRect = (RectTransform)soulPanel.transform;
            soulRect.anchorMin = new Vector2(1f, 1f);
            soulRect.anchorMax = new Vector2(1f, 1f);
            soulRect.offsetMin = new Vector2(-150f, -112f);
            soulRect.offsetMax = new Vector2(-16f, -72f);
            var soulText = EnsureText(soulPanel.transform, "SoulCounterText", "SOULS  0", 17,
                TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 2f), new Vector2(-4f, -2f));
            soulCounter.CounterPanel = soulPanel;
            soulCounter.CounterText = soulText;
            EditorUtility.SetDirty(soulCounter);

            // --- Pause paneli ---
            GameObject pausePanel = EnsurePanel(root.transform, "PausePanel", false, new Color(0.02f, 0.02f, 0.04f, 0.92f));
            Stretch(pausePanel.GetComponent<RectTransform>());
            EnsureText(pausePanel.transform, "PauseTitleText", "PAUSED", 44, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, 120f), new Vector2(220f, 190f));
            var resumeButton = EnsureButton(pausePanel.transform, "ResumeButton",
                new Vector2(0.5f, 0.5f), new Vector2(-130f, 30f), new Vector2(130f, 88f), out var resumeLabel);
            resumeLabel.text = "RESUME";
            var pauseSettingsButton = EnsureButton(pausePanel.transform, "PauseSettingsButton",
                new Vector2(0.5f, 0.5f), new Vector2(-130f, -40f), new Vector2(130f, 18f), out var pauseSettingsLabel);
            pauseSettingsLabel.text = "SETTINGS";
            var pauseRestartButton = EnsureButton(pausePanel.transform, "PauseRestartButton",
                new Vector2(0.5f, 0.5f), new Vector2(-130f, -110f), new Vector2(130f, -52f), out var pauseRestartLabel);
            pauseRestartLabel.text = "NEW RUN";
            var pauseMainMenuButton = EnsureButton(pausePanel.transform, "PauseMainMenuButton",
                new Vector2(0.5f, 0.5f), new Vector2(-130f, -180f), new Vector2(130f, -122f), out var pauseMainMenuLabel);
            pauseMainMenuLabel.text = "MAIN MENU";
            pauseMenu.PausePanel = pausePanel;
            pauseMenu.ResumeButton = resumeButton;
            pauseMenu.SettingsButton = pauseSettingsButton;
            pauseMenu.RestartButton = pauseRestartButton;
            pauseMenu.MainMenuButton = pauseMainMenuButton;
            pauseMenu.Settings = settings;

            // --- Settings paneli (pause acar; acan panelin ustune gecer) ---
            BuildSettingsPanel(root.transform, settings);

            pausePanel.SetActive(false);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(pauseMenu);
        }

        /// <summary>Ses ayarlari paneli ureticisi — oyun sahnesi (pause) ve ana menu sahnesi paylasir.</summary>
        private static void BuildSettingsPanel(Transform parent, SettingsUI settings)
        {
            GameObject settingsPanel = EnsurePanel(parent, "SettingsPanel", false, new Color(0.03f, 0.03f, 0.06f, 0.96f));
            Center(settingsPanel.GetComponent<RectTransform>(), new Vector2(520f, 360f));
            EnsureText(settingsPanel.transform, "SettingsTitleText", "SETTINGS", 34, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-200f, -70f), new Vector2(200f, -16f));
            EnsureText(settingsPanel.transform, "SfxLabelText", "SFX", 20, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 26f), new Vector2(180f, 62f));
            var sfxSlider = EnsureSlider(settingsPanel.transform, "SfxSlider",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, 30f), new Vector2(220f, 56f),
                new Color(0.12f, 0.13f, 0.16f, 1f), new Color(0.85f, 0.55f, 0.20f, 1f));
            EnsureText(settingsPanel.transform, "AmbienceLabelText", "AMBIENCE", 20, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, -48f), new Vector2(180f, -12f));
            var ambienceSlider = EnsureSlider(settingsPanel.transform, "AmbienceSlider",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70f, -44f), new Vector2(220f, -18f),
                new Color(0.12f, 0.13f, 0.16f, 1f), new Color(0.35f, 0.60f, 0.90f, 1f));
            var closeButton = EnsureButton(settingsPanel.transform, "SettingsCloseButton",
                new Vector2(0.5f, 0f), new Vector2(-110f, 18f), new Vector2(110f, 70f), out var closeLabel);
            closeLabel.text = "CLOSE";
            settings.SettingsPanel = settingsPanel;
            settings.SfxSlider = sfxSlider;
            settings.AmbienceSlider = ambienceSlider;
            settings.CloseButton = closeButton;
            settingsPanel.SetActive(false);
        }

        // ---------------------------------------------------------------------------------
        // Ana menu SAHNESI (M-E v2, owner istegi): ayri hafif sahne — kamera + Canvas + menu.
        // Additive acilir/kurulur/kaydedilir/kapatilir (aktif sahne degismez). Gorseller
        // runtime uretilir (MainMenuSceneUI/MenuSpriteFactory); burada yalniz iskelet + binding.
        // ---------------------------------------------------------------------------------

        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string GameScenePathForBuild = "Assets/Scenes/NewGameScene.unity";

        private static void EnsureMainMenuScene()
        {
            bool exists = System.IO.File.Exists(MainMenuScenePath);
            Scene menuScene = exists
                ? EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            // kamera (solid koyu; UI-disi bir sey render etmez)
            GameObject cameraGo = FindRoot(menuScene, "Main Camera");
            if (cameraGo == null)
            {
                cameraGo = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraGo, menuScene);
            }
            var camera = EnsureComponent<Camera>(cameraGo);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";
            EnsureComponent<AudioListener>(cameraGo);

            // EventSystem
            GameObject eventSystemGo = FindRoot(menuScene, "EventSystem");
            if (eventSystemGo == null)
            {
                eventSystemGo = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(eventSystemGo, menuScene);
            }
            EnsureComponent<UnityEngine.EventSystems.EventSystem>(eventSystemGo);
            EnsureComponent<UnityEngine.EventSystems.StandaloneInputModule>(eventSystemGo);

            // Canvas
            GameObject canvasGo = FindRoot(menuScene, "Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasGo, menuScene);
            }
            var canvas = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = EnsureComponent<UnityEngine.UI.CanvasScaler>(canvasGo);
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<UnityEngine.UI.GraphicRaycaster>(canvasGo);
            SetLayerRecursive(canvasGo, LayerMask.NameToLayer("UI"));

            var menuUi = EnsureComponent<MainMenuSceneUI>(canvasGo);
            var settings = EnsureComponent<SettingsUI>(canvasGo);
            Transform ct = canvasGo.transform;

            // arka plan (sprite runtime'da uretilir; burada bos Image)
            GameObject bg = EnsureChild(ct, "BackgroundImage", true);
            Stretch(bg.GetComponent<RectTransform>());
            var bgImage = EnsureComponent<UnityEngine.UI.Image>(bg);
            bgImage.raycastTarget = false;
            menuUi.BackgroundImage = bgImage;

            // kanli ay (glow altta, ay ustte) — sag ust bolge
            GameObject glow = EnsureChild(ct, "MoonGlowImage", true);
            SetRect(glow.GetComponent<RectTransform>(), new Vector2(0.84f, 0.62f), new Vector2(0.84f, 0.62f),
                new Vector2(-240f, -240f), new Vector2(240f, 240f));
            var glowImage = EnsureComponent<UnityEngine.UI.Image>(glow);
            glowImage.raycastTarget = false;
            menuUi.MoonGlowImage = glowImage;

            GameObject moon = EnsureChild(ct, "MoonImage", true);
            SetRect(moon.GetComponent<RectTransform>(), new Vector2(0.84f, 0.62f), new Vector2(0.84f, 0.62f),
                new Vector2(-150f, -150f), new Vector2(150f, 150f));
            var moonImage = EnsureComponent<UnityEngine.UI.Image>(moon);
            moonImage.raycastTarget = false;
            menuUi.MoonImage = moonImage;

            // baslik + tagline
            var title = EnsureText(ct, "TitleText", "DEAD WALLS", 84, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-460f, -300f), new Vector2(460f, -170f));
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.93f, 0.88f, 0.84f, 1f);
            title.characterSpacing = 8f;
            menuUi.TitleText = title;

            var tagline = EnsureText(ct, "TaglineText", "THE HORDE COMES AT NIGHT", 21, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-380f, -348f), new Vector2(380f, -300f));
            tagline.color = new Color(0.62f, 0.56f, 0.55f, 1f);
            tagline.characterSpacing = 14f;
            menuUi.TaglineText = tagline;

            // butonlar (dikey grup; CanvasGroup giris animasyonu icin)
            GameObject buttonsRoot = EnsureChild(ct, "ButtonsRoot", true);
            SetRect(buttonsRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.40f),
                new Vector2(-210f, -140f), new Vector2(210f, 140f));
            var buttonsGroup = EnsureComponent<CanvasGroup>(buttonsRoot);
            var layout = EnsureComponent<UnityEngine.UI.VerticalLayoutGroup>(buttonsRoot);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            menuUi.ButtonsGroup = buttonsGroup;

            var continueButton = EnsureButton(buttonsRoot.transform, "ContinueButton",
                new Vector2(0.5f, 0.5f), new Vector2(-210f, -33f), new Vector2(210f, 33f), out var continueLabel);
            continueLabel.text = "CONTINUE";
            continueLabel.fontSize = 25;
            EnsureComponent<UnityEngine.UI.LayoutElement>(continueButton.gameObject).preferredHeight = 66f;
            menuUi.ContinueButton = continueButton;
            menuUi.ContinueLabelText = continueLabel;

            var newRunButton = EnsureButton(buttonsRoot.transform, "NewRunButton",
                new Vector2(0.5f, 0.5f), new Vector2(-210f, -33f), new Vector2(210f, 33f), out var newRunLabel);
            newRunLabel.text = "NEW RUN";
            newRunLabel.fontSize = 25;
            EnsureComponent<UnityEngine.UI.LayoutElement>(newRunButton.gameObject).preferredHeight = 66f;
            menuUi.NewRunButton = newRunButton;

            var menuSettingsButton = EnsureButton(buttonsRoot.transform, "MenuSettingsButton",
                new Vector2(0.5f, 0.5f), new Vector2(-210f, -33f), new Vector2(210f, 33f), out var menuSettingsLabel);
            menuSettingsLabel.text = "SETTINGS";
            menuSettingsLabel.fontSize = 25;
            EnsureComponent<UnityEngine.UI.LayoutElement>(menuSettingsButton.gameObject).preferredHeight = 66f;
            menuUi.SettingsButton = menuSettingsButton;

            // versiyon
            var version = EnsureText(ct, "VersionText", "v0.1", 16, TextAlignmentOptions.BottomLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 14f), new Vector2(160f, 44f));
            version.color = new Color(1f, 1f, 1f, 0.35f);
            menuUi.VersionText = version;

            // settings paneli (ortak uretici)
            BuildSettingsPanel(ct, settings);
            menuUi.Settings = settings;

            // menu sesleri (Polish 3): tik/olay sesleri + arka plan ambiyansi
            ConfigureUiSounds(canvasGo);
            GameObject ambienceGo = FindRoot(menuScene, "MenuAmbience");
            if (ambienceGo == null)
            {
                ambienceGo = new GameObject("MenuAmbience");
                SceneManager.MoveGameObjectToScene(ambienceGo, menuScene);
            }
            var ambienceSource = EnsureComponent<AudioSource>(ambienceGo);
            ambienceSource.playOnAwake = false;
            if (ambienceSource.clip == null)
                ambienceSource.clip = LoadSfx("Wind Magic/RPG3_WindMagic_Drone01_LowSubtleLoop.wav");
            menuUi.AmbienceSource = ambienceSource;

            EditorUtility.SetDirty(menuUi);
            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
            EditorSceneManager.CloseScene(menuScene, true);

            EnsureBuildSettingsScenes();
        }

        /// <summary>Build Settings: menu sahnesi index 0, oyun sahnesi index 1 (yalniz eksikse eklenir).</summary>
        private static void EnsureBuildSettingsScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool hasMenu = scenes.Exists(s => s.path == MainMenuScenePath);
            bool hasGame = scenes.Exists(s => s.path == GameScenePathForBuild);
            bool changed = false;
            if (!hasMenu)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(MainMenuScenePath, true));
                changed = true;
            }
            if (!hasGame)
            {
                scenes.Add(new EditorBuildSettingsScene(GameScenePathForBuild, true));
                changed = true;
            }
            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>UI ses geribildirimi (Polish 3): merkezi tik + olay sesleri; clip'ler yalniz-bossa atanir.</summary>
        private static void ConfigureUiSounds(GameObject canvasObject)
        {
            var sounds = EnsureComponent<UiSoundFeedback>(canvasObject);
            if (sounds.ClickClip == null)
                sounds.ClickClip = LoadSfx("Generic Magic and Impacts/RPG3_Generic_SubtleWhoosh01.wav");
            if (sounds.SuccessClip == null)
                sounds.SuccessClip = LoadSfx("UI, Pads, Enchantments and Misc/RPG3_Enchantment2_Success01v2_Short.wav");
            if (sounds.FailClip == null)
                sounds.FailClip = LoadSfx("UI, Pads, Enchantments and Misc/RPG3_UI_NegativeAlert01.wav");
            if (sounds.DeathStingClip == null)
                sounds.DeathStingClip = LoadSfx("Generic Magic and Impacts/RPG3_GenericMisc_LowBoom01.wav");
            EditorUtility.SetDirty(sounds);
        }

        /// <summary>Kale hasar hissi (M-D): ana kameraya CameraShaker kurar.</summary>
        private static void EnsureCameraShaker(Scene scene)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraGo = FindRoot(scene, "Main Camera");
                camera = cameraGo != null ? cameraGo.GetComponent<Camera>() : null;
            }
            if (camera == null)
                return;

            EnsureComponent<CameraShaker>(camera.gameObject);
            EditorUtility.SetDirty(camera.gameObject);
        }

        /// <summary>Kale hasar flash'i (M-D): Canvas'in EN USTUNE tam-ekran FlashOverlay kurar.</summary>
        private static void EnsureDamageFlash(Transform canvasTransform)
        {
            GameObject overlay = FindDirectChild(canvasTransform, "DamageFlashOverlay");
            if (overlay == null)
            {
                overlay = new GameObject("DamageFlashOverlay", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(overlay, "Create Damage Flash Overlay");
                overlay.layer = canvasTransform.gameObject.layer;
                overlay.transform.SetParent(canvasTransform, false);
            }
            var rect = (RectTransform)overlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay.transform.SetAsLastSibling(); // her seyin ustunde parlar

            var image = EnsureComponent<UnityEngine.UI.Image>(overlay);
            image.color = new Color(0.75f, 0.08f, 0.05f, 0f);
            image.raycastTarget = false;

            var flash = EnsureComponent<DamageFlashUI>(overlay);
            flash.FlashImage = image;
            EditorUtility.SetDirty(flash);
        }

        /// <summary>Gece/kanli ay ambiyansi (M-D): sahne kokune AmbientAudioController kurar, clip'leri yalniz-bossa atar.</summary>
        private static void EnsureAmbientAudio(Scene scene)
        {
            GameObject root = FindRoot(scene, "AmbientAudioRoot");
            if (root == null)
            {
                root = new GameObject("AmbientAudioRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create Ambient Audio Root");
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            var ambient = EnsureComponent<AmbientAudioController>(root);
            EnsureComponent<MomentVignetteUI>(root); // an vurgulari (Polish 2): safak altini + kanli ay kizili
            if (ambient.NightLoop == null)
                ambient.NightLoop = LoadSfx("Wind Magic/RPG3_WindMagic_Drone01_LowSubtleLoop.wav");
            if (ambient.BloodMoonLoop == null)
                ambient.BloodMoonLoop = LoadSfx("Dark Magic/RPG3_DarkMagic_DroneUnderworld_Loop.wav");
            if (ambient.BloodMoonSting == null)
                ambient.BloodMoonSting = LoadSfx("UI, Pads, Enchantments and Misc/RPG3_MONSTER_Roar01.wav");
            EditorUtility.SetDirty(ambient);
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

        // ---------------------------------------------------------------------------------
        // UI gorsel birligi (Polish 2): menu'nun rounded-rect dilini oyun ici panellere
        // tasimak icin sprite ASSET olarak uretilir (MenuSpriteFactory ayni matematigi
        // runtime'da menu icin kullanir; asset versiyonu edit-time atanabilir olsun diye).
        // ---------------------------------------------------------------------------------

        private const string GeneratedArtFolder = "Assets/Art/Generated";
        private const string RoundedRectAssetPath = GeneratedArtFolder + "/ui_rounded_rect.png";

        private static Sprite EnsureRoundedRectAsset()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedArtFolder))
                AssetDatabase.CreateFolder("Assets/Art", "Generated");

            if (!System.IO.File.Exists(RoundedRectAssetPath))
            {
                // MenuSpriteFactory.CreateRoundedRect ile ayni matematik (64px, radius 18, AA)
                const int size = 64;
                const float radius = 18f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                        float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                System.IO.File.WriteAllBytes(RoundedRectAssetPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(RoundedRectAssetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = AssetImporter.GetAtPath(RoundedRectAssetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (importer.spriteBorder != new Vector4(20f, 20f, 20f, 20f)) { importer.spriteBorder = new Vector4(20f, 20f, 20f, 20f); dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectAssetPath);
        }

        /// <summary>Bir Image'i rounded-rect diline gecirir (sprite + Sliced; renk korunur/verilir).</summary>
        private static void ApplyRoundedSkin(UnityEngine.UI.Image image, Sprite rounded, Color? color = null, float ppuMultiplier = 1.6f)
        {
            if (image == null || rounded == null)
                return;

            image.sprite = rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = ppuMultiplier;
            if (color.HasValue)
                image.color = color.Value;
            EditorUtility.SetDirty(image);
        }

        /// <summary>
        /// Oyun ici kod-uretimli panelleri menu diline gecirir (Polish 2): panel kokleri +
        /// TUM butonlar rounded olur (renkler korunur). Dim/flash katmanlari ve slider'lar
        /// bilerek duz kalir (tam-ekran ortuler koseye ihtiyac duymaz; slider'lar ince).
        /// </summary>
        private static void ApplyGameUiSkin(Transform canvasTransform)
        {
            Sprite rounded = EnsureRoundedRectAsset();
            if (rounded == null)
                return;

            string[] panelPaths =
            {
                "GameOverPanel",
                "MenuUiRoot/PausePanel",
                "MenuUiRoot/SettingsPanel",
                "MobileCastleHudRoot/AbilityBarPanel",
                "LevelUpPanel",
                "GameOverPanel/MetaShopListRoot/MetaShopRowTemplate"
            };
            foreach (var path in panelPaths)
            {
                var t = canvasTransform.Find(path);
                if (t != null)
                    ApplyRoundedSkin(t.GetComponent<UnityEngine.UI.Image>(), rounded);
            }

            // tum butonlar (dim'ler Button degil — etkilenmez)
            foreach (var button in canvasTransform.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var image = button.GetComponent<UnityEngine.UI.Image>();
                float height = ((RectTransform)button.transform).rect.height;
                ApplyRoundedSkin(image, rounded, null, height < 46f ? 2.2f : 1.6f);
            }
        }

        // ---------------------------------------------------------------------------------
        // Fireball flipbook'lari (polish): Super Pixel paketlerinden ucus + patlama kareleri.
        // Sheet'ler grid-slice edilir (spritesheet.txt duzeni) ve SpellCastUI'ya atanir.
        // ---------------------------------------------------------------------------------

        private const string FireballProjectileSheetPath =
            "Assets/Art/Projectiles/Super Pixel Projectiles Pack 2/spritesheet/pj2_fireball_large_orange/spritesheet.png";
        private const string FireballBlastSheetPath =
            "Assets/Art/Super Pixel Fantasy FX Pack 2/spritesheet/fanfx2_fire_spell_large_orange/spritesheet.png";

        private static void ConfigureFireballVisuals(SpellCastUI spell)
        {
            // ucus: 10 kare, 72x32; PPU 28 -> dunyada ~2.6 birim uzunluk (net gorunur meteor)
            EnsureGridSlicedSheet(FireballProjectileSheetPath, 72, 32, 10, 28f);
            // patlama: 23 kare, 160x160; PPU 100 -> SpellCastUI radius'a gore olcekler
            EnsureGridSlicedSheet(FireballBlastSheetPath, 160, 160, 23, 100f);

            spell.ProjectileFrames = LoadSlicedSprites(FireballProjectileSheetPath, 10);
            spell.BlastFrames = LoadSlicedSprites(FireballBlastSheetPath, 23);
            EditorUtility.SetDirty(spell);
        }

        /// <summary>Sheet'i Sprite/Multiple + grid slice olarak import eder (idempotent).</summary>
        private static void EnsureGridSlicedSheet(string path, int frameWidth, int frameHeight, int frameCount, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[MobileCastleSceneSetup] Sheet bulunamadi: {path}");
                return;
            }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Multiple) { importer.spriteImportMode = SpriteImportMode.Multiple; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; } // pixel-art
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)) { importer.spritePixelsPerUnit = pixelsPerUnit; dirty = true; }
            if (importer.maxTextureSize < 4096) { importer.maxTextureSize = 4096; dirty = true; }

            var existing = importer.spritesheet;
            if (existing == null || existing.Length != frameCount)
            {
                var metas = new SpriteMetaData[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    metas[i] = new SpriteMetaData
                    {
                        name = "frame_" + i.ToString("000"),
                        rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                }
                importer.spritesheet = metas;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        private static Sprite[] LoadSlicedSprites(string path, int expectedCount)
        {
            var sprites = new List<Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); // frame_000.. sirali
            if (sprites.Count != expectedCount)
                Debug.LogWarning($"[MobileCastleSceneSetup] {path}: {expectedCount} kare beklenirken {sprites.Count} bulundu.");
            return sprites.ToArray();
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

        private static void EnsureArcherTilePlacement(
            Scene scene, ArcherFormationDefinitionSO formationDefinition)
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
            placement.Configure(outside, formationDefinition);
            placement.RebuildCache();
            if (placement.SpawnCellCount != ArcherFormationUtility.RequiredTileCount
                || placement.FormationCapacity != ArcherFormationUtility.TotalCapacity)
            {
                throw new InvalidOperationException(
                    "NewGameScene outside tilemap 40x25 archer formation contract'ini karsilamiyor.");
            }

            EditorUtility.SetDirty(placement);
        }

        private static void NormalizeCastleTilemapSorting(Scene scene)
        {
            // Owner duvar tasarimi (05de29e98'den geri getirildi, 2026-07-07): kaldirim govde
            // (outside0/outside, Wall/2) + G-dilim zirh (outside2, Wall/4 = on-orducu; birimleri
            // duvarin arkasinda gizler). outside ayni zamanda okcu slot kaynagi (H4 yuruyus yolu).
            NormalizeTilemapRenderer(scene, "outside0", "Wall", 2, MobileCastleRenderDepth.BackTilemapZ);
            NormalizeTilemapRenderer(scene, "outside", "Wall", 2, MobileCastleRenderDepth.BackTilemapZ);
            NormalizeTilemapRenderer(scene, "outside2", "Wall", 4, MobileCastleRenderDepth.FrontOccluderZ);
        }

        private static void NormalizeTilemapRenderer(Scene scene, string tilemapName, string sortingLayer, int sortingOrder, float worldZ)
        {
            Tilemap tilemap = FindSceneTilemap(scene, tilemapName);
            if (tilemap == null)
                return;

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer == null)
                return;

            bool changed = false;
            if (renderer.sortingLayerName != sortingLayer)
            {
                renderer.sortingLayerName = sortingLayer;
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
            Center(gameOverPanel.GetComponent<RectTransform>(), new Vector2(680f, 640f));
            var gameOver = EnsureComponent<GameOverUI>(gameOverPanel);
            gameOver.GameOverText = EnsureText(gameOverPanel.transform, "GameOverText", "GAME OVER", 48,
                TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-260f, -86f), new Vector2(260f, -20f));
            gameOver.StatsText = EnsureText(gameOverPanel.transform, "StatsText", "Wave: 1\nLevel: 1", 22,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, 190f), new Vector2(240f, 252f));
            gameOver.RestartButton = EnsureButton(gameOverPanel.transform, "RestartButton",
                new Vector2(0.5f, 0f), new Vector2(-120f, 24f), new Vector2(120f, 84f), out var restartText);
            restartText.text = "Restart";

            ConfigureMetaProgressionUI(gameOverPanel);
            ConfigureBloodMoonWarning(canvasTransform);
            ConfigureUiSounds(canvasTransform.gameObject); // Polish 3: tik/basari/fail/sting

            uiManager.HUDPanel = hudRoot;
            uiManager.LevelUpPanel = levelUpPanel;
            uiManager.MarketPanel = hudRoot;
            uiManager.GameOverPanel = gameOverPanel;

            // Olum ekrani diger tum UI'in (council karti dahil) USTUNDE durmali;
            // DamageFlashOverlay setup akisinin sonunda kendini yine en sona alir.
            gameOverPanel.transform.SetAsLastSibling();

            hudRoot.SetActive(true);
            levelUpPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        /// <summary>
        /// Olum ekrani meta katmani (roguelite): kosu ozeti + RUH bakiyesi + kalici yukseltme
        /// magazasi. GameOverPanel kod-uretimli oldugundan objeler burada kurulur (prefab degil);
        /// isim sozlesmesi: MetaSummaryText / MetaSoulsText / MetaShopListRoot / MetaShopRowTemplate
        /// (Row cocuklari: RowTitleText / RowLevelText / RowCostText / RowBuyButton).
        /// </summary>
        private static void ConfigureMetaProgressionUI(GameObject gameOverPanel)
        {
            var meta = EnsureComponent<MetaProgressionUI>(gameOverPanel);

            // Tam-ekran dim: arka plandaki HUD'u karartir + yanlis tiklamayi bloklar
            GameObject dim = FindDirectChild(gameOverPanel.transform, "GameOverDim");
            if (dim == null)
            {
                dim = new GameObject("GameOverDim", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(dim, "Create Game Over Dim");
                dim.layer = gameOverPanel.layer;
                dim.transform.SetParent(gameOverPanel.transform, false);
            }
            var dimRect = (RectTransform)dim.transform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = new Vector2(-2400f, -1400f); // panel sinirlarini asip ekrani kaplar
            dimRect.offsetMax = new Vector2(2400f, 1400f);
            var dimImage = EnsureComponent<UnityEngine.UI.Image>(dim);
            dimImage.color = new Color(0.01f, 0.01f, 0.02f, 0.86f);
            dimImage.raycastTarget = true;
            dim.transform.SetAsFirstSibling(); // icerik ustte kalir

            meta.MetaSummaryText = EnsureText(gameOverPanel.transform, "MetaSummaryText",
                "DAY 1 — 0 kills\n+0 SOULS", 22, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300f, 116f), new Vector2(300f, 184f));

            meta.MetaSoulsText = EnsureText(gameOverPanel.transform, "MetaSoulsText",
                "0 SOULS", 18, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-300f, 84f), new Vector2(300f, 112f));
            meta.MetaSoulsText.color = new Color(0.69f, 0.52f, 0.96f, 1f);

            // Magaza listesi: dikey layout'lu konteyner
            GameObject listRoot = FindDirectChild(gameOverPanel.transform, "MetaShopListRoot");
            if (listRoot == null)
            {
                listRoot = new GameObject("MetaShopListRoot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(listRoot, "Create Meta Shop List");
                listRoot.layer = gameOverPanel.layer;
                listRoot.transform.SetParent(gameOverPanel.transform, false);
            }
            var listRect = (RectTransform)listRoot.transform;
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.offsetMin = new Vector2(-320f, -250f);
            listRect.offsetMax = new Vector2(320f, 78f);
            var layout = EnsureComponent<UnityEngine.UI.VerticalLayoutGroup>(listRoot);
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
            meta.MetaShopListRoot = listRect;

            // Satir sablonu (inactive; MetaProgressionUI klonlar)
            GameObject template = FindDirectChild(listRoot.transform, "MetaShopRowTemplate");
            if (template == null)
            {
                template = EnsurePanel(listRoot.transform, "MetaShopRowTemplate", false,
                    new Color(0.10f, 0.12f, 0.15f, 0.95f));
                var rowRect = (RectTransform)template.transform;
                rowRect.sizeDelta = new Vector2(640f, 40f);

                EnsureText(template.transform, "RowTitleText", "Upgrade", 15,
                    TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, -16f), new Vector2(280f, 16f));
                EnsureText(template.transform, "RowLevelText", "LV 0/5", 13,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-40f, -14f), new Vector2(50f, 14f));
                EnsureText(template.transform, "RowCostText", "150 SOULS", 13,
                    TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-260f, -14f), new Vector2(-110f, 14f));
                var buy = EnsureButton(template.transform, "RowBuyButton",
                    new Vector2(1f, 0.5f), new Vector2(-100f, -15f), new Vector2(-10f, 15f), out var buyText);
                buyText.text = "BUY";
                buyText.fontSize = 13;
                var colors = buy.colors;
                colors.normalColor = new Color(0.18f, 0.55f, 0.25f, 1f);
                colors.disabledColor = new Color(0.3f, 0.32f, 0.35f, 0.6f);
                buy.colors = colors;
            }
            var layoutElement = EnsureComponent<UnityEngine.UI.LayoutElement>(template);
            layoutElement.preferredHeight = 40f;
            layoutElement.minHeight = 40f;
            template.SetActive(false);
            meta.MetaShopRowTemplate = template;

            EditorUtility.SetDirty(meta);
        }

        /// <summary>Kanli ay gunduz uyarisi (M-C): ust-orta toast text'i + BloodMoonWarningUI controller'i.</summary>
        private static void ConfigureBloodMoonWarning(Transform canvasTransform)
        {
            GameObject root = FindDirectChild(canvasTransform, "BloodMoonWarningRoot");
            if (root == null)
            {
                root = new GameObject("BloodMoonWarningRoot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "Create Blood Moon Warning Root");
                root.layer = canvasTransform.gameObject.layer;
                root.transform.SetParent(canvasTransform, false);
            }
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var warning = EnsureComponent<BloodMoonWarningUI>(root);
            warning.WarningText = EnsureText(root.transform, "BloodMoonWarningText",
                "BLOOD MOON RISES TONIGHT", 30, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-380f, -240f), new Vector2(380f, -150f));
            warning.WarningText.gameObject.SetActive(false);
            EditorUtility.SetDirty(warning);
        }

        private static GameObject EnsureHudRoot(Transform canvasTransform)
        {
            DestroyChildIfExists(canvasTransform, "HUDPanel");
            DestroyChildIfExists(canvasTransform, "MarketPanel");

            EnsureWorkerDrawerTargetControlsInPrefab();
            EnsureArcherRetrainControlInPrefab();
            EnsureArrowAmmoPanelInPrefab();
            EnsureCouncilDecisionUIInPrefab();
            EnsureUnifiedAbilityBarInPrefab();

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

        private static void EnsureWorkerDrawerTargetControlsInPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Worker drawer HUD prefab bulunamadi: " + GeneratedHudPrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(GeneratedHudPrefabPath);
            try
            {
                EnsureWorkerDrawerTargetControls(root);
                // Runtime component sahnede otoriter olarak ekleniyor. Prefabda ikinci bir
                // WorkerEconomyDrawerUI birakmak scene instance'inda cift listener uretir.
                DestroyComponentIfExists<WorkerEconomyDrawerUI>(root);
                PrefabUtility.SaveAsPrefabAsset(root, GeneratedHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureArcherRetrainControlInPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Archer HUD prefab bulunamadi: " + GeneratedHudPrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(GeneratedHudPrefabPath);
            try
            {
                RectTransform template = FindRectTransformByName(root, "ArcherRecruitmentRowTemplate");
                EnsureArcherRetrainTemplateControl(template);
                PrefabUtility.SaveAsPrefabAsset(root, GeneratedHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureArrowAmmoPanelInPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Arrow ammo HUD prefab bulunamadi: " + GeneratedHudPrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(GeneratedHudPrefabPath);
            try
            {
                EnsureArrowAmmoPanel(root);
                // Runtime controller sahnedeki HUD owner'inda tutulur; prefabda ikinci listener olusmaz.
                DestroyComponentIfExists<ArrowSupplyUI>(root);
                PrefabUtility.SaveAsPrefabAsset(root, GeneratedHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureCouncilDecisionUIInPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Council HUD prefab bulunamadi: " + GeneratedHudPrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(GeneratedHudPrefabPath);
            try
            {
                EnsureCouncilDecisionUI(root);
                // Runtime controller sahnedeki HUD owner'inda tutulur; prefab yalniz gorsel truth'tur.
                DestroyComponentIfExists<CouncilEventUI>(root);
                PrefabUtility.SaveAsPrefabAsset(root, GeneratedHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureUnifiedAbilityBarInPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedHudPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Ability bar HUD prefab bulunamadi: " + GeneratedHudPrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(GeneratedHudPrefabPath);
            try
            {
                EnsureUnifiedAbilityBar(root);
                // Runtime controller scene owner'inda tutulur; prefab yalniz gorsel truth'tur.
                DestroyComponentIfExists<SpellCastUI>(root);
                PrefabUtility.SaveAsPrefabAsset(root, GeneratedHudPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureUnifiedAbilityBar(GameObject hudRoot)
        {
            GameObject panel = FindChildByName(hudRoot, "AbilityBarPanel");
            if (panel == null)
                panel = EnsurePanel(hudRoot.transform, "AbilityBarPanel", true,
                    new Color(0.035f, 0.045f, 0.065f, 0.94f));

            SetRect((RectTransform)panel.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-248f, 18f), new Vector2(248f, 108f));
            panel.SetActive(true);

            Button fireball = EnsureAbilityButton(panel.transform, "FireballButton",
                new Vector2(-232f, 10f), new Vector2(-78f, 80f),
                "[1] FIREBALL\nLOCKED", new Color(0.72f, 0.28f, 0.10f, 1f),
                out _, out _);
            Button rally = EnsureAbilityButton(panel.transform, "RallyAbilityButton",
                new Vector2(-72f, 10f), new Vector2(72f, 80f),
                "[2] RALLY\nREADY", new Color(0.20f, 0.48f, 0.72f, 1f),
                out _, out _);
            Button emergency = EnsureAbilityButton(panel.transform, "EmergencyRepairAbilityButton",
                new Vector2(78f, 10f), new Vector2(232f, 80f),
                "[3] REPAIR\nNIGHT ONLY", new Color(0.24f, 0.60f, 0.38f, 1f),
                out _, out _);

            Sprite rounded = EnsureRoundedRectAsset();
            if (rounded != null)
            {
                ApplyRoundedSkin(panel.GetComponent<Image>(), rounded);
                ApplyRoundedSkin(fireball.GetComponent<Image>(), rounded, null, 1.6f);
                ApplyRoundedSkin(rally.GetComponent<Image>(), rounded, null, 1.6f);
                ApplyRoundedSkin(emergency.GetComponent<Image>(), rounded, null, 1.6f);
            }
        }

        private static Button EnsureAbilityButton(
            Transform parent,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax,
            string labelValue,
            Color normalColor,
            out TMP_Text label,
            out Image cooldownFill)
        {
            Button button = EnsureButton(parent, name, new Vector2(0.5f, 0.5f),
                offsetMin, offsetMax, out label);
            label.text = labelValue;
            label.fontSize = 15f;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.22f);
            colors.disabledColor = new Color(0.18f, 0.20f, 0.24f, 0.82f);
            button.colors = colors;

            GameObject fillObject = FindDirectChild(button.transform, name + "CooldownFill");
            if (fillObject == null)
            {
                fillObject = new GameObject(name + "CooldownFill", typeof(RectTransform));
                fillObject.layer = parent.gameObject.layer;
                fillObject.transform.SetParent(button.transform, false);
            }

            Stretch((RectTransform)fillObject.transform);
            cooldownFill = EnsureComponent<Image>(fillObject);
            cooldownFill.color = new Color(0f, 0f, 0f, 0.62f);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Vertical;
            cooldownFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            cooldownFill.fillAmount = 0f;
            cooldownFill.raycastTarget = false;
            label.transform.SetAsLastSibling();
            return button;
        }

        private static void EnsureCouncilDecisionUI(GameObject hudRoot)
        {
            GameObject panel = FindChildByName(hudRoot, "CouncilEventPanel");
            if (panel == null)
                throw new InvalidOperationException("CouncilEventPanel bulunamadi; exact karar UI baglanamadi.");

            TextMeshProUGUI title =
                FindComponentInChildrenByName<TextMeshProUGUI>(panel, "CouncilTitleText");
            if (title != null)
            {
                SetRect(title.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-202f, 87f), new Vector2(100f, 109f));
            }

            TextMeshProUGUI timer = EnsureText(panel.transform, "CouncilTimerText", "DECIDE  35s", 12,
                TextAlignmentOptions.Right, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(108f, 87f), new Vector2(198f, 109f));
            if (title != null)
            {
                timer.font = title.font;
                timer.fontSharedMaterial = title.fontSharedMaterial;
            }
            timer.color = new Color(0.949f, 0.788f, 0.298f, 1f);
            timer.enableWordWrapping = false;
            timer.raycastTarget = false;
        }

        private static void EnsureArrowAmmoPanel(GameObject hudRoot)
        {
            GameObject arrowChip = FindChildByName(hudRoot, "ArrowChip");
            if (arrowChip == null)
                throw new InvalidOperationException("ArrowChip bulunamadi; finite ammo paneli baglanamadi.");

            Button toggle = EnsureComponent<Button>(arrowChip);
            toggle.targetGraphic = arrowChip.GetComponent<Image>();
            toggle.transition = Selectable.Transition.ColorTint;

            GameObject panel = FindChildByName(hudRoot, "AmmoPurchasePanel")
                ?? EnsurePanel(hudRoot.transform, "AmmoPurchasePanel", true,
                    new Color(0.055f, 0.045f, 0.035f, 0.97f));
            SetRect(panel.GetComponent<RectTransform>(),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(28f, -236f), new Vector2(760f, -158f));

            var stock = FindOrCreateText(panel.transform, "AmmoStockText", "ARROWS 200 / 200", 16,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(12f, 8f), new Vector2(170f, -8f));
            stock.fontStyle = FontStyles.Bold;
            stock.textWrappingMode = TextWrappingModes.NoWrap;
            stock.enableAutoSizing = true;
            stock.fontSizeMin = 10f;
            stock.fontSizeMax = 16f;
            var efficiencyText = FindOrCreateText(panel.transform, "AmmoEfficiencyText", "4 / WOOD", 12,
                TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(174f, 8f), new Vector2(240f, -8f));
            efficiencyText.textWrappingMode = TextWrappingModes.NoWrap;
            efficiencyText.enableAutoSizing = true;
            efficiencyText.fontSizeMin = 9f;
            efficiencyText.fontSizeMax = 12f;

            Button package = EnsureAmmoButton(panel.transform, "AmmoPackageButton",
                new Vector2(244f, 8f), new Vector2(326f, -8f), "BUY +100\n25W",
                new Color(0.43f, 0.29f, 0.12f, 1f));
            Button large = EnsureAmmoButton(panel.transform, "AmmoLargePackageButton",
                new Vector2(330f, 8f), new Vector2(412f, -8f), "BUY x5 +500\n125W",
                new Color(0.49f, 0.34f, 0.14f, 1f));
            Button buyMax = EnsureAmmoButton(panel.transform, "AmmoBuyMaxButton",
                new Vector2(416f, 8f), new Vector2(506f, -8f), "BUY MAX\n50W",
                new Color(0.60f, 0.40f, 0.13f, 1f));
            Button capacity = EnsureAmmoButton(panel.transform, "AmmoCapacityUpgradeButton",
                new Vector2(510f, 8f), new Vector2(612f, -8f), "CAP +200 L0\n150W + 25I",
                new Color(0.15f, 0.38f, 0.52f, 1f));
            Button efficiency = EnsureAmmoButton(panel.transform, "AmmoEfficiencyUpgradeButton",
                new Vector2(616f, 8f), new Vector2(720f, -8f), "EFF 4>5/W L0\n200W + 50I",
                new Color(0.34f, 0.22f, 0.52f, 1f));

            package.navigation = Navigation.defaultNavigation;
            large.navigation = Navigation.defaultNavigation;
            buyMax.navigation = Navigation.defaultNavigation;
            capacity.navigation = Navigation.defaultNavigation;
            efficiency.navigation = Navigation.defaultNavigation;
            panel.SetActive(false);
        }

        private static Button EnsureAmmoButton(Transform parent, string name,
            Vector2 offsetMin, Vector2 offsetMax, string label, Color color)
        {
            Button button = FindComponentInChildrenByName<Button>(parent.gameObject, name)
                ?? EnsureButton(parent, name, new Vector2(0f, 0f), offsetMin, offsetMax, out _);
            RectTransform rect = button.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), offsetMin, offsetMax);
            if (button.targetGraphic is Image image)
                image.color = color;

            SetButtonLabel(button, label);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = 7f;
                text.fontSizeMax = 11f;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.alignment = TextAlignmentOptions.Center;
            }

            return button;
        }

        private static void EnsureWorkerDrawerTargetControls(GameObject hudRoot)
        {
            GameObject panelObject = FindChildByName(hudRoot, "WorkerEconomyDrawerPanel");
            if (panelObject == null)
                throw new InvalidOperationException("WorkerEconomyDrawerPanel bulunamadi.");

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(24f, 160f);
            panelRect.sizeDelta = new Vector2(980f, 382f);

            Button toggle = FindComponentInChildrenByName<Button>(hudRoot, "WorkerDrawerToggleButton");
            if (toggle != null)
            {
                RectTransform toggleRect = toggle.GetComponent<RectTransform>();
                toggleRect.anchorMin = Vector2.zero;
                toggleRect.anchorMax = Vector2.zero;
                toggleRect.pivot = Vector2.zero;
                toggleRect.anchoredPosition = new Vector2(24f, 28f);
                toggleRect.sizeDelta = new Vector2(206f, 56f);
                SetButtonLabel(toggle, "WORKERS + HOUSING");
            }

            TextMeshProUGUI title = FindComponentInChildrenByName<TextMeshProUGUI>(
                panelObject, "WorkerDrawerTitleText");
            if (title != null)
            {
                title.rectTransform.anchoredPosition = new Vector2(-88f, 0f);
                title.rectTransform.sizeDelta = new Vector2(220f, 24f);
                title.text = "WORKERS + HOUSING";
            }

            string[] prefixes = { "Wood", "Stone", "Iron", "Food" };
            foreach (string prefix in prefixes)
                EnsureWorkerDrawerTargetRow(panelObject, prefix);

            EnsureWorkerHousingRow(panelObject);
        }

        private static void EnsureWorkerHousingRow(GameObject panelObject)
        {
            GameObject sourceRow = FindChildByName(panelObject, "FoodWorkerRow");
            var capacitySource = FindComponentInChildrenByName<TextMeshProUGUI>(
                panelObject, "FoodWorkerCountText");
            var availabilitySource = FindComponentInChildrenByName<TextMeshProUGUI>(
                panelObject, "FoodWorkerRateText");
            var purchasedSource = FindComponentInChildrenByName<TextMeshProUGUI>(
                panelObject, "FoodWorkerStatusText");
            var buttonSource = FindComponentInChildrenByName<Button>(
                panelObject, "FoodCapacityUpgradeButton");
            if (sourceRow == null || capacitySource == null || availabilitySource == null
                || purchasedSource == null || buttonSource == null)
            {
                throw new InvalidOperationException("Housing satiri icin worker UI stil kaynaklari bulunamadi.");
            }

            GameObject rowObject = FindChildByName(panelObject, "HousingRow");
            if (rowObject == null)
            {
                rowObject = Instantiate(sourceRow, panelObject.transform);
                rowObject.name = "HousingRow";
                for (int i = rowObject.transform.childCount - 1; i >= 0; i--)
                    DestroyImmediate(rowObject.transform.GetChild(i).gameObject);
            }

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.anchoredPosition = new Vector2(0f, -146f);
            rowRect.sizeDelta = new Vector2(956f, 44f);
            rowRect.localScale = Vector3.one;
            if (rowObject.GetComponent<Image>() is Image rowImage)
                rowImage.color = new Color(0.10f, 0.145f, 0.16f, 0.96f);

            TextMeshProUGUI capacity = EnsureWorkerHousingText(rowObject.transform,
                "HousingCapacityText", capacitySource, "HOUSING 60/60", -390f, 160f,
                new Color(0.91f, 0.925f, 0.945f, 1f), TextAlignmentOptions.Left);
            TextMeshProUGUI availability = EnsureWorkerHousingText(rowObject.transform,
                "HousingAvailabilityText", availabilitySource, "FREE 0", -245f, 110f,
                new Color(0.349f, 0.765f, 0.416f, 1f), TextAlignmentOptions.Center);
            TextMeshProUGUI purchased = EnsureWorkerHousingText(rowObject.transform,
                "HousingPurchasedText", purchasedSource, "BOUGHT +0", -125f, 120f,
                new Color(0.596f, 0.635f, 0.678f, 1f), TextAlignmentOptions.Center);

            EnsureWorkerHousingButton(rowObject.transform, "HousingBuyOneButton", buttonSource,
                "+1 BED\n100W", 50f);
            EnsureWorkerHousingButton(rowObject.transform, "HousingBuyTenButton", buttonSource,
                "+10 BEDS\n1,000W", 210f);
            EnsureWorkerHousingButton(rowObject.transform, "HousingBuyHundredButton", buttonSource,
                "+100 BEDS\nCOST", 370f);

            capacity.raycastTarget = false;
            availability.raycastTarget = false;
            purchased.raycastTarget = false;
        }

        private static TextMeshProUGUI EnsureWorkerHousingText(Transform row, string name,
            TextMeshProUGUI styleSource, string label, float x, float width, Color color,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = FindComponentInChildrenByName<TextMeshProUGUI>(row.gameObject, name);
            if (text == null)
            {
                GameObject clone = Instantiate(styleSource.gameObject, row);
                clone.name = name;
                text = clone.GetComponent<TextMeshProUGUI>();
            }

            SetWorkerDrawerControlRect(text.rectTransform, x, width);
            text.text = label;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureWorkerHousingButton(Transform row, string name,
            Button styleSource, string label, float x)
        {
            Button button = FindComponentInChildrenByName<Button>(row.gameObject, name);
            if (button == null)
            {
                GameObject clone = Instantiate(styleSource.gameObject, row);
                clone.name = name;
                button = clone.GetComponent<Button>();
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.navigation = Navigation.defaultNavigation;
            SetWorkerDrawerUpgradeRect(button, x);
            SetButtonLabel(button, label);
            if (button.targetGraphic is Image image)
                image.color = new Color(0.78f, 0.52f, 0.16f, 1f);
            return button;
        }

        private static void EnsureWorkerDrawerTargetRow(GameObject panelObject, string prefix)
        {
            GameObject rowObject = FindChildByName(panelObject, prefix + "WorkerRow");
            var countText = FindComponentInChildrenByName<TextMeshProUGUI>(panelObject, prefix + "WorkerCountText");
            var rateText = FindComponentInChildrenByName<TextMeshProUGUI>(panelObject, prefix + "WorkerRateText");
            var statusText = FindComponentInChildrenByName<TextMeshProUGUI>(panelObject, prefix + "WorkerStatusText");
            var plus1Button = FindComponentInChildrenByName<Button>(panelObject, prefix + "WorkerAddButton");
            if (rowObject == null || countText == null || rateText == null || statusText == null || plus1Button == null)
                throw new InvalidOperationException(prefix + " worker row gerekli legacy kontrolleri icermiyor.");

            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(956f, rowRect.sizeDelta.y);

            Button plus10Button = FindComponentInChildrenByName<Button>(panelObject,
                prefix + "WorkerTargetPlus10Button");
            if (plus10Button == null)
            {
                GameObject clone = Instantiate(plus1Button.gameObject, rowObject.transform);
                clone.name = prefix + "WorkerTargetPlus10Button";
                plus10Button = clone.GetComponent<Button>();
            }

            Button plus100Button = FindComponentInChildrenByName<Button>(panelObject,
                prefix + "WorkerTargetPlus100Button");
            if (plus100Button == null)
            {
                GameObject clone = Instantiate(plus1Button.gameObject, rowObject.transform);
                clone.name = prefix + "WorkerTargetPlus100Button";
                plus100Button = clone.GetComponent<Button>();
            }

            TMP_InputField targetInput = FindComponentInChildrenByName<TMP_InputField>(panelObject,
                prefix + "WorkerTargetInput");
            if (targetInput == null)
                targetInput = CreateWorkerTargetInput(rowObject.transform, prefix, statusText);

            Button capacityButton = FindComponentInChildrenByName<Button>(panelObject,
                prefix + "CapacityUpgradeButton");
            if (capacityButton == null)
            {
                GameObject clone = Instantiate(plus1Button.gameObject, rowObject.transform);
                clone.name = prefix + "CapacityUpgradeButton";
                capacityButton = clone.GetComponent<Button>();
            }

            Button efficiencyButton = FindComponentInChildrenByName<Button>(panelObject,
                prefix + "EfficiencyUpgradeButton");
            if (efficiencyButton == null)
            {
                GameObject clone = Instantiate(plus1Button.gameObject, rowObject.transform);
                clone.name = prefix + "EfficiencyUpgradeButton";
                efficiencyButton = clone.GetComponent<Button>();
            }

            SetButtonLabel(plus1Button, "+1%");
            SetButtonLabel(plus10Button, "+10%");
            SetButtonLabel(plus100Button, "+100%");
            SetButtonLabel(capacityButton, "CAP L0\n100W 25I");
            SetButtonLabel(efficiencyButton, "EFF L0\n150W 50I");

            SetWorkerDrawerControlRect(countText.rectTransform, -420f, 105f);
            SetWorkerDrawerControlRect(rateText.rectTransform, -315f, 90f);
            SetWorkerDrawerControlRect(statusText.rectTransform, -220f, 105f);
            SetWorkerDrawerControlRect(targetInput.GetComponent<RectTransform>(), -127f, 70f);
            SetWorkerDrawerControlRect(plus1Button.GetComponent<RectTransform>(), -65f, 52f);
            SetWorkerDrawerControlRect(plus10Button.GetComponent<RectTransform>(), -3f, 62f);
            SetWorkerDrawerControlRect(plus100Button.GetComponent<RectTransform>(), 78f, 82f);
            SetWorkerDrawerUpgradeRect(capacityButton, 205f);
            SetWorkerDrawerUpgradeRect(efficiencyButton, 370f);
        }

        private static TMP_InputField CreateWorkerTargetInput(Transform row, string prefix,
            TextMeshProUGUI styleSource)
        {
            var inputObject = new GameObject(prefix + "WorkerTargetInput", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            inputObject.transform.SetParent(row, false);
            inputObject.layer = row.gameObject.layer;
            var image = inputObject.GetComponent<Image>();
            image.color = new Color(0.035f, 0.055f, 0.075f, 0.96f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(inputObject.transform, false);
            textObject.layer = row.gameObject.layer;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            CopyWorkerTargetInputTextStyle(text, styleSource, 16f, Color.white);

            var placeholderObject = new GameObject("Placeholder", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            placeholderObject.transform.SetParent(inputObject.transform, false);
            placeholderObject.layer = row.gameObject.layer;
            var placeholderRect = placeholderObject.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(4f, 2f);
            placeholderRect.offsetMax = new Vector2(-4f, -2f);
            var placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
            CopyWorkerTargetInputTextStyle(placeholder, styleSource, 13f,
                new Color(1f, 1f, 1f, 0.35f));
            placeholder.text = "0-100";

            var input = inputObject.AddComponent<TMP_InputField>();
            input.textViewport = inputObject.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.DecimalNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 6;
            input.targetGraphic = image;
            input.selectionColor = new Color(0.18f, 0.62f, 0.86f, 0.65f);
            return input;
        }

        private static void CopyWorkerTargetInputTextStyle(TextMeshProUGUI target,
            TextMeshProUGUI source, float fontSize, Color color)
        {
            if (source != null)
            {
                target.font = source.font;
                target.fontSharedMaterial = source.fontSharedMaterial;
            }

            target.fontSize = fontSize;
            target.color = color;
            target.alignment = TextAlignmentOptions.Center;
            target.textWrappingMode = TextWrappingModes.NoWrap;
            target.raycastTarget = false;
        }

        private static void SetWorkerDrawerControlRect(RectTransform rect, float x, float width)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 36f);
            rect.localScale = Vector3.one;
        }

        private static void SetWorkerDrawerUpgradeRect(Button button, float x)
        {
            SetWorkerDrawerControlRect(button.GetComponent<RectTransform>(), x, 145f);
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
                return;

            text.fontSize = 12f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = 12f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
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
            ConfigureArrowAmmo(hudRoot);

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
            EnsureArcherRetrainTemplateControl(market.ArcherRecruitmentRowTemplate);
            HidePlayerFacingPrepButtons(market);
            HidePlayerFacingArcherProgressionControls(market);

            ConfigureTechTree(hudRoot);
            ConfigureUnifiedAbilityBar(hudRoot);
            ConfigureDefenseRepair(hudRoot);
            ConfigureDawnToast(hudRoot);
            ConfigureCouncilUI(hudRoot);
        }

        private static void ConfigureArrowAmmo(GameObject hudRoot)
        {
            EnsureArrowAmmoPanel(hudRoot);
            var controller = EnsureComponent<ArrowSupplyUI>(hudRoot);
            GameObject arrowChip = FindChildByName(hudRoot, "ArrowChip");
            controller.ToggleButton = arrowChip != null ? arrowChip.GetComponent<Button>() : null;
            controller.AmmoPanel = FindChildByName(hudRoot, "AmmoPurchasePanel");
            controller.StockText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "AmmoStockText");
            controller.EfficiencyText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "AmmoEfficiencyText");
            controller.PackageButton = FindComponentInChildrenByName<Button>(hudRoot, "AmmoPackageButton");
            controller.LargePackageButton = FindComponentInChildrenByName<Button>(hudRoot, "AmmoLargePackageButton");
            controller.BuyMaxButton = FindComponentInChildrenByName<Button>(hudRoot, "AmmoBuyMaxButton");
            controller.CapacityUpgradeButton =
                FindComponentInChildrenByName<Button>(hudRoot, "AmmoCapacityUpgradeButton");
            controller.EfficiencyUpgradeButton =
                FindComponentInChildrenByName<Button>(hudRoot, "AmmoEfficiencyUpgradeButton");
            controller.StartOpen = false;
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
            council.CouncilTimerText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CouncilTimerText");
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

        private static void ConfigureUnifiedAbilityBar(GameObject hudRoot)
        {
            EnsureUnifiedAbilityBar(hudRoot);
            var abilityBar = EnsureComponent<SpellCastUI>(hudRoot);
            abilityBar.SpellPanel = FindChildByName(hudRoot, "AbilityBarPanel");
            abilityBar.FireballButton = FindComponentInChildrenByName<Button>(hudRoot, "FireballButton");
            abilityBar.FireballLabelText =
                FindComponentInChildrenByName<TextMeshProUGUI>(abilityBar.FireballButton.gameObject, "Text");
            abilityBar.FireballCooldownFill =
                FindComponentInChildrenByName<Image>(abilityBar.FireballButton.gameObject, "FireballButtonCooldownFill");
            abilityBar.RallyButton = FindComponentInChildrenByName<Button>(hudRoot, "RallyAbilityButton");
            abilityBar.RallyLabelText =
                FindComponentInChildrenByName<TextMeshProUGUI>(abilityBar.RallyButton.gameObject, "Text");
            abilityBar.RallyCooldownFill =
                FindComponentInChildrenByName<Image>(abilityBar.RallyButton.gameObject, "RallyAbilityButtonCooldownFill");
            abilityBar.EmergencyRepairButton =
                FindComponentInChildrenByName<Button>(hudRoot, "EmergencyRepairAbilityButton");
            abilityBar.EmergencyRepairLabelText = FindComponentInChildrenByName<TextMeshProUGUI>(
                abilityBar.EmergencyRepairButton.gameObject, "Text");
            abilityBar.EmergencyRepairCooldownFill = FindComponentInChildrenByName<Image>(
                abilityBar.EmergencyRepairButton.gameObject,
                "EmergencyRepairAbilityButtonCooldownFill");
            ConfigureFireballVisuals(abilityBar);
            EditorUtility.SetDirty(abilityBar);
        }

        private static void EnsureActiveAbilityTuning()
        {
            DifficultyProfileSO profile = EnsureDefaultDifficultyProfile();
            if (profile.NormalRepairHealPercent <= 0f)
            {
                profile.NormalRepairHealPercent = 0.25f;
            }
            if (profile.RepairStonePerMissingHp <= 0f)
            {
                profile.RepairStonePerMissingHp = 0.10f;
            }
            if (profile.RepairDayPriceMultiplier <= 0f)
            {
                profile.RepairDayPriceMultiplier = 1f;
            }
            if (profile.RallyCooldown <= 0f)
            {
                profile.RallyCooldown = 60f;
            }
            if (profile.EmergencyRepairHealPercent <= 0f)
            {
                profile.EmergencyRepairHealPercent = 0.20f;
            }
            if (profile.EmergencyRepairCooldown <= 0f)
            {
                profile.EmergencyRepairCooldown = 120f;
            }

            // Yeni serialize alanlari mevcut asset'e de yazilsin; mevcut pozitif owner
            // tuning'i korunur, yalniz eksik/sifir alanlar default ile tamamlanir.
            EditorUtility.SetDirty(profile);
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
        /// Generated Castle Heart ekranini HUD root'a baglar. Eski TechTreeUI aktif owner olarak
        /// birakilmaz; mevcut prefab iskeleti Heart presentation/purchase controller'i tarafindan
        /// kullanilir. Eksik header kontrolleri yalniz scene fallback senaryosunda uretilir.
        /// </summary>
        private static void ConfigureTechTree(GameObject hudRoot)
        {
            var legacy = hudRoot.GetComponent<TechTreeUI>();
            if (legacy != null)
                DestroyImmediate(legacy);

            var heart = EnsureComponent<HeartScreenUI>(hudRoot);

            var panel = FindChildByName(hudRoot, "CastleHeartPanel")
                ?? FindChildByName(hudRoot, "TechTreePanel");
            if (panel == null)
                panel = EnsureFallbackTechTreePanel(hudRoot.transform);
            heart.HeartPanel = panel;

            heart.HeartOpenButton = FindComponentInChildrenByName<Button>(hudRoot, "CastleHeartOpenButton")
                ?? FindComponentInChildrenByName<Button>(hudRoot, "TechTreeOpenButton");
            if (heart.HeartOpenButton == null)
            {
                heart.HeartOpenButton = EnsureButton(hudRoot.transform, "CastleHeartOpenButton",
                    new Vector2(0f, 1f), new Vector2(232f, -168f), new Vector2(358f, -130f), out _);
            }
            SetButtonLabel(heart.HeartOpenButton, "HEART");

            heart.HeartCloseButton = FindComponentInChildrenByName<Button>(hudRoot, "CastleHeartCloseButton")
                ?? FindComponentInChildrenByName<Button>(hudRoot, "TechTreeCloseButton");
            heart.HeartViewport = FindRectTransformByName(hudRoot, "HeartViewport")
                ?? FindRectTransformByName(hudRoot, "TechTreeViewport");
            heart.HeartContent = FindRectTransformByName(hudRoot, "HeartContent")
                ?? FindRectTransformByName(hudRoot, "TechTreeContent");
            heart.HeartNodeTemplate = FindRectTransformByName(hudRoot, "HeartNodeTemplate")
                ?? FindRectTransformByName(hudRoot, "TechNodeTemplate");
            heart.HeartConnectionTemplate = FindRectTransformByName(hudRoot, "HeartConnectionTemplate")
                ?? FindRectTransformByName(hudRoot, "TechConnectionTemplate");

            if (heart.HeartNodeTemplate != null)
                heart.HeartNodeTemplate.gameObject.SetActive(false);
            if (heart.HeartConnectionTemplate != null)
                heart.HeartConnectionTemplate.gameObject.SetActive(false);

            if (heart.HeartViewport != null)
            {
                var scroll = heart.HeartViewport.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    scroll.movementType = ScrollRect.MovementType.Elastic;
                    scroll.elasticity = 0.08f;
                    scroll.inertia = true;
                    scroll.decelerationRate = 0.15f;
                    scroll.scrollSensitivity = 0f;
                }
                EnsureComponent<TechTreeViewController>(heart.HeartViewport.gameObject);
            }

            if (heart.HeartPanel != null)
                EnsureComponent<CanvasGroup>(heart.HeartPanel);

            heart.AffordableBadge = FindChildByName(hudRoot, "CastleHeartBadge")
                ?? FindChildByName(hudRoot, "TechTreeBadge");
            if (heart.AffordableBadge == null && heart.HeartOpenButton != null)
            {
                var badgeGo = EnsureChild(heart.HeartOpenButton.transform, "CastleHeartBadge", true);
                var badgeRect = badgeGo.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.anchoredPosition = new Vector2(-4f, -2f);
                badgeRect.sizeDelta = new Vector2(14f, 14f);
                var badgeImage = EnsureComponent<Image>(badgeGo);
                badgeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                badgeImage.color = new Color(0.949f, 0.788f, 0.298f, 1f);
                badgeImage.raycastTarget = false;
                heart.AffordableBadge = badgeGo;
            }
            if (heart.AffordableBadge != null)
                heart.AffordableBadge.SetActive(false);

            var title = FindComponentInChildrenByName<TextMeshProUGUI>(panel, "TechTreeTitleText")
                ?? FindComponentInChildrenByName<TextMeshProUGUI>(panel, "CastleHeartTitleText");
            if (title != null)
                title.text = "CASTLE HEART";

            heart.GraveEssenceText = FindComponentInChildrenByName<TextMeshProUGUI>(panel, "GraveEssenceText");
            if (heart.GraveEssenceText == null)
            {
                heart.GraveEssenceText = EnsureText(panel.transform, "GraveEssenceText", "GRAVE ESSENCE  0", 18,
                    TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-470f, -62f), new Vector2(-28f, -22f));
            }

            heart.ScreenStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(panel, "HeartScreenStatusText");
            if (heart.ScreenStatusText == null)
            {
                heart.ScreenStatusText = EnsureText(panel.transform, "HeartScreenStatusText", string.Empty, 12,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(-390f, -88f), new Vector2(390f, -66f));
            }

            heart.BranchCompassText = FindComponentInChildrenByName<TextMeshProUGUI>(panel, "HeartBranchCompassText");
            if (heart.BranchCompassText == null)
            {
                heart.BranchCompassText = EnsureText(panel.transform, "HeartBranchCompassText", string.Empty, 12,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(-520f, 20f), new Vector2(520f, 46f));
            }

            heart.QuantityOneButton = FindComponentInChildrenByName<Button>(panel, "HeartQuantityOneButton")
                ?? EnsureButton(panel.transform, "HeartQuantityOneButton", new Vector2(1f, 1f),
                    new Vector2(-378f, -118f), new Vector2(-298f, -82f), out _);
            heart.QuantityTenButton = FindComponentInChildrenByName<Button>(panel, "HeartQuantityTenButton")
                ?? EnsureButton(panel.transform, "HeartQuantityTenButton", new Vector2(1f, 1f),
                    new Vector2(-290f, -118f), new Vector2(-210f, -82f), out _);
            heart.QuantityMaxButton = FindComponentInChildrenByName<Button>(panel, "HeartQuantityMaxButton")
                ?? EnsureButton(panel.transform, "HeartQuantityMaxButton", new Vector2(1f, 1f),
                    new Vector2(-202f, -118f), new Vector2(-112f, -82f), out _);
            SetButtonLabel(heart.QuantityOneButton, "+1");
            SetButtonLabel(heart.QuantityTenButton, "+10");
            SetButtonLabel(heart.QuantityMaxButton, "MAX");

            var toast = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "CastleHeartToastText")
                ?? FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "TechTreeToastText");
            if (toast == null)
            {
                toast = EnsureText(hudRoot.transform, "CastleHeartToastText", "HEART AWAKENED", 18,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-220f, 258f), new Vector2(232f, 292f));
                toast.raycastTarget = false;
            }
            heart.ToastText = toast;

            heart.BuyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechBuySfxPath);
            heart.RevealClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechRevealSfxPath);
            heart.DeniedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechDeniedSfxPath);
            heart.PanelOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TechPanelOpenSfxPath);

            if (heart.HeartPanel != null)
                heart.HeartPanel.SetActive(false);
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
            hud.DefenseWallFill = FindComponentInChildrenByName<Image>(hudRoot, "DefenseWallFill");
            hud.DefenseDamageGlow = FindComponentInChildrenByName<Image>(hudRoot, "DefenseDamageGlow");

            ConfigureDefenseFillImage(hud.DefenseWallFill);

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
            hud.CycleCelestialArc = FindRectTransformByName(hudRoot, "CycleProgressTrack");
            hud.CycleCelestialGlow = FindComponentInChildrenByName<Image>(hudRoot, "CycleCelestialGlow");

            bool hasCelestialDial = hud.CycleCelestialArc != null && hud.CycleCelestialGlow != null;
            if (hasCelestialDial)
                ConfigureCelestialDialLayout(hud);
            else
                ConfigureCycleProgressLayout(hud.CycleProgressFill, hud.CycleProgressMarker);

            if (!hasCelestialDial && hud.CyclePhaseText != null)
                hud.CyclePhaseText.text = "DAY";
            if (!hasCelestialDial && hud.CycleDayLabelText != null)
                hud.CycleDayLabelText.text = "DAY";
            if (!hasCelestialDial && hud.CycleDuskLabelText != null)
                hud.CycleDuskLabelText.text = "DUSK";
            if (!hasCelestialDial && hud.CycleNightLabelText != null)
                hud.CycleNightLabelText.text = "NIGHT";
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

        private static void ConfigureCelestialDialLayout(HUDController hud)
        {
            SetOptionalComponentActive(hud.CyclePhaseText, false);
            SetOptionalComponentActive(hud.CycleDayLabelText, false);
            SetOptionalComponentActive(hud.CycleDuskLabelText, false);
            SetOptionalComponentActive(hud.CycleNightLabelText, false);
            SetOptionalComponentActive(hud.CycleProgressFill, false);

            if (hud.CyclePanel != null)
            {
                RectTransform panelRect = hud.CyclePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 1f);
                    panelRect.anchorMax = new Vector2(0.5f, 1f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.anchoredPosition = new Vector2(0f, -68f);
                    panelRect.sizeDelta = new Vector2(290f, 68f);
                }

                GameObject divider = FindChildByName(hud.CyclePanel, "CycleDayDivider");
                if (divider != null)
                    divider.SetActive(false);
            }

            if (hud.CycleDayCounterText != null)
            {
                RectTransform dayRect = hud.CycleDayCounterText.rectTransform;
                dayRect.anchorMin = new Vector2(0.5f, 0.5f);
                dayRect.anchorMax = new Vector2(0.5f, 0.5f);
                dayRect.pivot = new Vector2(0.5f, 0.5f);
                dayRect.anchoredPosition = new Vector2(-102f, 0f);
                dayRect.sizeDelta = new Vector2(54f, 24f);
                hud.CycleDayCounterText.enableAutoSizing = false;
                hud.CycleDayCounterText.fontSize = 11f;
                hud.CycleDayCounterText.alignment = TextAlignmentOptions.MidlineLeft;
                hud.CycleDayCounterText.raycastTarget = false;
            }

            if (hud.CycleCelestialArc != null)
            {
                hud.CycleCelestialArc.anchorMin = new Vector2(0.5f, 0.5f);
                hud.CycleCelestialArc.anchorMax = new Vector2(0.5f, 0.5f);
                hud.CycleCelestialArc.pivot = new Vector2(0.5f, 0.5f);
                hud.CycleCelestialArc.anchoredPosition = new Vector2(22f, -1f);
                hud.CycleCelestialArc.sizeDelta = new Vector2(178f, 44f);
                Image arcBackground = hud.CycleCelestialArc.GetComponent<Image>();
                if (arcBackground != null)
                {
                    arcBackground.color = Color.clear;
                    arcBackground.raycastTarget = false;
                }
            }

            if (hud.CycleProgressMarker != null)
            {
                hud.CycleProgressMarker.anchorMin = new Vector2(0.5f, 0.5f);
                hud.CycleProgressMarker.anchorMax = new Vector2(0.5f, 0.5f);
                hud.CycleProgressMarker.pivot = new Vector2(0.5f, 0.5f);
                hud.CycleProgressMarker.sizeDelta = new Vector2(8f, 8f);
                Image markerImage = hud.CycleProgressMarker.GetComponent<Image>();
                if (markerImage != null)
                    markerImage.raycastTarget = false;
            }

            if (hud.CycleCelestialGlow != null)
            {
                hud.CycleCelestialGlow.rectTransform.sizeDelta = new Vector2(24f, 24f);
                hud.CycleCelestialGlow.raycastTarget = false;
            }
        }

        private static void SetOptionalComponentActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
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
            workerDrawer.HousingCapacityText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HousingCapacityText");
            workerDrawer.HousingAvailabilityText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HousingAvailabilityText");
            workerDrawer.HousingPurchasedText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "HousingPurchasedText");
            workerDrawer.HousingBuyOneButton = FindComponentInChildrenByName<Button>(hudRoot, "HousingBuyOneButton");
            workerDrawer.HousingBuyTenButton = FindComponentInChildrenByName<Button>(hudRoot, "HousingBuyTenButton");
            workerDrawer.HousingBuyHundredButton = FindComponentInChildrenByName<Button>(hudRoot, "HousingBuyHundredButton");

            workerDrawer.WoodWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerCountText");
            workerDrawer.WoodWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerRateText");
            workerDrawer.WoodWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "WoodWorkerAddButton");
            workerDrawer.WoodWorkerTargetPlus10Button = FindComponentInChildrenByName<Button>(hudRoot, "WoodWorkerTargetPlus10Button");
            workerDrawer.WoodWorkerTargetPlus100Button = FindComponentInChildrenByName<Button>(hudRoot, "WoodWorkerTargetPlus100Button");
            workerDrawer.WoodWorkerTargetInput = FindComponentInChildrenByName<TMP_InputField>(hudRoot, "WoodWorkerTargetInput");
            workerDrawer.WoodWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "WoodWorkerStatusText");
            workerDrawer.WoodCapacityUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "WoodCapacityUpgradeButton");
            workerDrawer.WoodEfficiencyUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "WoodEfficiencyUpgradeButton");

            workerDrawer.StoneWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerCountText");
            workerDrawer.StoneWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerRateText");
            workerDrawer.StoneWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "StoneWorkerAddButton");
            workerDrawer.StoneWorkerTargetPlus10Button = FindComponentInChildrenByName<Button>(hudRoot, "StoneWorkerTargetPlus10Button");
            workerDrawer.StoneWorkerTargetPlus100Button = FindComponentInChildrenByName<Button>(hudRoot, "StoneWorkerTargetPlus100Button");
            workerDrawer.StoneWorkerTargetInput = FindComponentInChildrenByName<TMP_InputField>(hudRoot, "StoneWorkerTargetInput");
            workerDrawer.StoneWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "StoneWorkerStatusText");
            workerDrawer.StoneCapacityUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "StoneCapacityUpgradeButton");
            workerDrawer.StoneEfficiencyUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "StoneEfficiencyUpgradeButton");

            workerDrawer.IronWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerCountText");
            workerDrawer.IronWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerRateText");
            workerDrawer.IronWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "IronWorkerAddButton");
            workerDrawer.IronWorkerTargetPlus10Button = FindComponentInChildrenByName<Button>(hudRoot, "IronWorkerTargetPlus10Button");
            workerDrawer.IronWorkerTargetPlus100Button = FindComponentInChildrenByName<Button>(hudRoot, "IronWorkerTargetPlus100Button");
            workerDrawer.IronWorkerTargetInput = FindComponentInChildrenByName<TMP_InputField>(hudRoot, "IronWorkerTargetInput");
            workerDrawer.IronWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "IronWorkerStatusText");
            workerDrawer.IronCapacityUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "IronCapacityUpgradeButton");
            workerDrawer.IronEfficiencyUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "IronEfficiencyUpgradeButton");

            workerDrawer.FoodWorkerCountText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerCountText");
            workerDrawer.FoodWorkerRateText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerRateText");
            workerDrawer.FoodWorkerAddButton = FindComponentInChildrenByName<Button>(hudRoot, "FoodWorkerAddButton");
            workerDrawer.FoodWorkerTargetPlus10Button = FindComponentInChildrenByName<Button>(hudRoot, "FoodWorkerTargetPlus10Button");
            workerDrawer.FoodWorkerTargetPlus100Button = FindComponentInChildrenByName<Button>(hudRoot, "FoodWorkerTargetPlus100Button");
            workerDrawer.FoodWorkerTargetInput = FindComponentInChildrenByName<TMP_InputField>(hudRoot, "FoodWorkerTargetInput");
            workerDrawer.FoodWorkerStatusText = FindComponentInChildrenByName<TextMeshProUGUI>(hudRoot, "FoodWorkerStatusText");
            workerDrawer.FoodCapacityUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "FoodCapacityUpgradeButton");
            workerDrawer.FoodEfficiencyUpgradeButton = FindComponentInChildrenByName<Button>(hudRoot, "FoodEfficiencyUpgradeButton");

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

        private static void EnsureArcherRetrainTemplateControl(RectTransform rowTemplate)
        {
            if (rowTemplate == null || rowTemplate.Find("ArcherRetrainButton") != null)
                return;

            Button buyButton = FindComponentInChildrenByName<Button>(
                rowTemplate.gameObject, "ArcherBuyButton");
            if (buyButton == null)
                return;

            GameObject retrainObject = UnityEngine.Object.Instantiate(buyButton.gameObject, rowTemplate);
            retrainObject.name = "ArcherRetrainButton";
            Undo.RegisterCreatedObjectUndo(retrainObject, "Create Archer Retrain Button");

            RectTransform rect = retrainObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(95f, 0f);
            rect.sizeDelta = new Vector2(110f, 60f);

            var image = retrainObject.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.20f, 0.46f, 0.74f, 1f);

            var label = retrainObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.gameObject.name = "ArcherRetrainButtonText";
                label.text = "RETRAIN";
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 13f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            retrainObject.SetActive(true);
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
