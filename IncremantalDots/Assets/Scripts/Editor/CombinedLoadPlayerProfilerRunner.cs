#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DeadWalls.Editor
{
    /// <summary>
    /// Mevcut explicit profiler testini StandaloneWindows64 Player'da calistirir ve
    /// olusan raw capture'i mevcut ProfilerDataAnalyzer ile rapora donusturur.
    /// </summary>
    public static class CombinedLoadPlayerProfilerRunner
    {
        private const string TestName =
            "DeadWalls.Tests.HordeScaleProfilerCapturePlayModeTests." +
            "HordeScale_10K_1K_CombinedProfilerCapture_ProducesLoadableRaw";
        private const string CapturePrefix = "DW_V1_PLAYER_COMBINED_";
        private const string FramePacingPrefix = "DW_V1_TARGET_HARDWARE_FRAME_PACING_";

        private static TestRunnerApi _testRunnerApi;
        private static PlayerCallbacks _callbacks;

        [Serializable]
        private sealed class RunStatus
        {
            public string status;
            public string runId;
            public string testName;
            public string startedUtc;
            public string finishedUtc;
            public int passed;
            public int failed;
            public int skipped;
            public string rawPath;
            public string framePacingPath;
            public string reportPath;
            public string summaryPath;
            public string error;
        }

        [MenuItem(DeadWallsEditorMenuPaths.Profiling + "Run Combined 10K + 1K Player Profile")]
        public static void RunCombinedPlayerProfile()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[DW-V1-PLAYER-PROFILE] Editor compile veya Play Mode sirasinda Player profili baslatilamaz.");
                return;
            }

            string logsDirectory = GetLogsDirectory();
            Directory.CreateDirectory(logsDirectory);
            DeleteIfExists(GetStatusPath());
            DeleteIfExists(GetReportPath());
            DeleteIfExists(GetSummaryPath());

            DateTime startedUtc = DateTime.UtcNow;
            _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            _callbacks = new PlayerCallbacks(startedUtc.Ticks);
            _testRunnerApi.RegisterCallbacks(_callbacks);

            var filter = new Filter
            {
                testMode = TestMode.PlayMode,
                testNames = new[] { TestName },
#pragma warning disable CS0618
                targetPlatform = BuildTarget.StandaloneWindows64
#pragma warning restore CS0618
            };
            var settings = new ExecutionSettings(filter)
            {
                playerHeartbeatTimeout = 600
            };

            try
            {
                string runId = _testRunnerApi.Execute(settings);
                _callbacks.SetRunId(runId);
                WriteStatus(new RunStatus
                {
                    status = "running",
                    runId = runId,
                    testName = TestName,
                    startedUtc = startedUtc.ToString("O")
                });
                Debug.Log(
                    $"[DW-V1-PLAYER-PROFILE] status=running; run_id={runId}; " +
                    $"test={TestName}; target=StandaloneWindows64");
            }
            catch (Exception ex)
            {
                WriteStatus(new RunStatus
                {
                    status = "failed",
                    testName = TestName,
                    startedUtc = startedUtc.ToString("O"),
                    finishedUtc = DateTime.UtcNow.ToString("O"),
                    error = ex.ToString()
                });
                Debug.LogError($"[DW-V1-PLAYER-PROFILE] start_failed={ex}");
                ReleaseRunner();
            }
        }

        [MenuItem(DeadWallsEditorMenuPaths.Profiling + "Analyze Latest Combined Player Profile")]
        public static void AnalyzeLatestCombinedPlayerProfile()
        {
            string rawPath = FindLatestRaw(0L);
            if (string.IsNullOrEmpty(rawPath))
            {
                Debug.LogError(
                    $"[DW-V1-PLAYER-PROFILE] {GetCaptureDirectory()} altinda {CapturePrefix}*.raw bulunamadi.");
                return;
            }

            DateTime analyzedUtc = DateTime.UtcNow;
            AnalyzeAndWriteStatus(
                rawPath,
                FindLatestFramePacing(0L),
                null,
                analyzedUtc,
                analyzedUtc,
                null,
                1,
                0,
                0);
        }

        private sealed class PlayerCallbacks : IErrorCallbacks
        {
            private readonly long _startedUtcTicks;
            private string _runId;

            public PlayerCallbacks(long startedUtcTicks)
            {
                _startedUtcTicks = startedUtcTicks;
            }

            public void SetRunId(string runId)
            {
                _runId = runId;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                DateTime finishedUtc = DateTime.UtcNow;
                string rawPath = FindLatestRaw(_startedUtcTicks);
                string framePacingPath = FindLatestFramePacing(_startedUtcTicks);
                string resultError = result.TestStatus == TestStatus.Passed
                    ? null
                    : $"Player test status={result.TestStatus}; message={result.Message}; " +
                      $"output={result.Output}";

                if (string.IsNullOrEmpty(resultError) && string.IsNullOrEmpty(rawPath))
                    resultError = "Player testi bitti fakat yeni combined raw capture bulunamadi.";
                if (string.IsNullOrEmpty(resultError) && string.IsNullOrEmpty(framePacingPath))
                    resultError = "Player testi bitti fakat target-hardware frame pacing raporu bulunamadi.";

                if (string.IsNullOrEmpty(resultError))
                {
                    AnalyzeAndWriteStatus(
                        rawPath,
                        framePacingPath,
                        result,
                        new DateTime(_startedUtcTicks, DateTimeKind.Utc),
                        finishedUtc,
                        _runId,
                        result.PassCount,
                        result.FailCount,
                        result.SkipCount);
                }
                else
                {
                    WriteStatus(new RunStatus
                    {
                        status = "failed",
                        testName = TestName,
                        startedUtc = new DateTime(_startedUtcTicks, DateTimeKind.Utc).ToString("O"),
                        finishedUtc = finishedUtc.ToString("O"),
                        passed = result.PassCount,
                        failed = result.FailCount,
                        skipped = result.SkipCount,
                        rawPath = rawPath,
                        framePacingPath = framePacingPath,
                        error = resultError
                    });
                    Debug.LogError($"[DW-V1-PLAYER-PROFILE] status=failed; error={resultError}");
                }

                EditorApplication.delayCall += ReleaseRunner;
            }

            public void OnError(string message)
            {
                WriteStatus(new RunStatus
                {
                    status = "failed",
                    runId = _runId,
                    testName = TestName,
                    startedUtc = new DateTime(_startedUtcTicks, DateTimeKind.Utc).ToString("O"),
                    finishedUtc = DateTime.UtcNow.ToString("O"),
                    error = message
                });
                Debug.LogError($"[DW-V1-PLAYER-PROFILE] status=failed; error={message}");
                EditorApplication.delayCall += ReleaseRunner;
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        private static void AnalyzeAndWriteStatus(
            string rawPath,
            string framePacingPath,
            ITestResultAdaptor result,
            DateTime startedUtc,
            DateTime finishedUtc,
            string runId,
            int passed,
            int failed,
            int skipped)
        {
            string reportPath = GetReportPath();
            string summaryPath = GetSummaryPath();
            bool analyzed = ProfilerDataAnalyzer.TryAnalyzeProfileToArtifacts(
                rawPath,
                reportPath,
                summaryPath,
                out string analysisError);

            WriteStatus(new RunStatus
            {
                status = analyzed ? "passed" : "failed",
                runId = runId,
                testName = TestName,
                startedUtc = startedUtc.ToString("O"),
                finishedUtc = finishedUtc.ToString("O"),
                passed = passed,
                failed = failed,
                skipped = skipped,
                rawPath = rawPath,
                framePacingPath = framePacingPath,
                reportPath = analyzed ? reportPath : null,
                summaryPath = analyzed ? summaryPath : null,
                error = analysisError
            });

            if (analyzed)
            {
                Debug.Log(
                    $"[DW-V1-PLAYER-PROFILE] status=passed; raw={rawPath}; " +
                    $"frame_pacing={framePacingPath}; report={reportPath}; summary={summaryPath}");
            }
            else
            {
                Debug.LogError(
                    $"[DW-V1-PLAYER-PROFILE] status=failed; raw={rawPath}; " +
                    $"analysis_error={analysisError}");
            }
        }

        private static string FindLatestRaw(long minimumUtcTicks)
        {
            string directory = GetCaptureDirectory();
            if (!Directory.Exists(directory))
                return null;

            return Directory.GetFiles(directory, CapturePrefix + "*.raw")
                .Where(path => minimumUtcTicks <= 0L
                    || File.GetLastWriteTimeUtc(path).Ticks >= minimumUtcTicks)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string FindLatestFramePacing(long minimumUtcTicks)
        {
            string directory = GetCaptureDirectory();
            if (!Directory.Exists(directory))
                return null;

            return Directory.GetFiles(directory, FramePacingPrefix + "*.json")
                .Where(path => minimumUtcTicks <= 0L
                    || File.GetLastWriteTimeUtc(path).Ticks >= minimumUtcTicks)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        private static string GetCaptureDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "DeadWallsProfilerCaptures");
        }

        private static string GetLogsDirectory()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
        }

        private static string GetStatusPath()
        {
            return Path.Combine(GetLogsDirectory(), "DW_V1_PLAYER_COMBINED_STATUS.json");
        }

        private static string GetReportPath()
        {
            return Path.Combine(GetLogsDirectory(), "DW_V1_PLAYER_COMBINED_REPORT.txt");
        }

        private static string GetSummaryPath()
        {
            return Path.Combine(GetLogsDirectory(), "DW_V1_PLAYER_COMBINED_SUMMARY.json");
        }

        private static void WriteStatus(RunStatus status)
        {
            Directory.CreateDirectory(GetLogsDirectory());
            File.WriteAllText(
                GetStatusPath(),
                JsonUtility.ToJson(status, true),
                new UTF8Encoding(false));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void ReleaseRunner()
        {
            if (_testRunnerApi != null && _callbacks != null)
                _testRunnerApi.UnregisterCallbacks(_callbacks);
            if (_testRunnerApi != null)
                UnityEngine.Object.DestroyImmediate(_testRunnerApi);
            _callbacks = null;
            _testRunnerApi = null;
        }
    }
}
#endif
