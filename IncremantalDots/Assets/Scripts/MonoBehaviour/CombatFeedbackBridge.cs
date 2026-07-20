using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using TMPro;

namespace DeadWalls
{
    public class CombatFeedbackBridge : MonoBehaviour
    {
        [Header("Central Audio Profile")]
        public DeadWallsAudioProfileSO AudioProfile;

        [Header("VFX Prefabs")]
        public GameObject ArrowMuzzlePrefab;
        public GameObject ArrowHitPrefab;
        public GameObject FrostHitPrefab;
        public GameObject CastleHitPrefab;

        [Header("Audio Clips")]
        public AudioClip ArrowShootClip;
        public AudioClip[] ArrowShootClips;
        public AudioClip ArrowHitClip;
        public AudioClip FrostHitClip;
        public AudioClip CastleHitClip;
        public AudioClip FireballBlastClip;

        [Header("Hit Flipbook")]
        public Sprite[] HitFlipbookSprites;
        public int HitFlipbookPoolSize = 128;
        public float HitFlipbookFrameRate = 90f;
        public float HitFlipbookScale = 0.35f;
        public float HitFlipbookRotationOffsetDegrees;
        public string HitFlipbookSortingLayer = "Wall";
        public int HitFlipbookSortingOrder = 12;

        [Header("Frost Feedback Hierarchy")]
        public int FrostHitSortingOrder = SpellFeedbackHierarchy.FrostHitSortingOrder;
        public float FrostHitScaleMultiplier = SpellFeedbackHierarchy.FrostHitScaleMultiplier;
        public float FrostRingStartScale = SpellFeedbackHierarchy.FrostRingStartScale;
        public float FrostRingEndScale = SpellFeedbackHierarchy.FrostRingEndScale;
        public Color FrostHitColor = SpellFeedbackHierarchy.FrostHitColor;
        public Color FrostRingColor = SpellFeedbackHierarchy.FrostRingColor;

        [Header("Hit VFX Budget")]
        public int MaxHitVfxPlayedPerFrame = CombatHitFeedbackBudget.MaxVfxEventsPerFrame;
        public float HitVfxMinInterval = 0.04f;

        [Header("Damage Numbers")]
        public int DamageNumberInitialPoolSize = 256;
        public int DamageNumberBatchCapacity = 512;
        public int DamageNumberAggregationThreshold = 512;
        public float DamageNumberAggregationCellSize = 0.6f;
        public float DamageNumberDuration = 0.72f;
        public float DamageNumberRiseDistance = 0.62f;
        public float DamageNumberHeadOffset = 0.56f;
        public float DamageNumberWorldScale = 0.13f;
        public string DamageNumberSortingLayer = "Wall";
        public int DamageNumberSortingOrder = 80;

        [Header("Pool")]
        public int VfxPoolSizePerType = 24;
        public int MaxVfxPlayedPerFrame = 24;
        public int AudioPoolSize = 16;
        public int MaxSfxPlayedPerFrame = 4;
        public bool DisableInStressMode = true;

        [Header("VFX Scale")]
        public float ArrowMuzzleScale = 0.18f;
        public float ArrowHitScale = 0.08f;
        public float FrostHitScale = 0.11f;
        public float CastleHitScale = 0.35f;

        [Header("VFX Rotation")]
        public float ArrowMuzzleRotationOffsetDegrees;
        public float ArrowHitRotationOffsetDegrees = 90f;
        public float FrostHitRotationOffsetDegrees;
        public float CastleHitRotationOffsetDegrees;

        [Header("Audio Rate Limit")]
        public float ShootSfxMinInterval = 0.075f;
        public float NightShootSfxMinInterval = 0.12f;
        public float HitSfxMinInterval = 0.08f;
        public float CastleHitSfxMinInterval = 0.18f;
        public float FireballBlastSfxMinInterval = 0.2f;
        [Range(0f, 1f)] public float MaxArcherSalvoVolume = 0.62f;
        [Range(0f, 0.2f)] public float ArcherSalvoPitchDepth = 0.08f;

        [Header("Castle Hit Feel (M-D)")]
        [Tooltip("CastleHit SFX calindiginda kamera sarsintisi siddeti (0 = kapali).")]
        public float CastleHitShakeTrauma = 0.35f;
        [Tooltip("CastleHit SFX calindiginda ekran flash'i tetiklenir.")]
        public bool CastleHitFlashEnabled = true;
        public float PitchRandomMin = 0.94f;
        public float PitchRandomMax = 1.06f;
        [Range(0f, 1f)] public float SpatialBlend = 0.45f;

        [Header("Render")]
        public string VfxSortingLayer = "Wall";
        public int VfxSortingOrder = 12;

        private readonly Dictionary<CombatVfxType, Queue<GameObject>> _vfxPools = new Dictionary<CombatVfxType, Queue<GameObject>>();
        private readonly List<ActiveVfx> _activeVfx = new List<ActiveVfx>();
        private readonly Queue<FlipbookVfx> _hitFlipbookPool = new Queue<FlipbookVfx>();
        private readonly List<FlipbookVfx> _activeHitFlipbooks = new List<FlipbookVfx>();
        private readonly List<AudioSource> _audioSources = new List<AudioSource>();
        private readonly Queue<DamageNumberBatchVisual> _damageNumberBatchPool =
            new Queue<DamageNumberBatchVisual>();
        private readonly List<DamageNumberBatchVisual> _activeDamageNumberBatches =
            new List<DamageNumberBatchVisual>();
        private readonly Dictionary<DamageNumberAggregateKey, DamageNumberAggregate>
            _damageNumberAggregates =
                new Dictionary<DamageNumberAggregateKey, DamageNumberAggregate>(1024);
        private readonly List<DamageNumberPresentation> _damageNumberPresentations =
            new List<DamageNumberPresentation>(1024);
        private int _activeDamageNumberCount;
        private Sprite _hitHierarchyRingSprite;
        // Boyut enum uzunlugundan buyuk tutulur (yeni SFX tipi eklerken tasma olmasin)
        private readonly float[] _lastSfxTimes = { -999f, -999f, -999f, -999f, -999f, -999f, -999f, -999f };
        private readonly int[] _sfxAggregateCounts = new int[8];
        private readonly float3[] _sfxAggregatePositions = new float3[8];
        private readonly float[] _sfxAggregateVolumes = new float[8];
        private readonly float[] _sfxAggregatePitches = new float[8];

        private static readonly CombatSfxType[] SfxPlaybackPriority =
        {
            CombatSfxType.FireballBlast,
            CombatSfxType.CastleHit,
            CombatSfxType.FrostHit,
            CombatSfxType.ArrowShoot,
            CombatSfxType.ArrowHit
        };

        public int LastProcessedSfxEventCount { get; private set; }
        public int LastFrameSfxPlayedCount { get; private set; }
        public int LastArrowSalvoSize { get; private set; }
        public int TotalSfxPlayedCount { get; private set; }
        public int TotalArrowSalvosPlayed { get; private set; }
        public int LastProcessedVfxEventCount { get; private set; }
        public int LastFrameHitVfxPlayedCount { get; private set; }
        public int LastFrameHitVfxDroppedCount { get; private set; }
        public int LastFrameFrostHitVfxPlayedCount { get; private set; }
        public long TotalHitVfxPlayedCount { get; private set; }
        public long TotalHitVfxDroppedCount { get; private set; }
        public int ActiveHitFlipbookCount => _activeHitFlipbooks.Count;
        public int ActiveDamageNumberCount => _activeDamageNumberCount;
        public int ActiveDamageNumberBatchCount => _activeDamageNumberBatches.Count;
        public int LastProcessedDamageNumberEventCount { get; private set; }
        public int LastDamageNumberPresentationCount { get; private set; }
        public double LastProcessedDamageNumberTotal { get; private set; }
        public double LastPresentedDamageNumberTotal { get; private set; }
        public long TotalDamageNumbersPlayedCount { get; private set; }

        private World _world;
        private EntityManager _entityManager;
        private EntityQuery _vfxQuery;
        private EntityQuery _sfxQuery;
        private EntityQuery _damageNumberQuery;
        private EntityQuery _waveQuery;
        private bool _queriesReady;
        private int _audioCursor;
        private float _lastHitVfxPlaybackTime = -999f;

        private struct ActiveVfx
        {
            public GameObject Instance;
            public CombatVfxType Type;
            public float Remaining;
            public Entity FollowTarget;
            public Vector3 FollowOffset;
            public bool FollowTargetPosition;
        }

        private struct FlipbookVfx
        {
            public GameObject Instance;
            public SpriteRenderer Renderer;
            public SpriteRenderer HierarchyRenderer;
            public CombatVfxType Type;
            public float Elapsed;
        }

        private struct DamageNumberBatchEntry
        {
            public int StartCharacterIndex;
            public int CharacterLength;
            public Vector3 StartPosition;
            public Vector3 BaseCenter;
            public Color32 BaseColor;
        }

        private readonly struct DamageNumberAggregateKey :
            IEquatable<DamageNumberAggregateKey>
        {
            public readonly int X;
            public readonly int Y;
            public readonly PlayerDamageSourceType Source;

            public DamageNumberAggregateKey(
                int x,
                int y,
                PlayerDamageSourceType source)
            {
                X = x;
                Y = y;
                Source = source;
            }

            public bool Equals(DamageNumberAggregateKey other)
            {
                return X == other.X && Y == other.Y && Source == other.Source;
            }

            public override bool Equals(object obj)
            {
                return obj is DamageNumberAggregateKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = X;
                    hash = (hash * 397) ^ Y;
                    return (hash * 397) ^ (int)Source;
                }
            }
        }

        private struct DamageNumberAggregate
        {
            public float3 PositionSum;
            public float AppliedDamage;
            public int EventCount;
        }

        private struct DamageNumberPresentation
        {
            public CombatDamageNumberEvent DamageEvent;
            public int SourceEventCount;
        }

        private sealed class DamageNumberBatchVisual
        {
            public GameObject Instance;
            public TextMeshPro Text;
            public readonly List<DamageNumberBatchEntry> Entries =
                new List<DamageNumberBatchEntry>(512);
            public readonly StringBuilder TextBuilder = new StringBuilder(4096);
            public TMP_MeshInfo[] BaseMeshInfo;
            public float Elapsed;
            public float Duration;
            public float RiseDistance;
            public float BaseScale;
            public int SourceEventCount;
        }

        private void Awake()
        {
            EnsureHitFlipbookPool();
            EnsureDamageNumberPool();
            EnsureAudioPool();
        }

        private void OnDisable()
        {
            ReleaseAllActiveHitFlipbooks();
            ReleaseAllActiveVfx();
            ReleaseAllActiveDamageNumbers();
            ResetQueryState();
        }

        private void OnDestroy()
        {
            if (_hitHierarchyRingSprite == null)
                return;

            Texture texture = _hitHierarchyRingSprite.texture;
            Destroy(_hitHierarchyRingSprite);
            if (texture != null)
                Destroy(texture);
            _hitHierarchyRingSprite = null;
        }

        private void Update()
        {
            try
            {
                bool queriesReady = TryEnsureQueries();
                UpdateActiveHitFlipbooks(Time.deltaTime);
                ReleaseExpiredVfx(Time.deltaTime, queriesReady);
                UpdateActiveDamageNumbers(Time.deltaTime);
                if (!queriesReady)
                    return;

                bool stressMode = DisableInStressMode && IsStressMode();
                ProcessDamageNumberEvents();
                ProcessVfxEvents(stressMode);
                ProcessSfxEvents(stressMode);
            }
            catch (ObjectDisposedException)
            {
                ResetQueryState();
            }
            catch (InvalidOperationException)
            {
                ResetQueryState();
            }
        }

        private bool TryEnsureQueries()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                ResetQueryState();
                return false;
            }

            if (_queriesReady && _world == world)
                return true;

            _world = world;
            _entityManager = world.EntityManager;
            _vfxQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CombatVfxEvent>());
            _sfxQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CombatSfxEvent>());
            _damageNumberQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CombatDamageNumberEvent>());
            _waveQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<WaveStateData>());
            _queriesReady = true;
            return true;
        }

        private void ResetQueryState()
        {
            _world = null;
            _queriesReady = false;
        }

        private bool IsStressMode()
        {
            if (!_queriesReady)
                return false;

            using NativeArray<WaveStateData> waves = _waveQuery.ToComponentDataArray<WaveStateData>(Allocator.Temp);
            return waves.Length > 0 && waves[0].StressTestMode;
        }

        private void ProcessVfxEvents(bool skipPlayback)
        {
            using NativeArray<Entity> entities = _vfxQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            using NativeArray<CombatVfxEvent> events = _vfxQuery.ToComponentDataArray<CombatVfxEvent>(Allocator.Temp);
            LastProcessedVfxEventCount = events.Length;
            LastFrameHitVfxPlayedCount = 0;
            LastFrameHitVfxDroppedCount = 0;
            LastFrameFrostHitVfxPlayedCount = 0;

            if (!skipPlayback)
            {
                int hitEventCount = 0;
                for (int i = 0; i < events.Length; i++)
                {
                    if (IsHitFlipbookType(events[i].Type))
                        hitEventCount++;
                }

                int hitBudget = Mathf.Max(0, MaxHitVfxPlayedPerFrame);
                bool hitWindowOpen = Time.time - _lastHitVfxPlaybackTime
                    >= Mathf.Max(0f, HitVfxMinInterval);
                if (hitWindowOpen && hitBudget > 0)
                {
                    LastFrameFrostHitVfxPlayedCount = PlayHitVfxType(
                        events,
                        CombatVfxType.FrostHit,
                        hitBudget - LastFrameHitVfxPlayedCount);
                    LastFrameHitVfxPlayedCount += LastFrameFrostHitVfxPlayedCount;
                    LastFrameHitVfxPlayedCount += PlayHitVfxType(
                        events,
                        CombatVfxType.ArrowHit,
                        hitBudget - LastFrameHitVfxPlayedCount);
                    if (LastFrameHitVfxPlayedCount > 0)
                        _lastHitVfxPlaybackTime = Time.time;
                }

                LastFrameHitVfxDroppedCount = Mathf.Max(
                    0,
                    hitEventCount - LastFrameHitVfxPlayedCount);
                TotalHitVfxPlayedCount += LastFrameHitVfxPlayedCount;
                TotalHitVfxDroppedCount += LastFrameHitVfxDroppedCount;

                int particlePlayed = 0;
                for (int i = 0; i < events.Length; i++)
                {
                    if (IsHitFlipbookType(events[i].Type))
                        continue;

                    if (particlePlayed >= MaxVfxPlayedPerFrame)
                        continue;

                    if (PlayVfx(events[i]))
                        particlePlayed++;
                }
            }

            _entityManager.DestroyEntity(entities);
        }

        private int PlayHitVfxType(
            NativeArray<CombatVfxEvent> events,
            CombatVfxType type,
            int budget)
        {
            if (budget <= 0)
                return 0;

            int played = 0;
            for (int i = 0; i < events.Length && played < budget; i++)
            {
                if (events[i].Type == type && PlayVfx(events[i]))
                    played++;
            }

            return played;
        }

        private void ProcessSfxEvents(bool skipPlayback)
        {
            using NativeArray<Entity> entities = _sfxQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            using NativeArray<CombatSfxEvent> events = _sfxQuery.ToComponentDataArray<CombatSfxEvent>(Allocator.Temp);
            LastProcessedSfxEventCount = events.Length;
            LastFrameSfxPlayedCount = 0;
            LastArrowSalvoSize = 0;

            if (!skipPlayback)
            {
                ResetSfxAggregates();
                for (int i = 0; i < events.Length; i++)
                    AggregateSfxEvent(events[i]);

                LastArrowSalvoSize = _sfxAggregateCounts[(int)CombatSfxType.ArrowShoot];
                int playbackBudget = Mathf.Max(1, MaxSfxPlayedPerFrame);
                for (int i = 0; i < SfxPlaybackPriority.Length; i++)
                {
                    CombatSfxType type = SfxPlaybackPriority[i];
                    int typeIndex = (int)type;
                    int count = _sfxAggregateCounts[typeIndex];
                    if (count <= 0)
                        continue;

                    if (!TryPlaySfx(BuildAggregatedSfxEvent(type, count)))
                        continue;

                    LastFrameSfxPlayedCount++;
                    TotalSfxPlayedCount++;
                    if (type == CombatSfxType.ArrowShoot)
                        TotalArrowSalvosPlayed++;
                    if (LastFrameSfxPlayedCount >= playbackBudget)
                        break;
                }
            }

            _entityManager.DestroyEntity(entities);
        }

        private void ProcessDamageNumberEvents()
        {
            using NativeArray<Entity> entities = _damageNumberQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            using NativeArray<CombatDamageNumberEvent> events =
                _damageNumberQuery.ToComponentDataArray<CombatDamageNumberEvent>(Allocator.Temp);
            BuildDamageNumberPresentations(events);
            int capacity = Mathf.Max(1, DamageNumberBatchCapacity);
            for (int start = 0;
                 start < _damageNumberPresentations.Count;
                 start += capacity)
            {
                int count = Mathf.Min(
                    capacity,
                    _damageNumberPresentations.Count - start);
                PlayDamageNumberBatch(_damageNumberPresentations, start, count);
            }

            _entityManager.DestroyEntity(entities);
        }

        private void BuildDamageNumberPresentations(
            NativeArray<CombatDamageNumberEvent> events)
        {
            _damageNumberPresentations.Clear();
            _damageNumberAggregates.Clear();
            LastProcessedDamageNumberEventCount = 0;
            LastDamageNumberPresentationCount = 0;
            LastProcessedDamageNumberTotal = 0d;
            LastPresentedDamageNumberTotal = 0d;

            int threshold = Mathf.Max(1, DamageNumberAggregationThreshold);
            if (events.Length <= threshold)
            {
                for (int i = 0; i < events.Length; i++)
                {
                    CombatDamageNumberEvent damageEvent = events[i];
                    if (damageEvent.AppliedDamage <= 0f)
                        continue;

                    _damageNumberPresentations.Add(new DamageNumberPresentation
                    {
                        DamageEvent = damageEvent,
                        SourceEventCount = 1
                    });
                    LastProcessedDamageNumberEventCount++;
                    LastProcessedDamageNumberTotal += damageEvent.AppliedDamage;
                }

                LastDamageNumberPresentationCount = _damageNumberPresentations.Count;
                LastPresentedDamageNumberTotal = LastProcessedDamageNumberTotal;
                return;
            }

            float cellSize = Mathf.Max(0.05f, DamageNumberAggregationCellSize);
            for (int i = 0; i < events.Length; i++)
            {
                CombatDamageNumberEvent damageEvent = events[i];
                if (damageEvent.AppliedDamage <= 0f)
                    continue;

                var key = new DamageNumberAggregateKey(
                    Mathf.FloorToInt(damageEvent.Position.x / cellSize),
                    Mathf.FloorToInt(damageEvent.Position.y / cellSize),
                    damageEvent.Source);
                _damageNumberAggregates.TryGetValue(
                    key,
                    out DamageNumberAggregate aggregate);
                aggregate.PositionSum += damageEvent.Position;
                aggregate.AppliedDamage += damageEvent.AppliedDamage;
                aggregate.EventCount++;
                _damageNumberAggregates[key] = aggregate;
                LastProcessedDamageNumberEventCount++;
                LastProcessedDamageNumberTotal += damageEvent.AppliedDamage;
            }

            foreach (KeyValuePair<DamageNumberAggregateKey, DamageNumberAggregate> pair
                     in _damageNumberAggregates)
            {
                DamageNumberAggregate aggregate = pair.Value;
                if (aggregate.EventCount <= 0)
                    continue;

                _damageNumberPresentations.Add(new DamageNumberPresentation
                {
                    DamageEvent = new CombatDamageNumberEvent
                    {
                        Position = aggregate.PositionSum / aggregate.EventCount,
                        AppliedDamage = aggregate.AppliedDamage,
                        Source = pair.Key.Source
                    },
                    SourceEventCount = aggregate.EventCount
                });
                LastPresentedDamageNumberTotal += aggregate.AppliedDamage;
            }

            LastDamageNumberPresentationCount = _damageNumberPresentations.Count;
        }

        private void PlayDamageNumberBatch(
            List<DamageNumberPresentation> presentations,
            int start,
            int count)
        {
            DamageNumberBatchVisual batch = GetDamageNumberBatchVisual();
            if (batch?.Instance == null || batch.Text == null)
                return;

            batch.Entries.Clear();
            batch.TextBuilder.Clear();
            batch.TextBuilder.EnsureCapacity(Mathf.Max(
                batch.TextBuilder.Capacity,
                count * 8));

            long firstSequence = TotalDamageNumbersPlayedCount;
            int sourceEventCount = 0;
            for (int i = 0; i < count; i++)
            {
                DamageNumberPresentation presentation = presentations[start + i];
                CombatDamageNumberEvent damageEvent = presentation.DamageEvent;
                if (damageEvent.AppliedDamage <= 0f)
                    continue;

                string formattedDamage = FormatDamageNumber(damageEvent.AppliedDamage);
                int lane = (int)((firstSequence + batch.Entries.Count) % 5L) - 2;
                var entry = new DamageNumberBatchEntry
                {
                    StartCharacterIndex = batch.TextBuilder.Length,
                    CharacterLength = formattedDamage.Length,
                    StartPosition = new Vector3(
                        damageEvent.Position.x + lane * 0.035f,
                        damageEvent.Position.y + Mathf.Max(0f, DamageNumberHeadOffset),
                        0f),
                    BaseCenter = Vector3.zero,
                    BaseColor = ResolveDamageNumberColor(damageEvent.Source)
                };
                batch.Entries.Add(entry);
                batch.TextBuilder.Append(formattedDamage);
                batch.TextBuilder.Append('\n');
                sourceEventCount += Mathf.Max(1, presentation.SourceEventCount);
            }

            if (batch.Entries.Count == 0)
            {
                ReleaseDamageNumberBatchVisual(batch);
                return;
            }

            batch.Elapsed = 0f;
            batch.Duration = Mathf.Max(0.1f, DamageNumberDuration);
            batch.RiseDistance = Mathf.Max(0.05f, DamageNumberRiseDistance);
            batch.BaseScale = Mathf.Max(0.01f, DamageNumberWorldScale);
            batch.SourceEventCount = sourceEventCount;
            batch.Text.text = batch.TextBuilder.ToString();
            batch.Instance.SetActive(true);
            batch.Text.ForceMeshUpdate(true, true);
            batch.BaseMeshInfo = batch.Text.textInfo.CopyMeshInfoVertexData();
            ResolveDamageNumberBatchCenters(batch);
            ApplyDamageNumberBatchAnimation(batch, 0f);

            _activeDamageNumberBatches.Add(batch);
            _activeDamageNumberCount += batch.SourceEventCount;
            TotalDamageNumbersPlayedCount += batch.SourceEventCount;
        }

        private void UpdateActiveDamageNumbers(float dt)
        {
            for (int i = _activeDamageNumberBatches.Count - 1; i >= 0; i--)
            {
                DamageNumberBatchVisual batch = _activeDamageNumberBatches[i];
                if (batch?.Instance == null || batch.Text == null)
                {
                    _activeDamageNumberCount -= batch?.SourceEventCount ?? 0;
                    _activeDamageNumberBatches.RemoveAt(i);
                    continue;
                }

                batch.Elapsed += Mathf.Max(0f, dt);
                float progress01 = Mathf.Clamp01(batch.Elapsed / batch.Duration);
                ApplyDamageNumberBatchAnimation(batch, progress01);

                if (progress01 >= 1f)
                {
                    _activeDamageNumberCount -= batch.SourceEventCount;
                    _activeDamageNumberBatches.RemoveAt(i);
                    ReleaseDamageNumberBatchVisual(batch);
                    continue;
                }
            }
        }

        private void EnsureDamageNumberPool()
        {
            int capacity = Mathf.Max(1, DamageNumberBatchCapacity);
            int targetBatchCount = Mathf.CeilToInt(
                Mathf.Max(0, DamageNumberInitialPoolSize) / (float)capacity);
            while (_damageNumberBatchPool.Count + _activeDamageNumberBatches.Count
                   < targetBatchCount)
            {
                _damageNumberBatchPool.Enqueue(CreateDamageNumberBatchVisual(
                    _damageNumberBatchPool.Count + _activeDamageNumberBatches.Count));
            }
        }

        private DamageNumberBatchVisual GetDamageNumberBatchVisual()
        {
            if (_damageNumberBatchPool.Count > 0)
                return _damageNumberBatchPool.Dequeue();

            return CreateDamageNumberBatchVisual(
                _damageNumberBatchPool.Count + _activeDamageNumberBatches.Count);
        }

        private DamageNumberBatchVisual CreateDamageNumberBatchVisual(int index)
        {
            var instance = new GameObject("DamageNumberBatch_" + index);
            instance.transform.SetParent(transform, false);
            instance.transform.position = new Vector3(
                0f, 0f, MobileCastleRenderDepth.ProjectileZ);
            var text = instance.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 5f;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(8192f, 8192f);
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color32(16, 10, 8, 225);
            MeshRenderer meshRenderer = text.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerName = DamageNumberSortingLayer;
                meshRenderer.sortingOrder = DamageNumberSortingOrder;
            }

            instance.SetActive(false);
            return new DamageNumberBatchVisual
            {
                Instance = instance,
                Text = text
            };
        }

        private void ReleaseAllActiveDamageNumbers()
        {
            for (int i = _activeDamageNumberBatches.Count - 1; i >= 0; i--)
                ReleaseDamageNumberBatchVisual(_activeDamageNumberBatches[i]);

            _activeDamageNumberBatches.Clear();
            _activeDamageNumberCount = 0;
        }

        private void ReleaseDamageNumberBatchVisual(DamageNumberBatchVisual batch)
        {
            if (batch?.Instance == null)
                return;

            batch.Instance.SetActive(false);
            batch.Text.text = string.Empty;
            batch.Entries.Clear();
            batch.BaseMeshInfo = null;
            batch.SourceEventCount = 0;
            _damageNumberBatchPool.Enqueue(batch);
        }

        private static void ResolveDamageNumberBatchCenters(DamageNumberBatchVisual batch)
        {
            TMP_TextInfo textInfo = batch.Text.textInfo;
            for (int entryIndex = 0; entryIndex < batch.Entries.Count; entryIndex++)
            {
                DamageNumberBatchEntry entry = batch.Entries[entryIndex];
                Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
                Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);
                bool foundVisibleCharacter = false;
                int end = Mathf.Min(
                    textInfo.characterCount,
                    entry.StartCharacterIndex + entry.CharacterLength);
                for (int charIndex = entry.StartCharacterIndex;
                     charIndex < end;
                     charIndex++)
                {
                    TMP_CharacterInfo character = textInfo.characterInfo[charIndex];
                    if (!character.isVisible)
                        continue;

                    int materialIndex = character.materialReferenceIndex;
                    int vertexIndex = character.vertexIndex;
                    Vector3[] vertices = batch.BaseMeshInfo[materialIndex].vertices;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        Vector3 vertex = vertices[vertexIndex + corner];
                        min = Vector3.Min(min, vertex);
                        max = Vector3.Max(max, vertex);
                    }

                    foundVisibleCharacter = true;
                }

                entry.BaseCenter = foundVisibleCharacter
                    ? (min + max) * 0.5f
                    : Vector3.zero;
                batch.Entries[entryIndex] = entry;
            }
        }

        private static void ApplyDamageNumberBatchAnimation(
            DamageNumberBatchVisual batch,
            float progress01)
        {
            if (batch.BaseMeshInfo == null)
                return;

            TMP_TextInfo textInfo = batch.Text.textInfo;
            for (int materialIndex = 0;
                 materialIndex < textInfo.meshInfo.Length
                 && materialIndex < batch.BaseMeshInfo.Length;
                 materialIndex++)
            {
                Vector3[] sourceVertices = batch.BaseMeshInfo[materialIndex].vertices;
                Vector3[] targetVertices = textInfo.meshInfo[materialIndex].vertices;
                Array.Copy(sourceVertices, targetVertices,
                    Mathf.Min(sourceVertices.Length, targetVertices.Length));
            }

            float easedRise = 1f - (1f - progress01) * (1f - progress01);
            float scaleMultiplier = progress01 < 0.18f
                ? Mathf.Lerp(0.72f, 1.12f, progress01 / 0.18f)
                : Mathf.Lerp(1.12f, 1f, (progress01 - 0.18f) / 0.82f);
            float alpha = progress01 < 0.58f
                ? 1f
                : 1f - (progress01 - 0.58f) / 0.42f;
            float scale = batch.BaseScale * scaleMultiplier;

            for (int entryIndex = 0; entryIndex < batch.Entries.Count; entryIndex++)
            {
                DamageNumberBatchEntry entry = batch.Entries[entryIndex];
                Vector3 targetCenter = entry.StartPosition
                    + Vector3.up * (batch.RiseDistance * easedRise);
                Color32 color = entry.BaseColor;
                color.a = (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01(alpha));
                int end = Mathf.Min(
                    textInfo.characterCount,
                    entry.StartCharacterIndex + entry.CharacterLength);
                for (int charIndex = entry.StartCharacterIndex;
                     charIndex < end;
                     charIndex++)
                {
                    TMP_CharacterInfo character = textInfo.characterInfo[charIndex];
                    if (!character.isVisible)
                        continue;

                    int materialIndex = character.materialReferenceIndex;
                    int vertexIndex = character.vertexIndex;
                    Vector3[] sourceVertices = batch.BaseMeshInfo[materialIndex].vertices;
                    Vector3[] targetVertices = textInfo.meshInfo[materialIndex].vertices;
                    Color32[] targetColors = textInfo.meshInfo[materialIndex].colors32;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        int index = vertexIndex + corner;
                        targetVertices[index] =
                            (sourceVertices[index] - entry.BaseCenter) * scale
                            + targetCenter;
                        targetColors[index] = color;
                    }
                }
            }

            batch.Text.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices
                | TMP_VertexDataUpdateFlags.Colors32);
        }

        public static string FormatDamageNumber(float appliedDamage)
        {
            float safeDamage = float.IsNaN(appliedDamage) || float.IsInfinity(appliedDamage)
                ? 0f
                : Mathf.Max(0f, appliedDamage);
            float rounded = Mathf.Round(safeDamage);
            if (Mathf.Abs(safeDamage - rounded) <= 0.01f)
                return Mathf.RoundToInt(rounded).ToString();

            return safeDamage.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static Color ResolveDamageNumberColor(PlayerDamageSourceType source)
        {
            return source switch
            {
                PlayerDamageSourceType.FrostArrow => new Color32(112, 226, 255, 255),
                PlayerDamageSourceType.Fireball => new Color32(255, 155, 64, 255),
                PlayerDamageSourceType.FireballSecondBlast => new Color32(255, 196, 90, 255),
                PlayerDamageSourceType.FireballBurningGround => new Color32(255, 112, 54, 255),
                PlayerDamageSourceType.RapidArrow => new Color32(255, 224, 142, 255),
                _ => new Color32(255, 244, 216, 255)
            };
        }

        private void ResetSfxAggregates()
        {
            Array.Clear(_sfxAggregateCounts, 0, _sfxAggregateCounts.Length);
            Array.Clear(_sfxAggregatePositions, 0, _sfxAggregatePositions.Length);
            Array.Clear(_sfxAggregateVolumes, 0, _sfxAggregateVolumes.Length);
            Array.Clear(_sfxAggregatePitches, 0, _sfxAggregatePitches.Length);
        }

        private void AggregateSfxEvent(CombatSfxEvent sfxEvent)
        {
            int index = (int)sfxEvent.Type;
            if ((uint)index >= (uint)_sfxAggregateCounts.Length)
                return;

            int multiplicity = Mathf.Max(1, sfxEvent.Multiplicity);
            _sfxAggregateCounts[index] += multiplicity;
            _sfxAggregatePositions[index] += sfxEvent.Position * multiplicity;
            _sfxAggregateVolumes[index] = Mathf.Max(
                _sfxAggregateVolumes[index],
                Mathf.Clamp01(sfxEvent.Volume));
            _sfxAggregatePitches[index] += Mathf.Max(0.05f, sfxEvent.Pitch)
                * multiplicity;
        }

        private CombatSfxEvent BuildAggregatedSfxEvent(CombatSfxType type, int count)
        {
            int index = (int)type;
            float safeCount = Mathf.Max(1, count);
            float volume = _sfxAggregateVolumes[index];
            float pitch = _sfxAggregatePitches[index] / safeCount;
            if (type == CombatSfxType.ArrowShoot)
            {
                volume = ResolveArcherSalvoVolume(count, volume, MaxArcherSalvoVolume);
                pitch *= ResolveArcherSalvoPitchMultiplier(count, ArcherSalvoPitchDepth);
            }

            return new CombatSfxEvent
            {
                Position = _sfxAggregatePositions[index] / safeCount,
                Type = type,
                Volume = volume,
                Pitch = pitch,
                Multiplicity = count
            };
        }

        private bool PlayVfx(CombatVfxEvent vfxEvent)
        {
            if (IsHitFlipbookType(vfxEvent.Type))
                return PlayHitFlipbook(vfxEvent);

            GameObject prefab = GetVfxPrefab(vfxEvent.Type);
            if (prefab == null)
                return false;

            GameObject instance = GetVfxInstance(vfxEvent.Type, prefab);
            if (instance == null)
                return false;

            Vector3 position = new Vector3(vfxEvent.Position.x, vfxEvent.Position.y, MobileCastleRenderDepth.ProjectileZ);
            instance.transform.SetPositionAndRotation(position, ResolveRotation(vfxEvent));
            instance.transform.localScale = Vector3.one * ResolveVfxScale(vfxEvent);
            instance.SetActive(true);

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Play(true);
            }

            _activeVfx.Add(new ActiveVfx
            {
                Instance = instance,
                Type = vfxEvent.Type,
                Remaining = CalculateVfxDuration(systems),
                FollowTarget = vfxEvent.FollowTarget,
                FollowOffset = new Vector3(vfxEvent.FollowOffset.x, vfxEvent.FollowOffset.y, vfxEvent.FollowOffset.z),
                FollowTargetPosition = vfxEvent.FollowTargetPosition
            });
            return true;
        }

        private bool PlayHitFlipbook(CombatVfxEvent vfxEvent)
        {
            if (HitFlipbookSprites == null || HitFlipbookSprites.Length == 0)
                return false;

            if (!TryGetHitFlipbook(out FlipbookVfx flipbook))
                return false;

            Vector3 position = new Vector3(vfxEvent.Position.x, vfxEvent.Position.y, MobileCastleRenderDepth.ProjectileZ);
            flipbook.Instance.transform.SetPositionAndRotation(position, ResolveHitFlipbookRotation(vfxEvent));
            bool frostHit = vfxEvent.Type == CombatVfxType.FrostHit;
            float scale = frostHit
                ? SpellFeedbackHierarchy.ResolveFrostHitScale(
                    HitFlipbookScale,
                    FrostHitScaleMultiplier)
                : Mathf.Max(0.001f, HitFlipbookScale);
            flipbook.Instance.transform.localScale = Vector3.one * scale;
            flipbook.Renderer.sortingLayerName = HitFlipbookSortingLayer;
            flipbook.Renderer.sortingOrder = frostHit
                ? FrostHitSortingOrder
                : HitFlipbookSortingOrder;
            flipbook.Renderer.color = frostHit ? FrostHitColor : Color.white;
            flipbook.Renderer.sprite = HitFlipbookSprites[0];
            flipbook.Renderer.enabled = true;
            if (flipbook.HierarchyRenderer != null)
            {
                flipbook.HierarchyRenderer.sortingLayerName = HitFlipbookSortingLayer;
                flipbook.HierarchyRenderer.sortingOrder = FrostHitSortingOrder - 1;
                flipbook.HierarchyRenderer.sprite = GetHitHierarchyRingSprite();
                flipbook.HierarchyRenderer.color = FrostRingColor;
                flipbook.HierarchyRenderer.transform.localScale = Vector3.one
                    * Mathf.Max(0.01f, FrostRingStartScale);
                flipbook.HierarchyRenderer.enabled = frostHit;
            }
            flipbook.Type = vfxEvent.Type;
            flipbook.Elapsed = 0f;
            flipbook.Instance.SetActive(true);
            _activeHitFlipbooks.Add(flipbook);
            return true;
        }

        private bool TryGetHitFlipbook(out FlipbookVfx flipbook)
        {
            if (_hitFlipbookPool.Count > 0)
            {
                flipbook = _hitFlipbookPool.Dequeue();
                return true;
            }

            if (_activeHitFlipbooks.Count > 0)
            {
                flipbook = _activeHitFlipbooks[0];
                _activeHitFlipbooks.RemoveAt(0);
                return true;
            }

            flipbook = default;
            return false;
        }

        private void EnsureHitFlipbookPool()
        {
            if (HitFlipbookSprites == null || HitFlipbookSprites.Length == 0)
                return;

            int targetSize = Mathf.Max(0, HitFlipbookPoolSize);
            while (_hitFlipbookPool.Count + _activeHitFlipbooks.Count < targetSize)
                _hitFlipbookPool.Enqueue(CreateHitFlipbookInstance(_hitFlipbookPool.Count + _activeHitFlipbooks.Count));
        }

        private FlipbookVfx CreateHitFlipbookInstance(int index)
        {
            var instance = new GameObject("HitFlipbook_" + index);
            instance.transform.SetParent(transform, false);
            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = HitFlipbookSortingLayer;
            renderer.sortingOrder = HitFlipbookSortingOrder;
            renderer.enabled = false;

            var hierarchyObject = new GameObject("FrostHierarchyRing");
            hierarchyObject.transform.SetParent(instance.transform, false);
            var hierarchyRenderer = hierarchyObject.AddComponent<SpriteRenderer>();
            hierarchyRenderer.sprite = GetHitHierarchyRingSprite();
            hierarchyRenderer.sortingLayerName = HitFlipbookSortingLayer;
            hierarchyRenderer.sortingOrder = FrostHitSortingOrder - 1;
            hierarchyRenderer.enabled = false;
            instance.SetActive(false);

            return new FlipbookVfx
            {
                Instance = instance,
                Renderer = renderer,
                HierarchyRenderer = hierarchyRenderer,
                Type = CombatVfxType.ArrowHit,
                Elapsed = 0f
            };
        }

        private void UpdateActiveHitFlipbooks(float dt)
        {
            if (HitFlipbookSprites == null || HitFlipbookSprites.Length == 0)
                return;

            float frameRate = Mathf.Max(1f, HitFlipbookFrameRate);
            for (int i = _activeHitFlipbooks.Count - 1; i >= 0; i--)
            {
                FlipbookVfx active = _activeHitFlipbooks[i];
                if (active.Instance == null || active.Renderer == null)
                {
                    _activeHitFlipbooks.RemoveAt(i);
                    continue;
                }

                active.Elapsed += dt;
                int frameIndex = Mathf.FloorToInt(active.Elapsed * frameRate);
                if (frameIndex >= HitFlipbookSprites.Length)
                {
                    ReleaseHitFlipbook(active);
                    _activeHitFlipbooks.RemoveAt(i);
                    continue;
                }

                active.Renderer.sprite = HitFlipbookSprites[frameIndex];
                if (active.Type == CombatVfxType.FrostHit
                    && active.HierarchyRenderer != null)
                {
                    float duration = HitFlipbookSprites.Length / frameRate;
                    float progress01 = duration > 0f
                        ? Mathf.Clamp01(active.Elapsed / duration)
                        : 1f;
                    float ringScale = SpellFeedbackHierarchy.ResolveFrostRingScale(
                        progress01,
                        FrostRingStartScale,
                        FrostRingEndScale);
                    active.HierarchyRenderer.transform.localScale = Vector3.one * ringScale;
                    active.HierarchyRenderer.color = SpellFeedbackHierarchy.ResolveFadingColor(
                        FrostRingColor,
                        progress01);
                }
                _activeHitFlipbooks[i] = active;
            }
        }

        private void ReleaseAllActiveHitFlipbooks()
        {
            for (int i = _activeHitFlipbooks.Count - 1; i >= 0; i--)
                ReleaseHitFlipbook(_activeHitFlipbooks[i]);

            _activeHitFlipbooks.Clear();
        }

        private void ReleaseHitFlipbook(FlipbookVfx flipbook)
        {
            if (flipbook.Instance == null)
                return;

            if (flipbook.Renderer != null)
            {
                flipbook.Renderer.sprite = null;
                flipbook.Renderer.color = Color.white;
                flipbook.Renderer.enabled = false;
            }

            if (flipbook.HierarchyRenderer != null)
            {
                flipbook.HierarchyRenderer.color = FrostRingColor;
                flipbook.HierarchyRenderer.enabled = false;
            }

            flipbook.Type = CombatVfxType.ArrowHit;
            flipbook.Elapsed = 0f;
            flipbook.Instance.SetActive(false);
            _hitFlipbookPool.Enqueue(flipbook);
        }

        private Sprite GetHitHierarchyRingSprite()
        {
            if (_hitHierarchyRingSprite != null)
                return _hitHierarchyRingSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "FrostHitHierarchyRingTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(
                        new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(half, half)) / half;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.78f) / 0.16f);
                    float edge = Mathf.Clamp01((1f - distance) / 0.04f);
                    pixels[y * size + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)(Mathf.Clamp01(ring * edge) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            _hitHierarchyRingSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _hitHierarchyRingSprite.name = "FrostHitHierarchyRingSprite";
            return _hitHierarchyRingSprite;
        }

        private GameObject GetVfxInstance(CombatVfxType type, GameObject prefab)
        {
            Queue<GameObject> pool = GetPool(type, prefab);
            GameObject instance = pool.Count > 0 ? pool.Dequeue() : CreateVfxInstance(type, prefab);
            return instance;
        }

        private Queue<GameObject> GetPool(CombatVfxType type, GameObject prefab)
        {
            if (_vfxPools.TryGetValue(type, out Queue<GameObject> pool))
                return pool;

            pool = new Queue<GameObject>(Mathf.Max(1, VfxPoolSizePerType));
            _vfxPools[type] = pool;
            for (int i = 0; i < VfxPoolSizePerType; i++)
                pool.Enqueue(CreateVfxInstance(type, prefab));

            return pool;
        }

        private GameObject CreateVfxInstance(CombatVfxType type, GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = prefab.name + "_Pooled";
            ConfigureParticleRenderers(instance);
            instance.SetActive(false);
            return instance;
        }

        private void ConfigureParticleRenderers(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = VfxSortingLayer;
                renderers[i].sortingOrder = VfxSortingOrder;
            }
        }

        private void ReleaseExpiredVfx(float dt, bool canFollowTargets)
        {
            for (int i = _activeVfx.Count - 1; i >= 0; i--)
            {
                ActiveVfx active = _activeVfx[i];
                if (canFollowTargets)
                    UpdateFollowTargetPosition(ref active);

                active.Remaining -= dt;
                if (active.Remaining > 0f && AnyParticleAlive(active.Instance))
                {
                    _activeVfx[i] = active;
                    continue;
                }

                ReleaseVfx(active);
                _activeVfx.RemoveAt(i);
            }
        }

        private void UpdateFollowTargetPosition(ref ActiveVfx active)
        {
            if (!active.FollowTargetPosition || active.FollowTarget == Entity.Null || active.Instance == null)
                return;

            if (!_entityManager.Exists(active.FollowTarget) ||
                !_entityManager.HasComponent<LocalTransform>(active.FollowTarget))
                return;

            LocalTransform targetTransform = _entityManager.GetComponentData<LocalTransform>(active.FollowTarget);
            Vector3 targetPosition = new Vector3(
                targetTransform.Position.x,
                targetTransform.Position.y,
                targetTransform.Position.z);
            Vector3 position = targetPosition + active.FollowOffset;
            position.z = MobileCastleRenderDepth.ProjectileZ;
            active.Instance.transform.position = position;
        }

        private void ReleaseAllActiveVfx()
        {
            for (int i = _activeVfx.Count - 1; i >= 0; i--)
                ReleaseVfx(_activeVfx[i]);

            _activeVfx.Clear();
        }

        private void ReleaseVfx(ActiveVfx active)
        {
            if (active.Instance == null)
                return;

            ParticleSystem[] systems = active.Instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            active.Instance.SetActive(false);
            if (_vfxPools.TryGetValue(active.Type, out Queue<GameObject> pool))
                pool.Enqueue(active.Instance);
        }

        private static bool AnyParticleAlive(GameObject instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].IsAlive(true))
                    return true;
            }

            return false;
        }

        private static float CalculateVfxDuration(ParticleSystem[] systems)
        {
            float duration = 0.25f;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                float candidate = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax + 0.1f;
                duration = Mathf.Max(duration, candidate);
            }

            return Mathf.Min(duration, 3f);
        }

        private void EnsureAudioPool()
        {
            int targetSize = Mathf.Max(1, AudioPoolSize);
            while (_audioSources.Count < targetSize)
            {
                var sourceObject = new GameObject("CombatFeedbackAudio_" + _audioSources.Count);
                sourceObject.transform.SetParent(transform, false);
                var source = sourceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = SpatialBlend;
                _audioSources.Add(source);
            }
        }

        private bool TryPlaySfx(CombatSfxEvent sfxEvent)
        {
            AudioClip clip = GetSfxClip(sfxEvent.Type);
            if (clip == null)
                return false;

            int index = (int)sfxEvent.Type;
            float now = Time.time;
            if (now - _lastSfxTimes[index] < GetSfxMinInterval(sfxEvent.Type))
                return false;

            _lastSfxTimes[index] = now;

            // Kale hasar hissi (M-D): SFX rate-limit'inden gecen her CastleHit sarsintiyi/flash'i tetikler
            if (sfxEvent.Type == CombatSfxType.CastleHit)
            {
                if (CastleHitShakeTrauma > 0f)
                    CameraShaker.Instance?.AddTrauma(CastleHitShakeTrauma);
                if (CastleHitFlashEnabled)
                    DamageFlashUI.Instance?.Flash();
            }

            EnsureAudioPool();

            AudioSource source = GetNextAudioSource();
            source.transform.position = new Vector3(sfxEvent.Position.x, sfxEvent.Position.y, MobileCastleRenderDepth.ProjectileZ);
            source.clip = clip;
            source.volume = Mathf.Clamp01(sfxEvent.Volume) * SoundSettings.SfxVolume;
            source.pitch = Mathf.Max(0.05f, sfxEvent.Pitch * UnityEngine.Random.Range(PitchRandomMin, PitchRandomMax));
            source.spatialBlend = SpatialBlend;
            source.Play();
            return true;
        }

        public static float ResolveArcherSalvoVolume(
            int arrowCount,
            float eventVolume,
            float maxVolume)
        {
            if (arrowCount <= 0)
                return 0f;

            float densityGain = 1f + Mathf.Log(Mathf.Max(1, arrowCount), 2f) * 0.055f;
            return Mathf.Min(
                Mathf.Clamp01(maxVolume),
                Mathf.Clamp01(eventVolume) * densityGain);
        }

        public static float ResolveArcherSalvoPitchMultiplier(int arrowCount, float pitchDepth)
        {
            if (arrowCount <= 1)
                return 1f;

            float density = Mathf.Clamp01(Mathf.Log(arrowCount, 2f) / 10f);
            return 1f - density * Mathf.Clamp(pitchDepth, 0f, 0.2f);
        }

        private AudioSource GetNextAudioSource()
        {
            for (int i = 0; i < _audioSources.Count; i++)
            {
                _audioCursor = (_audioCursor + 1) % _audioSources.Count;
                if (!_audioSources[_audioCursor].isPlaying)
                    return _audioSources[_audioCursor];
            }

            _audioCursor = (_audioCursor + 1) % _audioSources.Count;
            return _audioSources[_audioCursor];
        }

        private GameObject GetVfxPrefab(CombatVfxType type)
        {
            return type switch
            {
                CombatVfxType.ArrowMuzzle => ArrowMuzzlePrefab,
                CombatVfxType.ArrowHit => ArrowHitPrefab,
                CombatVfxType.FrostHit => FrostHitPrefab,
                CombatVfxType.CastleHit => CastleHitPrefab,
                _ => null
            };
        }

        private AudioClip GetSfxClip(CombatSfxType type)
        {
            if (type == CombatSfxType.ArrowShoot)
                return GetRandomArrowShootClip();

            // Owner karari: Skeleton/zombie olumleri bireysel ses uretmez. Enum degeri
            // serialized event uyumlulugu icin korunur fakat runtime'da daima sessizdir.
            if (type == CombatSfxType.ZombieDeath)
                return null;

            DeadWallsAudioProfileSO profile = ResolveAudioProfile();
            bool useProfile = profile != null && profile.OverrideCombat;

            return type switch
            {
                CombatSfxType.ArrowHit => useProfile && profile.ArrowHitClip != null
                    ? profile.ArrowHitClip
                    : ArrowHitClip,
                CombatSfxType.FrostHit => useProfile && profile.FrostHitClip != null
                    ? profile.FrostHitClip
                    : FrostHitClip,
                CombatSfxType.CastleHit => useProfile
                    ? GetRandomClip(profile.WallHitClips) ?? CastleHitClip
                    : CastleHitClip,
                CombatSfxType.FireballBlast => useProfile && profile.FireballBlastClip != null
                    ? profile.FireballBlastClip
                    : FireballBlastClip,
                _ => null
            };
        }

        private static AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int startIndex = UnityEngine.Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(startIndex + i) % clips.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private AudioClip GetRandomArrowShootClip()
        {
            DeadWallsAudioProfileSO profile = ResolveAudioProfile();
            if (profile != null && profile.OverrideCombat)
            {
                AudioClip profiled = GetRandomClip(profile.ArrowShootClips);
                if (profiled != null)
                    return profiled;
            }

            if (ArrowShootClips != null && ArrowShootClips.Length > 0)
            {
                int startIndex = UnityEngine.Random.Range(0, ArrowShootClips.Length);
                for (int i = 0; i < ArrowShootClips.Length; i++)
                {
                    AudioClip clip = ArrowShootClips[(startIndex + i) % ArrowShootClips.Length];
                    if (clip != null)
                        return clip;
                }
            }

            return ArrowShootClip;
        }

        private DeadWallsAudioProfileSO ResolveAudioProfile()
        {
            return AudioProfile != null ? AudioProfile : DeadWallsAudioProfileSO.LoadDefault();
        }

        private static bool IsHitFlipbookType(CombatVfxType type)
        {
            return type == CombatVfxType.ArrowHit || type == CombatVfxType.FrostHit;
        }

        private float ResolveVfxScale(CombatVfxEvent vfxEvent)
        {
            if (vfxEvent.Scale > 0f)
                return vfxEvent.Scale;

            return vfxEvent.Type switch
            {
                CombatVfxType.ArrowMuzzle => ArrowMuzzleScale,
                CombatVfxType.ArrowHit => ArrowHitScale,
                CombatVfxType.FrostHit => FrostHitScale,
                CombatVfxType.CastleHit => CastleHitScale,
                _ => 1f
            };
        }

        private float GetSfxMinInterval(CombatSfxType type)
        {
            return type switch
            {
                CombatSfxType.ArrowShoot => IsNightPhase()
                    ? Mathf.Max(ShootSfxMinInterval, NightShootSfxMinInterval)
                    : ShootSfxMinInterval,
                CombatSfxType.ArrowHit => HitSfxMinInterval,
                CombatSfxType.FrostHit => HitSfxMinInterval,
                CombatSfxType.CastleHit => CastleHitSfxMinInterval,
                CombatSfxType.FireballBlast => FireballBlastSfxMinInterval,
                _ => 0.05f
            };
        }

        private static bool IsNightPhase()
        {
            GameManager gameManager = GameManager.Instance;
            return gameManager != null
                && gameManager.ContinuousSiegeCycle.Enabled
                && gameManager.ContinuousSiegeCycle.Phase == SiegeCyclePhase.Night;
        }

        private Quaternion ResolveHitFlipbookRotation(CombatVfxEvent vfxEvent)
        {
            Unity.Mathematics.float3 direction = vfxEvent.Direction;
            Vector2 dir = new Vector2(direction.x, direction.y);
            if (dir.sqrMagnitude < 0.0001f)
                return Quaternion.Euler(0f, 0f, HitFlipbookRotationOffsetDegrees);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle + HitFlipbookRotationOffsetDegrees);
        }

        private Quaternion ResolveRotation(CombatVfxEvent vfxEvent)
        {
            Unity.Mathematics.float3 direction = vfxEvent.Direction;
            Vector2 dir = new Vector2(direction.x, direction.y);
            if (dir.sqrMagnitude < 0.0001f)
                return Quaternion.Euler(0f, 0f, ResolveRotationOffset(vfxEvent.Type));

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle + ResolveRotationOffset(vfxEvent.Type));
        }

        private float ResolveRotationOffset(CombatVfxType type)
        {
            return type switch
            {
                CombatVfxType.ArrowMuzzle => ArrowMuzzleRotationOffsetDegrees,
                CombatVfxType.ArrowHit => ArrowHitRotationOffsetDegrees,
                CombatVfxType.FrostHit => FrostHitRotationOffsetDegrees,
                CombatVfxType.CastleHit => CastleHitRotationOffsetDegrees,
                _ => 0f
            };
        }
    }
}
