using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Fullscreen dinamik Tech Tree paneli. Gorunur node'lar GameManager'in reveal state'inden
    /// (GetRevealedTechNodes) runtime'da TechNodeTemplate klonlanarak uretilir; baglanti cizgileri
    /// RevealChildNodeIds iliskisinden cizilir. Sabit kategori/tier/elle-yerlestirilmis agac YOKTUR;
    /// layout graf derinligi + gorunur yaprak sayisindan deterministik hesaplanir.
    /// Panel acikken oyun DURMAZ (drawer emsali; MobilePrepPauseState continuous siege'de olu,
    /// Time.timeScale=0 ise "oyun durmaz" ilkesiyle catisir — bkz. TECH_TREE_UI_ARCHITECTURE.md).
    /// </summary>
    public class TechTreeUI : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject TechTreePanel;
        public Button TechTreeOpenButton;
        public Button TechTreeCloseButton;
        public RectTransform TechTreeViewport;
        public RectTransform TechTreeContent;
        public RectTransform TechNodeTemplate;
        public RectTransform TechConnectionTemplate;

        [Header("Layout")]
        public float NodeSpacingX = 300f;
        public float NodeSpacingY = 132f;
        public Vector2 ContentPadding = new Vector2(90f, 90f);

        private const float RefreshInterval = 0.2f;

        private static readonly Color AvailableColor = new Color(0.349f, 0.765f, 0.416f, 1f);
        private static readonly Color BoughtColor = new Color(0.56f, 0.66f, 0.76f, 1f);
        private static readonly Color LockedColor = new Color(0.42f, 0.47f, 0.52f, 1f);
        private static readonly Color MaxColor = new Color(0.949f, 0.788f, 0.298f, 1f);
        private static readonly Color NeedColor = new Color(0.898f, 0.632f, 0.235f, 1f);
        private static readonly Color RowAvailableBg = new Color(0.137f, 0.165f, 0.196f, 0.95f);
        private static readonly Color RowBoughtBg = new Color(0.118f, 0.184f, 0.137f, 0.95f);
        private static readonly Color RowLockedBg = new Color(0.09f, 0.105f, 0.125f, 0.9f);
        private static readonly Color ConnectionColor = new Color(0.42f, 0.47f, 0.52f, 0.85f);

        private readonly List<TechNodeView> _nodeViews = new List<TechNodeView>();
        private readonly List<GameObject> _connectionViews = new List<GameObject>();
        private float _nextRefreshTime;
        private int _builtVisibleCount = -1;
        private bool _buttonsBound;

        private sealed class TechNodeView
        {
            public TechNodeDefinitionSO Definition;
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public TMP_Text TitleText;
            public TMP_Text LevelText;
            public TMP_Text CostText;
            public TMP_Text StatusText;
            public TMP_Text DescriptionText;
            public Image IconImage;
            public TMP_Text IconFallbackText;
            public Button BuyButton;
            public TMP_Text BuyButtonText;
            public UnityAction BuyAction;
        }

        private void OnEnable()
        {
            BindButtons();
            // Panel default kapali baslar; state sahibi bu controller'dir.
            if (TechTreePanel != null && TechTreePanel.activeSelf)
                TechTreePanel.SetActive(false);
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void Update()
        {
            if (TechTreePanel == null || !TechTreePanel.activeSelf)
                return;

            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        public void OpenPanel()
        {
            if (TechTreePanel == null)
                return;

            TechTreePanel.SetActive(true);
            RebuildGraph();
        }

        public void ClosePanel()
        {
            if (TechTreePanel != null)
                TechTreePanel.SetActive(false);
        }

        public void TogglePanel()
        {
            if (TechTreePanel == null)
                return;

            if (TechTreePanel.activeSelf)
                ClosePanel();
            else
                OpenPanel();
        }

        private void BindButtons()
        {
            if (_buttonsBound)
                return;

            _buttonsBound = true;
            if (TechTreeOpenButton != null)
            {
                TechTreeOpenButton.onClick.RemoveListener(OpenPanel);
                TechTreeOpenButton.onClick.AddListener(OpenPanel);
            }

            if (TechTreeCloseButton != null)
            {
                TechTreeCloseButton.onClick.RemoveListener(ClosePanel);
                TechTreeCloseButton.onClick.AddListener(ClosePanel);
            }
        }

        private void UnbindButtons()
        {
            _buttonsBound = false;
            if (TechTreeOpenButton != null)
                TechTreeOpenButton.onClick.RemoveListener(OpenPanel);
            if (TechTreeCloseButton != null)
                TechTreeCloseButton.onClick.RemoveListener(ClosePanel);
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            // Yeni reveal olduysa graf yeniden kurulur; degilse sadece durum metinleri tazelenir.
            int visibleCount = gm.GetRevealedTechNodes().Count;
            if (visibleCount != _builtVisibleCount)
            {
                RebuildGraph();
                return;
            }

            foreach (var view in _nodeViews)
                RefreshNodeView(view, gm);
        }

        /// <summary>
        /// Gorunur grafi sifirdan kurar: onceki klonlari yok et, reveal edilmis node'lardan
        /// parent->child gorunur agacini cikar, deterministik layout hesapla, node + cizgi klonla.
        /// </summary>
        private void RebuildGraph()
        {
            var gm = GameManager.Instance;
            if (gm == null || TechTreeContent == null || TechNodeTemplate == null)
                return;

            ClearGraph();

            var catalog = gm.TechCatalog;
            var visible = gm.GetRevealedTechNodes();
            _builtVisibleCount = visible.Count;
            if (catalog == null || visible.Count == 0)
                return;

            var visibleIds = new HashSet<string>();
            foreach (var node in visible)
                visibleIds.Add(node.Id);

            // Gorunur cocuk listeleri: reveal iliskisi + her iki ucun da gorunur olmasi.
            var childrenByParent = new Dictionary<string, List<TechNodeDefinitionSO>>();
            foreach (var parent in visible)
            {
                var children = new List<TechNodeDefinitionSO>();
                if (parent.RevealChildNodeIds != null)
                {
                    foreach (var childId in parent.RevealChildNodeIds)
                    {
                        if (string.IsNullOrEmpty(childId) || !visibleIds.Contains(childId))
                            continue;

                        var child = catalog.GetNode(childId);
                        if (child != null)
                            children.Add(child);
                    }
                }
                childrenByParent[parent.Id] = children;
            }

            var root = catalog.GetRootNode();
            if (root == null || !visibleIds.Contains(root.Id))
                return;

            // Deterministik agac layout'u: x = derinlik, y = gorunur yaprak sayisina gore dagitim.
            var positions = new Dictionary<string, Vector2>();
            var leafCounts = new Dictionary<string, int>();
            int totalLeaves = CountLeaves(root, childrenByParent, leafCounts, new HashSet<string>());
            int maxDepth = 0;
            LayoutSubtree(root, 0, 0f, childrenByParent, leafCounts, positions, ref maxDepth, new HashSet<string>());

            var nodeSize = TechNodeTemplate.sizeDelta;
            float contentWidth = ContentPadding.x * 2f + maxDepth * NodeSpacingX + nodeSize.x;
            float contentHeight = ContentPadding.y * 2f + Mathf.Max(1, totalLeaves) * NodeSpacingY;
            TechTreeContent.sizeDelta = new Vector2(contentWidth, contentHeight);

            // Once cizgiler (altta kalsin), sonra node'lar.
            foreach (var parent in visible)
            {
                if (!positions.ContainsKey(parent.Id))
                    continue;

                foreach (var child in childrenByParent[parent.Id])
                {
                    if (positions.ContainsKey(child.Id))
                        CreateConnection(positions[parent.Id], positions[child.Id], nodeSize);
                }
            }

            foreach (var node in visible)
            {
                if (positions.TryGetValue(node.Id, out var position))
                    CreateNodeView(node, position, gm);
            }
        }

        private int CountLeaves(TechNodeDefinitionSO node,
            Dictionary<string, List<TechNodeDefinitionSO>> childrenByParent,
            Dictionary<string, int> leafCounts,
            HashSet<string> guard)
        {
            // Dongusel reveal verisine karsi koruma: ayni node ikinci kez gelirse yaprak say.
            if (node == null || !guard.Add(node.Id))
                return 1;

            int total = 0;
            if (childrenByParent.TryGetValue(node.Id, out var children))
            {
                foreach (var child in children)
                    total += CountLeaves(child, childrenByParent, leafCounts, guard);
            }

            if (total <= 0)
                total = 1;

            leafCounts[node.Id] = total;
            return total;
        }

        private void LayoutSubtree(TechNodeDefinitionSO node, int depth, float yCursor,
            Dictionary<string, List<TechNodeDefinitionSO>> childrenByParent,
            Dictionary<string, int> leafCounts,
            Dictionary<string, Vector2> positions,
            ref int maxDepth,
            HashSet<string> guard)
        {
            if (node == null || !guard.Add(node.Id))
                return;

            if (depth > maxDepth)
                maxDepth = depth;

            int leaves = leafCounts.TryGetValue(node.Id, out int count) ? count : 1;
            float subtreeHeight = leaves * NodeSpacingY;
            positions[node.Id] = new Vector2(depth * NodeSpacingX, yCursor + subtreeHeight * 0.5f);

            if (!childrenByParent.TryGetValue(node.Id, out var children))
                return;

            float childCursor = yCursor;
            foreach (var child in children)
            {
                int childLeaves = leafCounts.TryGetValue(child.Id, out int c) ? c : 1;
                LayoutSubtree(child, depth + 1, childCursor, childrenByParent, leafCounts, positions, ref maxDepth, guard);
                childCursor += childLeaves * NodeSpacingY;
            }
        }

        private Vector2 GraphToContent(Vector2 graphPosition)
        {
            // Graf uzayi: x saga, y asagi buyur. Content sol-ust anchorlu; anchoredPosition y negatif.
            return new Vector2(ContentPadding.x + graphPosition.x, -(ContentPadding.y + graphPosition.y));
        }

        private void CreateConnection(Vector2 parentGraphPos, Vector2 childGraphPos, Vector2 nodeSize)
        {
            if (TechConnectionTemplate == null)
                return;

            // Node merkezi content'te G(pos)+(w/2,0) oldugundan: sag kenar = G(parent)+(w,0), sol kenar = G(child)
            var start = GraphToContent(parentGraphPos) + new Vector2(nodeSize.x, 0f);
            var end = GraphToContent(childGraphPos);
            var delta = end - start;
            float length = delta.magnitude;
            if (length < 1f)
                return;

            var clone = Instantiate(TechConnectionTemplate.gameObject, TechTreeContent);
            clone.name = "TechConnection";
            clone.SetActive(true);
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(length, rect.sizeDelta.y);
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            var image = clone.GetComponent<Image>();
            if (image != null)
                image.color = ConnectionColor;

            _connectionViews.Add(clone);
        }

        private void CreateNodeView(TechNodeDefinitionSO definition, Vector2 graphPosition, GameManager gm)
        {
            var clone = Instantiate(TechNodeTemplate.gameObject, TechTreeContent);
            clone.name = $"TechNode_{definition.Id}";
            clone.SetActive(true);

            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = GraphToContent(graphPosition) + new Vector2(TechNodeTemplate.sizeDelta.x * 0.5f, 0f);

            var view = new TechNodeView
            {
                Definition = definition,
                Root = clone,
                Rect = rect,
                Background = clone.GetComponent<Image>(),
                TitleText = FindChildComponent<TMP_Text>(clone, "TechNodeTitleText"),
                LevelText = FindChildComponent<TMP_Text>(clone, "TechNodeLevelText"),
                CostText = FindChildComponent<TMP_Text>(clone, "TechNodeCostText"),
                StatusText = FindChildComponent<TMP_Text>(clone, "TechNodeStatusText"),
                DescriptionText = FindChildComponent<TMP_Text>(clone, "TechNodeDescriptionText"),
                IconImage = FindChildComponent<Image>(clone, "TechNodeIconImage"),
                IconFallbackText = FindChildComponent<TMP_Text>(clone, "TechNodeIconFallbackText"),
                BuyButton = FindChildComponent<Button>(clone, "TechNodeBuyButton"),
                BuyButtonText = FindChildComponent<TMP_Text>(clone, "TechNodeBuyButtonText"),
            };

            if (view.BuyButton != null)
            {
                view.BuyAction = () => HandleBuyClicked(definition);
                view.BuyButton.onClick.AddListener(view.BuyAction);
            }

            _nodeViews.Add(view);
            RefreshNodeView(view, gm);
        }

        private void HandleBuyClicked(TechNodeDefinitionSO definition)
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            if (gm.TryBuyTechNode(definition))
                RebuildGraph(); // yeni reveal edilen cocuklar hemen gorunsun
        }

        private void RefreshNodeView(TechNodeView view, GameManager gm)
        {
            var def = view.Definition;
            int level = gm.GetTechNodeLevel(def.Id);
            bool canBuy = gm.CanBuyTechNode(def, out string reason);
            bool maxed = level >= def.MaxLevel;
            bool owned = level > 0;

            SetText(view.TitleText, def.Title);
            SetText(view.DescriptionText, def.Description);
            SetText(view.LevelText, def.MaxLevel > 1 ? $"LV {level}/{def.MaxLevel}" : (owned ? "LV 1" : string.Empty));

            string costLabel = maxed ? string.Empty : def.Cost.ToDisplayString();
            if (!maxed && string.IsNullOrEmpty(costLabel))
                costLabel = "FREE";
            SetText(view.CostText, costLabel);

            // Durum etiketi + rengi: AVAILABLE / BOUGHT / LOCKED / MAX / NEED ...
            string status;
            Color statusColor;
            Color background;
            if (maxed)
            {
                status = def.MaxLevel > 1 ? "MAX" : "BOUGHT";
                statusColor = def.MaxLevel > 1 ? MaxColor : BoughtColor;
                background = RowBoughtBg;
            }
            else if (canBuy)
            {
                status = "AVAILABLE";
                statusColor = AvailableColor;
                background = RowAvailableBg;
            }
            else if (!string.IsNullOrEmpty(reason) && reason.StartsWith("NEED"))
            {
                status = reason;
                statusColor = NeedColor;
                background = owned ? RowBoughtBg : RowAvailableBg;
            }
            else
            {
                status = "LOCKED";
                statusColor = LockedColor;
                background = RowLockedBg;
            }

            SetText(view.StatusText, status);
            if (view.StatusText != null)
                view.StatusText.color = statusColor;
            if (view.Background != null)
                view.Background.color = background;

            if (view.BuyButton != null)
            {
                bool showButton = !maxed;
                view.BuyButton.gameObject.SetActive(showButton);
                view.BuyButton.interactable = canBuy;
            }
            SetText(view.BuyButtonText, "BUY");

            // Icon null olabilir: bas-harf placeholder, art uretme.
            bool hasIcon = def.Icon != null;
            if (view.IconImage != null)
            {
                view.IconImage.sprite = hasIcon ? def.Icon : null;
                view.IconImage.enabled = hasIcon;
            }
            if (view.IconFallbackText != null)
            {
                view.IconFallbackText.gameObject.SetActive(!hasIcon);
                if (!hasIcon)
                    view.IconFallbackText.text = GetTitleInitials(def.Title);
            }
        }

        private static string GetTitleInitials(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "?";

            var parts = title.Split(' ');
            string initials = string.Empty;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                initials += char.ToUpperInvariant(part[0]);
                if (initials.Length >= 2)
                    break;
            }

            return string.IsNullOrEmpty(initials) ? "?" : initials;
        }

        private void ClearGraph()
        {
            foreach (var view in _nodeViews)
            {
                if (view.BuyButton != null && view.BuyAction != null)
                    view.BuyButton.onClick.RemoveListener(view.BuyAction);
                if (view.Root != null)
                    Destroy(view.Root);
            }
            _nodeViews.Clear();

            foreach (var connection in _connectionViews)
            {
                if (connection != null)
                    Destroy(connection);
            }
            _connectionViews.Clear();
            _builtVisibleCount = -1;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value)
                text.text = value;
        }

        private static T FindChildComponent<T>(GameObject root, string childName) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            foreach (var component in components)
            {
                if (component.gameObject.name == childName)
                    return component;
            }

            return null;
        }
    }
}
