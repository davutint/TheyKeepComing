using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    [ExecuteAlways]
    public class MobileCastleArcherTilePlacement : MonoBehaviour
    {
        public const string DefaultSpawnTilemapName = "outside";
        public const float SpawnZ = MobileCastleRenderDepth.UnitZ;

        public static MobileCastleArcherTilePlacement Instance { get; private set; }

        [SerializeField] private Tilemap spawnTilemap;
        [SerializeField] private ArcherFormationDefinitionSO formationDefinition;
        [SerializeField] private string spawnTilemapName = DefaultSpawnTilemapName;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float gizmoPointRadius = 0.018f;
        [SerializeField] private Color firstLayerColor = new Color(1f, 0.88f, 0.25f, 0.95f);
        [SerializeField] private Color formationColor = new Color(0.25f, 0.75f, 1f, 0.75f);

        private readonly List<Vector3Int> _spawnCells = new List<Vector3Int>(
            ArcherFormationUtility.RequiredTileCount);
        private readonly List<float3> _formationPositions = new List<float3>(
            ArcherFormationUtility.TotalCapacity);
        private bool _cacheDirty = true;
        private int _cachedDefinitionFingerprint;
        private int _cachedFormationVersion = ArcherFormationUtility.CurrentVersion;
        private Vector2 _cachedRightVertex;
        private Vector2 _cachedTopVertex;
        private bool _missingTilemapWarningLogged;
        private bool _invalidFormationWarningLogged;

        public int SpawnCellCount
        {
            get
            {
                EnsureCache();
                return _spawnCells.Count;
            }
        }

        public int FormationCapacity
        {
            get
            {
                EnsureCache();
                return _formationPositions.Count;
            }
        }

        public int FormationVersion
        {
            get
            {
                EnsureCache();
                return _cachedFormationVersion;
            }
        }

        public ArcherFormationDefinitionSO FormationDefinition => formationDefinition;

        public void Configure(Tilemap tilemap)
        {
            Configure(tilemap, formationDefinition);
        }

        public void Configure(Tilemap tilemap, ArcherFormationDefinitionSO definition)
        {
            if (spawnTilemap == tilemap && formationDefinition == definition)
                return;

            spawnTilemap = tilemap;
            formationDefinition = definition;
            InvalidateCache();
        }

        public static MobileCastleArcherTilePlacement GetOrCreateRuntime()
        {
            if (Instance != null && Instance.isActiveAndEnabled)
                return Instance;

            var existing = UnityEngine.Object.FindFirstObjectByType<MobileCastleArcherTilePlacement>();
            if (existing != null)
                return existing;

            Tilemap outside = FindTilemapByName(DefaultSpawnTilemapName);
            if (outside == null)
                return null;

            Grid grid = outside.GetComponentInParent<Grid>();
            GameObject host = grid != null ? grid.gameObject : outside.gameObject;
            var placement = host.GetComponent<MobileCastleArcherTilePlacement>();
            if (placement == null)
                placement = host.AddComponent<MobileCastleArcherTilePlacement>();

            placement.Configure(outside);
            return placement;
        }

        public bool TryGetSpawnPosition(int archerIndex, out float3 position)
        {
            return TryGetSpawnPosition(archerIndex, FormationVersion, out position);
        }

        public bool TryGetSpawnPosition(int archerIndex, int requestedVersion, out float3 position)
        {
            position = default;
            EnsureCache();

            if (archerIndex < 0
                || archerIndex >= _formationPositions.Count
                || requestedVersion != _cachedFormationVersion)
            {
                return false;
            }

            position = _formationPositions[archerIndex];
            return true;
        }

        public bool TryGetSpawnCell(int tileIndex, out Vector3Int cell)
        {
            EnsureCache();
            if (tileIndex < 0 || tileIndex >= _spawnCells.Count)
            {
                cell = default;
                return false;
            }

            cell = _spawnCells[tileIndex];
            return true;
        }

        public bool TryGetDiamondAxes(out Vector2 rightVertex, out Vector2 topVertex)
        {
            EnsureCache();
            rightVertex = _cachedRightVertex;
            topVertex = _cachedTopVertex;
            return _formationPositions.Count == ArcherFormationUtility.TotalCapacity;
        }

        public void RebuildCache()
        {
            _cacheDirty = false;
            _cachedDefinitionFingerprint = CalculateDefinitionFingerprint();
            _spawnCells.Clear();
            _formationPositions.Clear();
            _cachedRightVertex = default;
            _cachedTopVertex = default;

            if (spawnTilemap == null)
                spawnTilemap = FindTilemapByName(spawnTilemapName);

            if (spawnTilemap == null)
            {
                LogMissingTilemapWarning();
                return;
            }

            int version;
            Vector3Int[] coordinates;
            float safeInset;
            float minimumDistance;
            int candidateAttempts;
            if (!TryReadDefinition(
                    out version,
                    out coordinates,
                    out safeInset,
                    out minimumDistance,
                    out candidateAttempts,
                    out string problem))
            {
                LogInvalidFormationWarning(problem);
                return;
            }

            for (int i = 0; i < coordinates.Length; i++)
            {
                Vector3Int cell = coordinates[i];
                if (!spawnTilemap.HasTile(cell))
                {
                    LogInvalidFormationWarning(
                        $"Versioned outside tile eksik: ({cell.x},{cell.y},{cell.z}).");
                    _spawnCells.Clear();
                    return;
                }

                _spawnCells.Add(cell);
            }

            Vector3Int referenceCell = _spawnCells[0];
            Vector3 center = spawnTilemap.GetCellCenterWorld(referenceCell);
            Vector3 xStep = spawnTilemap.GetCellCenterWorld(referenceCell + Vector3Int.right) - center;
            Vector3 yStep = spawnTilemap.GetCellCenterWorld(referenceCell + Vector3Int.up) - center;
            _cachedRightVertex = new Vector2(xStep.x - yStep.x, xStep.y - yStep.y) * 0.5f;
            _cachedTopVertex = new Vector2(xStep.x + yStep.x, xStep.y + yStep.y) * 0.5f;

            var offsetsByTile = new Vector2[_spawnCells.Count][];
            for (int tileIndex = 0; tileIndex < _spawnCells.Count; tileIndex++)
            {
                if (!ArcherFormationUtility.TryGenerateTileOffsets(
                        _spawnCells[tileIndex],
                        _cachedRightVertex,
                        _cachedTopVertex,
                        version,
                        ArcherFormationUtility.SlotsPerTile,
                        safeInset,
                        minimumDistance,
                        candidateAttempts,
                        out offsetsByTile[tileIndex]))
                {
                    LogInvalidFormationWarning(
                        $"Tile {tileIndex} icin 25 minimum-distance slot uretilemedi.");
                    _spawnCells.Clear();
                    _formationPositions.Clear();
                    return;
                }
            }

            for (int localSlotIndex = 0;
                 localSlotIndex < ArcherFormationUtility.SlotsPerTile;
                 localSlotIndex++)
            {
                for (int tileIndex = 0; tileIndex < _spawnCells.Count; tileIndex++)
                {
                    Vector3 tileCenter = spawnTilemap.GetCellCenterWorld(_spawnCells[tileIndex]);
                    Vector2 offset = offsetsByTile[tileIndex][localSlotIndex];
                    _formationPositions.Add(new float3(
                        tileCenter.x + offset.x,
                        tileCenter.y + offset.y,
                        SpawnZ));
                }
            }

            _cachedFormationVersion = version;
            _missingTilemapWarningLogged = false;
            _invalidFormationWarningLogged = false;
        }

        private bool TryReadDefinition(
            out int version,
            out Vector3Int[] coordinates,
            out float safeInset,
            out float minimumDistance,
            out int candidateAttempts,
            out string problem)
        {
            if (formationDefinition != null)
            {
                version = formationDefinition.Version;
                coordinates = formationDefinition.TileCoordinates;
                safeInset = formationDefinition.SafeInset;
                minimumDistance = formationDefinition.MinimumLocalDistance;
                candidateAttempts = formationDefinition.CandidateAttempts;
                return formationDefinition.ValidateV1(out problem);
            }

            version = ArcherFormationUtility.CurrentVersion;
            coordinates = ArcherFormationUtility.CreateCanonicalV1TileCoordinates();
            safeInset = ArcherFormationUtility.DefaultSafeInset;
            minimumDistance = ArcherFormationUtility.DefaultMinimumLocalDistance;
            candidateAttempts = ArcherFormationUtility.DefaultCandidateAttempts;
            problem = string.Empty;
            return true;
        }

        private void Awake()
        {
            RegisterInstance();
        }

        private void OnEnable()
        {
            RegisterInstance();
            InvalidateCache();
            Tilemap.tilemapTileChanged += OnTilemapTileChanged;
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
            if (Instance == this)
                Instance = null;
        }

        private void OnTilemapTileChanged(Tilemap tilemap, Tilemap.SyncTile[] tiles)
        {
            if (tilemap == spawnTilemap
                || (spawnTilemap == null && tilemap != null && tilemap.name == spawnTilemapName))
            {
                InvalidateCache();
            }
        }

        private void OnValidate()
        {
            gizmoPointRadius = Mathf.Max(0.005f, gizmoPointRadius);
            InvalidateCache();
        }

        private void RegisterInstance()
        {
            if (Instance == null || Instance == this)
                Instance = this;
        }

        private void EnsureCache()
        {
            if (CalculateDefinitionFingerprint() != _cachedDefinitionFingerprint)
                _cacheDirty = true;

            if (_cacheDirty)
                RebuildCache();
        }

        private void InvalidateCache()
        {
            _cacheDirty = true;
            _missingTilemapWarningLogged = false;
            _invalidFormationWarningLogged = false;
        }

        private int CalculateDefinitionFingerprint()
        {
            unchecked
            {
                if (formationDefinition == null)
                    return ArcherFormationUtility.CurrentVersion * 397
                        + ArcherFormationUtility.RequiredTileCount;

                int hash = formationDefinition.Version;
                hash = hash * 31 + formationDefinition.SafeInset.GetHashCode();
                hash = hash * 31 + formationDefinition.MinimumLocalDistance.GetHashCode();
                hash = hash * 31 + formationDefinition.CandidateAttempts;
                Vector3Int[] coordinates = formationDefinition.TileCoordinates;
                hash = hash * 31 + (coordinates?.Length ?? 0);
                if (coordinates != null)
                {
                    for (int i = 0; i < coordinates.Length; i++)
                        hash = hash * 31 + coordinates[i].GetHashCode();
                }

                return hash;
            }
        }

        private static Tilemap FindTilemapByName(string tilemapName)
        {
            var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap != null && tilemap.name == tilemapName)
                    return tilemap;
            }

            return null;
        }

        private void LogMissingTilemapWarning()
        {
            if (_missingTilemapWarningLogged)
                return;

            _missingTilemapWarningLogged = true;
            Debug.LogWarning(
                "[MobileCastleArcherTilePlacement] 'outside' tilemap bulunamadi; mobile okcu spawn iptal edilecek.");
        }

        private void LogInvalidFormationWarning(string problem)
        {
            if (_invalidFormationWarningLogged)
                return;

            _invalidFormationWarningLogged = true;
            Debug.LogWarning(
                "[MobileCastleArcherTilePlacement] 40x25 formation contract gecersiz: " + problem);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            EnsureCache();
            for (int i = 0; i < _formationPositions.Count; i++)
            {
                Gizmos.color = i < ArcherFormationUtility.RequiredTileCount
                    ? firstLayerColor
                    : formationColor;
                float3 position = _formationPositions[i];
                Gizmos.DrawWireSphere(
                    new Vector3(position.x, position.y, position.z),
                    gizmoPointRadius);
            }
        }
    }
}
