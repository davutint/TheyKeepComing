using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// M-A olcum harness'i: play modda basit bir "bot oyuncu" politikasi kosar
    /// (worker bas, tech/okcu al, repair yap, council'da sec) ve her safakta gun-sonu
    /// metriklerini CSV'ye doker. Amac optimal oyun DEGIL, DAY 1-20 egrilerinin seklini
    /// gormektir (kosu suresi = M-B roguelite meta tasariminin girdisi).
    /// Kullanim: Play'e gir -> pencereden Start. CSV: Logs/LongRun/.
    /// </summary>
    public class LongRunSimulatorWindow : EditorWindow
    {
        private const float TickInterval = 0.25f; // gercek-zaman bot tick araligi
        private const int MaxArchers = 40;        // pop'un tamamini okcuya bagLama tavani

        private bool _running;
        private float _timeScale = 3f;
        private int _targetDay = 20; // 0 = GameOver'a kadar
        private bool _autoRestartOnGameOver;

        private double _nextTickTime;
        private SiegeCyclePhase _lastPhase = SiegeCyclePhase.Day;
        private int _lastLoggedDay;
        private string _csvPath;
        private StringBuilder _csv;
        private double _runStartRealTime;

        // FPS olcumu: oyun frame sayisi / gercek zaman (editor-update dt oyun dt'si degildir)
        private int _lastFrameCount;
        private double _lastFpsTime;
        private float _fps;

        // kumulatif harcama takibi
        private int _repairSpends, _techSpends, _archerSpends;
        private ResourceCost _totalRepairCost, _totalTechCost, _totalArcherCost;

        private string _status = "hazir";

        [MenuItem("Window/DeadWalls/Long Run Simulator")]
        public static void ShowWindow()
        {
            GetWindow<LongRunSimulatorWindow>("Long Run Sim");
        }

        public bool IsRunning => _running;
        public string CsvPath => _csvPath;
        public string Status => _status;

        /// <summary>Difficulty Tuner koprusu: pencereyi acar, parametreleri kurar ve kosuyu baslatir (play modda olunmali).</summary>
        public static LongRunSimulatorWindow OpenAndStart(float timeScale, int targetDay, bool autoRestartOnGameOver)
        {
            var win = GetWindow<LongRunSimulatorWindow>("Long Run Sim");
            win._timeScale = Mathf.Clamp(timeScale, 1f, 5f);
            win._targetDay = Mathf.Max(0, targetDay);
            win._autoRestartOnGameOver = autoRestartOnGameOver;
            win.StartRun();
            return win;
        }

        private void OnEnable() => EditorApplication.update += OnEditorUpdate;
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopRun("pencere kapandi");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "M-A olcum botu: worker atar, tech/okcu alir, repair yapar, council'da secer;\n" +
                "her SAFAKTA gun-sonu metriklerini CSV'ye yazar. Once Play'e gir, sonra Start.",
                MessageType.Info);

            _timeScale = EditorGUILayout.Slider("Time Scale", _timeScale, 1f, 5f);
            _targetDay = EditorGUILayout.IntField("Hedef Gun (0 = olene kadar)", _targetDay);
            _autoRestartOnGameOver = EditorGUILayout.Toggle("GameOver'da yeni kosu", _autoRestartOnGameOver);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (!_running && GUILayout.Button("Start", GUILayout.Height(28)))
                    StartRun();
                if (_running && GUILayout.Button("Stop", GUILayout.Height(28)))
                    StopRun("elle durduruldu");
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Play modda degil.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Durum", _status);
            if (!string.IsNullOrEmpty(_csvPath))
                EditorGUILayout.LabelField("CSV", _csvPath);
        }

        private void StartRun()
        {
            var gm = GameManager.Instance;
            if (gm == null) { _status = "GameManager yok"; return; }

            Application.runInBackground = true; // editor arka planda da oyun aksin (bilinen tuzak)
            Time.timeScale = _timeScale;

            Directory.CreateDirectory("Logs/LongRun");
            _csvPath = $"Logs/LongRun/longrun_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
            _csv = new StringBuilder();
            _csv.AppendLine("day,gameMin,realMin,wood,stone,iron,food,prodW,prodS,prodI,prodF," +
                "popTotal,popIdle,popWorkers,popArchers,archers,techLevels,aliveZombies," +
                "wallPct,gatePct,corePct,fps,repairs,techBuys,archerBuys," +
                "repairWood,repairStone,techWood,techStone,techIron,techFood,archWood,archFood");

            _running = true;
            _lastPhase = gm.ContinuousSiegeCycle.Phase;
            _lastLoggedDay = 0;
            _runStartRealTime = EditorApplication.timeSinceStartup;
            _lastFrameCount = Time.frameCount;
            _lastFpsTime = EditorApplication.timeSinceStartup;
            _repairSpends = _techSpends = _archerSpends = 0;
            _totalRepairCost = _totalTechCost = _totalArcherCost = ResourceCost.Zero;
            _status = "kosuyor...";
        }

        private void StopRun(string reason)
        {
            if (!_running)
                return;

            _running = false;
            Time.timeScale = 1f;
            FlushCsv();
            _status = "durdu: " + reason + (string.IsNullOrEmpty(_csvPath) ? "" : " -> " + _csvPath);
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!_running)
                return;

            if (!Application.isPlaying) { StopRun("play modundan cikildi"); return; }
            if (EditorApplication.timeSinceStartup < _nextTickTime)
                return;
            _nextTickTime = EditorApplication.timeSinceStartup + TickInterval;

            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
                return;

            // FPS: oyun frame farki / gercek zaman farki
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastFpsTime >= 1.0)
            {
                _fps = (float)((Time.frameCount - _lastFrameCount) / (now - _lastFpsTime));
                _lastFrameCount = Time.frameCount;
                _lastFpsTime = now;
            }

            Time.timeScale = _timeScale; // restart vb. sifirlarsa geri kur

            if (gm.GameState.IsGameOver)
            {
                LogDayRow(gm, dayOverride: gm.ContinuousSiegeCycle.CycleIndex + 1, gameOver: true);
                if (_autoRestartOnGameOver)
                {
                    gm.RestartGame();
                    Time.timeScale = _timeScale; // koddan restart timeScale'i geri acmaz (bilinen tuzak)
                    _lastLoggedDay = 0;
                    return;
                }
                StopRun("GAME OVER — DAY " + (gm.ContinuousSiegeCycle.CycleIndex + 1));
                return;
            }

            var cycle = gm.ContinuousSiegeCycle;

            // Safak = gun sonu: metrik satiri (gun basina bir kez)
            if (cycle.Phase == SiegeCyclePhase.Dawn && _lastPhase != SiegeCyclePhase.Dawn)
            {
                int day = cycle.CycleIndex + 1;
                if (day > _lastLoggedDay)
                {
                    LogDayRow(gm, day, false);
                    _lastLoggedDay = day;
                    FlushCsv(); // olası kilitlenmede veri kaybolmasin
                }

                if (_targetDay > 0 && day >= _targetDay)
                {
                    StopRun("hedef gune ulasildi: DAY " + day);
                    return;
                }
            }

            _lastPhase = cycle.Phase;
            _status = $"DAY {cycle.CycleIndex + 1} {cycle.Phase} | fps {_fps:0} | W{gm.Resources.Wood} S{gm.Resources.Stone} I{gm.Resources.Iron} F{gm.Resources.Food}";

            RunBotPolicy(gm);
        }

        // ---------------------------------------------------------------
        // Bot politikasi: makul-ortalama oyuncu (optimal degil)
        // ---------------------------------------------------------------
        private void RunBotPolicy(GameManager gm)
        {
            // 1) Council: A'yi tercih et, karsilanamiyorsa B
            var council = gm.ActiveCouncilEvent;
            if (council != null)
            {
                if (gm.CanAffordCouncilOption(council.OptionA)) gm.ChooseCouncilOption(true);
                else if (gm.CanAffordCouncilOption(council.OptionB)) gm.ChooseCouncilOption(false);
            }

            // 2) Repair: savunma %60 altina dustuyse
            if (gm.GetDefensePercent() < 0.6f && gm.CanRepairDefenseFull())
            {
                var cost = gm.GetRepairCost();
                if (gm.RepairDefenseFull())
                {
                    _repairSpends++;
                    _totalRepairCost = Add(_totalRepairCost, cost);
                }
            }

            // 3) Worker: idle varsa en dusuk doluluk oranli kaynaga (tick basina en fazla 2)
            for (int i = 0; i < 2 && gm.GetIdlePopulation() > 0; i++)
            {
                var target = PickLowestFillResource(gm);
                if (target == EconomyFocusType.Balanced || !gm.AssignResourceWorker(target))
                    break;
            }

            // 4) Tech: alinabilir en ucuz gorunur node (tick basina 1)
            TechNodeDefinitionSO cheapest = null;
            int cheapestTotal = int.MaxValue;
            foreach (var node in gm.GetRevealedTechNodes())
            {
                if (!gm.CanBuyTechNode(node, out _))
                    continue;
                var cost = gm.GetTechNodeCost(node);
                int total = cost.Wood + cost.Stone + cost.Iron + cost.Food;
                if (total < cheapestTotal) { cheapestTotal = total; cheapest = node; }
            }
            if (cheapest != null)
            {
                var cost = gm.GetTechNodeCost(cheapest);
                if (gm.TryBuyTechNode(cheapest))
                {
                    _techSpends++;
                    _totalTechCost = Add(_totalTechCost, cost);
                }
            }

            // 5) Okcu: tavanin altindaysa; Frost > Rapid > Basic tercihi (tick basina 1)
            if (CountArchers() < MaxArchers)
            {
                TryBuyArcherPreferred(gm);
            }
        }

        private void TryBuyArcherPreferred(GameManager gm)
        {
            var order = new[] { ArcherType.Frost, ArcherType.Rapid, ArcherType.Basic };
            foreach (var type in order)
            {
                if (!gm.CanBuyArcher(type))
                    continue;
                if (gm.BuyArcher(type))
                {
                    _archerSpends++;
                    // yaklasik maliyet takibi: katalog fiyatini bilmeden kaba (raporda adet esas)
                    return;
                }
            }
        }

        private static EconomyFocusType PickLowestFillResource(GameManager gm)
        {
            var resources = new[] { EconomyFocusType.Wood, EconomyFocusType.Stone, EconomyFocusType.Iron, EconomyFocusType.Food };
            EconomyFocusType best = EconomyFocusType.Balanced;
            float bestFill = float.MaxValue;
            foreach (var r in resources)
            {
                if (!gm.CanAssignResourceWorker(r))
                    continue;
                float cap = Mathf.Max(1, GetCap(gm, r));
                float fill = gm.GetResourceWorkers(r) / cap;
                if (fill < bestFill) { bestFill = fill; best = r; }
            }
            return best;
        }

        private static int GetCap(GameManager gm, EconomyFocusType resource)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 1;
            var q = world.EntityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            if (q.CalculateEntityCount() == 0) return 1;
            var cfg = q.GetSingleton<MobileCastleCombatConfig>();
            switch (resource)
            {
                case EconomyFocusType.Stone: return cfg.StoneWorkerCap;
                case EconomyFocusType.Iron: return cfg.IronWorkerCap;
                case EconomyFocusType.Food: return cfg.FoodWorkerCap;
                default: return cfg.WoodWorkerCap;
            }
        }

        // ---------------------------------------------------------------
        // Metrikler
        // ---------------------------------------------------------------
        private void LogDayRow(GameManager gm, int dayOverride, bool gameOver)
        {
            var cycle = gm.ContinuousSiegeCycle;
            float gameMin = (cycle.CycleIndex * cycle.CycleDuration + cycle.CycleTimer) / 60f;
            float realMin = (float)((EditorApplication.timeSinceStartup - _runStartRealTime) / 60.0);

            int techLevels = 0;
            var catalog = gm.TechCatalog;
            if (catalog != null && catalog.Nodes != null)
                foreach (var n in catalog.Nodes)
                    if (n != null) techLevels += gm.GetTechNodeLevel(n.Id);

            float wallPct = gm.Wall.MaxHP > 0 ? gm.Wall.CurrentHP / gm.Wall.MaxHP : 0f;
            float gatePct = gm.Gate.MaxHP > 0 ? gm.Gate.CurrentHP / gm.Gate.MaxHP : 0f;
            float corePct = gm.Castle.MaxHP > 0 ? gm.Castle.CurrentHP / gm.Castle.MaxHP : 0f;

            var ci = CultureInfo.InvariantCulture;
            _csv.AppendLine(string.Join(",",
                dayOverride + (gameOver ? " (GAMEOVER)" : ""),
                gameMin.ToString("0.0", ci), realMin.ToString("0.0", ci),
                gm.Resources.Wood, gm.Resources.Stone, gm.Resources.Iron, gm.Resources.Food,
                gm.GetWorkerProductionRate(EconomyFocusType.Wood).ToString("0.0", ci),
                gm.GetWorkerProductionRate(EconomyFocusType.Stone).ToString("0.0", ci),
                gm.GetWorkerProductionRate(EconomyFocusType.Iron).ToString("0.0", ci),
                gm.GetWorkerProductionRate(EconomyFocusType.Food).ToString("0.0", ci),
                gm.Population.Total, gm.Population.Idle, gm.Population.Workers, gm.Population.Archers,
                CountArchers(), techLevels, CountAliveZombies(),
                wallPct.ToString("0.00", ci), gatePct.ToString("0.00", ci), corePct.ToString("0.00", ci),
                _fps.ToString("0", ci), _repairSpends, _techSpends, _archerSpends,
                _totalRepairCost.Wood, _totalRepairCost.Stone,
                _totalTechCost.Wood, _totalTechCost.Stone, _totalTechCost.Iron, _totalTechCost.Food,
                _totalArcherCost.Wood, _totalArcherCost.Food));
        }

        private static int CountArchers()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 0;
            var q = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ArcherUnit>());
            return q.CalculateEntityCount();
        }

        private static int CountAliveZombies()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 0;
            var q = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ZombieTag>(), ComponentType.ReadOnly<ZombieState>());
            var states = q.ToComponentDataArray<ZombieState>(Unity.Collections.Allocator.Temp);
            int alive = 0;
            foreach (var s in states)
                if (s.Value != ZombieStateType.Dead) alive++;
            states.Dispose();
            return alive;
        }

        private static ResourceCost Add(ResourceCost a, ResourceCost b)
        {
            return new ResourceCost(a.Wood + b.Wood, a.Stone + b.Stone, a.Iron + b.Iron, a.Food + b.Food);
        }

        private void FlushCsv()
        {
            if (_csv == null || string.IsNullOrEmpty(_csvPath))
                return;
            File.WriteAllText(_csvPath, _csv.ToString());
        }
    }
}
