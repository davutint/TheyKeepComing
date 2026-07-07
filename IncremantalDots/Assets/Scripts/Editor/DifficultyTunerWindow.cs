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
        private bool _foldRepair;
        private bool _foldFuture;
        private bool _foldBot = true;

        private float _botTimeScale = 3f;
        private int _botTargetDay = 20;
        private bool _botAutoRestart = true;

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
            DrawRepairSection();
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
                    "Lineer HP buyumesine EK gun carpani.");
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

        private void DrawRepairSection()
        {
            _foldRepair = DrawSectionHeader(_foldRepair, "Tamir Maliyeti", "tam kayipta odenen taban");
            if (!_foldRepair)
                return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("RepairBaseWoodCost");
                DrawProp("RepairBaseStoneCost");
            }
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
            config.SpawnBatchSize = Mathf.Max(1, p.SpawnBatchSize);
            config.ZombieBaseHP = Mathf.Max(1f, p.ZombieBaseHP);
            config.ZombieHpGrowthPerCycle = Mathf.Max(0f, p.ZombieHpGrowthPerCycle);
            config.ZombieBaseDamage = Mathf.Max(0.1f, p.ZombieBaseDamage);
            config.ZombieDamagePerCycle = Mathf.Max(0f, p.ZombieDamagePerCycle);
            config.SpawnBatchGrowthPerCycle = Mathf.Max(0f, p.SpawnBatchGrowthPerCycle);
            config.MaxSpawnBatch = Mathf.Max(0, p.MaxSpawnBatch);
            config.MaxAliveZombies = Mathf.Max(0, p.MaxAliveZombies);
            config.BaseSpawnInterval = Mathf.Max(0.01f, p.BaseSpawnInterval);
            config.MinSpawnInterval = Mathf.Max(0.01f, p.MinSpawnInterval);
            config.SiegeDayIntensityMultiplier = Mathf.Max(0.01f, p.DayIntensity);
            config.SiegeDuskStartIntensityMultiplier = Mathf.Max(0.01f, p.DuskStartIntensity);
            config.SiegeDuskEndIntensityMultiplier = Mathf.Max(0.01f, p.DuskEndIntensity);
            config.SiegeNightIntensityMultiplier = Mathf.Max(0.01f, p.NightIntensity);
            config.SiegeDawnIntensityMultiplier = Mathf.Max(0.01f, p.DawnIntensity);
            config.RepairBaseWoodCost = Mathf.Max(0, p.RepairBaseWoodCost);
            config.RepairBaseStoneCost = Mathf.Max(0, p.RepairBaseStoneCost);
            em.SetComponentData(entity, config);

            var buffer = em.HasBuffer<DifficultyDaySample>(entity)
                ? em.GetBuffer<DifficultyDaySample>(entity)
                : em.AddBuffer<DifficultyDaySample>(entity);
            buffer.Clear();
            int days = Mathf.Clamp(p.SampleDays, 1, 200);
            for (int day = 1; day <= days; day++)
            {
                buffer.Add(new DifficultyDaySample
                {
                    NightIntensityMult = p.EvaluateCurve(p.NightIntensityByDay, day),
                    ZombieHpMult = p.EvaluateCurve(p.ZombieHpMultByDay, day),
                    SpawnBatchMult = p.EvaluateCurve(p.SpawnBatchMultByDay, day),
                });
            }
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
