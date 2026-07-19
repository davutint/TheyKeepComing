using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public static class HeartConquestLayoutUtility
    {
        private static readonly Vector2[] ArmyOffsets =
        {
            new Vector2(142f, -52f),
            new Vector2(260f, -158f),
            new Vector2(405f, -112f),
            new Vector2(515f, -245f),
            new Vector2(650f, -175f)
        };

        private static readonly Vector2[] DefenseOffsets =
        {
            new Vector2(-146f, 48f),
            new Vector2(-272f, 158f),
            new Vector2(-400f, 95f),
            new Vector2(-520f, 230f),
            new Vector2(-652f, 150f)
        };

        private static readonly Vector2[] ProductionOffsets =
        {
            new Vector2(82f, 145f),
            new Vector2(202f, 255f),
            new Vector2(340f, 190f),
            new Vector2(460f, 310f),
            new Vector2(600f, 230f)
        };

        private static readonly Vector2[] HeartMagicOffsets =
        {
            new Vector2(-70f, -145f),
            new Vector2(-195f, -250f),
            new Vector2(-330f, -175f),
            new Vector2(-455f, -295f),
            new Vector2(-590f, -205f)
        };

        public static Vector2 GetPosition(HeartNodeBranch branch, int depth, Vector2 center)
        {
            if (depth <= 0)
                return center;

            Vector2[] offsets = branch switch
            {
                HeartNodeBranch.Army => ArmyOffsets,
                HeartNodeBranch.Defense => DefenseOffsets,
                HeartNodeBranch.Production => ProductionOffsets,
                HeartNodeBranch.HeartMagic => HeartMagicOffsets,
                _ => ArmyOffsets
            };

            int index = Mathf.Min(depth, offsets.Length) - 1;
            Vector2 offset = offsets[index];
            if (depth > offsets.Length)
            {
                Vector2 continuation = offsets[offsets.Length - 1] - offsets[offsets.Length - 2];
                offset += continuation * (depth - offsets.Length);
            }
            return center + offset;
        }
    }

    public static class HeartGraphNavigationUtility
    {
        public const float MinimumZoomMultiplier = 0.65f;
        public const float MaximumZoomMultiplier = 2.25f;

        public static float ClampZoomMultiplier(float multiplier)
        {
            return Mathf.Clamp(multiplier, MinimumZoomMultiplier, MaximumZoomMultiplier);
        }

        public static Vector2 CalculateAnchoredOffset(
            Vector2 currentOffset,
            float currentScale,
            float nextScale,
            Vector2 anchorBefore,
            Vector2 anchorAfter)
        {
            if (currentScale <= 0.001f || nextScale <= 0.001f)
                return currentOffset;

            Vector2 graphPoint = (anchorBefore - currentOffset) / currentScale;
            return anchorAfter - graphPoint * nextScale;
        }
    }

    public sealed partial class GameplayHUDToolkitUI
    {
        private const float HeartCanvasWidth = 1480f;
        private const float HeartCanvasHeight = 820f;
        private const float HeartRevealDuration = 0.34f;
        private const float HeartZoomStep = 1.16f;
        private const float HeartNavigationMargin = 36f;

        private VisualElement _heartViewport;
        private VisualElement _heartGraphContent;
        private HeartConnectorLayer _heartConnectorLayer;
        private Label _graveEssenceValue;
        private VisualElement _heartInspectorIcon;
        private Label _heartInspectorTitle;
        private Label _heartInspectorMeta;
        private Label _heartInspectorBody;
        private Label _heartInspectorStatus;
        private Label _heartInspectorCost;
        private Button _heartPurchaseButton;
        private Button _heartZoomOutButton;
        private Button _heartZoomResetButton;
        private Button _heartZoomInButton;
        private Label _heartZoomValue;
        private HeartGraphNodePresentation _selectedHeartNode;
        private HeartGraphPresentation _heartPresentation;
        private readonly Dictionary<string, Button> _heartNodeButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Vector2> _heartNodeCenters = new Dictionary<string, Vector2>();
        private readonly HashSet<string> _heartPendingRevealNodeIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<IVisualElementScheduledItem> _heartScheduledItems = new List<IVisualElementScheduledItem>();
        private readonly Dictionary<int, Vector2> _heartTouchPointers = new Dictionary<int, Vector2>();
        private int _heartGraphSignature;
        private AudioSource _heartAudioSource;
        private float _heartFitScale = 1f;
        private float _heartZoomMultiplier = 1f;
        private Vector2 _heartPanOffset;
        private Vector2 _heartAppliedOffset;
        private Vector2 _heartVisibleBoundsMin;
        private Vector2 _heartVisibleBoundsMax;
        private bool _heartLayoutReady;
        private bool _heartDragging;
        private int _heartDragPointer = -1;
        private Vector2 _heartDragStart;
        private Vector2 _heartPanStart;
        private bool _heartPinching;
        private float _heartPinchDistance;
        private Vector2 _heartPinchMidpoint;

        private void BindCastleHeartActions()
        {
            Q<Button>("heartClose").clicked += CloseSurface;
            _heartViewport = Q<VisualElement>("heartViewport");
            _heartGraphContent = Q<VisualElement>("heartGraphContent");
            _graveEssenceValue = Q<Label>("graveEssenceValue");
            _heartInspectorIcon = Q<VisualElement>("heartInspectorIcon");
            _heartInspectorTitle = Q<Label>("heartInspectorTitle");
            _heartInspectorMeta = Q<Label>("heartInspectorMeta");
            _heartInspectorBody = Q<Label>("heartInspectorBody");
            _heartInspectorStatus = Q<Label>("heartInspectorStatus");
            _heartInspectorCost = Q<Label>("heartInspectorCost");
            _heartPurchaseButton = Q<Button>("heartPurchase");
            _heartPurchaseButton.clicked += PurchaseSelectedHeartNode;
            _heartZoomOutButton = Q<Button>("heartZoomOut");
            _heartZoomResetButton = Q<Button>("heartZoomReset");
            _heartZoomInButton = Q<Button>("heartZoomIn");
            _heartZoomValue = Q<Label>("heartZoomValue");

            _heartZoomOutButton.clicked += () => ZoomHeartGraphBy(1f / HeartZoomStep);
            _heartZoomResetButton.clicked += ResetHeartGraphView;
            _heartZoomInButton.clicked += () => ZoomHeartGraphBy(HeartZoomStep);

            _heartViewport.RegisterCallback<WheelEvent>(OnHeartWheel);
            _heartViewport.RegisterCallback<PointerDownEvent>(OnHeartPointerDown, TrickleDown.TrickleDown);
            _heartViewport.RegisterCallback<PointerMoveEvent>(OnHeartPointerMove, TrickleDown.TrickleDown);
            _heartViewport.RegisterCallback<PointerUpEvent>(OnHeartPointerUp, TrickleDown.TrickleDown);
            _heartViewport.RegisterCallback<PointerCancelEvent>(OnHeartPointerCancel, TrickleDown.TrickleDown);
            UpdateHeartZoomControls();
        }

        private void RebuildHeartGraph(bool force)
        {
            GameManager gm = GameManager.Instance;
            if (_heartGraphContent == null || gm == null)
                return;

            if (!gm.TryBuildHeartPresentation(
                    out HeartGraphPresentation presentation,
                    out IReadOnlyList<string> errors)
                || presentation == null)
            {
                CancelHeartAnimations();
                _heartGraphContent.Clear();
                _heartInspectorTitle.text = "HEART UNAVAILABLE";
                _heartInspectorMeta.text = "RUNTIME ERROR";
                _heartInspectorBody.text = errors != null && errors.Count > 0
                    ? errors[0]
                    : gm.HeartRuntimeError;
                _heartInspectorStatus.text = "UNAVAILABLE";
                _heartInspectorCost.text = string.Empty;
                _heartPurchaseButton.SetEnabled(false);
                return;
            }

            int signature = BuildHeartSignature(presentation);
            _graveEssenceValue.text = $"{gm.GraveEssenceAmount:N0} ESSENCE";
            if (!force && signature == _heartGraphSignature)
            {
                RefreshHeartInspector(gm);
                return;
            }

            CancelHeartAnimations();
            _heartGraphSignature = signature;
            _heartPresentation = presentation;
            _heartGraphContent.Clear();
            _heartNodeButtons.Clear();
            _heartNodeCenters.Clear();
            _heartGraphContent.style.width = HeartCanvasWidth;
            _heartGraphContent.style.height = HeartCanvasHeight;

            var visibleBySlot = new Dictionary<string, HeartGraphNodePresentation>(StringComparer.Ordinal);
            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                if (node == null || !node.IsExactContentVisible)
                    continue;

                visibleBySlot[node.SlotId] = node;
                _heartNodeCenters[node.SlotId] = GetHeartNodeCenter(node);
            }

            ResolveHeartSelection(presentation);
            var revealSlots = ResolvePendingRevealSlots(presentation);

            _heartConnectorLayer = new HeartConnectorLayer();
            _heartConnectorLayer.AddToClassList("heart-connector-layer");
            _heartGraphContent.Add(_heartConnectorLayer);

            var connections = new List<HeartConnection>();
            for (int i = 0; i < presentation.Edges.Count; i++)
            {
                HeartGraphEdgePresentation edge = presentation.Edges[i];
                if (edge == null
                    || !visibleBySlot.TryGetValue(edge.FromSlotId, out HeartGraphNodePresentation fromNode)
                    || !visibleBySlot.ContainsKey(edge.ToSlotId)
                    || !_heartNodeCenters.TryGetValue(edge.FromSlotId, out Vector2 from)
                    || !_heartNodeCenters.TryGetValue(edge.ToSlotId, out Vector2 to))
                {
                    continue;
                }

                connections.Add(new HeartConnection
                {
                    From = from,
                    To = to,
                    FromSlotId = edge.FromSlotId,
                    ToSlotId = edge.ToSlotId,
                    Branch = edge.ToBranch,
                    IsCrossLink = !fromNode.IsRoot && edge.FromBranch != edge.ToBranch,
                    IsActive = fromNode.IsRoot || fromNode.Level > 0,
                    Progress = revealSlots.Contains(edge.ToSlotId) ? 0f : 1f
                });
            }
            _heartConnectorLayer.SetConnections(connections);

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                if (node == null || !node.IsExactContentVisible)
                    continue;

                Button button = CreateHeartNodeButton(node, gm, revealSlots.Contains(node.SlotId));
                Vector2 socketCenter = _heartNodeCenters[node.SlotId];
                float width = node.IsRoot ? 126f : 96f;
                button.style.left = socketCenter.x - width * 0.5f;
                button.style.top = node.IsRoot
                    ? socketCenter.y - 63f
                    : socketCenter.y - 31f;
                _heartGraphContent.Add(button);
                _heartNodeButtons[node.SlotId] = button;
            }

            RefreshHeartInspector(gm);
            ScheduleHeartRevealSequence(revealSlots);
            _heartPendingRevealNodeIds.Clear();
            _heartGraphContent.schedule.Execute(RelayoutHeartGraph);
        }

        private static int BuildHeartSignature(HeartGraphPresentation presentation)
        {
            int signature = presentation.Nodes.Count * 397 ^ presentation.Edges.Count;
            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                if (node == null)
                    continue;
                signature = signature * 31 + (node.SlotId?.GetHashCode() ?? 0);
                signature = signature * 31 + node.Level;
                signature = signature * 31 + (node.IsExactContentVisible ? 1 : 0);
            }
            return signature;
        }

        private void ResolveHeartSelection(HeartGraphPresentation presentation)
        {
            string selectedSlotId = _selectedHeartNode?.SlotId;
            _selectedHeartNode = null;
            if (!string.IsNullOrWhiteSpace(selectedSlotId))
            {
                for (int i = 0; i < presentation.Nodes.Count; i++)
                {
                    HeartGraphNodePresentation candidate = presentation.Nodes[i];
                    if (candidate != null
                        && candidate.IsExactContentVisible
                        && string.Equals(candidate.SlotId, selectedSlotId, StringComparison.Ordinal))
                    {
                        _selectedHeartNode = candidate;
                        return;
                    }
                }
            }

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation candidate = presentation.Nodes[i];
                if (candidate != null && candidate.IsExactContentVisible && !candidate.IsRoot)
                {
                    _selectedHeartNode = candidate;
                    return;
                }
            }

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation candidate = presentation.Nodes[i];
                if (candidate != null && candidate.IsExactContentVisible)
                {
                    _selectedHeartNode = candidate;
                    return;
                }
            }
        }

        private HashSet<string> ResolvePendingRevealSlots(HeartGraphPresentation presentation)
        {
            var slots = new HashSet<string>(StringComparer.Ordinal);
            if (_heartPendingRevealNodeIds.Count == 0)
                return slots;

            for (int i = 0; i < presentation.Nodes.Count; i++)
            {
                HeartGraphNodePresentation node = presentation.Nodes[i];
                if (node != null
                    && node.IsExactContentVisible
                    && !string.IsNullOrWhiteSpace(node.ExactNodeId)
                    && _heartPendingRevealNodeIds.Contains(node.ExactNodeId))
                {
                    slots.Add(node.SlotId);
                }
            }
            return slots;
        }

        private static Vector2 GetHeartNodeCenter(HeartGraphNodePresentation node)
        {
            Vector2 center = new Vector2(740f, 410f);
            if (node.IsRoot || node.Depth <= 0)
                return center;
            return HeartConquestLayoutUtility.GetPosition(node.Branch, node.Depth, center);
        }

        private Button CreateHeartNodeButton(
            HeartGraphNodePresentation node,
            GameManager gm,
            bool isRevealing)
        {
            Button button = new Button(() => SelectHeartNode(node));
            button.name = "heartNode-" + node.SlotId.Replace(':', '-');
            button.AddToClassList("heart-tech-node");
            button.AddToClassList("branch--" + node.Branch.ToString().ToLowerInvariant());
            button.EnableInClassList("is-root", node.IsRoot);
            button.EnableInClassList("is-owned", node.Level > 0);
            button.EnableInClassList("is-selected", _selectedHeartNode != null
                && string.Equals(_selectedHeartNode.SlotId, node.SlotId, StringComparison.Ordinal));
            button.EnableInClassList("is-revealing", isRevealing);

            VisualElement socket = new VisualElement();
            socket.AddToClassList("heart-node-socket");
            VisualElement icon = new VisualElement();
            icon.AddToClassList("heart-node-icon");
            icon.EnableInClassList("heart-node-icon--root", node.IsRoot);
            if (node.Icon != null)
                icon.style.backgroundImage = new StyleBackground(Background.FromSprite(node.Icon));
            socket.Add(icon);
            button.Add(socket);

            if (!node.IsRoot && node.Level > 0)
            {
                Label level = new Label(node.Type == HeartNodeType.Repeatable
                    ? node.Level.ToString()
                    : "✓");
                level.AddToClassList("heart-node-level");
                button.Add(level);
            }

            button.tooltip = node.IsRoot ? "Castle Heart" : node.Title;
            if (!node.IsRoot)
            {
                Label title = new Label(node.Title.ToUpperInvariant());
                title.AddToClassList("heart-node-title");
                button.Add(title);
            }

            if (!node.IsRoot && !string.IsNullOrWhiteSpace(node.ExactNodeId))
            {
                HeartPurchaseEvaluation evaluation = gm.EvaluateHeartPurchase(
                    node.ExactNodeId,
                    HeartPurchaseQuantity.One);
                button.EnableInClassList("is-available", evaluation != null && evaluation.CanPurchase);
            }
            return button;
        }

        private void SelectHeartNode(HeartGraphNodePresentation node)
        {
            if (_selectedHeartNode != null
                && _heartNodeButtons.TryGetValue(_selectedHeartNode.SlotId, out Button previous))
            {
                previous.RemoveFromClassList("is-selected");
            }

            _selectedHeartNode = node;
            if (node != null && _heartNodeButtons.TryGetValue(node.SlotId, out Button current))
                current.AddToClassList("is-selected");
            RefreshHeartInspector(GameManager.Instance);
        }

        private void RefreshHeartInspector(GameManager gm)
        {
            if (_heartInspectorTitle == null || gm == null)
                return;

            _graveEssenceValue.text = $"{gm.GraveEssenceAmount:N0} ESSENCE";
            HeartGraphNodePresentation node = _selectedHeartNode;
            if (node == null)
            {
                SetHeartInspectorIcon(null, false);
                _heartInspectorTitle.text = "CASTLE HEART";
                _heartInspectorMeta.text = "SELECT A TECHNOLOGY";
                _heartInspectorBody.text = "Select a revealed technology to inspect its effect.";
                _heartInspectorStatus.text = "NO SELECTION";
                _heartInspectorCost.text = string.Empty;
                _heartPurchaseButton.text = "RESEARCH";
                _heartPurchaseButton.SetEnabled(false);
                return;
            }

            SetHeartInspectorIcon(node.Icon, node.IsRoot);
            _heartInspectorTitle.text = node.IsRoot
                ? "CASTLE HEART"
                : node.Title.ToUpperInvariant();

            if (node.IsRoot || string.IsNullOrWhiteSpace(node.ExactNodeId))
            {
                _heartInspectorMeta.text = "ORIGIN";
                _heartInspectorBody.text = "The living core of the keep. Four paths answer its first pulse.";
                _heartInspectorStatus.text = "ACTIVE";
                _heartInspectorCost.text = "THE HEART'S ORIGIN";
                _heartPurchaseButton.text = "ORIGIN";
                _heartPurchaseButton.SetEnabled(false);
                return;
            }

            _heartInspectorMeta.text = node.Level > 0
                ? $"LEVEL {node.Level:N0}"
                : "UNRESEARCHED";
            _heartInspectorBody.text = BuildHeartInspectorBody(node);

            HeartPurchaseEvaluation evaluation = gm.EvaluateHeartPurchase(
                node.ExactNodeId,
                HeartPurchaseQuantity.One);
            bool repeatable = node.Type == HeartNodeType.Repeatable;
            bool completed = !repeatable && node.Level > 0;

            if (completed)
            {
                _heartInspectorStatus.text = "RESEARCHED";
                _heartInspectorCost.text = "COMPLETE";
                _heartPurchaseButton.text = "RESEARCHED";
                _heartPurchaseButton.SetEnabled(false);
                return;
            }

            _heartInspectorStatus.text = evaluation != null && evaluation.CanPurchase
                ? node.Level > 0 ? "UPGRADE AVAILABLE" : "AVAILABLE TO RESEARCH"
                : evaluation?.Message?.ToUpperInvariant() ?? "UNAVAILABLE";
            _heartInspectorCost.text = evaluation?.Quote != null
                ? $"{evaluation.Quote.TotalGraveEssenceCost:N0} GRAVE ESSENCE"
                : "COST UNAVAILABLE";
            _heartPurchaseButton.text = repeatable && node.Level > 0 ? "UPGRADE" : "RESEARCH";
            _heartPurchaseButton.SetEnabled(evaluation != null && evaluation.CanPurchase);
        }

        private void SetHeartInspectorIcon(Sprite icon, bool isRoot)
        {
            if (_heartInspectorIcon == null)
                return;

            _heartInspectorIcon.EnableInClassList("is-root", isRoot);
            _heartInspectorIcon.style.backgroundImage = icon != null
                ? new StyleBackground(Background.FromSprite(icon))
                : new StyleBackground(StyleKeyword.None);
        }

        private static string BuildHeartInspectorBody(HeartGraphNodePresentation node)
        {
            var body = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(node.Description))
                body.Append(node.Description.Trim());

            for (int i = 0; i < node.Effects.Count; i++)
            {
                HeartEffectPresentation effect = node.Effects[i];
                if (effect == null)
                    continue;

                if (body.Length > 0)
                    body.AppendLine().AppendLine();
                body.Append(effect.Label);
                if (!string.IsNullOrWhiteSpace(effect.CurrentValueText)
                    && !string.IsNullOrWhiteSpace(effect.AfterPurchaseValueText))
                {
                    body.AppendLine();
                    body.Append(effect.CurrentValueText)
                        .Append("  ->  ")
                        .Append(effect.AfterPurchaseValueText);
                }
                if (!string.IsNullOrWhiteSpace(effect.DeltaText))
                    body.Append("  ").Append(effect.DeltaText);
            }
            return body.ToString();
        }

        private void PurchaseSelectedHeartNode()
        {
            GameManager gm = GameManager.Instance;
            HeartGraphNodePresentation node = _selectedHeartNode;
            if (gm == null || node == null || string.IsNullOrWhiteSpace(node.ExactNodeId))
                return;

            HeartPurchaseResult result = gm.TryPurchaseHeartNode(
                node.ExactNodeId,
                HeartPurchaseQuantity.One);
            if (result == null || !result.Succeeded)
            {
                ResolveRuntimeOwners();
                PlayHeartSfx(_heartLegacy != null ? _heartLegacy.DeniedClip : null, 0.82f);
                ShowPrimaryToast(result?.Message ?? "HEART RESEARCH BLOCKED");
                RefreshHeartInspector(gm);
                return;
            }

            for (int i = 0; i < result.NewlyRevealedNodeIds.Count; i++)
                _heartPendingRevealNodeIds.Add(result.NewlyRevealedNodeIds[i]);

            ResolveRuntimeOwners();
            PlayHeartSfx(_heartLegacy != null ? _heartLegacy.BuyClip : null, 0.88f);
            if (result.NewlyRevealedNodeIds.Count > 0)
                PlayHeartSfx(_heartLegacy != null ? _heartLegacy.RevealClip : null, 0.68f);

            ShowPrimaryToast(node.Level > 0
                ? $"TECHNOLOGY UPGRADED  ·  LEVEL {result.Quote.NewLevel:N0}"
                : "TECHNOLOGY RESEARCHED");
            _heartGraphSignature = 0;
            RebuildHeartGraph(true);
        }

        private void PlayHeartSfx(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            if (_heartAudioSource == null)
            {
                _heartAudioSource = GetComponent<AudioSource>();
                if (_heartAudioSource == null)
                    _heartAudioSource = gameObject.AddComponent<AudioSource>();
                _heartAudioSource.playOnAwake = false;
            }
            _heartAudioSource.PlayOneShot(clip, volume * SoundSettings.SfxVolume);
        }

        private void ScheduleHeartRevealSequence(HashSet<string> revealSlots)
        {
            if (revealSlots.Count == 0 || _heartGraphContent == null)
                return;

            var orderedSlots = new List<string>(revealSlots);
            orderedSlots.Sort(StringComparer.Ordinal);
            for (int i = 0; i < orderedSlots.Count; i++)
            {
                string slotId = orderedSlots[i];
                long delay = 70L + i * 95L;
                IVisualElementScheduledItem edgeStart = _heartGraphContent.schedule
                    .Execute(() => AnimateHeartConnector(slotId))
                    .StartingIn(delay);
                _heartScheduledItems.Add(edgeStart);

                if (_heartNodeButtons.TryGetValue(slotId, out Button button))
                {
                    IVisualElementScheduledItem nodeStart = button.schedule
                        .Execute(() => button.RemoveFromClassList("is-revealing"))
                        .StartingIn(delay + 230L);
                    _heartScheduledItems.Add(nodeStart);
                }
            }
        }

        private void AnimateHeartConnector(string toSlotId)
        {
            if (_heartConnectorLayer == null || _heartGraphContent == null)
                return;

            float startedAt = Time.unscaledTime;
            IVisualElementScheduledItem tick = null;
            tick = _heartGraphContent.schedule.Execute(() =>
            {
                float progress = Mathf.Clamp01((Time.unscaledTime - startedAt) / HeartRevealDuration);
                _heartConnectorLayer?.SetProgress(toSlotId, progress);
                if (progress >= 1f)
                    tick?.Pause();
            }).Every(16L);
            _heartScheduledItems.Add(tick);
        }

        private void CancelHeartAnimations()
        {
            for (int i = 0; i < _heartScheduledItems.Count; i++)
                _heartScheduledItems[i]?.Pause();
            _heartScheduledItems.Clear();
        }

        private void OnHeartWheel(WheelEvent evt)
        {
            if (Mathf.Approximately(evt.delta.y, 0f))
                return;

            Vector2 anchor = _heartViewport.WorldToLocal(evt.mousePosition);
            float factor = evt.delta.y > 0f ? 1f / HeartZoomStep : HeartZoomStep;
            SetHeartZoom(_heartZoomMultiplier * factor, anchor, anchor);
            evt.StopPropagation();
        }

        private void ZoomHeartGraphBy(float factor)
        {
            if (!_heartLayoutReady)
                return;

            Vector2 anchor = GetHeartViewportCenter();
            SetHeartZoom(_heartZoomMultiplier * factor, anchor, anchor);
        }

        private void ResetHeartGraphView()
        {
            _heartZoomMultiplier = 1f;
            _heartPanOffset = Vector2.zero;
            ApplyHeartGraphTransform();
        }

        private void SetHeartZoom(float multiplier, Vector2 anchorBefore, Vector2 anchorAfter)
        {
            if (!_heartLayoutReady)
                return;

            float nextMultiplier = HeartGraphNavigationUtility.ClampZoomMultiplier(multiplier);
            float currentScale = _heartFitScale * _heartZoomMultiplier;
            float nextScale = _heartFitScale * nextMultiplier;
            Vector2 nextOffset = HeartGraphNavigationUtility.CalculateAnchoredOffset(
                _heartAppliedOffset,
                currentScale,
                nextScale,
                anchorBefore,
                anchorAfter);

            _heartZoomMultiplier = nextMultiplier;
            _heartPanOffset = nextOffset - GetHeartCenteredOffset(nextScale);
            ApplyHeartGraphTransform();
        }

        private void OnHeartPointerDown(PointerDownEvent evt)
        {
            Vector2 localPosition = _heartViewport.WorldToLocal(
                new Vector2(evt.position.x, evt.position.y));
            bool isTouch = string.Equals(
                evt.pointerType,
                UnityEngine.UIElements.PointerType.touch,
                StringComparison.Ordinal);
            if (isTouch)
            {
                _heartTouchPointers[evt.pointerId] = localPosition;
                if (_heartTouchPointers.Count >= 2)
                {
                    BeginHeartPinch();
                    evt.StopPropagation();
                    return;
                }
            }

            if ((evt.button != 0 && evt.button != 2)
                || IsInsideButton(evt.target as VisualElement, _heartViewport))
            {
                return;
            }

            _heartDragging = true;
            _heartDragPointer = evt.pointerId;
            _heartDragStart = new Vector2(evt.position.x, evt.position.y);
            _heartPanStart = _heartPanOffset;
            _heartViewport.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnHeartPointerMove(PointerMoveEvent evt)
        {
            bool isTouch = string.Equals(
                evt.pointerType,
                UnityEngine.UIElements.PointerType.touch,
                StringComparison.Ordinal);
            if (isTouch && _heartTouchPointers.ContainsKey(evt.pointerId))
            {
                _heartTouchPointers[evt.pointerId] = _heartViewport.WorldToLocal(
                    new Vector2(evt.position.x, evt.position.y));
                if (_heartPinching && TryGetHeartPinchPoints(out Vector2 first, out Vector2 second))
                {
                    float distance = Vector2.Distance(first, second);
                    Vector2 midpoint = (first + second) * 0.5f;
                    if (_heartPinchDistance > 1f && distance > 1f)
                    {
                        float ratio = distance / _heartPinchDistance;
                        SetHeartZoom(
                            _heartZoomMultiplier * ratio,
                            _heartPinchMidpoint,
                            midpoint);
                    }
                    _heartPinchDistance = distance;
                    _heartPinchMidpoint = midpoint;
                    evt.StopPropagation();
                    return;
                }
            }

            if (!_heartDragging || evt.pointerId != _heartDragPointer)
                return;

            Vector2 pointer = new Vector2(evt.position.x, evt.position.y);
            _heartPanOffset = _heartPanStart + pointer - _heartDragStart;
            ApplyHeartGraphTransform();
            evt.StopPropagation();
        }

        private void OnHeartPointerUp(PointerUpEvent evt)
        {
            bool handled = _heartPinching || (_heartDragging && evt.pointerId == _heartDragPointer);
            EndHeartDrag(evt.pointerId);
            if (string.Equals(
                    evt.pointerType,
                    UnityEngine.UIElements.PointerType.touch,
                    StringComparison.Ordinal))
            {
                _heartTouchPointers.Remove(evt.pointerId);
                if (_heartTouchPointers.Count < 2)
                    EndHeartPinch();
            }

            if (handled)
                evt.StopPropagation();
        }

        private void OnHeartPointerCancel(PointerCancelEvent evt)
        {
            EndHeartDrag(evt.pointerId);
            _heartTouchPointers.Remove(evt.pointerId);
            if (_heartTouchPointers.Count < 2)
                EndHeartPinch();
        }

        private void BeginHeartPinch()
        {
            EndHeartDrag(_heartDragPointer);
            if (!TryGetHeartPinchPoints(out Vector2 first, out Vector2 second))
                return;

            _heartPinching = true;
            _heartPinchDistance = Vector2.Distance(first, second);
            _heartPinchMidpoint = (first + second) * 0.5f;
            foreach (int pointerId in _heartTouchPointers.Keys)
            {
                if (!_heartViewport.HasPointerCapture(pointerId))
                    _heartViewport.CapturePointer(pointerId);
            }
        }

        private void EndHeartPinch()
        {
            if (!_heartPinching)
                return;

            _heartPinching = false;
            _heartPinchDistance = 0f;
            foreach (int pointerId in _heartTouchPointers.Keys)
            {
                if (_heartViewport.HasPointerCapture(pointerId))
                    _heartViewport.ReleasePointer(pointerId);
            }
        }

        private void EndHeartDrag(int pointerId)
        {
            if (!_heartDragging || pointerId != _heartDragPointer)
                return;

            if (_heartViewport.HasPointerCapture(pointerId))
                _heartViewport.ReleasePointer(pointerId);
            _heartDragging = false;
            _heartDragPointer = -1;
        }

        private void CancelHeartNavigationGestures()
        {
            EndHeartDrag(_heartDragPointer);
            EndHeartPinch();
            _heartTouchPointers.Clear();
        }

        private bool TryGetHeartPinchPoints(out Vector2 first, out Vector2 second)
        {
            first = Vector2.zero;
            second = Vector2.zero;
            int index = 0;
            foreach (Vector2 point in _heartTouchPointers.Values)
            {
                if (index == 0)
                    first = point;
                else if (index == 1)
                {
                    second = point;
                    return true;
                }
                index++;
            }
            return false;
        }

        private void RelayoutHeartGraph()
        {
            if (_heartViewport == null || _heartGraphContent == null)
                return;

            float viewportWidth = _heartViewport.resolvedStyle.width;
            float viewportHeight = _heartViewport.resolvedStyle.height;
            if (viewportWidth <= 1f || viewportHeight <= 1f)
                return;

            float minX = HeartCanvasWidth * 0.5f;
            float maxX = minX;
            float minY = HeartCanvasHeight * 0.5f;
            float maxY = minY;
            foreach (Vector2 center in _heartNodeCenters.Values)
            {
                minX = Mathf.Min(minX, center.x);
                maxX = Mathf.Max(maxX, center.x);
                minY = Mathf.Min(minY, center.y);
                maxY = Mathf.Max(maxY, center.y);
            }

            float visibleWidth = maxX - minX + 210f;
            float visibleHeight = maxY - minY + 190f;
            _heartFitScale = Mathf.Min(viewportWidth / visibleWidth, viewportHeight / visibleHeight);
            _heartFitScale = Mathf.Clamp(_heartFitScale, 0.48f, 1.22f);
            _heartVisibleBoundsMin = new Vector2(minX - 105f, minY - 95f);
            _heartVisibleBoundsMax = new Vector2(maxX + 105f, maxY + 95f);
            _heartLayoutReady = true;
            ApplyHeartGraphTransform();
        }

        private void ApplyHeartGraphTransform()
        {
            if (!_heartLayoutReady || _heartViewport == null || _heartGraphContent == null)
                return;

            float scale = _heartFitScale * _heartZoomMultiplier;
            Vector2 offset = GetHeartCenteredOffset(scale) + _heartPanOffset;
            offset = ClampHeartGraphOffset(offset, scale);
            _heartPanOffset = offset - GetHeartCenteredOffset(scale);
            _heartAppliedOffset = offset;

            _heartGraphContent.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _heartGraphContent.style.left = offset.x;
            _heartGraphContent.style.top = offset.y;
            UpdateHeartZoomControls();
        }

        private Vector2 GetHeartCenteredOffset(float scale)
        {
            Vector2 viewportSize = new Vector2(
                _heartViewport.resolvedStyle.width,
                _heartViewport.resolvedStyle.height);
            Vector2 visibleCenter = (_heartVisibleBoundsMin + _heartVisibleBoundsMax) * 0.5f;
            return viewportSize * 0.5f - visibleCenter * scale;
        }

        private Vector2 ClampHeartGraphOffset(Vector2 offset, float scale)
        {
            float viewportWidth = _heartViewport.resolvedStyle.width;
            float viewportHeight = _heartViewport.resolvedStyle.height;
            float scaledWidth = (_heartVisibleBoundsMax.x - _heartVisibleBoundsMin.x) * scale;
            float scaledHeight = (_heartVisibleBoundsMax.y - _heartVisibleBoundsMin.y) * scale;

            offset.x = ClampHeartAxis(
                offset.x,
                viewportWidth,
                scaledWidth,
                _heartVisibleBoundsMin.x * scale,
                _heartVisibleBoundsMax.x * scale);
            offset.y = ClampHeartAxis(
                offset.y,
                viewportHeight,
                scaledHeight,
                _heartVisibleBoundsMin.y * scale,
                _heartVisibleBoundsMax.y * scale);
            return offset;
        }

        private static float ClampHeartAxis(
            float offset,
            float viewportSize,
            float scaledContentSize,
            float scaledMinimum,
            float scaledMaximum)
        {
            float centered = viewportSize * 0.5f - (scaledMinimum + scaledMaximum) * 0.5f;
            if (scaledContentSize <= viewportSize - HeartNavigationMargin * 2f)
                return centered;

            float minimumOffset = HeartNavigationMargin - scaledMaximum;
            float maximumOffset = viewportSize - HeartNavigationMargin - scaledMinimum;
            if (minimumOffset > maximumOffset)
                return centered;
            return Mathf.Clamp(offset, minimumOffset, maximumOffset);
        }

        private Vector2 GetHeartViewportCenter()
        {
            return new Vector2(
                _heartViewport.resolvedStyle.width * 0.5f,
                _heartViewport.resolvedStyle.height * 0.5f);
        }

        private void UpdateHeartZoomControls()
        {
            if (_heartZoomValue != null)
                _heartZoomValue.text = $"{Mathf.RoundToInt(_heartZoomMultiplier * 100f)}%";
            _heartZoomOutButton?.SetEnabled(
                _heartZoomMultiplier > HeartGraphNavigationUtility.MinimumZoomMultiplier + 0.001f);
            _heartZoomInButton?.SetEnabled(
                _heartZoomMultiplier < HeartGraphNavigationUtility.MaximumZoomMultiplier - 0.001f);
        }

        private sealed class HeartConnection
        {
            public Vector2 From;
            public Vector2 To;
            public string FromSlotId;
            public string ToSlotId;
            public HeartNodeBranch Branch;
            public bool IsCrossLink;
            public bool IsActive;
            public float Progress;
        }

        private sealed class HeartConnectorLayer : VisualElement
        {
            private readonly List<HeartConnection> _connections = new List<HeartConnection>();

            public HeartConnectorLayer()
            {
                pickingMode = PickingMode.Ignore;
                generateVisualContent += DrawConnections;
            }

            public void SetConnections(List<HeartConnection> connections)
            {
                _connections.Clear();
                if (connections != null)
                    _connections.AddRange(connections);
                MarkDirtyRepaint();
            }

            public void SetProgress(string toSlotId, float progress)
            {
                for (int i = 0; i < _connections.Count; i++)
                {
                    HeartConnection connection = _connections[i];
                    if (string.Equals(connection.ToSlotId, toSlotId, StringComparison.Ordinal))
                        connection.Progress = Mathf.Clamp01(progress);
                }
                MarkDirtyRepaint();
            }

            private void DrawConnections(MeshGenerationContext context)
            {
                Painter2D painter = context.painter2D;
                painter.lineCap = LineCap.Round;
                painter.lineJoin = LineJoin.Round;

                for (int i = 0; i < _connections.Count; i++)
                {
                    HeartConnection connection = _connections[i];
                    if (connection.Progress <= 0f)
                        continue;

                    painter.SetDashPattern(
                        connection.IsCrossLink ? 2.4f : 3.4f,
                        connection.IsCrossLink ? 8.8f : 6.8f);

                    Color shadow = new Color(0.015f, 0.018f, 0.017f,
                        connection.IsActive ? 0.90f : 0.68f);
                    DrawConnectionPath(
                        painter,
                        connection,
                        shadow,
                        connection.IsCrossLink ? 4.2f : 5.4f);

                    Color branchColor = GetHeartBranchColor(connection.Branch);
                    branchColor.a = connection.IsActive
                        ? connection.IsCrossLink ? 0.76f : 0.94f
                        : connection.IsCrossLink ? 0.46f : 0.62f;
                    DrawConnectionPath(
                        painter,
                        connection,
                        branchColor,
                        connection.IsCrossLink ? 2.0f : 2.8f);
                }
            }

            private static void DrawConnectionPath(
                Painter2D painter,
                HeartConnection connection,
                Color color,
                float width)
            {
                Vector2 fullDelta = connection.To - connection.From;
                float distance = fullDelta.magnitude;
                if (distance <= 1f)
                    return;

                Vector2 direction = fullDelta / distance;
                float fromRadius = string.Equals(
                    connection.FromSlotId,
                    HeartGraphSlotUtility.RootSlotId,
                    StringComparison.Ordinal)
                    ? 49f
                    : 33f;
                Vector2 start = connection.From + direction * fromRadius;
                Vector2 end = connection.To - direction * 33f;
                Vector2 routeDelta = end - start;
                Vector2 perpendicular = new Vector2(-routeDelta.y, routeDelta.x).normalized;
                float bend = connection.IsCrossLink
                    ? Mathf.Clamp(routeDelta.magnitude * 0.15f, 34f, 86f)
                    : Mathf.Clamp(routeDelta.magnitude * 0.09f, 12f, 34f);
                float curveDirection = GetStableCurveDirection(
                    connection.FromSlotId,
                    connection.ToSlotId);
                Vector2 controlA = Vector2.Lerp(start, end, 0.34f)
                                   + perpendicular * bend * curveDirection;
                Vector2 controlB = Vector2.Lerp(start, end, 0.68f)
                                   + perpendicular * bend * curveDirection;

                painter.strokeColor = color;
                painter.lineWidth = width;
                painter.BeginPath();
                painter.MoveTo(start);
                int steps = Mathf.Max(4, Mathf.CeilToInt(32f * connection.Progress));
                for (int step = 1; step <= steps; step++)
                {
                    float t = connection.Progress * step / steps;
                    float inverse = 1f - t;
                    Vector2 point = inverse * inverse * inverse * start
                                    + 3f * inverse * inverse * t * controlA
                                    + 3f * inverse * t * t * controlB
                                    + t * t * t * end;
                    painter.LineTo(point);
                }
                painter.Stroke();
            }

            private static float GetStableCurveDirection(string fromSlotId, string toSlotId)
            {
                int checksum = 17;
                string key = (fromSlotId ?? string.Empty) + ">" + (toSlotId ?? string.Empty);
                for (int i = 0; i < key.Length; i++)
                    checksum = unchecked(checksum * 31 + key[i]);
                return (checksum & 1) == 0 ? 1f : -1f;
            }

            private static Color GetHeartBranchColor(HeartNodeBranch branch)
            {
                return branch switch
                {
                    HeartNodeBranch.Army => new Color(0.82f, 0.46f, 0.32f),
                    HeartNodeBranch.Defense => new Color(0.42f, 0.63f, 0.78f),
                    HeartNodeBranch.Production => new Color(0.52f, 0.70f, 0.42f),
                    HeartNodeBranch.HeartMagic => new Color(0.66f, 0.43f, 0.76f),
                    _ => new Color(0.78f, 0.66f, 0.48f)
                };
            }
        }
    }
}
