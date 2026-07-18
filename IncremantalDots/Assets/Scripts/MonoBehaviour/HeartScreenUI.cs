using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DeadWalls
{
    public static class HeartGraphLayoutUtility
    {
        public static Vector2 GetPosition(
            HeartNodeBranch branch,
            float depth,
            float horizontalSpacing,
            float verticalSpacing)
        {
            float safeDepth = Mathf.Max(0, depth);
            switch (branch)
            {
                case HeartNodeBranch.Army:
                    return Vector2.right * (safeDepth * horizontalSpacing);
                case HeartNodeBranch.Defense:
                    return Vector2.left * (safeDepth * horizontalSpacing);
                case HeartNodeBranch.Production:
                    return Vector2.up * (safeDepth * verticalSpacing);
                case HeartNodeBranch.HeartMagic:
                    return Vector2.down * (safeDepth * verticalSpacing);
                default:
                    return Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Generated Castle Heart graph'inin tek player-facing controller'i. Hidden-safe
    /// presentation contract'ini cizer, Grave Essence transaction'ini GameManager'a yollar
    /// ve panel yasami boyunca tum simulation'i merkezi pause lease ile durdurur.
    /// </summary>
    public sealed class HeartScreenUI : MonoBehaviour
    {
        [Header("Screen")]
        public GameObject HeartPanel;
        public Button HeartOpenButton;
        public Button HeartCloseButton;
        public RectTransform HeartViewport;
        public RectTransform HeartContent;
        public RectTransform HeartNodeTemplate;
        public RectTransform HeartConnectionTemplate;

        [Header("Header")]
        public TMP_Text GraveEssenceText;
        public TMP_Text ScreenStatusText;
        public TMP_Text BranchCompassText;
        public Button QuantityOneButton;
        public Button QuantityTenButton;
        public Button QuantityMaxButton;

        [Header("Feedback")]
        public GameObject AffordableBadge;
        public TMP_Text ToastText;
        public AudioClip BuyClip;
        public AudioClip RevealClip;
        public AudioClip DeniedClip;
        public AudioClip PanelOpenClip;

        [Header("Layout")]
        public Vector2 NodeSize = new Vector2(292f, 188f);
        public float HorizontalSpacing = 386f;
        public float VerticalSpacing = 278f;
        public Vector2 ContentPadding = new Vector2(190f, 160f);

        private const float RefreshInterval = 0.15f;
        private const float BadgeInterval = 0.5f;

        private static readonly Color ArmyColor = new Color(0.86f, 0.25f, 0.22f, 1f);
        private static readonly Color DefenseColor = new Color(0.25f, 0.55f, 0.92f, 1f);
        private static readonly Color ProductionColor = new Color(0.27f, 0.76f, 0.44f, 1f);
        private static readonly Color MagicColor = new Color(0.68f, 0.34f, 0.94f, 1f);
        private static readonly Color HiddenColor = new Color(0.075f, 0.09f, 0.12f, 0.94f);
        private static readonly Color RootColor = new Color(0.76f, 0.18f, 0.22f, 0.98f);
        private static readonly Color LockedColor = new Color(0.25f, 0.27f, 0.31f, 0.96f);
        private static readonly Color KeystoneColor = new Color(0.96f, 0.67f, 0.22f, 1f);

        private readonly Dictionary<string, NodeView> _nodeViews =
            new Dictionary<string, NodeView>(StringComparer.Ordinal);
        private readonly Dictionary<string, ConnectionView> _connectionViews =
            new Dictionary<string, ConnectionView>(StringComparer.Ordinal);
        private readonly Dictionary<string, HeartGraphNodePresentation> _presentationsBySlot =
            new Dictionary<string, HeartGraphNodePresentation>(StringComparer.Ordinal);
        private readonly Dictionary<string, TMP_Text> _keystoneChoiceLabels =
            new Dictionary<string, TMP_Text>(StringComparer.Ordinal);
        private readonly Dictionary<HeartNodeBranch, Image> _branchAxes =
            new Dictionary<HeartNodeBranch, Image>();

        private HeartPurchaseQuantity _selectedQuantity = HeartPurchaseQuantity.One;
        private IDisposable _pauseLease;
        private float _nextRefreshTime;
        private float _nextBadgeTime;
        private AudioSource _audio;
        private Sequence _toastSequence;
        private bool _buttonsBound;

        public bool IsOpen => HeartPanel != null && HeartPanel.activeSelf;
        public bool HasActiveOwnedTweens => HasOwnedTweenActivity();
        public HeartPurchaseQuantity SelectedQuantity => _selectedQuantity;

        public event Action HeartOpenedByPlayer;
        public event Action HeartClosedByPlayer;

        private IHeartScreenRuntime Runtime => GameManager.Instance;

        private sealed class NodeView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
            public Image Accent;
            public Image RarityMarker;
            public Outline Outline;
            public Image Icon;
            public TMP_Text IconFallback;
            public TMP_Text Title;
            public TMP_Text Level;
            public TMP_Text Description;
            public TMP_Text Cost;
            public TMP_Text Status;
            public Button BuyButton;
            public TMP_Text BuyButtonText;
            public UnityAction BuyAction;
            public string SlotId;
        }

        private sealed class ConnectionView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Image;
        }

        private void OnEnable()
        {
            ApplyScreenPolish();
            BindButtons();
            if (HeartPanel != null)
                HeartPanel.SetActive(false);
            if (AffordableBadge != null)
                AffordableBadge.SetActive(false);
            if (ToastText != null)
                ToastText.alpha = 0f;
            SetQuantity(HeartPurchaseQuantity.One);
        }

        private void OnDisable()
        {
            UnbindButtons();
            ReleasePause();
            KillOwnedTweens();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                UpdateAffordableBadge();
                return;
            }

            SimulationPauseService.EnforcePausedState();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePanelFromPlayer();
                return;
            }

            if (Time.unscaledTime < _nextRefreshTime)
                return;
            _nextRefreshTime = Time.unscaledTime + RefreshInterval;
            RefreshScreen();
        }

        public void OpenPanel()
        {
            if (HeartPanel == null || IsOpen)
                return;

            _pauseLease = SimulationPauseService.Acquire(nameof(HeartScreenUI));
            HeartPanel.SetActive(true);
            HeartPanel.transform.SetAsLastSibling();
            if (ToastText != null)
                ToastText.transform.SetAsLastSibling();
            if (AffordableBadge != null)
                AffordableBadge.SetActive(false);
            PlaySfx(PanelOpenClip, 0.7f);
            RefreshScreen();
            FocusRoot();

            CanvasGroup group = HeartPanel.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.DOKill();
                group.alpha = 0f;
                group.DOFade(1f, 0.14f)
                    .SetUpdate(true)
                    .SetLink(HeartPanel, LinkBehaviour.KillOnDestroy);
            }
        }

        public void ClosePanel()
        {
            KillOwnedTweens();
            if (HeartPanel != null)
                HeartPanel.SetActive(false);
            ReleasePause();
        }

        public void TogglePanel()
        {
            if (IsOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        private void OpenPanelFromPlayer()
        {
            bool wasOpen = IsOpen;
            OpenPanel();
            if (!wasOpen && IsOpen)
                HeartOpenedByPlayer?.Invoke();
        }

        private void ClosePanelFromPlayer()
        {
            if (!IsOpen)
                return;

            ClosePanel();
            HeartClosedByPlayer?.Invoke();
        }

        public void SelectQuantityOne() => SetQuantity(HeartPurchaseQuantity.One);
        public void SelectQuantityTen() => SetQuantity(HeartPurchaseQuantity.Ten);
        public void SelectQuantityMax() => SetQuantity(HeartPurchaseQuantity.BuyMax);

        private void SetQuantity(HeartPurchaseQuantity quantity)
        {
            _selectedQuantity = quantity;
            RefreshQuantityButtons();
            if (IsOpen)
                RefreshScreen();
        }

        private void RefreshScreen()
        {
            IHeartScreenRuntime runtime = Runtime;
            if (GraveEssenceText != null)
                GraveEssenceText.text = $"GRAVE ESSENCE  {FormatLong(runtime?.GraveEssenceAmount ?? 0L)}";
            if (BranchCompassText != null)
                BranchCompassText.text =
                    "<color=#E76058>ARMY  /  EAST</color>     "
                    + "<color=#68A9F2>DEFENSE  /  WEST</color>     "
                    + "<color=#E0B44E>PRODUCTION  /  NORTH</color>     "
                    + "<color=#B479F2>HEART MAGIC  /  SOUTH</color>";

            HeartGraphPresentation presentation = null;
            IReadOnlyList<string> errors = null;
            if (runtime == null
                || !runtime.TryBuildHeartPresentation(out presentation, out errors))
            {
                ClearGraph();
                SetScreenStatus(errors != null && errors.Count > 0
                    ? errors[0]
                    : "CASTLE HEART RUNTIME NOT READY", true);
                return;
            }

            SetScreenStatus("THE SIEGE IS FROZEN WHILE THE HEART IS OPEN", false);
            SyncGraph(presentation, runtime);
        }

        private void SyncGraph(HeartGraphPresentation presentation, IHeartScreenRuntime runtime)
        {
            if (HeartContent == null || HeartNodeTemplate == null || HeartConnectionTemplate == null)
                return;

            _presentationsBySlot.Clear();
            int maxDepth = 1;
            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                _presentationsBySlot[node.SlotId] = node;
                maxDepth = Mathf.Max(maxDepth, node.Depth);
            }

            RemoveStaleNodes(_presentationsBySlot);
            float width = ContentPadding.x * 2f + maxDepth * HorizontalSpacing * 2f + NodeSize.x;
            float height = ContentPadding.y * 2f + maxDepth * VerticalSpacing * 2f + NodeSize.y;
            HeartContent.anchorMin = HeartContent.anchorMax = HeartContent.pivot = new Vector2(0.5f, 0.5f);
            HeartContent.sizeDelta = new Vector2(width, height);
            UpdateBranchAxes(maxDepth);

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                NodeView view = GetOrCreateNode(node.SlotId);
                Vector2 position = GetDisplayPosition(node);
                view.Rect.anchoredPosition = position;
                RefreshNode(view, node, runtime);
            }

            var liveConnections = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < presentation.Edges.Count; i++)
            {
                HeartGraphEdgePresentation edge = presentation.Edges[i];
                if (!_nodeViews.TryGetValue(edge.FromSlotId, out NodeView from)
                    || !_nodeViews.TryGetValue(edge.ToSlotId, out NodeView to))
                {
                    continue;
                }

                string key = edge.FromSlotId + ">" + edge.ToSlotId;
                liveConnections.Add(key);
                ConnectionView connection = GetOrCreateConnection(key);
                bool keystoneChoice = IsKeystonePair(edge.FromSlotId, edge.ToSlotId);
                UpdateConnection(
                    connection,
                    from.Rect.anchoredPosition,
                    to.Rect.anchoredPosition,
                    keystoneChoice ? KeystoneColor : GetBranchColor(edge.ToBranch),
                    IsHiddenSlot(edge.ToSlotId),
                    keystoneChoice);
            }
            AddKeystoneForkConnections(presentation, liveConnections);
            RemoveStaleConnections(liveConnections);
            SyncKeystoneChoiceLabels();
        }

        private Vector2 GetDisplayPosition(HeartGraphNodePresentation node)
        {
            if (node.IsRoot)
                return Vector2.zero;

            Vector2 defaultPosition = HeartGraphLayoutUtility.GetPosition(
                node.Branch,
                node.Depth,
                HorizontalSpacing,
                VerticalSpacing);
            if (!TryGetVisibleKeystonePartner(node, out HeartGraphNodePresentation partner)
                || partner.Branch != node.Branch)
            {
                return defaultPosition;
            }

            float sharedDepth = (node.Depth + partner.Depth) * 0.5f;
            Vector2 sharedPosition = HeartGraphLayoutUtility.GetPosition(
                node.Branch,
                sharedDepth,
                HorizontalSpacing,
                VerticalSpacing);
            bool nodeComesFirst = node.Depth < partner.Depth
                                  || (node.Depth == partner.Depth
                                      && string.CompareOrdinal(node.SlotId, partner.SlotId) < 0);
            float direction = nodeComesFirst ? -1f : 1f;
            bool horizontalBranch = node.Branch == HeartNodeBranch.Army
                                    || node.Branch == HeartNodeBranch.Defense;
            Vector2 choiceOffset = horizontalBranch
                ? Vector2.up * (NodeSize.y * 0.66f * direction)
                : Vector2.right * (NodeSize.x * 0.62f * direction);
            return sharedPosition + choiceOffset;
        }

        private void AddKeystoneForkConnections(
            HeartGraphPresentation presentation,
            HashSet<string> liveConnections)
        {
            for (int nodeIndex = 0; nodeIndex < presentation.Nodes.Count; nodeIndex++)
            {
                HeartGraphNodePresentation first = presentation.Nodes[nodeIndex];
                if (!TryGetCanonicalVisibleKeystonePair(
                        first,
                        out HeartGraphNodePresentation second,
                        out string pairKey))
                {
                    continue;
                }

                HeartGraphEdgePresentation closestIncoming = null;
                HeartGraphNodePresentation closestIncomingSource = null;
                bool incomingTargetsFirst = false;
                HeartGraphEdgePresentation closestOutgoing = null;
                HeartGraphNodePresentation closestOutgoingTarget = null;
                bool outgoingStartsAtFirst = false;
                int minimumPairDepth = Mathf.Min(first.Depth, second.Depth);
                int maximumPairDepth = Mathf.Max(first.Depth, second.Depth);
                for (int edgeIndex = 0; edgeIndex < presentation.Edges.Count; edgeIndex++)
                {
                    HeartGraphEdgePresentation edge = presentation.Edges[edgeIndex];
                    bool incomingToFirst = string.Equals(edge.ToSlotId, first.SlotId, StringComparison.Ordinal)
                                           && !string.Equals(edge.FromSlotId, second.SlotId, StringComparison.Ordinal);
                    bool incomingToSecond = string.Equals(edge.ToSlotId, second.SlotId, StringComparison.Ordinal)
                                            && !string.Equals(edge.FromSlotId, first.SlotId, StringComparison.Ordinal);
                    if ((incomingToFirst || incomingToSecond)
                        && _presentationsBySlot.TryGetValue(
                            edge.FromSlotId,
                            out HeartGraphNodePresentation incomingSource)
                        && incomingSource.Branch == first.Branch
                        && incomingSource.Depth < minimumPairDepth
                        && (closestIncomingSource == null
                            || incomingSource.Depth > closestIncomingSource.Depth))
                    {
                        closestIncoming = edge;
                        closestIncomingSource = incomingSource;
                        incomingTargetsFirst = incomingToFirst;
                    }

                    bool outgoingFromFirst = string.Equals(edge.FromSlotId, first.SlotId, StringComparison.Ordinal)
                                             && !string.Equals(edge.ToSlotId, second.SlotId, StringComparison.Ordinal);
                    bool outgoingFromSecond = string.Equals(edge.FromSlotId, second.SlotId, StringComparison.Ordinal)
                                              && !string.Equals(edge.ToSlotId, first.SlotId, StringComparison.Ordinal);
                    if ((outgoingFromFirst || outgoingFromSecond)
                        && _presentationsBySlot.TryGetValue(
                            edge.ToSlotId,
                            out HeartGraphNodePresentation outgoingTarget)
                        && outgoingTarget.Branch == first.Branch
                        && outgoingTarget.Depth > maximumPairDepth
                        && (closestOutgoingTarget == null
                            || outgoingTarget.Depth < closestOutgoingTarget.Depth))
                    {
                        closestOutgoing = edge;
                        closestOutgoingTarget = outgoingTarget;
                        outgoingStartsAtFirst = outgoingFromFirst;
                    }
                }

                if (closestIncoming != null)
                {
                    string otherTarget = incomingTargetsFirst ? second.SlotId : first.SlotId;
                    AddDerivedChoiceConnection(
                        $"choice-in:{pairKey}:{closestIncoming.FromSlotId}>{otherTarget}",
                        closestIncoming.FromSlotId,
                        otherTarget,
                        liveConnections);
                }

                if (closestOutgoing != null)
                {
                    string otherSource = outgoingStartsAtFirst ? second.SlotId : first.SlotId;
                    AddDerivedChoiceConnection(
                        $"choice-out:{pairKey}:{otherSource}>{closestOutgoing.ToSlotId}",
                        otherSource,
                        closestOutgoing.ToSlotId,
                        liveConnections);
                }
            }
        }

        private void AddDerivedChoiceConnection(
            string key,
            string fromSlotId,
            string toSlotId,
            HashSet<string> liveConnections)
        {
            if (!_nodeViews.TryGetValue(fromSlotId, out NodeView from)
                || !_nodeViews.TryGetValue(toSlotId, out NodeView to)
                || !liveConnections.Add(key))
            {
                return;
            }

            ConnectionView connection = GetOrCreateConnection(key);
            UpdateConnection(
                connection,
                from.Rect.anchoredPosition,
                to.Rect.anchoredPosition,
                KeystoneColor,
                IsHiddenSlot(toSlotId),
                true);
        }

        private NodeView GetOrCreateNode(string slotId)
        {
            if (_nodeViews.TryGetValue(slotId, out NodeView existing))
                return existing;

            GameObject root = Instantiate(HeartNodeTemplate.gameObject, HeartContent);
            root.name = "HeartNode_" + SanitizeSlot(slotId);
            root.SetActive(true);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = NodeSize;

            var view = new NodeView
            {
                Root = root,
                Rect = rect,
                Background = root.GetComponent<Image>(),
                Icon = FindDeep<Image>(root.transform, "TechNodeIconImage", "HeartNodeIconImage"),
                IconFallback = FindDeep<TMP_Text>(root.transform, "TechNodeIconFallbackText", "HeartNodeIconFallbackText"),
                Title = FindDeep<TMP_Text>(root.transform, "TechNodeTitleText", "HeartNodeTitleText"),
                Level = FindDeep<TMP_Text>(root.transform, "TechNodeLevelText", "HeartNodeLevelText"),
                Description = FindDeep<TMP_Text>(root.transform, "TechNodeDescriptionText", "HeartNodeDescriptionText"),
                Cost = FindDeep<TMP_Text>(root.transform, "TechNodeCostText", "HeartNodeCostText"),
                Status = FindDeep<TMP_Text>(root.transform, "TechNodeStatusText", "HeartNodeStatusText"),
                BuyButton = FindDeep<Button>(root.transform, "TechNodeBuyButton", "HeartNodeBuyButton"),
                BuyButtonText = FindDeep<TMP_Text>(root.transform, "TechNodeBuyButtonText", "HeartNodeBuyButtonText"),
                SlotId = slotId
            };
            view.Accent = EnsureDecorationImage(root.transform, "HeartNodeAccent");
            view.RarityMarker = EnsureDecorationImage(root.transform, "HeartNodeRarityMarker");
            if (view.Background != null)
                view.Outline = view.Background.GetComponent<Outline>()
                               ?? view.Background.gameObject.AddComponent<Outline>();
            ConfigureNodeLayout(view);
            if (view.BuyButton != null)
            {
                view.BuyAction = () => Purchase(view.SlotId);
                view.BuyButton.onClick.RemoveAllListeners();
                view.BuyButton.onClick.AddListener(view.BuyAction);
            }
            SetOptionalChildActive(root.transform, "TechNodePipsRoot", false);
            _nodeViews.Add(slotId, view);

            rect.localScale = Vector3.one * 0.82f;
            rect.DOScale(1f, 0.18f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(root, LinkBehaviour.KillOnDestroy);
            return view;
        }

        private void RefreshNode(
            NodeView view,
            HeartGraphNodePresentation node,
            IHeartScreenRuntime runtime)
        {
            Color branchColor = GetBranchColor(node.Branch);
            if (node.IsRoot)
            {
                SetText(view.Title, "CASTLE HEART");
                SetText(view.Level, "ROOT");
                SetText(view.Description,
                    "Shape this stand with Grave Essence. Every awakened path endures until the Wall falls.");
                SetText(view.Cost, $"{FormatLong(runtime.GraveEssenceAmount)} GRAVE ESSENCE");
                SetText(view.Status, "FOUR PATHS  /  ONE STAND");
                SetIcon(view, null, "DW", RootColor);
                SetButton(view, false, false, string.Empty);
                SetBackground(view, RootColor);
                SetNodeChrome(view, RootColor, false,
                    new Color(1f, 0.72f, 0.30f, 0.95f), true);
                return;
            }

            if (!node.IsExactContentVisible)
            {
                SetText(view.Title, "VEILED");
                SetText(view.Level, $"DEPTH {node.Depth}");
                SetText(view.Description, "Awaken a connected node to uncover this choice.");
                SetText(view.Cost, string.Empty);
                SetText(view.Status, GetBranchName(node.Branch));
                SetIcon(view, null, "?", branchColor);
                SetButton(view, false, false, string.Empty);
                SetBackground(view, HiddenColor);
                SetNodeChrome(view, branchColor, false, branchColor, false, true);
                return;
            }

            SetText(view.Title, string.IsNullOrWhiteSpace(node.Title) ? "UNNAMED" : node.Title.ToUpperInvariant());
            SetText(view.Level, BuildNodeEyebrow(node));
            SetText(view.Description, BuildDescription(node));
            SetIcon(view, node.Icon, BuildFallback(node.Title), branchColor);

            if (node.LockState == HeartNodeLockState.KeystoneConflict)
            {
                SetText(view.Cost, "LOCKED FOR THIS RUN");
                SetText(view.Status, node.KeystoneConflict != null
                    ? $"SEALED BY {node.KeystoneConflict.ConflictingChoiceTitle.ToUpperInvariant()}"
                    : "OPPOSING DOCTRINE COMMITTED");
                SetButton(view, true, false, "LOCKED");
                SetBackground(view, LockedColor);
                SetNodeChrome(view, branchColor, node.Rarity == HeartNodeRarity.Rare,
                    new Color(0.72f, 0.18f, 0.20f, 0.90f), false);
                return;
            }

            HeartPurchaseQuantity quantity = node.Type == HeartNodeType.Repeatable
                ? _selectedQuantity
                : HeartPurchaseQuantity.One;
            bool alreadyOwned = node.Type != HeartNodeType.Repeatable && node.Level > 0;
            HeartPurchaseEvaluation evaluation = alreadyOwned
                ? null
                : runtime.EvaluateHeartPurchase(node.ExactNodeId, quantity);
            bool canPurchase = evaluation != null
                               && evaluation.CanPurchase
                               && node.EffectInformationComplete;
            HeartPurchaseQuote quote = evaluation?.Quote;
            if (alreadyOwned)
            {
                SetText(view.Cost, node.Type == HeartNodeType.Keystone
                    ? "ACTIVE UNTIL THE WALL FALLS"
                    : "PURCHASED FOR THIS RUN");
            }
            else if (quote != null)
            {
                SetText(view.Cost, node.Type == HeartNodeType.Keystone
                    ? $"{FormatLong(quote.TotalGraveEssenceCost)} ESSENCE  ·  RUN COMMITMENT"
                    : $"{FormatLong(quote.TotalGraveEssenceCost)} ESSENCE  /  +{quote.LevelsToBuy}");
            }
            else
            {
                SetText(view.Cost, evaluation?.Message ?? string.Empty);
            }

            string status = BuildNodeStatus(node);
            if (node.KeystoneConflict != null)
            {
                if (alreadyOwned)
                    status = "DOCTRINE COMMITTED";
                else if (node.KeystoneConflict.WillLockOnPurchase)
                    status = $"CHOOSE ONE  ·  LOCKS {node.KeystoneConflict.ConflictingChoiceTitle.ToUpperInvariant()}";
            }
            SetText(view.Status, status);

            SetButton(
                view,
                true,
                canPurchase,
                alreadyOwned
                    ? node.Type == HeartNodeType.Keystone ? "COMMITTED" : "AWAKENED"
                    : canPurchase ? GetPurchaseAction(node.Type) : "UNAVAILABLE");
            SetBackground(view, Color.Lerp(HiddenColor, branchColor, node.Level > 0 ? 0.48f : 0.28f));
            SetNodeChrome(view, branchColor, node.Rarity == HeartNodeRarity.Rare,
                node.Level > 0 ? new Color(1f, 0.75f, 0.32f, 0.95f) : branchColor,
                node.Level > 0);
            StylePurchaseButton(view, branchColor, canPurchase, alreadyOwned);
        }

        private void Purchase(string slotId)
        {
            if (!_presentationsBySlot.TryGetValue(slotId, out HeartGraphNodePresentation node)
                || !node.IsExactContentVisible
                || string.IsNullOrWhiteSpace(node.ExactNodeId)
                || Runtime == null)
            {
                return;
            }

            HeartPurchaseQuantity quantity = node.Type == HeartNodeType.Repeatable
                ? _selectedQuantity
                : HeartPurchaseQuantity.One;
            HeartPurchaseResult result = Runtime.TryPurchaseHeartNode(node.ExactNodeId, quantity);
            if (result == null || !result.Succeeded)
            {
                PlaySfx(DeniedClip, 0.85f);
                ShowToast(result?.Message ?? "PURCHASE REJECTED", false);
                return;
            }

            PlaySfx(BuyClip, 0.9f);
            if (result.NewlyRevealedNodeIds.Count > 0)
                PlaySfx(RevealClip, 0.75f);
            ShowToast(
                node.Type == HeartNodeType.Repeatable
                    ? $"{node.Title.ToUpperInvariant()}  /  LEVEL {result.Quote.NewLevel}"
                    : $"{node.Title.ToUpperInvariant()} AWAKENED",
                true);
            RefreshScreen();
        }

        private void UpdateAffordableBadge()
        {
            if (AffordableBadge == null || Time.unscaledTime < _nextBadgeTime)
                return;
            _nextBadgeTime = Time.unscaledTime + BadgeInterval;

            bool affordable = false;
            IHeartScreenRuntime runtime = Runtime;
            if (runtime != null
                && runtime.TryBuildHeartPresentation(
                    out HeartGraphPresentation presentation,
                    out _))
            {
                for (int i = 0; i < presentation.Nodes.Count; i++)
                {
                    HeartGraphNodePresentation node = presentation.Nodes[i];
                    if (!node.IsExactContentVisible || node.IsRoot || string.IsNullOrWhiteSpace(node.ExactNodeId))
                        continue;
                    HeartPurchaseEvaluation evaluation = runtime.EvaluateHeartPurchase(
                        node.ExactNodeId,
                        HeartPurchaseQuantity.One);
                    if (evaluation.CanPurchase)
                    {
                        affordable = true;
                        break;
                    }
                }
            }
            AffordableBadge.SetActive(affordable);
        }

        private ConnectionView GetOrCreateConnection(string key)
        {
            if (_connectionViews.TryGetValue(key, out ConnectionView existing))
                return existing;

            GameObject root = Instantiate(HeartConnectionTemplate.gameObject, HeartContent);
            root.name = "HeartVein_" + SanitizeSlot(key);
            root.SetActive(true);
            root.transform.SetAsFirstSibling();
            var view = new ConnectionView
            {
                Root = root,
                Rect = (RectTransform)root.transform,
                Image = root.GetComponent<Image>()
            };
            _connectionViews.Add(key, view);
            return view;
        }

        private static void UpdateConnection(
            ConnectionView view,
            Vector2 start,
            Vector2 end,
            Color color,
            bool hidden,
            bool emphasized = false)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            view.Rect.anchorMin = view.Rect.anchorMax = view.Rect.pivot = new Vector2(0.5f, 0.5f);
            view.Rect.anchoredPosition = (start + end) * 0.5f;
            view.Rect.sizeDelta = new Vector2(length, hidden ? 3f : emphasized ? 8f : 5f);
            view.Rect.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            if (view.Image != null)
            {
                color.a = hidden ? 0.24f : emphasized ? 0.92f : 0.72f;
                view.Image.color = color;
                view.Image.raycastTarget = false;
            }
        }

        private static string BuildDescription(HeartGraphNodePresentation node)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(node.Description))
                builder.Append(node.Description.Trim());

            for (int i = 0; i < node.Effects.Count; i++)
            {
                HeartEffectPresentation effect = node.Effects[i];
                if (builder.Length > 0)
                    builder.AppendLine();
                builder.Append("EFFECT  /  ").Append(effect.Label);
                if (!string.IsNullOrWhiteSpace(effect.CurrentValueText))
                {
                    builder.Append(": ")
                        .Append(effect.CurrentValueText)
                        .Append(" > ")
                        .Append(effect.AfterPurchaseValueText);
                    if (!string.IsNullOrWhiteSpace(effect.DeltaText))
                        builder.Append("  (").Append(effect.DeltaText).Append(')');
                }
            }

            return builder.ToString();
        }

        private static string BuildNodeEyebrow(HeartGraphNodePresentation node)
        {
            return node.Branch == HeartNodeBranch.HeartMagic
                ? "HEART MAGIC"
                : GetBranchName(node.Branch);
        }

        private static string BuildNodeStatus(HeartGraphNodePresentation node)
        {
            if (node.Level > 0 && node.Type != HeartNodeType.Repeatable)
                return "AWAKENED";
            if (node.Type == HeartNodeType.Repeatable)
                return node.Level > 0
                    ? $"REPEATABLE  /  LEVEL {node.Level}"
                    : "REPEATABLE SINK";
            if (node.Type == HeartNodeType.Evolution)
                return "RARE  /  EVOLUTION";
            return node.Type?.ToString().ToUpperInvariant() ?? "AVAILABLE";
        }

        private static string GetPurchaseAction(HeartNodeType? type)
        {
            return type switch
            {
                HeartNodeType.Unlock => "UNLOCK",
                HeartNodeType.Repeatable => "DEEPEN",
                HeartNodeType.Evolution => "EVOLVE",
                HeartNodeType.Keystone => "COMMIT",
                _ => "AWAKEN"
            };
        }

        private void ApplyScreenPolish()
        {
            if (HeartPanel == null)
                return;

            Image panelImage = HeartPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.018f, 0.014f, 0.026f, 0.985f);

            TMP_Text title = FindDeep<TMP_Text>(HeartPanel.transform,
                "CastleHeartTitleText", "TechTreeTitleText");
            if (title != null)
            {
                title.text = "CASTLE HEART";
                title.fontStyle = FontStyles.Bold;
                title.characterSpacing = 4f;
                title.color = new Color(0.96f, 0.88f, 0.72f, 1f);
            }

            if (GraveEssenceText != null)
            {
                GraveEssenceText.fontStyle = FontStyles.Bold;
                GraveEssenceText.characterSpacing = 1.5f;
                GraveEssenceText.color = new Color(0.91f, 0.75f, 0.35f, 1f);
            }
            if (ScreenStatusText != null)
            {
                ScreenStatusText.fontStyle = FontStyles.UpperCase;
                ScreenStatusText.characterSpacing = 1.2f;
            }
            if (BranchCompassText != null)
            {
                BranchCompassText.fontStyle = FontStyles.Bold;
                BranchCompassText.characterSpacing = 0.8f;
            }

            if (HeartViewport != null)
            {
                Image viewportImage = HeartViewport.GetComponent<Image>();
                if (viewportImage != null)
                    viewportImage.color = new Color(0.035f, 0.028f, 0.050f, 0.72f);
            }

            Image topRule = EnsureDecorationImage(HeartPanel.transform, "HeartTopRule");
            RectTransform topRect = topRule.rectTransform;
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.offsetMin = new Vector2(24f, -72f);
            topRect.offsetMax = new Vector2(-24f, -68f);
            topRule.color = new Color(0.78f, 0.43f, 0.22f, 0.82f);
            topRule.transform.SetAsFirstSibling();

            Image bottomRule = EnsureDecorationImage(HeartPanel.transform, "HeartBottomRule");
            RectTransform bottomRect = bottomRule.rectTransform;
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0f);
            bottomRect.offsetMin = new Vector2(24f, 58f);
            bottomRect.offsetMax = new Vector2(-24f, 61f);
            bottomRule.color = new Color(0.42f, 0.27f, 0.48f, 0.70f);
            bottomRule.transform.SetAsFirstSibling();
        }

        private void UpdateBranchAxes(int maxDepth)
        {
            if (HeartContent == null)
                return;

            HeartNodeBranch[] branches =
            {
                HeartNodeBranch.Army,
                HeartNodeBranch.Defense,
                HeartNodeBranch.Production,
                HeartNodeBranch.HeartMagic
            };

            for (int i = 0; i < branches.Length; i++)
            {
                HeartNodeBranch branch = branches[i];
                if (!_branchAxes.TryGetValue(branch, out Image axis) || axis == null)
                {
                    axis = EnsureDecorationImage(HeartContent, "HeartAxis_" + branch);
                    _branchAxes[branch] = axis;
                }

                bool horizontal = branch == HeartNodeBranch.Army
                                  || branch == HeartNodeBranch.Defense;
                float spacing = horizontal ? HorizontalSpacing : VerticalSpacing;
                float length = Mathf.Max(spacing, maxDepth * spacing);
                float sign = branch == HeartNodeBranch.Army
                             || branch == HeartNodeBranch.Production ? 1f : -1f;
                RectTransform rect = axis.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = horizontal
                    ? new Vector2(sign * length * 0.5f, 0f)
                    : new Vector2(0f, sign * length * 0.5f);
                rect.sizeDelta = horizontal
                    ? new Vector2(length, 4f)
                    : new Vector2(4f, length);
                Color color = GetBranchColor(branch);
                color.a = 0.13f;
                axis.color = color;
                axis.transform.SetAsFirstSibling();
            }
        }

        private static Image EnsureDecorationImage(Transform parent, string name)
        {
            Transform existing = FindTransform(parent, name);
            if (existing != null && existing.TryGetComponent(out Image existingImage))
            {
                existingImage.raycastTarget = false;
                return existingImage;
            }

            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            var image = root.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void SetNodeChrome(
            NodeView view,
            Color accentColor,
            bool rare,
            Color outlineColor,
            bool emphasized,
            bool hidden = false)
        {
            if (view.Accent != null)
            {
                RectTransform rect = view.Accent.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = new Vector2(6f, 0f);
                accentColor.a = hidden ? 0.28f : 0.94f;
                view.Accent.color = accentColor;
                view.Accent.transform.SetAsFirstSibling();
            }

            if (view.RarityMarker != null)
            {
                RectTransform rect = view.RarityMarker.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-8f, -8f);
                rect.sizeDelta = new Vector2(10f, 10f);
                view.RarityMarker.color = new Color(1f, 0.76f, 0.28f, 0.98f);
                view.RarityMarker.gameObject.SetActive(rare && !hidden);
                view.RarityMarker.transform.SetAsLastSibling();
            }

            if (view.Outline != null)
            {
                outlineColor.a = hidden ? 0.22f : outlineColor.a;
                view.Outline.effectColor = outlineColor;
                view.Outline.effectDistance = emphasized
                    ? new Vector2(2.2f, -2.2f)
                    : new Vector2(1.2f, -1.2f);
                view.Outline.useGraphicAlpha = true;
            }

            view.Rect.sizeDelta = string.Equals(
                view.SlotId, HeartGraphSlotUtility.RootSlotId, StringComparison.Ordinal)
                    ? NodeSize * 1.08f
                    : NodeSize;
        }

        private static void StylePurchaseButton(
            NodeView view,
            Color branchColor,
            bool canPurchase,
            bool alreadyOwned)
        {
            if (view.BuyButton == null)
                return;

            Color baseColor = alreadyOwned
                ? new Color(0.30f, 0.24f, 0.15f, 0.96f)
                : canPurchase
                    ? Color.Lerp(new Color(0.12f, 0.10f, 0.16f, 1f), branchColor, 0.72f)
                    : new Color(0.12f, 0.13f, 0.16f, 0.90f);
            if (view.BuyButton.targetGraphic is Image image)
                image.color = baseColor;

            ColorBlock colors = view.BuyButton.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.11f, 0.12f, 0.14f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            view.BuyButton.colors = colors;
        }

        private static void ConfigureNodeLayout(NodeView view)
        {
            SetRect(view.Title, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(54f, -38f), new Vector2(-10f, -8f));
            SetRect(view.Level, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-92f, -58f), new Vector2(-10f, -40f));
            SetRect(view.Description, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(12f, -111f), new Vector2(-12f, -59f));
            SetRect(view.Status, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(12f, 38f), new Vector2(-12f, 58f));
            SetRect(view.Cost, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(12f, 12f), new Vector2(-100f, 36f));
            if (view.Title != null)
            {
                view.Title.fontSize = 15f;
                view.Title.fontStyle = FontStyles.Bold;
                view.Title.characterSpacing = 0.6f;
                view.Title.color = new Color(0.98f, 0.96f, 0.91f, 1f);
            }
            if (view.Level != null)
            {
                view.Level.fontSize = 9.5f;
                view.Level.fontStyle = FontStyles.Bold;
                view.Level.color = new Color(0.80f, 0.82f, 0.86f, 1f);
            }
            if (view.Description != null)
            {
                view.Description.fontSize = 9.6f;
                view.Description.lineSpacing = -5f;
                view.Description.color = new Color(0.85f, 0.86f, 0.88f, 1f);
            }
            if (view.Cost != null)
            {
                view.Cost.fontSize = 10f;
                view.Cost.fontStyle = FontStyles.Bold;
                view.Cost.color = new Color(0.96f, 0.78f, 0.36f, 1f);
            }
            if (view.Status != null)
            {
                view.Status.fontSize = 9f;
                view.Status.fontStyle = FontStyles.Bold;
                view.Status.characterSpacing = 0.8f;
                view.Status.color = new Color(0.72f, 0.75f, 0.80f, 1f);
            }
            if (view.BuyButton != null)
            {
                RectTransform rect = (RectTransform)view.BuyButton.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-49f, 23f);
                rect.sizeDelta = new Vector2(86f, 34f);
                if (view.BuyButtonText != null)
                {
                    view.BuyButtonText.fontSize = 10f;
                    view.BuyButtonText.fontStyle = FontStyles.Bold;
                    view.BuyButtonText.characterSpacing = 0.7f;
                }
            }
        }

        private static void SetRect(
            TMP_Text text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (text == null)
                return;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void FocusRoot()
        {
            if (HeartContent == null)
                return;
            TechTreeViewController navigation = HeartViewport != null
                ? HeartViewport.GetComponent<TechTreeViewController>()
                : null;
            navigation?.ResetZoom();
            HeartContent.anchoredPosition = Vector2.zero;
        }

        private void RefreshQuantityButtons()
        {
            RefreshQuantityButton(QuantityOneButton, HeartPurchaseQuantity.One, "+1");
            RefreshQuantityButton(QuantityTenButton, HeartPurchaseQuantity.Ten, "+10");
            RefreshQuantityButton(QuantityMaxButton, HeartPurchaseQuantity.BuyMax, "MAX");
        }

        private void RefreshQuantityButton(
            Button button,
            HeartPurchaseQuantity quantity,
            string label)
        {
            if (button == null)
                return;
            SetButtonLabel(button, label);
            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = _selectedQuantity == quantity
                    ? new Color(0.72f, 0.18f, 0.22f, 1f)
                    : new Color(0.12f, 0.15f, 0.19f, 0.96f);
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound)
                return;
            _buttonsBound = true;
            HeartOpenButton?.onClick.AddListener(OpenPanelFromPlayer);
            HeartCloseButton?.onClick.AddListener(ClosePanelFromPlayer);
            QuantityOneButton?.onClick.AddListener(SelectQuantityOne);
            QuantityTenButton?.onClick.AddListener(SelectQuantityTen);
            QuantityMaxButton?.onClick.AddListener(SelectQuantityMax);
        }

        private void UnbindButtons()
        {
            _buttonsBound = false;
            HeartOpenButton?.onClick.RemoveListener(OpenPanelFromPlayer);
            HeartCloseButton?.onClick.RemoveListener(ClosePanelFromPlayer);
            QuantityOneButton?.onClick.RemoveListener(SelectQuantityOne);
            QuantityTenButton?.onClick.RemoveListener(SelectQuantityTen);
            QuantityMaxButton?.onClick.RemoveListener(SelectQuantityMax);
        }

        private void ReleasePause()
        {
            _pauseLease?.Dispose();
            _pauseLease = null;
        }

        private void ShowToast(string message, bool success)
        {
            if (ToastText == null)
                return;
            _toastSequence?.Kill();
            ToastText.text = message;
            ToastText.color = success
                ? new Color(0.86f, 0.72f, 0.3f, 1f)
                : new Color(0.95f, 0.34f, 0.3f, 1f);
            ToastText.alpha = 0f;
            _toastSequence = DOTween.Sequence().SetUpdate(true)
                .Append(ToastText.DOFade(1f, 0.12f))
                .AppendInterval(1.15f)
                .Append(ToastText.DOFade(0f, 0.24f))
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

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
            }
            _audio.PlayOneShot(clip, volume);
        }

        private void SetScreenStatus(string value, bool error)
        {
            if (ScreenStatusText == null)
                return;
            ScreenStatusText.text = value;
            ScreenStatusText.color = error
                ? new Color(0.95f, 0.34f, 0.3f, 1f)
                : new Color(0.65f, 0.69f, 0.74f, 1f);
        }

        private void ClearGraph()
        {
            foreach (NodeView view in _nodeViews.Values)
            {
                view.Rect?.DOKill();
                Destroy(view.Root);
            }
            foreach (ConnectionView view in _connectionViews.Values)
                Destroy(view.Root);
            foreach (TMP_Text label in _keystoneChoiceLabels.Values)
                Destroy(label.transform.parent.gameObject);
            _nodeViews.Clear();
            _connectionViews.Clear();
            _keystoneChoiceLabels.Clear();
            _presentationsBySlot.Clear();
        }

        private void KillOwnedTweens()
        {
            _toastSequence?.Kill();
            _toastSequence = null;
            ToastText?.DOKill();

            if (HeartPanel != null)
            {
                CanvasGroup group = HeartPanel.GetComponent<CanvasGroup>();
                group?.DOKill();
            }

            foreach (NodeView view in _nodeViews.Values)
            {
                if (view?.Rect == null)
                    continue;

                view.Rect.DOKill();
                view.Rect.localScale = Vector3.one;
            }
        }

        private bool HasOwnedTweenActivity()
        {
            if (_toastSequence != null && _toastSequence.IsActive())
                return true;

            if (HeartPanel != null)
            {
                CanvasGroup group = HeartPanel.GetComponent<CanvasGroup>();
                if (group != null && DOTween.IsTweening(group))
                    return true;
            }

            foreach (NodeView view in _nodeViews.Values)
            {
                if (view?.Rect != null && DOTween.IsTweening(view.Rect))
                    return true;
            }

            return false;
        }

        private void SyncKeystoneChoiceLabels()
        {
            var livePairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeartGraphNodePresentation node in _presentationsBySlot.Values)
            {
                if (!TryGetCanonicalVisibleKeystonePair(
                        node,
                        out HeartGraphNodePresentation partner,
                        out string pairKey))
                {
                    continue;
                }

                livePairs.Add(pairKey);
                TMP_Text label = GetOrCreateKeystoneChoiceLabel(pairKey);
                Vector2 firstPosition = _nodeViews[node.SlotId].Rect.anchoredPosition;
                Vector2 secondPosition = _nodeViews[partner.SlotId].Rect.anchoredPosition;
                bool horizontalBranch = node.Branch == HeartNodeBranch.Army
                                        || node.Branch == HeartNodeBranch.Defense;
                RectTransform labelRoot = (RectTransform)label.transform.parent;
                labelRoot.anchoredPosition = (firstPosition + secondPosition) * 0.5f
                                             + (horizontalBranch
                                                 ? Vector2.zero
                                                 : Vector2.up * (NodeSize.y * 0.66f));
                label.text = "CHOOSE ONE  ·  RUN COMMITMENT";
            }

            var stale = new List<string>();
            foreach (KeyValuePair<string, TMP_Text> pair in _keystoneChoiceLabels)
            {
                if (!livePairs.Contains(pair.Key))
                    stale.Add(pair.Key);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                Destroy(_keystoneChoiceLabels[stale[i]].transform.parent.gameObject);
                _keystoneChoiceLabels.Remove(stale[i]);
            }
        }

        private TMP_Text GetOrCreateKeystoneChoiceLabel(string pairKey)
        {
            if (_keystoneChoiceLabels.TryGetValue(pairKey, out TMP_Text existing))
                return existing;

            var root = new GameObject(
                "HeartKeystoneChoice_" + SanitizeSlot(pairKey),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(HeartContent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(410f, 42f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.055f, 0.035f, 0.07f, 0.96f);
            background.raycastTarget = false;
            Outline backgroundOutline = root.AddComponent<Outline>();
            backgroundOutline.effectColor = new Color(0.96f, 0.58f, 0.14f, 0.82f);
            backgroundOutline.effectDistance = new Vector2(2f, -2f);

            var textRoot = new GameObject(
                "KeystoneChoiceText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textRoot.transform.SetParent(root.transform, false);
            var label = textRoot.GetComponent<TextMeshProUGUI>();
            RectTransform textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 17f;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 1.2f;
            label.color = KeystoneColor;
            label.outlineColor = new Color(0.05f, 0.03f, 0.02f, 0.95f);
            label.outlineWidth = 0.22f;
            label.raycastTarget = false;
            _keystoneChoiceLabels.Add(pairKey, label);
            return label;
        }

        private bool TryGetCanonicalVisibleKeystonePair(
            HeartGraphNodePresentation node,
            out HeartGraphNodePresentation partner,
            out string pairKey)
        {
            pairKey = string.Empty;
            if (!TryGetVisibleKeystonePartner(node, out partner)
                || string.CompareOrdinal(node.SlotId, partner.SlotId) >= 0)
            {
                return false;
            }

            pairKey = node.SlotId + "|" + partner.SlotId;
            return true;
        }

        private bool TryGetVisibleKeystonePartner(
            HeartGraphNodePresentation node,
            out HeartGraphNodePresentation partner)
        {
            partner = null;
            return node != null
                   && node.IsExactContentVisible
                   && node.Type == HeartNodeType.Keystone
                   && node.KeystoneConflict != null
                   && !string.IsNullOrWhiteSpace(node.KeystoneConflict.ConflictingChoiceSlotId)
                   && _presentationsBySlot.TryGetValue(
                       node.KeystoneConflict.ConflictingChoiceSlotId,
                       out partner)
                   && partner.IsExactContentVisible
                   && partner.Type == HeartNodeType.Keystone;
        }

        private bool IsKeystonePair(string fromSlotId, string toSlotId)
        {
            return _presentationsBySlot.TryGetValue(fromSlotId, out HeartGraphNodePresentation from)
                   && TryGetVisibleKeystonePartner(from, out HeartGraphNodePresentation partner)
                   && string.Equals(partner.SlotId, toSlotId, StringComparison.Ordinal);
        }

        private void RemoveStaleNodes(
            Dictionary<string, HeartGraphNodePresentation> liveNodes)
        {
            var stale = new List<string>();
            foreach (KeyValuePair<string, NodeView> pair in _nodeViews)
            {
                if (!liveNodes.ContainsKey(pair.Key))
                    stale.Add(pair.Key);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                NodeView view = _nodeViews[stale[i]];
                view.Rect?.DOKill();
                Destroy(view.Root);
                _nodeViews.Remove(stale[i]);
            }
        }

        private void RemoveStaleConnections(HashSet<string> liveConnections)
        {
            var stale = new List<string>();
            foreach (KeyValuePair<string, ConnectionView> pair in _connectionViews)
            {
                if (!liveConnections.Contains(pair.Key))
                    stale.Add(pair.Key);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                Destroy(_connectionViews[stale[i]].Root);
                _connectionViews.Remove(stale[i]);
            }
        }

        private bool IsHiddenSlot(string slotId)
        {
            return _presentationsBySlot.TryGetValue(slotId, out HeartGraphNodePresentation node)
                   && !node.IsExactContentVisible;
        }

        private static T FindDeep<T>(Transform root, params string[] names) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex].name == names[nameIndex])
                        return components[componentIndex];
                }
            }
            return null;
        }

        private static void SetIcon(NodeView view, Sprite sprite, string fallback, Color color)
        {
            if (view.Icon != null)
            {
                view.Icon.sprite = sprite;
                view.Icon.color = sprite != null ? Color.white : color;
            }
            if (view.IconFallback != null)
            {
                view.IconFallback.gameObject.SetActive(sprite == null);
                view.IconFallback.text = fallback;
            }
        }

        private static void SetBackground(NodeView view, Color color)
        {
            if (view.Background != null)
                view.Background.color = color;
        }

        private static void SetButton(NodeView view, bool visible, bool interactable, string label)
        {
            if (view.BuyButton == null)
                return;
            view.BuyButton.gameObject.SetActive(visible);
            view.BuyButton.interactable = interactable;
            SetText(view.BuyButtonText, label);
        }

        private static void SetButtonLabel(Button button, string value)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = value;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetOptionalChildActive(Transform root, string name, bool active)
        {
            Transform child = FindTransform(root, name);
            if (child != null)
                child.gameObject.SetActive(active);
        }

        private static Transform FindTransform(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i];
            }
            return null;
        }

        private static Color GetBranchColor(HeartNodeBranch branch)
        {
            switch (branch)
            {
                case HeartNodeBranch.Army: return ArmyColor;
                case HeartNodeBranch.Defense: return DefenseColor;
                case HeartNodeBranch.Production: return ProductionColor;
                case HeartNodeBranch.HeartMagic: return MagicColor;
                default: return Color.white;
            }
        }

        private static string GetBranchName(HeartNodeBranch branch)
        {
            return branch == HeartNodeBranch.HeartMagic
                ? "HEART / MAGIC"
                : branch.ToString().ToUpperInvariant();
        }

        private static string BuildFallback(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "?";
            string[] words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
                return words[0].Substring(0, 1).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }

        private static string SanitizeSlot(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "unknown"
                : value.Replace(':', '_').Replace('>', '_').Replace('/', '_');
        }

        private static string FormatLong(long value)
        {
            if (value < 1000L)
                return value.ToString();
            if (value < 1_000_000L)
                return (value / 1000d).ToString("0.##") + "K";
            if (value < 1_000_000_000L)
                return (value / 1_000_000d).ToString("0.##") + "M";
            if (value < 1_000_000_000_000L)
                return (value / 1_000_000_000d).ToString("0.##") + "B";
            return (value / 1_000_000_000_000d).ToString("0.##") + "T";
        }
    }
}
