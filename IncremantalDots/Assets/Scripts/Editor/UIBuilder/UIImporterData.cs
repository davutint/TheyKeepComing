using System.Collections.Generic;
using UnityEngine;

namespace DeadWalls
{
    // ═══════════════════════════════════════════════════════════════
    // ROOT
    // ═══════════════════════════════════════════════════════════════

    public class UIDocumentData
    {
        public string version;
        public CanvasData canvas;
        public UIElementData root;
    }

    // ═══════════════════════════════════════════════════════════════
    // CANVAS
    // ═══════════════════════════════════════════════════════════════

    public class CanvasData
    {
        public Vector2Data referenceResolution = new Vector2Data(1920f, 1080f);
        public string scaleMode = "ScaleWithScreenSize";
        public float matchWidthOrHeight = 0.5f;
    }

    // ═══════════════════════════════════════════════════════════════
    // UI ELEMENT (recursive)
    // ═══════════════════════════════════════════════════════════════

    public class UIElementData
    {
        public string type;
        public string name;
        public bool active = true;
        public bool raycastTarget = true;

        public CanvasGroupData canvasGroup;
        public RectTransformData rectTransform;
        public LayoutGroupData layoutGroup;
        public LayoutElementData layoutElement;
        public ContentSizeFitterData contentSizeFitter;
        public StyleData style;
        public TextData text;
        public ImageData image;
        public ButtonData button;
        public ScrollViewData scrollView;
        public InputFieldData inputField;
        public SliderData slider;
        public ToggleData toggle;
        public TableConfigData table;
        public TabGroupConfigData tabGroup;
        public SpriteSlotData spriteSlot;

        public List<UIElementData> children;

        public int CountAll()
        {
            int count = 1;
            if (children != null)
                foreach (var child in children)
                    count += child.CountAll();
            return count;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIMITIVES
    // ═══════════════════════════════════════════════════════════════

    public class Vector2Data
    {
        public float x;
        public float y;

        public Vector2Data() { }
        public Vector2Data(float x, float y) { this.x = x; this.y = y; }

        public Vector2 ToVector2() => new Vector2(x, y);
    }

    public class PaddingData
    {
        public int top;
        public int right;
        public int bottom;
        public int left;
    }

    // ═══════════════════════════════════════════════════════════════
    // RECT TRANSFORM
    // ═══════════════════════════════════════════════════════════════

    public class RectTransformData
    {
        public Vector2Data anchorMin = new Vector2Data(0f, 0f);
        public Vector2Data anchorMax = new Vector2Data(1f, 1f);
        public Vector2Data pivot = new Vector2Data(0.5f, 0.5f);
        public Vector2Data anchoredPosition = new Vector2Data(0f, 0f);
        public Vector2Data sizeDelta = new Vector2Data(0f, 0f);
    }

    // ═══════════════════════════════════════════════════════════════
    // CANVAS GROUP
    // ═══════════════════════════════════════════════════════════════

    public class CanvasGroupData
    {
        public float alpha = 1f;
        public bool interactable = true;
        public bool blocksRaycasts = true;
        public bool ignoreParentGroups;
    }

    // ═══════════════════════════════════════════════════════════════
    // LAYOUT
    // ═══════════════════════════════════════════════════════════════

    public class LayoutGroupData
    {
        public string direction = "vertical";
        public float spacing;
        public PaddingData padding;
        public string childAlignment = "UpperLeft";
        public bool childForceExpandWidth;
        public bool childForceExpandHeight;
        public bool controlChildWidth = true;
        public bool controlChildHeight = true;
    }

    public class LayoutElementData
    {
        public float minWidth = -1f;
        public float minHeight = -1f;
        public float preferredWidth = -1f;
        public float preferredHeight = -1f;
        public float flexibleWidth = -1f;
        public float flexibleHeight = -1f;
        public bool ignoreLayout;
    }

    public class ContentSizeFitterData
    {
        public string horizontalFit = "Unconstrained";
        public string verticalFit = "Unconstrained";
    }

    // ═══════════════════════════════════════════════════════════════
    // STYLE
    // ═══════════════════════════════════════════════════════════════

    public class StyleData
    {
        public string backgroundColor;
        public float borderRadius;
        public float opacity = 1f;
    }

    // ═══════════════════════════════════════════════════════════════
    // TEXT
    // ═══════════════════════════════════════════════════════════════

    public class TextData
    {
        public string content = "";
        public bool richText;
        public string fontAsset;
        public float fontSize = 24f;
        public string fontStyle = "Normal";
        public string color = "#FFFFFFFF";
        public string alignment = "MiddleCenter";
        public string overflow = "Wrap";
    }

    // ═══════════════════════════════════════════════════════════════
    // IMAGE
    // ═══════════════════════════════════════════════════════════════

    public class ImageData
    {
        public string spriteName;
        public string color = "#FFFFFFFF";
        public string imageType = "Simple";
        public bool preserveAspect;
    }

    // ═══════════════════════════════════════════════════════════════
    // BUTTON
    // ═══════════════════════════════════════════════════════════════

    public class ButtonData
    {
        public bool interactable = true;
        public string transition = "ColorTint";
        public ColorBlockData colors;
        public string onClick;
    }

    public class ColorBlockData
    {
        public string normal = "#FFFFFFFF";
        public string highlighted = "#F5F5F5FF";
        public string pressed = "#C8C8C8FF";
        public string disabled = "#C8C8C880";
    }

    // ═══════════════════════════════════════════════════════════════
    // SCROLL VIEW
    // ═══════════════════════════════════════════════════════════════

    public class ScrollViewData
    {
        public bool horizontal;
        public bool vertical = true;
        public string movementType = "Elastic";
        public string scrollbarVisibility = "AutoHideAndExpandViewport";
        public LayoutGroupData contentLayoutGroup;
    }

    // ═══════════════════════════════════════════════════════════════
    // INPUT FIELD
    // ═══════════════════════════════════════════════════════════════

    public class InputFieldData
    {
        public string placeholder = "Enter text...";
        public int characterLimit;
        public string contentType = "Standard";
        public string lineType = "SingleLine";
        public string textColor = "#FFFFFFFF";
        public string placeholderColor = "#FFFFFF80";
        public string onValueChanged;
        public string onEndEdit;
    }

    // ═══════════════════════════════════════════════════════════════
    // SLIDER
    // ═══════════════════════════════════════════════════════════════

    public class SliderData
    {
        public float minValue;
        public float maxValue = 1f;
        public float value = 0.5f;
        public bool wholeNumbers;
        public string direction = "LeftToRight";
        public string fillColor;
        public string handleColor;
        public string backgroundColor;
        public string onValueChanged;
    }

    // ═══════════════════════════════════════════════════════════════
    // TOGGLE
    // ═══════════════════════════════════════════════════════════════

    public class ToggleData
    {
        public bool isOn;
        public string toggleGroup;
        public string checkmarkColor;
        public string backgroundColor;
        public string onValueChanged;
    }

    // ═══════════════════════════════════════════════════════════════
    // SPRITE SLOT (V1.2)
    // ═══════════════════════════════════════════════════════════════

    public class SpriteSlotData
    {
        public string category = "icon"; // icon, background, border, button, decoration
        public string hint;
        public string suggestedSize;     // "32x32", "9-slice", "64x64" gibi
    }

    // ═══════════════════════════════════════════════════════════════
    // TABLE (V1.2)
    // ═══════════════════════════════════════════════════════════════

    public class TableConfigData
    {
        public List<TableColumnData> columns;
        public TableTextStyleData headerStyle;
        public TableRowStyleData rowStyle;
        public float rowHeight = 28f;
        public bool scrollable = true;
    }

    public class TableColumnData
    {
        public string header;
        public float width;
    }

    public class TableTextStyleData
    {
        public string backgroundColor;
        public string textColor;
        public float fontSize = 12f;
        public string fontStyle = "Bold";
        public float height = 30f;
    }

    public class TableRowStyleData
    {
        public string backgroundColor;
        public string altBackgroundColor;
        public string textColor;
        public float fontSize = 12f;
        public string fontStyle = "Normal";
    }

    // ═══════════════════════════════════════════════════════════════
    // TAB GROUP (V1.2)
    // ═══════════════════════════════════════════════════════════════

    public class TabGroupConfigData
    {
        public string tabPosition = "top";
        public float tabHeight = 30f;
        public float tabWidth;
        public int activeTab;
        public List<TabDefinitionData> tabs;
        public TabStyleData tabStyle;
    }

    public class TabDefinitionData
    {
        public string label;
        public string iconSlot;
    }

    public class TabStyleData
    {
        public string activeBackgroundColor;
        public string inactiveBackgroundColor;
        public string activeTextColor;
        public string inactiveTextColor;
        public float fontSize = 12f;
        public string fontStyle = "Bold";
    }
}
