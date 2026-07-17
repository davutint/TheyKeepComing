using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Zorluk ayar merkezi: DifficultyProfileSO'yu bolumlu bir panelde duzenler, sahneye (bake)
    /// ve play modda CANLI uygular, olcum botunu ayni panelden kosturur ve son olcumun
    /// olum-gunu dagilimini mini histogramla gosterir. Detay: DIFFICULTY_TUNER_ARCHITECTURE.md.
    /// </summary>
    public class DifficultyTunerWindow : EditorWindow
    {
        private const string DefaultArcherCatalogPath =
            "Assets/ScriptableObject/MobileCastle/Archers/ArcherRecruitmentCatalog.asset";
        private const string DefaultCouncilCatalogPath =
            "Assets/ScriptableObject/MobileCastle/Council/CouncilEventCatalog.asset";
        private const string MobileCastleCombatSubScenePath =
            "Assets/Scenes/NewGameScene/MobileCastleCombatSubScene.unity";

        private DifficultyProfileSO _profile;
        private SerializedObject _profileSO;
        private ArcherRecruitmentCatalogSO _fallbackArcherCatalog;
        private CouncilEventCatalogSO _fallbackCouncilCatalog;
        private Vector2 _scroll;

        private bool _foldCurves = true;
        private bool _foldEscalation = true;
        private bool _foldIntensity;
        private bool _foldSpawnContract = true;
        private bool _foldRepair = true;
        private bool _foldEconomyPrices = true;
        private bool _foldPopulation = true;
        private bool _foldArchers = true;
        private bool _foldHeart = true;
        private bool _foldCouncil = true;
        private bool _foldFuture;
        private bool _foldBot = true;

        private float _botTimeScale = 3f;
        private int _botTargetDay = 20;
        private bool _botAutoRestart = true;
        private int _spawnPreviewDay = 1;
        private float _wallPreviewMissingPercent = 0.50f;
        private int _economyPreviewLevel;
        private int _populationPreviewCurrentPopulation = 60;
        private int _populationPreviewPurchasedBeds = 15;
        private int _populationPreviewFood = 30;
        private int _archerPreviewTargetTypeCount = 25;
        private int _arrowPreviewCurrent;
        private int _arrowPreviewCapacityLevel;
        private int _arrowPreviewEfficiencyLevel;
        private int _arrowPreviewPackageCount = 1;
        private int _arrowPreviewAvailableWood = 100;
        private int _heartPreviewSeed = 1;
        private int _heartPreviewCurrentLevel;
        private long _heartPreviewAvailableEssence = 1000L;
        private long _lastArrowRentCount = -1L;
        private double _lastArrowRentSampleTime;
        private float _observedArrowDrainPerSecond;
        private bool _hasObservedArrowDrainSample;
        private bool _hasCouncilDecisionOwnerSnapshot;
        private float _councilDecisionDawnDuration;
        private float _councilDecisionDayDuration;
        private string _councilDecisionOwnerProblem = string.Empty;
        private double _nextSpawnTelemetryRepaint;

        private List<int> _deaths = new List<int>();
        private int _maxDayReached;
        private string _summaryFile = "";

        private static readonly Color AccentGreen = new Color(0.35f, 0.72f, 0.38f);
        private static readonly Color AccentBlue = new Color(0.32f, 0.55f, 0.85f);
        private static readonly Color BarDeath = new Color(0.85f, 0.35f, 0.30f);
        private static readonly Color BarSurvive = new Color(0.35f, 0.70f, 0.40f);

        [MenuItem("Window/DeadWalls/Difficulty Tuner")]
        public static void ShowWindow()
        {
            var win = GetWindow<DifficultyTunerWindow>("Difficulty Tuner");
            win.minSize = new Vector2(380f, 520f);
        }

        private void OnEnable()
        {
            if (_profile == null)
                _profile = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(
                    MobileCastleSceneSetupWindow.DifficultyProfilePath);
            if (_fallbackArcherCatalog == null)
                _fallbackArcherCatalog = AssetDatabase.LoadAssetAtPath<ArcherRecruitmentCatalogSO>(
                    DefaultArcherCatalogPath);
            if (_fallbackCouncilCatalog == null)
                _fallbackCouncilCatalog = AssetDatabase.LoadAssetAtPath<CouncilEventCatalogSO>(
                    DefaultCouncilCatalogPath);

            _lastArrowRentCount = -1L;
            _lastArrowRentSampleTime = 0d;
            _observedArrowDrainPerSecond = 0f;
            _hasObservedArrowDrainSample = false;
            RefreshCouncilDecisionOwnerSnapshot();

            EditorApplication.update -= RepaintLiveTelemetry;
            EditorApplication.update += RepaintLiveTelemetry;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintLiveTelemetry;
        }

        private void RepaintLiveTelemetry()
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup < _nextSpawnTelemetryRepaint)
                return;

            _nextSpawnTelemetryRepaint = EditorApplication.timeSinceStartup + 0.25d;
            Repaint();
        }

        private void OnGUI()
        {
            DrawStatusBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space(4);

            DrawProfilePicker();
            if (_profile == null)
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_profileSO == null || _profileSO.targetObject != _profile)
                _profileSO = new SerializedObject(_profile);
            _profileSO.Update();

            DrawCurvesSection();
            DrawEscalationSection();
            DrawIntensitySection();
            DrawSpawnContractSection();
            DrawRepairSection();
            DrawEconomyPriceSection();
            DrawPopulationSection();
            DrawArcherSection();
            DrawHeartSection();
            DrawCouncilSection();
            DrawFutureSection();

            _profileSO.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawApplyButton();
            DrawBotSection();
            DrawSummarySection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------
        // Ust durum cubugu + profil secici
        // ---------------------------------------------------------------
        private void DrawStatusBar()
        {
            Rect bar = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, Application.isPlaying
                ? new Color(0.18f, 0.32f, 0.20f)
                : new Color(0.16f, 0.20f, 0.28f));

            string mode = Application.isPlaying ? "PLAY — Apply canli uygular" : "EDIT — Apply bake'e baglar";
            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(bar.x + 8f, bar.y, bar.width - 96f, bar.height),
                "DIFFICULTY TUNER   |   " + mode, style);

            // Owner ayar kilavuzu: "hangi degeri neden degistiririm" (his-tablosu + sozluk)
            if (GUI.Button(new Rect(bar.xMax - 84f, bar.y + 3f, 78f, bar.height - 6f), "Kilavuz"))
            {
                var guide = AssetDatabase.LoadAssetAtPath<Object>("Assets/Docs/DIFFICULTY_TUNING_GUIDE.md");
                if (guide != null)
                {
                    Selection.activeObject = guide;
                    EditorGUIUtility.PingObject(guide);
                    AssetDatabase.OpenAsset(guide);
                }
            }
        }

        private void DrawProfilePicker()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var newProfile = (DifficultyProfileSO)EditorGUILayout.ObjectField(
                        "Profil", _profile, typeof(DifficultyProfileSO), false);
                    if (newProfile != _profile)
                    {
                        _profile = newProfile;
                        _profileSO = null;
                    }

                    if (GUILayout.Button("Default", GUILayout.Width(64f)))
                    {
                        _profile = MobileCastleSceneSetupWindow.EnsureDefaultDifficultyProfile();
                        _profileSO = null;
                    }
                }

                if (_profile == null)
                    EditorGUILayout.HelpBox("Profil sec veya Default olustur.", MessageType.Warning);
            }
        }

        // ---------------------------------------------------------------
        // Bolumler
        // ---------------------------------------------------------------
        private void DrawCurvesSection()
        {
            _foldCurves = DrawSectionHeader(_foldCurves, "Gun Egrileri", "x = GUN, y = CARPAN (1 = etkisiz)");
            if (!_foldCurves)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawBigCurve("NightIntensityByDay", "Gece Siddeti",
                    "Erken oyun rampi: dusuk basla, kacinci gunde 1.0'a cikacagini belirle.");
                DrawBigCurve("ZombieHpMultByDay", "Zombi HP",
                    "V1 quantity-only: dormant legacy egri, runtime zombi HP'sini degistirmez.");
                DrawBigCurve("SpawnBatchMultByDay", "Spawn Batch",
                    "Kalabalik carpani (intensity ve cycle buyumesine EK).");
                EditorGUILayout.PropertyField(_profileSO.FindProperty("SampleDays"));
            }
        }

        private void DrawBigCurve(string propertyName, string label, string hint)
        {
            var prop = _profileSO.FindProperty(propertyName);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(hint, EditorStyles.miniLabel);
            prop.animationCurveValue = EditorGUILayout.CurveField(
                prop.animationCurveValue, AccentBlue,
                new Rect(1f, 0f, Mathf.Max(10, _profile.SampleDays - 1), 2f),
                GUILayout.Height(56f));
            EditorGUILayout.Space(6);
        }

        private void DrawEscalationSection()
        {
            _foldEscalation = DrawSectionHeader(_foldEscalation, "Kutle Eskalasyonu", "HP / batch / tavanlar");
            if (!_foldEscalation)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("ZombieBaseHP");
                DrawProp("ZombieHpGrowthPerCycle");
                DrawProp("ZombieBaseDamage");
                DrawProp("ZombieDamagePerCycle");
                EditorGUILayout.Space(4);
                DrawProp("SpawnBatchSize");
                DrawProp("SpawnBatchGrowthPerCycle");
                DrawProp("MaxSpawnBatch");
                DrawProp("MaxAliveZombies");
                EditorGUILayout.Space(4);
                DrawProp("BaseSpawnInterval");
                DrawProp("MinSpawnInterval");
                EditorGUILayout.HelpBox(
                    "V1 quantity-only: Zombie HP/Damage growth alanlari dormanttir; aktif enemy "
                    + "base stat owner'i EnemyDefinitionSO'dur. Zorluk Spawn Batch/interval ve "
                    + "faz yogunluklariyla ayarlanir.", MessageType.None);
            }
        }

        private void DrawIntensitySection()
        {
            _foldIntensity = DrawSectionHeader(_foldIntensity, "Faz Yogunluklari", "DAY / DUSK / NIGHT / DAWN");
            if (!_foldIntensity)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("DayIntensity");
                DrawProp("DuskStartIntensity");
                DrawProp("DuskEndIntensity");
                DrawProp("NightIntensity");
                DrawProp("DawnIntensity");
            }
        }

        private void DrawSpawnContractSection()
        {
            _foldSpawnContract = DrawSectionHeader(_foldSpawnContract,
                "Spawn Runtime Contract", "day curve / phase / backlog / active cap");
            if (!_foldSpawnContract)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                int sampleDays = Mathf.Clamp(_profile.SampleDays, 1, 200);
                _spawnPreviewDay = EditorGUILayout.IntSlider(
                    "Preview Day", Mathf.Clamp(_spawnPreviewDay, 1, sampleDays), 1, sampleDays);

                DifficultyDaySample sample = MobileCastleTuningResolver.ResolveDaySample(
                    _profile, _spawnPreviewDay);
                EditorGUILayout.LabelField("BaseSpawn day curve",
                    $"x{sample.SpawnBatchMult:0.###} quantity");
                EditorGUILayout.LabelField("Night/Dusk-end day curve",
                    $"x{sample.NightIntensityMult:0.###} intensity");
                EditorGUILayout.LabelField("Phase multipliers",
                    $"DAY x{_profile.DayIntensity:0.###}  |  DUSK x{_profile.DuskStartIntensity:0.###}->x{_profile.DuskEndIntensity:0.###}  |  NIGHT x{_profile.NightIntensity:0.###}  |  DAWN x{_profile.DawnIntensity:0.###}");
                EditorGUILayout.LabelField("Demand / drain",
                    $"batch {_profile.SpawnBatchSize}, cycle +{_profile.SpawnBatchGrowthPerCycle:P0}, max {_profile.MaxSpawnBatch}/frame");
                EditorGUILayout.LabelField("Active cap", _profile.MaxAliveZombies.ToString("N0"));

                EditorGUILayout.HelpBox(
                    "Backlog bir designer secenegi degildir: V1 PreserveDemand politikasi cap "
                    + "doluyken talebi PendingEnemies icinde exact korur. MaxAliveZombies sahadaki "
                    + "active tavani, MaxSpawnBatch ise kapasite acilinca frame basina drain tavanidir.",
                    MessageType.Info);

                DrawSpawnRuntimeTelemetry();
            }
        }

        private static void DrawSpawnRuntimeTelemetry()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Live Runtime", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Play Mode'da phase, alive, backlog ve demand/drain telemetrisi burada gorunur.",
                    EditorStyles.miniLabel);
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                EditorGUILayout.LabelField("ECS world henuz hazir degil.", EditorStyles.miniLabel);
                return;
            }

            EntityManager em = world.EntityManager;
            using EntityQuery configQuery = em.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(ContinuousSiegeCycleData),
                typeof(ContinuousSpawnBudgetData));
            using EntityQuery waveQuery = em.CreateEntityQuery(typeof(WaveStateData));
            if (configQuery.CalculateEntityCount() != 1 || waveQuery.CalculateEntityCount() != 1)
            {
                EditorGUILayout.LabelField("Spawn singleton'lari henuz hazir degil.", EditorStyles.miniLabel);
                return;
            }

            Entity configEntity = configQuery.GetSingletonEntity();
            MobileCastleCombatConfig config = em.GetComponentData<MobileCastleCombatConfig>(configEntity);
            ContinuousSiegeCycleData cycle = em.GetComponentData<ContinuousSiegeCycleData>(configEntity);
            ContinuousSpawnBudgetData budget = em.GetComponentData<ContinuousSpawnBudgetData>(configEntity);
            WaveStateData wave = em.GetComponentData<WaveStateData>(waveQuery.GetSingletonEntity());

            EditorGUILayout.LabelField("Phase / day", $"{cycle.Phase} / {Mathf.Max(1, wave.CurrentWave)}");
            EditorGUILayout.LabelField("Alive / active cap",
                $"{Mathf.Max(0, wave.ZombiesAlive):N0} / {Mathf.Max(0, config.MaxAliveZombies):N0}");
            EditorGUILayout.LabelField("Pending backlog", System.Math.Max(0L, budget.PendingEnemies).ToString("N0"));
            EditorGUILayout.LabelField("Last demand / spawn",
                $"{Mathf.Max(0, budget.LastDemandedEnemies):N0} / {Mathf.Max(0, budget.LastSpawnedEnemies):N0}");
            EditorGUILayout.LabelField("Total demand / spawn",
                $"{System.Math.Max(0L, budget.TotalDemandedEnemies):N0} / {System.Math.Max(0L, budget.TotalSpawnedEnemies):N0}");
            EditorGUILayout.LabelField("Live multipliers",
                $"day x{budget.DayQuantityMultiplier:0.###}  |  phase x{budget.PhaseIntensityMultiplier:0.###}");
            EditorGUILayout.LabelField("Demand / interval / drain cap",
                $"{Mathf.Max(0, budget.DemandPerInterval):N0} / {Mathf.Max(0f, budget.EffectiveSpawnInterval):0.###}s / {Mathf.Max(0, config.MaxSpawnBatch):N0}");
        }

        private void DrawRepairSection()
        {
            _foldRepair = DrawSectionHeader(_foldRepair,
                "Wall Runtime Contract", "base HP / Stone / normal / emergency");
            if (!_foldRepair)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Baseline Defense", EditorStyles.boldLabel);
                DrawProp("WallBaseHp");
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Normal Repair — Day / Dusk", EditorStyles.boldLabel);
                DrawProp("NormalRepairHealPercent");
                DrawProp("RepairStonePerMissingHp");
                DrawProp("RepairDayPriceMultiplier");
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Emergency Repair — Night", EditorStyles.boldLabel);
                DrawProp("EmergencyRepairHealPercent");
                DrawProp("EmergencyRepairCooldown");

                EditorGUILayout.Space(6f);
                _wallPreviewMissingPercent = EditorGUILayout.Slider(
                    "Preview missing HP", _wallPreviewMissingPercent, 0.05f, 1f);
                float baseHp = Mathf.Max(1f, _profile.WallBaseHp);
                float previewCurrentHp = baseHp * (1f - _wallPreviewMissingPercent);
                float previewHealHp = SingleWallDefenseRules.GetRepairHealAmount(
                    previewCurrentHp, baseHp, _profile.NormalRepairHealPercent);
                int previewStone = SingleWallDefenseRules.CalculateRepairStoneCost(
                    previewCurrentHp,
                    baseHp,
                    _profile.NormalRepairHealPercent,
                    _profile.RepairStonePerMissingHp,
                    _profile.RepairDayPriceMultiplier);
                EditorGUILayout.LabelField("Baseline package preview",
                    $"+{previewHealHp:0.##} HP / {previewStone:N0} Stone");
                EditorGUILayout.HelpBox(
                    "V1 aktif fiyat = ceil(actual healed HP x Stone/HP x Day price x discounts). "
                    + "RepairBaseWoodCost ve RepairBaseStoneCost yalniz eski serialized content "
                    + "uyumlulugu icin kalir; aktif fiyat owner'i degildir.", MessageType.Info);

                DrawWallRuntimeTelemetry();
            }
        }

        private static void DrawWallRuntimeTelemetry()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Live Runtime", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "Play Mode'da baseline/effective HP ve gercek repair quote burada gorunur.",
                    EditorStyles.miniLabel);
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                EditorGUILayout.LabelField("ECS world henuz hazir degil.", EditorStyles.miniLabel);
                return;
            }

            EntityManager em = world.EntityManager;
            using EntityQuery configQuery = em.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            using EntityQuery wallQuery = em.CreateEntityQuery(typeof(WallSegment));
            if (configQuery.CalculateEntityCount() != 1 || wallQuery.CalculateEntityCount() != 1)
            {
                EditorGUILayout.LabelField("Wall/config singleton'lari henuz hazir degil.", EditorStyles.miniLabel);
                return;
            }

            MobileCastleCombatConfig config = em.GetComponentData<MobileCastleCombatConfig>(
                configQuery.GetSingletonEntity());
            WallSegment wall = em.GetComponentData<WallSegment>(wallQuery.GetSingletonEntity());
            GameManager gm = GameManager.Instance;
            ResourceCost quote = gm != null ? gm.GetRepairCost() : ResourceCost.Zero;

            EditorGUILayout.LabelField("Baseline / effective MaxHP",
                $"{Mathf.Max(0f, config.WallBaseHp):N0} / {Mathf.Max(0f, wall.MaxHP):N0}");
            EditorGUILayout.LabelField("Current HP",
                $"{Mathf.Max(0f, wall.CurrentHP):N0} / {Mathf.Max(0f, wall.MaxHP):N0} ({SingleWallDefenseRules.GetHealthRatio(wall.CurrentHP, wall.MaxHP):P0})");
            EditorGUILayout.LabelField("Normal package / Stone quote",
                $"{Mathf.Clamp01(config.NormalRepairHealPercent):P0} / {Mathf.Max(0, quote.Stone):N0}");
            EditorGUILayout.LabelField("Stone per HP / Day price",
                $"{Mathf.Max(0f, config.RepairStonePerMissingHp):0.###} / x{Mathf.Max(0f, config.RepairDayPriceMultiplier):0.###}");
            EditorGUILayout.LabelField("Normal phase gate",
                gm != null && gm.IsRepairPhaseAvailable() ? "AVAILABLE" : "LOCKED");
            EditorGUILayout.LabelField("Emergency heal / cooldown",
                $"{Mathf.Clamp01(config.EmergencyRepairHealPercent):P0} / {Mathf.Max(0f, config.EmergencyRepairCooldown):0.##}s");
        }

        private void DrawEconomyPriceSection()
        {
            _foldEconomyPrices = DrawSectionHeader(_foldEconomyPrices,
                "Economy Runtime Contract", "base rates + CAP cost + EFF growth");
            if (!_foldEconomyPrices)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Per-Worker Base Production / Min", EditorStyles.boldLabel);
                DrawProp("WoodWorkerProductionPerMin");
                DrawProp("StoneWorkerProductionPerMin");
                DrawProp("IronWorkerProductionPerMin");
                DrawProp("FoodWorkerProductionPerMin");

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Capacity Investment", EditorStyles.boldLabel);
                DrawProp("WorkerCapacityBaseWoodCost");
                DrawProp("WorkerCapacityBaseIronCost");
                EditorGUILayout.LabelField("Effect per level",
                    $"+{MobileWorkerBuildingUpgradeUtility.CapacityPerLevel} worker slots (V1 fixed)");

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Efficiency Investment", EditorStyles.boldLabel);
                DrawProp("WorkerEfficiencyBaseWoodCost");
                DrawProp("WorkerEfficiencyBaseIronCost");
                DrawProp("WorkerEfficiencyPercentPerLevel");
                DrawProp("WorkerBuildingCostGrowthMultiplier");

                EditorGUILayout.Space(6);
                _economyPreviewLevel = EditorGUILayout.IntSlider(
                    "Preview current level", _economyPreviewLevel, 0, 25);
                MobileEconomyPriceTuning previewTuning =
                    MobileCastleTuningResolver.ResolveEconomyPriceTuning(_profile);
                MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                    WorkerBuildingUpgradeType.Capacity,
                    _economyPreviewLevel,
                    previewTuning,
                    out WorkerBuildingUpgradeCost capacityCost);
                MobileWorkerBuildingUpgradeUtility.TryGetCostForLevel(
                    WorkerBuildingUpgradeType.Efficiency,
                    _economyPreviewLevel,
                    previewTuning,
                    out WorkerBuildingUpgradeCost efficiencyCost);
                EditorGUILayout.LabelField("Next CAP / EFF cost",
                    $"{capacityCost.Wood:N0}W + {capacityCost.Iron:N0}I  /  "
                    + $"{efficiencyCost.Wood:N0}W + {efficiencyCost.Iron:N0}I");
                EditorGUILayout.LabelField("Owned CAP / EFF effect",
                    $"+{MobileWorkerBuildingUpgradeUtility.GetCapacityBonus(_economyPreviewLevel):N0} slots  /  "
                    + $"+{MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(_economyPreviewLevel, previewTuning):P0}");
                EditorGUILayout.HelpBox(
                    "Base rates profile-owned'dir. CAP ve EFF her alisveriste Wood + Iron'i "
                    + "tek transaction olarak harcar; iki fiyat da ilgili bina seviyesinde ortak "
                    + "growth carpanini kullanir. EFF bonusu additive'dir, compound olmaz.",
                    MessageType.None);

                DrawLiveEconomyTelemetry();
            }
        }

        private void DrawPopulationSection()
        {
            _foldPopulation = DrawSectionHeader(_foldPopulation,
                "Population Runtime Contract", "Dawn request + Food + bed curve");
            if (!_foldPopulation)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Dawn Arrival", EditorStyles.boldLabel);
                DrawProp("PopulationGrowthPerDayPrep");
                DrawProp("FoodCostPerArrival");

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("House Bed Curve", EditorStyles.boldLabel);
                DrawProp("BedBaseWoodCost");
                DrawProp("BedCostGrowthCapacityInterval");
                EditorGUILayout.LabelField("Initial bed capacity",
                    $"{MobileBedCapacityUtility.DefaultInitialCapacity:N0} (SubScene Authoring baseline)");

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Contract Preview", EditorStyles.boldLabel);
                _populationPreviewCurrentPopulation = Mathf.Max(0,
                    EditorGUILayout.IntField("Current population",
                        _populationPreviewCurrentPopulation));
                _populationPreviewPurchasedBeds = Mathf.Max(0,
                    EditorGUILayout.IntField("Purchased beds",
                        _populationPreviewPurchasedBeds));
                _populationPreviewFood = Mathf.Max(0,
                    EditorGUILayout.IntField("Available Food", _populationPreviewFood));

                var previewBeds = new MobileBedCapacityState
                {
                    BaseCapacity = MobileBedCapacityUtility.DefaultInitialCapacity,
                    PurchasedCapacity = _populationPreviewPurchasedBeds
                };
                int totalBeds = MobileBedCapacityUtility.GetTotalCapacity(previewBeds);
                MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                    _profile.PopulationGrowthPerDayPrep,
                    _populationPreviewCurrentPopulation,
                    totalBeds,
                    _populationPreviewFood,
                    _profile.FoodCostPerArrival);
                MobileEconomyPriceTuning previewTuning =
                    MobileCastleTuningResolver.ResolveEconomyPriceTuning(_profile);
                bool hasOneBedQuote = MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                    previewBeds, 1, previewTuning, out int oneBedCost);
                bool hasTenBedQuote = MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                    previewBeds, 10, previewTuning, out int tenBedCost);

                EditorGUILayout.LabelField("Beds / free space",
                    $"{totalBeds:N0} / {budget.AvailableBedSpace:N0}");
                EditorGUILayout.LabelField("Requested / affordable / accepted",
                    $"{budget.RequestedArrivals:N0} / {budget.AffordableArrivals:N0} / {budget.AcceptedArrivals:N0}");
                EditorGUILayout.LabelField("One-time Food spend",
                    $"{budget.AcceptedArrivals:N0} x {budget.FoodCostPerArrival:N0} = {budget.RequiredFood:N0}");
                EditorGUILayout.LabelField("Next +1 / +10 bed quote",
                    $"{FormatWoodQuote(hasOneBedQuote, oneBedCost)} / "
                    + FormatWoodQuote(hasTenBedQuote, tenBedCost));
                EditorGUILayout.HelpBox(
                    "Dawn kabul formulu min(requested, bos yatak, Food / kisi maliyeti)'dir. "
                    + "Food yalniz kabul edilen survivor icin ayni transaction'da bir kez harcanir. "
                    + "Yatakta hard max yoktur; Wood fiyati toplam sahip olunan yatak sayisiyla "
                    + "quadratic buyur ve bulk alim her yatagin sirali fiyatini toplar.",
                    MessageType.None);

                DrawLivePopulationTelemetry();
            }
        }

        private void DrawArcherSection()
        {
            _foldArchers = DrawSectionHeader(_foldArchers,
                "Archer Runtime Contract", "base stats + buy/retrain + finite Arrow drain");
            if (!_foldArchers)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                ArcherRecruitmentCatalogSO catalog = ResolveArcherCatalog();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Active definition catalog", catalog,
                        typeof(ArcherRecruitmentCatalogSO), false);
                }

                if (catalog == null)
                {
                    EditorGUILayout.HelpBox(
                        "Active GameManager/default ArcherRecruitmentCatalog bulunamadi. "
                        + "Mobile Castle Scene Setup ile catalog binding'ini onar.",
                        MessageType.Error);
                    return;
                }

                ArcherDefinitionSO[] definitions = catalog.GetOrderedDefinitions();
                if (definitions.Length == 0)
                {
                    EditorGUILayout.HelpBox("Catalog definition icermiyor.", MessageType.Error);
                    return;
                }

                EditorGUILayout.HelpBox(
                    "Combat ve buy/retrain alanlari dogrudan aktif ArcherDefinitionSO asset'lerini "
                    + "duzenler; DifficultyProfileSO icine kopyalanmaz. Cost preview gameplay ile "
                    + "ayni target-type count egrisini kullanir.", MessageType.None);
                _archerPreviewTargetTypeCount = EditorGUILayout.IntSlider(
                    "Preview target-type count", _archerPreviewTargetTypeCount,
                    0, ArcherCapacityUtility.MaxTotalArchers);

                for (int i = 0; i < definitions.Length; i++)
                {
                    ArcherDefinitionSO definition = definitions[i];
                    if (definition != null)
                        DrawArcherDefinitionEditor(definition);
                }

                EditorGUILayout.Space(8);
                DrawFiniteArrowContract();
                DrawLiveArcherTelemetry();
            }
        }

        private void DrawHeartSection()
        {
            _foldHeart = DrawSectionHeader(_foldHeart,
                "Heart Runtime Contract", "Essence gate + node cost/growth + rarity/depth");
            if (!_foldHeart)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GameManager gameManager = ResolveGameManagerOwner();
                if (gameManager == null)
                {
                    EditorGUILayout.HelpBox(
                        "Aktif scene'de canonical GameManager Heart owner'i bulunamadi.",
                        MessageType.Error);
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Runtime owner", gameManager,
                        typeof(GameManager), true);
                    EditorGUILayout.ObjectField("Production node catalog", gameManager.HeartCatalog,
                        typeof(HeartNodeCatalogSO), false);
                }

                DrawHeartEssenceGainContract(gameManager);
                DrawHeartGraphSettingsEditor(gameManager);

                HeartNodeCatalogSO catalog = gameManager.HeartCatalog;
                if (catalog == null)
                {
                    EditorGUILayout.HelpBox(
                        "OWNER CONTENT GATE: Production HeartNodeCatalogSO atanmamis. Launch node listesi, "
                        + "base cost/growth, rarity/depth, Keystone ve effect sayilari owner onayi olmadan "
                        + "uretilmez. Tuner legacy TechTree catalog'una fallback yapmaz.",
                        MessageType.Error);
                    DrawLiveHeartTelemetry(gameManager);
                    return;
                }

                DrawHeartCatalogTuning(catalog, gameManager.GetHeartGraphSettingsSnapshot());
                DrawLiveHeartTelemetry(gameManager);
            }
        }

        private void DrawCouncilSection()
        {
            _foldCouncil = DrawSectionHeader(_foldCouncil,
                "Council Runtime Contract", "fixed cadence + effect bands + repeat memory + derived timer");
            if (!_foldCouncil)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GameManager gameManager = ResolveGameManagerOwner();
                CouncilEventCatalogSO catalog = gameManager != null && gameManager.CouncilCatalog != null
                    ? gameManager.CouncilCatalog
                    : _fallbackCouncilCatalog;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Runtime owner", gameManager,
                        typeof(GameManager), true);
                    EditorGUILayout.ObjectField("Production event catalog", catalog,
                        typeof(CouncilEventCatalogSO), false);
                    EditorGUILayout.IntField("First regular day", CouncilRegularSchedule.FirstRegularDay);
                    EditorGUILayout.IntField("Regular interval (days)", CouncilRegularSchedule.IntervalDays);
                }

                EditorGUILayout.HelpBox(
                    "V1 takvimi sabittir: regular Council yalniz Dawn'da Day 3/6/9... gunlerinde bir kez acilir. "
                    + "Emergency Council yoktur; legacy chance, pity ve cooldown alanlari runtime'da dormantdadir.",
                    MessageType.None);

                if (catalog == null)
                {
                    EditorGUILayout.HelpBox(
                        "Production CouncilEventCatalogSO bulunamadi. Mobile Castle Scene Setup ile binding'i onar.",
                        MessageType.Error);
                    DrawCouncilDecisionWindow(gameManager);
                    return;
                }

                DrawCouncilCatalogSettings(catalog);
                DrawCouncilDecisionWindow(gameManager);
                DrawLiveCouncilTelemetry(gameManager);
            }
        }

        private static void DrawCouncilCatalogSettings(CouncilEventCatalogSO catalog)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authored Effect + Memory Contract", EditorStyles.boldLabel);
            var catalogSO = new SerializedObject(catalog);
            catalogSO.Update();
            SerializedProperty bands = catalogSO.FindProperty("EffectBands");
            SerializedProperty recentMemory = catalogSO.FindProperty("RecentTemplateMemory");
            if (bands == null || recentMemory == null)
            {
                EditorGUILayout.HelpBox("Council catalog tuning alanlari bulunamadi.", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawRelativeProp(bands, "SmallMultiplier", "Small multiplier");
            DrawRelativeProp(bands, "FairMultiplier", "Fair multiplier");
            DrawRelativeProp(bands, "GenerousMultiplier", "Generous multiplier");
            DrawRelativeProp(bands, "SmallWeight", "Small weight");
            DrawRelativeProp(bands, "FairWeight", "Fair weight");
            DrawRelativeProp(bands, "GenerousWeight", "Generous weight");
            DrawRelativeProp(bands, "BudgetTolerance", "A/B budget tolerance");
            EditorGUILayout.PropertyField(recentMemory, new GUIContent("Recent template memory"));
            bool changed = EditorGUI.EndChangeCheck();
            catalogSO.ApplyModifiedProperties();
            if (changed)
                EditorUtility.SetDirty(catalog);

            CouncilEffectBandSettings settings = catalog.EffectBands;
            float totalWeight = settings?.GetTotalWeight() ?? 0f;
            string distribution = totalWeight > 0f
                ? $"{settings.SmallWeight / totalWeight:P1} / {settings.FairWeight / totalWeight:P1} / "
                  + $"{settings.GenerousWeight / totalWeight:P1}"
                : "INVALID";
            EditorGUILayout.LabelField("Normalized Small / Fair / Generous", distribution);
            EditorGUILayout.LabelField("Templates / atoms / curated chains",
                $"{(catalog.Templates?.Length ?? 0):N0} / {(catalog.Atoms?.Length ?? 0):N0} / "
                + $"{(catalog.CuratedChains?.Length ?? 0):N0}");

            List<string> problems = catalog.ValidateCatalog();
            if (problems.Count > 0)
                DrawHeartErrors("Council catalog validation failed", problems);
            else
                EditorGUILayout.HelpBox("Production Council catalog valid.", MessageType.Info);

            EditorGUILayout.HelpBox(
                "Bu alanlar dogrudan production CouncilEventCatalogSO asset'ini duzenler; DifficultyProfileSO "
                + "icine kopyalanmaz. Memory azaltilirsa yeni sinir bir sonraki scheduled kart compose edilmeden "
                + "once mevcut recent listeye uygulanir.", MessageType.None);
        }

        private void DrawCouncilDecisionWindow(GameManager gameManager)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Decision Window Owner", EditorStyles.boldLabel);
            MobileCastleCombatAuthoring authoring = FindFirstObjectByType<MobileCastleCombatAuthoring>(
                FindObjectsInactive.Include);
            float dawn = authoring != null
                ? authoring.SiegeDawnDuration
                : _councilDecisionDawnDuration;
            float day = authoring != null
                ? authoring.SiegeDayDuration
                : _councilDecisionDayDuration;
            bool hasOwner = authoring != null || _hasCouncilDecisionOwnerSnapshot;
            float total = CouncilDecisionWindowUtility.GetTotalWindowSeconds(dawn, day);
            if (Application.isPlaying && gameManager != null)
            {
                CouncilRuntimeTuningTelemetry telemetry = gameManager.GetCouncilRuntimeTuningTelemetry();
                dawn = gameManager.ContinuousSiegeCycle.DawnDuration;
                day = gameManager.ContinuousSiegeCycle.DayDuration;
                total = telemetry.TotalDecisionSeconds;
                hasOwner = true;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Cycle owner",
                    Application.isPlaying
                        ? "Live ContinuousSiegeCycleData"
                        : authoring != null
                            ? "Loaded MobileCastleCombatAuthoring"
                            : _hasCouncilDecisionOwnerSnapshot
                                ? MobileCastleCombatSubScenePath
                                : "UNAVAILABLE");
                EditorGUILayout.TextField("Dawn duration", hasOwner ? $"{dawn:0.###}s" : "UNAVAILABLE");
                EditorGUILayout.TextField("Day duration", hasOwner ? $"{day:0.###}s" : "UNAVAILABLE");
                EditorGUILayout.TextField("Total decision seconds", hasOwner ? $"{total:0.###}s" : "UNAVAILABLE");
            }
            if (!Application.isPlaying && GUILayout.Button("Refresh cycle owner snapshot"))
                RefreshCouncilDecisionOwnerSnapshot();
            if (!hasOwner)
                EditorGUILayout.HelpBox(_councilDecisionOwnerProblem, MessageType.Error);
            EditorGUILayout.HelpBox(
                "Karar suresi ayri bir Council timer ayari degildir. Production cycle owner'indaki Dawn + Day "
                + "surelerinden turetilir; Dusk girisinde kart expire olur.", MessageType.None);
        }

        private void RefreshCouncilDecisionOwnerSnapshot()
        {
            _hasCouncilDecisionOwnerSnapshot = false;
            _councilDecisionDawnDuration = 0f;
            _councilDecisionDayDuration = 0f;
            _councilDecisionOwnerProblem = string.Empty;

            MobileCastleCombatAuthoring loaded = FindFirstObjectByType<MobileCastleCombatAuthoring>(
                FindObjectsInactive.Include);
            if (loaded != null)
            {
                _councilDecisionDawnDuration = loaded.SiegeDawnDuration;
                _councilDecisionDayDuration = loaded.SiegeDayDuration;
                _hasCouncilDecisionOwnerSnapshot = true;
                return;
            }

            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string scenePath = Path.Combine(projectRoot,
                    MobileCastleCombatSubScenePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(scenePath))
                {
                    _councilDecisionOwnerProblem = "Mobile Castle combat SubScene asset'i bulunamadi.";
                    return;
                }

                bool hasDawn = false;
                bool hasDay = false;
                foreach (string line in File.ReadLines(scenePath))
                {
                    string trimmed = line.Trim();
                    if (!hasDawn && TryReadSerializedFloat(
                            trimmed, "SiegeDawnDuration:", out float dawn))
                    {
                        _councilDecisionDawnDuration = dawn;
                        hasDawn = true;
                    }
                    else if (!hasDay && TryReadSerializedFloat(
                                 trimmed, "SiegeDayDuration:", out float day))
                    {
                        _councilDecisionDayDuration = day;
                        hasDay = true;
                    }

                    if (hasDawn && hasDay)
                        break;
                }

                _hasCouncilDecisionOwnerSnapshot = hasDawn && hasDay;
                if (!_hasCouncilDecisionOwnerSnapshot)
                    _councilDecisionOwnerProblem =
                        "MobileCastleCombatSubScene serialized Dawn/Day owner alanlari okunamadi.";
            }
            catch (System.Exception exception)
            {
                _councilDecisionOwnerProblem =
                    "Cycle owner snapshot okunamadi: " + exception.Message;
            }
        }

        private static bool TryReadSerializedFloat(
            string line,
            string fieldPrefix,
            out float value)
        {
            value = 0f;
            if (!line.StartsWith(fieldPrefix, System.StringComparison.Ordinal))
                return false;

            string raw = line.Substring(fieldPrefix.Length).Trim();
            return float.TryParse(
                raw,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private static void DrawLiveCouncilTelemetry(GameManager gameManager)
        {
            if (!Application.isPlaying || gameManager == null)
                return;

            CouncilRuntimeTuningTelemetry telemetry = gameManager.GetCouncilRuntimeTuningTelemetry();
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Live Council Aggregate", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Catalog present / valid",
                $"{telemetry.HasCatalog} / {telemetry.CatalogValid}");
            EditorGUILayout.LabelField("Day / phase / last handled",
                $"{telemetry.CurrentDay} / {telemetry.Phase} / {telemetry.LastHandledRegularDay}");
            EditorGUILayout.LabelField("Recent / limit / flags / one-shots",
                $"{telemetry.RecentTemplateCount} / {telemetry.RecentTemplateMemory} / "
                + $"{telemetry.FlagCount} / {telemetry.UsedOneShotCount}");
            EditorGUILayout.LabelField("Active card / A-B budgets",
                telemetry.HasActiveEvent
                    ? $"{telemetry.ActiveTemplateId} / {telemetry.OptionABudgetMinutes:0.###} - "
                      + $"{telemetry.OptionBBudgetMinutes:0.###} min"
                    : "NONE");
            EditorGUILayout.LabelField("Decision remaining / total",
                $"{telemetry.RemainingDecisionSeconds:0.0}s / {telemetry.TotalDecisionSeconds:0.0}s");
            EditorGUILayout.LabelField("Production modifier / expiry",
                $"{telemetry.ProductionModifierResource} x{telemetry.ProductionModifierMultiplier:0.###} / "
                + $"wave {telemetry.ProductionModifierExpiresAfterWave}");
            EditorGUILayout.LabelField("Next-night count / expiry",
                $"x{telemetry.NextNightSpawnMultiplier:0.###} / wave {telemetry.NightSpawnExpiresAfterWave}");
            if (!telemetry.CatalogValid)
                EditorGUILayout.HelpBox(telemetry.CatalogProblem, MessageType.Error);
        }

        private static void DrawHeartEssenceGainContract(GameManager gameManager)
        {
            EditorGUILayout.LabelField("Grave Essence Gain", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Run wallet / spending owner",
                "GraveEssence ECS + GameManager.TrySpendGraveEssenceAtHeart");
            EditorGUILayout.LabelField("Positive grant gate",
                "GameManager.GrantGraveEssence(long)");
            EditorGUILayout.LabelField("Production drop source", "UNCONFIGURED");
            EditorGUILayout.HelpBox(
                "Blueprint Essence drop ve ilk kill/Essence yonunu tanimliyor; fakat drop ihtimali, "
                + "miktari veya cadence sayisi onayli degil. Runtime'da GrantGraveEssence kullanan "
                + "production kill/drop owner'i yoktur. Tuner burada sahte bir per-kill deger uretmez.",
                MessageType.Warning);

            if (!Application.isPlaying)
                return;

            HeartRuntimeTuningTelemetry telemetry = gameManager.GetHeartRuntimeTuningTelemetry();
            EditorGUILayout.LabelField("Live Essence / meta gain",
                $"{telemetry.GraveEssence:N0} / +{telemetry.MetaGainPercent:P2}");
            EditorGUILayout.LabelField("Exact fractional accumulator",
                telemetry.MetaGainAccumulator.ToString("0.######"));
        }

        private static void DrawHeartGraphSettingsEditor(GameManager gameManager)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Future-Run Graph Generation", EditorStyles.boldLabel);
            var owner = new SerializedObject(gameManager);
            owner.Update();
            SerializedProperty settings = owner.FindProperty("heartGraphSettings");
            if (settings == null)
            {
                EditorGUILayout.HelpBox("heartGraphSettings serialized owner'i bulunamadi.",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawRelativeProp(settings, "MinimumBranchDepth", "Minimum branch depth");
            DrawRelativeProp(settings, "MaximumBranchDepth", "Maximum branch depth");
            DrawRelativeProp(settings, "MaximumCrossLinks", "Maximum cross-links");
            DrawRelativeProp(settings, "KeystonePairCount", "Keystone pair count");
            DrawRelativeProp(settings, "MaximumAttempts", "Deterministic attempts");
            DrawRelativeProp(settings, "StandardRarityWeight", "Standard rarity weight");
            DrawRelativeProp(settings, "RareRarityWeight", "Rare rarity weight");
            bool changed = EditorGUI.EndChangeCheck();
            owner.ApplyModifiedProperties();
            if (changed && !Application.isPlaying && gameManager.gameObject.scene.IsValid())
            {
                EditorUtility.SetDirty(gameManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    gameManager.gameObject.scene);
            }

            EditorGUILayout.HelpBox(
                "Bu alanlar yalniz yeni bir run graph'i uretilirken okunur. Aktif veya Continue ile "
                + "restore edilen exact graph reroll edilmez; mevcut node/level/reveal/lock state'i degismez.",
                MessageType.None);
        }

        private void DrawHeartCatalogTuning(
            HeartNodeCatalogSO catalog,
            HeartGraphRuntimeSettings graphSettings)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Authored Node Pool", EditorStyles.boldLabel);
            HeartNodeDefinitionSO[] definitions = catalog.Nodes ?? System.Array.Empty<HeartNodeDefinitionSO>();
            int standardCount = 0;
            int rareCount = 0;
            int repeatableCount = 0;
            int minimumDepth = int.MaxValue;
            int maximumDepth = 0;
            for (int i = 0; i < definitions.Length; i++)
            {
                HeartNodeDefinitionSO definition = definitions[i];
                if (definition == null)
                    continue;
                if (definition.Rarity == HeartNodeRarity.Rare)
                    rareCount++;
                else
                    standardCount++;
                if (definition.IsRepeatable)
                    repeatableCount++;
                minimumDepth = Mathf.Min(minimumDepth, definition.MinimumDepth);
                maximumDepth = Mathf.Max(maximumDepth, definition.MaximumDepth);
            }

            EditorGUILayout.LabelField("Catalog version / nodes / repeatable",
                $"v{catalog.CatalogVersion} / {definitions.Length:N0} / {repeatableCount:N0}");
            EditorGUILayout.LabelField("Standard / Rare definitions",
                $"{standardCount:N0} / {rareCount:N0}");
            EditorGUILayout.LabelField("Authored depth envelope",
                definitions.Length > 0 && minimumDepth != int.MaxValue
                    ? $"{minimumDepth}..{maximumDepth}"
                    : "EMPTY");

            _heartPreviewCurrentLevel = Mathf.Max(0,
                EditorGUILayout.IntField("Cost preview current level", _heartPreviewCurrentLevel));
            _heartPreviewAvailableEssence = System.Math.Max(0L,
                EditorGUILayout.LongField("Cost preview available Essence",
                    _heartPreviewAvailableEssence));

            for (int i = 0; i < definitions.Length; i++)
            {
                HeartNodeDefinitionSO definition = definitions[i];
                if (definition == null)
                {
                    EditorGUILayout.HelpBox($"Catalog Nodes[{i}] bos.", MessageType.Error);
                    continue;
                }
                DrawHeartDefinitionEditor(definition);
            }

            DrawHeartGeneratorPreview(catalog, graphSettings);
        }

        private void DrawHeartDefinitionEditor(HeartNodeDefinitionSO definition)
        {
            using (new EditorGUILayout.VerticalScope("helpbox"))
            {
                EditorGUILayout.LabelField(
                    $"{definition.Title}  [{definition.Branch} / {definition.Type}]",
                    EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Definition owner", definition,
                        typeof(HeartNodeDefinitionSO), false);
                    EditorGUILayout.TextField("Stable Id", definition.Id ?? string.Empty);
                }

                var definitionSO = new SerializedObject(definition);
                definitionSO.Update();
                DrawDefinitionProp(definitionSO, "Rarity", "Rarity");
                DrawDefinitionProp(definitionSO, "MinimumDepth", "Minimum depth");
                DrawDefinitionProp(definitionSO, "MaximumDepth", "Maximum depth");
                DrawDefinitionProp(definitionSO, "BaseGraveEssenceCost", "Base Grave Essence cost");
                DrawDefinitionProp(definitionSO, "CostGrowthPerLevel", "Linear growth per level");
                definitionSO.ApplyModifiedProperties();

                bool hasOne = HeartPurchasePricing.TryGetLevelCost(
                    definition, _heartPreviewCurrentLevel, out long oneCost);
                long tenCost = 0L;
                bool hasTen = definition.IsRepeatable
                              && HeartPurchasePricing.TryGetTotalCost(
                                  definition, _heartPreviewCurrentLevel, 10, out tenCost);
                int affordableLevels = 0;
                long affordableCost = 0L;
                bool hasMax = definition.IsRepeatable
                              && HeartPurchasePricing.TryGetAffordableLevels(
                                  definition,
                                  _heartPreviewCurrentLevel,
                                  _heartPreviewAvailableEssence,
                                  out affordableLevels,
                                  out affordableCost);
                EditorGUILayout.LabelField("Preview +1 / +10",
                    $"{FormatEssenceQuote(hasOne, oneCost)} / "
                    + (definition.IsRepeatable
                        ? FormatEssenceQuote(hasTen, tenCost)
                        : "N/A (single purchase)"));
                EditorGUILayout.LabelField("Preview Buy Max",
                    definition.IsRepeatable
                        ? hasMax
                            ? $"+{affordableLevels:N0} / {affordableCost:N0} GE"
                            : "NOT AFFORDABLE / INVALID"
                        : "N/A (single purchase)");
            }
        }

        private void DrawHeartGeneratorPreview(
            HeartNodeCatalogSO catalog,
            HeartGraphRuntimeSettings settings)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Deterministic Generator Preview", EditorStyles.boldLabel);
            _heartPreviewSeed = Mathf.Max(1,
                EditorGUILayout.IntField("Preview seed", _heartPreviewSeed));

            var catalogErrors = new List<string>();
            catalog.CollectValidationErrors(catalogErrors);
            if (catalogErrors.Count > 0)
            {
                DrawHeartErrors("Catalog validation failed", catalogErrors);
                return;
            }

            bool generated = HeartGraphGenerator.TryGenerate(
                settings.CreateRequest(catalog, (uint)_heartPreviewSeed),
                out GeneratedRunGraph graph,
                out HeartGraphGenerationReport report);
            if (!generated)
            {
                DrawHeartErrors("Graph generation failed", report.Errors);
                return;
            }

            int standardCount = 0;
            int rareCount = 0;
            int purchasedCount = 0;
            List<GeneratedHeartNodeState> nodes = graph.Nodes ?? new List<GeneratedHeartNodeState>();
            for (int i = 0; i < nodes.Count; i++)
            {
                GeneratedHeartNodeState node = nodes[i];
                HeartNodeDefinitionSO definition = node == null ? null : catalog.GetNode(node.NodeId);
                if (definition == null)
                    continue;
                if (definition.Rarity == HeartNodeRarity.Rare)
                    rareCount++;
                else
                    standardCount++;
                if (node.Level > 0)
                    purchasedCount++;
            }

            EditorGUILayout.LabelField("Graph / catalog version",
                $"v{graph.GraphVersion} / v{graph.CatalogVersion}");
            EditorGUILayout.LabelField("Nodes / edges / attempts",
                $"{nodes.Count:N0} / {(graph.Edges?.Count ?? 0):N0} / {report.SuccessfulAttempt:N0}");
            EditorGUILayout.LabelField("Placed Standard / Rare",
                $"{standardCount:N0} / {rareCount:N0}");
            EditorGUILayout.LabelField("Initial purchased nodes", purchasedCount.ToString("N0"));
            EditorGUILayout.HelpBox(
                "Preview production generator ve validator'i aynen kullanir; aktif run state'ine yazmaz.",
                MessageType.Info);
        }

        private static void DrawLiveHeartTelemetry(GameManager gameManager)
        {
            if (!Application.isPlaying)
                return;

            HeartRuntimeTuningTelemetry telemetry = gameManager.GetHeartRuntimeTuningTelemetry();
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Live Heart Aggregate", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Catalog / attempted / ready",
                $"{telemetry.HasCatalog} / {telemetry.RuntimeAttempted} / {telemetry.RuntimeReady}");
            EditorGUILayout.LabelField("Graph v / catalog v / seed",
                $"{telemetry.GraphVersion} / {telemetry.CatalogVersion} / {telemetry.Seed}");
            EditorGUILayout.LabelField("Nodes / edges / revealed",
                $"{telemetry.NodeCount:N0} / {telemetry.EdgeCount:N0} / {telemetry.RevealedNodeCount:N0}");
            EditorGUILayout.LabelField("Purchased / locked",
                $"{telemetry.PurchasedNodeCount:N0} / {telemetry.LockedNodeCount:N0}");
            if (!string.IsNullOrWhiteSpace(telemetry.RuntimeError))
                EditorGUILayout.HelpBox(telemetry.RuntimeError, MessageType.Error);
        }

        private static void DrawRelativeProp(
            SerializedProperty owner,
            string propertyName,
            string label)
        {
            SerializedProperty property = owner.FindPropertyRelative(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label));
        }

        private static string FormatEssenceQuote(bool valid, long value)
        {
            return valid ? $"{System.Math.Max(0L, value):N0} GE" : "INVALID / OVERFLOW";
        }

        private static void DrawHeartErrors(string heading, IReadOnlyList<string> errors)
        {
            int count = errors?.Count ?? 0;
            var message = heading + ".";
            int visibleCount = Mathf.Min(count, 4);
            for (int i = 0; i < visibleCount; i++)
                message += "\n- " + errors[i];
            if (count > visibleCount)
                message += $"\n- ... {count - visibleCount} more";
            EditorGUILayout.HelpBox(message, MessageType.Error);
        }

        private ArcherRecruitmentCatalogSO ResolveArcherCatalog()
        {
            GameManager gameManager = ResolveGameManagerOwner();

            return gameManager != null && gameManager.ArcherCatalog != null
                ? gameManager.ArcherCatalog
                : _fallbackArcherCatalog;
        }

        private static GameManager ResolveGameManagerOwner()
        {
            GameManager gameManager = GameManager.Instance;
            return gameManager != null
                ? gameManager
                : FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        }

        private void DrawArcherDefinitionEditor(ArcherDefinitionSO definition)
        {
            using (new EditorGUILayout.VerticalScope("helpbox"))
            {
                EditorGUILayout.LabelField(
                    $"{definition.DisplayName}  [{definition.Type}]",
                    EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Definition owner", definition,
                        typeof(ArcherDefinitionSO), false);
                    EditorGUILayout.TextField("Required Heart tech", string.IsNullOrWhiteSpace(
                        definition.RequiredTechId) ? "None" : definition.RequiredTechId);
                }

                var definitionSO = new SerializedObject(definition);
                definitionSO.Update();
                EditorGUILayout.LabelField("Base Combat", EditorStyles.miniBoldLabel);
                DrawDefinitionProp(definitionSO, "Damage", "Damage / projectile");
                DrawDefinitionProp(definitionSO, "FireRate", "Fire rate / second");
                DrawDefinitionProp(definitionSO, "Range", "Range");
                DrawDefinitionProp(definitionSO, "SlowDuration", "Slow duration");
                DrawDefinitionProp(definitionSO, "SlowMultiplier", "Slow multiplier");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Recruitment + Retrain", EditorStyles.miniBoldLabel);
                DrawDefinitionProp(definitionSO, "BuyCost", "Buy base cost", true);
                DrawDefinitionProp(definitionSO, "RetrainCost", "Retrain base cost", true);
                DrawDefinitionProp(definitionSO, "PopulationCost", "Population cost");
                DrawDefinitionProp(definitionSO, "CostGrowthInterval", "Growth interval");
                DrawDefinitionProp(definitionSO, "CostGrowthExponent", "Growth exponent");
                definitionSO.ApplyModifiedProperties();

                ResourceCost buyQuote = ArcherRecruitmentCostUtility.GetScaledCost(
                    definition.BuyCost,
                    _archerPreviewTargetTypeCount,
                    definition.CostGrowthInterval,
                    definition.CostGrowthExponent);
                ResourceCost retrainQuote = ArcherRecruitmentCostUtility.GetScaledCost(
                    definition.RetrainCost,
                    _archerPreviewTargetTypeCount,
                    definition.CostGrowthInterval,
                    definition.CostGrowthExponent);
                EditorGUILayout.LabelField("Base DPS",
                    $"{Mathf.Max(0f, definition.Damage) * Mathf.Max(0f, definition.FireRate):0.###}");
                EditorGUILayout.LabelField("Preview buy / retrain",
                    $"{buyQuote.ToDisplayString()} / "
                    + (definition.Type == ArcherType.Basic
                        ? "N/A (retrain target degil)"
                        : retrainQuote.ToDisplayString()));
            }
        }

        private static void DrawDefinitionProp(SerializedObject owner, string propertyName,
            string label, bool includeChildren = false)
        {
            SerializedProperty property = owner.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label), includeChildren);
        }

        private void DrawFiniteArrowContract()
        {
            EditorGUILayout.LabelField("Finite Arrow Supply", EditorStyles.boldLabel);
            DrawProp("ArrowBaseCapacity");
            DrawProp("ArrowCapacityPerLevel");
            DrawProp("ArrowRefillPackageSize");
            DrawProp("ArrowBaseArrowsPerWood");
            DrawProp("ArrowArrowsPerWoodPerEfficiencyLevel");
            DrawProp("ArrowCapacityBaseWoodCost");
            DrawProp("ArrowCapacityBaseIronCost");
            DrawProp("ArrowEfficiencyBaseWoodCost");
            DrawProp("ArrowEfficiencyBaseIronCost");
            DrawProp("ArrowUpgradeCostGrowthMultiplier");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Arrow Contract Preview", EditorStyles.miniBoldLabel);
            _arrowPreviewCapacityLevel = Mathf.Max(0,
                EditorGUILayout.IntField("Capacity level", _arrowPreviewCapacityLevel));
            _arrowPreviewEfficiencyLevel = Mathf.Max(0,
                EditorGUILayout.IntField("Efficiency level", _arrowPreviewEfficiencyLevel));
            _arrowPreviewCurrent = Mathf.Max(0,
                EditorGUILayout.IntField("Current Arrow", _arrowPreviewCurrent));
            _arrowPreviewPackageCount = EditorGUILayout.IntSlider(
                "Refill package count", _arrowPreviewPackageCount, 1, 10);
            _arrowPreviewAvailableWood = Mathf.Max(0,
                EditorGUILayout.IntField("Available Wood", _arrowPreviewAvailableWood));

            MobileEconomyPriceTuning tuning =
                MobileCastleTuningResolver.ResolveEconomyPriceTuning(_profile);
            var supply = new ArrowSupply
            {
                Current = _arrowPreviewCurrent,
                CapacityLevel = _arrowPreviewCapacityLevel,
                EfficiencyLevel = _arrowPreviewEfficiencyLevel
            };
            int capacity = ArrowEconomyUtility.GetCapacity(supply, tuning);
            supply.Current = Mathf.Clamp(supply.Current, 0, capacity);
            bool hasPackage = ArrowEconomyUtility.TryGetPackageQuote(
                supply, tuning, _arrowPreviewPackageCount, out ArrowRefillQuote packageQuote);
            bool hasBuyMax = ArrowEconomyUtility.TryGetBuyMaxQuote(
                supply, tuning, _arrowPreviewAvailableWood, out ArrowRefillQuote buyMaxQuote);
            bool hasCapacityCost = ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Capacity, tuning, out ArrowUpgradeCost capacityCost);
            bool hasEfficiencyCost = ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Efficiency, tuning, out ArrowUpgradeCost efficiencyCost);

            EditorGUILayout.LabelField("Current / capacity / Arrow per Wood",
                $"{supply.Current:N0} / {capacity:N0} / "
                + $"{ArrowEconomyUtility.GetArrowsPerWood(supply, tuning):N0}");
            EditorGUILayout.LabelField($"+{_arrowPreviewPackageCount} package quote",
                hasPackage
                    ? $"{packageQuote.ArrowAmount:N0} Arrow / {packageQuote.WoodCost:N0}W"
                    : "FULL");
            EditorGUILayout.LabelField("Buy Max quote",
                hasBuyMax
                    ? $"{buyMaxQuote.ArrowAmount:N0} Arrow / {buyMaxQuote.WoodCost:N0}W"
                    : "FULL / NEED WOOD");
            EditorGUILayout.LabelField("Next CAP / EFF investment",
                $"{FormatArrowUpgradeCost(hasCapacityCost, capacityCost)} / "
                + FormatArrowUpgradeCost(hasEfficiencyCost, efficiencyCost));
            EditorGUILayout.LabelField("Arrow per successful projectile rent",
                $"{ArcherShootSystem.ArrowCostPerSuccessfulProjectileRent} (V1 fixed, read-only)");
            EditorGUILayout.HelpBox(
                "Arrow yalniz projectile pool rent'i basarili olduktan sonra harcanir. "
                + "Pool bos, hedef yok veya stok 0 ise tuketim olmaz. Refill beklemesizdir; "
                + "hizli okcular ayni surede daha fazla Arrow talep eder.", MessageType.None);
        }

        private void DrawLiveArcherTelemetry()
        {
            if (!Application.isPlaying)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            using EntityQuery tuningQuery = em.CreateEntityQuery(typeof(MobileEconomyPriceTuning));
            using EntityQuery supplyQuery = em.CreateEntityQuery(typeof(ArrowSupply));
            using EntityQuery poolQuery = em.CreateEntityQuery(typeof(ArrowPoolRuntimeData));
            using EntityQuery archerQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<ArcherUnit>() },
                None = new[] { ComponentType.ReadOnly<Prefab>() }
            });
            if (tuningQuery.CalculateEntityCount() != 1
                || supplyQuery.CalculateEntityCount() != 1
                || poolQuery.CalculateEntityCount() != 1)
            {
                EditorGUILayout.HelpBox("Live Archer/Arrow singleton'lari henuz hazir degil.",
                    MessageType.Info);
                return;
            }

            MobileEconomyPriceTuning tuning = MobileEconomyPriceTuningUtility.Sanitize(
                em.GetComponentData<MobileEconomyPriceTuning>(tuningQuery.GetSingletonEntity()));
            ArrowSupply supply = em.GetComponentData<ArrowSupply>(supplyQuery.GetSingletonEntity());
            ArrowPoolRuntimeData pool = em.GetComponentData<ArrowPoolRuntimeData>(
                poolQuery.GetSingletonEntity());
            var aggregates = new ArcherLiveAggregate[3];
            using (Unity.Collections.NativeArray<ArcherUnit> archers =
                   archerQuery.ToComponentDataArray<ArcherUnit>(Unity.Collections.Allocator.Temp))
            {
                for (int i = 0; i < archers.Length; i++)
                {
                    int typeIndex = Mathf.Clamp((int)archers[i].Type, 0, aggregates.Length - 1);
                    aggregates[typeIndex].Add(archers[i]);
                }
            }

            UpdateObservedArrowDrain(pool.TotalRentCount);
            int capacity = ArrowEconomyUtility.GetCapacity(supply, tuning);
            bool hasCapacityCost = ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Capacity, tuning, out ArrowUpgradeCost capacityCost);
            bool hasEfficiencyCost = ArrowEconomyUtility.TryGetUpgradeCost(
                supply, ArrowUpgradeType.Efficiency, tuning, out ArrowUpgradeCost efficiencyCost);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Live Effective Archer + Arrow", EditorStyles.boldLabel);
            DrawLiveArcherType("Basic", aggregates[(int)ArcherType.Basic]);
            DrawLiveArcherType("Rapid", aggregates[(int)ArcherType.Rapid]);
            DrawLiveArcherType("Frost", aggregates[(int)ArcherType.Frost]);
            float maximumShotDemand = aggregates[0].FireRateSum
                + aggregates[1].FireRateSum
                + aggregates[2].FireRateSum;
            EditorGUILayout.LabelField("Effective max shot demand",
                $"{maximumShotDemand:0.##} Arrow/s before target/pool gating");
            EditorGUILayout.LabelField("Observed successful rent drain",
                _hasObservedArrowDrainSample
                    ? $"{_observedArrowDrainPerSecond:0.##} Arrow/s"
                    : "sampling...");
            EditorGUILayout.LabelField("Arrow current / capacity / per Wood",
                $"{Mathf.Max(0, supply.Current):N0} / {capacity:N0} / "
                + $"{ArrowEconomyUtility.GetArrowsPerWood(supply, tuning):N0}");
            EditorGUILayout.LabelField("CAP / EFF level",
                $"L{Mathf.Max(0, supply.CapacityLevel)} / L{Mathf.Max(0, supply.EfficiencyLevel)}");
            EditorGUILayout.LabelField("Next CAP / EFF investment",
                $"{FormatArrowUpgradeCost(hasCapacityCost, capacityCost)} / "
                + FormatArrowUpgradeCost(hasEfficiencyCost, efficiencyCost));
            EditorGUILayout.LabelField("Projectile pool active / available / total rents",
                $"{Mathf.Max(0, pool.ActiveCount):N0} / {Mathf.Max(0, pool.AvailableCount):N0} / "
                + $"{System.Math.Max(0L, pool.TotalRentCount):N0}");
        }

        private void UpdateObservedArrowDrain(long totalRentCount)
        {
            double now = EditorApplication.timeSinceStartup;
            if (_lastArrowRentCount < 0L || totalRentCount < _lastArrowRentCount)
            {
                _lastArrowRentCount = totalRentCount;
                _lastArrowRentSampleTime = now;
                _hasObservedArrowDrainSample = false;
                return;
            }

            double elapsed = now - _lastArrowRentSampleTime;
            if (elapsed < 0.20d)
                return;

            long rentDelta = totalRentCount - _lastArrowRentCount;
            _observedArrowDrainPerSecond = elapsed > 0d
                ? (float)(rentDelta * ArcherShootSystem.ArrowCostPerSuccessfulProjectileRent / elapsed)
                : 0f;
            _hasObservedArrowDrainSample = true;
            _lastArrowRentCount = totalRentCount;
            _lastArrowRentSampleTime = now;
        }

        private static void DrawLiveArcherType(string label, in ArcherLiveAggregate aggregate)
        {
            float averageDamage = aggregate.Count > 0 ? aggregate.DamageSum / aggregate.Count : 0f;
            float averageFireRate = aggregate.Count > 0 ? aggregate.FireRateSum / aggregate.Count : 0f;
            EditorGUILayout.LabelField(label,
                $"x{aggregate.Count:N0} | avg {averageDamage:0.###} dmg x "
                + $"{averageFireRate:0.###}/s | {aggregate.DpsSum:0.##} DPS");
        }

        private static string FormatArrowUpgradeCost(bool valid, in ArrowUpgradeCost cost)
        {
            return valid ? $"{cost.Wood:N0}W + {cost.Iron:N0}I" : "INT LIMIT";
        }

        private struct ArcherLiveAggregate
        {
            public int Count;
            public float DamageSum;
            public float FireRateSum;
            public float DpsSum;

            public void Add(in ArcherUnit archer)
            {
                Count++;
                DamageSum += Mathf.Max(0f, archer.ArrowDamage);
                FireRateSum += Mathf.Max(0f, archer.FireRate);
                DpsSum += Mathf.Max(0f, archer.ArrowDamage) * Mathf.Max(0f, archer.FireRate);
            }
        }

        private static string FormatWoodQuote(bool valid, int woodCost)
        {
            return valid ? $"{Mathf.Max(0, woodCost):N0}W" : "INT LIMIT";
        }

        private static void DrawLivePopulationTelemetry()
        {
            if (!Application.isPlaying)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            using EntityQuery configQuery = em.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(MobileEconomyPriceTuning),
                typeof(MobileBedCapacityState),
                typeof(MobilePopulationAllocation));
            using EntityQuery stateQuery = em.CreateEntityQuery(
                typeof(PopulationState), typeof(ResourceData));
            if (configQuery.CalculateEntityCount() != 1 || stateQuery.CalculateEntityCount() != 1)
            {
                EditorGUILayout.HelpBox("Live population singleton'lari henuz hazir degil.",
                    MessageType.Info);
                return;
            }

            Entity configEntity = configQuery.GetSingletonEntity();
            Entity stateEntity = stateQuery.GetSingletonEntity();
            MobileCastleCombatConfig config =
                em.GetComponentData<MobileCastleCombatConfig>(configEntity);
            MobileEconomyPriceTuning tuning = MobileEconomyPriceTuningUtility.Sanitize(
                em.GetComponentData<MobileEconomyPriceTuning>(configEntity));
            MobileBedCapacityState beds =
                em.GetComponentData<MobileBedCapacityState>(configEntity);
            MobilePopulationAllocation allocation =
                em.GetComponentData<MobilePopulationAllocation>(configEntity);
            PopulationState population = em.GetComponentData<PopulationState>(stateEntity);
            ResourceData resources = em.GetComponentData<ResourceData>(stateEntity);
            int totalBeds = MobileBedCapacityUtility.GetTotalCapacity(beds);
            MobilePopulationArrivalBudget budget = MobilePopulationArrivalUtility.CalculateBudget(
                config.PopulationGrowthPerDayPrep,
                population.Total,
                totalBeds,
                resources.Food,
                config.FoodCostPerArrival);
            bool hasOneBedQuote = MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                beds, 1, tuning, out int oneBedCost);
            bool hasTenBedQuote = MobileBedCapacityUtility.TryGetPurchaseWoodCost(
                beds, 10, tuning, out int tenBedCost);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Live Next-Dawn Budget", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Requested / Food each",
                $"{budget.RequestedArrivals:N0} / {budget.FoodCostPerArrival:N0}");
            EditorGUILayout.LabelField("Population / beds / free",
                $"{Mathf.Max(0, population.Total):N0} / {totalBeds:N0} / {budget.AvailableBedSpace:N0}");
            EditorGUILayout.LabelField("Food / affordable / accepted",
                $"{Mathf.Max(0, resources.Food):N0} / {budget.AffordableArrivals:N0} / {budget.AcceptedArrivals:N0}");
            EditorGUILayout.LabelField("Predicted one-time Food spend",
                $"{budget.RequiredFood:N0}");
            EditorGUILayout.LabelField("Last Dawn requested / accepted / spent",
                $"{Mathf.Max(0, allocation.LastArrivalRequestedCount):N0} / "
                + $"{Mathf.Max(0, allocation.LastArrivalAcceptedCount):N0} / "
                + $"{Mathf.Max(0, allocation.LastArrivalFoodCost):N0}");
            EditorGUILayout.LabelField("Base / purchased beds",
                $"{Mathf.Max(0, beds.BaseCapacity):N0} / {Mathf.Max(0, beds.PurchasedCapacity):N0}");
            EditorGUILayout.LabelField("Next +1 / +10 bed quote",
                $"{FormatWoodQuote(hasOneBedQuote, oneBedCost)} / "
                + FormatWoodQuote(hasTenBedQuote, tenBedCost));
            EditorGUILayout.LabelField("Bed curve base / interval",
                $"{tuning.BedBaseWoodCost:N0}W / {tuning.BedCostGrowthCapacityInterval:N0} owned beds");
        }

        private void DrawLiveEconomyTelemetry()
        {
            if (!Application.isPlaying)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            EntityManager em = world.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                typeof(MobileCastleCombatConfig),
                typeof(MobileEconomyPriceTuning),
                typeof(MobilePopulationAllocation),
                typeof(MobileWorkerBuildingUpgradeState));
            if (query.CalculateEntityCount() != 1)
            {
                EditorGUILayout.HelpBox("Live worker economy singleton henuz hazir degil.",
                    MessageType.Info);
                return;
            }

            Entity entity = query.GetSingletonEntity();
            MobileCastleCombatConfig config = em.GetComponentData<MobileCastleCombatConfig>(entity);
            MobileEconomyPriceTuning tuning = MobileEconomyPriceTuningUtility.Sanitize(
                em.GetComponentData<MobileEconomyPriceTuning>(entity));
            MobilePopulationAllocation allocation =
                em.GetComponentData<MobilePopulationAllocation>(entity);
            MobileWorkerBuildingUpgradeState upgrades =
                em.GetComponentData<MobileWorkerBuildingUpgradeState>(entity);
            GameManager gm = GameManager.Instance;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Live Effective Economy", EditorStyles.boldLabel);
            DrawEconomyResourceTelemetry("Wood", EconomyFocusType.Wood,
                allocation.WoodWorkers, config.WoodWorkerCap,
                _profile.WoodWorkerProductionPerMin, config.WoodWorkerProductionPerMin,
                upgrades, tuning, gm);
            DrawEconomyResourceTelemetry("Stone", EconomyFocusType.Stone,
                allocation.StoneWorkers, config.StoneWorkerCap,
                _profile.StoneWorkerProductionPerMin, config.StoneWorkerProductionPerMin,
                upgrades, tuning, gm);
            DrawEconomyResourceTelemetry("Iron", EconomyFocusType.Iron,
                allocation.IronWorkers, config.IronWorkerCap,
                _profile.IronWorkerProductionPerMin, config.IronWorkerProductionPerMin,
                upgrades, tuning, gm);
            DrawEconomyResourceTelemetry("Food", EconomyFocusType.Food,
                allocation.FoodWorkers, config.FoodWorkerCap,
                _profile.FoodWorkerProductionPerMin, config.FoodWorkerProductionPerMin,
                upgrades, tuning, gm);
        }

        private static void DrawEconomyResourceTelemetry(string label,
            EconomyFocusType resource, int workers, int effectiveCap, float profileBaseRate,
            float effectiveRate, in MobileWorkerBuildingUpgradeState upgrades,
            in MobileEconomyPriceTuning tuning, GameManager gameManager)
        {
            int capacityLevel = MobileWorkerBuildingUpgradeUtility.GetLevel(
                upgrades, resource, WorkerBuildingUpgradeType.Capacity);
            int efficiencyLevel = MobileWorkerBuildingUpgradeUtility.GetLevel(
                upgrades, resource, WorkerBuildingUpgradeType.Efficiency);
            MobileWorkerBuildingUpgradeUtility.TryGetNextCost(
                upgrades, resource, WorkerBuildingUpgradeType.Capacity, tuning,
                out WorkerBuildingUpgradeCost capacityCost);
            MobileWorkerBuildingUpgradeUtility.TryGetNextCost(
                upgrades, resource, WorkerBuildingUpgradeType.Efficiency, tuning,
                out WorkerBuildingUpgradeCost efficiencyCost);
            float totalPerMin = gameManager != null
                ? gameManager.GetWorkerProductionRate(resource)
                : Mathf.Max(0, workers) * Mathf.Max(0f, effectiveRate);

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Workers / effective cap",
                $"{Mathf.Max(0, workers):N0} / {Mathf.Max(0, effectiveCap):N0}");
            EditorGUILayout.LabelField("Profile base / effective / total",
                $"{Mathf.Max(0f, profileBaseRate):0.###} / {Mathf.Max(0f, effectiveRate):0.###} / {Mathf.Max(0f, totalPerMin):0.##} per min");
            EditorGUILayout.LabelField("CAP level / next",
                $"L{capacityLevel} / {capacityCost.Wood:N0}W + {capacityCost.Iron:N0}I");
            EditorGUILayout.LabelField("EFF level / bonus / next",
                $"L{efficiencyLevel} / +{MobileWorkerBuildingUpgradeUtility.GetEfficiencyBonusPercent(efficiencyLevel, tuning):P0} / "
                + $"{efficiencyCost.Wood:N0}W + {efficiencyCost.Iron:N0}I");
        }

        private void DrawFutureSection()
        {
            _foldFuture = DrawSectionHeader(_foldFuture, "M-C Hazirlik", "spawn tablosu + ozel geceler (sistem henuz okumaz)");
            if (!_foldFuture)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.HelpBox("Veri iskeleti: zombi cesitliligi (M-C) gelince sistem bu tablolara baglanacak. Simdiden doldurabilirsin.", MessageType.None);
                EditorGUILayout.PropertyField(_profileSO.FindProperty("SpawnTable"), true);
                EditorGUILayout.PropertyField(_profileSO.FindProperty("SpecialNights"), true);
            }
        }

        private void DrawProp(string name)
        {
            EditorGUILayout.PropertyField(_profileSO.FindProperty(name));
        }

        private static bool DrawSectionHeader(bool folded, string title, string subtitle)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                folded = EditorGUILayout.Foldout(folded, title, true, EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel, GUILayout.MaxWidth(240f));
            }
            return folded;
        }

        // ---------------------------------------------------------------
        // Uygulama + bot + ozet
        // ---------------------------------------------------------------
        private void DrawApplyButton()
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = AccentGreen;
            string label = Application.isPlaying
                ? "APPLY  —  sahneye bagla + CANLI uygula"
                : "APPLY  —  sahneye bagla (bake)";
            if (GUILayout.Button(label, GUILayout.Height(32f)))
                ApplyProfile();
            GUI.backgroundColor = prev;
        }

        private void DrawBotSection()
        {
            _foldBot = DrawSectionHeader(_foldBot, "Olcum Botu", "Long Run Simulator");
            if (!_foldBot)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _botTimeScale = EditorGUILayout.Slider("Time Scale", _botTimeScale, 1f, 5f);
                _botTargetDay = EditorGUILayout.IntField("Hedef Gun", _botTargetDay);
                _botAutoRestart = EditorGUILayout.Toggle("GameOver'da yeni kosu", _botAutoRestart);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = AccentBlue;
                    if (GUILayout.Button("RUN BOT  —  profili uygula + temiz kosu", GUILayout.Height(28f)))
                    {
                        ApplyProfile();
                        var gm = GameManager.Instance;
                        if (gm != null)
                        {
                            gm.RestartGame();
                            Time.timeScale = 1f; // StartRun kendi carpanini kurar
                        }
                        LongRunSimulatorWindow.OpenAndStart(_botTimeScale, _botTargetDay, _botAutoRestart);
                    }
                    GUI.backgroundColor = prev;
                }

                if (!Application.isPlaying)
                    EditorGUILayout.HelpBox("Bot icin once Play'e gir.", MessageType.Info);
            }
        }

        private void DrawSummarySection()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Son Olcum", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Yenile", GUILayout.Width(64f)))
                        LoadLatestSummary();
                }

                if (string.IsNullOrEmpty(_summaryFile))
                {
                    EditorGUILayout.LabelField("Henuz olcum yuklenmedi.", EditorStyles.miniLabel);
                    return;
                }

                EditorGUILayout.LabelField(_summaryFile, EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "olumler: " + (_deaths.Count == 0 ? "yok" : string.Join(", ", _deaths))
                    + "    en yuksek gun: " + _maxDayReached);

                DrawDeathHistogram();
            }
        }

        /// <summary>Olum gunlerini 1..N ekseninde mini histogram olarak cizer (kirmizi=olum, yesil=ulasilan uc).</summary>
        private void DrawDeathHistogram()
        {
            int axisMax = Mathf.Max(_maxDayReached, _botTargetDay, 10);
            Rect area = GUILayoutUtility.GetRect(0f, 46f, GUILayout.ExpandWidth(true));
            area = new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 18f);
            EditorGUI.DrawRect(area, new Color(0f, 0f, 0f, 0.25f));

            var counts = new Dictionary<int, int>();
            int maxCount = 1;
            foreach (var d in _deaths)
            {
                int c;
                counts.TryGetValue(d, out c);
                counts[d] = c + 1;
                maxCount = Mathf.Max(maxCount, c + 1);
            }

            float slot = area.width / axisMax;
            foreach (var kv in counts)
            {
                float h = area.height * (kv.Value / (float)maxCount);
                var r = new Rect(area.x + (kv.Key - 1) * slot + 1f, area.yMax - h, Mathf.Max(2f, slot - 2f), h);
                EditorGUI.DrawRect(r, BarDeath);
            }

            if (_maxDayReached > 0 && !counts.ContainsKey(_maxDayReached))
            {
                var r = new Rect(area.x + (_maxDayReached - 1) * slot + 1f, area.y, Mathf.Max(2f, slot - 2f), area.height);
                EditorGUI.DrawRect(r, new Color(BarSurvive.r, BarSurvive.g, BarSurvive.b, 0.45f));
            }

            var axisStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft };
            GUI.Label(new Rect(area.x, area.yMax + 1f, 60f, 14f), "gun 1", axisStyle);
            axisStyle.alignment = TextAnchor.UpperRight;
            GUI.Label(new Rect(area.xMax - 60f, area.yMax + 1f, 60f, 14f), "gun " + axisMax, axisStyle);
        }

        // ---------------------------------------------------------------
        // Is mantigi (gorsel revizyonda DEGISMEDI)
        // ---------------------------------------------------------------
        private void ApplyProfile()
        {
            if (_profile == null)
                return;

            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();

            var authoring = FindFirstObjectByType<MobileCastleCombatAuthoring>(FindObjectsInactive.Include);
            if (authoring != null && authoring.Profile != _profile)
            {
                Undo.RecordObject(authoring, "Assign Difficulty Profile");
                authoring.Profile = _profile;
                EditorUtility.SetDirty(authoring);
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(authoring.gameObject.scene);
            }

            if (Application.isPlaying)
                ApplyProfileLive(_profile);
        }

        private static void ApplyProfileLive(DifficultyProfileSO p)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            if (query.CalculateEntityCount() == 0)
                return;

            var entity = query.GetSingletonEntity();
            var config = em.GetComponentData<MobileCastleCombatConfig>(entity);
            MobileCastleTuningResolver.ApplyDifficultyProfile(ref config, p);
            em.SetComponentData(entity, config);

            var economyPriceTuning = MobileCastleTuningResolver.ResolveEconomyPriceTuning(p);
            if (em.HasComponent<MobileEconomyPriceTuning>(entity))
                em.SetComponentData(entity, economyPriceTuning);
            else
                em.AddComponentData(entity, economyPriceTuning);

            var buffer = em.HasBuffer<DifficultyDaySample>(entity)
                ? em.GetBuffer<DifficultyDaySample>(entity)
                : em.AddBuffer<DifficultyDaySample>(entity);
            buffer.Clear();
            int days = Mathf.Clamp(p.SampleDays, 1, 200);
            for (int day = 1; day <= days; day++)
            {
                buffer.Add(MobileCastleTuningResolver.ResolveDaySample(p, day));
            }

            GameManager gameManager = GameManager.Instance;
            gameManager?.ApplyWallBaseHpTuning(config.WallBaseHp);
            gameManager?.ApplyWorkerEconomyTuning(
                config.WoodWorkerProductionPerMin,
                config.StoneWorkerProductionPerMin,
                config.IronWorkerProductionPerMin,
                config.FoodWorkerProductionPerMin);
            gameManager?.ApplyArcherDefinitionTuning();
        }

        private void LoadLatestSummary()
        {
            _deaths.Clear();
            _maxDayReached = 0;
            _summaryFile = "";

            const string dir = "Logs/LongRun";
            if (!Directory.Exists(dir))
            {
                _summaryFile = "Logs/LongRun yok — once bot kostur.";
                return;
            }

            string latest = null;
            System.DateTime latestTime = System.DateTime.MinValue;
            foreach (var file in Directory.GetFiles(dir, "*.csv"))
            {
                var t = File.GetLastWriteTime(file);
                if (t > latestTime) { latestTime = t; latest = file; }
            }

            if (latest == null)
            {
                _summaryFile = "CSV bulunamadi.";
                return;
            }

            _summaryFile = Path.GetFileName(latest);
            foreach (var line in File.ReadAllLines(latest))
            {
                int comma = line.IndexOf(',');
                if (comma <= 0) continue;
                string dayField = line.Substring(0, comma);
                bool isGameOver = dayField.Contains("GAMEOVER");
                string num = dayField.Replace("(GAMEOVER)", "").Trim();
                int day;
                if (!int.TryParse(num, out day)) continue;
                _maxDayReached = Mathf.Max(_maxDayReached, day);
                if (isGameOver) _deaths.Add(day);
            }
        }
    }
}
