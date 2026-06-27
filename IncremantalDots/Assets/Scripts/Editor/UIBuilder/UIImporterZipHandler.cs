using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DeadWalls
{
    // ═══════════════════════════════════════════════════════════════
    // RESULT
    // ═══════════════════════════════════════════════════════════════

    public class ZipImportResult
    {
        public bool success;
        public bool cancelled;
        public string jsonContent;
        public string jsonFileName;
        public string projectName;
        public string outputDir;
        public Dictionary<string, Sprite> spriteAssignments = new Dictionary<string, Sprite>();
        public readonly List<string> importedSpritePaths = new List<string>();
        public readonly List<string> newFiles = new List<string>();
        public readonly List<string> overwrittenFiles = new List<string>();
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> errors = new List<string>();
    }

    // ═══════════════════════════════════════════════════════════════
    // ZIP HANDLER
    // ═══════════════════════════════════════════════════════════════

    public static class UIImporterZipHandler
    {
        const string DEFAULT_SPRITE_ROOT = "Assets/Sprites/UI/Generated";
        const string MANIFEST_FILENAME = "manifest.json";
        const int PROJECT_NAME_MIN = 3;
        const int PROJECT_NAME_MAX = 64;
        static readonly Regex ProjectNamePattern = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);

        // Collision dialog user choice
        enum CollisionChoice { Overwrite = 0, CreateUniqueFolder = 1, Cancel = 2 }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Opens a ZIP file, extracts sprites, parses manifest, and returns
        /// a ready-to-use sprite assignment dictionary keyed by elementPath.
        /// </summary>
        public static ZipImportResult ProcessZip(string zipPath, string spriteOutputFolder = null)
        {
            var result = new ZipImportResult();

            if (string.IsNullOrEmpty(spriteOutputFolder))
                spriteOutputFolder = DEFAULT_SPRITE_ROOT;

            if (!File.Exists(zipPath))
            {
                result.errors.Add($"ZIP file not found: {zipPath}");
                return result;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(zipPath))
                {
                    // 1. Find and read manifest.json
                    string manifestJson = ReadEntryAsString(archive, MANIFEST_FILENAME);
                    if (manifestJson == null)
                    {
                        result.errors.Add("ZIP does not contain manifest.json — cannot map sprites to elements.");
                        return result;
                    }

                    // 2. Parse manifest
                    string projectName;
                    Dictionary<string, string> spriteMap;
                    Dictionary<string, Vector4> borderMap;
                    try
                    {
                        ParseManifest(manifestJson, out projectName, out spriteMap, out borderMap);
                    }
                    catch (Exception ex)
                    {
                        result.errors.Add($"manifest.json parse error: {ex.Message}");
                        return result;
                    }

                    // 2a. Validate projectName (required)
                    string validationError = ValidateProjectName(projectName);
                    if (validationError != null)
                    {
                        result.errors.Add(validationError);
                        return result;
                    }

                    result.projectName = projectName;

                    if (spriteMap.Count == 0)
                    {
                        result.warnings.Add("manifest.json contains no sprite mappings.");
                    }

                    // 3. Find ui.json (any .json that isn't manifest.json)
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                            && !entry.Name.Equals(MANIFEST_FILENAME, StringComparison.OrdinalIgnoreCase))
                        {
                            result.jsonContent = ReadEntry(entry);
                            result.jsonFileName = entry.Name;
                            break;
                        }
                    }

                    // 4. Determine output folder from projectName (collision-safe)
                    string outputDir = $"{spriteOutputFolder}/{projectName}";

                    // 4a. Detect collisions and prompt user if any
                    var collidingFiles = DetectCollisions(spriteMap, outputDir);
                    if (collidingFiles.Count > 0)
                    {
                        var choice = PromptCollisionDialog(outputDir, collidingFiles);
                        if (choice == CollisionChoice.Cancel)
                        {
                            result.cancelled = true;
                            result.errors.Add("Import cancelled by user (collision detected).");
                            return result;
                        }
                        if (choice == CollisionChoice.CreateUniqueFolder)
                        {
                            outputDir = AssetDatabase.GenerateUniqueAssetPath(outputDir);
                            result.warnings.Add($"Collision resolved — using unique folder: {outputDir}");
                        }
                        // CollisionChoice.Overwrite → outputDir unchanged, ImportSprites will log overwrites
                    }

                    result.outputDir = outputDir;

                    // 5. Extract and import sprites
                    EditorUtility.DisplayProgressBar("UI Importer", "Importing sprites from ZIP...", 0f);
                    try
                    {
                        result.spriteAssignments = ImportSprites(archive, spriteMap, borderMap, outputDir, result);
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }

                result.success = result.errors.Count == 0;

                // Final summary log
                Debug.Log($"[UIImporter] Import {(result.success ? "complete" : "failed")} for '{result.projectName}':\n" +
                          $"  Folder:       {result.outputDir}\n" +
                          $"  New sprites:  {result.newFiles.Count}\n" +
                          $"  Overwritten:  {result.overwrittenFiles.Count}\n" +
                          $"  Warnings:     {result.warnings.Count}\n" +
                          $"  Errors:       {result.errors.Count}");
            }
            catch (Exception ex)
            {
                result.errors.Add($"ZIP processing error: {ex.Message}");
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        // PROJECT NAME VALIDATION
        // ═══════════════════════════════════════════════════════════════

        static string ValidateProjectName(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return "manifest.json missing required 'projectName' field. " +
                       "Add a 'projectName' (snake_case, e.g. \"hiring_panel\") at the top of manifest.json.";
            }
            if (projectName.Length < PROJECT_NAME_MIN || projectName.Length > PROJECT_NAME_MAX)
            {
                return $"manifest.json 'projectName' must be between {PROJECT_NAME_MIN} and {PROJECT_NAME_MAX} characters. " +
                       $"Got: '{projectName}' ({projectName.Length} chars).";
            }
            if (!ProjectNamePattern.IsMatch(projectName))
            {
                return $"manifest.json 'projectName' must be snake_case (lowercase letters, digits, underscores only). " +
                       $"Got: '{projectName}'. Example: 'hiring_panel'.";
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // COLLISION DETECTION + DIALOG
        // ═══════════════════════════════════════════════════════════════

        static List<string> DetectCollisions(Dictionary<string, string> spriteMap, string outputDir)
        {
            var collisions = new List<string>();
            var seen = new HashSet<string>();
            foreach (var filename in spriteMap.Values)
            {
                if (!seen.Add(filename)) continue;
                string fullPath = Path.GetFullPath($"{outputDir}/{filename}");
                if (File.Exists(fullPath))
                    collisions.Add(filename);
            }
            return collisions;
        }

        static CollisionChoice PromptCollisionDialog(string outputDir, List<string> collidingFiles)
        {
            const int MAX_LIST = 10;
            string list = string.Join("\n  ", collidingFiles.GetRange(0, Math.Min(MAX_LIST, collidingFiles.Count)));
            if (collidingFiles.Count > MAX_LIST)
                list += $"\n  ... and {collidingFiles.Count - MAX_LIST} more";

            string message =
                $"{collidingFiles.Count} existing sprite(s) in:\n  {outputDir}\n\n" +
                $"would be affected by this import:\n  {list}\n\n" +
                "Choose how to proceed:\n" +
                "  • Overwrite         — replace existing pixels (GUID preserved; existing prefab references keep pointing, new content)\n" +
                "  • Create unique folder — import into a versioned sibling folder (safer; old assets untouched)\n" +
                "  • Cancel            — abort the import";

            int choice = EditorUtility.DisplayDialogComplex(
                "UI Importer — Collision Detected",
                message,
                "Overwrite",
                "Cancel",
                "Create unique folder");

            // DisplayDialogComplex: 0=ok(Overwrite), 1=cancel(Cancel), 2=alt(Create unique folder)
            switch (choice)
            {
                case 0: return CollisionChoice.Overwrite;
                case 2: return CollisionChoice.CreateUniqueFolder;
                default: return CollisionChoice.Cancel;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // MANIFEST PARSER
        // ═══════════════════════════════════════════════════════════════

        static void ParseManifest(string json,
            out string projectName,
            out Dictionary<string, string> spriteMap,
            out Dictionary<string, Vector4> borderMap)
        {
            projectName = null;
            spriteMap = new Dictionary<string, string>();
            borderMap = new Dictionary<string, Vector4>();

            // Reuse the project's existing JSON parser
            int index = 0;
            var raw = ParseValue(json, ref index) as Dictionary<string, object>;
            if (raw == null)
                throw new Exception("manifest.json root must be a JSON object.");

            // Parse "projectName" (validated in caller)
            if (raw.TryGetValue("projectName", out object pnObj) && pnObj is string pnStr)
                projectName = pnStr;

            // Parse "sprites" block
            if (raw.TryGetValue("sprites", out object spritesObj) && spritesObj is Dictionary<string, object> sprites)
            {
                foreach (var kvp in sprites)
                {
                    if (kvp.Value is string filename)
                        spriteMap[kvp.Key] = filename;
                }
            }

            // Parse "borders" block (optional)
            if (raw.TryGetValue("borders", out object bordersObj) && bordersObj is Dictionary<string, object> borders)
            {
                foreach (var kvp in borders)
                {
                    if (kvp.Value is Dictionary<string, object> b)
                    {
                        float left = GetFloat(b, "left", 0f);
                        float bottom = GetFloat(b, "bottom", 0f);
                        float right = GetFloat(b, "right", 0f);
                        float top = GetFloat(b, "top", 0f);
                        borderMap[kvp.Key] = new Vector4(left, bottom, right, top);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SPRITE IMPORT
        // ═══════════════════════════════════════════════════════════════

        static Dictionary<string, Sprite> ImportSprites(
            ZipArchive archive,
            Dictionary<string, string> spriteMap,
            Dictionary<string, Vector4> borderMap,
            string outputDir,
            ZipImportResult result)
        {
            var assignments = new Dictionary<string, Sprite>();

            // Collect unique filenames we need to extract
            var filesToExtract = new HashSet<string>();
            foreach (var kvp in spriteMap)
                filesToExtract.Add(kvp.Value);

            // Ensure output directory
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                AssetDatabase.Refresh();
            }

            // Extract each sprite file
            int processed = 0;
            int total = filesToExtract.Count;
            var filenameToAssetPath = new Dictionary<string, string>();

            foreach (string filename in filesToExtract)
            {
                processed++;
                EditorUtility.DisplayProgressBar("UI Importer",
                    $"Importing sprite {processed}/{total}: {filename}",
                    (float)processed / total);

                var entry = FindEntry(archive, filename);
                if (entry == null)
                {
                    result.warnings.Add($"Sprite file '{filename}' referenced in manifest but not found in ZIP.");
                    continue;
                }

                string assetPath = $"{outputDir}/{filename}";
                string fullPathForCheck = Path.GetFullPath(assetPath);
                bool wasExisting = File.Exists(fullPathForCheck);

                // Extract to disk
                try
                {
                    string dir = Path.GetDirectoryName(fullPathForCheck);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(fullPathForCheck, FileMode.Create, FileAccess.Write))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    result.errors.Add($"Failed to extract '{filename}': {ex.Message}");
                    continue;
                }

                // Track new vs overwritten
                if (wasExisting)
                    result.overwrittenFiles.Add(filename);
                else
                    result.newFiles.Add(filename);

                // Import into AssetDatabase
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                // Configure TextureImporter
                Vector4? border = null;
                if (borderMap.TryGetValue(filename, out Vector4 b))
                    border = b;

                ConfigureSpriteImporter(assetPath, border);

                filenameToAssetPath[filename] = assetPath;
                result.importedSpritePaths.Add(assetPath);
            }

            // Build elementPath → Sprite assignments
            foreach (var kvp in spriteMap)
            {
                string elementPath = kvp.Key;
                string filename = kvp.Value;

                if (!filenameToAssetPath.TryGetValue(filename, out string assetPath))
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    assignments[elementPath] = sprite;
                }
                else
                {
                    result.warnings.Add($"Sprite loaded as null for '{filename}' at '{assetPath}'.");
                }
            }

            return assignments;
        }

        // ═══════════════════════════════════════════════════════════════
        // TEXTURE IMPORTER CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        static void ConfigureSpriteImporter(string assetPath, Vector4? border)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                dirty = true;
            }

            if (importer.sRGBTexture != true)
            {
                importer.sRGBTexture = true;
                dirty = true;
            }

            // 9-slice border
            if (border.HasValue && importer.spriteBorder != border.Value)
            {
                importer.spriteBorder = border.Value;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ZIP HELPERS
        // ═══════════════════════════════════════════════════════════════

        static string ReadEntryAsString(ZipArchive archive, string entryName)
        {
            var entry = FindEntry(archive, entryName);
            if (entry == null) return null;
            return ReadEntry(entry);
        }

        static string ReadEntry(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        static ZipArchiveEntry FindEntry(ZipArchive archive, string filename)
        {
            // Exact match first
            foreach (var entry in archive.Entries)
            {
                if (entry.Name.Equals(filename, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            // Full path match (in case of nested folders in ZIP)
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/" + filename, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // MINIMAL JSON HELPERS (manifest only — reuse parser pattern)
        // ═══════════════════════════════════════════════════════════════

        // Lightweight reuse of UIImporterJsonParser's approach for manifest.
        // We don't call UIImporterJsonParser directly to keep ZipHandler self-contained.

        static object ParseValue(string json, ref int i)
        {
            SkipWhitespace(json, ref i);
            if (i >= json.Length) return null;

            char c = json[i];
            if (c == '{') return ParseObject(json, ref i);
            if (c == '[') return ParseArray(json, ref i);
            if (c == '"') return ParseString(json, ref i);
            if (c == 't' || c == 'f') return ParseBool(json, ref i);
            if (c == 'n') { i += 4; return null; }
            return ParseNumber(json, ref i);
        }

        static Dictionary<string, object> ParseObject(string json, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // skip '{'
            SkipWhitespace(json, ref i);
            if (i < json.Length && json[i] == '}') { i++; return dict; }

            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                string key = ParseString(json, ref i);
                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ':') i++;
                dict[key] = ParseValue(json, ref i);
                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == '}') { i++; return dict; }
                if (i < json.Length && json[i] == ',') i++;
            }
            return dict;
        }

        static List<object> ParseArray(string json, ref int i)
        {
            var list = new List<object>();
            i++; // skip '['
            SkipWhitespace(json, ref i);
            if (i < json.Length && json[i] == ']') { i++; return list; }

            while (i < json.Length)
            {
                list.Add(ParseValue(json, ref i));
                SkipWhitespace(json, ref i);
                if (i < json.Length && json[i] == ']') { i++; return list; }
                if (i < json.Length && json[i] == ',') i++;
            }
            return list;
        }

        static string ParseString(string json, ref int i)
        {
            if (i >= json.Length || json[i] != '"') return "";
            i++;
            int start = i;
            while (i < json.Length)
            {
                if (json[i] == '\\') { i += 2; continue; }
                if (json[i] == '"') { string s = json.Substring(start, i - start); i++; return s; }
                i++;
            }
            return json.Substring(start);
        }

        static bool ParseBool(string json, ref int i)
        {
            if (i + 4 <= json.Length && json.Substring(i, 4) == "true") { i += 4; return true; }
            if (i + 5 <= json.Length && json.Substring(i, 5) == "false") { i += 5; return false; }
            i++;
            return false;
        }

        static object ParseNumber(string json, ref int i)
        {
            int start = i;
            if (i < json.Length && json[i] == '-') i++;
            while (i < json.Length && char.IsDigit(json[i])) i++;
            bool isFloat = false;
            if (i < json.Length && json[i] == '.') { isFloat = true; i++; while (i < json.Length && char.IsDigit(json[i])) i++; }
            string num = json.Substring(start, i - start);
            if (isFloat) return double.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
            return long.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
        }

        static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }

        static float GetFloat(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (!dict.TryGetValue(key, out object val)) return defaultValue;
            if (val is double d) return (float)d;
            if (val is long l) return l;
            return defaultValue;
        }
    }
}
