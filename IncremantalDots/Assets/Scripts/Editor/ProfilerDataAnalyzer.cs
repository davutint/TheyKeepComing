#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

// ═══════════════════════════════════════════════════════════════
// PROFILER DATA ANALYZER V6
// Unity Profiler .raw dosyasını yükleyip kapsamlı performans
// raporu üreten EditorWindow tabanlı analiz aracı.
// 11 tab: Overview, Spikes, Functions, Update, User Code,
//         Rendering, GC Analysis, Call Chains, Compare,
//         ECS Pipeline, Physics
// V3: Snapshot A/B comparison, file browser, delta analysis
// V4: A/B/Both display mode (dual view), GC/Call metric,
//     User Code GC section, selfTime=0 GC fix
// V5: DeadWalls ECS/DOTS pipeline analizi, physics breakdown,
//     ECS sistem bazli A/B karsilastirma
// V6: Worker thread job taramasi — Burst job surelerini
//     worker thread'lerden yakalar, Job Avg/Max kolonlari
// ═══════════════════════════════════════════════════════════════

public class ProfilerDataAnalyzer : EditorWindow
{
    // ═══════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════

    private const string REPORT_OUTPUT_PATH = @"C:\Users\PC\Desktop\PERFORMANS ANALIZI\profiler_report.txt";
    private const string USER_CODE_PREFIX = "Assembly-CSharp.dll!";

    private const int MAX_HIERARCHY_DEPTH = 8;
    private const int TOP_SPIKE_COUNT = 20;
    private const int TOP_FUNCTION_COUNT = 30;
    private const int TOP_GC_ALLOCATOR_COUNT = 20;
    private const int TOP_GC_FRAME_COUNT = 20;
    private const int TOP_GC_CHAIN_CALLERS = 30;
    private const int TOP_GC_CHAIN_CHILDREN = 5;
    private const int PROGRESS_UPDATE_INTERVAL = 50;
    private const float MIN_NODE_TIME_MS = 0.01f;
    private const int DICTIONARY_INITIAL_CAPACITY = 2048;

    // Renk eşikleri
    private const float GC_WARNING_BYTES = 10 * 1024;       // 10 KB → sarı
    private const float GC_CRITICAL_BYTES = 100 * 1024;     // 100 KB → kırmızı
    private const float CPU_WARNING_MS = 1f;                  // 1 ms → sarı
    private const float CPU_CRITICAL_MS = 5f;                 // 5 ms → kırmızı

    private static readonly string[] UPDATE_METHOD_NAMES =
    {
        "BehaviourUpdate",       // Update()
        "LateBehaviourUpdate",   // LateUpdate()
        "FixedBehaviourUpdate"   // FixedUpdate()
    };

    private static readonly string[] UPDATE_DISPLAY_NAMES =
    {
        "Update (BehaviourUpdate)",
        "LateUpdate (LateBehaviourUpdate)",
        "FixedUpdate (FixedBehaviourUpdate)"
    };

    // DeadWalls ECS sistemleri — profiler marker tespiti icin
    private static readonly Dictionary<string, string> KNOWN_ECS_SYSTEMS = new()
    {
        { "WaveSpawnSystem", "SimulationSystemGroup" },
        { "ApplyMovementForceSystem", "SimulationSystemGroup" },
        { "BuildSpatialHashSystem", "SimulationSystemGroup" },
        { "PhysicsCollisionSystem", "SimulationSystemGroup" },
        { "IntegrateSystem", "SimulationSystemGroup" },
        { "BoundarySystem", "SimulationSystemGroup" },
        { "ZombieAttackSystem", "SimulationSystemGroup" },
        { "ArcherShootSystem", "SimulationSystemGroup" },
        { "ArrowMoveSystem", "SimulationSystemGroup" },
        { "ArrowHitSystem", "SimulationSystemGroup" },
        { "ClickDamageSystem", "SimulationSystemGroup" },
        { "ZombieDeathSystem", "SimulationSystemGroup" },
        { "ZombieAnimationStateSystem", "SimulationSystemGroup" },
        { "DamageCleanupSystem", "SimulationSystemGroup" },
        { "SpriteAnimationSystem", "PresentationSystemGroup" },
    };

    // Job struct adi → parent sistem adi eslestirmesi
    private static readonly Dictionary<string, string> KNOWN_ECS_JOBS = new()
    {
        { "ApplyForceJob", "ApplyMovementForceSystem" },
        { "HashJob", "BuildSpatialHashSystem" },
        { "CollisionJob", "PhysicsCollisionSystem" },
        { "IntegrateJob", "IntegrateSystem" },
        { "BoundaryJob", "BoundarySystem" },
    };

    private static readonly string[] PHYSICS_PIPELINE_SYSTEMS =
    {
        "ApplyMovementForceSystem", "BuildSpatialHashSystem",
        "PhysicsCollisionSystem", "IntegrateSystem", "BoundarySystem"
    };

    private static readonly string[] TAB_NAMES =
    {
        "Overview", "Spikes", "Functions", "Update",
        "User Code", "Rendering", "GC Analysis", "Call Chains",
        "Compare", "ECS Pipeline", "Physics"
    };

    // ═══════════════════════════════════════════════════════════
    // DATA STRUCTURES
    // ═══════════════════════════════════════════════════════════

    private struct FrameTimingData
    {
        public int frameIndex;
        public float cpuMs;
        public float gpuMs;
        public float fps;
        public double gcBytes;
    }

    private class FunctionProfileEntry
    {
        public string name;
        public double totalSelfTimeMs;
        public float maxSelfTimeMs;
        public long totalCalls;
        public double totalGcBytes;
        public int frameCount;

        public float AverageSelfTimeMs => frameCount > 0 ? (float)(totalSelfTimeMs / frameCount) : 0f;
        public double GcPerCall => totalCalls > 0 ? totalGcBytes / totalCalls : 0;
    }

    private struct RenderingFrameStats
    {
        public long drawCalls;
        public long batches;
        public long setPassCalls;
        public long triangles;
        public long vertices;
        public long shadowCasters;
        public long dynamicBatches;
        public long staticBatches;
        public long instancedBatches;
        public long visibleSkinnedMeshes;
        public bool isValid;
    }

    private struct UpdateContributorData
    {
        public string name;
        public double timeMs;
        public double gcBytes;
    }

    private class UpdateMethodStats
    {
        public string methodName;
        public string displayName;
        public double totalTimeMs;
        public double totalGcBytes;
        public int frameCount;
        public List<UpdateContributorData> topContributors;
    }

    private class GcCallChainEntry
    {
        public string userCodeCaller;
        public string gcProducer;
        public double totalGcBytes;
        public long totalCalls;
        public int occurrences;
        public double GcPerCall => totalCalls > 0 ? totalGcBytes / totalCalls : 0;
    }

    private class GcCallChainGroup
    {
        public string userCodeCaller;
        public double totalGcBytes;
        public List<GcCallChainEntry> children;
    }

    private class EcsSystemEntry
    {
        public string systemName;
        public string groupName;
        public double totalTimeMs;
        public double totalSelfTimeMs;
        public float maxTimeMs;
        public int frameCount;
        public double totalGcBytes;
        public bool hasSyncPoint;
        public double syncPointTimeMs;
        public float AvgTimeMs => frameCount > 0 ? (float)(totalTimeMs / frameCount) : 0f;

        // Worker thread job sureleri
        public double jobTimeMs;       // Worker thread'deki toplam job suresi
        public float maxJobTimeMs;     // En yavas frame'deki job suresi
        public float AvgJobTimeMs => frameCount > 0 ? (float)(jobTimeMs / frameCount) : 0f;
    }

    private class EcsFrameData
    {
        public int frameIndex;
        public float totalPipelineTimeMs;
        public Dictionary<string, float> systemTimesMs;
        public Dictionary<string, float> jobTimesMs; // job bazinda worker thread sureleri
    }

    // ═══════════════════════════════════════════════════════════
    // ANALYSIS SNAPSHOT (V3)
    // ═══════════════════════════════════════════════════════════

    private class AnalysisSnapshot
    {
        public string filePath;

        // Raw data
        public List<FrameTimingData> frameTimings;
        public Dictionary<string, FunctionProfileEntry> functionMap;
        public List<RenderingFrameStats> renderingStats;
        public UpdateMethodStats[] updateMethodStats;
        public Dictionary<string, GcCallChainEntry> gcCallChains;
        public Dictionary<string, EcsSystemEntry> ecsSystemMap;
        public List<EcsFrameData> ecsFrameTimeline;

        // Summary stats
        public int totalFrames, skippedFrames, firstFrame, lastFrame;
        public float avgCpu, minCpu, maxCpu;
        public float avgGpu, minGpu, maxGpu;
        public float avgFps, minFps, maxFps;
        public double totalGcBytes;
        public int totalUserFunctions;

        // Derived cache
        public List<FrameTimingData> topSpikes;
        public List<FunctionProfileEntry> topFunctions;
        public List<FunctionProfileEntry> userCodeFunctions;
        public List<FunctionProfileEntry> topGcAllocators;
        public List<FrameTimingData> topGcFrames;
        public Dictionary<string, int> fpsDistribution;
        public List<GcCallChainGroup> gcCallChainGroups;

        // ECS derived cache
        public List<EcsSystemEntry> ecsSystemsSorted;
        public List<EcsSystemEntry> physicsPipelineSystems;
        public float avgPhysicsPipelineMs;
        public float maxPhysicsPipelineMs;
    }

    // ═══════════════════════════════════════════════════════════
    // WINDOW STATE
    // ═══════════════════════════════════════════════════════════

    private AnalysisSnapshot _snapshotA;
    private AnalysisSnapshot _snapshotB;
    private string _filePathA = "";
    private string _filePathB = "";
    private string _lastBrowseDir = "";
    private int _activeTab;
    private Vector2[] _scrollPositions = new Vector2[11];

    // Foldout state for call chains
    private HashSet<string> _expandedChains = new HashSet<string>();

    // A/B display mode (V4)
    private enum SnapshotDisplayMode { A, B, Both }
    private SnapshotDisplayMode _snapshotDisplayMode = SnapshotDisplayMode.A;
    private AnalysisSnapshot _displaySnapshot;

    // ═══════════════════════════════════════════════════════════
    // COMPATIBILITY PROPERTIES (Tab 0-7 use these → delegates to _displaySnapshot ?? _snapshotA)
    // ═══════════════════════════════════════════════════════════

    private AnalysisSnapshot _activeSnapshot => _displaySnapshot ?? _snapshotA;
    private bool _hasData => _snapshotA != null;
    private List<FrameTimingData> _frameTimings => _activeSnapshot?.frameTimings;
    private Dictionary<string, FunctionProfileEntry> _functionMap => _activeSnapshot?.functionMap;
    private List<RenderingFrameStats> _renderingStats => _activeSnapshot?.renderingStats;
    private UpdateMethodStats[] _updateMethodStats => _activeSnapshot?.updateMethodStats;
    private List<FrameTimingData> _topSpikes => _activeSnapshot?.topSpikes;
    private List<FunctionProfileEntry> _topFunctions => _activeSnapshot?.topFunctions;
    private List<FunctionProfileEntry> _userCodeFunctions => _activeSnapshot?.userCodeFunctions;
    private List<FunctionProfileEntry> _topGcAllocators => _activeSnapshot?.topGcAllocators;
    private List<FrameTimingData> _topGcFrames => _activeSnapshot?.topGcFrames;
    private Dictionary<string, int> _fpsDistribution => _activeSnapshot?.fpsDistribution;
    private List<GcCallChainGroup> _gcCallChainGroups => _activeSnapshot?.gcCallChainGroups;
    private int _totalFrames => _activeSnapshot?.totalFrames ?? 0;
    private int _skippedFrames => _activeSnapshot?.skippedFrames ?? 0;
    private float _avgCpu => _activeSnapshot?.avgCpu ?? 0;
    private float _minCpu => _activeSnapshot?.minCpu ?? 0;
    private float _maxCpu => _activeSnapshot?.maxCpu ?? 0;
    private float _avgGpu => _activeSnapshot?.avgGpu ?? 0;
    private float _minGpu => _activeSnapshot?.minGpu ?? 0;
    private float _maxGpu => _activeSnapshot?.maxGpu ?? 0;
    private float _avgFps => _activeSnapshot?.avgFps ?? 0;
    private float _minFps => _activeSnapshot?.minFps ?? 0;
    private float _maxFps => _activeSnapshot?.maxFps ?? 0;
    private double _totalGcBytes => _activeSnapshot?.totalGcBytes ?? 0;
    private int _totalUserFunctions => _activeSnapshot?.totalUserFunctions ?? 0;
    private int _firstFrame => _activeSnapshot?.firstFrame ?? 0;
    private int _lastFrame => _activeSnapshot?.lastFrame ?? 0;

    // ═══════════════════════════════════════════════════════════
    // MENU & WINDOW
    // ═══════════════════════════════════════════════════════════

    [MenuItem("Tools/Analyze Profiler Data")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProfilerDataAnalyzer>("Profiler Analyzer");
        window.minSize = new Vector2(900, 600);
    }

    // ═══════════════════════════════════════════════════════════
    // ON GUI
    // ═══════════════════════════════════════════════════════════

    private static readonly string[] DISPLAY_MODE_LABELS = { "A", "B", "A|B" };

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.Space(4);
        int newTab = GUILayout.Toolbar(_activeTab, TAB_NAMES, GUILayout.Height(26));
        if (newTab != _activeTab)
            _activeTab = newTab;
        EditorGUILayout.Space(4);

        // Compare tab is always accessible (has its own A/B handling)
        if (_activeTab == 8)
        {
            DrawCompareTab();
            return;
        }

        // Display mode toggle (only visible when B is loaded and not on Compare tab)
        if (_snapshotB != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Görüntüle:", EditorStyles.miniLabel);
            int modeIdx = (int)_snapshotDisplayMode;
            int newMode = GUILayout.Toolbar(modeIdx, DISPLAY_MODE_LABELS, GUILayout.Width(160), GUILayout.Height(20));
            if (newMode != modeIdx)
                _snapshotDisplayMode = (SnapshotDisplayMode)newMode;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        if (!_hasData)
        {
            EditorGUILayout.HelpBox(
                "Profiler verisi yüklenmedi. \"Load A\" butonuna basarak .raw dosyası yükleyin.", MessageType.Info);
            return;
        }

        // Centralized rendering with scroll view
        _scrollPositions[_activeTab] = EditorGUILayout.BeginScrollView(_scrollPositions[_activeTab]);

        if (_snapshotB != null && _snapshotDisplayMode == SnapshotDisplayMode.Both)
        {
            // ── A section ──
            DrawSnapshotLabel("A", _snapshotA);
            _displaySnapshot = _snapshotA;
            DrawTabContent(_activeTab);

            EditorGUILayout.Space(8);
            DrawSeparator();
            EditorGUILayout.Space(8);

            // ── B section ──
            DrawSnapshotLabel("B", _snapshotB);
            _displaySnapshot = _snapshotB;
            DrawTabContent(_activeTab);

            _displaySnapshot = null;
        }
        else
        {
            // Single snapshot mode
            if (_snapshotB != null && _snapshotDisplayMode == SnapshotDisplayMode.B)
                _displaySnapshot = _snapshotB;
            else
                _displaySnapshot = null; // falls back to _snapshotA via _activeSnapshot

            DrawTabContent(_activeTab);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTabContent(int tab)
    {
        switch (tab)
        {
            case 0: DrawOverviewContent(); break;
            case 1: DrawSpikesContent(); break;
            case 2: DrawFunctionsContent(); break;
            case 3: DrawUpdateContent(); break;
            case 4: DrawUserCodeContent(); break;
            case 5: DrawRenderingContent(); break;
            case 6: DrawGcAnalysisContent(); break;
            case 7: DrawCallChainsContent(); break;
            case 9: DrawEcsPipelineContent(); break;
            case 10: DrawPhysicsBreakdownContent(); break;
        }
    }

    private void DrawSnapshotLabel(string label, AnalysisSnapshot snapshot)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = label == "A" ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.7f, 0.3f) }
        };
        EditorGUILayout.LabelField(
            $"━━━ Snapshot {label}: {Path.GetFileName(snapshot?.filePath ?? "")} ━━━",
            style);
        EditorGUILayout.Space(4);
    }

    private void DrawSeparator()
    {
        Rect rect = GUILayoutUtility.GetRect(1, 2, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        GUILayout.Label("Profiler Analyzer", titleStyle, GUILayout.Width(120));

        GUILayout.Space(4);

        // Snapshot A info
        string aLabel = _snapshotA != null
            ? $"A: {Path.GetFileName(_snapshotA.filePath)} ({_snapshotA.totalFrames - _snapshotA.skippedFrames}f)"
            : "A: —";
        GUILayout.Label(aLabel, EditorStyles.miniLabel, GUILayout.MaxWidth(220));

        if (GUILayout.Button("Load A", GUILayout.Width(55)))
            BrowseAndAnalyze(true);

        GUILayout.Space(6);

        // Snapshot B info
        string bLabel = _snapshotB != null
            ? $"B: {Path.GetFileName(_snapshotB.filePath)} ({_snapshotB.totalFrames - _snapshotB.skippedFrames}f)"
            : "B: —";
        GUILayout.Label(bLabel, EditorStyles.miniLabel, GUILayout.MaxWidth(220));

        if (GUILayout.Button("Load B", GUILayout.Width(55)))
            BrowseAndAnalyze(false);

        GUILayout.Space(4);

        GUI.enabled = _snapshotB != null;
        if (GUILayout.Button("Clear B", GUILayout.Width(55)))
        {
            _snapshotB = null;
            _filePathB = "";
            _snapshotDisplayMode = SnapshotDisplayMode.A;
            _displaySnapshot = null;
            Repaint();
        }
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        GUI.enabled = _hasData;
        if (GUILayout.Button("Export TXT", GUILayout.Width(80)))
            ExportReport();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════
    // ANALYSIS (ENTRY POINT)
    // ═══════════════════════════════════════════════════════════

    private void BrowseAndAnalyze(bool isSlotA)
    {
        string startDir = !string.IsNullOrEmpty(_lastBrowseDir) ? _lastBrowseDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = EditorUtility.OpenFilePanel("Select Profiler .raw file", startDir, "raw");

        if (string.IsNullOrEmpty(path))
            return;

        _lastBrowseDir = Path.GetDirectoryName(path);
        string label = isSlotA ? "A" : "B";

        var snapshot = AnalyzeFile(path, label);
        if (snapshot == null)
            return;

        if (isSlotA)
        {
            _snapshotA = snapshot;
            _filePathA = path;
        }
        else
        {
            _snapshotB = snapshot;
            _filePathB = path;
        }

        Repaint();
    }

    private AnalysisSnapshot AnalyzeFile(string path, string label)
    {
        try
        {
            if (!LoadProfile(path))
                return null;

            int firstFrame = ProfilerDriver.firstFrameIndex;
            int lastFrame = ProfilerDriver.lastFrameIndex;

            if (firstFrame < 0 || lastFrame < 0 || lastFrame < firstFrame)
            {
                EditorUtility.DisplayDialog("Profiler Analyzer",
                    $"[{label}] Frame aralığı geçersiz: {firstFrame} - {lastFrame}", "OK");
                return null;
            }

            var snapshot = new AnalysisSnapshot
            {
                filePath = path,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                totalFrames = lastFrame - firstFrame + 1,
                skippedFrames = 0,
                frameTimings = new List<FrameTimingData>(lastFrame - firstFrame + 1),
                functionMap = new Dictionary<string, FunctionProfileEntry>(DICTIONARY_INITIAL_CAPACITY),
                renderingStats = new List<RenderingFrameStats>(lastFrame - firstFrame + 1),
                gcCallChains = new Dictionary<string, GcCallChainEntry>(DICTIONARY_INITIAL_CAPACITY),
                ecsSystemMap = new Dictionary<string, EcsSystemEntry>(32),
                ecsFrameTimeline = new List<EcsFrameData>(lastFrame - firstFrame + 1),
                updateMethodStats = new UpdateMethodStats[UPDATE_METHOD_NAMES.Length]
            };

            Debug.Log($"[ProfilerAnalyzer][{label}] Analiz başlıyor: {snapshot.totalFrames} frame ({firstFrame} - {lastFrame})");

            // Debug state sifirla
            _workerThreadDebugLogged = false;
            _discoveredThreadNames = new HashSet<string>();
            _discoveredJobMarkers = new HashSet<string>();
            _ecsChildDebugLogged = false;
            _discoveredEcsChildren = new HashSet<string>();

            var childrenBuffer = new List<int>(256);

            for (int i = 0; i < UPDATE_METHOD_NAMES.Length; i++)
            {
                snapshot.updateMethodStats[i] = new UpdateMethodStats
                {
                    methodName = UPDATE_METHOD_NAMES[i],
                    displayName = UPDATE_DISPLAY_NAMES[i],
                    totalTimeMs = 0,
                    totalGcBytes = 0,
                    frameCount = 0,
                    topContributors = new List<UpdateContributorData>()
                };
            }

            for (int frameIdx = firstFrame; frameIdx <= lastFrame; frameIdx++)
            {
                if (frameIdx % PROGRESS_UPDATE_INTERVAL == 0)
                {
                    float progress = (float)(frameIdx - firstFrame) / snapshot.totalFrames;
                    if (EditorUtility.DisplayCancelableProgressBar(
                        $"Profiler Analizi [{label}]",
                        $"Frame {frameIdx - firstFrame + 1} / {snapshot.totalFrames} işleniyor...",
                        progress))
                    {
                        Debug.LogWarning($"[ProfilerAnalyzer][{label}] Kullanıcı tarafından iptal edildi.");
                        EditorUtility.ClearProgressBar();
                        return null;
                    }
                }

                try
                {
                    ProcessFrame(frameIdx, childrenBuffer, snapshot);
                }
                catch (Exception ex)
                {
                    snapshot.skippedFrames++;
                    if (snapshot.skippedFrames <= 5)
                        Debug.LogWarning($"[ProfilerAnalyzer][{label}] Frame {frameIdx} atlandı: {ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();

            if (snapshot.skippedFrames > 0)
                Debug.LogWarning($"[ProfilerAnalyzer][{label}] Toplam {snapshot.skippedFrames} frame atlandı (hata nedeniyle).");

            CacheAnalysisResults(snapshot);

            // Debug: ECS sistem altindaki child marker'larini logla
            if (_discoveredEcsChildren.Count > 0)
            {
                var matches = _discoveredEcsChildren.Where(s => s.StartsWith("[MATCH]")).ToList();
                var children = _discoveredEcsChildren.Where(s => s.StartsWith("[CHILD]")).ToList();
                if (matches.Count > 0)
                    Debug.Log($"[ProfilerAnalyzer V6] Eslesen job marker'lar ({matches.Count}):\n" +
                        string.Join("\n", matches.Select(n => $"  {n}")));
                if (children.Count > 0)
                    Debug.Log($"[ProfilerAnalyzer V6] ECS sistem altindaki bilinmeyen child marker'lar ({children.Count}):\n" +
                        string.Join("\n", children.OrderBy(s => s).Select(n => $"  {n}")));
            }
            else
            {
                Debug.Log("[ProfilerAnalyzer V6] Main thread'de ECS sistem altinda hicbir anlamli child marker bulunamadi.");
            }

            int analyzedFrames = snapshot.totalFrames - snapshot.skippedFrames;
            Debug.Log($"[ProfilerAnalyzer][{label}] Analiz tamamlandı: {analyzedFrames} frame, " +
                      $"{snapshot.functionMap.Count} fonksiyon, {snapshot.gcCallChainGroups.Count} GC call chain grubu");

            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProfilerAnalyzer][{label}] Kritik hata: {ex}");
            EditorUtility.DisplayDialog("Profiler Analyzer",
                $"[{label}] Beklenmeyen hata oluştu:\n{ex.Message}", "OK");
            return null;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PROFILE LOADING
    // ═══════════════════════════════════════════════════════════

    private bool LoadProfile(string path)
    {
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Profiler Analyzer",
                $"Profiler dosyası bulunamadı:\n{path}\n\n" +
                "Unity Profiler'dan File > Save as .raw ile dışa aktarın.", "OK");
            return false;
        }

        EditorUtility.DisplayProgressBar("Profiler Analizi", "Profil yükleniyor...", 0f);

        bool loaded = ProfilerDriver.LoadProfile(path, false);

        if (!loaded)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Profiler Analyzer",
                $"Profil yüklenemedi:\n{path}\n\n" +
                "Dosyanın geçerli bir Unity Profiler .raw dosyası olduğundan emin olun.", "OK");
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // FRAME PROCESSING
    // ═══════════════════════════════════════════════════════════

    private void ProcessFrame(int frameIndex, List<int> childrenBuffer, AnalysisSnapshot snapshot)
    {
        using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
            frameIndex, 0,
            HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
            HierarchyFrameDataView.columnTotalTime,
            false))
        {
            if (frameData == null || !frameData.valid)
                return;

            // Frame timing
            float cpuMs = frameData.frameTimeMs;
            float gpuMs = frameData.frameGpuTimeMs;
            float fps = cpuMs > 0 ? 1000f / cpuMs : 0f;

            // Per-frame GC: root item'ın GC memory'si = frame toplam GC
            int rootId = frameData.GetRootItemID();
            double frameGcBytes = 0;
            if (rootId != -1)
            {
                frameGcBytes = frameData.GetItemColumnDataAsFloat(rootId,
                    HierarchyFrameDataView.columnGcMemory);
            }

            snapshot.frameTimings.Add(new FrameTimingData
            {
                frameIndex = frameIndex,
                cpuMs = cpuMs,
                gpuMs = gpuMs,
                fps = fps,
                gcBytes = frameGcBytes
            });

            // Hierarchy traversal
            if (rootId != -1)
            {
                TraverseHierarchy(frameData, rootId, childrenBuffer, 0, snapshot, frameIndex);
            }

            // Rendering counters
            snapshot.renderingStats.Add(ReadRenderingCounters(frameData));
        }

        // Worker thread taramasi — ileride Development Build profiling icin
        // (Editor profiling'de worker thread'ler job verisi icermez)
        ScanWorkerThreads(frameIndex, childrenBuffer, snapshot);

        // Debug: Ilk frame sonrasi debug loglamayi kapat
        if (!_ecsChildDebugLogged)
            _ecsChildDebugLogged = true;
    }

    // ═══════════════════════════════════════════════════════════
    // HIERARCHY TRAVERSAL (BFS) — with GC Call Chain tracking
    // ═══════════════════════════════════════════════════════════

    private void TraverseHierarchy(
        HierarchyFrameDataView frameData,
        int rootId,
        List<int> childrenBuffer,
        int startDepth,
        AnalysisSnapshot snapshot,
        int currentFrameIndex)
    {
        // BFS queue: (itemId, depth, nearestUserAncestor, nearestEcsSystem)
        var queue = new Queue<(int id, int depth, string nearestUserAncestor, string nearestEcsSystem)>();
        queue.Enqueue((rootId, startDepth, null, null));

        while (queue.Count > 0)
        {
            var (itemId, depth, nearestUserAncestor, nearestEcsSystem) = queue.Dequeue();

            string name = frameData.GetItemName(itemId);
            float selfTimeMs = frameData.GetItemColumnDataAsFloat(itemId,
                HierarchyFrameDataView.columnSelfTime);
            float totalTimeMs = frameData.GetItemColumnDataAsFloat(itemId,
                HierarchyFrameDataView.columnTotalTime);
            int calls = (int)frameData.GetItemColumnDataAsFloat(itemId,
                HierarchyFrameDataView.columnCalls);
            float gcBytes = frameData.GetItemColumnDataAsFloat(itemId,
                HierarchyFrameDataView.columnGcMemory);

            // Update nearestUserAncestor for children
            string ancestorForChildren = nearestUserAncestor;
            if (!string.IsNullOrEmpty(name) && name.StartsWith(USER_CODE_PREFIX, StringComparison.Ordinal))
            {
                ancestorForChildren = name;
            }

            // Record function data (include selfTime>0 OR gcBytes>0 to catch GC-only functions)
            if (!string.IsNullOrEmpty(name) && (selfTimeMs > 0 || gcBytes > 0))
            {
                if (!snapshot.functionMap.TryGetValue(name, out var entry))
                {
                    entry = new FunctionProfileEntry { name = name };
                    snapshot.functionMap[name] = entry;
                }

                entry.totalSelfTimeMs += selfTimeMs;
                entry.totalCalls += calls;
                entry.totalGcBytes += gcBytes;
                entry.frameCount++;
                if (selfTimeMs > entry.maxSelfTimeMs)
                    entry.maxSelfTimeMs = selfTimeMs;
            }

            // ECS sistem tespiti
            string ecsName = ExtractSystemName(name);
            string ecsForChildren = nearestEcsSystem;
            if (ecsName != null)
            {
                ecsForChildren = ecsName;
                if (!snapshot.ecsSystemMap.TryGetValue(ecsName, out var ecsEntry))
                {
                    ecsEntry = new EcsSystemEntry
                    {
                        systemName = ecsName,
                        groupName = KNOWN_ECS_SYSTEMS[ecsName]
                    };
                    snapshot.ecsSystemMap[ecsName] = ecsEntry;
                }
                ecsEntry.totalTimeMs += totalTimeMs;
                ecsEntry.totalSelfTimeMs += selfTimeMs;
                ecsEntry.totalGcBytes += gcBytes;
                ecsEntry.frameCount++;
                if (totalTimeMs > ecsEntry.maxTimeMs) ecsEntry.maxTimeMs = (float)totalTimeMs;
                RecordEcsFrameData(snapshot, currentFrameIndex, ecsName, totalTimeMs);

                // Child time = Total - Self (job bekleme + ECB + diger child sureler)
                float childTimeMs = totalTimeMs - selfTimeMs;
                if (childTimeMs > 0)
                {
                    ecsEntry.jobTimeMs += childTimeMs;
                    if (childTimeMs > ecsEntry.maxJobTimeMs)
                        ecsEntry.maxJobTimeMs = childTimeMs;
                    RecordEcsJobFrameData(snapshot, currentFrameIndex, ecsName, childTimeMs);
                }
            }

            // Sync point tespiti (bilinen ECS sistem altindaysa)
            if (nearestEcsSystem != null && IsSyncPointMarker(name) && selfTimeMs > 0)
            {
                if (snapshot.ecsSystemMap.TryGetValue(nearestEcsSystem, out var parentSys))
                {
                    parentSys.syncPointTimeMs += selfTimeMs;
                }
            }

            // Debug: ECS sistem altindaki anlamli child marker'lari logla (ilk frame)
            if (nearestEcsSystem != null && ecsName == null
                && !_ecsChildDebugLogged && !string.IsNullOrEmpty(name) && totalTimeMs >= 0.05f)
            {
                _discoveredEcsChildren.Add($"[CHILD] {nearestEcsSystem} → \"{name}\" ({totalTimeMs:F3}ms)");
            }

            // GC Call Chain tracking
            if (gcBytes > 0 && !string.IsNullOrEmpty(name)
                && !name.StartsWith(USER_CODE_PREFIX, StringComparison.Ordinal)
                && nearestUserAncestor != null)
            {
                string chainKey = $"{nearestUserAncestor}||{name}";
                if (!snapshot.gcCallChains.TryGetValue(chainKey, out var chainEntry))
                {
                    chainEntry = new GcCallChainEntry
                    {
                        userCodeCaller = nearestUserAncestor,
                        gcProducer = name,
                        totalGcBytes = 0,
                        totalCalls = 0,
                        occurrences = 0
                    };
                    snapshot.gcCallChains[chainKey] = chainEntry;
                }

                chainEntry.totalGcBytes += gcBytes;
                chainEntry.totalCalls += calls;
                chainEntry.occurrences++;
            }

            // Track Update/LateUpdate/FixedUpdate
            for (int i = 0; i < UPDATE_METHOD_NAMES.Length; i++)
            {
                if (name == UPDATE_METHOD_NAMES[i])
                {
                    snapshot.updateMethodStats[i].totalTimeMs += totalTimeMs;
                    snapshot.updateMethodStats[i].totalGcBytes += gcBytes;
                    snapshot.updateMethodStats[i].frameCount++;

                    CollectUpdateContributors(frameData, itemId, snapshot.updateMethodStats[i], childrenBuffer);
                }
            }

            // Enqueue children if within depth limit and node is significant
            if (depth < MAX_HIERARCHY_DEPTH && totalTimeMs >= MIN_NODE_TIME_MS)
            {
                childrenBuffer.Clear();
                frameData.GetItemChildren(itemId, childrenBuffer);

                for (int i = 0; i < childrenBuffer.Count; i++)
                {
                    queue.Enqueue((childrenBuffer[i], depth + 1, ancestorForChildren, ecsForChildren));
                }
            }
        }
    }

    private void CollectUpdateContributors(
        HierarchyFrameDataView frameData,
        int parentId,
        UpdateMethodStats stats,
        List<int> childrenBuffer)
    {
        childrenBuffer.Clear();
        frameData.GetItemChildren(parentId, childrenBuffer);

        for (int i = 0; i < childrenBuffer.Count; i++)
        {
            int childId = childrenBuffer[i];
            string childName = frameData.GetItemName(childId);
            float childTotalMs = frameData.GetItemColumnDataAsFloat(childId,
                HierarchyFrameDataView.columnTotalTime);
            float childGcBytes = frameData.GetItemColumnDataAsFloat(childId,
                HierarchyFrameDataView.columnGcMemory);

            if (!string.IsNullOrEmpty(childName) && childTotalMs > 0.01f)
            {
                stats.topContributors.Add(new UpdateContributorData
                {
                    name = childName,
                    timeMs = childTotalMs,
                    gcBytes = childGcBytes
                });
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // WORKER THREAD TARAMASI — Job surelerini yakala
    // ═══════════════════════════════════════════════════════════

    // Debug: Ilk analizde bulunan thread ve job bilgilerini logla
    private bool _workerThreadDebugLogged;
    private HashSet<string> _discoveredThreadNames = new();
    private HashSet<string> _discoveredJobMarkers = new();

    // Debug: Main thread'deki ECS sistem child marker'larini logla
    private bool _ecsChildDebugLogged;
    private HashSet<string> _discoveredEcsChildren = new();

    private void ScanWorkerThreads(int frameIndex, List<int> childrenBuffer, AnalysisSnapshot snapshot)
    {
        int invalidCount = 0;
        // Thread 1'den baslayarak tum thread'leri tara
        for (int threadIdx = 1; threadIdx < 64; threadIdx++)
        {
            using (var threadData = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, threadIdx,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnTotalTime, false))
            {
                if (threadData == null || !threadData.valid)
                {
                    invalidCount++;
                    // Ardisik 5 invalid thread gorursek dur (gap'ler olabilir, break yerine tolerans)
                    if (invalidCount >= 5)
                        break;
                    continue;
                }
                invalidCount = 0; // Valid thread goruldu, sayaci sifirla

                string threadName = threadData.threadName ?? "";

                // Debug: Thread isimlerini topla (sadece ilk frame)
                if (!_workerThreadDebugLogged)
                    _discoveredThreadNames.Add(threadName);

                // Sadece worker/job thread'lerini tara
                if (!threadName.Contains("Worker") && !threadName.Contains("Job"))
                    continue;

                int rootId = threadData.GetRootItemID();
                if (rootId == -1) continue;

                TraverseWorkerThread(threadData, rootId, childrenBuffer, snapshot, frameIndex);
            }
        }

        // Debug log: Ilk frame'de bulunan thread ve marker bilgilerini logla
        if (!_workerThreadDebugLogged)
        {
            _workerThreadDebugLogged = true;
            Debug.Log($"[ProfilerAnalyzer V6] Worker thread taramasi — Bulunan thread'ler ({_discoveredThreadNames.Count}):\n" +
                string.Join("\n", _discoveredThreadNames.Select(n => $"  - \"{n}\"")));
            if (_discoveredJobMarkers.Count > 0)
                Debug.Log($"[ProfilerAnalyzer V6] Eslesen job marker'lar:\n" +
                    string.Join("\n", _discoveredJobMarkers.Select(n => $"  - \"{n}\"")));
            else
                Debug.Log("[ProfilerAnalyzer V6] Worker thread'lerde hicbir bilinen job marker bulunamadi.");
        }
    }

    private void TraverseWorkerThread(
        HierarchyFrameDataView threadData,
        int rootId,
        List<int> childrenBuffer,
        AnalysisSnapshot snapshot,
        int frameIndex)
    {
        var queue = new Queue<(int id, int depth)>();
        queue.Enqueue((rootId, 0));

        while (queue.Count > 0)
        {
            var (itemId, depth) = queue.Dequeue();
            string name = threadData.GetItemName(itemId);
            float totalTimeMs = threadData.GetItemColumnDataAsFloat(itemId,
                HierarchyFrameDataView.columnTotalTime);

            // Job marker tespiti
            string jobName = ExtractJobName(name);
            if (jobName != null && KNOWN_ECS_JOBS.TryGetValue(jobName, out string systemName))
            {
                // Debug: Eslesen marker'i kaydet
                if (!_workerThreadDebugLogged)
                    _discoveredJobMarkers.Add($"{name} → {jobName} → {systemName}");

                if (snapshot.ecsSystemMap.TryGetValue(systemName, out var sysEntry))
                {
                    sysEntry.jobTimeMs += totalTimeMs;
                    if (totalTimeMs > sysEntry.maxJobTimeMs)
                        sysEntry.maxJobTimeMs = totalTimeMs;

                    // Per-frame job timing
                    RecordEcsJobFrameData(snapshot, frameIndex, systemName, totalTimeMs);
                }
            }
            else if (!_workerThreadDebugLogged && !string.IsNullOrEmpty(name)
                && totalTimeMs >= 0.1f) // Sadece anlamli sureleri logla
            {
                // Debug: Bilinmeyen ama anlamli marker'lari raporla
                _discoveredJobMarkers.Add($"[UNKNOWN] \"{name}\" ({totalTimeMs:F3}ms)");
            }

            // Children (sadece 4 seviye yeterli — job hierarchy derin degil)
            if (depth < 4 && totalTimeMs >= MIN_NODE_TIME_MS)
            {
                childrenBuffer.Clear();
                threadData.GetItemChildren(itemId, childrenBuffer);
                for (int i = 0; i < childrenBuffer.Count; i++)
                    queue.Enqueue((childrenBuffer[i], depth + 1));
            }
        }
    }

    private static string ExtractJobName(string markerName)
    {
        if (string.IsNullOrEmpty(markerName)) return null;
        string name = markerName;
        // Namespace temizle: "DeadWalls.CollisionJob" → "CollisionJob"
        int dotIdx = name.LastIndexOf('.');
        if (dotIdx >= 0) name = name.Substring(dotIdx + 1);
        // Suffix temizle: "CollisionJob.Execute()" → "CollisionJob"
        int parenIdx = name.IndexOf('(');
        if (parenIdx > 0) name = name.Substring(0, parenIdx);
        return KNOWN_ECS_JOBS.ContainsKey(name) ? name : null;
    }

    private void RecordEcsJobFrameData(AnalysisSnapshot snapshot, int frameIndex, string systemName, float timeMs)
    {
        var list = snapshot.ecsFrameTimeline;
        if (list.Count == 0 || list[list.Count - 1].frameIndex != frameIndex) return;
        var fd = list[list.Count - 1];
        if (fd.jobTimesMs == null) fd.jobTimesMs = new Dictionary<string, float>();
        fd.jobTimesMs.TryGetValue(systemName, out float existing);
        fd.jobTimesMs[systemName] = existing + timeMs; // Birden fazla worker'dan toplanabilir
    }

    // ═══════════════════════════════════════════════════════════
    // RENDERING COUNTERS
    // ═══════════════════════════════════════════════════════════

    private RenderingFrameStats ReadRenderingCounters(HierarchyFrameDataView frameData)
    {
        var stats = new RenderingFrameStats();

        try
        {
            stats.drawCalls = GetCounterValue(frameData, "Draw Calls Count");
            stats.batches = GetCounterValue(frameData, "Batches Count");
            stats.setPassCalls = GetCounterValue(frameData, "SetPass Calls Count");
            stats.triangles = GetCounterValue(frameData, "Triangles Count");
            stats.vertices = GetCounterValue(frameData, "Vertices Count");
            stats.shadowCasters = GetCounterValue(frameData, "Shadow Casters Count");
            stats.dynamicBatches = GetCounterValue(frameData, "Dynamic Batches Count");
            stats.staticBatches = GetCounterValue(frameData, "Static Batches Count");
            stats.instancedBatches = GetCounterValue(frameData, "Instanced Batches Count");
            stats.visibleSkinnedMeshes = GetCounterValue(frameData, "Visible Skinned Meshes Count");
            stats.isValid = true;
        }
        catch
        {
            stats.isValid = false;
        }

        return stats;
    }

    private long GetCounterValue(HierarchyFrameDataView frameData, string counterName)
    {
        int markerId = frameData.GetMarkerId(counterName);
        if (markerId == -1)
            return -1;

        return frameData.GetCounterValueAsInt(markerId);
    }

    // ═══════════════════════════════════════════════════════════
    // CACHE ANALYSIS RESULTS
    // ═══════════════════════════════════════════════════════════

    private void CacheAnalysisResults(AnalysisSnapshot s)
    {
        // Frame timing stats
        if (s.frameTimings.Count > 0)
        {
            s.avgCpu = (float)s.frameTimings.Average(f => f.cpuMs);
            s.minCpu = s.frameTimings.Min(f => f.cpuMs);
            s.maxCpu = s.frameTimings.Max(f => f.cpuMs);
            s.avgGpu = (float)s.frameTimings.Average(f => f.gpuMs);
            s.minGpu = s.frameTimings.Min(f => f.gpuMs);
            s.maxGpu = s.frameTimings.Max(f => f.gpuMs);
            s.avgFps = s.frameTimings.Average(f => f.fps);
            s.minFps = s.frameTimings.Min(f => f.fps);
            s.maxFps = s.frameTimings.Max(f => f.fps);
        }

        // Top spikes
        s.topSpikes = s.frameTimings
            .OrderByDescending(f => f.cpuMs)
            .Take(TOP_SPIKE_COUNT)
            .ToList();

        // Top functions (all)
        s.topFunctions = s.functionMap.Values
            .OrderByDescending(f => f.totalSelfTimeMs)
            .Take(TOP_FUNCTION_COUNT)
            .ToList();

        // User code functions
        s.userCodeFunctions = s.functionMap.Values
            .Where(f => IsUserCode(f.name))
            .OrderByDescending(f => f.totalSelfTimeMs)
            .Take(TOP_FUNCTION_COUNT)
            .ToList();

        s.totalUserFunctions = s.functionMap.Values.Count(f => IsUserCode(f.name));

        // Top GC allocators
        s.topGcAllocators = s.functionMap.Values
            .Where(f => f.totalGcBytes > 0)
            .OrderByDescending(f => f.totalGcBytes)
            .Take(TOP_GC_ALLOCATOR_COUNT)
            .ToList();

        // Top GC frames
        s.topGcFrames = s.frameTimings
            .Where(f => f.gcBytes > 0)
            .OrderByDescending(f => f.gcBytes)
            .Take(TOP_GC_FRAME_COUNT)
            .ToList();

        // Total GC
        s.totalGcBytes = s.functionMap.Values.Sum(f => f.totalGcBytes);

        // FPS distribution
        s.fpsDistribution = ComputeFpsDistribution(s.frameTimings);

        // GC Call Chain groups
        s.gcCallChainGroups = s.gcCallChains.Values
            .GroupBy(e => e.userCodeCaller)
            .Select(g => new GcCallChainGroup
            {
                userCodeCaller = g.Key,
                totalGcBytes = g.Sum(e => e.totalGcBytes),
                children = g.OrderByDescending(e => e.totalGcBytes)
                    .Take(TOP_GC_CHAIN_CHILDREN)
                    .ToList()
            })
            .OrderByDescending(g => g.totalGcBytes)
            .Take(TOP_GC_CHAIN_CALLERS)
            .ToList();

        // ECS derived cache — sync point threshold hesapla
        foreach (var entry in s.ecsSystemMap.Values)
        {
            float avgSync = entry.frameCount > 0 ? (float)(entry.syncPointTimeMs / entry.frameCount) : 0;
            entry.hasSyncPoint = avgSync >= SYNC_POINT_THRESHOLD_MS;
        }

        s.ecsSystemsSorted = s.ecsSystemMap.Values
            .OrderByDescending(e => e.totalTimeMs).ToList();
        s.physicsPipelineSystems = new List<EcsSystemEntry>();
        foreach (var n in PHYSICS_PIPELINE_SYSTEMS)
            if (s.ecsSystemMap.TryGetValue(n, out var e)) s.physicsPipelineSystems.Add(e);
        if (s.ecsFrameTimeline.Count > 0)
        {
            s.avgPhysicsPipelineMs = (float)s.ecsFrameTimeline.Average(f => f.totalPipelineTimeMs);
            s.maxPhysicsPipelineMs = s.ecsFrameTimeline.Max(f => f.totalPipelineTimeMs);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ANALYSIS HELPERS
    // ═══════════════════════════════════════════════════════════

    private static Dictionary<string, int> ComputeFpsDistribution(List<FrameTimingData> timings)
    {
        var buckets = new Dictionary<string, int>
        {
            { "<15 FPS", 0 },
            { "15-30 FPS", 0 },
            { "30-60 FPS", 0 },
            { "60-90 FPS", 0 },
            { "90+ FPS", 0 }
        };

        foreach (var frame in timings)
        {
            if (frame.fps < 15f) buckets["<15 FPS"]++;
            else if (frame.fps < 30f) buckets["15-30 FPS"]++;
            else if (frame.fps < 60f) buckets["30-60 FPS"]++;
            else if (frame.fps < 90f) buckets["60-90 FPS"]++;
            else buckets["90+ FPS"]++;
        }

        return buckets;
    }

    private static bool IsUserCode(string functionName)
    {
        if (string.IsNullOrEmpty(functionName))
            return false;

        return functionName.StartsWith(USER_CODE_PREFIX, StringComparison.Ordinal);
    }

    private static string ExtractSystemName(string markerName)
    {
        if (string.IsNullOrEmpty(markerName)) return null;
        string name = markerName;
        int nsIdx = name.LastIndexOf('.');
        if (nsIdx >= 0) name = name.Substring(nsIdx + 1);
        // "System.OnUpdate()" gibi suffix'leri temizle
        int parenIdx = name.IndexOf('(');
        if (parenIdx > 0) name = name.Substring(0, parenIdx);
        return KNOWN_ECS_SYSTEMS.ContainsKey(name) ? name : null;
    }

    private const float SYNC_POINT_THRESHOLD_MS = 0.5f;

    private static bool IsSyncPointMarker(string name)
    {
        if (name == null) return false;
        // Sadece gercek sync/bekleme marker'larini yakala
        // "JobHandle.Complete" standart ECS akisi — onu degil,
        // CompleteDependency, Sync Point gibi blokleyici olanlari hedefle
        return name.Contains("Sync Point")
            || name.Contains("CompleteDependency")
            || name.Contains("CompleteAllJobs");
    }

    private void RecordEcsFrameData(AnalysisSnapshot snapshot, int frameIndex, string systemName, float timeMs)
    {
        var list = snapshot.ecsFrameTimeline;
        EcsFrameData fd;
        if (list.Count > 0 && list[list.Count - 1].frameIndex == frameIndex)
            fd = list[list.Count - 1];
        else
        {
            fd = new EcsFrameData { frameIndex = frameIndex, systemTimesMs = new Dictionary<string, float>() };
            list.Add(fd);
        }
        fd.systemTimesMs[systemName] = timeMs;
        float total = 0f;
        foreach (var ps in PHYSICS_PIPELINE_SYSTEMS)
            if (fd.systemTimesMs.TryGetValue(ps, out float t)) total += t;
        fd.totalPipelineTimeMs = total;
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 0: OVERVIEW
    // ═══════════════════════════════════════════════════════════

    private void DrawOverviewContent()
    {
        // Info header
        EditorGUILayout.LabelField("Analiz Bilgileri", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Kaynak: {_activeSnapshot?.filePath ?? ""}");
        EditorGUILayout.LabelField($"Frame aralığı: {_firstFrame} - {_lastFrame} ({_totalFrames} frame)");
        EditorGUILayout.LabelField($"Atlanan: {_skippedFrames} | Analiz edilen: {_totalFrames - _skippedFrames}");
        EditorGUILayout.Space(8);

        if (_frameTimings == null || _frameTimings.Count == 0)
        {
            EditorGUILayout.HelpBox("Frame verisi yok.", MessageType.Warning);
            return;
        }

        // CPU / GPU / FPS table
        EditorGUILayout.LabelField("Frame Timing", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        bool hasGpu = _avgGpu > 0;

        // Header row
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(120));
        GUILayout.Label("Ortalama", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("Min", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("Max", EditorStyles.boldLabel, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        // CPU row
        DrawOverviewMetricRow("CPU (ms)", FormatMs(_avgCpu), FormatMs(_minCpu), FormatMs(_maxCpu));

        // GPU row
        if (hasGpu)
            DrawOverviewMetricRow("GPU (ms)", FormatMs(_avgGpu), FormatMs(_minGpu), FormatMs(_maxGpu));
        else
            DrawOverviewMetricRow("GPU (ms)", "N/A", "N/A", "N/A");

        // FPS row
        DrawOverviewMetricRow("FPS", $"{_avgFps:F1}", $"{_minFps:F1}", $"{_maxFps:F1}");

        EditorGUILayout.Space(12);

        // FPS Distribution (bar graph with HelpBox)
        EditorGUILayout.LabelField("FPS Dağılımı", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (_fpsDistribution != null)
        {
            foreach (var bucket in _fpsDistribution)
            {
                float percentage = _frameTimings.Count > 0
                    ? (float)bucket.Value / _frameTimings.Count * 100f
                    : 0f;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(bucket.Key, GUILayout.Width(80));
                GUILayout.Label($"{bucket.Value}", GUILayout.Width(60));
                GUILayout.Label($"({percentage:F1}%)", GUILayout.Width(60));

                // Bar visualization
                Rect barRect = GUILayoutUtility.GetRect(200, 16, GUILayout.ExpandWidth(true));
                float barWidth = barRect.width * (percentage / 100f);

                Color barColor;
                if (bucket.Key == "<15 FPS") barColor = new Color(0.9f, 0.2f, 0.2f);
                else if (bucket.Key == "15-30 FPS") barColor = new Color(0.9f, 0.6f, 0.2f);
                else if (bucket.Key == "30-60 FPS") barColor = new Color(0.9f, 0.9f, 0.2f);
                else barColor = new Color(0.2f, 0.8f, 0.2f);

                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y + 2, barWidth, 12), barColor);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.y + 2, barRect.width, 12),
                    new Color(0.3f, 0.3f, 0.3f, 0.2f));

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(8);

        // GC Summary
        EditorGUILayout.LabelField("GC Özet", EditorStyles.boldLabel);
        double avgGcPerFrame = _frameTimings.Count > 0 ? _totalGcBytes / _frameTimings.Count : 0;
        EditorGUILayout.LabelField($"Toplam GC: {FormatBytes((long)_totalGcBytes)} | Frame başı ort: {FormatBytes((long)avgGcPerFrame)}");
    }

    private void DrawOverviewMetricRow(string label, string avg, string min, string max)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        GUILayout.Label(avg, GUILayout.Width(90));
        GUILayout.Label(min, GUILayout.Width(90));
        GUILayout.Label(max, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 1: SPIKES
    // ═══════════════════════════════════════════════════════════

    private void DrawSpikesContent()
    {
        if (_topSpikes == null || _topSpikes.Count == 0)
        {
            EditorGUILayout.HelpBox("Spike verisi yok.", MessageType.Info);
            return;
        }

        // Spike vs general comparison
        float avgSpikeMs = (float)_topSpikes.Average(s => s.cpuMs);
        float ratio = _avgCpu > 0 ? avgSpikeMs / _avgCpu : 0;
        EditorGUILayout.HelpBox(
            $"Spike ortalaması: {FormatMs(avgSpikeMs)} | Genel ort: {FormatMs(_avgCpu)} | {ratio:F1}x daha yavaş",
            MessageType.Warning);

        EditorGUILayout.Space(4);

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(35));
        GUILayout.Label("Frame", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("CPU (ms)", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("GPU (ms)", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("FPS", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("GC", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _topSpikes.Count; i++)
        {
            var s = _topSpikes[i];
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"{i + 1}", GUILayout.Width(35));
            GUILayout.Label($"{s.frameIndex}", GUILayout.Width(70));

            GUI.color = GetSeverityColor(s.cpuMs, CPU_WARNING_MS * 16.67f, CPU_CRITICAL_MS * 16.67f);
            GUILayout.Label(FormatMs(s.cpuMs), GUILayout.Width(90));
            GUI.color = Color.white;

            GUILayout.Label(s.gpuMs > 0 ? FormatMs(s.gpuMs) : "N/A", GUILayout.Width(90));

            GUI.color = s.fps < 15f ? Color.red : s.fps < 30f ? Color.yellow : Color.white;
            GUILayout.Label($"{s.fps:F1}", GUILayout.Width(70));
            GUI.color = Color.white;

            GUI.color = GetSeverityColor((float)s.gcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
            GUILayout.Label(FormatBytes((long)s.gcBytes), GUILayout.Width(80));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 2: FUNCTIONS (ALL)
    // ═══════════════════════════════════════════════════════════

    private void DrawFunctionsContent()
    {
        if (_topFunctions == null || _topFunctions.Count == 0)
        {
            EditorGUILayout.HelpBox("Fonksiyon verisi yok.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Top {_topFunctions.Count} fonksiyon (self time sıralı)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(400));
        GUILayout.Label("Toplam (ms)", EditorStyles.boldLabel, GUILayout.Width(85));
        GUILayout.Label("Ort (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Max (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Çağrı", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("GC", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _topFunctions.Count; i++)
        {
            var f = _topFunctions[i];
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(f.name, GUILayout.Width(400));

            GUI.color = GetSeverityColor((float)f.totalSelfTimeMs, CPU_WARNING_MS * 100, CPU_CRITICAL_MS * 100);
            GUILayout.Label($"{f.totalSelfTimeMs:F2}", GUILayout.Width(85));
            GUI.color = Color.white;

            GUILayout.Label($"{f.AverageSelfTimeMs:F3}", GUILayout.Width(70));
            GUILayout.Label($"{f.maxSelfTimeMs:F3}", GUILayout.Width(70));
            GUILayout.Label($"{f.totalCalls}", GUILayout.Width(70));

            GUI.color = GetSeverityColor((float)f.totalGcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
            GUILayout.Label(f.totalGcBytes > 0 ? FormatBytes((long)f.totalGcBytes) : "-", GUILayout.Width(80));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 3: UPDATE BREAKDOWN
    // ═══════════════════════════════════════════════════════════

    private void DrawUpdateContent()
    {
        if (_updateMethodStats == null)
        {
            EditorGUILayout.HelpBox("Update verisi yok.", MessageType.Info);
            return;
        }

        for (int i = 0; i < _updateMethodStats.Length; i++)
        {
            var s = _updateMethodStats[i];
            float avgMs = s.frameCount > 0 ? (float)(s.totalTimeMs / s.frameCount) : 0;
            double avgGc = s.frameCount > 0 ? s.totalGcBytes / s.frameCount : 0;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(s.displayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Toplam: {s.totalTimeMs:F2} ms | Frame başı: {FormatMs(avgMs)} | Frame sayısı: {s.frameCount}");
            EditorGUILayout.LabelField($"GC Toplam: {FormatBytes((long)s.totalGcBytes)} | GC Frame başı: {FormatBytes((long)avgGc)}");

            if (s.topContributors.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Top Contributors:", EditorStyles.boldLabel);

                // Aggregate contributors
                var aggregated = s.topContributors
                    .GroupBy(c => c.name)
                    .Select(g => new
                    {
                        Name = g.Key,
                        TotalMs = g.Sum(x => x.timeMs),
                        TotalGc = g.Sum(x => x.gcBytes)
                    })
                    .OrderByDescending(x => x.TotalMs)
                    .Take(10)
                    .ToList();

                // Contributor header
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("#", EditorStyles.miniLabel, GUILayout.Width(25));
                GUILayout.Label("Fonksiyon", EditorStyles.miniLabel, GUILayout.Width(350));
                GUILayout.Label("Süre (ms)", EditorStyles.miniLabel, GUILayout.Width(80));
                GUILayout.Label("%", EditorStyles.miniLabel, GUILayout.Width(50));
                GUILayout.Label("GC", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                for (int j = 0; j < aggregated.Count; j++)
                {
                    var c = aggregated[j];
                    float pct = s.totalTimeMs > 0 ? (float)(c.TotalMs / s.totalTimeMs * 100) : 0;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"{j + 1}", GUILayout.Width(25));
                    GUILayout.Label(c.Name, GUILayout.Width(350));
                    GUILayout.Label($"{c.TotalMs:F2}", GUILayout.Width(80));
                    GUILayout.Label($"{pct:F1}%", GUILayout.Width(50));

                    GUI.color = GetSeverityColor((float)c.TotalGc, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
                    GUILayout.Label(c.TotalGc > 0 ? FormatBytes((long)c.TotalGc) : "-", GUILayout.Width(80));
                    GUI.color = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 4: USER CODE
    // ═══════════════════════════════════════════════════════════

    private void DrawUserCodeContent()
    {
        if (_userCodeFunctions == null || _userCodeFunctions.Count == 0)
        {
            EditorGUILayout.HelpBox("Assembly-CSharp.dll fonksiyonu bulunamadı.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(
            $"Toplam {_totalUserFunctions} kullanıcı fonksiyonu (ilk {_userCodeFunctions.Count} gösteriliyor)",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(400));
        GUILayout.Label("Toplam (ms)", EditorStyles.boldLabel, GUILayout.Width(85));
        GUILayout.Label("Ort (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Max (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Çağrı", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("GC", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _userCodeFunctions.Count; i++)
        {
            var f = _userCodeFunctions[i];
            string displayName = StripUserCodePrefix(f.name);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(displayName, GUILayout.Width(400));

            GUI.color = GetSeverityColor((float)f.totalSelfTimeMs, CPU_WARNING_MS * 100, CPU_CRITICAL_MS * 100);
            GUILayout.Label($"{f.totalSelfTimeMs:F2}", GUILayout.Width(85));
            GUI.color = Color.white;

            GUILayout.Label($"{f.AverageSelfTimeMs:F3}", GUILayout.Width(70));
            GUILayout.Label($"{f.maxSelfTimeMs:F3}", GUILayout.Width(70));
            GUILayout.Label($"{f.totalCalls}", GUILayout.Width(70));

            GUI.color = GetSeverityColor((float)f.totalGcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
            GUILayout.Label(f.totalGcBytes > 0 ? FormatBytes((long)f.totalGcBytes) : "-", GUILayout.Width(80));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 5: RENDERING
    // ═══════════════════════════════════════════════════════════

    private void DrawRenderingContent()
    {
        if (_renderingStats == null)
        {
            EditorGUILayout.HelpBox("Rendering verisi yok.", MessageType.Info);
            return;
        }

        var validStats = _renderingStats.Where(s => s.isValid).ToList();

        if (validStats.Count == 0)
        {
            EditorGUILayout.HelpBox("Rendering counter'ları bulunamadı.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Geçerli frame sayısı: {validStats.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Metrik", EditorStyles.boldLabel, GUILayout.Width(180));
        GUILayout.Label("Ortalama", EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.Label("Min", EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.Label("Max", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        DrawRenderingMetricRow("Draw Calls", validStats, s => s.drawCalls, false);
        DrawRenderingMetricRow("Batches", validStats, s => s.batches, false);
        DrawRenderingMetricRow("SetPass Calls", validStats, s => s.setPassCalls, false);
        DrawRenderingMetricRow("Triangles", validStats, s => s.triangles, true);
        DrawRenderingMetricRow("Vertices", validStats, s => s.vertices, true);
        DrawRenderingMetricRow("Shadow Casters", validStats, s => s.shadowCasters, false);
        DrawRenderingMetricRow("Dynamic Batches", validStats, s => s.dynamicBatches, false);
        DrawRenderingMetricRow("Static Batches", validStats, s => s.staticBatches, false);
        DrawRenderingMetricRow("Instanced Batches", validStats, s => s.instancedBatches, false);
        DrawRenderingMetricRow("Skinned Meshes", validStats, s => s.visibleSkinnedMeshes, false);
    }

    private void DrawRenderingMetricRow(
        string label,
        List<RenderingFrameStats> stats,
        Func<RenderingFrameStats, long> selector,
        bool useLargeFormat)
    {
        var values = stats.Select(selector).Where(v => v >= 0).ToList();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(180));

        if (values.Count == 0)
        {
            GUILayout.Label("N/A", GUILayout.Width(120));
            GUILayout.Label("N/A", GUILayout.Width(120));
            GUILayout.Label("N/A", GUILayout.Width(120));
        }
        else
        {
            double avg = values.Average(v => (double)v);
            long min = values.Min();
            long max = values.Max();

            string avgStr = useLargeFormat ? FormatLargeNumber((long)avg) : ((long)avg).ToString("N0");
            string minStr = useLargeFormat ? FormatLargeNumber(min) : min.ToString("N0");
            string maxStr = useLargeFormat ? FormatLargeNumber(max) : max.ToString("N0");

            GUILayout.Label(avgStr, GUILayout.Width(120));
            GUILayout.Label(minStr, GUILayout.Width(120));
            GUILayout.Label(maxStr, GUILayout.Width(120));
        }

        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 6: GC ANALYSIS
    // ═══════════════════════════════════════════════════════════

    private void DrawGcAnalysisContent()
    {
        if (_functionMap == null || _frameTimings == null)
        {
            EditorGUILayout.HelpBox("GC verisi yok.", MessageType.Info);
            return;
        }

        // Summary
        double avgGcPerFrame = _frameTimings.Count > 0 ? _totalGcBytes / _frameTimings.Count : 0;

        EditorGUILayout.LabelField("GC Allocation Özet", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Toplam GC: {FormatBytes((long)_totalGcBytes)}");
        EditorGUILayout.LabelField($"Frame başı ortalama: {FormatBytes((long)avgGcPerFrame)}");

        // User vs Engine split
        double userGcBytes = _functionMap.Values
            .Where(f => IsUserCode(f.name))
            .Sum(f => f.totalGcBytes);
        double engineGcBytes = _totalGcBytes - userGcBytes;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("GC Kaynağı Dağılımı:", EditorStyles.boldLabel);
        float userPct = _totalGcBytes > 0 ? (float)(userGcBytes / _totalGcBytes * 100) : 0;
        float enginePct = _totalGcBytes > 0 ? (float)(engineGcBytes / _totalGcBytes * 100) : 0;
        EditorGUILayout.LabelField($"  Kullanıcı kodu: {FormatBytes((long)userGcBytes)} ({userPct:F1}%)");
        EditorGUILayout.LabelField($"  Engine/System: {FormatBytes((long)engineGcBytes)} ({enginePct:F1}%)");

        EditorGUILayout.Space(8);

        // Top GC Allocators (with GC/Call column)
        if (_topGcAllocators != null && _topGcAllocators.Count > 0)
        {
            EditorGUILayout.LabelField($"Top {_topGcAllocators.Count} GC Allocator:", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(350));
            GUILayout.Label("Toplam GC", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("GC/Call", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Çağrı", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("%", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("User?", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _topGcAllocators.Count; i++)
            {
                var f = _topGcAllocators[i];
                float pct = _totalGcBytes > 0 ? (float)(f.totalGcBytes / _totalGcBytes * 100) : 0;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}", GUILayout.Width(30));
                GUILayout.Label(f.name, GUILayout.Width(350));

                GUI.color = GetSeverityColor((float)f.totalGcBytes, GC_WARNING_BYTES * 10, GC_CRITICAL_BYTES * 10);
                GUILayout.Label(FormatBytes((long)f.totalGcBytes), GUILayout.Width(90));
                GUI.color = Color.white;

                GUILayout.Label(FormatBytes((long)f.GcPerCall), GUILayout.Width(80));
                GUILayout.Label($"{f.totalCalls}", GUILayout.Width(70));
                GUILayout.Label($"{pct:F1}%", GUILayout.Width(50));
                GUILayout.Label(IsUserCode(f.name) ? "EVET" : "-", GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(8);

        // User Code GC Direct Allocators (Top 20)
        if (_functionMap != null)
        {
            var userGcAllocators = _functionMap.Values
                .Where(f => IsUserCode(f.name) && f.totalGcBytes > 0)
                .OrderByDescending(f => f.totalGcBytes)
                .Take(TOP_GC_ALLOCATOR_COUNT)
                .ToList();

            if (userGcAllocators.Count > 0)
            {
                EditorGUILayout.LabelField($"User Code GC Direkt Allocator (Top {userGcAllocators.Count}):", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
                GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(350));
                GUILayout.Label("Toplam GC", EditorStyles.boldLabel, GUILayout.Width(90));
                GUILayout.Label("GC/Call", EditorStyles.boldLabel, GUILayout.Width(80));
                GUILayout.Label("Çağrı", EditorStyles.boldLabel, GUILayout.Width(70));
                GUILayout.Label("Self (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < userGcAllocators.Count; i++)
                {
                    var f = userGcAllocators[i];
                    string displayName = StripUserCodePrefix(f.name);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}", GUILayout.Width(30));
                    GUILayout.Label(displayName, GUILayout.Width(350));

                    GUI.color = GetSeverityColor((float)f.totalGcBytes, GC_WARNING_BYTES * 10, GC_CRITICAL_BYTES * 10);
                    GUILayout.Label(FormatBytes((long)f.totalGcBytes), GUILayout.Width(90));
                    GUI.color = Color.white;

                    GUILayout.Label(FormatBytes((long)f.GcPerCall), GUILayout.Width(80));
                    GUILayout.Label($"{f.totalCalls}", GUILayout.Width(70));
                    GUILayout.Label($"{f.totalSelfTimeMs:F2}", GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(8);
            }
        }

        // Top GC Frames
        if (_topGcFrames != null && _topGcFrames.Count > 0)
        {
            EditorGUILayout.LabelField($"Top {_topGcFrames.Count} GC Frame:", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.Label("Frame", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("GC", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("CPU (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("FPS", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < _topGcFrames.Count; i++)
            {
                var f = _topGcFrames[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}", GUILayout.Width(30));
                GUILayout.Label($"{f.frameIndex}", GUILayout.Width(70));

                GUI.color = GetSeverityColor((float)f.gcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
                GUILayout.Label(FormatBytes((long)f.gcBytes), GUILayout.Width(90));
                GUI.color = Color.white;

                GUILayout.Label(FormatMs(f.cpuMs), GUILayout.Width(80));
                GUILayout.Label($"{f.fps:F1}", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 7: CALL CHAINS
    // ═══════════════════════════════════════════════════════════

    private void DrawCallChainsContent()
    {
        if (_gcCallChainGroups == null || _gcCallChainGroups.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "GC Call Chain verisi bulunamadı.\n\n" +
                "Bu tab, Assembly-CSharp.dll kodunuzun çağırdığı engine/system fonksiyonlarının\n" +
                "GC allocation'larını gösterir. User Code → Engine GC Producer zincirleri.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(
            $"User Code → GC Producer Zincirleri (Top {_gcCallChainGroups.Count} caller)",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Her grup, bir Assembly-CSharp fonksiyonunun alt ağacında GC allocation yapan engine/system çağrılarını gösterir.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        for (int i = 0; i < _gcCallChainGroups.Count; i++)
        {
            var group = _gcCallChainGroups[i];
            string displayCaller = StripUserCodePrefix(group.userCodeCaller);
            string foldoutKey = group.userCodeCaller;

            bool isExpanded = _expandedChains.Contains(foldoutKey);

            EditorGUILayout.BeginVertical("box");

            // Group header (foldout)
            EditorGUILayout.BeginHorizontal();

            bool newExpanded = EditorGUILayout.Foldout(isExpanded, "", true, EditorStyles.foldout);
            if (newExpanded != isExpanded)
            {
                if (newExpanded)
                    _expandedChains.Add(foldoutKey);
                else
                    _expandedChains.Remove(foldoutKey);
            }

            GUILayout.Label(displayCaller, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            GUI.color = GetSeverityColor((float)group.totalGcBytes, GC_WARNING_BYTES * 10, GC_CRITICAL_BYTES * 10);
            GUILayout.Label($"Toplam GC: {FormatBytes((long)group.totalGcBytes)}", GUILayout.Width(160));
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // Children (if expanded) — with GC/Call info
            if (newExpanded && group.children != null)
            {
                EditorGUI.indentLevel += 2;

                for (int j = 0; j < group.children.Count; j++)
                {
                    var child = group.children[j];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    GUILayout.Label($"└── {child.gcProducer}", GUILayout.Width(400));

                    GUI.color = GetSeverityColor((float)child.totalGcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
                    GUILayout.Label(FormatBytes((long)child.totalGcBytes), GUILayout.Width(90));
                    GUI.color = Color.white;

                    GUILayout.Label($"({child.totalCalls} call, {FormatBytes((long)child.GcPerCall)}/call, {child.occurrences} frame)", GUILayout.Width(220));
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel -= 2;
            }

            EditorGUILayout.EndVertical();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 9: ECS PIPELINE
    // ═══════════════════════════════════════════════════════════

    private void DrawEcsPipelineContent()
    {
        var snapshot = _activeSnapshot;
        if (snapshot == null || snapshot.ecsSystemsSorted == null || snapshot.ecsSystemsSorted.Count == 0)
        {
            EditorGUILayout.HelpBox("ECS sistem verisi bulunamadi. Profiler verisinde bilinen DeadWalls ECS sistemi tespit edilemedi.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"ECS Sistemleri ({snapshot.ecsSystemsSorted.Count} sistem tespit edildi)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Toplam/Ort/Max = main thread inclusive sure (dispatch + job bekleme dahil).\n" +
            "Self = sadece sistemin kendi kodu (schedule, ECB, vs.).\n" +
            "Child = Toplam - Self (job bekleme + child marker sureleri).\n" +
            "Toplam >> Self ise zamanin cogu job beklemede harcaniyordur.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        // Grup bazli gosterim
        var groups = snapshot.ecsSystemsSorted.GroupBy(e => e.groupName).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            EditorGUILayout.LabelField($"  {group.Key}", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Sistem", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label("Toplam (ms)", EditorStyles.boldLabel, GUILayout.Width(85));
            GUILayout.Label("Ort (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Max (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Self (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Child Avg", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Child Max", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("GC", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Label("Sync", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            foreach (var sys in group.OrderByDescending(e => e.totalTimeMs))
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(sys.systemName, GUILayout.Width(200));

                GUI.color = GetSeverityColor((float)sys.totalTimeMs, CPU_WARNING_MS * 100, CPU_CRITICAL_MS * 100);
                GUILayout.Label($"{sys.totalTimeMs:F2}", GUILayout.Width(85));
                GUI.color = Color.white;

                GUI.color = GetSeverityColor(sys.AvgTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                GUILayout.Label($"{sys.AvgTimeMs:F3}", GUILayout.Width(70));
                GUI.color = Color.white;

                GUI.color = GetSeverityColor(sys.maxTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                GUILayout.Label($"{sys.maxTimeMs:F3}", GUILayout.Width(70));
                GUI.color = Color.white;

                GUILayout.Label($"{sys.totalSelfTimeMs:F2}", GUILayout.Width(70));

                // Child Avg
                if (sys.jobTimeMs > 0)
                {
                    GUI.color = GetSeverityColor(sys.AvgJobTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                    GUILayout.Label($"{sys.AvgJobTimeMs:F3}", GUILayout.Width(70));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("-", GUILayout.Width(70));
                }

                // Child Max
                if (sys.maxJobTimeMs > 0)
                {
                    GUI.color = GetSeverityColor(sys.maxJobTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                    GUILayout.Label($"{sys.maxJobTimeMs:F3}", GUILayout.Width(70));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("-", GUILayout.Width(70));
                }

                GUI.color = GetSeverityColor((float)sys.totalGcBytes, GC_WARNING_BYTES, GC_CRITICAL_BYTES);
                GUILayout.Label(sys.totalGcBytes > 0 ? FormatBytes((long)sys.totalGcBytes) : "-", GUILayout.Width(70));
                GUI.color = Color.white;

                if (sys.hasSyncPoint)
                {
                    GUI.color = new Color(1f, 0.5f, 0.2f);
                    GUILayout.Label("BLOCK", GUILayout.Width(50));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("-", GUILayout.Width(50));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 10: PHYSICS BREAKDOWN
    // ═══════════════════════════════════════════════════════════

    private static readonly Color[] PHYSICS_COLORS =
    {
        new Color(0.3f, 0.8f, 0.3f),   // ApplyMovementForce: yesil
        new Color(0.9f, 0.9f, 0.2f),   // BuildSpatialHash: sari
        new Color(0.9f, 0.3f, 0.3f),   // PhysicsCollision: kirmizi
        new Color(0.3f, 0.5f, 0.9f),   // Integrate: mavi
        new Color(0.9f, 0.6f, 0.2f),   // Boundary: turuncu
    };

    private void DrawPhysicsBreakdownContent()
    {
        var snapshot = _activeSnapshot;
        if (snapshot == null || snapshot.physicsPipelineSystems == null || snapshot.physicsPipelineSystems.Count == 0)
        {
            EditorGUILayout.HelpBox("Physics pipeline verisi bulunamadi.", MessageType.Info);
            return;
        }

        // Pipeline ozeti
        EditorGUILayout.LabelField("Physics Pipeline Ozeti", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Ortalama pipeline suresi: {FormatMs(snapshot.avgPhysicsPipelineMs)} | Max: {FormatMs(snapshot.maxPhysicsPipelineMs)}");
        EditorGUILayout.Space(8);

        // Stacked bar chart
        EditorGUILayout.LabelField("Pipeline Dagilimi (Stacked Bar)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        double totalPipelineTime = snapshot.physicsPipelineSystems.Sum(s => s.totalTimeMs);

        // Renk aciklamasi
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < snapshot.physicsPipelineSystems.Count && i < PHYSICS_COLORS.Length; i++)
        {
            var sys = snapshot.physicsPipelineSystems[i];
            float pct = totalPipelineTime > 0 ? (float)(sys.totalTimeMs / totalPipelineTime * 100) : 0;
            GUI.color = PHYSICS_COLORS[i];
            GUILayout.Label($"  {sys.systemName} ({pct:F1}%)", GUILayout.Width(180));
        }
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);

        // Stacked bar
        Rect barRect = GUILayoutUtility.GetRect(100, 28, GUILayout.ExpandWidth(true));
        float barX = barRect.x;
        for (int i = 0; i < snapshot.physicsPipelineSystems.Count && i < PHYSICS_COLORS.Length; i++)
        {
            var sys = snapshot.physicsPipelineSystems[i];
            float pct = totalPipelineTime > 0 ? (float)(sys.totalTimeMs / totalPipelineTime) : 0;
            float segmentWidth = barRect.width * pct;
            if (segmentWidth > 0)
            {
                EditorGUI.DrawRect(new Rect(barX, barRect.y, segmentWidth, barRect.height), PHYSICS_COLORS[i]);
                barX += segmentWidth;
            }
        }
        // arka plan
        EditorGUI.DrawRect(new Rect(barX, barRect.y, barRect.width - (barX - barRect.x), barRect.height),
            new Color(0.2f, 0.2f, 0.2f, 0.3f));

        EditorGUILayout.Space(12);

        // Detay tablosu
        EditorGUILayout.LabelField("Sistem Detaylari (calisma sirasina gore)", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Sistem", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Label("Ort (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Max (ms)", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Child Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Child Max", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Toplam (ms)", EditorStyles.boldLabel, GUILayout.Width(85));
        GUILayout.Label("%", EditorStyles.boldLabel, GUILayout.Width(50));
        GUILayout.Label("Sync", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < snapshot.physicsPipelineSystems.Count; i++)
        {
            var sys = snapshot.physicsPipelineSystems[i];
            float pct = totalPipelineTime > 0 ? (float)(sys.totalTimeMs / totalPipelineTime * 100) : 0;

            EditorGUILayout.BeginHorizontal();

            GUI.color = (i < PHYSICS_COLORS.Length) ? PHYSICS_COLORS[i] : Color.white;
            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(sys.systemName, GUILayout.Width(200));
            GUI.color = Color.white;

            GUI.color = GetSeverityColor(sys.AvgTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
            GUILayout.Label($"{sys.AvgTimeMs:F3}", GUILayout.Width(70));
            GUI.color = Color.white;

            GUI.color = GetSeverityColor(sys.maxTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
            GUILayout.Label($"{sys.maxTimeMs:F3}", GUILayout.Width(70));
            GUI.color = Color.white;

            // Child Avg (Total - Self)
            if (sys.jobTimeMs > 0)
            {
                GUI.color = GetSeverityColor(sys.AvgJobTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                GUILayout.Label($"{sys.AvgJobTimeMs:F3}", GUILayout.Width(70));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("-", GUILayout.Width(70));
            }

            // Child Max
            if (sys.maxJobTimeMs > 0)
            {
                GUI.color = GetSeverityColor(sys.maxJobTimeMs, CPU_WARNING_MS, CPU_CRITICAL_MS);
                GUILayout.Label($"{sys.maxJobTimeMs:F3}", GUILayout.Width(70));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("-", GUILayout.Width(70));
            }

            GUILayout.Label($"{sys.totalTimeMs:F2}", GUILayout.Width(85));
            GUILayout.Label($"{pct:F1}%", GUILayout.Width(50));

            if (sys.hasSyncPoint)
            {
                GUI.color = new Color(1f, 0.5f, 0.2f);
                GUILayout.Label("BLOCK", GUILayout.Width(50));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("-", GUILayout.Width(50));
            }

            EditorGUILayout.EndHorizontal();
        }

        // Sync point uyarisi
        bool anySyncPoint = snapshot.physicsPipelineSystems.Any(s => s.hasSyncPoint);
        if (anySyncPoint)
        {
            EditorGUILayout.Space(8);
            var syncSystems = snapshot.physicsPipelineSystems.Where(s => s.hasSyncPoint).ToList();
            string syncNames = string.Join(", ", syncSystems.Select(s => s.systemName));
            double syncTotal = syncSystems.Sum(s => s.syncPointTimeMs);
            EditorGUILayout.HelpBox(
                $"Sync Point Tespit Edildi!\n\n" +
                $"Sistemler: {syncNames}\n" +
                $"Toplam sync bekleme: {FormatMs((float)syncTotal)}\n\n" +
                "Bu sistemlerdeki .Complete() cagrilari main thread'i bloklayarak paralelligi kirabilir.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(12);

        // Top 10 en yavas pipeline frame'leri
        if (snapshot.ecsFrameTimeline != null && snapshot.ecsFrameTimeline.Count > 0)
        {
            EditorGUILayout.LabelField("Top 10 En Yavas Pipeline Frame", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var topFrames = snapshot.ecsFrameTimeline
                .OrderByDescending(f => f.totalPipelineTimeMs)
                .Take(10)
                .ToList();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
            GUILayout.Label("Frame", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Pipeline", EditorStyles.boldLabel, GUILayout.Width(70));
            foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
            {
                string shortName = psName.Replace("System", "");
                GUILayout.Label(shortName, EditorStyles.boldLabel, GUILayout.Width(80));
            }
            GUILayout.Label("| Child Tot", EditorStyles.boldLabel, GUILayout.Width(80));
            foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
            {
                string shortName = psName.Replace("System", "");
                GUILayout.Label("C:" + shortName.Substring(0, Math.Min(6, shortName.Length)), EditorStyles.boldLabel, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < topFrames.Count; i++)
            {
                var fd = topFrames[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}", GUILayout.Width(30));
                GUILayout.Label($"{fd.frameIndex}", GUILayout.Width(60));

                GUI.color = GetSeverityColor(fd.totalPipelineTimeMs, CPU_WARNING_MS * 5, CPU_CRITICAL_MS * 5);
                GUILayout.Label(FormatMs(fd.totalPipelineTimeMs), GUILayout.Width(70));
                GUI.color = Color.white;

                foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                {
                    fd.systemTimesMs.TryGetValue(psName, out float sysTime);
                    GUI.color = GetSeverityColor(sysTime, CPU_WARNING_MS, CPU_CRITICAL_MS);
                    GUILayout.Label(sysTime > 0 ? FormatMs(sysTime) : "-", GUILayout.Width(80));
                    GUI.color = Color.white;
                }

                // Child timing kolonu (Total - Self)
                float jobTotal = 0;
                if (fd.jobTimesMs != null)
                    foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                        if (fd.jobTimesMs.TryGetValue(psName, out float jt)) jobTotal += jt;
                GUI.color = GetSeverityColor(jobTotal, CPU_WARNING_MS * 5, CPU_CRITICAL_MS * 5);
                GUILayout.Label(jobTotal > 0 ? FormatMs(jobTotal) : "-", GUILayout.Width(80));
                GUI.color = Color.white;

                foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                {
                    float jobTime = 0;
                    if (fd.jobTimesMs != null) fd.jobTimesMs.TryGetValue(psName, out jobTime);
                    GUI.color = GetSeverityColor(jobTime, CPU_WARNING_MS, CPU_CRITICAL_MS);
                    GUILayout.Label(jobTime > 0 ? FormatMs(jobTime) : "-", GUILayout.Width(70));
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TAB 8: COMPARE (V3)
    // ═══════════════════════════════════════════════════════════

    private void DrawCompareTab()
    {
        _scrollPositions[8] = EditorGUILayout.BeginScrollView(_scrollPositions[8]);

        if (_snapshotA == null || _snapshotB == null)
        {
            EditorGUILayout.HelpBox(
                "Karşılaştırma için iki snapshot gereklidir.\n\n" +
                "1. \"Load A\" ile fix öncesi .raw dosyasını yükleyin\n" +
                "2. \"Load B\" ile fix sonrası .raw dosyasını yükleyin\n\n" +
                "Her iki snapshot yüklendikten sonra karşılaştırma tablosu burada görünecektir.",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        var a = _snapshotA;
        var b = _snapshotB;

        // ── Section 1: File Info ──
        EditorGUILayout.LabelField("Dosya Bilgileri", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"A (Before): {Path.GetFileName(a.filePath)}  —  {a.totalFrames - a.skippedFrames} frame");
        EditorGUILayout.LabelField($"B (After):  {Path.GetFileName(b.filePath)}  —  {b.totalFrames - b.skippedFrames} frame");
        EditorGUILayout.Space(8);

        // ── Section 2: Quick Summary ──
        EditorGUILayout.LabelField("Özet Karşılaştırma", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Metrik", EditorStyles.boldLabel, GUILayout.Width(140));
        GUILayout.Label("A (Before)", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("B (After)", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(120));
        GUILayout.Label("Değişim", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        DrawCompareRow("CPU Avg (ms)", a.avgCpu, b.avgCpu, true, FormatMs);
        DrawCompareRow("CPU Max (ms)", a.maxCpu, b.maxCpu, true, FormatMs);
        DrawCompareRow("GPU Avg (ms)", a.avgGpu, b.avgGpu, true, FormatMs);
        DrawCompareRow("FPS Avg", a.avgFps, b.avgFps, false, v => $"{v:F1}");
        DrawCompareRow("FPS Min", a.minFps, b.minFps, false, v => $"{v:F1}");
        DrawCompareRowBytes("Total GC", a.totalGcBytes, b.totalGcBytes);

        EditorGUILayout.Space(12);

        // ── Section 3: Frame Timing Detail ──
        EditorGUILayout.LabelField("Frame Timing Detay", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("", GUILayout.Width(120));
        GUILayout.Label("A Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("A Min", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("A Max", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Min", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Max", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Δ Avg", EditorStyles.boldLabel, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        DrawCompareTimingRow("CPU (ms)", a.avgCpu, a.minCpu, a.maxCpu, b.avgCpu, b.minCpu, b.maxCpu, true);
        DrawCompareTimingRow("GPU (ms)", a.avgGpu, a.minGpu, a.maxGpu, b.avgGpu, b.minGpu, b.maxGpu, true);
        DrawCompareTimingRow("FPS", a.avgFps, a.minFps, a.maxFps, b.avgFps, b.minFps, b.maxFps, false);

        EditorGUILayout.Space(12);

        // ── Section 4: FPS Distribution ──
        if (a.fpsDistribution != null && b.fpsDistribution != null)
        {
            EditorGUILayout.LabelField("FPS Dağılımı Karşılaştırma", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Aralık", EditorStyles.boldLabel, GUILayout.Width(90));
            GUILayout.Label("A", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("A %", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("B", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("B %", EditorStyles.boldLabel, GUILayout.Width(60));
            GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            int aTotal = a.frameTimings?.Count ?? 1;
            int bTotal = b.frameTimings?.Count ?? 1;

            foreach (var key in a.fpsDistribution.Keys)
            {
                int aVal = a.fpsDistribution.ContainsKey(key) ? a.fpsDistribution[key] : 0;
                int bVal = b.fpsDistribution.ContainsKey(key) ? b.fpsDistribution[key] : 0;
                float aPct = aTotal > 0 ? (float)aVal / aTotal * 100f : 0;
                float bPct = bTotal > 0 ? (float)bVal / bTotal * 100f : 0;
                int delta = bVal - aVal;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(key, GUILayout.Width(90));
                GUILayout.Label($"{aVal}", GUILayout.Width(60));
                GUILayout.Label($"{aPct:F1}%", GUILayout.Width(60));
                GUILayout.Label($"{bVal}", GUILayout.Width(60));
                GUILayout.Label($"{bPct:F1}%", GUILayout.Width(60));

                // For FPS distribution: lower bad buckets = good, higher good buckets = good
                bool lowerIsBetter = key == "<15 FPS" || key == "15-30 FPS";
                Color deltaColor = delta == 0 ? Color.white :
                    (lowerIsBetter ? (delta < 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f))
                                   : (delta > 0 ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.9f, 0.3f, 0.3f)));
                GUI.color = deltaColor;
                string arrow = delta > 0 ? "↑" : delta < 0 ? "↓" : "—";
                GUILayout.Label($"{arrow} {delta:+0;-0;0}", GUILayout.Width(70));
                GUI.color = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);
        }

        // ── Section 5: GC Overview ──
        EditorGUILayout.LabelField("GC Özet Karşılaştırma", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        double aUserGc = a.functionMap.Values.Where(f => IsUserCode(f.name)).Sum(f => f.totalGcBytes);
        double bUserGc = b.functionMap.Values.Where(f => IsUserCode(f.name)).Sum(f => f.totalGcBytes);
        double aEngineGc = a.totalGcBytes - aUserGc;
        double bEngineGc = b.totalGcBytes - bUserGc;

        DrawCompareRowBytes("Toplam GC", a.totalGcBytes, b.totalGcBytes);
        DrawCompareRowBytes("User Code GC", aUserGc, bUserGc);
        DrawCompareRowBytes("Engine GC", aEngineGc, bEngineGc);

        double aAvgGcPerFrame = a.frameTimings.Count > 0 ? a.totalGcBytes / a.frameTimings.Count : 0;
        double bAvgGcPerFrame = b.frameTimings.Count > 0 ? b.totalGcBytes / b.frameTimings.Count : 0;
        DrawCompareRowBytes("GC/Frame Avg", aAvgGcPerFrame, bAvgGcPerFrame);

        EditorGUILayout.Space(12);

        // ── Section 6: Top GC Delta ──
        DrawCompareFunctionDelta("Top GC Delta (Top 20)", a, b, true, 20);

        EditorGUILayout.Space(12);

        // ── Section 7: Top CPU Delta ──
        DrawCompareFunctionDelta("Top CPU Delta (Top 20)", a, b, false, 20);

        EditorGUILayout.Space(12);

        // ── Section 8: User Code Delta ──
        DrawCompareUserCodeDelta("User Code Delta (Top 20)", a, b, 20);

        EditorGUILayout.Space(12);

        // ── Section 9: Update Method Comparison ──
        if (a.updateMethodStats != null && b.updateMethodStats != null)
        {
            EditorGUILayout.LabelField("Update Method Karşılaştırma", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Method", EditorStyles.boldLabel, GUILayout.Width(250));
            GUILayout.Label("A (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("B (ms)", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Δ (ms)", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("A GC", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("B GC", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Label("Δ GC", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            int count = Mathf.Min(a.updateMethodStats.Length, b.updateMethodStats.Length);
            for (int i = 0; i < count; i++)
            {
                var us = a.updateMethodStats[i];
                var ub = b.updateMethodStats[i];
                float aAvg = us.frameCount > 0 ? (float)(us.totalTimeMs / us.frameCount) : 0;
                float bAvg = ub.frameCount > 0 ? (float)(ub.totalTimeMs / ub.frameCount) : 0;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(us.displayName, GUILayout.Width(250));
                GUILayout.Label(FormatMs(aAvg), GUILayout.Width(80));
                GUILayout.Label(FormatMs(bAvg), GUILayout.Width(80));
                DrawDeltaLabel(aAvg, bAvg, true, 100);
                GUILayout.Label(FormatBytes((long)us.totalGcBytes), GUILayout.Width(80));
                GUILayout.Label(FormatBytes((long)ub.totalGcBytes), GUILayout.Width(80));
                DrawDeltaBytesLabel(us.totalGcBytes, ub.totalGcBytes, 100);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);
        }

        // ── Section 10: Rendering Comparison ──
        if (a.renderingStats != null && b.renderingStats != null)
        {
            var aValid = a.renderingStats.Where(s => s.isValid).ToList();
            var bValid = b.renderingStats.Where(s => s.isValid).ToList();

            if (aValid.Count > 0 && bValid.Count > 0)
            {
                EditorGUILayout.LabelField("Rendering Karşılaştırma", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("Metrik", EditorStyles.boldLabel, GUILayout.Width(160));
                GUILayout.Label("A Avg", EditorStyles.boldLabel, GUILayout.Width(90));
                GUILayout.Label("B Avg", EditorStyles.boldLabel, GUILayout.Width(90));
                GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(110));
                EditorGUILayout.EndHorizontal();

                DrawCompareRenderingRow("Draw Calls", aValid, bValid, s => s.drawCalls);
                DrawCompareRenderingRow("Batches", aValid, bValid, s => s.batches);
                DrawCompareRenderingRow("SetPass Calls", aValid, bValid, s => s.setPassCalls);
                DrawCompareRenderingRow("Triangles", aValid, bValid, s => s.triangles);
                DrawCompareRenderingRow("Vertices", aValid, bValid, s => s.vertices);
                DrawCompareRenderingRow("Shadow Casters", aValid, bValid, s => s.shadowCasters);
                DrawCompareRenderingRow("Dynamic Batches", aValid, bValid, s => s.dynamicBatches);
                DrawCompareRenderingRow("Static Batches", aValid, bValid, s => s.staticBatches);
                DrawCompareRenderingRow("Instanced Batches", aValid, bValid, s => s.instancedBatches);
                DrawCompareRenderingRow("Skinned Meshes", aValid, bValid, s => s.visibleSkinnedMeshes);

                EditorGUILayout.Space(12);
            }
        }

        // ── Section 11: Call Chain Delta ──
        DrawCompareCallChainDelta("Call Chain GC Delta (Top 15)", a, b, 15);

        EditorGUILayout.Space(12);

        // ── Section 12: ECS System Delta ──
        DrawCompareEcsSystemDelta("ECS System Delta", a, b);

        EditorGUILayout.Space(12);

        // ── Section 13: Physics Pipeline Delta ──
        DrawComparePhysicsPipelineDelta("Physics Pipeline Delta", a, b);

        EditorGUILayout.EndScrollView();
    }

    // ───────────────────────────────────────────────────────────
    // COMPARE TAB HELPERS
    // ───────────────────────────────────────────────────────────

    private void DrawCompareRow(string label, float aVal, float bVal, bool lowerIsBetter, Func<float, string> formatter)
    {
        float delta = bVal - aVal;
        float pct = aVal != 0 ? delta / aVal * 100f : 0;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        GUILayout.Label(formatter(aVal), GUILayout.Width(100));
        GUILayout.Label(formatter(bVal), GUILayout.Width(100));
        DrawDeltaLabel(aVal, bVal, lowerIsBetter, 120);
        GUILayout.Label($"{pct:+0.0;-0.0;0.0}%", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareRowBytes(string label, double aVal, double bVal)
    {
        double delta = bVal - aVal;
        float pct = aVal != 0 ? (float)(delta / aVal * 100) : 0;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(140));
        GUILayout.Label(FormatBytes((long)aVal), GUILayout.Width(100));
        GUILayout.Label(FormatBytes((long)bVal), GUILayout.Width(100));
        DrawDeltaBytesLabel(aVal, bVal, 120);
        GUILayout.Label($"{pct:+0.0;-0.0;0.0}%", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareTimingRow(string label, float aAvg, float aMin, float aMax,
        float bAvg, float bMin, float bMax, bool lowerIsBetter)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(120));
        GUILayout.Label(FormatMs(aAvg), GUILayout.Width(70));
        GUILayout.Label(FormatMs(aMin), GUILayout.Width(70));
        GUILayout.Label(FormatMs(aMax), GUILayout.Width(70));
        GUILayout.Label(FormatMs(bAvg), GUILayout.Width(70));
        GUILayout.Label(FormatMs(bMin), GUILayout.Width(70));
        GUILayout.Label(FormatMs(bMax), GUILayout.Width(70));
        DrawDeltaLabel(aAvg, bAvg, lowerIsBetter, 90);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDeltaLabel(float aVal, float bVal, bool lowerIsBetter, float width)
    {
        float delta = bVal - aVal;
        bool isImprovement = lowerIsBetter ? delta < 0 : delta > 0;
        bool isRegression = lowerIsBetter ? delta > 0 : delta < 0;
        string arrow = delta > 0.001f ? "↑" : delta < -0.001f ? "↓" : "—";

        GUI.color = Mathf.Abs(delta) < 0.001f ? Color.white :
            isImprovement ? new Color(0.3f, 0.9f, 0.3f) :
            isRegression ? new Color(0.9f, 0.3f, 0.3f) : Color.white;
        GUILayout.Label($"{arrow} {delta:+0.00;-0.00;0.00}", GUILayout.Width(width));
        GUI.color = Color.white;
    }

    private void DrawDeltaBytesLabel(double aVal, double bVal, float width)
    {
        double delta = bVal - aVal;
        bool isImprovement = delta < 0;
        bool isRegression = delta > 0;
        string arrow = delta > 0 ? "↑" : delta < 0 ? "↓" : "—";
        string deltaStr = $"{arrow} {FormatBytes((long)Math.Abs(delta))}";

        GUI.color = Math.Abs(delta) < 1 ? Color.white :
            isImprovement ? new Color(0.3f, 0.9f, 0.3f) :
            isRegression ? new Color(0.9f, 0.3f, 0.3f) : Color.white;
        GUILayout.Label(deltaStr, GUILayout.Width(width));
        GUI.color = Color.white;
    }

    private void DrawCompareFunctionDelta(string title, AnalysisSnapshot a, AnalysisSnapshot b, bool byGc, int topCount)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Merge function names from both snapshots
        var allNames = new HashSet<string>();
        foreach (var kv in a.functionMap) allNames.Add(kv.Key);
        foreach (var kv in b.functionMap) allNames.Add(kv.Key);

        var deltas = new List<(string name, double aVal, double bVal, double delta)>();

        foreach (var name in allNames)
        {
            a.functionMap.TryGetValue(name, out var aEntry);
            b.functionMap.TryGetValue(name, out var bEntry);

            double aV = byGc ? (aEntry?.totalGcBytes ?? 0) : (aEntry?.totalSelfTimeMs ?? 0);
            double bV = byGc ? (bEntry?.totalGcBytes ?? 0) : (bEntry?.totalSelfTimeMs ?? 0);

            if (aV > 0 || bV > 0)
                deltas.Add((name, aV, bV, bV - aV));
        }

        // Sort by absolute delta descending
        deltas.Sort((x, y) => Math.Abs(y.delta).CompareTo(Math.Abs(x.delta)));
        int count = Math.Min(topCount, deltas.Count);

        if (count == 0)
        {
            EditorGUILayout.HelpBox("Delta verisi yok.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(350));
        GUILayout.Label("A", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("B", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < count; i++)
        {
            var d = deltas[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(d.name, GUILayout.Width(350));

            if (byGc)
            {
                GUILayout.Label(FormatBytes((long)d.aVal), GUILayout.Width(90));
                GUILayout.Label(FormatBytes((long)d.bVal), GUILayout.Width(90));
                DrawDeltaBytesLabel(d.aVal, d.bVal, 110);
            }
            else
            {
                GUILayout.Label($"{d.aVal:F2}", GUILayout.Width(90));
                GUILayout.Label($"{d.bVal:F2}", GUILayout.Width(90));
                DrawDeltaLabel((float)d.aVal, (float)d.bVal, true, 110);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCompareUserCodeDelta(string title, AnalysisSnapshot a, AnalysisSnapshot b, int topCount)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        var allNames = new HashSet<string>();
        foreach (var kv in a.functionMap)
            if (IsUserCode(kv.Key)) allNames.Add(kv.Key);
        foreach (var kv in b.functionMap)
            if (IsUserCode(kv.Key)) allNames.Add(kv.Key);

        var deltas = new List<(string name, double aCpu, double bCpu, double cpuDelta, double aGc, double bGc, double gcDelta)>();

        foreach (var name in allNames)
        {
            a.functionMap.TryGetValue(name, out var aEntry);
            b.functionMap.TryGetValue(name, out var bEntry);

            double aCpu = aEntry?.totalSelfTimeMs ?? 0;
            double bCpu = bEntry?.totalSelfTimeMs ?? 0;
            double aGc = aEntry?.totalGcBytes ?? 0;
            double bGc = bEntry?.totalGcBytes ?? 0;

            if (aCpu > 0 || bCpu > 0 || aGc > 0 || bGc > 0)
                deltas.Add((name, aCpu, bCpu, bCpu - aCpu, aGc, bGc, bGc - aGc));
        }

        deltas.Sort((x, y) => Math.Abs(y.cpuDelta).CompareTo(Math.Abs(x.cpuDelta)));
        int count = Math.Min(topCount, deltas.Count);

        if (count == 0)
        {
            EditorGUILayout.HelpBox("User code delta verisi yok.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("Fonksiyon", EditorStyles.boldLabel, GUILayout.Width(300));
        GUILayout.Label("A CPU", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B CPU", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Δ CPU", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("A GC", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B GC", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Δ GC", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < count; i++)
        {
            var d = deltas[i];
            string displayName = StripUserCodePrefix(d.name);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(displayName, GUILayout.Width(300));
            GUILayout.Label($"{d.aCpu:F2}", GUILayout.Width(70));
            GUILayout.Label($"{d.bCpu:F2}", GUILayout.Width(70));
            DrawDeltaLabel((float)d.aCpu, (float)d.bCpu, true, 90);
            GUILayout.Label(d.aGc > 0 ? FormatBytes((long)d.aGc) : "-", GUILayout.Width(70));
            GUILayout.Label(d.bGc > 0 ? FormatBytes((long)d.bGc) : "-", GUILayout.Width(70));
            DrawDeltaBytesLabel(d.aGc, d.bGc, 100);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCompareRenderingRow(string label, List<RenderingFrameStats> aStats,
        List<RenderingFrameStats> bStats, Func<RenderingFrameStats, long> selector)
    {
        var aValues = aStats.Select(selector).Where(v => v >= 0).ToList();
        var bValues = bStats.Select(selector).Where(v => v >= 0).ToList();

        double aAvg = aValues.Count > 0 ? aValues.Average(v => (double)v) : 0;
        double bAvg = bValues.Count > 0 ? bValues.Average(v => (double)v) : 0;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(160));
        GUILayout.Label(aValues.Count > 0 ? $"{(long)aAvg:N0}" : "N/A", GUILayout.Width(90));
        GUILayout.Label(bValues.Count > 0 ? $"{(long)bAvg:N0}" : "N/A", GUILayout.Width(90));

        if (aValues.Count > 0 && bValues.Count > 0)
            DrawDeltaLabel((float)aAvg, (float)bAvg, true, 110);
        else
            GUILayout.Label("—", GUILayout.Width(110));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCompareCallChainDelta(string title, AnalysisSnapshot a, AnalysisSnapshot b, int topCount)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        if ((a.gcCallChainGroups == null || a.gcCallChainGroups.Count == 0) &&
            (b.gcCallChainGroups == null || b.gcCallChainGroups.Count == 0))
        {
            EditorGUILayout.HelpBox("Call chain verisi yok.", MessageType.Info);
            return;
        }

        // Build caller → totalGcBytes maps
        var aMap = new Dictionary<string, double>();
        var bMap = new Dictionary<string, double>();

        if (a.gcCallChainGroups != null)
            foreach (var g in a.gcCallChainGroups) aMap[g.userCodeCaller] = g.totalGcBytes;
        if (b.gcCallChainGroups != null)
            foreach (var g in b.gcCallChainGroups) bMap[g.userCodeCaller] = g.totalGcBytes;

        var allCallers = new HashSet<string>();
        foreach (var k in aMap.Keys) allCallers.Add(k);
        foreach (var k in bMap.Keys) allCallers.Add(k);

        var deltas = new List<(string caller, double aVal, double bVal, double delta)>();
        foreach (var caller in allCallers)
        {
            aMap.TryGetValue(caller, out double aV);
            bMap.TryGetValue(caller, out double bV);
            deltas.Add((caller, aV, bV, bV - aV));
        }

        deltas.Sort((x, y) => Math.Abs(y.delta).CompareTo(Math.Abs(x.delta)));
        int count = Math.Min(topCount, deltas.Count);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("User Code Caller", EditorStyles.boldLabel, GUILayout.Width(350));
        GUILayout.Label("A GC", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("B GC", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("Δ GC", EditorStyles.boldLabel, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < count; i++)
        {
            var d = deltas[i];
            string displayName = StripUserCodePrefix(d.caller);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(displayName, GUILayout.Width(350));
            GUILayout.Label(FormatBytes((long)d.aVal), GUILayout.Width(90));
            GUILayout.Label(FormatBytes((long)d.bVal), GUILayout.Width(90));
            DrawDeltaBytesLabel(d.aVal, d.bVal, 110);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawCompareEcsSystemDelta(string title, AnalysisSnapshot a, AnalysisSnapshot b)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        if ((a.ecsSystemsSorted == null || a.ecsSystemsSorted.Count == 0) &&
            (b.ecsSystemsSorted == null || b.ecsSystemsSorted.Count == 0))
        {
            EditorGUILayout.HelpBox("ECS sistem verisi yok.", MessageType.Info);
            return;
        }

        // Tum sistem adlarini birlestir
        var allNames = new HashSet<string>();
        if (a.ecsSystemMap != null)
            foreach (var k in a.ecsSystemMap.Keys) allNames.Add(k);
        if (b.ecsSystemMap != null)
            foreach (var k in b.ecsSystemMap.Keys) allNames.Add(k);

        var deltas = new List<(string name, float aAvg, float bAvg, float delta, float aJobAvg, float bJobAvg, float jobDelta)>();
        foreach (var name in allNames)
        {
            float aAvg = 0, bAvg = 0, aJobAvg = 0, bJobAvg = 0;
            if (a.ecsSystemMap != null && a.ecsSystemMap.TryGetValue(name, out var ae)) { aAvg = ae.AvgTimeMs; aJobAvg = ae.AvgJobTimeMs; }
            if (b.ecsSystemMap != null && b.ecsSystemMap.TryGetValue(name, out var be)) { bAvg = be.AvgTimeMs; bJobAvg = be.AvgJobTimeMs; }
            deltas.Add((name, aAvg, bAvg, bAvg - aAvg, aJobAvg, bJobAvg, bJobAvg - aJobAvg));
        }

        deltas.Sort((x, y) => Math.Abs(y.delta).CompareTo(Math.Abs(x.delta)));

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("#", EditorStyles.boldLabel, GUILayout.Width(30));
        GUILayout.Label("ECS Sistem", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Label("A Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("A Child", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Child", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Child Δ", EditorStyles.boldLabel, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < deltas.Count; i++)
        {
            var d = deltas[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{i + 1}", GUILayout.Width(30));
            GUILayout.Label(d.name, GUILayout.Width(200));
            GUILayout.Label(FormatMs(d.aAvg), GUILayout.Width(70));
            GUILayout.Label(FormatMs(d.bAvg), GUILayout.Width(70));
            DrawDeltaLabel(d.aAvg, d.bAvg, true, 90);

            // Job Avg delta
            GUILayout.Label(d.aJobAvg > 0 ? FormatMs(d.aJobAvg) : "-", GUILayout.Width(70));
            GUILayout.Label(d.bJobAvg > 0 ? FormatMs(d.bJobAvg) : "-", GUILayout.Width(70));
            if (d.aJobAvg > 0 || d.bJobAvg > 0)
                DrawDeltaLabel(d.aJobAvg, d.bJobAvg, true, 90);
            else
                GUILayout.Label("-", GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawComparePhysicsPipelineDelta(string title, AnalysisSnapshot a, AnalysisSnapshot b)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        bool aHas = a.physicsPipelineSystems != null && a.physicsPipelineSystems.Count > 0;
        bool bHas = b.physicsPipelineSystems != null && b.physicsPipelineSystems.Count > 0;

        if (!aHas && !bHas)
        {
            EditorGUILayout.HelpBox("Physics pipeline verisi yok.", MessageType.Info);
            return;
        }

        // Pipeline Avg/Max karsilastirma
        DrawCompareRow("Pipeline Avg (ms)", a.avgPhysicsPipelineMs, b.avgPhysicsPipelineMs, true, FormatMs);
        DrawCompareRow("Pipeline Max (ms)", a.maxPhysicsPipelineMs, b.maxPhysicsPipelineMs, true, FormatMs);

        EditorGUILayout.Space(8);

        // Sistem bazinda before/after delta (main thread + job)
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Sistem", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Label("A Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Avg", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Delta", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("A Child", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("B Child", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label("Child Δ", EditorStyles.boldLabel, GUILayout.Width(90));
        EditorGUILayout.EndHorizontal();

        foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
        {
            float aAvg = 0, bAvg = 0, aJobAvg = 0, bJobAvg = 0;
            if (a.ecsSystemMap != null && a.ecsSystemMap.TryGetValue(psName, out var ae)) { aAvg = ae.AvgTimeMs; aJobAvg = ae.AvgJobTimeMs; }
            if (b.ecsSystemMap != null && b.ecsSystemMap.TryGetValue(psName, out var be)) { bAvg = be.AvgTimeMs; bJobAvg = be.AvgJobTimeMs; }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(psName, GUILayout.Width(200));
            GUILayout.Label(FormatMs(aAvg), GUILayout.Width(70));
            GUILayout.Label(FormatMs(bAvg), GUILayout.Width(70));
            DrawDeltaLabel(aAvg, bAvg, true, 90);

            GUILayout.Label(aJobAvg > 0 ? FormatMs(aJobAvg) : "-", GUILayout.Width(70));
            GUILayout.Label(bJobAvg > 0 ? FormatMs(bJobAvg) : "-", GUILayout.Width(70));
            if (aJobAvg > 0 || bJobAvg > 0)
                DrawDeltaLabel(aJobAvg, bJobAvg, true, 90);
            else
                GUILayout.Label("-", GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        // Stacked bar A vs B yan yana
        if (aHas && bHas)
        {
            EditorGUILayout.LabelField("Pipeline Dagilimi (A vs B)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            double aTotalPipeline = a.physicsPipelineSystems.Sum(s => s.totalTimeMs);
            double bTotalPipeline = b.physicsPipelineSystems.Sum(s => s.totalTimeMs);

            // Bar A
            GUILayout.Label("A:", EditorStyles.miniLabel);
            Rect barA = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(true));
            float xA = barA.x;
            for (int i = 0; i < PHYSICS_PIPELINE_SYSTEMS.Length && i < PHYSICS_COLORS.Length; i++)
            {
                float pct = 0;
                if (a.ecsSystemMap != null && a.ecsSystemMap.TryGetValue(PHYSICS_PIPELINE_SYSTEMS[i], out var ae))
                    pct = aTotalPipeline > 0 ? (float)(ae.totalTimeMs / aTotalPipeline) : 0;
                float w = barA.width * pct;
                if (w > 0) EditorGUI.DrawRect(new Rect(xA, barA.y, w, barA.height), PHYSICS_COLORS[i]);
                xA += w;
            }
            EditorGUI.DrawRect(new Rect(xA, barA.y, barA.width - (xA - barA.x), barA.height), new Color(0.2f, 0.2f, 0.2f, 0.3f));

            // Bar B
            GUILayout.Label("B:", EditorStyles.miniLabel);
            Rect barB = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(true));
            float xB = barB.x;
            for (int i = 0; i < PHYSICS_PIPELINE_SYSTEMS.Length && i < PHYSICS_COLORS.Length; i++)
            {
                float pct = 0;
                if (b.ecsSystemMap != null && b.ecsSystemMap.TryGetValue(PHYSICS_PIPELINE_SYSTEMS[i], out var be))
                    pct = bTotalPipeline > 0 ? (float)(be.totalTimeMs / bTotalPipeline) : 0;
                float w = barB.width * pct;
                if (w > 0) EditorGUI.DrawRect(new Rect(xB, barB.y, w, barB.height), PHYSICS_COLORS[i]);
                xB += w;
            }
            EditorGUI.DrawRect(new Rect(xB, barB.y, barB.width - (xB - barB.x), barB.height), new Color(0.2f, 0.2f, 0.2f, 0.3f));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // EXPORT REPORT
    // ═══════════════════════════════════════════════════════════

    private void ExportReport()
    {
        if (!_hasData)
        {
            EditorUtility.DisplayDialog("Profiler Analyzer", "Önce analiz yapın.", "OK");
            return;
        }

        try
        {
            string report = GenerateTextReport();

            string directory = Path.GetDirectoryName(REPORT_OUTPUT_PATH);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(REPORT_OUTPUT_PATH, report, Encoding.UTF8);

            EditorUtility.DisplayDialog("Profiler Analyzer",
                $"Rapor kaydedildi:\n{REPORT_OUTPUT_PATH}", "OK");

            Debug.Log($"[ProfilerAnalyzer] Rapor kaydedildi: {REPORT_OUTPUT_PATH}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProfilerAnalyzer] Rapor kaydedilemedi: {ex.Message}");
            EditorUtility.DisplayDialog("Profiler Analyzer",
                $"Dosya yazılamadı:\n{ex.Message}", "OK");
        }
    }

    private int _reportSectionCounter;

    private string NextSectionNumber()
    {
        _reportSectionCounter++;
        return _reportSectionCounter.ToString();
    }

    private string GenerateTextReport()
    {
        _reportSectionCounter = 0;
        var sb = new StringBuilder(32768);

        sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           UNITY PROFILER DATA ANALYSIS REPORT                ║");
        sb.AppendLine("║           DeadWalls — Performance Analysis                   ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Tarih          : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  Kaynak dosya A : {_snapshotA?.filePath ?? ""}");
        if (_snapshotB != null)
            sb.AppendLine($"  Kaynak dosya B : {_snapshotB.filePath}");
        sb.AppendLine($"  Frame aralığı  : {_snapshotA?.firstFrame ?? 0} - {_snapshotA?.lastFrame ?? 0} ({_snapshotA?.totalFrames ?? 0} frame)");
        sb.AppendLine($"  Atlanan frame  : {_snapshotA?.skippedFrames ?? 0}");
        sb.AppendLine($"  Analiz edilen  : {(_snapshotA?.totalFrames ?? 0) - (_snapshotA?.skippedFrames ?? 0)}");
        sb.AppendLine();

        AppendFrameTimingOverview(sb);
        AppendTopSlowestFrames(sb);
        AppendTopExpensiveFunctions(sb);
        AppendUpdateBreakdown(sb);
        AppendUserCodeFunctions(sb);
        AppendRenderingStatistics(sb);
        AppendGcAllocationAnalysis(sb);
        AppendCallChains(sb);
        if (_snapshotB != null)
            AppendCompareSection(sb);
        AppendEcsPipelineSection(sb);
        AppendPhysicsBreakdownSection(sb);

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                      RAPOR SONU");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    // ───────────────────────────────────────────────────────────
    // TEXT REPORT SECTIONS
    // ───────────────────────────────────────────────────────────

    private void AppendFrameTimingOverview(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. FRAME TIMING OVERVIEW");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_frameTimings.Count == 0)
        {
            sb.AppendLine("  Veri yok.");
            sb.AppendLine();
            return;
        }

        bool hasGpuData = _avgGpu > 0;

        sb.AppendLine($"  Toplam Frame    : {_frameTimings.Count}");
        sb.AppendLine();
        sb.AppendLine($"  {"",- 18}{"Ortalama",10}{"Min",10}{"Max",10}");
        sb.AppendLine($"  {"─────────────────",- 18}{"─────────",10}{"─────────",10}{"─────────",10}");
        sb.AppendLine($"  {"CPU (ms)",-18}{FormatMs(_avgCpu),10}{FormatMs(_minCpu),10}{FormatMs(_maxCpu),10}");

        if (hasGpuData)
            sb.AppendLine($"  {"GPU (ms)",-18}{FormatMs(_avgGpu),10}{FormatMs(_minGpu),10}{FormatMs(_maxGpu),10}");
        else
            sb.AppendLine($"  {"GPU (ms)",-18}{"N/A",10}{"N/A",10}{"N/A",10}");

        sb.AppendLine($"  {"FPS",-18}{_avgFps,10:F1}{_minFps,10:F1}{_maxFps,10:F1}");
        sb.AppendLine();

        // FPS Distribution
        sb.AppendLine("  FPS Dağılımı:");
        sb.AppendLine();

        if (_fpsDistribution != null)
        {
            foreach (var bucket in _fpsDistribution)
            {
                float percentage = _frameTimings.Count > 0 ? (float)bucket.Value / _frameTimings.Count * 100f : 0f;
                int barLength = (int)(percentage / 2);
                string bar = new string('█', barLength);
                sb.AppendLine($"    {bucket.Key,-12} {bucket.Value,6} frame  ({percentage,5:F1}%)  {bar}");
            }
        }

        sb.AppendLine();
    }

    private void AppendTopSlowestFrames(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. TOP 20 SLOWEST FRAMES (SPIKE DETECTION)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_topSpikes == null || _topSpikes.Count == 0)
        {
            sb.AppendLine("  Veri yok.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"    {"#",-5}{"Frame",-10}{"CPU (ms)",12}{"GPU (ms)",12}{"FPS",10}{"GC",12}");
        sb.AppendLine($"    {"─",-5}{"─────────",-10}{"───────────",12}{"───────────",12}{"─────────",10}{"──────────",12}");

        for (int i = 0; i < _topSpikes.Count; i++)
        {
            var s = _topSpikes[i];
            string gpuStr = s.gpuMs > 0 ? FormatMs(s.gpuMs) : "N/A";
            string gcStr = FormatBytes((long)s.gcBytes);
            sb.AppendLine($"    {i + 1,-5}{s.frameIndex,-10}{FormatMs(s.cpuMs),12}{gpuStr,12}{s.fps,10:F1}{gcStr,12}");
        }

        sb.AppendLine();

        float avgSpikeMs = (float)_topSpikes.Average(s => s.cpuMs);
        sb.AppendLine($"  Spike ortalaması: {FormatMs(avgSpikeMs)} (genel ort: {FormatMs(_avgCpu)}, " +
                      $"{(_avgCpu > 0 ? avgSpikeMs / _avgCpu : 0):F1}x daha yavaş)");
        sb.AppendLine();
    }

    private void AppendTopExpensiveFunctions(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. TOP 30 MOST EXPENSIVE FUNCTIONS (by Self Time)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_topFunctions == null || _topFunctions.Count == 0)
        {
            sb.AppendLine("  Veri yok.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"    {"#",-4}{"Fonksiyon",-70}{"Toplam (ms)",12}{"Ort (ms)",10}{"Max (ms)",10}{"Çağrı",10}{"GC",10}");
        sb.AppendLine($"    {"─",-4}{"─".PadRight(69, '─'),-70}{"──────────",12}{"────────",10}{"────────",10}{"────────",10}{"────────",10}");

        for (int i = 0; i < _topFunctions.Count; i++)
        {
            var f = _topFunctions[i];
            string gc = f.totalGcBytes > 0 ? FormatBytes((long)f.totalGcBytes) : "-";
            sb.AppendLine($"    {i + 1,-4}{f.name,-70}{f.totalSelfTimeMs,12:F2}{f.AverageSelfTimeMs,10:F3}{f.maxSelfTimeMs,10:F3}{f.totalCalls,10}{gc,10}");
        }

        sb.AppendLine();
    }

    private void AppendUpdateBreakdown(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. UPDATE / LATEUPDATE / FIXEDUPDATE BREAKDOWN");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_updateMethodStats == null) return;

        for (int i = 0; i < _updateMethodStats.Length; i++)
        {
            var s = _updateMethodStats[i];
            float avgMs = s.frameCount > 0 ? (float)(s.totalTimeMs / s.frameCount) : 0;
            double avgGc = s.frameCount > 0 ? s.totalGcBytes / s.frameCount : 0;

            sb.AppendLine($"  ┌─ {s.displayName}");
            sb.AppendLine($"  │  Toplam süre : {s.totalTimeMs:F2} ms");
            sb.AppendLine($"  │  Frame sayısı: {s.frameCount}");
            sb.AppendLine($"  │  Frame başı  : {FormatMs(avgMs)}");
            sb.AppendLine($"  │  GC Toplam   : {FormatBytes((long)s.totalGcBytes)}");
            sb.AppendLine($"  │  GC Frame başı: {FormatBytes((long)avgGc)}");

            if (s.topContributors.Count > 0)
            {
                var aggregated = s.topContributors
                    .GroupBy(c => c.name)
                    .Select(g => new { Name = g.Key, TotalMs = g.Sum(x => x.timeMs), TotalGc = g.Sum(x => x.gcBytes) })
                    .OrderByDescending(x => x.TotalMs)
                    .Take(10)
                    .ToList();

                sb.AppendLine($"  │");
                sb.AppendLine($"  │  Top Contributors:");

                for (int j = 0; j < aggregated.Count; j++)
                {
                    var c = aggregated[j];
                    float pct = s.totalTimeMs > 0 ? (float)(c.TotalMs / s.totalTimeMs * 100) : 0;
                    string gcStr = c.TotalGc > 0 ? FormatBytes((long)c.TotalGc) : "-";
                    sb.AppendLine($"  │    {j + 1,2}. {c.Name,-60} {c.TotalMs,10:F2} ms ({pct,5:F1}%)  GC: {gcStr}");
                }
            }

            sb.AppendLine($"  └───────────────────────────────────────────────");
            sb.AppendLine();
        }
    }

    private void AppendUserCodeFunctions(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. ASSEMBLY-CSHARP FUNCTIONS (User Code Only)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_userCodeFunctions == null || _userCodeFunctions.Count == 0)
        {
            sb.AppendLine("  Kullanıcı kodu bulunamadı.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  Toplam kullanıcı fonksiyonu: {_totalUserFunctions} (ilk {_userCodeFunctions.Count} gösteriliyor)");
        sb.AppendLine();

        sb.AppendLine($"    {"#",-4}{"Fonksiyon",-70}{"Toplam (ms)",12}{"Ort (ms)",10}{"Max (ms)",10}{"Çağrı",10}{"GC",10}");
        sb.AppendLine($"    {"─",-4}{"─".PadRight(69, '─'),-70}{"──────────",12}{"────────",10}{"────────",10}{"────────",10}{"────────",10}");

        for (int i = 0; i < _userCodeFunctions.Count; i++)
        {
            var f = _userCodeFunctions[i];
            string gc = f.totalGcBytes > 0 ? FormatBytes((long)f.totalGcBytes) : "-";
            sb.AppendLine($"    {i + 1,-4}{f.name,-70}{f.totalSelfTimeMs,12:F2}{f.AverageSelfTimeMs,10:F3}{f.maxSelfTimeMs,10:F3}{f.totalCalls,10}{gc,10}");
        }

        sb.AppendLine();
    }

    private void AppendRenderingStatistics(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. RENDERING STATISTICS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_renderingStats == null) return;

        var validStats = _renderingStats.Where(s => s.isValid).ToList();

        if (validStats.Count == 0)
        {
            sb.AppendLine("  Rendering istatistikleri bulunamadı.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"  Geçerli frame sayısı: {validStats.Count}");
        sb.AppendLine();

        AppendRenderingRow(sb, "Draw Calls", validStats, s => s.drawCalls);
        AppendRenderingRow(sb, "Batches", validStats, s => s.batches);
        AppendRenderingRow(sb, "SetPass Calls", validStats, s => s.setPassCalls);
        AppendRenderingRow(sb, "Triangles", validStats, s => s.triangles, true);
        AppendRenderingRow(sb, "Vertices", validStats, s => s.vertices, true);
        AppendRenderingRow(sb, "Shadow Casters", validStats, s => s.shadowCasters);
        AppendRenderingRow(sb, "Dynamic Batches", validStats, s => s.dynamicBatches);
        AppendRenderingRow(sb, "Static Batches", validStats, s => s.staticBatches);
        AppendRenderingRow(sb, "Instanced Batches", validStats, s => s.instancedBatches);
        AppendRenderingRow(sb, "Skinned Meshes", validStats, s => s.visibleSkinnedMeshes);

        sb.AppendLine();
    }

    private void AppendRenderingRow(
        StringBuilder sb,
        string label,
        List<RenderingFrameStats> stats,
        Func<RenderingFrameStats, long> selector,
        bool useLargeFormat = false)
    {
        var values = stats.Select(selector).Where(v => v >= 0).ToList();

        if (values.Count == 0)
        {
            sb.AppendLine($"    {label,-22} N/A");
            return;
        }

        double avg = values.Average(v => (double)v);
        long min = values.Min();
        long max = values.Max();

        string avgStr = useLargeFormat ? FormatLargeNumber((long)avg) : ((long)avg).ToString("N0");
        string minStr = useLargeFormat ? FormatLargeNumber(min) : min.ToString("N0");
        string maxStr = useLargeFormat ? FormatLargeNumber(max) : max.ToString("N0");

        sb.AppendLine($"    {label,-22} Ort: {avgStr,12}   Min: {minStr,12}   Max: {maxStr,12}");
    }

    private void AppendGcAllocationAnalysis(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. GC ALLOCATION ANALYSIS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        double avgGcPerFrame = _frameTimings.Count > 0 ? _totalGcBytes / _frameTimings.Count : 0;

        sb.AppendLine($"  Toplam GC Allocation : {FormatBytes((long)_totalGcBytes)}");
        sb.AppendLine($"  Frame başı ortalama  : {FormatBytes((long)avgGcPerFrame)}");
        sb.AppendLine();

        // Top GC allocators (with GC/Call)
        if (_topGcAllocators != null && _topGcAllocators.Count > 0)
        {
            sb.AppendLine($"  Top {_topGcAllocators.Count} GC Allocator:");
            sb.AppendLine();
            sb.AppendLine($"    {"#",-4}{"Fonksiyon",-70}{"Toplam GC",12}{"GC/Call",10}{"Çağrı",10}{"User?",8}");
            sb.AppendLine($"    {"─",-4}{"─".PadRight(69, '─'),-70}{"──────────",12}{"────────",10}{"────────",10}{"──────",8}");

            for (int i = 0; i < _topGcAllocators.Count; i++)
            {
                var f = _topGcAllocators[i];
                string isUser = IsUserCode(f.name) ? "EVET" : "-";
                float pct = _totalGcBytes > 0 ? (float)(f.totalGcBytes / _totalGcBytes * 100) : 0;
                sb.AppendLine($"    {i + 1,-4}{f.name,-70}{FormatBytes((long)f.totalGcBytes),12}{FormatBytes((long)f.GcPerCall),10}{f.totalCalls,10}{isUser,8}  ({pct:F1}%)");
            }

            sb.AppendLine();
        }

        // User vs Engine split
        double userGcBytes = _functionMap.Values.Where(f => IsUserCode(f.name)).Sum(f => f.totalGcBytes);
        double engineGcBytes = _totalGcBytes - userGcBytes;

        sb.AppendLine($"  GC Kaynağı Dağılımı:");
        sb.AppendLine($"    Kullanıcı kodu : {FormatBytes((long)userGcBytes)} ({(_totalGcBytes > 0 ? userGcBytes / _totalGcBytes * 100 : 0):F1}%)");
        sb.AppendLine($"    Engine/System  : {FormatBytes((long)engineGcBytes)} ({(_totalGcBytes > 0 ? engineGcBytes / _totalGcBytes * 100 : 0):F1}%)");
        sb.AppendLine();

        // User Code GC Direct Allocators
        if (_functionMap != null)
        {
            var userGcAllocators = _functionMap.Values
                .Where(f => IsUserCode(f.name) && f.totalGcBytes > 0)
                .OrderByDescending(f => f.totalGcBytes)
                .Take(TOP_GC_ALLOCATOR_COUNT)
                .ToList();

            if (userGcAllocators.Count > 0)
            {
                sb.AppendLine($"  User Code GC Direkt Allocator (Top {userGcAllocators.Count}):");
                sb.AppendLine();
                sb.AppendLine($"    {"#",-4}{"Fonksiyon",-70}{"Toplam GC",12}{"GC/Call",10}{"Çağrı",10}{"Self(ms)",10}");
                sb.AppendLine($"    {"─",-4}{"─".PadRight(69, '─'),-70}{"──────────",12}{"────────",10}{"────────",10}{"────────",10}");

                for (int i = 0; i < userGcAllocators.Count; i++)
                {
                    var f = userGcAllocators[i];
                    sb.AppendLine($"    {i + 1,-4}{StripUserCodePrefix(f.name),-70}{FormatBytes((long)f.totalGcBytes),12}{FormatBytes((long)f.GcPerCall),10}{f.totalCalls,10}{f.totalSelfTimeMs,10:F2}");
                }

                sb.AppendLine();
            }
        }

        // Top GC frames
        if (_topGcFrames != null && _topGcFrames.Count > 0)
        {
            sb.AppendLine($"  Top {_topGcFrames.Count} GC Frame:");
            sb.AppendLine();
            sb.AppendLine($"    {"#",-5}{"Frame",-10}{"GC",12}{"CPU (ms)",12}{"FPS",10}");
            sb.AppendLine($"    {"─",-5}{"─────────",-10}{"──────────",12}{"───────────",12}{"─────────",10}");

            for (int i = 0; i < _topGcFrames.Count; i++)
            {
                var f = _topGcFrames[i];
                sb.AppendLine($"    {i + 1,-5}{f.frameIndex,-10}{FormatBytes((long)f.gcBytes),12}{FormatMs(f.cpuMs),12}{f.fps,10:F1}");
            }

            sb.AppendLine();
        }
    }

    private void AppendCallChains(StringBuilder sb)
    {
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. GC CALL CHAINS (User Code → GC Producer)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        if (_gcCallChainGroups == null || _gcCallChainGroups.Count == 0)
        {
            sb.AppendLine("  GC Call Chain verisi bulunamadı.");
            sb.AppendLine();
            return;
        }

        for (int i = 0; i < _gcCallChainGroups.Count; i++)
        {
            var group = _gcCallChainGroups[i];
            string displayCaller = StripUserCodePrefix(group.userCodeCaller);

            sb.AppendLine($"  ┌─ {displayCaller,-60} Toplam GC: {FormatBytes((long)group.totalGcBytes)}");

            if (group.children != null)
            {
                for (int j = 0; j < group.children.Count; j++)
                {
                    var child = group.children[j];
                    sb.AppendLine($"  │  └── {child.gcProducer,-55} {FormatBytes((long)child.totalGcBytes),10}  ({child.totalCalls} call, {FormatBytes((long)child.GcPerCall)}/call, {child.occurrences} frame)");
                }
            }

            sb.AppendLine($"  │");
        }

        sb.AppendLine();
    }

    private void AppendEcsPipelineSection(StringBuilder sb)
    {
        var snapshot = _snapshotA;
        if (snapshot?.ecsSystemsSorted == null || snapshot.ecsSystemsSorted.Count == 0)
            return;

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. ECS PIPELINE ANALYSIS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine($"  Tespit edilen ECS sistem sayisi: {snapshot.ecsSystemsSorted.Count}");
        sb.AppendLine("  Not: Toplam/Ort/Max = main thread inclusive sure (dispatch + job bekleme dahil)");
        sb.AppendLine("        Self = sadece sistemin kendi kodu (schedule, ECB, vs.)");
        sb.AppendLine("        Child = Toplam - Self (job bekleme + child marker sureleri)");
        sb.AppendLine();

        sb.AppendLine($"    {"Sistem",-30}{"Toplam(ms)",12}{"Ort(ms)",10}{"Child Avg",10}{"Child Max",10}{"Self(ms)",10}{"GC",10}{"Sync",8}");
        sb.AppendLine($"    {"──────────────────────────────",-30}{"──────────",12}{"────────",10}{"────────",10}{"────────",10}{"────────",10}{"────────",10}{"──────",8}");

        foreach (var sys in snapshot.ecsSystemsSorted)
        {
            string gc = sys.totalGcBytes > 0 ? FormatBytes((long)sys.totalGcBytes) : "-";
            string sync = sys.hasSyncPoint ? "BLOCK" : "-";
            string jobAvg = sys.jobTimeMs > 0 ? $"{sys.AvgJobTimeMs:F3}" : "-";
            string jobMax = sys.maxJobTimeMs > 0 ? $"{sys.maxJobTimeMs:F3}" : "-";
            sb.AppendLine($"    {sys.systemName,-30}{sys.totalTimeMs,12:F2}{sys.AvgTimeMs,10:F3}{jobAvg,10}{jobMax,10}{sys.totalSelfTimeMs,10:F2}{gc,10}{sync,8}");
        }
        sb.AppendLine();
    }

    private void AppendPhysicsBreakdownSection(StringBuilder sb)
    {
        var snapshot = _snapshotA;
        if (snapshot?.physicsPipelineSystems == null || snapshot.physicsPipelineSystems.Count == 0)
            return;

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. PHYSICS PIPELINE BREAKDOWN");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        sb.AppendLine($"  Pipeline Ortalama (main thread): {FormatMs(snapshot.avgPhysicsPipelineMs)}");
        sb.AppendLine($"  Pipeline Max (main thread):      {FormatMs(snapshot.maxPhysicsPipelineMs)}");
        sb.AppendLine();

        double totalPipelineTime = snapshot.physicsPipelineSystems.Sum(s => s.totalTimeMs);

        sb.AppendLine($"    {"#",-4}{"Sistem",-30}{"Ort(ms)",10}{"Max(ms)",10}{"Child Avg",10}{"Child Max",10}{"Toplam(ms)",12}{"%",8}");
        sb.AppendLine($"    {"─",-4}{"──────────────────────────────",-30}{"────────",10}{"────────",10}{"────────",10}{"────────",10}{"──────────",12}{"──────",8}");

        for (int i = 0; i < snapshot.physicsPipelineSystems.Count; i++)
        {
            var sys = snapshot.physicsPipelineSystems[i];
            float pct = totalPipelineTime > 0 ? (float)(sys.totalTimeMs / totalPipelineTime * 100) : 0;
            string jobAvg = sys.jobTimeMs > 0 ? $"{sys.AvgJobTimeMs:F3}" : "-";
            string jobMax = sys.maxJobTimeMs > 0 ? $"{sys.maxJobTimeMs:F3}" : "-";
            sb.AppendLine($"    {i + 1,-4}{sys.systemName,-30}{sys.AvgTimeMs,10:F3}{sys.maxTimeMs,10:F3}{jobAvg,10}{jobMax,10}{sys.totalTimeMs,12:F2}{pct,7:F1}%");
        }
        sb.AppendLine();

        // Top 10 en yavas pipeline frame
        if (snapshot.ecsFrameTimeline != null && snapshot.ecsFrameTimeline.Count > 0)
        {
            sb.AppendLine("  Top 10 En Yavas Pipeline Frame:");
            sb.AppendLine();

            var topFrames = snapshot.ecsFrameTimeline
                .OrderByDescending(f => f.totalPipelineTimeMs)
                .Take(10)
                .ToList();

            // Kisa kolon isimleri (text raporda okunabilirlik icin)
            string[] shortNames = { "MovForce", "SpatHash", "Collision", "Integrate", "Boundary" };
            sb.Append($"    {"#",-4}{"Frame",-10}{"Pipeline",12}");
            foreach (var sn in shortNames)
                sb.Append($"{sn,12}");
            sb.Append($"{"| ChildTot",12}");
            foreach (var sn in shortNames)
                sb.Append($"{"C:" + sn,12}");
            sb.AppendLine();

            for (int i = 0; i < topFrames.Count; i++)
            {
                var fd = topFrames[i];
                sb.Append($"    {i + 1,-4}{fd.frameIndex,-10}{FormatMs(fd.totalPipelineTimeMs),12}");
                foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                {
                    fd.systemTimesMs.TryGetValue(psName, out float sysTime);
                    sb.Append($"{(sysTime > 0 ? FormatMs(sysTime) : "-"),12}");
                }
                // Job timing
                float jobTotal = 0;
                if (fd.jobTimesMs != null)
                    foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                        if (fd.jobTimesMs.TryGetValue(psName, out float jt)) jobTotal += jt;
                sb.Append($"{(jobTotal > 0 ? FormatMs(jobTotal) : "-"),12}");
                foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
                {
                    float jobTime = 0;
                    if (fd.jobTimesMs != null) fd.jobTimesMs.TryGetValue(psName, out jobTime);
                    sb.Append($"{(jobTime > 0 ? FormatMs(jobTime) : "-"),12}");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
        }
    }

    // ───────────────────────────────────────────────────────────
    // TEXT REPORT: COMPARE SECTION (V4)
    // ───────────────────────────────────────────────────────────

    private void AppendCompareSection(StringBuilder sb)
    {
        var a = _snapshotA;
        var b = _snapshotB;
        if (a == null || b == null) return;

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"  {NextSectionNumber()}. A/B KARŞILAŞTIRMA (COMPARE)");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // File info
        int aFrames = a.totalFrames - a.skippedFrames;
        int bFrames = b.totalFrames - b.skippedFrames;
        sb.AppendLine($"  A (Before): {Path.GetFileName(a.filePath)}  —  {aFrames} frame");
        sb.AppendLine($"  B (After):  {Path.GetFileName(b.filePath)}  —  {bFrames} frame");
        sb.AppendLine();

        // ── Quick Summary ──
        sb.AppendLine("  Özet Karşılaştırma:");
        sb.AppendLine();
        sb.AppendLine($"    {"Metrik",-20}{"A",12}{"B",12}{"Delta",14}{"Değişim",10}");
        sb.AppendLine($"    {"────────────────────",-20}{"──────────",12}{"──────────",12}{"────────────",14}{"────────",10}");

        AppendCompareMetric(sb, "CPU Avg (ms)", a.avgCpu, b.avgCpu, true, FormatMs);
        AppendCompareMetric(sb, "CPU Max (ms)", a.maxCpu, b.maxCpu, true, FormatMs);
        AppendCompareMetric(sb, "GPU Avg (ms)", a.avgGpu, b.avgGpu, true, FormatMs);
        AppendCompareMetric(sb, "FPS Avg", a.avgFps, b.avgFps, false, v => $"{v:F1}");
        AppendCompareMetric(sb, "FPS Min", a.minFps, b.minFps, false, v => $"{v:F1}");
        AppendCompareMetricBytes(sb, "Total GC", a.totalGcBytes, b.totalGcBytes);

        sb.AppendLine();

        // ── GC Breakdown ──
        double aUserGc = a.functionMap.Values.Where(f => IsUserCode(f.name)).Sum(f => f.totalGcBytes);
        double bUserGc = b.functionMap.Values.Where(f => IsUserCode(f.name)).Sum(f => f.totalGcBytes);
        double aEngineGc = a.totalGcBytes - aUserGc;
        double bEngineGc = b.totalGcBytes - bUserGc;
        double aAvgGcFrame = a.frameTimings.Count > 0 ? a.totalGcBytes / a.frameTimings.Count : 0;
        double bAvgGcFrame = b.frameTimings.Count > 0 ? b.totalGcBytes / b.frameTimings.Count : 0;

        sb.AppendLine("  GC Karşılaştırma:");
        sb.AppendLine();
        AppendCompareMetricBytes(sb, "User Code GC", aUserGc, bUserGc);
        AppendCompareMetricBytes(sb, "Engine GC", aEngineGc, bEngineGc);
        AppendCompareMetricBytes(sb, "GC/Frame Avg", aAvgGcFrame, bAvgGcFrame);

        sb.AppendLine();

        // ── FPS Distribution ──
        if (a.fpsDistribution != null && b.fpsDistribution != null)
        {
            sb.AppendLine("  FPS Dağılımı Karşılaştırma:");
            sb.AppendLine();
            sb.AppendLine($"    {"Aralık",-14}{"A",8}{"A %",8}{"B",8}{"B %",8}{"Delta",8}");
            sb.AppendLine($"    {"──────────────",-14}{"──────",8}{"──────",8}{"──────",8}{"──────",8}{"──────",8}");

            int aTotal = a.frameTimings?.Count ?? 1;
            int bTotal = b.frameTimings?.Count ?? 1;

            foreach (var key in a.fpsDistribution.Keys)
            {
                int aVal = a.fpsDistribution.ContainsKey(key) ? a.fpsDistribution[key] : 0;
                int bVal = b.fpsDistribution.ContainsKey(key) ? b.fpsDistribution[key] : 0;
                float aPct = aTotal > 0 ? (float)aVal / aTotal * 100f : 0;
                float bPct = bTotal > 0 ? (float)bVal / bTotal * 100f : 0;
                int delta = bVal - aVal;
                string arrow = delta > 0 ? "+" : delta < 0 ? "" : " ";
                sb.AppendLine($"    {key,-14}{aVal,8}{aPct,7:F1}%{bVal,8}{bPct,7:F1}%{arrow + delta,8}");
            }

            sb.AppendLine();
        }

        // ── Update Method Comparison ──
        if (a.updateMethodStats != null && b.updateMethodStats != null)
        {
            sb.AppendLine("  Update Method Karşılaştırma:");
            sb.AppendLine();
            sb.AppendLine($"    {"Method",-42}{"A (ms)",10}{"B (ms)",10}{"Δ (ms)",12}{"A GC",10}{"B GC",10}{"Δ GC",12}");
            sb.AppendLine($"    {"──────────────────────────────────────────",-42}{"────────",10}{"────────",10}{"──────────",12}{"────────",10}{"────────",10}{"──────────",12}");

            int count = Math.Min(a.updateMethodStats.Length, b.updateMethodStats.Length);
            for (int i = 0; i < count; i++)
            {
                var ua = a.updateMethodStats[i];
                var ub = b.updateMethodStats[i];
                float aAvg = ua.frameCount > 0 ? (float)(ua.totalTimeMs / ua.frameCount) : 0;
                float bAvg = ub.frameCount > 0 ? (float)(ub.totalTimeMs / ub.frameCount) : 0;
                float cpuDelta = bAvg - aAvg;
                double gcDelta = ub.totalGcBytes - ua.totalGcBytes;
                sb.AppendLine($"    {ua.displayName,-42}{FormatMs(aAvg),10}{FormatMs(bAvg),10}{cpuDelta,+12:+0.00;-0.00;0.00}{FormatBytes((long)ua.totalGcBytes),10}{FormatBytes((long)ub.totalGcBytes),10}{FormatBytesDelta(gcDelta),12}");
            }

            sb.AppendLine();
        }

        // ── Top CPU Delta (Top 20) ──
        AppendCompareFunctionDeltaText(sb, "Top CPU Delta (Top 20)", a, b, false, 20);

        // ── Top GC Delta (Top 20) ──
        AppendCompareFunctionDeltaText(sb, "Top GC Delta (Top 20)", a, b, true, 20);

        // ── User Code Delta (Top 20) ──
        AppendCompareUserCodeDeltaText(sb, "User Code Delta (Top 20)", a, b, 20);

        // ── ECS System Delta ──
        AppendCompareEcsDeltaText(sb, a, b);

        // ── Physics Pipeline Delta ──
        AppendComparePhysicsPipelineDeltaText(sb, a, b);

        sb.AppendLine();
    }

    private void AppendCompareEcsDeltaText(StringBuilder sb, AnalysisSnapshot a, AnalysisSnapshot b)
    {
        sb.AppendLine("  ECS System Delta:");
        sb.AppendLine();

        var allNames = new HashSet<string>();
        if (a.ecsSystemMap != null) foreach (var k in a.ecsSystemMap.Keys) allNames.Add(k);
        if (b.ecsSystemMap != null) foreach (var k in b.ecsSystemMap.Keys) allNames.Add(k);

        if (allNames.Count == 0)
        {
            sb.AppendLine("    ECS sistem verisi yok.");
            sb.AppendLine();
            return;
        }

        var deltas = new List<(string name, float aAvg, float bAvg, float delta, float aJobAvg, float bJobAvg, float jobDelta)>();
        foreach (var name in allNames)
        {
            float aAvg = 0, bAvg = 0, aJobAvg = 0, bJobAvg = 0;
            if (a.ecsSystemMap != null && a.ecsSystemMap.TryGetValue(name, out var ae)) { aAvg = ae.AvgTimeMs; aJobAvg = ae.AvgJobTimeMs; }
            if (b.ecsSystemMap != null && b.ecsSystemMap.TryGetValue(name, out var be)) { bAvg = be.AvgTimeMs; bJobAvg = be.AvgJobTimeMs; }
            deltas.Add((name, aAvg, bAvg, bAvg - aAvg, aJobAvg, bJobAvg, bJobAvg - aJobAvg));
        }
        deltas.Sort((x, y) => Math.Abs(y.delta).CompareTo(Math.Abs(x.delta)));

        sb.AppendLine($"    {"#",-4}{"ECS Sistem",-30}{"A Avg",10}{"B Avg",10}{"Delta",12}{"A Child",10}{"B Child",10}{"Child Δ",12}");
        sb.AppendLine($"    {"─",-4}{"─".PadRight(29, '─'),-30}{"────────",10}{"────────",10}{"──────────",12}{"────────",10}{"────────",10}{"──────────",12}");

        for (int i = 0; i < deltas.Count; i++)
        {
            var d = deltas[i];
            string arrow = d.delta > 0.001f ? "↑" : d.delta < -0.001f ? "↓" : "—";
            string jobArrow = d.jobDelta > 0.001f ? "↑" : d.jobDelta < -0.001f ? "↓" : "—";
            string aJobStr = d.aJobAvg > 0 ? FormatMs(d.aJobAvg) : "-";
            string bJobStr = d.bJobAvg > 0 ? FormatMs(d.bJobAvg) : "-";
            string jobDeltaStr = (d.aJobAvg > 0 || d.bJobAvg > 0) ? jobArrow + " " + FormatMs(Math.Abs(d.jobDelta)) : "-";
            sb.AppendLine($"    {i + 1,-4}{d.name,-30}{FormatMs(d.aAvg),10}{FormatMs(d.bAvg),10}{arrow + " " + FormatMs(Math.Abs(d.delta)),12}{aJobStr,10}{bJobStr,10}{jobDeltaStr,12}");
        }
        sb.AppendLine();
    }

    private void AppendComparePhysicsPipelineDeltaText(StringBuilder sb, AnalysisSnapshot a, AnalysisSnapshot b)
    {
        sb.AppendLine("  Physics Pipeline Delta:");
        sb.AppendLine();

        string arrow;
        float delta;

        delta = b.avgPhysicsPipelineMs - a.avgPhysicsPipelineMs;
        arrow = delta > 0.001f ? "↑" : delta < -0.001f ? "↓" : "—";
        sb.AppendLine($"    Pipeline Avg (main thread): A={FormatMs(a.avgPhysicsPipelineMs)}  B={FormatMs(b.avgPhysicsPipelineMs)}  {arrow} {FormatMs(Math.Abs(delta))}");

        delta = b.maxPhysicsPipelineMs - a.maxPhysicsPipelineMs;
        arrow = delta > 0.001f ? "↑" : delta < -0.001f ? "↓" : "—";
        sb.AppendLine($"    Pipeline Max (main thread): A={FormatMs(a.maxPhysicsPipelineMs)}  B={FormatMs(b.maxPhysicsPipelineMs)}  {arrow} {FormatMs(Math.Abs(delta))}");
        sb.AppendLine();

        sb.AppendLine($"    {"Sistem",-30}{"A Avg",10}{"B Avg",10}{"Delta",12}{"A Child",10}{"B Child",10}{"Child Δ",12}");
        sb.AppendLine($"    {"──────────────────────────────",-30}{"────────",10}{"────────",10}{"──────────",12}{"────────",10}{"────────",10}{"──────────",12}");

        foreach (var psName in PHYSICS_PIPELINE_SYSTEMS)
        {
            float aAvg = 0, bAvg = 0, aJobAvg = 0, bJobAvg = 0;
            if (a.ecsSystemMap != null && a.ecsSystemMap.TryGetValue(psName, out var ae)) { aAvg = ae.AvgTimeMs; aJobAvg = ae.AvgJobTimeMs; }
            if (b.ecsSystemMap != null && b.ecsSystemMap.TryGetValue(psName, out var be)) { bAvg = be.AvgTimeMs; bJobAvg = be.AvgJobTimeMs; }
            delta = bAvg - aAvg;
            arrow = delta > 0.001f ? "↑" : delta < -0.001f ? "↓" : "—";
            float jobDelta = bJobAvg - aJobAvg;
            string jobArrow = jobDelta > 0.001f ? "↑" : jobDelta < -0.001f ? "↓" : "—";
            string aJobStr = aJobAvg > 0 ? FormatMs(aJobAvg) : "-";
            string bJobStr = bJobAvg > 0 ? FormatMs(bJobAvg) : "-";
            string jobDeltaStr = (aJobAvg > 0 || bJobAvg > 0) ? jobArrow + " " + FormatMs(Math.Abs(jobDelta)) : "-";
            sb.AppendLine($"    {psName,-30}{FormatMs(aAvg),10}{FormatMs(bAvg),10}{arrow + " " + FormatMs(Math.Abs(delta)),12}{aJobStr,10}{bJobStr,10}{jobDeltaStr,12}");
        }
        sb.AppendLine();
    }

    private void AppendCompareMetric(StringBuilder sb, string label, float aVal, float bVal, bool lowerIsBetter, Func<float, string> fmt)
    {
        float delta = bVal - aVal;
        float pct = aVal != 0 ? delta / aVal * 100f : 0;
        string arrow = delta > 0.001f ? "↑" : delta < -0.001f ? "↓" : "—";
        bool improved = lowerIsBetter ? delta < 0 : delta > 0;
        string verdict = Math.Abs(delta) < 0.001f ? "" : improved ? " [OK]" : " [!!]";
        sb.AppendLine($"    {label,-20}{fmt(aVal),12}{fmt(bVal),12}{arrow + " " + $"{delta:+0.00;-0.00;0.00}",14}{pct:+0.0;-0.0;0.0}%{verdict}");
    }

    private void AppendCompareMetricBytes(StringBuilder sb, string label, double aVal, double bVal)
    {
        double delta = bVal - aVal;
        float pct = aVal != 0 ? (float)(delta / aVal * 100) : 0;
        string arrow = delta > 0 ? "↑" : delta < 0 ? "↓" : "—";
        bool improved = delta < 0;
        string verdict = Math.Abs(delta) < 1 ? "" : improved ? " [OK]" : " [!!]";
        sb.AppendLine($"    {label,-20}{FormatBytes((long)aVal),12}{FormatBytes((long)bVal),12}{arrow + " " + FormatBytes((long)Math.Abs(delta)),14}{pct:+0.0;-0.0;0.0}%{verdict}");
    }

    private static string FormatBytesDelta(double delta)
    {
        string arrow = delta > 0 ? "↑" : delta < 0 ? "↓" : "—";
        return $"{arrow} {FormatBytes((long)Math.Abs(delta))}";
    }

    private void AppendCompareFunctionDeltaText(StringBuilder sb, string title, AnalysisSnapshot a, AnalysisSnapshot b, bool byGc, int topCount)
    {
        sb.AppendLine($"  {title}:");
        sb.AppendLine();

        var allNames = new HashSet<string>();
        foreach (var kv in a.functionMap) allNames.Add(kv.Key);
        foreach (var kv in b.functionMap) allNames.Add(kv.Key);

        var deltas = new List<(string name, double aVal, double bVal, double delta)>();
        foreach (var name in allNames)
        {
            a.functionMap.TryGetValue(name, out var aEntry);
            b.functionMap.TryGetValue(name, out var bEntry);
            double aV = byGc ? (aEntry?.totalGcBytes ?? 0) : (aEntry?.totalSelfTimeMs ?? 0);
            double bV = byGc ? (bEntry?.totalGcBytes ?? 0) : (bEntry?.totalSelfTimeMs ?? 0);
            if (aV > 0 || bV > 0)
                deltas.Add((name, aV, bV, bV - aV));
        }

        deltas.Sort((x, y) => Math.Abs(y.delta).CompareTo(Math.Abs(x.delta)));
        int count = Math.Min(topCount, deltas.Count);

        if (count == 0)
        {
            sb.AppendLine("    Veri yok.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"    {"#",-4}{"Fonksiyon",-60}{"A",12}{"B",12}{"Delta",14}");
        sb.AppendLine($"    {"─",-4}{"─".PadRight(59, '─'),-60}{"──────────",12}{"──────────",12}{"────────────",14}");

        for (int i = 0; i < count; i++)
        {
            var d = deltas[i];
            string arrow = d.delta > 0 ? "↑" : d.delta < 0 ? "↓" : "—";
            if (byGc)
                sb.AppendLine($"    {i + 1,-4}{d.name,-60}{FormatBytes((long)d.aVal),12}{FormatBytes((long)d.bVal),12}{arrow + " " + FormatBytes((long)Math.Abs(d.delta)),14}");
            else
                sb.AppendLine($"    {i + 1,-4}{d.name,-60}{d.aVal,12:F2}{d.bVal,12:F2}{arrow + " " + $"{Math.Abs(d.delta):F2}",14}");
        }

        sb.AppendLine();
    }

    private void AppendCompareUserCodeDeltaText(StringBuilder sb, string title, AnalysisSnapshot a, AnalysisSnapshot b, int topCount)
    {
        sb.AppendLine($"  {title}:");
        sb.AppendLine();

        var allNames = new HashSet<string>();
        foreach (var kv in a.functionMap)
            if (IsUserCode(kv.Key)) allNames.Add(kv.Key);
        foreach (var kv in b.functionMap)
            if (IsUserCode(kv.Key)) allNames.Add(kv.Key);

        var deltas = new List<(string name, double aCpu, double bCpu, double cpuDelta, double aGc, double bGc, double gcDelta)>();
        foreach (var name in allNames)
        {
            a.functionMap.TryGetValue(name, out var aEntry);
            b.functionMap.TryGetValue(name, out var bEntry);
            double aCpu = aEntry?.totalSelfTimeMs ?? 0;
            double bCpu = bEntry?.totalSelfTimeMs ?? 0;
            double aGc = aEntry?.totalGcBytes ?? 0;
            double bGc = bEntry?.totalGcBytes ?? 0;
            if (aCpu > 0 || bCpu > 0 || aGc > 0 || bGc > 0)
                deltas.Add((name, aCpu, bCpu, bCpu - aCpu, aGc, bGc, bGc - aGc));
        }

        deltas.Sort((x, y) => Math.Abs(y.cpuDelta).CompareTo(Math.Abs(x.cpuDelta)));
        int count = Math.Min(topCount, deltas.Count);

        if (count == 0)
        {
            sb.AppendLine("    Veri yok.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"    {"#",-4}{"Fonksiyon",-55}{"A CPU",10}{"B CPU",10}{"Δ CPU",12}{"A GC",10}{"B GC",10}{"Δ GC",12}");
        sb.AppendLine($"    {"─",-4}{"─".PadRight(54, '─'),-55}{"────────",10}{"────────",10}{"──────────",12}{"────────",10}{"────────",10}{"──────────",12}");

        for (int i = 0; i < count; i++)
        {
            var d = deltas[i];
            string displayName = StripUserCodePrefix(d.name);
            string cpuArrow = d.cpuDelta > 0.001 ? "↑" : d.cpuDelta < -0.001 ? "↓" : "—";
            string gcArrow = d.gcDelta > 0 ? "↑" : d.gcDelta < 0 ? "↓" : "—";
            string aGcStr = d.aGc > 0 ? FormatBytes((long)d.aGc) : "-";
            string bGcStr = d.bGc > 0 ? FormatBytes((long)d.bGc) : "-";
            string gcDeltaStr = Math.Abs(d.gcDelta) > 0 ? gcArrow + " " + FormatBytes((long)Math.Abs(d.gcDelta)) : "—";
            sb.AppendLine($"    {i + 1,-4}{displayName,-55}{d.aCpu,10:F2}{d.bCpu,10:F2}{cpuArrow + " " + $"{Math.Abs(d.cpuDelta):F2}",12}{aGcStr,10}{bGcStr,10}{gcDeltaStr,12}");
        }

        sb.AppendLine();
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════

    private static string FormatMs(float ms)
    {
        if (ms < 0.001f) return "0.000";
        if (ms < 1f) return $"{ms:F3}";
        if (ms < 10f) return $"{ms:F2}";
        if (ms < 100f) return $"{ms:F1}";
        return $"{ms:F0}";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";
        if (bytes == 0) return "0 B";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
        return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
    }

    private static string FormatLargeNumber(long number)
    {
        if (number < 0) return "N/A";
        if (number < 1000) return number.ToString();
        if (number < 1000000) return $"{number / 1000f:F1}K";
        if (number < 1000000000) return $"{number / 1000000f:F2}M";
        return $"{number / 1000000000f:F2}B";
    }

    private static string StripUserCodePrefix(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.StartsWith(USER_CODE_PREFIX, StringComparison.Ordinal))
            return name.Substring(USER_CODE_PREFIX.Length);
        return name;
    }

    private static Color GetSeverityColor(float value, float warningThreshold, float criticalThreshold)
    {
        if (value >= criticalThreshold) return new Color(1f, 0.3f, 0.3f);
        if (value >= warningThreshold) return new Color(1f, 0.85f, 0.3f);
        return Color.white;
    }
}
#endif
