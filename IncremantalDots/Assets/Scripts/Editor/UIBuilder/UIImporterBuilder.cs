using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

namespace DeadWalls
{
    // ═══════════════════════════════════════════════════════════════
    // SETTINGS
    // ═══════════════════════════════════════════════════════════════

    public class UIImporterSettings
    {
        public string outputPath = "Assets/Prefabs/UI/Generated";
        public TMP_FontAsset defaultFont;
        public bool includeCanvas = true;
        public bool alwaysOverwrite;
    }

    // ═══════════════════════════════════════════════════════════════
    // IMPORT RESULT
    // ═══════════════════════════════════════════════════════════════

    public class UIImportResult
    {
        public bool success;
        public int totalElements;
        public int skippedElements;
        public string outputPath;
        public float elapsedSeconds;
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> errors = new List<string>();
        public readonly List<string> info = new List<string>();
        public readonly List<SpriteSlotInfo> spriteSlots = new List<SpriteSlotInfo>();
    }

    public class SpriteSlotInfo
    {
        public string elementName;
        public string elementPath;
        public string category;
        public string hint;
        public string elementType;     // "Image", "Button", "Panel", etc.
        public string parentName;      // parent element adı
        public Vector2 rectSize;       // element sizeDelta
        public string suggestedSize;   // web'den: "32x32", "9-slice"
    }

    // ═══════════════════════════════════════════════════════════════
    // PREVIEW CONTEXT — gizli Canvas + Camera + RenderTexture
    // ═══════════════════════════════════════════════════════════════

    public class PreviewContext
    {
        public GameObject canvasGO;
        public GameObject cameraGO;
        public Camera camera;
        public RenderTexture renderTexture;
        public UIDocumentData document;
        public List<SpriteSlotInfo> spriteSlots = new List<SpriteSlotInfo>();
        public Dictionary<string, GameObject> elementMap = new Dictionary<string, GameObject>();

        /// <summary>
        /// Returns the element's rect in RenderTexture pixel coordinates (0,0 = bottom-left).
        /// Returns Rect.zero if element not found.
        /// </summary>
        public Rect GetElementTextureRect(string elementPath)
        {
            if (camera == null || renderTexture == null) return Rect.zero;
            if (!elementMap.TryGetValue(elementPath, out var go) || go == null) return Rect.zero;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return Rect.zero;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners); // bottom-left, top-left, top-right, bottom-right

            // World → Screen (RenderTexture pixels)
            Vector3 min = camera.WorldToScreenPoint(corners[0]);
            Vector3 max = camera.WorldToScreenPoint(corners[2]);

            float x = Mathf.Min(min.x, max.x);
            float y = Mathf.Min(min.y, max.y);
            float w = Mathf.Abs(max.x - min.x);
            float h = Mathf.Abs(max.y - min.y);

            return new Rect(x, y, w, h);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // BUILDER — orchestrates the entire import pipeline
    // ═══════════════════════════════════════════════════════════════

    public static class UIImporterBuilder
    {
        // ═══════════════════════════════════════════════════════════════
        // PREVIEW — gizli Canvas+Camera ile WYSIWYG preview
        // ═══════════════════════════════════════════════════════════════

        public static PreviewContext CreatePreview(UIDocumentData doc, UIImporterAssetResolver resolver,
            Dictionary<string, Sprite> spriteAssignments, int width, int height)
        {
            var ctx = new PreviewContext();
            ctx.document = doc;

            // RenderTexture
            ctx.renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            ctx.renderTexture.hideFlags = HideFlags.HideAndDontSave;
            ctx.renderTexture.Create();

            // Camera
            ctx.cameraGO = new GameObject("__UIImporter_PreviewCamera__");
            ctx.cameraGO.hideFlags = HideFlags.HideAndDontSave;
            ctx.camera = ctx.cameraGO.AddComponent<Camera>();
            ctx.camera.clearFlags = CameraClearFlags.SolidColor;
            ctx.camera.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 1f);
            ctx.camera.orthographic = true;
            ctx.camera.cullingMask = 1 << 5; // UI layer
            ctx.camera.targetTexture = ctx.renderTexture;
            ctx.camera.enabled = false; // Manuel render

            // Canvas (ScreenSpace - Camera)
            ctx.canvasGO = new GameObject("__UIImporter_PreviewCanvas__", typeof(RectTransform));
            ctx.canvasGO.hideFlags = HideFlags.HideAndDontSave;
            ctx.canvasGO.layer = 5; // UI layer

            var canvas = ctx.canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = ctx.camera;
            canvas.planeDistance = 10f;

            var scaler = ctx.canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = doc.canvas.referenceResolution.ToVector2();
            scaler.matchWidthOrHeight = doc.canvas.matchWidthOrHeight;

            ctx.canvasGO.AddComponent<GraphicRaycaster>();

            // Build UI
            var toggleGroups = new Dictionary<string, ToggleGroup>();
            var result = new UIImportResult();
            BuildElementRecursiveWithMap(doc.root, ctx.canvasGO.GetComponent<RectTransform>(),
                resolver, toggleGroups, ctx.canvasGO.transform, result, ctx.elementMap, "");

            // Collect sprite slots
            CollectSpriteSlots(doc.root, "", ctx.spriteSlots);

            // Apply initial sprite assignments
            if (spriteAssignments != null)
            {
                foreach (var kvp in spriteAssignments)
                    UpdatePreviewSprite(ctx, kvp.Key, kvp.Value);
            }

            // Set all children to UI layer
            SetLayerRecursive(ctx.canvasGO, 5);

            // Initial render
            ctx.camera.Render();

            return ctx;
        }

        public static void UpdatePreviewSprite(PreviewContext ctx, string elementPath, Sprite sprite)
        {
            if (ctx == null || ctx.elementMap == null) return;

            if (ctx.elementMap.TryGetValue(elementPath, out var go) && go != null)
            {
                var image = go.GetComponent<Image>();
                if (image != null)
                    image.sprite = sprite;
            }
        }

        public static void RenderPreview(PreviewContext ctx)
        {
            if (ctx?.camera != null)
                ctx.camera.Render();
        }

        public static void ResizePreview(PreviewContext ctx, int width, int height)
        {
            if (ctx == null) return;

            if (ctx.renderTexture != null)
            {
                ctx.renderTexture.Release();
                ctx.renderTexture.width = width;
                ctx.renderTexture.height = height;
                ctx.renderTexture.Create();
            }

            ctx.camera.Render();
        }

        public static void DestroyPreview(PreviewContext ctx)
        {
            if (ctx == null) return;

            if (ctx.renderTexture != null)
            {
                ctx.renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(ctx.renderTexture);
            }
            if (ctx.canvasGO != null)
                UnityEngine.Object.DestroyImmediate(ctx.canvasGO);
            if (ctx.cameraGO != null)
                UnityEngine.Object.DestroyImmediate(ctx.cameraGO);
        }

        static void BuildElementRecursiveWithMap(UIElementData data, Transform parent,
            UIImporterAssetResolver resolver, Dictionary<string, ToggleGroup> toggleGroups,
            Transform canvasRoot, UIImportResult result,
            Dictionary<string, GameObject> elementMap, string parentPath)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? data.name : $"{parentPath}/{data.name}";
            GameObject go;

            try
            {
                go = UIImporterElementFactory.CreateElement(data, resolver, toggleGroups, canvasRoot);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIImporter Preview] Failed to create '{data.name}': {ex.Message}");
                return;
            }

            go.transform.SetParent(parent, false);
            elementMap[currentPath] = go;
            RegisterGeneratedElementPaths(data, go, currentPath, elementMap);

            // Special children routing (ScrollView → Content, TabGroup → Pages, Table → Content)
            Transform childTarget = go.transform;

            if (data.type == "ScrollView")
            {
                var content = FindChild(go.transform, "Content");
                if (content != null) childTarget = content;
            }
            else if (data.type == "Table")
            {
                var content = FindChild(go.transform, "Content");
                if (content != null) childTarget = content;
            }
            else if (data.type == "TabGroup")
            {
                var pages = FindChild(go.transform, "Pages");
                if (pages != null)
                {
                    if (data.children != null)
                    {
                        int activeTab = data.tabGroup?.activeTab ?? 0;
                        for (int i = 0; i < data.children.Count; i++)
                        {
                            BuildElementRecursiveWithMap(data.children[i], pages, resolver,
                                toggleGroups, canvasRoot, result, elementMap, currentPath);
                            var pageGO = pages.GetChild(pages.childCount - 1).gameObject;
                            pageGO.SetActive(i == activeTab);
                        }
                    }
                    return;
                }
            }

            if (data.children != null)
            {
                foreach (var child in data.children)
                    BuildElementRecursiveWithMap(child, childTarget, resolver,
                        toggleGroups, canvasRoot, result, elementMap, currentPath);
            }
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }

        static void RegisterGeneratedElementPaths(UIElementData data, GameObject go, string currentPath,
            Dictionary<string, GameObject> elementMap)
        {
            if (data.type != "TabGroup" || data.tabGroup?.tabs == null)
                return;

            foreach (var tab in data.tabGroup.tabs)
            {
                if (string.IsNullOrEmpty(tab.iconSlot))
                    continue;

                var icon = go.transform.Find($"TabBar/Tab_{tab.label}/Icon");
                if (icon != null)
                    elementMap[$"{currentPath}/TabBar/Tab_{tab.label}/Icon"] = icon.gameObject;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SPRITE SLOT COLLECTOR (standalone — Window'dan da çağrılabilir)
        // ═══════════════════════════════════════════════════════════════

        public static void CollectSpriteSlots(UIElementData data, string path, List<SpriteSlotInfo> slots,
            string parentName = null)
        {
            string currentPath = string.IsNullOrEmpty(path) ? data.name : $"{path}/{data.name}";

            // Element'in boyutunu al
            Vector2 size = Vector2.zero;
            if (data.rectTransform?.sizeDelta != null)
                size = data.rectTransform.sizeDelta.ToVector2();

            if (data.spriteSlot != null)
            {
                slots.Add(new SpriteSlotInfo
                {
                    elementName = data.name,
                    elementPath = currentPath,
                    category = data.spriteSlot.category ?? "icon",
                    hint = data.spriteSlot.hint,
                    elementType = data.type ?? "Image",
                    parentName = parentName ?? "",
                    rectSize = size,
                    suggestedSize = data.spriteSlot.suggestedSize
                });
            }

            if (data.tabGroup?.tabs != null)
            {
                foreach (var tab in data.tabGroup.tabs)
                {
                    if (!string.IsNullOrEmpty(tab.iconSlot))
                    {
                        slots.Add(new SpriteSlotInfo
                        {
                            elementName = tab.iconSlot,
                            elementPath = $"{currentPath}/TabBar/Tab_{tab.label}/Icon",
                            category = "icon",
                            hint = $"Tab icon for '{tab.label}'",
                            elementType = "Image",
                            parentName = $"Tab_{tab.label}",
                            rectSize = new Vector2(20f, 20f),
                            suggestedSize = "20x20"
                        });
                    }
                }
            }

            if (data.children == null) return;
            foreach (var child in data.children)
                CollectSpriteSlots(child, currentPath, slots, data.name);
        }

        // ═══════════════════════════════════════════════════════════════
        // IMPORT
        // ═══════════════════════════════════════════════════════════════

        public static UIImportResult Import(string jsonPath, UIImporterSettings settings,
            Dictionary<string, Sprite> spriteAssignments = null, string jsonContent = null)
        {
            var result = new UIImportResult();
            var sw = Stopwatch.StartNew();
            GameObject rootGO = null;

            try
            {
                // 1. Read JSON (from content if provided, else from file)
                string json;
                if (!string.IsNullOrEmpty(jsonContent))
                {
                    json = jsonContent;
                }
                else
                {
                    if (!File.Exists(jsonPath))
                    {
                        result.errors.Add($"File not found: {jsonPath}");
                        result.success = false;
                        return result;
                    }

                    json = File.ReadAllText(jsonPath);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    result.errors.Add("JSON content is empty.");
                    result.success = false;
                    return result;
                }

                // 2. Parse
                UIDocumentData doc;
                try
                {
                    doc = UIImporterJsonParser.Parse(json);
                }
                catch (Exception ex)
                {
                    result.errors.Add($"JSON parse error: {ex.Message}");
                    result.success = false;
                    return result;
                }

                // 3. Validate
                if (doc.root == null)
                {
                    result.errors.Add("JSON has no 'root' element.");
                    result.success = false;
                    return result;
                }

                if (string.IsNullOrEmpty(doc.root.name))
                    doc.root.name = Path.GetFileNameWithoutExtension(jsonPath);

                result.totalElements = doc.root.CountAll();

                // 4. Resolve assets
                var resolver = new UIImporterAssetResolver(settings.defaultFont);

                // 5. Build hierarchy (with elementMap for path-based sprite assignment)
                var toggleGroups = new Dictionary<string, ToggleGroup>();
                var elementMap = new Dictionary<string, GameObject>();

                if (settings.includeCanvas)
                {
                    rootGO = CreateCanvas(doc.canvas, doc.root.name);
                    var canvasRT = rootGO.GetComponent<RectTransform>();
                    BuildElementRecursiveWithMap(doc.root, canvasRT, resolver, toggleGroups,
                        canvasRT, result, elementMap, "");
                }
                else
                {
                    rootGO = UIImporterElementFactory.CreateElement(doc.root, resolver, toggleGroups, null);
                    elementMap[doc.root.name] = rootGO;
                    if (doc.root.children != null)
                    {
                        foreach (var child in doc.root.children)
                            BuildElementRecursiveWithMap(child, rootGO.GetComponent<RectTransform>(),
                                resolver, toggleGroups, rootGO.transform, result, elementMap, doc.root.name);
                    }
                }

                // Collect resolver warnings
                foreach (var w in resolver.Warnings)
                    result.warnings.Add(w);

                // 6. Log onClick bindings + collect sprite slots
                CollectOnClickInfo(doc.root, "", result);
                var slotList = new List<SpriteSlotInfo>();
                CollectSpriteSlots(doc.root, "", slotList);
                result.spriteSlots.AddRange(slotList);

                // 6b. Apply sprite assignments via elementPath (not elementName)
                if (spriteAssignments != null)
                {
                    foreach (var slot in result.spriteSlots)
                    {
                        if (spriteAssignments.TryGetValue(slot.elementPath, out var sprite) && sprite != null)
                        {
                            if (elementMap.TryGetValue(slot.elementPath, out var go) && go != null)
                            {
                                var image = go.GetComponent<Image>();
                                if (image != null)
                                    image.sprite = sprite;
                            }
                        }
                    }
                }

                // 7. Ensure output directory
                if (!Directory.Exists(settings.outputPath))
                {
                    Directory.CreateDirectory(settings.outputPath);
                    result.info.Add($"Created directory: {settings.outputPath}");
                }

                // 8. Save prefab
                string prefabName = doc.root.name + ".prefab";
                string fullPath = Path.Combine(settings.outputPath, prefabName).Replace('\\', '/');

                if (File.Exists(fullPath) && !settings.alwaysOverwrite)
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "UI Importer",
                        $"Prefab already exists:\n{fullPath}\n\nOverwrite?",
                        "Overwrite", "Cancel");

                    if (!overwrite)
                    {
                        result.info.Add("Import cancelled — prefab already exists.");
                        result.success = false;
                        return result;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(rootGO, fullPath);
                result.outputPath = fullPath;
                result.success = true;
                result.info.Add($"Prefab saved: {fullPath}");
                result.info.Add($"{result.totalElements} elements processed, {result.skippedElements} skipped.");
            }
            catch (Exception ex)
            {
                result.errors.Add($"Unexpected error: {ex.Message}");
                result.success = false;
            }
            finally
            {
                // 9. Cleanup scene object
                if (rootGO != null)
                    UnityEngine.Object.DestroyImmediate(rootGO);

                sw.Stop();
                result.elapsedSeconds = (float)sw.Elapsed.TotalSeconds;

                AssetDatabase.Refresh();
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        // CANVAS CREATION
        // ═══════════════════════════════════════════════════════════════

        static GameObject CreateCanvas(CanvasData canvasData, string name)
        {
            var go = new GameObject($"Canvas_{name}", typeof(RectTransform));

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = ParseScaleMode(canvasData.scaleMode);
            scaler.referenceResolution = canvasData.referenceResolution.ToVector2();
            scaler.matchWidthOrHeight = canvasData.matchWidthOrHeight;

            go.AddComponent<GraphicRaycaster>();

            return go;
        }

        static CanvasScaler.ScaleMode ParseScaleMode(string mode)
        {
            switch (mode)
            {
                case "ConstantPixelSize": return CanvasScaler.ScaleMode.ConstantPixelSize;
                case "ConstantPhysicalSize": return CanvasScaler.ScaleMode.ConstantPhysicalSize;
                default: return CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // RECURSIVE BUILD
        // ═══════════════════════════════════════════════════════════════

        static void BuildElementRecursive(UIElementData data, Transform parent,
            UIImporterAssetResolver resolver, Dictionary<string, ToggleGroup> toggleGroups,
            Transform canvasRoot, UIImportResult result)
        {
            GameObject go;
            try
            {
                go = UIImporterElementFactory.CreateElement(data, resolver, toggleGroups, canvasRoot);
            }
            catch (Exception ex)
            {
                result.errors.Add($"Failed to create '{data.name}' ({data.type}): {ex.Message}");
                result.skippedElements++;
                return;
            }

            go.transform.SetParent(parent, false);

            // ScrollView special case: children go into Content, not directly
            if (data.type == "ScrollView" && data.children != null)
            {
                var content = FindChild(go.transform, "Content");
                if (content != null)
                {
                    foreach (var child in data.children)
                        BuildElementRecursive(child, content, resolver, toggleGroups, canvasRoot, result);
                    return;
                }
            }

            // Table special case: children go into Content (if scrollable) or directly
            if (data.type == "Table" && data.children != null)
            {
                var content = FindChild(go.transform, "Content");
                var tableParent = content != null ? content : go.transform;
                foreach (var child in data.children)
                    BuildElementRecursive(child, tableParent, resolver, toggleGroups, canvasRoot, result);
                return;
            }

            // TabGroup special case: children go into Pages container
            if (data.type == "TabGroup" && data.children != null)
            {
                var pages = FindChild(go.transform, "Pages");
                if (pages != null)
                {
                    for (int i = 0; i < data.children.Count; i++)
                    {
                        var child = data.children[i];
                        BuildElementRecursive(child, pages, resolver, toggleGroups, canvasRoot, result);

                        // Only active tab's page is visible by default
                        var tabGroup = data.tabGroup;
                        int activeTab = tabGroup?.activeTab ?? 0;
                        var pageGO = pages.GetChild(pages.childCount - 1).gameObject;
                        pageGO.SetActive(i == activeTab);
                    }
                    return;
                }
            }

            BuildChildrenRecursive(data, go.GetComponent<RectTransform>(), resolver,
                toggleGroups, canvasRoot, result);
        }

        static void BuildChildrenRecursive(UIElementData data, Transform parent,
            UIImporterAssetResolver resolver, Dictionary<string, ToggleGroup> toggleGroups,
            Transform canvasRoot, UIImportResult result)
        {
            if (data.children == null) return;

            foreach (var child in data.children)
                BuildElementRecursive(child, parent, resolver, toggleGroups, canvasRoot, result);
        }

        static Transform FindChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // ON CLICK INFO COLLECTOR
        // ═══════════════════════════════════════════════════════════════

        static void CollectOnClickInfo(UIElementData data, string path, UIImportResult result)
        {
            string currentPath = string.IsNullOrEmpty(path) ? data.name : $"{path}/{data.name}";

            if (data.button != null && !string.IsNullOrEmpty(data.button.onClick))
                result.info.Add($"Button '{currentPath}' needs onClick binding: {data.button.onClick}");

            if (data.inputField != null)
            {
                if (!string.IsNullOrEmpty(data.inputField.onValueChanged))
                    result.info.Add($"InputField '{currentPath}' needs onValueChanged: {data.inputField.onValueChanged}");
                if (!string.IsNullOrEmpty(data.inputField.onEndEdit))
                    result.info.Add($"InputField '{currentPath}' needs onEndEdit: {data.inputField.onEndEdit}");
            }

            if (data.slider != null && !string.IsNullOrEmpty(data.slider.onValueChanged))
                result.info.Add($"Slider '{currentPath}' needs onValueChanged: {data.slider.onValueChanged}");

            if (data.toggle != null && !string.IsNullOrEmpty(data.toggle.onValueChanged))
                result.info.Add($"Toggle '{currentPath}' needs onValueChanged: {data.toggle.onValueChanged}");

            if (data.children == null) return;
            foreach (var child in data.children)
                CollectOnClickInfo(child, currentPath, result);
        }

    }
}
