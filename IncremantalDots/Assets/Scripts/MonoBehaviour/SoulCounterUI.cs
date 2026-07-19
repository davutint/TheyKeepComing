using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DeadWalls
{
    /// <summary>
    /// Kosu ici projected death reward sayaci ve Skeleton olumunden HUD'a giden Soul
    /// presentation owner'i. Gameplay odulu ZombieDeathSystem'de olum aninda yazilir;
    /// bu sinif yalniz event'i gorunur animasyona cevirir.
    /// </summary>
    public class SoulCounterUI : MonoBehaviour
    {
        /// <summary>
        /// UI Toolkit runtime shell'inin ayni production pickup event'inden sunum uretmesi icin
        /// player-facing olmayan davranis koprusu. Gameplay odulu bu event'e bagli degildir.
        /// </summary>
        public static event System.Action<SoulPickupEvent> ToolkitSoulPickupRequested;

        public GameObject CounterPanel;
        public TMP_Text CounterText;

        [Header("Soul Pickup Animation")]
        public int InitialSoulVisualPoolSize = 64;
        public float SoulTravelDuration = 0.82f;
        public float SoulArcHeight = 115f;
        public float SoulStartWorldOffset = 0.45f;
        public float CounterPulseDuration = 0.24f;
        public float CounterPulseScale = 1.14f;
        public Color SoulColor = new Color32(118, 224, 255, 255);

        [Header("Dense Soul Burst Presentation")]
        public int SoulPickupAggregationThreshold = 96;
        public int MaxSoulPickupPresentationsPerBurst = 96;

        public int ActiveSoulVisualCount => _activeSoulVisuals.Count;
        public long TotalSoulVisualsPlayedCount { get; private set; }
        public int LastProcessedSoulEventCount { get; private set; }
        public int LastSoulPresentationCount { get; private set; }
        public long LastProcessedSoulAmount { get; private set; }
        public long LastPresentedSoulAmount { get; private set; }

        private const float CheckInterval = 0.25f;
        private float _checkTimer;
        private int _lastShown = -1;
        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _soulPickupQuery;
        private bool _queryReady;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Camera _uiCamera;
        private Sprite _soulSprite;
        private Vector3 _counterBaseScale = Vector3.one;
        private float _counterPulseRemaining;
        private readonly Queue<SoulVisual> _soulVisualPool = new Queue<SoulVisual>();
        private readonly List<ActiveSoulVisual> _activeSoulVisuals =
            new List<ActiveSoulVisual>();
        private readonly List<SoulPickupEvent> _soulPickupPresentations =
            new List<SoulPickupEvent>();
        private readonly Dictionary<SoulAggregateKey, SoulAggregate> _soulPickupAggregates =
            new Dictionary<SoulAggregateKey, SoulAggregate>();

        private struct SoulVisual
        {
            public GameObject Instance;
            public RectTransform Rect;
            public CanvasGroup Group;
            public Image Icon;
            public TMP_Text AmountText;
        }

        private struct ActiveSoulVisual
        {
            public SoulVisual Visual;
            public Vector2 Start;
            public Vector2 Control;
            public Vector2 Target;
            public float Elapsed;
            public float Duration;
        }

        private readonly struct SoulAggregateKey : IEquatable<SoulAggregateKey>
        {
            public readonly int X;
            public readonly int Y;

            public SoulAggregateKey(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(SoulAggregateKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is SoulAggregateKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        private struct SoulAggregate
        {
            public Vector3 PositionSum;
            public long Amount;
            public int EventCount;
        }

        private void Awake()
        {
            CacheCanvas();
            if (CounterPanel != null)
                _counterBaseScale = CounterPanel.transform.localScale;
            EnsureSoulVisualPool();
        }

        private void OnDisable()
        {
            ReleaseAllSoulVisuals();
            ResetQueryState();
            _counterPulseRemaining = 0f;
            if (CounterPanel != null)
                CounterPanel.transform.localScale = _counterBaseScale;
        }

        private void OnDestroy()
        {
            if (_soulSprite == null)
                return;

            Texture texture = _soulSprite.texture;
            Destroy(_soulSprite);
            if (texture != null)
                Destroy(texture);
            _soulSprite = null;
        }

        private void Update()
        {
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            UpdateSoulVisuals(unscaledDeltaTime);
            UpdateCounterPulse(unscaledDeltaTime);

            try
            {
                if (TryEnsureSoulQuery())
                    ProcessSoulPickupEvents();
            }
            catch (ObjectDisposedException)
            {
                ResetQueryState();
            }
            catch (InvalidOperationException)
            {
                ResetQueryState();
            }

            _checkTimer -= unscaledDeltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = CheckInterval;
                RefreshCounter(false);
            }
        }

        private void RefreshCounter(bool force)
        {
            var gm = GameManager.Instance;
            MetaRuntimeTelemetry telemetry = gm != null ? gm.GetMetaRuntimeTelemetry() : default;
            bool visible = gm != null
                           && gm.ContinuousSiegeCycle.Enabled
                           && !gm.GameState.IsGameOver
                           && telemetry.HasCurrentRewardQuote;
            if (CounterPanel != null && CounterPanel.activeSelf != visible)
                CounterPanel.SetActive(visible);
            if (!visible)
                return;

            int projectedSouls = telemetry.CurrentRewardQuote.TotalSouls;
            if ((force || projectedSouls != _lastShown) && CounterText != null)
            {
                _lastShown = projectedSouls;
                MetaPresentationSettings presentation = gm.MetaCatalog != null
                    ? gm.MetaCatalog.Presentation
                    : null;
                string currency = presentation != null
                    ? presentation.ShortName
                    : MetaProgression.CurrencyName;
                CounterText.text =
                    $"<color=#FFB33F>ON DEATH</color>  +{projectedSouls:N0} {currency}";
            }
        }

        private bool TryEnsureSoulQuery()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                ResetQueryState();
                return false;
            }

            if (_queryReady && _world == world)
                return true;

            _world = world;
            _entityManager = world.EntityManager;
            _soulPickupQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<SoulPickupEvent>());
            _queryReady = true;
            return true;
        }

        private void ResetQueryState()
        {
            _world = null;
            _queryReady = false;
        }

        private void ProcessSoulPickupEvents()
        {
            using NativeArray<Entity> entities = _soulPickupQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            using NativeArray<SoulPickupEvent> events =
                _soulPickupQuery.ToComponentDataArray<SoulPickupEvent>(Allocator.Temp);
            BuildSoulPickupPresentations(events);
            for (int i = 0; i < _soulPickupPresentations.Count; i++)
            {
                SoulPickupEvent presentation = _soulPickupPresentations[i];
                ToolkitSoulPickupRequested?.Invoke(presentation);
                PlaySoulPickup(presentation);
            }

            _entityManager.DestroyEntity(entities);
        }

        private void BuildSoulPickupPresentations(NativeArray<SoulPickupEvent> events)
        {
            _soulPickupPresentations.Clear();
            _soulPickupAggregates.Clear();
            LastProcessedSoulEventCount = 0;
            LastSoulPresentationCount = 0;
            LastProcessedSoulAmount = 0L;
            LastPresentedSoulAmount = 0L;

            Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
            Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);
            for (int i = 0; i < events.Length; i++)
            {
                SoulPickupEvent soulEvent = events[i];
                if (soulEvent.Amount <= 0)
                    continue;

                LastProcessedSoulEventCount++;
                LastProcessedSoulAmount += soulEvent.Amount;
                minimum.x = Mathf.Min(minimum.x, soulEvent.Position.x);
                minimum.y = Mathf.Min(minimum.y, soulEvent.Position.y);
                maximum.x = Mathf.Max(maximum.x, soulEvent.Position.x);
                maximum.y = Mathf.Max(maximum.y, soulEvent.Position.y);
            }

            if (LastProcessedSoulEventCount <= 0)
                return;

            int threshold = Mathf.Max(1, SoulPickupAggregationThreshold);
            if (LastProcessedSoulEventCount <= threshold)
            {
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i].Amount <= 0)
                        continue;

                    _soulPickupPresentations.Add(events[i]);
                    LastPresentedSoulAmount += events[i].Amount;
                }

                LastSoulPresentationCount = _soulPickupPresentations.Count;
                return;
            }

            int presentationLimit = Mathf.Max(1, MaxSoulPickupPresentationsPerBurst);
            float extentX = Mathf.Max(0.01f, maximum.x - minimum.x);
            float extentY = Mathf.Max(0.01f, maximum.y - minimum.y);
            float aspect = Mathf.Clamp(extentX / extentY, 0.125f, 8f);
            int gridX = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Sqrt(presentationLimit * aspect)),
                1,
                presentationLimit);
            int gridY = Mathf.Max(1, presentationLimit / gridX);

            for (int i = 0; i < events.Length; i++)
            {
                SoulPickupEvent soulEvent = events[i];
                if (soulEvent.Amount <= 0)
                    continue;

                int cellX = Mathf.Clamp(
                    Mathf.FloorToInt((soulEvent.Position.x - minimum.x) / extentX * gridX),
                    0,
                    gridX - 1);
                int cellY = Mathf.Clamp(
                    Mathf.FloorToInt((soulEvent.Position.y - minimum.y) / extentY * gridY),
                    0,
                    gridY - 1);
                var key = new SoulAggregateKey(cellX, cellY);
                _soulPickupAggregates.TryGetValue(key, out SoulAggregate aggregate);
                aggregate.PositionSum += new Vector3(
                    soulEvent.Position.x,
                    soulEvent.Position.y,
                    soulEvent.Position.z);
                aggregate.Amount += soulEvent.Amount;
                aggregate.EventCount++;
                _soulPickupAggregates[key] = aggregate;
            }

            foreach (KeyValuePair<SoulAggregateKey, SoulAggregate> pair in _soulPickupAggregates)
            {
                SoulAggregate aggregate = pair.Value;
                if (aggregate.EventCount <= 0 || aggregate.Amount <= 0L)
                    continue;

                Vector3 averagePosition = aggregate.PositionSum / aggregate.EventCount;
                int amount = aggregate.Amount >= int.MaxValue
                    ? int.MaxValue
                    : (int)aggregate.Amount;
                _soulPickupPresentations.Add(new SoulPickupEvent
                {
                    Position = new Unity.Mathematics.float3(
                        averagePosition.x,
                        averagePosition.y,
                        averagePosition.z),
                    Amount = amount
                });
                LastPresentedSoulAmount += amount;
            }

            LastSoulPresentationCount = _soulPickupPresentations.Count;
        }

        private void PlaySoulPickup(SoulPickupEvent soulEvent)
        {
            if (soulEvent.Amount <= 0 || CounterPanel == null)
                return;

            CacheCanvas();
            if (_canvasRect == null)
                return;

            SoulVisual visual = GetSoulVisual();
            if (visual.Instance == null)
                return;

            Vector3 worldPosition = new Vector3(
                soulEvent.Position.x,
                soulEvent.Position.y + Mathf.Max(0f, SoulStartWorldOffset),
                soulEvent.Position.z);
            Camera gameplayCamera = Camera.main;
            Vector2 startScreen = gameplayCamera != null
                ? (Vector2)gameplayCamera.WorldToScreenPoint(worldPosition)
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 start = ScreenToCanvasLocal(startScreen);
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(
                _uiCamera,
                CounterPanel.transform.position);
            Vector2 target = ScreenToCanvasLocal(targetScreen);
            float lateral = ((TotalSoulVisualsPlayedCount % 5L) - 2L) * 13f;
            Vector2 control = (start + target) * 0.5f
                + Vector2.up * Mathf.Max(0f, SoulArcHeight)
                + Vector2.right * lateral;

            visual.Rect.anchoredPosition = start;
            visual.Rect.localScale = Vector3.one * 0.72f;
            visual.Group.alpha = 0f;
            visual.Icon.color = SoulColor;
            if (visual.AmountText != null)
                visual.AmountText.text = "+" + soulEvent.Amount;
            visual.Instance.SetActive(true);

            _activeSoulVisuals.Add(new ActiveSoulVisual
            {
                Visual = visual,
                Start = start,
                Control = control,
                Target = target,
                Elapsed = 0f,
                Duration = Mathf.Max(0.2f, SoulTravelDuration)
            });
            TotalSoulVisualsPlayedCount++;
        }

        private void UpdateSoulVisuals(float dt)
        {
            for (int i = _activeSoulVisuals.Count - 1; i >= 0; i--)
            {
                ActiveSoulVisual active = _activeSoulVisuals[i];
                if (active.Visual.Instance == null)
                {
                    _activeSoulVisuals.RemoveAt(i);
                    continue;
                }

                active.Elapsed += Mathf.Max(0f, dt);
                float progress01 = Mathf.Clamp01(active.Elapsed / active.Duration);
                float eased = progress01 * progress01 * (3f - 2f * progress01);
                active.Visual.Rect.anchoredPosition = QuadraticBezier(
                    active.Start,
                    active.Control,
                    active.Target,
                    eased);

                float scale = progress01 < 0.2f
                    ? Mathf.Lerp(0.72f, 1.12f, progress01 / 0.2f)
                    : Mathf.Lerp(1.12f, 0.64f, (progress01 - 0.2f) / 0.8f);
                active.Visual.Rect.localScale = Vector3.one * scale;
                active.Visual.Group.alpha = progress01 < 0.12f
                    ? progress01 / 0.12f
                    : 1f;

                if (progress01 >= 1f)
                {
                    ReleaseSoulVisual(active.Visual);
                    _activeSoulVisuals.RemoveAt(i);
                    RefreshCounter(true);
                    StartCounterPulse();
                    continue;
                }

                _activeSoulVisuals[i] = active;
            }
        }

        private void StartCounterPulse()
        {
            _counterPulseRemaining = Mathf.Max(0.01f, CounterPulseDuration);
        }

        private void UpdateCounterPulse(float dt)
        {
            if (CounterPanel == null)
                return;

            if (_counterPulseRemaining <= 0f)
            {
                CounterPanel.transform.localScale = _counterBaseScale;
                return;
            }

            float duration = Mathf.Max(0.01f, CounterPulseDuration);
            _counterPulseRemaining = Mathf.Max(0f, _counterPulseRemaining - Mathf.Max(0f, dt));
            float progress01 = 1f - _counterPulseRemaining / duration;
            float pulse = Mathf.Sin(progress01 * Mathf.PI);
            float scale = Mathf.Lerp(1f, Mathf.Max(1f, CounterPulseScale), pulse);
            CounterPanel.transform.localScale = _counterBaseScale * scale;
        }

        private void CacheCanvas()
        {
            if (_canvasRect != null)
                return;

            _canvas = CounterPanel != null
                ? CounterPanel.GetComponentInParent<Canvas>()
                : GetComponentInParent<Canvas>();
            if (_canvas == null)
                return;

            _canvasRect = _canvas.transform as RectTransform;
            _uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;
        }

        private Vector2 ScreenToCanvasLocal(Vector2 screenPosition)
        {
            if (_canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    screenPosition,
                    _uiCamera,
                    out Vector2 localPoint))
                return localPoint;

            return Vector2.zero;
        }

        private void EnsureSoulVisualPool()
        {
            CacheCanvas();
            if (_canvasRect == null)
                return;

            int targetSize = Mathf.Max(0, InitialSoulVisualPoolSize);
            while (_soulVisualPool.Count + _activeSoulVisuals.Count < targetSize)
                _soulVisualPool.Enqueue(CreateSoulVisual(
                    _soulVisualPool.Count + _activeSoulVisuals.Count));
        }

        private SoulVisual GetSoulVisual()
        {
            EnsureSoulVisualPool();
            if (_soulVisualPool.Count > 0)
                return _soulVisualPool.Dequeue();

            return CreateSoulVisual(_activeSoulVisuals.Count);
        }

        private SoulVisual CreateSoulVisual(int index)
        {
            if (_canvasRect == null)
                return default;

            var instance = new GameObject(
                "SoulPickup_" + index,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            var rect = instance.GetComponent<RectTransform>();
            rect.SetParent(_canvasRect, false);
            rect.sizeDelta = new Vector2(30f, 30f);
            rect.SetAsLastSibling();

            var group = instance.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var icon = instance.GetComponent<Image>();
            icon.sprite = GetSoulSprite();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = SoulColor;

            var amountObject = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
            var amountRect = amountObject.GetComponent<RectTransform>();
            amountRect.SetParent(rect, false);
            amountRect.anchorMin = new Vector2(0.5f, 0.5f);
            amountRect.anchorMax = new Vector2(0.5f, 0.5f);
            amountRect.anchoredPosition = new Vector2(24f, 0f);
            amountRect.sizeDelta = new Vector2(38f, 24f);
            var amountText = amountObject.GetComponent<TextMeshProUGUI>();
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.fontSize = 15f;
            amountText.fontStyle = FontStyles.Bold;
            amountText.color = Color.white;
            amountText.raycastTarget = false;

            instance.SetActive(false);
            return new SoulVisual
            {
                Instance = instance,
                Rect = rect,
                Group = group,
                Icon = icon,
                AmountText = amountText
            };
        }

        private Sprite GetSoulSprite()
        {
            if (_soulSprite != null)
                return _soulSprite;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeSoulPickupTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / radius;
                    float ny = (y - center) / radius;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float core = Mathf.Clamp01(1f - distance);
                    float glow = Mathf.Clamp01(1f - distance * 0.86f);
                    byte alpha = (byte)(Mathf.Pow(glow, 1.6f) * 255f);
                    byte brightness = (byte)Mathf.Lerp(170f, 255f, core);
                    pixels[y * size + x] = new Color32(
                        brightness,
                        255,
                        255,
                        alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            _soulSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _soulSprite.name = "RuntimeSoulPickupSprite";
            return _soulSprite;
        }

        private void ReleaseAllSoulVisuals()
        {
            for (int i = _activeSoulVisuals.Count - 1; i >= 0; i--)
                ReleaseSoulVisual(_activeSoulVisuals[i].Visual);

            _activeSoulVisuals.Clear();
        }

        private void ReleaseSoulVisual(SoulVisual visual)
        {
            if (visual.Instance == null)
                return;

            visual.Instance.SetActive(false);
            _soulVisualPool.Enqueue(visual);
        }

        private static Vector2 QuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float progress01)
        {
            float inverse = 1f - progress01;
            return inverse * inverse * start
                   + 2f * inverse * progress01 * control
                   + progress01 * progress01 * end;
        }
    }
}
