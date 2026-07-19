using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeadWalls.UIPrototype
{
    /// <summary>
    /// UI Toolkit Tech Tree visual prototype.
    /// It intentionally has no gameplay bindings and never mutates run state or resources.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TechTreePrototypeController : MonoBehaviour
    {
        private enum NodeState
        {
            Owned,
            Available,
            Locked,
        }

        private sealed class NodeModel
        {
            public string Id;
            public string Title;
            public string Branch;
            public string Kicker;
            public string Quote;
            public string Description;
            public string Requirement;
            public string Effect;
            public string Primary;
            public string Secondary;
            public string Tertiary;
            public int Tier;
            public int Cost;
            public Vector2 Position;
            public NodeState State;
        }

        private readonly struct EdgeModel
        {
            public readonly string From;
            public readonly string To;
            public readonly NodeState State;

            public EdgeModel(string from, string to, NodeState state)
            {
                From = from;
                To = to;
                State = state;
            }
        }

        private const float NodeWidth = 232f;
        private const float NodeHeight = 108f;
        private const float MinZoom = 0.70f;
        private const float MaxZoom = 1.25f;

        private readonly List<NodeModel> _nodes = new List<NodeModel>();
        private readonly List<EdgeModel> _edges = new List<EdgeModel>();
        private readonly Dictionary<string, NodeModel> _nodesById = new Dictionary<string, NodeModel>();
        private readonly Dictionary<string, Button> _cardsById = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> _filterButtons = new Dictionary<string, Button>();

        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _viewport;
        private VisualElement _graphContent;
        private VisualElement _edgeLayer;
        private VisualElement _nodeLayer;
        private VisualElement _detailsPanel;
        private VisualElement _detailSigil;
        private Label _detailSigilLetter;
        private VisualElement _requirementMark;
        private VisualElement _toast;
        private VisualElement _siegePulse;
        private Label _detailEyebrow;
        private Label _detailTitle;
        private Label _detailTier;
        private Label _detailQuote;
        private Label _detailDescription;
        private Label _detailRequirement;
        private Label _detailCost;
        private Label _impactPrimary;
        private Label _impactSecondary;
        private Label _impactTertiary;
        private Label _toastTitle;
        private Label _toastMessage;
        private Button _previewButton;
        private Button _zoomResetButton;

        private NodeModel _selected;
        private Vector2 _pan = new Vector2(12f, 8f);
        private Vector2 _dragStart;
        private Vector2 _panAtDragStart;
        private float _zoom = 0.94f;
        private bool _dragging;
        private int _dragPointerId = -1;
        private bool _initialized;
        private bool _ambientPulse;
        private bool _developmentOverlaySuppressed;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
            _document.rootVisualElement.schedule.Execute(Initialize).StartingIn(1);
        }

        private void LateUpdate()
        {
            if (!_developmentOverlaySuppressed)
                _developmentOverlaySuppressed = TryDisableGlobalDevelopmentOverlay();
        }

        private void Initialize()
        {
            if (_initialized || _document == null)
                return;

            _root = _document.rootVisualElement.Q<VisualElement>("prototype-root");
            _viewport = _document.rootVisualElement.Q<VisualElement>("map-viewport");
            _graphContent = _document.rootVisualElement.Q<VisualElement>("graph-content");
            _edgeLayer = _document.rootVisualElement.Q<VisualElement>("edge-layer");
            _nodeLayer = _document.rootVisualElement.Q<VisualElement>("node-layer");
            _detailsPanel = _document.rootVisualElement.Q<VisualElement>("details-panel");

            if (_root == null || _viewport == null || _graphContent == null || _edgeLayer == null || _nodeLayer == null)
            {
                Debug.LogError("[TechTreePrototype] UXML hierarchy is incomplete.", this);
                return;
            }

            CacheDetailsReferences();
            _developmentOverlaySuppressed = TryDisableGlobalDevelopmentOverlay();
            CreatePrototypeData();
            BuildGraph();
            BindCommands();
            ApplyGraphTransform();
            SelectNode("masons_oath");
            PlayEntranceSequence();
            StartAmbientMotion();
            _initialized = true;
        }

        private void CacheDetailsReferences()
        {
            VisualElement visualRoot = _document.rootVisualElement;
            _detailSigil = visualRoot.Q<VisualElement>("detail-sigil");
            _detailSigilLetter = visualRoot.Q<Label>("detail-sigil-letter");
            _requirementMark = visualRoot.Q<VisualElement>("requirement-mark");
            _detailEyebrow = visualRoot.Q<Label>("detail-eyebrow");
            _detailTitle = visualRoot.Q<Label>("detail-title");
            _detailTier = visualRoot.Q<Label>("detail-tier");
            _detailQuote = visualRoot.Q<Label>("detail-quote");
            _detailDescription = visualRoot.Q<Label>("detail-description");
            _detailRequirement = visualRoot.Q<Label>("detail-requirement");
            _detailCost = visualRoot.Q<Label>("detail-cost");
            _impactPrimary = visualRoot.Q<Label>("impact-primary");
            _impactSecondary = visualRoot.Q<Label>("impact-secondary");
            _impactTertiary = visualRoot.Q<Label>("impact-tertiary");
            _previewButton = visualRoot.Q<Button>("preview-unlock-button");
            _zoomResetButton = visualRoot.Q<Button>("zoom-reset-button");
            _toast = visualRoot.Q<VisualElement>("prototype-toast");
            _toastTitle = visualRoot.Q<Label>("toast-title");
            _toastMessage = visualRoot.Q<Label>("toast-message");
            _siegePulse = visualRoot.Q<VisualElement>("siege-pulse");
        }

        private void CreatePrototypeData()
        {
            _nodes.Clear();
            _edges.Clear();
            _nodesById.Clear();

            AddNode("castle_heart", "Castle Heart", "root", "FOUNDATION", 0, 0, NodeState.Owned,
                new Vector2(24f, 322f), "The wall is stone. The doctrine is memory.",
                "The living core of every doctrine path. All knowledge begins here.",
                "None", "Doctrine paths revealed", "4 paths", "12 nodes", "1 citadel");

            AddNode("ration_ledgers", "Ration Ledgers", "economy", "STEWARD PATH", 1, 2, NodeState.Owned,
                new Vector2(330f, 58f), "Count what remains before the gates close.",
                "A disciplined ledger reveals waste before the next siege begins.",
                "Castle Heart", "Food yield +8%", "+8%", "+1 reserve", "-3% waste");
            AddNode("masons_oath", "Mason's Oath", "defense", "BASTION PATH", 1, 3, NodeState.Available,
                new Vector2(330f, 232f), "Stone holds when every hand knows where to brace it.",
                "Reinforce the outer wall with a shared defensive doctrine.",
                "Castle Heart", "Wall integrity +12%", "+12%", "+8%", "-4% pressure");
            AddNode("fletching_hall", "Fletching Hall", "archery", "MARKSMEN PATH", 1, 3, NodeState.Available,
                new Vector2(330f, 406f), "Every arrow is a promise made before nightfall.",
                "Standardize arrow craft and prepare the keep for sustained volleys.",
                "Castle Heart", "Arrow stock +20", "+20 stock", "+6% craft", "-5% loss");
            AddNode("war_council", "War Council", "command", "COMMAND PATH", 1, 4, NodeState.Available,
                new Vector2(330f, 580f), "A clear order travels faster than fear.",
                "Formalize command signals so every defender reacts to the same threat.",
                "Castle Heart", "Order response +10%", "+10%", "+1 signal", "-6% delay");

            AddNode("efficient_crews", "Efficient Crews", "economy", "STEWARD PATH", 2, 5, NodeState.Available,
                new Vector2(648f, 58f), "No idle hand survives a long winter.",
                "Improve workforce transitions between production duties.",
                "Ration Ledgers", "Worker efficiency +10%", "+10%", "+1 transfer", "-5% idle");
            AddNode("reinforced_curtain", "Reinforced Curtain", "defense", "BASTION PATH", 2, 6, NodeState.Locked,
                new Vector2(648f, 232f), "The second wall is built inside the first.",
                "Layered masonry reduces the impact of sustained assaults.",
                "Mason's Oath", "Damage resistance +9%", "+9%", "+120 health", "-3% breach");
            AddNode("rapid_volley", "Rapid Volley", "archery", "MARKSMEN PATH", 2, 6, NodeState.Locked,
                new Vector2(648f, 406f), "Loose together. Reload together. Live together.",
                "Train archers to synchronize volleys without sacrificing accuracy.",
                "Fletching Hall", "Archer rate +14%", "+14%", "+6% aim", "-4% delay");
            AddNode("night_watch", "Night Watch", "command", "COMMAND PATH", 2, 7, NodeState.Locked,
                new Vector2(648f, 580f), "The first warning is worth a hundred swords.",
                "Dedicated watch rotations reveal pressure before the horde reaches the wall.",
                "War Council", "Night warning +18 sec", "+18 sec", "+1 alert", "-8% panic");

            AddNode("salvage_doctrine", "Salvage Doctrine", "economy", "STEWARD PATH", 3, 9, NodeState.Locked,
                new Vector2(966f, 58f), "Nothing beyond the wall is truly lost.",
                "Recover usable material from every broken weapon and shattered defense.",
                "Efficient Crews", "Salvage return +15%", "+15%", "+3 iron", "+5 stone");
            AddNode("bastion_heart", "Bastion Heart", "defense", "BASTION PATH", 3, 10, NodeState.Locked,
                new Vector2(966f, 232f), "The keep does not retreat. It becomes the wall.",
                "Convert the castle core into a final defensive anchor.",
                "Reinforced Curtain", "Core integrity +18%", "+18%", "+180 health", "-10% breach");
            AddNode("frostbound_tips", "Frostbound Tips", "archery", "MARKSMEN PATH", 3, 10, NodeState.Locked,
                new Vector2(966f, 406f), "Let winter ride with every shaft.",
                "A rare arrow treatment slows dense groups at the point of impact.",
                "Rapid Volley", "Prototype slow effect", "+12% slow", "+2 sec", "-6% spread");
            AddNode("blood_moon_edict", "Blood Moon Edict", "command", "COMMAND PATH", 3, 12, NodeState.Locked,
                new Vector2(966f, 580f), "When the moon turns, the citadel answers.",
                "Issue a final doctrine reserved for the most dangerous siege nights.",
                "Night Watch", "Crisis response +20%", "+20%", "+1 decree", "-12% panic");

            AddEdge("castle_heart", "ration_ledgers", NodeState.Owned);
            AddEdge("castle_heart", "masons_oath", NodeState.Available);
            AddEdge("castle_heart", "fletching_hall", NodeState.Available);
            AddEdge("castle_heart", "war_council", NodeState.Available);
            AddEdge("ration_ledgers", "efficient_crews", NodeState.Available);
            AddEdge("masons_oath", "reinforced_curtain", NodeState.Locked);
            AddEdge("fletching_hall", "rapid_volley", NodeState.Locked);
            AddEdge("war_council", "night_watch", NodeState.Locked);
            AddEdge("efficient_crews", "salvage_doctrine", NodeState.Locked);
            AddEdge("reinforced_curtain", "bastion_heart", NodeState.Locked);
            AddEdge("rapid_volley", "frostbound_tips", NodeState.Locked);
            AddEdge("night_watch", "blood_moon_edict", NodeState.Locked);
        }

        private void AddNode(
            string id,
            string title,
            string branch,
            string kicker,
            int tier,
            int cost,
            NodeState state,
            Vector2 position,
            string quote,
            string description,
            string requirement,
            string effect,
            string primary,
            string secondary,
            string tertiary)
        {
            var node = new NodeModel
            {
                Id = id,
                Title = title,
                Branch = branch,
                Kicker = kicker,
                Tier = tier,
                Cost = cost,
                State = state,
                Position = position,
                Quote = quote,
                Description = description,
                Requirement = requirement,
                Effect = effect,
                Primary = primary,
                Secondary = secondary,
                Tertiary = tertiary,
            };

            _nodes.Add(node);
            _nodesById.Add(id, node);
        }

        private void AddEdge(string from, string to, NodeState state)
        {
            _edges.Add(new EdgeModel(from, to, state));
        }

        private void BuildGraph()
        {
            _edgeLayer.Clear();
            _nodeLayer.Clear();
            _cardsById.Clear();

            CreateDoctrineLane("economy", 38f);
            CreateDoctrineLane("defense", 212f);
            CreateDoctrineLane("archery", 386f);
            CreateDoctrineLane("command", 560f);
            CreateTierMarker("FOUNDATION", 24f, 232f);
            CreateTierMarker("TIER I", 330f, 232f);
            CreateTierMarker("TIER II", 648f, 232f);
            CreateTierMarker("TIER III", 966f, 232f);

            foreach (EdgeModel edge in _edges)
                CreateEdge(edge);

            for (int i = 0; i < _nodes.Count; i++)
                CreateNodeCard(_nodes[i], i);
        }

        private void CreateDoctrineLane(string branch, float top)
        {
            var lane = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            lane.AddToClassList("doctrine-lane");
            lane.AddToClassList("doctrine-lane--" + branch);
            lane.style.top = top;
            _edgeLayer.Add(lane);
        }

        private void CreateTierMarker(string text, float left, float width)
        {
            Label marker = CreateLabel(text, "tier-marker");
            marker.style.left = left;
            marker.style.width = width;
            _edgeLayer.Add(marker);
        }

        private void CreateNodeCard(NodeModel node, int index)
        {
            var card = new Button
            {
                name = "node-" + node.Id,
                text = string.Empty,
                tooltip = node.Title + " - " + GetStateLabel(node.State),
                focusable = true,
                tabIndex = index,
            };
            card.AddToClassList("node-card");
            card.AddToClassList("node-card--" + node.State.ToString().ToLowerInvariant());
            card.AddToClassList("node-card--enter");
            card.AddToClassList("node-branch--" + node.Branch);
            card.style.left = node.Position.x;
            card.style.top = node.Position.y;

            var accent = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            accent.AddToClassList("node-accent");
            accent.AddToClassList("branch-accent--" + node.Branch);
            card.Add(accent);

            var corner = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            corner.AddToClassList("node-corner");
            card.Add(corner);

            var top = new VisualElement();
            top.AddToClassList("node-top");

            var sigil = new VisualElement();
            sigil.AddToClassList("node-sigil");
            sigil.AddToClassList("branch--" + node.Branch);
            sigil.pickingMode = PickingMode.Ignore;
            sigil.Add(CreateLabel(GetBranchSigil(node.Branch), "node-sigil-letter"));
            top.Add(sigil);

            var copy = new VisualElement();
            copy.AddToClassList("node-copy");
            copy.pickingMode = PickingMode.Ignore;
            copy.Add(CreateLabel(node.Kicker, "node-kicker"));
            copy.Add(CreateLabel(node.Title, "node-title"));
            copy.Add(CreateLabel(GetStateLabel(node.State), "node-state"));
            top.Add(copy);

            var bottom = new VisualElement();
            bottom.AddToClassList("node-bottom");
            bottom.pickingMode = PickingMode.Ignore;
            bottom.Add(CreateLabel(node.Effect, "node-effect"));
            bottom.Add(CreateLabel(node.Cost == 0 ? "ROOT" : node.Cost + " KNOWLEDGE", "node-cost"));

            card.Add(top);
            card.Add(bottom);
            card.clicked += () => SelectNode(node.Id);
            _nodeLayer.Add(card);
            _cardsById.Add(node.Id, card);
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = new Label(text)
            {
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList(className);
            return label;
        }

        private void CreateEdge(EdgeModel edge)
        {
            NodeModel from = _nodesById[edge.From];
            NodeModel to = _nodesById[edge.To];
            float fromX = from.Position.x + NodeWidth;
            float fromY = from.Position.y + NodeHeight * 0.5f;
            float toX = to.Position.x;
            float toY = to.Position.y + NodeHeight * 0.5f;
            float jointX = Mathf.Lerp(fromX, toX, 0.5f);
            string stateClass = "edge--" + edge.State.ToString().ToLowerInvariant();

            AddEdgeSegment(fromX, fromY - 1f, jointX - fromX, 2f, stateClass);
            AddEdgeSegment(jointX - 1f, Mathf.Min(fromY, toY), 2f, Mathf.Abs(toY - fromY), stateClass);
            AddEdgeSegment(jointX, toY - 1f, toX - jointX, 2f, stateClass);

            var joint = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            joint.AddToClassList("edge-joint");
            joint.style.left = jointX;
            joint.style.top = toY;
            _edgeLayer.Add(joint);
        }

        private void AddEdgeSegment(float left, float top, float width, float height, string stateClass)
        {
            var segment = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
            };
            segment.AddToClassList("edge");
            segment.AddToClassList(stateClass);
            segment.style.left = left;
            segment.style.top = top;
            segment.style.width = Mathf.Max(2f, width);
            segment.style.height = Mathf.Max(2f, height);
            _edgeLayer.Add(segment);
        }

        private void BindCommands()
        {
            _viewport.RegisterCallback<WheelEvent>(OnWheel);
            _viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel);

            _document.rootVisualElement.Q<Button>("zoom-out-button").clicked += () => SetZoom(_zoom - 0.10f, true);
            _document.rootVisualElement.Q<Button>("zoom-in-button").clicked += () => SetZoom(_zoom + 0.10f, true);
            _zoomResetButton.clicked += ResetView;
            _previewButton.clicked += PreviewSelectedNode;
            _document.rootVisualElement.Q<Button>("close-button").clicked += () =>
                ShowToast("PROTOTYPE SCENE", "Exit Play Mode to return. No game state exists here.");

            BindFilter("all", "filter-all");
            BindFilter("defense", "filter-defense");
            BindFilter("economy", "filter-economy");
            BindFilter("archery", "filter-archery");
            BindFilter("command", "filter-command");

            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void BindFilter(string branch, string buttonName)
        {
            Button button = _document.rootVisualElement.Q<Button>(buttonName);
            _filterButtons.Add(branch, button);
            button.clicked += () => ApplyFilter(branch);
        }

        private void ApplyFilter(string branch)
        {
            foreach (KeyValuePair<string, Button> pair in _filterButtons)
                pair.Value.EnableInClassList("filter-button--active", pair.Key == branch);

            foreach (NodeModel node in _nodes)
            {
                bool filtered = branch != "all" && node.Branch != branch && node.Branch != "root";
                _cardsById[node.Id].EnableInClassList("node-card--filtered", filtered);
            }
        }

        private void SelectNode(string id)
        {
            if (!_nodesById.TryGetValue(id, out NodeModel node))
                return;

            if (_selected != null && _cardsById.TryGetValue(_selected.Id, out Button previous))
                previous.RemoveFromClassList("node-card--selected");

            _selected = node;
            Button selectedCard = _cardsById[node.Id];
            selectedCard.AddToClassList("node-card--selected");
            selectedCard.Focus();

            _detailsPanel.AddToClassList("details-panel--refresh");
            _detailsPanel.schedule.Execute(() =>
                _detailsPanel.RemoveFromClassList("details-panel--refresh")).StartingIn(75);

            SetBranchClass(_detailSigil, node.Branch);
            _detailSigilLetter.text = GetBranchSigil(node.Branch);
            _detailEyebrow.text = node.Branch == "root" ? "FOUNDATION DOCTRINE" : node.Branch.ToUpperInvariant() + " DOCTRINE";
            _detailTitle.text = node.Title.ToUpperInvariant();
            _detailTier.text = "TIER " + ToRoman(node.Tier) + " / " + GetStateLabel(node.State);
            _detailQuote.text = node.Quote;
            _detailDescription.text = node.Description + " This prototype is presentation-only and changes no gameplay data.";
            _detailRequirement.text = node.Requirement;
            _detailCost.text = node.Cost == 0 ? "-" : node.Cost.ToString();
            _impactPrimary.text = node.Primary;
            _impactSecondary.text = node.Secondary;
            _impactTertiary.text = node.Tertiary;

            bool requirementMet = node.State != NodeState.Locked;
            _requirementMark.EnableInClassList("requirement-mark--met", requirementMet);
            _requirementMark.EnableInClassList("requirement-mark--blocked", !requirementMet);

            _previewButton.SetEnabled(node.State == NodeState.Available);
            _previewButton.text = node.State switch
            {
                NodeState.Owned => "DOCTRINE MASTERED",
                NodeState.Available => "PREVIEW UNLOCK",
                _ => "REQUIRES " + node.Requirement.ToUpperInvariant(),
            };
        }

        private static void SetBranchClass(VisualElement element, string branch)
        {
            element.RemoveFromClassList("branch--root");
            element.RemoveFromClassList("branch--defense");
            element.RemoveFromClassList("branch--economy");
            element.RemoveFromClassList("branch--archery");
            element.RemoveFromClassList("branch--command");
            element.AddToClassList("branch--" + branch);
        }

        private void PreviewSelectedNode()
        {
            if (_selected == null || _selected.State != NodeState.Available)
                return;

            Button card = _cardsById[_selected.Id];
            card.AddToClassList("node-card--preview");
            _previewButton.SetEnabled(false);
            _previewButton.text = "DOCTRINE PREVIEWED";
            ShowToast("DOCTRINE PREVIEWED", _selected.Title + " - no resources spent, no game state changed.");

            card.schedule.Execute(() => card.RemoveFromClassList("node-card--preview")).StartingIn(620);
            _previewButton.schedule.Execute(() =>
            {
                if (_selected != null && _selected.State == NodeState.Available)
                {
                    _previewButton.SetEnabled(true);
                    _previewButton.text = "PREVIEW UNLOCK";
                }
            }).StartingIn(900);
        }

        private void ShowToast(string title, string message)
        {
            _toastTitle.text = title;
            _toastMessage.text = message;
            _toast.AddToClassList("prototype-toast--visible");
            _toast.schedule.Execute(() => _toast.RemoveFromClassList("prototype-toast--visible")).StartingIn(2200);
        }

        private void OnWheel(WheelEvent evt)
        {
            float direction = evt.delta.y > 0f ? -1f : 1f;
            float nextZoom = Mathf.Clamp(_zoom + direction * 0.08f, MinZoom, MaxZoom);
            Vector2 cursor = evt.localMousePosition;
            Vector2 graphPoint = (cursor - _pan) / Mathf.Max(0.001f, _zoom);
            _pan = cursor - graphPoint * nextZoom;
            _zoom = nextZoom;
            ApplyGraphTransform();
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 && evt.button != 2)
                return;

            if (IsInsideButton(evt.target as VisualElement))
                return;

            _dragging = true;
            _dragPointerId = evt.pointerId;
            _dragStart = new Vector2(evt.position.x, evt.position.y);
            _panAtDragStart = _pan;
            _viewport.CapturePointer(evt.pointerId);
            _viewport.Focus();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_dragging || evt.pointerId != _dragPointerId)
                return;

            var pointerPosition = new Vector2(evt.position.x, evt.position.y);
            _pan = _panAtDragStart + (pointerPosition - _dragStart);
            ClampPan();
            ApplyGraphTransform();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            EndDrag(evt.pointerId);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            EndDrag(evt.pointerId);
        }

        private void EndDrag(int pointerId)
        {
            if (!_dragging || pointerId != _dragPointerId)
                return;

            if (_viewport.HasPointerCapture(pointerId))
                _viewport.ReleasePointer(pointerId);

            _dragging = false;
            _dragPointerId = -1;
        }

        private bool IsInsideButton(VisualElement target)
        {
            VisualElement current = target;
            while (current != null && current != _viewport)
            {
                if (current is Button)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private void SetZoom(float value, bool keepCenter)
        {
            float nextZoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            if (keepCenter && _viewport.resolvedStyle.width > 0f && _viewport.resolvedStyle.height > 0f)
            {
                Vector2 center = new Vector2(_viewport.resolvedStyle.width * 0.5f, _viewport.resolvedStyle.height * 0.5f);
                Vector2 graphPoint = (center - _pan) / Mathf.Max(0.001f, _zoom);
                _pan = center - graphPoint * nextZoom;
            }
            _zoom = nextZoom;
            ClampPan();
            ApplyGraphTransform();
        }

        private void ResetView()
        {
            _zoom = 0.94f;
            _pan = new Vector2(12f, 8f);
            ApplyGraphTransform();
            ShowToast("VIEW RECENTERED", "Doctrine map returned to its presentation framing.");
        }

        private void ClampPan()
        {
            float viewportWidth = Mathf.Max(400f, _viewport.resolvedStyle.width);
            float viewportHeight = Mathf.Max(300f, _viewport.resolvedStyle.height);
            float contentWidth = 1300f * _zoom;
            float contentHeight = 760f * _zoom;
            const float margin = 120f;

            float minX = Mathf.Min(margin, viewportWidth - contentWidth - margin);
            float minY = Mathf.Min(margin, viewportHeight - contentHeight - margin);
            _pan.x = Mathf.Clamp(_pan.x, minX, margin);
            _pan.y = Mathf.Clamp(_pan.y, minY, margin);
        }

        private void ApplyGraphTransform()
        {
            _graphContent.style.left = _pan.x;
            _graphContent.style.top = _pan.y;
            _graphContent.style.scale = new Scale(new Vector3(_zoom, _zoom, 1f));
            if (_zoomResetButton != null)
                _zoomResetButton.text = Mathf.RoundToInt(_zoom * 100f) + "%";
        }

        private void PlayEntranceSequence()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                Button card = _cardsById[_nodes[i].Id];
                int delay = 70 + i * 52;
                card.schedule.Execute(() => card.RemoveFromClassList("node-card--enter")).StartingIn(delay);
            }

            _detailsPanel.AddToClassList("details-panel--refresh");
            _detailsPanel.schedule.Execute(() =>
                _detailsPanel.RemoveFromClassList("details-panel--refresh")).StartingIn(260);
        }

        private void StartAmbientMotion()
        {
            _root.schedule.Execute(() =>
            {
                _ambientPulse = !_ambientPulse;
                _siegePulse.EnableInClassList("siege-pulse--dim", _ambientPulse);
                if (_cardsById.TryGetValue("castle_heart", out Button rootCard))
                    rootCard.EnableInClassList("node-card--ambient", _ambientPulse);
            }).Every(720);
        }

        private static bool TryDisableGlobalDevelopmentOverlay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DeadWalls.DevelopmentTestPanel[] developmentPanels = Resources.FindObjectsOfTypeAll<DeadWalls.DevelopmentTestPanel>();
            if (developmentPanels.Length > 0)
            {
                foreach (DeadWalls.DevelopmentTestPanel developmentPanel in developmentPanels)
                {
                    developmentPanel.enabled = false;
                    developmentPanel.gameObject.SetActive(false);
                }
                return true;
            }
#endif
            return false;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.R)
            {
                ResetView();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Equals || evt.keyCode == KeyCode.KeypadPlus)
            {
                SetZoom(_zoom + 0.10f, true);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Minus || evt.keyCode == KeyCode.KeypadMinus)
            {
                SetZoom(_zoom - 0.10f, true);
                evt.StopPropagation();
            }
        }

        private static string GetStateLabel(NodeState state)
        {
            return state switch
            {
                NodeState.Owned => "MASTERED",
                NodeState.Available => "AVAILABLE",
                _ => "LOCKED",
            };
        }

        private static string GetBranchSigil(string branch)
        {
            return branch switch
            {
                "root" => "H",
                "defense" => "B",
                "economy" => "S",
                "archery" => "M",
                "command" => "C",
                _ => "?",
            };
        }

        private static string ToRoman(int tier)
        {
            return tier switch
            {
                0 => "ROOT",
                1 => "I",
                2 => "II",
                3 => "III",
                _ => tier.ToString(),
            };
        }
    }
}
