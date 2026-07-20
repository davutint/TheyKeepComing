using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    public enum LongRunBotPolicy
    {
        Balanced = 0,
        Economy = 1,
        Defense = 2
    }

    /// <summary>
    /// M-A olcum harness'i: Play Mode'da Balanced/Economy/Defense bot politikalarini tek
    /// run veya fresh-run cohort olarak kosar. Worker/housing/Arrow/Heart/okcu, ability,
    /// repair ve Council kararlarini yonetir; run detayini ve cohort finalini CSV'ye doker.
    /// Amac optimal oyun degil, ayni fingerprint altindaki DAY 1-20 egrisini karsilastirmaktir.
    /// </summary>
    public class LongRunSimulatorWindow : EditorWindow
    {
        private const float TickIntervalAtFiveX = 0.25f;
        private const double RestartSettleDelay = 1.0;
        private const int MaxArchers = 40;        // pop'un tamamini okcuya bagLama tavani

        private bool _running;
        private float _timeScale = 3f;
        private int _targetDay = 20; // 0 = GameOver'a kadar
        private bool _autoRestartOnGameOver;
        private LongRunBotPolicy _policy = LongRunBotPolicy.Balanced;
        private int _cohortTargetRuns = 1;
        private int _cohortCompletedRuns;
        private int _currentRunIndex = 1;
        private bool _freshMetaPerRun;
        private bool _waitingForRestart;
        private bool _restartIssued;
        private double _nextRestartTime;
        private string _cohortId;
        private string _cohortSummaryPath;
        private StringBuilder _cohortSummary;

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
        private int _repairSpends, _heartSpends, _archerSpends, _housingSpends;
        private int _arrowRefillSpends, _workerUpgradeSpends, _fireballCasts;
        private int _rallyCasts, _emergencyRepairCasts;
        private long _totalHeartEssenceSpent;
        private ResourceCost _totalRepairCost, _totalArcherCost, _totalHousingCost;
        private ResourceCost _totalArrowCost, _totalWorkerUpgradeCost;

        private string _status = "hazir";

        [MenuItem("Window/DeadWalls/Long Run Simulator")]
        public static void ShowWindow()
        {
            GetWindow<LongRunSimulatorWindow>("Long Run Sim");
        }

        public bool IsRunning => _running;
        public string CsvPath => _csvPath;
        public string Status => _status;
        public int CompletedRuns => _cohortCompletedRuns;
        public int TargetRuns => _cohortTargetRuns;
        public LongRunBotPolicy Policy => _policy;
        public string CohortSummaryPath => _cohortSummaryPath;

        /// <summary>Difficulty Tuner koprusu: pencereyi acar, parametreleri kurar ve kosuyu baslatir (play modda olunmali).</summary>
        public static LongRunSimulatorWindow OpenAndStart(float timeScale, int targetDay, bool autoRestartOnGameOver)
        {
            var win = GetWindow<LongRunSimulatorWindow>("Long Run Sim");
            win._timeScale = Mathf.Clamp(timeScale, 1f, 10f);
            win._targetDay = Mathf.Max(0, targetDay);
            win._autoRestartOnGameOver = autoRestartOnGameOver;
            win._policy = LongRunBotPolicy.Balanced;
            win._cohortTargetRuns = 1;
            win._freshMetaPerRun = false;
            win.StartRun();
            return win;
        }

        public static LongRunSimulatorWindow OpenAndStartCohort(
            float timeScale,
            int targetDay,
            int runCount,
            LongRunBotPolicy policy)
        {
            var win = GetWindow<LongRunSimulatorWindow>("Long Run Sim");
            win._timeScale = Mathf.Clamp(timeScale, 1f, 10f);
            win._targetDay = Mathf.Max(0, targetDay);
            win._autoRestartOnGameOver = false;
            win._policy = policy;
            win._cohortTargetRuns = Mathf.Clamp(runCount, 1, 100);
            win._freshMetaPerRun = true;
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
                "Fresh-run olcum botu: policy'ye gore worker/housing/Arrow/Heart/okcu yonetir;\n" +
                "her safagi run CSV'sine, her finali cohort CSV'sine yazar. Once Play'e gir.",
                MessageType.Info);

            _timeScale = EditorGUILayout.Slider("Time Scale", _timeScale, 1f, 10f);
            _targetDay = EditorGUILayout.IntField("Hedef Gun (0 = olene kadar)", _targetDay);
            _policy = (LongRunBotPolicy)EditorGUILayout.EnumPopup("Bot Policy", _policy);
            _cohortTargetRuns = EditorGUILayout.IntSlider("Cohort Runs", _cohortTargetRuns, 1, 100);
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
            EditorGUILayout.LabelField("Cohort", $"{_cohortCompletedRuns}/{_cohortTargetRuns} · {_policy}");
            if (!string.IsNullOrEmpty(_csvPath))
                EditorGUILayout.LabelField("CSV", _csvPath);
            if (!string.IsNullOrEmpty(_cohortSummaryPath))
                EditorGUILayout.LabelField("Summary", _cohortSummaryPath);
        }

        private void StartRun()
        {
            var gm = GameManager.Instance;
            if (gm == null) { _status = "GameManager yok"; return; }

            Application.runInBackground = true; // editor arka planda da oyun aksin (bilinen tuzak)
            Directory.CreateDirectory("Logs/LongRun");
            _cohortId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            _cohortSummaryPath = $"Logs/LongRun/cohort_{_cohortId}_{PolicySlug()}.csv";
            _cohortSummary = new StringBuilder();
            _cohortSummary.AppendLine(
                "run,policy,result,outcomeDay,gameMin,realMin,wood,stone,iron,food," +
                "popTotal,beds,archers,heartBuys,heartNodes,graveEssence,arrows,aliveZombies," +
                "wallPct,repairs,housingBuys,arrowRefills,workerUpgrades,fireballCasts," +
                "rallyCasts,emergencyRepairs");

            _cohortCompletedRuns = 0;
            _currentRunIndex = 1;
            _csvPath = string.Empty;
            _csv = null;
            _waitingForRestart = false;
            _restartIssued = false;
            _running = true;

            if (_freshMetaPerRun)
            {
                ScheduleRestart();
                return;
            }

            BeginRunCapture(gm);
        }

        private void BeginRunCapture(GameManager gm)
        {
            _csvPath = $"Logs/LongRun/longrun_{_cohortId}_{PolicySlug()}_r{_currentRunIndex:00}.csv";
            _csv = new StringBuilder();
            _csv.AppendLine("day,gameMin,realMin,wood,stone,iron,food,prodW,prodS,prodI,prodF," +
                "popTotal,popIdle,popWorkers,popArchers,beds,archers,heartNodes,graveEssence," +
                "arrows,arrowCapacity,aliveZombies,wallPct,fps,repairs,heartBuys,archerBuys," +
                "housingBuys,arrowRefills,workerUpgrades,fireballCasts,rallyCasts,emergencyRepairs," +
                "repairWood,repairStone,heartEssence,housingWood,arrowWood,upgradeWood,upgradeIron," +
                "archWood,archStone,archIron,archFood");

            _lastPhase = gm.ContinuousSiegeCycle.Phase;
            _lastLoggedDay = 0;
            _runStartRealTime = EditorApplication.timeSinceStartup;
            _lastFrameCount = Time.frameCount;
            _lastFpsTime = EditorApplication.timeSinceStartup;
            _repairSpends = _heartSpends = _archerSpends = _housingSpends = 0;
            _arrowRefillSpends = _workerUpgradeSpends = _fireballCasts = 0;
            _rallyCasts = _emergencyRepairCasts = 0;
            _totalHeartEssenceSpent = 0L;
            _totalRepairCost = _totalArcherCost = _totalHousingCost = ResourceCost.Zero;
            _totalArrowCost = _totalWorkerUpgradeCost = ResourceCost.Zero;
            _waitingForRestart = false;
            _restartIssued = false;
            _nextTickTime = EditorApplication.timeSinceStartup;

            ApplyInitialWorkerPolicy(gm);
            Time.timeScale = _timeScale;
            _status = $"{_policy} cohort {_currentRunIndex}/{_cohortTargetRuns} kosuyor...";
        }

        private void StopRun(string reason)
        {
            if (!_running)
                return;

            _running = false;
            Time.timeScale = 1f;
            FlushCsv();
            FlushCohortSummary();
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
            double tickInterval = TickIntervalAtFiveX * (5f / Mathf.Max(1f, _timeScale));
            _nextTickTime = EditorApplication.timeSinceStartup + tickInterval;

            if (_waitingForRestart)
            {
                HandleCohortRestart();
                return;
            }

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
                CompleteCurrentRun(
                    gm,
                    gm.ContinuousSiegeCycle.CycleIndex + 1,
                    gameOver: true);
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
                    CompleteCurrentRun(gm, day, gameOver: false);
                    return;
                }
            }

            _lastPhase = cycle.Phase;
            _status = $"{_policy} {_currentRunIndex}/{_cohortTargetRuns} | DAY {cycle.CycleIndex + 1} " +
                      $"{cycle.Phase} | fps {_fps:0} | W{gm.Resources.Wood} S{gm.Resources.Stone} " +
                      $"I{gm.Resources.Iron} F{gm.Resources.Food}";

            RunBotPolicy(gm);
        }

        private void CompleteCurrentRun(GameManager gm, int day, bool gameOver)
        {
            if (gameOver)
                LogDayRow(gm, day, gameOver: true);

            FlushCsv();
            AppendCohortSummaryRow(gm, day, gameOver);
            _cohortCompletedRuns++;

            bool continueRunning = _cohortCompletedRuns < _cohortTargetRuns
                                   || _autoRestartOnGameOver;
            if (!continueRunning)
            {
                string result = gameOver ? $"GAME OVER DAY {day}" : $"TARGET DAY {day}";
                StopRun($"cohort tamamlandi ({_cohortCompletedRuns} run) · son sonuc {result}");
                return;
            }

            _currentRunIndex++;
            ScheduleRestart();
        }

        private void ScheduleRestart()
        {
            _waitingForRestart = true;
            _restartIssued = false;
            _nextRestartTime = EditorApplication.timeSinceStartup + 0.25;
            Time.timeScale = 0f;
            _status = $"{_policy} cohort: fresh run {_currentRunIndex} hazirlaniyor...";
        }

        private void HandleCohortRestart()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < _nextRestartTime)
                return;

            GameManager gm = GameManager.Instance;
            if (!_restartIssued)
            {
                if (gm == null)
                    return;

                if (_freshMetaPerRun)
                    ResetMetaForFreshCohortRun();
                gm.RestartGame();
                Time.timeScale = 0f;
                _restartIssued = true;
                _nextRestartTime = now + RestartSettleDelay;
                return;
            }

            if (gm == null || !gm.ContinuousSiegeCycle.Enabled || gm.GameState.IsGameOver)
                return;
            if (gm.ContinuousSiegeCycle.CycleIndex != 0
                || gm.ContinuousSiegeCycle.CycleTimer > 0.25f)
            {
                return;
            }

            BeginRunCapture(gm);
        }

        private static void ResetMetaForFreshCohortRun()
        {
            MetaProgressState meta = MetaProgression.State;
            meta.Souls = 0;
            meta.TotalSoulsEarned = 0;
            meta.BestDay = 0;
            meta.TotalRuns = 0;
            meta.TotalKillsAllTime = 0;
            meta.Upgrades.Clear();
            meta.UnlockedPoolIds.Clear();
            meta.RewardedRunIds.Clear();
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

            // 2) Finite Arrow: stok kritik seviyeye gelmeden bir refill paketi al.
            int arrowCapacity = Mathf.Max(1, gm.GetArrowCapacity());
            int refillThreshold = Mathf.Max(50, Mathf.CeilToInt(arrowCapacity * 0.35f));
            if (gm.ArrowSupply.Current <= refillThreshold && gm.CanBuyArrowRefill(1))
            {
                ArrowRefillQuote quote = gm.GetArrowRefillQuote(1);
                if (gm.TryBuyArrowRefill(1))
                {
                    _arrowRefillSpends++;
                    _totalArrowCost = Add(_totalArrowCost,
                        new ResourceCost(quote.WoodCost, 0, 0, 0));
                }
            }

            // 3) Gece savunma ability'leri ve normal repair.
            if (gm.GetDefensePercent() < 0.45f && gm.TryUseEmergencyRepair())
                _emergencyRepairCasts++;
            if (gm.ContinuousSiegeCycle.Phase == SiegeCyclePhase.Night
                && CountAliveZombies() >= 8
                && gm.TryUseRally())
            {
                _rallyCasts++;
            }
            if (gm.GetDefensePercent() < 0.65f && gm.CanRepairDefenseFull())
            {
                var cost = gm.GetRepairCost();
                if (gm.RepairDefenseFull())
                {
                    _repairSpends++;
                    _totalRepairCost = Add(_totalRepairCost, cost);
                }
            }

            // 4) Heart: policy'ye gore economy veya savunma etkilerini onceliklendir.
            TryBuyHeartNodeForPolicy(gm);

            // 5) Fireball aciksa en ilerlemis canli hedefe kullan.
            if (gm.FireballReady && TryCastFireballAtFront(gm))
                _fireballCasts++;

            // 6-7) Defense once okcuyu, Economy once ileri yatak hazirligini ele alir.
            bool reserveIdleForArcher;
            if (_policy == LongRunBotPolicy.Defense)
            {
                reserveIdleForArcher = TryRecruitArcherForPolicy(gm);
                TryPrepareHousing(gm);
            }
            else
            {
                TryPrepareHousing(gm);
                reserveIdleForArcher = TryRecruitArcherForPolicy(gm);
            }

            // 8) Satin alma icin bekletilmeyen idle population policy agirliklarina atanir.
            for (int i = 0; !reserveIdleForArcher && i < 4 && gm.GetIdlePopulation() > 0; i++)
            {
                var target = PickWorkerResource(gm);
                if (target == EconomyFocusType.Balanced || !gm.AssignResourceWorker(target))
                    break;
            }

            // 9) Fazla ekonomi tamponu olusunca Wood/Arrow efficiency'yi dengeli ilerlet.
            TryBuyEfficiencyInvestment(gm);
        }

        private void ApplyInitialWorkerPolicy(GameManager gm)
        {
            if (_policy == LongRunBotPolicy.Balanced)
                return;

            // Once Food'u azaltarak idle ac; sonra policy hedeflerine dagit. Toplam 53 worker
            // korunur, yalniz player'in yapabilecegi allocation karari modellenir.
            if (_policy == LongRunBotPolicy.Economy)
            {
                gm.SetResourceWorkers(EconomyFocusType.Food, 7);
                gm.SetResourceWorkers(EconomyFocusType.Stone, 10);
                gm.SetResourceWorkers(EconomyFocusType.Iron, 10);
                gm.SetResourceWorkers(EconomyFocusType.Wood, 26);
                return;
            }

            gm.SetResourceWorkers(EconomyFocusType.Food, 6);
            gm.SetResourceWorkers(EconomyFocusType.Stone, 12);
            gm.SetResourceWorkers(EconomyFocusType.Iron, 8);
            gm.SetResourceWorkers(EconomyFocusType.Wood, 27);
        }

        private void TryPrepareHousing(GameManager gm)
        {
            int targetFreeBeds = _policy == LongRunBotPolicy.Economy ? 15 :
                _policy == LongRunBotPolicy.Defense ? 5 : 1;
            int freeBeds = Mathf.Max(0, gm.GetTotalBedCapacity() - gm.Population.Total);
            if (freeBeds >= targetFreeBeds)
                return;

            ResourceCost housingCost = gm.GetBedCapacityPurchaseCost(1);
            int reserveWood = Mathf.Max(50, gm.GetArrowRefillQuote(2).WoodCost);
            if (_policy == LongRunBotPolicy.Defense)
                reserveWood += gm.GetArcherBuyCost(ArcherType.Basic).Wood;

            if (housingCost.Wood <= 0
                || gm.Resources.Wood - housingCost.Wood < reserveWood
                || !gm.TryBuyBedCapacity(1))
            {
                return;
            }

            _housingSpends++;
            _totalHousingCost = Add(_totalHousingCost, housingCost);
        }

        /// <summary>
        /// true donerse mevcut idle, policy hedefindeki bir sonraki okcu icin korunur.
        /// </summary>
        private bool TryRecruitArcherForPolicy(GameManager gm)
        {
            int currentArchers = CountArchers();
            int desiredArchers = GetDesiredArcherCount(gm.Population.Total);
            if (currentArchers >= Mathf.Min(MaxArchers, desiredArchers)
                || gm.GetIdlePopulation() <= 0)
            {
                return false;
            }

            if (!HasSustainableArrowBudget(gm))
                return false;

            bool bought = TryBuyArcherPreferred(gm);
            int afterPurchase = bought ? currentArchers + 1 : currentArchers;
            return afterPurchase < Mathf.Min(MaxArchers, desiredArchers)
                   && gm.GetIdlePopulation() > 0;
        }

        private int GetDesiredArcherCount(int population)
        {
            switch (_policy)
            {
                case LongRunBotPolicy.Economy:
                    return Mathf.Max(6, Mathf.CeilToInt(population * 0.09f));
                case LongRunBotPolicy.Defense:
                    return Mathf.Max(8, Mathf.CeilToInt(population * 0.18f));
                default:
                    return 5;
            }
        }

        private void TryBuyHeartNodeForPolicy(GameManager gm)
        {
            if (!gm.TryBuildHeartPresentation(out HeartGraphPresentation presentation, out _))
                return;

            string selectedNodeId = string.Empty;
            long selectedCost = 0L;
            int selectedScore = int.MinValue;
            bool selectedCanPurchase = false;
            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                if (node == null || node.IsRoot || !node.IsExactContentVisible
                    || string.IsNullOrWhiteSpace(node.ExactNodeId))
                {
                    continue;
                }

                HeartPurchaseEvaluation evaluation = gm.EvaluateHeartPurchase(
                    node.ExactNodeId,
                    HeartPurchaseQuantity.One);
                bool canSaveForDefense = _policy == LongRunBotPolicy.Defense
                                         && evaluation.FailureReason ==
                                         HeartPurchaseFailureReason.InsufficientGraveEssence;
                if ((!evaluation.CanPurchase && !canSaveForDefense) || evaluation.Quote == null)
                {
                    continue;
                }

                long cost = evaluation.Quote.TotalGraveEssenceCost;
                int score = GetHeartPolicyScore(node) - (int)System.Math.Min(cost, 1000L);
                if (score <= selectedScore)
                    continue;

                selectedNodeId = node.ExactNodeId;
                selectedCost = cost;
                selectedScore = score;
                selectedCanPurchase = evaluation.CanPurchase;
            }

            // Defense policy, en yuksek oncelikli node icin Essence biriktirir; daha ucuz
            // ama alakasiz bir node'a sirf su anda alinabiliyor diye sapmaz.
            if (string.IsNullOrWhiteSpace(selectedNodeId) || !selectedCanPurchase)
                return;

            HeartPurchaseResult result = gm.TryPurchaseHeartNode(
                selectedNodeId,
                HeartPurchaseQuantity.One);
            if (!result.Succeeded || result.Quote == null)
                return;

            _heartSpends++;
            _totalHeartEssenceSpent += selectedCost;
        }

        private int GetHeartPolicyScore(HeartGraphNodePresentation node)
        {
            if (_policy == LongRunBotPolicy.Balanced)
                return 0;

            int score = 0;
            if (_policy == LongRunBotPolicy.Economy)
            {
                if (node.Branch == HeartNodeBranch.Production)
                    score += 350;
                for (int i = 0; i < node.Effects.Count; i++)
                {
                    switch (node.Effects[i].Type)
                    {
                        case HeartNodeEffectType.IncreaseResourceProductionPercent:
                            score += 900;
                            break;
                        case HeartNodeEffectType.IncreaseWorkerCapacity:
                            score += 800;
                            break;
                        case HeartNodeEffectType.IncreasePopulationGrowth:
                            score += 700;
                            break;
                        case HeartNodeEffectType.IncreaseArrowEfficiency:
                        case HeartNodeEffectType.IncreaseArrowCapacity:
                            score += 300;
                            break;
                    }
                }
                return score;
            }

            if (node.Branch == HeartNodeBranch.Army
                || node.Branch == HeartNodeBranch.Defense
                || node.Branch == HeartNodeBranch.HeartMagic)
            {
                score += 250;
            }
            for (int i = 0; i < node.Effects.Count; i++)
            {
                switch (node.Effects[i].Type)
                {
                    case HeartNodeEffectType.UnlockSpellcasting:
                        score += 1100;
                        break;
                    case HeartNodeEffectType.UnlockArcherType:
                        score += 1000;
                        break;
                    case HeartNodeEffectType.ModifyWallMaxHpPercent:
                    case HeartNodeEffectType.ReduceWallRepairCostPercent:
                        score += 900;
                        break;
                    case HeartNodeEffectType.ModifyArcherDamagePercent:
                    case HeartNodeEffectType.ModifyArcherFireRatePercent:
                    case HeartNodeEffectType.AddArcherRange:
                        score += 800;
                        break;
                    case HeartNodeEffectType.IncreaseArrowEfficiency:
                    case HeartNodeEffectType.IncreaseArrowCapacity:
                        score += 700;
                        break;
                    case HeartNodeEffectType.ModifySpellDamagePercent:
                    case HeartNodeEffectType.AddSpellRadius:
                    case HeartNodeEffectType.ReduceSpellCooldownPercent:
                    case HeartNodeEffectType.EnableSplitShot:
                    case HeartNodeEffectType.EnableBurningGround:
                    case HeartNodeEffectType.EnableSecondBlast:
                        score += 600;
                        break;
                }
            }
            return score;
        }

        private void TryBuyEfficiencyInvestment(GameManager gm)
        {
            int minimumWood = _policy == LongRunBotPolicy.Defense ? 300 : 350;
            if (gm.Resources.Wood < minimumWood || gm.Resources.Iron < 100)
                return;

            if (_policy == LongRunBotPolicy.Defense)
            {
                if (!TryBuyArrowEfficiency(gm))
                    TryBuyWoodEfficiency(gm);
                return;
            }

            if (_policy == LongRunBotPolicy.Economy)
            {
                if (!TryBuyWoodEfficiency(gm))
                    TryBuyArrowEfficiency(gm);
                return;
            }

            int arrowLevel = gm.GetArrowUpgradeLevel(ArrowUpgradeType.Efficiency);
            int woodLevel = gm.GetWorkerBuildingUpgradeLevel(
                EconomyFocusType.Wood,
                WorkerBuildingUpgradeType.Efficiency);

            // Ayni Wood butcesini paylasan iki yatirimi dengeli ilerlet. Esitlikte Arrow
            // once gelir; aksi halde Wood efficiency her kontrolde Arrow'u bloke ediyordu.
            if (arrowLevel <= woodLevel && TryBuyArrowEfficiency(gm))
                return;

            if (TryBuyWoodEfficiency(gm))
                return;

            TryBuyArrowEfficiency(gm);
        }

        private bool TryBuyWoodEfficiency(GameManager gm)
        {
            ResourceCost workerCost = gm.GetWorkerBuildingUpgradeCost(
                EconomyFocusType.Wood,
                WorkerBuildingUpgradeType.Efficiency);
            if (workerCost.Wood > 0
                && gm.Resources.Wood - workerCost.Wood >= 150
                && gm.Resources.Iron - workerCost.Iron >= 50
                && gm.TryBuyWorkerBuildingUpgrade(
                    EconomyFocusType.Wood,
                    WorkerBuildingUpgradeType.Efficiency))
            {
                _workerUpgradeSpends++;
                _totalWorkerUpgradeCost = Add(_totalWorkerUpgradeCost, workerCost);
                return true;
            }

            return false;
        }

        private bool TryBuyArrowEfficiency(GameManager gm)
        {
            ResourceCost arrowCost = gm.GetArrowUpgradeCost(ArrowUpgradeType.Efficiency);
            if (arrowCost.Wood > 0
                && gm.Resources.Wood - arrowCost.Wood >= 150
                && gm.Resources.Iron - arrowCost.Iron >= 50
                && gm.TryBuyArrowUpgrade(ArrowUpgradeType.Efficiency))
            {
                _workerUpgradeSpends++;
                _totalWorkerUpgradeCost = Add(_totalWorkerUpgradeCost, arrowCost);
                return true;
            }

            return false;
        }

        private bool HasSustainableArrowBudget(GameManager gm)
        {
            float currentDrainPerSecond = 0f;
            float nextArcherDrain = 1.5f;
            ArcherDefinitionSO[] definitions = gm.GetArcherDefinitions();
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    ArcherDefinitionSO definition = definitions[i];
                    if (definition == null)
                        continue;
                    currentDrainPerSecond += gm.GetArcherTypeCount(definition.Type)
                        * Mathf.Max(0f, definition.FireRate);
                }

                var preferredOrder = new[] { ArcherType.Frost, ArcherType.Rapid, ArcherType.Basic };
                for (int i = 0; i < preferredOrder.Length; i++)
                {
                    if (!gm.IsArcherTypeUnlocked(preferredOrder[i]))
                        continue;
                    for (int j = 0; j < definitions.Length; j++)
                    {
                        if (definitions[j] == null || definitions[j].Type != preferredOrder[i])
                            continue;
                        nextArcherDrain = Mathf.Max(0f, definitions[j].FireRate);
                        i = preferredOrder.Length;
                        break;
                    }
                }
            }

            float budgetShare = _policy == LongRunBotPolicy.Defense ? 0.95f :
                _policy == LongRunBotPolicy.Economy ? 0.80f : 0.75f;
            float stockThreshold = _policy == LongRunBotPolicy.Defense ? 0.35f : 0.55f;
            float sustainableDrain = gm.GetWorkerProductionRate(EconomyFocusType.Wood)
                * gm.GetArrowsPerWood() / 60f * budgetShare;
            return gm.ArrowSupply.Current >= Mathf.CeilToInt(gm.GetArrowCapacity() * stockThreshold)
                   && currentDrainPerSecond + nextArcherDrain <= sustainableDrain;
        }

        private static bool TryCastFireballAtFront(GameManager gm)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return false;

            EntityQuery query = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ZombieTag>(),
                ComponentType.ReadOnly<ZombieState>(),
                ComponentType.ReadOnly<LocalTransform>());
            var states = query.ToComponentDataArray<ZombieState>(Unity.Collections.Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
            bool found = false;
            float bestX = float.MaxValue;
            Vector2 target = default;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].Value == ZombieStateType.Dead || transforms[i].Position.x >= bestX)
                    continue;
                bestX = transforms[i].Position.x;
                target = new Vector2(transforms[i].Position.x, transforms[i].Position.y);
                found = true;
            }
            transforms.Dispose();
            states.Dispose();
            query.Dispose();
            return found && gm.TryCastFireball(target);
        }

        private bool TryBuyArcherPreferred(GameManager gm)
        {
            var order = new[] { ArcherType.Frost, ArcherType.Rapid, ArcherType.Basic };
            foreach (var type in order)
            {
                if (!gm.CanBuyArcher(type))
                    continue;
                ResourceCost cost = gm.GetArcherBuyCost(type);
                if (gm.BuyArcher(type))
                {
                    _archerSpends++;
                    _totalArcherCost = Add(_totalArcherCost, cost);
                    return true;
                }
            }

            return false;
        }

        private EconomyFocusType PickWorkerResource(GameManager gm)
        {
            var resources = new[] { EconomyFocusType.Wood, EconomyFocusType.Stone, EconomyFocusType.Iron, EconomyFocusType.Food };
            EconomyFocusType best = EconomyFocusType.Balanced;
            float bestFill = float.MaxValue;
            foreach (var r in resources)
            {
                if (!gm.CanAssignResourceWorker(r))
                    continue;
                float denominator = _policy == LongRunBotPolicy.Balanced
                    ? Mathf.Max(1, GetCap(gm, r))
                    : Mathf.Max(0.01f, GetWorkerPolicyWeight(r));
                float fill = gm.GetResourceWorkers(r) / denominator;
                if (fill < bestFill) { bestFill = fill; best = r; }
            }
            return best;
        }

        private float GetWorkerPolicyWeight(EconomyFocusType resource)
        {
            if (_policy == LongRunBotPolicy.Economy)
            {
                switch (resource)
                {
                    case EconomyFocusType.Wood: return 0.49f;
                    case EconomyFocusType.Stone: return 0.19f;
                    case EconomyFocusType.Iron: return 0.19f;
                    case EconomyFocusType.Food: return 0.13f;
                }
            }
            else if (_policy == LongRunBotPolicy.Defense)
            {
                switch (resource)
                {
                    case EconomyFocusType.Wood: return 0.51f;
                    case EconomyFocusType.Stone: return 0.25f;
                    case EconomyFocusType.Iron: return 0.15f;
                    case EconomyFocusType.Food: return 0.09f;
                }
            }

            return 0.25f;
        }

        private static int GetCap(GameManager gm, EconomyFocusType resource)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 1;
            var q = world.EntityManager.CreateEntityQuery(typeof(MobileCastleCombatConfig));
            if (q.CalculateEntityCount() == 0)
            {
                q.Dispose();
                return 1;
            }
            var cfg = q.GetSingleton<MobileCastleCombatConfig>();
            q.Dispose();
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
        private void AppendCohortSummaryRow(GameManager gm, int day, bool gameOver)
        {
            var cycle = gm.ContinuousSiegeCycle;
            float gameMin = (cycle.CycleIndex * cycle.CycleDuration + cycle.CycleTimer) / 60f;
            float realMin = (float)((EditorApplication.timeSinceStartup - _runStartRealTime) / 60.0);
            float wallPct = gm.Wall.MaxHP > 0 ? gm.Wall.CurrentHP / gm.Wall.MaxHP : 0f;
            HeartRuntimeTuningTelemetry heart = gm.GetHeartRuntimeTuningTelemetry();
            CultureInfo ci = CultureInfo.InvariantCulture;

            _cohortSummary.AppendLine(string.Join(",",
                _currentRunIndex,
                _policy,
                gameOver ? "GAMEOVER" : "TARGET",
                day,
                gameMin.ToString("0.0", ci),
                realMin.ToString("0.0", ci),
                gm.Resources.Wood,
                gm.Resources.Stone,
                gm.Resources.Iron,
                gm.Resources.Food,
                gm.Population.Total,
                gm.GetTotalBedCapacity(),
                CountArchers(),
                _heartSpends,
                heart.PurchasedNodeCount,
                gm.GraveEssenceAmount,
                gm.ArrowSupply.Current,
                CountAliveZombies(),
                wallPct.ToString("0.00", ci),
                _repairSpends,
                _housingSpends,
                _arrowRefillSpends,
                _workerUpgradeSpends,
                _fireballCasts,
                _rallyCasts,
                _emergencyRepairCasts));
            FlushCohortSummary();
        }

        private void LogDayRow(GameManager gm, int dayOverride, bool gameOver)
        {
            var cycle = gm.ContinuousSiegeCycle;
            float gameMin = (cycle.CycleIndex * cycle.CycleDuration + cycle.CycleTimer) / 60f;
            float realMin = (float)((EditorApplication.timeSinceStartup - _runStartRealTime) / 60.0);

            HeartRuntimeTuningTelemetry heart = gm.GetHeartRuntimeTuningTelemetry();

            float wallPct = gm.Wall.MaxHP > 0 ? gm.Wall.CurrentHP / gm.Wall.MaxHP : 0f;

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
                gm.GetTotalBedCapacity(), CountArchers(), heart.PurchasedNodeCount,
                gm.GraveEssenceAmount, gm.ArrowSupply.Current, gm.GetArrowCapacity(), CountAliveZombies(),
                wallPct.ToString("0.00", ci),
                _fps.ToString("0", ci), _repairSpends, _heartSpends, _archerSpends,
                _housingSpends, _arrowRefillSpends, _workerUpgradeSpends,
                _fireballCasts, _rallyCasts, _emergencyRepairCasts,
                _totalRepairCost.Wood, _totalRepairCost.Stone,
                _totalHeartEssenceSpent, _totalHousingCost.Wood, _totalArrowCost.Wood,
                _totalWorkerUpgradeCost.Wood, _totalWorkerUpgradeCost.Iron,
                _totalArcherCost.Wood, _totalArcherCost.Stone,
                _totalArcherCost.Iron, _totalArcherCost.Food));
        }

        private static int CountArchers()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return 0;
            var q = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ArcherUnit>());
            int count = q.CalculateEntityCount();
            q.Dispose();
            return count;
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
            q.Dispose();
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

        private void FlushCohortSummary()
        {
            if (_cohortSummary == null || string.IsNullOrEmpty(_cohortSummaryPath))
                return;
            File.WriteAllText(_cohortSummaryPath, _cohortSummary.ToString());
        }

        private string PolicySlug()
        {
            return _policy.ToString().ToLowerInvariant();
        }
    }
}
