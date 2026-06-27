using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    public sealed class UIImporterManifestData
    {
        public string projectName;
        public readonly Dictionary<string, string> sprites = new Dictionary<string, string>();
        public readonly Dictionary<string, Vector4> borders = new Dictionary<string, Vector4>();
    }

    public sealed class UIImporterExportLoadResult
    {
        public string exportFolderPath;
        public string uiJsonPath;
        public string manifestPath;
        public string jsonContent;
        public UIDocumentData document;
        public UIImporterManifestData manifest;
        public readonly List<string> errors = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> info = new List<string>();
        public readonly List<string> integrationReport = new List<string>();

        public bool success => errors.Count == 0;
    }

    public static class UIImporterExportFolder
    {
        public const string DefaultExportRoot = "Assets/UIExports";
        public const string PrefabOutputPath = "Assets/Prefabs/UI/Generated";
        public const string SpriteOutputRoot = "Assets/Sprites/UI/Generated";

        static readonly HashSet<string> SupportedElementTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Panel",
            "Text",
            "Image",
            "Button",
            "ScrollView",
            "InputField",
            "Slider",
            "Toggle",
            "Table",
            "TabGroup"
        };

        public static UIImporterExportLoadResult Load(string exportFolderPath)
        {
            var result = new UIImporterExportLoadResult();
            result.exportFolderPath = NormalizeAssetPath(exportFolderPath);

            if (string.IsNullOrWhiteSpace(result.exportFolderPath))
            {
                result.errors.Add("Export folder path is empty.");
                return result;
            }

            if (!AssetDatabase.IsValidFolder(result.exportFolderPath))
            {
                result.errors.Add($"Export folder is not inside Assets or does not exist: {result.exportFolderPath}");
                return result;
            }

            result.uiJsonPath = $"{result.exportFolderPath}/ui.json";
            result.manifestPath = $"{result.exportFolderPath}/manifest.json";

            if (!File.Exists(result.uiJsonPath))
                result.errors.Add($"Missing required ui.json: {result.uiJsonPath}");

            if (!File.Exists(result.manifestPath))
                result.errors.Add($"Missing required manifest.json: {result.manifestPath}");

            if (result.errors.Count > 0)
                return result;

            result.manifest = LoadManifest(result.manifestPath, result.errors, result.warnings);
            if (result.manifest == null)
                return result;

            try
            {
                result.jsonContent = File.ReadAllText(result.uiJsonPath);
                ValidateUiJsonShape(result.jsonContent, result.errors);
                if (result.errors.Count > 0)
                    return result;

                result.document = UIImporterJsonParser.Parse(result.jsonContent);
            }
            catch (Exception ex)
            {
                result.errors.Add($"ui.json parse error: {ex.Message}");
                return result;
            }

            ValidateDocument(result);
            ValidateSpriteMappings(result);
            BuildIntegrationReport(result);

            if (result.success)
                result.info.Add($"Ready: {result.manifest.projectName}");

            return result;
        }

        public static Dictionary<string, Sprite> ImportManifestSprites(UIImporterExportLoadResult loadResult,
            List<string> warnings, List<string> errors)
        {
            var assignments = new Dictionary<string, Sprite>();
            if (loadResult?.manifest == null || loadResult.manifest.sprites.Count == 0)
                return assignments;

            string outputDir = $"{SpriteOutputRoot}/{loadResult.manifest.projectName}";
            EnsureAssetFolder(SpriteOutputRoot);
            EnsureAssetFolder(outputDir);

            foreach (var kvp in loadResult.manifest.sprites)
            {
                string relativeSpritePath = NormalizeRelativePath(kvp.Value);
                string sourceAssetPath = $"{loadResult.exportFolderPath}/sprites/{relativeSpritePath}";
                string targetAssetPath = $"{outputDir}/{relativeSpritePath}";

                if (!File.Exists(sourceAssetPath))
                {
                    warnings.Add($"Sprite file missing for '{kvp.Key}': {sourceAssetPath}");
                    continue;
                }

                try
                {
                    string targetFullPath = Path.GetFullPath(targetAssetPath);
                    string targetDirectory = Path.GetDirectoryName(targetFullPath);
                    if (!Directory.Exists(targetDirectory))
                        Directory.CreateDirectory(targetDirectory);

                    File.Copy(Path.GetFullPath(sourceAssetPath), targetFullPath, true);
                    AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);

                    Vector4? border = null;
                    if (loadResult.manifest.borders.TryGetValue(relativeSpritePath, out var mappedBorder))
                        border = mappedBorder;

                    ConfigureSpriteImporter(targetAssetPath, border);
                    var sprite = LoadSpriteAtPath(targetAssetPath);
                    if (sprite != null)
                        assignments[kvp.Key] = sprite;
                    else
                        warnings.Add($"Sprite imported but loaded as null: {targetAssetPath}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to import sprite '{relativeSpritePath}': {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            return assignments;
        }

        public static Dictionary<string, Sprite> LoadManifestSpriteAssignments(UIImporterExportLoadResult loadResult,
            List<string> warnings)
        {
            var assignments = new Dictionary<string, Sprite>();
            if (loadResult?.manifest == null || loadResult.manifest.sprites.Count == 0)
                return assignments;

            foreach (var kvp in loadResult.manifest.sprites)
            {
                string relativeSpritePath = NormalizeRelativePath(kvp.Value);
                string sourceAssetPath = $"{loadResult.exportFolderPath}/sprites/{relativeSpritePath}";

                if (!File.Exists(sourceAssetPath))
                {
                    warnings.Add($"Sprite file missing for '{kvp.Key}': {sourceAssetPath}");
                    continue;
                }

                Vector4? border = null;
                if (loadResult.manifest.borders.TryGetValue(relativeSpritePath, out var mappedBorder))
                    border = mappedBorder;

                ConfigureSpriteImporter(sourceAssetPath, border);
                AssetDatabase.ImportAsset(sourceAssetPath, ImportAssetOptions.ForceUpdate);

                var sprite = LoadSpriteAtPath(sourceAssetPath);
                if (sprite != null)
                    assignments[kvp.Key] = sprite;
                else
                    warnings.Add($"Sprite loaded as null for '{kvp.Key}': {sourceAssetPath}");
            }

            return assignments;
        }

        static UIImporterManifestData LoadManifest(string manifestPath, List<string> errors, List<string> warnings)
        {
            JObject root;
            try
            {
                root = JObject.Parse(File.ReadAllText(manifestPath));
            }
            catch (Exception ex)
            {
                errors.Add($"manifest.json parse error: {ex.Message}");
                return null;
            }

            var manifest = new UIImporterManifestData();
            manifest.projectName = root.Value<string>("projectName");

            string projectNameError = ValidateProjectName(manifest.projectName);
            if (projectNameError != null)
                errors.Add(projectNameError);

            ReadStringMap(root, "sprites", manifest.sprites, errors);
            ReadBorderMap(root, "borders", manifest.borders, errors);

            foreach (var kvp in manifest.sprites)
            {
                string pathError = ValidateRelativeSpritePath(kvp.Value);
                if (pathError != null)
                    errors.Add($"Invalid sprite path for '{kvp.Key}': {pathError}");
            }

            foreach (string path in manifest.borders.Keys)
            {
                string pathError = ValidateRelativeSpritePath(path);
                if (pathError != null)
                    errors.Add($"Invalid border path '{path}': {pathError}");
            }

            if (manifest.sprites.Count == 0)
                warnings.Add("manifest.json has no sprite mappings. Sprite slots can still be assigned manually.");

            return errors.Count == 0 ? manifest : null;
        }

        static void ValidateUiJsonShape(string json, List<string> errors)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                errors.Add($"ui.json parse error: {ex.Message}");
                return;
            }

            if (!root.TryGetValue("canvas", out var canvasToken) || canvasToken.Type != JTokenType.Object)
                errors.Add("ui.json missing required canvas object.");

            if (!root.TryGetValue("root", out var rootToken) || rootToken.Type != JTokenType.Object)
                errors.Add("ui.json missing required root object.");
        }

        static void ValidateDocument(UIImporterExportLoadResult result)
        {
            if (result.document == null)
            {
                result.errors.Add("ui.json produced no document.");
                return;
            }

            if (result.document.canvas == null)
                result.errors.Add("ui.json missing required canvas object.");

            if (result.document.root == null)
            {
                result.errors.Add("ui.json missing required root element.");
                return;
            }

            var paths = new HashSet<string>(StringComparer.Ordinal);
            ValidateElement(result.document.root, "", paths, result.errors);
        }

        static void ValidateElement(UIElementData element, string parentPath, HashSet<string> paths, List<string> errors)
        {
            if (element == null)
                return;

            if (string.IsNullOrWhiteSpace(element.name))
                errors.Add($"Element under '{parentPath}' has an empty name.");

            string type = string.IsNullOrWhiteSpace(element.type) ? "Panel" : element.type;
            if (!SupportedElementTypes.Contains(type))
                errors.Add($"Unsupported element type '{type}' at '{BuildPath(parentPath, element.name)}'.");

            string currentPath = BuildPath(parentPath, element.name);
            if (!paths.Add(currentPath))
                errors.Add($"Duplicate element path: {currentPath}");

            if (element.children == null)
                return;

            foreach (var child in element.children)
                ValidateElement(child, currentPath, paths, errors);
        }

        static void ValidateSpriteMappings(UIImporterExportLoadResult result)
        {
            var slots = new List<SpriteSlotInfo>();
            UIImporterBuilder.CollectSpriteSlots(result.document.root, "", slots);
            var slotPaths = new HashSet<string>(slots.Select(s => s.elementPath), StringComparer.Ordinal);

            foreach (var slot in slots)
            {
                if (!result.manifest.sprites.ContainsKey(slot.elementPath))
                    result.warnings.Add($"Sprite slot has no manifest assignment: {slot.elementPath}");
            }

            foreach (string mappedPath in result.manifest.sprites.Keys)
            {
                if (!slotPaths.Contains(mappedPath))
                    result.warnings.Add($"manifest.json maps a non-slot path: {mappedPath}");
            }
        }

        static void BuildIntegrationReport(UIImporterExportLoadResult result)
        {
            if (result.document?.root == null)
                return;

            CollectIntegrationReport(result.document.root, "", result.integrationReport);
            if (result.integrationReport.Count == 0)
                result.integrationReport.Add("No interactive bindings declared in ui.json.");
        }

        static void CollectIntegrationReport(UIElementData element, string parentPath, List<string> report)
        {
            string currentPath = BuildPath(parentPath, element.name);

            if (element.button != null)
            {
                string field = ToSuggestedFieldName(element.name, "Button");
                string action = string.IsNullOrEmpty(element.button.onClick) ? "onClick not declared" : element.button.onClick;
                report.Add($"Button  {currentPath}  ->  {field}  ({action})");
            }

            if (element.inputField != null)
                report.Add($"Input   {currentPath}  ->  {ToSuggestedFieldName(element.name, "Input")}");

            if (element.slider != null)
                report.Add($"Slider  {currentPath}  ->  {ToSuggestedFieldName(element.name, "Slider")}");

            if (element.toggle != null)
                report.Add($"Toggle  {currentPath}  ->  {ToSuggestedFieldName(element.name, "Toggle")}");

            if (element.text != null)
                report.Add($"Text    {currentPath}  ->  {ToSuggestedFieldName(element.name, "Text")}");

            if (element.children == null)
                return;

            foreach (var child in element.children)
                CollectIntegrationReport(child, currentPath, report);
        }

        static string ToSuggestedFieldName(string elementName, string suffix)
        {
            if (string.IsNullOrWhiteSpace(elementName))
                return $"_{suffix.ToLowerInvariant()}";

            string cleaned = new string(elementName.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
                cleaned = suffix;

            return "_" + char.ToLowerInvariant(cleaned[0]) + cleaned.Substring(1);
        }

        static void ReadStringMap(JObject root, string key, Dictionary<string, string> map, List<string> errors)
        {
            if (!root.TryGetValue(key, out var token) || token.Type == JTokenType.Null)
                return;

            var obj = token as JObject;
            if (obj == null)
            {
                errors.Add($"manifest.json '{key}' must be an object.");
                return;
            }

            foreach (var prop in obj.Properties())
            {
                if (prop.Value.Type != JTokenType.String)
                {
                    errors.Add($"manifest.json '{key}.{prop.Name}' must be a string.");
                    continue;
                }

                map[prop.Name] = NormalizeRelativePath(prop.Value.Value<string>());
            }
        }

        static void ReadBorderMap(JObject root, string key, Dictionary<string, Vector4> map, List<string> errors)
        {
            if (!root.TryGetValue(key, out var token) || token.Type == JTokenType.Null)
                return;

            var obj = token as JObject;
            if (obj == null)
            {
                errors.Add($"manifest.json '{key}' must be an object.");
                return;
            }

            foreach (var prop in obj.Properties())
            {
                var borderObj = prop.Value as JObject;
                if (borderObj == null)
                {
                    errors.Add($"manifest.json '{key}.{prop.Name}' must be an object.");
                    continue;
                }

                float left = borderObj.Value<float?>("left") ?? 0f;
                float bottom = borderObj.Value<float?>("bottom") ?? 0f;
                float right = borderObj.Value<float?>("right") ?? 0f;
                float top = borderObj.Value<float?>("top") ?? 0f;
                map[NormalizeRelativePath(prop.Name)] = new Vector4(left, bottom, right, top);
            }
        }

        static string ValidateProjectName(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return "manifest.json missing required projectName.";

            if (projectName.Length < 3 || projectName.Length > 64)
                return $"projectName must be 3-64 characters: {projectName}";

            foreach (char c in projectName)
            {
                bool valid = char.IsLower(c) || char.IsDigit(c) || c == '_';
                if (!valid)
                    return $"projectName must be snake_case lowercase letters, digits, underscores only: {projectName}";
            }

            return null;
        }

        static string ValidateRelativeSpritePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "path is empty.";

            string path = NormalizeRelativePath(value);
            if (path.StartsWith("/", StringComparison.Ordinal) || path.Contains(":"))
                return "absolute paths are not allowed.";

            string[] parts = path.Split('/');
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part) || part == "." || part == "..")
                    return "path traversal or empty path segments are not allowed.";
            }

            return null;
        }

        static void ConfigureSpriteImporter(string assetPath, Vector4? border)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;

            if (border.HasValue)
                importer.spriteBorder = border.Value;

            importer.SaveAndReimport();
        }

        static Sprite LoadSpriteAtPath(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                return sprite;

            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }

        static void EnsureAssetFolder(string assetPath)
        {
            string normalized = NormalizeAssetPath(assetPath);
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string folder = Path.GetFileName(normalized);
            if (!string.IsNullOrEmpty(parent))
                EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        public static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? path : path.Replace('\\', '/').TrimEnd('/');
        }

        public static string NormalizeRelativePath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? path : path.Replace('\\', '/').TrimStart('/');
        }

        static string BuildPath(string parentPath, string name)
        {
            if (string.IsNullOrEmpty(parentPath))
                return name ?? "";
            return $"{parentPath}/{name}";
        }
    }
}
