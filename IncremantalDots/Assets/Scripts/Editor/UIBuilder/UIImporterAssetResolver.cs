using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace DeadWalls
{
    /// <summary>
    /// Resolves TMP_FontAsset and Sprite references from AssetDatabase.
    /// Uses lazy-init cache for fast repeated lookups.
    /// </summary>
    public class UIImporterAssetResolver
    {
        Dictionary<string, TMP_FontAsset> _fontCache;
        Dictionary<string, Sprite> _spriteCache;
        TMP_FontAsset _defaultFont;
        readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Warnings => _warnings;

        public UIImporterAssetResolver(TMP_FontAsset defaultFont)
        {
            _defaultFont = defaultFont;
        }

        public void ClearWarnings() => _warnings.Clear();

        // ═══════════════════════════════════════════════════════════════
        // FONT RESOLUTION
        // ═══════════════════════════════════════════════════════════════

        public TMP_FontAsset ResolveFont(string fontName)
        {
            if (string.IsNullOrEmpty(fontName))
                return GetDefaultFont();

            EnsureFontCache();

            // Exact match
            if (_fontCache.TryGetValue(fontName, out var exact))
                return exact;

            // Fuzzy match (contains)
            string lowerName = fontName.ToLowerInvariant();
            foreach (var kvp in _fontCache)
            {
                if (kvp.Key.ToLowerInvariant().Contains(lowerName))
                    return kvp.Value;
            }

            _warnings.Add($"Font '{fontName}' not found — using default font.");
            return GetDefaultFont();
        }

        public TMP_FontAsset GetDefaultFont()
        {
            if (_defaultFont != null)
                return _defaultFont;

            // Try TMP_Settings default
            if (TMP_Settings.defaultFontAsset != null)
            {
                _defaultFont = TMP_Settings.defaultFontAsset;
                return _defaultFont;
            }

            // Last resort: find any font in project
            EnsureFontCache();
            foreach (var kvp in _fontCache)
            {
                _defaultFont = kvp.Value;
                return _defaultFont;
            }

            _warnings.Add("No TMP font asset found in project.");
            return null;
        }

        public string[] GetAvailableFontNames()
        {
            EnsureFontCache();
            var names = new string[_fontCache.Count];
            int i = 0;
            foreach (var key in _fontCache.Keys)
                names[i++] = key;
            return names;
        }

        public TMP_FontAsset GetFontByIndex(int index)
        {
            EnsureFontCache();
            int i = 0;
            foreach (var kvp in _fontCache)
            {
                if (i == index) return kvp.Value;
                i++;
            }
            return GetDefaultFont();
        }

        void EnsureFontCache()
        {
            if (_fontCache != null) return;

            _fontCache = new Dictionary<string, TMP_FontAsset>();
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null && !_fontCache.ContainsKey(font.name))
                    _fontCache[font.name] = font;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SPRITE RESOLUTION
        // ═══════════════════════════════════════════════════════════════

        public Sprite ResolveSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            if (_spriteCache == null)
                _spriteCache = new Dictionary<string, Sprite>();

            if (_spriteCache.TryGetValue(spriteName, out var cached))
                return cached;

            // Search by name
            string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    _spriteCache[spriteName] = sprite;
                    if (guids.Length > 1)
                        _warnings.Add($"Multiple sprites found for '{spriteName}' — using first match: {path}");
                    return sprite;
                }
            }

            // Search by path (user might provide "UI/Icons/myIcon")
            var directSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{spriteName}.png");
            if (directSprite == null)
                directSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{spriteName}.jpg");

            if (directSprite != null)
            {
                _spriteCache[spriteName] = directSprite;
                return directSprite;
            }

            _warnings.Add($"Sprite '{spriteName}' not found — Image will have no sprite.");
            _spriteCache[spriteName] = null;
            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // COLOR UTILITY
        // ═══════════════════════════════════════════════════════════════

        public static Color ParseColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex))
                return defaultColor;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            Debug.LogWarning($"[UIImporter] Invalid color format: {hex}");
            return defaultColor;
        }

        public static Color ParseColor(string hex)
        {
            return ParseColor(hex, Color.white);
        }

        /// <summary>
        /// Invalidates all caches — useful if assets change between imports.
        /// </summary>
        public void InvalidateCache()
        {
            _fontCache = null;
            _spriteCache = null;
        }
    }
}
