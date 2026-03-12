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
        public Tilemap BuildingVisualTilemap;   // bina sprite gosterimi (yeni layer)
        public int GridWidth = 32;
        public int GridHeight = 32;
        public Vector3Int GridOrigin;           // Grid'in world-space baslangic noktasi

        [Header("Bina Konfigurasyonlari")]
        public BuildingConfigSO[] BuildingConfigs;

        // Grid durumu — truth source
        // 0 = bos (yerlestirilebilir), 1 = dolu, -1 = yerlestirilemez
        private int[,] _grid;
        private Entity[,] _entityGrid;  // Hangi hucrede hangi bina entity'si var

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

            _grid = new int[GridWidth, GridHeight];
            _entityGrid = new Entity[GridWidth, GridHeight];

            // Varsayilan: hepsi yerlestirilemez
            for (int x = 0; x < GridWidth; x++)
                for (int y = 0; y < GridHeight; y++)
                    _grid[x, y] = -1;

            if (BuildableZoneTilemap == null) return;

            // Buildable zone tilemap'teki tile'lari oku
            BoundsInt bounds = BuildableZoneTilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    var tilePos = new Vector3Int(x, y, 0);
                    if (BuildableZoneTilemap.HasTile(tilePos))
                    {
                        // World → grid koordinat cevrimi
                        int gx = x - GridOrigin.x;
                        int gy = y - GridOrigin.y;
                        if (gx >= 0 && gx < GridWidth && gy >= 0 && gy < GridHeight)
                            _grid[gx, gy] = 0;
                    }
                }
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

            return true;
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

            // 4. Entity grid'e yaz (sol-alt kose referans)
            _entityGrid[gridX, gridY] = entity;

            // 5. Tilemap'e gorsel tile koy
            PlaceVisualTile(config, gridX, gridY);

            return true;
        }

        /// <summary>
        /// Bina kaldir. M1.7 icin — su an stub.
        /// </summary>
        public void RemoveBuilding(int gridX, int gridY)
        {
            // M1.7'de implement edilecek
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

            // Gorsel tilemap'i temizle
            if (BuildingVisualTilemap != null)
                BuildingVisualTilemap.ClearAllTiles();
        }

        /// <summary>
        /// World pozisyonunu grid koordinatina cevir.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x) - GridOrigin.x;
            int gy = Mathf.FloorToInt(worldPos.y) - GridOrigin.y;
            return new Vector2Int(gx, gy);
        }

        /// <summary>
        /// Grid koordinatini world pozisyonuna cevir (bina merkezine).
        /// </summary>
        public Vector3 GridToWorld(int gridX, int gridY, BuildingConfigSO config)
        {
            float worldX = gridX + GridOrigin.x + config.GridWidth * 0.5f;
            float worldY = gridY + GridOrigin.y + config.GridHeight * 0.5f;
            return new Vector3(worldX, worldY, 0f);
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

            return entity;
        }

        private void PlaceVisualTile(BuildingConfigSO config, int gridX, int gridY)
        {
            if (BuildingVisualTilemap == null || config.GhostSprite == null) return;

            // Binanin merkez hucresine gorsel koy
            // Tam tile olusturma M1.4+'te yapilacak — su an sadece placeholder
            int centerX = gridX + config.GridWidth / 2 + GridOrigin.x;
            int centerY = gridY + config.GridHeight / 2 + GridOrigin.y;

            // Sprite'tan Tile olustur (runtime)
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = config.GhostSprite;
            tile.color = Color.white;
            BuildingVisualTilemap.SetTile(new Vector3Int(centerX, centerY, 0), tile);
        }
    }
}
