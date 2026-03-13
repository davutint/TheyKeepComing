using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private EntityManager _entityManager;
        private Entity _gameStateEntity;
        private Entity _waveStateEntity;
        private Entity _castleEntity;
        private Entity _archerPrefabEntity;
        private bool _initialized;

        public GameStateData GameState { get; private set; }
        public WaveStateData WaveState { get; private set; }
        public WallSegment Wall { get; private set; }
        public GateComponent Gate { get; private set; }
        public CastleHP Castle { get; private set; }
        public ResourceData Resources { get; private set; }
        public ResourceProductionRate ResourceProduction { get; private set; }
        public ResourceConsumptionRate ResourceConsumption { get; private set; }
        public PopulationState Population { get; private set; }
        public ArrowSupply ArrowSupply { get; private set; }

        public event System.Action OnGameOver;
        public event System.Action OnLevelUp;
        public event System.Action OnWaveChanged;
        public event System.Action OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!TryInitialize())
                return;

            ReadECSData();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            _entityManager = world.EntityManager;

            var query = _entityManager.CreateEntityQuery(typeof(GameStateData));
            if (query.IsEmpty) return false;

            _gameStateEntity = query.GetSingletonEntity();
            _waveStateEntity = _gameStateEntity; // ayni entity uzerinde

            var archerPrefabQuery = _entityManager.CreateEntityQuery(typeof(ArcherPrefabData));
            if (archerPrefabQuery.IsEmpty) return false;

            _archerPrefabEntity = _entityManager.GetComponentData<ArcherPrefabData>(
                archerPrefabQuery.GetSingletonEntity()).ArcherPrefab;

            var castleQuery = _entityManager.CreateEntityQuery(typeof(CastleHP));
            if (castleQuery.IsEmpty) return false;

            _castleEntity = castleQuery.GetSingletonEntity();
            _initialized = true;
            return true;
        }

        private void ReadECSData()
        {
            if (!_entityManager.Exists(_gameStateEntity) || !_entityManager.Exists(_castleEntity))
            {
                _initialized = false;
                return;
            }

            var prevGameState = GameState;
            var prevWaveState = WaveState;

            GameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            WaveState = _entityManager.GetComponentData<WaveStateData>(_gameStateEntity);
            Resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            ResourceProduction = _entityManager.GetComponentData<ResourceProductionRate>(_gameStateEntity);
            ResourceConsumption = _entityManager.GetComponentData<ResourceConsumptionRate>(_gameStateEntity);
            Population = _entityManager.GetComponentData<PopulationState>(_gameStateEntity);
            ArrowSupply = _entityManager.GetComponentData<ArrowSupply>(_gameStateEntity);
            Wall = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            Gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            Castle = _entityManager.GetComponentData<CastleHP>(_castleEntity);

            OnGameStateChanged?.Invoke();

            if (GameState.IsGameOver && !prevGameState.IsGameOver)
                OnGameOver?.Invoke();

            if (GameState.IsLevelUpPending && !prevGameState.IsLevelUpPending)
                OnLevelUp?.Invoke();

            if (WaveState.CurrentWave != prevWaveState.CurrentWave)
                OnWaveChanged?.Invoke();
        }

        public void ApplyUpgrade(UpgradeType type)
        {
            if (!_initialized || !_entityManager.Exists(_gameStateEntity)) return;

            var gameState = _entityManager.GetComponentData<GameStateData>(_gameStateEntity);
            gameState.IsLevelUpPending = false;
            gameState.Level++;
            gameState.XP -= gameState.XPToNextLevel;
            gameState.XPToNextLevel = (int)(gameState.XPToNextLevel * 1.5f);

            switch (type)
            {
                case UpgradeType.AddArcher:
                    SpawnArcher();
                    break;

                case UpgradeType.ArrowDamageUp:
                    UpgradeArcherDamage(5f);
                    break;

                case UpgradeType.RepairGate:
                    RepairGate();
                    break;
            }

            _entityManager.SetComponentData(_gameStateEntity, gameState);
        }

        private void SpawnArcher()
        {
            var archerCount = _entityManager.CreateEntityQuery(typeof(ArcherUnit)).CalculateEntityCount();

            var entity = _entityManager.Instantiate(_archerPrefabEntity);
            _entityManager.SetComponentData(entity, new ArcherUnit
            {
                FireRate = 1.5f,
                FireTimer = 0f,
                ArrowDamage = 10f,
                Range = 15f
            });
            _entityManager.SetComponentData(entity, Unity.Transforms.LocalTransform.FromPosition(
                new float3(3.76f, -5f + archerCount * 2f, -1f)));
        }

        private void UpgradeArcherDamage(float amount)
        {
            var query = _entityManager.CreateEntityQuery(typeof(ArcherUnit));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in entities)
            {
                var archer = _entityManager.GetComponentData<ArcherUnit>(e);
                archer.ArrowDamage += amount;
                _entityManager.SetComponentData(e, archer);
            }
            entities.Dispose();
        }

        private void RepairGate()
        {
            if (!_entityManager.Exists(_castleEntity)) return;
            var gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            gate.CurrentHP = gate.MaxHP;
            _entityManager.SetComponentData(_castleEntity, gate);
        }

        public void RestartGame()
        {
            if (!_initialized || !_entityManager.Exists(_gameStateEntity) || !_entityManager.Exists(_castleEntity))
            {
                _initialized = false;
                return;
            }

            // Tum zombileri sil
            var zombieQuery = _entityManager.CreateEntityQuery(typeof(ZombieTag));
            _entityManager.DestroyEntity(zombieQuery);

            // Tum oklari sil
            var arrowQuery = _entityManager.CreateEntityQuery(typeof(ArrowTag));
            _entityManager.DestroyEntity(arrowQuery);

            // Tum mancinik mermilerini sil
            var catapultProjectileQuery = _entityManager.CreateEntityQuery(typeof(CatapultProjectileTag));
            _entityManager.DestroyEntity(catapultProjectileQuery);

            // Tum mancinik entity'lerini sil
            var catapultQuery = _entityManager.CreateEntityQuery(typeof(CatapultUnit));
            _entityManager.DestroyEntity(catapultQuery);

            // Sur slotlarini sifirla
            if (WallSlotManager.Instance != null)
                WallSlotManager.Instance.ResetSlots();

            // Tum bina entity'lerini sil
            var buildingQuery = _entityManager.CreateEntityQuery(typeof(BuildingData));
            _entityManager.DestroyEntity(buildingQuery);

            // Grid'i sifirla
            if (BuildingGridManager.Instance != null)
                BuildingGridManager.Instance.ResetGrid();

            // Detay paneli kapat
            if (BuildingDetailUI.Instance != null)
                BuildingDetailUI.Instance.CloseDetail();

            // Game state resetle
            _entityManager.SetComponentData(_gameStateEntity, new GameStateData
            {
                XP = 0,
                Level = 1,
                XPToNextLevel = 100,
                IsGameOver = false,
                IsLevelUpPending = false
            });

            _entityManager.SetComponentData(_gameStateEntity, new WaveStateData
            {
                CurrentWave = 1,
                ZombiesToSpawn = 500,
                ZombiesSpawned = 0,
                ZombiesAlive = 0,
                SpawnTimer = 0f,
                SpawnInterval = 0.05f,
                ZombieHP = 20f,
                ZombieDamage = 5f,
                ZombieSpeed = 1.5f,
                WaveActive = true,
                WaveStartDelay = 3f,
                WaveStartTimer = 3f
            });

            // Kaynak resetle
            _entityManager.SetComponentData(_gameStateEntity, new ResourceData
            {
                Wood = 100,
                Stone = 50,
                Iron = 20,
                Food = 100
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceProductionRate
            {
                WoodPerMin = 0f,
                StonePerMin = 0f,
                IronPerMin = 0f,
                FoodPerMin = 0f
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceConsumptionRate
            {
                WoodPerMin = 0f,
                StonePerMin = 0f,
                IronPerMin = 0f,
                FoodPerMin = 0f
            });

            _entityManager.SetComponentData(_gameStateEntity, new ResourceAccumulator
            {
                Wood = 0f,
                Stone = 0f,
                Iron = 0f,
                Food = 0f
            });

            // Ok envanter resetle
            _entityManager.SetComponentData(_gameStateEntity, new ArrowSupply
            {
                Current = 50,
                Accumulator = 0f
            });

            // Nufus resetle
            _entityManager.SetComponentData(_gameStateEntity, new PopulationState
            {
                Total = 10,
                Workers = 0,
                Archers = 0,
                Idle = 10,
                Capacity = 20,
                BaseCapacity = 20,
                FoodPerAssignedPerMin = 2f
            });

            // Kale resetle
            var castle = _entityManager.GetComponentData<WallSegment>(_castleEntity);
            castle.CurrentHP = castle.MaxHP;
            _entityManager.SetComponentData(_castleEntity, castle);

            var gate = _entityManager.GetComponentData<GateComponent>(_castleEntity);
            gate.CurrentHP = gate.MaxHP;
            _entityManager.SetComponentData(_castleEntity, gate);

            var castleHP = _entityManager.GetComponentData<CastleHP>(_castleEntity);
            castleHP.CurrentHP = castleHP.MaxHP;
            _entityManager.SetComponentData(_castleEntity, castleHP);

            // Kale yukseltme resetle
            if (_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
            {
                var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);
                upgrade.Level = 0;
                _entityManager.SetComponentData(_castleEntity, upgrade);
            }
        }

        /// <summary>
        /// Kaleyi bir seviye yukseltir. Basarili ise true doner.
        /// </summary>
        public bool UpgradeCastle()
        {
            if (!_initialized || !_entityManager.Exists(_castleEntity) || !_entityManager.Exists(_gameStateEntity))
                return false;

            // CastleUpgradeData oku
            if (!_entityManager.HasComponent<CastleUpgradeData>(_castleEntity))
                return false;

            var upgrade = _entityManager.GetComponentData<CastleUpgradeData>(_castleEntity);

            // Maks seviye kontrolu
            if (upgrade.Level >= upgrade.MaxLevel)
                return false;

            // Kaynak yeterliligi kontrolu
            var resources = _entityManager.GetComponentData<ResourceData>(_gameStateEntity);
            if (resources.Wood < upgrade.WoodCostPerLevel || resources.Stone < upgrade.StoneCostPerLevel)
                return false;

            // Kaynaklari dus
            resources.Wood -= upgrade.WoodCostPerLevel;
            resources.Stone -= upgrade.StoneCostPerLevel;
            _entityManager.SetComponentData(_gameStateEntity, resources);

            // Seviye artir
            upgrade.Level++;
            _entityManager.SetComponentData(_castleEntity, upgrade);

            return true;
        }
    }

    public enum UpgradeType
    {
        AddArcher,
        ArrowDamageUp,
        RepairGate
    }
}
