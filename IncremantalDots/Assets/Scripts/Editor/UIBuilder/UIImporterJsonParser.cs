using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DeadWalls
{
    /// <summary>
    /// Lightweight recursive JSON parser.
    /// JSON string → Dictionary/List tree → UIDocumentData.
    /// No external dependencies (JsonUtility can't handle recursive types).
    /// </summary>
    public static class UIImporterJsonParser
    {
        // ═══════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════

        public static UIDocumentData Parse(string json)
        {
            int index = 0;
            var raw = ParseValue(json, ref index) as Dictionary<string, object>;
            if (raw == null)
                throw new Exception("JSON root must be an object.");

            return DeserializeDocument(raw);
        }

        // ═══════════════════════════════════════════════════════════════
        // TOKENIZER — character-by-character JSON parser
        // ═══════════════════════════════════════════════════════════════

        static object ParseValue(string json, ref int i)
        {
            SkipWhitespace(json, ref i);
            if (i >= json.Length)
                throw new Exception("Unexpected end of JSON.");

            char c = json[i];

            if (c == '{') return ParseObject(json, ref i);
            if (c == '[') return ParseArray(json, ref i);
            if (c == '"') return ParseString(json, ref i);
            if (c == 't' || c == 'f') return ParseBool(json, ref i);
            if (c == 'n') return ParseNull(json, ref i);
            return ParseNumber(json, ref i);
        }

        static Dictionary<string, object> ParseObject(string json, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // skip '{'
            SkipWhitespace(json, ref i);

            if (json[i] == '}') { i++; return dict; }

            while (true)
            {
                SkipWhitespace(json, ref i);
                string key = ParseString(json, ref i);
                SkipWhitespace(json, ref i);

                if (json[i] != ':')
                    throw new Exception($"Expected ':' at position {i}");
                i++;

                object value = ParseValue(json, ref i);
                dict[key] = value;

                SkipWhitespace(json, ref i);
                if (json[i] == '}') { i++; return dict; }
                if (json[i] != ',')
                    throw new Exception($"Expected ',' or '}}' at position {i}");
                i++;
            }
        }

        static List<object> ParseArray(string json, ref int i)
        {
            var list = new List<object>();
            i++; // skip '['
            SkipWhitespace(json, ref i);

            if (json[i] == ']') { i++; return list; }

            while (true)
            {
                list.Add(ParseValue(json, ref i));
                SkipWhitespace(json, ref i);
                if (json[i] == ']') { i++; return list; }
                if (json[i] != ',')
                    throw new Exception($"Expected ',' or ']' at position {i}");
                i++;
            }
        }

        static string ParseString(string json, ref int i)
        {
            if (json[i] != '"')
                throw new Exception($"Expected '\"' at position {i}");
            i++;

            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '\\')
                {
                    i++;
                    char esc = json[i];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            string hex = json.Substring(i + 1, 4);
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            i += 4;
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else if (c == '"')
                {
                    i++;
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                }
                i++;
            }
            throw new Exception("Unterminated string.");
        }

        static object ParseNumber(string json, ref int i)
        {
            int start = i;
            if (json[i] == '-') i++;
            while (i < json.Length && char.IsDigit(json[i])) i++;

            bool isFloat = false;
            if (i < json.Length && json[i] == '.')
            {
                isFloat = true;
                i++;
                while (i < json.Length && char.IsDigit(json[i])) i++;
            }
            if (i < json.Length && (json[i] == 'e' || json[i] == 'E'))
            {
                isFloat = true;
                i++;
                if (i < json.Length && (json[i] == '+' || json[i] == '-')) i++;
                while (i < json.Length && char.IsDigit(json[i])) i++;
            }

            string numStr = json.Substring(start, i - start);
            if (isFloat)
                return double.Parse(numStr, CultureInfo.InvariantCulture);
            return long.Parse(numStr, CultureInfo.InvariantCulture);
        }

        static bool ParseBool(string json, ref int i)
        {
            if (i + 4 <= json.Length && json.Substring(i, 4) == "true") { i += 4; return true; }
            if (i + 5 <= json.Length && json.Substring(i, 5) == "false") { i += 5; return false; }
            throw new Exception($"Invalid boolean at position {i}");
        }

        static object ParseNull(string json, ref int i)
        {
            if (i + 4 <= json.Length && json.Substring(i, 4) == "null") { i += 4; return null; }
            throw new Exception($"Invalid null at position {i}");
        }

        static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }

        // ═══════════════════════════════════════════════════════════════
        // DESERIALIZERS — Dictionary tree → typed DTO objects
        // ═══════════════════════════════════════════════════════════════

        static UIDocumentData DeserializeDocument(Dictionary<string, object> raw)
        {
            var doc = new UIDocumentData();
            doc.version = GetString(raw, "version", "1.0");

            if (raw.TryGetValue("canvas", out object canvasObj))
                doc.canvas = DeserializeCanvas(canvasObj as Dictionary<string, object>);
            else
                doc.canvas = new CanvasData();

            if (raw.TryGetValue("root", out object rootObj))
                doc.root = DeserializeElement(rootObj as Dictionary<string, object>);

            return doc;
        }

        static CanvasData DeserializeCanvas(Dictionary<string, object> raw)
        {
            if (raw == null) return new CanvasData();
            var c = new CanvasData();
            c.scaleMode = GetString(raw, "scaleMode", "ScaleWithScreenSize");
            c.matchWidthOrHeight = GetFloat(raw, "matchWidthOrHeight", 0.5f);
            if (raw.TryGetValue("referenceResolution", out object resObj))
                c.referenceResolution = DeserializeVector2(resObj as Dictionary<string, object>, 1920f, 1080f);
            return c;
        }

        static UIElementData DeserializeElement(Dictionary<string, object> raw)
        {
            if (raw == null) return null;

            var e = new UIElementData();
            e.type = GetString(raw, "type", "Panel");
            e.name = GetString(raw, "name", e.type);
            e.active = GetBool(raw, "active", true);
            e.raycastTarget = GetBool(raw, "raycastTarget", true);

            if (raw.TryGetValue("canvasGroup", out object cgObj))
                e.canvasGroup = DeserializeCanvasGroup(cgObj as Dictionary<string, object>);

            if (raw.TryGetValue("rectTransform", out object rtObj))
                e.rectTransform = DeserializeRectTransform(rtObj as Dictionary<string, object>);

            if (raw.TryGetValue("layoutGroup", out object lgObj))
                e.layoutGroup = DeserializeLayoutGroup(lgObj as Dictionary<string, object>);

            if (raw.TryGetValue("layoutElement", out object leObj))
                e.layoutElement = DeserializeLayoutElement(leObj as Dictionary<string, object>);

            if (raw.TryGetValue("contentSizeFitter", out object csfObj))
                e.contentSizeFitter = DeserializeContentSizeFitter(csfObj as Dictionary<string, object>);

            if (raw.TryGetValue("style", out object stObj))
                e.style = DeserializeStyle(stObj as Dictionary<string, object>);

            if (raw.TryGetValue("text", out object txObj))
                e.text = DeserializeText(txObj as Dictionary<string, object>);

            if (raw.TryGetValue("image", out object imgObj))
                e.image = DeserializeImage(imgObj as Dictionary<string, object>);

            if (raw.TryGetValue("button", out object btnObj))
                e.button = DeserializeButton(btnObj as Dictionary<string, object>);

            if (raw.TryGetValue("scrollView", out object svObj))
                e.scrollView = DeserializeScrollView(svObj as Dictionary<string, object>);

            if (raw.TryGetValue("inputField", out object ifObj))
                e.inputField = DeserializeInputField(ifObj as Dictionary<string, object>);

            if (raw.TryGetValue("slider", out object slObj))
                e.slider = DeserializeSlider(slObj as Dictionary<string, object>);

            if (raw.TryGetValue("toggle", out object tgObj))
                e.toggle = DeserializeToggle(tgObj as Dictionary<string, object>);

            if (raw.TryGetValue("table", out object tableObj))
                e.table = DeserializeTableConfig(tableObj as Dictionary<string, object>);

            if (raw.TryGetValue("tabGroup", out object tabGrpObj))
                e.tabGroup = DeserializeTabGroupConfig(tabGrpObj as Dictionary<string, object>);

            if (raw.TryGetValue("spriteSlot", out object ssObj))
                e.spriteSlot = DeserializeSpriteSlot(ssObj as Dictionary<string, object>);

            if (raw.TryGetValue("children", out object chObj) && chObj is List<object> childList)
            {
                e.children = new List<UIElementData>(childList.Count);
                foreach (var childRaw in childList)
                {
                    var child = DeserializeElement(childRaw as Dictionary<string, object>);
                    if (child != null)
                        e.children.Add(child);
                }
            }

            return e;
        }

        // ───── Sub-object deserializers ─────

        static CanvasGroupData DeserializeCanvasGroup(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var cg = new CanvasGroupData();
            cg.alpha = GetFloat(raw, "alpha", 1f);
            cg.interactable = GetBool(raw, "interactable", true);
            cg.blocksRaycasts = GetBool(raw, "blocksRaycasts", true);
            cg.ignoreParentGroups = GetBool(raw, "ignoreParentGroups", false);
            return cg;
        }

        static RectTransformData DeserializeRectTransform(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var rt = new RectTransformData();
            if (raw.TryGetValue("anchorMin", out object v)) rt.anchorMin = DeserializeVector2(v as Dictionary<string, object>, 0f, 0f);
            if (raw.TryGetValue("anchorMax", out object v2)) rt.anchorMax = DeserializeVector2(v2 as Dictionary<string, object>, 1f, 1f);
            if (raw.TryGetValue("pivot", out object v3)) rt.pivot = DeserializeVector2(v3 as Dictionary<string, object>, 0.5f, 0.5f);
            if (raw.TryGetValue("anchoredPosition", out object v4)) rt.anchoredPosition = DeserializeVector2(v4 as Dictionary<string, object>, 0f, 0f);
            if (raw.TryGetValue("sizeDelta", out object v5)) rt.sizeDelta = DeserializeVector2(v5 as Dictionary<string, object>, 0f, 0f);
            return rt;
        }

        static LayoutGroupData DeserializeLayoutGroup(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var lg = new LayoutGroupData();
            lg.direction = GetString(raw, "direction", "vertical");
            lg.spacing = GetFloat(raw, "spacing", 0f);
            lg.childAlignment = GetString(raw, "childAlignment", "UpperLeft");
            lg.childForceExpandWidth = GetBool(raw, "childForceExpandWidth", false);
            lg.childForceExpandHeight = GetBool(raw, "childForceExpandHeight", false);
            lg.controlChildWidth = GetBool(raw, "controlChildWidth", true);
            lg.controlChildHeight = GetBool(raw, "controlChildHeight", true);
            if (raw.TryGetValue("padding", out object padObj))
                lg.padding = DeserializePadding(padObj as Dictionary<string, object>);
            return lg;
        }

        static PaddingData DeserializePadding(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var p = new PaddingData();
            p.top = GetInt(raw, "top", 0);
            p.right = GetInt(raw, "right", 0);
            p.bottom = GetInt(raw, "bottom", 0);
            p.left = GetInt(raw, "left", 0);
            return p;
        }

        static LayoutElementData DeserializeLayoutElement(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var le = new LayoutElementData();
            le.minWidth = GetFloat(raw, "minWidth", -1f);
            le.minHeight = GetFloat(raw, "minHeight", -1f);
            le.preferredWidth = GetFloat(raw, "preferredWidth", -1f);
            le.preferredHeight = GetFloat(raw, "preferredHeight", -1f);
            le.flexibleWidth = GetFloat(raw, "flexibleWidth", -1f);
            le.flexibleHeight = GetFloat(raw, "flexibleHeight", -1f);
            le.ignoreLayout = GetBool(raw, "ignoreLayout", false);
            return le;
        }

        static ContentSizeFitterData DeserializeContentSizeFitter(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var csf = new ContentSizeFitterData();
            csf.horizontalFit = GetString(raw, "horizontalFit", "Unconstrained");
            csf.verticalFit = GetString(raw, "verticalFit", "Unconstrained");
            return csf;
        }

        static StyleData DeserializeStyle(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var s = new StyleData();
            s.backgroundColor = GetString(raw, "backgroundColor", null);
            s.borderRadius = GetFloat(raw, "borderRadius", 0f);
            s.opacity = GetFloat(raw, "opacity", 1f);
            return s;
        }

        static TextData DeserializeText(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var t = new TextData();
            t.content = GetString(raw, "content", "");
            t.richText = GetBool(raw, "richText", false);
            t.fontAsset = GetString(raw, "fontAsset", null);
            t.fontSize = GetFloat(raw, "fontSize", 24f);
            t.fontStyle = GetString(raw, "fontStyle", "Normal");
            t.color = GetString(raw, "color", "#FFFFFFFF");
            t.alignment = GetString(raw, "alignment", "MiddleCenter");
            t.overflow = GetString(raw, "overflow", "Wrap");
            return t;
        }

        static ImageData DeserializeImage(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var img = new ImageData();
            img.spriteName = GetString(raw, "spriteName", null);
            img.color = GetString(raw, "color", "#FFFFFFFF");
            img.imageType = GetString(raw, "imageType", "Simple");
            img.preserveAspect = GetBool(raw, "preserveAspect", false);
            return img;
        }

        static ButtonData DeserializeButton(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var btn = new ButtonData();
            btn.interactable = GetBool(raw, "interactable", true);
            btn.transition = GetString(raw, "transition", "ColorTint");
            btn.onClick = GetString(raw, "onClick", null);
            if (raw.TryGetValue("colors", out object cObj))
                btn.colors = DeserializeColorBlock(cObj as Dictionary<string, object>);
            return btn;
        }

        static ColorBlockData DeserializeColorBlock(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var cb = new ColorBlockData();
            cb.normal = GetString(raw, "normal", "#FFFFFFFF");
            cb.highlighted = GetString(raw, "highlighted", "#F5F5F5FF");
            cb.pressed = GetString(raw, "pressed", "#C8C8C8FF");
            cb.disabled = GetString(raw, "disabled", "#C8C8C880");
            return cb;
        }

        static ScrollViewData DeserializeScrollView(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var sv = new ScrollViewData();
            sv.horizontal = GetBool(raw, "horizontal", false);
            sv.vertical = GetBool(raw, "vertical", true);
            sv.movementType = GetString(raw, "movementType", "Elastic");
            sv.scrollbarVisibility = GetString(raw, "scrollbarVisibility", "AutoHideAndExpandViewport");
            if (raw.TryGetValue("contentLayoutGroup", out object clgObj))
                sv.contentLayoutGroup = DeserializeLayoutGroup(clgObj as Dictionary<string, object>);
            return sv;
        }

        static InputFieldData DeserializeInputField(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var inf = new InputFieldData();
            inf.placeholder = GetString(raw, "placeholder", "Enter text...");
            inf.characterLimit = GetInt(raw, "characterLimit", 0);
            inf.contentType = GetString(raw, "contentType", "Standard");
            inf.lineType = GetString(raw, "lineType", "SingleLine");
            inf.textColor = GetString(raw, "textColor", "#FFFFFFFF");
            inf.placeholderColor = GetString(raw, "placeholderColor", "#FFFFFF80");
            inf.onValueChanged = GetString(raw, "onValueChanged", null);
            inf.onEndEdit = GetString(raw, "onEndEdit", null);
            return inf;
        }

        static SliderData DeserializeSlider(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var sl = new SliderData();
            sl.minValue = GetFloat(raw, "minValue", 0f);
            sl.maxValue = GetFloat(raw, "maxValue", 1f);
            sl.value = GetFloat(raw, "value", 0.5f);
            sl.wholeNumbers = GetBool(raw, "wholeNumbers", false);
            sl.direction = GetString(raw, "direction", "LeftToRight");
            sl.fillColor = GetString(raw, "fillColor", null);
            sl.handleColor = GetString(raw, "handleColor", null);
            sl.backgroundColor = GetString(raw, "backgroundColor", null);
            sl.onValueChanged = GetString(raw, "onValueChanged", null);
            return sl;
        }

        static ToggleData DeserializeToggle(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var tg = new ToggleData();
            tg.isOn = GetBool(raw, "isOn", false);
            tg.toggleGroup = GetString(raw, "toggleGroup", null);
            tg.checkmarkColor = GetString(raw, "checkmarkColor", null);
            tg.backgroundColor = GetString(raw, "backgroundColor", null);
            tg.onValueChanged = GetString(raw, "onValueChanged", null);
            return tg;
        }

        // ───── V1.2: SpriteSlot ─────

        static SpriteSlotData DeserializeSpriteSlot(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var ss = new SpriteSlotData();
            ss.category = GetString(raw, "category", "icon");
            ss.hint = GetString(raw, "hint", null);
            ss.suggestedSize = GetString(raw, "suggestedSize", null);
            return ss;
        }

        // ───── V1.2: Table ─────

        static TableConfigData DeserializeTableConfig(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var tc = new TableConfigData();
            tc.rowHeight = GetFloat(raw, "rowHeight", 28f);
            tc.scrollable = GetBool(raw, "scrollable", true);

            if (raw.TryGetValue("columns", out object colsObj) && colsObj is List<object> colsList)
            {
                tc.columns = new List<TableColumnData>(colsList.Count);
                foreach (var colRaw in colsList)
                {
                    var colDict = colRaw as Dictionary<string, object>;
                    if (colDict != null)
                    {
                        var col = new TableColumnData();
                        col.header = GetString(colDict, "header", "");
                        col.width = GetFloat(colDict, "width", 100f);
                        tc.columns.Add(col);
                    }
                }
            }

            if (raw.TryGetValue("headerStyle", out object hsObj))
                tc.headerStyle = DeserializeTableTextStyle(hsObj as Dictionary<string, object>);

            if (raw.TryGetValue("rowStyle", out object rsObj))
                tc.rowStyle = DeserializeTableRowStyle(rsObj as Dictionary<string, object>);

            return tc;
        }

        static TableTextStyleData DeserializeTableTextStyle(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var s = new TableTextStyleData();
            s.backgroundColor = GetString(raw, "backgroundColor", null);
            s.textColor = GetString(raw, "textColor", null);
            s.fontSize = GetFloat(raw, "fontSize", 12f);
            s.fontStyle = GetString(raw, "fontStyle", "Bold");
            s.height = GetFloat(raw, "height", 30f);
            return s;
        }

        static TableRowStyleData DeserializeTableRowStyle(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var s = new TableRowStyleData();
            s.backgroundColor = GetString(raw, "backgroundColor", null);
            s.altBackgroundColor = GetString(raw, "altBackgroundColor", null);
            s.textColor = GetString(raw, "textColor", null);
            s.fontSize = GetFloat(raw, "fontSize", 12f);
            s.fontStyle = GetString(raw, "fontStyle", "Normal");
            return s;
        }

        // ───── V1.2: TabGroup ─────

        static TabGroupConfigData DeserializeTabGroupConfig(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var tg = new TabGroupConfigData();
            tg.tabPosition = GetString(raw, "tabPosition", "top");
            tg.tabHeight = GetFloat(raw, "tabHeight", 30f);
            tg.tabWidth = GetFloat(raw, "tabWidth", 0f);
            tg.activeTab = GetInt(raw, "activeTab", 0);

            if (raw.TryGetValue("tabs", out object tabsObj) && tabsObj is List<object> tabsList)
            {
                tg.tabs = new List<TabDefinitionData>(tabsList.Count);
                foreach (var tabRaw in tabsList)
                {
                    var tabDict = tabRaw as Dictionary<string, object>;
                    if (tabDict != null)
                    {
                        var tab = new TabDefinitionData();
                        tab.label = GetString(tabDict, "label", "Tab");
                        tab.iconSlot = GetString(tabDict, "iconSlot", null);
                        tg.tabs.Add(tab);
                    }
                }
            }

            if (raw.TryGetValue("tabStyle", out object tsObj))
                tg.tabStyle = DeserializeTabStyle(tsObj as Dictionary<string, object>);

            return tg;
        }

        static TabStyleData DeserializeTabStyle(Dictionary<string, object> raw)
        {
            if (raw == null) return null;
            var ts = new TabStyleData();
            ts.activeBackgroundColor = GetString(raw, "activeBackgroundColor", null);
            ts.inactiveBackgroundColor = GetString(raw, "inactiveBackgroundColor", null);
            ts.activeTextColor = GetString(raw, "activeTextColor", null);
            ts.inactiveTextColor = GetString(raw, "inactiveTextColor", null);
            ts.fontSize = GetFloat(raw, "fontSize", 12f);
            ts.fontStyle = GetString(raw, "fontStyle", "Bold");
            return ts;
        }

        static Vector2Data DeserializeVector2(Dictionary<string, object> raw, float defX, float defY)
        {
            if (raw == null) return new Vector2Data(defX, defY);
            return new Vector2Data(GetFloat(raw, "x", defX), GetFloat(raw, "y", defY));
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS — safe dictionary accessors
        // ═══════════════════════════════════════════════════════════════

        static string GetString(Dictionary<string, object> dict, string key, string defaultValue)
        {
            if (dict.TryGetValue(key, out object val) && val is string s) return s;
            return defaultValue;
        }

        static float GetFloat(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (!dict.TryGetValue(key, out object val)) return defaultValue;
            if (val is double d) return (float)d;
            if (val is long l) return l;
            if (val is float f) return f;
            return defaultValue;
        }

        static int GetInt(Dictionary<string, object> dict, string key, int defaultValue)
        {
            if (!dict.TryGetValue(key, out object val)) return defaultValue;
            if (val is long l) return (int)l;
            if (val is double d) return (int)d;
            if (val is int i) return i;
            return defaultValue;
        }

        static bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue)
        {
            if (dict.TryGetValue(key, out object val) && val is bool b) return b;
            return defaultValue;
        }
    }
}
