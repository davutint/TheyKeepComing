using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public class UIImporterWindow : EditorWindow
    {
        const string StyleSheetPath = "Assets/Scripts/Editor/UIBuilder/UIImporterWindow.uss";
        const int PreviewWidth = 1280;
        const int PreviewHeight = 720;

        string _exportFolderPath;
        UIImporterExportLoadResult _loadResult;
        PreviewContext _previewCtx;
        UIImportResult _lastImportResult;

        readonly List<SpriteSlotInfo> _spriteSlots = new List<SpriteSlotInfo>();
        readonly Dictionary<string, Sprite> _spriteAssignments = new Dictionary<string, Sprite>();
        UIImporterAssetResolver _resolver;

        Label _statusLabel;
        Label _folderLabel;
        Label _projectLabel;
        Label _statsLabel;
        Label _previewHint;
        Image _previewImage;
        VisualElement _previewPanel;
        VisualElement _metaPanel;
        ScrollView _validationList;
        ScrollView _spriteSlotList;
        ScrollView _treeList;
        ScrollView _reportList;
        ScrollView _logList;
        Button _validateButton;
        Button _refreshButton;
        Button _importButton;
        Button _copyReportButton;
        Toggle _includeCanvasToggle;
        Toggle _overwriteToggle;

        [MenuItem("Window/DeadWalls/UI Importer")]
        static void ShowWindow()
        {
            var window = GetWindow<UIImporterWindow>();
            window.titleContent = new GUIContent("DeadWalls UI Importer");
            window.minSize = new Vector2(1040f, 660f);
            window.Show();
        }

        [MenuItem("Assets/DeadWalls/Load UI Export Folder", true)]
        static bool ValidateContextFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return AssetDatabase.IsValidFolder(path)
                   && File.Exists($"{path}/ui.json")
                   && File.Exists($"{path}/manifest.json");
        }

        [MenuItem("Assets/DeadWalls/Load UI Export Folder")]
        static void LoadContextFolder()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var window = GetWindow<UIImporterWindow>();
            window.LoadExportFolder(path);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.AddToClassList("dw-root");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            BuildToolbar();
            BuildBody();
            BuildFooter();
            RefreshView();
        }

        void OnDisable()
        {
            DestroyPreview();
        }

        void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("dw-toolbar");
            rootVisualElement.Add(toolbar);

            var titleBlock = new VisualElement();
            titleBlock.AddToClassList("dw-title-block");
            toolbar.Add(titleBlock);

            var title = new Label("DeadWalls UI Importer");
            title.AddToClassList("dw-title");
            titleBlock.Add(title);

            var subtitle = new Label("Codex export folder -> uGUI/TMP prefab");
            subtitle.AddToClassList("dw-subtitle");
            titleBlock.Add(subtitle);

            var actions = new VisualElement();
            actions.AddToClassList("dw-toolbar-actions");
            toolbar.Add(actions);

            actions.Add(MakeButton("Load Export Folder", LoadExportFolderFromDialog, "primary"));
            _validateButton = MakeButton("Validate", ReloadCurrentFolder, "secondary");
            _refreshButton = MakeButton("Refresh Preview", RefreshPreview, "secondary");
            _importButton = MakeButton("Import Prefab", ImportPrefab, "primary");
            _copyReportButton = MakeButton("Copy Report", CopyReport, "secondary");

            actions.Add(_validateButton);
            actions.Add(_refreshButton);
            actions.Add(_importButton);
            actions.Add(_copyReportButton);
        }

        void BuildBody()
        {
            var body = new VisualElement();
            body.AddToClassList("dw-body");
            rootVisualElement.Add(body);

            var left = new VisualElement();
            left.AddToClassList("dw-left");
            body.Add(left);

            _previewPanel = new VisualElement();
            _previewPanel.AddToClassList("dw-preview-panel");
            _previewPanel.RegisterCallback<DragUpdatedEvent>(HandleDragUpdated);
            _previewPanel.RegisterCallback<DragPerformEvent>(HandleDragPerform);
            _previewPanel.RegisterCallback<DragExitedEvent>(_ => _previewPanel.RemoveFromClassList("is-dragging"));
            left.Add(_previewPanel);

            _previewImage = new Image { scaleMode = ScaleMode.ScaleToFit };
            _previewImage.AddToClassList("dw-preview-image");
            _previewPanel.Add(_previewImage);

            _previewHint = new Label("Drop an Assets/UIExports/<projectName> folder here");
            _previewHint.AddToClassList("dw-preview-hint");
            _previewPanel.Add(_previewHint);

            _statusLabel = new Label("No export folder loaded.");
            _statusLabel.AddToClassList("dw-status");
            left.Add(_statusLabel);

            var right = new ScrollView();
            right.AddToClassList("dw-right");
            body.Add(right);

            _metaPanel = AddSection(right, "Project", out _);
            _validationList = AddScrollSection(right, "Validation");
            _spriteSlotList = AddScrollSection(right, "Sprite Slots");
            _treeList = AddScrollSection(right, "Hierarchy");
            _reportList = AddScrollSection(right, "Integration Report");
            _logList = AddScrollSection(right, "Import Result");
        }

        void BuildFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList("dw-footer");
            rootVisualElement.Add(footer);

            _includeCanvasToggle = new Toggle("Include Canvas") { value = true };
            _overwriteToggle = new Toggle("Overwrite Prefab") { value = false };
            footer.Add(_includeCanvasToggle);
            footer.Add(_overwriteToggle);

            var output = new Label($"Output: {UIImporterExportFolder.PrefabOutputPath}");
            output.AddToClassList("dw-footer-path");
            footer.Add(output);
        }

        void LoadExportFolderFromDialog()
        {
            string start = AssetDatabase.IsValidFolder(UIImporterExportFolder.DefaultExportRoot)
                ? Path.GetFullPath(UIImporterExportFolder.DefaultExportRoot)
                : Application.dataPath;

            string selected = EditorUtility.OpenFolderPanel("Select DeadWalls UI Export Folder", start, "");
            if (string.IsNullOrEmpty(selected))
                return;

            if (!TryToAssetPath(selected, out string assetPath))
            {
                SetStatus("Selected folder must be inside this project's Assets folder.", true);
                return;
            }

            LoadExportFolder(assetPath);
        }

        void ReloadCurrentFolder()
        {
            if (string.IsNullOrEmpty(_exportFolderPath))
                return;

            LoadExportFolder(_exportFolderPath);
        }

        void LoadExportFolder(string assetPath)
        {
            _exportFolderPath = UIImporterExportFolder.NormalizeAssetPath(assetPath);
            _spriteAssignments.Clear();
            _lastImportResult = null;
            DestroyPreview();

            _loadResult = UIImporterExportFolder.Load(_exportFolderPath);
            _spriteSlots.Clear();

            if (_loadResult.success)
            {
                UIImporterBuilder.CollectSpriteSlots(_loadResult.document.root, "", _spriteSlots);
                _resolver = new UIImporterAssetResolver(null);
                ApplyManifestSpriteAssignments(_loadResult.warnings);
                AutoFindSprites(false);
                CreatePreview();
            }

            RefreshView();
        }

        void RefreshPreview()
        {
            if (_loadResult?.success != true)
                return;

            DestroyPreview();
            CreatePreview();
            RefreshView();
        }

        void CreatePreview()
        {
            if (_resolver == null)
                _resolver = new UIImporterAssetResolver(null);
            _previewCtx = UIImporterBuilder.CreatePreview(_loadResult.document, _resolver, _spriteAssignments,
                PreviewWidth, PreviewHeight);
        }

        void DestroyPreview()
        {
            if (_previewCtx == null)
                return;

            UIImporterBuilder.DestroyPreview(_previewCtx);
            _previewCtx = null;
        }

        void ImportPrefab()
        {
            if (_loadResult?.success != true)
            {
                SetStatus("Cannot import: export folder is invalid.", true);
                return;
            }

            var spriteWarnings = new List<string>();
            var spriteErrors = new List<string>();
            var manifestAssignments = UIImporterExportFolder.ImportManifestSprites(_loadResult, spriteWarnings, spriteErrors);
            foreach (var kvp in manifestAssignments)
            {
                // Manifest export sozlesmesidir; Auto-Find sadece eksik slotlar icin fallback kalmali.
                _spriteAssignments[kvp.Key] = kvp.Value;
            }

            if (spriteErrors.Count > 0)
            {
                _lastImportResult = new UIImportResult { success = false };
                foreach (string error in spriteErrors)
                    _lastImportResult.errors.Add(error);
                foreach (string warning in spriteWarnings)
                    _lastImportResult.warnings.Add(warning);
                RefreshView();
                return;
            }

            var settings = new UIImporterSettings
            {
                outputPath = UIImporterExportFolder.PrefabOutputPath,
                defaultFont = null,
                includeCanvas = _includeCanvasToggle?.value ?? true,
                alwaysOverwrite = _overwriteToggle?.value ?? false
            };

            _lastImportResult = UIImporterBuilder.Import(_loadResult.uiJsonPath, settings, _spriteAssignments,
                _loadResult.jsonContent);

            foreach (string warning in spriteWarnings)
                _lastImportResult.warnings.Add(warning);

            if (_lastImportResult.success && !string.IsNullOrEmpty(_lastImportResult.outputPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_lastImportResult.outputPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
            }

            RefreshPreview();
        }

        void ApplyManifestSpriteAssignments(List<string> warnings)
        {
            var manifestAssignments = UIImporterExportFolder.LoadManifestSpriteAssignments(_loadResult, warnings);
            foreach (var kvp in manifestAssignments)
                _spriteAssignments[kvp.Key] = kvp.Value;
        }

        void AutoFindSprites(bool refreshPreview = true)
        {
            if (_spriteSlots.Count == 0)
                return;

            var searchFolders = new List<string>();
            if (_loadResult?.manifest != null)
            {
                string generatedFolder = $"{UIImporterExportFolder.SpriteOutputRoot}/{_loadResult.manifest.projectName}";
                if (AssetDatabase.IsValidFolder(generatedFolder))
                    searchFolders.Add(generatedFolder);
            }

            if (AssetDatabase.IsValidFolder("Assets/Sprites/UI"))
                searchFolders.Add("Assets/Sprites/UI");

            if (searchFolders.Count == 0)
                return;

            string[] guids = AssetDatabase.FindAssets("t:Sprite", searchFolders.ToArray());
            var spritesByName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (!spritesByName.ContainsKey(name))
                        spritesByName[name] = sprite;
                }
            }

            foreach (var slot in _spriteSlots)
            {
                if (_spriteAssignments.TryGetValue(slot.elementPath, out var existing) && existing != null)
                    continue;

                Sprite found = TryMatchSprite(slot, spritesByName);
                if (found != null)
                    _spriteAssignments[slot.elementPath] = found;
            }

            if (refreshPreview)
                RefreshPreview();
        }

        Sprite TryMatchSprite(SpriteSlotInfo slot, Dictionary<string, Sprite> spritesByName)
        {
            if (spritesByName.TryGetValue(slot.elementName, out var exact))
                return exact;

            string[] prefixes = { "icon_", "bg_", "border_", "btn_", "deco_" };
            foreach (string prefix in prefixes)
            {
                if (spritesByName.TryGetValue(prefix + slot.elementName, out var prefixed))
                    return prefixed;
            }

            var elementWords = SplitIntoWords(slot.elementName);
            Sprite bestMatch = null;
            int bestScore = 0;

            foreach (var kvp in spritesByName)
            {
                var spriteWords = SplitIntoWords(kvp.Key);
                int score = CountWordMatches(elementWords, spriteWords);
                if (score <= bestScore || score < 1)
                    continue;

                string expectedPrefix = GetSpritePrefix(slot.category);

                if (!string.IsNullOrEmpty(expectedPrefix) && spriteWords.Contains(expectedPrefix))
                    score += 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = kvp.Value;
                }
            }

            return bestMatch;
        }

        void RefreshView()
        {
            if (_metaPanel == null)
                return;

            bool hasFolder = !string.IsNullOrEmpty(_exportFolderPath);
            bool isValid = _loadResult?.success == true;

            _validateButton?.SetEnabled(hasFolder);
            _refreshButton?.SetEnabled(isValid);
            _importButton?.SetEnabled(isValid);
            _copyReportButton?.SetEnabled(hasFolder);

            RefreshStatus();
            RefreshPreviewElement();
            RefreshMetaPanel();
            RefreshValidationPanel();
            RefreshSpriteSlotsPanel();
            RefreshTreePanel();
            RefreshReportPanel();
            RefreshLogPanel();
        }

        void RefreshStatus()
        {
            if (_loadResult == null)
            {
                SetStatus("No export folder loaded.", false);
                return;
            }

            if (_loadResult.errors.Count > 0)
                SetStatus($"{_loadResult.errors.Count} validation error(s).", true);
            else if (_loadResult.warnings.Count > 0)
                SetStatus($"Valid with {_loadResult.warnings.Count} warning(s).", false, true);
            else
                SetStatus("Valid export folder. Ready to import.", false);
        }

        void SetStatus(string message, bool error, bool warning = false)
        {
            if (_statusLabel == null)
                return;

            _statusLabel.text = message;
            _statusLabel.EnableInClassList("is-error", error);
            _statusLabel.EnableInClassList("is-warning", warning && !error);
            _statusLabel.EnableInClassList("is-ok", !error && !warning);
        }

        void RefreshPreviewElement()
        {
            if (_previewImage == null || _previewHint == null)
                return;

            if (_previewCtx?.renderTexture != null)
            {
                _previewImage.image = _previewCtx.renderTexture;
                _previewImage.style.display = DisplayStyle.Flex;
                _previewHint.style.display = DisplayStyle.None;
            }
            else
            {
                _previewImage.image = null;
                _previewImage.style.display = DisplayStyle.None;
                _previewHint.style.display = DisplayStyle.Flex;
            }
        }

        void RefreshMetaPanel()
        {
            _metaPanel.Clear();
            _folderLabel = MakeMetaLine("Folder", string.IsNullOrEmpty(_exportFolderPath) ? "-" : _exportFolderPath);
            _projectLabel = MakeMetaLine("Project", _loadResult?.manifest?.projectName ?? "-");
            _statsLabel = MakeMetaLine("Elements", _loadResult?.document?.root != null
                ? _loadResult.document.root.CountAll().ToString()
                : "-");

            _metaPanel.Add(_folderLabel);
            _metaPanel.Add(_projectLabel);
            _metaPanel.Add(_statsLabel);
            _metaPanel.Add(MakeMetaLine("Prefab Output", UIImporterExportFolder.PrefabOutputPath));
            _metaPanel.Add(MakeMetaLine("Sprite Output", UIImporterExportFolder.SpriteOutputRoot));
        }

        void RefreshValidationPanel()
        {
            _validationList.Clear();
            if (_loadResult == null)
            {
                _validationList.Add(MakeMutedLine("Load an export folder to validate ui.json and manifest.json."));
                return;
            }

            AddMessages(_validationList, _loadResult.errors, "error");
            AddMessages(_validationList, _loadResult.warnings, "warning");
            AddMessages(_validationList, _loadResult.info, "info");
        }

        void RefreshSpriteSlotsPanel()
        {
            _spriteSlotList.Clear();
            if (_spriteSlots.Count == 0)
            {
                _spriteSlotList.Add(MakeMutedLine("No sprite slots declared."));
                return;
            }

            foreach (var slot in _spriteSlots.OrderBy(s => s.category).ThenBy(s => s.elementPath))
            {
                string path = slot.elementPath;
                var row = new VisualElement();
                row.AddToClassList("dw-slot-row");

                var header = new Label($"{slot.category} / {slot.elementName}");
                header.AddToClassList("dw-slot-title");
                row.Add(header);

                var hint = new Label(string.IsNullOrEmpty(slot.hint) ? path : $"{slot.hint}  -  {path}");
                hint.AddToClassList("dw-slot-hint");
                row.Add(hint);

                var field = new ObjectField { objectType = typeof(Sprite), allowSceneObjects = false };
                if (_spriteAssignments.TryGetValue(path, out var sprite))
                    field.value = sprite;
                field.RegisterValueChangedCallback(evt =>
                {
                    _spriteAssignments[path] = evt.newValue as Sprite;
                    UIImporterBuilder.UpdatePreviewSprite(_previewCtx, path, _spriteAssignments[path]);
                    UIImporterBuilder.RenderPreview(_previewCtx);
                    RefreshPreviewElement();
                });
                row.Add(field);
                _spriteSlotList.Add(row);
            }
        }

        void RefreshTreePanel()
        {
            _treeList.Clear();
            if (_loadResult?.document?.root == null)
            {
                _treeList.Add(MakeMutedLine("No hierarchy loaded."));
                return;
            }

            AddTreeNode(_treeList, _loadResult.document.root, 0);
        }

        void AddTreeNode(VisualElement parent, UIElementData element, int depth)
        {
            var line = new Label($"{new string(' ', depth * 2)}{element.type}: {element.name}");
            line.AddToClassList("dw-tree-line");
            parent.Add(line);

            if (element.children == null)
                return;

            foreach (var child in element.children)
                AddTreeNode(parent, child, depth + 1);
        }

        void RefreshReportPanel()
        {
            _reportList.Clear();
            if (_loadResult == null)
            {
                _reportList.Add(MakeMutedLine("No integration report yet."));
                return;
            }

            foreach (string line in _loadResult.integrationReport)
                _reportList.Add(MakeMessageLine(line, "info"));
        }

        void RefreshLogPanel()
        {
            _logList.Clear();
            if (_lastImportResult == null)
            {
                _logList.Add(MakeMutedLine("Import has not run in this window session."));
                return;
            }

            string status = _lastImportResult.success ? "Import succeeded" : "Import failed";
            _logList.Add(MakeMessageLine(status, _lastImportResult.success ? "info" : "error"));

            if (!string.IsNullOrEmpty(_lastImportResult.outputPath))
                _logList.Add(MakeMessageLine($"Prefab: {_lastImportResult.outputPath}", "info"));

            _logList.Add(MakeMessageLine(
                $"Elements: {_lastImportResult.totalElements} total, {_lastImportResult.skippedElements} skipped",
                _lastImportResult.skippedElements > 0 ? "warning" : "info"));

            AddMessages(_logList, _lastImportResult.errors, "error");
            AddMessages(_logList, _lastImportResult.warnings, "warning");
            AddMessages(_logList, _lastImportResult.info, "info");
        }

        void CopyReport()
        {
            EditorGUIUtility.systemCopyBuffer = BuildReportText();
            SetStatus("Report copied to clipboard.", false);
        }

        string BuildReportText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DeadWalls UI Importer Report");
            sb.AppendLine($"Folder: {_exportFolderPath ?? "-"}");
            sb.AppendLine($"Project: {_loadResult?.manifest?.projectName ?? "-"}");

            if (_loadResult != null)
            {
                AppendSection(sb, "Errors", _loadResult.errors);
                AppendSection(sb, "Warnings", _loadResult.warnings);
                AppendSection(sb, "Integration", _loadResult.integrationReport);
            }

            if (_lastImportResult != null)
            {
                sb.AppendLine();
                sb.AppendLine(_lastImportResult.success ? "Import: success" : "Import: failed");
                sb.AppendLine($"Prefab: {_lastImportResult.outputPath ?? "-"}");
                sb.AppendLine($"Elements: {_lastImportResult.totalElements} total, {_lastImportResult.skippedElements} skipped");
                AppendSection(sb, "Import Errors", _lastImportResult.errors);
                AppendSection(sb, "Import Warnings", _lastImportResult.warnings);
                AppendSection(sb, "Import Info", _lastImportResult.info);
            }

            return sb.ToString();
        }

        static void AppendSection(StringBuilder sb, string title, IReadOnlyList<string> lines)
        {
            sb.AppendLine();
            sb.AppendLine(title + ":");
            if (lines == null || lines.Count == 0)
            {
                sb.AppendLine("- none");
                return;
            }

            foreach (string line in lines)
                sb.AppendLine("- " + line);
        }

        void HandleDragUpdated(DragUpdatedEvent evt)
        {
            if (TryGetDraggedExportFolder(out _))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                _previewPanel.AddToClassList("is-dragging");
                evt.StopPropagation();
            }
        }

        void HandleDragPerform(DragPerformEvent evt)
        {
            _previewPanel.RemoveFromClassList("is-dragging");
            if (TryGetDraggedExportFolder(out string path))
            {
                DragAndDrop.AcceptDrag();
                LoadExportFolder(path);
                evt.StopPropagation();
            }
        }

        bool TryGetDraggedExportFolder(out string assetPath)
        {
            assetPath = null;
            if (DragAndDrop.paths == null)
                return false;

            foreach (string path in DragAndDrop.paths)
            {
                string candidate = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (TryToAssetPath(candidate, out assetPath)
                    && AssetDatabase.IsValidFolder(assetPath)
                    && File.Exists($"{assetPath}/ui.json")
                    && File.Exists($"{assetPath}/manifest.json"))
                    return true;
            }

            return false;
        }

        static Button MakeButton(string text, Action action, string styleClass)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("dw-button");
            button.AddToClassList($"dw-button-{styleClass}");
            return button;
        }

        static VisualElement AddSection(VisualElement parent, string title, out Label titleLabel)
        {
            var section = new VisualElement();
            section.AddToClassList("dw-section");
            parent.Add(section);

            titleLabel = new Label(title);
            titleLabel.AddToClassList("dw-section-title");
            section.Add(titleLabel);

            var content = new VisualElement();
            content.AddToClassList("dw-section-content");
            section.Add(content);
            return content;
        }

        static ScrollView AddScrollSection(VisualElement parent, string title)
        {
            var content = AddSection(parent, title, out _);
            var scroll = new ScrollView();
            scroll.AddToClassList("dw-section-scroll");
            content.Add(scroll);
            return scroll;
        }

        static Label MakeMetaLine(string label, string value)
        {
            var line = new Label($"{label}: {value}");
            line.AddToClassList("dw-meta-line");
            return line;
        }

        static Label MakeMutedLine(string text)
        {
            var line = new Label(text);
            line.AddToClassList("dw-muted-line");
            return line;
        }

        static Label MakeMessageLine(string text, string type)
        {
            var line = new Label(text);
            line.AddToClassList("dw-message");
            line.AddToClassList($"dw-message-{type}");
            return line;
        }

        static void AddMessages(VisualElement parent, IReadOnlyList<string> messages, string type)
        {
            if (messages == null || messages.Count == 0)
                return;

            foreach (string message in messages)
                parent.Add(MakeMessageLine(message, type));
        }

        static bool TryToAssetPath(string path, out string assetPath)
        {
            assetPath = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalized = path.Replace('\\', '/').TrimEnd('/');
            if (normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized == "Assets")
            {
                assetPath = normalized;
                return true;
            }

            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            if (normalized == dataPath)
            {
                assetPath = "Assets";
                return true;
            }

            if (normalized.StartsWith(dataPath + "/", StringComparison.Ordinal))
            {
                assetPath = "Assets" + normalized.Substring(dataPath.Length);
                return true;
            }

            return false;
        }

        static List<string> SplitIntoWords(string name)
        {
            var words = new List<string>();
            if (string.IsNullOrWhiteSpace(name))
                return words;

            foreach (string part in name.Split('_', '-', ' '))
            {
                int wordStart = 0;
                for (int i = 1; i < part.Length; i++)
                {
                    if (char.IsUpper(part[i]) && !char.IsUpper(part[i - 1]))
                    {
                        words.Add(part.Substring(wordStart, i - wordStart).ToLowerInvariant());
                        wordStart = i;
                    }
                }

                if (wordStart < part.Length)
                    words.Add(part.Substring(wordStart).ToLowerInvariant());
            }

            return words;
        }

        static int CountWordMatches(List<string> elementWords, List<string> spriteWords)
        {
            var commonWords = new HashSet<string> { "icon", "bg", "btn", "border", "deco", "image", "img", "sprite", "ui" };
            int matches = 0;
            foreach (string elementWord in elementWords)
            {
                if (elementWord.Length < 2 || commonWords.Contains(elementWord))
                    continue;

                foreach (string spriteWord in spriteWords)
                {
                    if (commonWords.Contains(spriteWord))
                        continue;

                    if (spriteWord == elementWord || spriteWord.Contains(elementWord) || elementWord.Contains(spriteWord))
                    {
                        matches++;
                        break;
                    }
                }
            }

            return matches;
        }

        static string GetSpritePrefix(string category)
        {
            switch (category)
            {
                case "icon": return "icon";
                case "background": return "bg";
                case "border": return "border";
                case "button": return "btn";
                case "decoration": return "deco";
                default: return "";
            }
        }
    }
}
