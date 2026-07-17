using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeadWalls.Tests
{
    public class NoSpeedOfflineProgressContractTests
    {
        private const string MainScenePath = "Assets/Scenes/NewGameScene.unity";

        private static readonly Regex TimeScaleWriterRegex = new Regex(
            @"\bTime\s*\.\s*timeScale\s*(?<operator>\*=|\+=|-=|/=|=)\s*(?<expression>[^;]+);",
            RegexOptions.Compiled);

        private static readonly Regex WallClockRegex = new Regex(
            @"\b(?:System\.)?DateTime(?:Offset)?\s*\.\s*(?:Now|UtcNow)\b|\bToUnixTime(?:Seconds|Milliseconds)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex OfflineOwnerRegex = new Regex(
            @"\b(?:offlineProgress|lastLogin|lastSeen|lastPlayed|elapsedOffline|awayTime|offlineIncome|offlineDeath)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PlayerSpeedControlRegex = new Regex(
            @"\b(?:speed\s*up|game\s*speed|fast\s*forward)\b|(?:^|[^a-z0-9])(?:x2|2x|x4|4x)(?:$|[^a-z0-9])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex NonCodeRegex = new Regex(
            "(?s:/\\*.*?\\*/)|(?://[^\\r\\n]*)|(?:@\"(?:\"\"|[^\"])*\")|(?:\"(?:\\\\.|[^\"\\\\])*\")|(?:'(?:\\\\.|[^'\\\\])*')",
            RegexOptions.Compiled);

        [Test]
        public void ProductionRuntime_HasNoPlayerAccelerationOrWallClockProgression()
        {
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string editorSegment = $"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}";
            var timeScaleWriterFiles = new HashSet<string>(StringComparer.Ordinal);
            var violations = new List<string>();

            foreach (string path in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (path.IndexOf(editorSegment, StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string relativePath = path.Substring(scriptsRoot.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace('\\', '/');
                string source = File.ReadAllText(path);
                string executableSource = NonCodeRegex.Replace(source, " ");

                foreach (Match match in TimeScaleWriterRegex.Matches(executableSource))
                {
                    timeScaleWriterFiles.Add(relativePath);
                    string operation = match.Groups["operator"].Value;
                    string expression = Regex.Replace(
                        match.Groups["expression"].Value,
                        @"\s+",
                        string.Empty);

                    bool approved = operation == "=" && IsApprovedTimeScaleWrite(
                        relativePath,
                        expression);
                    if (!approved)
                        violations.Add($"{relativePath}: Time.timeScale {operation} {expression}");
                }

                if (Regex.IsMatch(executableSource, @"\bTime\s*\.\s*fixedDeltaTime\s*(?:\*=|\+=|-=|/=|=)"))
                    violations.Add($"{relativePath}: Time.fixedDeltaTime writer");
                if (WallClockRegex.IsMatch(executableSource))
                    violations.Add($"{relativePath}: wall-clock progression API");
                if (OfflineOwnerRegex.IsMatch(executableSource))
                    violations.Add($"{relativePath}: offline progression owner name");
            }

            Assert.That(violations, Is.Empty,
                "Production runtime oyuncuya x2/x4 hizlandirma veya wall-clock/offline ilerleme eklememeli.");
            Assert.That(timeScaleWriterFiles.OrderBy(path => path), Is.EqualTo(new[]
            {
                "MonoBehaviour/MainMenuSceneUI.cs",
                "MonoBehaviour/RunBootstrap.cs",
                "MonoBehaviour/SimulationPauseService.cs",
                "MonoBehaviour/UIManager.cs"
            }), "Time scale sahipligi yalniz normal hiz, merkezi pause ve olum sunumu sinirinda kalmali.");
        }

        [Test]
        public void RunAndMetaSaveSchemas_HaveNoOfflineAccrualFields()
        {
            Type[] durableSchemas =
            {
                typeof(RunSaveState),
                typeof(MetaProgressState),
                typeof(RunDeathReceipt)
            };
            Regex forbiddenField = new Regex(
                @"timestamp|offline|lastLogin|lastSeen|lastPlayed|away|elapsedReal",
                RegexOptions.IgnoreCase);

            string[] violations = durableSchemas
                .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(field => forbiddenField.IsMatch(field.Name))
                    .Select(field => $"{type.Name}.{field.Name}"))
                .OrderBy(name => name)
                .ToArray();

            Assert.That(violations, Is.Empty,
                "Run/meta save semasi kapali sureyi olcmemeli veya offline kazanc/olum uygulamamali.");
        }

        [Test]
        public void ProductionScene_ExposesPauseButNoPlayerSpeedControl()
        {
            Scene scene = GetOrOpenScene(MainScenePath, out bool openedByTest);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                PauseMenuUI[] pauseMenus = roots
                    .SelectMany(root => root.GetComponentsInChildren<PauseMenuUI>(true))
                    .ToArray();
                Assert.That(pauseMenus, Has.Length.EqualTo(1));
                Assert.That(pauseMenus[0].PauseButton, Is.Not.Null);

                string[] forbiddenControls = roots
                    .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                    .SelectMany(button => new[] { button.gameObject.name }.Concat(
                        button.GetComponentsInChildren<Component>(true).Select(ReadText)))
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Where(value => PlayerSpeedControlRegex.IsMatch(value))
                    .OrderBy(value => value)
                    .ToArray();

                Assert.That(forbiddenControls, Is.Empty,
                    "Production scene pause sunabilir fakat player-facing x2/x4 kontrolu sunmamali.");
            }
            finally
            {
                if (openedByTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool IsApprovedTimeScaleWrite(string relativePath, string expression)
        {
            switch (relativePath)
            {
                case "MonoBehaviour/MainMenuSceneUI.cs":
                case "MonoBehaviour/RunBootstrap.cs":
                    return expression == "1f";
                case "MonoBehaviour/SimulationPauseService.cs":
                    return expression == "value";
                case "MonoBehaviour/UIManager.cs":
                    return expression == "0f" || expression == "0.25f" || expression == "1f";
                default:
                    return false;
            }
        }

        private static string ReadText(Component component)
        {
            PropertyInfo textProperty = component.GetType().GetProperty(
                "text",
                BindingFlags.Instance | BindingFlags.Public);
            return textProperty?.PropertyType == typeof(string)
                ? textProperty.GetValue(component) as string
                : null;
        }

        private static Scene GetOrOpenScene(string path, out bool openedByTest)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.path == path)
                {
                    openedByTest = false;
                    return loadedScene;
                }
            }

            openedByTest = true;
            Scene openedScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Assert.That(openedScene.IsValid(), Is.True, path);
            return openedScene;
        }
    }
}
