using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls
{
    public sealed partial class GameplayHUDToolkitUI
    {
        private VisualElement _techViewport;
        private VisualElement _techGraphContent;
        private Label _techInspectorTitle;
        private Label _techInspectorLevel;
        private Label _techInspectorBody;
        private Label _techInspectorCost;
        private Label _techInspectorStatus;
        private Button _techPurchaseButton;
        private TechNodeDefinitionSO _selectedTechNode;
        private readonly Dictionary<string, Button> _techNodeButtons = new Dictionary<string, Button>();
        private int _techGraphSignature;
        private float _techZoom = 0.92f;
        private Vector2 _techPan = new Vector2(20f, 20f);
        private float _techContentWidth = 1320f;
        private float _techContentHeight = 760f;
        private bool _techDragging;
        private int _techDragPointer = -1;
        private Vector2 _techDragStart;
        private Vector2 _techPanStart;

        private void BindGraphActions()
        {
            BindCastleHeartActions();
        }

        private void BindGraphManipulation()
        {
            // Kaldirilan ikinci teknoloji yuzeyi artik production input callback'i kaydetmez.
        }

        private void RebuildTechGraph(bool force)
        {
            GameManager gm = GameManager.Instance;
            if (_techGraphContent == null || gm == null || gm.TechCatalog == null)
                return;

            List<TechNodeDefinitionSO> visible = gm.GetRevealedTechNodes();
            int signature = visible.Count;
            for (int i = 0; i < visible.Count; i++)
            {
                TechNodeDefinitionSO node = visible[i];
                if (node == null)
                    continue;
                signature = signature * 31 + node.Id.GetHashCode();
                signature = signature * 31 + gm.GetTechNodeLevel(node.Id);
            }

            if (!force && signature == _techGraphSignature)
            {
                RefreshTechInspector(gm);
                return;
            }

            _techGraphSignature = signature;
            _techGraphContent.Clear();
            _techNodeButtons.Clear();
            var byId = new Dictionary<string, TechNodeDefinitionSO>();
            for (int i = 0; i < visible.Count; i++)
                if (visible[i] != null && !string.IsNullOrWhiteSpace(visible[i].Id))
                    byId[visible[i].Id] = visible[i];

            var depths = new Dictionary<string, int>();
            for (int i = 0; i < visible.Count; i++)
                ResolveTechDepth(visible[i], byId, depths, new HashSet<string>());

            var lanes = new Dictionary<int, List<TechNodeDefinitionSO>>();
            int maxDepth = 0;
            foreach (TechNodeDefinitionSO node in visible)
            {
                if (node == null)
                    continue;
                int depth = depths.TryGetValue(node.Id, out int value) ? value : 0;
                maxDepth = Mathf.Max(maxDepth, depth);
                if (!lanes.TryGetValue(depth, out List<TechNodeDefinitionSO> lane))
                {
                    lane = new List<TechNodeDefinitionSO>();
                    lanes.Add(depth, lane);
                }
                lane.Add(node);
            }
            foreach (List<TechNodeDefinitionSO> lane in lanes.Values)
                lane.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));

            int maxLane = 1;
            foreach (List<TechNodeDefinitionSO> lane in lanes.Values)
                maxLane = Mathf.Max(maxLane, lane.Count);
            float contentWidth = Mathf.Max(1320f, 150f + (maxDepth + 1) * 270f);
            float contentHeight = Mathf.Max(760f, 110f + maxLane * 132f);
            _techContentWidth = contentWidth;
            _techContentHeight = contentHeight;
            _techGraphContent.style.width = contentWidth;
            _techGraphContent.style.height = contentHeight;

            if (_selectedTechNode != null && !byId.TryGetValue(_selectedTechNode.Id, out _selectedTechNode))
                _selectedTechNode = null;
            if (_selectedTechNode != null && gm.IsTechNodeMaxed(_selectedTechNode))
                _selectedTechNode = null;
            if (_selectedTechNode == null)
            {
                for (int i = 0; i < visible.Count; i++)
                {
                    TechNodeDefinitionSO candidate = visible[i];
                    if (candidate != null
                        && !gm.IsTechNodeMaxed(candidate)
                        && gm.CanBuyTechNode(candidate, out _))
                    {
                        _selectedTechNode = candidate;
                        break;
                    }
                }
                if (_selectedTechNode == null && visible.Count > 0)
                    _selectedTechNode = visible[0];
            }

            var positions = new Dictionary<string, Vector2>();
            float graphWidth = maxDepth * 270f + 210f;
            float startX = Mathf.Max(70f, (contentWidth - graphWidth) * 0.5f);
            foreach (KeyValuePair<int, List<TechNodeDefinitionSO>> pair in lanes)
            {
                float laneHeight = pair.Value.Count * 132f;
                float startY = Mathf.Max(70f, (contentHeight - laneHeight) * 0.5f);
                for (int i = 0; i < pair.Value.Count; i++)
                    positions[pair.Value[i].Id] = new Vector2(startX + pair.Key * 270f, startY + i * 132f);
            }

            foreach (TechNodeDefinitionSO child in visible)
            {
                if (child == null || child.PrerequisiteNodeIds == null || !positions.TryGetValue(child.Id, out Vector2 childPos))
                    continue;
                for (int i = 0; i < child.PrerequisiteNodeIds.Length; i++)
                {
                    string parentId = child.PrerequisiteNodeIds[i];
                    if (positions.TryGetValue(parentId, out Vector2 parentPos))
                        AddGraphLine(_techGraphContent, parentPos + new Vector2(210f, 44f), childPos + new Vector2(0f, 44f), gm.GetTechNodeLevel(parentId) > 0);
                }
            }

            foreach (TechNodeDefinitionSO node in visible)
            {
                if (node == null)
                    continue;
                Button card = CreateTechNodeCard(node, gm);
                Vector2 position = positions[node.Id];
                card.style.left = position.x;
                card.style.top = position.y;
                _techGraphContent.Add(card);
                _techNodeButtons[node.Id] = card;
            }

            if (force)
                CenterTechView(false);
            else
                ApplyTechTransform();
            RefreshTechInspector(gm);
        }

        private static int ResolveTechDepth(TechNodeDefinitionSO node,
            Dictionary<string, TechNodeDefinitionSO> byId,
            Dictionary<string, int> cache,
            HashSet<string> visiting)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.Id))
                return 0;
            if (cache.TryGetValue(node.Id, out int known))
                return known;
            if (!visiting.Add(node.Id))
                return 0;

            int depth = 0;
            if (node.PrerequisiteNodeIds != null)
            {
                for (int i = 0; i < node.PrerequisiteNodeIds.Length; i++)
                {
                    if (byId.TryGetValue(node.PrerequisiteNodeIds[i], out TechNodeDefinitionSO parent))
                        depth = Mathf.Max(depth, ResolveTechDepth(parent, byId, cache, visiting) + 1);
                }
            }
            visiting.Remove(node.Id);
            cache[node.Id] = depth;
            return depth;
        }

        private Button CreateTechNodeCard(TechNodeDefinitionSO node, GameManager gm)
        {
            Button card = new Button(() => SelectTechNode(node));
            card.AddToClassList("graph-node");
            int level = gm.GetTechNodeLevel(node.Id);
            bool canBuy = gm.CanBuyTechNode(node, out _);
            bool maxed = gm.IsTechNodeMaxed(node);
            card.EnableInClassList("is-owned", level > 0);
            card.EnableInClassList("is-available", canBuy);
            card.EnableInClassList("is-locked", !canBuy && level <= 0);
            if (_selectedTechNode != null && _selectedTechNode.Id == node.Id)
                card.AddToClassList("is-selected");

            Label title = new Label(node.Title.ToUpperInvariant());
            title.AddToClassList("node-title");
            card.Add(title);
            Label meta = new Label(maxed ? $"MASTERED  ·  LEVEL {level:N0}" : $"LEVEL {level:N0} / {node.MaxLevel:N0}");
            meta.AddToClassList("node-meta");
            card.Add(meta);
            Label cost = new Label(maxed ? "COMPLETE" : FormatCost(gm.GetTechNodeCost(node)));
            cost.AddToClassList("node-cost");
            card.Add(cost);
            return card;
        }

        private void SelectTechNode(TechNodeDefinitionSO node)
        {
            if (_selectedTechNode != null && _techNodeButtons.TryGetValue(_selectedTechNode.Id, out Button previous))
                previous.RemoveFromClassList("is-selected");
            _selectedTechNode = node;
            if (node != null && _techNodeButtons.TryGetValue(node.Id, out Button current))
                current.AddToClassList("is-selected");
            RefreshTechInspector(GameManager.Instance);
        }

        private void RefreshTechInspector(GameManager gm)
        {
            if (_techInspectorTitle == null || gm == null)
                return;
            if (_selectedTechNode == null)
            {
                _techInspectorTitle.text = "SELECT A DOCTRINE";
                _techInspectorLevel.text = string.Empty;
                _techInspectorBody.text = "Inspect cost, effect and prerequisites before committing.";
                _techInspectorCost.text = string.Empty;
                _techInspectorStatus.text = string.Empty;
                _techPurchaseButton.SetEnabled(false);
                return;
            }

            TechNodeDefinitionSO node = _selectedTechNode;
            int level = gm.GetTechNodeLevel(node.Id);
            bool maxed = gm.IsTechNodeMaxed(node);
            bool canBuy = gm.CanBuyTechNode(node, out string reason);
            _techInspectorTitle.text = node.Title.ToUpperInvariant();
            _techInspectorLevel.text = $"LEVEL {level:N0} / {node.MaxLevel:N0}";
            _techInspectorBody.text = node.Description;
            _techInspectorCost.text = maxed ? "DOCTRINE MASTERED" : FormatCost(gm.GetTechNodeCost(node));
            _techInspectorStatus.text = maxed ? "All available ranks acquired." : canBuy ? "Requirements met." : reason;
            _techPurchaseButton.text = maxed ? "MASTERED" : "RESEARCH";
            _techPurchaseButton.SetEnabled(canBuy && !maxed);
        }

        private void PurchaseSelectedTechNode()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || _selectedTechNode == null)
                return;
            bool purchased = gm.TryBuyTechNode(_selectedTechNode);
            ShowPrimaryToast(purchased ? $"DOCTRINE RESEARCHED  ·  {_selectedTechNode.Title.ToUpperInvariant()}" : "RESEARCH BLOCKED");
            _techGraphSignature = 0;
            RebuildTechGraph(true);
        }

        private static void AddGraphLine(VisualElement parent, Vector2 from, Vector2 to, bool open)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            VisualElement line = new VisualElement();
            line.AddToClassList("graph-line");
            line.EnableInClassList("is-open", open);
            line.style.left = from.x;
            line.style.top = from.y;
            line.style.width = length;
            line.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            parent.Add(line);
        }

        private void OnTechWheel(WheelEvent evt)
        {
            float direction = evt.delta.y > 0f ? -1f : 1f;
            float next = Mathf.Clamp(_techZoom + direction * 0.08f, 0.58f, 1.35f);
            Vector2 cursor = evt.localMousePosition;
            Vector2 graphPoint = (cursor - _techPan) / Mathf.Max(0.001f, _techZoom);
            _techPan = cursor - graphPoint * next;
            _techZoom = next;
            ApplyTechTransform();
            evt.StopPropagation();
        }

        private void OnTechPointerDown(PointerDownEvent evt)
        {
            if ((evt.button != 0 && evt.button != 2) || IsInsideButton(evt.target as VisualElement, _techViewport))
                return;
            _techDragging = true;
            _techDragPointer = evt.pointerId;
            _techDragStart = new Vector2(evt.position.x, evt.position.y);
            _techPanStart = _techPan;
            _techViewport.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTechPointerMove(PointerMoveEvent evt)
        {
            if (!_techDragging || evt.pointerId != _techDragPointer)
                return;
            Vector2 pointer = new Vector2(evt.position.x, evt.position.y);
            _techPan = _techPanStart + (pointer - _techDragStart);
            ApplyTechTransform();
            evt.StopPropagation();
        }

        private void OnTechPointerUp(PointerUpEvent evt) => EndTechDrag(evt.pointerId);
        private void OnTechPointerCancel(PointerCancelEvent evt) => EndTechDrag(evt.pointerId);

        private void EndTechDrag(int pointerId)
        {
            if (!_techDragging || pointerId != _techDragPointer)
                return;
            if (_techViewport.HasPointerCapture(pointerId))
                _techViewport.ReleasePointer(pointerId);
            _techDragging = false;
            _techDragPointer = -1;
        }

        private static bool IsInsideButton(VisualElement target, VisualElement boundary)
        {
            VisualElement current = target;
            while (current != null && current != boundary)
            {
                if (current is Button)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private void SetTechZoom(float value, bool keepCenter)
        {
            float next = Mathf.Clamp(value, 0.58f, 1.35f);
            if (keepCenter && _techViewport != null)
            {
                Vector2 center = new Vector2(_techViewport.resolvedStyle.width * 0.5f, _techViewport.resolvedStyle.height * 0.5f);
                Vector2 graphPoint = (center - _techPan) / Mathf.Max(0.001f, _techZoom);
                _techPan = center - graphPoint * next;
            }
            _techZoom = next;
            ApplyTechTransform();
        }

        private void ResetTechView()
        {
            CenterTechView(true);
        }

        private void CenterTechView(bool notify)
        {
            _techZoom = 0.92f;
            float viewportWidth = _techViewport != null && _techViewport.resolvedStyle.width > 1f
                ? _techViewport.resolvedStyle.width
                : 1516f;
            float viewportHeight = _techViewport != null && _techViewport.resolvedStyle.height > 1f
                ? _techViewport.resolvedStyle.height
                : 904f;
            _techPan = new Vector2(
                Mathf.Max(16f, (viewportWidth - _techContentWidth * _techZoom) * 0.5f),
                Mathf.Max(16f, (viewportHeight - _techContentHeight * _techZoom) * 0.5f));
            ApplyTechTransform();
            if (notify)
                ShowSecondaryToast("DOCTRINE VIEW RECENTERED");
        }

        private void ApplyTechTransform()
        {
            if (_techGraphContent == null)
                return;
            _techGraphContent.style.left = _techPan.x;
            _techGraphContent.style.top = _techPan.y;
            _techGraphContent.style.scale = new Scale(new Vector3(_techZoom, _techZoom, 1f));
        }
    }
}
