using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeadWalls
{
    /// <summary>
    /// Creates Unity GameObjects with uGUI components for each element type.
    /// Each Build* method returns the created GameObject.
    /// </summary>
    public static class UIImporterElementFactory
    {
        // ═══════════════════════════════════════════════════════════════
        // MAIN DISPATCHER
        // ═══════════════════════════════════════════════════════════════

        public static GameObject CreateElement(UIElementData data, UIImporterAssetResolver resolver,
            Dictionary<string, ToggleGroup> toggleGroups, Transform canvasRoot)
        {
            GameObject go;
            switch (data.type)
            {
                case "Panel":      go = BuildPanel(data); break;
                case "Text":       go = BuildText(data, resolver); break;
                case "Image":      go = BuildImage(data, resolver); break;
                case "Button":     go = BuildButton(data, resolver); break;
                case "ScrollView": go = BuildScrollView(data); break;
                case "InputField": go = BuildInputField(data, resolver); break;
                case "Slider":     go = BuildSlider(data); break;
                case "Toggle":     go = BuildToggle(data, toggleGroups, canvasRoot); break;
                case "Table":      go = BuildTable(data, resolver); break;
                case "TabGroup":   go = BuildTabGroup(data, resolver); break;
                default:
                    Debug.LogWarning($"[UIImporter] Unknown element type '{data.type}' — creating empty Panel.");
                    go = BuildPanel(data);
                    break;
            }

            go.name = data.name;
            go.SetActive(data.active);

            ApplyRectTransform(go, data.rectTransform);
            ApplyCanvasGroup(go, data);

            // ScrollView/Table: layoutGroup goes to Content, not root
            // Root layoutGroup would override Viewport anchors
            if (data.type == "ScrollView" || data.type == "Table")
            {
                if (data.layoutGroup != null)
                {
                    var content = go.transform.Find("Viewport/Content");
                    if (content == null) content = go.transform.Find("Content");
                    if (content != null)
                        ApplyLayoutGroup(content.gameObject, data.layoutGroup);
                    else
                        Debug.LogWarning($"[UIImporter] {data.name}: layoutGroup ignored — Content not found in ScrollView.");
                }
            }
            else
            {
                ApplyLayoutGroup(go, data.layoutGroup);
            }

            ApplyLayoutElement(go, data.layoutElement);
            ApplyContentSizeFitter(go, data.contentSizeFitter);
            ApplyRaycastTarget(go, data.raycastTarget);

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildPanel(UIElementData data)
        {
            var go = new GameObject(data.name, typeof(RectTransform));
            var image = go.AddComponent<Image>();

            Color bgColor = new Color(1f, 1f, 1f, 0f); // transparent default
            if (data.style != null && !string.IsNullOrEmpty(data.style.backgroundColor))
                bgColor = UIImporterAssetResolver.ParseColor(data.style.backgroundColor);

            image.color = bgColor;
            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // TEXT
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildText(UIElementData data, UIImporterAssetResolver resolver)
        {
            var go = new GameObject(data.name, typeof(RectTransform));
            var tmp = go.AddComponent<TextMeshProUGUI>();

            var t = data.text ?? new TextData();

            tmp.text = t.content;
            tmp.richText = t.richText;
            tmp.fontSize = t.fontSize;
            tmp.color = UIImporterAssetResolver.ParseColor(t.color);
            tmp.font = resolver.ResolveFont(t.fontAsset);
            tmp.fontStyle = ParseFontStyle(t.fontStyle);
            tmp.alignment = ParseTextAlignment(t.alignment);
            tmp.overflowMode = ParseOverflowMode(t.overflow);
            tmp.enableWordWrapping = t.overflow == "Wrap";

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // IMAGE
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildImage(UIElementData data, UIImporterAssetResolver resolver)
        {
            var go = new GameObject(data.name, typeof(RectTransform));
            var image = go.AddComponent<Image>();

            var img = data.image ?? new ImageData();

            image.sprite = resolver.ResolveSprite(img.spriteName);
            image.color = UIImporterAssetResolver.ParseColor(img.color);
            image.type = ParseImageType(img.imageType);
            image.preserveAspect = img.preserveAspect;

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildButton(UIElementData data, UIImporterAssetResolver resolver)
        {
            var go = new GameObject(data.name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();

            var btn = data.button ?? new ButtonData();

            // Background color from style
            if (data.style != null && !string.IsNullOrEmpty(data.style.backgroundColor))
                image.color = UIImporterAssetResolver.ParseColor(data.style.backgroundColor);

            button.interactable = btn.interactable;
            button.transition = ParseTransition(btn.transition);

            if (btn.colors != null)
            {
                var cb = button.colors;
                cb.normalColor = UIImporterAssetResolver.ParseColor(btn.colors.normal);
                cb.highlightedColor = UIImporterAssetResolver.ParseColor(btn.colors.highlighted);
                cb.pressedColor = UIImporterAssetResolver.ParseColor(btn.colors.pressed);
                cb.disabledColor = UIImporterAssetResolver.ParseColor(btn.colors.disabled);
                button.colors = cb;
            }

            // Create label text as child (if text data provided)
            if (data.text != null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);

                var labelRT = labelGo.GetComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.sizeDelta = Vector2.zero;

                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = data.text.content;
                tmp.fontSize = data.text.fontSize;
                tmp.color = UIImporterAssetResolver.ParseColor(data.text.color);
                tmp.font = resolver.ResolveFont(data.text.fontAsset);
                tmp.fontStyle = ParseFontStyle(data.text.fontStyle);
                tmp.alignment = ParseTextAlignment(data.text.alignment);
                tmp.raycastTarget = false;
            }

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // SCROLL VIEW
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildScrollView(UIElementData data)
        {
            var sv = data.scrollView ?? new ScrollViewData();

            // Root
            var go = new GameObject(data.name, typeof(RectTransform));
            var scrollRect = go.AddComponent<ScrollRect>();
            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.01f);

            if (data.style != null && !string.IsNullOrEmpty(data.style.backgroundColor))
                bgImage.color = UIImporterAssetResolver.ParseColor(data.style.backgroundColor);

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var vpImage = viewport.AddComponent<Image>();
            vpImage.color = Color.white;
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            vpRT.pivot = new Vector2(0f, 1f);

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);

            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 300f);

            if (sv.contentLayoutGroup != null)
                ApplyLayoutGroup(content, sv.contentLayoutGroup);

            var csf = content.AddComponent<ContentSizeFitter>();
            if (sv.vertical)
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (sv.horizontal)
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect setup
            scrollRect.content = contentRT;
            scrollRect.viewport = vpRT;
            scrollRect.horizontal = sv.horizontal;
            scrollRect.vertical = sv.vertical;
            scrollRect.movementType = ParseMovementType(sv.movementType);

            // Vertical scrollbar
            if (sv.vertical)
            {
                var vScrollbar = CreateScrollbar("Scrollbar Vertical", true);
                vScrollbar.transform.SetParent(go.transform, false);
                scrollRect.verticalScrollbar = vScrollbar.GetComponent<Scrollbar>();
                scrollRect.verticalScrollbarVisibility = ParseScrollbarVisibility(sv.scrollbarVisibility);
            }

            // Horizontal scrollbar
            if (sv.horizontal)
            {
                var hScrollbar = CreateScrollbar("Scrollbar Horizontal", false);
                hScrollbar.transform.SetParent(go.transform, false);
                scrollRect.horizontalScrollbar = hScrollbar.GetComponent<Scrollbar>();
                scrollRect.horizontalScrollbarVisibility = ParseScrollbarVisibility(sv.scrollbarVisibility);
            }

            return go;
        }

        static GameObject CreateScrollbar(string name, bool vertical)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            var scrollbar = go.AddComponent<Scrollbar>();

            // Sliding area
            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(go.transform, false);
            var saRT = slidingArea.GetComponent<RectTransform>();
            saRT.anchorMin = Vector2.zero;
            saRT.anchorMax = Vector2.one;
            saRT.sizeDelta = new Vector2(-20f, -20f);

            // Handle
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(slidingArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20f, 20f);

            scrollbar.handleRect = handleRT;
            scrollbar.targetGraphic = handleImage;

            var goRT = go.GetComponent<RectTransform>();
            if (vertical)
            {
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                goRT.anchorMin = new Vector2(1f, 0f);
                goRT.anchorMax = Vector2.one;
                goRT.pivot = Vector2.one;
                goRT.sizeDelta = new Vector2(20f, 0f);
            }
            else
            {
                scrollbar.direction = Scrollbar.Direction.LeftToRight;
                goRT.anchorMin = Vector2.zero;
                goRT.anchorMax = new Vector2(1f, 0f);
                goRT.pivot = Vector2.zero;
                goRT.sizeDelta = new Vector2(0f, 20f);
            }

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT FIELD
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildInputField(UIElementData data, UIImporterAssetResolver resolver)
        {
            var inf = data.inputField ?? new InputFieldData();

            var go = new GameObject(data.name, typeof(RectTransform));
            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            if (data.style != null && !string.IsNullOrEmpty(data.style.backgroundColor))
                bgImage.color = UIImporterAssetResolver.ParseColor(data.style.backgroundColor);

            // Text area
            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            textArea.AddComponent<RectMask2D>();
            var taRT = textArea.GetComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero;
            taRT.anchorMax = Vector2.one;
            taRT.sizeDelta = new Vector2(-16f, -8f);

            // Placeholder
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGo.transform.SetParent(textArea.transform, false);
            var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholder.text = inf.placeholder;
            placeholder.color = UIImporterAssetResolver.ParseColor(inf.placeholderColor);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.font = resolver.ResolveFont(data.text?.fontAsset);
            placeholder.fontSize = data.text?.fontSize ?? 24f;
            placeholder.enableWordWrapping = true;
            placeholder.raycastTarget = false;

            var phRT = placeholderGo.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.sizeDelta = Vector2.zero;

            // Text
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.color = UIImporterAssetResolver.ParseColor(inf.textColor);
            text.font = resolver.ResolveFont(data.text?.fontAsset);
            text.fontSize = data.text?.fontSize ?? 24f;
            text.enableWordWrapping = true;
            text.raycastTarget = false;

            var txtRT = textGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;

            // TMP_InputField
            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textViewport = taRT;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.characterLimit = inf.characterLimit;
            inputField.contentType = ParseInputContentType(inf.contentType);
            inputField.lineType = ParseInputLineType(inf.lineType);
            inputField.fontAsset = resolver.ResolveFont(data.text?.fontAsset);

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // SLIDER
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildSlider(UIElementData data)
        {
            var sl = data.slider ?? new SliderData();

            var go = new GameObject(data.name, typeof(RectTransform));

            // Background
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = sl.backgroundColor != null
                ? UIImporterAssetResolver.ParseColor(sl.backgroundColor)
                : new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.sizeDelta = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.25f);
            faRT.anchorMax = new Vector2(1f, 0.75f);
            faRT.sizeDelta = new Vector2(-20f, 0f);
            faRT.anchoredPosition = new Vector2(-5f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = sl.fillColor != null
                ? UIImporterAssetResolver.ParseColor(sl.fillColor)
                : new Color(0.3f, 0.6f, 1f, 1f);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.sizeDelta = new Vector2(10f, 0f);

            // Handle slide area
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero;
            haRT.anchorMax = Vector2.one;
            haRT.sizeDelta = new Vector2(-20f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = sl.handleColor != null
                ? UIImporterAssetResolver.ParseColor(sl.handleColor)
                : Color.white;
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20f, 0f);

            // Slider component
            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImage;
            slider.minValue = sl.minValue;
            slider.maxValue = sl.maxValue;
            slider.value = sl.value;
            slider.wholeNumbers = sl.wholeNumbers;
            slider.direction = ParseSliderDirection(sl.direction);

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // TOGGLE
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildToggle(UIElementData data, Dictionary<string, ToggleGroup> toggleGroups,
            Transform canvasRoot)
        {
            var tg = data.toggle ?? new ToggleData();

            var go = new GameObject(data.name, typeof(RectTransform));

            // Background
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = tg.backgroundColor != null
                ? UIImporterAssetResolver.ParseColor(tg.backgroundColor)
                : new Color(0.25f, 0.25f, 0.25f, 1f);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0f);
            bgRT.anchorMax = new Vector2(0f, 1f);
            bgRT.sizeDelta = new Vector2(30f, 0f);
            bgRT.anchoredPosition = new Vector2(15f, 0f);

            // Checkmark
            var checkmark = new GameObject("Checkmark", typeof(RectTransform));
            checkmark.transform.SetParent(bg.transform, false);
            var cmImage = checkmark.AddComponent<Image>();
            cmImage.color = tg.checkmarkColor != null
                ? UIImporterAssetResolver.ParseColor(tg.checkmarkColor)
                : Color.white;
            var cmRT = checkmark.GetComponent<RectTransform>();
            cmRT.anchorMin = new Vector2(0.1f, 0.1f);
            cmRT.anchorMax = new Vector2(0.9f, 0.9f);
            cmRT.sizeDelta = Vector2.zero;

            // Toggle component
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = tg.isOn;
            toggle.graphic = cmImage;
            toggle.targetGraphic = bgImage;

            // Toggle group
            if (!string.IsNullOrEmpty(tg.toggleGroup) && toggleGroups != null)
            {
                if (!toggleGroups.TryGetValue(tg.toggleGroup, out var group))
                {
                    var groupGo = new GameObject($"ToggleGroup_{tg.toggleGroup}");
                    if (canvasRoot != null)
                        groupGo.transform.SetParent(canvasRoot, false);
                    group = groupGo.AddComponent<ToggleGroup>();
                    group.allowSwitchOff = false;
                    toggleGroups[tg.toggleGroup] = group;
                }
                toggle.group = group;
            }

            // Label (if text data exists)
            if (data.text != null)
            {
                var label = new GameObject("Label", typeof(RectTransform));
                label.transform.SetParent(go.transform, false);
                var tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = data.text.content;
                tmp.fontSize = data.text.fontSize;
                tmp.color = UIImporterAssetResolver.ParseColor(data.text.color);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;

                var lRT = label.GetComponent<RectTransform>();
                lRT.anchorMin = new Vector2(0f, 0f);
                lRT.anchorMax = new Vector2(1f, 1f);
                lRT.sizeDelta = new Vector2(-40f, 0f);
                lRT.anchoredPosition = new Vector2(20f, 0f);
            }

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // TABLE (V1.2)
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildTable(UIElementData data, UIImporterAssetResolver resolver)
        {
            var tc = data.table ?? new TableConfigData();
            var columns = tc.columns ?? new List<TableColumnData>();

            var go = new GameObject(data.name, typeof(RectTransform));

            // Container — optional ScrollView wrapper
            GameObject contentParent;
            if (tc.scrollable)
            {
                var scrollRect = go.AddComponent<ScrollRect>();
                var bgImage = go.AddComponent<Image>();
                bgImage.color = new Color(1f, 1f, 1f, 0.01f);

                if (data.style != null && !string.IsNullOrEmpty(data.style.backgroundColor))
                    bgImage.color = UIImporterAssetResolver.ParseColor(data.style.backgroundColor);

                var viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(go.transform, false);
                viewport.AddComponent<Image>().color = Color.white;
                viewport.AddComponent<Mask>().showMaskGraphic = false;
                var vpRT = viewport.GetComponent<RectTransform>();
                vpRT.anchorMin = Vector2.zero;
                vpRT.anchorMax = Vector2.one;
                vpRT.sizeDelta = Vector2.zero;

                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                var contentRT = content.GetComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0f, 1f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.pivot = new Vector2(0.5f, 1f);
                contentRT.sizeDelta = new Vector2(0f, 300f);

                var vlg = content.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandWidth = true;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;

                var csf = content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.content = contentRT;
                scrollRect.viewport = vpRT;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;

                contentParent = content;
            }
            else
            {
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandWidth = true;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                contentParent = go;
            }

            // Header row
            if (columns.Count > 0)
            {
                var headerRow = CreateTableRow("Header", columns, tc.headerStyle?.height ?? 30f,
                    tc.headerStyle?.backgroundColor ?? "#000080FF",
                    tc.headerStyle?.textColor ?? "#FFFFFFFF",
                    tc.headerStyle?.fontSize ?? 12f,
                    tc.headerStyle?.fontStyle ?? "Bold",
                    resolver, true);
                headerRow.transform.SetParent(contentParent.transform, false);
            }

            return go;
        }

        static GameObject CreateTableRow(string rowName, List<TableColumnData> columns,
            float rowHeight, string bgColor, string textColor, float fontSize, string fontStyle,
            UIImporterAssetResolver resolver, bool isHeader)
        {
            var row = new GameObject(rowName, typeof(RectTransform));
            var rowImage = row.AddComponent<Image>();
            rowImage.color = UIImporterAssetResolver.ParseColor(bgColor);
            rowImage.raycastTarget = false;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.spacing = 1f;
            hlg.padding = new RectOffset(2, 2, 0, 0);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;

            foreach (var col in columns)
            {
                var cell = new GameObject(col.header, typeof(RectTransform));
                cell.transform.SetParent(row.transform, false);

                var cellLE = cell.AddComponent<LayoutElement>();
                cellLE.preferredWidth = col.width;

                var tmp = cell.AddComponent<TextMeshProUGUI>();
                tmp.text = isHeader ? col.header : "";
                tmp.fontSize = fontSize;
                tmp.fontStyle = ParseFontStyle(fontStyle);
                tmp.color = UIImporterAssetResolver.ParseColor(textColor);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.font = resolver.ResolveFont(null);
                tmp.raycastTarget = false;
                tmp.margin = new Vector4(4f, 0f, 4f, 0f);
            }

            return row;
        }

        // ═══════════════════════════════════════════════════════════════
        // TAB GROUP (V1.2)
        // ═══════════════════════════════════════════════════════════════

        static GameObject BuildTabGroup(UIElementData data, UIImporterAssetResolver resolver)
        {
            var tgc = data.tabGroup ?? new TabGroupConfigData();
            var tabs = tgc.tabs ?? new List<TabDefinitionData>();
            var style = tgc.tabStyle ?? new TabStyleData();

            var go = new GameObject(data.name, typeof(RectTransform));
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.spacing = 0f;

            // Tab bar
            var tabBar = new GameObject("TabBar", typeof(RectTransform));
            tabBar.transform.SetParent(go.transform, false);
            var tabBarImage = tabBar.AddComponent<Image>();
            tabBarImage.color = UIImporterAssetResolver.ParseColor(
                style.inactiveBackgroundColor ?? "#C0C0C0FF");
            tabBarImage.raycastTarget = false;

            var tabBarLE = tabBar.AddComponent<LayoutElement>();
            tabBarLE.preferredHeight = tgc.tabHeight;

            var tabBarHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabBarHLG.childForceExpandHeight = true;
            tabBarHLG.childControlWidth = tgc.tabWidth <= 0f;
            tabBarHLG.childControlHeight = true;
            tabBarHLG.childForceExpandWidth = tgc.tabWidth <= 0f;
            tabBarHLG.spacing = 1f;
            tabBarHLG.padding = new RectOffset(2, 2, 0, 0);

            // Toggle group
            var toggleGroup = tabBar.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false;

            // Pages container
            var pagesContainer = new GameObject("Pages", typeof(RectTransform));
            pagesContainer.transform.SetParent(go.transform, false);
            var pagesLE = pagesContainer.AddComponent<LayoutElement>();
            pagesLE.flexibleHeight = 1f;

            // Create tab buttons
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                bool isActive = i == tgc.activeTab;

                var tabBtn = new GameObject($"Tab_{tab.label}", typeof(RectTransform));
                tabBtn.transform.SetParent(tabBar.transform, false);

                var tabImage = tabBtn.AddComponent<Image>();
                tabImage.color = UIImporterAssetResolver.ParseColor(
                    isActive
                        ? (style.activeBackgroundColor ?? "#FFFFFFFF")
                        : (style.inactiveBackgroundColor ?? "#C0C0C0FF"));

                var toggle = tabBtn.AddComponent<Toggle>();
                toggle.isOn = isActive;
                toggle.group = toggleGroup;
                toggle.targetGraphic = tabImage;

                if (tgc.tabWidth > 0f)
                {
                    var tabLE = tabBtn.AddComponent<LayoutElement>();
                    tabLE.preferredWidth = tgc.tabWidth;
                }

                // Icon (spriteSlot reference)
                if (!string.IsNullOrEmpty(tab.iconSlot))
                {
                    var iconGo = new GameObject("Icon", typeof(RectTransform));
                    iconGo.transform.SetParent(tabBtn.transform, false);
                    var iconImage = iconGo.AddComponent<Image>();
                    iconImage.color = Color.white;
                    iconImage.raycastTarget = false;
                    var iconRT = iconGo.GetComponent<RectTransform>();
                    iconRT.anchorMin = new Vector2(0f, 0.15f);
                    iconRT.anchorMax = new Vector2(0f, 0.85f);
                    iconRT.sizeDelta = new Vector2(20f, 0f);
                    iconRT.anchoredPosition = new Vector2(16f, 0f);
                }

                // Label
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(tabBtn.transform, false);
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = tab.label;
                tmp.fontSize = style.fontSize;
                tmp.fontStyle = ParseFontStyle(style.fontStyle);
                tmp.color = UIImporterAssetResolver.ParseColor(
                    isActive
                        ? (style.activeTextColor ?? "#000000FF")
                        : (style.inactiveTextColor ?? "#333333FF"));
                tmp.alignment = TextAlignmentOptions.Midline;
                tmp.raycastTarget = false;
                var labelRT = labelGo.GetComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.sizeDelta = Vector2.zero;
            }

            return go;
        }

        // ═══════════════════════════════════════════════════════════════
        // COMMON APPLIERS
        // ═══════════════════════════════════════════════════════════════

        static void ApplyRectTransform(GameObject go, RectTransformData data)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            if (data != null)
            {
                rt.anchorMin = data.anchorMin.ToVector2();
                rt.anchorMax = data.anchorMax.ToVector2();
                rt.pivot = data.pivot.ToVector2();
                rt.anchoredPosition = data.anchoredPosition.ToVector2();
                rt.sizeDelta = data.sizeDelta.ToVector2();
            }
            else
            {
                // Default: stretch to fill parent
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            // Smart fallback: if sizeDelta is zero AND anchors are NOT stretch,
            // the element would be invisible. Apply type-based default size.
            if (NeedsDefaultSize(rt))
            {
                string type = go.name; // name is set before ApplyRectTransform
                rt.sizeDelta = GetDefaultSizeForType(go);
            }
        }

        static bool NeedsDefaultSize(RectTransform rt)
        {
            // If sizeDelta is already non-zero, user/json explicitly set it
            if (rt.sizeDelta.x > 0.01f || rt.sizeDelta.y > 0.01f)
                return false;

            // Check if anchors define a stretch region (min != max on that axis)
            bool stretchX = Mathf.Abs(rt.anchorMax.x - rt.anchorMin.x) > 0.01f;
            bool stretchY = Mathf.Abs(rt.anchorMax.y - rt.anchorMin.y) > 0.01f;

            // If both axes stretch, sizeDelta 0 is fine (fills parent)
            if (stretchX && stretchY) return false;

            // At least one axis is fixed (anchors equal) but size is 0 → needs default
            return true;
        }

        static Vector2 GetDefaultSizeForType(GameObject go)
        {
            // Determine type from components already added
            if (go.GetComponent<TMP_InputField>() != null) return new Vector2(250f, 40f);
            if (go.GetComponent<Slider>() != null) return new Vector2(200f, 20f);
            if (go.GetComponent<Toggle>() != null) return new Vector2(180f, 30f);
            if (go.GetComponent<ScrollRect>() != null) return new Vector2(300f, 200f);
            if (go.GetComponent<Button>() != null) return new Vector2(180f, 40f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                // Estimate size from font size and content length
                float height = tmp.fontSize * 1.5f;
                float width = Mathf.Max(100f, tmp.text.Length * tmp.fontSize * 0.6f);
                return new Vector2(width, height);
            }

            var img = go.GetComponent<Image>();
            if (img != null && img.sprite != null)
                return new Vector2(img.sprite.rect.width, img.sprite.rect.height);

            // Generic panel/container fallback
            return new Vector2(200f, 100f);
        }

        static void ApplyCanvasGroup(GameObject go, UIElementData data)
        {
            if (data.canvasGroup == null && (data.style == null || data.style.opacity >= 1f))
                return;

            var cg = go.AddComponent<CanvasGroup>();

            if (data.canvasGroup != null)
            {
                cg.alpha = data.canvasGroup.alpha;
                cg.interactable = data.canvasGroup.interactable;
                cg.blocksRaycasts = data.canvasGroup.blocksRaycasts;
                cg.ignoreParentGroups = data.canvasGroup.ignoreParentGroups;
            }

            if (data.style != null && data.style.opacity < 1f)
                cg.alpha = data.style.opacity;
        }

        public static void ApplyLayoutGroup(GameObject go, LayoutGroupData data)
        {
            if (data == null) return;

            HorizontalOrVerticalLayoutGroup lg;
            if (data.direction == "horizontal")
                lg = go.AddComponent<HorizontalLayoutGroup>();
            else
                lg = go.AddComponent<VerticalLayoutGroup>();

            lg.spacing = data.spacing;
            lg.childAlignment = ParseTextAnchor(data.childAlignment);
            lg.childForceExpandWidth = data.childForceExpandWidth;
            lg.childForceExpandHeight = data.childForceExpandHeight;
            lg.childControlWidth = data.controlChildWidth;
            lg.childControlHeight = data.controlChildHeight;

            if (data.padding != null)
            {
                lg.padding = new RectOffset(
                    data.padding.left,
                    data.padding.right,
                    data.padding.top,
                    data.padding.bottom
                );
            }
        }

        static void ApplyLayoutElement(GameObject go, LayoutElementData data)
        {
            if (data == null) return;

            var le = go.AddComponent<LayoutElement>();
            if (data.minWidth >= 0f) le.minWidth = data.minWidth;
            if (data.minHeight >= 0f) le.minHeight = data.minHeight;
            if (data.preferredWidth >= 0f) le.preferredWidth = data.preferredWidth;
            if (data.preferredHeight >= 0f) le.preferredHeight = data.preferredHeight;
            if (data.flexibleWidth >= 0f) le.flexibleWidth = data.flexibleWidth;
            if (data.flexibleHeight >= 0f) le.flexibleHeight = data.flexibleHeight;
            le.ignoreLayout = data.ignoreLayout;
        }

        static void ApplyContentSizeFitter(GameObject go, ContentSizeFitterData data)
        {
            if (data == null) return;

            var csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ParseFitMode(data.horizontalFit);
            csf.verticalFit = ParseFitMode(data.verticalFit);
        }

        static void ApplyRaycastTarget(GameObject go, bool raycastTarget)
        {
            foreach (var graphic in go.GetComponents<Graphic>())
                graphic.raycastTarget = raycastTarget;
        }

        // ═══════════════════════════════════════════════════════════════
        // ENUM PARSERS
        // ═══════════════════════════════════════════════════════════════

        static FontStyles ParseFontStyle(string style)
        {
            switch (style)
            {
                case "Bold": return FontStyles.Bold;
                case "Italic": return FontStyles.Italic;
                case "BoldItalic": return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        static TextAlignmentOptions ParseTextAlignment(string alignment)
        {
            switch (alignment)
            {
                case "TopLeft": return TextAlignmentOptions.TopLeft;
                case "TopCenter": return TextAlignmentOptions.Top;
                case "TopRight": return TextAlignmentOptions.TopRight;
                case "MiddleLeft": return TextAlignmentOptions.MidlineLeft;
                case "MiddleCenter": return TextAlignmentOptions.Midline;
                case "MiddleRight": return TextAlignmentOptions.MidlineRight;
                case "BottomLeft": return TextAlignmentOptions.BottomLeft;
                case "BottomCenter": return TextAlignmentOptions.Bottom;
                case "BottomRight": return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Midline;
            }
        }

        static TextOverflowModes ParseOverflowMode(string overflow)
        {
            switch (overflow)
            {
                case "Overflow": return TextOverflowModes.Overflow;
                case "Ellipsis": return TextOverflowModes.Ellipsis;
                case "Truncate": return TextOverflowModes.Truncate;
                default: return TextOverflowModes.Overflow; // "Wrap" handled by enableWordWrapping
            }
        }

        static Image.Type ParseImageType(string imageType)
        {
            switch (imageType)
            {
                case "Sliced": return Image.Type.Sliced;
                case "Tiled": return Image.Type.Tiled;
                case "Filled": return Image.Type.Filled;
                default: return Image.Type.Simple;
            }
        }

        static Selectable.Transition ParseTransition(string transition)
        {
            switch (transition)
            {
                case "SpriteSwap": return Selectable.Transition.SpriteSwap;
                case "Animation": return Selectable.Transition.Animation;
                default: return Selectable.Transition.ColorTint;
            }
        }

        static TextAnchor ParseTextAnchor(string anchor)
        {
            switch (anchor)
            {
                case "UpperLeft": return TextAnchor.UpperLeft;
                case "UpperCenter": return TextAnchor.UpperCenter;
                case "UpperRight": return TextAnchor.UpperRight;
                case "MiddleLeft": return TextAnchor.MiddleLeft;
                case "MiddleCenter": return TextAnchor.MiddleCenter;
                case "MiddleRight": return TextAnchor.MiddleRight;
                case "LowerLeft": return TextAnchor.LowerLeft;
                case "LowerCenter": return TextAnchor.LowerCenter;
                case "LowerRight": return TextAnchor.LowerRight;
                default: return TextAnchor.UpperLeft;
            }
        }

        static ScrollRect.MovementType ParseMovementType(string type)
        {
            switch (type)
            {
                case "Unrestricted": return ScrollRect.MovementType.Unrestricted;
                case "Clamped": return ScrollRect.MovementType.Clamped;
                default: return ScrollRect.MovementType.Elastic;
            }
        }

        static ScrollRect.ScrollbarVisibility ParseScrollbarVisibility(string vis)
        {
            switch (vis)
            {
                case "Permanent": return ScrollRect.ScrollbarVisibility.Permanent;
                case "AutoHide": return ScrollRect.ScrollbarVisibility.AutoHide;
                default: return ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            }
        }

        static ContentSizeFitter.FitMode ParseFitMode(string mode)
        {
            switch (mode)
            {
                case "MinSize": return ContentSizeFitter.FitMode.MinSize;
                case "PreferredSize": return ContentSizeFitter.FitMode.PreferredSize;
                default: return ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        static TMP_InputField.ContentType ParseInputContentType(string type)
        {
            switch (type)
            {
                case "Autocorrected": return TMP_InputField.ContentType.Autocorrected;
                case "IntegerNumber": return TMP_InputField.ContentType.IntegerNumber;
                case "DecimalNumber": return TMP_InputField.ContentType.DecimalNumber;
                case "Alphanumeric": return TMP_InputField.ContentType.Alphanumeric;
                case "Name": return TMP_InputField.ContentType.Name;
                case "EmailAddress": return TMP_InputField.ContentType.EmailAddress;
                case "Password": return TMP_InputField.ContentType.Password;
                case "Pin": return TMP_InputField.ContentType.Pin;
                default: return TMP_InputField.ContentType.Standard;
            }
        }

        static TMP_InputField.LineType ParseInputLineType(string type)
        {
            switch (type)
            {
                case "MultiLineSubmit": return TMP_InputField.LineType.MultiLineSubmit;
                case "MultiLineNewline": return TMP_InputField.LineType.MultiLineNewline;
                default: return TMP_InputField.LineType.SingleLine;
            }
        }

        static Slider.Direction ParseSliderDirection(string dir)
        {
            switch (dir)
            {
                case "RightToLeft": return Slider.Direction.RightToLeft;
                case "BottomToTop": return Slider.Direction.BottomToTop;
                case "TopToBottom": return Slider.Direction.TopToBottom;
                default: return Slider.Direction.LeftToRight;
            }
        }
    }
}
