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
        private DifficultyProfileSO _profile;
        private SerializedObject _profileSO;
        private Vector2 _scroll;

        private bool _foldCurves = true;
        private bool _foldEscalation = true;
        private bool _foldIntensity;
        private bool _foldSpawnContract = true;
        private bool _foldRepair = true;
        private bool _foldEconomyPrices = true;
        private bool _foldFuture;
        private bool _foldBot = true;

        private float _botTimeScale = 3f;
        private int _botTargetDay = 20;
        private bool _botAutoRestart = true;
        private int _spawnPreviewDay = 1;
        private float _wallPreviewMissingPercent = 0.50f;
        private int _economyPreviewLevel;
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

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Adjacent Population / Arrow Inputs", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Bu alanlar mevcut edit yetenegini korur; kendi tracker audit'lerinde ayrica "
                    + "Population ve Archer runtime contract yuzeylerine alinacak.", MessageType.None);
                EditorGUILayout.LabelField("House Beds", EditorStyles.miniBoldLabel);
                DrawProp("BedBaseWoodCost");
                DrawProp("BedCostGrowthCapacityInterval");
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Finite Arrow Supply", EditorStyles.miniBoldLabel);
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
            }
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
