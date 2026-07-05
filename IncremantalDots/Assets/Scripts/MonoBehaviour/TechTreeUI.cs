using System.Collections.Generic;
using DG.Tweening;
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
    ///
    /// Graf INCREMENTAL calisir (yik-yeniden-kur degil): mevcut node/cizgi view'lari korunur,
    /// yeni reveal edilenler parent'tan cizgi cizilerek + scale-pop ile eklenir, yeri degisenler
    /// tween'le kayar. Juice: DOTween (unscaled), SFX (Fantasy UI SFX Lite), TECH butonu badge'i,
    /// unlock toast'u, resource chip / archer drawer flash'lari.
    ///
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

        [Header("Juice")]
        public GameObject TechTreeOpenBadge;
        public TMP_Text ToastText;
        public AudioClip BuyClip;
        public AudioClip RevealClip;
        public AudioClip DeniedClip;
        public AudioClip PanelOpenClip;

        [Header("Layout")]
        public float NodeSpacingX = 300f;
        public float NodeSpacingY = 132f;
        public Vector2 ContentPadding = new Vector2(90f, 90f);

        private const float RefreshInterval = 0.2f;
        private const float BadgeCheckInterval = 0.5f;
        private const float MoveTweenDuration = 0.25f;
        private const float PopTweenDuration = 0.22f;
        private const float LineDrawDuration = 0.22f;
        private const float StatusColorDuration = 0.2f;
        private const int MaxLevelPips = 4;

        private static readonly Color AvailableColor = new Color(0.349f, 0.765f, 0.416f, 1f);
        private static readonly Color BoughtColor = new Color(0.56f, 0.66f, 0.76f, 1f);
        private static readonly Color LockedColor = new Color(0.42f, 0.47f, 0.52f, 1f);
        private static readonly Color MaxColor = new Color(0.949f, 0.788f, 0.298f, 1f);
        private static readonly Color NeedColor = new Color(0.898f, 0.632f, 0.235f, 1f);
        private static readonly Color RowAvailableBg = new Color(0.137f, 0.165f, 0.196f, 0.95f);
        private static readonly Color RowBoughtBg = new Color(0.118f, 0.184f, 0.137f, 0.95f);
        private static readonly Color RowLockedBg = new Color(0.09f, 0.105f, 0.125f, 0.9f);
        private static readonly Color ConnectionOwnedColor = new Color(0.42f, 0.68f, 0.46f, 0.95f);
        private static readonly Color ConnectionOpenColor = new Color(0.42f, 0.47f, 0.52f, 0.85f);
        private static readonly Color ConnectionLockedColor = new Color(0.42f, 0.47f, 0.52f, 0.32f);
        private static readonly Color PipFilledColor = new Color(0.949f, 0.788f, 0.298f, 1f);
        private static readonly Color PipEmptyColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color ChipFlashColor = new Color(0.85f, 0.35f, 0.28f, 0.95f);
        private static readonly Color DrawerFlashColor = new Color(0.28f, 0.62f, 0.34f, 0.95f);

        private readonly Dictionary<string, TechNodeView> _viewsById = new Dictionary<string, TechNodeView>();
        private readonly Dictionary<string, ConnectionView> _connectionsByKey = new Dictionary<string, ConnectionView>();
        private readonly Dictionary<string, Image> _resourceChipCache = new Dictionary<string, Image>();
        // Flash geri donus renkleri ILK goruste cache'lenir; flash ortasinda ikinci flash gelirse
        // "orijinal" olarak yari-flash rengi okunup kalici yanlis renk kalmasin diye.
        private readonly Dictionary<string, Color> _flashOriginalColors = new Dictionary<string, Color>();
        private float _nextRefreshTime;
        private float _nextBadgeCheckTime;
        private int _builtVisibleCount = -1;
        private bool _buttonsBound;
        private string _lastBoughtNodeId;
        private AudioSource _audio;
        private CanvasGroup _panelCanvasGroup;
        private Tween _badgeTween;
        private Sequence _toastSequence;

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
            public RectTransform PipsRoot;
            public Image[] Pips;
            public string CachedStatus;
            public Vector2 TargetPosition;
        }

        private sealed class ConnectionView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Image;
            public string ChildId;
            public Color CachedColor;
        }

        // ---------------------------------------------------------------
        // Yasam dongusu
        // ---------------------------------------------------------------
        private void OnEnable()
        {
            BindButtons();
            // Panel default kapali baslar; state sahibi bu controller'dir.
            if (TechTreePanel != null && TechTreePanel.activeSelf)
                TechTreePanel.SetActive(false);
            if (TechTreeOpenBadge != null)
                TechTreeOpenBadge.SetActive(false);
            if (ToastText != null)
                ToastText.alpha = 0f;
        }

        private void OnDisable()
        {
            UnbindButtons();
            _badgeTween?.Kill();
            _badgeTween = null;
            _toastSequence?.Kill();
            _toastSequence = null;
        }

        private void Update()
        {
            UpdateOpenBadge();

            if (TechTreePanel == null || !TechTreePanel.activeSelf)
                return;

            if (Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            Refresh();
        }

        // ---------------------------------------------------------------
        // Panel ac/kapa
        // ---------------------------------------------------------------
        public void OpenPanel()
        {
            if (TechTreePanel == null)
                return;

            TechTreePanel.SetActive(true);
            PlaySfx(PanelOpenClip, 0.7f);
            SyncGraph(false);
            FocusOnRelevantNode();

            // Fade + hafif scale acilisi (unscaled)
            var group = GetPanelCanvasGroup();
            var panelRect = TechTreePanel.transform as RectTransform;
            if (group != null)
            {
                group.DOKill();
                group.alpha = 0f;
                group.DOFade(1f, 0.16f).SetUpdate(true);
            }
            if (panelRect != null)
            {
                panelRect.DOKill();
                panelRect.localScale = Vector3.one * 0.96f;
                panelRect.DOScale(1f, 0.16f).SetEase(Ease.OutQuad).SetUpdate(true);
            }
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

        private CanvasGroup GetPanelCanvasGroup()
        {
            if (_panelCanvasGroup == null && TechTreePanel != null)
            {
                _panelCanvasGroup = TechTreePanel.GetComponent<CanvasGroup>();
                if (_panelCanvasGroup == null)
                    _panelCanvasGroup = TechTreePanel.AddComponent<CanvasGroup>();
            }
            return _panelCanvasGroup;
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

        // ---------------------------------------------------------------
        // TECH butonu badge'i: alinabilir node varken nabiz atar (panel kapaliyken)
        // ---------------------------------------------------------------
        private void UpdateOpenBadge()
        {
            if (TechTreeOpenBadge == null || Time.unscaledTime < _nextBadgeCheckTime)
                return;

            _nextBadgeCheckTime = Time.unscaledTime + BadgeCheckInterval;
            var gm = GameManager.Instance;
            bool panelOpen = TechTreePanel != null && TechTreePanel.activeSelf;
            bool show = !panelOpen && gm != null && HasAffordableTech(gm);

            if (TechTreeOpenBadge.activeSelf != show)
            {
                TechTreeOpenBadge.SetActive(show);
                _badgeTween?.Kill();
                _badgeTween = null;
                if (show)
                {
                    var rect = (RectTransform)TechTreeOpenBadge.transform;
                    rect.localScale = Vector3.one;
                    _badgeTween = rect.DOScale(1.25f, 0.55f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .SetUpdate(true);
                }
            }
        }

        private static bool HasAffordableTech(GameManager gm)
        {
            var revealed = gm.GetRevealedTechNodes();
            for (int i = 0; i < revealed.Count; i++)
            {
                if (gm.CanBuyTechNode(revealed[i], out _))
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------
        // Graf senkronu (INCREMENTAL): mevcutlar korunur/kayar, yeniler animasyonla gelir
        // ---------------------------------------------------------------
        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null)
                return;

            int visibleCount = gm.GetRevealedTechNodes().Count;
            if (visibleCount != _builtVisibleCount)
            {
                SyncGraph(true);
                return;
            }

            foreach (var view in _viewsById.Values)
                RefreshNodeView(view, gm);
            RefreshConnectionColors(gm, false);
        }

        private void SyncGraph(bool animateNew)
        {
            var gm = GameManager.Instance;
            if (gm == null || TechTreeContent == null || TechNodeTemplate == null)
                return;

            var catalog = gm.TechCatalog;
            var visible = gm.GetRevealedTechNodes();
            _builtVisibleCount = visible.Count;
            if (catalog == null || visible.Count == 0)
            {
                ClearGraph();
                _builtVisibleCount = visible.Count;
                return;
            }

            var visibleIds = new HashSet<string>();
            foreach (var node in visible)
                visibleIds.Add(node.Id);

            // Gorunur set kuculduyse (restart) diff yerine temiz kurulum
            foreach (var existingId in _viewsById.Keys)
            {
                if (!visibleIds.Contains(existingId))
                {
                    ClearGraph();
                    break;
                }
            }

            // Gorunur cocuk listeleri (reveal iliskisi + iki uc da gorunur)
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

            // Deterministik layout: x = derinlik, y = gorunur yaprak dagilimi
            var positions = new Dictionary<string, Vector2>();
            var leafCounts = new Dictionary<string, int>();
            int totalLeaves = CountLeaves(root, childrenByParent, leafCounts, new HashSet<string>());
            int maxDepth = 0;
            LayoutSubtree(root, 0, 0f, childrenByParent, leafCounts, positions, ref maxDepth, new HashSet<string>());

            var nodeSize = TechNodeTemplate.sizeDelta;
            float contentWidth = ContentPadding.x * 2f + maxDepth * NodeSpacingX + nodeSize.x;
            float contentHeight = ContentPadding.y * 2f + Mathf.Max(1, totalLeaves) * NodeSpacingY;
            TechTreeContent.sizeDelta = new Vector2(contentWidth, contentHeight);

            bool anyNew = false;

            // 1) Node'lar: mevcutsa yeni yerine kaydir, yoksa yarat (+pop)
            foreach (var node in visible)
            {
                if (!positions.TryGetValue(node.Id, out var graphPos))
                    continue;

                Vector2 target = GraphToContent(graphPos) + new Vector2(nodeSize.x * 0.5f, 0f);
                if (_viewsById.TryGetValue(node.Id, out var view))
                {
                    if ((view.TargetPosition - target).sqrMagnitude > 0.5f)
                    {
                        view.TargetPosition = target;
                        view.Rect.DOKill();
                        view.Rect.DOAnchorPos(target, MoveTweenDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                    }
                }
                else
                {
                    view = CreateNodeView(node, target, gm);
                    if (animateNew)
                    {
                        anyNew = true;
                        view.Rect.localScale = Vector3.one * 0.25f;
                        view.Rect.DOScale(1f, PopTweenDuration)
                            .SetDelay(LineDrawDuration * 0.6f)
                            .SetEase(Ease.OutBack)
                            .SetUpdate(true);
                    }
                }
            }

            // 2) Cizgiler: yeni ise cizilerek uzar, mevcutsa yeni uclara tasinir
            foreach (var parent in visible)
            {
                if (!positions.ContainsKey(parent.Id))
                    continue;

                foreach (var child in childrenByParent[parent.Id])
                {
                    if (!positions.ContainsKey(child.Id))
                        continue;

                    Vector2 start = GraphToContent(positions[parent.Id]) + new Vector2(nodeSize.x, 0f);
                    Vector2 end = GraphToContent(positions[child.Id]);
                    string key = parent.Id + ">" + child.Id;

                    if (_connectionsByKey.TryGetValue(key, out var connection))
                        UpdateConnectionTransform(connection, start, end, true);
                    else
                        CreateConnection(key, child.Id, start, end, animateNew);
                }
            }

            RefreshConnectionColors(gm, !animateNew);

            if (anyNew)
                PlaySfx(RevealClip, 0.8f);
        }

        private int CountLeaves(TechNodeDefinitionSO node,
            Dictionary<string, List<TechNodeDefinitionSO>> childrenByParent,
            Dictionary<string, int> leafCounts,
            HashSet<string> guard)
        {
            // Dongusel reveal verisine karsi koruma
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
            // Graf uzayi: x saga, y asagi buyur; content sol-ust anchorlu, y negatif
            return new Vector2(ContentPadding.x + graphPosition.x, -(ContentPadding.y + graphPosition.y));
        }

        // ---------------------------------------------------------------
        // Cizgi olusturma/guncelleme (node merkezleri G(pos)+(w/2,0) oldugundan
        // sag kenar = G(parent)+(w,0), sol kenar = G(child))
        // ---------------------------------------------------------------
        private void CreateConnection(string key, string childId, Vector2 start, Vector2 end, bool animate)
        {
            if (TechConnectionTemplate == null)
                return;

            var delta = end - start;
            float length = delta.magnitude;
            if (length < 1f)
                return;

            var clone = Instantiate(TechConnectionTemplate.gameObject, TechTreeContent);
            clone.name = "TechConnection_" + key;
            clone.SetActive(true);
            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = start;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            // Cizgiler node'lardan once cizilsin (mevcut node'larin altina)
            rect.SetSiblingIndex(TechConnectionTemplate.GetSiblingIndex() + 1);

            float height = rect.sizeDelta.y;
            var connection = new ConnectionView
            {
                Root = clone,
                Rect = rect,
                Image = clone.GetComponent<Image>(),
                ChildId = childId,
                CachedColor = ConnectionOpenColor,
            };
            if (connection.Image != null)
                connection.Image.color = ConnectionOpenColor;

            if (animate)
            {
                rect.sizeDelta = new Vector2(0f, height);
                rect.DOSizeDelta(new Vector2(length, height), LineDrawDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            }
            else
            {
                rect.sizeDelta = new Vector2(length, height);
            }

            _connectionsByKey[key] = connection;
        }

        private void UpdateConnectionTransform(ConnectionView connection, Vector2 start, Vector2 end, bool tween)
        {
            var delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var size = new Vector2(length, connection.Rect.sizeDelta.y);

            if (!tween)
            {
                connection.Rect.anchoredPosition = start;
                connection.Rect.sizeDelta = size;
                connection.Rect.localEulerAngles = new Vector3(0f, 0f, angle);
                return;
            }

            if ((connection.Rect.anchoredPosition - start).sqrMagnitude > 0.5f
                || Mathf.Abs(connection.Rect.sizeDelta.x - length) > 0.5f)
            {
                connection.Rect.DOKill();
                connection.Rect.DOAnchorPos(start, MoveTweenDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                connection.Rect.DOSizeDelta(size, MoveTweenDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                connection.Rect.DORotate(new Vector3(0f, 0f, angle), MoveTweenDuration).SetUpdate(true);
            }
        }

        /// <summary>Satin alinmis yollar parlak yesilimsi, alinabilir hedefe gidenler normal, kilitliler soluk.</summary>
        private void RefreshConnectionColors(GameManager gm, bool instant)
        {
            foreach (var connection in _connectionsByKey.Values)
            {
                if (connection.Image == null)
                    continue;

                Color target;
                int childLevel = gm.GetTechNodeLevel(connection.ChildId);
                if (childLevel > 0)
                    target = ConnectionOwnedColor;
                else
                {
                    var childDef = gm.TechCatalog != null ? gm.TechCatalog.GetNode(connection.ChildId) : null;
                    target = childDef != null && gm.CanBuyTechNode(childDef, out _)
                        ? ConnectionOpenColor
                        : ConnectionLockedColor;
                }

                if (connection.CachedColor == target)
                    continue;

                connection.CachedColor = target;
                connection.Image.DOKill();
                if (instant)
                    connection.Image.color = target;
                else
                    connection.Image.DOColor(target, StatusColorDuration).SetUpdate(true);
            }
        }

        // ---------------------------------------------------------------
        // Node view olusturma + durum yenileme
        // ---------------------------------------------------------------
        private TechNodeView CreateNodeView(TechNodeDefinitionSO definition, Vector2 contentPosition, GameManager gm)
        {
            var clone = Instantiate(TechNodeTemplate.gameObject, TechTreeContent);
            clone.name = "TechNode_" + definition.Id;
            clone.SetActive(true);

            var rect = (RectTransform)clone.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = contentPosition;

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
                PipsRoot = FindChildComponent<RectTransform>(clone, "TechNodePipsRoot"),
                TargetPosition = contentPosition,
            };

            if (view.PipsRoot != null)
            {
                var pips = new List<Image>();
                foreach (Transform pip in view.PipsRoot)
                {
                    var pipImage = pip.GetComponent<Image>();
                    if (pipImage != null)
                        pips.Add(pipImage);
                }
                view.Pips = pips.ToArray();
            }

            if (view.BuyButton != null)
            {
                view.BuyAction = () => HandleBuyClicked(view);
                view.BuyButton.onClick.AddListener(view.BuyAction);
            }

            _viewsById[definition.Id] = view;
            RefreshNodeView(view, gm, true);
            return view;
        }

        private void HandleBuyClicked(TechNodeView view)
        {
            var gm = GameManager.Instance;
            if (gm == null || view.Definition == null)
                return;

            var def = view.Definition;
            if (!gm.CanBuyTechNode(def, out _))
            {
                // Reddetme: kilit sesi + shake + kisa kirmizi tint
                PlaySfx(DeniedClip, 0.7f);
                view.Rect.DOKill(true);
                view.Rect.DOShakeAnchorPos(0.24f, new Vector2(7f, 0f), 18, 90f, false, true).SetUpdate(true);
                if (view.Background != null)
                {
                    view.Background.DOKill();
                    view.Background.DOColor(new Color(0.4f, 0.12f, 0.1f, 0.95f), 0.08f)
                        .SetUpdate(true)
                        .OnComplete(() => view.Background.DOColor(GetStatusBackground(view.CachedStatus), 0.25f).SetUpdate(true));
                }
                return;
            }

            var cost = def.Cost;
            bool unlockedSomething = false;
            ArcherType unlockedType = ArcherType.Basic;
            if (def.Effects != null)
            {
                foreach (var effect in def.Effects)
                {
                    if (effect.Type == TechNodeEffectType.UnlockArcherType)
                    {
                        unlockedSomething = true;
                        unlockedType = effect.ArcherType;
                    }
                }
            }

            if (!gm.TryBuyTechNode(def))
                return;

            _lastBoughtNodeId = def.Id;
            PlaySfx(BuyClip, 0.85f);

            // Punch + durum guncelle + graf genislet
            view.Rect.DOKill(true);
            view.Rect.DOPunchScale(Vector3.one * 0.07f, 0.2f, 7, 0.7f).SetUpdate(true);
            FlashResourceChips(cost);

            if (unlockedSomething)
            {
                var archerDef = gm.GetArcherDefinition(unlockedType);
                ShowToast((archerDef != null ? archerDef.DisplayName : def.Title).ToUpperInvariant() + " UNLOCKED");
                FlashArcherDrawerRow(archerDef);
            }
            else
            {
                int level = gm.GetTechNodeLevel(def.Id);
                ShowToast(def.Title.ToUpperInvariant() + (def.MaxLevel > 1 ? $" LV {level}" : " BOUGHT"));
            }

            SyncGraph(true);
            foreach (var v in _viewsById.Values)
                RefreshNodeView(v, gm);
        }

        private void RefreshNodeView(TechNodeView view, GameManager gm, bool instant = false)
        {
            var def = view.Definition;
            int level = gm.GetTechNodeLevel(def.Id);
            bool canBuy = gm.CanBuyTechNode(def, out string reason);
            bool maxed = level >= def.MaxLevel;
            bool owned = level > 0;

            SetText(view.TitleText, def.Title);
            SetText(view.DescriptionText, def.Description);

            // Seviye gosterimi: 2..4 arasi MaxLevel = pip'ler, digerleri text
            bool usePips = view.Pips != null && view.Pips.Length > 0 && def.MaxLevel > 1 && def.MaxLevel <= MaxLevelPips;
            if (view.PipsRoot != null)
                view.PipsRoot.gameObject.SetActive(usePips);
            if (usePips)
            {
                for (int i = 0; i < view.Pips.Length; i++)
                {
                    bool inRange = i < def.MaxLevel;
                    view.Pips[i].gameObject.SetActive(inRange);
                    if (inRange)
                        view.Pips[i].color = i < level ? PipFilledColor : PipEmptyColor;
                }
                if (view.LevelText != null)
                    view.LevelText.gameObject.SetActive(false);
            }
            else if (view.LevelText != null)
            {
                view.LevelText.gameObject.SetActive(true);
                SetText(view.LevelText, def.MaxLevel > 1 ? $"LV {level}/{def.MaxLevel}" : (owned ? "LV 1" : string.Empty));
            }

            string costLabel = maxed ? string.Empty : def.Cost.ToDisplayString();
            if (!maxed && string.IsNullOrEmpty(costLabel))
                costLabel = "FREE";
            SetText(view.CostText, costLabel);

            string status = ResolveStatus(maxed, canBuy, reason, def);
            if (view.CachedStatus != status)
            {
                bool firstFill = view.CachedStatus == null || instant;
                view.CachedStatus = status;
                SetText(view.StatusText, status);

                Color statusColor = GetStatusColor(status);
                Color background = GetStatusBackground(status);
                if (firstFill)
                {
                    if (view.StatusText != null) view.StatusText.color = statusColor;
                    if (view.Background != null) view.Background.color = background;
                }
                else
                {
                    if (view.StatusText != null)
                    {
                        view.StatusText.DOKill();
                        view.StatusText.DOColor(statusColor, StatusColorDuration).SetUpdate(true);
                    }
                    if (view.Background != null)
                    {
                        view.Background.DOKill();
                        view.Background.DOColor(background, StatusColorDuration).SetUpdate(true);
                    }
                }
            }

            if (view.BuyButton != null)
            {
                bool showButton = !maxed;
                if (view.BuyButton.gameObject.activeSelf != showButton)
                    view.BuyButton.gameObject.SetActive(showButton);
                view.BuyButton.interactable = true; // reddetme feedback'i (shake/ses) icin tiklanabilir kalir
            }
            SetText(view.BuyButtonText, "BUY");

            // Icon null olabilir: bas-harf placeholder, art uretme
            bool hasIcon = def.Icon != null;
            if (view.IconImage != null)
            {
                if (view.IconImage.sprite != def.Icon)
                    view.IconImage.sprite = hasIcon ? def.Icon : null;
                view.IconImage.enabled = hasIcon;
            }
            if (view.IconFallbackText != null)
            {
                if (view.IconFallbackText.gameObject.activeSelf != !hasIcon)
                    view.IconFallbackText.gameObject.SetActive(!hasIcon);
                if (!hasIcon)
                    SetText(view.IconFallbackText, GetTitleInitials(def.Title));
            }
        }

        private static string ResolveStatus(bool maxed, bool canBuy, string reason, TechNodeDefinitionSO def)
        {
            if (maxed)
                return def.MaxLevel > 1 ? "MAX" : "BOUGHT";
            if (canBuy)
                return "AVAILABLE";
            if (!string.IsNullOrEmpty(reason) && reason.StartsWith("NEED"))
                return reason;
            return "LOCKED";
        }

        private static Color GetStatusColor(string status)
        {
            if (status == "MAX") return MaxColor;
            if (status == "BOUGHT") return BoughtColor;
            if (status == "AVAILABLE") return AvailableColor;
            if (status != null && status.StartsWith("NEED")) return NeedColor;
            return LockedColor;
        }

        private static Color GetStatusBackground(string status)
        {
            if (status == "MAX" || status == "BOUGHT") return RowBoughtBg;
            if (status == "AVAILABLE" || (status != null && status.StartsWith("NEED"))) return RowAvailableBg;
            return RowLockedBg;
        }

        // ---------------------------------------------------------------
        // Juice yardimcilari: SFX / toast / chip flash / drawer flash / odak
        // ---------------------------------------------------------------
        private void PlaySfx(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            if (_audio == null)
            {
                _audio = GetComponent<AudioSource>();
                if (_audio == null)
                    _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
            }

            _audio.PlayOneShot(clip, volume);
        }

        private void ShowToast(string message)
        {
            if (ToastText == null)
                return;

            _toastSequence?.Kill();
            ToastText.text = message;
            ToastText.alpha = 0f;
            ToastText.gameObject.SetActive(true);
            _toastSequence = DOTween.Sequence()
                .Append(ToastText.DOFade(1f, 0.15f))
                .AppendInterval(1.6f)
                .Append(ToastText.DOFade(0f, 0.35f))
                .SetUpdate(true);
        }

        /// <summary>Harcanan kaynaklarin resource bar chip'lerinde kisa kirmizi flash.</summary>
        private void FlashResourceChips(ResourceCost cost)
        {
            if (cost.Wood > 0) FlashChip("WoodChip");
            if (cost.Stone > 0) FlashChip("StoneChip");
            if (cost.Iron > 0) FlashChip("IronChip");
            if (cost.Food > 0) FlashChip("FoodChip");
        }

        private void FlashChip(string chipName)
        {
            if (!_resourceChipCache.TryGetValue(chipName, out var chip) || chip == null)
            {
                chip = FindChildComponent<Image>(gameObject, chipName);
                _resourceChipCache[chipName] = chip;
            }

            if (chip == null)
                return;

            if (!_flashOriginalColors.TryGetValue(chipName, out var original))
            {
                original = chip.color;
                _flashOriginalColors[chipName] = original;
            }

            chip.DOKill();
            chip.color = original;
            chip.DOColor(ChipFlashColor, 0.1f)
                .SetUpdate(true)
                .OnComplete(() => chip.DOColor(original, 0.35f).SetUpdate(true));
        }

        /// <summary>Unlock sonrasi sag drawer'daki ilgili okcu satirinda yesil flash (MarketUI'ya dokunmadan, isimle).</summary>
        private void FlashArcherDrawerRow(ArcherDefinitionSO archerDef)
        {
            if (archerDef == null)
                return;

            string rowName = "ArcherRecruitmentRow_" + archerDef.Id;
            var row = FindChildComponent<Image>(gameObject, rowName);
            if (row == null)
                return;

            if (!_flashOriginalColors.TryGetValue(rowName, out var original))
            {
                original = row.color;
                _flashOriginalColors[rowName] = original;
            }

            row.DOKill();
            row.color = original;
            row.DOColor(DrawerFlashColor, 0.15f)
                .SetLoops(4, LoopType.Yoyo)
                .SetUpdate(true)
                .OnComplete(() => row.color = original);
        }

        /// <summary>Panel acilisinda son satin alinan (yoksa ilk AVAILABLE, o da yoksa root) node'u viewport merkezine getirir.</summary>
        private void FocusOnRelevantNode()
        {
            var gm = GameManager.Instance;
            if (gm == null || TechTreeContent == null || TechTreeViewport == null)
                return;

            TechNodeView focus = null;
            if (!string.IsNullOrEmpty(_lastBoughtNodeId))
                _viewsById.TryGetValue(_lastBoughtNodeId, out focus);

            if (focus == null)
            {
                foreach (var view in _viewsById.Values)
                {
                    if (view.CachedStatus == "AVAILABLE")
                    {
                        focus = view;
                        break;
                    }
                }
            }

            if (focus == null && gm.TechCatalog != null)
                _viewsById.TryGetValue(gm.TechCatalog.RootNodeId, out focus);

            if (focus == null)
                return;

            float scale = TechTreeContent.localScale.x;
            Vector2 viewportSize = TechTreeViewport.rect.size;
            Vector2 desired = new Vector2(
                viewportSize.x * 0.5f - focus.TargetPosition.x * scale,
                -viewportSize.y * 0.5f - focus.TargetPosition.y * scale);

            // Icerik sinirlari icinde kal (elastic sarkmasin)
            Vector2 contentSize = TechTreeContent.sizeDelta * scale;
            desired.x = Mathf.Clamp(desired.x, Mathf.Min(0f, viewportSize.x - contentSize.x), 0f);
            desired.y = Mathf.Clamp(desired.y, 0f, Mathf.Max(0f, contentSize.y - viewportSize.y));
            TechTreeContent.anchoredPosition = desired;
        }

        // ---------------------------------------------------------------
        // Temizlik
        // ---------------------------------------------------------------
        private void ClearGraph()
        {
            foreach (var view in _viewsById.Values)
            {
                if (view.BuyButton != null && view.BuyAction != null)
                    view.BuyButton.onClick.RemoveListener(view.BuyAction);
                if (view.Rect != null) view.Rect.DOKill();
                if (view.Background != null) view.Background.DOKill();
                if (view.StatusText != null) view.StatusText.DOKill();
                if (view.Root != null)
                    Destroy(view.Root);
            }
            _viewsById.Clear();

            foreach (var connection in _connectionsByKey.Values)
            {
                if (connection.Rect != null) connection.Rect.DOKill();
                if (connection.Image != null) connection.Image.DOKill();
                if (connection.Root != null)
                    Destroy(connection.Root);
            }
            _connectionsByKey.Clear();
            _builtVisibleCount = -1;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value)
                text.text = value;
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
