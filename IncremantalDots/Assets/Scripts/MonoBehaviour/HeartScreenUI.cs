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
            int depth,
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
        public Vector2 NodeSize = new Vector2(268f, 172f);
        public float HorizontalSpacing = 350f;
        public float VerticalSpacing = 250f;
        public Vector2 ContentPadding = new Vector2(170f, 140f);

        private const float RefreshInterval = 0.15f;
        private const float BadgeInterval = 0.5f;

        private static readonly Color ArmyColor = new Color(0.86f, 0.25f, 0.22f, 1f);
        private static readonly Color DefenseColor = new Color(0.25f, 0.55f, 0.92f, 1f);
        private static readonly Color ProductionColor = new Color(0.27f, 0.76f, 0.44f, 1f);
        private static readonly Color MagicColor = new Color(0.68f, 0.34f, 0.94f, 1f);
        private static readonly Color HiddenColor = new Color(0.075f, 0.09f, 0.12f, 0.94f);
        private static readonly Color RootColor = new Color(0.76f, 0.18f, 0.22f, 0.98f);
        private static readonly Color LockedColor = new Color(0.25f, 0.27f, 0.31f, 0.96f);

        private readonly Dictionary<string, NodeView> _nodeViews =
            new Dictionary<string, NodeView>(StringComparer.Ordinal);
        private readonly Dictionary<string, ConnectionView> _connectionViews =
            new Dictionary<string, ConnectionView>(StringComparer.Ordinal);
        private readonly Dictionary<string, HeartGraphNodePresentation> _presentationsBySlot =
            new Dictionary<string, HeartGraphNodePresentation>(StringComparer.Ordinal);

        private HeartPurchaseQuantity _selectedQuantity = HeartPurchaseQuantity.One;
        private IDisposable _pauseLease;
        private float _nextRefreshTime;
        private float _nextBadgeTime;
        private AudioSource _audio;
        private Sequence _toastSequence;
        private bool _buttonsBound;

        public bool IsOpen => HeartPanel != null && HeartPanel.activeSelf;
        public HeartPurchaseQuantity SelectedQuantity => _selectedQuantity;

        public event Action HeartOpenedByPlayer;
        public event Action HeartClosedByPlayer;

        private IHeartScreenRuntime Runtime => GameManager.Instance;

        private sealed class NodeView
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Background;
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
            _toastSequence?.Kill();
            _toastSequence = null;
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
                group.DOFade(1f, 0.14f).SetUpdate(true);
            }
        }

        public void ClosePanel()
        {
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
                    "<color=#D95C54>ARMY  ></color>    "
                    + "<color=#6EA8D9><  DEFENSE</color>    "
                    + "<color=#D5A548>^  PRODUCTION</color>    "
                    + "<color=#A477CF>HEART / MAGIC  v</color>";

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

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                NodeView view = GetOrCreateNode(node.SlotId);
                Vector2 position = node.IsRoot
                    ? Vector2.zero
                    : HeartGraphLayoutUtility.GetPosition(
                        node.Branch,
                        node.Depth,
                        HorizontalSpacing,
                        VerticalSpacing);
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
                UpdateConnection(
                    connection,
                    from.Rect.anchoredPosition,
                    to.Rect.anchoredPosition,
                    GetBranchColor(edge.ToBranch),
                    IsHiddenSlot(edge.ToSlotId));
            }
            RemoveStaleConnections(liveConnections);
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
            rect.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
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
                SetText(view.Description, "The run's living progression core.");
                SetText(view.Cost, $"{FormatLong(runtime.GraveEssenceAmount)} GE AVAILABLE");
                SetText(view.Status, "FOUR PATHS • ONE RUN");
                SetIcon(view, null, "♥", RootColor);
                SetButton(view, false, false, string.Empty);
                SetBackground(view, RootColor);
                return;
            }

            if (!node.IsExactContentVisible)
            {
                SetText(view.Title, "VEILED");
                SetText(view.Level, $"DEPTH {node.Depth}");
                SetText(view.Description, "Purchase the connected node to reveal this choice.");
                SetText(view.Cost, string.Empty);
                SetText(view.Status, GetBranchName(node.Branch));
                SetIcon(view, null, "?", branchColor);
                SetButton(view, false, false, string.Empty);
                SetBackground(view, HiddenColor);
                return;
            }

            SetText(view.Title, string.IsNullOrWhiteSpace(node.Title) ? "UNNAMED" : node.Title.ToUpperInvariant());
            SetText(view.Level, node.Type == HeartNodeType.Repeatable
                ? $"LEVEL {node.Level}"
                : node.Type?.ToString().ToUpperInvariant() ?? string.Empty);
            SetText(view.Description, BuildDescription(node));
            SetIcon(view, node.Icon, BuildFallback(node.Title), branchColor);

            if (node.LockState == HeartNodeLockState.KeystoneConflict)
            {
                SetText(view.Cost, "LOCKED BY KEYSTONE");
                SetText(view.Status, "PATH SEALED");
                SetButton(view, true, false, "LOCKED");
                SetBackground(view, LockedColor);
                return;
            }

            HeartPurchaseQuantity quantity = node.Type == HeartNodeType.Repeatable
                ? _selectedQuantity
                : HeartPurchaseQuantity.One;
            HeartPurchaseEvaluation evaluation = runtime.EvaluateHeartPurchase(
                node.ExactNodeId,
                quantity);
            bool canPurchase = evaluation != null
                               && evaluation.CanPurchase
                               && node.EffectInformationComplete;
            HeartPurchaseQuote quote = evaluation?.Quote;
            if (quote != null)
            {
                SetText(view.Cost,
                    $"{FormatLong(quote.TotalGraveEssenceCost)} GE  •  +{quote.LevelsToBuy}");
            }
            else
            {
                SetText(view.Cost, evaluation?.Message ?? string.Empty);
            }

            string status = node.Type == HeartNodeType.Repeatable
                ? GetBranchName(node.Branch)
                : node.Level > 0 ? "OWNED" : GetBranchName(node.Branch);
            if (node.KeystoneConflict != null)
            {
                status = node.KeystoneConflict.WillLockOnPurchase
                    ? $"LOCKS {node.KeystoneConflict.ConflictingChoiceTitle}"
                    : status;
            }
            SetText(view.Status, status);

            bool alreadyOwned = node.Type != HeartNodeType.Repeatable && node.Level > 0;
            SetButton(
                view,
                true,
                canPurchase,
                alreadyOwned ? "OWNED" : canPurchase ? "PURCHASE" : "UNAVAILABLE");
            SetBackground(view, Color.Lerp(HiddenColor, branchColor, node.Level > 0 ? 0.48f : 0.28f));
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
                $"{node.Title.ToUpperInvariant()}  +{result.Quote.LevelsToBuy}",
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
            bool hidden)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            view.Rect.anchorMin = view.Rect.anchorMax = view.Rect.pivot = new Vector2(0.5f, 0.5f);
            view.Rect.anchoredPosition = (start + end) * 0.5f;
            view.Rect.sizeDelta = new Vector2(length, hidden ? 3f : 5f);
            view.Rect.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            if (view.Image != null)
            {
                color.a = hidden ? 0.24f : 0.72f;
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
                builder.Append(effect.Label);
                if (!string.IsNullOrWhiteSpace(effect.CurrentValueText))
                {
                    builder.Append(": ")
                        .Append(effect.CurrentValueText)
                        .Append(" → ")
                        .Append(effect.AfterPurchaseValueText);
                    if (!string.IsNullOrWhiteSpace(effect.DeltaText))
                        builder.Append("  (").Append(effect.DeltaText).Append(')');
                }
            }

            if (node.KeystoneConflict != null
                && !string.IsNullOrWhiteSpace(node.KeystoneConflict.ConflictingChoiceTitle))
            {
                if (builder.Length > 0)
                    builder.AppendLine();
                builder.Append("Choosing this seals ")
                    .Append(node.KeystoneConflict.ConflictingChoiceTitle)
                    .Append('.');
            }
            return builder.ToString();
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
            if (view.BuyButton != null)
            {
                RectTransform rect = (RectTransform)view.BuyButton.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-49f, 23f);
                rect.sizeDelta = new Vector2(86f, 34f);
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
                .Append(ToastText.DOFade(0f, 0.24f));
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
                Destroy(view.Root);
            foreach (ConnectionView view in _connectionViews.Values)
                Destroy(view.Root);
            _nodeViews.Clear();
            _connectionViews.Clear();
            _presentationsBySlot.Clear();
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
                Destroy(_nodeViews[stale[i]].Root);
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
