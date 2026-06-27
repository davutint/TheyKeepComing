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
        [SerializeField] private string spawnTilemapName = DefaultSpawnTilemapName;
        [SerializeField] private float maxStackOffset = 0.14f;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private int previewArcherCount = 96;
        [SerializeField] private float gizmoPointRadius = 0.065f;
        [SerializeField] private Color slotColor = new Color(1f, 0.88f, 0.25f, 0.9f);
        [SerializeField] private Color repeatColor = new Color(0.25f, 0.75f, 1f, 0.75f);

        private readonly List<Vector3Int> _spawnCells = new List<Vector3Int>();
        private bool _cacheDirty = true;
        private bool _missingTilemapWarningLogged;
        private bool _emptyTilemapWarningLogged;

        public int SpawnCellCount
        {
            get
            {
                EnsureCache();
                return _spawnCells.Count;
            }
        }

        public void Configure(Tilemap tilemap)
        {
            if (spawnTilemap == tilemap)
                return;

            spawnTilemap = tilemap;
            _cacheDirty = true;
            _missingTilemapWarningLogged = false;
            _emptyTilemapWarningLogged = false;
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
            position = default;
            EnsureCache();

            if (_spawnCells.Count == 0)
            {
                LogEmptyTilemapWarning();
                return false;
            }

            int safeIndex = math.max(0, archerIndex);
            int baseIndex = safeIndex % _spawnCells.Count;
            int stackIndex = safeIndex / _spawnCells.Count;
            Vector3 center = spawnTilemap.GetCellCenterWorld(_spawnCells[baseIndex]);
            Vector2 offset = CalculateStackOffset(stackIndex);
            position = new float3(center.x + offset.x, center.y + offset.y, SpawnZ);
            return true;
        }

        public void RebuildCache()
        {
            _cacheDirty = false;
            _spawnCells.Clear();

            if (spawnTilemap == null)
                spawnTilemap = FindTilemapByName(spawnTilemapName);

            if (spawnTilemap == null)
            {
                LogMissingTilemapWarning();
                return;
            }

            BoundsInt bounds = spawnTilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (spawnTilemap.HasTile(cell))
                    _spawnCells.Add(cell);
            }

            Vector3 center = bounds.center;
            _spawnCells.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y + 0.5f - center.y, a.x + 0.5f - center.x);
                float angleB = Mathf.Atan2(b.y + 0.5f - center.y, b.x + 0.5f - center.x);
                int angleCompare = angleA.CompareTo(angleB);
                if (angleCompare != 0)
                    return angleCompare;

                float distA = (a.x + 0.5f - center.x) * (a.x + 0.5f - center.x)
                    + (a.y + 0.5f - center.y) * (a.y + 0.5f - center.y);
                float distB = (b.x + 0.5f - center.x) * (b.x + 0.5f - center.x)
                    + (b.y + 0.5f - center.y) * (b.y + 0.5f - center.y);
                int distanceCompare = distB.CompareTo(distA);
                if (distanceCompare != 0)
                    return distanceCompare;

                int yCompare = a.y.CompareTo(b.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            if (_spawnCells.Count > 0)
                _emptyTilemapWarningLogged = false;
        }

        private void Awake()
        {
            RegisterInstance();
        }

        private void OnEnable()
        {
            RegisterInstance();
            _cacheDirty = true;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnValidate()
        {
            maxStackOffset = Mathf.Max(0f, maxStackOffset);
            previewArcherCount = Mathf.Max(1, previewArcherCount);
            gizmoPointRadius = Mathf.Max(0.01f, gizmoPointRadius);
            _cacheDirty = true;
        }

        private void RegisterInstance()
        {
            if (Instance == null || Instance == this)
                Instance = this;
        }

        private void EnsureCache()
        {
            if (_cacheDirty)
                RebuildCache();
        }

        private Vector2 CalculateStackOffset(int stackIndex)
        {
            if (stackIndex <= 0 || maxStackOffset <= 0f)
                return Vector2.zero;

            const float GoldenAngle = 2.3999632f;
            const float GoldenRatioConjugate = 0.61803399f;
            float angle = stackIndex * GoldenAngle;
            float radius01 = Mathf.Repeat(stackIndex * GoldenRatioConjugate, 1f);
            float radius = maxStackOffset * Mathf.Sqrt(0.25f + radius01 * 0.75f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static Tilemap FindTilemapByName(string tilemapName)
        {
            var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            Debug.LogWarning("[MobileCastleArcherTilePlacement] 'outside' tilemap bulunamadi; mobile okcu spawn iptal edilecek.");
        }

        private void LogEmptyTilemapWarning()
        {
            if (_emptyTilemapWarningLogged)
                return;

            _emptyTilemapWarningLogged = true;
            Debug.LogWarning("[MobileCastleArcherTilePlacement] 'outside' tilemap bos; mobile okcu spawn iptal edilecek.");
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;

            EnsureCache();
            if (spawnTilemap == null || _spawnCells.Count == 0)
                return;

            Gizmos.color = slotColor;
            foreach (Vector3Int cell in _spawnCells)
            {
                Vector3 center = spawnTilemap.GetCellCenterWorld(cell);
                center.z = SpawnZ;
                Gizmos.DrawWireSphere(center, gizmoPointRadius);
            }

            Gizmos.color = repeatColor;
            int previewCount = Mathf.Max(_spawnCells.Count, previewArcherCount);
            for (int i = _spawnCells.Count; i < previewCount; i++)
            {
                if (!TryGetSpawnPosition(i, out float3 position))
                    return;

                Gizmos.DrawWireSphere(new Vector3(position.x, position.y, position.z), gizmoPointRadius * 0.75f);
            }
        }
    }
}
