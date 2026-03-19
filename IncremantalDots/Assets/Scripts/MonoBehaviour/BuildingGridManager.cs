using Unity.Entities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DeadWalls
{
    /// <summary>
    /// Grid truth source + yerlestirme mantigi + ECS entity olusturma.
    /// Hibrit yaklasim: MonoBehaviour int[,] (truth source) + Tilemap (gorsel) + ECS entity (data).
    /// </summary>
    public class BuildingGridManager : MonoBehaviour
    {
        public static BuildingGridManager Instance { get; private set; }

        [Header("Grid Ayarlari")]
        public Tilemap BuildableZoneTilemap;   // buildable_zone layer referansi
        public Tilemap BuildingVisualTilemap;   // bina base layer (Order in Layer: 5)
        public Tilemap BuildingTopTilemap;     // bina cati + detay layer (Order in Layer: 6)

        // cellBounds'tan otomatik hesaplanir — elle girmeye gerek yok
        [Header("Otomatik Hesaplanan (Debug)")]
        [SerializeField] private int GridWidth;
        [SerializeField] private int GridHeight;
        [SerializeField] private Vector3Int GridOrigin;

        // Debug erisim
        public int GridWidthDebug => GridWidth;
        public int GridHeightDebug => GridHeight;
        public Vector3Int GridOriginDebug => GridOrigin;

        [Header("Dogal Kaynak Zone'lari")]
        public Tilemap ResourceZoneTilemap;   // resource_zones layer referansi
        public TileBase ForestTile;           // Orman tile asset
        public TileBase StoneTile;            // Tas tile asset
        public TileBase IronTile;             // Demir tile asset

        [Header("Bina Konfigurasyonlari")]
        public BuildingConfigSO[] BuildingConfigs;

        // Grid durumu — truth source
        // 0 = bos (yerlestirilebilir), 1 = dolu, -1 = yerlestirilemez
        private int[,] _grid;
        private Entity[,] _entityGrid;  // Hangi hucrede hangi bina entity'si var
        private ResourcePointType[,] _zoneGrid;

        // Izometrik grid donusumu icin Unity Grid component cache
        private Grid _isoGrid;
        public Grid IsoGrid => _isoGrid;

        // ECS erisim
        private EntityManager _entityManager;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeGrid();
        }

        /// <summary>
        /// Grid'i buildable_zone tilemap'ten initialize et.
        /// BuildableZoneTilemap'teki tile'lar → 0 (bos, yerlestirilebilir).
        /// Tile olmayan yerler → -1 (yerlestirilemez).
        /// </summary>
        public void InitializeGrid()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                _entityManager = world.EntityManager;

            if (BuildableZoneTilemap == null)
            {
                Debug.LogError("BuildingGridManager: BuildableZoneTilemap atanmamis!");
                return;
            }

            // Unity Grid component'ini cache'le — izometrik donusum icin
            _isoGrid = BuildableZoneTilemap.layoutGrid;

            // cellBounds'tan GridOrigin, GridWidth, GridHeight otomatik hesapla
            BoundsInt bounds = BuildableZoneTilemap.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                Debug.LogError("BuildingGridManager: buildable_zone tilemap bos! Tile ile boyayin.");
                return;
            }

            GridOrigin = new Vector3Int(bounds.xMin, bounds.yMin, 0);
            GridWidth = bounds.size.x;
            GridHeight = bounds.size.y;

            _grid = new int[GridWidth, GridHeight];
            _entityGrid = new Entity[GridWidth, GridHeight];

            // Varsayilan: hepsi yerlestirilemez
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++)
                    _grid[x, y] = -1;

            // Buildable zone tilemap'teki tile'lari oku
            int buildableCount = 0;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var tilePos = new Vector3Int(x, y, 0);
                    if (BuildableZoneTilemap.HasTile(tilePos))
                    {
                        int gx = x - GridOrigin.x;
                        int gy = y - GridOrigin.y;
                        _grid[gx, gy] = 0;
                        buildableCount++;
                    }
                }
            }
            Debug.Log($"[BuildingGrid] Bounds: {bounds} | Origin: {GridOrigin} | Size: {GridWidth}x{GridHeight} | Buildable: {buildableCount}/{GridWidth * GridHeight}");

            // Dogal kaynak zone'larini oku ve cache'le
            _zoneGrid = new ResourcePointType[GridWidth, GridHeight];
            if (ResourceZoneTilemap != null)
            {
                int zoneCount = 0;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        var tilePos = new Vector3Int(x, y, 0);
                        var tile = ResourceZoneTilemap.GetTile(tilePos);
                        if (tile == null) continue;

                        int gx = x - GridOrigin.x;
                        int gy = y - GridOrigin.y;
                        if (gx < 0 || gy < 0 || gx >= GridWidth || gy >= GridHeight) continue;

                        if (tile == ForestTile)      _zoneGrid[gx, gy] = ResourcePointType.Forest;
                        else if (tile == StoneTile)   _zoneGrid[gx, gy] = ResourcePointType.Stone;
                        else if (tile == IronTile)    _zoneGrid[gx, gy] = ResourcePointType.Iron;

                        if (_zoneGrid[gx, gy] != ResourcePointType.None) zoneCount++;
                    }
                }
                Debug.Log($"[BuildingGrid] Resource zones: {zoneCount} cells cached");
            }

            _initialized = true;
        }

        /// <summary>
        /// Belirtilen grid pozisyonuna bina yerlestirilebilir mi kontrol et.
        /// 3xN alan bos mu + kaynak yeterli mi?
        /// </summary>
        public bool CanPlace(BuildingConfigSO config, int gridX, int gridY)
        {
            if (!_initialized || config == null) return false;

            // Grid sinir kontrolu
            if (gridX < 0 || gridY < 0 ||
                gridX + config.GridWidth > GridWidth ||
                gridY + config.GridHeight > GridHeight)
                return false;

            // Tum hucrelerin bos olmasi gerekiyor
            for (int x = gridX; x < gridX + config.GridWidth; x++)
                for (int y = gridY; y < gridY + config.GridHeight; y++)
                    if (_grid[x, y] != 0)
                        return false;

            // Kaynak yeterliligi kontrolu
            if (GameManager.Instance == null) return false;
            var res = GameManager.Instance.Resources;
            if (res.Wood < config.WoodCost || res.Stone < config.StoneCost ||
                res.Iron < config.IronCost || res.Food < config.FoodCost)
                return false;

            // Dogal kaynak zone yakinlik kontrolu
            if (!IsNearZone(config.RequiredZone, gridX, gridY, config.GridWidth, config.GridHeight, config.ZoneProximityRadius))
                return false;

            return true;
        }

        /// <summary>
        /// Bina footprint'i etrafinda belirtilen radius icerisinde zone tile var mi kontrol et.
        /// zoneType == None ise hemen true doner.
        /// </summary>
        public bool IsNearZone(ResourcePointType zoneType, int gridX, int gridY, int width, int height, int radius)
        {
            if (zoneType == ResourcePointType.None) return true;
            if (_zoneGrid == null) return false;

            int minX = Mathf.Max(0, gridX - radius);
            int minY = Mathf.Max(0, gridY - radius);
            int maxX = Mathf.Min(GridWidth - 1, gridX + width - 1 + radius);
            int maxY = Mathf.Min(GridHeight - 1, gridY + height - 1 + radius);

            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    if (_zoneGrid[x, y] == zoneType)
                        return true;

            return false;
        }

        /// <summary>
        /// Bina yerlestir — grid guncelle + kaynak dus + ECS entity olustur + gorsel tile koy.
        /// </summary>
        public bool PlaceBuilding(BuildingConfigSO config, int gridX, int gridY)
        {
            if (!CanPlace(config, gridX, gridY)) return false;

            // 1. Kaynak dus
            DeductResources(config);

            // 2. Grid hucreleri dolu olarak isaretle
            for (int x = gridX; x < gridX + config.GridWidth; x++)
                for (int y = gridY; y < gridY + config.GridHeight; y++)
                    _grid[x, y] = 1;

            // 3. ECS entity olustur
            var entity = CreateBuildingEntity(config, gridX, gridY);

            // 4. Entity grid'e yaz (tum hucrelere — herhangi hucreye tiklaninca entity bulunabilsin)
            for (int x = gridX; x < gridX + config.GridWidth; x++)
                for (int y = gridY; y < gridY + config.GridHeight; y++)
                    _entityGrid[x, y] = entity;

            // 5. Tilemap'e gorsel tile koy
            PlaceVisualTile(config, gridX, gridY);

            return true;
        }

        /// <summary>
        /// Bina kaldir — grid serbest birak + gorsel sil + %50 kaynak iade + ECS entity sil.
        /// </summary>
        public void RemoveBuilding(int gridX, int gridY)
        {
            Entity entity = GetBuildingEntity(gridX, gridY);
            if (entity == Entity.Null) return;

            var buildingData = _entityManager.GetComponentData<BuildingData>(entity);
            var config = GetConfigByType(buildingData.Type);
            if (config == null) return;

            int ox = buildingData.GridX, oy = buildingData.GridY;

            // Grid hucreleri serbest birak + entity referanslari temizle
            for (int x = ox; x < ox + config.GridWidth; x++)
                for (int y = oy; y < oy + config.GridHeight; y++)
                {
                    _grid[x, y] = 0;
                    _entityGrid[x, y] = Entity.Null;
                }

            // Gorsel tile kaldir
            RemoveVisualTile(config, ox, oy);

            // %50 kaynak iade
            RefundResources(config);

            // ECS entity sil
            _entityManager.DestroyEntity(entity);
        }

        /// <summary>
        /// BuildingType'a gore BuildingConfigs array'inden config bul.
        /// </summary>
        public BuildingConfigSO GetConfigByType(BuildingType type)
        {
            if (BuildingConfigs == null) return null;
            foreach (var c in BuildingConfigs)
                if (c != null && c.Type == type)
                    return c;
            return null;
        }

        private void RemoveVisualTile(BuildingConfigSO config, int gridX, int gridY)
        {
            if (BuildingVisualTilemap == null) return;

            bool hasLayout = config.TileLayoutBase != null &&
                             config.TileLayoutBase.Length == config.GridWidth * config.GridHeight;

            if (hasLayout)
            {
                // Tum hucrelerdeki tile'lari temizle (her iki layer)
                for (int lx = 0; lx < config.GridWidth; lx++)
                {
                    for (int ly = 0; ly < config.GridHeight; ly++)
                    {
                        int cellX = gridX + lx + GridOrigin.x;
                        int cellY = gridY + ly + GridOrigin.y;
                        var pos = new Vector3Int(cellX, cellY, 0);

                        BuildingVisualTilemap.SetTile(pos, null);
                        if (BuildingTopTilemap != null)
                            BuildingTopTilemap.SetTile(pos, null);
                    }
                }
            }
            else
            {
                // Fallback — eski tek-tile silme
                int centerX = gridX + config.GridWidth / 2 + GridOrigin.x;
                int centerY = gridY + config.GridHeight / 2 + GridOrigin.y;
                BuildingVisualTilemap.SetTile(new Vector3Int(centerX, centerY, 0), null);
            }
        }

        private void RefundResources(BuildingConfigSO config)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(ResourceData));
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var res = em.GetComponentData<ResourceData>(entity);
            res.Wood += config.WoodCost / 2;
            res.Stone += config.StoneCost / 2;
            res.Iron += config.IronCost / 2;
            res.Food += config.FoodCost / 2;
            em.SetComponentData(entity, res);
        }

        /// <summary>
        /// Grid'i tamamen sifirla. Restart icin.
        /// </summary>
        public void ResetGrid()
        {
            if (_grid == null) return;

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    // Dolu olan hucreleri tekrar bos yap (yerlestirilebilir olanlari koru)
                    if (_grid[x, y] == 1)
                        _grid[x, y] = 0;
                    _entityGrid[x, y] = Entity.Null;
                }
            }

            // Gorsel tilemap'leri temizle
            if (BuildingVisualTilemap != null)
                BuildingVisualTilemap.ClearAllTiles();
            if (BuildingTopTilemap != null)
                BuildingTopTilemap.ClearAllTiles();
        }

        /// <summary>
        /// World pozisyonunu grid koordinatina cevir.
        /// Izometrik: Unity Grid.WorldToCell() diamond hucre hesabini otomatik yapar.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3Int cell = _isoGrid.WorldToCell(worldPos);
            int gx = cell.x - GridOrigin.x;
            int gy = cell.y - GridOrigin.y;
            return new Vector2Int(gx, gy);
        }

        /// <summary>
        /// Grid koordinatini world pozisyonuna cevir (bina merkezine).
        /// Izometrik: GetCellCenterWorld() diamond hucrenin tam merkezini verir.
        /// </summary>
        public Vector3 GridToWorld(int gridX, int gridY, BuildingConfigSO config)
        {
            int centerCellX = gridX + GridOrigin.x + config.GridWidth / 2;
            int centerCellY = gridY + GridOrigin.y + config.GridHeight / 2;
            return _isoGrid.GetCellCenterWorld(new Vector3Int(centerCellX, centerCellY, 0));
        }

        /// <summary>
        /// Tek hucre grid koordinatini world pozisyonuna cevir (config'siz).
        /// Gizmo cizimi ve diger basit donusumler icin.
        /// </summary>
        public Vector3 CellToWorld(int gridX, int gridY)
        {
            return _isoGrid.GetCellCenterWorld(
                new Vector3Int(gridX + GridOrigin.x, gridY + GridOrigin.y, 0));
        }

        /// <summary>
        /// Grid hucresinde bina var mi?
        /// </summary>
        public bool HasBuilding(int gridX, int gridY)
        {
            if (gridX < 0 || gridY < 0 || gridX >= GridWidth || gridY >= GridHeight)
                return false;
            return _grid[gridX, gridY] == 1;
        }

        /// <summary>
        /// Grid hucresindeki bina entity'sini dondur.
        /// </summary>
        public Entity GetBuildingEntity(int gridX, int gridY)
        {
            if (gridX < 0 || gridY < 0 || gridX >= GridWidth || gridY >= GridHeight)
                return Entity.Null;
            return _entityGrid[gridX, gridY];
        }

        private void DeductResources(BuildingConfigSO config)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(ResourceData));
            if (query.IsEmpty) return;

            var entity = query.GetSingletonEntity();
            var res = em.GetComponentData<ResourceData>(entity);
            res.Wood -= config.WoodCost;
            res.Stone -= config.StoneCost;
            res.Iron -= config.IronCost;
            res.Food -= config.FoodCost;
            em.SetComponentData(entity, res);
        }

        private Entity CreateBuildingEntity(BuildingConfigSO config, int gridX, int gridY)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return Entity.Null;

            var em = world.EntityManager;
            var entity = em.CreateEntity();

            // Her binada BuildingData var
            em.AddComponentData(entity, new BuildingData
            {
                Type = config.Type,
                Level = 1,
                GridX = gridX,
                GridY = gridY
            });

            // Kaynak binasi ise (MaxWorkers > 0)
            if (config.MaxWorkers > 0)
            {
                em.AddComponentData(entity, new ResourceProducer
                {
                    ResourceType = config.ProducedResource,
                    RatePerWorkerPerMin = config.RatePerWorkerPerMin,
                    AssignedWorkers = 0,
                    MaxWorkers = config.MaxWorkers
                });
            }

            // Ev ise (PopulationCapacity > 0)
            if (config.PopulationCapacity > 0)
            {
                em.AddComponentData(entity, new PopulationProvider
                {
                    CapacityAmount = config.PopulationCapacity
                });
                em.AddComponentData(entity, new BuildingFoodCost
                {
                    FoodPerMin = config.FoodCostPerMin
                });
            }

            // Kisla ise (TrainingDuration > 0)
            if (config.TrainingDuration > 0f)
            {
                em.AddComponentData(entity, new ArcherTrainer
                {
                    TrainingTimer = 0f,
                    TrainingDuration = config.TrainingDuration,
                    FoodCostPerArcher = config.FoodCostPerArcher,
                    WoodCostPerArcher = config.WoodCostPerArcher,
                    IsTraining = false
                });
            }

            // Fletcher ise (ArrowsPerWorkerPerMin > 0)
            if (config.ArrowsPerWorkerPerMin > 0f)
            {
                em.AddComponentData(entity, new ArrowProducer
                {
                    ArrowsPerWorkerPerMin = config.ArrowsPerWorkerPerMin,
                    AssignedWorkers = 0,
                    MaxWorkers = config.MaxWorkers,
                    WoodCostPerBatchPerMin = config.WoodCostPerBatchPerMin
                });
            }

            return entity;
        }

        /// <summary>
        /// Belirtilen tipte en az bir bina var mi kontrol et.
        /// </summary>
        public bool HasBuildingOfType(BuildingType type)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(BuildingData));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            bool found = false;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<BuildingData>(entities[i]).Type == type)
                {
                    found = true;
                    break;
                }
            }

            entities.Dispose();
            return found;
        }

        /// <summary>
        /// Resource zone overlay'i goster (yerlestirme modunda).
        /// </summary>
        public void ShowResourceZones()
        {
            if (ResourceZoneTilemap == null) return;
            var renderer = ResourceZoneTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null) renderer.enabled = true;
        }

        /// <summary>
        /// Resource zone overlay'i gizle.
        /// </summary>
        public void HideResourceZones()
        {
            if (ResourceZoneTilemap == null) return;
            var renderer = ResourceZoneTilemap.GetComponent<TilemapRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private void PlaceVisualTile(BuildingConfigSO config, int gridX, int gridY)
        {
            if (BuildingVisualTilemap == null) return;

            // TileLayout varsa → 2-layer tile yerlesim
            bool hasLayout = config.TileLayoutBase != null &&
                             config.TileLayoutBase.Length == config.GridWidth * config.GridHeight;

            if (hasLayout)
            {
                bool hasTop = config.TileLayoutTop != null &&
                              config.TileLayoutTop.Length == config.GridWidth * config.GridHeight;

                for (int lx = 0; lx < config.GridWidth; lx++)
                {
                    for (int ly = 0; ly < config.GridHeight; ly++)
                    {
                        int idx = lx + ly * config.GridWidth;
                        int cellX = gridX + lx + GridOrigin.x;
                        int cellY = gridY + ly + GridOrigin.y;
                        var pos = new Vector3Int(cellX, cellY, 0);

                        // Base layer
                        var baseTile = config.TileLayoutBase[idx];
                        if (baseTile != null)
                            BuildingVisualTilemap.SetTile(pos, baseTile);

                        // Top layer
                        if (hasTop && BuildingTopTilemap != null)
                        {
                            var topTile = config.TileLayoutTop[idx];
                            if (topTile != null)
                                BuildingTopTilemap.SetTile(pos, topTile);
                        }
                    }
                }
            }
            else
            {
                // Fallback — eski placeholder GhostSprite kodu
                if (config.GhostSprite == null) return;

                int centerX = gridX + config.GridWidth / 2 + GridOrigin.x;
                int centerY = gridY + config.GridHeight / 2 + GridOrigin.y;

                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = config.GhostSprite;
                tile.color = Color.white;
                BuildingVisualTilemap.SetTile(new Vector3Int(centerX, centerY, 0), tile);
            }
        }
    }
}
